using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using WebApi.Models;

namespace WebApi.Services
{
    public class PlaygroundService
    {
        private readonly PlaygroundOptions options;
        private readonly SemaphoreSlim gate;
        private readonly string workRoot;

        public PlaygroundService(IOptions<PlaygroundOptions> options)
        {
            this.options = options.Value;
            this.gate = new SemaphoreSlim(Math.Max(1, this.options.MaxConcurrency));
            this.workRoot = ResolveWorkRoot(this.options.WorkRoot);
        }

        private static string ResolveWorkRoot(string configured)
        {
            if (string.IsNullOrWhiteSpace(configured))
            {
                return Path.Combine(Path.GetTempPath(), "rux-playground");
            }
            if (configured.StartsWith('~'))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                configured = home + configured[1..];
            }
            return configured;
        }

        public async Task<RunResult> RunAsync(string code, CancellationToken cancellationToken)
        {
            var result = await ExecuteAsync(code, "run", cancellationToken);
            return new RunResult
            {
                Success = result.Success,
                Stdout = result.Stdout,
                Stderr = result.Stderr,
                Error = result.Error,
                DurationMs = result.DurationMs
            };
        }

        public async Task<AsmResult> DumpAsmAsync(string code, CancellationToken cancellationToken)
        {
            var result = await ExecuteAsync(code, "asm", cancellationToken);
            // On success the assembly listing arrives on stdout. On failure the
            // compiler diagnostics are in stderr, so leave the assembly empty.
            var assembly = result.Success ? result.Stdout : "";
            var lines = result.Success ? CountLines(assembly) : 0;
            return new AsmResult
            {
                Success = result.Success,
                AsmUser = assembly,
                AsmFull = assembly,
                UserLines = lines,
                TotalLines = lines,
                Error = result.Error,
                Stdout = result.Success ? "" : result.Stdout,
                Stderr = result.Stderr,
                DurationMs = result.DurationMs
            };
        }

        private async Task<ExecutionResult> ExecuteAsync(
            string code, string mode, CancellationToken cancellationToken)
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var workDir = Path.Combine(workRoot, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(workDir);
                try
                {
                    await File.WriteAllTextAsync(
                        Path.Combine(workDir, "Main.rux"), code, cancellationToken);
                    return await RunContainerAsync(workDir, mode, cancellationToken);
                }
                finally
                {
                    try
                    {
                        Directory.Delete(workDir, true);
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task<ExecutionResult> RunContainerAsync(
            string workDir, string mode, CancellationToken cancellationToken)
        {
            var containerName = $"rux-playground-{Guid.NewGuid():N}";
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var memory = options.MemoryLimit;
            var arguments = new[]
            {
                "run", "--rm",
                "--name", containerName,
                "--network", "none",
                "--read-only",
                "--tmpfs", "/tmp:rw,exec,size=64m",
                "--memory", memory,
                "--memory-swap", memory,
                "--cpus", options.Cpus.ToString(CultureInfo.InvariantCulture),
                "--pids-limit", options.PidsLimit.ToString(CultureInfo.InvariantCulture),
                "--cap-drop", "ALL",
                "--security-opt", "no-new-privileges",
                "--ulimit", "fsize=8388608",
                "--ulimit", "nofile=256",
                "-v", $"{workDir}:/playground:ro",
                options.Image,
                mode
            };
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }
            var stopwatch = Stopwatch.StartNew();
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start the docker process.");
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
            var timedOut = false;
            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                await KillContainerAsync(containerName);
                KillProcess(process);
                await process.WaitForExitAsync(CancellationToken.None);
                cancellationToken.ThrowIfCancellationRequested();
                timedOut = true;
            }
            stopwatch.Stop();
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            if (timedOut)
            {
                return new ExecutionResult
                {
                    Success = false,
                    Stdout = stdout,
                    Stderr = "",
                    Error = $"Execution timed out after {options.TimeoutSeconds} seconds.",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }
            return new ExecutionResult
            {
                Success = process.ExitCode == 0,
                Stdout = stdout,
                Stderr = stderr,
                Error = null,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }
            var count = 1;
            foreach (var ch in text)
            {
                if (ch == '\n')
                {
                    count++;
                }
            }
            // A trailing newline does not start a new line.
            return text.EndsWith('\n') ? count - 1 : count;
        }

        private static void KillProcess(Process process)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static async Task KillContainerAsync(string containerName)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("kill");
            startInfo.ArgumentList.Add(containerName);
            using var process = Process.Start(startInfo);
            if (process is not null)
            {
                await process.WaitForExitAsync(CancellationToken.None);
            }
        }

        private sealed class ExecutionResult
        {
            public bool Success { get; init; }

            public required string Stdout { get; init; }

            public required string Stderr { get; init; }

            public string? Error { get; init; }

            public long DurationMs { get; init; }
        }
    }
}
