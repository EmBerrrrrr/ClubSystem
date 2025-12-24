using Azure.Core;
using DTO.DTO.PayOS;
using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Helper;

namespace Service.Service.Implements
{
    /// <summary>
    /// Service tích hợp PayOS cho thanh toán phí membership.
    ///
    /// Công dụng: Tạo link thanh toán, xử lý webhook callback từ PayOS, update status.
    ///
    /// Luồng chính từ front-end:
    /// 1. Student gọi POST /api/payment/create-link/{paymentId} → CreatePaymentLink → Lấy Payment từ DB → Tạo link PayOS → Redirect user.
    /// → Lưu OrderCode, Method="PayOS" vào Payment.
    /// 2. PayOS gửi webhook POST /api/payment/webhook → HandleWebhook → Tìm Payment bằng orderCode → Update Status="paid"/"failed".
    /// → Nếu success: Update Membership "active", MembershipRequest "Paid", Payment PaidDate=now.
    ///
    /// Tương tác giữa các API:
    /// - Phải approve MembershipRequest trước (nếu có fee) → Tạo Membership "pending_payment" + Payment pending.
    /// - Student thanh toán → Webhook auto update → Leader xem payment history (API leader/payment/history).
    /// - ConfirmWebhook để verify webhook URL từ PayOS.
    /// </summary>
    public class PayOSService : IPayOSService
    {
        private readonly PayOS _payOS;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IConfiguration _config;
        private readonly IMembershipRepository _membershipRepo;
        private readonly IMembershipRequestRepository _membershipRequestRepo;
        private readonly IClubRepository _clubRepo;
        private readonly IAuthRepository _accountRepo;

        public PayOSService(
        PayOS payOS,
        IPaymentRepository paymentRepo,
        IConfiguration config,
        IMembershipRepository membershipRepo,
        IMembershipRequestRepository membershipRequestRepo,
        IClubRepository clubRepo,
        IAuthRepository accountRepo)
        {
            _payOS = payOS;
            _paymentRepo = paymentRepo;
            _config = config;
            _membershipRepo = membershipRepo;
            _membershipRequestRepo = membershipRequestRepo;
            _clubRepo = clubRepo;
            _accountRepo = accountRepo;
        }

        private long GenerateOrderCode()
        {
            // 13 chữ số, luôn unique, nằm trong long
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }


        /// <summary>
        /// Tạo link thanh toán PayOS cho một payment.
        ///
        /// API: POST /api/payment/create-link/{paymentId}
        /// Luồng: Lấy Payment từ DB → Tạo ItemData (phí membership) → Gọi PayOS createPaymentLink → Trả link.
        /// </summary>
        // Tạo link thanh toán cho 1 payment trong DB
        public async Task<string> CreatePaymentLink(int paymentId)
        {
            var payment = await _paymentRepo.GetByIdAsync(paymentId)
                ?? throw new Exception("Không tìm thấy payment.");

            var membershipId = payment.MembershipId
                ?? throw new Exception("Payment không còn gắn với membership.");

            var membership = await _membershipRepo.GetMembershipByIdAsync(membershipId)
                ?? throw new Exception("Không tìm thấy membership.");

            if (membership.Status == "active")
                throw new Exception("Membership đã được thanh toán.");

            // Chỉ cho tạo link khi:
            // - created  → lần đầu tạo QR
            // - cancelled → đã hủy trước đó, tạo QR mới
            // - pending nhưng CHƯA có orderCode (phòng trường hợp data lỗi)
            if (payment.Status != "created" &&
                payment.Status != "cancelled" &&
                payment.Status != "pending") // pending nhưng tới đây chắc chắn OrderCode null
            {
                throw new Exception("Trạng thái payment không hợp lệ để tạo link.");
            }

            // 1️⃣ Check có payment pending khác cùng membership
            var existingPending = await _paymentRepo
                .GetLatestPendingPaymentByMembershipAsync(membershipId);

            if (existingPending != null && existingPending.Id != payment.Id)
                throw new Exception("Đang có đơn thanh toán khác đang chờ xử lý.");

            // 2️⃣ Nếu chính payment này đã pending + có orderCode → không tạo QR mới
            if (payment.Status == "pending" && payment.OrderCode.HasValue)
                throw new Exception("Đơn thanh toán này đã có mã QR đang chờ xử lý, vui lòng dùng lại mã cũ.");

            // 3️⃣ Lần đầu tạo link cho payment này
            long orderCode = GenerateOrderCode();

            payment.OrderCode = orderCode;
            payment.Method = "PayOS";
            payment.Status = "pending";
            payment.Description = "Don Phi Gia Nhap";

            await _paymentRepo.UpdateAsync(payment);

            var baseUrl = (_config["Frontend:BaseUrl"] ?? "").TrimEnd('/');
            string returnUrl = $"{baseUrl}/student/membership-requests";
            string cancelUrl = $"{baseUrl}/student/membership-requests";

            var result = await _payOS.createPaymentLink(
                new PaymentData(
                    orderCode: orderCode,
                    amount: (int)payment.Amount,
                    description: payment.Description,
                    items: new List<ItemData>
                    {
                new ItemData(payment.Description, 1, (int)payment.Amount)
                    },
                    cancelUrl: cancelUrl,
                    returnUrl: returnUrl
                )
            );

            return result.checkoutUrl;
        }
        public async Task CancelPaymentAsync(int paymentId)
        {
            var payment = await _paymentRepo.GetByIdAsync(paymentId)
                ?? throw new Exception("Không tìm thấy payment.");

            if (payment.Status != "pending")
                throw new Exception("Chỉ hủy được đơn đang chờ thanh toán.");

            if (!payment.OrderCode.HasValue)
                throw new Exception("Đơn này chưa có mã thanh toán.");

            // Hủy link bên payOS (nếu đã tạo)
            await _payOS.cancelPaymentLink(payment.OrderCode.Value, "User cancelled");

            // Cập nhật trạng thái trong hệ thống:
            // ➜ Đưa về cancelled và xóa orderCode để lần sau tạo lại QR mới với cùng paymentId
            payment.Status = "cancelled";
            payment.OrderCode = null;

            await _paymentRepo.UpdateAsync(payment);
        }

        /// <summary>
        /// Xử lý webhook từ PayOS (thanh toán success/fail).
        ///
        /// API: POST /api/payment/webhook (webhook endpoint)
        /// Luồng: PayOS post WebhookData → Tìm Payment bằng orderCode → Update status.
        /// → Nếu success (code="00"): Payment "paid", Membership "active", Request "Paid".
        /// → Nếu fail: Payment "failed", Request "Failed".
        /// </summary>
        // Xử lý webhook PayOS
        public async Task HandlePaymentWebhook(WebhookType webhookData)
        {
            WebhookData data;
            try
            {
                data = _payOS.verifyPaymentWebhookData(webhookData);
            }
            catch
            {
                return;
            }

            // 1️⃣ Tìm payment
            var payment = await _paymentRepo.GetByOrderCodeAsync(data.orderCode);
            if (payment == null)
                return;

            // 2️⃣ Nếu payment đã xử lý rồi → bỏ
            if (payment.Status == "paid" || payment.Status == "failed")
                return;

            // 3️⃣ Lấy membership
            if (!payment.MembershipId.HasValue)
                return;

            var membership = await _membershipRepo.GetMembershipByIdAsync(payment.MembershipId.Value);
            if (membership == null)
                return;

            // 🚨 CHẶN CỨNG: membership đã active thì KHÔNG cho payment nào nữa
            if (membership.Status == "active")
            {
                payment.Status = "cancelled";
                await _paymentRepo.UpdateAsync(payment);
                return;
            }

            // 4️⃣ Xử lý theo kết quả PayOS
            if (data.code == "00")
            {
                // ✅ CHỈ 1 payment đầu tiên vào được đây
                payment.Status = "paid";
                payment.PaidDate = DateTimeExtensions.NowVietnam();

                membership.Status = "active";

                await _paymentRepo.UpdateAsync(payment);
                _membershipRepo.UpdateMembership(membership);
                await _membershipRepo.SaveAsync();

                // Cập nhật MembershipRequest tương ứng (nếu có) về trạng thái đã thanh toán
                var requestsOfAccount = await _membershipRequestRepo.GetRequestsOfAccountAsync(membership.AccountId);
                var relatedRequest = requestsOfAccount
                    .FirstOrDefault(r => r.ClubId == membership.ClubId &&
                                         (r.Status == "Awaiting Payment" || r.Status == "Pending"));
                if (relatedRequest != null)
                {
                    relatedRequest.Status = "Paid";
                    await _membershipRequestRepo.UpdateAsync(relatedRequest);
                }
            }
            else
            {
                payment.Status = "failed";
                await _paymentRepo.UpdateAsync(payment);

                // Nếu thanh toán thất bại, cập nhật request (nếu đang chờ thanh toán) về Failed
                var requestsOfAccount = await _membershipRequestRepo.GetRequestsOfAccountAsync(membership.AccountId);
                var relatedRequest = requestsOfAccount
                    .FirstOrDefault(r => r.ClubId == membership.ClubId &&
                                         (r.Status == "Awaiting Payment" || r.Status == "Pending"));
                if (relatedRequest != null)
                {
                    relatedRequest.Status = "Failed";
                    await _membershipRequestRepo.UpdateAsync(relatedRequest);
                }
            }
        }

        /// <summary>
        /// Confirm webhook URL với PayOS (setup ban đầu).
        ///
        /// API: POST /api/payment/confirm-webhook
        /// Luồng: Gọi PayOS confirmWebhook để verify endpoint webhook.
        /// </summary>
        public async Task<string> ConfirmWebhook(WebhookURL body)
        {
            return await _payOS.confirmWebhook(body.webhook_url);
        }
    }
}