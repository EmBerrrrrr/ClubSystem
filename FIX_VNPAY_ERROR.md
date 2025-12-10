# 🔧 Fix Lỗi VNPay: "An error occurred during transaction process"

## ❌ Vấn Đề

Khi mở `paymentUrl` từ VNPay, bạn gặp lỗi:
```
An error occurred during transaction process. Please contact 1900 55 55 77 for assistance
```

## ✅ Giải Pháp

### **1. Sử dụng Ngrok (BẮT BUỘC cho test local)**

VNPay **KHÔNG THỂ** gọi về `localhost` từ server của họ. Bạn **PHẢI** dùng ngrok.

#### **Bước 1: Cài đặt Ngrok**

**Windows:**
```bash
# Tải từ: https://ngrok.com/download
# Hoặc dùng Chocolatey:
choco install ngrok
```

**Mac:**
```bash
brew install ngrok
```

**Linux:**
```bash
# Tải từ: https://ngrok.com/download
# Hoặc dùng snap:
snap install ngrok
```

#### **Bước 2: Chạy Ngrok**

1. **Đảm bảo API đang chạy** tại `http://localhost:5168`

2. **Mở terminal mới** và chạy:
   ```bash
   ngrok http 5168
   ```

3. **Copy URL ngrok** (ví dụ: `https://abc123.ngrok.io`)
   - URL này sẽ thay đổi mỗi lần chạy ngrok (trừ khi dùng account có tên miền cố định)

#### **Bước 3: Cập nhật appsettings.json**

Mở `ClubSystem/appsettings.json` và cập nhật:

```json
{
  "VNPay": {
    "TmnCode": "FJ9A99T4",
    "HashSecret": "TRVIOSZTHRDE0W8FMWQTRTU0W8F5V9KR",
    "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ReturnUrl": "https://abc123.ngrok.io/api/payment/vnpay-return",
    "IpnUrl": "https://abc123.ngrok.io/api/payment/vnpay-ipn"
  }
}
```

**Lưu ý**: Thay `abc123.ngrok.io` bằng URL ngrok thực tế của bạn.

#### **Bước 4: Restart Application**

```bash
# Dừng application (Ctrl+C)
# Chạy lại:
dotnet run --project ClubSystem/ClubSystem.csproj
```

---

### **2. Kiểm tra Số Tiền Tối Thiểu**

VNPay yêu cầu số tiền tối thiểu là **10,000 VND**.

#### **Kiểm tra trong Database:**

```sql
-- Kiểm tra MembershipFee của CLB
SELECT Id, Name, MembershipFee FROM Clubs WHERE Id = {clubId};

-- Nếu MembershipFee < 10000, cập nhật:
UPDATE Clubs SET MembershipFee = 10000 WHERE Id = {clubId};
```

#### **Hoặc test với CLB có phí >= 10,000 VND:**

Tạo hoặc chọn CLB có `MembershipFee >= 10000` để test.

---

### **3. Kiểm tra Code đã được cập nhật**

Code đã được cập nhật để:
- ✅ Tự động lấy IP từ request (không còn hardcode `127.0.0.1`)
- ✅ Validate số tiền tối thiểu (10,000 VND)
- ✅ Hỗ trợ truyền IP từ controller

**Không cần thay đổi gì thêm trong code.**

---

## 🧪 Test Lại

Sau khi setup ngrok và cập nhật config:

1. **Tạo lại payment URL**:
   ```bash
   POST /api/payment/vnpay/create
   ```

2. **Mở `paymentUrl` trong browser**

3. **Kỳ vọng**: Không còn lỗi, hiển thị form thanh toán VNPay

4. **Điền thông tin thẻ test**:
   - Số thẻ: `9704198526191432198`
   - Tên: `NGUYEN VAN A`
   - Ngày: `07/15`
   - OTP: `123456`

5. **Thanh toán thành công** → Redirect về ngrok URL → Redirect về frontend

---

## 📝 Checklist

- [ ] Đã cài đặt ngrok
- [ ] Đã chạy `ngrok http 5168`
- [ ] Đã copy URL ngrok
- [ ] Đã cập nhật `ReturnUrl` và `IpnUrl` trong `appsettings.json`
- [ ] Đã restart application
- [ ] Đã kiểm tra `MembershipFee >= 10000` VND
- [ ] Đã test lại và không còn lỗi

---

## ⚠️ Lưu Ý

1. **URL ngrok thay đổi mỗi lần chạy** (trừ khi dùng account có tên miền cố định)
   - Mỗi lần chạy ngrok mới, cần cập nhật lại `appsettings.json`

2. **Ngrok free có giới hạn**:
   - Session timeout sau 2 giờ không dùng
   - Có thể bị rate limit nếu dùng nhiều

3. **Production**:
   - Không dùng ngrok
   - Deploy lên server public (Azure, AWS, VPS, ...)
   - Cập nhật `ReturnUrl` và `IpnUrl` thành domain thật

---

## 🆘 Vẫn Còn Lỗi?

Nếu vẫn còn lỗi sau khi làm theo hướng dẫn:

1. **Kiểm tra ngrok đang chạy**:
   - Mở http://localhost:4040 (ngrok web interface)
   - Xem requests có đến không

2. **Kiểm tra application log**:
   - Xem có lỗi gì trong console không

3. **Kiểm tra VNPay config**:
   - `TmnCode` và `HashSecret` có đúng không
   - URL ngrok có đúng format không (phải là `https://`)

4. **Test với số tiền lớn hơn**:
   - Thử với `MembershipFee = 50000` VND

---

**Chúc bạn test thành công! 🎉**

