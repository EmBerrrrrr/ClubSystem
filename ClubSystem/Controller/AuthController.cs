using DTO;
using DTO.DTO.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Services;

namespace StudentClubAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthBusinessService _service;

    public AuthController(IAuthBusinessService service)
    {
        _service = service;
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDTO>> Login(LoginRequestDTO request)
    {
        try
        {
            var result = await _service.LoginAsync(request);

            if (result == null)
                return Unauthorized("Invalid username or password");

            return Ok(result);
        }
        catch (Exception ex) when (ex.Message == "ACCOUNT_LOCKED")
        {
            return StatusCode(403, "Account has been locked by administrator");
        }
    }


    [HttpPost("register")]
    public async Task<ActionResult<LoginResponseDTO>> Register([FromBody] RegisterRequestDTO request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _service.RegisterAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
