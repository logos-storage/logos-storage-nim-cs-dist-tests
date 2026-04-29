using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;
using Utils;

namespace LogosStorageDiscordBotPlugin
{
    public class DiscordBotContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "discordbot-bibliotech";
        public override string Image => "logosstorage/logos-storage-discordbot:sha-8033da1";

        public static string RewardsPort = "bot_rewards_port";

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<DiscordBotStartupConfig>();

            ScheduleInPoolsWithLabel("workload-type", "tests-pods");
            AddToleration("cloud.google.com/gke-spot", "true", "NoSchedule");

            AddEnvVar("TOKEN", config.Token);
            AddEnvVar("SERVERNAME", config.ServerName);
            AddEnvVar("ADMINROLE", config.AdminRoleName);
            AddEnvVar("ADMINCHANNELNAME", config.AdminChannelName);
            AddEnvVar("REWARDSCHANNELNAME", config.RewardChannelName);
            AddEnvVar("KUBECONFIG", "/opt/kubeconfig.yaml");
            AddEnvVar("KUBENAMESPACE", config.KubeNamespace);

            var gethInfo = config.GethInfo;
            AddEnvVar("GETH_HOST", gethInfo.Host);
            AddEnvVar("GETH_HTTP_PORT", gethInfo.Port.ToString());
            AddEnvVar("GETH_PRIVATE_KEY", gethInfo.PrivKey);
            AddEnvVar("CODEXCONTRACTS_MARKETPLACEADDRESS", gethInfo.MarketplaceAddress);
            AddEnvVar("CODEXCONTRACTS_TOKENADDRESS", gethInfo.TokenAddress);
            AddEnvVar("CODEXCONTRACTS_ABI", gethInfo.Abi);

            AddEnvVar("NODISCORD", "1");

            AddInternalPortAndVar("REWARDAPIPORT", RewardsPort);

            if (!string.IsNullOrEmpty(config.DataPath))
            {
                AddEnvVar("DATAPATH", config.DataPath);
                AddVolume(config.DataPath, 1.GB());
            }
        }
    }
}
