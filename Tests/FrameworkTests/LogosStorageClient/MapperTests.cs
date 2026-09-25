using NUnit.Framework;
using LogosStorageClient;
using StorageOpenApi;

namespace FrameworkTests.LogosStorageClient
{
    [TestFixture]
    public class MapperTests
    {
        private Mapper mapper = null!;

        [SetUp]
        public void SetUp()
        {
            mapper = new Mapper();
        }

        [Test]
        public void Map_DebugInfo_MapsAllFields()
        {
            var openApiDebugInfo = new StorageOpenApi.DebugInfo
            {
                Id = "peer-123",
                Spr = "spr-abc",
                Addrs = new List<string> { "/ip4/1.2.3.4/tcp/8080" },
                AnnounceAddresses = new List<string> { "/ip4/5.6.7.8/tcp/8080" },
                Storage = new StorageVersion { Version = "1.0.0", Revision = "abc123" },
                Table = new PeersTable
                {
                    LocalNode = new Node { NodeId = "nodeId-1", PeerId = "peerId-1" },
                    Nodes = new List<Node>()
                }
            };

            var result = mapper.Map(openApiDebugInfo);

            Assert.That(result.Id, Is.EqualTo("peer-123"));
            Assert.That(result.Spr, Is.EqualTo("spr-abc"));
            Assert.That(result.Addrs, Is.EqualTo(new[] { "/ip4/1.2.3.4/tcp/8080" }));
            Assert.That(result.AnnounceAddresses, Is.EqualTo(new[] { "/ip4/5.6.7.8/tcp/8080" }));
            Assert.That(result.Version.Version, Is.EqualTo("1.0.0"));
            Assert.That(result.Version.Revision, Is.EqualTo("abc123"));
            Assert.That(result.Table.LocalNode.NodeId, Is.EqualTo("nodeId-1"));
        }

        [Test]
        public void Map_Space_MapsAllBytes()
        {
            var openApiSpace = new StorageOpenApi.Space
            {
                QuotaMaxBytes = 10_000_000,
                QuotaUsedBytes = 3_000_000,
                QuotaReservedBytes = 1_000_000,
                TotalBlocks = 42
            };

            var result = mapper.Map(openApiSpace);

            Assert.That(result.QuotaMaxBytes, Is.EqualTo(10_000_000));
            Assert.That(result.QuotaUsedBytes, Is.EqualTo(3_000_000));
            Assert.That(result.QuotaReservedBytes, Is.EqualTo(1_000_000));
            Assert.That(result.TotalBlocks, Is.EqualTo(42));
        }

        [Test]
        public void Map_DataList_MapsContent()
        {
            var openApiDataList = new StorageOpenApi.DataList
            {
                Content = new List<DataItem>
                {
                    new DataItem
                    {
                        Cid = "bafybeicid1",
                        Manifest = new ManifestItem
                        {
                            TreeCid = "rootHashXYZ",
                            DatasetSize = 1024,
                            BlockSize = 512
                        }
                    }
                }
            };

            var result = mapper.Map(openApiDataList);

            Assert.That(result.Content.Length, Is.EqualTo(1));
            Assert.That(result.Content[0].Cid.Id, Is.EqualTo("bafybeicid1"));
            Assert.That(result.Content[0].Manifest.RootHash, Is.EqualTo("rootHashXYZ"));
            Assert.That(result.Content[0].Manifest.DatasetSize.SizeInBytes, Is.EqualTo(1024));
            Assert.That(result.Content[0].Manifest.BlockSize.SizeInBytes, Is.EqualTo(512));
        }

        [Test]
        public void Map_DebugInfoTable_WithPeers_MapsNodes()
        {
            var openApiDebugInfo = new StorageOpenApi.DebugInfo
            {
                Id = "x",
                Spr = "x",
                Addrs = new List<string>(),
                AnnounceAddresses = new List<string>(),
                Storage = new StorageVersion { Version = "1", Revision = "r" },
                Table = new PeersTable
                {
                    LocalNode = new Node { NodeId = "local-nodeId", PeerId = "local-peerId" },
                    Nodes = new List<Node>
                    {
                        new Node { NodeId = "node-1", PeerId = "peer-1", Seen = true }
                    }
                }
            };

            var result = mapper.Map(openApiDebugInfo);

            Assert.That(result.Table.Nodes.Length, Is.EqualTo(1));
            Assert.That(result.Table.Nodes[0].PeerId, Is.EqualTo("peer-1"));
            Assert.That(result.Table.Nodes[0].Seen, Is.True);
        }

        [Test]
        public void Map_DataList_EmptyContent_ReturnsEmptyArray()
        {
            var openApiDataList = new StorageOpenApi.DataList
            {
                Content = new List<DataItem>()
            };

            var result = mapper.Map(openApiDataList);

            Assert.That(result.Content, Is.Empty);
        }
    }
}
