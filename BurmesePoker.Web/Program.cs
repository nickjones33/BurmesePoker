using BurmesePoker.Web;
using BurmesePoker.Web.Components;

using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ⚠️ Interactivity is a *component's* opt-in, never the app's (BUILD-PLAN §3.11 C12). Adding
// the server components here makes InteractiveServer available to whoever asks for it; the
// shell, the rules and the settlement stay static SSR, and the render mode is named on the
// table component and nowhere near the root. This is the one decision on the list that cannot
// be walked back cheaply.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The tables this site is hosting. ⚠️ A lobby rather than one table (BUILD-PLAN P13.6): a
// second table is a dictionary of them and a route that names one, and nothing else in the
// client counts tables.
builder.Services.AddSingleton<Lobby>();

// 🔥 What a TLS-terminating proxy in front of this app forwards, and nothing else can supply
// (BUILD-PLAN P51). Behind Traefik the app sees a plain HTTP request from another container:
// without these headers it computes redirects and antiforgery tokens against http:// and the
// container's own host, and the page renders perfectly while the button does nothing — the
// exact failure class P13.6 met with the doubled antiforgery token.
// ⚠️ The known networks and proxies are cleared deliberately: the proxy is a container on a
// docker network whose address is assigned at run time, so there is no address to trust in
// advance. That trusts whatever fronts the app, which is safe only because nothing but the
// proxy can reach it — do not publish this port straight to the internet.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ⚠️ First, and before anything that reads the scheme or the host — routing, antiforgery and
// the static-asset endpoints all do.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// ⚠️ MapStaticAssets, not UseStaticFiles: the framework's own files — blazor.web.js and the
// CSS-isolation bundle — are *static web assets*, described by a manifest the build writes,
// and UseStaticFiles serves none of them outside Development. It 404s the script that starts
// the circuit, so the page renders once and then never moves again. Found by asking for the
// file rather than by assuming it was there.
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// A restart policy and an ingress probe both need one cheap URL that answers without touching
// a table. It says nothing about the game on purpose: a health check is about the process.
app.MapGet("/healthz", () => Results.Text("ok"));

// One table is open from boot, from the same command line the console takes, so that
// `dotnet run --project BurmesePoker.Web` is a game rather than an empty room with a form in
// it. ⚠️ It does not *deal* from boot: a table deals while somebody is at it and every seat is
// either the computer's or somebody's (BUILD-PLAN P13.6), which is the honest answer P13.4 had
// no way to give — every question an empty seat is asked spends the whole of its patience
// before the stand-in plays, and an unattended round is over an hour of nothing.
app.Services.GetRequiredService<Lobby>().OpenTheHouseTable();

app.Run();
