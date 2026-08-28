using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wolfgang.Extensions.IAsyncEnumerable;

/// <summary>
/// Terminal operator extension methods for IAsyncEnumerable{T}, for consumers on
/// net462 / netstandard2.0 where System.Linq.AsyncEnumerable is not available.
/// </summary>
// ReSharper disable once InconsistentNaming
public static class IAsyncEnumerableLegacyExtensions
{

    /// <summary>
    /// Asynchronously counts the elements in a sequence.
    /// </summary>
    /// <param name="source">The IAsyncEnumerable{T} to count.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>The number of elements in the source sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static async ValueTask<int> CountAsync<T>
    (
        this IAsyncEnumerable<T> source,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        token.ThrowIfCancellationRequested();

        var count = 0;

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                count++;
                token.ThrowIfCancellationRequested();
            }
        }

        return count;
    }



    /// <summary>
    /// Asynchronously determines whether a sequence contains any elements.
    /// </summary>
    /// <param name="source">The IAsyncEnumerable{T} to check.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>true if the source sequence contains any elements; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static async ValueTask<bool> AnyAsync<T>
    (
        this IAsyncEnumerable<T> source,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        token.ThrowIfCancellationRequested();

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            return await enumerator.MoveNextAsync().ConfigureAwait(false);
        }
    }



    /// <summary>
    /// Asynchronously determines whether any element of a sequence satisfies a condition.
    /// </summary>
    /// <param name="source">The IAsyncEnumerable{T} whose elements to apply the predicate to.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>true if any elements in the source sequence pass the test in the specified predicate; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
    public static async ValueTask<bool> AnyAsync<T>
    (
        this IAsyncEnumerable<T> source,
        Func<T, bool> predicate,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        token.ThrowIfCancellationRequested();

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                if (predicate(enumerator.Current))
                {
                    return true;
                }

                token.ThrowIfCancellationRequested();
            }
        }

        return false;
    }



    /// <summary>
    /// Asynchronously returns the first element of a sequence.
    /// </summary>
    /// <param name="source">The IAsyncEnumerable{T} to return the first element of.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>The first element in the source sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the source sequence contains no elements.</exception>
    public static async ValueTask<T> FirstAsync<T>
    (
        this IAsyncEnumerable<T> source,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        token.ThrowIfCancellationRequested();

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException("Sequence contains no elements.");
            }

            return enumerator.Current;
        }
    }



    /// <summary>
    /// Asynchronously returns the first element of a sequence, or a default value if the sequence
    /// contains no elements.
    /// </summary>
    /// <param name="source">The IAsyncEnumerable{T} to return the first element of.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>
    /// The first element in the source sequence, or the default value for <typeparamref name="T"/>
    /// if the sequence contains no elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static async ValueTask<T?> FirstOrDefaultAsync<T>
    (
        this IAsyncEnumerable<T> source,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        token.ThrowIfCancellationRequested();

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            return await enumerator.MoveNextAsync().ConfigureAwait(false)
                ? enumerator.Current
                : default;
        }
    }



    /// <summary>
    /// Asynchronously creates a List{T} from an IAsyncEnumerable{T}.
    /// </summary>
    /// <param name="source">The IAsyncEnumerable{T} to create a List{T} from.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>A List{T} containing every element of the source sequence, in enumeration order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static async ValueTask<List<T>> ToListAsync<T>
    (
        this IAsyncEnumerable<T> source,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        token.ThrowIfCancellationRequested();

        var list = new List<T>();

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                list.Add(enumerator.Current);
                token.ThrowIfCancellationRequested();
            }
        }

        return list;
    }
}
