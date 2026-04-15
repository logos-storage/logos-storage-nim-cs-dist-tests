using LogosStorageClient;
using LogosStorageTests;
using LogosStorageTests.Helpers;
using NUnit.Framework;

namespace LogosStorageReleaseTests.NodeTests
{
    [TestFixture]
    public class PeerTableTests : AutoBootstrapDistTest
    {
        [Test]
        public void PeerTableCompleteness()
        {
            var nodes = StartLogosStorage(10);

            AssertAllNodesSeeEachOther(nodes.Concat([BootstrapNode!]));
        }

        private void AssertAllNodesSeeEachOther(IEnumerable<IStorageNode> nodes)
        {
            var helper = new PeerConnectionTestHelpers(GetTestLog());
            helper.AssertFullyConnected(nodes);
        }
    }
}
