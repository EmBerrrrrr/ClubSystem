using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Service.Interfaces;
using Service.Helper;
using Service.Services;
using DTO;
using DTO.DTO.Auth;
using Repository.Models;

namespace ClubSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly StudentClubManagementContext _context;
        private readonly IPhotoService _photoService;
        private readonly IAuthBusinessService _authService;

        public AccountController(StudentClubManagementContext context, IPhotoService photoService, IAuthBusinessService authService)
        {
            _context = context;
            _photoService = photoService;
            _authService = authService;
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                var currentAccountId = User.GetAccountId();
                
                // Chỉ cho phép user update avatar của chính mình - không có tham số id trong route
                // Không ai có quyền update avatar của người khác, kể cả admin
                var account = await _context.Accounts.FindAsync(currentAccountId);
                if (account == null)
                    return NotFound("Account not found");

                Console.WriteLine($"[AccountController] Upload avatar for account ID: {currentAccountId}");

                string? oldPublicId = account.AvatarPublicId;
                string? newPublicId = null;
                string? newUrl = null;

                try
                {
                    // 2. Upload ảnh mới (validation đã được thực hiện trong PhotoService)
                    Console.WriteLine($"[AccountController] Uploading new image...");
                    var (url, publicId) = await _photoService.UploadImageAsync(file);

                    if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(publicId))
                    {
                        Console.WriteLine($"[AccountController] Upload returned null URL or PublicId");
                        return BadRequest("Upload failed: No URL or PublicId returned");
                    }

                    newUrl = url;
                    newPublicId = publicId;
                    Console.WriteLine($"[AccountController] Upload successful. URL: {url}, PublicId: {publicId}");

                    // 3. Lưu DB
                    account.ImageAccountUrl = url;
                    account.AvatarPublicId = publicId;

                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[AccountController] Database updated successfully");

                    // 4. Xóa ảnh cũ sau khi lưu DB thành công
                    if (!string.IsNullOrEmpty(oldPublicId))
                    {
                        try
                        {
                            Console.WriteLine($"[AccountController] Deleting old image: {oldPublicId}");
                            await _photoService.DeleteImageAsync(oldPublicId);
                        }
                        catch (Exception deleteEx)
                        {
                            // Log nhưng không throw - ảnh cũ có thể đã bị xóa hoặc không tồn tại
                            Console.WriteLine($"[AccountController] Warning: Failed to delete old image: {deleteEx.Message}");
                        }
                    }

                    return Ok(new
                    {
                        Message = "Upload successful",
                        AvatarUrl = url,
                        PublicId = publicId
                    });
                }
                catch (Exception)
                {
                    // 7. Rollback: Nếu lưu DB thất bại, xóa ảnh đã upload
                    if (!string.IsNullOrEmpty(newPublicId))
                    {
                        try
                        {
                            Console.WriteLine($"[AccountController] Rollback: Deleting uploaded image {newPublicId} due to DB save failure");
                            await _photoService.DeleteImageAsync(newPublicId);
                        }
                        catch (Exception rollbackEx)
                        {
                            Console.WriteLine($"[AccountController] Rollback failed: {rollbackEx.Message}");
                        }
                    }
                    throw; // Re-throw để xử lý ở catch bên ngoài
                }
            }
            catch (ArgumentException ex)
            {
                // Validation errors
                Console.WriteLine($"[AccountController] Validation error: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AccountController] Error: {ex.Message}");
                Console.WriteLine($"[AccountController] StackTrace: {ex.StackTrace}");
                return BadRequest($"Upload failed: {ex.Message}");
            }
        }

        [HttpPut("profile")]
        public async Task<ActionResult<LoginResponseDTO>> UpdateProfile([FromBody] UpdateAccountRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Tự động lấy accountId từ token - user chỉ có thể update chính mình
                var accountId = User.GetAccountId();
                var result = await _authService.UpdateAccountAsync(accountId, request);
                if (result == null) return NotFound("Account not found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
