using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

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
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxChunkSize is less than one.</exception>
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
}
