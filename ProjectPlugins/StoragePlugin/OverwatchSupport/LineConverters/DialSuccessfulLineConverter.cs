using LogosStorageClient;

namespace StoragePlugin.OverwatchSupport.LineConverters
{
    public class DialSuccessfulLineConverter : ILineConverter
    {
        public string Interest => "Dial successful";

        public void Process(LogosStorageLogLine line, Action<Action<OverwatchLogosStorageEvent>> addEvent)
        {
            var peerId = line.Attributes["peerId"];

            addEvent(e =>
            {
                e.DialSuccessful = new PeerDialSuccessfulEvent
                {
                    TargetPeerId = peerId
                };
            });
        }
    }
}
