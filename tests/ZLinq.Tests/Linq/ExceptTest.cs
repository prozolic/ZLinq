﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZLinq;

namespace ZLinq.Tests.Linq
{
    public class ExceptTest
    {
        [Fact]
        public void BasicExcept()
        {
            // Arrange
            var first = new[] { 1, 2, 3, 4, 5 }.AsValueEnumerable();
            var second = new[] { 4, 5, 6, 7, 8 }.AsValueEnumerable();

            // Act
            var result = first.Except(second).ToArray();

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        [Fact]
        public void ExceptWithEmptySource()
        {
            // Arrange
            var first = Array.Empty<int>().AsValueEnumerable();
            var second = new[] { 1, 2, 3 }.AsValueEnumerable();

            // Act
            var result = first.Except(second).ToArray();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ExceptWithEmptySecond()
        {
            // Arrange
            var first = new[] { 1, 2, 3 }.AsValueEnumerable();
            var second = Array.Empty<int>().AsValueEnumerable();

            // Act
            var result = first.Except(second).ToArray();

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        [Fact]
        public void ExceptWithBothEmpty()
        {
            // Arrange
            var first = Array.Empty<int>().AsValueEnumerable();
            var second = Array.Empty<int>().AsValueEnumerable();

            // Act
            var result = first.Except(second).ToArray();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ExceptWithDuplicatesInSource()
        {
            // Arrange
            var first = new[] { 1, 1, 2, 3, 3 }.AsValueEnumerable();
            var second = new[] { 3, 5 }.AsValueEnumerable();

            // Act
            var result = first.Except(second).ToArray();

            // Assert
            Assert.Equal(new[] { 1, 2 }, result);
        }

        [Fact]
        public void ExceptWithDuplicatesInSecond()
        {
            // Arrange
            var first = new[] { 1, 2, 3 }.AsValueEnumerable();
            var second = new[] { 3, 3, 5, 5 }.AsValueEnumerable();

            // Act
            var result = first.Except(second).ToArray();

            // Assert
            Assert.Equal(new[] { 1, 2 }, result);
        }

        [Fact]
        public void ExceptWithCustomComparer()
        {
            // Arrange
            var first = new[] { "a", "B", "c" }.AsValueEnumerable();
            var second = new[] { "A", "d" }.AsValueEnumerable();

            // Act
            var result = first.Except(second, StringComparer.OrdinalIgnoreCase).ToArray();

            // Assert
            Assert.Equal(new[] { "B", "c" }, result);
        }

        [Fact]
        public void ExceptWithNullValues()
        {
            // Arrange
            var first = new[] { "a", null, "c" }.AsValueEnumerable();
            var second = new[] { null, "b", "d" }.AsValueEnumerable();

            // Act
            var result = first.Except(second).ToArray();

            // Assert
            Assert.Equal(new string?[] { "a", "c" }, result);
        }

        [Fact]
        public void ExceptPreservesOrderOfFirstSequence()
        {
            // Arrange
            var first = new[] { 5, 3, 1, 7, 9 }.AsValueEnumerable();
            var second = new[] { 3, 1, 8 }.AsValueEnumerable();

            // Act
            var result = first.Except(second).ToArray();

            // Assert
            Assert.Equal(new[] { 5, 7, 9 }, result);
        }

        [Fact]
        public void ExceptWithReferenceTypes()
        {
            // Arrange
            var person1 = new Person { Name = "Alice", Age = 30 };
            var person2 = new Person { Name = "Bob", Age = 25 };
            var person3 = new Person { Name = "Charlie", Age = 35 };

            var first = new[] { person1, person2, person3 }.AsValueEnumerable();

            var person1Copy = new Person { Name = "Alice", Age = 30 };
            var person3Copy = new Person { Name = "Charlie", Age = 35 };

            var second = new[] { person1Copy, person3Copy }.AsValueEnumerable();

            // Act
            var result = first.Except(second, new PersonEqualityComparer()).ToArray();

            // Assert
            Assert.Single(result);
            Assert.Equal("Bob", result[0].Name);
        }

        [Fact]
        public void ExceptWithIEnumerableSource()
        {
            // Arrange
            IEnumerable<int> first = new[] { 1, 2, 3, 4, 5 };
            var second = new[] { 4, 5, 6, 7, 8 }.AsValueEnumerable();

            // Act
            var result = first.AsValueEnumerable().Except(second).ToArray();

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        [Fact]
        public void ExceptWithIEnumerableSecond()
        {
            // Arrange
            var first = new[] { 1, 2, 3, 4, 5 }.AsValueEnumerable();
            IEnumerable<int> second = new[] { 4, 5, 6, 7, 8 };

            // Act
            var result = first.Except(second).ToArray();

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        [Fact]
        public void TryGetNonEnumeratedCountReturnsFalse()
        {
            // Arrange
            var first = new[] { 1, 2, 3 }.AsValueEnumerable();
            var second = new[] { 2, 3, 4 }.AsValueEnumerable();

            // Act
            var except = first.Except(second);
            bool result = except.TryGetNonEnumeratedCount(out int count);

            // Assert
            Assert.False(result);
            Assert.Equal(0, count);
        }

        [Fact]
        public void TryGetSpanReturnsFalse()
        {
            // Arrange
            var first = new[] { 1, 2, 3 }.AsValueEnumerable();
            var second = new[] { 2, 3, 4 }.AsValueEnumerable();

            // Act
            var except = first.Except(second);
            bool result = except.TryGetSpan(out ReadOnlySpan<int> span);

            // Assert
            Assert.False(result);
            Assert.True(span.IsEmpty);
        }

        [Fact]
        public void TryCopyToReturnsFalse()
        {
            // Arrange
            var first = new[] { 1, 2, 3 }.AsValueEnumerable();
            var second = new[] { 2, 3, 4 }.AsValueEnumerable();
            var except = first.Except(second);
            var destination = new int[3];

            // Act
            bool result = except.TryCopyTo(destination, 0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ExceptWithLargeSequences()
        {
            // Arrange
            var first = Enumerable.Range(0, 1000).AsValueEnumerable();
            var second = Enumerable.Range(500, 1000).AsValueEnumerable();

            // Act
            var result = first.Except(second).ToArray();

            // Assert
            Assert.Equal(500, result.Length);
            Assert.Equal(Enumerable.Range(0, 500), result);
        }

        [Fact]
        public void ConsistentWithSystemLinq()
        {
            // Arrange
            var first = new[] { 1, 2, 3, 4, 5, 1, 2 };
            var second = new[] { 3, 4, 9, 10 };

            // Act
            var systemResult = first.Except(second).ToArray();
            var zlinqResult = first.AsValueEnumerable().Except(second).ToArray();

            // Assert
            Assert.Equal(systemResult, zlinqResult);
        }

        [Fact]
        public void ExceptWithAllElementsInSecond()
        {
            // Arrange
            var first = new[] { 1, 2, 3 }.AsValueEnumerable();
            var second = new[] { 1, 2, 3, 4, 5 }.AsValueEnumerable();

            // Act
            var result = first.Except(second).ToArray();

            // Assert
            Assert.Empty(result);
        }

        // Helper classes
        private class Person
        {
            public string Name { get; set; } = default!;
            public int Age { get; set; }
        }

        private class PersonEqualityComparer : IEqualityComparer<Person>
        {
            public bool Equals(Person? x, Person? y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;
                return x.Name == y.Name && x.Age == y.Age;
            }

            public int GetHashCode(Person obj)
            {
                if (obj == null)
                    return 0;
                return HashCode.Combine(obj.Name, obj.Age);
            }
        }

        private class EnumeratorTracker<T> : IValueEnumerator<T>
        {
            private readonly IEnumerator<T> _enumerator;
            public bool IsDisposed { get; private set; }

            public EnumeratorTracker(IEnumerable<T> source)
            {
                _enumerator = source.GetEnumerator();
            }

            public bool TryGetNext(out T current)
            {
                if (_enumerator.MoveNext())
                {
                    current = _enumerator.Current;
                    return true;
                }
                current = default!;
                return false;
            }

            public bool TryGetNonEnumeratedCount(out int count)
            {
                count = 0;
                return false;
            }

            public bool TryGetSpan(out ReadOnlySpan<T> span)
            {
                span = default;
                return false;
            }

            public bool TryCopyTo(Span<T> destination, Index offset)
            {
                return false;
            }

            public void Dispose()
            {
                IsDisposed = true;
                _enumerator.Dispose();
            }
        }
    }
}
