using System.Diagnostics;
using System.Reflection;
using Core;
using k8s;
using k8s.Models;
using Logging;

namespace DistTestCore
{
    public class Global
    {
        public const string TestNamespacePrefix = "storage-";
        public Configuration Configuration { get; } = new Configuration();

        public Assembly[] TestAssemblies { get; }
        private readonly EntryPoint globalEntryPoint;
        private readonly ILog log;

        private static readonly IKubernetes? k8sClient = CreateK8sClient();

        private static IKubernetes? CreateK8sClient()
        {
            var kubeconfig = Environment.GetEnvironmentVariable("KUBECONFIG");
            if (string.IsNullOrEmpty(kubeconfig)) return null;
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeconfig);
            return new Kubernetes(config);
        }

        public Global()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            TestAssemblies = assemblies.Where(a => a.FullName!.ToLowerInvariant().Contains("test")).ToArray();

            log = new ConsoleLog();
            globalEntryPoint = new EntryPoint(
                log,
                Configuration.GetK8sConfiguration(
                    new DefaultK8sTimeSet(),
                    TestNamespacePrefix
                ),
                Configuration.GetFileManagerFolder()
            );
        }

        /// <summary>
        /// Write test result to ConfigMap so it can be read from the workflow
        /// </summary>
        /// <param name="runId">RunId</param>
        /// <param name="json">Json payload containing the test result</param>
        public static void WriteTestResult(string runId, string json)
        {
            if (k8sClient == null) return;
            try
            {
                var cm = new V1ConfigMap
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = $"test-result-{Guid.NewGuid():N}",
                        NamespaceProperty = "default",
                        Labels = new Dictionary<string, string>
                        {
                            ["runid"] = runId,
                            ["app"] = "test-result"
                        }
                    },
                    Data = new Dictionary<string, string> { ["result"] = json }
                };
                k8sClient.CreateNamespacedConfigMap(cm, "default");
            }
            catch { }
        }

        public void Setup()
        {
            try
            {
                Trace.Listeners.Add(new ConsoleTraceListener());

                Logging.Stopwatch.Measure(log, "Global setup", () =>
                {
                    globalEntryPoint.Announce();
                    globalEntryPoint.Tools.CreateWorkflow().DeleteNamespacesStartingWith(TestNamespacePrefix, wait: true);
                });
            }
            catch (Exception ex)
            {
                GlobalTestFailure.HasFailed = true;
                log.Error($"Global setup cleanup failed with: {ex}");
                throw;
            }
        }

        public void TearDown()
        {
            globalEntryPoint.Decommission(
                // There shouldn't be any of either, but clean everything up regardless.
                deleteKubernetesResources: true,
                deleteTrackedFiles: true,
                waitTillDone: true
            );

            Trace.Flush();
        }
    }
}
