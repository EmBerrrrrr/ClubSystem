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

            // Lấy orderCode từ DB, nếu chưa có / không hợp lệ thì dùng payment.Id
            long orderCode = payment.OrderCode ?? 0;

            if (orderCode <= 0 || orderCode > int.MaxValue) //đảm bảo trong range
            {
                orderCode = payment.Id; // luôn nhỏ, unique theo bảng payments //MỚI
            }

            payment.OrderCode = orderCode;  
            payment.Method = "PayOS";
            var membership = await _membershipRepo.GetMembershipByIdAsync(payment.MembershipId)
                ?? throw new Exception("Không tìm thấy membership.");
            var club = await _clubRepo.GetByIdAsync(membership.ClubId)
                ?? throw new Exception("Không tìm thấy club.");
            var account = await _accountRepo.GetAccountByIdAsync(membership.AccountId)
                ?? throw new Exception("Không tìm thấy account.");
            payment.Description =
                $"Đơn của {account.FullName}";

            payment.Status = "pending";

            await _paymentRepo.UpdateAsync(payment);  // lưu lại orderCode, status

            // BaseUrl: tránh bị dư dấu '/'
            var baseUrl = (_config["Frontend:BaseUrl"] ?? "").TrimEnd('/');

            string returnUrl = $"{baseUrl}/student/membership-requests";
            string cancelUrl = $"{baseUrl}/student/membership-requests";

            int amount = (int)payment.Amount; // PayOS cần int
            // (Có thể log ra để debug nếu cần)
            Console.WriteLine($"[PayOS] CreatePaymentLink: paymentId={paymentId}, orderCode={orderCode}, amount={amount}");
            var req = new PaymentLinkRequest()
            {
                orderCode = orderCode,
                amount = amount,
                description = payment.Description,
                returnUrl = returnUrl,
                cancelUrl = cancelUrl
            };
            // Gọi PayOS SDK
            CreatePaymentResult result = await _payOS.createPaymentLink(
                new PaymentData(
                    orderCode: req.orderCode,
                    amount: req.amount,
                    description: req.description,
                    items: new List<ItemData>
                    {
                        new ItemData(req.description, 1, req.amount)
                    },
                    cancelUrl: req.cancelUrl,
                    returnUrl: req.returnUrl
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
                // Xác thực chữ ký & parse webhook
                data = _payOS.verifyPaymentWebhookData(webhookData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayOS Webhook] Verify failed: {ex.Message}");
                return;
            }

            Console.WriteLine($"[PayOS Webhook] code={data.code}, orderCode={data.orderCode}, amount={data.amount}");

            // Tìm payment theo orderCode
            var payment = await _paymentRepo.GetByOrderCodeAsync(data.orderCode);

            // Nếu không có payment trong DB -> có thể là webhook test -> bỏ qua, KHÔNG throw
            if (payment == null)
            {
                Console.WriteLine($"[PayOS Webhook] Payment not found for orderCode={data.orderCode} (maybe test webhook).");
                return;
            }

            // Lấy membership tương ứng
            Membership? membership = null;
            try
            {
                membership = await _membershipRepo.GetMembershipByIdAsync(payment.MembershipId);
            }
            catch
            {
                // nếu method trả null thì xử lý tiếp phía dưới
            }

            // Nếu có membership thì tìm request tương ứng (theo account + club)
            MembershipRequest? request = null;
            if (membership != null)
            {
                var requests = await _membershipRequestRepo.GetRequestsOfAccountAsync(membership.AccountId);
                request = requests
                    .Where(r => r.ClubId == membership.ClubId)
                    .OrderByDescending(r => r.RequestDate)
                    .FirstOrDefault();
            }

            // Thanh toán thành công
            if (data.code == "00")
            {
                payment.Status = "paid";
                payment.PaidDate = DateTimeExtensions.NowVietnam();

                if (membership != null)
                {
                    membership.Status = "active";
                    _membershipRepo.UpdateMembership(membership);
                    await _membershipRepo.SaveAsync();
                }

                if (request != null)
                {
                    request.Status = "Paid";
                    await _membershipRequestRepo.UpdateAsync(request);
                }
            }
            else
            {
                // Thanh toán thất bại / hủy
                payment.Status = "failed";

                if (request != null)
                {
                    request.Status = "Failed";
                    await _membershipRequestRepo.UpdateAsync(request);
                }
                // membership.status giữ nguyên "pending_payment" để có thể thanh toán lại
            }

            await _paymentRepo.UpdateAsync(payment);
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
