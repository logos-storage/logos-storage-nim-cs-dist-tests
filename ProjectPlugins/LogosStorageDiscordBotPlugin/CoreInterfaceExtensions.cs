using Core;
using KubernetesWorkflow.Types;

namespace LogosStorageDiscordBotPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningPod DeployLogosStorageDiscordBot(this CoreInterface ci, DiscordBotStartupConfig config)
        {
            return Plugin(ci).Deploy(config);
        }

        public static RunningPod DeployRewarderBot(this CoreInterface ci, RewarderBotStartupConfig config)
        {
            return Plugin(ci).DeployRewarder(config);
        }

        private static LogosStorageDiscordBotPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<LogosStorageDiscordBotPlugin>();
        }
    }
}
