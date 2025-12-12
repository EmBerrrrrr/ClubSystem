using DTO;
using DTO.DTO.Club;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Helper;
using Service.Service.Interfaces;
using Service.Services;
using Service.Services.Interfaces;

namespace StudentClubAPI.Controllers
{
    [ApiController]
    [Route("api/clubs")]
    public class ClubsController : ControllerBase
    {
        private readonly IClubService _service;
        private readonly IPhotoService _photoService;
        private readonly IClubRepository _clubRepo;

        public ClubsController(
            IClubService service,
            IPhotoService photoService,
            IClubRepository clubRepo)
        {
            _service = service;
            _photoService = photoService;
            _clubRepo = clubRepo;
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

        // Upload club image - chỉ clubleader của CLB mới được phép
        [Authorize(Roles = "clubleader")]
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadClubImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var leaderId = User.GetAccountId();

            // Kiểm tra quyền: leader phải quản lý CLB này
            if (!await _clubRepo.IsLeaderOfClubAsync(id, leaderId))
                return StatusCode(403, "Bạn không có quyền cập nhật hình ảnh cho CLB này.");

            var club = await _clubRepo.GetByIdAsync(id);
            if (club == null)
                return NotFound("Club not found");

            string? oldPublicId = club.AvatarPublicId;
            string? newPublicId = null;
            try
            {
                var (url, publicId) = await _photoService.UploadImageAsync(file);
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(publicId))
                    return BadRequest("Upload failed: No URL or PublicId returned");

                newPublicId = publicId; // chỉ để rollback; chưa lưu publicId vào DB vì schema chưa có

                club.ImageClubsUrl = url;
                club.AvatarPublicId = publicId;
                await _clubRepo.UpdateAsync(club);

                // Xóa ảnh cũ sau khi lưu DB thành công
                if (!string.IsNullOrEmpty(oldPublicId))
                {
                    try { await _photoService.DeleteImageAsync(oldPublicId); }
                    catch (Exception deleteEx)
                    {
                        Console.WriteLine($"[ClubsController] Warning: Failed to delete old image: {deleteEx.Message}");
                    }
                }

                return Ok(new
                {
                    Message = "Upload successful",
                    ImageUrl = url,
                    PublicId = publicId
                });
            }
            catch (Exception ex)
            {
                // Rollback: xóa ảnh mới nếu update DB thất bại
                if (!string.IsNullOrEmpty(newPublicId))
                {
                    try { await _photoService.DeleteImageAsync(newPublicId); }
                    catch { /* ignore rollback failure */ }
                }

                return BadRequest($"Upload failed: {ex.Message}");
            }
        }
    }

}
