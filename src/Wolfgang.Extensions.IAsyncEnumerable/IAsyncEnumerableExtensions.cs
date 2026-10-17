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
    /// <remarks>
    /// Matches <c>Enumerable.Chunk</c> (net6.0+) item for item, including which chunks are
    /// produced before a source failure, with one intentional difference: the size parameter is
    /// named <paramref name="maxChunkSize"/>, where <c>Enumerable.Chunk</c> calls it
    /// <c>size</c>, so the exception's <c>ParamName</c> differs. Renaming
    /// a shipped parameter would break callers that pass it by name.
    /// </remarks>
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
    /// Suppresses consecutive duplicate elements of an IAsyncEnumerable{T}, yielding an element
    /// only when it differs from the one immediately before it.
    /// </summary>
    /// <remarks>
    /// Only <em>adjacent</em> duplicates are removed: <c>1, 1, 2, 1</c> becomes <c>1, 2, 1</c>.
    /// The first element is always yielded, and when a run is collapsed the first element of the
    /// run is the one kept. Use <c>Distinct</c> to remove every repeat, or
    /// <see cref="DistinctUntilChangedByAsync{T, TKey}"/> to compare a key of each element.
    /// </remarks>
    /// <param name="source">The source IAsyncEnumerable{T}.</param>
    /// <param name="comparer">
    /// The comparer used to decide whether two adjacent elements are equal, or <see langword="null"/>
    /// to use <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>An IAsyncEnumerable{T} with runs of equal adjacent elements collapsed to one.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled. Observed at enumeration
    /// time, surfaced through the consuming <c>await foreach</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// // 1, 1, 2, 2, 2, 3, 1, 1 becomes 1, 2, 3, 1
    /// await foreach (var item in source.DistinctUntilChangedAsync())
    /// {
    ///     Console.WriteLine(item);
    /// }
    /// </code>
    /// </example>
    public static IAsyncEnumerable<T> DistinctUntilChangedAsync<T>
    (
        this IAsyncEnumerable<T> source,
        IEqualityComparer<T>? comparer = null,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return DistinctUntilChangedCoreAsync(source, static item => item, comparer ?? EqualityComparer<T>.Default, token);
    }



    /// <summary>
    /// Suppresses consecutive elements of an IAsyncEnumerable{T} whose keys are equal, yielding an
    /// element only when its key differs from the key of the element immediately before it.
    /// </summary>
    /// <remarks>
    /// The key-selector form of <see cref="DistinctUntilChangedAsync{T}"/>, named like LINQ's
    /// <c>DistinctBy</c>. Only <em>adjacent</em> elements are compared, by key. The first element
    /// is always yielded, and each yielded element is the first of its run.
    /// <paramref name="keySelector"/> runs once per element; exceptions it throws propagate to the
    /// consuming <c>await foreach</c>.
    /// </remarks>
    /// <param name="source">The source IAsyncEnumerable{T}.</param>
    /// <param name="keySelector">A function that extracts the comparison key from each element.</param>
    /// <param name="comparer">
    /// The comparer used to decide whether two adjacent keys are equal, or <see langword="null"/>
    /// to use <see cref="EqualityComparer{T}.Default"/> for <typeparamref name="TKey"/>.
    /// </param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <typeparam name="TKey">The type of the comparison key.</typeparam>
    /// <returns>An IAsyncEnumerable{T} with runs of equal-key adjacent elements collapsed to their first element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="keySelector"/> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled. Observed at enumeration
    /// time, surfaced through the consuming <c>await foreach</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// // Report only when the sign of the reading changes
    /// await foreach (var item in source.DistinctUntilChangedByAsync(x =&gt; Math.Sign(x)))
    /// {
    ///     Console.WriteLine($"Sign changed at {item}");
    /// }
    /// </code>
    /// </example>
    public static IAsyncEnumerable<T> DistinctUntilChangedByAsync<T, TKey>
    (
        this IAsyncEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (keySelector is null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        return DistinctUntilChangedCoreAsync(source, keySelector, comparer ?? EqualityComparer<TKey>.Default, token);
    }



    private static async IAsyncEnumerable<T> DistinctUntilChangedCoreAsync<T, TKey>
    (
        IAsyncEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey> comparer,
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

            var previousKey = keySelector(enumerator.Current);
            yield return enumerator.Current;
            token.ThrowIfCancellationRequested();

            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                var key = keySelector(enumerator.Current);
                if (comparer.Equals(previousKey, key))
                {
                    // A suppressed element is never yielded, so the post-yield check below never
                    // runs for it. Check here as well, or a long run of duplicates from a source
                    // that ignores the token would keep being consumed after cancellation.
                    token.ThrowIfCancellationRequested();
                    continue;
                }

                previousKey = key;
                yield return enumerator.Current;
                token.ThrowIfCancellationRequested();
            }
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
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is null. Thrown at the call, before a task is
    /// returned, as <c>Enumerable.Any</c> does.
    /// </exception>
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
    public static Task<bool> IsEmptyAsync<T>
    (
        this IAsyncEnumerable<T> source,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return IsEmptyCoreAsync(source, token);
    }



    private static async Task<bool> IsEmptyCoreAsync<T>
    (
        IAsyncEnumerable<T> source,
        CancellationToken token
    )
    {
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
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> or <paramref name="predicate"/> is null. Thrown at
    /// the call, before a task is returned, as <c>Enumerable.Any</c> does.
    /// </exception>
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
    public static Task<bool> NoneAsync<T>
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

        return NoneCoreAsync(source, predicate, token);
    }



    private static async Task<bool> NoneCoreAsync<T>
    (
        IAsyncEnumerable<T> source,
        Func<T, bool> predicate,
        CancellationToken token
    )
    {
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



    /// <summary>
    /// Applies an accumulator over an IAsyncEnumerable{T} and yields every intermediate
    /// result, like <c>Aggregate</c> but emitting the running value at each step.
    /// </summary>
    /// <remarks>
    /// The <paramref name="seed"/> itself is not yielded: the first value produced is
    /// <c>accumulator(seed, first)</c>, so an empty source yields nothing. Exceptions thrown
    /// by <paramref name="accumulator"/> propagate to the consuming <c>await foreach</c> and
    /// terminate the enumeration.
    /// </remarks>
    /// <param name="source">The source IAsyncEnumerable{T}.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="accumulator">A function that combines the running value with the next element.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TAccumulate">The type of the accumulated value.</typeparam>
    /// <returns>An IAsyncEnumerable{TAccumulate} of the running values, one per source element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="accumulator"/> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled. Observed at enumeration
    /// time, surfaced through the consuming <c>await foreach</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// // Running sum: 1, 2, 3, 4 produces 1, 3, 6, 10
    /// await foreach (var total in source.ScanAsync(0, (sum, x) =&gt; sum + x))
    /// {
    ///     Console.WriteLine(total);
    /// }
    /// </code>
    /// </example>
    public static IAsyncEnumerable<TAccumulate> ScanAsync<TSource, TAccumulate>
    (
        this IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> accumulator,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (accumulator is null)
        {
            throw new ArgumentNullException(nameof(accumulator));
        }

        return ScanCoreAsync(source, seed, accumulator, token);
    }



    private static async IAsyncEnumerable<TAccumulate> ScanCoreAsync<TSource, TAccumulate>
    (
        IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> accumulator,
        [EnumeratorCancellation] CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        var current = seed;
        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                current = accumulator(current, enumerator.Current);
                yield return current;
                token.ThrowIfCancellationRequested();
            }
        }
    }



    /// <summary>
    /// Yields overlapping windows of <paramref name="windowSize"/> consecutive elements from an
    /// IAsyncEnumerable{T}, advancing one element at a time.
    /// </summary>
    /// <remarks>
    /// The first window is yielded once <paramref name="windowSize"/> elements have arrived; each
    /// later element yields the next window. A source with fewer than
    /// <paramref name="windowSize"/> elements yields nothing. Every yielded window is a new,
    /// independent list, so a consumer may keep it after the enumeration moves on. Unlike
    /// <c>ChunkAsync</c>, the windows are returned as <see cref="IReadOnlyList{T}"/> because
    /// positional access (oldest, newest, a fixed offset) is the point of a window; see ADR-0006.
    /// </remarks>
    /// <param name="source">The source IAsyncEnumerable{T}.</param>
    /// <param name="windowSize">The number of elements in each window.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <typeparam name="T">The type of elements in the IAsyncEnumerable{T}.</typeparam>
    /// <returns>An IAsyncEnumerable{IReadOnlyList{T}} of windows, oldest element first.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="windowSize"/> is less than one.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="token"/> is canceled. Observed at enumeration
    /// time, surfaced through the consuming <c>await foreach</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// // Moving average over the last three readings
    /// await foreach (var window in source.SlidingWindowAsync(3))
    /// {
    ///     Console.WriteLine((window[0] + window[1] + window[2]) / 3.0);
    /// }
    /// </code>
    /// </example>
    public static IAsyncEnumerable<IReadOnlyList<T>> SlidingWindowAsync<T>
    (
        this IAsyncEnumerable<T> source,
        int windowSize,
        CancellationToken token = default
    )
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (windowSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be greater than zero.");
        }

        return SlidingWindowCoreAsync(source, windowSize, token);
    }



    private static async IAsyncEnumerable<IReadOnlyList<T>> SlidingWindowCoreAsync<T>
    (
        IAsyncEnumerable<T> source,
        int windowSize,
        [EnumeratorCancellation] CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();

        // Circular buffer: `next` is where the next element is written, which is also the
        // oldest element once the buffer is full.
        var buffer = new T[windowSize];
        var count = 0;
        var next = 0;

        var enumerator = source.GetAsyncEnumerator(token);
        await using (enumerator.ConfigureAwait(false))
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                buffer[next] = enumerator.Current;
                next = (next + 1) % windowSize;

                if (count < windowSize)
                {
                    count++;
                    if (count < windowSize)
                    {
                        continue;
                    }
                }

                // Each window escapes to the consumer, so it is a fresh copy in oldest-first
                // order rather than a view over the shared buffer.
                var window = new T[windowSize];
                Array.Copy(buffer, next, window, 0, windowSize - next);
                Array.Copy(buffer, 0, window, windowSize - next, next);

                yield return window;
                token.ThrowIfCancellationRequested();
            }
        }
    }
}
