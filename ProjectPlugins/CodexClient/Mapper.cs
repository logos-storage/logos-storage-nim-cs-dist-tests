using Newtonsoft.Json.Linq;
using System.Numerics;
using Utils;

namespace CodexClient
{
    public class Mapper
    {
        public DebugInfo Map(CodexOpenApi.DebugInfo debugInfo)
        {
            return new DebugInfo
            {
                Id = debugInfo.Id,
                Spr = debugInfo.Spr,
                Addrs = debugInfo.Addrs.ToArray(),
                AnnounceAddresses = debugInfo.AnnounceAddresses.ToArray(),
                Version = Map(debugInfo.Storage),
                Table = Map(debugInfo.Table)
            };
        }

        public LocalDatasetList Map(CodexOpenApi.DataList dataList)
        {
            return new LocalDatasetList
            {
                Content = dataList.Content.Select(Map).ToArray()
            };
        }

        public LocalDataset Map(CodexOpenApi.DataItem dataItem)
        {
            return new LocalDataset
            {
                Cid = new ContentId(dataItem.Cid),
                Manifest = MapManifest(dataItem.Manifest)
            };
        }
        public CodexSpace Map(CodexOpenApi.Space space)
        {
            return new CodexSpace
            {
                QuotaMaxBytes = space.QuotaMaxBytes,
                QuotaReservedBytes = space.QuotaReservedBytes,
                QuotaUsedBytes = space.QuotaUsedBytes,
                TotalBlocks = space.TotalBlocks
            };
        }

        private DebugInfoVersion Map(CodexOpenApi.StorageVersion obj)
        {
            return new DebugInfoVersion
            {
                Version = obj.Version,
                Revision = obj.Revision
            };
        }

        private DebugInfoTable Map(CodexOpenApi.PeersTable obj)
        {
            return new DebugInfoTable
            {
                LocalNode = Map(obj.LocalNode),
                Nodes = Map(obj.Nodes)
            };
        }

        private DebugInfoTableNode Map(CodexOpenApi.Node? token)
        {
            if (token == null) return new DebugInfoTableNode();
            return new DebugInfoTableNode
            {
                Address = token.Address,
                NodeId = token.NodeId,
                PeerId = token.PeerId,
                Record = token.Record,
                Seen = token.Seen
            };
        }

        private DebugInfoTableNode[] Map(ICollection<CodexOpenApi.Node> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return new DebugInfoTableNode[0];
            }

            return nodes.Select(Map).ToArray();
        }

        private Manifest MapManifest(CodexOpenApi.ManifestItem manifest)
        {
            return new Manifest
            {
                BlockSize = new ByteSize(Convert.ToInt64(manifest.BlockSize)),
                DatasetSize = new ByteSize(Convert.ToInt64(manifest.DatasetSize)),
                RootHash = manifest.TreeCid
            };
        }

        private JArray JArray(IDictionary<string, object> map, string name)
        {
            return (JArray)map[name];
        }

        private JObject JObject(IDictionary<string, object> map, string name)
        {
            return (JObject)map[name];
        }

        private string StringOrEmpty(JObject obj, string name)
        {
            if (obj.TryGetValue(name, out var token))
            {
                var str = (string?)token;
                if (!string.IsNullOrEmpty(str)) return str;
            }
            return string.Empty;
        }

        private bool Bool(JObject obj, string name)
        {
            if (obj.TryGetValue(name, out var token))
            {
                return (bool)token;
            }
            return false;
        }

        private string ToDecInt(double d)
        {
            var i = new BigInteger(d);
            return i.ToString("D");
        }

        private string ToDecInt(TestToken t)
        {
            return t.TstWei.ToString("D");
        }

        private TestToken ToTestToken(string s)
        {
            return new TestToken(ToBigInt(s));
        }

        private long ToLong(double value)
        {
            return Convert.ToInt64(value);
        }

        private BigInteger ToBigInt(string tokens)
        {
            return BigInteger.Parse(tokens);
        }

        private TimeSpan ToTimespan(long duration)
        {
            return TimeSpan.FromSeconds(duration);
        }

        private ByteSize ToByteSize(long size)
        {
            return new ByteSize(size);
        }
    }
}
