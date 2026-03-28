using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ProgrammingTutor.Core;
using System;

namespace ProgrammingTutor.Services
{
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "system", "user", or "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class AIHintService
    {
        private readonly HttpClient _httpClient;
        private readonly List<ChatMessage> _history = new List<ChatMessage>();
        private readonly Random _random = new();
        private readonly string[] _fallbackModels = {
            "openrouter/free",
            "qwen/qwen3-coder:free",
            "meta-llama/llama-3.3-70b-instruct:free",
            "google/gemma-3-27b-it:free",
            "nousresearch/hermes-3-llama-3.1-405b:free"
        };

        public AIHintService() 
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        }

        public async Task<string> GetHintAsync(string lessonTitle, string challengeExercise, string userCode, List<string> requiredKeywords, string lessonId, int challengeIndex, string lastError = "")
        {
            string apiKey = SecureConfig.GetApiKey();
            bool isAl = App.CurrentLanguage == "sq";

            if (string.IsNullOrWhiteSpace(apiKey))
                return await GetLocalHeuristicHint(lessonTitle, userCode, requiredKeywords, lessonId, isAl);

            // Prepare context
            string userContext = GetUserPrompt(lessonTitle, challengeExercise, userCode, requiredKeywords, lessonId, lastError);
            
            if (_history.Count == 0)
                _history.Add(new ChatMessage { Role = "system", Content = GetSystemPrompt(isAl) });

            _history.Add(new ChatMessage { Role = "user", Content = userContext });

            return await GetAIResponseWithRotationAsync(isAl);
        }

        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            string apiKey = SecureConfig.GetApiKey();
            bool isAl = App.CurrentLanguage == "sq";

            if (string.IsNullOrWhiteSpace(apiKey)) 
                return isAl ? "[GABIM] API nuk disponohet." : "[ERROR] API unavailable.";

            if (_history.Count == 0)
                _history.Add(new ChatMessage { Role = "system", Content = GetSystemPrompt(isAl) });
            
            _history.Add(new ChatMessage { Role = "user", Content = userMessage });

            return await GetAIResponseWithRotationAsync(isAl);
        }

        private async Task<string> GetAIResponseWithRotationAsync(bool isAl)
        {
            // Trim history to prevent context overflow (Keep system prompt + last 14 messages)
            if (_history.Count > 15)
            {
                // Preserve system prompt at index 0, remove the oldest User/Assistant pair
                if (_history.Count > 1 && _history[1].Role != "system")
                {
                    _history.RemoveAt(1); // Oldest User
                    if (_history.Count > 1 && _history[1].Role == "assistant")
                        _history.RemoveAt(1); // Oldest Assistant
                }
            }

            string apiKey = SecureConfig.GetApiKey();
            string lastError = "";

            foreach (var modelId in _fallbackModels)
            {
                try
                {
                    var requestBody = new
                    {
                        model = modelId,
                        messages = _history.Select(m => new { role = m.Role.ToLower(), content = m.Content }).ToArray(),
                        temperature = 0.9
                    };

                    var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://programmingtutor.app");
                    _httpClient.DefaultRequestHeaders.Add("X-Title", "Programming Tutor IDE");

                    var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(responseJson);
                        var aiResponse = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                        
                        _history.Add(new ChatMessage { Role = "assistant", Content = aiResponse });
                        return aiResponse;
                    }

                    int statusCode = (int)response.StatusCode;
                    lastError = await response.Content.ReadAsStringAsync();
                    
                    // If any error occurs, log it and try the NEXT model in rotation
                    Console.WriteLine($"[ROTATION] OpenRouter Model {modelId} failed ({statusCode}): {lastError.Substring(0, Math.Min(50, lastError.Length))}...");
                    continue; 
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Console.WriteLine($"[ROTATION] OpenRouter Exception ({modelId}): {ex.Message}");
                    continue;
                }
            }

            // If we've exhausted all OpenRouter models, try Google Gemini Native (with Key Rotation)
            string[] geminiKeys = SecureConfig.GetGeminiApiKeys();
            int keyIndex = 1;
            foreach (var geminiKey in geminiKeys)
            {
                Console.WriteLine($"[ROTATION] Trying Gemini Account #{keyIndex}...");
                var geminiResponse = await CallGeminiNativeAsync(geminiKey, isAl);
                
                if (!geminiResponse.StartsWith("[ERROR]"))
                {
                    return geminiResponse;
                }
                
                Console.WriteLine($"[ROTATION] Gemini Account #{keyIndex} failed: {geminiResponse}");
                keyIndex++;
            }

            // If we've exhausted everything
            Console.WriteLine("[ROTATION] CRITICAL: All providers exhausted.");
            
            // Clean up history so we don't have consecutive User messages on the next attempt
            if (_history.Count > 0 && _history.Last().Role == "user")
            {
                _history.RemoveAt(_history.Count - 1);
            }

            return isAl 
                ? "Aris (AI) nuk është këtu momentalisht, ju lutem provoni përsëri më vonë." 
                : "Aris (The AI) isn't here at the moment, please come back later.";
        }

        private async Task<string> CallGeminiNativeAsync(string apiKey, bool isAl)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear(); // CRITICAL: REMOVE OPENROUTER HEADERS!

                // Google Gemini is VERY strict: Roles MUST alternate User -> Model -> User.
                var contents = new List<object>();
                string lastRole = "";
                foreach (var m in _history.Where(msg => msg.Role != "system"))
                {
                    string currentRole = m.Role == "assistant" ? "model" : "user";
                    if (currentRole == lastRole) continue; // Safety: Never send same role twice
                    
                    contents.Add(new
                    {
                        role = currentRole,
                        parts = new[] { new { text = m.Content } }
                    });
                    lastRole = currentRole;
                }

                var googleRequest = new
                {
                    contents = contents.ToArray(),
                    system_instruction = new
                    {
                        parts = new[] { new { text = GetSystemPrompt(isAl) } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.9,
                        maxOutputTokens = 1000
                    }
                };

                var json = JsonSerializer.Serialize(googleRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
                
                var response = await _httpClient.PostAsync(url, content);
                string responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                    {
                        var candidate = candidates[0];
                        if (candidate.TryGetProperty("content", out var resContent))
                        {
                            var aiResponse = resContent.GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
                            _history.Add(new ChatMessage { Role = "assistant", Content = aiResponse });
                            return aiResponse;
                        }
                    }
                    
                    Console.WriteLine($"[GEMINI] Blocked: {responseJson}");
                    return "[ERROR] Gemini blocked/empty.";
                }

                Console.WriteLine($"[GEMINI] API Error {response.StatusCode}: {responseJson}");
                return $"[ERROR] Gemini {response.StatusCode}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GEMINI] Exception: {ex.Message}");
                return $"[ERROR] {ex.Message}";
            }
        }

        public void ClearHistory()
        {
            _history.Clear();
        }

        private string GetSystemPrompt(bool isAl)
        {
            if (isAl)
                return "Emri yt është ARIS. Ti je një Mentor i Programimit miqësor dhe njerëzor. Misioni yt është të ndihmosh studentët me gabimet në kod (Python/C#). Nëse të pyesin për gjëra jashtë programimit, ktheji me mirësjellje tek kodi duke përdorur fjalët e tua, jo fjali të gatshme. Je i shkurtër, teknik, por motivues. Përgjigju vetëm në Shqip.";
            
            return "Your name is ARIS. You are a friendly and human Programming Mentor. Your mission is to help students with coding challenges (Python/C#). If asked about unrelated topics, politely redirect them back to coding using your own natural phrasing instead of a fixed script. Be concise, technical, and motivating.";
        }

        private string GetUserPrompt(string title, string exercise, string code, List<string> keywords, string lang, string error)
        {
            string targetLang = lang.StartsWith("py") ? "Python" : "C#";
            string errorContext = string.IsNullOrWhiteSpace(error) ? "" : $"\nLast Terminal Error:\n{error}";
            
            return $"Lesson: {title}\nChallenge: {exercise}\nTarget Language: {targetLang}\nRequired Keywords: {string.Join(", ", keywords ?? new List<string>())}\n{errorContext}\n\nStudent's Code:\n```\n{code}\n```\n\nAnalyze the error (if any) and the code, then give a hint toward fixing it.";
        }

        private async Task<string> GetLocalHeuristicHint(string lessonTitle, string userCode, List<string> requiredKeywords, string lessonId, bool isAl)
        {
            // Simulate AI "Thinking" to maintain parity with the premium feel
            await Task.Delay(_random.Next(1200, 2500));
            
            var hint = isAl 
                ? $"Shihet që po punoni në '{lessonTitle}'. " 
                : $"I see you're working on '{lessonTitle}'. ";

            if (string.IsNullOrWhiteSpace(userCode))
            {
                hint += isAl 
                    ? "Filloni duke shkruar kodin për të zgjidhur sfidën. Referojuni shpjegimit." 
                    : "Start by writing some code to solve the challenge. Refer to the explanation for help.";
                return isAl ? $"[ARIS AI LOKAL] {hint}" : $"[ARIS LOCAL AI] {hint}";
            }

            // Check for common pitfalls
            if (lessonId.StartsWith("py"))
            {
                if (userCode.Contains("==") && !userCode.Contains("if "))
                    hint += isAl ? "Harruat të përdorni 'if' për krahasimin tuaj? '==' përdoret brenda kushteve." : "Did you forget to use 'if' for your comparison? '==' is used inside conditions.";
                else if (userCode.Count(c => c == '(') != userCode.Count(c => c == ')'))
                    hint += isAl ? "Kontrolloni kllapat tuaja! Sigurohuni që çdo '(' ka një ')' përkatëse." : "Check your parentheses! Ensure every '(' has a matching ')'.";
                else if (requiredKeywords != null && !requiredKeywords.Any(k => userCode.Contains(k)))
                    hint += isAl ? $"Provoni të përdorni: {string.Join(", ", requiredKeywords)}" : $"Try incorporating: {string.Join(", ", requiredKeywords)}";
                else
                    hint += isAl ? "Kodi duket mirë deri tani. Kontrolloni nese i keni plotësuar të gjitha kërkesat e sfidës." : "Your code looks solid so far. Double-check if you've met all challenge requirements.";
            }
            else // C#
            {
                if (!userCode.Trim().EndsWith(";") && !userCode.Contains("{"))
                    hint += isAl ? "Mos harroni pikëpresjen ';' në fund të rreshtit!" : "Don't forget the semicolon ';' at the end of your lines!";
                else if (userCode.Count(c => c == '{') != userCode.Count(c => c == '}'))
                    hint += isAl ? "Mbyllni kllapat gjarpërushe {}! Ato duhet të jenë gjithmonë çift." : "Close your curly braces {}! They should always come in pairs.";
                else
                    hint += isAl ? "Struktura e C# duket në rregull. Sigurohuni që tipat e të dhënave të jenë të saktë." : "Your C# structure looks correct. Ensure your data types are properly defined.";
            }

            return isAl ? $"[ARIS AI LOKAL] {hint}" : $"[ARIS LOCAL AI] {hint}";
        }
    }
}

