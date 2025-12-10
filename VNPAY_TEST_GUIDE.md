# Hướng Dẫn Test VNPay Payment

## 📋 Chuẩn Bị

### 1. Thông tin VNPay Sandbox
- **Terminal ID (TmnCode)**: `FJ9A99T4`
- **Secret Key (HashSecret)**: `TRVIOSZTHRDE0W8FMWQTRTU0W8F5V9KR`
- **Payment URL**: `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html`
- **Test Card**: 
  - Số thẻ: `9704198526191432198`
  - Tên chủ thẻ: `NGUYEN VAN A`
  - Ngày phát hành: `07/15`
  - Mật khẩu OTP: `123456`

### 2. Đảm bảo Project đang chạy
```bash
dotnet run --project ClubSystem/ClubSystem.csproj
```
API sẽ chạy tại: `http://localhost:5168`

---

## 🔄 Flow Test VNPay

### **Bước 1: Student gửi Membership Request**

**Endpoint**: `POST /api/student/membership/request`

**Headers**:
```
Authorization: Bearer {student_token}
Content-Type: application/json
```

**Body**:
```json
{
  "clubId": 1,
  "reason": "Tôi muốn tham gia CLB để học hỏi và phát triển kỹ năng"
}
```

**Kỳ vọng**:
- Status code: `200 OK`
- Response có `id` của membership request
- Status của request: `"pending"`

---

### **Bước 2: Club Leader duyệt Request**

**Endpoint**: `POST /api/leader/membership/{requestId}/approve`

**Headers**:
```
Authorization: Bearer {clubleader_token}
Content-Type: application/json
```

**Body**:
```json
{
  "note": "Chấp nhận yêu cầu"
}
```

**Kỳ vọng**:
- Status code: `200 OK`
- Membership request status chuyển thành: `"approved_pending_payment"`

---

### **Bước 3: Student tạo VNPay Payment**

**Endpoint**: `POST /api/payment/vnpay/create`

**Headers**:
```
Authorization: Bearer {student_token}
Content-Type: application/json
```

**Body**:
```json
{
  "membershipRequestId": {requestId_từ_bước_1}
}
```

**Kỳ vọng**:
- Status code: `200 OK`
- Response:
```json
{
  "paymentId": 123,
  "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?...",
  "amount": 100000,
  "orderId": "123"
}
```

**Lưu ý**: Copy `paymentUrl` để test thanh toán

---

### **Bước 4: Setup Ngrok (BẮT BUỘC cho test local)**

⚠️ **QUAN TRỌNG**: VNPay **KHÔNG THỂ** gọi về `localhost`. Bạn **PHẢI** dùng ngrok.

1. **Cài đặt ngrok** (nếu chưa có):
   - Tải từ: https://ngrok.com/download
   - Hoặc dùng: `choco install ngrok` (Windows với Chocolatey)

2. **Chạy ngrok**:
   ```bash
   ngrok http 5168
   ```

3. **Copy URL ngrok** (ví dụ: `https://abc123.ngrok.io`)

4. **Cập nhật `appsettings.json`**:
   ```json
   "VNPay": {
     "TmnCode": "FJ9A99T4",
     "HashSecret": "TRVIOSZTHRDE0W8FMWQTRTU0W8F5V9KR",
     "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
     "ReturnUrl": "https://abc123.ngrok.io/api/payment/vnpay-return",
     "IpnUrl": "https://abc123.ngrok.io/api/payment/vnpay-ipn"
   }
   ```

5. **Restart application** để load config mới

### **Bước 5: Test Thanh Toán trên VNPay Sandbox**

1. **Đảm bảo số tiền >= 10,000 VND**:
   - Kiểm tra `MembershipFee` trong database
   - Nếu < 10,000 VND, cập nhật lên >= 10,000 VND

2. **Mở `paymentUrl` trong browser** (URL từ bước 3)

3. **Điền thông tin thẻ test**:
   - Số thẻ: `9704198526191432198`
   - Tên chủ thẻ: `NGUYEN VAN A`
   - Ngày phát hành: `07/15`
   - Mật khẩu OTP: `123456`

4. **Click "Thanh toán"**

5. **Kỳ vọng**:
   - VNPay redirect về: `https://abc123.ngrok.io/api/payment/vnpay-return?...`
   - Sau đó redirect tiếp về: `http://localhost:5173/payment/success` (nếu thành công)

---

### **Bước 6: Kiểm tra Callback (ReturnUrl)**

**Endpoint**: `GET /api/payment/vnpay-return`

**Query Parameters** (VNPay tự động thêm):
```
vnp_Amount=10000000
vnp_BankCode=NCB
vnp_CardType=ATM
vnp_OrderInfo=Thanh+toan+phi+thanh+vien+CLB+...
vnp_PayDate=20240101120000
vnp_ResponseCode=00
vnp_TmnCode=FJ9A99T4
vnp_TransactionNo=12345678
vnp_TransactionStatus=00
vnp_TxnRef=123
vnp_SecureHash=...
```

**Kỳ vọng**:
- Status code: `302 Redirect`
- Redirect đến: `http://localhost:5173/payment/success`
- Payment status trong DB: `"paid"`
- Membership status: `"active"`
- MembershipRequest status: `"completed"`

---

### **Bước 7: Kiểm tra IPN Callback (Tùy chọn)**

VNPay cũng gửi callback đến IPN URL để xác nhận thanh toán.

**Endpoint**: `POST /api/payment/vnpay-ipn` hoặc `GET /api/payment/vnpay-ipn`

**Kỳ vọng**:
- Status code: `200 OK`
- Response:
```json
{
  "RspCode": "00",
  "Message": "Success"
}
```

---

### **Bước 8: Kiểm tra Payment Status**

**Endpoint**: `GET /api/payment/status`

**Headers**:
```
Authorization: Bearer {student_token}
```

**Kỳ vọng**:
- Status code: `200 OK`
- Response:
```json
[
  {
    "clubId": 1,
    "clubName": "Tên CLB",
    "membershipFee": 100000,
    "paymentStatus": "paid",
    "paymentId": 123,
    "paidDate": "2024-01-01T12:00:00",
    "isMember": true
  }
]
```

---

### **Bước 9: Kiểm tra Payment History**

**Endpoint**: `GET /api/payment/history`

**Headers**:
```
Authorization: Bearer {student_token}
```

**Kỳ vọng**:
- Status code: `200 OK`
- Response chứa payment vừa tạo với status `"paid"`

---

## 🧪 Test Cases

### **Test Case 1: Thanh toán thành công**
1. Tạo membership request → Approve → Tạo payment → Thanh toán thành công
2. **Kỳ vọng**: Payment = `paid`, Membership = `active`, Request = `completed`

### **Test Case 2: Thanh toán thất bại**
1. Tạo payment → Vào VNPay → Hủy thanh toán
2. **Kỳ vọng**: Payment = `failed`, Membership vẫn = `pending_payment`

### **Test Case 3: Tạo lại payment URL**
1. Tạo payment → Lấy URL → Không thanh toán → Gọi lại API tạo payment
2. **Kỳ vọng**: Trả về cùng `paymentId` nhưng URL mới

### **Test Case 4: Validate signature sai**
1. Gọi callback với signature sai
2. **Kỳ vọng**: Return `false`, payment không được cập nhật

---

## 🔍 Kiểm tra Database

Sau khi test, kiểm tra các bảng:

### **1. Payment Table**
```sql
SELECT * FROM Payments WHERE Id = {paymentId}
```
- `Status` = `"paid"` (nếu thành công)
- `PaidDate` có giá trị
- `Method` = `"VNPay"`

### **2. Membership Table**
```sql
SELECT * FROM Memberships WHERE AccountId = {accountId} AND ClubId = {clubId}
```
- `Status` = `"active"` (nếu thanh toán thành công)
- `JoinDate` có giá trị

### **3. MembershipRequest Table**
```sql
SELECT * FROM MembershipRequests WHERE Id = {requestId}
```
- `Status` = `"completed"` (nếu thanh toán thành công)

---

## ⚠️ Lưu Ý Quan Trọng

1. **ReturnUrl và IpnUrl**:
   - `ReturnUrl`: VNPay redirect user về sau khi thanh toán (dùng cho frontend)
   - `IpnUrl`: VNPay gửi callback để xác nhận thanh toán (dùng cho backend)
   - **QUAN TRỌNG**: VNPay **KHÔNG THỂ** gọi về `localhost` từ server của họ
   - **Giải pháp**: Dùng **ngrok** hoặc deploy lên server public để test

2. **Số tiền tối thiểu**:
   - VNPay yêu cầu số tiền tối thiểu là **10,000 VND**
   - Code đã có validation, nếu số tiền < 10,000 VND sẽ báo lỗi
   - Đảm bảo `MembershipFee` trong database >= 10,000 VND

3. **IP Address**:
   - Code đã tự động lấy IP từ request (X-Forwarded-For, X-Real-IP, hoặc RemoteIpAddress)
   - Không còn hardcode `127.0.0.1` nữa

4. **Signature Validation**:
   - Luôn validate signature từ VNPay để đảm bảo tính toàn vẹn dữ liệu
   - Không tin tưởng dữ liệu nếu signature không hợp lệ

5. **Response Code**:
   - `vnp_ResponseCode = "00"`: Thanh toán thành công
   - `vnp_TransactionStatus = "00"`: Giao dịch thành công
   - Cần check cả 2 giá trị này

6. **Idempotency**:
   - Nếu payment đã `paid`, callback vẫn return `true` nhưng không cập nhật lại

7. **Test Environment**:
   - Đảm bảo `ReturnUrl` và `IpnUrl` có thể truy cập được từ internet (VNPay cần gọi về)
   - **Bắt buộc**: Dùng **ngrok** để expose localhost hoặc deploy lên server public

---

## 🐛 Troubleshooting

### **Lỗi: "An error occurred during transaction process" (VNPay trả về lỗi khi mở payment URL)**

**Nguyên nhân phổ biến:**

1. **ReturnUrl/IpnUrl là localhost** ⚠️ **QUAN TRỌNG NHẤT**
   - VNPay **KHÔNG THỂ** gọi về `localhost` từ server của họ
   - **Giải pháp**: Dùng **ngrok** để expose localhost:
     ```bash
     # Cài đặt ngrok (nếu chưa có)
     # Windows: tải từ https://ngrok.com/download
     
     # Chạy ngrok để expose port 5168
     ngrok http 5168
     
     # Copy URL (ví dụ: https://abc123.ngrok.io)
     # Cập nhật appsettings.json:
     # "ReturnUrl": "https://abc123.ngrok.io/api/payment/vnpay-return"
     # "IpnUrl": "https://abc123.ngrok.io/api/payment/vnpay-ipn"
     ```

2. **Số tiền quá nhỏ (< 10,000 VND)**
   - VNPay yêu cầu số tiền tối thiểu 10,000 VND
   - **Giải pháp**: Cập nhật `MembershipFee` trong database >= 10,000 VND
   - Code đã có validation, sẽ báo lỗi nếu số tiền < 10,000 VND

3. **IP Address không hợp lệ**
   - Code đã tự động lấy IP từ request
   - Nếu vẫn lỗi, kiểm tra network configuration

4. **Signature sai**
   - Kiểm tra `HashSecret` trong `appsettings.json` có đúng không
   - Kiểm tra `TmnCode` có đúng không

### **Lỗi: "Checksum failed"**
- Kiểm tra `HashSecret` trong `appsettings.json` có đúng không
- Kiểm tra signature validation logic
- Đảm bảo không có ký tự đặc biệt trong `HashSecret`

### **Lỗi: "Payment not found"**
- Kiểm tra `vnp_TxnRef` (orderId) có đúng là `paymentId` không
- Kiểm tra payment đã được tạo trong DB chưa
- Kiểm tra `vnp_TxnRef` có phải là số không

### **Lỗi: "Số tiền thanh toán tối thiểu là 10,000 VND"**
- Cập nhật `MembershipFee` trong database >= 10,000 VND
- Hoặc test với CLB có phí >= 10,000 VND

### **Lỗi: Redirect không hoạt động**
- Kiểm tra `ReturnUrl` trong config (phải là public URL, không phải localhost)
- Kiểm tra CORS settings
- Kiểm tra frontend URL có đúng không

### **Lỗi: IPN không nhận được**
- VNPay cần gọi được đến server của bạn
- **Bắt buộc**: Dùng ngrok hoặc deploy lên server public
- Kiểm tra firewall/security group
- Kiểm tra `IpnUrl` trong config phải là public URL

---

## 📝 Checklist Test

- [ ] Tạo membership request thành công
- [ ] Club leader approve request
- [ ] Tạo VNPay payment URL thành công
- [ ] Thanh toán thành công trên VNPay sandbox
- [ ] ReturnUrl callback hoạt động đúng
- [ ] Payment status = `paid` trong DB
- [ ] Membership status = `active` trong DB
- [ ] MembershipRequest status = `completed` trong DB
- [ ] API `/api/payment/status` trả về đúng
- [ ] API `/api/payment/history` trả về đúng
- [ ] Test thanh toán thất bại (hủy)
- [ ] Test tạo lại payment URL

---

## 🚀 Quick Test Script (Postman/Thunder Client)

### **1. Create Payment**
```
POST http://localhost:5168/api/payment/vnpay/create
Authorization: Bearer {token}
Content-Type: application/json

{
  "membershipRequestId": 1
}
```

### **2. Check Status**
```
GET http://localhost:5168/api/payment/status
Authorization: Bearer {token}
```

### **3. Check History**
```
GET http://localhost:5168/api/payment/history
Authorization: Bearer {token}
```

---

**Chúc bạn test thành công! 🎉**

