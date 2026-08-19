using BurmesePoker.Web;
using BurmesePoker.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// ⚠️ Interactivity is a *component's* opt-in, never the app's (BUILD-PLAN §3.11 C12). Adding
// the server components here makes InteractiveServer available to whoever asks for it; the
// shell, the rules and the settlement stay static SSR, and the render mode is named on the
// table component and nowhere near the root. This is the one decision on the list that cannot
// be walked back cheaply.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// One table for the site, dealing itself round after round. P13.5 makes this a lobby.
builder.Services.AddSingleton<TableHost>();

var app = builder.Build();

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

// ⚠️ A table nobody is playing deals from the moment the site is up, so the first person to
// open the page walks into a round in progress rather than starting one — a table is a place,
// not a button. A table with a *seat* in it waits, because every question that seat is asked
// spends the whole of its patience before the stand-in answers, and an unattended round would
// be over an hour of nothing (P13.4).
var table = app.Services.GetRequiredService<TableHost>();

if (table.Yours is null)
{
    table.Start();
}

app.Run();
