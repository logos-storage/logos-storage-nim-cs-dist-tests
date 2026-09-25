using Moq;
using NUnit.Framework;
using LogosStorageClient;
using LogosStorageClient.Hooks;
using Utils;

namespace FrameworkTests.LogosStorageClient
{
    [TestFixture]
    public class MuxingStorageNodeHooksTests
    {
        private Mock<IStorageNodeHooks> hookA = null!;
        private Mock<IStorageNodeHooks> hookB = null!;
        private MuxingStorageNodeHooks mux = null!;

        [SetUp]
        public void SetUp()
        {
            hookA = new Mock<IStorageNodeHooks>();
            hookB = new Mock<IStorageNodeHooks>();
            mux = new MuxingStorageNodeHooks(new[] { hookA.Object, hookB.Object });
        }

        [Test]
        public void OnNodeStarting_DispatchesToAllHooks()
        {
            var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            mux.OnNodeStarting(start, "image:tag");
            hookA.Verify(h => h.OnNodeStarting(start, "image:tag"), Times.Once);
            hookB.Verify(h => h.OnNodeStarting(start, "image:tag"), Times.Once);
        }

        [Test]
        public void OnNodeStarted_DispatchesToAllHooks()
        {
            var mockNode = new Mock<IStorageNode>();
            mux.OnNodeStarted(mockNode.Object, "peerId-abc", "nodeId-xyz");
            hookA.Verify(h => h.OnNodeStarted(mockNode.Object, "peerId-abc", "nodeId-xyz"), Times.Once);
            hookB.Verify(h => h.OnNodeStarted(mockNode.Object, "peerId-abc", "nodeId-xyz"), Times.Once);
        }

        [Test]
        public void OnNodeStopping_DispatchesToAllHooks()
        {
            mux.OnNodeStopping();
            hookA.Verify(h => h.OnNodeStopping(), Times.Once);
            hookB.Verify(h => h.OnNodeStopping(), Times.Once);
        }

        [Test]
        public void OnFileUploading_DispatchesToAllHooks()
        {
            var size = new ByteSize(1024);
            mux.OnFileUploading("uid-1", size);
            hookA.Verify(h => h.OnFileUploading("uid-1", size), Times.Once);
            hookB.Verify(h => h.OnFileUploading("uid-1", size), Times.Once);
        }

        [Test]
        public void OnFileUploaded_DispatchesToAllHooks()
        {
            var size = new ByteSize(1024);
            var cid = new ContentId("bafybeicid1");
            mux.OnFileUploaded("uid-1", size, cid);
            hookA.Verify(h => h.OnFileUploaded("uid-1", size, cid), Times.Once);
            hookB.Verify(h => h.OnFileUploaded("uid-1", size, cid), Times.Once);
        }

        [Test]
        public void OnFileDownloading_DispatchesToAllHooks()
        {
            var cid = new ContentId("bafybeicid1");
            mux.OnFileDownloading(cid);
            hookA.Verify(h => h.OnFileDownloading(cid), Times.Once);
            hookB.Verify(h => h.OnFileDownloading(cid), Times.Once);
        }

        [Test]
        public void OnFileDownloaded_DispatchesToAllHooks()
        {
            var size = new ByteSize(2048);
            var cid = new ContentId("bafybeicid2");
            mux.OnFileDownloaded(size, cid);
            hookA.Verify(h => h.OnFileDownloaded(size, cid), Times.Once);
            hookB.Verify(h => h.OnFileDownloaded(size, cid), Times.Once);
        }
    }

    [TestFixture]
    public class LogosStorageHooksFactoryTests
    {
        [Test]
        public void CreateHooks_NoProviders_ReturnsDoNothingHooks()
        {
            var factory = new LogosStorageHooksFactory();
            var hooks = factory.CreateHooks("test-node");
            Assert.That(hooks, Is.InstanceOf<DoNothingLogosStorageHooks>());
        }

        [Test]
        public void CreateHooks_OneProvider_ReturnsMuxingHooks()
        {
            var mockHooks = new Mock<IStorageNodeHooks>();
            var mockProvider = new Mock<ILogosStorageHooksProvider>();
            mockProvider.Setup(p => p.CreateHooks("my-node")).Returns(mockHooks.Object);

            var factory = new LogosStorageHooksFactory();
            factory.Providers.Add(mockProvider.Object);

            var result = factory.CreateHooks("my-node");
            Assert.That(result, Is.InstanceOf<MuxingStorageNodeHooks>());
        }

        [Test]
        public void CreateHooks_MultipleProviders_ReturnsMuxingHooks()
        {
            var factory = new LogosStorageHooksFactory();
            factory.Providers.Add(new DoNothingHooksProvider());
            factory.Providers.Add(new DoNothingHooksProvider());

            var result = factory.CreateHooks("my-node");
            Assert.That(result, Is.InstanceOf<MuxingStorageNodeHooks>());
        }
    }
}
