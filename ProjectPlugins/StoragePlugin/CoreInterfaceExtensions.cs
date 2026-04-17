using LogosStorageClient;
using LogosStorageClient.Hooks;
using Core;

namespace StoragePlugin
{
    public static class CoreInterfaceExtensions
    {
        public static ILogosStorageInstance[] DeployLogosStorageNodes(this CoreInterface ci, int number, Action<ILogosStorageSetup> setup)
        {
            return Plugin(ci).DeployLogosStorageNodes(number, setup);
        }

        public static IStorageNodeGroup WrapLogosStorageContainers(this CoreInterface ci, ILogosStorageInstance[] instances)
        {
            return Plugin(ci).WrapLogosStorageContainers(instances);
        }

        public static IStorageNode StartStorageNode(this CoreInterface ci)
        {
            return ci.StartStorageNodes(1)[0];
        }

        public static IStorageNode StartStorageNode(this CoreInterface ci, Action<ILogosStorageSetup> setup)
        {
            return ci.StartStorageNodes(1, setup)[0];
        }

        public static IStorageNodeGroup StartStorageNodes(this CoreInterface ci, int number, Action<ILogosStorageSetup> setup)
        {
            var rc = ci.DeployLogosStorageNodes(number, setup);
            var result = ci.WrapLogosStorageContainers(rc);
            return result;
        }

        public static IStorageNodeGroup StartStorageNodes(this CoreInterface ci, int number)
        {
            return ci.StartStorageNodes(number, s => { });
        }

        public static void AddLogosStorageHooksProvider(this CoreInterface ci, ILogosStorageHooksProvider hooksProvider)
        {
            Plugin(ci).AddLogosStorageHooksProvider(hooksProvider);
        }

        private static StoragePlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<StoragePlugin>();
        }
    }
}
