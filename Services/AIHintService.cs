using System.Text.RegularExpressions;

namespace ProgrammingTutor.Services
{
    public class AIHintService
    {
        private readonly Dictionary<string, int> _hintRequests = new();
        private readonly Random _random = new();

        private enum AIPersona { TechnicalCoach, EncouragingMentor, CodeDetective }

        public async Task<string> GetHintAsync(string lessonTitle, string challengeExercise, string userCode, List<string> requiredKeywords, string lessonId, int challengeIndex)
        {
            // Human-like variable delay (thinking time)
            int thinkingTime = _random.Next(1000, 2500); 
            await Task.Delay(thinkingTime);

            string challengeKey = $"{lessonId}_{challengeIndex}";
            _hintRequests[challengeKey] = _hintRequests.GetValueOrDefault(challengeKey, 0) + 1;
            int hintLevel = _hintRequests[challengeKey];

            // Pick a random persona for this interaction to feel less robotic
            AIPersona persona = (AIPersona)_random.Next(0, 3);
            string prefix = GetPersonaPrefix(persona, hintLevel);

            if (string.IsNullOrWhiteSpace(userCode))
            {
                return $"{prefix} Your workspace is empty. The instructions say: '{challengeExercise.Split('.')[0]}'. Try starting with the basic structure!";
            }

            // Advanced Regex Analysis
            string analysis = AnalyzeCode(userCode, lessonId);
            if (!string.IsNullOrEmpty(analysis) && hintLevel > 1)
            {
                return $"{prefix} {analysis}";
            }

            // Tiered Logic
            if (hintLevel >= 3)
            {
                string keywordsString = string.Join(", ", requiredKeywords);
                return $"{prefix} You're working hard! Here's a heavy hint: make sure your code uses these specific terms: [{keywordsString}]. Focus on the syntax!";
            }

            if (hintLevel == 2)
            {
                var missing = requiredKeywords.FirstOrDefault(kw => !userCode.Contains(kw));
                if (missing != null)
                {
                    return $"{prefix} I found a missing piece! You definitely need to incorporate `{missing}` to solve this.";
                }
            }

            return $"{prefix} Your code looks like a good start. Check for small typos or missing brackets. You've got this!";
        }

        private string GetPersonaPrefix(AIPersona persona, int level)
        {
            return persona switch
            {
                AIPersona.TechnicalCoach => $"[Aris AI: Technical Coach] (Step {level}):",
                AIPersona.EncouragingMentor => $"[Aris AI: Mentor] (Step {level}):",
                AIPersona.CodeDetective => $"[Aris AI: Detective] (Step {level}):",
                _ => $"[Aris AI] (Step {level}):"
            };
        }

        private string AnalyzeCode(string code, string langId)
        {
            if (langId.StartsWith("py"))
            {
                if (code.Contains("def") && !code.Contains(":")) return "I see a function, but is it missing a colon ':' at the end of the line?";
                if (Regex.Matches(code, "\n").Count > 1 && !code.Contains("    ")) return "In Python, indentation (using the Tab key or 4 spaces) is critical. Make sure your logic is indented!";
            }
            else if (langId.StartsWith("cs"))
            {
                if (Regex.IsMatch(code, @"\w+\([^)]*\)\s*{") && !code.Contains(";")) return "Careful! In C#, almost every line (except brackets) must end with a semicolon ';'.";
            }
            return "";
        }
    }
}

