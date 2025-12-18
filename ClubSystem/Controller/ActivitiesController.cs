using DTO.DTO.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Repo.Interfaces;
using Service.Helper;
using Service.Service.Interfaces;
using Service.Services;

namespace StudentClubAPI.Controllers
{
    /// <summary>
    /// Controller xử lý các thao tác quản lý Activity dành cho Club Leader và Admin.
    /// 
    /// Các endpoint chính:
    /// - Public: Xem danh sách, chi tiết activity
    /// - Authenticated (clubleader/admin): Tạo, sửa, xóa, mở/đóng đăng ký, upload ảnh, xem danh sách tham gia
    /// 
    /// Tương tác với ActivityService:
    /// - Tất cả thao tác thay đổi dữ liệu đều đi qua service để kiểm tra quyền và club locked.
    /// </summary>
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

        /// <summary>
        /// [Public] Lấy tất cả activity (có tính status động: Ongoing, Completed...).
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        /// <summary>
        /// [Public] Lấy activity theo clubId.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("club/{clubId}")]
        public async Task<IActionResult> GetByClub(int clubId)
            => Ok(await _service.GetByClubAsync(clubId));

        /// <summary>
        /// [Public] Lấy chi tiết một activity.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var data = await _service.GetDetailAsync(id);
            return data == null ? NotFound("Activity không tồn tại.") : Ok(data);
        }

        /// <summary>
        /// [ClubLeader] Tạo activity mới (Status mặc định: Not_yet_open).
        /// </summary>
        [Authorize(Roles = "clubleader")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateActivityDto dto)
        {
            var accountId = User.GetAccountId();
            var isAdmin = User.IsInRole("admin");

            var created = await _service.CreateAsync(dto, accountId, isAdmin);
            return Ok(created);
        }

        /// <summary>
        /// [ClubLeader/Admin] Cập nhật activity.
        /// </summary>
        [Authorize(Roles = "clubleader,admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateActivityDto dto)
        {
            var accountId = User.GetAccountId();
            var isAdmin = User.IsInRole("admin");

            await _service.UpdateAsync(id, dto, accountId, isAdmin);
            return Ok(new { message = "Cập nhật activity thành công." });
        }

        /// <summary>
        /// [ClubLeader/Admin] Xóa activity (chỉ khi Cancelled hoặc Completed).
        /// </summary>
        [Authorize(Roles = "clubleader,admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var accountId = User.GetAccountId();
            var isAdmin = User.IsInRole("admin");

            await _service.DeleteAsync(id, accountId, isAdmin);
            return Ok(new { message = "Xóa activity thành công." });
        }

        /// <summary>
        /// [ClubLeader] Upload ảnh cho activity (chỉ leader của club đó mới được).
        /// </summary>
        [Authorize(Roles = "clubleader")]
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadActivityImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng chọn file ảnh.");

            var leaderId = User.GetAccountId();

            var activity = await _activityRepo.GetByIdAsync(id);
            if (activity == null)
                return NotFound("Activity không tồn tại.");

            // Kiểm tra quyền leader của club tổ chức activity
            var isLeader = await _activityRepo.IsLeaderOfClubAsync(activity.ClubId, leaderId);
            if (!isLeader)
                return Forbid("Bạn không có quyền upload ảnh cho activity này.");

            string? oldPublicId = activity.AvatarPublicId;
            string? newPublicId = null;

            try
            {
                var (url, publicId) = await _photoService.UploadImageAsync(file);

                newPublicId = publicId;
                activity.ImageActsUrl = url;
                activity.AvatarPublicId = publicId;

                await _activityRepo.UpdateAsync(activity);

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(oldPublicId))
                {
                    try { await _photoService.DeleteImageAsync(oldPublicId); }
                    catch { /* Ignore lỗi xóa ảnh cũ */ }
                }

                return Ok(new
                {
                    message = "Upload ảnh thành công.",
                    imageUrl = url,
                    publicId
                });
            }
            catch (Exception ex)
            {
                // Rollback: xóa ảnh mới nếu upload DB thất bại
                if (!string.IsNullOrEmpty(newPublicId))
                {
                    try { await _photoService.DeleteImageAsync(newPublicId); }
                    catch { /* Ignore */ }
                }

                return BadRequest($"Upload thất bại: {ex.Message}");
            }
        }

        /// <summary>
        /// [ClubLeader] Mở đăng ký tham gia activity.
        /// </summary>
        [Authorize(Roles = "clubleader")]
        [HttpPut("{id}/open-registration")]
        public async Task<IActionResult> OpenRegistration(int id)
        {
            var leaderId = User.GetAccountId();
            try
            {
                await _service.OpenRegistrationAsync(id, leaderId);
                return Ok(new { message = "Đã mở đăng ký tham gia." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// [ClubLeader] Đóng đăng ký tham gia activity.
        /// </summary>
        [Authorize(Roles = "clubleader")]
        [HttpPut("{id}/close-registration")]
        public async Task<IActionResult> CloseRegistration(int id)
        {
            var leaderId = User.GetAccountId();
            try
            {
                await _service.CloseRegistrationAsync(id, leaderId);
                return Ok(new { message = "Đã đóng đăng ký tham gia." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// [ClubLeader] Xem danh sách người đăng ký tham gia activity.
        /// </summary>
        [Authorize(Roles = "clubleader")]
        [HttpGet("{id}/participants")]
        public async Task<IActionResult> GetParticipants(int id)
        {
            var leaderId = User.GetAccountId();
            try
            {
                var result = await _service.GetActivityParticipantsAsync(id, leaderId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}