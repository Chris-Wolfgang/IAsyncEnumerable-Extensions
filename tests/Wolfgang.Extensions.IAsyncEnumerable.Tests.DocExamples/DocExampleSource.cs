using System.IO;
using System.Net;
using System.Text;
using Xunit.Abstractions;

namespace Wolfgang.Extensions.IAsyncEnumerable.Tests.DocExamples;

/// <summary>
/// One extracted XML-doc &lt;example&gt;&lt;code&gt; block. Implements
/// <see cref="IXunitSerializable"/> so it can flow through
/// <c>[Theory]</c>/<c>[MemberData]</c> with a readable
/// <c>File:Line</c> display name per case.
/// </summary>
public sealed class DocExample : IXunitSerializable
{
    /// <summary>Absolute path of the source file the example came from.</summary>
    public string FilePath { get; private set; } = string.Empty;



    /// <summary>1-based line number of the first line of code inside &lt;code&gt;.</summary>
    public int FirstCodeLine { get; private set; }



    /// <summary>The de-commented, HTML-decoded C# snippet.</summary>
    public string Code { get; private set; } = string.Empty;



    /// <summary>Required by <see cref="IXunitSerializable"/> — do not remove.</summary>
    public DocExample()
    {
    }



    public DocExample(string filePath, int firstCodeLine, string code)
    {
        FilePath = filePath;
        FirstCodeLine = firstCodeLine;
        Code = code;
    }



    public void Deserialize(IXunitSerializationInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        FilePath = info.GetValue<string>(nameof(FilePath));
        FirstCodeLine = info.GetValue<int>(nameof(FirstCodeLine));
        Code = info.GetValue<string>(nameof(Code));
    }



    public void Serialize(IXunitSerializationInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        info.AddValue(nameof(FilePath), FilePath);
        info.AddValue(nameof(FirstCodeLine), FirstCodeLine);
        info.AddValue(nameof(Code), Code);
    }



    public override string ToString() => $"{Path.GetFileName(FilePath)}:{FirstCodeLine}";
}



/// <summary>
/// Locates the library's source directory and extracts every XML-doc
/// &lt;example&gt;&lt;code&gt; block for compilation.
/// </summary>
public static class DocExampleSource
{
    private const string LibraryDirectoryName = "Wolfgang.Extensions.IAsyncEnumerable";



    /// <summary>
    /// Extracts every &lt;example&gt;&lt;code&gt; block from every .cs file under the
    /// library's src directory.
    /// </summary>
    public static IReadOnlyList<DocExample> ExtractAll()
    {
        var srcDir = FindSrcDirectory();
        var examples = new List<DocExample>();

        foreach (var file in Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.TopDirectoryOnly))
        {
            examples.AddRange(ExtractFromFile(file));
        }

        return examples;
    }



    /// <summary>
    /// Walks up from <see cref="AppContext.BaseDirectory"/> looking for
    /// <c>src/Wolfgang.Extensions.IAsyncEnumerable/</c>. Deliberately NOT
    /// <c>[CallerFilePath]</c> — that bakes in a build-machine path that
    /// resolves to a deterministic-build placeholder (e.g. <c>/_/...</c>)
    /// under CI, not a real filesystem path.
    /// </summary>
    private static string FindSrcDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", LibraryDirectoryName);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate src/{LibraryDirectoryName}/ by walking up from '{AppContext.BaseDirectory}'.");
    }



    private static IEnumerable<DocExample> ExtractFromFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var inExample = false;
        var inCode = false;
        var codeStartLine = -1;
        var codeLines = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();

            if (!inExample)
            {
                if (trimmed.StartsWith("/// <example>", StringComparison.Ordinal))
                {
                    inExample = true;
                }

                continue;
            }

            if (!inCode)
            {
                if (trimmed.StartsWith("/// <code>", StringComparison.Ordinal))
                {
                    inCode = true;
                    codeStartLine = i + 2;  // 1-based line number of the line AFTER <code>
                    codeLines.Clear();
                }
                else if (trimmed.StartsWith("/// </example>", StringComparison.Ordinal))
                {
                    inExample = false;
                }

                continue;
            }

            if (trimmed.StartsWith("/// </code>", StringComparison.Ordinal))
            {
                inCode = false;
                yield return new DocExample(filePath, codeStartLine, DecodeSnippet(codeLines));
                continue;
            }

            codeLines.Add(StripDocCommentPrefix(trimmed));
        }
    }



    private static string StripDocCommentPrefix(string trimmedLine)
    {
        // trimmedLine starts with "///" — strip it plus at most one following space.
        var afterSlashes = trimmedLine[3..];
        return afterSlashes.StartsWith(' ') ? afterSlashes[1..] : afterSlashes;
    }



    private static string DecodeSnippet(IReadOnlyList<string> codeLines)
    {
        var builder = new StringBuilder();
        foreach (var line in codeLines)
        {
            builder.AppendLine(WebUtility.HtmlDecode(line));
        }

        return builder.ToString();
    }
}
