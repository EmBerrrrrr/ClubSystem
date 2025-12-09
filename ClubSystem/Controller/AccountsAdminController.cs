using DTO.DTO.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Service.Interfaces;

namespace StudentClubAPI.Controllers;

[ApiController]
[Route("api/admin/accounts")]
[Authorize(Roles = "admin")]
public class AccountsAdminController : ControllerBase
{
    private readonly IAdminAccountService _service;

    public AccountsAdminController(IAdminAccountService service)
    {
        _service = service;
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
}
