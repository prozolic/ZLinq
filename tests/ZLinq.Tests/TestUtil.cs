﻿using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZLinq.Linq;

namespace ZLinq.Tests;

public static class TestUtil
{
    public static void Throws<T>(Action expectedTestCode, Action actualTestCode)
        where T : Exception
    {
        var expectedException = Assert.Throws<T>(expectedTestCode);
        var actualException = Assert.Throws<T>(actualTestCode);
#if !NET48
        expectedException.Message.ShouldBe(actualException.Message);
#endif
    }

    public static void NoThrow(Action expectedTestCode, Action actualTestCode)
    {
        expectedTestCode();
        actualTestCode();
    }

    public static IEnumerable<int> Empty()
    {
        yield break;
    }

    public static IEnumerable<T> Empty<T>()
    {
        yield break;
    }

    // hide source type to avoid Span optimization
    public static ValueEnumerable<FromEnumerable<T>, T> ToValueEnumerable<T>(this IEnumerable<T> source)
    {
        static IEnumerable<T> Core(IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
            }
        }

        return Core(source).AsValueEnumerable();
    }

    public static T[] IterateToArray<TEnumerator, T>(this ValueEnumerable<TEnumerator, T> enumerable)
        where TEnumerator : struct, IValueEnumerator<T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var list = new List<T>();
        using var e = enumerable.Enumerator;
        while (e.TryGetNext(out var current))
        {
            list.Add(current);
        }
        return list.ToArray();
    }

    // direct shortcut of enumerable.enumerator

    // Enumerator is struct so this shortcut is dangerous.
    //    public static bool TryGetNext<TEnumerator, T>(this ValueEnumerable<TEnumerator, T> enumerable, out T current)
    //        where TEnumerator : struct, IValueEnumerator<T>
    //#if NET9_0_OR_GREATER
    //        , allows ref struct
    //#endif
    //    {
    //        return enumerable.Enumerator.TryGetNext(out current);
    //    }

    public static bool TryGetNonEnumeratedCount<TEnumerator, T>(this ValueEnumerable<TEnumerator, T> enumerable, out int count)
        where TEnumerator : struct, IValueEnumerator<T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        using var e = enumerable.Enumerator;
        return e.TryGetNonEnumeratedCount(out count);
    }

    public static bool TryGetSpan<TEnumerator, T>(this ValueEnumerable<TEnumerator, T> enumerable, out ReadOnlySpan<T> span)
        where TEnumerator : struct, IValueEnumerator<T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        using var e = enumerable.Enumerator;
        var result = e.TryGetSpan(out var enumeratorSpan);
        if (result)
        {
            span = enumeratorSpan.ToArray(); // after enumerator dispose, sometimes span is return to arraypool.
        }
        else
        {
            span = default;
        }
        return result;
    }

    public static bool TryCopyTo<TEnumerator, T>(this ValueEnumerable<TEnumerator, T> enumerable, scoped Span<T> destination, Index offset = default)
        where TEnumerator : struct, IValueEnumerator<T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        using var e = enumerable.Enumerator;
        return e.TryCopyTo(destination, offset);
    }

    public static void Dispose<TEnumerator, T>(this ValueEnumerable<TEnumerator, T> enumerable)
        where TEnumerator : struct, IValueEnumerator<T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        using var e = enumerable.Enumerator;
        e.Dispose();
    }

    public static ValueEnumerable<TEnumerator, T> AsValueEnumerable<TEnumerator, T>(this TEnumerator enumerable, T typeHint)
        where TEnumerator : struct, IValueEnumerator<T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return new ValueEnumerable<TEnumerator, T>(enumerable);
    }
}

public sealed class DelegateIterator<TSource> : IEnumerable<TSource>, IEnumerator<TSource>
{
    private readonly Func<IEnumerator<TSource>> _getEnumerator;
    private readonly Func<bool> _moveNext;
    private readonly Func<TSource> _current;
    private readonly Func<IEnumerator> _explicitGetEnumerator;
    private readonly Func<object> _explicitCurrent;
    private readonly Action _reset;
    private readonly Action _dispose;

    public DelegateIterator(
        Func<IEnumerator<TSource>> getEnumerator = null!,
        Func<bool> moveNext = null!,
        Func<TSource> current = null!,
        Func<IEnumerator> explicitGetEnumerator = null!,
        Func<object> explicitCurrent = null!,
        Action reset = null!,
        Action dispose = null!)
    {
        _getEnumerator = getEnumerator ?? (() => this);
        _moveNext = moveNext ?? (() => { throw new NotImplementedException(); });
        _current = current ?? (() => { throw new NotImplementedException(); });
        _explicitGetEnumerator = explicitGetEnumerator ?? (() => { throw new NotImplementedException(); });
        _explicitCurrent = explicitCurrent ?? (() => { throw new NotImplementedException(); });
        _reset = reset ?? (() => { throw new NotImplementedException(); });
        _dispose = dispose ?? (() => { throw new NotImplementedException(); });
    }

    public IEnumerator<TSource> GetEnumerator() => _getEnumerator();

    public bool MoveNext() => _moveNext();

    public TSource Current => _current();

    IEnumerator IEnumerable.GetEnumerator() => _explicitGetEnumerator();

    object IEnumerator.Current => _explicitCurrent();

    void IEnumerator.Reset() => _reset();

    void IDisposable.Dispose() => _dispose();
}

public sealed class FastInfiniteEnumerator<T> : IEnumerable<T>, IEnumerator<T>
{
    public IEnumerator<T> GetEnumerator() => this;

    IEnumerator IEnumerable.GetEnumerator() => this;

    public bool MoveNext() => true;

    public void Reset() { }

    object IEnumerator.Current => default(T)!;

    public void Dispose() { }

    public T Current => default(T)!;
}

public class DelegateBasedCollection<T> : ICollection<T>
{
    public Func<int> CountWorker { get; set; }
    public Func<bool> IsReadOnlyWorker { get; set; }
    public Action<T> AddWorker { get; set; }
    public Action ClearWorker { get; set; }
    public Func<T, bool> ContainsWorker { get; set; }
    public Func<T, bool> RemoveWorker { get; set; }
    public Action<T[], int> CopyToWorker { get; set; }
    public Func<IEnumerator<T>> GetEnumeratorWorker { get; set; }
    public Func<IEnumerator> NonGenericGetEnumeratorWorker { get; set; }

    public DelegateBasedCollection()
    {
        CountWorker = () => 0;
        IsReadOnlyWorker = () => false;
        AddWorker = item => { };
        ClearWorker = () => { };
        ContainsWorker = item => false;
        RemoveWorker = item => false;
        CopyToWorker = (array, arrayIndex) => { };
        GetEnumeratorWorker = () => Enumerable.Empty<T>().GetEnumerator();
        NonGenericGetEnumeratorWorker = () => GetEnumeratorWorker();
    }

    public int Count => CountWorker();
    public bool IsReadOnly => IsReadOnlyWorker();
    public void Add(T item) => AddWorker(item);
    public void Clear() => ClearWorker();
    public bool Contains(T item) => ContainsWorker(item);
    public bool Remove(T item) => RemoveWorker(item);
    public void CopyTo(T[] array, int arrayIndex) => CopyToWorker(array, arrayIndex);
    public IEnumerator<T> GetEnumerator() => GetEnumeratorWorker();
    IEnumerator IEnumerable.GetEnumerator() => NonGenericGetEnumeratorWorker();
}
