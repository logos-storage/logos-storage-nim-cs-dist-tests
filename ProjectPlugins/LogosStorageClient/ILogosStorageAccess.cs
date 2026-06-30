using Logging;
using Utils;

namespace LogosStorageClient
{
    public interface ILogosStorageAccess
    {
        string GetName();
        string GetImageName();
        DateTime GetStartUtc();
        DebugInfo GetDebugInfo();
        void SetLogLevel(string logLevel);
        string GetSpr();
        DebugPeer GetDebugPeer(string peerId);
        void ConnectToPeer(string peerId, string[] peerMultiAddresses);
        string UploadFile(UploadInput uploadInput);
        Stream DownloadFile(string contentId);
        LocalDataset DownloadStreamless(ContentId cid);
        LocalDataset DownloadManifestOnly(ContentId cid);
        LocalDatasetList LocalFiles();
        LogosStorageSpace Space();
        Address GetDiscoveryEndpoint();
        Address GetApiEndpoint();
        Address GetListenEndpoint();
        Address? GetMetricsEndpoint();
        bool HasCrashed();
        void DeleteDataDirFolder();
        void Stop(bool waitTillStopped);
        IDownloadedLog DownloadLog(string additionalName = "");
    }
}
