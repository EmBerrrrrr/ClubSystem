using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces; // nếu có interface IStudentMembershipService
using DTO.DTO.Membership; // nơi đặt MembershipRequestDto, MyMembershipDto

namespace Service.Service.Implements
{
    public class StudentMembershipService : IStudentMembershipService
    {
        private readonly IMembershipRequestRepository _reqRepo;
        private readonly IMembershipRepository _memberRepo;
        private readonly IClubRepository _clubRepo;

        public StudentMembershipService(
            IMembershipRequestRepository reqRepo,
            IMembershipRepository memberRepo,
            IClubRepository clubRepo)
        {
            _reqRepo = reqRepo ?? throw new ArgumentNullException(nameof(reqRepo));
            _memberRepo = memberRepo ?? throw new ArgumentNullException(nameof(memberRepo));
            _clubRepo = clubRepo ?? throw new ArgumentNullException(nameof(clubRepo));
        }

        // 1) Student gửi request tham gia CLB
        public async Task SendMembershipRequestAsync(int accountId, int clubId)
        {
            // Club tồn tại không?
            var club = await _clubRepo.GetByIdAsync(clubId);
            if (club == null)
                throw new Exception("Câu lạc bộ không tồn tại.");

            // Đã là member chưa?
            if (await _memberRepo.IsMemberAsync(accountId, clubId))
            {
                throw new Exception("Bạn đã là thành viên của CLB này.");
            }

            // Đã có request pending chưa? (note: gọi HasPendingRequestAsync)
            if (await _reqRepo.HasPendingRequestAsync(accountId, clubId))
                throw new Exception("Bạn đã gửi yêu cầu và đang chờ duyệt.");

            var req = new MembershipRequest
            {
                AccountId = accountId,
                ClubId = clubId,
                Status = "pending",
                RequestDate = DateTime.UtcNow
            };

            await _reqRepo.CreateRequestAsync(req);   // dùng tên interface
            await _reqRepo.SaveAsync();
        }

        // 2) Student xem status các request
        public async Task<List<MembershipRequestDto>> GetMyRequestsAsync(int accountId)
        {
            var list = await _reqRepo.GetRequestsOfAccountAsync(accountId);

            return list.Select(x => new MembershipRequestDto
            {
                Id = x.Id,
                ClubName = x.Club?.Name ?? "",
                Status = x.Status,
                Note = x.Note,
                RequestDate = x.RequestDate
            }).ToList();
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
