using LogosStorageClient;
using LogosStorageTests;
using NUnit.Framework;
using Utils;

namespace ExperimentalTests.BasicTests
{
    [TestFixture]
    public class PyramidTests : LogosStorageDistTest
    {
        [Test]
        [CreateTranscript(nameof(PyramidTest))]
        public void PyramidTest()
        {
            var size = 5.MB();
            var numberOfLayers = 3;

            var bottomLayer = StartLayers(numberOfLayers);

            var cids = UploadFiles(bottomLayer, size);

            DownloadAllFilesFromEachNodeInLayer(bottomLayer, cids);
        }

        private List<IStorageNode> StartLayers(int numberOfLayers)
        {
            var layer = new List<IStorageNode>();
            layer.Add(StartLogosStorage(s => s.WithName("Top")));

            for (var i = 0; i < numberOfLayers; i++)
            {
                var newLayer = new List<IStorageNode>();
                foreach (var node in layer)
                {
                    newLayer.AddRange(StartLogosStorage(2, s => s.WithBootstrapNode(node).WithName("Layer[" + i + "]")));
                }

                layer.Clear();
                layer.AddRange(newLayer);
            }

            return layer;
        }

        private ContentId[] UploadFiles(List<IStorageNode> layer, ByteSize size)
        {
            var uploadTasks = new List<Task<ContentId>>();
            foreach (var node in layer)
            {
                uploadTasks.Add(Task.Run(() =>
                {
                    var file = GenerateTestFile(size);
                    return node.UploadFile(file);
                }));
            }

            var cids = uploadTasks.Select(t =>
            {
                t.Wait();
                return t.Result;
            }).ToArray();

            return cids;
        }

        private void DownloadAllFilesFromEachNodeInLayer(List<IStorageNode> layer, ContentId[] cids)
        {
            var downloadTasks = new List<Task>();
            foreach (var node in layer)
            {
                downloadTasks.Add(Task.Run(() =>
                {
                    var dlCids = RandomUtils.Shuffled(cids);
                    foreach (var cid in dlCids)
                    {
                        node.DownloadContent(cid);
                    }
                }));
            }

            Task.WaitAll(downloadTasks.ToArray());
        }
    }
}
