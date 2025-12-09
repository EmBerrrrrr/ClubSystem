using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Models;
using Service.Services;

namespace StudentClubAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClubsController : ControllerBase
{
    private readonly IClubService _service;

    public ClubsController(IClubService service)
    {
        _service = service;
    }

    // GET: api/clubs
    [HttpGet]
    public async Task<ActionResult<List<ClubDTO>>> GetAll()
    {
        var clubs = await _service.GetAllClubsAsync();
        return Ok(clubs);
    }

    // GET: api/clubs/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Club>> GetById(int id)
    {
        var club = await _service.GetClubDetailAsync(id);
        if (club == null) return NotFound();
        return Ok(club);
    }

    // POST: api/clubs/{clubId}/join
    [HttpPost("{clubId:int}/join")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> Join(int clubId, [FromQuery] int accountId)
    {
        var ok = await _service.SendJoinRequestAsync(accountId, clubId);
        if (!ok) return BadRequest("Request already exists.");
        return Ok();
    }
}
