using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Service.Helper;
using Service.Service.Interfaces;
using DTO.DTO.ClubLeader;

namespace ClubSystem.Controller
{
    [ApiController]
    [Route("api/club-leader-requests")]
    public class ClubLeaderRequestsController : ControllerBase
    {
        private readonly IClubLeaderRequestService _service;
        private readonly StudentClubManagementContext _db;

        public ClubLeaderRequestsController(
            IClubLeaderRequestService service,
            StudentClubManagementContext db)
        {
            _service = service;
            _db = db;
        }

        // ================= STUDENT SEND REQUEST =================
        [Authorize(Roles = "student")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLeaderRequestDto dto)
        {
            int studentId = User.GetAccountId();

            bool isLeader = await _db.AccountRoles
                .Include(ar => ar.Role)
                .AnyAsync(ar =>
                    ar.AccountId == studentId &&
                    ar.Role.Name.ToLower() == "clubleader");

            if (isLeader)
                return BadRequest("Bạn đã là Club Leader, không thể gửi request");

            await _service.CreateRequestAsync(studentId, dto);

            return Ok("Request submitted");
        }

        // ================= STUDENT VIEW MY REQUEST =================
        [Authorize(Roles = "student")]
        [HttpGet("my-request")]
        public async Task<IActionResult> GetMyRequest()
        {
            int studentId = User.GetAccountId();

            var data = await _service.GetMyRequestAsync(studentId);

            if (data == null)
                return NotFound("Bạn chưa gửi request nào");

            return Ok(data);
        }

        // ================= ADMIN VIEW PENDING =================
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> GetPending()
        {
            var data = await _service.GetPendingAsync();
            return Ok(data);
        }

        // ================= ADMIN APPROVE =================
        [Authorize(Roles = "admin")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(
            int id,
            [FromBody] ApproveLeaderRequestDto? dto)
        {
            int adminId = User.GetAccountId();

            await _service.ApproveAsync(
                id,
                adminId,
                dto?.AdminNote
            );

            return Ok("Approved");
        }

        // ================= ADMIN REJECT =================
        [Authorize(Roles = "admin")]
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(
            int id,
            [FromBody] RejectLeaderRequestDto dto)
        {
            int adminId = User.GetAccountId();

            await _service.RejectAsync(
                id,
                adminId,
                dto.RejectReason
            );

            return Ok("Rejected");
        }

        // ================= ADMIN VIEW DETAIL =================
        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var data = await _service.GetRequestDetailAsync(id);

            if (data == null)
                return NotFound("Request không tồn tại");

            return Ok(data);
        }
    }
}
