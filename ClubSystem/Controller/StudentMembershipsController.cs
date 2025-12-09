using DTO;
using DTO.DTO.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Service.Interfaces;

[ApiController]
[Route("api/student/memberships")]
[Authorize(Roles = "student")]
public class StudentMembershipsController : ControllerBase
{
    private readonly IStudentMembershipService _service;

    public StudentMembershipsController(IStudentMembershipService service)
    {
        _service = service;
    }

    private int GetCurrentAccountId()
        => int.Parse(User.Claims.First(c => c.Type == "AccountId").Value);

    // GET: api/student/memberships/requests
    // -> xem trạng thái tất cả đơn: pending / approved / rejected
    [HttpGet("requests")]
    public async Task<ActionResult<List<MembershipRequestDTO>>> GetMyRequests()
    {
        var accountId = GetCurrentAccountId();
        var list = await _service.GetMyRequestsAsync(accountId);
        return Ok(list);
    }

    // GET: api/student/memberships/my-clubs
    // -> xem các CLB mình đang tham gia (membership active)
    [HttpGet("my-clubs")]
    public async Task<ActionResult<List<MembershipDTO>>> GetMyClubs()
    {
        var accountId = GetCurrentAccountId();
        var list = await _service.GetMyClubsAsync(accountId);
        return Ok(list);
    }
}
