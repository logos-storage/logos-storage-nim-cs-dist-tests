using System.Threading;
using StorageOpenApi;
using Logging;
using Newtonsoft.Json;
using Utils;
using WebUtils;

namespace LogosStorageClient
{
    public class LogosStorageAccess
    {
        public const string API_BASE_URL = "/api/storage/v1";

        private readonly ILog log;
        private readonly IHttpFactory httpFactory;
        private readonly IProcessControl processControl;
        private readonly ILogosStorageInstance instance;
        private readonly Mapper mapper = new Mapper();

        public LogosStorageAccess(ILog log, IHttpFactory httpFactory, IProcessControl processControl, ILogosStorageInstance instance)
        {
            this.log = log;
            this.httpFactory = httpFactory;
            this.processControl = processControl;
            this.instance = instance;
        }

        public void Stop(bool waitTillStopped)
        {
            processControl.Stop(waitTillStopped);
        }

        public IDownloadedLog DownloadLog(string additionalName = "")
        {
            var file = log.CreateSubfile(GetName() + additionalName);
            Log($"Downloading logs to '{file.Filename}'");
            return processControl.DownloadLog(file);
        }

        public string GetImageName()
        {
            return instance.ImageName;
        }

        public DateTime GetStartUtc()
        {
            return instance.StartUtc;
        }

        public DebugInfo GetDebugInfo()
        {
            return mapper.Map(OnLogosStorage(api => api.GetDebugInfoAsync()));
        }

        public void SetLogLevel(string logLevel)
        {
            try
            {
                OnLogosStorage(async api =>
                {
                    await api.SetDebugLogLevelAsync(logLevel);
                    return string.Empty;
                });
            }
            catch (Exception exc)
            {
                log.Error("Failed to set log level: " + exc);
            }
        }

        public string GetSpr()
        {
            return CrashCheck(() =>
            {
                var endpoint = GetEndpoint();
                var json = endpoint.HttpGetString("spr");
                var response = JsonConvert.DeserializeObject<SprResponse>(json);
                return response!.Spr;
            });
        }

        private class SprResponse
        {
            public string Spr { get; set; } = string.Empty;
        }

        public DebugPeer GetDebugPeer(string peerId)
        {
            // Cannot use openAPI: debug/peer endpoint is not specified there.
            return CrashCheck(() =>
            {
                var endpoint = GetEndpoint();
                var str = endpoint.HttpGetString($"debug/peer/{peerId}");

                if (str.ToLowerInvariant() == "unable to find peer!")
                {
                    return new DebugPeer
                    {
                        IsPeerFound = false
                    };
                }

                var result = endpoint.Deserialize<DebugPeer>(str);
                result.IsPeerFound = true;
                return result;
            });
        }

        public void ConnectToPeer(string peerId, string[] peerMultiAddresses)
        {
            OnLogosStorage(api =>
            {
                Time.Wait(api.ConnectPeerAsync(peerId, peerMultiAddresses));
                return Task.FromResult(string.Empty);
            });
        }

        public string UploadFile(UploadInput uploadInput)
        {
            return OnLogosStorage(api => api.UploadAsync(uploadInput.ContentType, uploadInput.ContentDisposition, uploadInput.FileStream));
        }

        public Stream DownloadFile(string contentId)
        {
            var fileResponse = OnLogosStorageNoRetry(api => api.DownloadNetworkStreamAsync(contentId));
            if (fileResponse.StatusCode != 200) throw new Exception("Download failed with StatusCode: " + fileResponse.StatusCode);
            return fileResponse.Stream;
        }

        public LocalDataset DownloadStreamless(ContentId cid)
        {
            var response = OnLogosStorage(api => api.DownloadNetworkAsync(cid.Id));
            return mapper.Map(response);
        }

        public LocalDataset DownloadManifestOnly(ContentId cid)
        {
            var response = OnLogosStorage(api => api.DownloadNetworkManifestAsync(cid.Id));
            return mapper.Map(response);
        }

        public LocalDatasetList LocalFiles()
        {
            return mapper.Map(OnLogosStorage(api => api.ListDataAsync()));
        }

        public LogosStorageSpace Space()
        {
            var space = OnLogosStorage(api => api.SpaceAsync());
            return mapper.Map(space);
        }

        public string GetName()
        {
            return instance.Name;
        }

        public Address GetDiscoveryEndpoint()
        {
            return instance.DiscoveryEndpoint;
        }

        public Address GetApiEndpoint()
        {
            return instance.ApiEndpoint;
        }

        public Address GetListenEndpoint()
        {
            return instance.ListenEndpoint;
        }

        public bool HasCrashed()
        {
            return processControl.HasCrashed();
        }

        public Address? GetMetricsEndpoint()
        {
            return instance.MetricsEndpoint;
        }

        public void DeleteDataDirFolder()
        {
            processControl.DeleteDataDirFolder();
        }

        private T OnLogosStorageNoRetry<T>(Func<StorageApiClient, Task<T>> action)
        {
            var timeSet = httpFactory.WebCallTimeSet;
            var noRetry = new Retry(nameof(OnLogosStorageNoRetry),
                maxTimeout: TimeSpan.FromSeconds(1.0),
                sleepAfterFail: TimeSpan.FromSeconds(2.0),
                onFail: f => { },
                failFast: true);

            var result = httpFactory.CreateHttp(GetHttpId(), h => CheckContainerCrashed()).OnClient(client => CallLogosStorage(client, action), noRetry);
            return result;
        }

        private T OnLogosStorage<T>(Func<StorageApiClient, Task<T>> action)
        {
            var result = httpFactory.CreateHttp(GetHttpId(), h => CheckContainerCrashed()).OnClient(client => CallLogosStorage(client, action));
            return result;
        }

        private T CallLogosStorage<T>(HttpClient client, Func<StorageApiClient, Task<T>> action)
        {
            var address = GetAddress();
            var api = new StorageApiClient(client);
            api.BaseUrl = $"{address.Host}:{address.Port}{API_BASE_URL}";
            return CrashCheck(() => Time.Wait(action(api)));
        }

        private T CrashCheck<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            finally
            {
                CheckContainerCrashed();
            }
        }

        private IEndpoint GetEndpoint()
        {
            return httpFactory
                .CreateHttp(GetHttpId(), h => CheckContainerCrashed())
                .CreateEndpoint(GetAddress(), $"{API_BASE_URL}/", GetName());
        }

        private Address GetAddress()
        {
            return instance.ApiEndpoint;
        }

        private string GetHttpId()
        {
            return GetAddress().ToString();
        }

        private void CheckContainerCrashed()
        {
            if (processControl.HasCrashed()) throw new Exception($"Container {GetName()} has crashed.");
        }

        private void Throw(Failure failure)
        {
            throw failure.Exception;
        }

        private void Log(string msg)
        {
            log.Log($"({GetName()}) {msg}");
        }
    }

    public class UploadInput
    {
        public UploadInput(string contentType, string contentDisposition, FileStream fileStream)
        {
            ContentType = contentType;
            ContentDisposition = contentDisposition;
            FileStream = fileStream;
        }

        public string ContentType { get; }
        public string ContentDisposition { get; }
        public FileStream FileStream { get; }
    }
}
