using LogosStorageClient;
using LogosStorageClient.Hooks;
using OverwatchTranscript;
using Utils;

namespace StoragePlugin.OverwatchSupport
{
    public class StorageNodeTranscriptWriter : IStorageNodeHooks
    {
        private readonly ITranscriptWriter writer;
        private readonly IdentityMap identityMap;
        private readonly string name;
        private int identityIndex = -1;
        private readonly List<(DateTime, OverwatchLogosStorageEvent)> pendingEvents = new List<(DateTime, OverwatchLogosStorageEvent)>();

        public StorageNodeTranscriptWriter(ITranscriptWriter writer, IdentityMap identityMap, string name)
        {
            this.writer = writer;
            this.identityMap = identityMap;
            this.name = name;
        }

        public void OnNodeStarting(DateTime startUtc, string image)
        {
            WriteLogosStorageEvent(startUtc, e =>
            {
                e.NodeStarting = new NodeStartingEvent
                {
                    Image = image,
                };
            });
        }

        public void OnNodeStarted(IStorageNode node, string peerId, string nodeId)
        {
            if (string.IsNullOrEmpty(peerId) || string.IsNullOrEmpty(nodeId))
            {
                throw new Exception("Node started - peerId and/or nodeId unknown.");
            }

            identityMap.Add(name, peerId, nodeId);
            identityIndex = identityMap.GetIndex(name);

            WriteLogosStorageEvent(e =>
            {
                e.NodeStarted = new NodeStartedEvent
                {
                };
            });
        }

        public void OnNodeStopping()
        {
            WriteLogosStorageEvent(e =>
            {
                e.NodeStopping = new NodeStoppingEvent
                {
                };
            });
        }

        public void OnFileDownloading(ContentId cid)
        {
            WriteLogosStorageEvent(e =>
            {
                e.FileDownloading = new FileDownloadingEvent
                {
                    Cid = cid.Id
                };
            });
        }

        public void OnFileDownloaded(ByteSize size, ContentId cid)
        {
            WriteLogosStorageEvent(e =>
            {
                e.FileDownloaded = new FileDownloadedEvent
                {
                    Cid = cid.Id,
                    ByteSize = size.SizeInBytes
                };
            });
        }

        public void OnFileUploading(string uid, ByteSize size)
        {
            WriteLogosStorageEvent(e =>
            {
                e.FileUploading = new FileUploadingEvent
                {
                    UniqueId = uid,
                    ByteSize = size.SizeInBytes
                };
            });
        }

        public void OnFileUploaded(string uid, ByteSize size, ContentId cid)
        {
            WriteLogosStorageEvent(e =>
            {
                e.FileUploaded = new FileUploadedEvent
                { 
                    UniqueId = uid,
                    Cid = cid.Id,
                    ByteSize = size.SizeInBytes
                };
            });
        }

        private void WriteLogosStorageEvent(Action<OverwatchLogosStorageEvent> action)
        {
            WriteLogosStorageEvent(DateTime.UtcNow, action);
        }

        private void WriteLogosStorageEvent(DateTime utc, Action<OverwatchLogosStorageEvent> action)
        {
            var e = new OverwatchLogosStorageEvent
            {
                NodeIdentity = identityIndex
            };

            action(e);

            if (identityIndex < 0)
            {
                // If we don't know our id, don't write the events yet.
                AddToCache(utc, e);
            }
            else
            {
                e.Write(utc, writer);

                // Write any events that we cached when we didn't have our id yet.
                WriteAndClearCache();
            }
        }

        private void AddToCache(DateTime utc, OverwatchLogosStorageEvent e)
        {
            pendingEvents.Add((utc, e));
        }

        private void WriteAndClearCache()
        {
            if (pendingEvents.Any())
            {
                foreach (var pair in pendingEvents)
                {
                    pair.Item2.NodeIdentity = identityIndex;
                    pair.Item2.Write(pair.Item1, writer);
                }
                pendingEvents.Clear();
            }
        }
    }
}
