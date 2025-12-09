using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;

namespace ClubSystem.Controller
{
    [ApiController]
    [Route("api/leader/membership")]
    [Authorize(Roles = "clubleader")]
    public class ClubLeaderMembershipController : ControllerBase
    {
        private readonly IClubLeaderMembershipService _service;

        public ClubLeaderMembershipController(IClubLeaderMembershipService service)
        {
            _service = service;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var leaderId = User.GetAccountId();
            var result = await _service.GetPendingRequestsAsync(leaderId);
            return Ok(result);
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] LeaderDecisionDto dto)
        {
            var leaderId = User.GetAccountId();
            await _service.ApproveAsync(leaderId, id, dto.Note);
            return Ok("Approved. Waiting payment.");
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] LeaderDecisionDto dto)
        {
            var leaderId = User.GetAccountId();
            await _service.RejectAsync(leaderId, id, dto.Note);
            return Ok("Rejected.");
        }
    }

    public class LeaderDecisionDto
    {
        public string? Note { get; set; }
    }

}
