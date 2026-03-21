using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Wolfgang.Extensions.IAsyncEnumerable.Benchmarks")]
[assembly: InternalsVisibleTo("Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit")]

namespace Wolfgang.Extensions.IAsyncEnumerable;

/// <summary>
/// A collection of extension methods for IAsyncEnumerable{T}.
/// </summary>
// ReSharper disable once InconsistentNaming
public static class IAsyncEnumerableExtensions
{

    /// <summary>
    /// Splits an IAsyncEnumerable{T} into chunks of a specified maximum size.
    /// </summary>
    /// <param name="source">The source IAsyncEnumerable{T} to chunk.</param>
    /// <param name="maxChunkSize">The maximum size of each chunk.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>An IAsyncEnumerable{ICollection{T}} representing the chunks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxChunkSize"/> is less than one.</exception>
    public static async IAsyncEnumerable<ICollection<T>> ChunkAsync<T>
    (
        this IAsyncEnumerable<T> source,
        int maxChunkSize,
        [EnumeratorCancellation] CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        token.ThrowIfCancellationRequested();

        if (maxChunkSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChunkSize), "Chunk size must be greater than zero.");
        }

        await using var enumerator = source.GetAsyncEnumerator(token);

        if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            yield break;
        }

        var array = new T[maxChunkSize];
        var index = 0;

        do
        {
            array[index++] = enumerator.Current;

            if (index == maxChunkSize)
            {
                yield return array;
                token.ThrowIfCancellationRequested();
                array = new T[maxChunkSize];
                index = 0;
            }

        } while (await enumerator.MoveNextAsync().ConfigureAwait(false));

        if (index == 0)
        {
            yield break;
        }

        Array.Resize(ref array, index);
        yield return array;
    }



    /// <summary>
    /// Executes a synchronous side-effect action on each element of an IAsyncEnumerable{T}
    /// without transforming the elements. The original items are yielded unchanged.
    /// </summary>
    /// <param name="source">The source IAsyncEnumerable{T}.</param>
    /// <param name="action">The synchronous action to execute on each element.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>An IAsyncEnumerable{T} that yields the original elements after executing the action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="action"/> is null.</exception>
    /// <example>
    /// <code>
    /// await foreach (var item in source.DoAsync(x => Console.WriteLine($"Processing: {x}")))
    /// {
    ///     // item is unchanged
    /// }
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<T> DoAsync<T>
    (
        this IAsyncEnumerable<T> source,
        Action<T> action,
        [EnumeratorCancellation] CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        token.ThrowIfCancellationRequested();

        await using var enumerator = source.GetAsyncEnumerator(token);

        while (await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            action(enumerator.Current);
            yield return enumerator.Current;
            token.ThrowIfCancellationRequested();
        }
    }



    /// <summary>
    /// Executes an asynchronous side-effect action on each element of an IAsyncEnumerable{T}
    /// without transforming the elements. The original items are yielded unchanged.
    /// </summary>
    /// <param name="source">The source IAsyncEnumerable{T}.</param>
    /// <param name="action">The asynchronous action to execute on each element.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>An IAsyncEnumerable{T} that yields the original elements after executing the action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="action"/> is null.</exception>
    /// <example>
    /// <code>
    /// await foreach (var item in source.DoAsync(async x => await logger.LogAsync($"Processing: {x}")))
    /// {
    ///     // item is unchanged
    /// }
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<T> DoAsync<T>
    (
        this IAsyncEnumerable<T> source,
        Func<T, Task> action,
        [EnumeratorCancellation] CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        token.ThrowIfCancellationRequested();

        await using var enumerator = source.GetAsyncEnumerator(token);

        while (await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            await action(enumerator.Current).ConfigureAwait(false);
            yield return enumerator.Current;
            token.ThrowIfCancellationRequested();
        }
    }
}
