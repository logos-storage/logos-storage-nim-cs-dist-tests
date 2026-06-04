using NUnit.Framework;
using LogosStorageClient;

namespace FrameworkTests.LogosStorageClient
{
    [TestFixture]
    public class DebugInfoVersionTests
    {
        [Test]
        public void IsValid_BothEmpty_ReturnsFalse()
        {
            var v = new DebugInfoVersion();
            Assert.That(v.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_VersionEmptyRevisionSet_ReturnsFalse()
        {
            var v = new DebugInfoVersion { Version = string.Empty, Revision = "abc123" };
            Assert.That(v.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_RevisionEmptyVersionSet_ReturnsFalse()
        {
            var v = new DebugInfoVersion { Version = "1.0.0", Revision = string.Empty };
            Assert.That(v.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_BothSet_ReturnsTrue()
        {
            var v = new DebugInfoVersion { Version = "1.0.0", Revision = "abc123" };
            Assert.That(v.IsValid(), Is.True);
        }
    }

    [TestFixture]
    public class LogosStorageSpaceTests
    {
        [Test]
        public void FreeBytes_IsQuotaMax_MinusUsedAndReserved()
        {
            var space = new LogosStorageSpace
            {
                QuotaMaxBytes = 1000,
                QuotaUsedBytes = 300,
                QuotaReservedBytes = 100
            };
            Assert.That(space.FreeBytes, Is.EqualTo(600));
        }

        [Test]
        public void FreeBytes_NothingUsed_EqualToMax()
        {
            var space = new LogosStorageSpace { QuotaMaxBytes = 5000, QuotaUsedBytes = 0, QuotaReservedBytes = 0 };
            Assert.That(space.FreeBytes, Is.EqualTo(5000));
        }

        [Test]
        public void FreeBytes_ReservedAloneReducesFreeSpace()
        {
            var space = new LogosStorageSpace { QuotaMaxBytes = 1000, QuotaUsedBytes = 0, QuotaReservedBytes = 400 };
            Assert.That(space.FreeBytes, Is.EqualTo(600));
        }
    }

    [TestFixture]
    public class LogosStorageUtilsTests
    {
        [Test]
        public void ToShortId_ShortString_ReturnedUnchanged()
        {
            Assert.That(LogosStorageUtils.ToShortId("abc"), Is.EqualTo("abc"));
        }

        [Test]
        public void ToShortId_ExactlyTenChars_ReturnedUnchanged()
        {
            Assert.That(LogosStorageUtils.ToShortId("abcde12345"), Is.EqualTo("abcde12345"));
        }

        [Test]
        public void ToShortId_LongString_ReturnsFirst3StarLast6()
        {
            // "abcde12345678xyz" (16 chars): id[..3]="abc", id[^6..]="678xyz"
            var result = LogosStorageUtils.ToShortId("abcde12345678xyz");
            Assert.That(result, Is.EqualTo("abc*678xyz"));
        }

        [Test]
        public void ToNodeIdShortId_ShortString_ReturnedUnchanged()
        {
            Assert.That(LogosStorageUtils.ToNodeIdShortId("abc"), Is.EqualTo("abc"));
        }

        [Test]
        public void ToNodeIdShortId_LongString_ReturnsFirst2StarLast6()
        {
            // "abcde12345678xyz" (16 chars): id[..2]="ab", id[^6..]="678xyz"
            var result = LogosStorageUtils.ToNodeIdShortId("abcde12345678xyz");
            Assert.That(result, Is.EqualTo("ab*678xyz"));
        }
    }
}
