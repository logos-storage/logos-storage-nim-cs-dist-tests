using ArgsUniform;
using StoragePlugin;
using Newtonsoft.Json;

namespace ContinuousTests
{
    public class Configuration
    {
        [Uniform("log-path", "l", "LOGPATH", true, "Path where log files will be written.")]
        public string LogPath { get; set; } = "logs";

        [Uniform("data-path", "d", "DATAPATH", true, "Path where temporary data files will be written.")]
        public string DataPath { get; set; } = "data";

        [Uniform("storage-deployment", "c", "CODEXDEPLOYMENT", true, "Path to storage-deployment JSON file.")]
        public string LogosStorageDeploymentJson { get; set; } = string.Empty;

        [Uniform("keep", "k", "KEEP", false, "Set to 1 or 'true' to retain logs of successful tests.")]
        public bool KeepPassedTestLogs { get; set; } = false;

        [Uniform("kube-config", "kc", "KUBECONFIG", true, "Path to Kubeconfig file. Use 'null' (default) to use local cluster.")]
        public string KubeConfigFile { get; set; } = "null";

        [Uniform("stop", "s", "STOPONFAIL", false, "If greater than zero, runner will stop after this many test failures and download all cluster container logs. 0 by default.")]
        public int StopOnFailure { get; set; } = 0;

        [Uniform("target-duration", "td", "TARGETDURATION", false, "If set, runner will run for this length of time before stopping. Supports seconds, or '1d2h3m4s' format.")]
        public string TargetDurationSeconds { get; set; } = string.Empty;

        [Uniform("filter", "f", "FILTER", false, "If set, runs only tests whose names contain any of the filter strings. Comma-separated. Case sensitive.")]
        public string Filter { get; set; } = string.Empty;

        [Uniform("cleanup", "cl", "CLEANUP", false, "If set to 1 or 'true', the kubernetes namespace will be deleted after the test run has finished.")]
        public bool Cleanup { get; set; } = false;

        [Uniform("full-container-logs", "fcl", "FULLCONTAINERLOGS", false, "If set to 1 or 'true', container logs downloaded on test failure will download from" +
            " the timestamp of the start of the network deployment. Otherwise, logs will start from the test start timestamp.")]
        public bool FullContainerLogs { get; set; } = false;

        public LogosStorageDeployment LogosStorageDeployment { get; set; } = null!;
    }

    public class ConfigLoader
    {
        public Configuration Load(string[] args)
        {
            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);

            var result = uniformArgs.Parse(true);
            result.LogosStorageDeployment = ParseLogosStorageDeploymentJson(result.LogosStorageDeploymentJson);
            return result;
        }
        
        private LogosStorageDeployment ParseLogosStorageDeploymentJson(string filename)
        {
            var d = JsonConvert.DeserializeObject<LogosStorageDeployment>(File.ReadAllText(filename))!;
            if (d == null) throw new Exception("Unable to parse " + filename);
            return d;
        }

        private static void PrintHelp()
        {
            var nl = Environment.NewLine;
            Console.WriteLine("ContinuousTests will run a set of tests against a logos-storage deployment given a storage-deployment.json file." + nl +
                "The tests will run in an endless loop unless otherwise specified." + nl);
        }
    }
}
