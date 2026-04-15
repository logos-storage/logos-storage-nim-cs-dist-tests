using Utils;

namespace LogosStorageClient.Hooks
{
    public interface IStorageNodeHooks
    {
        void OnNodeStarting(DateTime startUtc, string image);
        void OnNodeStarted(IStorageNode node, string peerId, string nodeId);
        void OnNodeStopping();
        void OnFileUploading(string uid, ByteSize size);
        void OnFileUploaded(string uid, ByteSize size, ContentId cid);
        void OnFileDownloading(ContentId cid);
        void OnFileDownloaded(ByteSize size, ContentId cid);
    }

    public class MuxingStorageNodeHooks : IStorageNodeHooks
    {
        private readonly IStorageNodeHooks[] backingHooks;

        public MuxingStorageNodeHooks(IStorageNodeHooks[] backingHooks)
        {
            this.backingHooks = backingHooks;
        }

        public void OnFileDownloaded(ByteSize size, ContentId cid)
        {
            foreach (var h in backingHooks) h.OnFileDownloaded(size, cid);
        }

        public void OnFileDownloading(ContentId cid)
        {
            foreach (var h in backingHooks) h.OnFileDownloading(cid);
        }

        public void OnFileUploaded(string uid, ByteSize size, ContentId cid)
        {
            foreach (var h in backingHooks) h.OnFileUploaded(uid, size, cid);
        }

        public void OnFileUploading(string uid, ByteSize size)
        {
            foreach (var h in backingHooks) h.OnFileUploading(uid, size);
        }

        public void OnNodeStarted(IStorageNode node, string peerId, string nodeId)
        {
            foreach (var h in backingHooks) h.OnNodeStarted(node, peerId, nodeId);
        }

        public void OnNodeStarting(DateTime startUtc, string image)
        {
            foreach (var h in backingHooks) h.OnNodeStarting(startUtc, image);
        }

        public void OnNodeStopping()
        {
            foreach (var h in backingHooks) h.OnNodeStopping();
        }
    }
}
