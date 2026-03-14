using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProgrammingTutor.Services
{
    public class LeaderboardEntry
    {
        public string Username { get; set; } = string.Empty;
        public int Points { get; set; }
        public string Rank { get; set; } = "Novice";
        public string ValidationKey { get; set; } = string.Empty;
    }

    public class LeaderboardService
    {
        private readonly HttpClient _httpClient;
        // Public open Firebase DB for demonstration

        private string GetUrl() => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9wcm8tdHV0b3ItMTRlODAtZGVmYXVsdC1ydGRiLmV1cm9wZS13ZXN0MS5maXJlYmFzZWRhdGFiYXNlLmFwcC9sZWFkZXJib2FyZC5qc29u"));
        public LeaderboardService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        private string GenerateValidationKey(string username, int points)
        {
            // Simple app-side handshake to prevent simple spoofing
            string secret = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("UHJvVHV0b3JfQXJpc19TZWN1cmVfMjAyNg=="));
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes($"{username}:{points}:{secret}");
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public async Task<List<LeaderboardEntry>> GetTopUsersAsync()
        {
            var allEntries = await FetchAllEntriesAsync();
            return allEntries.OrderByDescending(e => e.Points).Take(15).ToList();
        }

        private async Task<List<LeaderboardEntry>> FetchAllEntriesAsync()
        {
            var entries = new List<LeaderboardEntry>();

            try
            {
                var response = await _httpClient.GetAsync(GetUrl());
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(json) && json != "null")
                    {
                        try
                        {
                            var dict = JsonSerializer.Deserialize<Dictionary<string, LeaderboardEntry>>(json);
                            if (dict != null)
                            {
                                entries = dict.Values.ToList();
                            }
                        }
                        catch
                        {
                            var list = JsonSerializer.Deserialize<List<LeaderboardEntry>>(json);
                            if (list != null)
                            {
                                entries = list.Where(x => x != null).ToList();
                            }
                        }
                    }
                    else
                    {
                        // Database is empty but accessible
                        return new List<LeaderboardEntry> 
                        { 
                            new LeaderboardEntry { Username = "Empty DB", Points = 0, Rank = "First!" }
                        };
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new List<LeaderboardEntry> 
                    { 
                        new LeaderboardEntry { Username = "Security Rules Error", Points = 0, Rank = "401 Unauthorized" }
                    };
                }
                else
                {
                    return new List<LeaderboardEntry> 
                    { 
                        new LeaderboardEntry { Username = "HTTP Error", Points = 0, Rank = response.StatusCode.ToString() }
                    };
                }
            }
            catch (Exception ex)
            {
                // Return offline placeholder if network fails
                return new List<LeaderboardEntry> 
                { 
                    new LeaderboardEntry { Username = "Connection Failed", Points = 0, Rank = ex.Message.Length > 20 ? ex.Message.Substring(0, 20) : ex.Message }
                };
            }

            // Verify integrity of fetched entries (ignore spoofed ones)
            var validEntries = entries.Where(e => e.ValidationKey == GenerateValidationKey(e.Username, e.Points)).ToList();
            
            if (entries.Count > 0 && validEntries.Count == 0)
            {
                return new List<LeaderboardEntry> 
                { 
                    new LeaderboardEntry { Username = "Integrity Error", Points = 0, Rank = "Spoofed data detected" }
                };
            }

            return validEntries;
        }

        // Simulating the rank locally before pushing to Firebase
        public string GetRank(int points)
        {
            if (points >= 5000) return "Master";
            if (points >= 3000) return "Expert";
            if (points >= 1500) return "Pro";
            if (points >= 500) return "Intermediate";
            return "Beginner";
        }

        public async Task UpdateUserScoreAsync(string username, int scoreToAdd)
        {
            if (username == "Guest" || string.IsNullOrWhiteSpace(username)) return;

            try
            {
                var currentEntries = await FetchAllEntriesAsync();
                
                // Remove offline placeholder if it exists
                currentEntries.RemoveAll(e => e.Username == "Offline Mode");

                var existingUser = currentEntries.FirstOrDefault(e => e.Username == username);
                
                if (existingUser != null)
                {
                    existingUser.Points += scoreToAdd;
                    existingUser.Rank = GetRank(existingUser.Points);
                }
                else
                {
                    currentEntries.Add(new LeaderboardEntry
                    {
                        Username = username,
                        Points = scoreToAdd,
                        Rank = GetRank(scoreToAdd)
                    });
                }

                // Sign all entries with the validation key before pushing
                foreach (var entry in currentEntries)
                {
                    entry.ValidationKey = GenerateValidationKey(entry.Username, entry.Points);
                }

                // Re-upload the entire list to our public open Firebase node
                var content = new StringContent(JsonSerializer.Serialize(currentEntries), System.Text.Encoding.UTF8, "application/json");
                await _httpClient.PutAsync(GetUrl(), content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update leaderboard: {ex.Message}");
            }
        }
    }
}
