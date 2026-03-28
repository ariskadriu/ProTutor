using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProgrammingTutor.Services
{
    public class JsonStorageService
    {
        private readonly string _dataDirectory;

        public JsonStorageService()
        {
            // Get the directory where the application is running
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }

        public async Task SaveAsync<T>(string fileName, T data)
        {
            try
            {
                string filePath = Path.Combine(_dataDirectory, fileName);
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                // In a real app, we'd log this
                Console.WriteLine($"Error saving JSON: {ex.Message}");
            }
        }

        public async Task<T?> LoadAsync<T>(string fileName) where T : class, new()
        {
            try
            {
                string filePath = Path.Combine(_dataDirectory, fileName);
                if (!File.Exists(filePath))
                {
                    return new T();
                }

                string json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading JSON: {ex.Message}");
                return new T();
            }
        }
    }
}
