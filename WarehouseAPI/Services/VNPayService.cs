// using Microsoft.Extensions.Options;
// using WarehouseAPI.Configuration;
// using WarehouseAPI.Helpers;
// using WarehouseAPI.ModelView.VNPay;

// namespace WarehouseAPI.Services
// {
//     public class VNPayService
//     {
//         private readonly VNPayConfiguration vnpayConfig;
//         public VNPayService(IOptionsMonitor<VNPayConfiguration> vnpayConfig) 
//         {
//             this.vnpayConfig = vnpayConfig.CurrentValue;
//         }
//         public string CreatePaymentUrl(HttpContext context, VNPaymentRequestModel model)
//         {
//             var tick = DateTime.Now.Ticks.ToString();

//             var vnpay = new VNPayLibrary();
//             vnpay.AddRequestData("vnp_Version", vnpayConfig.Version);
//             vnpay.AddRequestData("vnp_Command", vnpayConfig.Command);
//             vnpay.AddRequestData("vnp_TmnCode", vnpayConfig.TmnCode);
//             vnpay.AddRequestData("vnp_Amount", ((long)model.Amount).ToString());

//             vnpay.AddRequestData("vnp_CreateDate", model.CreatedDate.ToString("yyyyMMddHHmmss"));
//             vnpay.AddRequestData("vnp_CurrCode", vnpayConfig.Currency);
//             vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
//             vnpay.AddRequestData("vnp_Locale", vnpayConfig.Locale);

//             vnpay.AddRequestData("vnp_OrderInfo", model.IdOrder.ToString());
//             vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other
//             if(model.FromWebsite == true)
//             {
//                 vnpay.AddRequestData("vnp_ReturnUrl", vnpayConfig.WebPaymentBackReturnUrl);
//             }
//             else if(model.IsPayRemaining == true)
//             {
//                 vnpay.AddRequestData("vnp_ReturnUrl", vnpayConfig.PaymentRemainingBackReturnUrl);
//             }
//             else
//             {
//                 vnpay.AddRequestData("vnp_ReturnUrl", vnpayConfig.PaymentBackReturnUrl);
//             }
//             var txnRef = $"{model.IdOrder}_{DateTime.Now:yyyyMMddHHmmssfff}";

//             vnpay.AddRequestData("vnp_TxnRef", txnRef); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

//             var paymentUrl = vnpay.CreateRequestUrl(vnpayConfig.Url, vnpayConfig.HashSecret);

//             return paymentUrl;
//         }
//         public VNPaymentResponseModel PaymentExecute(IQueryCollection collections)
//         {
//             var vnpay = new VNPayLibrary();
//             foreach (var (key, value) in collections)
//             {
//                 if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
//                 {
//                     vnpay.AddResponseData(key, value.ToString());
//                 }
//             }
//             var vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
//             string idOrderStr = vnp_TxnRef.Contains("_") ? vnp_TxnRef.Split('_')[0] : vnp_TxnRef;

//             var vnp_orderId = idOrderStr;
//             var vnp_TransactionId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
//             var vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
//             var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
//             var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

//             bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnpayConfig.HashSecret);
//             if (!checkSignature)
//             {
//                 return null;
//             }

//             return new VNPaymentResponseModel
//             {
//                 PaymentMethod = "VNPay",
//                 OrderDescription = vnp_OrderInfo,
//                 IdOrder = vnp_orderId,
//                 TransactionId = vnp_TransactionId.ToString(),
//                 Token = vnp_SecureHash,
//                 VNPayResponseCode = vnp_ResponseCode
//             };
//         }
//     }
// }
