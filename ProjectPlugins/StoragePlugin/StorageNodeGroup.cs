using LogosStorageClient;
using Core;
using System.Collections;
using Utils;

namespace StoragePlugin
{
    public interface IStorageNodeGroup : IEnumerable<IStorageNode>, IHasManyMetricScrapeTargets
    {
        void Stop(bool waitTillStopped);
        IStorageNode this[int index] { get; }
    }

    public class StorageNodeGroup : IStorageNodeGroup
    {
        private readonly IStorageNode[] nodes;

        public StorageNodeGroup(IPluginTools tools, IStorageNode[] nodes)
        {
            this.nodes = nodes;
            Version = new DebugInfoVersion();
        }

        public IStorageNode this[int index]
        {
            get
            {
                return Nodes[index];
            }
        }

        public void Stop(bool waitTillStopped)
        {
            foreach (var node in Nodes) node.Stop(waitTillStopped);
        }

        public void Stop(StorageNode node, bool waitTillStopped)
        {
            node.Stop(waitTillStopped);
        }

        public IStorageNode[] Nodes => nodes;
        public DebugInfoVersion Version { get; private set; }

        public Address[] GetMetricsScrapeTargets()
        {
            return Nodes.Select(n => n.GetMetricsScrapeTarget()).ToArray();
        }

        public IEnumerator<IStorageNode> GetEnumerator()
        {
            return Nodes.Cast<IStorageNode>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }

        public string Names()
        {
            return $"[{string.Join(",", Nodes.Select(n => n.GetName()))}]";
        }

        public override string ToString()
        {
            return Names();
        }

        public void EnsureOnline()
        {
            var versionResponses = Nodes.Select(n => n.Version);

            var first = versionResponses.First();
            if (!versionResponses.All(v => v.Version == first.Version && v.Revision == first.Revision))
            {
                throw new Exception("Inconsistent version information received from one or more Logos Storage nodes: " +
                    string.Join(",", versionResponses.Select(v => v.ToString())));
            }

            Version = first;
        }
    }

    public static class StorageNodeGroupExtensions
    {
        public static string Names(this IStorageNode[] nodes)
        {
            return $"[{string.Join(",", nodes.Select(n => n.GetName()))}]";
        }

        public static string Names(this List<IStorageNode> nodes)
        {
            return $"[{string.Join(",", nodes.Select(n => n.GetName()))}]";
        }
    }
}
