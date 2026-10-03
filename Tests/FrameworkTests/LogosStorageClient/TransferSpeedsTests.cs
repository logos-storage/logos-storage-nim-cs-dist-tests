using NUnit.Framework;
using LogosStorageClient;
using Utils;

namespace FrameworkTests.LogosStorageClient
{
    [TestFixture]
    public class TransferSpeedsTests
    {
        private TransferSpeeds speeds;

        [SetUp]
        public void SetUp()
        {
            speeds = new TransferSpeeds();
        }

        [Test]
        public void GetUploadSpeed_NoSamples_ReturnsNull()
        {
            Assert.That(speeds.GetUploadSpeed(), Is.Null);
        }

        [Test]
        public void GetDownloadSpeed_NoSamples_ReturnsNull()
        {
            Assert.That(speeds.GetDownloadSpeed(), Is.Null);
        }

        [Test]
        public void GetUploadSpeed_OneSample_ReturnsCorrectSpeed()
        {
            // 1000 bytes in 2 seconds = 500 bytes/sec
            speeds.AddUploadSample(new ByteSize(1000), TimeSpan.FromSeconds(2));
            Assert.That(speeds.GetUploadSpeed()!.SizeInBytes, Is.EqualTo(500));
        }

        [Test]
        public void GetDownloadSpeed_OneSample_ReturnsCorrectSpeed()
        {
            // 3000 bytes in 3 seconds = 1000 bytes/sec
            speeds.AddDownloadSample(new ByteSize(3000), TimeSpan.FromSeconds(3));
            Assert.That(speeds.GetDownloadSpeed()!.SizeInBytes, Is.EqualTo(1000));
        }

        [Test]
        public void GetUploadSpeed_MultipleSamples_ReturnsAverage()
        {
            // 1000/1s = 1000, 3000/3s = 1000 → avg = 1000
            speeds.AddUploadSample(new ByteSize(1000), TimeSpan.FromSeconds(1));
            speeds.AddUploadSample(new ByteSize(3000), TimeSpan.FromSeconds(3));
            Assert.That(speeds.GetUploadSpeed()!.SizeInBytes, Is.EqualTo(1000));
        }

        [Test]
        public void UploadAndDownload_Independent()
        {
            speeds.AddUploadSample(new ByteSize(2000), TimeSpan.FromSeconds(1));
            speeds.AddDownloadSample(new ByteSize(500), TimeSpan.FromSeconds(1));

            Assert.That(speeds.GetUploadSpeed()!.SizeInBytes, Is.EqualTo(2000));
            Assert.That(speeds.GetDownloadSpeed()!.SizeInBytes, Is.EqualTo(500));
        }

        [Test]
        public void Combine_WithNull_ReturnsSelf()
        {
            speeds.AddUploadSample(new ByteSize(1000), TimeSpan.FromSeconds(1));
            var combined = speeds.Combine(null);
            Assert.That(combined.GetUploadSpeed()!.SizeInBytes, Is.EqualTo(1000));
        }

        [Test]
        public void Combine_TwoSpeeds_MergesAndAverages()
        {
            speeds.AddUploadSample(new ByteSize(1000), TimeSpan.FromSeconds(1)); // 1000 b/s
            var other = new TransferSpeeds();
            other.AddUploadSample(new ByteSize(3000), TimeSpan.FromSeconds(1)); // 3000 b/s

            var combined = speeds.Combine(other);
            // avg(1000, 3000) = 2000
            Assert.That(combined.GetUploadSpeed()!.SizeInBytes, Is.EqualTo(2000));
        }
    }
}
