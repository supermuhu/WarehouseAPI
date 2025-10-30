using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace WarehouseAPI.Services
{
    public class EncryptionService
    {
        private readonly IDataProtector dataProtector;

        public EncryptionService(IDataProtectionProvider dataProtectionProvider)
        {
            dataProtector = dataProtectionProvider.CreateProtector("Encryptinformationtoauthenticateaccounts");
        }

        public string Encrypt(string plainText)
        {
            return dataProtector.Protect(plainText);
        }

        public string Decrypt(string cipherText)
        {
            return dataProtector.Unprotect(cipherText);
        }
    }
}
