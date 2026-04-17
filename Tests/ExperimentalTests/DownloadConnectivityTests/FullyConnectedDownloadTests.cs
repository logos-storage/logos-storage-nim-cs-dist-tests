using LogosStorageClient;
using LogosStorageTests;
using NUnit.Framework;
using Utils;

namespace ExperimentalTests.DownloadConnectivityTests
{
    [TestFixture]
    public class FullyConnectedDownloadTests : AutoBootstrapDistTest
    {
        [Test]
        public void MetricsDoesNotInterfereWithPeerDownload()
        {
            var nodes = StartLogosStorage(2, s => s.EnableMetrics());

            AssertAllNodesConnected(nodes);
        }

        [Test]
        [Combinatorial]
        public void FullyConnectedDownloadTest(
            [Values(2, 5)] int numberOfNodes,
            [Values(1, 10)] int sizeMBs)
        {
            var nodes = StartLogosStorage(numberOfNodes);

            AssertAllNodesConnected(nodes, sizeMBs);
        }

        private void AssertAllNodesConnected(IEnumerable<IStorageNode> nodes, int sizeMBs = 10)
        {
            CreatePeerDownloadTestHelpers().AssertFullDownloadInterconnectivity(nodes, sizeMBs.MB());
        }
    }
}
