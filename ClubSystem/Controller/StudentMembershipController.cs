using DTO.DTO.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller xử lý Membership dành cho sinh viên (gửi request, xem status, xem club đã tham gia).
    /// 
    /// Luồng chính:
    /// - Student gửi request → chờ leader approve → nếu có phí thì thanh toán → trở thành member active
    /// - Chỉ khi là member active mới được register activity
    /// </summary>
    [ApiController]
    [Route("api/student/membership")]
    public class StudentMembershipController : ControllerBase
    {
        private readonly IStudentMembershipService _service;

        public StudentMembershipController(IStudentMembershipService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy thông tin cá nhân để điền form (public - nhưng thường gọi sau login).
        /// </summary>
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
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Gửi yêu cầu tham gia CLB.
        /// </summary>
        [HttpPost("request")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> SendRequest([FromBody] CreateMembershipRequestDto dto)
        {
            var accountId = User.GetAccountId();
            try
            {
                await _service.SendMembershipRequestAsync(accountId, dto);
                return Ok(new { message = "Gửi yêu cầu tham gia câu lạc bộ thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xem danh sách tất cả yêu cầu đã gửi.
        /// </summary>
        [HttpGet("requests")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> MyRequests()
        {
            var accountId = User.GetAccountId();
            var result = await _service.GetMyRequestsAsync(accountId);
            return Ok(result);
        }

        /// <summary>
        /// Xem chi tiết một yêu cầu (chỉ request của chính mình).
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> GetRequestDetail(int id)
        {
            var accountId = User.GetAccountId();
            var dto = await _service.GetRequestDetailAsync(id, accountId);
            return dto == null ? NotFound("Yêu cầu không tồn tại hoặc không thuộc về bạn.") : Ok(dto);
        }

        /// <summary>
        /// Xem danh sách các CLB mà mình đã là thành viên chính thức.
        /// </summary>
        [HttpGet("my-clubs")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> MyClubs()
        {
            var accountId = User.GetAccountId();
            var result = await _service.GetMyMembershipsAsync(accountId);
            return Ok(result);
        }

        [HttpPost("leave/{clubId}")]
        public async Task<IActionResult> LeaveClub(int clubId)
        {
            try
            {
                var accountId = User.GetAccountId();
                await _service.LeaveClubAsync(accountId, clubId);
                return Ok("Bạn đã rời khỏi câu lạc bộ thành công.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}