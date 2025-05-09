﻿namespace ZLinq.Tests.Linq;

public class ElementAtOrDefaultTest
{
    [Fact]
    public void Empty()
    {
        var xs = new int[0];
        xs.AsValueEnumerable().ElementAtOrDefault(0).ShouldBe(default(int));
        xs.ToValueEnumerable().ElementAtOrDefault(0).ShouldBe(default(int));
    }

    [Fact]
    public void NonEmpty()
    {
        var xs = new int[] { 1, 2, 3, 4, 5 };
        xs.AsValueEnumerable().ElementAtOrDefault(2).ShouldBe(3);
        xs.ToValueEnumerable().ElementAtOrDefault(2).ShouldBe(3);
    }

    [Fact]
    public void IndexOutOfRange()
    {
        var xs = new int[] { 1, 2, 3, 4, 5 };
        xs.AsValueEnumerable().ElementAtOrDefault(5).ShouldBe(default(int));
        xs.ToValueEnumerable().ElementAtOrDefault(5).ShouldBe(default(int));
    }

    [Fact]
    public void NegativeIndex()
    {
        var xs = new int[] { 1, 2, 3, 4, 5 };
        xs.AsValueEnumerable().ElementAtOrDefault(-1).ShouldBe(default(int));
        xs.ToValueEnumerable().ElementAtOrDefault(-1).ShouldBe(default(int));
    }

    [Fact]
    public void FromEnd()
    {
        var xs = new int[] { 1, 2, 3, 4, 5 };
        xs.AsValueEnumerable().ElementAtOrDefault(^2).ShouldBe(4);
        xs.ToValueEnumerable().ElementAtOrDefault(^1).ShouldBe(5);
        xs.ToValueEnumerable().ElementAtOrDefault(^2).ShouldBe(4);
        xs.ToValueEnumerable().ElementAtOrDefault(^3).ShouldBe(3);
        xs.ToValueEnumerable().ElementAtOrDefault(^4).ShouldBe(2);
        xs.ToValueEnumerable().ElementAtOrDefault(^5).ShouldBe(1);

        xs.ToValueEnumerable().ElementAtOrDefault(^6).ShouldBe(default(int));

        // large q
        Enumerable.Range(0, 1000).AsValueEnumerable().ElementAtOrDefault(^100).ShouldBe(900);
    }

#if !NET48

    [Fact]
    public void ElementAtOrDefault_Last0()
    {
        var source = new int[] { 1, 2, 3, 4 };

        // System.Linq
        var expected = source.ElementAtOrDefault(^0); // expected: 0

        // ZLinq
        var actual = source.AsValueEnumerable()
                           .ElementAtOrDefault(^0);  // System.ArgumentOutOfRangeException : Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')

        Assert.Equal(expected, actual);
    }

#endif
}
