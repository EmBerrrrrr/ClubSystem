using DTO;
using DTO.DTO.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Services;

namespace StudentClubAPI.Controllers
{
    [ApiController]
    [Route("api/activities")]
    public class ActivitiesController : ControllerBase
    {
        private readonly IActivityService _service;

        public ActivitiesController(IActivityService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        [AllowAnonymous]
        [HttpGet("club/{clubId}")]
        public async Task<IActionResult> GetByClub(int clubId)
            => Ok(await _service.GetByClubAsync(clubId));

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var data = await _service.GetDetailAsync(id);
            return data == null ? NotFound() : Ok(data);
        }

        [Authorize(Roles = "clubleader")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateActivityDto dto)
        {
            var accountId = User.GetAccountId();
            var isAdmin = User.IsInRole("admin");

            return Ok(await _service.CreateAsync(dto, accountId, isAdmin));
        }

        [Authorize(Roles = "clubleader,admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateActivityDto dto)
        {
            var accountId = User.GetAccountId();
            var isAdmin = User.IsInRole("admin");

            await _service.UpdateAsync(id, dto, accountId, isAdmin);
            return Ok("Updated");
        }

        [Authorize(Roles = "clubleader,admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var accountId = User.GetAccountId();
            var isAdmin = User.IsInRole("admin");

            await _service.DeleteAsync(id, accountId, isAdmin);
            return Ok("Deleted");
        }

        [Authorize(Roles = "clubleader")]
        [HttpPut("{id}/open-registration")]
        public async Task<IActionResult> OpenRegistration(int id)
        {
            try
            {
                var leaderId = User.GetAccountId();
                await _service.OpenRegistrationAsync(id, leaderId);
                return Ok("Đã mở đăng ký.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "clubleader")]
        [HttpPut("{id}/close-registration")]
        public async Task<IActionResult> CloseRegistration(int id)
        {
            try
            {
                var leaderId = User.GetAccountId();
                await _service.CloseRegistrationAsync(id, leaderId);
                return Ok("Đã đóng đăng ký.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "clubleader")]
        [HttpGet("{id}/participants")]
        public async Task<IActionResult> GetParticipants(int id)
        {
            try
            {
                var leaderId = User.GetAccountId();
                var result = await _service.GetActivityParticipantsAsync(id, leaderId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}