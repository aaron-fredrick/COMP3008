using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chat.Client.Shared.Converters;

namespace Chat.Client.Tests.Unit.Converters
{
    [TestClass]
    public class FileNameExtractorConverterTests
    {
        private readonly FileNameExtractorConverter _converter = new FileNameExtractorConverter();

        [TestMethod]
        [TestCategory("Unit")]
        public void SharedFilePrefix_IsStripped()
        {
            var result = Convert("Shared file: report.txt");
            Assert.AreEqual("report.txt", result);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ContentWithoutPrefix_IsReturnedAsIs()
        {
            var result = Convert("just a normal message");
            Assert.AreEqual("just a normal message", result);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NullContent_IsPassedThrough()
        {
            var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.IsNull(result);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void EmptyContent_IsPassedThrough()
        {
            var result = Convert(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NonStringValue_IsPassedThrough()
        {
            var result = _converter.Convert(99, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(99, result);
        }

        private string Convert(string content)
            => _converter.Convert(content, typeof(string), null, CultureInfo.InvariantCulture) as string;
    }
}
