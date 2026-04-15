using Core;
using KubernetesWorkflow.Types;
using Logging;
using System.Security.Cryptography;
using System.Text;
using Utils;

namespace StoragePlugin
{
    public class ApiChecker
    {
        // <INSERT-OPENAPI-YAML-HASH>
        private const string OpenApiYamlHash = "47-B8-22-44-3B-51-EB-82-CC-C1-DD-56-F0-7E-EC-7F-CD-E0-8F-5E-F6-3B-40-E0-02-E6-71-DB-B8-03-65-18";
        private const string OpenApiFilePath = "/logosstorage/openapi.yaml";
        private const string DisableEnvironmentVariable = "StoragePlugin_DISABLE_APICHECK";

        private const bool Disable = false;

        private const string Warning =
            "Warning: StoragePlugin was unable to find the openapi.yaml file in the Logos Storage container. Are you running an old version of Logos Storage? " +
            "Plugin will continue as normal, but API compatibility is not guaranteed!";

        private const string Failure =
            "Logos Storage API compatibility check failed! " +
            "openapi.yaml used by StoragePlugin does not match openapi.yaml in Logos Storage container. The openapi.yaml in " +
            "'ProjectPlugins/StoragePlugin' has been overwritten with the container one. " +
            "Please and rebuild this project. If you wish to disable API compatibility checking, please set " +
            $"the environment variable '{DisableEnvironmentVariable}' or set the disable bool in 'ProjectPlugins/StoragePlugin/ApiChecker.cs'.";

        private static bool checkPassed = false;

        private readonly IPluginTools pluginTools;
        private readonly ILog log;

        public ApiChecker(IPluginTools pluginTools)
        {
            this.pluginTools = pluginTools;
            log = pluginTools.GetLog();

            if (string.IsNullOrEmpty(OpenApiYamlHash)) throw new Exception("OpenAPI yaml hash was not inserted by pre-build trigger.");
        }

        public void CheckCompatibility(RunningPod[] containers)
        {
            if (checkPassed) return;

            Log("StoragePlugin is checking API compatibility...");

            if (Disable || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DisableEnvironmentVariable)))
            {
                Log("API compatibility checking has been disabled.");
                checkPassed = true;
                return;
            }

            var workflow = pluginTools.CreateWorkflow();
            var container = containers.First().Containers.First();
            var containerApi = workflow.ExecuteCommand(container, "cat", OpenApiFilePath);

            if (string.IsNullOrEmpty(containerApi))
            {
                log.Error(Warning);

                checkPassed = true;
                return;
            }

            var containerHash = Hash(containerApi);
            if (containerHash == OpenApiYamlHash)
            {
                Log("API compatibility check passed.");
                checkPassed = true;
                return;
            }

            OverwriteOpenApiYaml(containerApi);

            log.Error(Failure);
            throw new Exception(Failure);
        }

        private void OverwriteOpenApiYaml(string containerApi)
        {
            Log("API compatibility check failed. Updating StoragePlugin...");
            var openApiFilePath = Path.Combine(PluginPathUtils.ProjectPluginsDir, "LogosStorageClient", "openapi.yaml");
            if (!File.Exists(openApiFilePath)) throw new Exception("Unable to locate LogosStorageClient/openapi.yaml. Expected: " + openApiFilePath);

            File.Delete(openApiFilePath);
            File.WriteAllText(openApiFilePath, containerApi);
            Log("LogosStorageClient/openapi.yaml has been updated.");
        }

        private string Hash(string file)
        {
            var fileBytes = Encoding.ASCII.GetBytes(file
                .Replace(Environment.NewLine, ""));
            var sha = SHA256.Create();
            var hash = sha.ComputeHash(fileBytes);
            return BitConverter.ToString(hash);
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
