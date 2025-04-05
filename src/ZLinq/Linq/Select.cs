﻿using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace ZLinq
{
    partial class ValueEnumerableExtensions
    {
        public static ValueEnumerable<Select<TEnumerator, TSource, TResult>, TResult> Select<TEnumerator, TSource, TResult>(this ValueEnumerable<TEnumerator, TSource> source, Func<TSource, TResult> selector)
            where TEnumerator : struct, IValueEnumerator<TSource>
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
            => new(new(source.Enumerator, Throws.IfNull(selector)));

        public static ValueEnumerable<Select2<TEnumerator, TSource, TResult>, TResult> Select<TEnumerator, TSource, TResult>(this ValueEnumerable<TEnumerator, TSource> source, Func<TSource, Int32, TResult> selector)
            where TEnumerator : struct, IValueEnumerator<TSource>
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
            => new(new(source.Enumerator, Throws.IfNull(selector)));

        public static ValueEnumerable<RangeSelect<TResult>, TResult> Select<TResult>(this ValueEnumerable<FromRange, int> source, Func<int, TResult> selector)
            => new(new(source.Enumerator, Throws.IfNull(selector)));

        public static ValueEnumerable<SelectWhere<TEnumerator, TSource, TResult>, TResult> Where<TEnumerator, TSource, TResult>(this ValueEnumerable<Select<TEnumerator, TSource, TResult>, TResult> source, Func<TResult, bool> predicate)
            where TEnumerator : struct, IValueEnumerator<TSource>
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
            => new(source.Enumerator.Where(Throws.IfNull(predicate)));
    }
}

namespace ZLinq.Linq
{
    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET9_0_OR_GREATER
    public ref
#else
    public
#endif
    struct Select<TEnumerator, TSource, TResult>(TEnumerator source, Func<TSource, TResult> selector) : IValueEnumerator<TResult>
        where TEnumerator : struct, IValueEnumerator<TSource>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        internal TEnumerator source = source;
        internal readonly Func<TSource, TResult> selector = selector;

        public bool TryGetNonEnumeratedCount(out int count) => source.TryGetNonEnumeratedCount(out count);

        public bool TryGetSpan(out ReadOnlySpan<TResult> span)
        {
            span = default;
            return false;
        }

        public bool TryCopyTo(Span<TResult> destination, Index offset)
        {
            // Iterate inlining
            if (source.TryGetSpan(out var span))
            {
                if (EnumeratorHelper.TryGetSlice(span, offset, destination.Length, out var slice))
                {
                    for (var i = 0; i < slice.Length; i++)
                    {
                        destination[i] = selector(slice[i]);
                    }
                    return true;
                }
            }

            //  First/ElementAt/Last
            if (destination.Length == 1)
            {
#if NETSTANDARD2_0
                var singleSpan = SingleSpan.Create<TSource>();
                if (source.TryCopyTo(singleSpan, offset))
                {
                    try
                    {
                        destination[0] = selector(singleSpan[0]);
                    }
                    finally
                    {
                        singleSpan.Clear();
                    }
                    return true;
                }
#else
                var current = default(TSource)!;
                if (source.TryCopyTo(SingleSpan.Create(ref current), offset))
                {
                    destination[0] = selector(current);
                    return true;
                }
#endif

                if (EnumeratorHelper.TryConsumeGetAt(ref source, offset, out TSource value))
                {
                    destination[0] = selector(value);
                    return true;
                }
            }

            return false;
        }

        public bool TryGetNext(out TResult current)
        {
            if (source.TryGetNext(out var value))
            {
                current = selector(value);
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
            source.Dispose();
        }

        internal SelectWhere<TEnumerator, TSource, TResult> Where(Func<TResult, bool> predicate)
            => new(source, selector, predicate);
    }

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET9_0_OR_GREATER
    public ref
#else
    public
#endif
    struct Select2<TEnumerator, TSource, TResult>(TEnumerator source, Func<TSource, Int32, TResult> selector)
        : IValueEnumerator<TResult>
        where TEnumerator : struct, IValueEnumerator<TSource>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        TEnumerator source = source;
        int index = 0;

        public bool TryGetNonEnumeratedCount(out int count) => source.TryGetNonEnumeratedCount(out count);

        public bool TryGetSpan(out ReadOnlySpan<TResult> span)
        {
            span = default;
            return false;
        }

        public bool TryCopyTo(Span<TResult> destination, Index offset) => false;

        public bool TryGetNext(out TResult current)
        {
            if (source.TryGetNext(out var value))
            {
                current = selector(value, index++);
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
            source.Dispose();
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET9_0_OR_GREATER
    public ref
#else
    public
#endif
    struct RangeSelect<TResult>(FromRange source, Func<int, TResult> selector) : IValueEnumerator<TResult>
    {
        // Range
        internal readonly int count = source.count;
        internal readonly int start = source.start;
        internal readonly int to = source.to;
        int value = source.start;

        // Select
        internal readonly Func<int, TResult> selector = selector;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = this.count;
            return true;
        }

        public bool TryGetSpan(out ReadOnlySpan<TResult> span)
        {
            span = default;
            return false;
        }

        public bool TryCopyTo(Span<TResult> destination, Index offset)
        {
            if (EnumeratorHelper.TryGetSliceRange(count, offset, destination.Length, out var fillStart, out var fillCount))
            {
                var value = start + fillStart;
                for (int i = 0; i < fillCount; i++)
                {
                    destination[i] = selector(value);
                    value++;
                }
                return true;
            }

            return false;
        }

        public bool TryGetNext(out TResult current)
        {
            if (value < to)
            {
                current = selector(value);
                value++;
                return true;
            }

            current = default!;
            return false;
        }

        public void Dispose()
        {
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET9_0_OR_GREATER
    public ref
#else
    public
#endif
    struct SelectWhere<TEnumerator, TSource, TResult>(TEnumerator source, Func<TSource, TResult> selector, Func<TResult, bool> predicate) // no in TEnumerator
        : IValueEnumerator<TResult>
        where TEnumerator : struct, IValueEnumerator<TSource>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        TEnumerator source = source;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = default;
            return false;
        }

        public bool TryGetSpan(out ReadOnlySpan<TResult> span)
        {
            span = default;
            return false;
        }

        public bool TryCopyTo(Span<TResult> destination, Index offset) => false;

        public bool TryGetNext(out TResult current)
        {
            while (source.TryGetNext(out var value))
            {
                var result = selector(value);
                if (predicate(result))
                {
                    current = result;
                    return true;
                }
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
            source.Dispose();
        }
    }

}
