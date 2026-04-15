using LogosStorageClient;
using KubernetesWorkflow;
using Utils;

namespace StoragePlugin
{
    public class LogosStorageStartupConfig
    {
        public string? NameOverride { get; set; }
        public ILocation Location { get; set; } = KnownLocations.UnspecifiedLocation;
        public LogosStorageLogLevel LogLevel { get; set; }
        public LogosStorageLogCustomTopics? CustomTopics { get; set; } = new LogosStorageLogCustomTopics(LogosStorageLogLevel.Info, LogosStorageLogLevel.Warn);
        public ByteSize? StorageQuota { get; set; }
        public bool MetricsEnabled { get; set; }
        public string? BootstrapSpr { get; set; }
        public int? BlockTTL { get; set; }
        public bool? EnableValidator { get; set; }
        public TimeSpan? BlockMaintenanceInterval { get; set; }
        public int? BlockMaintenanceNumber { get; set; }
        public LogosStorageTestNetConfig? PublicTestNet { get; set; }

        public string LogLevelWithTopics()
        {
            var level = LogLevel.ToString()!.ToUpperInvariant();
            if (CustomTopics != null)
            {
                var discV5Topics = new[]
                {
                    "discv5",
                    "providers",
                    "routingtable",
                    "manager",
                    "cache",
                };
                var libp2pTopics = new[]
                {
                    "libp2p",
                    "multistream",
                    "switch",
                    "transport",
                    "tcptransport",
                    "semaphore",
                    "asyncstreamwrapper",
                    "lpstream",
                    "mplex",
                    "mplexchannel",
                    "noise",
                    "bufferstream",
                    "mplexcoder",
                    "secure",
                    "chronosstream",
                    "connection",
                    // Removed: "connmanager", is used for transcript peer-dropped event.
                    "websock",
                    "ws-session",
                    // Removed: "dialer", is used for transcript successful-dial event.
                    "muxedupgrade",
                    "upgrade",
                    "identify"
                };
                var blockExchangeTopics = new[]
                {
                    "codex",
                    "pendingblocks",
                    "peerctxstore",
                    "discoveryengine",
                    "blockexcengine",
                    "blockexcnetwork",
                    "blockexcnetworkpeer"
                };
                var jsonSerializeTopics = new[]
                {
                    "serde",
                    "json",
                    "serialization"
                };

                var alwaysIgnoreTopics = new []
                {
                    "JSONRPC-CLIENT"
                };

                level = $"{level};" +
                    $"{CustomTopics.DiscV5.ToString()!.ToLowerInvariant()}:{string.Join(",", discV5Topics)};" +
                    $"{CustomTopics.Libp2p.ToString()!.ToLowerInvariant()}:{string.Join(",", libp2pTopics)};" +
                    $"{CustomTopics.JsonSerialize.ToString().ToLowerInvariant()}:{string.Join(",", jsonSerializeTopics)};" +
                    $"{LogosStorageLogLevel.Error.ToString()}:{string.Join(",", alwaysIgnoreTopics)}";

                if (CustomTopics.BlockExchange != null)
                {
                    level += $";{CustomTopics.BlockExchange.ToString()!.ToLowerInvariant()}:{string.Join(",", blockExchangeTopics)}";
                }
            }
            return level;
        }
    }

    public class LogosStorageTestNetConfig
    {
        public int PublicDiscoveryPort { get; set; }
        public int PublicListenPort { get; set; }
    }
}
