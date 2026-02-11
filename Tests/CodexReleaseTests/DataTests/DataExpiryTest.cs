using CodexPlugin;
using CodexTests;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.DataTests
{
    [TestFixture]
    public class DataExpiryTest : CodexDistTest
    {
        private readonly TimeSpan blockTtl = TimeSpan.FromMinutes(1.0);
        private readonly TimeSpan blockInterval = TimeSpan.FromSeconds(10.0);
        private readonly int blockCount = 100000;

        private ICodexSetup WithFastBlockExpiry(ICodexSetup setup)
        {
            return setup
                .WithBlockTTL(blockTtl)
                .WithBlockMaintenanceInterval(blockInterval)
                .WithBlockMaintenanceNumber(blockCount);
        }

        [Test]
        public void DeletesExpiredData()
        {
            var fileSize = 3.MB();
            var node = StartCodex(s => WithFastBlockExpiry(s));

            var startSpace = node.Space();
            Assert.That(startSpace.QuotaUsedBytes, Is.EqualTo(0));

            node.UploadFile(GenerateTestFile(fileSize));
            var usedSpace = node.Space();
            var usedFiles = node.LocalFiles();
            Assert.That(usedSpace.QuotaUsedBytes, Is.GreaterThanOrEqualTo(fileSize.SizeInBytes));
            Assert.That(usedSpace.FreeBytes, Is.LessThanOrEqualTo(startSpace.FreeBytes - fileSize.SizeInBytes));
            Assert.That(usedFiles.Content.Length, Is.EqualTo(1));

            Thread.Sleep(blockTtl * 2);

            var cleanupSpace = node.Space();
            var cleanupFiles = node.LocalFiles();

            Assert.That(cleanupSpace.QuotaUsedBytes, Is.LessThan(usedSpace.QuotaUsedBytes));
            Assert.That(cleanupSpace.FreeBytes, Is.GreaterThan(usedSpace.FreeBytes));
            Assert.That(cleanupFiles.Content.Length, Is.EqualTo(0));

            Assert.That(cleanupSpace.QuotaUsedBytes, Is.EqualTo(startSpace.QuotaUsedBytes));
            Assert.That(cleanupSpace.FreeBytes, Is.EqualTo(startSpace.FreeBytes));
        }
    }
}
