using LogosStorageClient;
using LogosStorageClient.Hooks;
using Core;

namespace StoragePlugin
{
    public class StoragePlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private const bool UseContainers = true;

        private readonly ILogosStorageStarter logosStorageStarter;
        private readonly IPluginTools tools;
        private readonly LogosStorageLogLevel defaultLogLevel = LogosStorageLogLevel.Trace;
        private readonly LogosStorageHooksFactory hooksFactory = new LogosStorageHooksFactory();
        private readonly ProcessControlMap processControlMap = new ProcessControlMap();
        private readonly LogosStorageDockerImage logosStorageDockerImage = new LogosStorageDockerImage();
        private readonly LogosStorageContainerRecipe recipe;
        private readonly LogosStorageWrapper logosStorageWrapper;

        public StoragePlugin(IPluginTools tools)
        {
            this.tools = tools;

            recipe = new LogosStorageContainerRecipe(logosStorageDockerImage);
            logosStorageStarter = CreateLogosStorageStarter();
            logosStorageWrapper = new LogosStorageWrapper(tools, processControlMap, hooksFactory);
        }

        private ILogosStorageStarter CreateLogosStorageStarter()
        {
            if (UseContainers)
            {
                Log("Using Containerized Logos Storage instances");
                return new ContainerLogosStorageStarter(tools, recipe, processControlMap);
            }

            Log("Using Binary Logos Storage instances");
            return new BinaryLogosStorageStarter(tools, processControlMap);
        }

        public string LogPrefix => "(LogosStorage) ";

        public void Awake(IPluginAccess access)
        {
        }

        public void Announce()
        {
            // give codex docker image to contracts plugin.

            Log($"Loaded with Logos Storage ID: '{logosStorageWrapper.GetLogosStorageId()}' - Revision: {logosStorageWrapper.GetLogosStorageRevision()}");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            metadata.Add("storageid", logosStorageWrapper.GetLogosStorageId());
            metadata.Add("storagerevision", logosStorageWrapper.GetLogosStorageRevision());
        }

        public void Decommission()
        {
            logosStorageStarter.Decommission();
        }

        public ILogosStorageInstance[] DeployLogosStorageNodes(int numberOfNodes, Action<ILogosStorageSetup> setup)
        {
            var logosStorageSetup = GetSetup(numberOfNodes, setup);
            return logosStorageStarter.BringOnline(logosStorageSetup);
        }

        public IStorageNodeGroup WrapLogosStorageContainers(ILogosStorageInstance[] instances)
        {
            instances = instances.Select(c => SerializeGate.Gate(c as LogosStorageInstance)).ToArray();
            return logosStorageWrapper.WrapLogosStorageInstances(instances);
        }

        public void AddLogosStorageHooksProvider(ILogosStorageHooksProvider hooksProvider)
        {
            if (hooksFactory.Providers.Contains(hooksProvider)) return;
            hooksFactory.Providers.Add(hooksProvider);
        }

        private LogosStorageSetup GetSetup(int numberOfNodes, Action<ILogosStorageSetup> setup)
        {
            var logosStorageSetup = new LogosStorageSetup(numberOfNodes);
            logosStorageSetup.LogLevel = defaultLogLevel;
            setup(logosStorageSetup);
            return logosStorageSetup;
        }

        private void Log(string msg)
        {
            tools.GetLog().Log(msg);
        }
    }
}
