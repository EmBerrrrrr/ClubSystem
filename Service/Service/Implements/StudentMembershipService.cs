using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;
using DTO.DTO.Membership;

namespace Service.Service.Implements
{
    public class StudentMembershipService : IStudentMembershipService
    {
        private readonly IMembershipRequestRepository _reqRepo;
        private readonly IMembershipRepository _memberRepo;
        private readonly IClubRepository _clubRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IAuthRepository _authRepo;

        public StudentMembershipService(
            IMembershipRequestRepository reqRepo,
            IMembershipRepository memberRepo,
            IClubRepository clubRepo,
            IPaymentRepository paymentRepo,
            IAuthRepository authRepo)
        {
            _reqRepo = reqRepo ?? throw new ArgumentNullException(nameof(reqRepo));
            _memberRepo = memberRepo ?? throw new ArgumentNullException(nameof(memberRepo));
            _clubRepo = clubRepo ?? throw new ArgumentNullException(nameof(clubRepo));
            _paymentRepo = paymentRepo ?? throw new ArgumentNullException(nameof(paymentRepo));
            _authRepo = authRepo ?? throw new ArgumentNullException(nameof(authRepo));
        }

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

        // 1) Student gửi request tham gia CLB
        public async Task SendMembershipRequestAsync(int accountId, CreateMembershipRequestDto dto)
        {
            // Club tồn tại không?
            var club = await _clubRepo.GetByIdAsync(dto.ClubId);
            if (club == null)
                throw new Exception("Câu lạc bộ không tồn tại.");

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
                Status = "pending",
                RequestDate = DateTime.UtcNow,
                Note = dto.Reason // Lưu lý do tham gia vào Note
            };

            await _reqRepo.CreateRequestAsync(req);
            await _reqRepo.SaveAsync();
        }

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
                    RequestDate = x.RequestDate
                };

                // Luôn hiển thị amount (phí thành viên) để student biết số tiền cần thanh toán
                dto.Amount = x.Club?.MembershipFee ?? 0;

                // Nếu status là approved_pending_payment, tìm payment nếu có
                if (x.Status == "approved_pending_payment")
                {
                    var payment = await _paymentRepo.GetPaymentByMembershipRequestIdAsync(x.Id);
                    if (payment != null)
                    {
                        dto.PaymentId = payment.Id;
                    }
                }

                result.Add(dto);
            }

            return result;
        }

        // 3) Student xem CLB mình đã tham gia
        public async Task<List<MyMembershipDto>> GetMyMembershipsAsync(int accountId)
        {
            var list = await _memberRepo.GetMembershipsAsync(accountId);

            return list.Select(x => new MyMembershipDto
            {
                ClubId = x.ClubId,
                ClubName = x.Club?.Name ?? "",
                JoinDate = x.JoinDate,
                Status = x.Status
            }).ToList();
        }
    }
}
