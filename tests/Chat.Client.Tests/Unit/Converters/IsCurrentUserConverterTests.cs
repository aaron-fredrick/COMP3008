using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chat.Client.Shared.Converters;

namespace Chat.Client.Tests.Unit.Converters
{
    [TestClass]
    public class IsCurrentUserConverterTests
    {
        private readonly IsCurrentUserConverter _converter = new IsCurrentUserConverter();

        [TestMethod]
        [TestCategory("Unit")]
        public void SameUserId_ReturnsTrue()
        {
            var result = _converter.Convert(new object[] { "alice", "alice" }, typeof(bool), null, CultureInfo.InvariantCulture);
            Assert.IsTrue((bool)result);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void DifferentUserIds_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "alice", "bob" }, typeof(bool), null, CultureInfo.InvariantCulture);
            Assert.IsFalse((bool)result);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ComparisonIsCaseInsensitive()
        {
            var result = _converter.Convert(new object[] { "Alice", "alice" }, typeof(bool), null, CultureInfo.InvariantCulture);
            Assert.IsTrue((bool)result, "Comparison must be case-insensitive");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NullValue_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { null, "alice" }, typeof(bool), null, CultureInfo.InvariantCulture);
            Assert.IsFalse((bool)result);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void InsufficientValues_ReturnsFalse()
        {
            var result = _converter.Convert(new object[] { "alice" }, typeof(bool), null, CultureInfo.InvariantCulture);
            Assert.IsFalse((bool)result);
        }
    }
}
