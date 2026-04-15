using LogosStorageClient;
using StoragePlugin;
using LogosStorageTests;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace LogosStorageReleaseTests.DataTests
{
    [TestFixture]
    public class TheseusTest : AutoBootstrapDistTest
    {
        private readonly List<IStorageNode> nodes = new List<IStorageNode>();
        private TrackedFile file = null!;
        private ContentId cid = new ContentId();

        [SetUp]
        public void Setup()
        {
            file = GenerateTestFile(10.MB());
        }

        [Test]
        [Combinatorial]
        public void Theseus(
            [Values(1, 2)] int remainingNodes,
            [Values(5)] int steps)
        {
            Assert.That(remainingNodes, Is.GreaterThan(0));
            Assert.That(steps, Is.GreaterThan(remainingNodes + 1));

            nodes.AddRange(StartLogosStorage(remainingNodes + 1));
            cid = nodes.First().UploadFile(file);

            AllNodesHaveFile();

            for (var i = 0; i < steps; i++)
            {
                Log($"{nameof(Theseus)} step {i}");
                nodes[0].Stop(waitTillStopped: true);
                nodes.RemoveAt(0);

                nodes.Add(StartLogosStorage());

                AllNodesHaveFile();
            }
        }

        private void AllNodesHaveFile()
        {
            Log($"{nameof(AllNodesHaveFile)} {nodes.Names()}");
            foreach (var n in nodes) HasFile(n);
        }

        private void HasFile(IStorageNode n)
        {
            var downloaded = n.DownloadContent(cid);
            file.AssertIsEqual(downloaded);
        }
    }
}
