using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled. Observed at enumeration
    /// time (surfaced through the consuming <c>await foreach</c>), checked at
    /// chunk boundaries rather than per element.
    /// </exception>
    public static IAsyncEnumerable<ICollection<T>> ChunkAsync<T>
    (
        this IAsyncEnumerable<T> source,
        int maxChunkSize,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (maxChunkSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChunkSize), "Chunk size must be greater than zero.");
        }

        return ChunkCoreAsync(source, maxChunkSize, token);
    }



    private static async IAsyncEnumerable<ICollection<T>> ChunkCoreAsync<T>
    (
        IAsyncEnumerable<T> source,
        int maxChunkSize,
        [EnumeratorCancellation] CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
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
                    // Deliberately checked at chunk boundaries only, not per
                    // element — the token is also passed to GetAsyncEnumerator
                    // above, so a well-behaved source observes it inside
                    // MoveNextAsync between elements. A per-element check here
                    // would buy little beyond the boundary check while adding a
                    // volatile read to the hot path.
                    token.ThrowIfCancellationRequested();
                    array = new T[maxChunkSize];
                    index = 0;
                }

            } while (await enumerator.MoveNextAsync().ConfigureAwait(false));

            if (index == 0)
            {
                yield break;
            }

            // Right-sized copy is one allocation + one copy; Array.Resize did
            // the same work plus an extra bookkeeping step. The final-chunk
            // array escapes the method, so pooling isn't safe.
            var tail = new T[index];
            Array.Copy(array, tail, index);
            yield return tail;
        }
    }



    /// <summary>
    /// Executes a synchronous side-effect action on each element of an IAsyncEnumerable{T}
    /// without transforming the elements. The original items are yielded unchanged.
    /// </summary>
    /// <remarks>
    /// Exceptions thrown by <paramref name="action"/> propagate to the consuming
    /// <c>await foreach</c> and terminate the enumeration. The cancellation token is
    /// observed between elements (after each <c>yield return</c>), not while the action
    /// is running — wrap long-running actions in their own cancellation if mid-action
    /// cancellation is required.
    /// </remarks>
    /// <param name="source">The source IAsyncEnumerable{T}.</param>
    /// <param name="action">The synchronous action to execute on each element.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>An IAsyncEnumerable{T} that yields the original elements after executing the action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="action"/> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled. Observed at enumeration
    /// time, surfaced through the consuming <c>await foreach</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// await foreach (var item in source.DoAsync(x =&gt; Console.WriteLine($"Processing: {x}")))
    /// {
    ///     // item is unchanged
    /// }
    /// </code>
    /// </example>
    public static IAsyncEnumerable<T> DoAsync<T>
    (
        this IAsyncEnumerable<T> source,
        Action<T> action,
        CancellationToken token = default
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

        return DoCoreAsync(source, action, token);
    }



    /// <summary>
    /// Executes an asynchronous side-effect action on each element of an IAsyncEnumerable{T}
    /// without transforming the elements. The original items are yielded unchanged.
    /// </summary>
    /// <remarks>
    /// Exceptions thrown by <paramref name="action"/> propagate to the consuming
    /// <c>await foreach</c> and terminate the enumeration. The cancellation token is
    /// observed between elements (after each <c>yield return</c>), not while the action
    /// is running — wrap long-running actions in their own cancellation if mid-action
    /// cancellation is required.
    /// </remarks>
    /// <param name="source">The source IAsyncEnumerable{T}.</param>
    /// <param name="action">The asynchronous action to execute on each element.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>An IAsyncEnumerable{T} that yields the original elements after executing the action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="action"/> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled. Observed at enumeration
    /// time, surfaced through the consuming <c>await foreach</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// await foreach (var item in source.DoAsync(async x =&gt; await logger.LogAsync($"Processing: {x}")))
    /// {
    ///     // item is unchanged
    /// }
    /// </code>
    /// </example>
    public static IAsyncEnumerable<T> DoAsync<T>
    (
        this IAsyncEnumerable<T> source,
        Func<T, Task> action,
        CancellationToken token = default
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

        return DoCoreAsync(source, action, token);
    }



    private static async IAsyncEnumerable<T> DoCoreAsync<T>
    (
        IAsyncEnumerable<T> source,
        Action<T> action,
        [EnumeratorCancellation] CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                action(enumerator.Current);
                yield return enumerator.Current;
                token.ThrowIfCancellationRequested();
            }
        }
    }



    private static async IAsyncEnumerable<T> DoCoreAsync<T>
    (
        IAsyncEnumerable<T> source,
        Func<T, Task> action,
        [EnumeratorCancellation] CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                await action(enumerator.Current).ConfigureAwait(false);
                yield return enumerator.Current;
                token.ThrowIfCancellationRequested();
            }
        }
    }



    /// <summary>
    /// Executes a synchronous action on each element of an IAsyncEnumerable{T},
    /// consuming the sequence. This is a terminal operation.
    /// </summary>
    /// <param name="source">The source IAsyncEnumerable{T}.</param>
    /// <param name="action">The synchronous action to execute on each element.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>A task that completes when every element has been processed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="action"/> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled before or during enumeration.
    /// Delivered through the returned <see cref="Task"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// await source.ForEachAsync(x =&gt; Console.WriteLine($"Processing: {x}"));
    /// </code>
    /// </example>
    public static async Task ForEachAsync<T>
    (
        this IAsyncEnumerable<T> source,
        Action<T> action,
        CancellationToken token = default
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

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                action(enumerator.Current);
                token.ThrowIfCancellationRequested();
            }
        }
    }



    /// <summary>
    /// Executes an asynchronous action on each element of an IAsyncEnumerable{T},
    /// consuming the sequence. This is a terminal operation.
    /// </summary>
    /// <param name="source">The source IAsyncEnumerable{T}.</param>
    /// <param name="action">The asynchronous action to execute on each element.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>A task that completes when every element has been processed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="action"/> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled before or during enumeration.
    /// Delivered through the returned <see cref="Task"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// await source.ForEachAsync(async x =&gt; await logger.LogAsync($"Processing: {x}"));
    /// </code>
    /// </example>
    public static async Task ForEachAsync<T>
    (
        this IAsyncEnumerable<T> source,
        Func<T, Task> action,
        CancellationToken token = default
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

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                await action(enumerator.Current).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            }
        }
    }



    /// <summary>
    /// Asynchronously determines whether a sequence contains no elements.
    /// </summary>
    /// <param name="source">The IAsyncEnumerable{T} to check.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>
    /// true if the source sequence contains no elements; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled before or during enumeration.
    /// Delivered through the returned <see cref="Task{TResult}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// if (await source.IsEmptyAsync())
    /// {
    ///     Console.WriteLine("No items found.");
    /// }
    /// </code>
    /// </example>
    public static async Task<bool> IsEmptyAsync<T>
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
            return !await enumerator.MoveNextAsync().ConfigureAwait(false);
        }
    }



    /// <summary>
    /// Asynchronously determines whether a sequence is null or contains no elements.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="IsEmptyAsync{T}"/>, this method is null-tolerant: passing a
    /// null <paramref name="source"/> returns <c>true</c> without throwing. The
    /// cancellation token is observed only when the source is non-null (a null source
    /// short-circuits before any token check).
    /// </remarks>
    /// <param name="source">The IAsyncEnumerable{T} to check. May be null.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>
    /// true if the source sequence is null or contains no elements; otherwise, false.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled before or during enumeration
    /// of a non-null source. Delivered through the returned <see cref="Task{TResult}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// if (await source.IsNullOrEmptyAsync())
    /// {
    ///     Console.WriteLine("No items found.");
    /// }
    /// </code>
    /// </example>
    public static Task<bool> IsNullOrEmptyAsync<T>
    (
        this IAsyncEnumerable<T>? source,
        CancellationToken token = default
    )
    {
        return source is null
            ? CachedTrueTask
            : IsEmptyAsync(source, token);
    }



    // Task.FromResult(true) is cached by the runtime on net8.0+ but allocates a
    // fresh Task<bool> per call on net462/netstandard2.0 — the null-source fast
    // path shouldn't allocate anywhere.
    private static readonly Task<bool> CachedTrueTask = Task.FromResult(true);



    /// <summary>
    /// Asynchronously determines whether a sequence contains no elements.
    /// </summary>
    /// <remarks>
    /// This overload is a naming alias for <see cref="IsEmptyAsync{T}"/> — the two
    /// are observationally equivalent. Pick whichever reads more naturally at the
    /// call site (e.g. <c>await source.NoneAsync()</c> as a guard, or
    /// <c>await source.IsEmptyAsync()</c> as a state check).
    /// </remarks>
    /// <param name="source">The IAsyncEnumerable{T} to check.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>true if the source sequence contains no elements; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled before or during enumeration.
    /// Delivered through the returned <see cref="Task{TResult}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// if (await source.NoneAsync())
    /// {
    ///     Console.WriteLine("No items found.");
    /// }
    /// </code>
    /// </example>
    public static Task<bool> NoneAsync<T>
    (
        this IAsyncEnumerable<T> source,
        CancellationToken token = default
    )
    {
        return IsEmptyAsync(source, token);
    }



    /// <summary>
    /// Asynchronously determines whether no element of a sequence satisfies a condition.
    /// </summary>
    /// <param name="source">The IAsyncEnumerable{T} whose elements to apply the predicate to.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>true if no elements in the source sequence pass the test in the specified predicate; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled before or during enumeration.
    /// Delivered through the returned <see cref="Task{TResult}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// if (await source.NoneAsync(x =&gt; x &gt; 100))
    /// {
    ///     Console.WriteLine("No items greater than 100.");
    /// }
    /// </code>
    /// </example>
    public static async Task<bool> NoneAsync<T>
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
                    return false;
                }

                token.ThrowIfCancellationRequested();
            }
        }

        return true;
    }
}
