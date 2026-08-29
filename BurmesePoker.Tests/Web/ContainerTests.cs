using System.Text.RegularExpressions;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>P51 — the image, held to the app it is supposed to carry.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The failure this exists for renders a perfect page.</b> Behind a TLS-terminating
/// proxy an app that does not read the forwarded headers computes its redirects and its
/// antiforgery tokens against the wrong scheme and the wrong host, and the result is a table
/// that draws correctly and a button that does nothing — the shape P13.6 already met once,
/// with two antiforgery tokens instead of one, and which no proxy label can fix from outside.
/// </para>
/// <para>
/// ⚠️ <b>These are source scans, in the idiom of <c>JackpotSpokenTests</c>.</b> Nothing here
/// runs the container: a build and a real browser round in it are the packet's acceptance and
/// belong to a person with a docker daemon. What a test can hold is the join — that the image
/// builds the framework the project targets, that the port it exposes is the port it listens
/// on, and that the two lines a proxy needs are actually in <c>Program.cs</c>.
/// </para>
/// </remarks>
public class ContainerTests
{
    private static string Dockerfile { get; } =
        File.ReadAllText(Path.Combine(Sources.Root.FullName, "Dockerfile"));

    private static string Program { get; } = Sources.Read("Program.cs");

    /// <summary>
    /// The publishing workflow, with its commentary taken out.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Written after the first version of this fence failed on the workflow's own
    /// comment.</b> The file explains, in prose, the trap the assertion below is looking for —
    /// so a scan that read the comments would redden on the most carefully documented file
    /// there is. <c>Sources.Markup</c> makes exactly this point and strips <c>//</c> and
    /// <c>&lt;!-- --&gt;</c>; YAML's <c>#</c> is a fourth comment syntax it does not know.
    /// ⚠️ Only a <c>#</c> that starts its line is a comment here, so a <c>#</c> inside a value
    /// survives.
    /// </remarks>
    private static string Workflow { get; } =
        Regex.Replace(
            File.ReadAllText(Path.Combine(
                Sources.Root.FullName, ".gitea", "workflows", "publish-image.yml")),
            @"^[ \t]*#.*$",
            string.Empty,
            RegexOptions.Multiline);

    /// <summary>
    /// ✅ <b>The image is built and run on the framework the browser client targets.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A framework bump is exactly the change that would rot this quietly</b> — the tree
    /// would move to <c>net11.0</c>, the tests would stay green, and the image would go on
    /// publishing against an SDK that no longer builds it. The tags are read off the Dockerfile
    /// and the moniker off the csproj, so neither can be a copy of the other.
    /// </remarks>
    [Fact]
    public void TheImageIsBuiltAndRunOnTheFrameworkTheBrowserClientTargets()
    {
        var targeted = Regex.Match(
            File.ReadAllText(Path.Combine(Sources.Web.FullName, "BurmesePoker.Web.csproj")),
            @"<TargetFramework>net([\d.]+)</TargetFramework>");

        Assert.True(targeted.Success, "BurmesePoker.Web names no target framework.");

        foreach (var image in new[] { "sdk", "aspnet" })
        {
            var tagged = Regex.Match(Dockerfile, $@"FROM mcr\.microsoft\.com/dotnet/{image}:([\d.]+)");

            Assert.True(tagged.Success, $"the Dockerfile has no `{image}` stage; a Blazor Server app needs both.");

            Assert.Equal(targeted.Groups[1].Value, tagged.Groups[1].Value);
        }
    }

    /// <summary>
    /// ✅ <b>The port the image exposes is the port the app is told to listen on.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>And it is <c>0.0.0.0</c>, never <c>localhost</c></b>: a container's loopback is its
    /// own, so an app bound to it answers nobody through a published port and nobody through a
    /// proxy. The two numbers are read separately and compared, because a Traefik label in
    /// <c>ansible-nas</c> names the container port and would be wrong the moment they diverged.
    /// </remarks>
    [Fact]
    public void ThePortTheImageExposesIsThePortTheAppListensOn()
    {
        var urls = Regex.Match(Dockerfile, @"ENV ASPNETCORE_URLS=http://(?<host>[\d.]+):(?<port>\d+)");

        Assert.True(urls.Success, "the Dockerfile does not set ASPNETCORE_URLS to a plain http:// address.");
        Assert.Equal("0.0.0.0", urls.Groups["host"].Value);

        var exposed = Regex.Match(Dockerfile, @"EXPOSE (\d+)");

        Assert.True(exposed.Success, "the Dockerfile exposes no port.");
        Assert.Equal(urls.Groups["port"].Value, exposed.Groups[1].Value);
    }

    /// <summary>
    /// 🔥 <b>The forwarded headers are read, and read before anything that reads the scheme.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Order is the whole of it.</b> <c>UseForwardedHeaders</c> after
    /// <c>UseAntiforgery</c> or after the endpoints is the same defect as leaving it out, and
    /// it is not a defect any test that only asked <em>is it present</em> could see.
    /// </remarks>
    [Fact]
    public void TheAppReadsWhatAProxyForwardsBeforeAnythingReadsTheSchemeOrTheHost()
    {
        var forwarded = Program.IndexOf("app.UseForwardedHeaders(", StringComparison.Ordinal);

        Assert.True(
            forwarded >= 0,
            "BurmesePoker.Web does not call UseForwardedHeaders. Behind a TLS-terminating proxy the page "
            + "renders perfectly and the buttons do nothing (BUILD-PLAN P51).");

        foreach (var later in new[] { "app.UseAntiforgery(", "app.MapStaticAssets(", "app.MapRazorComponents<" })
        {
            var at = Program.IndexOf(later, StringComparison.Ordinal);

            Assert.True(at >= 0, $"BurmesePoker.Web no longer calls `{later}`; this ordering check is stale.");
            Assert.True(
                forwarded < at,
                $"UseForwardedHeaders is called after `{later}`, which reads the scheme and the host. "
                + "It must come first, or it does nothing for the thing that needed it.");
        }
    }

    /// <summary>
    /// ✅ <b>There is one cheap URL that answers without touching a table.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ A restart policy and an ingress probe both need one, and neither may deal a round to
    /// find out whether the process is alive.
    /// </remarks>
    [Fact]
    public void TheAppAnswersAHealthCheckThatTouchesNoTable()
    {
        var health = Regex.Match(Program, @"app\.MapGet\(""/healthz"",\s*(?<handler>[^;]*)\);", RegexOptions.Singleline);

        Assert.True(health.Success, "BurmesePoker.Web maps no /healthz endpoint (BUILD-PLAN P51).");

        Assert.DoesNotContain("Lobby", health.Groups["handler"].Value, StringComparison.Ordinal);
    }

    /// <summary>
    /// 🔥 <b>The image's publish restores with the sources present, and <c>--no-restore</c> is
    /// the bug.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>Measured, not argued</b> (P51): copy the csproj files, restore, copy the sources,
    /// publish <c>--no-restore</c> — the standard layer-caching idiom — and the published app
    /// comes out with no <c>wwwroot/_framework/blazor.web.js</c> at all; the endpoint manifest
    /// names it zero times against one when the publish restores for itself. The framework's
    /// own static web assets are resolved against a project the restore could not fully see.
    /// </para>
    /// <para>
    /// 🔥 <b>The symptom is P13.3's</b>: <c>MapStaticAssets</c> 404s the script that starts the
    /// circuit, so the table draws once, perfectly, and never moves again. Nothing in the tree
    /// can see it — the app is fine and the image is not — which is why the flag is fenced
    /// here rather than left to whoever next tidies a Dockerfile.
    /// </para>
    /// </remarks>
    [Fact]
    public void ThePublishThatMakesTheImageRestoresWithTheSourcesInFrontOfIt()
    {
        var publish = Regex.Match(Dockerfile, @"RUN dotnet publish .*");

        Assert.True(publish.Success, "the Dockerfile publishes nothing.");

        Assert.DoesNotContain("--no-restore", publish.Value, StringComparison.Ordinal);
    }

    /// <summary>
    /// ✅ <b>P52 — the workflow publishes <em>this</em> image, not one of its own.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>A CI file is the obvious place for the trap to come back.</b> The publish is one
    /// line and <c>--no-restore</c> reads like a free speed-up to anyone who has not met
    /// P51's finding; a workflow that rewrote the build would ship a table that renders
    /// perfectly and never moves, and the tree would stay green.
    /// </para>
    /// <para>
    /// ⚠️ <b>The runner label is checked because the wrong one does not fail — it queues.</b>
    /// Only the <c>docker-builder</c> runner has the docker socket passed through; on any
    /// other label the job waits for a runner that is never coming, which reads as a slow
    /// build rather than a broken one.
    /// </para>
    /// </remarks>
    [Fact]
    public void ThePublishingWorkflowBuildsThisRepositorysOwnImage()
    {
        Assert.Contains("runs-on: docker-builder", Workflow, StringComparison.Ordinal);

        Assert.DoesNotContain("--no-restore", Workflow, StringComparison.Ordinal);

        // The tag the Ansible role pulls (BUILD-PLAN P53). A rename here is a role that
        // quietly goes on running last week's image.
        Assert.Contains(
            "gitea.nickjones.dev/nickjones/burmesepoker:latest",
            Workflow,
            StringComparison.Ordinal);

        // The one request that separates a working image from a perfect-looking dead one.
        Assert.Contains("_framework/blazor.web.js", Workflow, StringComparison.Ordinal);
    }

    /// <summary>
    /// ✅ <b>P54 — the seat's patience outlasts the circuit the framework is holding for it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Two numbers in two files that only mean anything together.</b> Inside
    /// <c>DisconnectedCircuitRetentionPeriod</c> the framework is deliberately hiding a dropped
    /// connection from the player — the overlay is up, the board is kept, and coming back is
    /// silent. A patience shorter than that window would have the computer play the turn of
    /// somebody the framework is still expecting back, and they would return to a table that had
    /// moved on for no reason they could see. A phone that loses signal in a lift is the case.
    /// </para>
    /// <para>
    /// ⚠️ <b>One side is read and the other is run.</b> The retention comes off
    /// <c>Program.cs</c>, because an option set on a builder is not reachable from a test that
    /// does not start the host; the patience is the real <c>Lobby</c>'s. So neither number can be
    /// a copy of the other.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task TheSeatsPatienceOutlastsTheCircuitTheFrameworkHolds()
    {
        var retention = Regex.Match(
            Program,
            @"DisconnectedCircuitRetentionPeriod\s*=\s*TimeSpan\.From(?<unit>\w+)\((?<of>[0-9.]+)\)");

        Assert.True(retention.Success, "Program.cs no longer sets DisconnectedCircuitRetentionPeriod.");

        var held = retention.Groups["unit"].Value switch
        {
            "Seconds" => TimeSpan.FromSeconds(double.Parse(retention.Groups["of"].Value, Culture)),
            "Minutes" => TimeSpan.FromMinutes(double.Parse(retention.Groups["of"].Value, Culture)),
            var other => throw new InvalidOperationException($"No arm here for TimeSpan.From{other}.")
        };

        await using var lobby = LobbyTests.Open();

        Assert.True(
            lobby.Opening.Patience > held,
            $"A browser table's patience is {lobby.Opening.Patience} against a retained circuit of {held}.");
    }

    /// <summary>
    /// ✅ <b>P54 — something actually calls the reaper.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The failure this catches is a correct method nobody runs</b>, which is the state
    /// the packet found: <c>Lobby.Close</c> was written in P13.6 and only the tests ever called
    /// it. A reaper that is never registered leaks exactly as much as no reaper at all, and
    /// nothing else in the tree would notice.
    /// </remarks>
    [Fact]
    public void TheSiteRunsTheSweepThatClosesIdleTables()
    {
        Assert.Contains("AddHostedService<TableSweeper>()", Program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton(TimeProvider.System)", Program, StringComparison.Ordinal);
        Assert.Contains("ReapIdleTables", Sources.Read("TableSweeper.cs"), StringComparison.Ordinal);
    }

    private static System.Globalization.CultureInfo Culture => System.Globalization.CultureInfo.InvariantCulture;
}
