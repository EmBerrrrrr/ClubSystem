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

        [HttpGet("members")]
        public async Task<IActionResult> GetClubMembers()
        {
            var leaderId = User.GetAccountId();
            var result = await _service.GetClubMembersAsync(leaderId);
            return Ok(result);
        }

        [HttpGet("clubs/{clubId}/members")]
        public async Task<IActionResult> GetClubMembersByClubId(int clubId)
        {
            try
            {
                var leaderId = User.GetAccountId();
                var result = await _service.GetClubMembersByClubIdAsync(leaderId, clubId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] LeaderDecisionDto dto)
        {
            try
            {
                var leaderId = User.GetAccountId();
                await _service.ApproveAsync(leaderId, id, dto.Note);
                return Ok("Approved. Waiting payment.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] LeaderDecisionDto dto)
        {
            try
            {
                var leaderId = User.GetAccountId();
                await _service.RejectAsync(leaderId, id, dto.Note);
                return Ok("Rejected.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class LeaderDecisionDto
    {
        public string? Note { get; set; }
    }

}
