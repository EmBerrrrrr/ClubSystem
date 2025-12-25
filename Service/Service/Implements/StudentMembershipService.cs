using DTO.DTO.Club;
using DTO.DTO.Membership;
using Microsoft.EntityFrameworkCore;
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
        private readonly INotificationService _noti;  // THÊM DÒNG NÀY
        public StudentMembershipService(
            IMembershipRequestRepository reqRepo,
            IMembershipRepository memberRepo,
            IClubRepository clubRepo,
            IAuthRepository authRepo,
            IPaymentRepository paymentRepo,
            INotificationService noti)
        {
            _reqRepo = reqRepo;
            _memberRepo = memberRepo;
            _clubRepo = clubRepo;
            _authRepo = authRepo;
            _paymentRepo = paymentRepo;
            _noti = noti;
        }

        /// <summary>
        /// Lấy thông tin cơ bản của tài khoản để tự động điền vào form gửi yêu cầu tham gia CLB.
        /// 
        /// API: GET /api/student/membership/account-info
        /// Luồng: Front-end gọi → Controller → Method này → Trả về FullName, Email, Phone từ bảng Account.
        /// Không thay đổi dữ liệu trong DB.
        /// </summary>
        // 0) Lấy thông tin account để pre-fill form
        public async Task<AccountInfoDto> GetAccountInfoAsync(int accountId)
        {
            var account = await _authRepo.GetAccountByIdAsync(accountId);
            if (account == null)
                throw new Exception("Không tìm thấy tài khoản.");

            return new AccountInfoDto
            {
                FullName = account.FullName,
                Email = account.Email,
                Phone = account.Phone
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
        // 1) Student gửi request tham gia CLB
        public async Task SendMembershipRequestAsync(int accountId, CreateMembershipRequestDto dto)
        {
            // Club tồn tại không?
            var club = await _clubRepo.GetByIdAsync(dto.ClubId);  // THÊM: Get club
            if (club == null) throw new Exception("Club not found");
            if (club.Status != null && club.Status.ToLower() == "locked") throw new Exception("Cannot request to join locked club");  // THÊM: Check locked

            // Đã là member chưa?
            if (await _memberRepo.IsMemberAsync(accountId, dto.ClubId))
            {
                throw new Exception("Bạn đã là thành viên của CLB này.");
            }

            // Đã có request pending chưa?
            if (await _reqRepo.HasPendingRequestAsync(accountId, dto.ClubId))
                throw new Exception("Bạn đã gửi yêu cầu và đang chờ duyệt.");

            // Lấy account để cập nhật thông tin nếu cần
            var account = await _authRepo.GetAccountByIdAsync(accountId);
            if (account == null)
                throw new Exception("Không tìm thấy tài khoản.");

            // Cập nhật thông tin account nếu được cung cấp và chưa có
            bool accountUpdated = false;
            if (!string.IsNullOrWhiteSpace(dto.FullName) && string.IsNullOrWhiteSpace(account.FullName))
            {
                account.FullName = dto.FullName;
                accountUpdated = true;
            }
            if (!string.IsNullOrWhiteSpace(dto.Email) && string.IsNullOrWhiteSpace(account.Email))
            {
                account.Email = dto.Email;
                accountUpdated = true;
            }
            if (!string.IsNullOrWhiteSpace(dto.Phone) && string.IsNullOrWhiteSpace(account.Phone))
            {
                account.Phone = dto.Phone;
                accountUpdated = true;
            }

            if (accountUpdated)
            {
                await _authRepo.UpdateAccountAsync(account);
            }

            // Tạo membership request với lý do tham gia (lưu vào Note)
            var req = new MembershipRequest
            {
                AccountId = accountId,
                ClubId = dto.ClubId,
                Status = "Pending",
                RequestDate = dto.RequestDate,
                Note = dto.Reason, // Lưu lý do tham gia vào Note
                Major = string.IsNullOrWhiteSpace(dto.Major) ? null : dto.Major,
                Skills = string.IsNullOrWhiteSpace(dto.Skills) ? null : dto.Skills
            };

            await _reqRepo.CreateRequestAsync(req);
            await _reqRepo.SaveAsync();
        }

        /// <summary>
        /// Xem danh sách tất cả các yêu cầu tham gia CLB của sinh viên hiện tại.
        /// 
        /// API: GET /api/student/membership/requests
        /// Luồng: Lấy từ bảng MembershipRequest → Join với Club để lấy tên CLB và phí → Nếu Status = "Awaiting Payment" thì lấy thêm thông tin Payment.
        /// </summary>
        // 2) Student xem status các request
        public async Task<List<MembershipRequestDto>> GetMyRequestsAsync(int accountId)
        {
            var list = await _reqRepo.GetRequestsOfAccountAsync(accountId);
            var result = new List<MembershipRequestDto>();

            foreach (var x in list)
            {
                var dto = new MembershipRequestDto
                {
                    Id = x.Id,
                    ClubName = x.Club?.Name ?? "",
                    Status = x.Status,
                    Note = x.Note,
                    RequestDate = x.RequestDate,
                    Amount = x.Club?.MembershipFee ?? 0,
                    Major = x.Major,
                    Skills = x.Skills
                };

                if (x.Status != null && x.Status.ToLower() == "awaiting payment")
                {
                    // Tìm membership tương ứng (pending_payment)
                    var membership = await _memberRepo
                        .GetMembershipByAccountAndClubAsync(accountId, x.ClubId); // bạn tạo thêm hàm này

                    if (membership != null)
                    {
                        var payment = await _paymentRepo.GetByMembershipIdAsync(membership.Id);
                        if (payment != null)
                        {
                            dto.PaymentId = payment.Id;
                            dto.Status = payment.Status ?? dto.Status;
                            dto.OrderCode = payment.OrderCode;
                            // nếu bạn muốn: dto.PaymentMethod = payment.Method;
                        }
                    }
                }

                result.Add(dto);
            }

            return result;
        }

        /// <summary>
        /// Xem chi tiết một yêu cầu tham gia cụ thể (chỉ cho phép xem request của chính mình).
        /// 
        /// API: GET /api/student/membership/{id}
        /// </summary>
        public async Task<MembershipRequestDto?> GetRequestDetailAsync(int requestId, int accountId)
        {
            // tuỳ repo, có thể là GetByIdAsync hoặc GetDetailAsync kèm include Club, Account
            var x = await _reqRepo.GetByIdAsync(requestId);

            // bảo vệ: chỉ cho xem request của chính mình
            if (x == null || x.AccountId != accountId)
                return null;

            var dto = new MembershipRequestDto
            {
                Id = x.Id,
                ClubName = x.Club?.Name ?? string.Empty,
                Status = x.Status,
                Note = x.Note,
                RequestDate = x.RequestDate,
                Amount = x.Club?.MembershipFee ?? 0,
                Major = x.Major,
                Skills = x.Skills
            };

            // nếu muốn hiện luôn thông tin payment nếu đã tạo
            var membership = await _memberRepo
                .GetMembershipByAccountAndClubAsync(accountId, x.ClubId);

            if (membership != null)
            {
                var payment = await _paymentRepo.GetByMembershipIdAsync(membership.Id);
                if (payment != null)
                {
                    dto.PaymentId = payment.Id;
                    dto.Status = payment.Status ?? dto.Status;
                    dto.OrderCode = payment.OrderCode;
                }
            }

            return dto;
        }

        /// <summary>
        /// Xem danh sách các câu lạc bộ mà sinh viên đã là thành viên chính thức (Membership.Status = "active").
        /// 
        /// API: GET /api/student/membership/my-clubs
        /// Luồng: Lấy từ bảng Membership → Join Club → Lấy thêm số lượng thành viên active của từng CLB.
        /// </summary>
        // 3) Student xem CLB mình đã tham gia
        public async Task<List<MyMembershipDto>> GetMyMembershipsAsync(int accountId)
        {
            var list = await _memberRepo.GetMembershipsAsync(accountId);

            if (!list.Any())
                return new List<MyMembershipDto>();

            var clubIds = list.Select(x => x.ClubId).Distinct().ToList();
            var memberCounts = await _memberRepo.GetActiveMemberCountsByClubIdsAsync(clubIds);

            return list.Select(x => new MyMembershipDto
            {
                Membership = new MembershipInfo
                {
                    ClubId = x.ClubId,
                    ClubName = x.Club?.Name ?? "",
                    JoinDate = x.JoinDate,
                    Status = x.Status ?? ""
                },
                Club = x.Club != null ? new ClubDto
                {
                    Id = x.Club.Id,
                    Name = x.Club.Name ?? "",
                    Description = x.Club.Description,
                    Status = x.Club.Status ?? "Unknown",
                    MembershipFee = x.Club.MembershipFee,
                    EstablishedDate = x.Club.EstablishedDate.HasValue
                       ? new DateTime(x.Club.EstablishedDate.Value.Year,
                       x.Club.EstablishedDate.Value.Month,
                       x.Club.EstablishedDate.Value.Day)
                       : (DateTime?)null,
                    ImageClubsUrl = x.Club.ImageClubsUrl,
                    AvatarPublicId = x.Club.AvatarPublicId,
                    Location = x.Club.Location,
                    ContactEmail = x.Club.ContactEmail,
                    ContactPhone = x.Club.ContactPhone,
                    ActivityFrequency = x.Club.ActivityFrequency,

                    // THÊM DÒNG NÀY: Gán số thành viên vào trong ClubInfo
                    MemberCount = memberCounts.GetValueOrDefault(x.ClubId, 0)
                } : null

                // XÓA DÒNG MemberCount ở cấp ngoài nữa (nếu bạn đã thêm trước đó)
            }).ToList();
        }

        public async Task LeaveClubAsync(int accountId, int clubId)
        {
            // 1. Validate: Phải là member active của CLB đó
            var membership = await _memberRepo.GetMembershipByAccountAndClubAsync(accountId, clubId)
                            ?? throw new Exception("Bạn không phải thành viên của câu lạc bộ này.");

            if (!"active".Equals(membership.Status, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Bạn không thể rời khỏi CLB vì trạng thái thành viên không active.");

            // 2. Check CLB không bị locked (tùy chọn - có thể cho rời dù locked)
            var club = await _clubRepo.GetByIdAsync(clubId);
            if (club == null)
                throw new Exception("Không tìm thấy câu lạc bộ.");
            if (club.Status != null && club.Status.ToLower() == "locked")
                throw new Exception("Câu lạc bộ đang bị khóa, bạn không thể rời lúc này.");

            // 3. Đảm bảo payments giữ được truy vết qua account khi xóa membership
            var payments = await _paymentRepo.GetPaymentHistoryByAccountIdAsync(accountId);
            var relatedPayments = payments.Where(p => p.MembershipId == membership.Id).ToList();
            foreach (var p in relatedPayments)
            {
                p.MembershipId = null;
                p.AccountId = accountId;
                await _paymentRepo.UpdateAsync(p);
            }

            // 4. Xóa cứng Membership (FK payments/activity_participants đã SET NULL)
            await _memberRepo.DeleteMembership(membership);
            await _memberRepo.SaveAsync();

            // 5. Gửi noti cho leader(s) của CLB
            var leaderIds = await _clubRepo.GetLeaderAccountIdsByClubIdAsync(clubId);
            foreach (var leaderId in leaderIds)
            {
                _noti.Push(leaderId,
                    "Thành viên rời CLB",
                    $"Thành viên {membership.Account?.FullName ?? "ID " + accountId} đã tự nguyện rời khỏi CLB {club.Name}.");
            }

            // 6. Gửi noti cho chính student
            _noti.Push(accountId,
                "Rời CLB thành công",
                $"Bạn đã rời khỏi CLB {club.Name} thành công.");
        }
    }
}
