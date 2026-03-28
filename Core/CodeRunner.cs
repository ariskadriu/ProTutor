using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ProgrammingTutor.Core
{
    public class CodeRunner
    {
        private readonly string _tempDir;
        private Process? _activeProcess;
        
        public event Action<string>? OutputReceived;
        public event Action<string>? ErrorReceived;
        public event Action<int>? ProcessExited;

        public CodeRunner()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ProgrammingTutor_Temp");
            if (!Directory.Exists(_tempDir))
            {
                Directory.CreateDirectory(_tempDir);
            }
        }

        public async Task RunPythonAsync(string code)
        {
            string filePath = Path.Combine(_tempDir, "temp.py");
            await File.WriteAllTextAsync(filePath, code);

            // Use -u for unbuffered output in Python
            await ExecuteProcessAsync("python", $"-u \"{filePath}\"");
        }

        public async Task RunCSharpAsync(string code)
        {
            KillProcessByName("temp");

            string projectDir = Path.Combine(_tempDir, "CSharpLocalProject");
            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
                string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
                await File.WriteAllTextAsync(Path.Combine(projectDir, "temp.csproj"), csproj);
            }

            string programPath = Path.Combine(projectDir, "Program.cs");
            await File.WriteAllTextAsync(programPath, code);

            await ExecuteProcessAsync("dotnet", "run", projectDir);
        }

        public void SendInput(string input)
        {
            if (_activeProcess != null && !_activeProcess.HasExited)
            {
                _activeProcess.StandardInput.WriteLine(input);
            }
        }

        public void Stop()
        {
            if (_activeProcess != null && !_activeProcess.HasExited)
            {
                try { _activeProcess.Kill(true); } catch { }
            }
        }

        private async Task ExecuteProcessAsync(string command, string arguments, string workingDir = null!)
        {
            Stop(); 

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir ?? _tempDir
            };

            _activeProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            _activeProcess.Exited += (s, e) => 
            {
                ProcessExited?.Invoke(_activeProcess.ExitCode);
                CleanupTempFiles(command, arguments, workingDir);
            };

            try
            {
                _activeProcess.Start();
                
                // Read Output and Error in separate threads/tasks
                _ = Task.Run(() => ReadStreamAsync(_activeProcess.StandardOutput, data => OutputReceived?.Invoke(data)));
                _ = Task.Run(() => ReadStreamAsync(_activeProcess.StandardError, data => ErrorReceived?.Invoke(data)));
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke($"Process execution failed: {ex.Message}" + Environment.NewLine);
            }

            await Task.Yield(); 
        }

        private async Task ReadStreamAsync(StreamReader reader, Action<string> onDataReceived)
        {
            char[] buffer = new char[1]; // Read character by character for immediate feedback
            while (!reader.EndOfStream)
            {
                int count = await reader.ReadAsync(buffer, 0, 1);
                if (count > 0)
                {
                    onDataReceived(new string(buffer, 0, count));
                }
            }
        }

        private void KillProcessByName(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try { process.Kill(); process.WaitForExit(1000); } catch { }
                }
            }
            catch { }
        }

        public async Task<(string Output, string Error)> RunAndCapturePythonAsync(string code)
        {
            string filePath = Path.Combine(_tempDir, "temp_val.py");
            await File.WriteAllTextAsync(filePath, code);
            var result = await ExecuteAndCaptureAsync("python", $"\"{filePath}\"");
            if (File.Exists(filePath)) File.Delete(filePath);
            return result;
        }

        public async Task<(string Output, string Error)> RunAndCaptureCSharpAsync(string code)
        {
            KillProcessByName("temp_val");
            string projectDir = Path.Combine(_tempDir, "CSharpValProject");
            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
                string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
                await File.WriteAllTextAsync(Path.Combine(projectDir, "val.csproj"), csproj);
            }
            string programPath = Path.Combine(projectDir, "Program.cs");
            await File.WriteAllTextAsync(programPath, code);
            var result = await ExecuteAndCaptureAsync("dotnet", "run", projectDir);
            return result;
        }

        private async Task<(string Output, string Error)> ExecuteAndCaptureAsync(string command, string arguments, string workingDir = null!)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir ?? _tempDir
            };
            using var proc = new Process { StartInfo = startInfo };
            proc.Start();

            var outTask = proc.StandardOutput.ReadToEndAsync();
            var errTask = proc.StandardError.ReadToEndAsync();
            
            var waitTask = proc.WaitForExitAsync();
            var timeoutTask = Task.Delay(10000); // 10 seconds timeout

            if (await Task.WhenAny(waitTask, timeoutTask) == timeoutTask)
            {
                try { proc.Kill(true); } catch { }
                return ("", "Validation timed out after 10 seconds.");
            }

            return (await outTask, await errTask);
        }

        private void CleanupTempFiles(string command, string arguments, string? workingDir)
        {
            try
            {
                if (command == "python")
                {
                    string filePath = arguments.Trim('"');
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
                else if (command == "dotnet" && !string.IsNullOrEmpty(workingDir))
                {
                    string programPath = Path.Combine(workingDir, "Program.cs");
                    if (File.Exists(programPath)) File.Delete(programPath);
                }
            }
            catch { }
        }
    }
}
