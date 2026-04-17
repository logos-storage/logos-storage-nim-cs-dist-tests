using LogosStorageClient;
using StoragePlugin;
using NUnit.Framework;

namespace LogosStorageTests
{
    public class AutoBootstrapDistTest : LogosStorageDistTest
    {
        private bool isBooting = false;

        public IStorageNode BootstrapNode { get; private set; } = null!;

        [SetUp]
        public void SetupBootstrapNode()
        {
            isBooting = true;
            BootstrapNode = StartLogosStorage(s => s.WithName("BOOTSTRAP_" + GetTestNamespace()));
            isBooting = false;
        }

        [TearDown]
        public void TearDownBootstrapNode()
        {
            BootstrapNode.Stop(waitTillStopped: false);
        }

        protected override void OnLogosStorageSetup(ILogosStorageSetup setup)
        {
            if (isBooting) return;

            var node = BootstrapNode;
            if (node != null) setup.WithBootstrapNode(node);
        }
    }
}
