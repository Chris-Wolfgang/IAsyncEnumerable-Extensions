using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Wolfgang.Extensions.IAsyncEnumerable.Legacy.Tests.Unit;

/// <summary>
/// Runs each Legacy operator and its <see cref="System.Linq.Enumerable"/> counterpart over the same
/// scripted source and requires the same outcome: the result, the exception (type and message),
/// how many items were pulled, and whether the source was disposed.
/// </summary>
public sealed class LinqParityTests
{
    private static readonly Dictionary<string, Func<ScriptedSource>> Scenarios = new()
    {
        ["empty"] = () => new ScriptedSource([], throwAt: -1, disposeThrows: false),
        ["single"] = () => new ScriptedSource([5], throwAt: -1, disposeThrows: false),
        ["many"] = () => new ScriptedSource([1, 2, 3, 4, 5], throwAt: -1, disposeThrows: false),
        ["throws on first MoveNext"] = () => new ScriptedSource([1, 2, 3], throwAt: 0, disposeThrows: false),
        ["throws midway"] = () => new ScriptedSource([1, 2, 3, 4], throwAt: 2, disposeThrows: false),
        ["disposal throws"] = () => new ScriptedSource([1, 2, 3], throwAt: -1, disposeThrows: true),
        ["throws midway and disposal throws"] = () => new ScriptedSource([1, 2, 3, 4], throwAt: 2, disposeThrows: true),
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
    public async Task AnyAsync_matches_Enumerable_Any(string scenario)
    {
        Assert.Equal
        (
            RunSync(scenario, static items => System.Linq.Enumerable.Any(items)),
            await RunAsync(scenario, static async items => await IAsyncEnumerableLegacyExtensions.AnyAsync(items))
        );
    }



    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task CountAsync_matches_Enumerable_Count(string scenario)
    {
        Assert.Equal
        (
            RunSync(scenario, static items => System.Linq.Enumerable.Count(items)),
            await RunAsync(scenario, static async items => await IAsyncEnumerableLegacyExtensions.CountAsync(items))
        );
    }



    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task FirstAsync_matches_Enumerable_First(string scenario)
    {
        Assert.Equal
        (
            RunSync(scenario, static items => System.Linq.Enumerable.First(items)),
            await RunAsync(scenario, static async items => await IAsyncEnumerableLegacyExtensions.FirstAsync(items))
        );
    }



    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task FirstOrDefaultAsync_matches_Enumerable_FirstOrDefault(string scenario)
    {
        Assert.Equal
        (
            RunSync(scenario, static items => System.Linq.Enumerable.FirstOrDefault(items)),
            await RunAsync(scenario, static async items => await IAsyncEnumerableLegacyExtensions.FirstOrDefaultAsync(items))
        );
    }



    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task ToListAsync_matches_Enumerable_ToList(string scenario)
    {
        Assert.Equal
        (
            RunSync(scenario, static items => System.Linq.Enumerable.ToList(items)),
            await RunAsync(scenario, static async items => await IAsyncEnumerableLegacyExtensions.ToListAsync(items))
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
    public async Task AnyAsync_with_predicate_matches_Enumerable_Any(string scenario, int match, int throwOn)
    {
        var syncPredicate = new ScriptedPredicate(match, throwOn);
        var asyncPredicate = new ScriptedPredicate(match, throwOn);

        var expected = RunSync(scenario, items => System.Linq.Enumerable.Any(items, syncPredicate.Test));
        var actual = await RunAsync(scenario, async items => await IAsyncEnumerableLegacyExtensions.AnyAsync(items, asyncPredicate.Test));

        Assert.Equal(expected, actual);
        Assert.Equal(syncPredicate.Calls, asyncPredicate.Calls);
    }



    [Fact]
    public void Null_source_is_rejected_eagerly_with_the_same_ParamName_as_Enumerable()
    {
        IAsyncEnumerable<int> asyncSource = null!;
        IEnumerable<int> syncSource = null!;

        // Each operator throws at the call, before anything is awaited, exactly as LINQ does.
        Exception?[] linq =
        [
            Record.Exception(() => System.Linq.Enumerable.Any(syncSource)),
            Record.Exception(() => System.Linq.Enumerable.Count(syncSource)),
            Record.Exception(() => System.Linq.Enumerable.First(syncSource)),
            Record.Exception(() => System.Linq.Enumerable.FirstOrDefault(syncSource)),
            Record.Exception(() => System.Linq.Enumerable.ToList(syncSource)),
            Record.Exception(() => System.Linq.Enumerable.Any(syncSource, new ScriptedPredicate(1, -1).Test)),
        ];
        // AsTask() consumes the returned ValueTask (CA2012); an eager throw happens before it runs.
        Exception?[] legacy =
        [
            ThrownAtCall(() => IAsyncEnumerableLegacyExtensions.AnyAsync(asyncSource).AsTask()),
            ThrownAtCall(() => IAsyncEnumerableLegacyExtensions.CountAsync(asyncSource).AsTask()),
            ThrownAtCall(() => IAsyncEnumerableLegacyExtensions.FirstAsync(asyncSource).AsTask()),
            ThrownAtCall(() => IAsyncEnumerableLegacyExtensions.FirstOrDefaultAsync(asyncSource).AsTask()),
            ThrownAtCall(() => IAsyncEnumerableLegacyExtensions.ToListAsync(asyncSource).AsTask()),
            ThrownAtCall(() => IAsyncEnumerableLegacyExtensions.AnyAsync(asyncSource, new ScriptedPredicate(1, -1).Test).AsTask()),
        ];

        for (var i = 0; i < linq.Length; i++)
        {
            Assert.Equal(DescribeArgument(linq[i]), DescribeArgument(legacy[i]));
        }

        // Sanity check on the probe itself: a call that does not throw is reported as such.
        Assert.Null(ThrownAtCall(() => IAsyncEnumerableLegacyExtensions.CountAsync(new ScriptedSource([1], -1, false).Async()).AsTask()));
    }



    [Fact]
    public void Null_predicate_is_rejected_eagerly_with_the_same_ParamName_as_Enumerable()
    {
        var linq = Record.Exception(() => System.Linq.Enumerable.Any(new ScriptedSource([1], -1, false).Sync(), null!));
        var legacy = ThrownAtCall(() => IAsyncEnumerableLegacyExtensions.AnyAsync(new ScriptedSource([1], -1, false).Async(), null!).AsTask());

        Assert.Equal(DescribeArgument(linq), DescribeArgument(legacy));
    }



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
        var shown = result is List<int> list ? "[" + string.Join(",", list) + "]" : result?.ToString() ?? "null";
        var failure = error is null ? "none" : error.GetType().Name + ": " + error.Message;

        return "result=" + shown + "; error=" + failure + "; pulled=" + source.Pulled + "; disposed=" + source.Disposed;
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
