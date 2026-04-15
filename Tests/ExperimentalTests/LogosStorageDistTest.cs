using LogosStorageClient;
using StoragePlugin;
using StoragePlugin.OverwatchSupport;
using LogosStorageTests.Helpers;
using Core;
using DistTestCore;
using DistTestCore.Logs;
using Logging;
using MetricsPlugin;
using Newtonsoft.Json;
using NUnit.Framework;
using OverwatchTranscript;
using Utils;

namespace LogosStorageTests
{
    public class LogosStorageDistTest : DistTest
    {
        private readonly List<IStorageNode> nodes = new List<IStorageNode>();
        private LogosStorageTranscriptWriter? writer;

        public LogosStorageDistTest()
        {
            ProjectPlugin.Load<StoragePlugin.StoragePlugin>();
            ProjectPlugin.Load<MetricsPlugin.MetricsPlugin>();
        }

        [SetUp]
        public void SetupLogosStorageDistTest()
        {
            writer = SetupTranscript();
        }

        [TearDown]
        public void TearDownLogosStorageDistTest()
        {
            TeardownTranscript();
        }

        protected override void Initialize(FixtureLog fixtureLog)
        {
            var localBuilder = new LocalNodeBuilder(fixtureLog);
            localBuilder.Intialize();
            localBuilder.Build();

            Ci.AddLogosStorageHooksProvider(new LogosStorageLogTrackerProvider(nodes.Add));
        }

        public IStorageNode StartLogosStorage()
        {
            return StartLogosStorage(s => { });
        }

        public IStorageNode StartLogosStorage(Action<ILogosStorageSetup> setup)
        {
            return StartLogosStorage(1, setup)[0];
        }

        public IStorageNodeGroup StartLogosStorage(int numberOfNodes)
        {
            return StartLogosStorage(numberOfNodes, s => { });
        }

        public IStorageNodeGroup StartLogosStorage(int numberOfNodes, Action<ILogosStorageSetup> setup)
        {
            var group = Ci.StartStorageNodes(numberOfNodes, s =>
            {
                setup(s);
                OnLogosStorageSetup(s);
            });

            return group;
        }

        public PeerConnectionTestHelpers CreatePeerConnectionTestHelpers()
        {
            return new PeerConnectionTestHelpers(GetTestLog());
        }

        public PeerDownloadTestHelpers CreatePeerDownloadTestHelpers()
        {
            return new PeerDownloadTestHelpers(GetTestLog(), GetFileManager());
        }

        public void CheckLogForErrors(params IStorageNode[] nodes)
        {
            foreach (var node in nodes) CheckLogForErrors(node);
        }

        public void CheckLogForErrors(IStorageNode node)
        {
            Log($"Checking {node.GetName()} log for errors.");
            var log = node.DownloadLog();

            log.AssertLogDoesNotContain("Block validation failed");
            log.AssertLogDoesNotContainLinesStartingWith("ERR ");
        }

        public void LogNodeStatus(IStorageNode node, IMetricsAccess? metrics = null)
        {
            Log("Status for " + node.GetName() + Environment.NewLine +
                GetBasicNodeStatus(node));
        }

        public void WaitAndCheckNodesStaysAlive(TimeSpan duration, IStorageNodeGroup nodes)
        {
            WaitAndCheckNodesStaysAlive(duration, nodes.ToArray());
        }

        public void WaitAndCheckNodesStaysAlive(TimeSpan duration, List<IStorageNode> nodes)
        {
            WaitAndCheckNodesStaysAlive(duration, nodes.ToArray());
        }

        public void WaitAndCheckNodesStaysAlive(TimeSpan duration, params IStorageNode[] nodes)
        {
            Log($"{nameof(WaitAndCheckNodesStaysAlive)} {Time.FormatDuration(duration)}...");

            var timeout = TimeSpan.FromSeconds(3.0);
            Assert.That(duration.TotalSeconds, Is.GreaterThan(timeout.TotalSeconds));

            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < duration)
            {
                Thread.Sleep(timeout);
                foreach (var node in nodes)
                {
                    Assert.That(node.HasCrashed(), Is.False);

                    var info = node.GetDebugInfo();
                    Assert.That(!string.IsNullOrEmpty(info.Id));
                }
            }

            Log($"{nameof(WaitAndCheckNodesStaysAlive)} OK");
        }

        public void AssertNodesContainFile(ContentId cid, IStorageNodeGroup nodes)
        {
            AssertNodesContainFile(cid, nodes.ToArray());
        }

        public void AssertNodesContainFile(ContentId cid, params IStorageNode[] nodes)
        {
            Log($"{nameof(AssertNodesContainFile)} {nodes.Names()} {cid}...");

            foreach (var node in nodes)
            {
                var localDatasets = node.LocalFiles();
                CollectionAssert.Contains(localDatasets.Content.Select(c => c.Cid), cid);
            }

            Log($"{nameof(AssertNodesContainFile)} OK");
        }

        private string GetBasicNodeStatus(IStorageNode node)
        {
            return JsonConvert.SerializeObject(node.GetDebugInfo(), Formatting.Indented) + Environment.NewLine +
                node.Space().ToString() + Environment.NewLine;
        }

        protected virtual void OnLogosStorageSetup(ILogosStorageSetup setup)
        {
        }

        private CreateTranscriptAttribute? GetTranscriptAttributeOfCurrentTest()
        {
            var attrs = GetCurrentTestMethodAttribute<CreateTranscriptAttribute>();
            if (attrs.Any()) return attrs.Single();
            return null;
        }

        private LogosStorageTranscriptWriter? SetupTranscript()
        {
            var attr = GetTranscriptAttributeOfCurrentTest();
            if (attr == null) return null;

            var config = new LogosStorageTranscriptWriterConfig(
                attr.OutputFilename,
                attr.IncludeBlockReceivedEvents
            );

            var log = new LogPrefixer(GetTestLog(), "(Transcript) ");
            var writer = new LogosStorageTranscriptWriter(log, config, Transcript.NewWriter(log));
            Ci.AddLogosStorageHooksProvider(writer);
            return writer;
        }

        private void TeardownTranscript()
        {
            if (writer == null) return;

            var result = GetTestResult();
            var log = GetTestLog();
            writer.AddResult(result.Success, result.Result);
            try
            {
                Stopwatch.Measure(log, "Transcript.ProcessLogs", () =>
                {
                    writer.ProcessLogs(DownloadAllLogs());
                });

                Stopwatch.Measure(log, $"Transcript.FinalizeWriter", () =>
                {
                    writer.IncludeFile(log.GetFullName() + ".log");
                    writer.FinalizeWriter();
                });
            }
            catch (Exception ex)
            {
                log.Error("Failure during transcript teardown: " + ex);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CreateTranscriptAttribute : PropertyAttribute
    {
        public CreateTranscriptAttribute(string outputFilename, bool includeBlockReceivedEvents = true)
        {
            OutputFilename = outputFilename;
            IncludeBlockReceivedEvents = includeBlockReceivedEvents;
        }

        public string OutputFilename { get; }
        public bool IncludeBlockReceivedEvents { get; }
    }
}
