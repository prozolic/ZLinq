﻿using System;
using System.Buffers;
#if NET8_0_OR_GREATER
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Numerics;
#endif

namespace ZLinq
{
    public static partial class ValueEnumerable
    {
        public static ValueEnumerable<FromEnumerable<T>, T> AsValueEnumerable<T>(this IEnumerable<T> source)
        {
            return new(new(Throws.IfNull(source)));
        }

        public static ValueEnumerable<FromArray<T>, T> AsValueEnumerable<T>(this T[] source)
        {
            return new(new(Throws.IfNull(source)));
        }

        public static ValueEnumerable<FromList<T>, T> AsValueEnumerable<T>(this List<T> source)
        {
            return new(new(Throws.IfNull(source)));
        }

        public static ValueEnumerable<FromMemory<T>, T> AsValueEnumerable<T>(this ArraySegment<T> source)
        {
            return new(new(source));
        }

        public static ValueEnumerable<FromMemory<T>, T> AsValueEnumerable<T>(this Memory<T> source)
        {
            return new(new(source));
        }

        public static ValueEnumerable<FromMemory<T>, T> AsValueEnumerable<T>(this ReadOnlyMemory<T> source)
        {
            return new(new(source));
        }

        public static ValueEnumerable<FromReadOnlySequence<T>, T> AsValueEnumerable<T>(this ReadOnlySequence<T> source)
        {
            return new(new(source));
        }

        // for System.Collections.Generic

        public static ValueEnumerable<FromDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>> AsValueEnumerable<TKey, TValue>(this Dictionary<TKey, TValue> source)
            where TKey : notnull
        {
            return new(new(Throws.IfNull(source)));
        }

        public static ValueEnumerable<FromQueue<T>, T> AsValueEnumerable<T>(this Queue<T> source)
        {
            return new(new(Throws.IfNull(source)));
        }

        public static ValueEnumerable<FromStack<T>, T> AsValueEnumerable<T>(this Stack<T> source)
        {
            return new(new(Throws.IfNull(source)));
        }

        public static ValueEnumerable<FromLinkedList<T>, T> AsValueEnumerable<T>(this LinkedList<T> source)
        {
            return new(new(Throws.IfNull(source)));
        }

        public static ValueEnumerable<FromHashSet<T>, T> AsValueEnumerable<T>(this HashSet<T> source)
        {
            return new(new(Throws.IfNull(source)));
        }

#if NET8_0_OR_GREATER

        public static ValueEnumerable<FromImmutableArray<T>, T> AsValueEnumerable<T>(this ImmutableArray<T> source)
        {
            return new(new(source));
        }

#endif

#if NET9_0_OR_GREATER

        public static ValueEnumerable<FromSpan<T>, T> AsValueEnumerable<T>(this Span<T> source)
        {
            return new(new(source));
        }

        public static ValueEnumerable<FromSpan<T>, T> AsValueEnumerable<T>(this ReadOnlySpan<T> source)
        {
            return new(new(source));
        }

#endif

    }
}

namespace ZLinq.Linq
{
    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct FromEnumerable<T>(IEnumerable<T> source) : IValueEnumerator<T>
    {
        CollectionIterator<T>? iterator; // field instantiate must deferred

        CollectionIterator<T> CreateIterator()
        {
            if (source is T[] array)
            {
                return new ArrayIterator<T>(array);
            }
            else if (source is List<T> list)
            {
                return new ListIterator<T>(list);
            }
            else if (source is IReadOnlyList<T> readonlyList)
            {
                return new IReadOnlyListIterator<T>(readonlyList);
            }
            else if (source is IList<T> ilist)
            {
                return new IListIterator<T>(ilist);
            }
            else
            {
                return new EnumerableIterator<T>(source);
            }
        }

        // for Contains, need to check ICollection of IEqualityComparer due to compatibility
        internal IEnumerable<T> GetSource() => (iterator ??= CreateIterator()).GetSource();

        public bool TryGetNonEnumeratedCount(out int count) => (iterator ??= CreateIterator()).TryGetNonEnumeratedCount(out count);

        public bool TryGetSpan(out ReadOnlySpan<T> span) => (iterator ??= CreateIterator()).TryGetSpan(out span);

        public bool TryCopyTo(Span<T> destination, Index offset) => (iterator ??= CreateIterator()).TryCopyTo(destination, offset);

        public bool TryGetNext(out T current) => (iterator ??= CreateIterator()).TryGetNext(out current);

        public void Dispose() => iterator?.Dispose();
    }

    // variation for FromEnumerable
    internal abstract class CollectionIterator<T>(IEnumerable<T> source) : IDisposable
    {
        public IEnumerable<T> GetSource() => source;

        public bool TryGetNonEnumeratedCount(out int count)
        {
#if NET8_0_OR_GREATER
            if (source.TryGetNonEnumeratedCount(out count)) // call System.Linq.Enumerable.TryGetNonEnumeratedCount
            {
                return true;
            }
#else
            if (source is ICollection<T> c)
            {
                count = c.Count;
                return true;
            }
#endif
            else if (source is IReadOnlyCollection<T> rc) // Enumerable.TryGetNonEnumeratedCount does not check IReadOnlyCollection
            {
                count = rc.Count;
                return true;
            }
            count = 0;
            return false;
        }

        public bool TryGetSpan(out ReadOnlySpan<T> span)
        {
            if (source.GetType() == typeof(T[]))
            {
                span = Unsafe.As<T[]>(source);
                return true;
            }
            else if (source.GetType() == typeof(List<T>))
            {
                span = CollectionsMarshal.AsSpan(Unsafe.As<List<T>>(source));
                return true;
            }
            else
            {
                span = default;
                return false;
            }
        }

        public bool TryCopyTo(Span<T> destination, Index offset)
        {
            if (TryGetSpan(out var span))
            {
                if (EnumeratorHelper.TryGetSlice<T>(span, offset, destination.Length, out var slice))
                {
                    slice.CopyTo(destination);
                    return true;
                }
            }

            return false;
        }

        public virtual void Dispose() { }

        public abstract bool TryGetNext(out T current);
    }

    internal sealed class ArrayIterator<T>(T[] source) : CollectionIterator<T>(source)
    {
        int index;

        public override bool TryGetNext(out T current)
        {
            if (index < source.Length)
            {
                current = source[index];
                index++;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }
    }

    internal sealed class ListIterator<T>(List<T> source) : CollectionIterator<T>(source)
    {
        int index;

        public override bool TryGetNext(out T current)
        {
            if (index < source.Count)
            {
                current = source[index];
                index++;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }
    }

    internal sealed class IListIterator<T>(IList<T> source) : CollectionIterator<T>(source)
    {
        int index;

        public override bool TryGetNext(out T current)
        {
            if (index < source.Count)
            {
                current = source[index];
                index++;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }
    }

    internal sealed class IReadOnlyListIterator<T>(IReadOnlyList<T> source) : CollectionIterator<T>(source)
    {
        int index;

        public override bool TryGetNext(out T current)
        {
            if (index < source.Count)
            {
                current = source[index];
                index++;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }
    }

#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
    internal sealed class EnumerableIterator<T>(IEnumerable<T> source) : CollectionIterator<T>(source)
#pragma warning restore CS9107
    {
        IEnumerator<T>? enumerator = null;

        public override bool TryGetNext(out T current)
        {
            if (enumerator == null)
            {
                enumerator = source.GetEnumerator();
            }

            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public override void Dispose()
        {
            enumerator?.Dispose();
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct FromArray<T>(T[] source) : IValueEnumerator<T>
    {
        int index;

        // becareful, don't call AsSpan
        internal T[] GetSource() => source;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = source.Length;
            return true;
        }

        public bool TryGetSpan(out ReadOnlySpan<T> span)
        {
            // AsSpan is failed by array variance
            if (!typeof(T).IsValueType && source.GetType() != typeof(T[]))
            {
                span = default;
                return false;
            }

            span = source.AsSpan();
            return true;
        }

        public bool TryCopyTo(Span<T> destination, Index offset)
        {
            if (EnumeratorHelper.TryGetSlice<T>(source, offset, destination.Length, out var slice))
            {
                slice.CopyTo(destination);
                return true;
            }
            return false;
        }

        public bool TryGetNext(out T current)
        {
            if (index < source.Length)
            {
                current = source[index];
                index++;
                return true;
            }

            Unsafe.SkipInit(out current);
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
    struct FromMemory<T>(ReadOnlyMemory<T> source) : IValueEnumerator<T>
    {
#if NET9_0_OR_GREATER
        ReadOnlySpan<T> source = source.Span;
#endif

        int index;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = source.Length;
            return true;
        }

        public bool TryCopyTo(Span<T> destination, Index offset)
        {
#if NET9_0_OR_GREATER
            var span = source;
#else
            var span = source.Span;
#endif
            if (EnumeratorHelper.TryGetSlice<T>(span, offset, destination.Length, out var slice))
            {
                slice.CopyTo(destination);
                return true;
            }
            return false;
        }

        public bool TryGetSpan(out ReadOnlySpan<T> span)
        {
#if NET9_0_OR_GREATER
            span = source;
#else
            span = source.Span;
#endif
            return true;
        }

        public bool TryGetNext(out T current)
        {
            if (index < source.Length)
            {
#if NET9_0_OR_GREATER
                current = source[index];
#else
                current = source.Span[index];
#endif
                index++;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct FromList<T>(List<T> source) : IValueEnumerator<T>
    {
        int index;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = source.Count;
            return true;
        }

        public bool TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = CollectionsMarshal.AsSpan(source);
            return true;
        }

        public bool TryCopyTo(Span<T> destination, Index offset)
        {
            var span = CollectionsMarshal.AsSpan(source);
            if (EnumeratorHelper.TryGetSlice<T>(span, offset, destination.Length, out var slice))
            {
                slice.CopyTo(destination);
                return true;
            }
            return false;
        }

        public bool TryGetNext(out T current)
        {
            if (index < source.Count)
            {
                current = source[index];
                index++;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct FromDictionary<TKey, TValue>(Dictionary<TKey, TValue> source) : IValueEnumerator<KeyValuePair<TKey, TValue>>
        where TKey : notnull
    {
        bool isInit = false;
        Dictionary<TKey, TValue>.Enumerator enumerator;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = source.Count;
            return true;
        }

        public bool TryGetSpan(out ReadOnlySpan<KeyValuePair<TKey, TValue>> span)
        {
            span = default;
            return false;
        }

        public bool TryCopyTo(Span<KeyValuePair<TKey, TValue>> destination, Index offset) => false;

        public bool TryGetNext(out KeyValuePair<TKey, TValue> current)
        {
            if (!isInit)
            {
                isInit = true;
                enumerator = source.GetEnumerator();
            }

            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
            if (isInit)
            {
                enumerator.Dispose();
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET9_0_OR_GREATER
    public ref
#else
    public
#endif
    struct FromReadOnlySequence<T>(ReadOnlySequence<T> source) : IValueEnumerator<T>
    {
        bool isInit = false;
        ReadOnlySequence<T>.Enumerator sequenceEnumerator;
        FromMemory<T> enumerator;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            const int ArrayMaxLength = 0X7FFFFFC7;
            if (source.Length <= ArrayMaxLength)
            {
                count = checked((int)source.Length);
                return true;
            }
            count = 0;
            return false;
        }

        public bool TryGetSpan(out ReadOnlySpan<T> span)
        {
            if (source.IsSingleSegment)
            {
                span = source.First.Span;
                return true;
            }

            span = default;
            return false;
        }

        public bool TryCopyTo(Span<T> destination, Index offset)
        {
            if (source.IsSingleSegment)
            {
                var span = source.First.Span;
                if (EnumeratorHelper.TryGetSlice<T>(span, offset, destination.Length, out var slice))
                {
                    slice.CopyTo(destination);
                    return true;
                }
            }
            else
            {
                if (EnumeratorHelper.TryGetSliceRange(checked((int)source.Length), offset, destination.Length, out var start, out var count))
                {
                    source.Slice(start, count).CopyTo(destination);
                    return true;
                }
            }

            return false;
        }

        public bool TryGetNext(out T current)
        {
            if (!isInit)
            {
                isInit = true;
                sequenceEnumerator = source.GetEnumerator();
            }

        MOVE_NEXT:
            if (enumerator.TryGetNext(out current))
            {
                return true;
            }

            if (sequenceEnumerator.MoveNext())
            {
                enumerator = sequenceEnumerator.Current.AsValueEnumerable().Enumerator;
                goto MOVE_NEXT;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
            // no needs FromMemory<T>.Dispose
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct FromQueue<T>(Queue<T> source) : IValueEnumerator<T>
    {
        bool isInit;
        Queue<T>.Enumerator enumerator;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = source.Count;
            return true;
        }

        public bool TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = default;
            return false;
        }

        public bool TryCopyTo(Span<T> destination, Index offset) => false;

        public bool TryGetNext(out T current)
        {
            if (!isInit)
            {
                isInit = true;
                enumerator = source.GetEnumerator();
            }

            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
            if (isInit)
            {
                enumerator.Dispose();
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct FromStack<T>(Stack<T> source) : IValueEnumerator<T>
    {
        bool isInit;
        Stack<T>.Enumerator enumerator;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = source.Count;
            return true;
        }

        public bool TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = default;
            return false;
        }

        public bool TryCopyTo(Span<T> destination, Index offset) => false;

        public bool TryGetNext(out T current)
        {
            if (!isInit)
            {
                isInit = true;
                enumerator = source.GetEnumerator();
            }

            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
            if (isInit)
            {
                enumerator.Dispose();
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct FromLinkedList<T>(LinkedList<T> source) : IValueEnumerator<T>
    {
        bool isInit;
        LinkedList<T>.Enumerator enumerator;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = source.Count;
            return true;
        }

        public bool TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = default;
            return false;
        }

        public bool TryCopyTo(Span<T> destination, Index offset) => false;

        public bool TryGetNext(out T current)
        {
            if (!isInit)
            {
                isInit = true;
                enumerator = source.GetEnumerator();
            }

            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
            if (isInit)
            {
                enumerator.Dispose();
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct FromHashSet<T>(HashSet<T> source) : IValueEnumerator<T>
    {
        bool isInit;
        HashSet<T>.Enumerator enumerator;

        // for Contains, need to check ICollection of IEqualityComparer due to compatibility
        internal HashSet<T> GetSource() => source;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = source.Count;
            return true;
        }

        public bool TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = default;
            return false;
        }

        public bool TryCopyTo(Span<T> destination, Index offset) => false;

        public bool TryGetNext(out T current)
        {
            if (!isInit)
            {
                isInit = true;
                enumerator = source.GetEnumerator();
            }

            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
            if (isInit)
            {
                enumerator.Dispose();
            }
        }
    }

#if NET8_0_OR_GREATER

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct FromImmutableArray<T>(ImmutableArray<T> source) : IValueEnumerator<T>
    {
        int index;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = source.Length;
            return true;
        }

        public bool TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = source.AsSpan();
            return true;
        }

        public bool TryCopyTo(Span<T> destination, Index offset)
        {
            if (EnumeratorHelper.TryGetSlice<T>(source.AsSpan(), offset, destination.Length, out var slice))
            {
                slice.CopyTo(destination);
                return true;
            }
            return false;
        }

        public bool TryGetNext(out T current)
        {
            if (index < source.Length)
            {
                current = source[index];
                index++;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
        }
    }

#endif

#if NET9_0_OR_GREATER

    [StructLayout(LayoutKind.Auto)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ref struct FromSpan<T>(ReadOnlySpan<T> source) : IValueEnumerator<T>
    {
        ReadOnlySpan<T> source = source;
        int index;

        public bool TryGetNonEnumeratedCount(out int count)
        {
            count = source.Length;
            return true;
        }

        public bool TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = source;
            return true;
        }

        public bool TryCopyTo(Span<T> destination, Index offset)
        {
            if (EnumeratorHelper.TryGetSlice<T>(source, offset, destination.Length, out var slice))
            {
                slice.CopyTo(destination);
                return true;
            }
            return false;
        }

        public bool TryGetNext(out T current)
        {
            if (index < source.Length)
            {
                current = source[index];
                index++;
                return true;
            }

            Unsafe.SkipInit(out current);
            return false;
        }

        public void Dispose()
        {
        }
    }
#endif
}
