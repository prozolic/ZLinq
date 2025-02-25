﻿using System;
using System.Collections.Generic;
using System.Text;

#if NETSTANDARD2_1

namespace System.Runtime.InteropServices
{
    internal static class CollectionsMarshal
    {
        internal static Span<T> AsSpan<T>(this List<T>? list)
        {
            Span<T> span = default;
            if (list is not null)
            {
                var view = Unsafe.As<ListView<T>>(list);

                int size = view._size;
                T[] items = view._items;

                span = items.AsSpan(0, size);
            }

            return span;
        }

        // This is not polyfill.
        // Unlike the original SetCount, this does not grow if the count is smaller.
        // Therefore, the internal collection size of the List must always be greater than or equal to the count.
        internal static void UnsafeSetCount<T>(this List<T>? list, int count)
        {
            if (list is not null)
            {
                var view = Unsafe.As<ListView<T>>(list);
                view._size = count;
            }
        }
    }

#pragma warning disable CS8618
    internal class ListView<T>
    {
        public T[] _items;
        public int _size;
        public int _version;
    }
#pragma warning restore CS8618

}

#endif
