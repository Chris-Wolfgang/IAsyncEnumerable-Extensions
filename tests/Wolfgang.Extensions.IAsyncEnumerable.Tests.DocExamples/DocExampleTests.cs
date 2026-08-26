namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.DocExamples;

/// <summary>
/// Compiles every XML-doc &lt;example&gt;&lt;code&gt; block in the library's public
/// API against the real assembly, so a renamed/removed member the docs
/// still reference fails the build instead of drifting silently (#237).
/// </summary>
/// <remarks>
/// Limitation: only identifiers explicitly stubbed as typed fields on
/// <see cref="DocExampleContext"/> (in <c>DocExampleCompiler</c>) resolve.
/// If a future example introduces a new placeholder identifier, add a
/// typed field for it there — the compile error you'd otherwise see
/// (<c>CS0103: name does not exist in the current context</c>) is
/// indistinguishable from a real doc-rot failure without checking.
/// </remarks>
public sealed class DocExampleTests
{
    private static readonly IReadOnlyList<DocExample> Examples = DocExampleSource.ExtractAll();



    public static IEnumerable<object[]> ExampleCases() => Examples.Select(example => new object[] { example });



    [Theory]
    [MemberData(nameof(ExampleCases))]
    public void Example_compiles(DocExample example)
    {
        ArgumentNullException.ThrowIfNull(example);

        var errors = DocExampleCompiler.Compile(example);

        Assert.True
        (
            errors.Count == 0,
            $"{example} failed to compile:{Environment.NewLine}" +
            string.Join(Environment.NewLine, errors.Select(error => error.ToString()))
        );
    }



    [Fact]
    public void ExtractAll_when_examples_exist_finds_at_least_the_known_count()
    {
        // Floor guard: if the extractor's <example>/<code> line-matching
        // ever breaks (e.g. a doc-comment format change it doesn't
        // handle), this fails LOUD instead of the Theory above silently
        // running zero cases and reporting a vacuous pass.
        Assert.True
        (
            Examples.Count >= 8,
            $"Expected at least 8 <example> blocks, found {Examples.Count}. " +
            "If this is a real removal, lower the floor deliberately — don't just delete this test."
        );
    }
}
