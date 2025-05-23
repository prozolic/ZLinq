// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace ZLinq.Tests
{
    public class ShortCircuitingTests : EnumerableTests
    {
        private class TrackingEnumerable : IEnumerable<int>
        {
            // Skipping tests of double calls on GetEnumerable. Just don't do them here!
            private readonly int _count;
            public TrackingEnumerable(int count)
            {
                _count = count;
            }
            public int Moves { get; private set; }
            public IEnumerator<int> GetEnumerator()
            {
                for (int i = 0; i < _count; ++i)
                    yield return ++Moves;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class CountedFunction<T, TResult>
        {
            private readonly Func<T, TResult> _baseFunc;
            public int Calls { get; private set; }
            public CountedFunction(Func<T, TResult> baseFunc)
            {
                _baseFunc = baseFunc;
            }
            public Func<T, TResult> Func
            {
                get
                {
                    return x =>
                    {
                        ++Calls;
                        return _baseFunc(x);
                    };
                }
            }
        }

        [Fact]
        public void ListLastDoesntCheckAll()
        {
            var source = Enumerable.Range(0, 10).ToList();
            var pred = new CountedFunction<int, bool>(i => i < 7);
            Assert.Equal(6, source.Last(pred.Func));

            // .NET Core shortcircuits as an optimization.
            // See https://github.com/dotnet/corefx/pull/2350.
            Assert.Equal(4, pred.Calls);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinDoubleDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => i == 5 ? double.NaN : (double)i);
            Assert.True(double.IsNaN(source.Min()));

            // .NET Core shortcircuits as an optimization.
            // See https://github.com/dotnet/corefx/pull/2350.
            Assert.Equal(5, tracker.Moves);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinNullableDoubleDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => (double?)(i == 5 ? double.NaN : (double)i));
            Assert.True(double.IsNaN(source.Min().GetValueOrDefault()));

            // .NET Core shortcircuits as an optimization.
            // See https://github.com/dotnet/corefx/pull/2350.
            Assert.Equal(5, tracker.Moves);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinSingleDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => i == 5 ? float.NaN : (float)i);
            Assert.True(float.IsNaN(source.Min()));

            // .NET Core shortcircuits as an optimization.
            // See https://github.com/dotnet/corefx/pull/2350.
            Assert.Equal(5, tracker.Moves);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinNullableSingleDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => (float?)(i == 5 ? float.NaN : (float)i));
            Assert.True(float.IsNaN(source.Min().GetValueOrDefault()));

            // .NET Core shortcircuits as an optimization.
            // See https://github.com/dotnet/corefx/pull/2350.
            Assert.Equal(5, tracker.Moves);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinDoubleDoesntCheckAllStartLeadingWithNaN()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => i == 1 ? double.NaN : (double)i);

            Assert.True(double.IsNaN(source.Min()));
            Assert.Equal(1, tracker.Moves);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinNullableDoubleDoesntCheckAllLeadingWithNaN()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => (double?)(i == 1 ? double.NaN : (double)i));

            Assert.True(double.IsNaN(source.Min().GetValueOrDefault()));
            Assert.Equal(1, tracker.Moves);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinSingleDoesntCheckAllLeadingWithNaN()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => i == 1 ? float.NaN : (float)i);

            Assert.True(float.IsNaN(source.Min()));
            Assert.Equal(1, tracker.Moves);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinNullableSingleDoesntCheckAllLeadingWithNaN()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => (float?)(i == 1 ? float.NaN : (float)i));

            Assert.True(float.IsNaN(source.Min().GetValueOrDefault()));
            Assert.Equal(1, tracker.Moves);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinDoubleSelectorDoesntCheckAllStartLeadingWithNaN()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => i == 1 ? double.NaN : (double)i);

            Assert.True(double.IsNaN(source.Min(x => x + 1d)));
            Assert.Equal(1, tracker.Moves);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinNullableDoubleSelectorDoesntCheckAllLeadingWithNaN()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => (double?)(i == 1 ? double.NaN : (double)i));

            Assert.True(double.IsNaN(source.Min(x => x + 1d).GetValueOrDefault()));
            Assert.Equal(1, tracker.Moves);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinSingleSelectorDoesntCheckAllLeadingWithNaN()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => i == 1 ? float.NaN : (float)i);

            Assert.True(float.IsNaN(source.Min(x => x + 1f)));
            Assert.Equal(1, tracker.Moves);
        }

        [Fact(Skip = SkipReason.Issue0092)]
        public void MinNullableSingleSelectorDoesntCheckAllLeadingWithNaN()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => (float?)(i == 1 ? float.NaN : (float)i));

            Assert.True(float.IsNaN(source.Min(x => x + 1f).GetValueOrDefault()));
            Assert.Equal(1, tracker.Moves);
        }

        [Fact]
        public void SingleWithPredicateDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var pred = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker.Single(pred.Func));

            // .NET Core shortcircuits as an optimization.
            // See https://github.com/dotnet/corefx/pull/2350.
            Assert.Equal(4, tracker.Moves);
            Assert.Equal(4, pred.Calls);
        }

        [Fact]
        public void SingleOrDefaultWithPredicateDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var pred = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker.SingleOrDefault(pred.Func));

            // .NET Core shortcircuits as an optimization.
            // See https://github.com/dotnet/corefx/pull/2350.
            Assert.Equal(4, tracker.Moves);
            Assert.Equal(4, pred.Calls);
        }

        [Fact]
        public void SingleWithPredicateWorksLikeWhereFollowedBySingle()
        {
            var tracker0 = new TrackingEnumerable(10);
            var pred0 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker0.Single(pred0.Func));
            var tracker1 = new TrackingEnumerable(10);
            var pred1 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker1.Where(pred1.Func).Single());

            // .NET Core shortcircuits as an optimization.
            // See https://github.com/dotnet/corefx/pull/2350.
            Assert.Equal(tracker0.Moves, tracker1.Moves);
            Assert.Equal(pred0.Calls, pred1.Calls);
        }

        [Fact]
        public void SingleOrDefaultWithPredicateWorksLikeWhereFollowedBySingleOrDefault()
        {
            var tracker0 = new TrackingEnumerable(10);
            var pred0 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker0.SingleOrDefault(pred0.Func));
            var tracker1 = new TrackingEnumerable(10);
            var pred1 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker1.Where(pred1.Func).SingleOrDefault());

            // .NET Core shortcircuits as an optimization.
            // See https://github.com/dotnet/corefx/pull/2350.
            Assert.Equal(tracker0.Moves, tracker1.Moves);
            Assert.Equal(pred0.Calls, pred1.Calls);
        }
    }
}
