using System;
using DTO.DTO.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/student/membership")]
    public class StudentMembershipController : ControllerBase
    {
        private readonly IStudentMembershipService _service;

        public StudentMembershipController(IStudentMembershipService service)
        {
            _service = service;
        }

        [HttpGet("account-info")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> GetAccountInfo()
        {
            var accountId = User.GetAccountId();
            try
            {
                var result = await _service.GetAccountInfoAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("request")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> SendRequest([FromBody] CreateMembershipRequestDto dto)
        {
            var accountId = User.GetAccountId();
            try
            {
                await _service.SendMembershipRequestAsync(accountId, dto);
                return Ok("Gửi yêu cầu thành công.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("requests")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> MyRequests()
        {
            var accountId = User.GetAccountId();
            var result = await _service.GetMyRequestsAsync(accountId);
            return Ok(result);
        }

        [HttpGet("my-clubs")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> MyClubs()
        {
            var accountId = User.GetAccountId();
            var result = await _service.GetMyMembershipsAsync(accountId);
            return Ok(result);
        }
    }
}
