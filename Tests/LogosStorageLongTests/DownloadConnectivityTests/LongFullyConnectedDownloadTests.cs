using LogosStorageTests;
using DistTestCore;
using NUnit.Framework;
using Utils;

namespace LogosStorageLongTests.DownloadConnectivityTests
{
    [TestFixture]
    public class LongFullyConnectedDownloadTests : AutoBootstrapDistTest
    {
        [Test]
        [UseLongTimeouts]
        [Combinatorial]
        public void FullyConnectedDownloadTest(
            [Values(10, 15, 20)] int numberOfNodes,
            [Values(10, 100)] int sizeMBs)
        {
            var nodes = StartLogosStorage(numberOfNodes);

            CreatePeerDownloadTestHelpers().AssertFullDownloadInterconnectivity(nodes, sizeMBs.MB());
        }
    }
}
