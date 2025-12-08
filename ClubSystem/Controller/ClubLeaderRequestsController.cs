using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;
using Service.Helper;

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

        [Authorize(Roles = "student")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLeaderRequestDto _)
        {
            var studentId = User.GetAccountId();
            await _service.CreateRequestAsync(studentId);
            return Ok("Request submitted");
        }


        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> GetPending()
        {
            var data = await _service.GetPendingAsync();
            return Ok(data);
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            int adminId = User.GetAccountId();
            await _service.ApproveAsync(id, adminId);
            return Ok("Approved");
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(
        int id,
        [FromBody] ProcessLeaderRequestDto dto)
        {
            int adminId = User.GetAccountId();
            await _service.RejectAsync(id, adminId, dto.RejectReason ?? "");
            return Ok("Rejected");
        }
    }

}
