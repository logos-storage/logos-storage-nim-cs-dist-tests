using LogosStorageClient;
using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Utils;

namespace StoragePlugin
{
    public class ContainerLogosStorageStarter : ILogosStorageStarter
    {
        private readonly IPluginTools pluginTools;
        private readonly ProcessControlMap processControlMap;
        private readonly LogosStorageContainerRecipe recipe;
        private readonly ApiChecker apiChecker;

        public ContainerLogosStorageStarter(IPluginTools pluginTools, LogosStorageContainerRecipe recipe, ProcessControlMap processControlMap)
        {
            this.pluginTools = pluginTools;
            this.recipe = recipe;
            this.processControlMap = processControlMap;
            apiChecker = new ApiChecker(pluginTools);
        }

        public ILogosStorageInstance[] BringOnline(LogosStorageSetup logosStorageSetup)
        {
            LogSeparator();
            Log($"Starting {logosStorageSetup.Describe()}...");

            var startupConfig = CreateStartupConfig(logosStorageSetup);

            var containers = StartLogosStorageContainers(startupConfig, logosStorageSetup.NumberOfNodes, logosStorageSetup.Location);

            apiChecker.CheckCompatibility(containers);

            foreach (var rc in containers)
            {
                var podInfo = GetPodInfo(rc);
                var podInfos = string.Join(", ", rc.Containers.Select(c => $"Container: '{c.Name}' PodLabel: '{c.RunningPod.StartResult.Deployment.PodLabel}' runs at '{podInfo.K8SNodeName}'={podInfo.Ip}"));
                Log($"Started node with image '{containers.First().Containers.First().Recipe.Image}'. ({podInfos})");
                LogEthAddress(rc);
            }
            LogSeparator();

            return containers.Select(CreateInstance).ToArray();
        }

        public void Decommission()
        {
        }

        private StartupConfig CreateStartupConfig(LogosStorageSetup logosStorageSetup)
        {
            var startupConfig = new StartupConfig();
            startupConfig.NameOverride = logosStorageSetup.NameOverride;
            startupConfig.Add(logosStorageSetup);
            return startupConfig;
        }

        private RunningPod[] StartLogosStorageContainers(StartupConfig startupConfig, int numberOfNodes, ILocation location)
        {
            var futureContainers = new List<FutureContainers>();
            for (var i = 0; i < numberOfNodes; i++)
            {
                var workflow = pluginTools.CreateWorkflow();
                futureContainers.Add(workflow.Start(1, location, recipe, startupConfig));
            }

            return futureContainers
                .Select(f => f.WaitForOnline())
                .ToArray();
        }

        private PodInfo GetPodInfo(RunningPod rc)
        {
            var workflow = pluginTools.CreateWorkflow();
            return workflow.GetPodInfo(rc);
        }

        private ILogosStorageInstance CreateInstance(RunningPod pod)
        {
            var instance = LogosStorageInstanceContainerExtension.CreateFromPod(pod);
            var processControl = new LogosStorageContainerProcessControl(pluginTools, pod, onStop: () =>
            {
                processControlMap.Remove(instance);
            });
            processControlMap.Add(instance, processControl);
            return instance;
        }

        private void LogSeparator()
        {
            Log("----------------------------------------------------------------------------");
        }

        private void LogEthAddress(RunningPod rc)
        {
            var account = rc.Containers.First().Recipe.Additionals.Get<EthAccount>();
            if (account == null) return;
            Log($"{rc.Name} = {account}");
        }

        private void Log(string message)
        {
            pluginTools.GetLog().Log(message);
        }
    }
}
