using LogosStorageClient;
using KubernetesWorkflow;
using Utils;

namespace StoragePlugin
{
    public interface ILogosStorageSetup
    {
        ILogosStorageSetup WithName(string name);
        ILogosStorageSetup At(ILocation location);
        ILogosStorageSetup WithBootstrapNode(IStorageNode node);
        ILogosStorageSetup WithLogLevel(LogosStorageLogLevel level);
        ILogosStorageSetup WithLogLevel(LogosStorageLogLevel level, LogosStorageLogCustomTopics customTopics);
        ILogosStorageSetup WithStorageQuota(ByteSize storageQuota);
        ILogosStorageSetup WithBlockTTL(TimeSpan duration);
        ILogosStorageSetup WithBlockMaintenanceInterval(TimeSpan duration);
        ILogosStorageSetup WithBlockMaintenanceNumber(int numberOfBlocks);
        ILogosStorageSetup EnableMetrics();
        ILogosStorageSetup AsPublicTestNet(LogosStorageTestNetConfig testNetConfig);
    }

    public class LogosStorageLogCustomTopics
    {
        public LogosStorageLogCustomTopics(LogosStorageLogLevel discV5, LogosStorageLogLevel libp2p, LogosStorageLogLevel blockExchange)
        {
            DiscV5 = discV5;
            Libp2p = libp2p;
            BlockExchange = blockExchange;
        }

        public LogosStorageLogCustomTopics(LogosStorageLogLevel discV5, LogosStorageLogLevel libp2p)
        {
            DiscV5 = discV5;
            Libp2p = libp2p;
        }

        public LogosStorageLogLevel DiscV5 { get; set; }
        public LogosStorageLogLevel Libp2p { get; set; }
        public LogosStorageLogLevel ContractClock { get; set; } = LogosStorageLogLevel.Warn;
        public LogosStorageLogLevel? BlockExchange { get; }
        public LogosStorageLogLevel JsonSerialize { get; set; } = LogosStorageLogLevel.Warn;
        public LogosStorageLogLevel MarketplaceInfra { get; set; } = LogosStorageLogLevel.Warn;
    }

    public class LogosStorageSetup : LogosStorageStartupConfig, ILogosStorageSetup
    {
        public int NumberOfNodes { get; }

        public LogosStorageSetup(int numberOfNodes)
        {
            NumberOfNodes = numberOfNodes;
        }

        public ILogosStorageSetup WithName(string name)
        {
            NameOverride = name;
            return this;
        }

        public ILogosStorageSetup At(ILocation location)
        {
            Location = location;
            return this;
        }

        public ILogosStorageSetup WithBootstrapNode(IStorageNode node)
        {
            BootstrapSpr = node.GetDebugInfo().Spr;
            return this;
        }

        public ILogosStorageSetup WithLogLevel(LogosStorageLogLevel level)
        {
            LogLevel = level;
            return this;
        }

        public ILogosStorageSetup WithLogLevel(LogosStorageLogLevel level, LogosStorageLogCustomTopics customTopics)
        {
            LogLevel = level;
            CustomTopics = customTopics;
            return this;
        }

        public ILogosStorageSetup WithStorageQuota(ByteSize storageQuota)
        {
            StorageQuota = storageQuota;
            return this;
        }

        public ILogosStorageSetup WithBlockTTL(TimeSpan duration)
        {
            BlockTTL = Convert.ToInt32(duration.TotalSeconds);
            return this;
        }

        public ILogosStorageSetup WithBlockMaintenanceInterval(TimeSpan duration)
        {
            BlockMaintenanceInterval = duration;
            return this;
        }

        public ILogosStorageSetup WithBlockMaintenanceNumber(int numberOfBlocks)
        {
            BlockMaintenanceNumber = numberOfBlocks;
            return this;
        }

        public ILogosStorageSetup EnableMetrics()
        {
            MetricsEnabled = true;
            return this;
        }

        public ILogosStorageSetup AsPublicTestNet(LogosStorageTestNetConfig testNetConfig)
        {
            PublicTestNet = testNetConfig;
            return this;
        }

        public string Describe()
        {
            var args = string.Join(',', DescribeArgs());
            return $"({NumberOfNodes} Logos Storage Nodes with args:[{args}])";
        }

        private IEnumerable<string> DescribeArgs()
        {
            if (PublicTestNet != null) yield return $"<!>Public TestNet with listenPort: {PublicTestNet.PublicListenPort}<!>";
            yield return $"LogLevel={LogLevelWithTopics()}";
            if (BootstrapSpr != null) yield return $"BootstrapNode={BootstrapSpr}";
            if (StorageQuota != null) yield return $"StorageQuota={StorageQuota}";
        }
    }
}
