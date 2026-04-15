using LogosStorageClient;
using KubernetesWorkflow.Types;
using Utils;

namespace StoragePlugin
{
    public static class LogosStorageInstanceContainerExtension
    {
        public static ILogosStorageInstance CreateFromPod(RunningPod pod)
        {
            var container = pod.Containers.Single();

            return new LogosStorageInstance(
                name: container.Name,
                imageName: container.Recipe.Image,
                startUtc: container.Recipe.RecipeCreatedUtc,
                discoveryEndpoint: SetClusterInternalIpAddress(pod, container.GetInternalAddress(LogosStorageContainerRecipe.DiscoveryPortTag)),
                apiEndpoint: container.GetAddress(LogosStorageContainerRecipe.ApiPortTag),
                listenEndpoint: container.GetInternalAddress(LogosStorageContainerRecipe.ListenPortTag),
                ethAccount: container.Recipe.Additionals.Get<EthAccount>(),
                metricsEndpoint: GetMetricsEndpoint(container)
            );
        }

        private static Address SetClusterInternalIpAddress(RunningPod pod, Address address)
        {
            return new Address(
                logName: address.LogName,
                host: pod.PodInfo.Ip,
                port: address.Port
            );
        }

        private static Address? GetMetricsEndpoint(RunningContainer container)
        {
            try
            {
                return container.GetInternalAddress(LogosStorageContainerRecipe.MetricsPortTag);
            }
            catch
            {
                return null;
            }
        }
    }
}
