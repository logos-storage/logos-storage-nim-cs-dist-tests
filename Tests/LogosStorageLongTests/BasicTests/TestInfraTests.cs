using LogosStorageTests;
using DistTestCore;
using NUnit.Framework;

namespace LogosStorageLongTests.BasicTests
{
    public class TestInfraTests : LogosStorageDistTest
    {
        [Test]
        [UseLongTimeouts]
        [Ignore("Not supported atm")]
        public void TestInfraShouldHave1000AddressSpacesPerPod()
        {
            var group = StartLogosStorage(1000, s => s.EnableMetrics());

            var nodeIds = group.Select(n => n.GetDebugInfo().Id).ToArray();

            Assert.That(nodeIds.Length, Is.EqualTo(nodeIds.Distinct().Count()),
                "Not all created nodes provided a unique id.");
        }

        [Test]
        [UseLongTimeouts]
        [Ignore("Not supported atm")]
        public void TestInfraSupportsManyConcurrentPods()
        {
            for (var i = 0; i < 20; i++)
            {
                var n = StartLogosStorage();

                Assert.That(!string.IsNullOrEmpty(n.GetDebugInfo().Id));
            }
        }
    }
}
