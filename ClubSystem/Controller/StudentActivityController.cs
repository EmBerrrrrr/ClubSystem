using System;
using DTO.DTO.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;

namespace ClubSystem.Controller
{
    [ApiController]
    [Route("api/student/activities")]
    [Authorize(Roles = "student")]
    public class StudentActivityController : ControllerBase
    {
        private readonly IStudentActivityService _service;

        public StudentActivityController(IStudentActivityService service)
        {
            _service = service;
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableActivities()
        {
            var accountId = User.GetAccountId();
            try
            {
                var result = await _service.GetAvailableActivitiesForMyClubsAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{activityId}/register")]
        public async Task<IActionResult> RegisterForActivity(int activityId)
        {
            var accountId = User.GetAccountId();
            try
            {
                await _service.RegisterForActivityAsync(accountId, activityId);
                return Ok("Đăng ký tham gia hoạt động thành công.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{activityId}/cancel")]
        public async Task<IActionResult> CancelRegistration(int activityId)
        {
            var accountId = User.GetAccountId();
            try
            {
                await _service.CancelRegistrationAsync(accountId, activityId);
                return Ok("Hủy đăng ký thành công.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetMyActivityHistory()
        {
            var accountId = User.GetAccountId();
            try
            {
                var result = await _service.GetMyActivityHistoryAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

