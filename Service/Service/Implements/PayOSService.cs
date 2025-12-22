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
    ///    → Lưu OrderCode, Method="PayOS" vào Payment.
    /// 2. PayOS gửi webhook POST /api/payment/webhook → HandleWebhook → Tìm Payment bằng orderCode → Update Status="paid"/"failed".
    ///    → Nếu success: Update Membership "active", MembershipRequest "Paid", Payment PaidDate=now.
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

            var membership = await _membershipRepo.GetMembershipByIdAsync(payment.MembershipId)
                ?? throw new Exception("Không tìm thấy membership.");

            // Membership đã active thì không cho thanh toán
            if (membership.Status == "active")
                throw new Exception("Membership đã được thanh toán.");

            // Payment này đã paid thì không cho tạo link nữa
            if (payment.Status == "paid")
                throw new Exception("Đơn này đã được thanh toán.");

            // Không cho tồn tại payment khác đang pending cho cùng membership
            bool hasPending = await _paymentRepo
                .HasOtherPendingPayment(payment.MembershipId, payment.Id);

            if (hasPending)
                throw new Exception("Đang có đơn thanh toán khác đang chờ xử lý.");

            // Nếu payment này đã từng tạo QR và đang pending thì có thể dùng lại orderCode cũ
            // hoặc bạn muốn luôn tạo orderCode mới thì để như bên dưới
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
        /// <summary>
        /// Xử lý webhook từ PayOS (thanh toán success/fail).
        /// 
        /// API: POST /api/payment/webhook (webhook endpoint)
        /// Luồng: PayOS post WebhookData → Tìm Payment bằng orderCode → Update status.
        ///    → Nếu success (code="00"): Payment "paid", Membership "active", Request "Paid".
        ///    → Nếu fail: Payment "failed", Request "Failed".
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
                // Sai chữ ký hoặc dữ liệu webhook không hợp lệ → bỏ qua
                return;
            }

            // 1. Tìm payment theo orderCode
            var payment = await _paymentRepo.GetByOrderCodeAsync(data.orderCode);
            if (payment == null)
                return;

            // 2. Nếu payment này đã xử lý xong rồi thì bỏ
            if (payment.Status == "paid" || payment.Status == "failed" || payment.Status == "cancelled")
                return;

            // 3. Lấy membership
            var membership = await _membershipRepo.GetMembershipByIdAsync(payment.MembershipId);
            if (membership == null)
                return;

            // Nếu membership đã active thì không cho bất kỳ payment nào nữa "ăn"
            if (membership.Status == "active")
            {
                payment.Status = "cancelled";
                await _paymentRepo.UpdateAsync(payment);
                return;
            }

            // 4. Nếu PayOS báo thành công
            if (data.code == "00")
            {
                var paidDate = DateTimeExtensions.NowVietnam();

                // *** ĐIỂM QUAN TRỌNG: chỉ update payment nếu còn đang pending ***
                var ok = await _paymentRepo.TryMarkPaymentPaidAsync(payment.OrderCode, paidDate);

                if (!ok)
                {
                    // Có webhook khác chạy trước, hoặc payment không còn pending
                    // → không làm gì nữa, không được set membership active nữa
                    return;
                }

                // Re-load payment nếu cần, hoặc set lại trong memory
                payment.Status = "paid";
                payment.PaidDate = paidDate;

                // Lúc này chắc chắn payment này là payment đầu tiên chuyển từ pending → paid
                // mới được phép active membership
                membership.Status = "active";

                _membershipRepo.UpdateMembership(membership);
                await _membershipRepo.SaveAsync();
            }
            else
            {
                // Thanh toán thất bại từ PayOS
                payment.Status = "failed";
                await _paymentRepo.UpdateAsync(payment);
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
