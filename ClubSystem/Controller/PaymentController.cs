using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;
using DTO.DTO.Payment;

namespace ClubSystem.Controller
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("vnpay/create")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> CreateVNPayPayment([FromBody] CreateVNPayPaymentDto dto)
        {
            var accountId = User.GetAccountId();
            try
            {
                // Lấy IP address từ request
                string? clientIp = GetClientIpAddress();
                var result = await _paymentService.CreateVNPayPaymentAsync(accountId, dto.MembershipRequestId, clientIp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private string? GetClientIpAddress()
        {
            // Lấy IP từ X-Forwarded-For header (nếu có proxy/load balancer)
            string? ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                // X-Forwarded-For có thể chứa nhiều IP, lấy IP đầu tiên
                ipAddress = ipAddress.Split(',')[0].Trim();
            }

            // Nếu không có, lấy từ X-Real-IP
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
            }

            // Nếu vẫn không có, lấy từ RemoteIpAddress
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            }

            // Nếu vẫn không có, dùng localhost (cho development)
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

            return ipAddress;
        }

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VNPayReturn()
        {
            var query = Request.Query;
            var vnpayData = new Dictionary<string, string>();

            foreach (var item in query)
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    vnpayData.Add(item.Key, item.Value.ToString());
                }
            }

            var result = await _paymentService.ProcessVNPayCallbackAsync(vnpayData);

            if (result)
            {
                // Redirect về trang thành công
                return Redirect("http://localhost:5173/payment/success");
            }
            else
            {
                // Redirect về trang thất bại
                return Redirect("http://localhost:5173/payment/failed");
            }
        }

        [HttpPost("vnpay-ipn")]
        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> VNPayIPN()
        {
            var vnpayData = new Dictionary<string, string>();

            // VNPay có thể gửi qua GET hoặc POST
            if (Request.Method == "POST")
            {
                var form = Request.Form;
                foreach (var item in form)
                {
                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        vnpayData.Add(item.Key, item.Value.ToString());
                    }
                }
            }
            else
            {
                var query = Request.Query;
                foreach (var item in query)
                {
                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        vnpayData.Add(item.Key, item.Value.ToString());
                    }
                }
            }

            var result = await _paymentService.ProcessVNPayCallbackAsync(vnpayData);

            // VNPay yêu cầu trả về response code
            if (result)
            {
                return Ok(new { RspCode = "00", Message = "Success" });
            }
            else
            {
                return Ok(new { RspCode = "97", Message = "Checksum failed" });
            }
        }

        [HttpGet("history")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> GetPaymentHistory()
        {
            var accountId = User.GetAccountId();
            var result = await _paymentService.GetMyPaymentHistoryAsync(accountId);
            return Ok(result);
        }

        [HttpGet("status")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> GetPaymentStatus()
        {
            var accountId = User.GetAccountId();
            try
            {
                var result = await _paymentService.GetMyPaymentStatusAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

