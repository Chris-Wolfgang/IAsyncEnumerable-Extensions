using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

/// <summary>
/// Runs each operator that has a <see cref="System.Linq.Enumerable"/> counterpart and the LINQ
/// method over the same scripted source and requires the same outcome: the result, the exception
/// (type and message), how many items were pulled, and whether the source was disposed.
/// </summary>
public sealed class LinqParityTests
{
    private static readonly Dictionary<string, Func<ScriptedSource>> Scenarios = new()
    {
        ["empty"] = () => new ScriptedSource([], throwAt: -1, disposeThrows: false),
        ["single"] = () => new ScriptedSource([5], throwAt: -1, disposeThrows: false),
        ["many"] = () => new ScriptedSource([1, 2, 3, 4, 5], throwAt: -1, disposeThrows: false),
        ["throws on first MoveNext"] = () => new ScriptedSource([1, 2, 3], throwAt: 0, disposeThrows: false),
        ["throws midway"] = () => new ScriptedSource([1, 2, 3, 4, 5], throwAt: 3, disposeThrows: false),
        ["disposal throws"] = () => new ScriptedSource([1, 2, 3], throwAt: -1, disposeThrows: true),
        ["throws midway and disposal throws"] = () => new ScriptedSource([1, 2, 3, 4, 5], throwAt: 3, disposeThrows: true),
    };



    public static TheoryData<string> ScenarioNames()
    {
        var names = new TheoryData<string>();
        foreach (var name in Scenarios.Keys)
        {
            names.Add(name);
        }

        return names;
    }



    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task IsEmptyAsync_matches_not_Enumerable_Any(string scenario)
    {
        Assert.Equal
        (
            RunSync(scenario, static items => !System.Linq.Enumerable.Any(items)),
            await RunAsync(scenario, static async items => await items.IsEmptyAsync())
        );
    }



    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task NoneAsync_matches_not_Enumerable_Any(string scenario)
    {
        Assert.Equal
        (
            RunSync(scenario, static items => !System.Linq.Enumerable.Any(items)),
            await RunAsync(scenario, static async items => await items.NoneAsync())
        );
    }



    [Theory]
    [InlineData("many", 99, -1)]
    [InlineData("many", 1, -1)]
    [InlineData("many", 3, -1)]
    [InlineData("many", -1, 2)]
    [InlineData("throws midway", 99, -1)]
    [InlineData("throws midway", 1, -1)]
    [InlineData("disposal throws", 2, -1)]
    [InlineData("empty", 1, -1)]
    public async Task NoneAsync_with_predicate_matches_not_Enumerable_Any(string scenario, int match, int throwOn)
    {
        var syncPredicate = new ScriptedPredicate(match, throwOn);
        var asyncPredicate = new ScriptedPredicate(match, throwOn);

        var expected = RunSync(scenario, items => !System.Linq.Enumerable.Any(items, syncPredicate.Test));
        var actual = await RunAsync(scenario, async items => await items.NoneAsync(asyncPredicate.Test));

        Assert.Equal(expected, actual);
        Assert.Equal(syncPredicate.Calls, asyncPredicate.Calls);
    }



    [Fact]
    public void Null_arguments_are_rejected_eagerly_with_the_same_ParamName_as_Enumerable()
    {
        IAsyncEnumerable<int> asyncSource = null!;
        IEnumerable<int> syncSource = null!;
        var items = new ScriptedSource([1], -1, false);

        Exception?[] linq =
        [
            Record.Exception(() => System.Linq.Enumerable.Any(syncSource)),
            Record.Exception(() => System.Linq.Enumerable.Any(syncSource)),
            Record.Exception(() => System.Linq.Enumerable.Any(syncSource, new ScriptedPredicate(1, -1).Test)),
            Record.Exception(() => System.Linq.Enumerable.Any(items.Sync(), null!)),
        ];
        Exception?[] ours =
        [
            ThrownAtCall(() => asyncSource.IsEmptyAsync()),
            ThrownAtCall(() => asyncSource.NoneAsync()),
            ThrownAtCall(() => asyncSource.NoneAsync(new ScriptedPredicate(1, -1).Test)),
            ThrownAtCall(() => items.Async().NoneAsync(null!)),
        ];

        for (var i = 0; i < linq.Length; i++)
        {
            Assert.Equal(DescribeArgument(linq[i]), DescribeArgument(ours[i]));
        }

        // Sanity check on the probe itself: a call that does not throw is reported as such.
        Assert.Null(ThrownAtCall(() => new ScriptedSource([1], -1, false).Async().IsEmptyAsync()));
    }

#if NET6_0_OR_GREATER

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task ChunkAsync_matches_Enumerable_Chunk(string scenario)
    {
        Assert.Equal
        (
            RunSync(scenario, static items => DescribeChunks(System.Linq.Enumerable.Chunk(items, 2))),
            await RunAsync(scenario, static async items => await CollectChunksAsync(items.ChunkAsync(2)))
        );
    }



    [Fact]
    public void ChunkAsync_rejects_null_source_and_bad_size_eagerly_like_Enumerable_Chunk()
    {
        IAsyncEnumerable<int> asyncSource = null!;
        IEnumerable<int> syncSource = null!;
        var items = new ScriptedSource([1], -1, false);

        Assert.Equal
        (
            DescribeArgument(Record.Exception(() => System.Linq.Enumerable.Chunk(syncSource, 2))),
            DescribeArgument(Record.Exception(() => asyncSource.ChunkAsync(2)))
        );

        // Same exception type, thrown at the call. The parameter is named differently by design:
        // ChunkAsync shipped as `maxChunkSize` where Enumerable.Chunk calls it `size`, and renaming a
        // shipped parameter breaks callers that pass it by name.
        var linqSize = Assert.Throws<ArgumentOutOfRangeException>(() => System.Linq.Enumerable.Chunk(items.Sync(), 0));
        var ourSize = Assert.Throws<ArgumentOutOfRangeException>(() => items.Async().ChunkAsync(0));

        Assert.Equal("size", linqSize.ParamName);
        Assert.Equal("maxChunkSize", ourSize.ParamName);
    }



    private static string DescribeChunks(IEnumerable<int[]> chunks)
    {
        var shown = new List<string>();
        foreach (var chunk in chunks)
        {
            shown.Add("[" + string.Join(",", chunk) + "]");
        }

        return string.Join(" ", shown);
    }



    private static async Task<object?> CollectChunksAsync(IAsyncEnumerable<ICollection<int>> chunks)
    {
        var shown = new List<string>();
        await foreach (var chunk in chunks)
        {
            shown.Add("[" + string.Join(",", chunk) + "]");
        }

        return string.Join(" ", shown);
    }

#endif



    // Returns the exception the call throws before returning its task, or null if it returns one.
    private static Exception? ThrownAtCall(Func<Task> call)
    {
        try
        {
            _ = call();
        }
        catch (ArgumentException exception)
        {
            return exception;
        }

        return null;
    }



    private static string RunSync(string scenario, Func<IEnumerable<int>, object?> operation)
    {
        var source = Scenarios[scenario]();
        object? result = null;
        Exception? error = null;
        try
        {
            result = operation(source.Sync());
        }
        catch (InvalidOperationException exception)
        {
            error = exception;
        }

        return Outcome(result, error, source);
    }



    private static async Task<string> RunAsync(string scenario, Func<IAsyncEnumerable<int>, Task<object?>> operation)
    {
        var source = Scenarios[scenario]();
        object? result = null;
        Exception? error = null;
        try
        {
            result = await operation(source.Async());
        }
        catch (InvalidOperationException exception)
        {
            error = exception;
        }

        return Outcome(result, error, source);
    }



    private static string Outcome(object? result, Exception? error, ScriptedSource source)
    {
        var failure = error is null ? "none" : error.GetType().Name + ": " + error.Message;

        return "result=" + (result?.ToString() ?? "null") + "; error=" + failure + "; pulled=" + source.Pulled + "; disposed=" + source.Disposed;
    }



    private static string DescribeArgument(Exception? exception)
    {
        return exception is ArgumentException argument
            ? argument.GetType().Name + " " + argument.ParamName
            : "not an eager ArgumentException: " + exception?.GetType().Name;
    }



    private sealed class ScriptedPredicate(int match, int throwOn)
    {
        public int Calls { get; private set; }



        public bool Test(int item)
        {
            Calls++;
            return item == throwOn ? throw new InvalidOperationException("predicate failed") : item == match;
        }
    }
}
