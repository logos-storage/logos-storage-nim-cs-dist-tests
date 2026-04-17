using LogosStorageClient;
using KubernetesWorkflow.Types;

namespace StoragePlugin
{
    public class LogosStorageDeployment
    {
        public LogosStorageDeployment(LogosStorageInstance[] logosStorageInstances,
            RunningPod? prometheusContainer,
            RunningPod? discordBotContainer, DeploymentMetadata metadata,
            string id)
        {
            Id = id;
            LogosStorageInstances = logosStorageInstances;
            PrometheusContainer = prometheusContainer;
            DiscordBotContainer = discordBotContainer;
            Metadata = metadata;
        }

        public string Id { get; }
        public LogosStorageInstance[] LogosStorageInstances { get; }
        public RunningPod? PrometheusContainer { get; }
        public RunningPod? DiscordBotContainer { get; }
        public DeploymentMetadata Metadata { get; }
    }

    public class DeploymentMetadata
    {
        public DeploymentMetadata(string name, DateTime startUtc, DateTime finishedUtc, string kubeNamespace,
            int numberOfLogosStorageNodes, int numberOfValidators, int storageQuotaMB, LogosStorageLogLevel logosStorageLogLevel,
            int initialTestTokens, int minPrice, int maxCollateral, int maxDuration, int blockTTL, int blockMI,
            int blockMN)
        {
            Name = name;
            StartUtc = startUtc;
            FinishedUtc = finishedUtc;
            KubeNamespace = kubeNamespace;
            NumberOfLogosStorageNodes = numberOfLogosStorageNodes;
            NumberOfValidators = numberOfValidators;
            StorageQuotaMB = storageQuotaMB;
            LogosStorageLogLevel = logosStorageLogLevel;
            InitialTestTokens = initialTestTokens;
            MinPrice = minPrice;
            MaxCollateral = maxCollateral;
            MaxDuration = maxDuration;
            BlockTTL = blockTTL;
            BlockMI = blockMI;
            BlockMN = blockMN;
        }

        public string Name { get; }
        public DateTime StartUtc { get; }
        public DateTime FinishedUtc { get; }
        public string KubeNamespace { get; }
        public int NumberOfLogosStorageNodes { get; }
        public int NumberOfValidators { get; }
        public int StorageQuotaMB { get; }
        public LogosStorageLogLevel LogosStorageLogLevel { get; }
        public int InitialTestTokens { get; }
        public int MinPrice { get; }
        public int MaxCollateral { get; }
        public int MaxDuration { get; }
        public int BlockTTL { get; }
        public int BlockMI { get; }
        public int BlockMN { get; }
    }
}