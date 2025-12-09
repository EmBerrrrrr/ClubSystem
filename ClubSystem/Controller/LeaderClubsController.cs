using DTO;
using DTO.DTO.Activity;
using DTO.DTO.Club;
using DTO.DTO.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Service.Interfaces;
using Service.Services;
using Service.Helper;

[ApiController]
[Route("api/leader/clubs")]
[Authorize(Roles = "clubleader")]
public class LeaderClubsController : ControllerBase
{
    private readonly ILeaderClubService _service;

    public LeaderClubsController(ILeaderClubService service)
    {
        _service = service;
    }

    private int GetCurrentAccountId()
        => User.GetAccountId();

    // GET: api/leader/clubs
    [HttpGet]
    public async Task<ActionResult<List<LeaderClubDTO>>> GetMyClubs()
    {
        var leaderId = GetCurrentAccountId();
        var clubs = await _service.GetClubsOfLeaderAsync(leaderId);
        return Ok(clubs);
    }

    // POST: api/leader/clubs
    // POST: api/leader/clubs  -> tạo club mới và gán leader hiện tại
    [HttpPost]
    public async Task<ActionResult<LeaderClubDTO>> CreateClub([FromBody] LeaderClubCreateDTO dto)
    {
        var leaderId = GetCurrentAccountId();
        var club = await _service.CreateClubAsync(leaderId, dto);
        return CreatedAtAction(nameof(GetClubById), new { clubId = club.Id }, club);
    }

    // PUT: api/leader/clubs/{clubId}  -> sửa club của leader
    [HttpPut("{clubId:int}")]
    public async Task<IActionResult> UpdateClub(int clubId, [FromBody] LeaderClubUpdateDTO dto)
    {
        var leaderId = GetCurrentAccountId();
        var ok = await _service.UpdateClubAsync(leaderId, clubId, dto);
        if (!ok) return Forbid();   // không phải club của mình
        return NoContent();
    }

    // DELETE: api/leader/clubs/{clubId}  -> xóa club của leader
    [HttpDelete("{clubId:int}")]
    public async Task<IActionResult> DeleteClub(int clubId)
    {
        var leaderId = GetCurrentAccountId();
        var ok = await _service.DeleteClubAsync(leaderId, clubId);
        if (!ok) return Forbid();
        return NoContent();
    }

    // optional: GET 1 club cho screen edit
    [HttpGet("{clubId:int}")]
    public async Task<ActionResult<LeaderClubDTO>> GetClubById(int clubId)
    {
        var leaderId = GetCurrentAccountId();
        var club = await _service.GetClubOfLeaderAsync(leaderId, clubId);
        if (club == null) return Forbid();
        return Ok(club);
    }

    // ========== MEMBERSHIP REQUESTS ==========

    // GET: api/leader/clubs/{clubId}/membership-requests?status=pending
    [HttpGet("{clubId:int}/membership-requests")]
    public async Task<ActionResult<List<MembershipRequestDTO>>> GetMembershipRequests(
        int clubId, [FromQuery] string? status = "pending")
    {
        var leaderId = GetCurrentAccountId();
        var list = await _service.GetMembershipRequestsAsync(leaderId, clubId, status);
        return Ok(list);
    }

    // POST: api/leader/clubs/{clubId}/membership-requests/{requestId}/approve
    [HttpPost("{clubId:int}/membership-requests/{requestId:int}/approve")]
    public async Task<IActionResult> ApproveMembershipRequest(int clubId, int requestId)
    {
        var leaderId = GetCurrentAccountId();
        var ok = await _service.ApproveMembershipRequestAsync(leaderId, clubId, requestId);
        if (!ok) return Forbid();      // không phải leader của club hoặc request không hợp lệ
        return Ok();
    }

    // POST: api/leader/clubs/{clubId}/membership-requests/{requestId}/reject
    [HttpPost("{clubId:int}/membership-requests/{requestId:int}/reject")]
    public async Task<IActionResult> RejectMembershipRequest(
        int clubId, int requestId, [FromBody] RejectRequestDTO dto)
    {
        var leaderId = GetCurrentAccountId();
        var ok = await _service.RejectMembershipRequestAsync(leaderId, clubId, requestId, dto.Reason);
        if (!ok) return Forbid();
        return Ok();
    }

    // ========== MEMBERS (ĐÃ ĐƯỢC DUYỆT) ==========

    // GET: api/leader/clubs/{clubId}/members
    [HttpGet("{clubId:int}/members")]
    public async Task<ActionResult<List<MembershipDTO>>> GetMembers(int clubId)
    {
        var leaderId = GetCurrentAccountId();
        var list = await _service.GetMembersAsync(leaderId, clubId);
        return Ok(list);
    }

    // PUT: api/leader/clubs/{clubId}/members/{membershipId}/status
    [HttpPut("{clubId:int}/members/{membershipId:int}/status")]
    public async Task<IActionResult> UpdateMemberStatus(
        int clubId, int membershipId, [FromBody] UpdateStatusDTO dto)
    {
        var leaderId = GetCurrentAccountId();
        var ok = await _service.UpdateMemberStatusAsync(leaderId, clubId, membershipId, dto.Status);
        if (!ok) return Forbid();
        return NoContent();
    }

    // DELETE: api/leader/clubs/{clubId}/members/{membershipId}
    [HttpDelete("{clubId:int}/members/{membershipId:int}")]
    public async Task<IActionResult> RemoveMember(int clubId, int membershipId)
    {
        var leaderId = GetCurrentAccountId();
        var ok = await _service.RemoveMemberAsync(leaderId, clubId, membershipId);
        if (!ok) return Forbid();
        return NoContent();
    }

    // GET: api/leader/clubs/{clubId}/activities
    [HttpGet("{clubId:int}/activities")]
    public async Task<ActionResult<List<ActivityDTO>>> GetActivities(int clubId)
    {
        var leaderId = GetCurrentAccountId();
        var list = await _service.GetActivitiesAsync(leaderId, clubId);
        return Ok(list);
    }

    // POST: api/leader/clubs/{clubId}/activities
    [HttpPost("{clubId:int}/activities")]
    public async Task<ActionResult<ActivityDTO>> CreateActivity(
        int clubId, [FromBody] ActivityCreateDTO dto)
    {
        var leaderId = GetCurrentAccountId();
        var activity = await _service.CreateActivityAsync(leaderId, clubId, dto);
        if (activity == null) return Forbid();

        return CreatedAtAction(nameof(GetActivities),
            new { clubId }, activity);
    }

    // PUT: api/leader/clubs/{clubId}/activities/{activityId}
    [HttpPut("{clubId:int}/activities/{activityId:int}")]
    public async Task<IActionResult> UpdateActivity(
        int clubId, int activityId, [FromBody] ActivityUpdateDTO dto)
    {
        var leaderId = GetCurrentAccountId();
        var ok = await _service.UpdateActivityAsync(leaderId, clubId, activityId, dto);
        if (!ok) return Forbid();
        return NoContent();
    }

    // DELETE: api/leader/clubs/{clubId:int}/activities/{activityId:int}
    [HttpDelete("{clubId:int}/activities/{activityId:int}")]
    public async Task<IActionResult> DeleteActivity(int clubId, int activityId)
    {
        var leaderId = GetCurrentAccountId();
        var ok = await _service.DeleteActivityAsync(leaderId, clubId, activityId);
        if (!ok) return Forbid();
        return NoContent();
    }

}