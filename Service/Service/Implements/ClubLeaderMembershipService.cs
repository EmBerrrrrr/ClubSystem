using DTO.DTO.Membership;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    public class ClubLeaderMembershipService : IClubLeaderMembershipService
    {
        private readonly IMembershipRequestRepository _reqRepo;
        private readonly IClubRepository _clubRepo;

        public ClubLeaderMembershipService(
            IMembershipRequestRepository reqRepo,
            IClubRepository clubRepo)
        {
            _reqRepo = reqRepo;
            _clubRepo = clubRepo;
        }

        // Lấy các request pending của tất cả CLB mà leader này quản lý
        public async Task<List<MembershipRequestForLeaderDto>> GetPendingRequestsAsync(int leaderId)
        {
            var myClubs = await _clubRepo.GetByLeaderIdAsync(leaderId);
            var requests = new List<MembershipRequest>();

            foreach (var club in myClubs)
            {
                var list = await _reqRepo.GetPendingRequestsByClubAsync(club.Id);
                requests.AddRange(list);
            }

            return requests.Select(r => new MembershipRequestForLeaderDto
            {
                Id = r.Id,
                AccountId = r.AccountId,
                ClubId = r.ClubId,
                Status = r.Status,
                Note = r.Note,
                RequestDate = r.RequestDate,
                FullName = r.Account?.FullName,
                Email = r.Account?.Email,
                Phone = r.Account?.Phone,
                Reason = r.Note // Lý do tham gia được lưu trong Note
            }).ToList();
        }


        public async Task ApproveAsync(int leaderId, int requestId, string? note)
        {
            var req = await _reqRepo.GetByIdAsync(requestId)
                ?? throw new Exception("Không tìm thấy request.");

            // kiểm tra leader có quyền với CLB không
            if (!await _clubRepo.IsLeaderOfClubAsync(req.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (req.Status != "pending")
                throw new Exception("Yêu cầu đã được xử lý.");

            req.Status = "approved_pending_payment";
            req.ProcessedBy = leaderId;
            req.ProcessedAt = DateTime.Now;
            req.Note = note;

            await _reqRepo.UpdateAsync(req);
        }

        public async Task RejectAsync(int leaderId, int requestId, string? note)
        {
            var req = await _reqRepo.GetByIdAsync(requestId)
                ?? throw new Exception("Không tìm thấy request.");

            if (!await _clubRepo.IsLeaderOfClubAsync(req.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (req.Status != "pending")
                throw new Exception("Yêu cầu đã được xử lý.");

            req.Status = "rejected";
            req.ProcessedBy = leaderId;
            req.ProcessedAt = DateTime.Now;
            req.Note = note;

            await _reqRepo.UpdateAsync(req);
        }
    }

}
