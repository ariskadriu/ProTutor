using System;
using System.Text;
using System.Linq;

namespace ProgrammingTutor.Core
{
    /// <summary>
    /// Holds the obfuscated API key to prevent casual discovery in the binary.
    /// </summary>
    public static class SecureConfig
    {
        // This is the obfuscated key. 
        // To update: Use the ObfuscateKey method below to generate a new string.
        private static readonly string ObfuscatedKey = "eHJ1dXkkeXl0dyAlJHdxeHRyd3V2IHN5eSciJXInJCJzJ3h0cngjcnFwd3RxJ3JxeHQlJXYgeHkjc3cjcXRxdGxwN2wzLmwqMg=="; 
        private static readonly string ObfuscatedGeminiKey1 = "IDhEZDhSRWxSVnBoY1JTWHhvdGdMWVlxOGF1czdNQ0h5U3phSUE=";
        private static readonly string ObfuscatedGeminiKey2 = "Z3dsVTY5LVZnYmJaMnI3cnN6b2o2NUwtUDNoRU9tTEVNVEN5U3phSUE=";

        private static readonly byte XOR_KEY = 0x41; // 'A' for Aris

        /// <summary>
        /// Retrieves the real OpenRouter API key.
        /// </summary>
        public static string GetApiKey() => Decrypt(ObfuscatedKey);

        /// <summary>
        /// Retrieves all available Gemini API keys.
        /// </summary>
        public static string[] GetGeminiApiKeys()
        {
            var keys = new List<string>();
            string k1 = Decrypt(ObfuscatedGeminiKey1);
            if (!string.IsNullOrEmpty(k1)) keys.Add(k1);
            
            string k2 = Decrypt(ObfuscatedGeminiKey2);
            if (!string.IsNullOrEmpty(k2)) keys.Add(k2);
            
            return keys.ToArray();
        }

        private static string Decrypt(string obfuscated)
        {
            try
            {
                byte[] data = Convert.FromBase64String(obfuscated);
                byte[] decoded = data.Select(b => (byte)(b ^ XOR_KEY)).Reverse().ToArray();
                return Encoding.UTF8.GetString(decoded);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Utility method for the CREATOR to generate the obfuscated string.
        /// Copy the output of this and paste it as 'ObfuscatedKey'.
        /// </summary>
        public static string ObfuscateKey(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            // Step 1: Reverse & XOR
            byte[] data = Encoding.UTF8.GetBytes(plainText).Reverse().Select(b => (byte)(b ^ XOR_KEY)).ToArray();
            
            // Step 2: Base64 encode
            return Convert.ToBase64String(data);
        }
    }
}
