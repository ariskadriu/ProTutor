using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ProgrammingTutor.Core
{
    public class CodeRunner
    {
        private readonly string _tempDir;

        public CodeRunner()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ProgrammingTutor_Temp");
            if (!Directory.Exists(_tempDir))
            {
                Directory.CreateDirectory(_tempDir);
            }
        }

        public async Task<(string Output, string Error)> RunPythonAsync(string code)
        {
            string filePath = Path.Combine(_tempDir, "temp.py");
            await File.WriteAllTextAsync(filePath, code);

            return await ExecuteProcessAsync("python", $"\"{filePath}\"");
        }

        public async Task<(string Output, string Error)> RunCSharpAsync(string code)
        {
            // Kill any existing 'temp' processes to avoid file locking issues
            KillProcessByName("temp");

            string projectDir = Path.Combine(_tempDir, "CSharpLocalProject");
            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
                // Create minimal csproj if it doesn't exist
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

            return await ExecuteProcessAsync("dotnet", "run", projectDir);
        }

        private void KillProcessByName(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(1000);
                    }
                    catch (Exception)
                    {
                        // Ignore errors when killing processes
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors finding processes
            }
        }

        private async Task<(string Output, string Error)> ExecuteProcessAsync(string command, string arguments, string workingDir = null!)
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

            using var process = new Process { StartInfo = startInfo };
            
            try
            {
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                var waitForExitTask = process.WaitForExitAsync();

                var completedTask = await Task.WhenAny(waitForExitTask, Task.Delay(10000));

                if (completedTask == waitForExitTask)
                {
                    string output = await outputTask;
                    string error = await errorTask;
                    CleanupTempFiles(command, arguments, workingDir);
                    return (output, error);
                }
                else
                {
                    try { process.Kill(true); } catch { }
                    CleanupTempFiles(command, arguments, workingDir);
                    return ("", "Process execution timed out after 10 seconds. Possible infinite loop or waiting for input.");
                }
            }
            catch (Exception ex)
            {
                return ("", $"Process execution failed: {ex.Message}");
            }
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
            catch { /* Ignore cleanup errors */ }
        }
    }
}
