using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace ClubSystem.Controller
{
    [ApiController]
    [Route("api/leader/payment")]
    [Authorize(Roles = "clubleader")]
    public class ClubLeaderPaymentController : ControllerBase
    {
        private readonly IClubLeaderPaymentService _service;

        public ClubLeaderPaymentController(IClubLeaderPaymentService service)
        {
            _service = service;
        }

        /// <summary>
        /// Xem danh sách tất cả payments của CLB
        /// </summary>
        [HttpGet("club/{clubId}")]
        public async Task<IActionResult> GetClubPayments(int clubId)
        {
            var leaderId = User.GetAccountId();
            try
            {
                var result = await _service.GetClubPaymentsAsync(leaderId, clubId);
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

        /// <summary>
        /// Xem lịch sử thanh toán của một member cụ thể trong CLB
        /// </summary>
        [HttpGet("club/{clubId}/member/{accountId}")]
        public async Task<IActionResult> GetMemberPayments(int clubId, int accountId)
        {
            var leaderId = User.GetAccountId();
            try
            {
                var result = await _service.GetMemberPaymentsAsync(leaderId, clubId, accountId);
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

        /// <summary>
        /// Kiểm tra danh sách members còn nợ phí CLB
        /// </summary>
        [HttpGet("club/{clubId}/debt")]
        public async Task<IActionResult> GetDebtMembers(int clubId)
        {
            var leaderId = User.GetAccountId();
            try
            {
                var result = await _service.GetDebtMembersAsync(leaderId, clubId);
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

