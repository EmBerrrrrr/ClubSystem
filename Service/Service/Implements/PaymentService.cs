using DTO.DTO.Payment;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Helper;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IMembershipRequestRepository _membershipRequestRepo;
        private readonly IMembershipRepository _membershipRepo;
        private readonly IClubRepository _clubRepo;
        private readonly VNPayHelper _vnPayHelper;

        public PaymentService(
            IPaymentRepository paymentRepo,
            IMembershipRequestRepository membershipRequestRepo,
            IMembershipRepository membershipRepo,
            IClubRepository clubRepo,
            VNPayHelper vnPayHelper)
        {
            _paymentRepo = paymentRepo;
            _membershipRequestRepo = membershipRequestRepo;
            _membershipRepo = membershipRepo;
            _clubRepo = clubRepo;
            _vnPayHelper = vnPayHelper;
        }

        public async Task<List<PaymentDto>> GetMyPendingPaymentsAsync(int accountId)
        {
            // Lấy các membership request đã được approve nhưng chưa thanh toán
            var requests = await _membershipRequestRepo.GetRequestsOfAccountAsync(accountId);
            var pendingRequests = requests
                .Where(r => r.Status == "approved_pending_payment")
                .ToList();

            var result = new List<PaymentDto>();

            foreach (var request in pendingRequests)
            {
                var club = await _clubRepo.GetByIdAsync(request.ClubId);
                if (club == null) continue;

                // Kiểm tra xem đã có payment chưa
                var existingPayment = await _paymentRepo.GetPaymentByMembershipRequestIdAsync(request.Id);
                
                if (existingPayment == null)
                {
                    // Chưa có payment, tạo DTO từ request
                    result.Add(new PaymentDto
                    {
                        MembershipRequestId = request.Id,
                        ClubId = request.ClubId,
                        ClubName = club.Name ?? "",
                        Amount = club.MembershipFee ?? 0,
                        Status = "pending",
                        Method = "",
                        PaidDate = null
                    });
                }
                else if (existingPayment.Status == "pending")
                {
                    // Đã có payment nhưng chưa thanh toán
                    result.Add(new PaymentDto
                    {
                        Id = existingPayment.Id,
                        MembershipRequestId = request.Id,
                        ClubId = existingPayment.ClubId,
                        ClubName = existingPayment.Club?.Name ?? "",
                        Amount = existingPayment.Amount,
                        Status = existingPayment.Status,
                        Method = existingPayment.Method,
                        PaidDate = existingPayment.PaidDate
                    });
                }
            }

            return result;
        }

        public async Task<VNPayPaymentResponseDto> CreateVNPayPaymentAsync(int accountId, int membershipRequestId)
        {
            // Kiểm tra membership request
            var request = await _membershipRequestRepo.GetByIdAsync(membershipRequestId);
            if (request == null)
                throw new Exception("Không tìm thấy yêu cầu thành viên.");

            // Kiểm tra request thuộc về account này
            if (request.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền thanh toán cho yêu cầu này.");

            // Kiểm tra status phải là approved_pending_payment
            if (request.Status != "approved_pending_payment")
                throw new Exception("Yêu cầu này không ở trạng thái chờ thanh toán.");

            // Kiểm tra đã có payment chưa
            var existingPayment = await _paymentRepo.GetPaymentByMembershipRequestIdAsync(request.Id);
            if (existingPayment != null && existingPayment.Status == "pending")
            {
                // Đã có payment pending, tạo lại URL
                var club = await _clubRepo.GetByIdAsync(request.ClubId);
                string existingOrderInfo = $"Thanh toán phí thành viên CLB {club?.Name ?? ""}";
                string existingOrderId = existingPayment.Id.ToString();
                string existingPaymentUrl = _vnPayHelper.CreatePaymentUrl(
                    existingPayment.Id,
                    existingPayment.Amount,
                    existingOrderInfo,
                    existingOrderId
                );

                return new VNPayPaymentResponseDto
                {
                    PaymentId = existingPayment.Id,
                    PaymentUrl = existingPaymentUrl,
                    Amount = existingPayment.Amount,
                    OrderId = existingOrderId
                };
            }

            // Lấy thông tin club để lấy membership fee
            var clubInfo = await _clubRepo.GetByIdAsync(request.ClubId);
            if (clubInfo == null)
                throw new Exception("Không tìm thấy câu lạc bộ.");

            // Kiểm tra xem đã có membership active chưa (tránh duplicate)
            if (await _membershipRepo.IsMemberAsync(accountId, request.ClubId))
                throw new Exception("Bạn đã là thành viên của câu lạc bộ này.");

            // Kiểm tra xem đã có membership pending_payment chưa
            var existingMembership = await _membershipRepo.GetMembershipByAccountAndClubAsync(accountId, request.ClubId);
            if (existingMembership != null && existingMembership.Status == "pending_payment")
                throw new Exception("Bạn đã có thanh toán đang chờ xử lý cho câu lạc bộ này.");

            // Tạo membership với status "pending_payment" (chờ thanh toán)
            var membership = new Membership
            {
                AccountId = accountId,
                ClubId = request.ClubId,
                JoinDate = null,
                Status = "pending_payment"
            };

            await _membershipRepo.AddMembershipAsync(membership);
            await _membershipRepo.SaveAsync();

            // Tạo payment
            var payment = new Payment
            {
                MembershipId = membership.Id,
                ClubId = request.ClubId,
                Amount = clubInfo.MembershipFee ?? 0,
                Method = "VNPay",
                Status = "pending",
                PaidDate = null
            };

            await _paymentRepo.AddPaymentAsync(payment);
            await _paymentRepo.SaveAsync();

            // Tạo VNPay payment URL
            string orderInfo = $"Thanh toán phí thành viên CLB {clubInfo.Name ?? ""}";
            string orderId = payment.Id.ToString();
            string paymentUrl = _vnPayHelper.CreatePaymentUrl(
                payment.Id,
                payment.Amount,
                orderInfo,
                orderId
            );

            return new VNPayPaymentResponseDto
            {
                PaymentId = payment.Id,
                PaymentUrl = paymentUrl,
                Amount = payment.Amount,
                OrderId = orderId
            };
        }

        public async Task<bool> ProcessVNPayCallbackAsync(Dictionary<string, string> vnpayData)
        {
            // Validate signature
            if (!_vnPayHelper.ValidateSignature(vnpayData))
            {
                return false;
            }

            // Lấy thông tin từ callback
            string orderId = vnpayData.ContainsKey("vnp_TxnRef") ? vnpayData["vnp_TxnRef"] : "";
            string responseCode = vnpayData.ContainsKey("vnp_ResponseCode") ? vnpayData["vnp_ResponseCode"] : "";
            string transactionStatus = vnpayData.ContainsKey("vnp_TransactionStatus") ? vnpayData["vnp_TransactionStatus"] : "";

            // Kiểm tra payment ID
            if (!int.TryParse(orderId, out int paymentId))
            {
                return false;
            }

            // Lấy payment
            var payment = await _paymentRepo.GetByIdAsync(paymentId);
            if (payment == null)
            {
                return false;
            }

            // Kiểm tra đã thanh toán chưa
            if (payment.Status == "paid")
            {
                return true; // Đã xử lý rồi
            }

            // Kiểm tra kết quả thanh toán
            // ResponseCode = "00" và TransactionStatus = "00" nghĩa là thanh toán thành công
            if (responseCode == "00" && transactionStatus == "00")
            {
                // Cập nhật payment status thành paid
                payment.Status = "paid";
                payment.PaidDate = DateTime.Now;
                await _paymentRepo.UpdatePaymentAsync(payment);

                // Cập nhật membership status thành active và set JoinDate
                if (payment.Membership != null)
                {
                    payment.Membership.Status = "active";
                    payment.Membership.JoinDate = DateOnly.FromDateTime(DateTime.Now);
                }

                // Tìm membership request tương ứng và cập nhật status thành completed
                if (payment.Membership != null)
                {
                    var requests = await _membershipRequestRepo.GetRequestsOfAccountAsync(payment.Membership.AccountId);
                    var request = requests.FirstOrDefault(r =>
                        r.ClubId == payment.ClubId &&
                        r.Status == "approved_pending_payment");

                    if (request != null)
                    {
                        request.Status = "completed";
                        await _membershipRequestRepo.UpdateAsync(request);
                    }
                }

                await _paymentRepo.SaveAsync();
                return true;
            }
            else
            {
                // Thanh toán thất bại hoặc bị hủy
                payment.Status = "failed";
                await _paymentRepo.UpdatePaymentAsync(payment);
                await _paymentRepo.SaveAsync();
                return false;
            }
        }

        public async Task<PaymentDto> CompletePaymentAsync(int accountId, int paymentId)
        {
            var payment = await _paymentRepo.GetByIdAsync(paymentId);
            if (payment == null)
                throw new Exception("Không tìm thấy thanh toán.");

            // Kiểm tra payment thuộc về account này
            if (payment.Membership?.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền hoàn tất thanh toán này.");

            // Kiểm tra status phải là pending
            if (payment.Status != "pending")
                throw new Exception("Thanh toán này đã được xử lý.");

            // Cập nhật payment status thành paid
            payment.Status = "paid";
            payment.PaidDate = DateTime.Now;
            await _paymentRepo.UpdatePaymentAsync(payment);

            // Cập nhật membership status thành active và set JoinDate
            if (payment.Membership != null)
            {
                payment.Membership.Status = "active";
                payment.Membership.JoinDate = DateOnly.FromDateTime(DateTime.Now);
            }

            // Tìm membership request tương ứng và cập nhật status thành completed
            var requests = await _membershipRequestRepo.GetRequestsOfAccountAsync(accountId);
            var request = requests.FirstOrDefault(r => 
                r.ClubId == payment.ClubId && 
                r.Status == "approved_pending_payment");

            if (request != null)
            {
                request.Status = "completed";
                await _membershipRequestRepo.UpdateAsync(request);
            }

            // Lưu tất cả thay đổi (payment và membership)
            await _paymentRepo.SaveAsync();

            return new PaymentDto
            {
                Id = payment.Id,
                MembershipRequestId = request?.Id ?? 0,
                ClubId = payment.ClubId,
                ClubName = payment.Club?.Name ?? "",
                Amount = payment.Amount,
                Status = payment.Status,
                Method = payment.Method,
                PaidDate = payment.PaidDate
            };
        }

        public async Task<List<PaymentDto>> GetMyPaymentHistoryAsync(int accountId)
        {
            var payments = await _paymentRepo.GetPaymentsByAccountIdAsync(accountId);

            var result = new List<PaymentDto>();
            foreach (var payment in payments)
            {
                // Tìm membership request tương ứng
                var requests = await _membershipRequestRepo.GetRequestsOfAccountAsync(accountId);
                var request = requests.FirstOrDefault(r => 
                    r.ClubId == payment.ClubId && 
                    (r.Status == "completed" || r.Status == "approved_pending_payment"));

                result.Add(new PaymentDto
                {
                    Id = payment.Id,
                    MembershipRequestId = request?.Id ?? 0,
                    ClubId = payment.ClubId,
                    ClubName = payment.Club?.Name ?? "",
                    Amount = payment.Amount,
                    Status = payment.Status,
                    Method = payment.Method,
                    PaidDate = payment.PaidDate
                });
            }

            return result;
        }

        public async Task<List<PaymentStatusDto>> GetMyPaymentStatusAsync(int accountId)
        {
            // Lấy tất cả memberships của student (bao gồm cả active và pending_payment)
            var allMemberships = await _membershipRepo.GetAllMembershipsAsync(accountId);
            var result = new List<PaymentStatusDto>();

            // Lấy danh sách CLB từ memberships
            var clubIds = allMemberships.Select(m => m.ClubId).Distinct().ToList();

            foreach (var clubId in clubIds)
            {
                var membership = allMemberships.FirstOrDefault(m => m.ClubId == clubId);
                if (membership == null) continue;

                var club = membership.Club ?? await _clubRepo.GetByIdAsync(clubId);
                if (club == null) continue;

                var statusDto = new PaymentStatusDto
                {
                    ClubId = clubId,
                    ClubName = club.Name ?? "",
                    MembershipFee = club.MembershipFee ?? 0,
                    IsMember = membership.Status == "active"
                };

                // Nếu CLB không có phí thành viên
                if (club.MembershipFee == null || club.MembershipFee == 0)
                {
                    statusDto.PaymentStatus = "no_fee";
                    result.Add(statusDto);
                    continue;
                }

                // Tìm payment gần nhất cho CLB này
                var payments = await _paymentRepo.GetPaymentsByAccountIdAsync(accountId);
                var clubPayment = payments
                    .Where(p => p.ClubId == clubId)
                    .OrderByDescending(p => p.PaidDate ?? DateTime.MinValue)
                    .FirstOrDefault();

                if (clubPayment == null)
                {
                    // Chưa có payment nào - kiểm tra xem có membership request đã approve chưa
                    var requests = await _membershipRequestRepo.GetRequestsOfAccountAsync(accountId);
                    var approvedRequest = requests.FirstOrDefault(r => 
                        r.ClubId == clubId && r.Status == "approved_pending_payment");
                    
                    if (approvedRequest != null)
                    {
                        statusDto.PaymentStatus = "not_paid"; // Cần thanh toán
                    }
                    else if (membership.Status == "active")
                    {
                        statusDto.PaymentStatus = "paid"; // Đã là member active mà không có payment record
                    }
                    else
                    {
                        statusDto.PaymentStatus = "not_paid";
                    }
                }
                else
                {
                    statusDto.PaymentId = clubPayment.Id;
                    statusDto.PaidDate = clubPayment.PaidDate;

                    if (clubPayment.Status == "paid")
                    {
                        statusDto.PaymentStatus = "paid";
                    }
                    else if (clubPayment.Status == "pending")
                    {
                        statusDto.PaymentStatus = "pending";
                    }
                    else
                    {
                        statusDto.PaymentStatus = "not_paid";
                    }
                }

                result.Add(statusDto);
            }

            return result;
        }
    }
}

