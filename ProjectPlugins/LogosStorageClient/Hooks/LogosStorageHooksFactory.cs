using Utils;

namespace LogosStorageClient.Hooks
{
    public interface ILogosStorageHooksProvider
    {
        IStorageNodeHooks CreateHooks(string nodeName);
    }

    public class LogosStorageHooksFactory
    {
        public List<ILogosStorageHooksProvider> Providers { get; } = new List<ILogosStorageHooksProvider>();

        public IStorageNodeHooks CreateHooks(string nodeName)
        {
            if (Providers.Count == 0) return new DoNothingLogosStorageHooks();

            var hooks = Providers.Select(p => p.CreateHooks(nodeName)).ToArray();
            return new MuxingStorageNodeHooks(hooks);
        }
    }

    public class DoNothingHooksProvider : ILogosStorageHooksProvider
    {
        public IStorageNodeHooks CreateHooks(string nodeName)
        {
            return new DoNothingLogosStorageHooks();
        }
    }

    public class DoNothingLogosStorageHooks : IStorageNodeHooks
    {
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
        }

        public void OnNodeStarting(DateTime startUtc, string image)
        {
        }

        public void OnNodeStopping()
        {
        }
    }
}
