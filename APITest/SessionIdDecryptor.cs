namespace WebServiceInfrastructure
{
    using System;
    using System.IO;
    using System.Security.Cryptography;

    public static class SessionIdDecryptor
    {
        public static string Decrypt(string password, string encryptedSessionId, string encodedSalt)
        {
            var encryptedSessionIdData = Convert.FromBase64String(encryptedSessionId);
            var decodedSaltData = Convert.FromBase64String(encodedSalt);
            var key = ComputeKeyFromPassword(password, decodedSaltData);
            var sessionIdData = Decrypt(key, encryptedSessionIdData);
            var sessionId = System.Text.Encoding.UTF8.GetString(sessionIdData);
            return sessionId;
        }

        private static byte[] ComputeKeyFromPassword(string password, byte[] saltData)
        {
            var passwordData = System.Text.Encoding.UTF8.GetBytes(password);
            var key = GetEncryptionKeyFromPassword(passwordData, saltData);
            return key;
        }

        private static byte[] Decrypt(byte[] key, byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var rijndaelManaged = new RijndaelManaged())
                {
                    rijndaelManaged.Key = key;
                    rijndaelManaged.Mode = CipherMode.ECB;
                    using (var cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(),
                        CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                        cryptoStream.FlushFinalBlock();
                        return memoryStream.ToArray();
                    }
                }
            }
        }

        private static byte[] GetEncryptionKeyFromPassword(byte[] password, byte[] salt)
        {
            var keyGen = new Rfc2898DeriveBytes(password, salt, 1000); // 1000 fix
            var key = keyGen.GetBytes(32);
            return key;
        }
    }
}

