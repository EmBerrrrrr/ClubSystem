using DTO;
using DTO.DTO.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Services;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;

namespace StudentClubAPI.Controllers
{
    [ApiController]
    [Route("api/activities")]
    public class ActivitiesController : ControllerBase
    {
        private readonly IActivityService _service;
        private readonly IActivityRepository _activityRepo;
        private readonly IPhotoService _photoService;

        public ActivitiesController(
            IActivityService service,
            IActivityRepository activityRepo,
            IPhotoService photoService)
        {
            _service = service;
            _activityRepo = activityRepo;
            _photoService = photoService;
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

        // Upload activity image - chỉ clubleader quản lý CLB của activity mới được phép
        [Authorize(Roles = "clubleader")]
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadActivityImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var leaderId = User.GetAccountId();

            var activity = await _activityRepo.GetByIdAsync(id);
            if (activity == null)
                return NotFound("Activity not found");

            // Kiểm tra quyền: leader phải quản lý CLB của activity này
            if (!await _activityRepo.IsLeaderOfClubAsync(activity.ClubId, leaderId))
                return StatusCode(403, "Bạn không có quyền cập nhật hình ảnh cho activity này.");

            string? oldPublicId = activity.AvatarPublicId;
            string? newPublicId = null;
            try
            {
                var (url, publicId) = await _photoService.UploadImageAsync(file);
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(publicId))
                    return BadRequest("Upload failed: No URL or PublicId returned");

                newPublicId = publicId;

                activity.ImageActsUrl = url;
                activity.AvatarPublicId = publicId;
                await _activityRepo.UpdateAsync(activity);

                // Xóa ảnh cũ sau khi lưu DB thành công
                if (!string.IsNullOrEmpty(oldPublicId))
                {
                    try { await _photoService.DeleteImageAsync(oldPublicId); }
                    catch (Exception deleteEx)
                    {
                        Console.WriteLine($"[ActivitiesController] Warning: Failed to delete old image: {deleteEx.Message}");
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