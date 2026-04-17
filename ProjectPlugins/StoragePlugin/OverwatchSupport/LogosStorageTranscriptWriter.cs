using LogosStorageClient.Hooks;
using Logging;
using OverwatchTranscript;
using Utils;

namespace StoragePlugin.OverwatchSupport
{
    public class LogosStorageTranscriptWriter : ILogosStorageHooksProvider
    {
        private const string LogosStorageHeaderKey = "cdx_h";
        private readonly ILog log;
        private readonly LogosStorageTranscriptWriterConfig config;
        private readonly ITranscriptWriter writer;
        private readonly LogosStorageLogConverter converter;
        private readonly IdentityMap identityMap = new IdentityMap();
        private readonly KademliaPositionFinder positionFinder = new KademliaPositionFinder();

        public LogosStorageTranscriptWriter(ILog log, LogosStorageTranscriptWriterConfig config, ITranscriptWriter transcriptWriter)
        {
            this.log = log;
            this.config = config;
            writer = transcriptWriter;
            converter = new LogosStorageLogConverter(writer, config, identityMap);
        }

        public void FinalizeWriter()
        {
            log.Log("Finalizing Logos Storage transcript...");

            writer.AddHeader(LogosStorageHeaderKey, CreateLogosStorageHeader());
            writer.Write(GetOutputFullPath());

            log.Log("Done");
        }

        private string GetOutputFullPath()
        {
            var outputPath = Path.GetDirectoryName(log.GetFullName());
            if (outputPath == null) throw new Exception("Logfile path is null");
            var filename = Path.GetFileNameWithoutExtension(log.GetFullName());
            if (string.IsNullOrEmpty(filename)) throw new Exception("Logfile name is null or empty");
            var outputFile = Path.Combine(outputPath, filename + "_" + config.OutputPath);
            if (!outputFile.EndsWith(".owts")) outputFile += ".owts";
            return outputFile;
        }

        public IStorageNodeHooks CreateHooks(string nodeName)
        {
            nodeName = Str.Between(nodeName, "'", "'");
            return new StorageNodeTranscriptWriter(writer, identityMap, nodeName);
        }

        public void IncludeFile(string filepath)
        {
            writer.IncludeArtifact(filepath);   
        }

        public void ProcessLogs(IDownloadedLog[] downloadedLogs)
        {
            foreach (var l in downloadedLogs)
            {
                log.Log("Include artifact: " + l.GetFilepath());
                writer.IncludeArtifact(l.GetFilepath());

                // Not all of these logs are necessarily Codex logs.
                // Check, and process only the Codex ones.
                if (IsLogosStorageLog(l))
                {
                    log.Log("Processing Logos Storage log: " + l.GetFilepath());
                    converter.ProcessLog(l);
                }
            }
        }

        public void AddResult(bool success, string result)
        {
            writer.Add(DateTime.UtcNow, new OverwatchLogosStorageEvent
            {
                NodeIdentity = -1,
                ScenarioFinished = new ScenarioFinishedEvent
                {
                    Success = success,
                    Result = result
                }
            });
        }

        private OverwatchLogosStorageHeader CreateLogosStorageHeader()
        {
            return new OverwatchLogosStorageHeader
            {
                Nodes = positionFinder.DeterminePositions(identityMap.Get())
            };
        }

        private bool IsLogosStorageLog(IDownloadedLog log)
        {
            return log.GetLinesContaining("Run Logos Storage node").Any();
        }
    }
}
