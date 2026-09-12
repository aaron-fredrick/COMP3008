using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chat.Client.Shared.Converters;

namespace Chat.Client.Tests.Unit.Converters
{
    [TestClass]
    public class FileSizeConverterTests
    {
        private readonly FileSizeConverter _converter = new FileSizeConverter();

        [TestMethod]
        [TestCategory("Unit")]
        public void ByteRange_ShowsBytes()
        {
            Assert.AreEqual("0 B", Convert(0L));
            Assert.AreEqual("512 B", Convert(512L));
            Assert.AreEqual("1023 B", Convert(1023L));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void KilobyteRange_ShowsKb()
        {
            var result = Convert(1024L);
            StringAssert.Contains(result, "kB");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void MegabyteRange_ShowsMb()
        {
            var result = Convert(1024L * 1024);
            StringAssert.Contains(result, "MB");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void TwoMegabytes_ShowsMb()
        {
            var result = Convert(2L * 1024 * 1024);
            Assert.AreEqual("2 MB", result);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NonLongValue_IsPassedThrough()
        {
            // Non-long input should be returned as-is (no crash)
            var result = _converter.Convert("not a long", typeof(string), null, CultureInfo.InvariantCulture);
            Assert.AreEqual("not a long", result);
        }

        private string Convert(long bytes)
            => _converter.Convert(bytes, typeof(string), null, CultureInfo.InvariantCulture) as string;
    }
}
