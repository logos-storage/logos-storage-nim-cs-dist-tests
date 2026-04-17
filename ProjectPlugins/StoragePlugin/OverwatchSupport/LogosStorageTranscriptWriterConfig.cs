namespace StoragePlugin.OverwatchSupport
{
    public class LogosStorageTranscriptWriterConfig
    {
        public LogosStorageTranscriptWriterConfig(string outputPath, bool includeBlockReceivedEvents)
        {
            OutputPath = outputPath;
            IncludeBlockReceivedEvents = includeBlockReceivedEvents;
        }

        public string OutputPath { get; }
        public bool IncludeBlockReceivedEvents { get; }
    }
}
