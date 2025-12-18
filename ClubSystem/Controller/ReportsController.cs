using DTO.DTO.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Service.Interfaces;
using System.Security.Claims;

namespace ClubSystem.Controller
{
    /// <summary>
    /// Controller cung cấp báo cáo thống kê cho Club Leader.
    /// 
    /// Quyền truy cập: Chỉ role "clubleader"
    /// 
    /// Các endpoint:
    /// - GET api/reports/clubs/{clubId} → Báo cáo chi tiết 1 CLB (phải là leader của CLB đó)
    /// - GET api/reports/my-clubs → Báo cáo tất cả CLB mà leader đang quản lý
    /// 
    /// Tương tác với ReportService: Service kiểm tra quyền leader trước khi trả dữ liệu.
    /// </summary>
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "clubleader")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Lấy báo cáo chi tiết của một câu lạc bộ cụ thể.
        /// 
        /// API: GET /api/reports/clubs/{clubId}
        /// Luồng: Leader gọi → Service kiểm tra leader có quản lý clubId không → Trả về thống kê thành viên, hoạt động.
        /// </summary>
        [HttpGet("clubs/{clubId}")]
        public async Task<IActionResult> GetClubReport(int clubId)
        {
            var leaderId = User.GetAccountIdFromClaims();

            try
            {
                var report = await _reportService.GetClubReportAsync(clubId, leaderId);
                return Ok(report);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy báo cáo của tất cả các câu lạc bộ mà leader hiện tại đang quản lý (có thể quản lý nhiều CLB).
        /// 
        /// API: GET /api/reports/my-clubs
        /// </summary>
        [HttpGet("my-clubs")]
        public async Task<IActionResult> GetMyClubsReport()
        {
            var leaderId = User.GetAccountIdFromClaims();

            try
            {
                var reports = await _reportService.GetMyClubsReportAsync(leaderId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // Extension helper để tránh lặp code lấy accountId từ token
    internal static class ClaimsPrincipalExtensions
    {
        public static int GetAccountIdFromClaims(this ClaimsPrincipal user)
        {
            var leaderIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("UserId not found in token");

            return int.Parse(leaderIdClaim.Value);
        }
    }
}