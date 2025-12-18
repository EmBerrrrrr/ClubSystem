using DTO.DTO.Club;
using DTO.DTO.Membership;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Helper;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    /// <summary>
    /// Service xử lý các thao tác liên quan đến membership dành cho sinh viên (student).
    /// 
    /// Các chức năng chính:
    /// - Lấy thông tin cá nhân để điền form yêu cầu tham gia CLB
    /// - Gửi yêu cầu tham gia CLB (MembershipRequest)
    /// - Xem danh sách các yêu cầu đã gửi
    /// - Xem chi tiết một yêu cầu
    /// - Xem danh sách các CLB mà sinh viên đã là thành viên chính thức
    /// 
    /// Luồng chính của hệ thống membership:
    /// 1. Student → POST /api/student/membership/request → SendMembershipRequestAsync
    ///    → Tạo MembershipRequest (Status = "Pending")
    /// 2. Club Leader → POST /api/leader/membership/{id}/approve
    ///    → Cập nhật MembershipRequest + Tạo Membership (Status = "active" hoặc "pending_payment")
    /// 3. Nếu có phí → Student thanh toán → Membership.Status = "active"
    /// 4. Chỉ khi Membership.Status = "active" → Student mới được register Activity
    /// </summary>
    public class StudentMembershipService : IStudentMembershipService
    {
        private readonly IMembershipRequestRepository _reqRepo;
        private readonly IMembershipRepository _memberRepo;
        private readonly IClubRepository _clubRepo;
        private readonly IAuthRepository _authRepo;
        private readonly IPaymentRepository _paymentRepo;

        public StudentMembershipService(
            IMembershipRequestRepository reqRepo,
            IMembershipRepository memberRepo,
            IClubRepository clubRepo,
            IAuthRepository authRepo,
            IPaymentRepository paymentRepo)
        {
            _reqRepo = reqRepo;
            _memberRepo = memberRepo;
            _clubRepo = clubRepo;
            _authRepo = authRepo;
            _paymentRepo = paymentRepo;
        }

        /// <summary>
        /// Lấy thông tin cơ bản của tài khoản để tự động điền vào form gửi yêu cầu tham gia CLB.
        /// 
        /// API: GET /api/student/membership/account-info
        /// Luồng: Front-end gọi → Controller → Method này → Trả về FullName, Email, Phone từ bảng Account.
        /// Không thay đổi dữ liệu trong DB.
        /// </summary>
        public async Task<AccountInfoDto> GetAccountInfoAsync(int accountId)
        {
            var account = await _authRepo.GetAccountByIdAsync(accountId);
            if (account == null)
                throw new Exception("Không tìm thấy tài khoản.");

            return new AccountInfoDto
            {
                FullName = account.FullName ?? string.Empty,
                Email = account.Email ?? string.Empty,
                Phone = account.Phone ?? string.Empty
            };
        }

        /// <summary>
        /// Sinh viên gửi yêu cầu tham gia một câu lạc bộ.
        /// 
        /// API: POST /api/student/membership/request
        /// Luồng dữ liệu:
        /// - Front-end gửi DTO (ClubId, Reason, Major, Skills, có thể có FullName/Email/Phone)
        /// - Kiểm tra: Club tồn tại? Club có bị locked? Đã là member chưa? Đã có request pending chưa?
        /// - Cập nhật thông tin cá nhân nếu chưa có (FullName, Email, Phone)
        /// - Tạo bản ghi MembershipRequest mới với Status = "Pending"
        /// - Lưu vào bảng MembershipRequest trong DB
        /// 
        /// Tương tác:
        /// - Đây là bước đầu tiên để trở thành thành viên.
        /// - Sau khi gửi, leader sẽ approve → tạo Membership → mới được register Activity.
        /// </summary>
        public async Task SendMembershipRequestAsync(int accountId, CreateMembershipRequestDto dto)
        {
            // Kiểm tra club tồn tại và trạng thái
            var club = await _clubRepo.GetByIdAsync(dto.ClubId);
            if (club == null)
                throw new Exception("Câu lạc bộ không tồn tại.");

            if (club.Status.Equals("Locked", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Không thể gửi yêu cầu tham gia câu lạc bộ đã bị khóa.");

            // Kiểm tra đã là thành viên chưa
            if (await _memberRepo.IsMemberAsync(accountId, dto.ClubId))
                throw new Exception("Bạn đã là thành viên của câu lạc bộ này.");

            // Kiểm tra đã có request đang chờ duyệt chưa
            if (await _reqRepo.HasPendingRequestAsync(accountId, dto.ClubId))
                throw new Exception("Bạn đã gửi yêu cầu và đang chờ duyệt.");

            // Lấy thông tin account để cập nhật (nếu cần)
            var account = await _authRepo.GetAccountByIdAsync(accountId);
            if (account == null)
                throw new Exception("Không tìm thấy tài khoản.");

            // Cập nhật thông tin cá nhân nếu người dùng cung cấp và chưa có
            UpdateAccountInfoIfNeeded(account, dto);

            if (account.FullName != null || account.Email != null || account.Phone != null)
            {
                await _authRepo.UpdateAccountAsync(account);
            }

            // Tạo request mới
            var request = new MembershipRequest
            {
                AccountId = accountId,
                ClubId = dto.ClubId,
                RequestDate = DateTimeExtensions.NowVietnam(),
                Status = "Pending",
                Note = dto.Reason,                    // Lý do tham gia (hiển thị cho leader)
                Major = string.IsNullOrWhiteSpace(dto.Major) ? null : dto.Major.Trim(),
                Skills = string.IsNullOrWhiteSpace(dto.Skills) ? null : dto.Skills.Trim()
            };

            await _reqRepo.CreateRequestAsync(request);
            await _reqRepo.SaveAsync();
        }

        /// <summary>
        /// Xem danh sách tất cả các yêu cầu tham gia CLB của sinh viên hiện tại.
        /// 
        /// API: GET /api/student/membership/requests
        /// Luồng: Lấy từ bảng MembershipRequest → Join với Club để lấy tên CLB và phí → Nếu Status = "Awaiting Payment" thì lấy thêm thông tin Payment.
        /// </summary>
        public async Task<List<MembershipRequestDto>> GetMyRequestsAsync(int accountId)
        {
            var requests = await _reqRepo.GetRequestsOfAccountAsync(accountId);
            var dtos = new List<MembershipRequestDto>();

            foreach (var req in requests)
            {
                var dto = MapToMembershipRequestDto(req);

                // Nếu đang chờ thanh toán, lấy thêm thông tin payment
                if (req.Status.Equals("Awaiting Payment", StringComparison.OrdinalIgnoreCase))
                {
                    await EnrichWithPaymentInfoAsync(dto, accountId, req.ClubId);
                }

                dtos.Add(dto);
            }

            return dtos;
        }

        /// <summary>
        /// Xem chi tiết một yêu cầu tham gia cụ thể (chỉ cho phép xem request của chính mình).
        /// 
        /// API: GET /api/student/membership/{id}
        /// </summary>
        public async Task<MembershipRequestDto> GetRequestDetailAsync(int requestId, int accountId)
        {
            var request = await _reqRepo.GetByIdAsync(requestId);

            if (request == null || request.AccountId != accountId)
                return null; // Không tìm thấy hoặc không phải của user

            var dto = MapToMembershipRequestDto(request);

            // Nếu có membership liên quan (đã approve), lấy thêm payment info
            var membership = await _memberRepo.GetMembershipByAccountAndClubAsync(accountId, request.ClubId);
            if (membership != null)
            {
                await EnrichWithPaymentInfoAsync(dto, accountId, request.ClubId);
            }

            return dto;
        }

        /// <summary>
        /// Xem danh sách các câu lạc bộ mà sinh viên đã là thành viên chính thức (Membership.Status = "active").
        /// 
        /// API: GET /api/student/membership/my-clubs
        /// Luồng: Lấy từ bảng Membership → Join Club → Lấy thêm số lượng thành viên active của từng CLB.
        /// </summary>
        public async Task<List<MyMembershipDto>> GetMyMembershipsAsync(int accountId)
        {
            var memberships = await _memberRepo.GetMembershipsAsync(accountId);

            if (!memberships.Any())
                return new List<MyMembershipDto>();

            var clubIds = memberships.Select(m => m.ClubId).Distinct().ToList();
            var memberCounts = await _memberRepo.GetActiveMemberCountsByClubIdsAsync(clubIds);

            return memberships.Select(m => new MyMembershipDto
            {
                Membership = new MembershipInfo
                {
                    ClubId = m.ClubId,
                    ClubName = m.Club?.Name ?? string.Empty,
                    JoinDate = m.JoinDate,
                    Status = m.Status ?? string.Empty
                },
                Club = m.Club != null ? new ClubDto
                {
                    Id = m.Club.Id,
                    Name = m.Club.Name ?? string.Empty,
                    Description = m.Club.Description,
                    Status = m.Club.Status ?? "Unknown",
                    MembershipFee = m.Club.MembershipFee,
                    EstablishedDate = m.Club.EstablishedDate.HasValue
                        ? new DateTime(m.Club.EstablishedDate.Value.Year, m.Club.EstablishedDate.Value.Month, m.Club.EstablishedDate.Value.Day)
                        : (DateTime?)null,
                    ImageClubsUrl = m.Club.ImageClubsUrl,
                    AvatarPublicId = m.Club.AvatarPublicId,
                    Location = m.Club.Location,
                    ContactEmail = m.Club.ContactEmail,
                    ContactPhone = m.Club.ContactPhone,
                    ActivityFrequency = m.Club.ActivityFrequency,
                    MemberCount = memberCounts.GetValueOrDefault(m.ClubId, 0)
                } : null
            }).ToList();
        }

        #region Private Helper Methods

        /// <summary>
        /// Cập nhật thông tin cá nhân nếu DTO cung cấp và tài khoản chưa có dữ liệu.
        /// </summary>
        private static void UpdateAccountInfoIfNeeded(Account account, CreateMembershipRequestDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.FullName) && string.IsNullOrWhiteSpace(account.FullName))
                account.FullName = dto.FullName.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Email) && string.IsNullOrWhiteSpace(account.Email))
                account.Email = dto.Email.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Phone) && string.IsNullOrWhiteSpace(account.Phone))
                account.Phone = dto.Phone.Trim();
        }

        /// <summary>
        /// Map từ entity MembershipRequest sang DTO cơ bản.
        /// </summary>
        private static MembershipRequestDto MapToMembershipRequestDto(MembershipRequest req)
        {
            return new MembershipRequestDto
            {
                Id = req.Id,
                ClubId = req.ClubId,
                ClubName = req.Club?.Name ?? string.Empty,
                Status = req.Status,
                Note = req.Note,
                RequestDate = req.RequestDate,
                Amount = req.Club?.MembershipFee ?? 0,
                Major = req.Major,
                Skills = req.Skills
            };
        }

        /// <summary>
        /// Nếu status là Awaiting Payment hoặc đã có membership, lấy thêm thông tin thanh toán.
        /// </summary>
        private async Task EnrichWithPaymentInfoAsync(MembershipRequestDto dto, int accountId, int clubId)
        {
            var membership = await _memberRepo.GetMembershipByAccountAndClubAsync(accountId, clubId);
            if (membership == null) return;

            var payment = await _paymentRepo.GetByMembershipIdAsync(membership.Id);
            if (payment == null) return;

            dto.PaymentId = payment.Id;
            dto.Status = payment.Status; // Override status bằng trạng thái payment thực tế
            dto.OrderCode = payment.OrderCode;
        }

        #endregion
    }
}