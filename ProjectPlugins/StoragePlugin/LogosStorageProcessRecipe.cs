using System.Net.Sockets;
using System.Net;

namespace StoragePlugin
{
    public class ProcessRecipe
    {
        public ProcessRecipe(string cmd, string[] args)
        {
            Cmd = cmd;
            Args = args;
        }

        public string Cmd { get; }
        public string[] Args { get; }
    }

    public class LogosStorageProcessConfig
    {
        public LogosStorageProcessConfig(string name, FreePortFinder freePortFinder, string dataDir)
        {
            ApiPort = freePortFinder.GetNextFreePort();
            DiscPort = freePortFinder.GetNextFreePort();
            ListenPort = freePortFinder.GetNextFreePort();
            Name = name;
            DataDir = dataDir;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var addrs = host.AddressList.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToList();

            LocalIpAddrs = addrs.First();
        }

        public int ApiPort { get; }
        public int DiscPort { get; }
        public int ListenPort { get; }
        public string Name { get; }
        public string DataDir { get; }
        public IPAddress LocalIpAddrs { get; }
    }

    public class LogosStorageProcessRecipe
    {
        private readonly LogosStorageProcessConfig pc;
        private readonly LogosStorageExePath logosStorageExePath;

        public LogosStorageProcessRecipe(LogosStorageProcessConfig pc, LogosStorageExePath logosStorageExePath)
        {
            this.pc = pc;
            this.logosStorageExePath = logosStorageExePath;
        }

        public ProcessRecipe Initialize(LogosStorageStartupConfig config)
        {
            args.Clear();
            
            AddArg("--api-port", pc.ApiPort);
            AddArg("--api-bindaddr", "0.0.0.0");

            AddArg("--data-dir", pc.DataDir);

            AddArg("--disc-port", pc.DiscPort);
            AddArg("--log-level", config.LogLevelWithTopics());

            // This makes the node announce itself to its local IP address.
            AddArg("--nat", $"extip:{pc.LocalIpAddrs.ToString()}");
            
            AddArg("--listen-addrs", $"/ip4/0.0.0.0/tcp/{pc.ListenPort}");

            if (!string.IsNullOrEmpty(config.BootstrapSpr))
            {
                AddArg("--bootstrap-node", config.BootstrapSpr);
            }
            if (config.StorageQuota != null)
            {
                AddArg("--storage-quota", config.StorageQuota.SizeInBytes.ToString()!);
            }
            if (config.BlockTTL != null)
            {
                AddArg("--block-ttl", config.BlockTTL.ToString()!);
            }
            if (config.BlockMaintenanceInterval != null)
            {
                AddArg("--block-mi", Convert.ToInt32(config.BlockMaintenanceInterval.Value.TotalSeconds).ToString());
            }
            if (config.BlockMaintenanceNumber != null)
            {
                AddArg("--block-mn", config.BlockMaintenanceNumber.ToString()!);
            }
            if (config.MetricsEnabled)
            {
                throw new Exception("Not supported");
                //var metricsPort = CreateApiPort(config, MetricsPortTag);
                //AddEnvVar("CODEX_METRICS", "true");
                //AddEnvVar("CODEX_METRICS_ADDRESS", "0.0.0.0");
                //AddEnvVar("CODEX_METRICS_PORT", metricsPort);
                //AddPodAnnotation("prometheus.io/scrape", "true");
                //AddPodAnnotation("prometheus.io/port", metricsPort.Number.ToString());
            }


            return Create();
        }

        private ProcessRecipe Create()
        {
            return new ProcessRecipe(
                cmd: logosStorageExePath.Get(),
                args: args.ToArray());
        }

        private readonly List<string> args = new List<string>();

        private void AddArg(string arg, string val)
        {
            args.Add($"{arg}={val}");
        }

        private void AddArg(string arg, int val)
        {
            args.Add($"{arg}={val}");
        }
    }
}
