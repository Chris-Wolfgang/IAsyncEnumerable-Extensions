using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wolfgang.Extensions.IAsyncEnumerable;

// Native-AOT / trim smoke test (#238). Exercises every public method on
// IAsyncEnumerableExtensions under a PublishAot + PublishTrimmed publish and
// asserts real results, so a trimmed member shows up as a non-zero exit
// instead of a silent no-op. This library has no reflection, no
// Expression.Compile, and no attribute-driven mapping — unlike the ETL
// family's reflection-based mappers, no [DynamicallyAccessedMembers] /
// TrimmerRootAssembly should be necessary here. The smoke exists to prove
// that, not assume it.

var failures = new List<string>();

await CheckAsync("ChunkAsync", async () =>
{
    var chunks = new List<int[]>();
    await foreach (var chunk in Range(1, 10).ChunkAsync(3))
    {
        chunks.Add(chunk.ToArray());
    }

    Assert(chunks.Count == 4, $"expected 4 chunks, got {chunks.Count}");
    Assert(chunks[0].SequenceEqual([1, 2, 3]), "chunk 0 mismatch");
    Assert(chunks[3].SequenceEqual([10]), "final partial chunk mismatch");
});

await CheckAsync("DoAsync(Action<T>)", async () =>
{
    var seen = new List<int>();
    var yielded = new List<int>();
    await foreach (var item in Range(1, 5).DoAsync(x => seen.Add(x)))
    {
        yielded.Add(item);
    }

    Assert(seen.SequenceEqual(yielded), "DoAsync(Action<T>) side-effect/yield mismatch");
    Assert(yielded.SequenceEqual([1, 2, 3, 4, 5]), "DoAsync(Action<T>) yielded wrong sequence");
});

await CheckAsync("DoAsync(Func<T, Task>)", async () =>
{
    var seen = new List<int>();
    await foreach (var item in Range(1, 5).DoAsync(async x =>
                   {
                       await Task.Yield();
                       seen.Add(x);
                   }))
    {
        _ = item;
    }

    Assert(seen.SequenceEqual([1, 2, 3, 4, 5]), "DoAsync(Func<T, Task>) side-effect mismatch");
});

await CheckAsync("ForEachAsync(Action<T>)", async () =>
{
    var sum = 0;
    await Range(1, 5).ForEachAsync(x => sum += x);
    Assert(sum == 15, $"expected sum 15, got {sum}");
});

await CheckAsync("ForEachAsync(Func<T, Task>)", async () =>
{
    var sum = 0;
    await Range(1, 5).ForEachAsync(async x =>
    {
        await Task.Yield();
        sum += x;
    });
    Assert(sum == 15, $"expected sum 15, got {sum}");
});

await CheckAsync("IsEmptyAsync", async () =>
{
    Assert(await Range(0, 0).IsEmptyAsync(), "empty source should report IsEmptyAsync == true");
    Assert(!await Range(1, 1).IsEmptyAsync(), "non-empty source should report IsEmptyAsync == false");
});

await CheckAsync("IsNullOrEmptyAsync", async () =>
{
    IAsyncEnumerable<int>? nullSource = null;
    Assert(await nullSource.IsNullOrEmptyAsync(), "null source should report IsNullOrEmptyAsync == true");
    Assert(await Range(0, 0).IsNullOrEmptyAsync(), "empty source should report IsNullOrEmptyAsync == true");
    Assert(!await Range(1, 1).IsNullOrEmptyAsync(), "non-empty source should report IsNullOrEmptyAsync == false");
});

await CheckAsync("NoneAsync()", async () =>
{
    Assert(await Range(0, 0).NoneAsync(), "empty source should report NoneAsync() == true");
    Assert(!await Range(1, 1).NoneAsync(), "non-empty source should report NoneAsync() == false");
});

await CheckAsync("NoneAsync(predicate)", async () =>
{
    Assert(await Range(1, 5).NoneAsync(x => x > 100), "no element satisfies x > 100");
    Assert(!await Range(1, 5).NoneAsync(x => x == 3), "element 3 satisfies the predicate");
});

if (failures.Count > 0)
{
    Console.Error.WriteLine($"AOT smoke FAILED ({failures.Count} check(s)):");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"  - {failure}");
    }

    return 1;
}

Console.WriteLine("AOT smoke OK — all public methods verified under PublishAot + PublishTrimmed.");
return 0;

async Task CheckAsync(string name, Func<Task> check)
{
    try
    {
        await check();
        Console.WriteLine($"  [ok] {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.Error.WriteLine($"  [FAIL] {name}: {ex.Message}");
    }
}

void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static async IAsyncEnumerable<int> Range(int start, int count)
{
    for (var i = 0; i < count; i++)
    {
        await Task.Yield();
        yield return start + i;
    }
}
