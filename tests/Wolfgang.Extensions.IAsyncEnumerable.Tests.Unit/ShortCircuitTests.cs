using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.Unit;

/// <summary>
/// Pins the documented short-circuit contract (README: "Short-circuits on the
/// first element"): the emptiness checks pull at most one element from the
/// source, however long it is.
/// </summary>
public sealed class ShortCircuitTests
{
    [Fact]
    public async Task IsEmptyAsync_only_enumerates_first_item()
    {
        var enumerated = new List<int>();
        var source = TestSources.CreateTracking(enumerated, 1, 2, 3, 4, 5);

        var result = await source.IsEmptyAsync();

        Assert.False(result);
        Assert.Equal([1], enumerated);
    }



    [Fact]
    public async Task IsNullOrEmptyAsync_only_enumerates_first_item()
    {
        var enumerated = new List<int>();
        var source = TestSources.CreateTracking(enumerated, 1, 2, 3, 4, 5);

        var result = await source.IsNullOrEmptyAsync();

        Assert.False(result);
        Assert.Equal([1], enumerated);
    }



    [Fact]
    public async Task NoneAsync_only_enumerates_first_item()
    {
        var enumerated = new List<int>();
        var source = TestSources.CreateTracking(enumerated, 1, 2, 3, 4, 5);

        var result = await source.NoneAsync();

        Assert.False(result);
        Assert.Equal([1], enumerated);
    }
}
