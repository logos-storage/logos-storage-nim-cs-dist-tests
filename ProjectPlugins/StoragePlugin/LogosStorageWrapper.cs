using LogosStorageClient;
using LogosStorageClient.Hooks;
using Core;
using Logging;

namespace StoragePlugin
{
    public class LogosStorageWrapper
    {
        private readonly IPluginTools pluginTools;
        private readonly ProcessControlMap processControlMap;
        private readonly LogosStorageHooksFactory hooksFactory;
        private DebugInfoVersion? versionResponse;

        public LogosStorageWrapper(IPluginTools pluginTools, ProcessControlMap processControlMap, LogosStorageHooksFactory hooksFactory)
        {
            this.pluginTools = pluginTools;
            this.processControlMap = processControlMap;
            this.hooksFactory = hooksFactory;
        }

        public string GetLogosStorageId()
        {
            if (versionResponse != null) return versionResponse.Version;
            return "unknown";
        }

        public string GetLogosStorageRevision()
        {
            if (versionResponse != null) return versionResponse.Revision;
            return "unknown";
        }

        public IStorageNodeGroup WrapLogosStorageInstances(ILogosStorageInstance[] instances)
        {
            var storageNodeFactory = new StorageNodeFactory(
                log: pluginTools.GetLog(),
                fileManager: pluginTools.GetFileManager(),
                hooksFactory: hooksFactory,
                httpFactory: pluginTools,
                processControlFactory: processControlMap);

            var group = CreateStorageNodeGroup(instances, storageNodeFactory);

            pluginTools.GetLog().Log($"Logos Storage version: {group.Version}");
            versionResponse = group.Version;

            return group;
        }

        private StorageNodeGroup CreateStorageNodeGroup(ILogosStorageInstance[] instances, StorageNodeFactory storageNodeFactory)
        {
            var nodes = instances.Select(storageNodeFactory.CreateStorageNode).ToArray();
            var group = new StorageNodeGroup(pluginTools, nodes);

            try
            {
                Stopwatch.Measure(pluginTools.GetLog(), "EnsureOnline", group.EnsureOnline);
            }
            catch
            {
                LogosStorageNodesNotOnline(instances);
                throw;
            }

            return group;
        }

        private void LogosStorageNodesNotOnline(ILogosStorageInstance[] instances)
        {
            pluginTools.GetLog().Log("Logos Storage nodes failed to start");
            var log = pluginTools.GetLog();
            foreach (var i in instances)
            {
                var pc = processControlMap.Get(i);
                pc.DownloadLog(log.CreateSubfile(i.Name + "_failed_to_start"));
            }
        }
    }
}
