using LogosStorageClient;

namespace StoragePlugin.OverwatchSupport.LineConverters
{
    public class PeerDroppedLineConverter : ILineConverter
    {
        public string Interest => "Dropping peer";

        public void Process(LogosStorageLogLine line, Action<Action<OverwatchLogosStorageEvent>> addEvent)
        {
            var peerId = line.Attributes["peer"];

            addEvent(e =>
            {
                e.PeerDropped = new PeerDroppedEvent
                {
                    DroppedPeerId = peerId
                };
            });
        }
    }
}
