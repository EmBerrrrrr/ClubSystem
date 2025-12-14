using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;
using System;

namespace ClubSystem.Controller
{
    [ApiController]
    [Route("api/student/payment")]
    [Authorize(Roles = "student")]
    public class StudentPaymentController : ControllerBase
    {
        private readonly IStudentPaymentService _service;

        public StudentPaymentController(IStudentPaymentService service)
        {
            _service = service;
        }

        // Xem các khoản đã đóng phí
        [HttpGet("paid")]
        public async Task<IActionResult> GetPaidPayments()
        {
            try
            {
                var accountId = User.GetAccountId();
                var result = await _service.GetPaidPaymentsAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Xem các khoản còn nợ
        [HttpGet("debts")]
        public async Task<IActionResult> GetDebts()
        {
            try
            {
                var accountId = User.GetAccountId();
                var result = await _service.GetDebtsAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Lịch sử thanh toán (tất cả trạng thái)
        [HttpGet("history")]
        public async Task<IActionResult> GetPaymentHistory()
        {
            try
            {
                var accountId = User.GetAccountId();
                var result = await _service.GetPaymentHistoryAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

