using StoragePlugin;
using Logging;
using System.Diagnostics;

namespace StoragePlugin
{
    public class LocalNodeBuilder
    {
        private readonly ILog log;
        private readonly string? repoPath;
        private readonly string? dockerUsername;

        public LocalNodeBuilder(ILog log, string? repoPath, string? dockerUsername)
        {
            this.log = new LogPrefixer(log, "(LocalNodeBuilder) ");
            this.repoPath = repoPath;
            this.dockerUsername = dockerUsername;
        }

        public LocalNodeBuilder(ILog log, string? repoPath)
            : this(log, repoPath, Environment.GetEnvironmentVariable("DOCKERUSERNAME"))
        {
        }

        public LocalNodeBuilder(ILog log)
            : this(log, Environment.GetEnvironmentVariable("STORAGEREPOPATH"))
        {
        }

        public void Intialize()
        {
            if (!IsEnabled()) return;

            if (string.IsNullOrEmpty(dockerUsername)) throw new Exception("Docker username required. (Pass to constructor or set 'DOCKERUSERNAME' environment variable.)");
            if (string.IsNullOrEmpty(repoPath)) throw new Exception("Logos Storage repo path required. (Pass to constructor or set 'CODEXREPOPATH' environment variable.)");
            if (!Directory.Exists(repoPath)) throw new Exception($"Path '{repoPath}' does not exist.");
            var files = Directory.GetFiles(repoPath);
            if (!files.Any(f => f.ToLowerInvariant().EndsWith("codex.nim"))) throw new Exception($"Path '{repoPath}' does not appear to be the Codex repo root.");

            Log($"Logos Storage docker image will be built in path '{repoPath}'.");
            Log("Please note this can take several minutes. If you're not trying to use a Logos Storage image with local code changes,");
            Log("Consider using the default test image or consider setting the 'STORAGEDOCKERIMAGE' environment variable to use an already built image.");
            LogosStorageDockerImage.Override = $"Using docker image locally built in path '{repoPath}'.";
        }

        public void Build()
        {
            if (!IsEnabled()) return;
            Log("Docker login...");
            DockerLogin();

            Log($"Logged in. Building Logos Storage image in path '{repoPath}'...");

            var customImage = GenerateImageName();
            Docker($"build", "-t", customImage, "-f", "./codex.Dockerfile",
                "--build-arg=\"MAKE_PARALLEL=4\"",
                "--build-arg=\"NIMFLAGS=-d:disableMarchNative -d:storage_enable_api_debug_peers=true -d:storage_enable_api_debug_fetch=true\"",
                "--build-arg=\"NAT_IP_AUTO=true\"",
                "..");

            Log($"Image '{customImage}' built successfully. Pushing...");

            Docker("push", customImage);

            LogosStorageDockerImage.Override = customImage;
            Log("Image pushed. Good to go!");
        }

        private void DockerLogin()
        {
            var dockerPassword = Environment.GetEnvironmentVariable("DOCKERPASSWORD");

            try
            {
                if (string.IsNullOrEmpty(dockerUsername) || string.IsNullOrEmpty(dockerPassword))
                {
                    Log("Environment variable 'DOCKERPASSWORD' not provided.");
                    Log("Trying system default...");
                    Docker("login");
                }
                else
                {
                    Docker("login", "-u", dockerUsername, "-p", dockerPassword);
                }
            }
            catch
            {
                Log("Docker login failed.");
                Log("Please check the docker username and password provided by the constructor arguments and/or");
                Log("set by 'DOCKERUSERNAME' and 'DOCKERPASSWORD' environment variables.");
                Log("Note: You can use a docker access token as DOCKERPASSWORD.");
                throw;
            }
        }

        private string GenerateImageName()
        {
            var tag = Environment.GetEnvironmentVariable("DOCKERTAG");
            if (string.IsNullOrEmpty(tag)) return $"{dockerUsername!}/logos-storage-nim-autoimage:{Guid.NewGuid().ToString().ToLowerInvariant()}";
            return $"{dockerUsername}/logos-storage-nim-autoimage:{tag}";
        }

        private void Docker(params string[] args)
        {
            var dockerPath = Path.Combine(repoPath!, "docker");

            var startInfo = new ProcessStartInfo()
            {
                FileName = "docker",
                Arguments = string.Join(" ", args),
                WorkingDirectory = dockerPath,
            };
            var process = Process.Start(startInfo);
            if (process == null) throw new Exception("Failed to start docker process.");
            if (!process.WaitForExit(TimeSpan.FromMinutes(10))) throw new Exception("Docker processed timed out after 10 minutes.");
            if (process.ExitCode != 0) throw new Exception("Docker process exited with error.");
        }

        private bool IsEnabled()
        {
            return !string.IsNullOrEmpty(repoPath);
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
