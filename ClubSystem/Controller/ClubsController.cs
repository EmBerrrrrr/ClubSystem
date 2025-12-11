using DTO;
using DTO.DTO.Club;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Models;
using Service.Helper;
using Service.Services;
using Service.Services.Interfaces;

namespace StudentClubAPI.Controllers
{
    [ApiController]
    [Route("api/clubs")]
    public class ClubsController : ControllerBase
    {
        private readonly IClubService _service;

        public ClubsController(IClubService service)
        {
            _service = service;
        }

        // CLUB LEADER - get my clubs
        [Authorize(Roles = "clubleader")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var id = User.GetAccountId();
            return Ok(await _service.GetMyClubsAsync(id));
        }

        // ADMIN - view all
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllClubsForAdminAsync());
        }

        // VIEW DETAIL
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var data = await _service.GetDetailAsync(id);

            if (data == null)
                return NotFound("Không tìm thấy câu lạc bộ");

            return Ok(data);
        }

        // CREATE CLUB
        [Authorize(Roles = "clubleader")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateClubDto dto)
        {
            var leaderId = User.GetAccountId();
            return Ok(await _service.CreateAsync(dto, leaderId));
        }

        // UPDATE
        [Authorize(Roles = "clubleader,admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateClubDto dto)
        {
            var accountId = User.GetAccountId();
            var isAdmin = User.IsInRole("admin");

            await _service.UpdateAsync(id, dto, accountId, isAdmin);
            return Ok("Updated");
        }

        // DELETE
        [Authorize(Roles = "clubleader,admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var accountId = User.GetAccountId();
            var isAdmin = User.IsInRole("admin");

            await _service.DeleteAsync(id, accountId, isAdmin);
            return Ok("Deleted");
        }
    }

}
