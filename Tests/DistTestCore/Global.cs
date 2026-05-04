using System.Diagnostics;
using System.Reflection;
using Core;
using Logging;

namespace DistTestCore
{
    public class Global
    {
        public const string TestNamespacePrefix = "storage-";
        public static readonly string TestResultsFile = Path.Combine(Path.GetTempPath(), "test-results.jsonl");
        public Configuration Configuration { get; } = new Configuration();

        public Assembly[] TestAssemblies { get; }
        private readonly EntryPoint globalEntryPoint;
        private readonly ILog log;

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

        public void Setup()
        {
            // At process exit, write accumulated test-result JSON lines to stdout.
            // ProcessExit fires after NUnit is completely done (no more output capture),
            // so writes here go directly to the real stdout pipe and appear in Cloud Logging.
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                if (!File.Exists(TestResultsFile)) return;
                var raw = new StreamWriter(Console.OpenStandardOutput(), leaveOpen: true) { AutoFlush = true };
                raw.Write(File.ReadAllText(TestResultsFile));
            };

            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

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
                // Write directly to raw stderr so this is visible even when NUnit
                // captures Console.Out/Console.Error for the fixture setup context.
                using var err = new StreamWriter(Console.OpenStandardError(), leaveOpen: true) { AutoFlush = true };
                err.WriteLine($"[global-setup-failure] {ex}");
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
