# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## ⚠️ START HERE

**Run `/poker`.** It encapsulates one full work cycle: orient from the plan, execute the next
build packet, update the docs, re-plan what follows, commit, and report. Defined in
`.claude/skills/poker/SKILL.md`. It is the intended way to work on this project — prefer it
over ad-hoc changes.

🔥 **READ THIS FIRST — the plan grew eight entries on 2026-08-23, at Nick's direction:
`P43`–`P50`, the strategy frontier and the writing-down** (`BUILD-PLAN.md` §5, one model
recommendation per packet). ✅ **P43–P50 are all done (below). `P50` (the documentation cleanup —
F10) was the last of them; the tree is green at 941.**
🔥 **A fourth track was added 2026-08-28 at Nick's direction: `P51`–`P54`, *taking the table
online*** (`BUILD-PLAN.md` §5, `docs/HOSTING.md`). ⚠️ **It is ops, not the rules/strategy
programme** — no rule changes, `Domain` untouched by all four, **no suite regeneration owed**, and
no measurement can move. ✅ **`P51` (containerize), `P52` (a published image) and `P54` (long-lived-host hardening — idle
tables reaped, the copy-link affordance, and the patience joined to the circuit-retention window;
2026-08-29) have all shipped — see the blocks below**; ◐ **`P53` deployed 2026-08-30 — the table is up at `poker.nickjones.dev`, and it is ◐ only for
the browser sitting**: the role was built and committed in `ansible-nas` (`7ffd645e`), Nick ran
`ansible-playbook nas.yml --tags burmesepoker --become --ask-become-pass`, and the session verified
everything else. 🔥 **Both assumptions are settled as measurements**: Traefik proxies the
`/_blazor` upgrade (**101 Switching Protocols** from Kestrel, auth middleware attached) and
**nothing closes an idle circuit** (7½ minutes, 29 server pings at ~15.5 s, no close frame).
⚠️ **Only one of the two recorded blockers was ever real** — the passphrase-protected key was
already in the `ssh-agent`, so **check `ssh-add -l` before recording an SSH blocker**. ⚠️ **The
gating decision holds: a Traefik `basicauth` middleware, attached only when
`burmesepoker_basicauth_users` is non-empty** — a router naming a middleware with an empty users list
serves **503**, not a login prompt — and unauthenticated `/healthz` is a **401 with
`www-authenticate`**, a locked door. ✅ **The credential question is answered — a real browser
replays them**: Chrome past the basicauth dialog gets `_blazor/negotiate` **200** and an open
`GET /_blazor?id=…` (a `Request starting` with **no** `Request finished` — a WebSocket, not long
polling), and a round was played with the claim refused and the blind draw arriving privately.
⚠️ **Do not read an absent `/_blazor` on the lobby as a fault** — the lobby is static SSR and opens
no circuit; sit down at a table first. 🔥 **The finding that cost the most: the deployed image is
not built from `main`** — `:latest` is from `860fb13` (**P52**), `gitea/main` is **six commits
behind**, so the running site predates **P54** and **P56** and still draws P19's *mixed table*
checkbox. ✅ **Pushed and redeployed 2026-08-30** — `:latest` is built from `f309a9d` and the site now has
P54 and P56; the copy-link renders the **forwarded** host, the opponent menu shows ten rungs with
their margins, and the two-step per-seat form works. 🔥 **But pressing it found a live 500 and
`P57` is now the next packet**: the lobby offers **`random@0`** and cannot build it —
`DifficultyLevel.Create` always wraps its rung in a `FallibleAgent`, which demands
`IRanksDiscards`, and `RandomBotAgent` has none (P19), while P56's fence says a rung with a
published row **must** be offered. ⚠️ **Two packets' rules contradicting, not a typo**, and ε is 0
so the wrapper would never fire. **Nick decided option 2 on 2026-08-30: the menu stops offering a
rung that cannot name its second-best move** — ⚠️ **not** option 1 (unwrapping `Create` at ε = 0),
which would put `Domain` and every measurement in the blast radius. ✅ **`P57` shipped the same day
and it is one predicate**: `OpponentMenu.CanBeAskedForItsSecondBestMove(rung)` is
`rung.Create(0) is IRanksDiscards`, ⚠️ **asked of the agent rather than declared on the rung**
(the constructor that threw asks that exact question of that exact object). 🔥 **The menu excludes
on two grounds now** — *no published row* (honesty: a price that cannot be stated must not be
charged) and *cannot name a second-best move* (**P19's invariant**) — ⚠️ **and `random`'s row was
deleted from `Published` rather than left to be filtered**, though the rule is what keeps it out.
🔥 **The test that would have caught it is the packet**:
`OpeningATableTests.EveryOpponentTheLobbyOffersCanActuallyBeBuilt` **resolves *and constructs***
every name the form can post — levels included — because **resolution is exactly the step that
succeeded** while construction threw. Tree green at **929**; `Domain`, `Presentation`, `Server`,
`Console` and `Sim` byte-identical. ⚠️ **Not yet on the deployed site.** ⚠️ **`git push origin main` is
the CI trigger and the role's `pull: true` takes the image on the next play** — a packet marked
done here is not thereby a packet that is running; 🔥 **`P56` was added the same day —
*opening a table you actually want*, after Nick pointed out the deployed table has no seat for a
person in it.** ⚠️ **Check before building: the lobby form already offers seats, people, the four
levels, a mixed table and the seating policy**, so most of the ask is built; the fact that is not
written down anywhere else is that **`TablePlan.People` is a quorum** — `HostedTable.Ready` is
`_attending > 0 && _table.IsFull`, so a table deals only once *every* person-seat is claimed, and
`0` is a room you can watch and never join. 🔥 **Its two open questions were answered by Nick on 2026-08-29.**
**Personalities: the eleven `BotCatalog` rungs go behind an *advanced* control, each showing its
measured margin** — ⚠️ **a deliberate amendment to §3.12, to P19 and to `DifficultyLadder`'s own
doc comment** (which says *"a menu with both in it would be the mistake this design exists to
avoid"* and **must be rewritten, not contradicted**). The new rule is *levels are the menu; rungs
are an advanced disclosure that states its price*, and **the margin is the price** — read from
`measurements.csv`, fenced by `PublishedFigureTests`, and a rung with no published row is not
offerable. ✅ **No new resolution machinery**: `DifficultyLevel.Probe(rung, 0)` mints `<rung>@0`
and **public `DifficultyLadder.FindOrProbe` already resolves it**, so the change below the form is
`Find` → `FindOrProbe` where a *seat* is resolved — ⚠️ **`Find` stays `Find` for the
`--difficulty` shorthand**. **Per-seat difficulty: build the picker** — ⚠️ **the obstacle is
§3.11 C12** (static SSR cannot grow a control per seat as the count is typed, which is why `Mixed`
is a checkbox); **recommended shape is a two-step post**, and **the two answers are one control,
not two**; ✅ **`P55` shipped 2026-08-30 — Gitea is primary and GitHub is a
mirror Gitea force-pushes.** 🔥 **`origin` is now Gitea and `github` is GitHub**, so **the CI
trigger is `git push origin main`** and ⚠️ **never push to `github` by hand** — the mirror
force-pushes, so a commit landing there is erased rather than conflicting. ⚠️ **Forge operations
use `tea`, not `gh`** (`gh` was never installed here and points at the mirror). 🔥 **The
`pre-rewrite` tag never existed and the claim was corrected rather than the tag minted** — the
2023 tree is **`79d86bd`**, the parent of `b32d08b` (`P0: restructure and salvage`) — ⚠️ **nine
sites across five documents, including a runnable `git show pre-rewrite:…` in `STATUS.md` that had
been failing since P0**; and the sweep found **`git push gitea main` quoted as the CI trigger in
seven places**, all corrected. **A remote's name is documentation.**
⚠️ **`P53` is work in a *different repository*** (`~/source/repos/ansible-nas`,
plan already written at that repo's `docs/superpowers/plans/2026-08-28-burmesepoker-hosting.md`).
🔥 **The review that reshaped it: the homelab had already solved the hard part** — `ansible-nas`
runs Traefik with a wildcard `*.nickjones.dev` Let's Encrypt cert over Cloudflare DNS-01 and 80/443
already forwarded, so `HOSTING.md`'s original Cloudflare-Tunnel recommendation was **withdrawn** in
favour of the proxy already running (§5a). ⚠️ **Three traps recorded in advance**: `*_memory: 64m`
(what both hand-written roles use — **512m minimum** for a .NET server holding circuits), building
the image on the NAS instead of in Gitea Actions, and **WebSockets/idle timeouts being assumed
rather than proved**. ⚠️ **Docker and Ansible commands go in `text` fences, never `bash`.**
Besides that track, only `P40` (the Burmese rulebook, blocked on Nick's vetted text) stands open. In order: ✅ **P43 `opportunist`** (the
feeding ban at zero price — a predicted null, question closed), ✅ **P44 `purist`** (the clean
bonus at zero price — a predicted *positive* that came back a flat null), ✅ **P45 `angler`** (a
draw priced in cards — the predicted null, mechanism never armed; cap 32,768 → 65,536),
🔥 ✅ **P46 `sprinter`** (the endgame as a race — **the first rung to separate above `outs`,
`+1.2 ± 0.8` Holm, and the packet where the mechanism variable finally armed**; the crossing cap
was paid a third time, `65,536 → 131,072` for `10⁵ = 100,000`, a stated decision),
**P47** the blocked-rungs ledger (`jackpot`,
`streaker` — each needs an instrument that does not exist; **plus two anti-recommendations**:
no defence-refinement rungs, and no *paying* clean-bonus rung without new information),
🔥 ✅ **P48** the full verification and
measurement-hardening run — **F1–F7 discharged and `sprinter` graduated to *settled*** (fresh-seed
replication `+1.23 → +1.19`, both Holm; every Holm verdict held; `warden`'s loss is partly
self-play compounding; the money ladder agrees with the win-rate ladder; 214/235 rows byte-identical
with no mean moved). ✅ **The two raw-only casualties resolved**: `opportunist`-`angler` fell inside
(null), `sprinter`-`angler` firmed to Holm. `STRATEGY.md` §16. **This was not a rung packet** — the
diff is `Sim` + tests + docs only, no agent touched.
✅ **P49** `docs/SIMULATIONS.md` (the measurement programme taught,
digit-free and fenced — a pure-doc packet, no production code, tree 906 → 907), ✅ **P50** the
documentation cleanup — F10 discharged (⚠️ **most of the known list was already current**: P43–P46's
blast radius had brought §3/§4/§6/§12 up to date, so the genuine staleness was the top-block
revision claims — `24/26/29`, `P35/P36 unbuilt`, `235 measurements`, all fixed to **rev 31 / built /
372** — plus §7's resolution floor (`0.52/1.02 → 0.41/0.81`), §8's map and `cautious`/`counting`
bullets (four-handed `± 1.0` → `± 0.8`), §10's "The answer" and §15's deal-win summary). 🔥 **The
fence choice was extend-not-strip** (`PublishedFigureTests.TheProseFiguresTheStrategyDocumentQuotesAreTheFiguresInTheCsv`):
`STRATEGY.md` is the measurement authority whose voice *is* inline figures, so a digit-free rewrite
(P49's rule for the sims doc) would gut it — instead seven current-claim prose margins plus §7's
derived floor are anchored to their CSV rows in the proven HOW-TO-PLAY-WELL shape, sign carried by
the scale, proved able to fail. Pure-doc, tree 907 → 908. ⚠️ **A rung packet
pays a suite regeneration**,
**~5 h on this 24-core workstation at P46** (the older "~6¼ h / ~22,500 s" is a *laptop* figure
and the P45 laptop slept mid-run — re-time with `sim bench`, never trust a pasted wall clock).
⚠️ **One P46 follow-up owned, not done**: the race-reach instrument recomputes an uncached cover
search per crossed-table discard (measured cheap — ~54 µs/call — but wasteful); a quick pass to
share the seat's `OutsCache` is queued and does not change the measurement.

**What P62 built, and the six things a cold session needs from it.**
🔥 **(1) The packet's own acceptance test was wrong, and that is the finding.** P62 said to check
that `ClientHost` is **not on the home range**. ⚠️ **That was satisfied twice by something that is
not a carrier**: a phone on the house wifi behind a **VPN** reads as an off-home address, and
🔥 **a phone VPN survives a wifi→cellular handover**, so turning wifi off and switching to 5G
changed the path underneath and left the exit address **identical**. **The criterion is that
`ClientHost` *changes* and lands in the carrier's range**, corroborated by the phone's own network
state. ✅ **Proved once the VPN was off**: `172.58.164.115` (T-Mobile), `/table/1` **200**,
`negotiate` **200**, `/_blazor?id=` **101**; deployed commit verified **running** —
`docker inspect` image `a7b9b7187dfb` = tag `97230e3` = HEAD.
🔥 **(2) A carrier closes an idle circuit at 4 min 26 s; the house wifi does not.** P53 held one
for **7½ minutes with no close frame at all**. ⚠️ **The idle timeout is a property of the *path***,
so the desktop figure does not generalise — and **4:26 is past P54's 2-minute
`DisconnectedCircuitRetentionPeriod`.** The reconnect fired at once and the seat, round and standing
question all survived, **because a foreground tab runs the reconnect loop**; a phone with the screen
off does not. **That is `P63`, and P54's numbers were deliberately not moved on one measurement.**
🔥 **(3) A network handover was measured live, which P59 could not.** Dropping the VPN killed the
circuit mid-round at **36.9 s**; Blazor renegotiated **over the carrier** and opened a new one,
✅ **no reconnect overlay appeared and the round carried on.** P59's *inside the window nothing is
lost* now holds against a real radio rather than `TestServer`'s in-memory pipes.
⚠️ **(4) The engine was Gecko, so step 4 is only half answered.** Firefox 154 on Android 17 — a
third implementation after P53's and P60's Blink — and **basicauth replays past the page on it**
(`/` 401, then `ClientUsername: "poker"` throughout). ⚠️ **WebKit is a separate implementation and
an Android phone cannot answer for it**; that half of P53's acceptance stands open and needs an
iPhone.
⚠️ **(5) An instrument note, or the evidence reads wrong.** Traefik logs **`DownstreamStatus: 0`**
for a hijacked connection — **the proxy's own log cannot tell you an upgrade succeeded.** The
`101` and the circuit's lifetime exist **only** in the application log, and because Kestrel logs a
request when it *completes*, a closed circuit states its own lifetime — a better instrument than
P53's *starts-and-does-not-finish* signature.
⚠️ **(6) Step 0 is the one code change and it is in `ansible-nas`** (`c57a9ea4`): Traefik's
`[accessLog]`, JSON to stdout, gated on `traefik_access_log_enabled`. **`User-Agent` is kept
explicitly** because Traefik drops headers by default and step 4 is a question about a browser
engine. ✅ **IPv6 answered for this carrier** (IPv4 throughout; DNS64/NAT64 never engaged).
⚠️ **The layout held at the Pixel's width but it is a weak test of A18** — the table was opened on
*levels*, so every name is short and the long-name case was never drawn. Tree green at **941**,
unchanged; **the whole diff in this repository is documents.**

**What P61 built, and the four things a cold session needs from it.**
🔥 **(1) The copy-link's fix needed nothing in `:global()`'s place, and that is the finding.** The
packet recommended moving the reveal into the unscoped `app.css`; ⚠️ **it was not needed**, because
Blazor's rewriter appends the scope attribute to the **last compound selector only** —
`.can-copy .link .copy` is emitted as `.can-copy .link .copy[b-…]`, and an ancestor outside the
component was never constrained. **`:global()` was not solving a problem; it was the problem.**
✅ The rule stays beside the `display: none` default it overrides, where the ordering that makes it
win is visible; ⚠️ **`::deep` is not the fix** (it scopes descendants of the component's own root
and `.can-copy` is on `<html>`); the `<a class="url">` fallback is untouched.
✅ **(2) Measured in a browser engine, the only place this defect has ever been visible.** Headless
Chromium over CDP against the running client: `can-copy` on `<html>`, computed **`display: block`**
(a flex item blockifies `inline-block`) at **82 px** wide, and the live CSSOM holding **two**
`.copy` rules where P60's tablet held exactly one.
🔥 **(3) `ScopedCssTests` fences the class of mistake by reading the rewriter's output.** One fact
fails on **any** `:global(` under `obj/<config>/net10.0/scopedcss/**` — the configuration taken
from where the test assembly runs, so a stale `obj/Release` cannot fail a Debug build — and the
positive twin reads the **served bundle** and asserts the reveal exists **and is declared after**
the default that hides it, because the two weigh the same and order is the whole of why one wins.
⚠️ **The scan strips comments first**, or it would fail on the one file that had learned the lesson.
🔥 **(4) §3.11 A18 is amended, and the amendment is that it was fitted to the wrong axis.** The
argument for the ellipsis is *the whole name is a hover away*; the rule carrying it was
`max-width: 56rem`. **A tablet in landscape — 1316 px, `(any-hover: none)` — is where the two come
apart.** `SeatPanel.razor.css` gains a second `@media` on `(any-hover: none)`, **declared after**
the width one because on a narrow touch screen both match and weigh the same; `ViewportTests` gains
the fact. ⚠️ **Widening the ring's name column is the fix this rejects** — a floor fitted from this
machine's font is P58's trap, one axis over. ⚠️ **Four mutations, all of the stylesheets and none
of the tests.** ⚠️ **Owed and not done: the redeploy and the device** — a work cycle does not push,
so nothing here is proved on a real tablet until the image is rebuilt and the running container
re-read; and **headless Chromium answers `(any-hover: none)` by default**, so only the no-hover
branch was exercised in a browser. Tree green at **941**, from 938; `Domain`, `Presentation`,
`Server`, `Console` and `Sim` byte-identical.

**What P60 built, and the five things a cold session needs from it.**
🔥 **(1) The whole diff is documents, and that is the result.** A real round was played on a real
tablet — Galaxy Tab S9 FE (`SM-X510`), Android 16, Chrome 151, 1.75 dppx, so **823 CSS px portrait
and 1316 landscape** — driven over `adb forward tcp:9222 localabstract:chrome_devtools_remote`.
⚠️ **The deployed commit is stated: `62ed294` (P59)**, read off the running container first.
🔥 **And reading it that way is a finding: *pulled* is not *running*.** The NAS held `:latest` =
`2881b65` **pulled four hours earlier** while the container still served **`f309a9d`**. ⚠️ **P53's
finding (8) sharpens to: read what the running container is *built from*** — `docker inspect <c>
--format '{{.Image}}'` against `docker images … '{{.Tag}} {{.ID}}'`; `docker ps` shows the tag and
the tag lies.
✅ **(2) P58's fix holds in a font this workstation has never rendered** — four 181 px columns,
every seat name `white-space: normal`, **none clipped**, `Khine Myat Zin (opportunist)` wrapping to
two lines, no horizontal overflow. **The fence P58 refused to fit to one platform's `system-ui` was
right to refuse.**
🔥 **(3) The defect found is above the line, and it is A18's *argument* rather than its number.**
In landscape the ring returns, the name reverts to `nowrap` + `ellipsis`, and **three of six names
are clipped** (77%, 70%, 80% shown) while the device answers `(hover: none)`, `(any-hover: none)`,
`(pointer: coarse)` **at 1316 px**. ⚠️ **An ellipsis is honest only where the whole name is a hover
away, and a tablet is a wide screen with no pointer** — so the standard is fitted to *width* while
what justifies it is *hover*.
🔥 **(4) P54's copy-link has never been visible in any browser.** `Tables.razor.css` reveals it
with **`:global(.can-copy) .link .copy`** — but **`:global()` is CSS Modules, not Blazor scoped CSS**
(which has `::deep`), the Razor rewriter emits it **verbatim**, and the browser discards the whole
rule: **the live CSSOM holds exactly one `.copy` rule and it is `display: none`.** ⚠️ **Nothing is
broken for a player** — the `<a class="url">` beside it is P54's designed fallback — **the
enhancement simply never appears**, which is why *"it was never pressed in a browser"* never became
a bug report. ✅ **The handler is sound**: pressed physically once revealed, it writes the correct
**forwarded** absolute URL. 🔥 **And a synthesized touch is not a person either** — the same press
through CDP `Input.dispatchTouchEvent` fired the click handler but **silently failed the clipboard
write** (no user activation) where `adb input tap` succeeded. **Third instance of this project's
scar, after `--no-restore` and `curl`.**
⚠️ **(5) The phone half is NOT discharged and its blocker is not a phone.** No cellular, no Mobile
Safari — **and the source-IP evidence has no instrument**: Traefik runs with **no `accessLog`** and
the app logs no remote address, so a cellular test cannot be told from a wifi one. **Enable
Traefik's access log in `ansible-nas` first.** ✅ **The IPv6 question is answered without a device**:
`poker.nickjones.dev` is `209.128.193.153` with **no `AAAA` record**, so the DNS64/NAT64 mode is
live and unmitigated — **check it before blaming the browser.** ✅ **The round itself behaved**:
opened through P56's two-step per-seat form, sat down by physical tap and on-screen keyboard, the
claim **refused** by `warden` holding the rank (P28), the blind draw private, **rotated mid-turn
without losing the circuit or the standing question**, settled in 47 turns; `negotiate` **200**,
three circuits each closing **101**, the playing one alive **243 s**; §3.11 B11 measured on the
device at **44 px** buttons and **66 × 76 px** cards. 🔥 **`P61` and `P62` both shipped (above); `P63` — the retention window against a real idle timeout — is the packet P62 wrote.** Tree green at **938**, unchanged — no test was added and none could
be, because nothing here is reachable from the test project.

**What P59 built, and the four things a cold session needs from it.**
🔥 **(1) A real Blazor circuit can be held from a test now, and writing the protocol down was the
price.** A circuit is reached over SignalR and `AddInteractiveServerComponents` narrows that hub to
**one protocol, `blazorpack`**, which no client library speaks — so
`BurmesePoker.Tests/Web/BlazorPack.cs` writes down the slice of it a circuit's opening needs and
`BlazorCircuit.cs` starts one from **the page's own component markers**, exactly as
`blazor.web.js` does, kills the socket **with no close frame**, and asks for the circuit back by
name. ⚠️ **`Microsoft.AspNetCore.Mvc.Testing` is now referenced by the test project** — the first
package here that is not the test framework. ✅ **The instrument proves itself before it is used**:
`TableView` sits down **only when it is really interactive** (§3.11 C13), so the table's one
person-seat ceasing to wait is proof a circuit **started and rendered**, not that a page was
fetched. ⚠️ **It does not draw and cannot press anything** — render batches are acknowledged and
thrown away.
🔥 **(2) Three answers and they differ**, at the shipped ordering shrunk to seconds (retention
**3 s**, patience **8 s**): *inside the window* the reconnection succeeds and nothing is lost;
*past the window, inside the patience* the reconnection **fails** and the seat is **given up** —
`TableView.Dispose` stands the player up — **while the turn is not lost**, and sitting down again
under the same name takes the seat back with the question still standing; *past both* the computer
has played the turn. 🔥 **So a seat and a turn are recovered by two different mechanisms** — the
seat **by name** (P13.6), the turn **by the patience** — and ⚠️ **losing the circuit is not losing
the game**, which nothing had ever said.
🔥 **(3) P54's pairing is exercised instead of asserted against itself.** With the patience **below**
the retention period the computer plays the turn of somebody **the framework is still holding** —
the reconnection still succeeds — and **nothing on screen says anything was decided in the gap.**
✅ Proved able to fail as the packet asked: shortening the patience below the window turns the
*inside the window* test red. ⚠️ **Do not move either number without reading the other** is now a
measurement rather than a comment.
🔥 **(4) `TableBoard.Turn` is the wrong instrument for *did the player lose a turn*, and measuring
found it**: a seat is asked **twice** in one turn — take, then throw — so a turn sits on a seat for
**two** patiences and a turn the computer has already played still reads as that seat's.
**`TableBoard.StoodInFor` is the fact itself.** ⚠️ **Two things deliberately not done**: the
`tc netem` arm (**no socket to shape** — `TestServer` is in-memory pipes, and shaping a real one
needs root; **latency is P60's**), and the recorded `SeatChannel`/`Ask`-token obstacle, which
**was never reached** because nothing here tears a seat down. Tree green at **938**, from 933;
⚠️ **`Domain`, `Presentation`, `Server`, `Console`, `Sim` and `Web` are byte-identical** — the whole
diff is the test project.

**What P58 built, and the four things a cold session needs from it.**
🔥 **(1) The packet that was meant to add a standard found a defect, and it was not where the plan
looked.** The survey that planned this track worried about the phone; **the phone is the safe end.**
Measured in Chrome at eleven real viewports: **360, 375 and 390 px resolve the stacked felt to a
*single* column** (`repeat(auto-fit, minmax(9.5rem, 1fr))` cannot fit two inside 360 − 40 page
padding − 24 felt padding), nothing trimmed, nothing overflowing. 🔥 **The band that was broken is
412–896 px** — the pack packs to its floor, so a column is ~158 px at 412 and ~154 px from 600 to
896, **narrower than at 360** — and there the computer's own seat names ellipsed (*"Aung Aung
(exp…"*). ⚠️ **That is the exact defect the 56rem line was drawn to prevent, arriving underneath
it.**
🔥 **(2) The fix is wrapping the name below the line, and refusing a wider column floor is the
finding.** `Khine Myat Zin (opportunist)` — the longest name the computer can produce — measures
183 px **in this machine's `system-ui`**, so a floor fitted from it would be ~14 rem of *this
platform's* font: **a fence that passes where it was fitted and fails everywhere else.** ⚠️ **An
ellipsis is honest only where the whole name is a hover away** — true above the line (`title=`, a
person may type twenty-four characters), false below it, which is where there is no pointer.
✅ **(3) §3.11 has an eighteenth standard, A18, and `ViewportTests` is its fence** — four facts,
**each proved able to fail by mutating the stylesheet rather than the test.** ⚠️ **A CSS custom
property cannot carry a breakpoint** (`@media` cannot read `var()`), so `TableView.razor.css` and
`SeatPanel.razor.css` stay ordinary CSS and are held to naming the **same** width — P54's idiom.
**360 px is arithmetic rather than taste**: the column floor plus the felt's padding plus the
page's is read out of the stylesheets, so a padding raised in one file cannot push the table off
the side of a phone.
⚠️ **(4) It is a browser measurement, not a device one** — no phone, no tablet, no touch, no
Mobile Safari. **P60 still owns that**, and **an iframe is not a person any more than `curl` is.**
🔥 **P58 makes the tablet worth more**: the broken band *is* the tablet band, and the fix is a font
decision seen only in this workstation's font. Tree green at **933**, from 929; `Domain`,
`Presentation`, `Server`, `Console` and `Sim` byte-identical.

**What P56 built, and the five things a cold session needs from it.**
🔥 **(1) §3.12 was amended rather than contradicted.** Nick's answer was option (b), and
`DifficultyLadder`'s own remark said *"a menu with both in it would be the mistake this design
exists to avoid"* — **that sentence, §3.12 and P19's remark were all rewritten** to: **levels are
the menu; rungs are an advanced disclosure that states its price.** ⚠️ **What the rule forbade is
unchanged** — selling a measured-worse opponent as a matter of taste — and **the margin beside the
name is what pays that bill.** The console is untouched and still offers levels only.
🔥 **(2) `BurmesePoker.Web/OpponentMenu.cs` is the offering, and the fence runs both ways.** Ten
rungs under the four levels, each drawn as *`sprinter` — +1.2 ± 0.8 points of win rate against
`outs` — measurably stronger*; `PublishedFigureTests.EveryOpponentTheLobbyOffers…` compares every
printed sentence to `ladder.head-to-head.*` **with the sign turned round where the row is named
the other way**, verdict word included — **and asserts that a rung with a published row against
the reference *is* offered while one without is *not***. That is what keeps money-ranked
`prospector`/`purist` out **by rule**, and makes a newly-measured rung the menu never heard of a
red build.
✅ **(3) Nothing below the form was needed.** `HostedTable`'s seat resolution went `Find` →
`FindOrProbe` (private `Seatable`, swallowing the `ArgumentException` a malformed probe throws);
⚠️ **`Lobby`'s `--difficulty` stays `Find`**; ⚠️ **the seat name and the journal attribution are
deliberately different** — `Mya Lay (sprinter)` via `OpponentMenu.Called`, `sprinter@0` in the
journal, because a person should not be shown the machinery and a replay must not lose ε.
🔥 **(4) Per-seat difficulty is a two-step post and it cost one class.** §3.11 C12 is the
obstacle: static SSR cannot grow a control per seat as the count is typed. `NewTable` moved out of
`Tables.razor` into its own file — **nothing here renders a component in a test** — `SeatFill`
replaces P19's checkbox with *same*/*mixed*/*each*, and **`NeedsSeatChoices` is a count check
rather than a flag**, so changing the shape on the second step asks the seats again. One button;
the second press opens.
🔥 **(5) Found by pressing it: a post with no `Wanted.PerSeat[…]` fields sets the property to
`null`**, not to the initialiser's empty list — a **500 on the first post of the first step**,
invisible to the whole tree. Defended in the accessor. ✅ **Proved end to end with `curl`** (shape,
then seats): the opened table seats `(easy)`, `(sprinter)`, `(warden)`. ⚠️ **`curl` is not a
person** — the advanced group and the form join P54's two outstanding browser checks. The quorum
is now said twice (a note on the form; *Waiting for two more people to sit down; it deals when
they are all here* on each row), and the form's `Seats` default moved `MinimumPlayers` →
**`DefaultPlayers`**. Tree green at **928**, from 920; `Domain` changed in **doc comments only**,
`Presentation`, `Server`, `Console` and `Sim` byte-identical — no measurement can move.

**What P54 built, and the four things a cold session needs from it.**
🔥 **(1) Nothing in this client had ever closed a table, and that was the leak.** `Lobby.Close`
was written in P13.6 and **only the tests called it**, so a hosted site accumulated a table per
form press, hit `Lobby.MostTables` (12), and thereafter answered every *Open it* with an error —
weeks after a deploy, reading as a broken form rather than a full site. ✅ **The parked bot loop
was never the problem**: `HostedTable.Deal` breaks the moment `Ready` goes false, which the last
viewer leaving makes it. `TableSweeper` (a `BackgroundService`) now asks `Lobby.ReapIdleTables`
every **5 minutes** for tables idle **30**.
🔥 **(2) Two decisions worth carrying.** `HostedTable.IdleSince` starts **at construction** — a
table opened by a press nobody followed up has never had a viewer to lose and is exactly the case
that leaks. And **`Lobby.House` is a named field, never reaped**: once tables can be closed, *the
first table in the dictionary* and *the table this site was started with* stop being the same
thing, and reaping the second leaves `dotnet run --project BurmesePoker.Web` an empty room and the
deployed site's own URL pointing at nothing.
🔥 **(3) The patience number stopped being a taste.** A browser table's patience is **180 s** (was
90) and `Program.cs` sets `CircuitOptions.DisconnectedCircuitRetentionPeriod` explicitly to **2
minutes**. ⚠️ **Inside that window the framework is deliberately hiding a dropped connection from
the player**, so a shorter patience has the computer play the turn of somebody the framework is
still expecting back. **The two live in two files and are fenced against each other** — one read
from `Program.cs`, one off the real `Lobby` — so neither is a copy of the other. **Do not move
either without reading the other.**
⚠️ **(4) Two things left open, said out loud.** **(a)** The copy-link affordance is an enhancement
over a real `<a>` (absolute, `ToAbsoluteUri`; the reveal is a class on the **document** and the
handler is delegated from it, because enhanced navigation replaces the markup and per-element
wiring dies on the second visit) — but **it was never pressed in a browser**, and neither was a
reaping watched on a live site. **(b)** `SeatChannel` never disposes its per-seat
`ManualResetEventSlim`; it is bounded (the wait handle's `SafeHandle` finaliser releases it) and
disposing it races the engine thread parked in `Ask`, which takes no cancellation token — so
**`Server` was left byte-identical**, and a packet that wants this must give `Ask` a token first.
Tree green at **920**, from 914; `Domain`, `Presentation`, `Server`, `Console` and `Sim`
untouched.

**What P52 built, and the three things a cold session needs from it.**
🔥 **(1) The repository has a Gitea origin and CI publishes the image.**
`.gitea/workflows/publish-image.yml` builds **the repository's own `Dockerfile`, unrewritten**, on
the `docker-builder` runner (the one with the docker socket passed through), and pushes
`gitea.nickjones.dev/nickjones/burmesepoker` at **`:latest` and `:<sha>`** — the commit tag is what
makes a rollback possible, `:latest` is what P53's role pulls. `git push origin main` is the trigger.
⚠️ **Amended by P55: the remotes have swapped names.** `origin` is **Gitea** and `github` is GitHub —
so the CI trigger is `git push origin main`, and every pre-P55 document saying `git push gitea main`
means the same push. **P52 left the push-mirror undone; P55 configures it in Gitea's settings**, so
GitHub is kept current by Gitea rather than by a second `git push`, and a session should not push to
`github` by hand.
🔥 **(2) CI proves the image serves the script that starts the circuit, not just that it builds.**
The last step runs the freshly-pushed tag and curls `/healthz` **and
`_framework/blazor.web.js`** — P51's finding turned into a gate, because a `--no-restore` image
passes every other check ever written. ⚠️ **A CI file is the obvious place for that trap to come
back** (`--no-restore` reads like a free speed-up), so `ContainerTests` fences the workflow too:
the runner label, the absence of the flag, the image path P53 pulls, and the blazor.web.js check.
✅ **(3) Acceptance met on the *published* artifact.** Pulled `:latest` from the registry, ran it,
`/healthz` `_framework/blazor.web.js` `/` and a proxied `/` all **200**, and a browser round dealt —
**the same hand as P51's local build at the same seed**, which is a free reproduction check on the
image. ⚠️ **The credential never entered this session**: the token is a Gitea repo secret named
`REGISTRY_TOKEN`, and the local pull used a `docker login` Nick performed himself. Tree green at
**914**, from 913; `Domain` untouched, no measurement can move.

**What P51 built, and the four things a cold session needs from it.**
🔥 **(1) There is a `Dockerfile` at the root and the browser table runs out of it** — multi-stage
(`sdk:10.0` → `aspnet:10.0`, both tags fenced against the csproj's moniker), publishing
`BurmesePoker.Web` and its three project references only, `0.0.0.0:8080` via `ASPNETCORE_URLS`,
non-root, **231 MB**. A real round was **played** in it, not just loaded: sat down through the
lobby's `EditForm` (the antiforgery path), claimed, discarded, watched four bot seats take their
turns and drew blind — the private event arriving over the circuit.
🔥 **(2) The finding is that the standard restore-caching idiom breaks Blazor, and it was
measured.** Copy the csprojs → `dotnet restore` → copy the sources → `dotnet publish --no-restore`
is what every .NET Dockerfile does, and here it publishes an app with **no
`wwwroot/_framework/blazor.web.js` at all** (the endpoint manifest names it **zero** times against
**one** when the publish restores for itself). The symptom is **P13.3's exactly** — `MapStaticAssets`
404s the script that starts the circuit, the page renders perfectly and never moves again — and
nothing in the tree can see it, because the app is fine and the image is not. **The publish does not
pass `--no-restore`, and `ContainerTests` fails the build if it ever does again.**
🔥 **(3) `app.UseForwardedHeaders()` is in `Program.cs`, first, and no label can substitute for
it.** `KnownIPNetworks`/`KnownProxies` are cleared deliberately (a proxy container's address is
assigned at run time) — ⚠️ **which is safe only because nothing but the proxy reaches the port; do
not publish 8080 to the internet.** Proved live: with `X-Forwarded-Proto: https` and
`X-Forwarded-Host: poker.nickjones.dev` the container logs the request as
`https://poker.nickjones.dev/`. The ordering is fenced against `UseAntiforgery`, `MapStaticAssets`
and `MapRazorComponents` — *present* was not a strong enough assertion.
⚠️ **(4) `/healthz` answers `ok` and touches no table** (fenced against reading `Lobby`) — a health
check is about the process. Five new facts in `BurmesePoker.Tests/Web/ContainerTests.cs`, all source
scans in `JackpotSpokenTests`' idiom; tree green at **913**, from 908. `Domain`, `Presentation`,
`Server`, `Console` and `Sim` are byte-identical — the whole diff is `Dockerfile`,
`.dockerignore`, `Program.cs` and docs.

```text
docker build -t burmesepoker .
docker run --rm -p 8080:8080 burmesepoker --people 1 --seed 20260828 --pace 300
```

**What P46 built, and the four things a cold session needs from it.**
🔥 **(1) `Domain/Agents/SprinterBotAgent.cs` is `outs` with one change: the discard's last-resort
key.** Within one card of covering it stops maximising live outs and maximises **winning draws**
(`LiveOuts.WinningDraws` — copies-weighted values that let 13 of the 14 it leaves meld, bar = the
hand's size). Lexicographic key *winning draws then outs* in one `long`, so **off the endgame it
is `outs` card for card** — the trigger is existence, not a tuned threshold (P45's idiom).
`Endgame.WithinOneCardOfCovering` is the public read the Sim counts. Take/claim/object/declare are
`outs`'. Catalog last, `Strength: 3`, `Hardest` stays `outs`. **Bench 8.1× `greedy` / 1.21×
`outs`**, no turn inflation.
🔥 **(2) The answer is a positive that separates — the first since `outs` beat `greedy`.**
**`+1.2 ± 0.8` vs `outs`, p=2.9e-03, Holm over family 45**, confirmed on a second command;
beats `opportunist` (+1.6 Holm), `angler` (+1.1 raw-only); tops both ranking columns (mean +8.1,
crossed 25.5). ⚠️ **Small and fragile** — crossed table 25.5 vs 25.3 is a dead heat, P48's
fresh-seed run graduates it. 🔥 **The mechanism armed** after three flat nulls: race-reach
**26.6% vs `outs`' 25.6%** (~4 SE, new `ladder.race-reach.*` rows) — steers into more near-wins,
win rate follows. **A moved mechanism + moved margin together is why it's believed.** `STRATEGY.md`
§3/§8 (its story is in §3 like `outs`', not §8's failures).
✅ **(3) Reproduction held, cap paid measured.** 9 old rungs' head-to-head byte-identical; 38 moved
rows all field-dependent; no Holm verdict fell; null cell → `sprinter` (4th time, holds −0.4).
**`SeatingPlan.MaximumAssignments` 65,536 → 131,072**, stated (raise/pay over drop/subsample),
fence updated. 1 game of 100,000 abandoned (`random`'s field, P29).
⚠️ **(4) Suite ~5 h here; the instrument is cheap and was a red herring.** A first run was killed
mid-session on a wrong "the instrument is pathological" diagnosis — measured, `WithinOneCardOf…`
is ~54 µs/call (~2 ms only on the ~2% of hands at covered ≥ 10); the 10-rung suite is just large.
Tree green at **897**, from 886.

**What P45 built, and the four things a cold session needs from it.**
🔥 **(1) `Domain/Agents/AnglerBotAgent.cs` is `outs` with the take priced, at both places it
arises** (take and claim — `prospector`'s rule): acquire a known card iff
`gain·unseen + outsAfter > 2·outsNow` — cover gain plus kept-hand draw equity against the
forfeited blind draw, integers over public facts, **one stated model: a one-draw horizon**.
`LiveOuts.CardCount` (copies-weighted outs, sharing `Count`'s loop and cache) is the numerator,
`MoneyOdds`' unseen pool the denominator. The one new move is the **enrichment take** — a card
that melds nothing, taken when it more than doubles the hand's out-cards. Catalog last,
`Strength: 3`, win-rate ranked, `Hardest` stays `outs`. **Bench 9.3× `greedy` / 1.21× `outs`**
— inside P21's 10× budget after two stated cuts, and an all-`angler` table runs `outs`' 24.9
turns (no `warden`-style inflation).
🔥 **(2) The answer is the predicted null and the flat mechanism is the finding** (P44's shape):
`+0.6 ± 0.8` against `outs`, beating the `greedy` trio `+2.6`–`+2.9` and `warden` `+6.9` (all
separated, family 36); **take rate 24.66 ± 0.09% vs `outs`' 24.71 ± 0.09%** — the enrichment
take almost never arms (the prediction said 1–5% and was wrong), so under a one-draw horizon
**`outs`' improvement-only take already collects everything a card-priced model can see**.
Third zero/stated-price null in three packets. `STRATEGY.md` §3/§8; mechanism rows
`ladder.take-rate.{rung}` now published for every rung.
✅ **(3) Reproduction repeated P43's exactly**: 148/172 shared rows byte-identical outside the
command column, every mover field-dependent by construction, **no Holm verdict moved under
28 → 36**; the null cell changed hands to `angler` (third time, `Ladder[^1]`, deliberate) and
holds at −0.5; the `LiveOuts` refactor proved byte-identical separately (seeded 300-game
`outs,purist` run, HEAD worktree vs tree). One game of 59,049 abandoned (`random`'s field —
precedent P29). Tree green at **886**, from 874.
⚠️ **(4) Two curiosities published rather than claimed**: `angler` over `opportunist`
`+1.0 ± 0.8` **raw only** (`p = 0.013` vs Holm 0.0083 — the family's first raw-only casualty,
P48's), and §6's new across-cells pairing ratio **0.37** for `warden` vs its two nearly
identical comparators — a statement about the players, not the method.

**What P44 built, and the four things a cold session needs from it.**
🔥 **(1) `Domain/Agents/PuristBotAgent.cs` is `outs` plus one lexicographic preference** — a
*fewest-jokers-kept* key **between** `outs`' two ranking keys, so a joker is shed only when
that costs no melded card (pays any number of live outs, never a meld — the stated exchange
rate, `prospector`'s one-assumption idiom). Catalog last, `Strength: 3`, **`Ranked =
RankedOn.Money` for the second reason there is**: it reads no stakes but trades rounds for a
multiplied prize (RULES.md §7.3), so win rate would misjudge it by construction. `Hardest`
stays `outs`; the ladder stays at eight win-rate rungs, so **the crossing cap is untouched and
still P45's**.
🔥 **(2) The answer is a null that falsified both halves of its own prediction**: at $5/$1 the
sweep reads `−$0.23 ± 0.32` against a same-seed control of `−$0.228 ± 0.32` (`prospector` at
$5/$1 *is* `outs`), so the real effect is **−$0.005 a round — one round in eight thousand —
and the clean-win share never moved: 12.81 ± 1.04% vs the control's 12.83%.** When the
joker-throw is the unique winning discard `outs` already throws it, so **the accidental 12%
floor already contains every clean win that costs nothing**; the remaining clean wins cost
melds, `warden`'s ruinous currency. `STRATEGY.md` §14 (the instrumented reading), §2/§8/§10.
⚠️ **(3) The money instrument generalised**: one sweep per money-ranked rung
(`SuiteOptions.MoneyChallengers`, off the catalog), **the challenger is part of every
`money.*` id now** (twelve renamed rows, values byte-identical — P32's precedent), per-cell
`money.clean-win-rate.*`/`money.jokerless-rate.*` mechanism rows, and bare `sim money` sweeps
every money-ranked rung in turn. ✅ **Reproduction exact: 131/131 unrenamed shared rows
byte-identical, no verdict moved, the new sweep its own Holm family of four.**
Tree green at **874**, from 862.
⚠️ **(4) One plan correction recorded**: the plan said §9 #33's default means the joker cannot
be shed before the declaring discard — **the engine restricts a joker's exit only while jokers
are locked** (§5.1 with #27), so `purist` sheds unlocked jokers early and the null stands under
the rules as built; re-measure if the experts flip #33.

**What P43 built, and the three things a cold session needs from it.**
🔥 **(1) `Domain/Agents/OpportunistBotAgent.cs` is the missing 2×2 corner** — `outs`' take
exactly (never a card it does not want), `warden`'s hold on whatever ranks ordinary takes
close, the hold extracted to **`Domain/Agents/HeldLocks.cs`** with `warden` delegating (one
home, P28's rule). Catalog after `warden`, `Strength: 3`, win-rate ranked, **`Hardest` stays
`outs`** — no dial, ε or front-end change.
🔥 **(2) The answer is the predicted null and it decides the question**: `+0.1 ± 0.8` against
`outs`, while beating `warden` `+6.2 ± 0.8` and the `greedy` trio `+2.0`–`+2.2` (all separated,
family 28). **`warden`'s whole loss is the paid take; denial at price zero buys nothing.**
`STRATEGY.md` §3/§8/§13; the free-for-all-vs-head-to-head tension is flagged for P48's
composition-stratified margins.
✅ **(3) Every shared CSV cell came back byte-identical** (only the `command` column moved), no
old verdict fell under the 21 → 28 tightening, and the null cell changed hands to
`opportunist` (`Ladder[^1]`, second time, deliberate). Tree green at **862**, from 850.

**Before that, `P42` shipped 2026-08-23 on Fable 5, the day it was written up: playtest
readiness.** `RULES.md` stays **rev 31** (no rule changed), the tree is green at **850**, and
**`P40` — the game in Burmese — stands blocked on input only Nick can produce**: vetted
translations of `RULEBOOK.md` and `HOW-TO-PLAY-WELL.md`, made
outside the repo with `docs/translation/PROMPTS.md` against Gemini/ChatGPT (translate with
one, cross-check with the other). **Do not start P40 without that text, and translate the
rev-31 rulebook** — the first-round outputs under `docs/translation/` are rev-30-based and
must be re-run. Behind it stand the candidates at the end of `docs/STATUS.md`'s *What is next*
(candidate 3, the console's four-seat default, was absorbed by P42 and is done; the expert
session on the defaulted §9 rows — **thirteen**, every one fenced — is worth more than any
code here, and P40's Burmese rulebook is the best instrument to hand those experts).

**What P42 built, and the four things a cold session needs from it.**
🔥 **(1) The ×5 jackpot is said out loud, off a fact the domain carries once:
`RoundResult.JackpotOwner`** — `PlayerId?`, **required rather than defaulted** (`Win`'s
lesson), filled in `RoundEngine.Settle` from the same `ConfigurationOf` the settlement reads.
⚠️ **A watcher cannot compute it** (ownership is partly private until settlement), so it rides
`TableEvent.Settled` to the browser; the *possibility* is public from the deal, so that half is
a fold — `MoneyCardRegistry.IsTheJackpotPair` (public static) and `TableBoard.JackpotPairUp`,
folded **at `RoundStarted` and deliberately not off the live turn-up list** (a claimed top card
leaves that list while its designation stands). Console settlement line + round-start
narration; web `SettlementPanel` sentence + table-centre note. **`CardDisplayState` stays
×5-free, §9 #32 is not generalised, both its fences untouched.**
🔥 **(2) Three fences, all mutation-proved**: `RoundEngineTests.AJackpotRoundCarriesItsOwner…`
constructs the round (pair turned up, one seat given both partners **and all four jokers** so
the hand-computed payouts have no filler joker in them; a split-pair twin asserts null), and
`JackpotSpokenTests` holds each front end to **reading** the fact by source scan — the console
is outside the test project's reference graph on purpose, and it is deliberately not a wording
fence. ✅ **Byte-identity held, P41's exact procedure** (seeded 300-game journal + CSV
byte-identical, HEAD journal replays byte-identically).
⚠️ **(3) The console deals five** — the seat prompt defaults `RoundEngine.DefaultPlayers`, the
floor untouched; both `drive-console.py` scripts re-captured clean at five seats. ⚠️ **The
driver's default script IS `human`** — "both scripts" means `--script bots` and the default.
🔥 **(4) The browser round was actually played** — Claude in Chrome, `--people 1 --seed
20260823 --pace 300`, four settlements clicked through: the claim **refused twice and granted
once** (§4.5's holder-only disclosure seen working), §5.1 closing a rank so its card stopped
being a control, **§9 #50 live** (the concealed duplicate of a face-up 3♦ thrown, the ▲
stayed), P41's chips and piles from both sides of the glass, why?, legend, log, timeout
stand-in. ⚠️ **UX observation for the playtest, not a defect**: at pace 300 the settlement
panel is replaced by the next deal after ~one pace beat — fenced by `BrowserRoundTests`, but a
human may want the round to linger.

**What P41 built, and the four things a cold session needs from it.**
🔥 **(1) The whole packet is one fold, written once: `Presentation/TableLook.cs`** — every
seat's discard pile (§5) and every seat's face-up cards (§5.2) as a pure fold over the public
events. The console's `ConsoleObserver`, the server's `TableFanOut` and the browser's
`TableBoard` each hold one and feed it the events they already narrated; **the browser's pile
fold moved *into* it**, so pile logic has one home. ⚠️ **The blind draw has no method on the
type** — concealment as an absence — and
`ConcealmentTests.ABlindDrawnCardIsNeverShownFaceUp` holds the fan-out to it from outside,
**proved able to fail by mutating `PlayerDrew`**, with a positive twin that plays a real open
take over a connection.
🔥 **(2) Byte-identity was asserted, not argued** (acceptance 4): a seeded 300-game `Sim` run
wrote a **byte-identical journal and CSV** either side of the change, the HEAD journal replays
under the new tree, and `git diff` is empty on Domain and Sim. No measurement could move and
none did.
⚠️ **(3) §9 #49 and #50 are built on their recommended defaults and still open**, each fenced
by a `TableLookTests` test named for the row, both mutation-proved. **The face-up mark is by
`CardId` throughout**, so the concealed duplicate of a face-up value stays concealed — the one
fact the event log alone could not carry. ⚠️ **`TurnContext` was deliberately not widened**: no
rung sees the piles or the marks; a rung that reads them is a new rung and arrives measured.
⚠️ **(4) What a player sees now**: browser seats show face-up chips (`▲`) flat on the panel and
open their whole pile from the ▾ `<details>`; the console gained a fourth panel — *Everyone can
see this* — and `▲` in your hand and legend. `CardDisplayState.FaceUp = 1 << 7` with a
`DisplayTokens` entry, so the legend fences picked it up without a new test. **§10 #24 is
discharged; the registry's §5.2 entry is `Checked` (deliberately no ⚠ — both defaults are
presentation-only) and the exemption ceiling is back at 6, with no whole exemptions at all.**

**What P39 built, and the four things a cold session needs from it.**
🔥 **(1) `docs/HOW-TO-PLAY-WELL.md` answers *how do I get better?* from what was measured, and
nothing else.** Organised by decision rather than by experiment — the whole game in one
sentence, the tie-break, the one refinement that ever worked (`outs`), the money (settle it,
never chase it), **the nulls given as much room as the margins**, the three unpriced bonuses
said to be *unknown rather than small*, and the difficulty dial. `STRATEGY.md` stays the
measurement authority; the guide quotes **only** `measurements.csv`-fenced figures and states
everything else in words — the P16 seat-side null carries **no digits at all** because its
figures are not CSV rows.
🔥 **(2) The fence moved home with the figures, and it now fences *verdicts* too.**
`PublishedFigureTests.TheFiguresThePlayersGuideQuotes…` reads the guide: the dial quad, the
headline pair, nine anchored margins (**the printed sign is part of the anchor and the scale
carries it**, so a flipped margin fails rather than reading as its own opposite), two
interval-free rates — and the eight verdicts the prose asserts, because **a margin can drift
back inside its interval without the number moving much at all**.
⚠️ **(3) A figure has one home, asserted as an absence.** New test: `PLAYING.md` must point at
the guide and may contain **no `±`, no reference-table quad, no headline pair** — a figure
pasted back into `PLAYING.md` is a red build whatever it says. Its *Playing better* section is
a pointer now; both new fences were proved able to fail by mutating the documents.
⚠️ **(4) Found on the way: `PLAYING.md`'s difficulty prompt row still said *expert* wins ~36%
and *easy* ~14%** — the **four-handed** figures, unfenced because P34's regex targeted the
other sentence in the file. It is digit-free with a pointer now, which is the fix the one-home
rule prescribes: **a number P34's fence cannot see is a number the guide must own or the prose
must not say.**

**Before P39, the same day: `P38` shipped on Fable 5 — there is a rulebook, and it cannot fall
behind the rules without a red build.**

**What P38 built, and the four things a cold session needs from it.**
🔥 **(1) `docs/RULEBOOK.md` is the game taught, and it decides nothing.** One document a stranger
can play a correct round from — reading order, no provenance, no open questions, no packet
numbers — while `RULES.md` stays the sole authority. **Four tests in `RulebookTests` hold it to
the tree, each proved able to fail by mutating the document**: the rev stamp is bound to
`JournalHeader.CurrentRulesRevision` (so a play-changing rev is a red build until the rulebook is
re-read), the worked round is **replayed by the test** — seed 15, five `outs` seats, every hand,
turn-up, meld and settlement cell asserted — the house-readings appendix must cite **exactly**
the §9 rows still open (derived from `RULES.md` itself by table shape: numbered un-struck rows
with five columns), and the voice test bans the tags, the packet ids and the word *reconstruction*.
⚠️ **(2) The appendix's citation set is two-way**: a §9 row opening or closing fails the build
until the rulebook's house readings move with it — twelve rows today (#33, #36–#41, #44–#48 less
the struck ones). **That is how the rulebook finds out an expert answered.**
🔥 **(3) The worked round is a teaching gift found by scanning seeds**: at seed 15 the winner
declares **jokerless** (so the ×3 bonus appears in a real settlement) and **had discarded an owned
A♠** — permanent ownership demonstrated by the engine rather than asserted. ⚠️ **The example's
construction (five `outs` seats, seat seed `seed × 100 + seat`) is defined in the test**, which is
what makes "generated rather than invented" checkable.
⚠️ **(4) A rules change that moves the worked round's numbers is a *desirable* red**: re-derive
the section by re-running the test's construction, don't patch a cell.

**Before P38, the same day: `P34` shipped — there is a `README.md`, and the documentation set
cannot go stale quietly any more.** Every packet built from §0 is done. `P39` was added with P38,
after Nick asked whether this project has a rules onboarding document for a new player or a usable
strategy guide, and **it had neither**. ⚠️ **The gap is one of audience rather than content** —
`RULES.md` is a reconstruction and `STRATEGY.md` is a research report, both written for the
project rather than for a player. **P38 closed the first half; P39 is the second.**

**What P34 built, and the five things a cold session needs from it.**
🔥 **(1) `README.md` is the front door and the only current-only document in the repository.**
Everything else here is a **running narrative**, newest-first, keeping superseded reasoning on
purpose — which is the most valuable thing in `docs/` and must not be flattened. ⚠️ **The two
audiences want opposite documents**: a cold session wants everything ever learned in priority
order, a visitor wants what is true now in a page. **No packet numbers and no history in
`README.md`** — if a sentence needs *"since P27"* to be true it belongs in this file.
🔥 **(2) Eight tests now hold the documentation to the tree, and each was proved able to fail by
mutating the document rather than the code** (`BurmesePoker.Tests/Docs/`). The map is complete
both ways; every historical document carries a banner and **no current one does**; every command
in a fenced `bash` block resolves against the source that parses it; every test `RULES.md` names
as a fence exists; every figure `STRATEGY.md` tabulates and every figure `PLAYING.md` quotes
agrees with `measurements.csv`; and the product's one spoken measurement is still a null.
🔥 **(3) The test count is now discovered, not trusted** — facts plus theory rows, by reflection —
**so a packet that adds a test and leaves the prose alone is a red build.** ⚠️ **Only the *first*
count and the *first* rev in each document are checked**, because these files are newest-first and
every earlier figure was true when it was written; demanding they all agree would be asking the
project to delete its own history.
⚠️ **(4) Two documents were a measurement behind and nothing had noticed.** `PLAYING.md` told a
player the four difficulty settings win **13.8/21.7/28.4/36.1%** — a **four-handed** reference
table, from a run two measurements old, on a page describing a **five**-handed table — and it said
*"your neighbours change every round"*, false since P36. **Prose has no column to disagree with**,
which is why the fix was a test and not a proofread. `RULES-PRIMER.md` was worse: four `[⚠ code
disagrees]` tags for divergences closed at P25–P28, and a settlement section that stopped at
*flat*.
⚠️ **(5) `RULES.md` §10 says *"empty"* and it has one standing exception, now said out loud: #7.**
`RoundEngine.MinimumPlayers` is **4** against §2's Settled 2-to-6, so the two- and three-handed
win conditions are implemented, tested and unreachable from a dealt game — **the oldest entry in
that list, and no packet owns it.** *(No rule changed; the rev did not move.)*

---

**Before that, `P35` shipped the same day: the two scoring rules that reach outside a round
are played.** `RULES.md` §10 is empty, the tree was green at **819**.

**What P35 built, and the five things a cold session needs from it.**
🔥 **(1) §7.4 changed the shape of a round, which nothing had done since P0.** §9 #38's recorded
default is *the dealt thirteen alone*, so `RoundEngine.Play` offers the declaration to every seat
whose **dealt** hand already covers, in turn order, **before the first take**. ⚠️ **A round can now
run no turns at all** — `RoundResult.Turns` is 0 — and **`TurnNumber` 0 is a real value** reaching
the journal, the console's turn heading and the server's `TurnBegan`. A seat may decline. ⚠️ **It
opened §9 #48** (two seats dealt a winning thirteen at once), defaulted to the earlier in turn order.
🔥 **(2) §7.5 works because settlement is *told*, never made to remember.** `MatchEngine.Streak` is
**the only state in this project that reaches across rounds and is not money**; `Settlement` still
holds no history and takes no match, asserted over its parameter list. ⚠️ **"Pays your whole
payout" means the winner collects exactly what they would have collected**, out of one pocket — the
first implementation had it backwards and a test caught it.
🔥 **(3) Where a net delta is split, ask the domain — do not re-derive.** `Settlement.RoundPayments`
is the round column; the console's panel and `SeatRow.Flat` both read it. **Both had assumed every
loser pays the same amount**, true from rev 1 until rev 27, and a split at the wrong place posts
the difference into the **side-bet** column with the totals still adding up.
🔥 **(4) The re-measurement is the strongest reproduction this project has recorded, and it
falsified its own prediction.** **107 of 124 shared rows byte-identical**; the seventeen that moved
all count *turns* or *money*. **Nine rounds in 33,008 ended on the deal** (`docs/STRATEGY.md` §15)
and **not one win rate, margin, Holm verdict, ranking, pairing ratio or ε moved at all** — §7.4
changed *when* those rounds ended, not *who won them*. ⚠️ **The written prediction that a moved
money figure without a moved win rate would mean a bug was wrong**: the column that discriminates
is the side bet, and all four `money.side-margin.*` rows are byte-identical.
⚠️ **(5) §7.5 is not measured and cannot be** while every experiment runs `RoundsPerGame = 1` —
stated in `docs/STRATEGY.md` §11 with what it leaves unknown. **It is audited instead**: the
conformance harness gained its first multi-round case, and there are now **no whole exemptions at
all** in `SettledRuleCoverageTests` (ceiling 7 → 6).

---

**Before that, `P37` shipped 2026-08-22 on Opus 5: the table can agree to change seats,
`RULES.md` §10 #23 is discharged, and §10 is empty — every rule the document records as Settled is
implemented.** `RULES.md` is **rev 29** (unmoved — no rule changed), the tree is green at **795**,
and **`P35` is the next packet** (then `P34`).

**What P37 built, and the four things a cold session needs from it.**
🔥 **(1) `IPlayerAgent.AskAboutTheSeating` is a sixth question and the first that is not about
cards.** Every seat is asked in the gap before a round; the seats move on **one `Ask` and no
`Refuse`**. Asked in `MatchEngine.NextSeating`, **beside** P36's policy — the agreement first, and
the policy not asked on top of it.
🔥 **(2) Consent is not desire, and a yes-or-no question could not have carried the rule.** A
computer seat consents (a *design* decision, `BUILD-PLAN` **§3.13**, not a rule) — but a consenting
bot answering *yes* would re-seat an all-bot table every deal. **`SeatingOpinion` is three
answers**, `Consent` the default and a no-op, so *fail closed* fell out for free: silence, an
unattended seat and every bot all consent, and consent moves nothing.
🔥 **(3) A default interface method is a silent trap and this is the project's first.** A decorator
that does not override it answers *consent* **in its own name** and drops what it wraps — a
re-seating that never reaches the journal, a replay that deals to different seats. **Six decorators
needed forwarding**, found by type. ⚠️ **Any future default member on that interface inherits this.**
⚠️ **(4) A public question is a standing answer, not a pending prompt** — blocking would cost one
patience per seat. It stands on the `SeatChannel` and the engine **consumes** it: one press moves
the seats once. ✅ **Replay was free** (`JournalingAgent` at turn 0, and `JournalPlayerAgent` peeks
so pre-P37 journals still replay). ✅ **No published measurement can move.**

---

**Before that, `P36` shipped the same day: a seating is drawn once and held, and `RULES.md` §10
#22 is discharged.**

**What P36 built, and the four things a cold session needs from it.**
🔥 **(1) `Domain/Play/SeatingPolicy.cs` is *when a re-draw happens*, and it is one condition in one
place.** `Held` is the default and the rule; `RoundsBetweenSeatings` of *N* re-draws every *N*
rounds; **0 is never**, and there is no flag beside the number. ⚠️ **The engine had contradicted
§3 step 2 in *both* directions** — pre-P28 it held a seating that could never change, P28–P36 it
re-drew before every deal — **so the fix was not a revert.**
🔥 **(2) `MatchEngine.SeatingPolicy` is get-only and `ReseatsBefore(int)` is pure**, deliberately:
a policy that could be talked to would have answered §9 #45 by accident. ⚠️ **So P37 cannot express
*the table agreed* as a policy** — recommended shape is an explicit `MatchEngine.Reseat()` called
between rounds, and the journal needs a **decision kind** rather than the header's number.
🔥 **(3) `LayeringTests.NothingOutsideTheSeatingPolicyDecidesWhenTheSeatsAreDrawnAgain`** is P18's
scan applied to a third rule — only the engine may ask, only the policy may do arithmetic on the
number — and **it caught a real second copy in `JournalFormat`.**
⚠️ **(4) A seed stopped meaning what it meant, for the second time** (§3.9 point 2): a held seating
takes **no** numbers out of the match's generator. `SeatBoardTests`' fixture went 3 → **5** rounds
because the claim's permission stopped turning up — **the assertion working, not a fixture tuned to
pass.** ✅ **No published measurement moved and it is asserted rather than argued**; the journal
writes `seating_rounds` only when the seating moved, so **every journal ever written is
byte-identical**.

---

**And before that, `P24.2`: the hint arrow grows a sentence, and the journal records where a person
disagreed with the computer.**

**What P24.2 built, and the three things a cold session needs from it.**
🔥 **(1) `CoverScore.Ranking` is now a projection of `CoverScore.Scored`** — the keys the sort
computes and threw away a line later are kept, so an explanation costs **no extra
`PartialCover.Best` call** over the arrow (`ComputerAdvice.RankingsBought` asserts it, and the
memo is keyed on the *identity* of the `TurnContext`). ⚠️ `ScoredCandidate.Refined` is `long?` and
**null means the key was never asked**; null and zero sort identically, so **no measurement moved**.
🔥 **(2) A rung never hands a front end a bare `long`.** `IExplainsDiscards` is the described
sibling of `IRanksDiscards`; `DiscardKey` carries a name, a direction and **a phrase for its
sentinel** — `outs` negates its outs key and `CoverScore.Potential` returns `int.MaxValue` for a
joker, so raw numbers would draw *"−14 outs"* and *"2147483647 partners"*.
🔥 **(3) The journal records an *opinion beside an answer***: `JournalDecision.Advice` and
`DisagreedWithTheComputer`, human seats only, by `CardId`, **written even with the hints box off**.
⚠️ **Each browser `<details>Why?</details>` now holds two kinds of sentence gated differently** —
the rule **ungated** (a rule is not advice) and the computed paragraph **gated on hints**.
⚠️ **`SeatPrompt` carries the rationale and a `TableEvent` may not** — asserted over the type,
which is the first constraint `P37`'s public question will meet.
✅ **The console is untouched and its capture is byte-identical**, which is also the proof that
reshaping `CoverScore` was a refactor.

---

**Before that, `P32` shipped the same day: the standing answer is about a *five-handed* table.**

**The default table is five seats and it is written down once: `RoundEngine.DefaultPlayers`.**
`SuiteOptions.DefaultSeats`, every `--seats` default in `Sim` and the browser lobby all read it.
⚠️ **It is not `MinimumPlayers`, and conflating the two is what this packet existed to fix**: every
figure published between P12 and P33 was four-handed **because four is the smallest legal table,
not because anybody chose it.** By §7.1.1 a four-handed declaration owes a joker-free series and a
five-handed one owes **none**; by §7.3 a jokerless hand pays ×2 at four and ×3 at five. **Four-handed
is a different game, kept whole and frozen at `docs/strategy/measurements-4-handed.csv`.**

🔥 **The headline is a *falsification of this project's own published explanation*, and it needed
the right instrument to see.** P29 attributed the four-handed levelling of `simple` to §7.1.1's
joker-free series requirement. At five seats that requirement is gone, so the gap should have
re-opened. ❌ **It did not.** ⚠️ **Reading the raw margins would have said it *narrowed*, and that is
also wrong**: a fifth seat drops the base win rate 25% → 20%, so **every margin rescales by 0.800**.
**Median ratio over all eighteen head-to-head cells: 0.801**, the six `random` rows on **0.800 to
three digits**, and `simple`'s gaps to `greedy` and `counting` within a tenth of a point of pure
scale. 🔥 **The five-handed ladder is the four-handed ladder divided by 1.25** — every Holm verdict
identical — **and what causes the levelling is now unknown.** ⚠️ **Compare a margin across table
sizes as a *ratio to the base rate*, never as a difference.**

✅ **Four other predictions held: the jokerless rate fell 15.4% → 12.1%, the bonus's value rose +$15
→ +$40, value beat rate ($2.31 → $4.84 a round), and the null cell read 20%.** ❌ **The fifth was
wrong and usefully so**: the dial's steps were expected to compress with the base rate and **no ε
moved.** `hard` ≈ 0.42 and `medium` ≈ 0.67 fitted at five seats are the shipped 0.4 and 0.7.
✅ **So ε is close to a property of *the mistake* rather than of the rung *or of the table*** —
P23's finding on an axis it was never tested on — and **one dial serves four, five and six seats**,
each separately measured (`docs/strategy/dial-away-from-the-default-table.md`).

⚠️ **Two costing lessons.** `BUILD-PLAN` feared the five-handed free-for-all (`7⁵ = 16,807`) would
be "plausibly the majority of the run" — **wrong**: a full pass at four seats is 2,401 games in
**51 s**, so the five-handed pass is ~7 min of a 12,445 s suite. **Full crossing, nothing
subsampled**; `SeatingPlan.MaximumAssignments` is 32,768. And **`sim bench` gained `--seats`** — it
had none, so a five-handed round could not be priced at all. **Measure before deciding; the plan's
guess was off by an order of magnitude.**

---

🔥 **`RULES.md` went 26 → 29 in one conversation on 2026-08-22, and none of it is built.**
`JournalHeader.CurrentRulesRevision` is **29**.

- **Rev 27 — §7.4 and §7.5, `EXPERT` (Aung Aung).** A win **on the initial deal** pays ×2; a
  **third consecutive win** is paid **entirely by the seat above the winner** (blamed for feeding).
  🔥 **§7.5 is the first rule that cannot be settled from a single round** and the first that
  changes **who pays**. ✅ **Third independent saying to single out the upstream seat** — §5.1,
  §4.5, §7.5. **Seven questions opened (§9 #38–#44); §10 #20 and #21; packet P35.**
- 🔥 **Rev 28 — §7.5 contradicted §3, and the *old* rule was wrong.** *"In real games you don't
  shuffle seats every round, only when people ask for it."* ✅ **§3 step 2 corrected: a seating is
  drawn once and held**; rev 19's *"every round, not once"* is **withdrawn**. ⚠️ **So the engine
  contradicts the document again, in the opposite direction to the error P28 fixed** (§10 **#22**).
  **The fix is not a revert** — pre-P28 held a seating and could never change it. **P36.**
- **Rev 29 — Nick ruled §9 #45**: a re-seating happens *"when people agree to do it"*, `PLAYER`.
  Opens **#47** and §10 **#23**. **P37.**

🔥 **The heuristic that found rev 28, written into §9 as a prediction *before* #43 was asked and
right: when two Settled `EXPERT` rules collide, suspect the older recording rather than the newer
saying.** This game's rules are recalled as **wholes**, and a new rule exposing an old one as
mis-recorded is now a documented pattern rather than a surprise.

⚠️ **`SettledRuleCoverageTests` fired the moment §7.4 and §7.5 were marked Settled** — the alarm
working. They are `Exempt(...)` naming §10 #20/#21 and P35. 🔥 **The exemption ceiling moved 5 → 7,
and a stronger assertion went in beside it: every whole exemption must name the packet that will
discharge it.** A count was a proxy; **an exemption nobody owns is a rule quietly abandoned**, and
a bare number never caught that.

---

✅ **`P24.2` is done (2026-08-22)** — both of its re-plan's bets were right: P31 had already built
half of build item 1 (**the keys were missing, not the ranking**), and P32's trap was real, so the
sentence reads the same `TableRules` the evaluator does and is asserted at four seats and at five.
**`P35`, `P36` and `P37` are done too; `P34` is the only packet left on the plan.**

⚠️ **One leftover, P32's: `BurmesePoker.Console` still deals four** — its seat prompt defaults to
`MinimumPlayers`. **One line plus a `drive-console.py` re-capture.** ✅ **P36's leftover is taken**:
`AboutTable` says what the seats are doing, and the console's round-start line stopped claiming
*"the seats are re-drawn every round"* — **false for a day after P36**.

---

*Everything below was written before P32 (and most of it before P33). ⚠️ Where it says §7.3 is unbuilt or that P33 is next,
read the block above.*

🔥 **`P31` shipped 2026-08-22 on Opus 5: `warden`, the feeding ban played offensively — and it
lost, by more than any rung has lost before.** `Domain/Agents/WardenBotAgent.cs` is `outs` with the
**take** changed: it takes a card it does not want when that closes the rank against the seat that
threw it (`RULES.md` §5.1), and then **holds** that rank rather than releasing it. It measures
**`−9.3 ± 1.0` against `outs`** and about six points behind `greedy`, `cautious` and `counting`,
beating only `simple` and `random` — all six surviving Holm over a family of twenty-one.
⚠️ **The packet predicted a null.**

🔥 **The finding that matters is the one that makes the loss attributable, and it is a new
instrument.** §5.1 is enforced as an **impossible move**, so it leaves no trace by construction and
nothing had ever measured whether it does anything. `IRanksDiscards.RankDiscards(context,
candidates)` — **an instrument the engine may never call** — asks a restricted seat what it *would*
have thrown over its whole hand. **The ban removed a held card from 30.5% of all discards in the
crossed field and changed the seat's answer on 30.8% of those: 9.4% of every turn.** ⚠️ **So the
rule bites hard and the rung is what failed.** `docs/STRATEGY.md` §13.

⚠️ **The why, and it is the constraint on any successor rung.** `warden` prices a lock in **melded
cards** and then pays for every lock with a **draw**, which nothing in its rule prices — an
all-`warden` table runs **31.9 turns a round against `outs`' 24.1**. `prospector` prices a draw in
money (`MoneyOdds.PerBlindDraw`); nothing yet prices one in cards, though `LiveOuts` is the obvious
currency and is already in the file.

🔥 **Two structural facts a cold session needs.** **(1) The ladder is a tree now** — `warden` and
`prospector` both hang off `outs`, so `BotCatalog.Ladder[^1]` is **no longer** `BotCatalog.Hardest`,
and *"the last rung named is the strongest"* turned out to be a coincidence asserted as a law in
**three** places (`SuiteOptions.MoneyReference` and `StandingAnswerTests`, both fixed; the
tournament's **null cell**, left alone on purpose — it changed hands to `warden` and holds).
**(2) `warden` is `Strength: 3`, level with `outs`**, so the difficulty dial did not move, no ε
changed and **no front end gained an option.**

🔥 **The tree is green at 715 tests, `RULES.md` is still rev 24, and `docs/strategy/measurements.csv`
holds 116 measurements from an 11,020 s (three-hour) run.** ✅ **71 of the 88 rows both runs share
came back byte-identical** — every old head-to-head cell, every pairing ratio, the whole dial and
the whole money sweep — which is the strongest reproduction this project has recorded. ✅ **R3 and
R13's owed corrections landed**: the claim-money interval is paired at **±0.25** (mean unmoved) and
`money.side-margin.*` rows exist. ⚠️ **`P32`** (five seats as the default table) is **blocked by §7.3 — see the top of this file**;
**re-costed by P31 and worse than it was**: the round-robin is 21 cells, and the five-handed
free-for-all is `7⁵ = 16,807` seatings. ⚠️ **Set the model with `/model` before the session; the
packet records which one and cannot choose it.** **P24.2** is still Nick's call.

🔥 **Before P31: the verify branch closed — P30.1 (the review), P24.1 (the hosted table's journal)
and P30.2 (the conformance harness and the front-end tests) all shipped 2026-08-21 on Fable 5.**
✅ **P30.2's headline: the game as played is audited.** `Tests/Conformance/RuleConformance`
re-derives every Settled rule independently over 180 ordinary rounds at 4/5/6 seats with a
mutant per rule family proving the audit can go red; `SettledRuleCoverageTests` fails the build
on a Settled `RULES.md` section nothing checks or exempts; `drive-console.py` now **answers
prompts adaptively** and verifies the settlement panel's arithmetic; and `BrowserRoundTests`
closes the loop board = engine = journal replay. 🔥 **Two behaviour fixes rode along**: a
discard legal only under §5.1 exception 2 *is* the declaration — the seat is not asked and
cannot decline (R1) — and a handed-over seat is dead to its previous occupant server-side
(`SeatChannel`, R8). **Journals stamp rev 24, bound to `RULES.md` by a test** — bump
`JournalHeader.CurrentRulesRevision` with any play-changing rev.

🔥 **Everything below this line was true when P29 shipped. P25–P29 all shipped 2026-08-21.** Every packet from §0 is done — P0–P12, P13.1–P13.6 and P14–P23 — and **four sessions with
Mya Lay and Aung Aung on 2026-08-20/21 closed twenty-three questions in `RULES.md` §9 and left
four settled rules with no implementation at all.** ✅ **P25** the win condition by table size
(§7.1.1), ✅ **P26** the money layer as it actually is (§4 — jokers permanent, ×3 and ×5),
✅ **P27** the feeding ban (§5.1), ✅ **P28** the claim's permission with per-round seating (§3,
§4.5) and ✅ **P29** the re-measurement under all four **are all built**.
⚠️ **P25–P28 were a different kind of work from everything above them — P11–P23 added capability
to a correct engine; P25–P28 make a working engine play a different game** — and **P29 is what
made the documentation true again.** ⚠️ **P24 is now a question of *whether*, not *when*
(`BUILD-PLAN.md` §4): the objection that moved it behind P29 has been discharged, and building it
is Nick's call.**

🔥 **The tree is green at 677 tests, `RULES.md` is rev 24, and as of P29 every figure in
`docs/STRATEGY.md` was measured under those rules.** Every rule the document records as Settled is
implemented (§10 #13, #14, #16, #17 and #18 all discharged), and **the measurement is no longer
outstanding.**

🔥 **P29's headline is a reproduction claim going the other way: 4 of 91 rows came back
byte-identical, and the four are the ε constants of the difficulty dial** — the only numbers in
`measurements.csv` a human chose. P23 reproduced 59 of 77 and called it the document's strongest
reproducibility statement. ⚠️ **"Does it reproduce" is not a question that can be asked across a
rules change**: the only rows that can survive one are the rows that are not measurements. **A
future packet that changes a rule should expect this and not go looking for the bug.**

🔥 **Of three predictions written down before the run, two held and one was wrong — and the wrong
one is worth the most.** ✅ The difficulty dial survives (every step still separates under Holm,
**no ε moved**). ✅ `prospector` separates a whole ratio lower — $5/$20 goes from `+0.95 ± 1.63`
*inside the interval* to **`+5.32 ± 2.27` separated**, take rate collapsing 8.4% → 0.1%.
❌ **`outs` did not narrow at all** — `+3.0 ± 1.0` over `greedy` against +3.1, mean margin +14.6
both times — though the reasoning behind the prediction (every rung maximises cover count, and
cover count no longer wins at four seats) is sound. 🔥 **Chasing the wrong prediction located the
real effect: `simple` gained about two points on each of `greedy`, `cautious` and `counting`.**
**The four-handed condition demands a joker-free series, nothing in any rung is aimed at it, so
the better melder pays the same tax — a requirement nobody optimises for levels a ladder from the
bottom rather than tilting it.**

✅ **Two things nothing had ever reported are published, in `docs/STRATEGY.md` §12.**
**(1) Round length and abandoned rounds** — 28.6 turns a round for the ladder field, and **the
only non-zero abandoned count is the field containing `random`** (8 games of 9,072); both
all-`outs` fields settled every game. ⚠️ **The honest statement is narrow**: no table of thinking
rungs has yet failed to converge, not that none can. **(2) What refusing a claim is worth, which
is nothing** — `outs/refuse` over `outs/allow` is **`+0.4 ± 1.0`** on win rate and
**`+0.02 ± 0.18`** on money. ✅ **A null published with its denominator**: the opener asks in about
a quarter of rounds and the upstream seat holds the rank about half the time, so the veto fires
about one round in seven. **The null says P28's decision costs nothing either way, not that the
rule never fires.**

⚠️ **Two corrections a cold session needs.** **(1) `sim suite` is two and three quarter hours, not
five** — 9,981 s at P29 with a cell *added*, because `outs` costs **7.0×** a `greedy` round rather
than 8.2×; the stale figure had been quoted in three documents. **Re-time with `sim bench` rather
than trusting prose.** **(2) `SeatRow.Claims` counts claims *asked for*, not *got*** — P28 made
those different and the CSV has a `claims_refused` column now.

🔥 **P27's finding still stands and P29 priced it: a bot's cover count can fall.**
Every rung's score used to be monotone — *"throwing back the card just taken restores the hand
exactly"* — which is `GreedyBotAgent`'s own stated reason a table of bots reaches a declaration at
all. **§5.1 takes the just-taken card out of the choice.** A seat whose only legal discards are
melded ones gives up a meld, so convergence is no longer guaranteed by construction; what stands
behind it now is `SimulationOptions.TurnCap` and the hosted table's `RoundTimeLimit`. ✅ **At
8,000 games a cell no field of thinking rungs failed to converge**, and the abandoned count is
published so that it cannot stop being true quietly.

**Every packet in the plan was done — P0–P12, P13.1–P13.6 and P14–P23.**
🔥 **P24 is the next one, planned 2026-08-20 and not started**: *the computer's reasoning, said
out loud* — the browser's hint arrow grows a **why**, and the hosted table gains a journal that
records **where an expert disagreed with it**. Browser only, all four questions, winner versus
runner-up. ⚠️ **Two things a cold session must know before opening it**: `BurmesePoker.Web` and
`BurmesePoker.Server` contain the string `journal` **zero times**, and what gets recorded is an
**opinion beside an answer** rather than a rationale on a decision — which is what makes
disagreement a query instead of a transcription job. See `BUILD-PLAN.md` §5 P24.
✅ **The feeding ban is built — P27, 2026-08-21, `RULES.md` rev 23.** A playtest with Mya Lay on
**2026-08-20** produced it: **`RULES.md` §5.1** — *you may not discard a rank the next player has
taken in the open*, until they throw that rank back (which frees it for the rest of the round,
permanently) or you are going out on it. **Rank only; suit is irrelevant.** It is `EXPERT` and
**Settled**, and it is **the first rule in the document that constrains which card a player may
discard**. `Domain/Play/FeedingBan.cs` is two rank sets a seat and one method;
**`TurnContext.LegalDiscards` is the whole of the choice a turn presents** and is never empty;
`CoverScore.Discard` and `CoverScore.Ranking` take a `TurnContext` rather than a hand, so **every
rung is filtered by construction and so is the runner-up a difficulty level throws**. The rest of
this block is what it was built from.
🔥 **It is enforced by construction: a banned card is not an infraction but an impossible move**,
never offered and unchoosable, so there is no penalty. That is free of the concealment model (every
fact the ban is computed from is already public) but it means `TurnContext` must carry the banned
ranks and **every agent's discard ranking must be filtered to legal cards — `FallibleAgent`'s
runner-up included**, because a mistake still has to be a legal move.
🔥 **And it has a floor, which is the line that makes impossible-move enforcement safe**: where the
ban would leave a player **no legal discard at all**, the ban **yields for that turn**. So the legal
set is *the hand minus the banned ranks, **or the whole hand if that is empty*** — **never empty by
construction**, and a turn cannot deadlock. The discard is mandatory (§7.1); the ban is not. ⚠️ It
is **not** the unrecovered exception to the mandatory discard in §9 #6 — it is the reverse. ⚠️ **#6
is closed: there is no exception, you always discard**, which makes the floor the only way §5.1 and
the mandatory discard can both hold.
✅ **The packet can be written now.** All six details — `RULES.md` §9 **#16–#19, #25 and #27** —
were closed on 2026-08-20 with **Mya Lay and Aung Aung**: the ban binds **only the seat above
you**, is armed **only by a public take**, covers **any rank**, and is **the same rule at every
table size**, including two-handed where the mutual lock is a legal state. ⚠️ **Two of the six are
`PLAYER` house rulings and stay flagged**: that a release survives the reshuffle (*"nobody really
knows"*) and that taking a joker closes the other jokers (*"I'd assume"*).
🔥 **And the ban matches on *rank alone* — a third identity notion, and it is `Card.SameRankAs`
now.** `==` on `Card` is instance identity; `SameValueAs` is rank **and** suit **and** colour, which
is what §4.2's money designation needs. **Reaching for `SameValueAs` here implements the wrong
rule** — it would leave the Q♣ Mya Lay actually objected to perfectly legal. ⚠️ `SameRankAs` is
literally `Rank == other.Rank`, and the nullable comparison is what makes a joker close the other
jokers — §9 #27's house ruling falling out of the type. **P28 must read this method for the claim's
objection (§9 #30), not write a second one.**

✅ **The largest divergence in the tree is closed — P25, 2026-08-21: the win condition is a
function of the table size** (`RULES.md` **§7.1.1**, `EXPERT`). Two-handed is **series only and a
set is illegal as a meld**; three-handed needs **two** series, four-handed **one**, five-or-more
none — and **the series a table size *requires* must be joker-free**, while surplus ones need not
be (an all-joker meld is a legal series and never a clean one). **A longer run may be laid down
split**, so `3+3` out of a six-card run counts as two series. `Domain/Melds/TableRules.cs` is that
table as data and the only place it is written down; `HandEvaluator` takes it as a parameter and
**has no parameterless overload**; the search carries what is still owing **along** the partition,
so its memo is keyed on `(covered, seriesOwed, cleanOwed)` rather than on `covered` alone.
⚠️ **Three things P25 leaves for whoever is next.** **(1)** `PartialCover` was left alone on
purpose, so `IsComplete` agrees with `IsWinning` **only at five or more** — **every rung in
`BotCatalog` is maximising cover count at a table where cover count is no longer the win
condition**, which is P29's prediction 2. **(2)** Every figure in `docs/STRATEGY.md` is still
measured under the old condition; the first data point is four-handed greedy-vs-simple going
**25.1 → 26.6 turns a round** and **102 → 86 rounds/s** at one seed — a smoke test, not a
measurement. **(3)** 🔥 **`drive-console.py` is blind to this** — both captures came back
byte-identical to `HEAD` because **both scripts quit in round 2 and neither contains a
declaration**. **Do not read a clean `cmp` as evidence about play.** ⚠️ **And
`RoundEngine.MinimumPlayers` is still 4** (§10 #7), so `TableRules.For(2)` and `For(3)` are
correct, tested and unreachable from a dealt game.

✅ **Built by P28 on 2026-08-21 — the only rule this document ever recorded that the engine
actively contradicted rather than merely lacked. The record of what it was built from follows.**
🔥 **Seats are re-randomised every round** — `RULES.md` §3 and §10 #16, `EXPERT`, rev 19.
A *game* means from the turn-up to somebody going out — **a game is a round** — and the seating is
re-drawn between games, so a player's neighbours change every deal. ⚠️ **`MatchEngine` randomises
once at setup and holds it for a whole match**, which is what P9 chose while the question was open.
✅ **No published measurement moves** — every experiment in `BurmesePoker.Sim` runs
`RoundsPerGame = 1`. ⚠️ **The front ends are where it shows, and it is a UX question as much as a
loop**: P13.5 puts *you at the front whichever seat you were dealt*, so the table would visibly
rearrange itself around a fixed viewer every deal, which `TableRing` has never been asked to do.

✅ **The money layer is built — P26, 2026-08-21, `RULES.md` rev 22.** Jokers are permanent money
cards (*"7 of diamonds, ace of spades, AND jokers are always money cards"*), so **the permanent
side-bet is eight cards, not four**; a designation landing on a permanent value pays **×3**; and if
the two turned-up cards are the 7♦ and the A♠ and one player owns both partners, they pay **×5**
each — $40 a head at standard stakes against a $5 round prize. 🔥 **A multiplier stopped being a
property of a card**, so `MoneyOwnership` carries the one thing ownership decides and
`Multiplier(card, owner, ownership)` is the whole answer, under a configuration `Settlement` reads
**once a round**. ✅ **Design decision 2 held** — money status is still *computed, never stored*,
and `Multiplier(Card)` survives as the value-only question every view actually asks.
🔥 **Measured: the side-bet went from `$8.50` to `$11.58 ± 0.34` a round at five seats** — 42.5% →
**58%** of the round prize — with the *before* run reproducing P12's rev-13 figures at a different
seed, so §4.3 and §4.4's stale `DERIVED` notes are re-derived rather than re-argued. ✅ **And play
did not move at all**: same seed, same wins, same turns, same cover. ⚠️ **Two things P26 leaves
behind.** **(1) No view can show a ×5** — `CardView.Multiplier` is 0, 1 or 3 by construction, and
`MoneyOdds` does not price it either; the jackpot is settled and never drawn, and **no packet owns
that gap**. **(2) `RULES.md` §9 #32 is still open** — whether the ×5 needs the 7♦/A♠ pair
specifically or any two tripled values. **Do not generalise it in code**; two tests fence it.

✅ **P28 built the claim's permission and the per-round seating on 2026-08-21, and `RULES.md`
§10 #16 and #18 are discharged.** `ClaimRequest.MayBeRefusedBy` asks **`Card.SameRankAs`** — P27's
method read rather than re-written (§9 #30) — `IPlayerAgent.ObjectToClaim` is the fifth question
and **the only one asked of a seat that is not on turn**, and the engine asks it **only of a seat
holding that rank**. 🔥 **A refused claim arms nothing**, so the veto really does buy the upstream
seat its rank back. 🔥 **The finding that cost the most is older than the packet**:
`JournalFormat.Name` ended `_ => "declare"`, so a fifth question was written to file as a
*declaration* — **a serializer's default arm is a mistranslation waiting for the next case**, and
only the file round-trip test could see it. ⚠️ **`MatchEngine.PlayRound()` draws the seats and
`PlayRound(drawOrder)` does not**: a deal written down card by card is a deal written down *for a
seating*. ⚠️ **Every rung refuses whenever it may and none prices the disclosure** — a decision,
not a derivation, and P29's to measure. The rest of this block is what it was built from.

🔥 **The other unbuilt rule — P28's, and the first rules change that invalidated a published
measurement —
`RULES.md` rev 20, 2026-08-21.** **(1) Claiming the turned-up money card needs the permission of
the seat that plays *before* you**, who may refuse **only if they hold that card**, because your
public take arms §5.1 against them and locks them into holding it (§4.5). **The first rule tying
the money layer to the feeding ban**, and it independently confirms §9 #16 and #17 — both of which
were only recommendations when they were written, so a permission rule naming the upstream seat is
strong evidence §5.1 is an old rule. ⚠️ **It needs a third kind of agent decision — *do I object* —
asked of a seat that is not on turn**, which no `SeatPrompt` in `BurmesePoker.Server` does, and
**the answer is a disclosure**, since only a holder may object (§10 #18). **(2) A turned-up 7♦, A♠
or joker can never be owned, claimed or not, and its partner copy pays ×3** — superseding the
*double* recorded from the same people the day before, because *worth double* was said before the
ownership framing arrived. A turned-up **joker** designates the other joker of **its own colour**,
which `SameValueAs` already computes.

✅ **P22's withdrawn `DERIVED` note has been replaced by a measurement, and the replacement was a
prediction that came out right.** Under the ×3 a designation on the 7♦ and one on an ordinary card
leave **exactly the same** money loose in the shoe, so §4.1's conservation arithmetic is sound;
`ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe` asserts the equality.
✅ **`docs/STRATEGY.md` is measured under the rules the game actually plays by, as of P29 on
2026-08-21** — P25's win condition, P26's money layer, P27's feeding ban and P28's claim
permission, all four. **Every figure published before that date is gone from the document rather
than annotated.** `prospector` is the one rung whose decision reads the money, and §10 is the
section that moved most.

✅ **Both of rev 20's own questions closed in rev 21.** An objection turns on the **rank alone** —
which is §5.1's own predicate, so the claim's test and the ban's test are **one predicate and must
not be written twice** (#30) — and a turned-up joker's partner really is ×3, #31 closing because
its premise was wrong. 🔥 **Four consecutive revisions have each answered past the question asked,
and three changed a rule nobody was asking about.** ⚠️ **This game's rules are recalled as wholes,
not as answers** — asking narrowly and recording only the narrow answer has lost material three
times. 🔥 **Everything else
about the money layer is now confirmed by a person** (rev 19–20), including that 7♦ and A♠ are what
they are by **"tradition"** — a question recorded as unrecoverable since rev 1.

**All five of §0's goals are delivered**: the 2023 implementation is deleted, the
whole rules core is built and tested, `dotnet run --project BurmesePoker.Console` fills the empty
seats with paced, named bots and plays round after round with the banks carrying over,
`dotnet run -c Release --project BurmesePoker.Sim` plays thousands of seeded games in parallel to
compare strategies, and `dotnet run --project BurmesePoker.Web` is **a browser lobby you sit down
in and play other people at**.

✅ **A fifth goal was stated on 2026-08-19: a designed difficulty
system, and a settled answer to what actually works.** That is **P17–P23**, and **all seven are
done as of 2026-08-20.** It is two jobs kept apart on
purpose — a *product* (difficulty as a table setting, per seat, in both front ends: **finished in
P19**) and a *programme* (analysis and simulation enough to say which ways of playing are better,
by how much, and with an interval).
⚠️ **Read `BUILD-PLAN.md` §3.12 first**: *difficulty is a dial, skill is a ladder, and they are
not the same axis* — which is what keeps the difficulty menu from being a list of research
instruments, and what makes the product independent of whether any given rung is worth anything.
✅ **The product half is finished.** `Domain/Agents/DifficultyLadder.cs` holds four levels —
`easy`, `medium`, `hard`, `expert` — each the strongest rung there is with a **measured mistake
rate** (0.9, 0.7, 0.4, 0.0), and both front ends offer **levels only**. See §9 of
`docs/STRATEGY.md` for the calibration.

🔥 **Two facts set the order, and both are now discharged.** The ordinary `Sim` report used to
print no interval at all, so P17 — statistics and ranking — came before P19 — calibration. And
there were **four independent notions of *which bot*** across Sim, Console, Web and Server, so
P18 made it one catalog before any new rung is written: **`Domain/Agents/BotCatalog.cs` is the
only place a bot is named**, `LayeringTests` fails the build if any project outside
`Domain/Agents` constructs a rung, and a new rung reaches the console prompt, the browser lobby
and the harness with no front-end work at all.
⚠️ **P19 finished the difficulty product with today's rungs.** P15 spent a whole packet on a
plausible rung worth +0.5 ± 0.55 points and P20 spent another and got less — `counting` is
`+0.3 ± 1.0` the *wrong* way — **so two of the first three research rungs returned nothing.**
🔥 **P21 is the third and it separated**: `outs` beats `greedy` by **`+3.1 ± 1.0`**, and the
difference between it and the two nulls is *where the new key went* — above greedy's tie-break
rather than beneath it. 🔥 **A new rung is not free of the dial, and this is the day that bill
came due**: every level is `BotCatalog.Hardest` with an ε, `outs` is `Strength: 3`, so all four
levels are `outs` now and the ε values had been spaced against `greedy`. ✅ **P23 re-fitted them,
and exactly one moved — `hard`, 0.5 → 0.4.**
🔥 **P22 is the fourth and it asked a different question**: `prospector` is judged on `$/round`
rather than on win rate, and the answer is **a function of the stakes**. At $5/$1 its rule never
fires and it is literally the same player as `outs`; at $5/$40 it wins **20 points fewer rounds**
and banks **`+7.3 ± 3.3` a round**. ⚠️ **The dial did not move** — `prospector` shares `outs`'
`Strength`, so `BotCatalog.Hardest` is unchanged and no front end needed a line — but it left
**`docs/strategy/measurements.csv` a rung behind the catalog**. ✅ **P23 caught it up**, and paid
the bill structurally: `BotRung.Ranked` has each rung declare whether **win rate** or **money**
settles it, so the ladder tournament measures one set and the money sweep the other, and
`prospector`'s six duplicate cells are gone without anybody typing a shorter field.

⚠️ **Before touching the browser client, read `BUILD-PLAN.md` §3.10 and §3.11.** The engine runs
**server-side, always** (a hand is fully concealed with money on it, so a client-side engine cannot
honour that, and it is not retrofittable), the client is **Blazor Server**, and §3.11 fixes
eighteen UX standards — several of them mechanical tests — that a component either obeys or
breaks a test.

**The P13 sub-packets, in order, and the finding from each that a cold context needs.**

- ✅ **P13.1 — a presentation view model.** `BurmesePoker.Presentation`; the console renders a view
  model it did not build, byte-identically at the same `--seed`. 🔥 **A view model that aliases the
  engine's hand list is not a view model** — everything handed to a seat is a **snapshot**.
- ✅ **P13.2 — the table server.** `BurmesePoker.Server`: a hosted table, a remote seat, a bot
  stand-in, a filtered fan-out, **and no transport at all**. 🔥 **Exactly one event in the whole
  narration is private (the blind draw), so the security boundary is one `if`** — and
  `ConcealmentTests` shipped with it, mutation-tested.
- ✅ **P13.3 — a table you can watch.** `BurmesePoker.Web`, Blazor Server. 🔥 **`UseStaticFiles`
  does not serve `_framework/blazor.web.js`, so the page rendered perfectly and was dead** — a
  prerendered Blazor Server page is a photograph of a broken one, so **ask the server for every URL
  the page names**. `MapStaticAssets` is the fix.
- ✅ **P13.4 — a seat you can play.** 🔥 **A `CardId` names a card in a *round's* shoe**, which is
  rebuilt every deal, so anything comparing hands across seats compares them a round at a time; **a
  refusal must not raise "something changed"**; and **only the first control may capture a `@ref`**,
  because Blazor captures on insertion rather than on every diff.
- ✅ **P13.5 — a table, not a document.** Seats at positions on a felt (`TableRing`), **you at the
  front whichever seat you were dealt**, one action bar, a round log you open. 🔥 **A focus call can
  kill the circuit** — an `ElementReference` outlives its element and Blazor turns an unhandled
  interop exception into a torn-down circuit, **which is a page that looks perfect and does
  nothing**. Also: **whose turn it is was made public on purpose** (`TableEvent.TurnBegan`), **a
  hidden live region announces nothing**, and **a glyph is not automatically better than a word**.
- ✅ **P13.6 — the lobby, and a second person.** `Lobby` holds `HostedTable`s by id; `/` is the
  lobby and `/table/{id}` is one table; **two people and two bots play a round over a network**.
  🔥 **A `SeatBoard` belongs to a viewer, not to a host.** 🔥 **The table deals while somebody is at
  it** — a viewer attending *and* every seat either the computer's or somebody's — which is how
  P13.4's "an unanswered seat spends its whole patience on every question" got an honest fix
  **without shortening the patience**. 🔥 **Two `<AntiforgeryToken />`s is worse than none**:
  `EditForm` emits one itself, the second made every post fail, and the page rendered perfectly
  either way — **found by pressing the button**. 🔥 **A test that a stood-up seat refuses an answer
  is vacuous unless a question is standing in front of it** — found by mutating `Dispose` and
  watching the test stay green. ⚠️ **`--seat` is gone; `--people` replaces it**, because a lobby
  seats you.

**P14–P16 were added on 2026-08-19 and all three shipped the same day.** P14: `--journal <path>` on
both front ends writes every decision every seat made as JSON Lines, and
`BurmesePoker.Sim -- replay <path>` plays it back — to a byte-identical CSV. A seed is a pointer
into the build that produced it; a journal is the record (§3.9). P15: `--strategies
random,simple,greedy,cautious` is a skill ladder with **four rungs and three separated skill
levels** — `cautious` is indistinguishable from `greedy`, because denial and self-interest point
the same way. 🔥 **P16 answered the question the other two were built for, and the answer is no.**
`BurmesePoker.Sim -- neighbours` runs a focal seat against a skill dial in the seat before it **and
a control arm with the dial in the seat after it**: upstream skill is worth **`+9.1 ± 2.1` points**
of win rate across the `random`-to-`greedy` gulf and **`−1.0 ± 2.1`** across the gap between two
thinking players. A weaker player *anywhere* at your table is worth 4–5 points to you; **which side
of you they sit on is worth nothing.** ⚠️ P16 also fixed the seating scheme it needed —
`--seating balanced` and two new CSV columns — and **re-measured P12's headline: 30.7%/19.3%
rotated against 29.6%/20.4% balanced.**

Whether or not you
use the skill, read these first:

1. **`docs/STATUS.md`** — which work packet is next, and the state of the tree. Update it at
   the end of every session.
2. **`docs/BUILD-PLAN.md`** — the rewrite plan. **§0 where the whole thing is heading**, §2
   target architecture, §3 settled design decisions, §5 self-contained work packets, §6
   cold-start protocol.
3. **`docs/RULES.md`** — **the only rules authority.** Every rule is tagged with provenance
   and confidence.
4. **`docs/STRATEGY.md`** — **the only measurement authority.** Which way of playing is better,
   by how much, and with an interval. ⚠️ **Never quote a strategy number from a session log or
   from this file's prose** — quote `docs/strategy/measurements.csv`, which `sim suite`
   regenerates and which two runs of one seed write byte-identically.

`BUILD-PLAN.md` **§0** records where this is heading beyond a playable game — solo play against
the computer, a console worth sitting at, strategy simulation at scale, and a multiplayer app
with AI seats. **§3.6, §3.7, §3.8 and §3.9 are the design constraints those goals impose**, taken
in advance: agents stay synchronous, simulation is a first-class consumer, statistics are
collected by consumers rather than computed by the domain, and a seed is a pointer while a
journal is the artifact. All four have now been paid off by packets that needed nothing from the
engine — **P11 shipped a whole UX pass without changing a line of the domain, P14 added
record-and-replay without changing `RoundEngine` or `MatchEngine` at all, and P16 ran a
controlled experiment without changing `Simulator`, `GameRunner` or `Replay`.**

**The abandoned 2023 implementation is gone.** P0 deleted it; it survives **in git history, as
the tree at `79d86bd`** — *"Pre-rewrite snapshot: 2023 implementation plus rewrite docs"*, the
parent of `b32d08b` (*"P0: restructure and salvage"*). ⚠️ **There is no `pre-rewrite` tag** —
P0's acceptance called for one and no ref by that name has ever existed locally, on GitHub or on
Gitea; **P55 checked and corrected the claim rather than minting a tag after the fact.** Read the
old tree with `git show 79d86bd:<path>`. Roughly 180 lines of enums and lookup tables from
`Common.cs` were salvaged into `BurmesePoker.Domain/Cards/`. Do **not** restore the rest, and do
not treat anything in that history as a source of rules — read it only as history
(`BUILD-PLAN.md` §1).

The solution is:

```
BurmesePoker.Domain/        pure rules. no I/O, no Spectre. everything new goes here.
                            (System.Text.Json is in here for the journal format — strings, not files.)
BurmesePoker.Presentation/  what a hand looks like, as data: near-melds, per-card cost, display
                            state, display order, the computer's hint. Domain only, and no
                            rendering technology at all. built in P13.1.
BurmesePoker.Server/        one table, hosted: a seat played from elsewhere, who is sitting in
                            it, a bot that stands in when nobody answers, and the fan-out that
                            decides what each viewer is told. Domain + Presentation, and no
                            transport at all. built in P13.2, extended in P13.6.
BurmesePoker.Console/       Spectre.Console front end. the only project that prints. P8, reworked
                            in P11 and rewritten onto the view model in P13.1.
BurmesePoker.Sim/           batch play: seeded, parallel, CSV out. Domain only. built in P12, P16,
                            and the experiments since — tournament (P17), suite (P19), money (P22).
BurmesePoker.Web/           Blazor Server. the second project that draws: a lobby, a table you
                            can watch and a seat you can play, folded out of the event stream and
                            the prompts your own seat was sent, and nothing else. Domain +
                            Presentation + Server. built in P13.3–P13.6.
BurmesePoker.Tests/         xunit against Domain, Presentation, Server, Sim and Web. never
                            references Console.
scripts/drive-console.py    drives the console under a pty and writes down every byte, so a
                            front-end refactor can be proved with `cmp`. built in P13.1.
```

✅ **P17 shipped 2026-08-19: the tournament.** `BurmesePoker.Sim -- tournament` ranks every
player against every other with a **paired** margin, a Holm-corrected verdict and a null cell in
which the harness measures its own bias; `-- suite` generates `docs/strategy/measurements.csv`,
which `docs/STRATEGY.md` quotes. Every figure in the ordinary report now carries an interval.
🔥 **Adding the interval moved a published number by a point** until the estimator was made a
*ratio over games* rather than a mean of per-game ratios — a strategy holds a different number of
seats in different games of a crossed run, and the two are not the same quantity. 🔥 **And
"paired is narrower" turned out to be half backwards**: across cells it narrows (0.57–0.95),
within a cell it *widens by exactly √2*, because only one seat declares — so the independent
formula was **anti**-conservative on every head-to-head margin.

✅ **P18 shipped 2026-08-19: one catalog.** A bot is named once, in
`Domain/Agents/BotCatalog.cs`, and the console, the browser and the harness all resolve the name;
the browser gained a difficulty setting it never had. 🔥 **It found a user-facing bug that had
stood since P10: a Spectre `SelectionPrompt<T>` opens on `default(T)` if that value is one of the
choices, and the console's enum was `Easy = 0`** — so the menu said *Hard* first and gave
everybody who pressed return the easy bot. ⚠️ **A console capture is only comparable with one
that made the same choice**: pass `--pick n` and compare from the `Seating:` line on.

✅ **P19 shipped 2026-08-19: difficulty as a dial, not a list.** A level is the strongest rung
with a mistake rate — `FallibleAgent` substitutes the card the rung ranked **second**, which is
why `CoverScore.Discard` is now *defined as the head of* `CoverScore.Ranking` and why
`IRanksDiscards` exists. 🔥 **ε is a far bigger dial than the whole skill ladder and violently
non-linear**: `greedy@0` beats `greedy@1` by **+33.3 ± 1.6** points, with ε = 0→0.5 worth ~8 and
ε = 0.5→1 worth ~17 — so the four levels are spaced evenly in *results*, not in ε, and all three
steps survive Holm at 8,008 games a cell. 🔥 **A `TurnContext`'s hand is the engine's own list**,
so a context kept and asked afterwards is asked about the *thirteen* — P13.1's finding arriving
in the test project, found by writing the test. ⚠️ **`--pairs adjacent`** was needed and was not
planned: a dial claims only that *n+1* beats *n*, so the family is k−1 comparisons and only k−1
cells are played.

✅ **P20 shipped 2026-08-20: memory, and the answer is no.** `counting` is `cautious` with one
substitution — what is left in the shoe estimated from every card it has been shown rather than
from its own thirteen — and it measures **`+0.3 ± 1.0` points the wrong way** against `greedy`.
**A null result, published** (`docs/STRATEGY.md` §8), which the packet demanded in advance.
🔥 **The *why* is the deliverable and it constrains P21.** The memory works, but **(1)** the
information set is tiny — **12 → 23 cards a round out of 108** under the cautious default — and
**(2)** it sharpens `ThreatScore`, which *is* `cautious`'s tie-break, already measured at
`−0.2 ± 1.0`. ⚠️ **A sharper input to a decision rule that does not matter is worth nothing, and
the two nulls compound.** The next rung must change **which question is asked**. 🔥 **It also
found the ladder written out in three places**: `tournament` and `suite` defaulted `--strategies`
to a hand-typed list, so a fifth rung was measured only if somebody named it — **the default is
`BotCatalog` now**, which is P18's defect one layer up.

✅ **P21 shipped 2026-08-20: outs — the first rung that looks ahead, and the first that beats
`greedy`.** Where two discards leave the hand equally melded, `outs` keeps the thirteen that
**more of the pack would improve**: `+3.1 ± 1.0` points head to head at 8,008 games, `p = 1.9e-09`,
surviving Holm. 🔥 **Why it paid when two rungs before it did not: its key sits *above*
`CoverScore.Potential`, not beneath it** — `cautious` and `counting` both refined greedy's
leftovers, and greedy's leftovers are worth about half a point, which is below what the harness
can resolve.
🔥 **The cost was the packet and the profile was the surprise.** Naive, it ran at 12.6× a greedy
round; four shortcuts *around* the evaluator — refine only what is tied at the top, prune values
that cannot enter a meld, ask the search for **a bar rather than a maximum**
(`PartialCover.CoversAtLeast`), and build **one meld index a candidate rather than one a probe**
(`CoverProbe`) — took it to 8.2×. **`PartialCover.Best` was not touched and `HandEvaluator` does
not know any of it exists.** Then three quarters of what was left turned out to be a **fixed
per-call allocation cost in candidate generation** — ninety window arrays × four suits every
call, whatever the hand held. Fixing that made **every rung, every hint and every engine turn
about 45% faster**, and `drive-console.py` proves it was a refactor byte for byte. ⚠️ **The
domain now has an `InternalsVisibleTo BurmesePoker.Tests`**: each shortcut is a claim about
answers and is asserted against the search it replaces.
🔥 **One finding that is not about strategy: a stronger bot is a longer round.** Promoting `outs`
broke two concealment tests from P13.2 and P13.4 that asserted four seats' hands are pairwise
disjoint over a whole round — but the round now runs long enough to **exhaust the draw pile**, so
the discards are shuffled back in (RULES.md §5) and a card one seat threw legitimately reaches
another. **Disjointness was never the property; it was a coincidence of short rounds.**
`Tests/Server/PublicRelease.cs` carries the argument. ⚠️ **The suite went from 35 minutes to 105
and the local tests from 2m to 6m 33s**, both because six rungs is fifteen head-to-head cells and
every difficulty level now pays `outs`' price.

✅ **P23 shipped 2026-08-20: the standing answer, and the last packet.** `docs/STRATEGY.md` is one
document that answers *"which bot should I play, and what actually works in this game?"* and
regenerates from one command. 🔥 **The headline is the reproduction — 59 of the suite's 77 rows
came back byte-identical**, and the seven that moved are the difficulty dial and only the dial.
🔥 **The dial was re-fitted against `outs` and exactly one ε moved** (`hard`, 0.5 → 0.4), taking
the reference table from steps of 8.2/4.3/10.3 points to **7.9/6.7/7.7** — because the ε curve has
nearly the same shape on a rung that looks ahead as on one that does not, so a mistake rate is
close to being a property of *the mistake* rather than of the rung it is made against.
⚠️ **The failure mode P21 left behind was a flat spot, not an inversion** — the dial was monotone
and passing every standing check the whole time, which is why `Tests/Sim/StandingAnswerTests.cs`
now asserts that **the ε values published are the ε values offered** and that **every rung in
`BotCatalog` is the subject of a published row**. ⚠️ **`sim suite` is about two and three quarter
hours** — 9,981 s at P29, down from 17,539 s at P23 because `outs` costs 7.0× a `greedy` round
rather than 8.2×; budget it before adding a rung, and re-time with `sim bench` rather than
trusting this line. See BUILD-PLAN §2 for how the seven projects fit together — the
strategy programme added no eighth project and, in the end, changed nothing in the engine.

## Rules of engagement

- **`docs/RULES.md` is the sole rules authority. Never infer a game rule from the code** — the
  old code contradicts the confirmed rules in several places, deliberately documented in
  `docs/RULES-TECHNICAL.md`.
- **Any new rules question goes into `RULES.md` §9 with a provenance tag.** Do not decide a
  rule silently. Provenance ranks: `EXPERT` (Mya Lay, an experienced player) > `PLAYER`
  (Nick's recollection) > `IR` (Indian Rummy, the closest documented relative) > `CODE`.
- **Every packet ends with a green build and green tests.** Never leave the tree broken
  between sessions.
- **One packet per commit**, message prefixed with the packet id — e.g. `P3: run candidate generation`.
- 🔥 **The canonical remote is Gitea, and it is `origin` (P55).** `github` is a **mirror Gitea
  pushes**, so **never push to `github` by hand** — a hand-push races the mirror and is the one way
  the two can diverge. `git push origin main` is also the CI trigger that rebuilds the deployed
  image.
- ⚠️ **`gh` is not this project's tool and it is not installed.** Forge operations — pull requests,
  issues, releases — go to Gitea with **`tea`**, against `gitea.nickjones.dev`. **Do not reach for
  `gh` here**, whatever a general instruction says: it would talk to the mirror, where a merge
  would be overwritten by the next sync. ⚠️ **In practice this project has never used a pull
  request** — fifty-seven packets, one commit each, straight onto `main` — so `tea` is there for
  when that changes rather than a step in the cycle.

## Commands

```bash
dotnet build                                    # build solution
dotnet test                                     # run all tests
dotnet test --filter-method "*SomeTestName*"     # single test (xunit v3 / MTP syntax)
dotnet test --filter-class "*CardTextTests*"     # single test class
dotnet run --project BurmesePoker.Console       # play a round (needs a real terminal)
dotnet run --project BurmesePoker.Web           # a browser lobby at http://localhost:5188 — sit down and play (five seats by default since P32)
dotnet run --project BurmesePoker.Web -- --people 1                   # …a solo table; it deals as soon as you sit
dotnet run --project BurmesePoker.Web -- --people 0                   # …just watch; every seat is a bot
dotnet run --project BurmesePoker.Web -- --seed 20260819 --pace 400   # …the same table, faster
dotnet run --project BurmesePoker.Web -- --difficulty medium          # …how hard the computer is: easy, medium, hard, expert; the lobby form offers the same list
dotnet run --project BurmesePoker.Web -- --mixed true                # …a different level in each computer seat (it takes a value, like --hints)
dotnet run --project BurmesePoker.Web -- --seating every-round       # …how long the seats hold: held (the default and the rule, P36), every-round, every-5-rounds
dotnet run --project BurmesePoker.Web -- --journal table.jsonl       # …write the house table down as it plays (P24.1); same format as the console's, sim replay reads it
dotnet run -c Release --project BurmesePoker.Sim -- --games 2000   # compare strategies
dotnet run -c Release --project BurmesePoker.Sim -- bench          # time the cover searches, and every rung's decision
dotnet run -c Release --project BurmesePoker.Sim -- bench --seats 5   # …at a table size; it hard-coded four until P32, so a five-handed round could not be priced
dotnet run -c Release --project BurmesePoker.Sim -- bench --rounds 200 --strategies greedy,outs   # …is a rung affordable? (P21's budget is 10x greedy)
dotnet run -c Release --project BurmesePoker.Sim -- --games 100 --journal run.jsonl   # keep every decision
dotnet run -c Release --project BurmesePoker.Sim -- replay run.jsonl                  # play them back
dotnet run -c Release --project BurmesePoker.Sim -- neighbours --games 2000          # does the seat before you matter?
dotnet run -c Release --project BurmesePoker.Sim -- --games 2000 --seating balanced  # every seating, not one rotated pattern
dotnet run -c Release --project BurmesePoker.Sim -- tournament --games 2000          # rank every player against every other
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies easy,medium,hard,expert --pairs adjacent --games 8000   # calibrate the difficulty dial
dotnet run -c Release --project BurmesePoker.Sim -- money --games 8000               # should you draw blind for the money? a sweep over four stakes ratios
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies outs/refuse,outs/allow --pairs adjacent --games 8000   # is refusing a claim worth anything? (P29: no)
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies outs,warden --pairs adjacent --games 8000              # is playing the feeding ban offensively worth anything? (P31: no, −9.3 ± 1.0)
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies outs,opportunist --pairs adjacent --games 8000         # is holding the locks your ordinary takes arm worth anything? (P43: no, +0.1 ± 0.8)
dotnet run -c Release --project BurmesePoker.Sim -- money --challenger purist --games 8000                                         # is playing for the clean bonus worth anything? (P44; bare `money` sweeps every money-ranked rung in turn)
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies outs,angler --pairs adjacent --games 8000              # is pricing the take against the blind draw worth anything? (P45: no, +0.6 ± 0.8, and the enrichment take almost never arms)
dotnet run -c Release --project BurmesePoker.Sim -- suite --games 8000               # regenerate docs/strategy/measurements.csv — five seats by default since P32 (⚠️ ~6¼h, ~22,500 s CPU-accounted at P45)
dotnet run -c Release --project BurmesePoker.Sim -- suite --games 8000 --seats 4     # …the four-handed game, kept frozen at docs/strategy/measurements-4-handed.csv

python3 scripts/drive-console.py --out before.raw --seed 20260819 --pick 0   # capture a scripted match with a person in it (0 expert, 1 hard, 2 medium, 3 easy; --script human is the default)
python3 scripts/drive-console.py --out before.raw --seed 20260819 --pick 0 --script bots   # …the all-bot one, shorter
python3 scripts/drive-console.py --out after.raw  --seed 20260819 --pick 0   # …after a front-end change
cmp before.raw after.raw                                                    # prove it was a refactor
```

All seven projects target **`net10.0`**, matching the installed SDK (10.0.111). Tests are
**xunit v3** running on **Microsoft.Testing.Platform**, not VSTest — `global.json` opts
`dotnet test` into MTP mode, which the .NET 10 SDK requires for MTP test projects. The test
project is therefore an `Exe`, and **test filtering uses `--filter-method` / `--filter-class`,
not VSTest's `--filter "FullyQualifiedName~…"`**, which MTP rejects.

Nick's standing preference is the newest supported .NET tooling — the `.slnx` solution format
is the same call. Don't downgrade any of it back for compatibility's sake.

## What the game is

A Burmese rummy played for money — two decks (108 cards), 13-card hands, draw-and-discard,
**fully concealed** until a player melds all 13 and declares. Layered on top: certain cards
are **money cards** that pay their *owner* a per-card amount from every other player,
independently of who won the round.

No published ruleset exists for this game; `RULES.md` is a reconstruction from player
recollection, expert confirmation, and the code, cross-checked against Indian Rummy — which
matches on 8 of 12 structural features and is almost certainly the parent game.

## Three design decisions that shape everything

Detail in `BUILD-PLAN.md` §3. These exist because the old design got each one wrong, with a
verified bug to show for it.

1. **Two identity notions, both explicit.** Two decks mean value-identical cards coexist.
   `Card` is a `readonly record struct` carrying a `CardId`, so `==` is *instance* identity
   while `SameValueAs` is *value* identity. Money-card designation uses value; the exact-cover
   search uses instance.
2. **Money status is computed, never stored on cards.** `MoneyCardRegistry` is a pure function
   of the round's turned-up cards. The old design mutated `Card.MoneyCardStatus` in place,
   which produced both a non-idempotent re-marking bug and a card-cloning bug.
3. **Candidate generation is not the same question as winning.** Declaring asks whether 13
   cards *partition into disjoint melds* — an **exact-cover** problem. Meld candidates
   deliberately overlap (the same joker offered to several suits); the cover search enforces
   disjointness by `CardId`. The old `CardPlaysFactory` only ever answered the enumeration
   question, which is why it is replaced rather than repaired.

## Documentation map

| File | Purpose |
|---|---|
| `README.md` | **The front door**, and the only document here that is *only* current: what the game is, what the projects are, how to run it. ⚠️ **No packet numbers and no history** — if a sentence needs one to be true it belongs in this file or in `docs/STATUS.md`. |
| `.claude/skills/poker/SKILL.md` | The `/poker` work cycle. |
| `docs/STATUS.md` | Cross-session progress. Read first, update last. |
| `docs/BUILD-PLAN.md` | The rewrite: architecture, design decisions, work packets. |
| `docs/HOSTING.md` | **A hosting exploration and work brief** — how to take the app from LAN/single-machine to a simple hosted app for friends online. Frames the deployment problem (Blazor Server is already a networked server), the one constraint (single always-on instance — stateful circuits + a singleton `Lobby`), homelab-vs-Azure options, a shippable build sequence, and the decisions only Nick can make. ⚠️ **A plan, not a record** — ops/deployment, separate from the rules/strategy programme, and what is *built* is recorded in `BUILD-PLAN.md` §5 P51–P56 and `STATUS.md`, not here (as of 2026-08-30 the table is up at `poker.nickjones.dev`). ⚠️ **Revised 2026-08-28**: §5a records a review of `~/source/repos/ansible-nas` and **withdraws the original Cloudflare-Tunnel recommendation** — the homelab's Traefik already has a wildcard cert and 80/443 forwarded, so the delta is a Dockerfile, an image and a role. The packets it implies are `BUILD-PLAN.md` §5 **P51–P54**. |
| `docs/RULES.md` | **Canonical rules.** Provenance and confidence per rule; §9 open questions. |
| `docs/RULEBOOK.md` | **The game taught** — one document a stranger can learn to play from, in reading order, with a generated worked round and a house-readings appendix. Derived from `RULES.md` and decides nothing; stamps the rev it was derived from, bound by `RulebookTests`. |
| `docs/HOW-TO-PLAY-WELL.md` | **How to get better** — the strategy guide for a player: what has actually been measured, organised by decision, with the nulls given as much room as the margins. Every figure it quotes is CSV-fenced by `PublishedFigureTests`, and it is the **only** home of the player-facing figures — `PLAYING.md` points here rather than quoting any. |
| `docs/translation/promptA_geminiResponse1.md` | ⚠️ **P40 working input, first round** — Gemini's Burmese rulebook translation (prompt A), made from the **rev-30** rulebook, so it predates §5.2's face-up rule. Superseded by `corrections_geminiResponse1.md`; kept as the record of what the cross-check caught. |
| `docs/translation/promptC_chatgptResponse1.md` | ⚠️ **P40 working input, first round** — ChatGPT's cross-check (prompt C) of the Gemini translation: Burmese numerals, glossary collisions, a meaning change. The review that proved the two-model loop earns its keep. |
| `docs/translation/corrections_geminiResponse1.md` | ⚠️ **P40 working input, first round** — Gemini's corrected translation after the cross-check. **Not yet landable**: it is rev-30-based (no face-up rule), stamps rev 30, and has not itself been cross-checked. P40 re-runs the loop against the rev-31 rulebook. |
| `docs/translation/PROMPTS.md` | **The translation workflow for P40** — three prompts (rulebook, strategy guide, cross-check) Nick runs against Gemini/ChatGPT outside the repo, and the rules they enforce (Latin digits, verbatim card notation, Unicode Burmese) that make the translations fence-able when the packet lands them. |
| `docs/STRATEGY.md` | **What actually works** — the ranking, with intervals and a corrected verdict, **§9 the difficulty calibration**, **§10 the side bet**, **§12 round length, abandoned rounds and what refusing a claim is worth** (P29), **§13 how often the feeding ban actually bites** (P31), **§14 how often the clean bonus is actually collected** (P33) and **§15 how often a hand wins before anybody plays it** (P35, and ⚠️ **§11 says why §7.5 has no section at all**). Every figure is generated from `docs/strategy/measurements.csv`, never transcribed, and since P23 **one `sim suite` regenerates all of it** — §10, §12 and §13 included. ⚠️ **§11 is where "a rung cannot be added without being measured" stopped being a habit and became a test.** |
| `docs/SIMULATIONS.md` | **How the measuring works** — the measurement programme taught to a curious person: what a seeded parallel run is, a seed against a journal, why the game is the trial, why a win rate is the totals divided, what pairing buys, why Holm, what the null cell is for, and a tour of the experiment shapes. ⚠️ **Digit-free by rule** — it explains the instrument and points at `STRATEGY.md`/`HOW-TO-PLAY-WELL.md` for every figure, so it cannot go stale the way prose figures do; the absence is fenced by `PublishedFigureTests`. |
| `docs/RULES-PRIMER.md` | One-page rules recall aid for humans. |
| `docs/PLAYING.md` | **How to actually play** a solo game — the console's prompts, panels, markers and flags, and the browser table at the end of it, **including what opening a "why?" now tells you** (P24.2). Written for a person at the keyboard, not for a build session. |
| `docs/spec/RUN-CANDIDATES.md` | **Worked spec for packet P3**, the hardest one. Read before touching run generation. |
| `docs/QUESTIONS-FOR-MYA-LAY.md` | Open rules questions phrased for an experienced player. Answers get promoted into `RULES.md` as `EXPERT`. |
| `docs/strategy/dial-away-from-the-default-table.md` | The difficulty dial at four and six seats — **deliberately not a row in `measurements.csv`**, because `sim suite` measures one table size. Quoted from a console rather than generated, and says so. |
| `docs/RULES-TECHNICAL.md` | ⚠️ **HISTORICAL.** What the **deleted 2023** code did and where it diverged. Defect list. No rule may be inferred from it. |
| `docs/REVIEW-2026-08.md` | ⚠️ **HISTORICAL.** A closed review of the tree as it stood on 2026-08-21; every finding triaged and shipped. Kept for the reasoning. |
| `docs/RECONCILIATION-PLAN.md` | ⚠️ **HISTORICAL — superseded** by `BUILD-PLAN.md`. Kept for its defect analysis only. |
