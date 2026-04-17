using LogosStorageClient;
using Core;
using Utils;
using System.Diagnostics;

namespace StoragePlugin
{
    public class BinaryLogosStorageStarter : ILogosStorageStarter
    {
        private readonly IPluginTools pluginTools;
        private readonly ProcessControlMap processControlMap;
        private readonly static NumberSource numberSource = new NumberSource(1);
        private readonly static FreePortFinder freePortFinder = new FreePortFinder();
        private readonly static object _lock = new object();
        private readonly static string dataParentDir = "storage_disttest_datadirs";
        private readonly static LogosStorageExePath logosStorageExePath = new LogosStorageExePath();

        static BinaryLogosStorageStarter()
        {
            StopAllLogosStorageProcesses();
            DeleteParentDataDir();
        }

        public BinaryLogosStorageStarter(IPluginTools pluginTools, ProcessControlMap processControlMap)
        {
            this.pluginTools = pluginTools;
            this.processControlMap = processControlMap;
        }

        public ILogosStorageInstance[] BringOnline(LogosStorageSetup logosStorageSetup)
        {
            lock (_lock)
            {
                LogSeparator();
                Log($"Starting {logosStorageSetup.Describe()}...");

                return StartLogosStorageBinaries(logosStorageSetup, logosStorageSetup.NumberOfNodes);
            }
        }

        public void Decommission()
        {
            lock (_lock)
            {
                processControlMap.StopAll();
            }
        }

        private ILogosStorageInstance[] StartLogosStorageBinaries(LogosStorageStartupConfig startupConfig, int numberOfNodes)
        {
            var result = new List<ILogosStorageInstance>();
            for (var i = 0; i < numberOfNodes; i++)
            {
                result.Add(StartBinary(startupConfig));
            }

            return result.ToArray();
        }

        private ILogosStorageInstance StartBinary(LogosStorageStartupConfig config)
        {
            var name = GetName(config);
            var dataDir = Path.Combine(dataParentDir, $"datadir_{numberSource.GetNextNumber()}");
            var pconfig = new LogosStorageProcessConfig(name, freePortFinder, dataDir);
            Log(pconfig);

            var factory = new LogosStorageProcessRecipe(pconfig, logosStorageExePath);
            var recipe = factory.Initialize(config);

            var startInfo = new ProcessStartInfo(
                fileName: recipe.Cmd,
                arguments: recipe.Args
            );
            //startInfo.UseShellExecute = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var process = Process.Start(startInfo);
            if (process == null || process.HasExited)
            {
                throw new Exception("Failed to start");
            }

            var local = "localhost";
            var instance = new LogosStorageInstance(
                name: name,
                imageName: "binary",
                startUtc: DateTime.UtcNow,
                discoveryEndpoint: new Address("Disc", pconfig.LocalIpAddrs.ToString(), pconfig.DiscPort),
                apiEndpoint: new Address("Api", "http://" + local, pconfig.ApiPort),
                listenEndpoint: new Address("Listen", local, pconfig.ListenPort),
                ethAccount: null,
                metricsEndpoint: null
            );

            var pc = new BinaryProcessControl(pluginTools.GetLog(), process, pconfig);
            processControlMap.Add(instance, pc);

            return instance;
        }

        private string GetName(LogosStorageStartupConfig config)
        {
            if (!string.IsNullOrEmpty(config.NameOverride))
            {
                return config.NameOverride + "_" + numberSource.GetNextNumber();
            }
            return "storage_" + numberSource.GetNextNumber();
        }

        private void LogSeparator()
        {
            Log("----------------------------------------------------------------------------");
        }

        private void Log(LogosStorageProcessConfig pconfig)
        {
            Log(
                "NodeConfig:Name=" + pconfig.Name +
                "ApiPort=" + pconfig.ApiPort +
                "DiscPort=" + pconfig.DiscPort +
                "ListenPort=" + pconfig.ListenPort +
                "DataDir=" + pconfig.DataDir
            );
        }

        private void Log(string message)
        {
            pluginTools.GetLog().Log(message);
        }

        private static void DeleteParentDataDir()
        {
            if (Directory.Exists(dataParentDir))
            {
                Directory.Delete(dataParentDir, true);
            }
        }

        private static void StopAllLogosStorageProcesses()
        {
            var processes = Process.GetProcesses();
            var storageProcesses = processes.Where(p =>
                p.ProcessName.ToLowerInvariant() == "storage" &&
                p.MainModule != null &&
                p.MainModule.FileName == logosStorageExePath.Get()
            ).ToArray();

            foreach (var c in storageProcesses)
            {
                c.Kill();
                c.WaitForExit();
            }
        }
    }
}
