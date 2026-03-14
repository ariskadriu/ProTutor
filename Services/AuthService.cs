using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ProgrammingTutor.Models;

namespace ProgrammingTutor.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        // Obfuscated Firebase Base URL
        private string GetBaseUrl() => Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9wcm8tdHV0b3ItMTRlODAtZGVmYXVsdC1ydGRiLmV1cm9wZS13ZXN0MS5maXJlYmFzZWRhdGFiYXNlLmFwcC91c2Vycw=="));

        public AuthService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        private bool IsUsernameValid(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            // Block path traversal and special firebase characters
            char[] invalidChars = { '/', '\\', '.', '$', '#', '[', ']', ' ' };
            return !username.Any(c => invalidChars.Contains(c));
        }

        public async Task<(bool Success, string Message, User? User)> RegisterAsync(string username, string name, string surname, string password)
        {
            if (!IsUsernameValid(username))
            {
                return (false, "Invalid username. Use only letters and numbers.", null);
            }

            try
            {
                string baseUrl = GetBaseUrl();
                // 1. Check if user already exists in Firebase
                var checkResponse = await _httpClient.GetAsync($"{baseUrl}/{username}.json");
                if (checkResponse.IsSuccessStatusCode)
                {
                    string existingData = await checkResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(existingData) && existingData != "null")
                    {
                        return (false, "Username already exists globally.", null);
                    }
                }

                // 2. Create local salt and hash
                string salt = GenerateSalt();
                var user = new User
                {
                    Username = username,
                    Name = name,
                    Surname = surname,
                    Salt = salt,
                    PasswordHash = HashPassword(password, salt),
                    CreatedAt = DateTime.UtcNow
                };

                // 3. Save to Firebase
                string json = JsonSerializer.Serialize(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var saveResponse = await _httpClient.PutAsync($"{baseUrl}/{username}.json", content);

                if (saveResponse.IsSuccessStatusCode)
                {
                    return (true, "Global registration successful!", user);
                }
                
                return (false, $"Cloud error: {saveResponse.StatusCode}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Connection failed: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password)
        {
            if (!IsUsernameValid(username))
            {
                return (false, "Invalid username.", null);
            }

            try
            {
                string baseUrl = GetBaseUrl();
                // 1. Fetch user from Firebase
                var response = await _httpClient.GetAsync($"{baseUrl}/{username}.json");
                if (!response.IsSuccessStatusCode)
                {
                    return (false, "Account not found or network error.", null);
                }

                string json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json) || json == "null")
                {
                    return (false, "Invalid username or password.", null);
                }

                var user = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (user == null) return (false, "Data corruption error.", null);

                // 2. Verify password with salt
                string computedHash = string.IsNullOrEmpty(user.Salt) 
                    ? HashPassword(password, "") 
                    : HashPassword(password, user.Salt);

                if (user.PasswordHash != computedHash)
                {
                    return (false, "Invalid username or password.", null);
                }

                return (true, "Login successful!", user);
            }
            catch (Exception ex)
            {
                return (false, $"Cloud connection failed: {ex.Message}", null);
            }
        }

        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            if (string.IsNullOrEmpty(salt)) return string.Empty;

            byte[] saltBytes = Convert.FromBase64String(salt);
            // PBKDF2 with 100,000 iterations - Industry standard for brute-force protection
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32); // 256 bits
                return Convert.ToBase64String(hash);
            }
        }
    }
}
