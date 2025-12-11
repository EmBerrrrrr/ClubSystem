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

        // Xem tất cả activities của tất cả CLB (không cần là member) - chỉ để xem
        [HttpGet("view-all")]
        public async Task<IActionResult> GetAllActivitiesForViewing()
        {
            try
            {
                var result = await _service.GetAllActivitiesForViewingAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Xem activities của một CLB cụ thể (không cần là member) - chỉ để xem
        [HttpGet("view-club/{clubId}")]
        public async Task<IActionResult> GetActivitiesByClubForViewing(int clubId)
        {
            try
            {
                var result = await _service.GetActivitiesByClubForViewingAsync(clubId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Lấy activities mà student (đã là member) có thể đăng ký
        [HttpGet("for-registration")]
        public async Task<IActionResult> GetActivitiesForRegistration()
        {
            var accountId = User.GetAccountId();
            try
            {
                var result = await _service.GetActivitiesForRegistrationAsync(accountId);
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
                return Ok("Đăng ký tham gia hoạt động thành công (trạng thái: attend).");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{activityId}/cancel")]
        public async Task<IActionResult> CancelRegistration(int activityId, [FromBody] CancelActivityRegistrationDto? dto)
        {
            var accountId = User.GetAccountId();
            try
            {
                await _service.CancelRegistrationAsync(accountId, activityId, dto?.Reason);
                return Ok("Hủy đăng ký thành công (trạng thái: cancel).");
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

