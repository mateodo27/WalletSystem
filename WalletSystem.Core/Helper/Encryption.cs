using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace WalletSystem.Core.Helper
{
    public static class Encryption
    {
        public static string Encrypt(this string plaintext)
        {
            var ciphertext = string.Empty;

            var key = Convert.ToInt16(ConfigurationManager.AppSettings["EncryptionKey"]);

            foreach (char c in plaintext)
            {
                if (char.IsLetter(c))
                {
                    char shifted = (char)(((int)char.ToUpper(c) + key - 65) % 26 + 65);
                    ciphertext += char.IsLower(c) ? char.ToLower(shifted) : shifted;
                }
                else
                {
                    ciphertext += c;
                }
            }

            return ciphertext;
        }

        public static string Decrypt(this string ciphertext)
        {
            var plaintext = string.Empty;

            var key = Convert.ToInt16(ConfigurationManager.AppSettings["EncryptionKey"]);

            foreach (char c in ciphertext)
            {
                if (char.IsLetter(c))
                {
                    char shifted = (char)(((int)char.ToUpper(c) - key - 65 + 26) % 26 + 65);
                    plaintext += char.IsLower(c) ? char.ToLower(shifted) : shifted;
                }
                else
                {
                    plaintext += c;
                }
            }

            return plaintext;
        }
    }
}
