using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.Claims;

namespace WarehouseAPI.Helpers
{
    public class Utils
    {
        /// <summary>
        /// Lấy AccountId từ JWT token claims
        /// </summary>
        public static int? GetCurrentAccountId(ClaimsPrincipal user)
        {
            var accountIdClaim = user.FindFirst("AccountId");
            if (accountIdClaim != null && int.TryParse(accountIdClaim.Value, out int accountId))
            {
                return accountId;
            }
            return null;
        }

        /// <summary>
        /// Lấy Role từ JWT token claims
        /// </summary>
        public static string GetCurrentRole(ClaimsPrincipal user)
        {
            return user.FindFirst("Role")?.Value ?? "";
        }

        /// <summary>
        /// Lấy Username từ JWT token claims
        /// </summary>
        public static string GetCurrentUsername(ClaimsPrincipal user)
        {
            return user.FindFirst("Username")?.Value ?? "";
        }
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }


        // có chế biến cho .NET Core MVC
        public static string GetIpAddress(HttpContext context)
        {
            var ipAddress = string.Empty;
            try
            {
                var remoteIpAddress = context.Connection.RemoteIpAddress;

                if (remoteIpAddress != null)
                {
                    if (remoteIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        remoteIpAddress = Dns.GetHostEntry(remoteIpAddress).AddressList
                            .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                    }

                    if (remoteIpAddress != null) ipAddress = remoteIpAddress.ToString();

                    return ipAddress;
                }
            }
            catch (Exception ex)
            {
                return "Invalid IP:" + ex.Message;
            }

            return "127.0.0.1";
        }
        public static string GenerateSlug(string input)
        {
            // Loại bỏ dấu tiếng Việt
            string normalizedString = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            string withoutDiacritics = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            // Thay thế khoảng trắng và ký tự không hợp lệ bằng dấu gạch ngang
            string slug = Regex.Replace(withoutDiacritics, @"\s+", "-"); // Thay khoảng trắng bằng "-"
            slug = Regex.Replace(slug, @"[^a-zA-Z0-9\-]", ""); // Loại bỏ ký tự không hợp lệ
            slug = slug.ToLowerInvariant(); // Chuyển về chữ thường

            return slug;
        }
    }
}
