using DTO;                          // Cần cho LoginResponseDTO (nằm trong DTO, không phải DTO.DTO.Auth)
using DTO.DTO.Auth;
using Microsoft.AspNetCore.Mvc;  
using Service.Service.Interfaces;   
using Service.Services;            

namespace StudentClubAPI.Controllers
{
    /// <summary>
    /// Controller xử lý đăng nhập và đăng ký tài khoản.
    /// 
    /// Endpoint public (không cần auth):
    /// - POST /api/auth/login
    /// - POST /api/auth/register
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthBusinessService _service;

        public AuthController(IAuthBusinessService service)
        {
            _service = service;
        }

        /// <summary>
        /// Đăng nhập hệ thống.
        /// 
        /// API: POST /api/auth/login
        /// Trả về JWT token + thông tin user nếu thành công.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                var result = await _service.LoginAsync(request);

                return result == null
                    ? Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng." })
                    : Ok(result);
            }
            catch (Exception ex) when (ex.Message == "ACCOUNT_LOCKED")
            {
                return StatusCode(403, new { message = "Tài khoản của bạn đã bị khóa bởi quản trị viên." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Đăng ký tài khoản mới (mặc định role = student).
        /// 
        /// API: POST /api/auth/register
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.RegisterAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}