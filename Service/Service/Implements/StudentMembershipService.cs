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
            var club = await _clubRepo.GetByIdAsync(dto.ClubId);  // THÊM: Get club
            if (club == null) throw new Exception("Club not found");
            if (club.Status == "Locked") throw new Exception("Cannot request to join locked club");  // THÊM: Check locked

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
                RequestDate = DateTimeExtensions.NowVietnam(),
                Note = dto.Reason, // Lưu lý do tham gia vào Note
                Major = string.IsNullOrWhiteSpace(dto.Major) ? null : dto.Major,
                Skills = string.IsNullOrWhiteSpace(dto.Skills) ? null : dto.Skills
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
                    RequestDate = DateTimeExtensions.NowVietnam(),
                    Amount = x.Club?.MembershipFee ?? 0,
                    Major = x.Major,
                    Skills = x.Skills
                };

                if (x.Status == "Awaiting Payment")
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
                            dto.Status = payment.Status;
                            dto.OrderCode = payment.OrderCode;
                            // nếu bạn muốn: dto.PaymentMethod = payment.Method;
                        }
                    }
                }

                result.Add(dto);
            }

            return result;
        }

        public async Task<MembershipRequestDto> GetRequestDetailAsync(int requestId, int accountId)
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
                RequestDate = DateTimeExtensions.NowVietnam(),
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
                    dto.Status = payment.Status;
                    dto.OrderCode = payment.OrderCode;
                }
            }

            return dto;
        }

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
    }
}

