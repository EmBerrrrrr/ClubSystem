using DTO.DTO.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;

namespace ClubSystem.Controller
{
    /// <summary>
    /// Controller xử lý Activity dành cho sinh viên (role: student).
    /// 
    /// Các endpoint:
    /// - Xem activity (public/viewing)
    /// - Đăng ký/hủy đăng ký (chỉ member active mới được)
    /// - Xem lịch sử tham gia
    /// </summary>
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

        /// <summary>
        /// Xem tất cả activity đang mở đăng ký (không cần là member - chỉ để xem).
        /// </summary>
        [HttpGet("view-all")]
        public async Task<IActionResult> GetAllActivitiesForViewing()
        {
            var result = await _service.GetAllActivitiesForViewingAsync();
            return Ok(result);
        }

        /// <summary>
        /// Xem activity của một CLB cụ thể (không cần là member).
        /// </summary>
        [HttpGet("view-club/{clubId}")]
        public async Task<IActionResult> GetActivitiesByClubForViewing(int clubId)
        {
            var result = await _service.GetActivitiesByClubForViewingAsync(clubId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách activity mà student có thể đăng ký (chỉ những CLB mình là member active).
        /// </summary>
        [HttpGet("for-registration")]
        public async Task<IActionResult> GetActivitiesForRegistration()
        {
            var accountId = User.GetAccountId();
            var result = await _service.GetActivitiesForRegistrationAsync(accountId);
            return Ok(result);
        }

        /// <summary>
        /// Đăng ký tham gia activity (phải là member active).
        /// </summary>
        [HttpPost("{activityId}/register")]
        public async Task<IActionResult> RegisterForActivity(int activityId)
        {
            var accountId = User.GetAccountId();
            try
            {
                await _service.RegisterForActivityAsync(accountId, activityId);
                return Ok(new { message = "Đăng ký tham gia hoạt động thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Hủy đăng ký tham gia activity.
        /// </summary>
        [HttpDelete("{activityId}/cancel")]
        public async Task<IActionResult> CancelRegistration(int activityId, [FromBody] CancelActivityRegistrationDto? dto)
        {
            var accountId = User.GetAccountId();
            try
            {
                await _service.CancelRegistrationAsync(accountId, activityId, dto?.Reason);
                return Ok(new { message = "Hủy đăng ký thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xem lịch sử tham gia activity của bản thân.
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetMyActivityHistory()
        {
            var accountId = User.GetAccountId();
            var result = await _service.GetMyActivityHistoryAsync(accountId);
            return Ok(result);
        }
    }
}