using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Service.Interfaces;
using Service.Services.Interfaces;
using System.Security.Claims;

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

    // 🔹 Báo cáo 1 CLB
    [HttpGet("clubs/{clubId}")]
    public async Task<IActionResult> GetClubReport(int clubId)
    {
        var leaderIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (leaderIdClaim == null)
            return Unauthorized();

        int leaderId = int.Parse(leaderIdClaim.Value);

        try
        {
            var report = await _reportService
                .GetClubReportAsync(clubId, leaderId);

            return Ok(report);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    // 🔹 Báo cáo các CLB mà leader quản lý
    [HttpGet("my-clubs")]
    public async Task<IActionResult> GetMyClubsReport()
    {
        var leaderIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (leaderIdClaim == null)
            return Unauthorized("UserId not found in token");

        int leaderId = int.Parse(leaderIdClaim.Value);

        var reports = await _reportService.GetMyClubsReportAsync(leaderId);
        return Ok(reports);
    }
}
