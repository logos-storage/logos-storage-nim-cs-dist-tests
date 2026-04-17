using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace DeployAndRunPlugin
{
    public class DeployAndRunContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "deploy-and-run";
        public override string Image => "thatbenbierens/dist-tests-deployandrun:initial";

        protected override void Initialize(StartupConfig config)
        {
            var setup = config.Get<RunConfig>();

            if (setup.LogosStorageImageOverride != null)
            {
                AddEnvVar("STORAGEDOCKERIMAGE", setup.LogosStorageImageOverride);
            }

            AddEnvVar("DNR_REP", setup.Replications.ToString());
            AddEnvVar("DNR_NAME", setup.Name);
            AddEnvVar("DNR_FILTER", setup.Filter);
            AddEnvVar("DNR_DURATION", setup.Duration.TotalSeconds.ToString());

            AddEnvVar("KUBECONFIG", "/opt/kubeconfig.yaml");
            AddEnvVar("LOGPATH", "/var/log/storage-continuous-tests");

            AddVolume(name: "kubeconfig", mountPath: "/opt/kubeconfig.yaml", subPath: "kubeconfig.yaml", secret: "storage-dist-tests-app-kubeconfig");
            AddVolume(name: "logs", mountPath: "/var/log/storage-continuous-tests", hostPath: "/var/log/storage-continuous-tests");
        }
    }

    public class RunConfig
    {
        public RunConfig(string name, string filter, TimeSpan duration, int replications, string? logosStorageImageOverride = null)
        {
            Name = name;
            Filter = filter;
            Duration = duration;
            Replications = replications;
            LogosStorageImageOverride = logosStorageImageOverride;
        }

        public string Name { get; }
        public string Filter { get; }
        public TimeSpan Duration { get; }
        public int Replications { get; }
        public string? LogosStorageImageOverride { get; }
    }
}