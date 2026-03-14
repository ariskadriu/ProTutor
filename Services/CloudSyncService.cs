using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ProgrammingTutor.Models;

namespace ProgrammingTutor.Services
{
    public class CloudSyncService
    {
        private readonly HttpClient _httpClient;
        private string GetBaseUrl() => Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9wcm8tdHV0b3ItMTRlODAtZGVmYXVsdC1ydGRiLmV1cm9wZS13ZXN0MS5maXJlYmFzZWRhdGFiYXNlLmFwcC9wcm9ncmVzcw=="));
        private string GetSecret() => Encoding.UTF8.GetString(Convert.FromBase64String("UHJvVHV0b3JfQXJpc19Qcm9ncmVzc19TZWN1cmVfMjAyNg=="));

        public CloudSyncService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        private string GenerateValidationKey(string username, Progress progress)
        {
            string lessonData = string.Join(",", progress.CompletedLessons.OrderBy(x => x));
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes($"{username}:{lessonData}:{GetSecret()}");
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public async Task SyncProgressToServerAsync(string username, Progress progress)
        {
            if (username == "Guest" || string.IsNullOrWhiteSpace(username)) return;

            try
            {
                progress.ValidationKey = GenerateValidationKey(username, progress);
                string json = JsonSerializer.Serialize(progress);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PutAsync($"{GetBaseUrl()}/{username}.json", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cloud Sync Error (Upload): {ex.Message}");
            }
        }

        public async Task<Progress?> DownloadProgressFromServerAsync(string username)
        {
            if (username == "Guest" || string.IsNullOrWhiteSpace(username)) return null;

            try
            {
                var response = await _httpClient.GetAsync($"{GetBaseUrl()}/{username}.json");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(json) && json != "null")
                    {
                        var progress = JsonSerializer.Deserialize<Progress>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        // Verify Integrity
                        if (progress != null && progress.ValidationKey == GenerateValidationKey(username, progress))
                        {
                            return progress;
                        }
                        else if (progress != null)
                        {
                            Console.WriteLine("Cloud Progress Validation Failed: Data potentially tampered.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cloud Sync Error (Download): {ex.Message}");
            }
            return null;
        }
    }
}
