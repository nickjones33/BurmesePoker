using System.Text.RegularExpressions;

using BurmesePoker.Tests.Web;

namespace BurmesePoker.Tests.Docs;

/// <summary>
/// The documentation set itself, for the tests that read it rather than the code.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>These documents are a running narrative and that is deliberate</b> (BUILD-PLAN P34):
/// they accumulate, they keep superseded reasoning, and three packets have been shaped by
/// re-reading why a wrong default was taken. <b>Nothing here may flatten that.</b> What it does
/// instead is make the joins checkable — a document nobody lists, a command that no longer
/// runs, a number that disagrees with the file it was copied from, a fence that names a test
/// which has been renamed away.
/// </para>
/// <para>
/// ⚠️ <b>The narrative form is what makes the current fact findable.</b> Every one of these
/// files is written newest-first, so <em>the first count in the file is the current count</em> —
/// which is the property <c>PublishedFigureTests</c> asserts rather than trying to date every
/// figure in a session log.
/// </para>
/// </remarks>
internal static class Documentation
{
    /// <summary>The repository root.</summary>
    internal static DirectoryInfo Root { get; } = Sources.Root;

    /// <summary>
    /// Every document this project maintains: the front door, the guidance file, everything
    /// under <c>docs/</c>, and the skill that drives a work cycle.
    /// </summary>
    internal static IReadOnlyList<string> All { get; } =
    [
        "README.md",
        "CLAUDE.md",
        .. Paths(Path.Combine(Root.FullName, "docs")),
        .. Paths(Path.Combine(Root.FullName, ".claude"))
    ];

    /// <summary>
    /// The documents the map is answerable for: everything in <see cref="All"/> except
    /// <c>CLAUDE.md</c>, which is the file the map lives in.
    /// </summary>
    internal static IReadOnlyList<string> Mapped { get; } =
        [.. All.Where(path => path != "CLAUDE.md")];

    /// <summary>One document, by repository-relative path.</summary>
    internal static string Text(string relative) =>
        File.ReadAllText(Path.Combine(Root.FullName, relative.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>
    /// Every command line printed in a fenced <c>bash</c> block, with the document that prints
    /// it and the comment taken off the end.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A command a visitor can type is the most expensive kind of stale</b>, because they
    /// try it. <c>--seat</c> became <c>--people</c> when the browser grew a lobby and prose
    /// elsewhere went on saying <c>--seat</c> for packets afterwards.
    /// </remarks>
    internal static IReadOnlyList<(string Document, string Command)> Commands { get; } = Fenced();

    private static IEnumerable<string> Paths(string directory) =>
        new DirectoryInfo(directory)
            .GetFiles("*.md", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Root.FullName, file.FullName).Replace(Path.DirectorySeparatorChar, '/'))
            .OrderBy(path => path, StringComparer.Ordinal);

    private static IReadOnlyList<(string, string)> Fenced()
    {
        var found = new List<(string, string)>();

        foreach (var document in All)
        {
            foreach (Match block in Regex.Matches(Text(document), "```bash\n(.*?)```", RegexOptions.Singleline))
            {
                foreach (var line in block.Groups[1].Value.Split('\n'))
                {
                    // The comment after a command is prose about it, and several of them
                    // contain flags that are being *described* rather than run.
                    var line_ = Regex.Replace(line, "#.*$", string.Empty);

                    // ⚠️ `dotnet build && dotnet test` is two commands, and checking only the
                    // first is how the second one would rot unwatched.
                    foreach (var command in line_.Split("&&").Select(one => one.Trim()).Where(one => one.Length > 0))
                    {
                        found.Add((document, command));
                    }
                }
            }
        }

        return found;
    }
}
