using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace MetricsPlugin
{
    public class PrometheusContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "prometheus";
        public override string Image => "logosstorage/dist-tests-prometheus:latest";

        public const string PortTag = "prometheus_port_tag";

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<PrometheusStartupConfig>();

            ScheduleInPoolsWithLabel("workload-type", "tests-pods");

            AddExposedPortAndVar("PROM_PORT", PortTag);
            AddEnvVar("PROM_CONFIG", config.PrometheusConfigBase64);
        }
    }
}
