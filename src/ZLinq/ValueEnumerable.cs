﻿using System.Diagnostics;
using System.Text;

namespace ZLinq;

// This struct is wrapper for enumerator(enumerable) to improve type inference in C# compiler.
// C# constraint inference issue: https://github.com/dotnet/csharplang/discussions/6930
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(ValueEnumerableDebugView<,>))]
#if NET9_0_OR_GREATER
public readonly ref
#else
public readonly
#endif
struct ValueEnumerable<TEnumerator, T>(TEnumerator enumerator)
    where TEnumerator : struct, IValueEnumerator<T>
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
{
    // enumerator is struct so it always copied, no need to create new Enumerator.
    // internal operator should use this
    public readonly TEnumerator Enumerator = enumerator;

    // Cast and OfType are implemented as instance methods rather than extension methods to simplify type specification.
    public ValueEnumerable<Cast<TEnumerator, T, TResult>, TResult> Cast<TResult>() => new(new(Enumerator));
    public ValueEnumerable<OfType<TEnumerator, T, TResult>, TResult> OfType<TResult>() => new(new(Enumerator));

    string DebuggerDisplay => ValueEnumerableDebuggerDisplayHelper.BuildDisplayText(typeof(TEnumerator));
}

// all implement types must be struct
public interface IValueEnumerator<T> : IDisposable
{
    /// <summary>
    /// Equivalent of IEnumerator.MoveNext + Current.
    /// </summary>
    bool TryGetNext(out T current);

    // for optimization

    /// <summary>
    /// Returns the length when processing time is not necessary.
    /// Always returns true if TryGetSpan or TryCopyTo returns true.
    /// </summary>
    bool TryGetNonEnumeratedCount(out int count);

    /// <summary>
    /// Returns true if it can return a Span.
    /// Used for SIMD and loop processing optimization.
    /// If copying the entire value is acceptable, prioritize TryGetNonEnumeratedCount -> TryCopyTo instead.
    /// </summary>
    bool TryGetSpan(out ReadOnlySpan<T> span);

    /// <summary>
    /// Unlike the semantics of normal CopyTo, this allows the destination to be smaller than the source.
    /// This serves as a TryGet function as well, e.g. single-span and ^1 is TryGetLast.
    /// </summary>
    bool TryCopyTo(scoped Span<T> destination, Index offset);
}

// generic implementation of enumerator
[StructLayout(LayoutKind.Auto)]
#if NET9_0_OR_GREATER
public ref
#else
public
#endif
struct ValueEnumerator<TEnumerator, T>(TEnumerator enumerator) : IDisposable
    where TEnumerator : struct, IValueEnumerator<T>
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
{
    TEnumerator enumerator = enumerator;
    T current = default!;

    public T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => current;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        if (enumerator.TryGetNext(out current))
        {
            return true;
        }
        else
        {
            current = default!;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => enumerator.Dispose();
}

public static partial class ValueEnumerableExtensions // keep `public static` partial class
{
    // for foreach
    public static ValueEnumerator<TEnumerator, T> GetEnumerator<TEnumerator, T>(in this ValueEnumerable<TEnumerator, T> valueEnumerable)
        where TEnumerator : struct, IValueEnumerator<T>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        => new(valueEnumerable.Enumerator);
}

internal static class ValueEnumerableDebuggerDisplayHelper // avoid <T> for assembly size
{
    public static string BuildDisplayText(Type type) // root is typeof(TEnumerator) : IValueEnumerator<T>
    {
        var sb = new StringBuilder();
        BuildCore(sb, type);
        var returnType = type.GetInterface("IValueEnumerator`1")?.GetGenericArguments()[0];
        if (returnType != null)
        {
            sb.Append(" => "); // like TypeScript definition
            sb.Append(returnType.Name);
        }
        return sb.ToString();
    }

    static void BuildCore(StringBuilder sb, Type type)
    {
        if (type.IsGenericType)
        {
            BuildCore(sb, type.GenericTypeArguments[0]);
            sb.Append(".");
            var genericsStart = type.Name.IndexOf('`');
            if (genericsStart != -1) // always come here
            {
                var name = type.Name.Substring(0, genericsStart);
                sb.Append(name);
            }
            else
            {
                sb.Append(type.Name);
            }
            return;
        }

        sb.Append("<");
        sb.Append(type.Name);
        sb.Append(">");
    }
}

// same approach as SpanDebugView<T> 
internal sealed class ValueEnumerableDebugView<TEnumerator, T>
    where TEnumerator : struct, IValueEnumerator<T>
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
{
    readonly T[] array;

    public ValueEnumerableDebugView(ValueEnumerable<TEnumerator, T> source)
    {
        // to avoid materialize infinite source
        array = source.Take(100000).ToArray();
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => array;
}