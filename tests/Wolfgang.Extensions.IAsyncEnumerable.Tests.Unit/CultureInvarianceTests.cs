using System.Globalization;

namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

/// <summary>
/// Empirically proves the culture-invariance claim in
/// docs/CULTURE-INVARIANCE.md (#240) rather than just asserting it in
/// prose: every public method is exercised under a set of hostile
/// non-en-US cultures (Turkish dotted-I, German decimal-comma, Arabic
/// RTL/Hindi-Arabic digits, Japanese full-width digits, Chinese
/// collation) and must produce results identical to the culture-invariant
/// baseline.
/// </summary>
/// <remarks>
/// This library has zero culture-sensitive public surface — no
/// <c>ToString()</c>, string formatting, string comparison, or
/// numeric-parsing call sites in <see cref="IAsyncEnumerableExtensions"/>.
/// Every method operates on generic <c>T</c> purely structurally
/// (enumerate, invoke a side-effect delegate, count). These tests exist to
/// catch a *future* regression — a contributor adding a culture-sensitive
/// operation without realizing it — not because today's implementation is
/// at risk.
/// </remarks>
public sealed class CultureInvarianceTests
{
    public static IEnumerable<object[]> HostileCultures() =>
    [
        [CultureInfo.GetCultureInfo("tr-TR")],  // dotted/dotless I casing trap
        [CultureInfo.GetCultureInfo("de-DE")],  // decimal comma
        [CultureInfo.GetCultureInfo("ar-SA")],  // RTL + Hindi-Arabic digit shapes
        [CultureInfo.GetCultureInfo("ja-JP")],  // full-width digits
        [CultureInfo.GetCultureInfo("zh-CN")]   // collation
    ];



    [Theory]
    [MemberData(nameof(HostileCultures))]
    public async Task ChunkAsync_under_hostile_culture_matches_invariant_result(CultureInfo culture)
    {
        await RunUnderCultureAsync(culture, async () =>
        {
            var chunks = new List<int[]>();
            await foreach (var chunk in CreateSource(1, 37).ChunkAsync(10))
            {
                chunks.Add([.. chunk]);
            }

            Assert.Equal(4, chunks.Count);
            Assert.Equal(Enumerable.Range(1, 37), chunks.SelectMany(c => c));
        });
    }



    [Theory]
    [MemberData(nameof(HostileCultures))]
    public async Task DoAsync_under_hostile_culture_matches_invariant_result(CultureInfo culture)
    {
        await RunUnderCultureAsync(culture, async () =>
        {
            var seen = new List<int>();
            var yielded = new List<int>();
            await foreach (var item in CreateSource(1, 5).DoAsync(x => seen.Add(x)))
            {
                yielded.Add(item);
            }

            Assert.Equal(Enumerable.Range(1, 5), seen);
            Assert.Equal(Enumerable.Range(1, 5), yielded);
        });
    }



    [Theory]
    [MemberData(nameof(HostileCultures))]
    public async Task ForEachAsync_under_hostile_culture_matches_invariant_result(CultureInfo culture)
    {
        await RunUnderCultureAsync(culture, async () =>
        {
            var sum = 0;
            await CreateSource(1, 5).ForEachAsync(x => sum += x);
            Assert.Equal(15, sum);
        });
    }



    [Theory]
    [MemberData(nameof(HostileCultures))]
    public async Task IsEmptyAsync_under_hostile_culture_matches_invariant_result(CultureInfo culture)
    {
        await RunUnderCultureAsync(culture, async () =>
        {
            Assert.True(await CreateSource().IsEmptyAsync());
            Assert.False(await CreateSource(1).IsEmptyAsync());
        });
    }



    [Theory]
    [MemberData(nameof(HostileCultures))]
    public async Task NoneAsync_predicate_under_hostile_culture_matches_invariant_result(CultureInfo culture)
    {
        await RunUnderCultureAsync(culture, async () =>
        {
            Assert.True(await CreateSource(1, 2, 3).NoneAsync(x => x > 100));
            Assert.False(await CreateSource(1, 2, 3).NoneAsync(x => x == 2));
        });
    }



    private static async Task RunUnderCultureAsync(CultureInfo culture, Func<Task> test)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        try
        {
            await test();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }



    private static async IAsyncEnumerable<int> CreateSource(params int[] items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }



    private static async IAsyncEnumerable<int> CreateSource(int start, int end)
    {
        for (var i = start; i <= end; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}
