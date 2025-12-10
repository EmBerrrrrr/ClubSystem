using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Service.Helper
{
    public class VNPayHelper
    {
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _paymentUrl;
        private readonly string _returnUrl;
        private readonly string _ipnUrl;

        public VNPayHelper(string tmnCode, string hashSecret, string paymentUrl, string returnUrl, string ipnUrl)
        {
            _tmnCode = tmnCode;
            _hashSecret = hashSecret;
            _paymentUrl = paymentUrl;
            _returnUrl = returnUrl;
            _ipnUrl = ipnUrl;
        }

        public string CreatePaymentUrl(int paymentId, decimal amount, string orderInfo, string orderId)
        {
            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _tmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString()); // VNPay yêu cầu số tiền nhân 100
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1"); // Có thể lấy từ HttpContext
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", _returnUrl);
            vnpay.AddRequestData("vnp_TxnRef", orderId); // Mã đơn hàng (dùng paymentId)
            vnpay.AddRequestData("vnp_IpNUrl", _ipnUrl);

            string paymentUrl = vnpay.CreateRequestUrl(_paymentUrl, _hashSecret);
            return paymentUrl;
        }

        public bool ValidateSignature(Dictionary<string, string> vnpayData)
        {
            if (!vnpayData.ContainsKey("vnp_SecureHash"))
                return false;

            string vnp_SecureHash = vnpayData["vnp_SecureHash"];
            var dataToCheck = new Dictionary<string, string>(vnpayData);
            dataToCheck.Remove("vnp_SecureHash");
            if (dataToCheck.ContainsKey("vnp_SecureHashType"))
                dataToCheck.Remove("vnp_SecureHashType");

            var vnpay = new VnPayLibrary();
            foreach (var item in dataToCheck.OrderBy(x => x.Key))
            {
                vnpay.AddRequestData(item.Key, item.Value);
            }

            string checkSum = vnpay.CreateRequestUrl(_hashSecret);
            return checkSum.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public class VnPayLibrary
    {
        private SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            string queryString = data.ToString();

            baseUrl += "?" + queryString;
            string signData = queryString;
            if (signData.Length > 0)
            {
                signData = signData.Remove(signData.Length - 1, 1);
            }

            string vnp_SecureHash = HmacSHA512(vnp_HashSecret, signData);
            baseUrl += "vnp_SecureHash=" + vnp_SecureHash;

            return baseUrl;
        }

        public string CreateRequestUrl(string vnp_HashSecret)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            string queryString = data.ToString();
            string signData = queryString;
            if (signData.Length > 0)
            {
                signData = signData.Remove(signData.Length - 1, 1);
            }

            string vnp_SecureHash = HmacSHA512(vnp_HashSecret, signData);
            return vnp_SecureHash;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            string rspRaw = GetResponseData();
            string myChecksum = HmacSHA512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        public string GetResponseData()
        {
            StringBuilder data = new StringBuilder();
            var responseDataToCheck = new SortedList<string, string>(_responseData, new VnPayCompare());
            if (responseDataToCheck.ContainsKey("vnp_SecureHashType"))
            {
                responseDataToCheck.Remove("vnp_SecureHashType");
            }
            if (responseDataToCheck.ContainsKey("vnp_SecureHash"))
            {
                responseDataToCheck.Remove("vnp_SecureHash");
            }
            foreach (KeyValuePair<string, string> kv in responseDataToCheck)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            //remove last '&'
            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }
            return data.ToString();
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }

    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}

