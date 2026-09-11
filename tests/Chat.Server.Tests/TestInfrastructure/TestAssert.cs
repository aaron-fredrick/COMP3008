using System;

namespace Chat.Server.Tests.TestInfrastructure
{
    internal static class TestAssert
    {
        public static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        public static void False(bool condition, string message)
        {
            True(!condition, message);
        }

        public static void Equal<T>(T expected, T actual, string message)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException($"{message}. Expected: {expected}; Actual: {actual}");
        }

        public static void Contains<T>(System.Collections.Generic.IEnumerable<T> values, Func<T, bool> predicate, string message)
        {
            foreach (var value in values)
                if (predicate(value)) return;
            throw new InvalidOperationException(message);
        }
    }
}
