using DTO.DTO.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Service.Helper;
using Service.Service.Interfaces;
using Service.Services;

namespace ClubSystem.Controller
{
    /// <summary>
    /// Controller xử lý thông tin tài khoản cá nhân (avatar, profile).
    /// 
    /// Quyền: Chỉ người dùng đã đăng nhập mới được truy cập.
    /// Bảo mật: User chỉ có thể cập nhật chính mình (lấy accountId từ token).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly StudentClubManagementContext _context;
        private readonly IPhotoService _photoService;
        private readonly IAuthBusinessService _authService;

        public AccountController(
            StudentClubManagementContext context,
            IPhotoService photoService,
            IAuthBusinessService authService)
        {
            _context = context;
            _photoService = photoService;
            _authService = authService;
        }

        /// <summary>
        /// Upload avatar cho tài khoản hiện tại.
        /// 
        /// API: POST /api/account/upload-avatar
        /// Bảo mật: Chỉ cho phép user upload avatar của chính mình.
        /// Luồng: Upload Cloudinary → Lưu URL & PublicId vào DB → Xóa ảnh cũ.
        /// </summary>
        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng chọn file ảnh để upload.");

            var currentAccountId = User.GetAccountId();

            var account = await _context.Accounts.FindAsync(currentAccountId);
            if (account == null)
                return NotFound("Tài khoản không tồn tại.");

            string? oldPublicId = account.AvatarPublicId;
            string? newPublicId = null;

            try
            {
                var (url, publicId) = await _photoService.UploadImageAsync(file);

                newPublicId = publicId;

                // Cập nhật DB
                account.ImageAccountUrl = url;
                account.AvatarPublicId = publicId;
                await _context.SaveChangesAsync();

                // Xóa ảnh cũ sau khi lưu thành công
                if (!string.IsNullOrEmpty(oldPublicId))
                {
                    try { await _photoService.DeleteImageAsync(oldPublicId); }
                    catch (Exception ex)
                    {
                        // Log nhưng không làm hỏng flow chính
                        Console.WriteLine($"[AccountController] Warning: Failed to delete old avatar {oldPublicId}: {ex.Message}");
                    }
                }

                return Ok(new
                {
                    message = "Upload avatar thành công.",
                    avatarUrl = url,
                    publicId
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Rollback: xóa ảnh mới nếu có lỗi sau upload
                if (!string.IsNullOrEmpty(newPublicId))
                {
                    try { await _photoService.DeleteImageAsync(newPublicId); }
                    catch { /* Ignore rollback failure */ }
                }

                return BadRequest($"Upload thất bại: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật thông tin profile (FullName, Email, Phone...).
        /// 
        /// API: PUT /api/account/profile
        /// Bảo mật: Chỉ cập nhật chính tài khoản của user đang đăng nhập.
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateAccountRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var accountId = User.GetAccountId();

            try
            {
                var result = await _authService.UpdateAccountAsync(accountId, request);
                return result == null
                    ? NotFound("Tài khoản không tồn tại.")
                    : Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}