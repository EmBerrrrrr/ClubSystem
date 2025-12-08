using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;

namespace ClubSystem.Controller
{
    [ApiController]
    [Route("api/club-leader-requests")]
    public class ClubLeaderRequestsController : ControllerBase
    {
        private readonly IClubLeaderRequestService _service;

        public ClubLeaderRequestsController(IClubLeaderRequestService service)
        {
            _service = service;
        }

        // STUDENT SEND REQUEST
        [Authorize(Roles = "student")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateLeaderRequestDto dto)
        {
            var studentId = User.GetAccountId();

            await _service.CreateRequestAsync(
                studentId,
                dto.Reason
            );

            return Ok("Request submitted");
        }

        // ADMIN VIEW PENDING
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> GetPending()
        {
            var data = await _service.GetPendingAsync();
            return Ok(data);
        }

        // ADMIN APPROVE
        [Authorize(Roles = "admin")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            int adminId = User.GetAccountId();

            await _service.ApproveAsync(id, adminId);

            return Ok("Approved");
        }

        // ADMIN REJECT
        [Authorize(Roles = "admin")]
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(
            int id,
            [FromBody] ProcessLeaderRequestDto dto)
        {
            int adminId = User.GetAccountId();

            await _service.RejectAsync(
                id,
                adminId,
                dto.RejectReason ?? ""
            );

            return Ok("Rejected");
        }
    }
}
