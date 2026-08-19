using System.Text.RegularExpressions;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// The tree itself, for the tests that read source rather than run it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three of §3.11's five mechanical standards are checks on markup and stylesheets</b>
/// (BUILD-PLAN P13.3): contrast is computed from the palette a browser actually loads, real
/// controls are a source scan "in the same spirit as <c>LayeringTests</c>", and the render
/// mode is a line in a <c>.razor</c> file. None of those is reachable from a compiled
/// assembly, so the files are read from the tree.
/// </para>
/// <para>
/// ⚠️ <b>The palette is read from the stylesheet and not from a copy in C#.</b> A copy would
/// let the two drift, and the drifted one would be the one on screen.
/// </para>
/// </remarks>
internal static class Sources
{
    /// <summary>The repository root — the directory holding the solution.</summary>
    internal static DirectoryInfo Root { get; } = Find();

    /// <summary>The browser client's project directory.</summary>
    internal static DirectoryInfo Web { get; } = new(Path.Combine(Root.FullName, "BurmesePoker.Web"));

    /// <summary>Every Razor component in the browser client, path and markup.</summary>
    internal static IReadOnlyList<(string Path, string Text)> Components { get; } =
    [
        .. Web.GetFiles("*.razor", SearchOption.AllDirectories)
            .OrderBy(file => file.FullName, StringComparer.Ordinal)
            .Select(file => (Path.GetRelativePath(Root.FullName, file.FullName), Markup(File.ReadAllText(file.FullName))))
    ];

    /// <summary>One file of the browser client, by path relative to its project directory.</summary>
    internal static string Read(string relative) =>
        Markup(File.ReadAllText(Path.Combine(Web.FullName, relative.Replace('/', Path.DirectorySeparatorChar))));

    /// <summary>
    /// A file with its commentary taken out.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A scan must read the markup and not the prose about the markup.</b> Every one of
    /// these components explains, in a comment, the standard it is obeying — <em>"there is
    /// deliberately no <c>@rendermode</c> here"</em>, <em>"polite, never assertive"</em> — and
    /// a scan that read those would fail on the very files that are most careful. Razor
    /// comments, block comments and whole-line <c>//</c> comments go; a <c>//</c> inside a URL
    /// stays, because only a comment that starts its line is treated as one.
    /// </remarks>
    internal static string Markup(string text)
    {
        text = Regex.Replace(text, @"@\*.*?\*@", string.Empty, RegexOptions.Singleline);
        text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        text = Regex.Replace(text, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(text, @"^[ \t]*//.*$", string.Empty, RegexOptions.Multiline);
    }

    private static DirectoryInfo Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BurmesePoker.slnx")))
        {
            directory = directory.Parent;
        }

        return directory
            ?? throw new InvalidOperationException(
                $"No BurmesePoker.slnx above {AppContext.BaseDirectory}, so the tree cannot be read.");
    }
}
