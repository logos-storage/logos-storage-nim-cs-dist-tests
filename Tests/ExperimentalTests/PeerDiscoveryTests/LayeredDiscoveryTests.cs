using LogosStorageClient;
using LogosStorageTests;
using NUnit.Framework;

namespace ExperimentalTests.PeerDiscoveryTests
{
    [TestFixture]
    public class LayeredDiscoveryTests : LogosStorageDistTest
    {
        [Test]
        public void TwoLayersTest()
        {
            var root = StartLogosStorage();
            var l1Source = StartLogosStorage(s => s.WithBootstrapNode(root));
            var l1Node = StartLogosStorage(s => s.WithBootstrapNode(root));
            var l2Target = StartLogosStorage(s => s.WithBootstrapNode(l1Node));

            AssertAllNodesConnected(root, l1Source, l1Node, l2Target);
        }

        [Test]
        public void ThreeLayersTest()
        {
            var root = StartLogosStorage();
            var l1Source = StartLogosStorage(s => s.WithBootstrapNode(root));
            var l1Node = StartLogosStorage(s => s.WithBootstrapNode(root));
            var l2Node = StartLogosStorage(s => s.WithBootstrapNode(l1Node));
            var l3Target = StartLogosStorage(s => s.WithBootstrapNode(l2Node));

            AssertAllNodesConnected(root, l1Source, l1Node, l2Node, l3Target);
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        public void NodeChainTest(int chainLength)
        {
            var nodes = new List<IStorageNode>();
            var node = StartLogosStorage();
            nodes.Add(node);

            for (var i = 1; i < chainLength; i++)
            {
                node = StartLogosStorage(s => s.WithBootstrapNode(node));
                nodes.Add(node);
            }

            AssertAllNodesConnected(nodes.ToArray());
        }

        private void AssertAllNodesConnected(params IStorageNode[] nodes)
        {
            CreatePeerConnectionTestHelpers().AssertFullyConnected(nodes);
        }
    }
}
