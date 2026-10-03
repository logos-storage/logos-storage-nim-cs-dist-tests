using Moq;
using NUnit.Framework;
using LogosStorageClient;
using LogosStorageClient.Hooks;
using FileUtils;
using Logging;
using Utils;

namespace FrameworkTests.LogosStorageClient
{
    [TestFixture]
    public class StorageNodeTests
    {
        private Mock<ILogosStorageAccess> mockAccess = null!;
        private Mock<IStorageNodeHooks> mockHooks = null!;
        private Mock<IFileManager> mockFileManager = null!;
        private StorageNode node = null!;
        private NullLog nullLog = null!;

        [SetUp]
        public void SetUp()
        {
            mockAccess = new Mock<ILogosStorageAccess>();
            mockHooks = new Mock<IStorageNodeHooks>();
            mockFileManager = new Mock<IFileManager>();
            nullLog = new NullLog();

            mockAccess.Setup(a => a.GetName()).Returns("test-node");
            mockAccess.Setup(a => a.GetStartUtc()).Returns(DateTime.UtcNow);
            mockAccess.Setup(a => a.GetImageName()).Returns("test-image");

            node = new StorageNode(nullLog, mockAccess.Object, mockFileManager.Object, mockHooks.Object);
        }

        // --- GetName ---

        [Test]
        public void GetName_DelegatesToAccess()
        {
            Assert.That(node.GetName(), Is.EqualTo("test-node"));
        }

        // --- UploadFile ---

        [Test]
        public void UploadFile_ValidResponse_ReturnsCid()
        {
            using var tmpFile = new TempFile();
            File.WriteAllText(tmpFile.Path, "hello world");
            var trackedFile = new TrackedFile(nullLog, tmpFile.Path, "test-file");

            mockAccess.Setup(a => a.UploadFile(It.IsAny<UploadInput>())).Returns("bafybeicid123");

            var cid = node.UploadFile(trackedFile);

            Assert.That(cid.Id, Is.EqualTo("bafybeicid123"));
        }

        [Test]
        public void UploadFile_FiresOnFileUploadingAndOnFileUploaded()
        {
            using var tmpFile = new TempFile();
            File.WriteAllText(tmpFile.Path, "data");
            var trackedFile = new TrackedFile(nullLog, tmpFile.Path, "test-file");
            mockAccess.Setup(a => a.UploadFile(It.IsAny<UploadInput>())).Returns("bafybeicid999");

            node.UploadFile(trackedFile);

            mockHooks.Verify(h => h.OnFileUploading(It.IsAny<string>(), It.IsAny<ByteSize>()), Times.Once);
            mockHooks.Verify(h => h.OnFileUploaded(
                It.IsAny<string>(),
                It.IsAny<ByteSize>(),
                It.Is<ContentId>(c => c.Id == "bafybeicid999")), Times.Once);
        }

        [Test]
        public void UploadFile_EmptyResponse_Throws()
        {
            using var tmpFile = new TempFile();
            File.WriteAllText(tmpFile.Path, "data");
            var trackedFile = new TrackedFile(nullLog, tmpFile.Path, "test-file");
            mockAccess.Setup(a => a.UploadFile(It.IsAny<UploadInput>())).Returns(string.Empty);

            Assert.Throws<Exception>(() => node.UploadFile(trackedFile));
        }

        [Test]
        public void UploadFile_StoreBlockFailure_Throws()
        {
            using var tmpFile = new TempFile();
            File.WriteAllText(tmpFile.Path, "data");
            var trackedFile = new TrackedFile(nullLog, tmpFile.Path, "test-file");
            mockAccess.Setup(a => a.UploadFile(It.IsAny<UploadInput>())).Returns("Unable to store block");

            Assert.Throws<Exception>(() => node.UploadFile(trackedFile));
        }

        // --- Stop ---

        [Test]
        public void Stop_FiresOnNodeStoppingAndCallsAccessStop()
        {
            node.Stop(waitTillStopped: true);

            mockHooks.Verify(h => h.OnNodeStopping(), Times.Once);
            mockAccess.Verify(a => a.Stop(true), Times.Once);
        }

        // --- Initialize ---

        [Test]
        public void Initialize_ValidDebugInfo_SetsVersion()
        {
            mockAccess.Setup(a => a.GetDebugInfo()).Returns(new DebugInfo
            {
                Id = "peer-abc",
                Table = new DebugInfoTable { LocalNode = new DebugInfoTableNode { NodeId = "node-xyz" } },
                Version = new DebugInfoVersion { Version = "1.0.0", Revision = "rev123" }
            });

            node.Initialize();

            Assert.That(node.Version.Version, Is.EqualTo("1.0.0"));
            Assert.That(node.Version.Revision, Is.EqualTo("rev123"));
            Assert.That(node.GetPeerId(), Is.EqualTo("peer-abc"));
        }

        [Test]
        public void Initialize_FiresOnNodeStarted()
        {
            mockAccess.Setup(a => a.GetDebugInfo()).Returns(new DebugInfo
            {
                Id = "peer-abc",
                Table = new DebugInfoTable { LocalNode = new DebugInfoTableNode { NodeId = "node-xyz" } },
                Version = new DebugInfoVersion { Version = "1.0.0", Revision = "rev123" }
            });

            node.Initialize();

            mockHooks.Verify(h => h.OnNodeStarted(node, "peer-abc", "node-xyz"), Times.Once);
        }

        // --- WaitUntilQuotaUsedIncreased ---

        [Test]
        public void WaitUntilQuotaUsedIncreased_QuotaAlreadyMet_DoesNotThrow()
        {
            var startSpace = new LogosStorageSpace { QuotaUsedBytes = 0, QuotaMaxBytes = 1_000_000, QuotaReservedBytes = 0 };
            var afterSpace = new LogosStorageSpace { QuotaUsedBytes = 500, QuotaMaxBytes = 1_000_000, QuotaReservedBytes = 0 };
            mockAccess.Setup(a => a.Space()).Returns(afterSpace);

            Assert.DoesNotThrow(() =>
                node.WaitUntilQuotaUsedIncreased(startSpace, new ByteSize(400), TimeSpan.FromSeconds(5)));
        }

        // --- ConnectToPeer ---

        [Test]
        public void ConnectToPeer_ReplacesZeroIPWithPeerDiscoveryHost()
        {
            var mockPeerNode = new Mock<IStorageNode>();
            mockPeerNode.Setup(n => n.GetName()).Returns("peer-node");
            mockPeerNode.Setup(n => n.GetDiscoveryEndpoint())
                .Returns(new Address("peer-node", "http://10.0.0.2", 1234));
            mockPeerNode.Setup(n => n.GetDebugInfo(It.IsAny<bool>())).Returns(new DebugInfo
            {
                Id = "peer-id-xyz",
                Addrs = new[] { "/ip4/0.0.0.0/tcp/8080", "/ip4/0.0.0.0/tcp/9090" },
                Table = new DebugInfoTable { LocalNode = new DebugInfoTableNode() },
                Version = new DebugInfoVersion { Version = "1.0", Revision = "r1" }
            });

            node.ConnectToPeer(mockPeerNode.Object);

            mockAccess.Verify(a => a.ConnectToPeer(
                "peer-id-xyz",
                It.Is<string[]>(addrs =>
                    addrs[0] == "/ip4/10.0.0.2/tcp/8080" &&
                    addrs[1] == "/ip4/10.0.0.2/tcp/9090")),
                Times.Once);
        }

        // --- TransferSpeeds ---

        [Test]
        public void TransferSpeeds_InitiallyEmpty()
        {
            Assert.That(node.TransferSpeeds.GetUploadSpeed(), Is.Null);
            Assert.That(node.TransferSpeeds.GetDownloadSpeed(), Is.Null);
        }

        // --- HasCrashed / Space / GetPeerId ---

        [Test]
        public void HasCrashed_DelegatesToAccess()
        {
            mockAccess.Setup(a => a.HasCrashed()).Returns(true);
            Assert.That(node.HasCrashed(), Is.True);
        }
    }

    internal class TempFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.GetTempFileName();
        public void Dispose() => File.Delete(Path);
    }
}
