using NUnit.Framework;
using LogosStorageClient;

namespace FrameworkTests.LogosStorageClient
{
    [TestFixture]
    public class LogosStorageLogLineTests
    {
        [Test]
        public void Parse_NullOrEmpty_ReturnsNull()
        {
            Assert.That(LogosStorageLogLine.Parse(string.Empty), Is.Null);
            Assert.That(LogosStorageLogLine.Parse(null!), Is.Null);
        }

        [Test]
        public void Parse_TooShort_ReturnsNull()
        {
            Assert.That(LogosStorageLogLine.Parse("INF 2024-01-01 00:00:00.000"), Is.Null);
        }

        [Test]
        public void Parse_ValidLine_ReturnsCorrectFields()
        {
            // Real Nim log format: LVL yyyy-MM-dd HH:mm:ss.fff+00:00 message attrs
            // position 3 = space after level, position 33 = space before message
            var line = "INF 2024-03-15 12:34:56.789+00:00 Node started successfully topic=storage";

            var result = LogosStorageLogLine.Parse(line);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.LogLevel, Is.EqualTo("INF"));
            Assert.That(result.TimestampUtc, Is.EqualTo(new DateTime(2024, 3, 15, 12, 34, 56, 789, DateTimeKind.Utc)));
            Assert.That(result.Message, Is.EqualTo("Node started successfully"));
        }

        [Test]
        public void Parse_SimpleAttribute_ParsedCorrectly()
        {
            var line = "ERR 2024-03-15 12:34:56.789+00:00 Upload failed reason=timeout";

            var result = LogosStorageLogLine.Parse(line);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Attributes.ContainsKey("reason"), Is.True);
            Assert.That(result.Attributes["reason"], Is.EqualTo("timeout"));
        }

        [Test]
        public void Parse_QuotedAttributeValue_ParsedCorrectly()
        {
            var line = "WRN 2024-03-15 12:34:56.789+00:00 Slow download path=\"/data dir/file.bin\"";

            var result = LogosStorageLogLine.Parse(line);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Attributes["path"], Is.EqualTo("/data dir/file.bin"));
        }

        [Test]
        public void Parse_MultipleAttributes_AllParsed()
        {
            var line = "DBG 2024-03-15 12:34:56.789+00:00 Peer discovered peerId=abc nodeId=xyz seen=true";

            var result = LogosStorageLogLine.Parse(line);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Attributes["peerId"], Is.EqualTo("abc"));
            Assert.That(result.Attributes["nodeId"], Is.EqualTo("xyz"));
            Assert.That(result.Attributes["seen"], Is.EqualTo("true"));
        }

        [Test]
        public void Parse_InvalidTimestamp_ReturnsNull()
        {
            // Passes position-3 and position-33 space checks, but the 23-char timestamp
            // substring (positions 4-26) is not a parseable datetime → returns null.
            // 29 X's fill positions 4-32, space at 33, then message with attributes.
            var line = "INF XXXXXXXXXXXXXXXXXXXXXXXXXXXXX message key=value";
            Assert.That(LogosStorageLogLine.Parse(line), Is.Null);
        }

        [Test]
        [TestCase("INF")]
        [TestCase("ERR")]
        [TestCase("WRN")]
        [TestCase("DBG")]
        public void Parse_LogLevel_IsPreserved(string level)
        {
            var line = $"{level} 2024-03-15 12:34:56.789+00:00 Message key=value";
            var result = LogosStorageLogLine.Parse(line);
            Assert.That(result?.LogLevel, Is.EqualTo(level));
        }
    }
}
