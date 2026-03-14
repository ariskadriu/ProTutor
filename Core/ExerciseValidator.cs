using System;
using System.Threading.Tasks;
using System.Linq;
using ProgrammingTutor.Models;
using ProgrammingTutor.Services;

namespace ProgrammingTutor.Core
{
    public class ValidationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ExerciseValidator
    {
        private readonly CodeRunner _codeRunner;
        private readonly JsonStorageService _storageService;
        private readonly LeaderboardService _leaderboardService;
        private readonly CloudSyncService _cloudSyncService;

        public ExerciseValidator(CodeRunner codeRunner, JsonStorageService storageService)
        {
            _codeRunner = codeRunner;
            _storageService = storageService;
            _leaderboardService = new LeaderboardService();
            _cloudSyncService = new CloudSyncService();
        }

        public async Task<ValidationResult> ValidateAsync(Lesson lesson, int challengeIndex, string userCode)
        {
            if (lesson.Challenges == null || challengeIndex >= lesson.Challenges.Count)
            {
                return new ValidationResult { Success = false, Message = "Invalid challenge index." };
            }

            var challenge = lesson.Challenges[challengeIndex];

            // 1. Keyword Validation (New in Phase 3)
            foreach (var keyword in challenge.RequiredKeywords)
            {
                if (!userCode.Contains(keyword))
                {
                    string msg = App.CurrentLanguage == "sq" 
                        ? $"Kodi juaj duhet të përdorë: '{keyword}'" 
                        : $"Your code must use: '{keyword}'";
                    return new ValidationResult { Success = false, Message = msg };
                }
            }

            // 2. Output Validation
            (string Output, string Error) result;
            if (lesson.Id.StartsWith("py"))
                result = await _codeRunner.RunPythonAsync(userCode);
            else
                result = await _codeRunner.RunCSharpAsync(userCode);

            if (!string.IsNullOrEmpty(result.Error))
            {
                return new ValidationResult { Success = false, Message = result.Error };
            }

            bool isCorrect = result.Output.Trim() == challenge.ExpectedOutput.Trim();

            if (isCorrect)
            {
                // Award 50 points per challenge success to the global leaderboard
                _ = _leaderboardService.UpdateUserScoreAsync(App.CurrentUser?.Username ?? "Guest", 50);

                string successMsg = App.CurrentLanguage == "sq" 
                    ? "Saktë! Sfida u plotësua." 
                    : "Correct! Challenge completed.";
                
                return new ValidationResult { Success = true, Message = successMsg };
            }
            else
            {
                string failMsg = App.CurrentLanguage == "sq" 
                    ? $"Rezultati i gabuar. Pritet: '{challenge.ExpectedOutput}', por doli: '{result.Output.Trim()}'" 
                    : $"Incorrect output. Expected: '{challenge.ExpectedOutput}', but got: '{result.Output.Trim()}'";
                return new ValidationResult { Success = false, Message = failMsg };
            }
        }

        public async Task SaveProgressAsync(string lessonId)
        {
            string username = App.CurrentUser?.Username ?? "Guest";
            string fileName = $"progress_{username}.json";
            
            var progress = await _storageService.LoadAsync<Progress>(fileName) ?? new Progress();
            if (!progress.CompletedLessons.Contains(lessonId))
            {
                progress.CompletedLessons.Add(lessonId);
                await _storageService.SaveAsync(fileName, progress);
                
                // Cloud Sync
                await _cloudSyncService.SyncProgressToServerAsync(username, progress);
            }
        }
    }
}
