using LogosStorageClient;
using Logging;
using static LogosStorageTests.Helpers.FullConnectivityHelper;

namespace LogosStorageTests.Helpers
{
    public class PeerConnectionTestHelpers : IFullConnectivityImplementation
    {
        private readonly FullConnectivityHelper helper;

        public PeerConnectionTestHelpers(ILog log)
        {
            helper = new FullConnectivityHelper(log, this);
        }

        public void AssertFullyConnected(IEnumerable<IStorageNode> nodes)
        {
            helper.AssertFullyConnected(nodes);
        }

        public string Description()
        {
            return "Peer Discovery";
        }

        public string ValidateEntry(Entry entry, Entry[] allEntries)
        {
            var result = string.Empty;
            foreach (var peer in entry.Response.Table.Nodes)
            {
                var known = allEntries.SingleOrDefault(e => e.Response.Table.LocalNode.PeerId == peer.PeerId);
                if (known == null) continue;

                var expected = known.Node.GetDiscoveryEndpoint().ToString();
                if (expected != peer.Address)
                {
                    result += $"Node:{entry.Node.GetName()} has incorrect peer table entry. Was: '{peer.Address}', expected: '{expected}'. ";
                }
            }
            return result;
        }

        public PeerConnectionState Check(Entry from, Entry to)
        {
            var peerId = to.Response.Id;

            var response = from.Node.GetDebugPeer(peerId);
            if (!response.IsPeerFound)
            {
                return PeerConnectionState.NoConnection;
            }
            if (!string.IsNullOrEmpty(response.PeerId) && response.Addresses.Any())
            {
                return PeerConnectionState.Connection;
            }
            return PeerConnectionState.Unknown;
        }
    }
}
