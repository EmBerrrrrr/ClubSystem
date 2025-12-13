using DTO.DTO.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Service.Interfaces;
using System;

namespace ClubSystem.Controller;

[ApiController]
[Route("api/admin/accounts")]
[Authorize(Roles = "admin")]
public class AccountsAdminController : ControllerBase
{
    private readonly IAdminAccountService _service;
    private readonly IClubLeaderRequestService _leaderRequestService;

    public AccountsAdminController(IAdminAccountService service, IClubLeaderRequestService leaderRequestService)
    {
        _service = service;
        _leaderRequestService = leaderRequestService;
    }

    // ==================== GET ====================

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAccounts());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
        => Ok(await _service.GetAccount(id));

    // ==================== LOCK / ACTIVATE ====================

    [HttpPut("{id}/lock")]
    public async Task<IActionResult> Lock(int id)
    {
        await _service.LockAccount(id);
        return Ok("Account locked");
    }

    [HttpPut("{id}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        await _service.ActivateAccount(id);
        return Ok("Account activated");
    }

    // ==================== RESET PASSWORD ====================

    [HttpPut("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
    {
        var pwd = await _service.ResetPassword(id, dto.NewPassword);
        return Ok(new { newPassword = pwd });
    }

    // ==================== ROLE MANAGEMENT ====================

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AddRole(int id, [FromBody] ModifyRoleDto dto)
    {
        await _service.AddRole(id, dto.RoleName);
        return Ok("Role added");
    }

    [HttpDelete("{id}/roles")]
    public async Task<IActionResult> RemoveRole(int id, [FromBody] ModifyRoleDto dto)
    {
        await _service.RemoveRole(id, dto.RoleName);
        return Ok("Role removed");
    }

    // ==================== LEADER REQUEST STATS ====================

    [HttpGet("leader-requests/stats")]
    public async Task<IActionResult> GetLeaderRequestStats()
    {
        var stats = await _leaderRequestService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("leader-requests/approved")]
    public async Task<IActionResult> GetApprovedLeaderRequests()
    {
        var data = await _leaderRequestService.GetApprovedAsync();
        return Ok(data);
    }

    [HttpGet("leader-requests/rejected")]
    public async Task<IActionResult> GetRejectedLeaderRequests()
    {
        var data = await _leaderRequestService.GetRejectedAsync();
        return Ok(data);
    }

    [HttpPost("hash-all-passwords")]
    public async Task<IActionResult> HashAllPasswords()
    {
        var count = await _service.HashAllPasswordsAsync();
        return Ok(new { message = $"Đã hash lại {count} password(s)", count });
    }

    // Giám sát CLB: Xem danh sách CLB với trạng thái, số thành viên, tổng doanh thu phí
    [HttpGet("clubs/monitoring")]
    public async Task<IActionResult> GetClubsForMonitoring([FromServices] IAdminClubService adminClubService)
    {
        try
        {
            var result = await adminClubService.GetAllClubsForMonitoringAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Quản lý chi tiết CLB: Xem thông tin chi tiết của một CLB bao gồm danh sách membership
    [HttpGet("clubs/{clubId}/detail")]
    public async Task<IActionResult> GetClubDetailForMonitoring(
        int clubId, 
        [FromServices] IAdminClubService adminClubService)
    {
        try
        {
            var result = await adminClubService.GetClubDetailForMonitoringAsync(clubId);
            
            if (result == null)
                return NotFound(new { message = "Không tìm thấy CLB" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
