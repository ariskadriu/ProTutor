using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ProgrammingTutor.Models;

namespace ProgrammingTutor.Core
{
    public class LessonManager
    {
        private readonly string _lessonsDirectory;
        private List<Lesson> _lessons = new List<Lesson>();

        public LessonManager()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _lessonsDirectory = Path.Combine(baseDir, "Content", "lessons");
            
            // Fallback for development (dotnet run)
            if (!Directory.Exists(_lessonsDirectory))
            {
                string? projectDir = Directory.GetParent(baseDir)?.Parent?.Parent?.FullName;
                if (projectDir != null)
                {
                    string fallbackPath = Path.Combine(projectDir, "Content", "lessons");
                    if (Directory.Exists(fallbackPath))
                    {
                        _lessonsDirectory = fallbackPath;
                    }
                }
            }

            if (!Directory.Exists(_lessonsDirectory))
            {
                Directory.CreateDirectory(_lessonsDirectory);
            }
        }

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public void LoadLessons()
        {
            _lessons.Clear();
            if (!Directory.Exists(_lessonsDirectory)) return;

            var files = Directory.GetFiles(_lessonsDirectory, "*.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var lesson = JsonSerializer.Deserialize<Lesson>(json, _jsonOptions);
                    if (lesson != null)
                    {
                        _lessons.Add(lesson);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading lesson from {file}: {ex.Message}");
                }
            }
        }

        public List<Lesson> GetLessons() => _lessons;

        public List<string> GetCategories()
        {
            return _lessons.Select(l => l.Id.Split('_')[0]).Distinct().ToList();
        }

        public List<Lesson> GetLessonsByCategory(string category)
        {
            return _lessons.Where(l => l.Id.StartsWith(category + "_")).ToList();
        }

        public Lesson? GetLessonById(string id)
        {
            return _lessons.FirstOrDefault(l => l.Id == id);
        }
    }
}
