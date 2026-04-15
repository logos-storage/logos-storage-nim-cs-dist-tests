using LogosStorageClient;
using LogosStorageTests;
using FileUtils;
using NUnit.Framework;
using System.Diagnostics;
using Utils;

namespace LogosStorageReleaseTests.DataTests
{
    public class InterruptUploadTest : LogosStorageDistTest
    {
        [Test]
        public void UploadInterruptTest()
        {
            var nodes = StartLogosStorage(10);

            var tasks = nodes.Select(n => Task<bool>.Run(() => RunInterruptUploadTest(n)));
            Task.WaitAll(tasks.ToArray());

            Assert.That(tasks.Select(t => t.Result).All(r => r == true));

            WaitAndCheckNodesStaysAlive(TimeSpan.FromMinutes(2), nodes);
        }

        private bool RunInterruptUploadTest(IStorageNode node)
        {
            var file = GenerateTestFile(300.MB());

            var process = StartCurlUploadProcess(node, file);

            Thread.Sleep(500);
            process.Kill();
            Thread.Sleep(1000);

            var log = node.DownloadLog();
            return !log.GetLinesContaining("Unhandled exception in async proc, aborting").Any();
        }

        private Process StartCurlUploadProcess(IStorageNode node, TrackedFile file)
        {
            var apiAddress = node.GetApiEndpoint();
            var codexUrl = $"{apiAddress}/api/storage/v1/data";
            var filePath = file.Filename;
            return Process.Start("curl", $"-X POST {codexUrl} -H \"Content-Type: application/octet-stream\" -T {filePath}");
        }
    }
}
