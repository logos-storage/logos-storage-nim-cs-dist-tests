using LogosStorageClient;
using LogosStorageTests;
using NUnit.Framework;
using Utils;

namespace LogosStorageReleaseTests.DataTests
{
    [TestFixture]
    public class OneClientTest : LogosStorageDistTest
    {
        [Test]
        public void OneClient()
        {
            var node = StartLogosStorage();

            PerformOneClientTest(node);

            LogNodeStatus(node);
        }

        private void PerformOneClientTest(IStorageNode primary)
        {
            var testFile = GenerateTestFile(1.MB());

            var contentId = primary.UploadFile(testFile);

            AssertNodesContainFile(contentId, primary);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
