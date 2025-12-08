using DTO;
using Microsoft.AspNetCore.Mvc;
using Service.Services;

namespace StudentClubAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivitiesController : ControllerBase
{
    private readonly IActivityService _service;

    public ActivitiesController(IActivityService service)
    {
        _service = service;
    }

    // GET: api/activities/club/3
    [HttpGet("club/{clubId:int}")]
    public async Task<ActionResult<List<ActivityDTO>>> GetByClub(int clubId)
    {
        var activities = await _service.GetActivitiesByClubIdAsync(clubId);
        return Ok(activities);
    }

    // GET: api/activities/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ActivityDTO>> GetById(int id)
    {
        var activity = await _service.GetActivityByIdAsync(id);
        if (activity == null) return NotFound();
        return Ok(activity);
    }
}
