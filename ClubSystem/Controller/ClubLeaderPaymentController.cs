using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;

namespace ClubSystem.Controller
{
    [ApiController]
    [Route("api/ClubLeaderPayment")]
    [Authorize(Roles = "clubleader")]
    public class ClubLeaderPaymentController : ControllerBase
    {
        private readonly IClubLeaderPaymentService _service;

        public ClubLeaderPaymentController(IClubLeaderPaymentService service)
        {
            _service = service;
        }

        // Xem payments theo CLB
        [HttpGet("clubs/{clubId}/payments")]
        public async Task<IActionResult> GetPaymentsByClub(int clubId)
        {
            try
            {
                var leaderId = User.GetAccountId();
                var result = await _service.GetPaymentsByClubAsync(leaderId, clubId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Xem ai còn nợ phí
        [HttpGet("clubs/{clubId}/debtors")]
        public async Task<IActionResult> GetDebtorsByClub(int clubId)
        {
            try
            {
                var leaderId = User.GetAccountId();
                var result = await _service.GetDebtorsByClubAsync(leaderId, clubId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Lịch sử thanh toán
        [HttpGet("clubs/{clubId}/history")]
        public async Task<IActionResult> GetPaymentHistoryByClub(int clubId)
        {
            try
            {
                var leaderId = User.GetAccountId();
                var result = await _service.GetPaymentHistoryByClubAsync(leaderId, clubId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

