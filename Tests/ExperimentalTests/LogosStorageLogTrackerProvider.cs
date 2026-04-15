using LogosStorageClient;
using LogosStorageClient.Hooks;
using Utils;

namespace LogosStorageTests
{
    public class LogosStorageLogTrackerProvider  : ILogosStorageHooksProvider
    {
        private readonly Action<IStorageNode> addNode;

        public LogosStorageLogTrackerProvider(Action<IStorageNode> addNode)
        {
            this.addNode = addNode;
        }

        // See TestLifecycle.cs DownloadAllLogs()
        public IStorageNodeHooks CreateHooks(string nodeName)
        {
            return new LogosStorageLogTracker(addNode);
        }

        public class LogosStorageLogTracker : IStorageNodeHooks
        {
            private readonly Action<IStorageNode> addNode;

            public LogosStorageLogTracker(Action<IStorageNode> addNode)
            {
                this.addNode = addNode;
            }

            public void OnFileDownloaded(ByteSize size, ContentId cid)
            {
            }

            public void OnFileDownloading(ContentId cid)
            {
            }

            public void OnFileUploaded(string uid, ByteSize size, ContentId cid)
            {
            }

            public void OnFileUploading(string uid, ByteSize size)
            {
            }

            public void OnNodeStarted(IStorageNode node, string peerId, string nodeId)
            {
                addNode(node);
            }

            public void OnNodeStarting(DateTime startUtc, string image)
            {
            }

            public void OnNodeStopping()
            {
            }
        }
    }
}
