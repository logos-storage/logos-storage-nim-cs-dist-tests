using LogosStorageClient;
using LogosStorageTests;
using NUnit.Framework;

namespace LogosStorageReleaseTests.DataTests
{
    [TestFixture]
    public class UnknownCidTest : LogosStorageDistTest
    {
        [Test]
        public void DownloadingUnknownCidDoesNotCauseCrash()
        {
            var node = StartLogosStorage();

            var unknownCid = new ContentId("zDvZRwzkzHsok3Z8yMoiXE9EDBFwgr8WygB8s4ddcLzzSwwXAxLZ");

            var localFiles = node.LocalFiles().Content;
            CollectionAssert.DoesNotContain(localFiles.Select(f => f.Cid), unknownCid);

            try
            {
                node.DownloadContent(unknownCid, TimeSpan.FromMinutes(2.0));
            }
            catch (Exception ex)
            {
                var expectedMessage = $"Download of '{unknownCid.Id}' timed out";
                if (!ex.Message.StartsWith(expectedMessage)) throw;
            }

            WaitAndCheckNodesStaysAlive(TimeSpan.FromMinutes(2), node);
        }
    }
}
