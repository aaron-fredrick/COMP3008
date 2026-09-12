using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chat.Client.Shared.Converters;

namespace Chat.Client.Tests.Unit.Converters
{
    [TestClass]
    public class InitialsConverterTests
    {
        private readonly InitialsConverter _converter = new InitialsConverter();

        [TestMethod]
        [TestCategory("Unit")]
        public void SingleWord_ReturnsTwoCharInitials()
        {
            Assert.AreEqual("al", Convert("alice"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void TwoWords_ReturnsFirstLetterOfEach()
        {
            Assert.AreEqual("AB", Convert("Alice Bob"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SingleCharName_ReturnsSingleChar()
        {
            Assert.AreEqual("X", Convert("X"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NullOrEmpty_IsPassedThrough()
        {
            var nullResult = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.IsNull(nullResult);

            var emptyResult = _converter.Convert(string.Empty, typeof(string), null, CultureInfo.InvariantCulture) as string;
            Assert.AreEqual(string.Empty, emptyResult);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NonStringValue_IsPassedThrough()
        {
            var result = _converter.Convert(42, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(42, result);
        }

        private string Convert(string name)
            => _converter.Convert(name, typeof(string), null, CultureInfo.InvariantCulture) as string;
    }
}
