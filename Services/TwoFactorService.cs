using System;

namespace CarRentalSystem.Services
{
    public class TwoFactorService
    {
        public string GenerateSecretKey()
        {
            Random res = new Random();
            return res.Next(100000, 999999).ToString();
        }
        public string GenerateQrCodeBase64(string secretKey, string username, string issuer = "CarRentalSystem")
        {
            return string.Empty;
        }

        public bool ValidateCode(string secretKey, string code)
        {
            if (string.IsNullOrEmpty(code)) return false;
            return secretKey == code;
        }
    }
}