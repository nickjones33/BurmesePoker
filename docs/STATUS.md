# Build Status

Cross-session progress tracker. **`/poker` reads this first and updates it last.**

Plan: `BUILD-PLAN.md` · Rules: `RULES.md` (rev 31) · Skill: `.claude/skills/poker/SKILL.md`

State markers: `☐` not started · `◐` in progress · `☑` done

---

## Current state

◐ **`P62` is in progress, and what is done is its step 0 — in a different repository.**
`RULES.md` stays **rev 31**, the tree is green at **941**, and ⚠️ **nothing in *this* repository
changed at all** — no code, no test, no measurement, and no suite is owed. 🔥 **The rest of P62
cannot be done from a session**: it is a round played on a real phone over a real carrier, and
emulation, a simulator, a responsive-mode viewport and a tablet are all named in the packet as ways
of *not being a person on a phone*. **It needs Nick, a phone and a train.**

- 🔥 **(1) The instrument P60 found missing now exists in source: Traefik has an access log.**
  `~/source/repos/ansible-nas` commit **`c57a9ea4`** — `traefik.toml` gains `[accessLog]` in JSON
  to the container's stdout, gated on a new `traefik_access_log_enabled` defaulting **on**.
  ⚠️ **Without it a carrier request and a house-wifi request leave *identical* evidence**, which is
  the single most likely way P62 returns a false pass: a phone with wifi accidentally left on.
- 🔥 **(2) `User-Agent` is kept explicitly, and that is not a detail.** Traefik's default is
  `fields.headers.defaultMode = "drop"`, so the header that names the browser engine is the one
  field the packet needs that the defaults throw away — and **P62 step 4 is a question about
  WebKit specifically**, answerable only if the log says which browser made the request.
  `ClientHost`, `RequestPath` and `DownstreamStatus` are kept by the `defaultMode = "keep"` that
  covers ordinary fields.
- ✅ **(3) The template was rendered and parsed rather than eyeballed.** Jinja-rendered both ways
  and parsed as TOML: on, it yields the `accessLog` table above; off, the key is absent and the
  document still parses. **A malformed `traefik.toml` takes the whole proxy down**, so this is the
  cheapest possible fence for the one change that could.
- ✅ **(4) There is a fence in the only place that repository has one.** The traefik role's
  `molecule/default/verify.yml` slurps the **templated** config back off the host and fails if it
  does not carry `[accessLog]`, `format = "json"` and `User-Agent = "keep"`.
- ⚠️ **(5) What is owed before travelling, said out loud.** **The playbook has not been run** —
  `ansible-playbook nas.yml --tags traefik --become --ask-become-pass`, after `ssh-add
  ~/ubuntuServer22key` — and **`ClientHost` has not been seen populated on the real box.** 🔥 **A
  log line with an empty or bridge-local `ClientHost` is exactly the failure step 0 exists to
  catch**, so read `docker logs traefik` for a request from a known device *before* trusting the
  field. Traefik runs `network_mode: host`, which is the reason to expect a real address, and
  `UseForwardedHeaders` in the app means the app and the proxy can still disagree.
- ⚠️ **(6) P61 is still owed a redeploy, and it is the same trip.** `git push origin main` is the
  CI trigger and a work cycle does not push, so the tablet fixes P61 shipped **are not on the
  deployed site**. P60's *pulled is not running* applies: read what the running container is
  **built from** (`docker inspect <c> --format '{{.Image}}'` against
  `docker images … '{{.Tag}} {{.ID}}'`), never the tag `docker ps` prints.
- ✅ **(7) One P62 question is already answered and does not need the trip.** `poker.nickjones.dev`
  is `209.128.193.153` with **no `AAAA` record** (P60), so on an IPv6-only carrier the site loads
  only if DNS64/NAT64 synthesises — ⚠️ **and if that bites, the fix is one DNS record rather than
  anything in this repository.** Check the phone's network before blaming Safari.

---

🔥 **`P61` shipped 2026-08-30 on Opus 5: the two defects a real device found are fixed, each with a
fence that fails for the right reason — and the copy-link's fix turned out to need nothing at all
in `:global()`'s place.** `RULES.md` stays **rev 31**, the tree is green at **941** (from 938), and
⚠️ **`Domain`, `Presentation`, `Server`, `Console` and `Sim` are byte-identical** — the whole diff
is two stylesheets, one new test class and one extended one, so **no measurement can move and no
suite is owed.**

- 🔥 **(1) The copy-link reveal is one selector shorter, not one file over.** P61's plan
  recommended moving the rule into the unscoped `app.css`; ⚠️ **that is not what shipped, and the
  reason is the finding**: Blazor's rewriter appends the scope attribute to the **last compound
  selector only**, so `.can-copy .link .copy` is emitted as `.can-copy .link .copy[b-…]` and an
  ancestor outside the component was never constrained in the first place. **`:global()` was not
  solving a problem — it was the problem.** ✅ **The rule stays beside the `display: none` default
  it overrides**, which is where the ordering that makes it win can be seen, and `app.css` did not
  have to learn a component's internals. ✅ **`::deep` rejected for the reason the packet gave** —
  it scopes descendants of the component's own root and `.can-copy` is on `<html>` — and the
  `<a class="url">` fallback is untouched.
- ✅ **(2) Measured in a browser engine, which is the only place this defect was ever visible.**
  Headless Chromium driven over CDP against the running client: `can-copy` on `<html>`, the button
  present, computed **`display: block`** (a flex item blockifies `inline-block`) at **82 px** wide,
  and the live CSSOM holding **two** `.copy` rules where P60's tablet held exactly one. The same
  run reads the served bundle at `/BurmesePoker.Web.styles.css` and finds both rules in it.
- 🔥 **(3) The scoped-CSS fence is the class of mistake, and it reads the rewriter's output.**
  `ScopedCssTests` is new: one fact fails on **any** `:global(` under
  `obj/<config>/net10.0/scopedcss/**` — the configuration taken from where the test assembly is
  running, so a stale `obj/Release` cannot fail a Debug build — and its positive twin reads the
  **served bundle** and asserts the reveal exists **and is declared after** the default that hides
  it, because the two weigh exactly the same and order is the whole of why one wins. ⚠️ **The scan
  strips comments first**: `Tables.razor.css` now names the construct in a comment beside the rule,
  and a scan that read the prose would fail on the one file that had learned the lesson — P13.3's
  rule, arriving in a stylesheet.
- 🔥 **(4) A18 is amended, and the amendment is that it was fitted to the wrong axis.** The
  argument for ellipsing is *the whole name is one hover away*; the rule carrying it was
  `max-width: 56rem`. **Those are two axes and a tablet in landscape is where they come apart** —
  1316 px, `(any-hover: none)`. `SeatPanel.razor.css` now carries a second `@media`, on
  `(any-hover: none)`, **declared after the width one** because on a narrow touch screen both match
  and they weigh the same. ⚠️ **Amended rather than contradicted** (P56's precedent): what A18
  forbade — a name nobody can read — is unchanged, and P58's width half stands as written.
  ⚠️ **Widening the ring's name column is the fix this rejects**, because a floor fitted from what
  the longest generated name measures in *this* machine's font passes only where it was fitted.
- ⚠️ **(5) Four mutations, all of the stylesheets and none of the tests.** `:global()` put back
  (both scoped-CSS facts red); the reveal moved above the default (the ordering fact red); the
  capability block's `normal` turned back to `nowrap` (the hover fact red); the capability block
  moved above the width one (its ordering fact red). **This project's rule, and here it is the only
  way to tell a live rule from a discarded one.**
- ⚠️ **(6) What is owed, said out loud.** **The redeploy and the device are not done.**
  `git push origin main` is the CI trigger and a work cycle does not push, so **nothing here is
  proved on a real tablet until the image is rebuilt and the running container re-read** —
  P60's *pulled is not running*. ⚠️ **And headless Chromium answers `(any-hover: none)` by
  default**, so the browser measurement exercised the **no-hover branch only**; the hover branch is
  fenced in source and unmeasured in a browser here.

**`P62` is the next packet — the table on a phone, on a carrier**, the remainder of P53's
acceptance. ⚠️ **It has a step 0 in a different repository** (`ansible-nas`): **Traefik must be
given an access log first**, because without a client IP a carrier path and the house wifi produce
identical evidence. See `BUILD-PLAN.md` §5.

---

🔥 **`P60` shipped 2026-08-30 on Opus 5: the table was played on a real tablet, in both
orientations, and pressing the two affordances nothing had ever pressed found a defect that has
been invisible since P54.** `RULES.md` stays **rev 31**, the tree is green at **938**, and
⚠️ **nothing in the repository changed** — the whole diff is documents, which for a verification
packet is a result rather than a failure. **The deployed commit was `62ed294` (P59), verified on
the box before anything was touched**; the tablet half of P53's acceptance is discharged and
**the phone-on-cellular half is not** — with a named instrument missing before it can be.

- ✅ **(1) The device, and why it was the right one.** Samsung Galaxy Tab S9 FE (`SM-X510`),
  Android 16, Chrome 151, 1440×2304 at **1.75 dppx** — so **823 CSS px portrait and 1316
  landscape**. 🔥 **One device rotated really does exercise both sides of the only breakpoint the
  application has**, exactly as the packet predicted, and portrait lands *inside* P58's 600–896 px
  defect band. Driven over `adb forward tcp:9222 localabstract:chrome_devtools_remote` with the
  container log tailed throughout.
- ✅ **(2) P58's fix holds on a real device, in a font this workstation has never rendered.** At
  823 px the felt stacks to four 181 px columns; every seat name is `white-space: normal`, **not
  one is clipped** (`scrollWidth == clientWidth` on all six), and the long ones wrap to two lines —
  `Khine Myat Zin (opportunist)` at 141 px over 44.6 px of height, in the device's own
  `ui-sans-serif` at 14.4 px. `documentElement.scrollWidth == clientWidth == 823`: no horizontal
  overflow. ⚠️ **This is the fence P58 refused to fit to one platform's font, and it survived the
  change of platform.**
- 🔥 **(3) The defect P58 left behind is above the line, not below it, and a tablet is where it
  lives.** In landscape the ring returns and the name reverts to `nowrap` + `text-overflow:
  ellipsis` — **and three of six names are actually clipped**: *"Su Htwe (oppo…"* (77% shown),
  *"Myat Htwe (op…"* (70%), *"Khine Myat Zin (oppo…"* (80%). ⚠️ **P58's honesty argument for the
  ellipsis is that the whole name is a hover away**, and this device answers
  `(hover: none)`, `(any-hover: none)`, `(pointer: coarse)`, `maxTouchPoints: 5` — **at 1316 px.**
  🔥 **So A18 is fitted to *width* while the argument that justifies it depends on *hover*, and a
  tablet in landscape is the counterexample.** The `title=` is present and unreachable.
- 🔥 **(4) P54's copy-link has never been visible in any browser, and this is why nobody noticed.**
  `Tables.razor.css` reveals the button with **`:global(.can-copy) .link .copy`** — but `:global()`
  is a **CSS-Modules** construct, not Blazor scoped CSS (which has `::deep`), and the Razor
  rewriter emits it **verbatim**. The browser discards the whole rule as an invalid selector, so
  only `.link .copy { display: none }` survives: **live CSSOM on the device contains exactly one
  `.copy` rule and it is the hiding one.** ⚠️ **Nothing is broken for a player** — the `<a
  class="url">` beside it is P54's designed fallback and works — **the enhancement simply never
  appears.** ✅ **The handler itself is sound**: revealed by an injected style and pressed
  physically, the delegated listener fires and the clipboard receives
  `https://poker.nickjones.dev/table/3` — the correct **forwarded** absolute URL.
- 🔥 **(5) A synthesized touch is not a person either, and it was measured in passing.** The same
  press dispatched through CDP `Input.dispatchTouchEvent` fired the click handler but **silently
  failed the clipboard write** (no user activation); the physical `adb input tap` succeeded.
  ⚠️ **This project's scar has a third instance now** — after `--no-restore` and `curl` — and it
  argues for physical input wherever a capability gate is involved.
- ✅ **(6) A whole round was played from the tablet, and the rules behaved.** Opened through
  **P56's two-step per-seat form** — which grew its four `Wanted.PerSeat[…]` selects on the device
  and produced a table of `opportunist, sprinter, easy, warden`, shown as such on the lobby card —
  then sat down with a **physical tap and the on-screen keyboard**. The claim on the turned-up 3♠
  was **refused**: *"Myat Htwe (warden) refused Nick the turned-up card — which they may only by
  holding that rank"* (P28, on a tablet), the blind draw arrived privately, and the round settled
  after **47 turns** with melds and money on screen. **Rotated mid-turn**: the circuit survived,
  the ring came back and the standing discard question was still standing.
- ✅ **(7) Log-side evidence, P53's grade.** `_blazor/negotiate` **200**; three circuits opened,
  each closing **101**, the playing one alive **243 s** across the whole round; touch targets
  measured on the device at **44 px** buttons and **66 × 76 px** cards (§3.11 B11, confirmed
  rather than assumed).
- ⚠️ **(8) What is NOT discharged, and the instrument that is missing.** **No phone, no cellular,
  no Mobile Safari** — the tablet was on home wifi throughout. 🔥 **And the packet's item 3 cannot
  be satisfied today even with a phone in hand: nothing logs the client IP.** Traefik has **no
  `accessLog` configured** (checked on the box) and the app logs no remote address, so *"prove the
  session came over the carrier rather than silently over the home wifi"* has **no instrument**.
  ⚠️ **That is a prerequisite for the phone half and it lives in `ansible-nas`, not here.**
- ✅ **(9) The IPv6 question is answered without a device**: `poker.nickjones.dev` resolves to
  `209.128.193.153` and has **no `AAAA` record**. The DNS64/NAT64 failure mode is live and
  unmitigated; **check it before blaming the browser**, and the fix is one record.
- 🔥 **(10) Found before the device was touched, and worth more than it looks: pulled is not
  running.** The NAS had `:latest` = `2881b65` **pulled four hours earlier** while the container
  was still serving **`f309a9d`** — the role fetched the image and never recreated the container.
  ⚠️ **P53's finding (8) is not "read the deployed tag", it is "read what the running container is
  built from"**, and `docker ps` alone would have said `:latest` and been wrong.

**`P61` is the next packet** — the two defects above, each with a fence — **and `P62` stands
behind it: the table on a phone, on a carrier**, which is the remainder of P53's acceptance.
⚠️ **`P62` has a step 0 in a different repository** (`ansible-nas`): **Traefik must be given an
access log before a cellular test can conclude anything**, because without a client IP a carrier
path and the house wifi produce identical evidence. See `BUILD-PLAN.md` §5.

---

🔥 **`P59` shipped 2026-08-30 on Opus 5: a real Blazor circuit can be held from a test, killed
without a close frame, and asked for back — and what a table does in that gap is measured rather
than assumed.** `RULES.md` stays **rev 31**, the tree is green at **938**, and ⚠️ **not one line of
production code changed** — `Domain`, `Presentation`, `Server`, `Console`, `Sim` **and `Web`** are
byte-identical, so **no measurement can move and no suite is owed**. **`P60` is the next packet,
and it is the last of the small-screen track.**

- 🔥 **(1) The instrument is the packet's real cost, and it is one new class plus one new
  package.** A circuit is reached over SignalR, and `AddInteractiveServerComponents` narrows that
  hub to **one protocol — `blazorpack`** — which no client library speaks. So
  `BurmesePoker.Tests/Web/BlazorPack.cs` writes down the slice of it a circuit's opening needs
  (SignalR's length-prefixed framing, the MessagePack shapes on it) and
  `BurmesePoker.Tests/Web/BlazorCircuit.cs` starts a circuit from **the page's own component
  markers**, exactly as `blazor.web.js` does. ⚠️ **`Microsoft.AspNetCore.Mvc.Testing` is now
  referenced by the test project** — the first package here that is not the test framework — for
  `WebApplicationFactory` and `TestServer`.
- ✅ **The instrument proves itself before it is used.** `TableView` claims the seat in
  `OnInitialized` **and only when it is really interactive** (§3.11 C13), so *the table's one
  person-seat ceasing to wait* is proof that a circuit started, rendered and ran the component —
  **not** that a page was fetched. That is `ARealCircuitSitsDownInTheSeat`, and it is the answer to
  P52's *"`curl` is not a person"* at the only level a test can give one.
- 🔥 **(2) The three answers differ, so P54's pairing is load-bearing rather than decorative.**
  At the test's scale (retention **3 s**, patience **8 s** — the shipped ordering, shrunk):
  **inside the window** the reconnection succeeds, the seat is still theirs and the turn is still
  theirs; **past the window but inside the patience** the reconnection **fails** and the seat is
  **given up** — `TableView.Dispose` stands the player up — **while the turn is not lost**, and
  sitting down again under the same name takes the seat back with the question still standing;
  **past both** the computer has played the turn.
- 🔥 **The finding inside that: a seat and a turn are recovered by two different mechanisms.**
  The seat comes back **by name** (P13.6); the turn comes back **because the patience has not run
  out**. ⚠️ **Losing the circuit is not losing the game**, and nothing had ever said so.
- 🔥 **(3) P54's claim is now exercised instead of asserted against itself.** Run the identical
  disconnection with the patience **below** the retention period and the computer plays the turn of
  somebody **the framework is still holding** — the reconnection succeeds, and the player comes
  back to a table that decided something in the gap **with nothing on screen to say so.** ⚠️ **That
  is why the two numbers must not be moved independently**, and it is the failure `ContainerTests`
  can only assert the absence of.
- ✅ **Proved able to fail exactly as the packet asked**: shortening the patience below the
  retention period turns the *inside the window* test red.
- 🔥 **(4) `TableBoard.Turn` is the wrong instrument for "did the player lose a turn", and
  measuring is what found it.** A seat is asked **twice** in one turn — whether to take, then what
  to throw — so **a turn sits on a seat for two patiences**, and a turn the computer has already
  played still reads as that seat's. (Measured: with a 12 s patience the stand-in played at ~13 s
  and the next seat's turn only began at ~25 s.) **`TableBoard.StoodInFor` is the fact itself** and
  is what the tests read.
- ⚠️ **(5) Two things deliberately not done, said out loud.** **(a)** The packet costed a
  `tc netem` arm at ~150 ms RTT with loss and jitter; **there is no socket to shape** — `TestServer`'s
  WebSocket is a pair of in-memory pipes — and shaping a real one needs root on an interface, which
  is not a thing a test may take. **Latency is P60's, on a real network.** **(b)** The obstacle
  recorded in advance — `SeatChannel`'s undisposed `ManualResetEventSlim` and `Ask` taking no
  cancellation token — **was never reached**: nothing here tears a seat down, the window is observed
  from outside, and `Server` is byte-identical. **Decide whether a packet needs that before assuming
  it does** — this one did not.

---

🔥 **`P58` shipped 2026-08-30 on Opus 5, and the packet that was meant to add a standard found a
defect.** `RULES.md` stays **rev 31**, the tree is green at **933**, and ⚠️ **`Domain`,
`Presentation`, `Server`, `Console` and `Sim` are byte-identical** — the whole diff is one CSS
block, `BurmesePoker.Tests/Web/ViewportTests.cs` and documents, **so no measurement can move and
no suite is owed.** **`P59` is the next packet, then `P60`.** *(Both since: `P59` is done — see the block at the top; `P60` is next.)*

- 🔥 **The sub-896 px question is answered — *no* — and the narrow end was never the problem.**
  Measured in Chrome at eleven real viewports: **at 360, 375 and 390 px the stacked felt resolves
  to a single column**, nothing trimmed, nothing overflowing. **A phone is the safe end of this
  layout**, and a breakpoint below 56rem would have been a control added where nothing is wrong.
- 🔥 **What was broken is the band above it, and it is the exact defect the 56rem line was drawn
  to prevent.** `repeat(auto-fit, minmax(9.5rem, 1fr))` packs to its floor, so a column is ~158 px
  at 412 px and ~154 px from 600 to 896 px — **narrower than at 360** — and there the computer's
  own seat names ellipsed: *"Aung Aung (exp…"*. ⚠️ **Nick saw it on screen mid-session**, which is
  worth recording: the survey that planned this track had called the sub-tablet felt *"probably
  works and never looked at"* and it was the *tablet* band that was wrong.
- 🔥 **Fixed by wrapping the name below the line, not by a wider column floor.** The longest name
  the computer can produce, `Khine Myat Zin (opportunist)`, measures 183 px **in this machine's
  `system-ui`** — ⚠️ **a floor fitted to one platform's font is a fence that passes where it was
  fitted and fails everywhere else.** And ⚠️ **an ellipsis is honest only where the whole name is
  a hover away**: true above the line (`title=`, and a person may type twenty-four characters),
  false below it, which is where there is no pointer.
- ✅ **§3.11 gained item A18 and `ViewportTests` is its fence — four facts, each proved able to
  fail by mutating the stylesheet rather than the test.** The two files are held to naming the
  same width (⚠️ **`@media` cannot read `var()`**, so agreeing is the only way they can be one
  decision — P54's idiom), and **360 px is arithmetic rather than taste**: the column floor plus
  the felt's padding plus the page's is read out of the stylesheets and must fit.
- ⚠️ **What P58 is not: a device test.** No phone, no tablet, no touch, no Mobile Safari. **P60
  still owns all of it**, and the track's own trap stands — **an iframe is not a person any more
  than `curl` is.** 🔥 **And P58 makes the tablet worth more, not less**: the band it found the
  defect in is exactly the band a tablet sits in, and the fix is a *font* decision that has only
  ever been seen in this workstation's font.

---

🔥 **`P58`–`P60` were planned 2026-08-30 at Nick's direction: the table on a small screen.**
⚠️ **Front-end and ops — no rule changes, `Domain` untouched by all three, no
suite owed, no measurement can move**; `P59` was expected to be the only one that touches server code — ⚠️ **and in the event it touched none**, because the window it measures is observed from outside the seat.
**`BUILD-PLAN.md` §5.**

🔥 **The finding that shaped the track, from a survey rather than a build: the app is responsive by
construction and completely unverified below tablet width.** ✅ The viewport meta is right; ✅
**touch targets already exceed the standard on purpose** (§3.11 B11 took Board Game Arena's
*recommendation* over WCAG's minimum — a pressable card is 3.4rem × 4.4rem, buttons 44px), so the
commonest mobile defect is designed out; ✅ the hand wraps; ✅ the felt stacks on **a reading
threshold rather than a fitting one** (*"at around 780 px the outer columns squeeze a seat panel
until the name truncates"*). 🔥 **But there is exactly one breakpoint in the whole application and
it is at 56rem (896 px)** — a 390 px phone and an 895 px tablet get an identical layout, and below
the line the felt has never been looked at. 🔥 **§3.11 has seventeen UX standards and not one is
about screen size**, and ⚠️ **the breakpoint is unfenced** — no test mentions `56rem`, while
`MarkupStandardsTests` and `PaletteContrastTests` prove this project fences presentation
mechanically when it decides to. ⚠️ **So the track is a verification pass, not a design pass, and
the distinction is the point: the design reasoning is careful and load-bearing, and a redesign is
the likeliest way to destroy it.** What is missing is a standard, a fence and evidence — never
taste.

- ☑ **`P58` — a viewport standard and a fence for the one breakpoint there is. Done 2026-08-30 — see above.**
  or device testing finds *"it is cramped"* and needs this packet anyway, later and with the effort
  wasted. ⚠️ **A CSS custom property cannot carry a breakpoint** (`@media` cannot read `var()`), so
  the recommendation is **a test asserting the two files name the same width** — P54's idiom
  exactly.
- ☑ **`P59` — the connection that drops and comes back. Done 2026-08-30 — see the block at the top.** 🔥 **The gap it closed was precise**: P53 measured
  an idle circuit for 7½ minutes **with keepalives flowing**, and **nobody has ever measured a
  client that stops answering and then returns** — which is the question a screen lock, an app
  switch and a wifi→cellular handover all ask. ⚠️ **P54's patience/retention pairing is fenced only
  against itself and never against a real disconnect.** **The one packet here likely to find a real
  bug, and the only one that can leave a repeatable regression test.**
- **`P60` — real devices, and it closes `P53`.** 🔥 **The tablet is the right first device rather
  than a consolation prize**: 896 px is the only breakpoint, **a tablet straddles it**, so one
  device rotated exercises both sides of the only layout decision in the application — and ✅
  **`adb` is installed here**, so the session can drive the real browser over CDP. ⚠️ **Mobile
  Safari's basicauth behaviour on the `/_blazor` upgrade stays an iPhone question** and no
  emulation may answer it.
- ⚠️ **One mobile-only failure mode found while planning and not ruled out:
  `poker.nickjones.dev` has an `A` record and no `AAAA`** (`209.128.193.153`). Carriers that are
  IPv6-only reach it **only** via DNS64/NAT64. **No desktop test can ever see this**, and the fix,
  if it bites, is one DNS record.
- 🔥 **The trap the whole track must not fall into.** Every instrument in it is a way of **not being
  a person**, and this project has the scar — `--no-restore` shipped an image that passed every
  check ever written and was dead (*"curl is not a person"*, P52). **Emulation going green must not
  close P53.**

---

🔥 **`P55` shipped 2026-08-30 on Opus 5: the canonical repository is Gitea, GitHub is a mirror
Gitea pushes, and a command this project had been handing cold sessions since P0 was found broken.**
`RULES.md` stays rev 31, the tree is green at **929**, and ⚠️ **not one line of code changed** —
`Domain`, `Presentation`, `Server`, `Console`, `Sim` and `Web` are byte-identical, **so no
measurement can move and no suite is owed.**

- 🔥 **(1) The remotes swapped names, and a remote's name turned out to be documentation.**
  `origin` is **`gitea.nickjones.dev/nickjones/burmesepoker`**; `github` is
  `github.com/nickjones33/BurmesePoker`. ⚠️ **`git push gitea main` was quoted as *the CI trigger*
  in seven places** across `CLAUDE.md`, `BUILD-PLAN.md` and `STATUS.md` — every one of them a
  command the rename makes fail — and all seven now say **`git push origin main`**. **Renaming a
  remote invalidates prose the way a rules change invalidates a measurement**, and nothing in the
  tree could have caught it: `DocumentationTests` resolves a `bash`-fenced command against the
  parser that would accept it, and a *ref* has no parser.
- 🔥 **(2) `pre-rewrite` was settled by correcting the claim, not by minting the tag — Nick's call,
  against the plan's own recommendation — and the correction was nine sites, not one sentence.**
  The plan costed this as `CLAUDE.md`'s *"survives only at the git tag `pre-rewrite`"*. It is
  `CLAUDE.md`, `RULES-TECHNICAL.md`'s HISTORICAL banner, three sites in `BUILD-PLAN.md` (including
  **P0's own acceptance line**, *"`git tag pre-rewrite` exists"*, which was never met) and three in
  `STATUS.md`. 🔥 **The one that had teeth is `STATUS.md`'s runnable command** —
  `git show pre-rewrite:BurmesePoker/Logic/Factories/UserPromptFactory.cs`, written for P8 as the
  way to read the old Spectre prompts — **failing since P0 and never noticed, because P8 read the
  file before it was deleted.** Every site now names **`79d86bd`** (*"Pre-rewrite snapshot: 2023
  implementation plus rewrite docs"*, the parent of `b32d08b`). ⚠️ **P0's session-log row and P0's
  acceptance were annotated rather than back-dated** — the record stands as written with the
  correction beside it, which is the rule for a newest-first narrative.
- ✅ **(3) Refs first, and it cost nothing precisely because it was checked.** GitHub's one stale
  branch, `p49-simulations-doc`, was at **exactly `a12c7a3`** — the same object as GitHub `main`,
  zero commits ahead — and is gone from the remote and the local tree. No tags on either remote,
  before or after. ⚠️ **The check is the deliverable**: this is the operation where a remote-only
  ref vanishes silently, and *"there was nothing to lose"* is a measurement here rather than an
  assumption.
- 🔥 **(4) The mirror's real cost is stated rather than discovered later: a push mirror
  force-pushes, permanently.** Today's first sync was a fast-forward — `a12c7a3` is a strict
  ancestor of `b2c84c2`, verified before the mirror was enabled — but from here **a commit made
  straight on GitHub is erased by the next sync rather than conflicting.** That is why
  `CLAUDE.md`'s new rule is **never push to `github` by hand**, and why it is a rule and not a
  preference. Sync-on-push is enabled, with the `8h` interval left as a backstop; the PAT is
  fine-grained, `contents: write`, that repository only, and ⚠️ **never entered a session** — the
  third credential in this track and the third to live only in a settings page. ✅ **Acceptance is a measurement rather than a settings-page claim**: P55's own commit `8d619bc` was pushed to `origin` and reached GitHub **in about ten seconds, with no second push** — ⚠️ **which is exactly what a manual *Synchronize Now* does not establish**, and the half the acceptance actually asks for.
- ✅ **(5) Nick's answer on pull requests is `tea`**, installed from Arch `extra` at **0.15.1**.
  ⚠️ **`gh` was never installed on this machine at all**, so the standing *"use the `gh` CLI"*
  guidance was unrunnable as well as aimed at the wrong forge — **and aiming it at the mirror is
  the dangerous half**, since a merge landing on GitHub would be overwritten by the next sync.
  `CLAUDE.md`'s *Rules of engagement* now carries both rules and says out loud that **this project
  has never used a pull request**: fifty-seven packets, one commit each, straight onto `main`.
- ⚠️ **(6) What is still owed, and it is not P55's.** `P53` remains ◐ on a real round **from a
  phone** — carrier NAT and mobile Safari untested — and **P57 has not been seen on the deployed
  site**, though `git push origin main` ran during this packet so the image is built from
  `b2c84c2`. 🔥 **P53's finding stands: read the deployed commit before testing a front end
  against the live site.**

---

🔥 **`P57` shipped 2026-08-30 on Opus 5: the lobby stopped offering an opponent it cannot build.**
`RULES.md` stays rev 31, the tree is green at **929**, and ⚠️ **`Domain`, `Presentation`, `Server`,
`Console` and `Sim` are byte-identical** — the whole diff is `BurmesePoker.Web/OpponentMenu.cs`, two
test files and documents, **so no measurement can move and no suite is owed.**

- 🔥 **(1) The fix is one predicate, and Nick's option 2.**
  `OpponentMenu.CanBeAskedForItsSecondBestMove(rung)` is `rung.Create(0) is IRanksDiscards`, and
  `Advanced` filters on it beside the published-row rule. ⚠️ **Asked of the agent rather than
  declared on the rung**: the `FallibleAgent` constructor that threw asks that exact question of
  that exact object, so anything shorter would be a second opinion free to drift away from the one
  that matters. ⚠️ **Option 1 — unwrapping `DifficultyLevel.Create` at ε = 0 — was not taken**; it
  would have put `Domain` and every published measurement in the blast radius for a defect living
  in the menu.
- 🔥 **(2) The menu now excludes on two grounds and they are different in kind.** *No published row
  → not offerable* is about honesty: a price that cannot be stated must not be charged, which is
  what keeps the money-ranked `prospector` and `purist` out. *Cannot name a second-best move → not
  offerable* is **P19's invariant**: a level is a rung wrapped in a mistake rate, so a rung with no
  second choice can never be one. ⚠️ **`random`'s row was deleted from `Published` rather than left
  to be filtered** — a row that can never be reached is dead data — but **the rule is what keeps it
  out**, not the deletion.
- 🔥 **(3) The fence was amended in the same breath, and it is stronger rather than weaker.**
  ⚠️ **Amending a fence to make a build go green is the move this project distrusts most**, so
  `PublishedFigureTests.EveryOpponentTheLobbyOffersShowsTheMarginTheCsvMeasured` does not merely
  qualify its converse: it asserts the exclusion **about `random` by its inability rather than by
  its name** — the rung *has* a published row against the reference, *cannot* be asked for a
  second-best move, is *not* in `Advanced`, and `Offers("random@0")` is false — and every rung that
  **is** offered is asserted able to answer.
- 🔥 **(4) The new test is the packet, and the distinction it draws is why nothing caught this.**
  `OpeningATableTests.EveryOpponentTheLobbyOffersCanActuallyBeBuilt` takes every name the form can
  post — **the advanced rungs and the four levels** — and **resolves it and then constructs it**:
  `FindOrProbe` *then* `Create`. **Resolution is exactly the step that succeeded** while
  construction threw, so a test that stopped at resolution is the test that was already here. It
  closes with the converse: `random@0` is not offered **and** `Create` on it still throws
  `ArgumentException`, so putting `random` back in the menu is a red build. ⚠️ **Both lists, not
  just the advanced one** — every level is `BotCatalog.Hardest` through the same constructor, so a
  rung promoted to `Hardest` without `IRanksDiscards` would break all four levels at once.
- ⚠️ **(5) Outstanding, and it is the acceptance's own last line: the deployed site has not been
  re-tested.** `git push origin main` is the CI trigger and the role's `pull: true` takes the image
  on the next play; then pick each seat in a browser. **It joins P53's phone round and P54's two
  browser checks** — ⚠️ **and P53's finding stands: read the deployed commit before testing a front
  end against the live site.**

---

🔥 **`P53`'s deploy ran 2026-08-30 on Opus 5: the table is up at `poker.nickjones.dev`, and both
assumptions the packet existed to settle are now measurements rather than assumptions.**
`RULES.md` stays rev 31, the tree was green at **928**, and ⚠️ **not one file in this repository
changed except these documents** — the work was a play in `ansible-nas` plus verification against
the running site.

- ✅ **(1) The play ran because the two blockers were half-imagined.** The passphrase-protected key
  was **already loaded in Nick's `ssh-agent`** (`ansible nas -m ping` → pong on the first try), so
  only `--ask-become-pass` ever needed a person. **Nick ran the playbook; the session did
  everything else.** ⚠️ **Recorded for the next ops packet: check `ssh-add -l` against
  `ssh-keygen -lf` before declaring an SSH blocker** — P53's own note claimed `Permission denied
  (publickey,password)`, and that had stopped being true.
- 🔥 **(2) Traefik proxies the WebSocket, and it is proved at the frame level.** A raw TLS
  handshake against `https://poker.nickjones.dev/_blazor?id=<token>` returns **`101 Switching
  Protocols`** from **Kestrel** through the proxy with the basicauth middleware attached; the
  negotiate offers `WebSockets`, `ServerSentEvents`, `LongPolling` in that order. ⚠️ **The
  handshake set its own `Authorization` header**, so what is proved is that *nothing in the path
  objects to an authenticated upgrade* — **not** that a browser replays cached credentials on one.
  That is the single question the phone round still owns.
- 🔥 **(3) Nothing closes an idle circuit — measured over 7½ minutes.** The same socket completed
  the SignalR handshake (⚠️ **the hub speaks `blazorpack`, not `json`** — a `json` handshake is
  refused by the *app* with `"The protocol 'json' is not supported."` and reads exactly like a
  proxy fault) and then sat still: **server pings every ~15.5 s, 29 of them, no close frame, no
  reset.** So the idle-timeout worry is answered, and answered on the harsher case — a connection
  carrying nothing but keepalives.
- ✅ **(4) `UseForwardedHeaders` is proved through the real proxy, not a synthetic header.** The
  container logs `Request finished HTTP/1.1 GET https://poker.nickjones.dev/…` for requests that
  arrived over the bridge at `172.17.0.13:8080`. ⚠️ **`Request starting` still shows `http://`** —
  hosting diagnostics logs it before the middleware runs — so **read the *finished* line**, or a
  working forwarder looks broken.
- ✅ **(5) Every decision the role took is confirmed on the running container**: memory
  **536870912** (512 MiB, not the `64m` both hand-written roles use), `restart=unless-stopped`,
  **`ports=map[]`** — no published host port, which is what keeps P51's empty `KnownProxies` safe —
  non-root uid **1654**, and all **eight** Traefik labels including the auth pair, the bcrypt
  `$2b$10$…` intact with no `$$` escaping. Resident at **42 MiB of 512**.
- ✅ **(6) The gating branch is right rather than merely present.** Unauthenticated,
  `https://poker.nickjones.dev/healthz` answers **401** with `www-authenticate: Basic
  realm="traefik"` — **a locked door, not the 503 an empty users list would have produced** — and
  with credentials `/healthz`, `/_framework/blazor.web.js` and `/` are all **200** over HTTPS on
  the wildcard `CN=nickjones.dev` Let's Encrypt certificate.
- ✅ **(7) A real browser played a real round, and the credential question is answered: yes.**
  Chrome, through the proxy, past the basicauth dialog: `_blazor/negotiate` **200** (the browser
  *does* replay its cached credentials), and the server log shows `GET /_blazor?id=…` with a
  `Request starting` and **no `Request finished`** — an open connection, which is a WebSocket and
  not the repeated start/finish pairs long polling would leave. Sat down, the claim was **refused**
  and the blind draw arrived privately over the circuit, a card was thrown, and four bot seats took
  their turns by server push. ⚠️ **Not off the home wifi and not a real phone** — carrier NAT and
  mobile Safari are still untested — but the transport question this packet was written around is
  settled.
- 🔥 **(8) The finding that cost the most, found by pressing the form: the deployed image is not
  built from `main`.** `:latest` was built **2026-08-28 21:50** from `860fb13` (**P52**), and
  `gitea/main` is **six commits behind** `main` — so the running site predates **P54** (idle
  reaping, the 180 s patience, copy-link) and **P56** (the opponent menu, the per-seat picker).
  The lobby form still draws P19's *mixed table* checkbox, which is how it was noticed.
  ⚠️ **`git push origin main` is the CI trigger and it has not been run**, so a packet marked done
  here is not thereby a packet that is running. **Correcting what this session wrote an hour
  earlier: one browser sitting does *not* close P53, P54 and P56** — P54's and P56's acceptances
  need a deploy first. 🔥 **Read the deployed commit before testing a front end against the live
  site.**
- 🔥 **(9) A live 500, found by pressing P56's per-seat form in a browser: `random@0` is offered
  and cannot be built.** Choosing *let me choose each seat* and picking **`random`** for a seat
  posts, and `HostedTable.Fill` throws — the site shows *Something went wrong*, the log shows
  `System.ArgumentException: RandomBotAgent cannot say which card it would throw instead…`
  🔥 **It is a contradiction between two packets' rules, not a typo.** `DifficultyLevel.Create`
  **always** wraps its rung in a `FallibleAgent`, whose constructor demands `IRanksDiscards`;
  `RandomBotAgent` does not implement it, because it cannot name a second-best card (P19's rule).
  But P56's menu rule says **a rung with a published head-to-head row against the reference must be
  offered**, and `random` has published rows — so the menu offers it, `FindOrProbe` resolves it,
  and construction throws. ⚠️ **ε is 0 here**: the wrapper would never substitute anything, and is
  refused anyway. ⚠️ **Nothing in the tree could see it** — no test renders the component (P56's
  own note), and P56's `curl` proof used `easy`, `sprinter` and `warden`, never `random`. **This is
  what *`curl` is not a person* was warning about.** ⚠️ **Only the per-seat picker reaches it**;
  `--difficulty` uses `Find`, which never mints a probe. 🔥 **`P57` is the next packet and the fix is decided — Nick chose
  option 2 on 2026-08-30: the menu stops offering a rung that cannot be asked for its second-best
  move.** ⚠️ **Not option 1** (unwrapping `DifficultyLevel.Create` at ε = 0), which would put
  `Domain` and every published measurement in the blast radius for a defect that lives in the menu.
- ⚠️ **(9) The basicauth password is in the 2026-08-30 session transcript.** It was never written
  down anywhere on disk in plaintext — only the bcrypt hash, in the gitignored inventory — and it
  was recovered from a `Basic …` header Nick read off his own browser. **Rotate it with
  `htpasswd -nbB poker <new password>` and re-run the play if that matters**; nothing in the repo
  needs to change.

---

🔥 **`P56` shipped 2026-08-29 on Opus 5: the lobby offers the ladder, every rung on it says what
it costs, and a person can choose their opponents seat by seat.** `RULES.md` stays rev 31 (no rule
changed), the tree is green at **928**, and ⚠️ **`Domain` changed in doc comments only** —
`Presentation`, `Server`, `Console` and `Sim` are byte-identical, **no rung was written, no
measurement can move and no suite is owed.**

- 🔥 **(1) §3.12 was amended rather than contradicted, which is the whole shape of this packet.**
  Nick's answer on 2026-08-29 was option (b) — the rungs, behind an *advanced* control, each with
  its measured margin — and `DifficultyLadder`'s own remarks said in as many words that *"a menu
  with both in it would be the mistake this design exists to avoid"*. **That sentence, §3.12 and
  P19's remark were all rewritten**, to: **levels are the menu; rungs are an advanced disclosure
  that states its price.** ⚠️ **What the old rule was protecting against is unchanged** — selling
  a measured-worse opponent as a matter of taste — and **the margin is what pays that bill**, so
  it is not decoration and not optional.
- 🔥 **(2) `BurmesePoker.Web/OpponentMenu.cs` is the offering, and `PublishedFigureTests` fences
  it both ways.** Ten rungs (`random` … `sprinter`), each drawn as *`sprinter` — +1.2 ± 0.8 points
  of win rate against `outs` — measurably stronger*, transcribed from `ladder.head-to-head.*` and
  compared back against `measurements.csv` **with the sign turned round where the row is named the
  other way**, verdict word included. 🔥 **The half that bites is the reverse direction**: a rung
  *with* a published head-to-head row against the reference **must** be offered, so a suite that
  measures a new rung against a menu that never heard of it is a red build — and a rung *without*
  one **must not** be, which is what keeps the money-ranked `prospector` and `purist` out by rule
  rather than by hand.
- ✅ **(3) Nothing below the form was needed, exactly as the plan predicted.** `HostedTable`'s seat
  resolution went `Find` → `FindOrProbe` (in a new private `Seatable`, which swallows the
  `ArgumentException` a malformed probe throws — a plan comes off a form). ⚠️ **`Lobby`'s
  `--difficulty` stays `Find`**, so a typo there still opens the house table on a level.
  ⚠️ **The seat name and the journal attribution were deliberately split**: a seat reads
  `Mya Lay (sprinter)` through `OpponentMenu.Called`, and the journal keeps `sprinter@0`, because
  a person should not be shown the machinery and a replay must not lose the mistake rate.
- 🔥 **(4) The per-seat picker is a two-step post, and it cost one class.** `NewTable` came out of
  `Tables.razor` into its own file — **nothing here renders a component in a test**, so a form
  whose clamping and fill lived in markup would be the one part of the lobby nothing could assert.
  `SeatFill` replaces P19's checkbox with three answers (*same*, *mixed*, *each*); *each* renders
  the seats on the page that comes back. ⚠️ **`NeedsSeatChoices` is a count check rather than a
  flag**, so changing the shape on the second step asks the seats again; there is one button and
  the second press opens.
- 🔥 **(5) Found by pressing it, which is this project's oldest lobby lesson.** A post carrying no
  `Wanted.PerSeat[…]` fields sets the property to **null**, not to the initialiser's empty list —
  a **500 on the first post of the first step**, and nothing in the tree could see it. `PerSeat`
  is defended in its accessor now. ✅ **Proved end to end without a browser**: the site was
  started and posted to twice with `curl` (shape, then seats), and the table it opened seats
  `(easy)`, `(sprinter)` and `(warden)` with the lobby row reading *The computer plays easy,
  sprinter, warden*.
- ⚠️ **(6) The quorum is said twice, because it is the one thing on the form a person cannot
  guess.** A note before the button (*the first card is dealt when every seat you keep for a
  person has somebody in it*), and each lobby row now reads *Waiting for two more people to sit
  down; it deals when they are all here* — beside P54's copy-link, which is the row somebody is
  about to send. ✅ **The house table's `people` was already `1`** (`ansible-nas` `f4fc41fe`), and
  the form's `Seats` default moved `MinimumPlayers` → **`DefaultPlayers`** — P32's confusion, one
  layer up.
- ⚠️ **(7) Still no browser.** P54's two outstanding browser acceptances are untouched, and this
  packet adds a third: **the advanced group and the two-step form were exercised with `curl`, not
  by a person**. Do all three in the sitting that runs P53's phone round.
- 🔥 **(8) Three existing fences fired, and one of them is the amendment arriving from the other
  side.** `MarkupStandardsTests.TheLobbyOffersEveryLevelAndKeepsNoListOfItsOwn` asserted **that no
  rung is offered in the lobby at all** — §3.12 written down as a test — so the packet could not
  quietly contradict the decision even if it had wanted to. It now asserts what the old rule was
  protecting: both lists **generated**, the levels heading the menu, and **no level and no rung
  named in the markup**. `StandingAnswerTests.NoFrontEndWritesOutWhatALevelIsCalled…` wanted
  `DifficultyLadder.ByStrength` in the razor and gets `OpponentMenu.Levels` plus
  `Assert.Same(DifficultyLadder.ByStrength, OpponentMenu.Levels)` — the chain rather than the
  name. And `LobbyTests`' seating scan followed `SeatingPolicy.Resolve` out of the markup into
  `NewTable.cs`.

---

🔥 **`P54` shipped 2026-08-29 on Opus 5: something closes the tables, and the number that keeps a
phone in its seat is joined to the number the framework hides a dropped connection behind.**
`RULES.md` stays rev 31 (no rule changed), the tree is green at **920**, and the whole diff is
`BurmesePoker.Web` plus three test files — ⚠️ **`Domain`, `Presentation`, `Server`, `Console` and
`Sim` are byte-identical, so no measurement can move and no suite is owed.**

- 🔥 **(1) Nothing in this client had ever closed a table.** `Lobby.Close` was written in P13.6
  and **only the tests called it**, so a hosted site accumulated a table per form press for ever,
  reached `Lobby.MostTables` (12), and from then on answered every *Open it* with an error —
  weeks after the deploy, and reading as **a broken form rather than a full site**
  (`HOSTING.md` §8's *table leak*, confirmed rather than assumed). ✅ **The bot loop was already
  fine**: `HostedTable.Deal` stops the moment `Ready` goes false, so what leaked was the slot and
  the memory behind it, never a running round.
- 🔥 **(2) A table is idle from the moment it opens, and the house table is never reaped.**
  `HostedTable.IdleSince` starts at construction — a table opened by a press nobody followed up
  has never had a viewer to lose and is exactly the leak — and is cleared by `Arrive`, restarted
  by the **last** `Leave`. ⚠️ **`Lobby.House` is a named field rather than *the first table in the
  dictionary***: once tables can be closed those stop being the same thing, and reaping the house
  table would leave `dotnet run` an empty room and the deployed site's own URL pointing at
  nothing. Window **30 minutes**, swept every **5** by `TableSweeper` (a `BackgroundService`,
  registered — ⚠️ **fenced, because a correct reaper nobody runs leaks exactly as much as none**).
- 🔥 **(3) The patience number stopped being a taste and became a relation.** A browser table's
  patience went **90 s → 180 s**, and `Program.cs` now sets
  `CircuitOptions.DisconnectedCircuitRetentionPeriod` explicitly to **2 minutes**. Inside that
  window the framework is *deliberately hiding a dropped connection from the player* — so a
  patience shorter than it has the computer play the turn of somebody the framework is still
  expecting back, and they return to a table that moved on for no reason they could see. ⚠️ **The
  two numbers live in two files and are fenced against each other**, one read from source and one
  off the real `Lobby`, so neither is a copy of the other. **A phone in a lift is the case.**
  ✅ **The retention is the *shorter* side of a trade on purpose** (the framework's default is 3
  minutes): a seat is recoverable by **name** whatever happens to the circuit (P13.6, §3.11 C16),
  so retention buys convenience while every minute of it holds a page of server state for
  somebody who may never come back.
- ⚠️ **(4) The copy-link affordance is an enhancement over a real link, not a control.** Each
  lobby row now writes the table's **absolute** address out as an `<a>` (`ToAbsoluteUri`, so
  behind Traefik it is the forwarded host — P51's `UseForwardedHeaders` doing the one job nothing
  else can do for it), with a copy button hidden until a script says otherwise. 🔥 **The reveal is
  a class on the *document* and the handler is delegated from it**, because enhanced navigation
  replaces this markup without reloading — per-element work done at load leaves dead buttons the
  first time somebody comes back to the lobby. Fenced, because it is invisible in a screenshot and
  only shows up on the second visit.
- ✅ **(5) The §7 gating decision was already landed by P53** (a Traefik `basicauth` middleware,
  attached only when the users list is non-empty), so P54 inherited it rather than taking it.
- ⚠️ **(6) One finding recorded and deliberately not acted on.** `SeatChannel` holds a
  `ManualResetEventSlim` per seat and never disposes it. It is **not** an unbounded leak — the
  underlying wait handle is released by its own `SafeHandle` finaliser once the channel is
  unreachable — and disposing it properly is a race against the engine thread parked in `Ask`,
  which takes no cancellation token and waits the whole patience. **`Server` was left
  byte-identical**; a packet that wants this must give `Ask` a token first.
- ⚠️ **(7) Not verified in a browser.** These are unit and source fences plus a build; **nothing
  here was watched reaping a table on a live site**, and the copy button was not pressed in a real
  browser. The acceptance's *"the lobby affordance works in a real browser"* is **outstanding** and
  is one of the things P53's phone round can settle in the same sitting.

---

◐ **`P53` is half-shipped, 2026-08-28 on Opus 5: the `burmesepoker` role exists in `ansible-nas`
and is proved as far as a session can prove it — but the table is not up, because the deploy needs
two secrets only Nick can type.** `RULES.md` stays rev 31, the tree is green at **914** (this
packet touched no code in this repository at all), and **`P53` stays ◐ until the play is run and a
round is played from a phone off the home network.**

- ✅ **(1) The role is built, wired and linted.** `ansible-nas` commit `7ffd645e`:
  `roles/burmesepoker/{defaults,tasks}/main.yml` on the `mirroquest` two-file shape (fail fast on
  missing credentials → `docker_login` → `docker_container` with `pull: true` and the Traefik
  labels → the stop block), the include in `nas.yml` between `booksonic` and `calibre`, a page at
  `website/docs/applications/gaming/burmesepoker.md`, and the gitignored inventory enabled.
  `yamllint` clean, `ansible-lint` clean at that repo's own `production` profile,
  `ansible-playbook nas.yml --syntax-check` passes. ✅ **P52's prediction held exactly**: the
  `read:package` token already in the inventory as `mirroquest_registry_password` is what
  `burmesepoker_registry_password` wants, and no credential was minted.
- ⚠️ **(2) What is not done, and why it is not a judgment call.** `ansible-playbook nas.yml --tags
  burmesepoker --become --ask-become-pass` needs `ssh-add ~/ubuntuServer22key` (the key is
  passphrase-protected — `ansible all -m ping` comes back `Permission denied (publickey,password)`)
  **and** an interactive become password. **A session can supply neither.** So the two assumptions
  this packet exists to settle — that Traefik proxies the WebSocket and that nothing in the path
  closes an idle one — are **still assumptions**.
- 🔥 **(3) The gating decision was taken: Task 5 option B, a Traefik `basicauth` middleware**, and
  the shape is the finding. There is no auth pattern anywhere in that repo. The two labels are
  **`combine`d in only when `burmesepoker_basicauth_users` is non-empty**, because a router naming a
  middleware with an empty users list serves **503** — a broken deployment rather than a locked
  door. Both branches were proved with a throwaway play before the commit (empty → six labels, set
  → eight), and the bcrypt `$` signs survive intact: `docker_container` needs none of compose's
  `$$` escaping. ⚠️ **The credentials are `poker` / a generated passphrase, in the gitignored
  inventory only**; regenerate with `htpasswd -nbB poker <new password>`.
- ⚠️ **(4) It gives the phone round a second thing to settle.** A browser cannot set an
  `Authorization` header on a `WebSocket`, so the `/_blazor` handshake depends on the browser
  replaying its cached basic credentials for the origin. If it does not, Blazor falls back to long
  polling and the table **works but feels slow** — so the acceptance is *look at the network tab*,
  not merely *a round played*.
- ⚠️ **(5) Two corrections to that repo's plan, made while building.** **(a) There is no LAN check
  at `http://192.168.50.142:8080`** — this role publishes no host ports, exactly as `mirroquest`
  does, because Traefik runs `network_mode: host` and reaches containers over the bridge; and
  publishing 8080 is the one thing P51 forbids (`KnownProxies` is empty, so the app trusts whatever
  sets the forwarded headers). **Verify through Traefik, or `docker exec` against the bridge
  address.** **(b) The plan said there is no `games` category on the website** — there is a
  **`gaming`** one, with three servers already in it.

---

🔥 **`P52` shipped 2026-08-28 on Opus 5: the image is built by CI and pulled from the registry,
never built on the server.** `RULES.md` stays rev 31, the tree is green at **914**, and **`P53`
(the Ansible role) is next — and it is work in `~/source/repos/ansible-nas`, not in this repo.**

- ✅ **(1) The repo has a Gitea origin** (`gitea` remote, `https://gitea.nickjones.dev/nickjones/burmesepoker.git`)
  and `.gitea/workflows/publish-image.yml` builds the repository's own `Dockerfile` on the
  `docker-builder` runner, pushing `:latest` and `:<sha>`. ⚠️ **The GitHub push-mirror is not set
  up** — it needs a *GitHub* PAT, which is a different credential, and nothing depends on it.
- 🔥 **(2) CI gates on the request that separates a working image from a dead one.** The final step
  runs the pushed tag and curls `/healthz` **and `_framework/blazor.web.js`**, because P51's
  `--no-restore` image passes every other check there is. `ContainerTests` fences the workflow
  itself — the runner label (⚠️ **a wrong label queues rather than fails**, which reads as a slow
  build), the absence of the flag, the image path P53 pulls, and the blazor check.
- ✅ **(3) Acceptance was met on the published artifact, not the local build.** Pulled `:latest`,
  ran it, four URLs 200 including a proxied one, and dealt a browser round — **the same hand as
  P51's local build at the same seed**, a free reproduction check on the image.
- 🔥 **(4) The fence caught its own documentation, which is a finding about the scans.** The first
  version of `ThePublishingWorkflowBuildsThisRepositorysOwnImage` went red on the workflow's `#`
  comment *explaining* the `--no-restore` trap — the exact failure `Sources.Markup` exists to
  prevent, and whose own remarks warn that such a scan "would fail on the very files that are most
  careful". ⚠️ **It strips `//`, `/* */`, `@* *@` and `<!-- -->`; YAML's `#` is a fourth comment
  syntax it had never met**, so this test strips whole-line `#` itself. Re-proved able to fail by
  putting a real `--no-restore` on the `docker build` line (red, reverted).
- ⚠️ **(5) Credentials, for the next session.** The registry token is a **Gitea repo secret named
  `REGISTRY_TOKEN`** (`write:package`, `read:package`); it never entered the session, and the local
  pull used a `docker login` Nick ran himself. 🔥 **P53 needs no new credential**: the
  `read:package` token already in `inventories/my-ansible-nas/group_vars/nas.yml` as
  `mirroquest_registry_password` is exactly what `burmesepoker_registry_password` wants. ⚠️ Noted
  in passing, unrelated to this packet: `nickjones_dev_repo_url` in that inventory carries a token
  inline in the URL in plaintext — gitignored, so not in history, but worth rotating.

---

🔥 **`P51` shipped 2026-08-28 on Opus 5: the browser table is a container, and the standard
.NET Dockerfile idiom turned out to ship a dead one.** `RULES.md` stays rev 31 (no rule changed),
the tree is green at **913**, and **`P52` (a published image) is next — blocked on a Gitea PAT**.
⚠️ **Ops, not the programme**: `Domain`, `Presentation`, `Server`, `Console` and `Sim` are
byte-identical, no measurement can move, and no suite is owed. The whole diff is `Dockerfile`,
`.dockerignore`, `BurmesePoker.Web/Program.cs`, one new test class and the docs.

- 🔥 **(1) The finding, and it was measured rather than reasoned.** Copy the csprojs → `dotnet
  restore` → copy the sources → `dotnet publish --no-restore` is what nearly every .NET Dockerfile
  does. Here it publishes an app **with no `wwwroot/_framework/blazor.web.js` at all** — the
  published endpoint manifest names it **zero** times against **one** when the publish restores for
  itself, reproduced four ways in the SDK image to isolate which step did it. The symptom is
  **P13.3's, exactly**: `MapStaticAssets` 404s the script that starts the circuit, so the table
  draws once, perfectly, and never moves again. ⚠️ **Nothing in the tree can see this** — the app is
  correct and the image is not — so the flag is fenced
  (`ContainerTests.ThePublishThatMakesTheImageRestoresWithTheSourcesInFrontOfIt`) rather than left
  to whoever next tidies a Dockerfile for speed. It was caught by curling the URL the page names,
  which is P13.3's own lesson applied a second time.
- 🔥 **(2) A real round was played in the container, not a page loaded.** Sat down through the
  lobby's `EditForm` — the antiforgery post, which is the failure this packet exists to prevent —
  claimed off the table, discarded, watched four bot seats take their turns, and drew blind with the
  private event arriving over the circuit. ⚠️ **The first attempt at `/` before the fix returned
  200 with a 404 script**, which is precisely why *the container answers* is not the acceptance.
- 🔥 **(3) `UseForwardedHeaders` is first, and it is proved live.** With `X-Forwarded-Proto: https`
  and `X-Forwarded-Host: poker.nickjones.dev` the container logs the request as
  `https://poker.nickjones.dev/`. `KnownIPNetworks`/`KnownProxies` are cleared because a proxy
  container's address is assigned at run time — ⚠️ **so the app trusts whatever forwards the
  headers, and port 8080 must never be published straight to the internet.** The fence checks the
  *ordering* against `UseAntiforgery`, `MapStaticAssets` and `MapRazorComponents`: *present* would
  have passed on a call placed after the endpoints, which does nothing.
- ✅ **(4) Verified green at 913** (was 908 — five new facts, all source scans in
  `JackpotSpokenTests`' idiom). Image **231 MB**, non-root, `0.0.0.0:8080`, `/healthz` answering
  `ok` and reading no `Lobby`. The framework tags in the Dockerfile are fenced against the csproj's
  moniker, so a `net11.0` bump reddens the build rather than publishing against the wrong SDK.

```text
docker build -t burmesepoker .
docker run --rm -p 8080:8080 burmesepoker --people 1 --seed 20260828 --pace 300
```

---

🔥 **`P50` shipped 2026-08-27 on Opus 4.8: the documentation cleanup — `STRATEGY.md`'s prose
caught up with its own tables (F10 discharged), and the class was fenced rather than left to
proofreading.** `RULES.md` stays rev 31 (no rule changed), the tree is green at **908**, and
**every planned packet is now done** — only `P40` (the Burmese rulebook, blocked on Nick's vetted
text) stands open. ⚠️ **Pure-documentation packet**: the diff is `docs/STRATEGY.md`, one new
`[Fact]` in `PublishedFigureTests`, and the count/state bumps in `CLAUDE.md` + this file — every
project byte-identical, no measurement moved.

- 🔥 **(1) Most of the known list was already current, which is the finding.** The 2026-08-23 F10
  list named seven stale spots; P43–P46's blast radius had since regenerated §3/§4/§6/§12, so five
  of the seven were already fixed. What was genuinely stale: **the top block** (`rev 24/26/29`,
  `§7.4/§7.5/§3-seating unbuilt`, `235 measurements`, `Last generated … P46`) → **rev 31, all built
  (§7.4 measured in §15, §7.5 audited not measured, seating/visibility can't reach a one-round
  figure), 372 measurements, P46-suite-plus-P48-readouts**; **§7's resolution floor** (four-handed
  `SE 0.52 / half-width 1.02 / ~34,000 games`) → **`0.41 / 0.81 / ~21,000`**; **§8's map and the
  `cautious`/`counting` bullets** (four-handed `± 1.0`) → **`± 0.8`** current margins; **§3's
  greedy/cautious/counting paragraph** (`+0.1 ± 1.0`, `30.7/30.6/30.1`) → **`+0.0 ± 0.8`,
  `22.4/22.2/22.2`**; **§10's "The answer"** (`+$5.32 / +$14.63 / −$0.21`) → **`+$3.99 / +$14.20 /
  −$0.35`** matching its own table (also fixed the table's `3.98 → 3.99` rounding); **§15's
  deal-win summary** (`28 in 75,250`) → **`52 in 116,201`**. All re-derived from the CSV, never
  patched by eye.
- 🔥 **(2) The fence choice was extend, not strip — and the reason is what `STRATEGY.md` is.**
  `PublishedFigureTests.TheProseFiguresTheStrategyDocumentQuotesAreTheFiguresInTheCsv` anchors seven
  current-claim prose margins (§3 sprinter-over-outs, §8 map's outs/cautious/counting/refuse, §10's
  two separated money cells) plus §7's *derived* floor (half-width = the head-to-head interval, SE =
  it over 1.96) to their CSV rows, in the exact shape already proven for `HOW-TO-PLAY-WELL.md` — the
  printed sign carried by the scale, tolerance from the printed precision. ⚠️ **Digit-free (P49's
  rule for the sims doc) was rejected on purpose**: `STRATEGY.md` is the measurement authority whose
  whole voice is the inline interpretation of a figure, so stripping its digits would gut it; the
  fence keeps the voice and makes the *current-claim* margins a red build (never the deliberately
  kept historical ones — P34's newest-first rule).
- ⚠️ **(3) Deliberately left alone.** Clearly-historical/dated narratives that name their packet
  and keep superseded figures on purpose (§9's P23 four-handed re-fit, §10/§14's "at P33 it went
  X→Y", the P16 neighbour figures) — P34's rule. ⚠️ **One possible P48 typo not touched** (out of
  F10 scope, and §16 is the hardened newest section): §16.3 line ~1642 quotes the *normal* money
  half-widths as `±2.28`/`±4.85` where the CSV rows read `±2.43`/`±4.81`; the *resampled* figures it
  quotes (`±2.41`, `+$3.99`, `+$14.20`) are correct. Worth a glance if §16 is ever revisited.
- ✅ **(4) Verified green at 908** (was 907 — one new `[Fact]`). The new prose fence proved able to
  fail (a margin mutated in `STRATEGY.md` reddened it, reverted); the existing table fence, map
  fence, command fence and count/rev fence all green; count bumped 907 → 908 in `CLAUDE.md` and
  this file.

---

🔥 **Before P50: `P49` shipped 2026-08-27 on Opus 4.8: `docs/SIMULATIONS.md` — the measurement
programme taught to a curious person, digit-free and fenced.** `RULES.md` stays rev 31 (no rule
changed), the tree was green at **907**, and **`P50` (the documentation cleanup — the prose catches
up with its own tables, F10) was the next and last planned packet** — `P40` stands beside it,
blocked on Nick's vetted Burmese text. ⚠️ **This is a pure-documentation packet**: no production
code touched, the whole diff is one new document, its fence in `PublishedFigureTests`, and pointers
in `README.md` + `CLAUDE.md`'s map — so every project is byte-identical and no measurement can move.

- 🔥 **(1) The document teaches the instrument, not the answers.** In reading order: what a
  seeded, parallel, replayable **run** is; a **seed** (a pointer, reproduces only against the same
  build) against a **journal** (the artifact, replays after the code moves on — §3.9 in plain
  words); why the **game is the trial** and counting the seat halves every interval; why a **win
  rate is the totals divided**, not the average of per-game rates; what **pairing** buys and why
  it *widens* within a cell (exactly one seat declares → opposed series → wider by √2) and
  *narrows* across cells (shared shoes → common random numbers); why a round-robin needs **Holm**
  and what *survives* vs *raw only* buys; what the **null cell** proves; and **a tour of the
  experiment shapes** — head-to-head, the crossed free-for-all, the neighbour experiment with its
  control arm, the money sweep, the dial's calibration — each with the question it answers and the
  one it deliberately cannot.
- 🔥 **(2) Digit-free by rule, P39's one-home applied in advance.** The document carries no figure
  of its own and points at `STRATEGY.md`/`HOW-TO-PLAY-WELL.md` for every number, so it cannot go
  stale the way F10's prose did. **The fence is an absence**:
  `PublishedFigureTests.TheSimulationsGuideTeachesTheInstrumentAndCarriesNoFigureOfItsOwn` asserts
  no `±` and no percentage figure (`\d\s*%`), and that both pointers still exist — **proved able
  to fail** by mutating in a margin (caught on `±`) and again a bare percentage (caught on `0%`).
  The commands it prints resolve through the existing `DocumentationTests` command fence (no new
  flags — all reused verbatim from CLAUDE.md), and it lands in the map with no banner.
- ✅ **(3) Verified green at 907** (was 906 — one new `[Fact]`). `dotnet build` clean; the map
  fence, the command fence and the historical-banner fence all green with the new file; the
  count fence's 906→907 bump made in `CLAUDE.md` and this file.

---

🔥 **Before P49: `P48` shipped 2026-08-27 on Opus 4.8: the verification-and-hardening run — the
statistical review's findings F1–F7 discharged, and `sprinter`'s edge graduated from *measured*
to *settled* at a fresh seed.** ⚠️ **This was not a rung packet**: no agent changed, `BotCatalog`
is untouched, and the whole diff was `BurmesePoker.Sim` + tests + two docs — so `Domain`,
`Presentation`, `Console` and `Web` are byte-identical (the front ends cannot see it). ⚠️ **It
cost the whole session in compute**: the suite is **~5 h wall on this 24-core box** and the
fresh-seed replication is another **~4 h** (both re-run the expensive `outs`-family ladder), so
budget a full day for a run like this and never trust a pasted "~20 min".

**What P48 built, and the five things a cold session needs from it.**

- 🔥 **(1) Composition-stratified margins (F1) — and `warden`'s loss is partly self-play
  compounding.** `TournamentCell.MarginAtComposition(rowSeats)` reads the head-to-head margin
  *within* one seating mix (the row's win-rate `Trials` **is** its seat count at
  `RoundsPerGame = 1`); new rows `ladder.composition.{pair}.{1,4}-of-5`. ⚠️ **The games partition by
  composition but the margin does not** — the pooled figure is a difference of ratios-of-sums over
  different seat-round denominators, so a stratum can sit outside the pool (verified on the
  degenerate `random`-`outs` cell); **read the two compositions against each other, not the pool.**
  Finding: `outs` beats a lone `warden` +6.3 but a table of four `warden`s +10.6, and `greedy`/
  `simple` point the same way — `warden` does worse the more `warden`s crowd it (round length
  compounds, §16.1). ✅ **P43's `opportunist` null holds at every composition** (both extremes inside
  the interval) — the exact instrument P43 asked for, and it finds no free-for-all-vs-head-to-head
  gap. `STRATEGY.md` §16.1.
- 🔥 **(2) Money margins for every ladder pair (F2) — the money ladder agrees with the win-rate
  ladder everywhere.** `ladder.money-margin.{pair}`, own Holm family of forty-five, from the
  per-game net series each cell already kept. **0 of 45 disagree in direction** with their win-rate
  twin (38 separated, 5 inside, 2 raw only). `outs` beats `warden` by +7.3 pts but +$2.71/round;
  **`sprinter` banks +$0.45 ± 0.32/round over `outs`, separated** — a second currency confirming
  the edge. `STRATEGY.md` §16.2.
- 🔥 **(3) The fresh-seed replication (F5) settled `sprinter`.** `sim replicate` runs §3's matrix +
  the dial's steps at a second master seed (20260826) beside the published one →
  `docs/strategy/replication.csv` (48 comparisons; the *efficient* path, Nick's call, reads seed A
  from `measurements.csv` and computes only seed B — `--recompute-a` runs both). **The written
  prediction: every Holm verdict holds ✅ (0 of 48 fell); every margin inside its interval ❌ (3 of
  48 outside, all keeping their verdict — and ~8 were *expected* outside by the interval geometry,
  so the estimates are more stable than the prediction, which was the thing slightly wrong).** 🔥
  **`sprinter` over `outs`: +1.23 (Holm) → +1.19 (Holm)** — the one verdict most likely to fall did
  not. The two raw-only casualties split: `opportunist`-`angler` fell inside (null confirmed),
  `sprinter`-`angler` firmed to Holm. `STRATEGY.md` §16.5.
- 🔥 **(4) Bootstrap (F6) + field-rate intervals (F7).** `Bootstrap.PairedMargin` (percentile,
  deterministic, resamples whole games) checks the two separated money cells — **the normal
  intervals hold at money's heavy tails** (`money.net-per-round-bootstrap.*`). A per-game
  `FieldSeries` gives the field rates `§12`/`§13` compare across fields a real interval by the house
  ratio method — **the ladder's lock-bite (30.0% ± 0.2) is now *separated* from the dial's (21.9% ±
  0.6)**. `STRATEGY.md` §16.3/§16.4.
- ✅ **(5) Reproduction is the strongest the project has recorded, and it is by design.** **214 of
  235 shared `measurements.csv` rows byte-identical, and not one *mean* moved** — the 21 movers are
  exactly the field-rate rows that gained an interval (F7); every win rate, margin, money figure,
  ranking, verdict and mechanism rate is unchanged. Journal replay byte-identical; `Domain` diff
  empty. ⚠️ **A dial-ordering bug cost the first replication run** (`ByStrength` vs the suite's
  weakest-first `StrategyCatalog.Levels` → `hard-over-expert` ≠ published `expert-over-hard`); fixed,
  and `AgainstPublished` now **pre-flights the ids before the multi-hour run** so a config error
  fails in seconds. `SeatingPlan.MaximumAssignments` unchanged (no new rung).

---

🔥 **Before P48: `P46` shipped 2026-08-25 on Opus 4.8: `sprinter` — the endgame played as a race —
and it is the first rung to separate above `outs` since `outs` was built.** `RULES.md` stays rev 31 (no rule
changed), the tree is green at **897**, and **`P48` (the verification-and-hardening run) is the
next code packet** — P47's ledger is already landed, and P49/P50 are the writing-down; `P40`
stands beside them, blocked on Nick's vetted Burmese text. ✅ **The P46 race-reach perf follow-up is
done** (a small second commit the same day): the recorder now holds an `Endgame.Reader` carrying a
per-seat `OutsCache`, so the winning-draw search is no longer bought fresh every crossed-table
discard — **2.95× on the instrument's hot path** (501 vs 1477 µs/call on a near-win-heavy workload),
**counts byte-identical** (asserted cached == static), so `measurements.csv` is unchanged and no
re-run was needed. It confirmed the instrument was ~1% of the suite — never the bottleneck.

**What P46 built, and the four things a cold session needs from it.**

- 🔥 **(1) The rung is `outs` with one change of objective, and the change is the discard's
  last-resort key.** `Domain/Agents/SprinterBotAgent.cs` is `outs` until the hand is within one
  card of covering; there it stops maximising live outs and maximises **winning draws** — the
  copies-weighted values that would let thirteen of the fourteen it leaves meld
  (`LiveOuts.WinningDraws`, bar = the hand's own size). The key is lexicographic, *winning draws
  then outs*, packed into one `long`, so **off the endgame every candidate scores zero winning
  draws and the rung is `outs` card for card** (P45's price-don't-tune idiom applied to a
  trigger). Take, claim, object and declare are `outs`' exactly. Catalog last, `Strength: 3`,
  win-rate ranked, `Hardest` stays `outs` (no dial/ε/front-end change). **Bench 8.1× `greedy` /
  1.21× `outs`** — inside P21's 10× budget, no turn inflation (all-`sprinter` runs `outs`' 24.3).
- 🔥 **(2) The answer is a positive that separates, and the mechanism armed to prove it.**
  **`+1.2 ± 0.8` against `outs`, `p = 2.9e-03`, surviving Holm over the family of forty-five** —
  confirmed on a second command (bare `tournament outs,sprinter`) and reproducing exactly. It
  beats `opportunist` (+1.6, Holm) and `angler` (+1.1, raw only), and tops **both** ranking
  columns (mean margin +8.1, crossed 25.5). ⚠️ **Small and fragile**: at the crossed table 25.5 vs
  `outs`' 25.3 are inside each other's intervals (a five-point edge over one opponent diluted by
  eight others), so it is P48's fresh-seed replication that graduates it from *measured* to
  *settled*. 🔥 **After three nulls whose mechanism stayed flat** (`opportunist` lock-bite,
  `purist` clean share, `angler` take rate), **`sprinter`'s moved**: race-reach 26.6 ± 0.2% vs
  `outs`' 25.6 ± 0.2% (~4 SE), the new `ladder.race-reach.*` rows — it steers into more near-wins
  and the win rate follows. **A moved mechanism and a moved margin together are why the +1.2 is
  believed.** `STRATEGY.md` §3/§8 (its story is in §3, like `outs`', not §8's failures).
- ✅ **(3) Reproduction held and the crossing cap was paid measured.** 9 old rungs' head-to-head
  cells came back **byte-identical**; the 38 rows that moved are all field-dependent by
  construction (every `free-for-all`, `rank`, `take-rate`, crossed-table mechanism row, and the
  null cell, which changed hands to `sprinter` a fourth time and holds at −0.4). No Holm verdict
  fell. **`SeatingPlan.MaximumAssignments` 65,536 → 131,072** — a stated decision (raise and pay,
  over dropping a rung or subsampling) for the `10⁵ = 100,000` crossing; fence updated. Two
  raw-only casualties at the top (`angler`-`sprinter` p=0.0079, `opportunist`-`angler` p=0.013),
  flagged for P48. One game of 100,000 abandoned (`random`'s field; precedent P29).
- ⚠️ **(4) The suite ran in ~5 h on this 24-core workstation** (vs the laptop's 5–6 h for the
  smaller field) — the "~6¼ h" the older docs quote is a laptop wall clock, not portable. **The
  race-reach instrument is cheap** (measured ~54 µs/call, ~2 ms only on the ~2% of hands at
  covered ≥ 10) and was **not** the reason a first run was slow — the 10-rung suite is just large;
  a mid-session over-diagnosis killed that run before this one replaced it. See the perf follow-up
  above.

---

🔥 **Before P46: `P45` shipped 2026-08-25 on Fable 5: `angler` — a draw priced in cards — and the
answer was the predicted null with P44's shape: the mechanism never armed.** `RULES.md` stayed rev
31, the tree was green at **886**.

**What P45 built, and the four things a cold session needs from it.**

- 🔥 **(1) The rung is the price `warden`'s autopsy called for, stated as one inequality.**
  `Domain/Agents/AnglerBotAgent.cs` is `outs` with the take changed at both places it arises
  (take and claim — `prospector`'s one-change-two-sites rule): acquire a known card iff
  `gain·unseen + outsAfter > 2·outsNow` — the certain cover gain plus the kept hand's draw
  equity must beat the forfeited blind draw's, all in integers over public facts.
  `LiveOuts.CardCount` (copies-weighted outs, sharing `Count`'s loop, probes and cache) is the
  numerator; `MoneyOdds`' unseen pool the denominator; **the one stated model is a one-draw
  horizon**. The new move is the **enrichment take** — a card that melds nothing, taken when it
  more than doubles the hand's out-cards. Catalog last, `Strength: 3`, win-rate ranked,
  `Hardest` stays `outs`; no dial, ε or front-end change. **Bench: 9.3× `greedy`, 1.21×
  `outs`** — inside P21's 10× budget after two stated cuts (the lookahead skips the outs
  refinement's probes; an enrichment take requires the offered card itself meldable with the
  hand, which also removes a take-a-junk-card-to-shed-a-blocked-duplicate artifact).
- 🔥 **(2) The answer is the predicted null, and the flat mechanism is the finding** (P44's
  shape, one packet later): `+0.6 ± 0.8` against `outs` — inside the interval — while beating
  the `greedy` trio `+2.6`–`+2.9` and `warden` `+6.9`, all separated over the new family of
  **36**. **The take rate never moved: 24.66 ± 0.09% against `outs`' 24.71 ± 0.09%** at the
  crossed table (`ladder.take-rate.*` — new mechanism rows, one per rung, added so a null is
  attributable). The prediction called the null and the never-firing refusal direction but
  expected the enrichment take on 1–5% of acquisitions; **it was wrong about that** — a hand
  poor enough for one card to double its outs is nearly absent from real play, so under a
  one-draw horizon `outs`' improvement-only take already collects everything a card-priced
  model sees. Three zero/stated-price rungs, three nulls: `outs` stands at the free-lunch
  frontier. ⚠️ **One loose end for P48**: `angler` over `opportunist` is `+1.0 ± 0.8`, raw
  `p = 0.013` against a Holm threshold of 0.0083 — the family's first raw-only casualty,
  flagged in §3/§8, not claimed.
- ✅ **(3) The crossing cap was paid measured, and the reproduction repeated P43's exactly.**
  `SeatingPlan.MaximumAssignments` 32,768 → **65,536** (its fence re-proved with the new
  bounds); the free-for-all is one full pass of 59,049, nothing subsampled; **148 of 172
  shared CSV rows byte-identical outside the command column, and all 24 movers are
  field-dependent by construction** (free-for-all, rank, crossed-table mechanism rows). No
  Holm verdict moved under 28 → 36. The null cell changed hands to `angler` (`Ladder[^1]`,
  third time, deliberate) and holds at −0.5. The `LiveOuts` refactor was proved byte-identical
  separately (300 seeded games, `outs,purist` field, HEAD worktree vs changed tree — journal
  and CSV identical). **Suite ~22,500 s now (~6¼ h) — ⚠️ estimated from CPU accounting; the
  laptop slept mid-run, so the 95,870 s wall clock is not the cost.** One game of 59,049
  abandoned (the field contains `random`; precedent P29).
- ⚠️ **(4) A striking pairing artifact, published**: the new across-cells row *`warden`: vs
  `opportunist` less vs `angler`* reads **0.37** — the strongest narrowing the file has —
  because `warden`'s two comparators are nearly the same player, so their margins from shared
  shoes are almost pure common noise. §6 says it is a statement about the players, not the
  method. Blast-radius F10 corrections made in §12/§13 (refusal 51.0 → 51.6%, lock-live/bit
  quads to the current CSV) and the guide's accidental-clean rate 12.1 → 12.2; **the rest of
  F10 stays P50's.**

---

🔥 **Before P45, the day before: `P44` — `purist`, the clean bonus at zero price — the
predicted positive came back a null whose mechanism was flat, which is the finding.**
`RULES.md` stayed rev 31, the tree was green at **874**.

**What P44 built, and the four things a cold session needs from it.**

- 🔥 **(1) The rung is one lexicographic preference, and the exchange rate is stated rather
  than tuned.** `Domain/Agents/PuristBotAgent.cs` is `outs` with a *fewest-jokers-kept* key
  between `outs`' two ranking keys: it sheds a joker whenever that costs **no melded card**,
  paying any number of live outs and never a meld (a numeric rate would need a win-probability
  estimate nothing supplies; a knob would be a family of rungs, P15). Catalog entry last,
  `Strength: 3`, **`Ranked = RankedOn.Money` for the second reason there is** — it reads no
  stakes, but trades rounds for a multiplied prize, so win rate would misjudge it by
  construction. `Hardest` stays `outs`; no dial, ε, or front-end change; **the ladder stays at
  eight win-rate rungs, so the free-for-all crossing did not move**.
- 🔥 **(2) The answer is a null that falsified both halves of its own written prediction**
  (predicted `+$0.5–1.5` a round and clean share 20–35%): at $5/$1 the sweep reads
  `−$0.23 ± 0.32`, and `prospector`'s $5/$1 cell — same seed, same seatings, played by a rung
  that *is* `outs` there — reads `−$0.228 ± 0.32`, so **`purist`'s real effect is −$0.005 a
  round, −0.01 points: about one round in eight thousand. Its clean-win share is 12.81 ± 1.04%
  against the control's 12.83%** — the mechanism never fired. Why (visible in hindsight): when
  the joker-throw is the *unique* winning discard `outs` already throws it, so **the accidental
  12% floor already contains every clean win that costs nothing**. `STRATEGY.md` §2/§8/§10/§14;
  a paying clean-bonus rung is now the second standing anti-recommendation (BUILD-PLAN P47).
- ✅ **(3) The instrument generalised without touching a value**: one money sweep per
  money-ranked rung (`SuiteOptions.MoneyChallengers`, every one, read off the catalog — a
  single challenger defaulting to `StakesSensitive[^1]` would have silently dropped
  `prospector` the day `purist` landed), the challenger is part of every `money.*` id (P32's
  precedent; the twelve renamed rows are **byte-identical on values**), per-cell
  `money.clean-win-rate.*` / `money.jokerless-rate.*` mechanism rows exist for both
  challengers, and a bare `sim money` sweeps each rung in turn. **All 131 unrenamed shared rows
  came back byte-identical; no old verdict moved; the new sweep is its own Holm family of
  four.** The suite is **~18,600 s now (5¼ h, was ~15,200 s)** — the second sweep adds four
  `outs`-priced cells.
- ⚠️ **(4) Two corrections recorded on the way.** The plan's constraint that *"the joker cannot
  be shed before the declaring discard"* overstates §9 #33's built default — the engine
  restricts a joker's exit **only while jokers are locked** (§5.1 with #27's joker-closes-jokers
  ruling), so `purist` legally sheds unlocked jokers early and the null is measured under the
  rules as built; the re-measure-if-#33-flips caveat stands in §14 and the P44 outcome. And two
  more F10-stale prose figures in the blast radius were corrected (`prospector`'s $5/$40 margin
  in §8's map and prose, `+14.6 ± 4.5`/`+7.34 ± 3.29` → the CSV's `+14.2 ± 4.8`); **the rest of
  F10 stays P50's**. One aside published in §14: `prospector`'s own clean-win share collapses
  to ~5.6% in the cells where its rule fires — a blind-draw-heavy hand declares holding more
  jokers, the first observed interaction between the draw decision and the clean bonus.

---

🔥 **Before P44, the day before: `P43` — `opportunist`, the feeding ban at zero price — the
null the packet predicted, closing the question `warden` left open.** `RULES.md` stayed rev 31,
the tree was green at **862**.

**What P43 built, and the four things a cold session needs from it.**

- 🔥 **(1) The rung is the missing 2×2 corner, and the shared code is the design.**
  `Domain/Agents/OpportunistBotAgent.cs` is `outs`' take exactly — a card is taken because it
  improves the hand, never for the lock — plus `warden`'s hold: a rank an ordinary take has
  closed against the seat above stays closed, with §5.1's own two escapes (the declaring
  discard, the floor) and no more. **The hold moved into `Domain/Agents/HeldLocks.cs`** and
  `warden` delegates to it, so the two rungs' restraint cannot drift apart (P28's
  one-predicate rule applied to a strategy). No memory, no `WorthLocking` — `outs` neither
  buys nor holds, `warden` buys and holds, `opportunist` holds without buying. Catalog entry
  after `warden` (it is one decision from each neighbour), `Strength: 3`, win-rate ranked;
  **`Hardest` is still `outs`** (ties keep ladder order), so no dial, ε or front end moved.
- 🔥 **(2) The answer: `+0.1 ± 0.8` against `outs` — inside the interval — while beating
  `warden` by `+6.2 ± 0.8` and the `greedy` trio by `+2.0`–`+2.2`, all separated over the new
  family of twenty-eight.** The prediction (null to small positive) was written before the run.
  **What the null decides**: `warden`'s whole loss lives in the *paid take*, and holding a lock
  nets to nothing this apparatus can see — denial did not fail for want of a better price; at
  the best possible price, zero, it still buys nothing. `STRATEGY.md` §3, §8 (a sixth kind of
  answer: a null that closes a question) and §13 carry it. ⚠️ **One tension for P48**: the
  free-for-all column has `opportunist` two points behind `outs` while the head-to-head is a
  dead heat — either the column being a statement about the field again (§4's standing
  warning) or a cost only a mixed table charges; **composition-stratified margins could split
  those**, and §4/§8 say so.
- ✅ **(3) The reproduction repeated P31's exactly**: every head-to-head cell, pairing row,
  dial row, money row and headline shared with the P42-era file came back **byte-identical**
  (only the `command` column moved — the field string now names `opportunist`), all
  twenty-one old Holm verdicts survived the tightening to twenty-eight, and the null cell
  **changed hands to `opportunist`** (`Ladder[^1]` again — second time, §6 updated) and holds
  at −0.6 with 20% inside every interval. **The suite is ~15,200 s now (~4¼ h, was 12,445 s)**:
  the eighth ladder rung adds seven `outs`-priced cells and doubles the free-for-all crossing
  to **8⁵ = 32,768 — exactly `SeatingPlan.MaximumAssignments`**. ⚠️ **The ninth win-rate rung
  will not fit under the cap** — P45/P46 must decide what gives (noted in their packets and in
  §4); money-ranked `P44` is safe.
- ⚠️ **(4) Fences that moved with it**: the ladder is written out by hand in **two** test
  files, deliberately rename-proof — `BotCatalogTests` *and*
  `SkillLadderRunTests.EveryRungTheCommandLineOffersCanActuallyBeSeated` — and both gained
  `opportunist` (the second found by the full run, not by grep: budget for it next rung).
  `OpportunistBotAgentTests` holds both halves (the zero-price
  take asserted *against* `warden`'s paid one, the hold, both escapes, the counterfactual
  instrument keeping the rung's own restraint, and outs-card-for-card when nothing is locked),
  and the discovered test count moved 850 → 862, which `PublishedFigureTests` caught in
  STATUS.md exactly as designed. While editing §3/§4/§8, four of F10's stale prose figures in
  the blast radius were corrected to the current CSV (`warden` −9.3 → −7.3 etc.); **the rest
  of F10 stays P50's** — `prospector`'s `+14.6 ± 4.5` in §8's map is still a P31-era figure,
  left for the sweep.

---

🔥 **Before P43, the same day: `P42` — playtest readiness — the console deals five, the ×5
jackpot is said out loud at both front ends, and the session itself played the browser table
through four settlements with Claude in Chrome.** `RULES.md` stayed rev 31, the tree was green
at **850**.

**What P42 built, and the four things a cold session needs from it.**

- 🔥 **(1) The jackpot fact is carried by the domain, once: `RoundResult.JackpotOwner`.**
  A `PlayerId?` filled in `RoundEngine.Settle` from the same
  `MoneyCardRegistry.ConfigurationOf(ownership, shoe)` the settlement reads — **required, not
  defaulted**, for `Win`'s reason: a defaulted null would settle a jackpot round as ordinary in
  silence. ⚠️ **A watcher cannot compute it** (ownership is partly private until settlement),
  which is why it rides `TableEvent.Settled`'s `RoundResult` to the browser rather than being
  folded client-side. The *possibility* is public from the deal, so that half **is** a fold:
  `MoneyCardRegistry.IsTheJackpotPair(turnedUp)` is public static, and
  `TableBoard.JackpotPairUp` folds it **at `RoundStarted`, deliberately not off the live
  `TurnedUp` list** — a claimed top card leaves that list while the designation it made stands.
  Console: a settlement line in the jokerless/deal/streak idiom plus a round-start narration
  line; web: a sentence in `SettlementPanel` from the result alone plus a quiet table-centre
  note. **`CardDisplayState` stays ×5-free and §9 #32 is not generalised** — both its fences
  pass untouched.
- 🔥 **(2) Three fences, each proved able to fail.** `RoundEngineTests.
  AJackpotRoundCarriesItsOwnerOnTheResult` constructs the round (7♦/A♠ turn-up, one seat given
  both partners **and all four jokers**, so the hand-computed payouts have no filler joker in
  them) and asserts the carried fact and the ×5 in the money; its twin asserts the split pair
  carries null. `JackpotSpokenTests` holds both front ends to *reading* the fact by source scan
  (the console is outside the test project's reference graph on purpose; the razor is markup) —
  **deliberately not a wording fence**: what must not regress is the read.
- ✅ **(3) Byte-identity asserted across the Domain change, exactly P41's procedure**: a seeded
  300-game `Sim` run (`--seed 20260823`) came back **byte-identical** on journal and CSV either
  side, and the HEAD journal replays to a byte-identical CSV under the new tree. The console's
  seat prompt defaults `RoundEngine.DefaultPlayers` (five) with the floor untouched; both
  `drive-console.py` scripts re-captured clean at five seats. ⚠️ **The driver's default script
  is `human`** — "both scripts" means `--script bots` *and* the default, not default plus
  `--script human`, which are the same run.
- 🔥 **(4) The browser round was actually played, and it found a UX observation rather than a
  defect.** Sitting down as a person at the house table (`--people 1 --seed 20260823 --pace
  300`), the session played through **four settlements** by clicking: both takes, throws, a
  claim **refused twice and granted once** (§4.5's disclosure working — the refuser held the
  rank both times), §5.1 closing a rank so the card stopped being a control, P41's ▲ from both
  sides of the glass including **§9 #50 live** (held the concealed duplicate of a face-up 3♦,
  threw it, the ▲ stayed), the why? disclosure, the legend, the log, and the timeout stand-in.
  ⚠️ **The one thing not seen on screen: the settlement panel** — at `--pace 300` the house
  table deals the next round within about a second of settling, so the panel's window is one
  pace beat; its correctness is fenced by `BrowserRoundTests` instead. **A human playtester may
  find the same thing** — worth watching for, not fixed in-packet (the packet's own rule).

---

🔥 **Before P42, the same day: `P41` — the table shows what the rules make public, and §10 is
empty again.** `RULES.md` stayed rev 31 (no rule changed), the tree was green at **845**.

**What P41 built, and the four things a cold session needs from it.**

- 🔥 **(1) The whole packet is one fold, written once: `Presentation/TableLook.cs`.** Every
  seat's discard pile (§5) and every seat's face-up cards (§5.2) are a pure fold over the
  public events — the console's `ConsoleObserver`, the server's `TableFanOut` and the
  browser's `TableBoard` each hold one and feed it the events they were already narrating, and
  **the browser's pile fold moved *into* it**, so pile logic that had lived in `TableBoard`
  since P13.3 has one home too. The blind draw has **no method on the type** — concealment as
  an absence — and `ConcealmentTests.ABlindDrawnCardIsNeverShownFaceUp` holds the fan-out to
  it from outside, **proved able to fail by mutating `PlayerDrew`**.
- 🔥 **(2) Byte-identity was asserted, not argued** (acceptance 4): a seeded 300-game `Sim` run
  (`--seed 20260823`, journal + CSV) came back **byte-identical** either side of the change,
  and the HEAD journal replays under the new tree. No Domain or Sim file moved at all —
  `git diff` on those directories is empty — so no measurement could move and none did.
- ⚠️ **(3) §9 #49 and #50 are built on their recommended defaults and still open**, each fenced
  by a test named for the row (`TableLookTests.TheClaimedTurnUpLiesFaceUp…` and
  `…TheFaceUpCopyStaysWhenTheConcealedDuplicateIsThrown…`, both proved able to fail by
  mutation). **The face-up mark is by `CardId` throughout** — the concealed duplicate of a
  face-up value stays concealed, which is the one fact the event log alone could not carry.
  ⚠️ **`TurnContext` was deliberately not widened**: no rung sees the piles or the marks; a
  rung that reads them is a new rung and arrives measured (`STRATEGY.md` §11's rule, §10 #14
  unchanged).
- ⚠️ **(4) What a player actually sees now.** Browser: each seat's face-up cards lie flat on
  its panel (`▲`), its pile opens from the ▾ summary (a real `<details>` — §3.11 A4), and your
  own face-up cards carry the same `▲` in your hand with the sentence in the accessible name.
  Console: a fourth panel — *Everyone can see this* — lists every seat's face-up cards and
  whole pile, and `▲` marks your own hand; `Palette.Legend` grew the token, and
  `drive-console.py` re-captured clean (both scripts settle; the capture differs, which is the
  point — byte-identity lives in the journal and the CSV, not the screen). `CardDisplayState`
  gained `FaceUp = 1 << 7` with a `DisplayTokens` entry, so the legend fences picked it up
  without a new test. The registry's §5.2 entry is **`Checked`** — deliberately with no ⚠,
  because both defaults are presentation-only — and the **exemption ceiling is back at 6**,
  with no whole exemptions at all.

---

🔥 **Before P41, the same day: `RULES.md` went to rev 31 — two visibility rules from Nick.**
`JournalHeader.CurrentRulesRevision` is **31**; the tree was green at **832**. *(The block
below was written before P41 shipped; where it says nothing shows either rule, P41 is the
answer.)*

- **(1) The public discard piles, corroborated**: every card in every player's pile may be
  looked through, not merely the top card — §5's rev-17 `EXPERT` rule, now independently
  `PLAYER` too. ⚠️ **No front end has ever let a player do it.**
- **(2) §5.2, new**: a card taken in the open is laid **face up in front of its taker, visible
  to everybody, for as long as it stays in their hand**. `PLAYER`, Settled. It amends §6.3's
  *"the only public information is the discards"*. ✅ **It changes what the table shows, not
  what the table knows** — open takes and discards are already public events by `CardId`, so
  the face-up set is a pure fold over the event stream: no engine change, no journal change,
  every journal ever written replays identically, and **no measurement should move**. ⚠️ **One
  instance-level exception**: two decks mean the event log alone cannot pin *which copy* a
  player still holds; the face-up card can (§9 #50).
- **Opens §9 #49** (does the claimed turn-up lie face up too? — recommend yes) **and #50**
  (which copy leaves when a duplicate is thrown? — recommend the player chooses; it is what
  discard-by-`CardId` already does). Both phrased for the experts in
  `QUESTIONS-FOR-MYA-LAY.md` Q13.
- **§10 reopens with #24** — the fourth reopening — and the coverage registry carries §5.2 as
  its first whole exemption since P35, naming **P41**; the exemption ceiling moved **6 → 7**
  with the finding written beside it. 🔥 **`P41` — the table shows what the rules make
  public — is written up in `BUILD-PLAN.md` §5 and is the next packet.**
- ✅ **The rulebook moved with the rules in the same session** (the rev-stamp fence's design,
  exercised by hand before the build could force it): stamp **rev 31**, the face-up rule taught
  in *A turn* and the table reference, and appendix rows 13–14 for #49/#50 — the citation fence
  requires exactly that set. ⚠️ **P40's translations must be made against this rulebook** —
  Burmese produced from the rev-30 text would arrive already stale.

---

🔥 **`P39` shipped 2026-08-23 on Fable 5: there is a strategy guide — `docs/HOW-TO-PLAY-WELL.md`
— and the plan is empty again.** One document that answers *how do I get better?* from what this
project has actually measured, organised by decision rather than by experiment: what you are
optimising → the discard tie-break → the one refinement that has ever worked → the money (settle
it, never chase it) → **the things that sound clever and measurably are not** → the three
unpriced bonuses → which difficulty setting to sit with. **The nulls get as much room as the
margins**, because a null is where a player would otherwise spend attention for free — and the
bonuses are stated as *unknown rather than small*, since no measured player knows they exist.
`STRATEGY.md` stays the measurement authority and is untouched.

🔥 **(1) Every figure the guide quotes is CSV-fenced, and the fence now covers *verdicts* as
well as numbers.** `PublishedFigureTests.TheFiguresThePlayersGuideQuotesAreTheFiguresInTheCsv`
was rewritten onto the new document: the dial quad and the headline pair (moved from
`PLAYING.md` with their regexes, exactly as the re-plan required), nine anchored margins —
⚠️ **the printed sign is part of each anchor and the scale carries it**, so a margin that flips
direction fails rather than silently reading as its own opposite — two interval-free rates
(what chasing the money costs in rounds; the accidental jokerless rate), and **the eight
verdicts the prose asserts**: the four nulls must still read `inside the interval` and the four
separations `separated (Holm)`, because a margin can drift back inside its interval without the
number moving much at all.

🔥 **(2) A figure has one home, and it is asserted as an absence.**
`TheFiguresHaveOneHomeAndThePlayingGuidePointsAtIt` requires `PLAYING.md` to point at the guide
and to contain **no `±` anywhere, no reference-table quad, no headline pair** — so a figure
pasted back into `PLAYING.md` is a red build whatever the figure says. Its *Playing better*
section is one pointer paragraph now. ✅ **Both new fences were proved able to fail by mutating
the documents** (a moved headline figure; a `±` reinserted into `PLAYING.md`).

⚠️ **(3) Found on the way, and it is exactly the staleness class P34 named: `PLAYING.md`'s
difficulty prompt row still said *expert* wins about 36% and *easy* about 14%** — the
**four-handed** reference figures, on the five-handed page, unfenced because P34's regex
targeted the *other* dial sentence in the file. The row is digit-free with a pointer now.
**A number the fence cannot see is a number the guide must own or the prose must not say.**

⚠️ **(4) The guide quotes only `measurements.csv` rows and says everything else in words.**
"One round in seven", "four rounds", "twice in five" are word-figures for rule arithmetic and
fenced constants; the P16 seat-side null ("which side of you a weaker player sits is worth
nothing") is stated with **no digits at all**, because P16's figures are not CSV rows and a
number without a fence is the defect this packet exists to end. The rulebook's worked round is
pointed at rather than duplicated — its dollars are engine-replayed, not CSV rows.

✅ **No rules question arose; `RULES.md` stays rev 30 and `JournalHeader.CurrentRulesRevision`
is unmoved.** ⚠️ **The tree is green at 832 tests**, from 831.

---

🔥 **Before that, `P38` shipped 2026-08-23 on Fable 5: there is a rulebook — `docs/RULEBOOK.md` — and it
cannot fall behind the rules without a red build.** One document a stranger can play a correct
round from, in reading order: the game → what you need → setup → a turn → the feeding rule
(taught as table manners, which is what it is) → the opening claim and its permission → melds →
winning by table size → what a win pays → the money cards → a generated worked round → a table
reference → the house readings. **No provenance, no open questions, no packet numbers**, and
`RULES.md` stays the sole authority — the rulebook decides nothing.

🔥 **(1) Four tests in `BurmesePoker.Tests/Docs/RulebookTests.cs` hold it to the tree, each
proved able to fail by mutating the document.** The rev stamp equals
`JournalHeader.CurrentRulesRevision` (already bound to `RULES.md` by `GameJournalTests`, so the
chain reaches the document) — **a play-changing rev is now a red build until somebody re-reads
the rulebook against what changed, which is the maintenance, compelled.**

🔥 **(2) The worked round is generated and *replayed by its test*, not merely stamped with a
seed.** `TheWorkedRoundIsTheRoundItsSeedActuallyPlays` re-runs the printed construction — seed
**15**, five seats of `outs`, seat seed `seed × 100 + seat` — and asserts every fact the prose
teaches from: all five dealt hands verbatim, the turn-up (2♣ and 6♥), all seven owned money
cards with owner and multiplier, the 24 turns, the winner, the four declared melds, and all
fifteen cells of the settlement table plus the round/money/net split (asked of
`Settlement.RoundPayments`, never re-derived). ⚠️ **A rules change that moves these numbers is a
red build and the right fix is to re-derive the section, not patch a cell.** 🔥 **Seed 15 was
picked from a 60-seed scan for its teaching value**: the winner declares **jokerless** — so the
×3 bonus shows up in a real settlement ($60 against $15 a head) — and **had discarded an owned
A♠ mid-round and is still paid for it**, permanent ownership demonstrated rather than asserted.

🔥 **(3) The house-readings appendix is fenced *two ways*, with its citation set derived from
`RULES.md` itself.** The open §9 rows are recognised by table shape — numbered, un-struck, five
columns, which tells them from the closed three-column tables — and the appendix must cite
**exactly** that set: today #33, #36–#41, #44–#48 (twelve rows, eleven recorded defaults plus
the `PLAYER`-ruled #45). **A question closing fails the build until the reading is folded into
the body; a new default fails it until the reader is told.** That is the packet's hard problem —
a rulebook silently promotes defaults to rules — made checkable.

⚠️ **(4) The voice is fenced too**: no provenance tags, no confidence words, no packet ids, and
not the word *reconstruction* anywhere in it — the deliberate exception is the closing pointer
at `RULES.md` §9, which is where a curious reader is sent. `README.md` points at the rulebook as
the way in for a new player, and the documentation map carries it.

✅ **No rules question arose; `RULES.md` stays rev 30 and `JournalHeader.CurrentRulesRevision`
is unmoved.** ⚠️ **The tree is green at 831 tests**, from 827.

---

🔥 **Before that, `P34` shipped 2026-08-23 on Opus 5: there is a front door, and the
documentation set cannot go stale quietly any more.** `README.md` exists; the three wholly historical documents say so above
the fold; and **eight tests in `BurmesePoker.Tests/Docs/` hold the documents to the tree**, each
proved able to fail by mutating the document rather than the code. 🔥 **Every packet on the plan is
now done** — `BUILD-PLAN.md` §5 has nothing left in it, and the next piece of work has to be
**chosen** rather than picked up.

🔥 **(1) The front door and the narrative are two documents for two audiences, and the second must
not be flattened into the first.** `README.md` is the only current-only file here: what the game
is, what the seven projects are, how to run it, where the answers live — **no packet numbers, no
🔥, no history**. Everything else stays a running narrative that keeps superseded reasoning on
purpose, which is the most valuable thing in `docs/` and has shaped three packets.

🔥 **(2) The habit is a test now, which is this project's third time doing that** (a rung cannot be
added without being measured; a Settled rule cannot be recorded without being checked). The joins
asserted: **the map is complete both ways**; **every historical document carries a banner and no
current one does**; **every command in a fenced `bash` block resolves** against the source that
parses it; **every test `RULES.md` names as a fence exists**; **every figure `STRATEGY.md`
tabulates and every figure `PLAYING.md` quotes agrees with `measurements.csv`**; and **the one
measurement the product speaks aloud is still a null**.

🔥 **(3) The test count is discovered rather than trusted.** `[Fact]`s plus theory rows, by
reflection over this assembly — the number a run reports — so **a packet that adds a test and
leaves the prose alone is a red build**. ⚠️ **Only the *first* count and the *first* rev in each
document are checked**: these files are newest-first and the log records the tree at 677, 697, 715
and 795, every one true when it was written. **A check demanding they all agree would be asking
the project to delete its own history.**

⚠️ **(4) Two documents were a whole measurement behind and nothing had noticed.** `PLAYING.md`
told a player the four settings win **13.8 / 21.7 / 28.4 / 36.1%** — the **four-handed** reference
table, from a run two measurements old, on a page describing a **five**-handed table — quoted
`headline.balanced.*` figures that match no row in either CSV, and said *"your neighbours change
every round"*, false since P36. 🔥 **Prose has no column to disagree with**, which is why the fix
is a test rather than a proofread. `RULES-PRIMER.md` was worse: **four `[⚠ code disagrees]` tags
for divergences closed at P25–P28**, a settlement section that stopped at *flat*, and an open
question that was answered a revision later.

⚠️ **(5) `RULES.md` §10 says *empty* and has one standing exception, now said out loud: #7.**
`RoundEngine.MinimumPlayers` is **4** against §2's Settled 2-to-6, so the two- and three-handed win
conditions are implemented, tested and **unreachable from a dealt game**. It is the oldest entry in
that list and **no packet owns it**. *(No rule changed and no play changed; the rev did not move.)*

⚠️ **The tree is green at 827 tests**, from 819.

---

🔥 **`P35` shipped 2026-08-23 on Opus 5: the two scoring rules that reach outside a round are
played, and `RULES.md` §10 is empty — every rule the document records as Settled is implemented.**
`RULES.md` is **rev 30**; `JournalHeader.CurrentRulesRevision` is **30**.

**Six §9 defaults were built on, every one fenced by a test named for the question** (#38, #39, #40
for §7.4; #41, #44, #46 for §7.5) — and **one new question was opened, §9 #48**.

🔥 **(1) §7.4 was the expensive half, and the plan had it the other way round.** §9 #38's recorded
default is *the dealt thirteen alone*, so `RoundEngine.Play` now offers the declaration to every
seat whose **dealt** hand already covers, in turn order, **before the first take**. ⚠️ **A round
can run no turns at all** — `RoundResult.Turns` is 0 — which is **the first change to the shape of
a round since P0**, and it makes `TurnNumber` **0** a real value reaching the journal, the console's
turn heading and the server's `TurnBegan`. A seat may decline and play the round out; declaring is
a choice (§7.1) and nothing has been discarded for §5.1 exception 2 to bind. ⚠️ **It opened §9 #48**
— two seats dealt a winning thirteen at once — defaulted to *the earlier in turn order*.

🔥 **(2) §7.5 was cheap once the division was seen: settlement is *told*, never made to remember.**
`MatchEngine.Streak` counts consecutive wins — **the only state in this project that reaches across
rounds and is not money** — and hands it down to each round as it is dealt. `Settlement` still
holds no history and takes no match, which is asserted over its parameter list.
⚠️ **The reading matters and the first implementation had it backwards**: *"pays your whole
payout"* means the winner collects **exactly what they would have collected**, out of one pocket.
Billing one loser's share would leave a player four fifths worse off for winning three in a row —
**a test caught it, not a reading**.

🔥 **(3) The consumer trap the packet predicted was real, and the fix was to delete the
re-derivation rather than extend it.** `Settlement.RoundPayments` is the round column, computed
once in the domain and read by both the console's settlement panel and `SeatRow.Flat`.
⚠️ **Both had assumed every loser pays the same amount** — true from rev 1 until rev 27 — and a
split at the wrong place posts the difference into the **side-bet** column, where every money
measurement reads it, **with the totals still adding up**.

⚠️ **(4) A test agent was silently declining the new question, and that was luck.**
`ScriptedPlayerAgent` advances its script by turn number and starts at 0, so it answered *no* to
the deal declaration without being asked to — which is why 795 tests stayed green the moment the
path went in. **A great many tests deal a seat a winning thirteen to script what it does on turn
1.** It is now an explicit `DeclaresOnTheDeal`, defaulted to no *as a decision*.
✅ **Exactly two tests really changed behaviour and both for the right reason**: the three-round
bank test's third round is now billed to one seat, and the round-number test sees turn 0 before
turn 1.

🔥 **(5) Conformance got its first multi-round case, and the exemption saying it never could was
half wrong.** §7.5's `Exempt` reasoned that the audit watches one round and a streak is not a
property of one. **It still watches one round — but it can be *told* what the rounds before it
did**, with the count kept by the driver rather than read off `MatchEngine`.
`AStreakOfWinsBreaksNoSettledRuleAndIsBilledToTheSeatAbove` plays 120 rounds at 4 and 5 seats and
**fails if no streak occurred**, so it cannot pass vacuously.
✅ **Both registry entries are `Checked` and there are now no whole exemptions at all** — a first
since P30.2 — and the ceiling came down **7 → 6**.

✅ **(6) Acceptance 4, stated either way (`docs/STRATEGY.md` §11).** **§7.5 is not in the standing
measurement set and cannot be** while `RoundsPerGame = 1` stands (BUILD-PLAN §3.8) — a three-round
streak cannot occur in a one-round game. ⚠️ **What that leaves unknown is what §7.5 is *worth***,
and whether asking to change seats before somebody's third win is a strategy: **nobody has measured
it and no rung knows the rule exists.** §7.4 *is* observable, so `bonus.deal-rate.*` is published
beside `bonus.jokerless-rate.*`.

🔥 **(7) The re-measurement is the strongest reproduction this project has recorded, and it
falsified the prediction written down before the run.** 13,257 s, 126 measurements (three new).
**107 of the 124 rows this suite shares with P32's came back byte-identical**, and the seventeen
that moved all count *turns* or *money*. **Nine rounds in 33,008 ended on the deal** — about one in
3,700, `docs/STRATEGY.md` **§15** — and ✅ **not one win rate, margin, Holm verdict, ranking,
pairing ratio, mistake rate or reference-table figure moved by so much as a millionth.** **§7.4
changed *when* those rounds ended, not *who won them***: a thirteen dealt complete stays complete,
every rung declares at its first opportunity, and in all nine the seat that was going to win won.

✅ **Three columns corroborate each other and nothing had to be argued.** The ladder's turn total
fell **502,830 → 502,812**, which is exactly the 18 turns those seven rounds used to take (2.6
each — about what a seat waits for its own first turn at five seats); **the feeding-ban denominator,
a different column computed a different way, fell by the same 18**; and **two claim attempts
disappeared**, against 7 × the 28.6% attempt rate = 2.0.

❌ **The prediction that was wrong is the useful one.** It said a deal win would move **a win rate
and a money figure together**, and that money moving alone would mean the split had landed in the
wrong column. **Money moved alone and the split is fine.** ✅ **The column that actually
discriminates is the side bet, and all four `money.side-margin.*` rows are byte-identical** —
acceptance 3, in P33's idiom. 🔥 **A falsifier has to name the column that could only move if the
bug were real, not merely one that would move if the rule fired.**

✅ **The console's capture is byte-identical to `HEAD` at `--seed 20260819 --pick 0`** — both new
panel lines and the new turn heading are dead paths in an ordinary round — **which is the proof the
console change was a presentation change and nothing else.**

⚠️ **Two stale product sentences were fixed on the way past**: the browser's declare *"why?"* and
its rules page both said the winner takes **a flat stake**, which stopped being true at P33 and is
now wrong in three ways.
⚠️ **And two stale documents**: `IGameObserver.RoundStarted`'s doc comment still said the seats are
re-randomised every round (a rev-19 reading, two revisions after it was withdrawn), and
`docs/STRATEGY.md` §11 quoted **`+0.06 ± 0.29`** for the claim-permission money null — a
**four-handed** figure left behind by P32, found by re-deriving the section from the CSV rather than
by reading it. **Exactly the class of staleness `P34` build item 4 exists to catch.**

⚠️ **The tree is green at 819 tests**, from 795.

---

🔥 **`P37` shipped 2026-08-22 on Opus 5: the table can agree to change seats, and `RULES.md` §10
#23 is discharged.** §3 step 2 is now built in both halves — **a seating is drawn once and held
(P36) and re-drawn when the players agree to it (P37)** — and this is the **first public question
this project has ever asked**.

**`IPlayerAgent.AskAboutTheSeating` is the sixth question, and it is the first that is not about
cards.** Every seat is asked in the gap before a round; the seats move on **one `Ask` and no
`Refuse`**. It is asked in `MatchEngine.NextSeating`, beside P36's policy — **the agreement first,
and the policy not asked on top of it.**

🔥 **The finding is that consent is not desire, and a yes-or-no question could not have carried
the rule.** The design decision going in was *a computer seat consents* (BUILD-PLAN **§3.13**,
recorded there rather than invented in `RULES.md`, because §3 says *the players* agree and a bot is
not a player in the sense the rule is about). ⚠️ **But a consenting bot answering *yes* re-seats an
all-bot table every deal** — the opposite of the rule. `SeatingOpinion` is therefore **three**
answers, `Consent` the default and a no-op. ✅ **Build item 5 — *fail closed* — then disappeared as
a problem**: silence, an unattended seat and every bot in the game all consent, and consent moves
nothing. **No clock, no timeout, no special case.**

⚠️ **A public question is a standing answer, not a pending prompt.** Blocking would have cost one
patience per seat to settle one question, so a person says what they think whenever they like — the
control sits on the table beside the hand — and it stands on the seat's `SeatChannel` until the
engine asks, which **consumes** it. **One press moves the seats once**, asserted at the browser and
at the server.

🔥 **The trap the packet did not name is the one that would have shipped quietly.** This is the
**first member of `IPlayerAgent` with a default implementation**, so a decorator that does not
override it does not fail to compile and does not throw — it answers *consent* in its own name and
drops what it wraps. For `JournalingAgent` that is a re-seating that never reaches the file; for
`JournalPlayerAgent`, a replay that quietly deals to different seats. **Six decorators needed it**,
and `SeatingAgreementTests.EveryDecoratorForwardsTheSeatingQuestion` finds them **by type** —
anything taking an `IPlayerAgent` in its constructor.

✅ **Acceptance 3 is asserted rather than passed over**:
`ConcealmentTests.TheSeatingConversationIsPublicAndCarriesNoHand` shows the three seating events
carry no card, hand or rationale, and that **a watcher who holds no seat hears every word**.
⚠️ **A superseded connection may not say anything either** (review R8): the public question is the
one thing a dead connection could otherwise have said out loud in somebody else's name.

✅ **Replay was free, and that is why the asking is an agent question rather than a host call.**
`JournalingAgent` records it at turn 0, `JournalPlayerAgent` answers it, and `GameRunner.Replay`
needed no new driving path. ⚠️ **`JournalPlayerAgent` peeks rather than consuming** — every journal
written before P37 has no seating decisions, and absence has to mean `Consent` or no old journal
replays. **A deliberate narrowing of *divergence is loud, always*, safe because consent changes
nothing.**

✅ **Two leftovers were taken with it, both in this packet's own subject.** The console's
round-start line still read *"the seats are re-drawn every round"* — ⚠️ **P36 missed it, and it was
a false sentence in the product for a day** — and `AboutTable` now says what the seats are doing,
which no view had ever said.

⚠️ **The console capture changed and the driver did not**: the new `SelectionPrompt<SeatingOpinion>`
is answered by `drive-console.py`'s generic ENTER arm, which takes *leave them* — the rule.
⚠️ **It is only visible in a two-round capture**; the standing one-round capture never reaches the
question. ✅ **No published measurement can move**: the harness plays one round a game, and the
question is never put before the first.

⚠️ **The tree is green at 795 tests**, from 778. **`RULES.md` stays rev 29** — no rule changed;
§3 step 2 already said this and the code has caught up.

---

🔥 **`P36` shipped 2026-08-22 on Opus 5: a seating is drawn once and held, and `RULES.md` §10 #22
is discharged.** The engine had contradicted the rules document in **both directions** — before
P28 it held a seating that could never change, between P28 and P36 it re-drew one before every
deal — and neither is the rule, which is that a seating **holds until the players agree to change
it** (§3 step 2, rev 28; §9 #45, rev 29).

**`Domain/Play/SeatingPolicy.cs` is *when a re-draw happens*, and it is the whole of it.**
`SeatingPolicy.Held` is the default; `RoundsBetweenSeatings` of *N* re-draws every *N* rounds;
**0 is never**, and there is no flag beside the number. `MatchEngine` takes one, asks it in one
place and exposes it read-only. ⚠️ **The setting is the mechanism and it is not the rule** — a
number chosen when a table opens is not people agreeing — **which is P37, and P36 fenced it with
two tests named for §9 #45 and #47** so a round-counting policy cannot answer them by accident.

🔥 **Acceptance 2 is the piece worth keeping and it caught something.**
`LayeringTests.NothingOutsideTheSeatingPolicyDecidesWhenTheSeatsAreDrawnAgain` is the P18/P19
source scan applied to a third rule: **only `MatchEngine` may ask the question, and only
`SeatingPolicy` may do arithmetic on the number.** A front end may carry a policy and hand it over;
it may not reason about one. ⚠️ **`JournalFormat` was the second copy** — it decided what 0 meant in
order to omit the field, and asks `header.Seating != SeatingPolicy.Default` now.

✅ **The journal writes `seating_rounds` only when the seating was not held**, so **every journal
ever written is byte-identical** and absence means the rule. ⚠️ **The one journal that cannot say
what it did is one written between P28 and P36**: it carries no field, reads back as held and
replays differently — `CurrentRulesRevision` **28** is what makes that detectable rather than
mysterious. `GameRunner.Replay` reads `header.Seating` and not this build's default.

⚠️ **A seed no longer means what it meant, for the second time in this project's life** (§3.9
point 2): a **held seating draws no numbers at all**, where the every-round draw took some the deal
now takes back. 🔥 **It cost a test, and the test was right to notice.** `SeatBoardTests`'s fixture
played **three** rounds, and `EverySeatIsAskedEveryQuestionOverAMatch` went red because the fifth
question — the claim's permission — needs a claim *and* the seat above holding the rank, and it
stopped turning up. It turns up again at **five**. ✅ **Nothing was tuned to pass**: the fixture is
a match, three was never the number, and the assertion is doing exactly its job.

✅ **No published measurement moved, and it is asserted rather than argued.** Every experiment runs
`RoundsPerGame = 1`, so there is never a second round for a re-draw to precede —
`MatchEngineTests.AOneRoundGameIsTheSameGameUnderEveryPolicy` plays a one-round game under every
offered policy and compares the narration.

✅ **Both front ends offer the setting, out of the domain's one list.** The console asks *"How long
do the seats hold?"* as a `SelectionPrompt<SeatingPolicy>`; the lobby form has a `<select>` over
`SeatingPolicy.Offered` and `--seating` on the command line, resolved through the domain and never
trusted. ⚠️ **The console's capture changed by exactly the new prompt and one sentence** — a
16-line diff against `HEAD` at `--seed 20260819 --pick 0`, everything from the deal on identical,
and `drive-console.py` needed no change because its generic ENTER arm answers a selection list.

✅ **The browser stops rearranging itself around a fixed viewer every deal** (P13.5's layout always
assumed it would not), checked rather than assumed: two rounds of a hosted table are compared deal
order for deal order, and a sibling test asks a table to re-seat and watches it move.

⚠️ **The tree is green at 778 tests**, from 757. **`RULES.md` stays rev 29** — no rule changed, and
`CurrentRulesRevision` does not move.

---

🔥 **`P24.2` shipped 2026-08-22 on Opus 5: the arrow grows a sentence, and the journal records
where a person disagreed with the computer.** Nick asked for it from the browser the same day —
*"I see the suggested card for me to discard but I don't see where the explanation for that is"* —
and it is the packet's own acceptance criterion arriving as a bug report.

**What is on screen.** Every one of the five questions now arrives with a computed paragraph
inside the `<details>Why?</details>` that was already there. The discard's reads, at the table the
browser deals:

> Q♣ and 2♠ both leave 12 of your cards melded. Q♣ leaves 10 cards of the pack that would improve
> the hand; 2♠ leaves 8. That is why. At 5 any thirteen that all meld win, runs and sets alike.

🔥 **Each disclosure now holds two kinds of sentence and they are gated differently, which is the
fiddly part of the packet and it is fiddly in markup rather than in the domain.** The rule text is
**ungated** — a rule is not advice, the same distinction `HandPanel.Words` draws about §5.1 — and
the computed paragraph beside it **is** gated on the hints box.
`MarkupStandardsTests.TheComputersReasoningIsGatedOnHintsAndTheRuleBesideItIsNot` fixes that by
test rather than by taste.

🔥 **The keys were the missing half, not the ranking, and the trap is that they are packed for
sorting rather than for reading.** `IExplainsDiscards` is the described sibling of P31's
`IRanksDiscards`: `DiscardKey` carries a **name**, a **direction** and — the part that matters —
**what to say when the value is a sentinel**. `outs` stores its second key as `-LiveOuts.Count(…)`
because the sort takes the lowest first, and `CoverScore.Potential` returns `int.MaxValue` for a
joker. ⚠️ **A front end reading the raw numbers would draw *"−14 outs"* and *"2147483647
partners"***; nothing in `AdviceRationale` ever interprets a bare `long`.

✅ **`CoverScore.Scored` is the same call as `CoverScore.Ranking`, and the ranking is now defined
as its projection** — so an explanation is free: a page drawing the arrow has already paid for
every number in the sentence. ⚠️ **One deliberate behaviour change with no effect on order**: the
refinement key is `long?` and **null where it was never asked**, which is every candidate that had
already lost on cover count. A zero and a null sort identically there, so **no published
measurement moves** — what the null buys is that nobody can read back a key nobody took.
`ComputerAdvice.RankingsBought` is the instrument that makes acceptance 2 an assertion: the arrow,
the sentence and the journal's second opinion are **one ranking between them**, memoised on the
identity of the `TurnContext` (the engine builds a fresh one per decision, so it remembers one
decision and forgets it when the next arrives).

🔥 **The journal records an *opinion beside an answer*, and that is deliberately narrower than
`JournalingAgent`'s stated stance.** `JournalDecision.Advice` is a `JournalAdvice(CardId, Rung,
Why)`, and `JournalDecision.DisagreedWithTheComputer` is the query the packet exists for. It is
**not** a guess at the player's intention: it is a different agent's answer to the same
`TurnContext`, taken before the seat replies. ⚠️ **Only seats a person is playing** — a bot's
advice is its own answer. ⚠️ **By `CardId`** (§3.1): two decks hold two 5♥ and a value comparison
would say *"she agreed"* on precisely the hands worth studying. ✅ **It is recorded with the hints
box off**, because a record of where somebody disagreed must not depend on whether they wanted to
be told; replay ignores the field and `CurrentRulesRevision` does not move.

⚠️ **Three traps the packet named in advance, all closed by assertion.** **(1)** The explanation is
the **bare rung's at ε = 0 and never a level's** — `FallibleAgent`'s mistake *is* the runner-up of
the very ranking this renders, so explaining through a level would confidently justify a move
chosen **because it was second best**. **(2)** A banned card is explained as a **rule**, and on the
turn §5.1's floor yields the sentence **stops saying it** — computed per turn, never from a ban
worked out earlier. **(3)** No sentence implies the computer plays for §7.3's clean bonus: it does
not, and the true sentence is *"it will never throw a joker."*

⚠️ **P32's trap was real and is closed**: the closing clause reads the same `TableRules` the
evaluator does, so it says *"At 4 one of your melds must be a run…"* and *"At 5 any thirteen that
all meld win"* — **asserted at both, because they are different games.**

✅ **Two facts about scope.** The **console is untouched** and `drive-console.py`'s capture is
**byte-identical to `HEAD`** at `--seed 20260819 --pick 0`, which is also the proof that
`CoverScore`'s reshaping was a refactor. And **no rules question arose** — `RULES.md` stays rev 29.

⚠️ **The tree is green at 795 tests**, from 778.

---

🔥 **`P32` shipped 2026-08-22 on Opus 5: the standing answer is about a *five-handed* table, and
the headline is a negative result about this project's own published explanation.**
`SuiteOptions.Seats` read `RoundEngine.MinimumPlayers` until this packet — so **every figure
published between P12 and P33 was four-handed because four is the smallest legal table, not
because anybody chose it.** `RoundEngine.DefaultPlayers = 5` is now the one place the default
table is written down; the browser lobby and every `--seats` default in `Sim` read it.

🔥 **❌ P29's explanation is falsified.** P29 attributed the four-handed levelling of `simple` to
§7.1.1's joker-free series requirement. At five seats that requirement is **gone**, so the gap
should have re-opened. ⚠️ **Reading the raw margins would have said it *narrowed*, and that is
also wrong**: a fifth seat drops the base win rate 25% → 20%, so **every margin rescales by
0.800**. **Median ratio over all eighteen head-to-head cells: 0.801**, and the six `random` rows
land on 0.800 to three digits. `simple`'s gaps to `greedy` and `counting` came in at **7.42 and
7.96 against a pure-scale prediction of 7.36 and 7.90** — within a tenth of a point of an interval
of ±0.8. **Removing the requirement did nothing.** 🔥 **The five-handed ladder is the four-handed
ladder divided by 1.25** — every Holm verdict identical, the same eighteen separated and the same
three inside — **and what actually causes the levelling is now unknown.**

✅ **Four other predictions were written down before the run and all four held.** The jokerless
rate **fell** 15.4% → **12.1%** (§7.1.1 pushes four-handed play toward clean hands and five-handed
play not at all); what the bonus is worth **rose** from +$15 to **+$40** over flat (×3 to four
losers, not ×2 to three); **value wins by better than two to one** — expected **$2.31 → $4.84 a
round**; and the null cell reads **20.3 / 19.7** against 20%.

🔥 **The dial prediction was the fifth, and it was wrong in the useful direction.** The steps were
expected to compress with the base rate and force at least one ε to move. **Nothing moved.** The
five-handed sweep asks for `hard` ≈ 0.42 and `medium` ≈ 0.67 — the shipped 0.4 and 0.7 inside the
rounding — and the reference table reads **6.0 / 7.9 / 6.6** against four-handed's 7.1 / 7.9 / 8.0.
✅ **So ε is close to a property of *the mistake* rather than of the rung *or of the table***,
which is P23's finding holding on an axis it was never tested on. ✅ **One dial, not one per table
size** — the packet's first open decision, taken on measurement: **separated at four, five and six
seats**, evidence kept verbatim at `docs/strategy/dial-away-from-the-default-table.md`.

✅ **The packet's other two decisions, both measured rather than guessed.** **(1) Full crossing,
nothing subsampled.** `BUILD-PLAN` feared the five-handed free-for-all (`7⁵ = 16,807`) would be
"plausibly the majority of the run" — ⚠️ **that guess was wrong**: a full pass of the seven-rung
crossing at four seats is **2,401 games in 51 s**, so the five-handed pass is ~7 minutes of a
12,445 s suite. `SeatingPlan.MaximumAssignments` raised 4,096 → 32,768 with the measurement in the
remark. **(2) The four-handed set is kept whole**, frozen at
`docs/strategy/measurements-4-handed.csv`, fenced by a test asserting every row of it says
`seats=4`. **P12's headline is published at both sizes** — `headline.*.4-handed.*` and
`headline.*.5-handed.*` — so the project's longest-running measurement keeps its continuity.

⚠️ **`sim bench` gained `--seats`, and it had none**: there was no way to price a five-handed round
at all. A five-handed round costs ~15% more than a four-handed one for the `outs` family (8.5 vs
9.8 rounds/s), and 🔥 **`HandEvaluator` is *cheaper* at five seats** (108 vs 110 µs/hand) — §7.1.1
asks less of a five-handed hand, exactly as predicted.

⚠️ **The suite cost 12,445 s (3h27) against P33's 11,159 s**, for 123 measurements against 119.

---

## Rules: three revisions landed this session, and one of them makes the engine wrong

🔥 **`RULES.md` went 26 → 29 on 2026-08-22, from a conversation with Nick and Aung Aung, and none
of it is built.** `JournalHeader.CurrentRulesRevision` is **29**.

- **Rev 27 — §7.4 and §7.5, from Aung Aung.** *"If you win on an initial deal you get double
  payout"*, and *"if you win three in a row then the player proceeding you in turn order pays your
  whole payout (blamed for feeding)."* 🔥 **§7.5 is the first rule in this document that cannot be
  settled from a single round**, and the first that changes **who pays** rather than how much.
  ✅ **It is also the third independent saying to single out the seat *above* you** — §5.1 bans
  feeding the seat below, §4.5 needs the permission of the seat above, §7.5 blames the seat above.
  **Seven questions opened at once (§9 #38–#44)**, the largest single addition that section has
  taken. **§10 #20 and #21; packet P35.**
- 🔥 **Rev 28 — §7.5 contradicted §3, and the *old* rule was the wrong one.** §7.5's blame means
  nothing if the seat above you changes every deal. Asked directly (§9 #43), **Aung Aung: in real
  games you don't shuffle seats every round, only when people ask for it.** ✅ **§3 step 2 is
  corrected — a seating is drawn once and held** — and rev 19's *"every round, not once"* is
  **withdrawn**. ⚠️ **So the engine contradicts the document again, in the opposite direction to
  the error P28 fixed** (§10 **#22**): P28 built the reading now withdrawn, and pre-P28 held a
  seating but could never change it. **The fix is not a revert.** ✅ **Built by P36 on 2026-08-22
  and §10 #22 is discharged** — `SeatingPolicy`, default *held*.
- **Rev 29 — Nick ruled §9 #45**: a re-seating happens *"when people agree to do it"*, not when one
  player asks. `PLAYER`, so `EXPERT` may still overturn it. ⚠️ **It reverses that row's own
  recommendation** — *one asking is enough* was recommended because it invents no machinery, and
  **the more expensive reading is the one taken.** Opens **#47** (everybody, or most?) and §10
  **#23**.

🔥 **The heuristic that found rev 28 is worth keeping: when two Settled `EXPERT` rules collide,
suspect the older recording rather than the newer saying.** It was written into §9 as a
prediction *before* #43 was asked, and it was right.

✅ **No published measurement is affected by any of it.** §7.4 and §7.5 are unbuilt; §3's
correction cannot reach a figure because every experiment runs `RoundsPerGame = 1`.

⚠️ **`SettledRuleCoverageTests` fired, exactly as designed**, the moment §7.4 and §7.5 were marked
Settled — and they stood as `Exempt(...)` naming §10 #20/#21 and P35 until that packet shipped.
✅ **P35 converted both to `Checked(...)`**, in the way P33 converted §7.3's, **and there are now no
whole exemptions in that registry at all.** ⚠️ **§3's entry
now says out loud that `TheSeatingIsRedrawnEveryRoundAfterTheFirst` pins a behaviour the rules
authority contradicts**, and it is left standing rather than deleted so the seating is not
unasserted altogether.

---

## What is next

🔥 **The plan grew a fourth track on 2026-08-28, at Nick's direction: `P51`–`P54`, *taking the
table online* (`BUILD-PLAN.md` §5, `docs/HOSTING.md`).** Every packet P0–P50 is done, so this and
`P40` are the whole of the open work. ⚠️ **This track is ops, not the rules/strategy programme**:
no rule changes, `Domain` untouched by all four, **no suite regeneration owed**, and no measurement
can move. ✅ **`P51` (containerize) and `P52` (a published image) both shipped 2026-08-28.**
◐ **`P53` (the `burmesepoker` Ansible role) is half-done the same day**: the role is built, wired
into `nas.yml`, linted and committed in `~/source/repos/ansible-nas` (`7ffd645e`), and the
gitignored inventory is enabled — **but the play has not been run.** ⚠️ **It is not blocked on
judgment, on a credential or on the plan**: `ansible-playbook nas.yml --tags burmesepoker --become
--ask-become-pass` needs `ssh-add ~/ubuntuServer22key` and an interactive become password, and a
session can type neither. **So the next move on this packet is Nick's, and it is two commands**:

```text
ssh-add ~/ubuntuServer22key
ansible-playbook nas.yml --tags burmesepoker --become --ask-become-pass
```

🔥 **Then the acceptance, which is the whole point of the packet: a real round played from a phone
off the home wifi, with the network tab open.** Not a page that loads — a page that loads and then
does nothing is the shared symptom of a broken WebSocket path, an idle timeout, a missing
`UseForwardedHeaders`, *and* of the new basicauth failing the `/_blazor` upgrade. ⚠️ **The login is
`poker` and a generated passphrase, in the gitignored inventory.**
✅ **`P54` (host hardening) is done — 2026-08-29, in this repo.** Idle tables are reaped, the
copy-link affordance is on the lobby, and the patience is joined to the circuit-retention window.
⚠️ **Two of its acceptances are outstanding and both want a browser**: *the lobby affordance works
in a real browser*, and *a table with no viewers is gone after the interval* was proved against a
stopped clock rather than watched on a live site. **Do them in the same sitting as P53's phone
round.**

✅ **`P56` (opening a table you actually want) is done — 2026-08-29, in this repo**, because both
of its open questions had been answered the same day. See the block at the top of this file.
⚠️ **It adds a third thing to the browser sitting**: the advanced opponent group and the two-step
open form were exercised with `curl`, which is not a person.

✅ **`P57` (the lobby offered an opponent it cannot build) is done — 2026-08-30, in this repo.**
It came out of P53's browser sitting: choosing `random` for a seat in the per-seat picker was a
live **500**. See the block at the top of this file. ⚠️ **It adds a fourth thing to the browser
sitting** — the fix has not been seen on the deployed site, and **`git push origin main` is the CI
trigger**.

🔥 **So the remaining work is `P53`'s phone round, `P55` (needs a GitHub PAT) and
`P40` (needs Nick's vetted Burmese text).** ⚠️ **Every open packet is blocked on something only
Nick can supply**, which is worth saying out loud: a session that opens this file expecting to
build something should read that sentence first. 🔥 **The one piece of work a session could take
unaided is the browser sitting the last four packets have each deferred** — P54's reaping and
copy button, P56's advanced group and two-step form, and P57's per-seat `random`, all against a
running site built from `main`.

🔥 **`P56` was added 2026-08-28 at Nick's direction — *opening a table you actually want*** —
after the P53 role was written with `burmesepoker_people: "0"` and he pointed out that the
deployed table has no seat for a person in it. ⚠️ **Checking it first made the packet much
smaller than the ask sounds**: `Tables.razor`'s open form **already** offers Called, Seats, *Of
those, people*, the four difficulty levels, a mixed table and the seating policy, all clamped in
`Open` — so *"X human seats and Y bot seats"* is built, bot seats being `Seats − People`.
🔥 **The fact worth carrying out of it is what `People` means**: `HostedTable.Ready` is
`_attending > 0 && _table.IsFull`, and `IsFull` means **every person-seat claimed** — so `People`
is *how many people must turn up before a card is dealt*, not how many may. **`0` is a room you
can watch and never join; `5` deals nothing until the fifth friend arrives.** ✅ **The role default
is `1` now** (`ansible-nas` `f4fc41fe`), which is a table a visitor can sit down at alone.
✅ **Built 2026-08-29 — what follows is the plan it was built from, kept for the reasoning.**
🔥 **P56's two open questions were both answered by Nick on 2026-08-29, and neither answer is the
one this file recommended.** **(a) Personalities: option (b) — the rungs, behind an *advanced*
control, each with its measured margin beside it.** ⚠️ **That is a deliberate amendment to §3.12,
to P19 and to `DifficultyLadder`'s own doc comment**, which currently says in as many words that
*a menu with both in it would be the mistake this design exists to avoid* — **the packet must
rewrite those rather than quietly contradict them.** The rule becomes *levels are the menu; rungs
are an advanced disclosure that states its price*, and **the margin is the price**: it is not
decoration, it must be read from `measurements.csv` and fenced by `PublishedFigureTests` like every
other published figure, and a rung with no published row must not be offerable. ✅ **The
resolution machinery already exists** — `DifficultyLevel.Probe(rung, 0)` mints `<rung>@0` and
**`DifficultyLadder.FindOrProbe` is public and already resolves it** — so the only change below the
form is `Find` → `FindOrProbe` where a *seat* is being resolved (and ⚠️ **`Find` stays `Find` for
the `--difficulty` shorthand**, or a typo silently opens a table against a research rung).
**(b) Per-seat difficulty: build the picker.** ⚠️ **The obstacle is §3.11 C12, not `TablePlan`** —
the lobby is static SSR on purpose and cannot grow a control per seat as the count is typed, which
is exactly why `Mixed` is a checkbox. **Recommended shape: a two-step post** (choose the shape,
post, choose the seats on the returned form), which keeps every control on the page a real one;
the alternative is making the open form an interactive island, the first interactivity outside the
table. 🔥 **The two answers are one control, not two**: a per-seat picker whose options are
levels-plus-advanced-rungs is a single design, and building them in separate passes means
designing that form twice.

🔥 **`P55` was added 2026-08-28 at Nick's direction: make Gitea primary and GitHub a mirror.**
⚠️ **The planning found two things worth knowing before anyone starts it.** **(1) The histories
already agree** — GitHub `main` `a12c7a3` (P50), Gitea `main` `c20ced5` (P52), same lineage, Gitea
two ahead — so a mirror push is a **fast-forward, not a rewrite**, and *"wipe the GitHub repo"* is a
decision about the repo object and stale refs rather than about commits (recommendation: **keep the
repository**, let the mirror force-sync). **(2) `CLAUDE.md`'s claim that the 2023 implementation
"survives only at the git tag `pre-rewrite`" is false** — there are **no tags at all**, on any
remote or locally; the pre-rewrite tree is the history before `b32d08b` (*"P0: restructure and
salvage"*). Nothing is lost, but a mirror **prunes remote refs the source lacks**, so P55 settles
refs and tags *before* configuring the mirror. It needs a third credential — a **GitHub** PAT,
fine-grained `contents: write` on that repo only — and it leaves one open decision for Nick: what a
pull request means once CI and the canonical remote are both Gitea's. ⚠️ **Docker and Ansible
commands go in `text` fences, never `bash`** — `DocumentationTests` resolves `bash`-fenced commands
against the parser that would accept them.

- 🔥 **The finding that reshaped the plan: the homelab had already solved the hard part.** A review
  of `~/source/repos/ansible-nas` on 2026-08-28 found **Traefik running with a wildcard
  `*.nickjones.dev` Let's Encrypt certificate over Cloudflare DNS-01**, 80/443 already forwarded,
  110 roles on one two-file shape, and a Gitea Actions runner already carrying an opt-in
  docker-socket passthrough for runners that build images. **So `HOSTING.md`'s original recommendation — a Cloudflare Tunnel —
  was withdrawn** in favour of the reverse proxy that is already there, and the "where to host"
  decision stopped being a fork (§5a records it).
- ⚠️ **Two of the four packets are work in a different repository.** `P53` is a role in
  `ansible-nas`, whose task-level plan is already written at that repo's
  `docs/superpowers/plans/2026-08-28-burmesepoker-hosting.md`. `P52` is **blocked on a Gitea PAT**
  only Nick can issue. `P51` and `P54` are this repo.
- ⚠️ **Three traps recorded in advance**, each from the review rather than from experience here:
  `*_memory: 64m` (what both hand-written roles use — an OOM-kill loop for a .NET server holding
  circuits, **512m minimum**); building the image on the NAS rather than in CI; and **WebSockets and
  idle timeouts being assumed rather than proved** — the only role in that repo that mentions
  websockets is `bitwarden`, so `P53`'s acceptance is *a real round played from a phone off the home
  network*.

---

✅ **`P50` shipped 2026-08-27 — the documentation cleanup (F10) — and was the last planned packet
of the original programme.** ✅ **`P49` — `docs/SIMULATIONS.md` — shipped the same day.** Both were pure-documentation
packets touching no production code, so the hardened P48 numbers stand and **no suite regeneration
is owed by anything on the plan today**. `P40` — the game in Burmese — still stands open, blocked
on Nick's vetted Burmese text.

✅ **`P44` — `purist` — is done (2026-08-24).** The predicted positive came back a null with a
flat mechanism — the accidental jokerless floor is already the whole free lunch — see *Current
state* at the top of this file. ✅ **`P43` — `opportunist` — done the day before** (its null
closed `warden`'s question). ⚠️ **What P44 hands forward**: a paying clean-bonus rung is the
second standing anti-recommendation (BUILD-PLAN P47); `purist` must be re-measured if the
experts flip §9 #33; and **the crossing cap is still P45's** — `purist` is money-ranked, so
the free-for-all sits at exactly `SeatingPlan.MaximumAssignments` and the next *win-rate* rung
still breaks it.

🔥 **The plan grew eight entries on 2026-08-23, at Nick's direction: `P43`–`P50` — the strategy
frontier and the writing-down** (`BUILD-PLAN.md` §5, with a model recommendation per packet).
✅ `P43` `opportunist` and `P44` `purist` are done (both shipped on Fable 5). Next in order:
**`P45` `angler`** (a draw priced in cards, §8's own named successor; Fable 5 —
⚠️ owns the crossing-cap decision), **`P46` `sprinter`** (the endgame as a race;
Opus 5) — then **`P47`**, the blocked-rungs ledger (`jackpot` needs a conditioned-deal
harness, `streaker` needs a match-unit harness, and two anti-recommendations: no more
defence-refinement rungs, and no paying clean-bonus rung without new information),
**`P48`**, the full verification and measurement-hardening run
(fresh-seed replication, composition-stratified margins, ladder money margins, the bootstrap;
Fable 5), **`P49`**, `docs/SIMULATIONS.md` — the measurement programme taught, digit-free and
fenced (Fable 5) — and **`P50`**, the documentation cleanup that fixes the stale prose
figures the review found in `STRATEGY.md` and fences the class (Opus 5 — ⚠️ P43 corrected four
and P44 two more in their own blast radii, so re-verify F10's list rather than trusting its
count).
⚠️ **Order matters: rungs, then the run, then the documents** — each rung packet pays a full
suite regeneration (**~5¼ h now — 18,600 s measured at P44**), and the documents are written
once, against hardened numbers.

**`P40` — the game in Burmese — stands beside them, blocked on input only Nick can produce.** ⚠️ **Its translations must be made from the rev-31 rulebook**
— attach the current `RULEBOOK.md`, which teaches the face-up rule; the first-round outputs
under `docs/translation/` are rev-30-based and must be re-run. The translations are made
outside the repository: Nick attaches the English documents and runs the prompts in
**`docs/translation/PROMPTS.md`** against Gemini and ChatGPT — translate with one, cross-check
with the other (prompt C), repeat until clean — and the packet lands the vetted Markdown and
builds the fences (`BUILD-PLAN.md` §5 P40). ⚠️ **Do not start P40 without the vetted Burmese
text**; there is nothing for it to do until then. 🔥 **It feeds candidate 2 below**: the
experts are Burmese speakers, and a rulebook in their own language is the best instrument the
project could hand them for the **thirteen** defaulted §9 rows.

**Behind it, the four candidates stand** — and this file's recommendation is unchanged: **the
expert session is worth more than any code here**, and the failing tests would be its change
list (now thirteen fences: the eleven below plus rev 31's #49 and #50, fenced by P41 in
`TableLookTests`).

<details>
<summary>What used to stand here, before P42 shipped</summary>

🔥 **`P42` — playtest readiness — is next** (added 2026-08-23 at Nick's direction;
`BUILD-PLAN.md` §5 P42). Three non-rules gaps before real people sit down: **(1)** the
console's seat prompt defaults `MinimumPlayers` → `DefaultPlayers` plus a `drive-console.py`
re-capture (P32's leftover — it absorbs candidate 3 below); **(2)** the ×5 jackpot display —
the domain carries the jackpot fact on the result (⚠️ a watcher cannot compute it: ownership
is partly private until settlement), both settlement panels say it, the table centre notes the
7♦/A♠ pair when it is up, `CardDisplayState` stays ×5-free on purpose, **§9 #32 is not
generalised**, and because Domain is touched **P41's byte-identity procedure is repeated**;
**(3)** 🔥 **the session itself plays a browser round to settlement with Claude in Chrome** —
⚠️ the real browser via the extension, never headless; if the browser tools are off, report
that item blocked rather than routing around it — against the checklist in the packet, and
**the report says what was actually exercised** (P11's rule).

</details>

<details>
<summary>What used to stand here, before P41 shipped</summary>

🔥 **`P41` — the table shows what the rules make public — is next** (rev 31's two visibility
rules, buildable now; `BUILD-PLAN.md` §5 P41). **`P40` — the game in Burmese — stands beside
it, blocked on input only Nick can produce.**

</details>

<details>
<summary>What used to stand here, before P39 shipped</summary>

🔥 **`P39` — the strategy guide (`docs/HOW-TO-PLAY-WELL.md`) — was next, and it was the last packet
on the plan.** ✅ **`P38` is done**, so the reader P39 writes for now exists: somebody who has a
rulebook to be strategic about. ⚠️ **Three things P38 settled that P39 should copy rather than
re-invent**: figures are fenced by extending `PublishedFigureTests.TheFiguresThePlayersGuideQuotes`
(the packet's own build item 5 already says so); the new document lands in the map or
`DocumentationTests` goes red; and **`PLAYING.md`'s *Playing better* section becomes a pointer**,
which means the two regexes that fence its figures today move to the new document with them — do
not leave them asserting a section that no longer quotes anything. ⚠️ **P38 leaves P39 one trap**:
`RULEBOOK.md`'s worked round quotes real dollar figures. Those are *engine-replayed*, not
`measurements.csv` rows — P39's guide must quote **only** CSV-fenced figures, and if it wants a
worked example it should point at the rulebook's rather than grow a second one.

</details>

<details>
<summary>What used to stand here, before P38 shipped</summary>

🔥 **`P38` — the rulebook — is next, and `P39` — the strategy guide — is behind it.** Both were
added on 2026-08-23, after P34 shipped, from a question Nick asked: *do we have a rules onboarding
document for a new player, and a definitive guide to strategy?* **The answer to both was no.**

🔥 **The gap is one of *audience*, not of content.** Everything a rulebook needs is in `RULES.md`
and everything a strategy guide needs is in `STRATEGY.md` — but `RULES.md` is organised as a
**reconstruction** (provenance on every rule, a live ledger of what nobody knows) and `STRATEGY.md`
is organised as a **research report** (paired margins, Holm verdicts, rungs indexed by packet).
⚠️ **Neither is wrong; both are written for the project rather than for a player**, and
`RULES-PRIMER.md` says in its own first line that it is a *recall aid* for somebody who already
knows the game.

1. **`P38` — the rulebook** (`docs/RULEBOOK.md`). The game taught in reading order, one answer per
   rule, no provenance tags and no open questions, with a generated worked round and a one-page
   reference. ⚠️ **`RULES.md` stays the sole authority** and the rulebook decides nothing; it
   stamps the rev it was derived from and **a test binds the two**, because a rulebook is the
   highest-consequence stale document this project could own — it is the one a person plays from
   and the furthest from anything a build would break. 🔥 **The hard part: eleven §9 rows are
   played on a recorded default, and a rulebook must state one** — so it carries a short *house
   readings* appendix in a player's language, for the day somebody sits down with a player who
   learned the game elsewhere.
2. **`P39` — how to play well** (`docs/HOW-TO-PLAY-WELL.md`). What has actually been measured, in
   plain language, organised by decision rather than by experiment, **with the nulls given as much
   room as the margins** — discarding a money card costs nothing, most of the money is decided at
   the deal, refusing a claim is worth nothing, counting cards is worth nothing. ⚠️ **It takes
   ownership of `PLAYING.md`'s *Playing better* figures**, which are the ones that went two whole
   measurements stale before P34 fenced them: **a figure with two homes has none.**

</details>

**Behind P39, four candidates** — none written up as a packet yet, in the order this file would
recommend:

1. **`RULES.md` §10 #7 — the table sizes nobody can deal.** `RoundEngine.MinimumPlayers` is 4 and
   §2 records 2 to 6 as Settled, so `TableRules.For(2)` and `For(3)` are correct, tested and
   unreachable. **It is the only Settled rule the program does not play**, and the oldest entry in
   §10 that no packet owns. ⚠️ **Two-handed is a different game** — series only, sets illegal as
   melds — so this is a rules-shaped packet and not a constant.
2. **The answers that are being played on a default.** Seven §9 rows are live defaults with a test
   named for each (#33, #36, #37, #38–#41, #44, #46, #47, #48). **An expert session is worth more
   than any code here**: the failing tests would be the change list.
3. ✅ **The console still deals four** — its seat prompt defaults to `RoundEngine.MinimumPlayers`
   rather than `DefaultPlayers`. **One line plus a `drive-console.py` re-capture**, outstanding
   since P32 and now through five packets. **Absorbed into P42 and done (2026-08-23).**
4. **A rung that knows §7.3, §7.4 or §7.5 exists.** No rung does, deliberately (P15's discipline),
   so nothing prices a joker thrown for the clean bonus or a seating asked for before somebody's
   third win. **Either is a new rung and arrives measured.**

<details>
<summary>What used to stand here, before P34 shipped</summary>

✅ **`P35` is done, so `P34` — the front door — is the only packet left on the plan.** §10 is empty
and every rule `RULES.md` records as Settled is implemented.

1. **`P34` — the front door, and documentation that cannot go stale quietly.** Needs no expert
   answer, regenerates nothing, collides with nothing. 🔥 **P35 re-planned it with a fifth check:
   every recorded default in §9 that says *"built on this default"* must name a test that exists.**
   Five packets have now shipped rules built on open questions and P35 alone did it six times; **a
   fence that has been renamed or deleted is worse than no fence**, because the document goes on
   promising it. ⚠️ **Two more claims for the staleness pass**: `STRATEGY.md` §11 now carries a
   *negative* measurement claim (§7.5 is not in the standing set and cannot be), and the exemption
   ceiling is a number in a test that a status file also quotes.

<details>
<summary>What used to stand here, before P35 shipped</summary>

1. **`P35` — §7.4 and §7.5.** ✅ Unblocked by rev 28, and ✅ **P36 discharged its dependency**: a
   streak blamed on the seat above you needs a seating that survives three rounds, and one does.
   🔥 **P36 and P37 between them made §9 #46 live** — the seats can now move under a two-round
   winner, both by a house policy and **by the table agreeing to it**, so *"what if the seats
   change mid-streak"* is no longer hypothetical and its safe default needs a test rather than a
   note. ⚠️ **Read the streak off `MatchEngine.Seating` and never off `Players`**: the membership
   is fixed and the order is not. ⚠️ **And §7.5's *seat above the winner* is now a thing a player
   can change on purpose**, which is a strategy nobody has measured and P35 should say so about
   rather than measure.
2. **`P34` — the front door.** Still needs no expert answer and collides with nothing.
   ⚠️ **P24.2 added one thing for it to watch**: `AdviceRationale.ForObjection` says out loud that
   refusing a claim *"has been measured, and is worth nothing either way"*. That is a measured
   claim shipped as prose in the product — **deliberately carrying no number**, so it cannot rot
   into a wrong figure, but P34's staleness habit should know it is there.

⚠️ **Two leftovers.** ✅ **P36's is taken** — `AboutTable` says what the seats are doing now, and
so does the console's round-start line, which had gone on claiming *"the seats are re-drawn every
round"* for a day after P36 made that false. **(1) The console still deals four** —
`Console/Program.cs`'s seat prompt defaults to `RoundEngine.MinimumPlayers` rather than
`DefaultPlayers` (P32's leftover; one line plus a `drive-console.py` re-capture). **(2) The console
gains nothing from P24.2 and that was the packet's scope decision, not an oversight** — it keeps
its arrow, which is what keeps P21's and P23's byte-identical captures spendable.

⚠️ **The tree is green at 757 tests**, from 736.

</details>

⚠️ **Three leftovers, and one of them has been outstanding since P32.** **(1) The console still
deals four** — `Console/Program.cs`'s seat prompt defaults to `RoundEngine.MinimumPlayers` rather
than `DefaultPlayers`; one line plus a `drive-console.py` re-capture, and it has now survived four
packets. **(2) The console gains nothing from P24.2**, which was that packet's scope decision and
is what keeps P21's and P23's byte-identical captures spendable. **(3) No rung knows §7.4 or §7.5
exists**, deliberately: a rung that declined a deal-bonus win, or that played the seating question
around somebody's streak, would be a **new rung** under P15's discipline — measured before it
joined the ladder — and not a change to `outs`.

</details>

⚠️ **Three leftovers carried into whatever comes next.** **(1) The console still deals four** —
`Console/Program.cs`'s seat prompt defaults to `RoundEngine.MinimumPlayers` rather than
`DefaultPlayers`; one line plus a `drive-console.py` re-capture, outstanding since P32 and now
through **five** packets. **(2) The console gains nothing from P24.2**, which was that packet's
scope decision and is what keeps P21's and P23's byte-identical captures spendable. **(3) No rung
knows §7.3, §7.4 or §7.5 exists**, deliberately: a rung that threw a joker for the clean bonus,
declined a deal-bonus win, or played the seating question around somebody's streak would be a
**new rung** under P15's discipline — measured before it joined the ladder.

---

*Everything below this line was written before P33 — P31's session log, then the two rules sessions
that produced revs 25 and 26. It is kept because the reasoning that produced §7.3 is the reasoning
P33 was built from; ⚠️ **where it says the bonus is unbuilt or that P33 is next, read the block
above.***

🔥 **`P31` shipped 2026-08-22 on Opus 5: `warden`, the feeding ban played offensively — and it
lost.** `Domain/Agents/WardenBotAgent.cs` is `outs` with the **take** changed: it will take a card
it does not want when that closes the rank against the seat that threw it (`RULES.md` §5.1), and
then **holds** that rank rather than throwing it back. It measures **`−9.3 ± 1.0` against `outs`**
and about six points behind `greedy`, `cautious` and `counting`, beating only `simple` and
`random` — **all six margins surviving Holm over a family of twenty-one.** ⚠️ **The packet
predicted a null; this is the largest separated *loss* the programme has produced.**

🔥 **The packet's third build item is what makes that a diagnosis instead of a shrug, and it is
the thing to remember.** `RULES.md` §5.1 is enforced as an **impossible move**, so it leaves no
trace by construction and nothing had ever measured whether it does anything. P31 built the
counterfactual — `IRanksDiscards.RankDiscards(context, candidates)`, **an instrument the engine may
never call** — which asks a restricted seat what it *would* have thrown over its whole hand.
**The ban removed a held card from 30.5% of all discards in the crossed field and changed the
seat's answer on 30.8% of those: it takes the card a seat meant to play on 9.4% of every turn.**
⚠️ **So the escape hatch the packet named in advance is closed** — the locks bite hard and the rung
still loses. **The rule is not what failed.**

⚠️ **The *why*, and it is the constraint on any successor.** `warden` prices a lock in **melded
cards** — it declines any lock it cannot absorb by shedding a partner-less card — and then pays for
every lock it takes with a **draw**, which is the only thing that improves a hand and which nothing
in its rule prices. An all-`warden` table runs **31.9 turns a round against `outs`' 24.1**: about a
third of its draws have become locks. **A successor rung must price the draw** — `prospector`
prices one in money (`MoneyOdds.PerBlindDraw`) and nothing yet prices one in cards, though
`LiveOuts` is the obvious currency and is already in the file.

🔥 **Reproduction: 71 of the 88 shared CSV rows came back byte-identical**, including every
head-to-head cell among the six older rungs, every pairing ratio, **the whole difficulty dial and
the whole money sweep**. The 17 that moved are exactly the rows a seventh rung must move — the
free-for-all column, the mean-margin ranking (21 comparisons, not 15) and the four ladder-scope
statistics computed off the free-for-all cell. ⚠️ **P29 reproduced 4 of 91 and was right to**: it
ran across a rules change and this did not. **"Does it reproduce" is a question with an answer
again**, and this is the strongest answer the document has carried (P23's best was 59 of 77).

🔥 **The unplanned finding, and a cold session should read it as a general lesson rather than a
ladder detail: "the last rung named is the strongest rung" was a coincidence asserted as a law, in
three places at once.** `warden` and `prospector` both hang off `outs`, so the ladder became a
**tree** and `BotCatalog.Ladder[^1]` stopped being `BotCatalog.Hardest`.
- ✅ `SuiteOptions.MoneyReference` read `Ladder[^1]` and **would have swept the side bet against
  `warden`** — a rung never ranked above anything. **Fixed** to `BotCatalog.Hardest`.
- ✅ `StandingAnswerTests` asserted `Assert.Same(Hardest, Ladder[^1])`. **Fixed** to assert what was
  meant.
- ⚠️ `TournamentOptions.NullTestStrategy` takes the last strategy named, so **the null cell changed
  hands from `outs` to `warden` with nobody choosing it**. **Left alone on purpose**: the cell's
  claim is that *any* strategy against a copy of itself wins 1/n, so a null test that depended on
  who played it would itself be the finding. It holds at both (`+0.5` then `+0.9 ± 1.0`).

⚠️ **`sim suite` is a three-hour job now — 11,020 s for 116 measurements**, up from 9,981 s for 91.
**40% more head-to-head cells for 10% more wall clock**, because `warden` costs **5.6× a `greedy`
round against `outs`' 6.1×** (its candidate set is smaller) even though its rounds are a third
longer. ✅ **R3 and R13's owed corrections landed with it**: `claim.permission.money.refuse-over-allow`
is paired at **±0.25** (mean unmoved to six decimal places, the null intact) and four
`money.side-margin.*` rows appeared.

⚠️ **`warden` is `Strength: 3`, level with `outs`, so `BotCatalog.Hardest` did not move** — the
difficulty dial is untouched, every ε is unchanged, and **no front end gained an option.** Same
standing `prospector` has had since P22.

✅ **No rules question was raised and `RULES.md` stays at rev 24** (`JournalHeader.CurrentRulesRevision`
unchanged). `warden` **deliberately declines to lock jokers**: whether taking a joker closes the
other jokers is §9 #27, a `PLAYER` house ruling, and the one rung whose whole claim rests on the
lock must not be built on the one part of §5.1 nobody has confirmed.

🔥 **STOP — read this first. `RULES.md` is rev 26 as of 2026-08-22, and `P33` is the next packet.**

**The expert corrected her own rule, unprompted, hours after giving it.** Rev 25 recorded §7.3 as a
flat **×3** for declaring with *"all series clean"*. Mya Lay came back the same day:

> *"But one thing, if you play two players, three players, or four players, you will only get two
> times of the winning prize … you got three times of the winning prize if we are playing five
> players. Not just series, if you want to win with **jokerless** … you can discard the joker."*

**Two corrections, and both widen the rule:**

| | Rev 25 recorded | Rev 26 |
|---|---|---|
| **The multiplier** | flat **×3** | 🔥 **×2 at two, three or four seats; ×3 at five or more** — a function of the table size |
| **The condition** | *"all series clean"* | 🔥 **jokerless — the whole declared thirteen**, a joker in a **set** forfeits it too |

✅ **That closes §9 #34 and #35, and #35 is the row that had no safe default and blocked `P32`.**
It closed the expensive way: the bonus exists at five-plus and pays **most** there, so
**cleanliness is relevant at every table size for the first time** and the win condition and the
scoring have stopped being separable. **Both P32 and P33 are unblocked.**

🔥 **Three things a cold session needs from this.**

**(1) The packet got *smaller*, and the reason is the deliverable.** Rev 25's P33 flagged its
hardest problem as *"a hand can partition more than one way — is the winner paid on the best
partition or on the one the engine found?"*. **Jokerless is a property of the hand, not of the
partition**, so the question does not arise: `HandEvaluator` needs nothing and the predicate is
*no joker in the thirteen*. ⚠️ **Do not reach for `Meld.IsClean`** — it implements §7.1.1's
*required clean series*, a different rule sharing a word. **The multiplier does need the seat
count**, which `Settlement` has never taken; `TableRules.For(n)` is where §7.1.1's table already
lives and is the home for §7.3's.

**(2) The arithmetic in rev 25 was wrong and is corrected.** Four-handed clean is **$30**, not $45.
Per opponent the bonus pays **+$5 a head at 2/3/4 and +$10 at 5+**, against the ×5 jackpot's $10 a
head — so rev 25's *"largest single swing in the game, ahead of the ×5 jackpot"* is **withdrawn**:
it is half the jackpot at small tables and level with it at five. ✅ **It is still far more
consequential**, because a 7♦/A♠ turn-up is one round in 1,444 and a jokerless declaration is not.

**(3) P33 and P32 have become one measurement.** At five seats §7.1.1 requires nothing clean, so
**the bonus is the only thing cleanliness is ever worth there**. Running them as two packets buys a
second three-hour suite for a figure the first invalidates. **Fold P32's seat-count change into
P33's regeneration.**

⚠️ **Two rows stay open and neither blocks the build** — **#36** (does the multiplier reach the
money settlement? recommend **not**; the new sentence names *the winning prize* twice more and never
the money) and **#37**, new (is six-plus also ×3? recommend **yes**, matching §7.1.1's five-or-more
grouping). **#33** is also still open — whether the joker may be shed before the declaring discard —
with a default that changes nothing. ⚠️ **But note what that default costs the measurement**: if
§5.1 only yields on the declaring discard, a hand needing to shed a joker a turn early cannot reach
the bonus at all, so **any measured bonus rate is a floor.**

⚠️ **`JournalHeader.CurrentRulesRevision` is 26** — the binding test is unconditional, so it moves
with every revision and not only the play-changing ones. **Rev 26 changes no play**; §7.3 is
settlement and is still unbuilt.

🔥 **The methodological finding, and it overturns rev 25's own lesson.** Rev 25 closed by observing
that every rule this document has got wrong was *flat and later narrowed*, and recommended the
**narrow** reading of #34 on that basis. **The answer was the broad one.** ⚠️ **The heuristic was
stated backwards**: what §6.2 and §7.1 share is not breadth, it is that **a broad rule was inferred
from a narrow sentence**. §7.3 inferred a narrow rule from a narrow sentence and was wrong in the
other direction. **Inference from silence is the variable, not breadth** — and six sessions running
have now answered past the question asked, this one without the question being put at all.
⚠️ **Do not treat a rules session as closed on the day it ends.**

---

*What follows was written when rev 25 landed, before the correction.*

🔥 **The plan changed on 2026-08-22, after P31 shipped. `RULES.md` reached rev 25 and `P32` was
blocked.** Three questions went to the experts and all three confirmed their standing default
(§9 **#19** a release survives the reshuffle — *yes*; **#32** the ×5 needs the 7♦/A♠ pair —
*"specifically"*; **#27** taking a joker closes the other jokers — *"yeah"*). **No code changed and
no play changed.** ⚠️ **But #27's answer carried a rule nobody had ever recorded:**

> *"Unless you want all series clean that got a 3-time winning game prize, you have a joker, so you
> discard the joker for the winning clean series."*

🔥 **`RULES.md` §7.3 is new: an all-clean declaration pays ×3 the winning prize.** §7.2 has called
the round payment **flat** since rev 1, `PLAYER`, Settled — and an `EXPERT` answer outranks it. At
standard stakes it takes a four-handed win from $15 to **$45** and a five-handed one from $20 to
**$60**, against a side bet measured at $11.58 a round. **It is the largest single swing in the
game, it is `EXPERT`, and nothing implements it** (§10 #19 — the only Settled ruling that is
unbuilt, which had been an empty list since P28).

⚠️ **It also answers a question nobody had asked: *why would anyone ever throw a joker away?*** No
rung in this project will part with one — `CoverScore.Potential` returns `int.MaxValue` for a
joker — and the expert has named the one situation in which a person does. **Every figure in
`docs/STRATEGY.md` is measured in a world where that reason does not exist.**

⚠️ **Four things the sentence does not say, and #35 has no safe default** (§9 #33–#36): whether the
joker may be thrown before the declaring discard or only as it (#33 — if only as it, §5.1's
existing exception already covers it and nothing changes); what *"all series clean"* reaches, hand
or required melds (#34 — ⚠️ **the exact shape this document has recorded flat and had to narrow
twice**); **whether the bonus exists at five or more seats, where §7.1.1 requires no series at all**
(#35 — **this is what blocks P32**); and whether the ×3 reaches the money settlement (#36).
✅ **All four are drafted flat in `docs/QUESTIONS-FOR-MYA-LAY.md` Q10, ready to ask.**

⚠️ **So the next packet is not P32.** Either **ask #33–#36 and then build §7.3** (`P33` — a
settlement packet, with a suite regeneration behind it), **or** get #35 answered at minimum, since a
five-handed re-measurement taken now would be invalidated by the bonus the moment it lands.
**Nothing is in progress and the tree is green, so there is no cost to waiting.**
✅ **Overtaken by rev 26 — #34 and #35 came back unasked, and `P33` can start.**

✅ **And there is a packet that needs no answer at all: `P34`, the front door.** There is **no
`README.md`** in this repository — a visitor's first sight of it is `CLAUDE.md`, which is written
for a cold Claude session rather than for a person — and three of the ten documents are wholly
historical without saying so above the fold. **P34 builds the front door and turns the
anti-staleness habit into tests**, needs no expert answer, regenerates nothing, and is the cheapest
packet on the plan. 🔥 **It is the one to run while P33 waits.**

⚠️ **One thing deliberately not done**: §7.3 is *not* registered in `SettledRuleCoverageTests`'
check-or-exemption registry, because its heading does not claim to be Settled — the rule is
settled, the details are not. **When #34–#36 close and the heading becomes Settled, that test
fires**, which is the mechanism working rather than a gap.
🔥 **Rev 26 made §7.3's heading Settled and the test fired the same hour** —
`EverySettledRuleIsCheckedOrNamesWhyItCannotBe` went red with *"RULES.md records Settled rules in
section(s) §7.3 that no conformance registry entry checks or exempts."* ✅ **Registered as an
`Exempt(...)` entry**, and it is the **only** exemption in that registry whose reason is *the code
is missing* rather than *no ordinary-play check could exist*. ⚠️ **P33 must convert it to
`Checked(...)`, not delete it** — the entry says so in the file. **A conformance check written
today would re-derive the bonus and fail every round somebody declares jokerless**, which is why it
is exempt rather than checked.

✅ **The packet after §7.3 is `P32`** — five seats as the default table. ⚠️ **It has been re-costed and
the amendment is in `BUILD-PLAN.md` §5 P32**: a seventh ranked rung makes the round-robin 21 cells,
and the five-handed free-for-all goes from `6⁵ = 7,776` seatings to **`7⁵ = 16,807`**, so that one
cell is plausibly the majority of the run. **Decide before starting whether it runs at the full
crossing or a stated subsample — and if it is capped, say so in the file.** ⚠️ **Set the model with
`/model` before the session; the packet records which one and cannot choose it.**
⚠️ **P24.2** (the computer's reasoning said out loud) is still Nick's call.

---

### The verify branch, which closed before P31

🔥 **P30.2 — the conformance harness and the front-end tests — shipped 2026-08-21, on Fable 5.**

⚠️ **`P24` was split** into **P24.1** (a journal for the hosted table — plumbing P30.2's browser
half needs, and `Web`/`Server` contain the string `journal` **zero times**) and **P24.2** (the
computer's reasoning said out loud — the product half, and still Nick's call whether to build it
at all).

🔥 **Why this order, and it is wall clock rather than taste.** P30 can invalidate the other two:
if a front end is not playing by a Settled rule then `warden` is a rung built on a
misunderstanding and the five-seat re-measurement measures the wrong game. **And P31 before P32 is
deliberate** — each costs a full suite regeneration (`StandingAnswerTests` demands a published row
for every rung), so measuring the new rung at **four** seats keeps it comparable with every
historical figure, and the five-seat move then measures a field that is already complete.
⚠️ **Merging them saves about three hours and costs the attribution**: a run that changed the
table size *and* the field at once could not say which moved a number — **which is exactly the
property that made P29's wrong prediction legible.**

🔥 **And the review is not ceremony in front of the tests — it writes their list.** A conformance
harness checks the rules somebody thought to check; **every defect class this project has actually
shipped was found by reading, not by running** — a predicate written twice (§9 #30), a `switch`
default that means something (`JournalFormat.Name`), a test that cannot fail (`ClickingPlayer`), a
test asserting a property of round length without saying so (P21).

**The plan as it stood before this branch: every packet was done. P29 shipped 2026-08-21.** P0–P12, P13.1–P13.6, P14–P23 and
P25–P29 are all in; `RULES.md` is rev 24 and **every rule the document records as Settled has an
implementation**; and as of P29 **every figure in `docs/STRATEGY.md` was measured under those
rules** rather than under the ones the game played by before 2026-08-21.

⚠️ **P24's ordering was a recommendation rather than a decision** (`BUILD-PLAN.md` §4); its stated
reason for going first is spent, and P26–P28 changed decisions it would have explained. ✅ **P24.1
now has a reason of its own** — P30.2 cannot audit a browser round that nothing wrote down.

🔥 **What P29 measured, and the headline is the reproduction going the other way.** `sim suite
--games 8000 --seed 20260819` produced **91 measurements in 9,981 s** and **4 of the 91 rows came
back byte-identical — and the four are the ε values of the difficulty dial, which are the only
numbers in the file a human chose.** P23's regeneration reproduced 59 of 77 and called that the
document's strongest reproducibility claim. ⚠️ **"Does it reproduce" is not a question that can be
asked across a rules change**, and this run is what that looks like: the only rows that can
survive one are the rows that are not measurements.

⚠️ **The suite got *cheaper*, which nobody predicted: 9,981 s against P23's 17,539 s**, with a
sixteenth cell added. `outs` costs **7.0× a `greedy` round** now against 8.2× at P21 (`sim
bench`), because P25's win condition prunes the cover search earlier than it lengthens the round.
**The five-hour budget in `BUILD-PLAN.md` and `CLAUDE.md` was wrong and is now two and three
quarter hours.**

🔥 **Three predictions were written down before the run so the packet could be wrong. Two held and
one did not, and the wrong one is worth the most.**
1. ✅ **The dial survives.** All three steps still separate under Holm, **no ε moved**, and the
   reference table's spacing improved to **8.1 / 7.9 / 7.1** from 7.9 / 6.7 / 7.7.
2. ❌ **`outs` did not lose its margin — the point estimate did not even drift.** `+3.0 ± 1.0`
   over `greedy` against +3.1 ± 1.0, and a mean margin over the field of **+14.6**, the same
   figure to the decimal. The prediction reasoned that every rung maximises cover count and cover
   count no longer wins at four seats (§7.1.1). 🔥 **What the new condition actually moved is
   `simple`**, which gained about two points on each of `greedy`, `cautious` and `counting`
   (11.2 → **9.2**, 10.8 → **8.8**, 11.0 → **9.9**). ⚠️ **A requirement no rung optimises for is a
   leveller and not a handicap**: the four-handed condition demands a joker-free series and
   nothing in any rung is aimed at it, so the better melder pays the same tax. **It compressed the
   bottom of the ladder and left the top margin alone**, which is the opposite of what "everybody
   is maximising the wrong objective" predicts.
3. ✅ **`prospector` separates a whole ratio lower.** $5/$20 was `+0.95 ± 1.63` and *inside the
   interval*; it is **`+5.32 ± 2.27` and separated under Holm**, with the take rate collapsing
   **8.4% → 0.1%** — the mechanism variable, so this is a measurement and not a coincidence.
   $5/$40 nearly doubled to `+14.63 ± 4.48`. ✅ **And the `$5/$1` identity survived P26** — checked
   at 400 games, `prospector` and `outs` play byte-identical rounds — so §10's central claim
   holds.

✅ **Two things nothing had ever reported are now published** (`STRATEGY.md` §12).
**(1) Round length and abandoned rounds.** 28.6 turns a round for the ladder, 30.0 for the dial,
23.9 for the two-arm claim cell; **the only non-zero abandoned count is the field containing
`random`** — 8 games in 9,072, or 0.088% — and both all-`outs` fields settled every game.
⚠️ **That is evidence and not a proof**: P27 broke the argument that a table of bots must
converge, and what stands behind convergence now is `SimulationOptions.TurnCap`. A weaker table is
a *slower* one, which is why the dial's rounds are the longest.
**(2) What refusing a claim is worth: nothing.** `outs/refuse` against `outs/allow` at 8,008
games is **`+0.4 ± 1.0`** on win rate and **`+0.02 ± 0.18`** on money — a null, published (P20's
discipline). ✅ **The branch is not rare**: the opener asks for the card in about **a quarter of
rounds** and the seat upstream is holding that rank about **half** the time, so the veto is
exercised in roughly one round in seven. **So the null says the decision P28 took costs nothing
either way, rather than saying the rule never fires.**

🔥 **`RULES.md` needed no change and `§9` gained no question.** P29 is the first packet since the
rules sessions began that measured without discovering a rule, which is what it was for.

Nothing is in progress and the tree is green at **715 passed / 0 failed** — 697 after P30.2, and
the eighteen new ones are P31's: nine in `WardenBotAgentTests` (the take, the two ways a lock is
worthless, the joker abstention, the restraint, and the restraint's two escapes), three in
`LockBiteTests` (the instrument changes no card, it is off unless a cell buys it, and the ban
really does take cards seats meant to throw), and six more that the catalog's theories pick up
automatically for a seventh rung. ⚠️ **The test suite takes about nine minutes idle** and **half an
hour while a `sim suite` is running** (measured at P24.1 under exactly that contention; not a
regression).

⚠️ **A `drive-console.py` capture from before P30.2 no longer compares** — R5 fixed the money
narration every round opens with, and the `human` script now declares and exits cleanly instead
of dying mid-round-2 — so **capture fresh baselines before any front-end refactor**. The `bots`
capture at seed 20260819 was proved byte-identical across the driver rewrite itself, so the
engine's play did not move except where R1 binds an exception-2 throw to its declaration (bots
unaffected — every rung's `Declare` was already `=> true`).

✅ **The feeding ban is built — P27, 2026-08-21, `RULES.md` rev 23.** The block below is the
record of what it was built *from*; what it became is in **Notes for the next session**.
`RULES.md` **§5.1, the feeding ban** —
*you may not discard a rank the next player has taken in the open, until they throw that rank back
or you are going out on it* — is `EXPERT` and **Settled**, and it is **the first rule in the
document that constrains *which* card a player may discard.** `RoundEngine` accepts any of the
fourteen, and `TurnContext` does not show a seat the information the rule is decided from.
🔥 **Two rulings by Nick the same day settle how it is enforced, and neither is safe alone.**
*(a)* **A banned discard is not an infraction but an impossible move** — never offered, cannot be
chosen — so there is no penalty and nothing to retract. *(b)* **Where the ban would leave a player
no legal discard at all, the ban yields for that turn**: the discard is mandatory (§7.1), the ban
is not. **(b) is what makes (a) safe** — impossible-move enforcement removes the social escape
hatch, so without the floor a fourteen-card hand of banned ranks is a turn that cannot be
completed. Together they are one line: the legal discards are *the hand minus the banned ranks, or
the whole hand if that is empty*, **never empty by construction**.
⚠️ **The bill is on `TurnContext` and on every agent**: the upstream seat must be *told* which
ranks are closed (free — the ban is computed from things done in the open, not from reading a
pile, so it holds either way §9 #15 is answered), and every discard ranking must be filtered to
the legal cards, **`FallibleAgent`'s runner-up included**, because a mistake still has to be a
legal move.
✅ **The packet can be written now.** All six details — `RULES.md` §9 **#16–#19, #25 and #27** —
were closed on 2026-08-20 by a session with **Mya Lay and Aung Aung**, and §5.1's specification is
complete. Four are `EXPERT`; ⚠️ **two are `PLAYER` house rulings and stay flagged as such** — that
a release survives the reshuffle (*"nobody really knows"*, was #19) and that taking a joker closes
the other jokers (*"I'd assume"*, was #27). A house ruling that later turns out wrong is a **rule
change**, not a typo fix.

✅ **The same run of sessions changed something bigger than the feeding ban, and P25 implemented
it on 2026-08-21: the win condition is a function of the table size** (`RULES.md` §7.1.1,
`EXPERT`). Two-handed is **series only, sets illegal as melds**; three-handed needs **two**
series; four-handed **one**; five-or-more none — and **the series a table size requires must be
joker-free**, while surplus ones need not be. A longer run may be laid down **split**, so `3+3`
out of a six-card run counts as two series. ✅ **`HandEvaluator` implements all of it since P25**:
`TableRules.For(players)` is the table as data, the evaluator takes it as a parameter and **has no
parameterless overload**, and the search carries what is still owing *along* the partition rather
than auditing a cover it has already chosen. ⚠️ **Every figure in `docs/STRATEGY.md` is still
measured at four seats under the wrong condition** — that is P29, and P25 gave it a first data
point: greedy-vs-simple over 200 games at one seed goes from **25.1 to 26.6 turns a round** and
from **102 to 86 rounds/s**, with greedy's share slipping 32.3% → 31.3%. **A smoke test, not a
measurement.** ⚠️ **§2's "four players minimum" was wrong** — two- and three-handed are real games,
and `RoundEngine.MinimumPlayers` is still 4 (§10 #7), **so the two strictest rules in §7.1.1 are
correct, tested and unreachable from a dealt game.** That is not P25's to fix and no packet owns
it.

🔥 **And a third settled rule has no code behind it, added 2026-08-21: seats are re-randomised
every round** (`RULES.md` §3, §9 #14, `EXPERT`). A *game* means from the turn-up to somebody going
out — **a game is a round** — and the seating is re-drawn between games, so a player's neighbours
change every deal. ⚠️ **`MatchEngine` randomises once at setup and keeps that seating for a whole
match**, which is what P9 chose while the question was open and the recommendation was *nothing
moves*. Recorded as `RULES.md` §10 #16. ✅ **No published measurement moves** — every experiment in
`BurmesePoker.Sim` runs `RoundsPerGame = 1`, so no measured game has a second round to re-seat
for, and `--seating rotated`/`balanced` assign strategies to seats *within* a game. ⚠️ **The two
front ends are where it shows**, and it is not only a loop: P13.5 puts *you at the front whichever
seat you were dealt*, so re-seating every round means the table visibly rearranges itself around a
fixed viewer, and `TableRing` has never been asked to do that between rounds.

✅ **P26 built the money layer on 2026-08-21 and `RULES.md` §10 #17 is discharged.** Jokers are
permanent (`Permanent` holds three values and eight cards), a designation landing on a permanent
value pays **×3**, and the **×5** is `MoneyCardRegistry.Multiplier(card, owner, MoneyOwnership)`
under a configuration `Settlement` reads from `CardOwnership` **once a round**. 🔥 **The design
decision held**: nothing is stored on a card, `Multiplier(Card)` survives as the value-only
question — which is what every view drawing one card at a time is really asking — and that is why
no caller outside `Settlement` needed a parameter. ⚠️ **The ×5 is fenced to the 7♦/A♠ pair** while
§9 #32 is open, and two tests pin the narrowness so widening it later cannot pass silently.
⚠️ **No view can show a ×5** — `CardView.Multiplier` is 0, 1 or 3 by construction — so the jackpot
is settled and never drawn. **A real UX gap, and no packet owns it.**

🔥 **What P26 measured, and it is bigger than the packet expected.** The side-bet went from
**`$8.50` to `$11.58 ± 0.34` a round** at five seats — **42.5% → 58% of the $20 round prize** —
measured with the *same* command before and after (`--games 600 --seats 5 --seed 20260821`, greedy
vs simple, summing each round's positive `side_bet` deltas out of `--csv`). ✅ **The instrument is
validated rather than asserted**: the *before* run reproduces P12's rev-13 published figures
($8.43 / 42% / 30%) at a different seed. ✅ **And play did not move at all** — same seed, same
wins, same turns, same cover — which is the first time §4.4's decoupling claim has been measured
*across* a change to the money layer. 🔥 **The packet's own stated prediction came out right**: a
designation on the 7♦ and one on an ordinary card leave **exactly the same** money loose in the
shoe, so §4.1's arithmetic is sound and the rule did not need re-asking.

⚠️ **P26's one surprise is that "domain only" was true of the logic and not of the text.** `($$)`
had to become `($$$)`, `CardDisplayState.PaysDouble` became `PaysTriple`, and four user-facing
strings said *double*; leaving them would have printed a false number to a player. 🔥 **So a
`drive-console.py` capture from before P26 no longer compares** — P23's promise that a pre-P23
capture still does is spent.

🔥 **And on 2026-08-21 the money layer's last two answers added a fourth unbuilt rule and broke a
shipped test.** *(The record of what P26 was built from.)* `RULES.md` is **rev 20**. **(1) Claiming the turned-up money card needs the
permission of the seat that plays *before* you**, who may refuse only if they hold that card —
because your public take arms §5.1 against them and locks them into holding it (§4.5, `EXPERT`).
**The first rule tying the money layer to the feeding ban**, and it independently confirms §9 #16
and #17, which were recommendations when they were written. ⚠️ **It needs a third kind of agent
decision — *do I object* — asked of a seat that is not on turn**, which no `SeatPrompt` in
`BurmesePoker.Server` does; and **the answer is a disclosure**, since only a holder may object
(§10 #18). **(2) A turned-up 7♦, A♠ or joker can never be owned, claimed or not, and its partner
copy pays ×3** — superseding the *double* recorded from the same people one day earlier.

❌ **This is the first rules change that invalidates a published measurement.**
`MoneyCardRegistry.Multiplier` is `(permanent ? 1 : 0) + (designated ? 1 : 0)` and caps at **2**;
it must return **3**. `MoneyOdds.PerBlindDraw` prices a blind draw from it, `prospector` is the one
rung whose decision reads the money, and **`docs/STRATEGY.md` §10 — the money sweep, twelve rows of
`measurements.csv` — was measured under the struck rule.** ⚠️ **P22's `DERIVED` note is withdrawn**
(*a designation landing on a permanent money card leaves less money in the deck*) — the reasoning
was sound and the premise moved. ✅ **`ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe`
asserts the withdrawn direction and will go red**, which is the good case: the derivation was
written as an assertion rather than as prose, so the rules change cannot pass silently. 🔥 **Its
last assertion should invert to an equality** — a designation on the 7♦ and one on an ordinary card
leave the same money loose — **and that is a prediction, not a measurement.**

⚠️ **§9 has two questions and both were raised by rev 20's answers**: whether an objection turns on
the exact card or the **rank** (#30 — §5.1 is rank-only, and the stated reason covers a rank-mate),
and whether a turned-up **joker**'s partner is really ×3 (#31 — the same multiplier *conserves* the
money on a 7♦ and *creates* it on a joker). **`QUESTIONS-FOR-MYA-LAY.md` Q8 carries both, and says
to ask #31 without mentioning the 7♦.** See **Decisions needed from Nick**.

🔥 **P23 is done (2026-08-20): the standing answer, and the last packet in the plan.**
`docs/STRATEGY.md` is one document that answers *"which bot should I play, and what actually works
in this game?"*, and it regenerates from one command. **Read `STRATEGY.md` before anything else if
the question is about strategy; read this block if the question is about how it is kept true.**

🔥 **The headline is the reproduction. 59 of the suite's 77 rows came back byte-identical.** Every
ladder cell, the harness's own null test, all twelve pairing ratios and both of P12's headline
rows — from a tree that had since gained a rung and changed what the standing field is. **The
seven rows that moved are the difficulty dial and only the dial**, which is the one thing this
packet meant to move; the twelve that are new are the money sweep, which §10 now quotes from
`measurements.csv` instead of from a file of its own. **That is the join P22 left open, closed.**

🔥 **The re-calibration moved exactly one value, and that is the finding.** P19 placed the four
levels against `greedy`; P21 re-based them onto `outs` without re-spacing, leaving a reference
table with steps of **8.2 / 4.3 / 10.3** points — monotone, passing every standing check, and
visibly not a dial. Re-sweeping ε on `outs` over P19's own seven probes and inverting the curve
moved **`hard` from ε = 0.5 to ε = 0.4 and left `easy` (0.9), `medium` (0.7) and `expert` (0.0)
alone**; the reference table is now **7.9 / 6.7 / 7.7** and the head-to-head steps are
**+11.20 / +9.80 / +6.82**, ± 1.00, all three surviving Holm at 6.8× the half-width or better.
⚠️ **Why only one: the ε curve has nearly the same shape on a rung that looks ahead as on one that
does not** — 0 → 0.5 costs 9.5 points on `outs` against about 8 on `greedy`, and 0.5 → 1 costs
16.5 against about 17. **A mistake rate is close to being a property of the mistake rather than of
the rung it is made against**, so the next rung to raise the ceiling should expect to *re-check*
this and not to re-derive it — but not to skip it, because skipping is what produced the flat spot.

🔥 **Two instruments, one dial, and they do not agree about spacing.** The reference table's steps
are 7.7 / 6.7 / 7.9 with the **middle** narrowest; head to head they are 6.8 / 9.8 / 11.2 with the
**top** narrowest. They are different measurements — a head-to-head cell holds two levels and a
reference table four, and two strong players deal a longer round than two weak ones. The re-fit
improved *both* (reference spread 6.0 → 1.1 points, head-to-head 5.1 → 4.4), so nothing had to be
chosen; ⚠️ **`STRATEGY.md` §9 records the tie-break for next time — the reference table wins**,
because a person who asks for a mixed table is sitting at exactly it.

🔥 **How P22's bill was paid, and it was not by typing a shorter field.** `BotRung.Ranked` is new:
each rung declares whether **win rate** or **money** settles it (`RankedOn`), `BotCatalog.Ladder`
and `BotCatalog.StakesSensitive` are **filters of `All`**, and the ladder tournament measures one
while the money sweep measures the other. So `prospector`'s six duplicate cells are gone and
**a rung still cannot be added without being measured**. ⚠️ **The wall clock was the smaller half
of the argument**: six null cells in a Holm family make **every real verdict in it harder to
reach**, and a duplicate is not a free row. ✅ **Measured rather than argued** — the run without
them reproduced every ladder figure to the digit.

⚠️ **Acceptance 2 is a test now, and it caught the tree in the state it was warning about.**
`Tests/Sim/StandingAnswerTests.cs` reads `docs/strategy/measurements.csv` and fails the build if
the published ε values are not the ones `DifficultyLadder` offers; if a published step did not
survive Holm; if a rung in `BotCatalog` is the subject of no published row; if the ladder and the
sweep are not between them the whole catalog; or if a front end writes out a level's or a rung's
description rather than asking for it. 🔥 **On the first run it failed on `prospector`** — exactly
the drift §11 had been recording in prose. **A default is not a guarantee**: P20 made the field
default to the catalog and nothing went red when nobody re-ran the suite.

⚠️ **Three things a future session should know.**
1. **The suite is five hours** (17,539 s for 77 measurements; 105 min at P21 for a smaller
   standing set). ⚠️ **Superseded by P29: it is 9,981 s — two and three quarter hours — with a
   cell added.** **The structural saving has been taken and there is not a second one.**
   `--pairs adjacent` on the ladder would take fifteen cells to five and **throw away §3's
   matrix**, which is the document's centre — it stays available and stays unused.
2. **Re-spacing the dial is not a play change and it was proved byte for byte.** `expert` is
   ε = 0 and `FallibleAgent` defers entirely at ε = 0, so a capture at `--pick 0` from `HEAD` and
   from this tree is **identical from the `Seating:` line on — 7,025 bytes (`--script bots`) and
   88,805 (`--script human`)**. Only the prompt above that line differs, because `hard`'s
   description reads *two times in five* now. ⚠️ **A capture from before P23 still compares**,
   which is not true of one from before P21.
3. 🔥 **ε = 0 and ε = 0.1 are indistinguishable** — 33.6% against 33.7%, ±1.6 on each. *The best
   card and the second-best card are usually the same card*, which is why the top of the dial has
   to be ε = 0 and why a fifth level between `expert` and `hard` would be two names for one
   opponent.

🔥 **P22 is done (2026-08-20) and the answer is "no, and here is how far away yes lives".**
`prospector` is `outs` with one change: a card taken from anywhere but the deck — the previous
player's discard, or the turned-up money card — must be worth more than the **ownership** a blind
draw would have conferred (RULES.md §4.4). At **$5/$1 the rule never fires at all**, and that is
an identity rather than a measurement: two tables, one rung each, dealt from the same shoes, play
the same rounds card for card. So the standard-stakes head-to-head cell is a **null cell**, and
at **`+0.01 ± 0.22`** a round it is the tightest one this harness has produced.
🔥 **And the yes, which is why it is a sweep and not a cell.** At **$5/$40** — a money card worth
eight rounds — `prospector` all but stops taking (**0.1%** against `outs`' 24.9%), wins **20.1 ±
0.9 points fewer rounds**, and banks **`+7.34 ± 3.29` a round**, surviving Holm at `p = 1.3e-05`.
The four cells are monotone in the stakes: $5/$10 is `−0.86 ± 0.82` (raw only), $5/$20 is
`+0.95 ± 1.63` (break-even, inside the interval). ⚠️ **This is the programme's first published
divergence between money and win rate** — at $5/$40 a reader ranking by win rate would rank the
better player last — and P12 split the two columns three packets before anybody needed them
apart. `docs/STRATEGY.md` §10 carries it; `docs/strategy/money.csv` is the data.

⚠️ **The bill P22 leaves behind is P23's, and it is the biggest single thing to know.**
`prospector` is one entry in `BotCatalog` (P18), so the suite's ladder field picks it up **by
construction** — **21 head-to-head cells against 15**, taking `sim suite` from an hour and three
quarters to about **three and a quarter hours**. It was therefore **not re-run**:
**`docs/strategy/measurements.csv` is one rung behind the catalog**, §10 of `STRATEGY.md` is
generated from `money.csv` instead, and §11 says so out loud. 🔥 **Six of those 21 cells are
`outs` against itself in all but name** — about three quarters of an hour reproducing a fact a
unit test already asserts — which is an argument for `--pairs adjacent` on the ladder as well as
the dial. ⚠️ **Do not fix it by hand-typing a shorter field**: that is the exact defect P18 and
P20 each had to remove, one layer apart.

✅ **The difficulty dial did not move and needed nothing.** `prospector` shares `outs`'
`Strength: 3`, and `BotCatalog.ByStrength` breaks the tie in ladder order, so
`BotCatalog.Hardest` is still `outs` — every level is still built on the rung P19 measured, the
stand-in seat and the browser's hint are unchanged, and no front end needed a line. A console
capture from before this packet still compares.

🔥 **Two findings worth more than the rung.** **(1) A rung's strength stopped being a property of
the rung.** Every rung before this plays the same game whatever the table is played for;
`prospector`'s one decision reads `Stakes.MoneyCardValue` against `Stakes.RoundValue`, so *"how
good is it"* has no answer until somebody says what the stakes are — which is why it shares
`outs`' ordinal rather than sitting above or below it. **(2) A `DERIVED` rules note fell out of
the arithmetic: RULES.md is rev 15.** A designation that lands on a **permanent** money card
leaves the deck with *less* money in it, not more — turning up a 7♦ makes that value a double but
takes the physical card out of the deck (§3 step 4), so one 7♦ worth $2 is left where an ordinary
designation leaves a partner worth $1 **and** the 7♦s untouched. **Doubling one value is not the
same as designating a second.** Found by writing a test that asserted the opposite.

⚠️ **Two things that will catch the next session out.** **(1) The $5/$1 identity cannot be shown
from a head-to-head cell**: two labels of one player sit in different seats there, so their
aggregates differ by seat luck however identical they are — the small-`n` version of that test
failed exactly this way before being rewritten around two *homogeneous* tables. **(2) The
exchange rate is the rung's one free parameter and it is a constant on purpose** — a rung with a
knob is a family of rungs, and a family cannot be measured against the one below it in a single
cell (P15). It is documented as a model and its bias is stated: it overvalues early melded cards,
so it takes the discard more often than a sharper model would, which moves the crossover **down**
rather than up.

🔥 **P21 is done (2026-08-20) and the answer is yes — the first rung in the programme that
beats `greedy`.** `outs` is `greedy` with one thing inserted: where two discards leave the hand
equally melded, it keeps the thirteen that **more of the pack would improve** — a count, per
candidate, of how many of the values still out there would raise the cover count of what is left
(`Domain/Agents/OutsBotAgent.cs`, `LiveOuts.cs`). At 8,008 games a cell it measures
**`+3.1 ± 1.0` points against `greedy`**, `p = 1.9e-09`, surviving Holm, and takes the
free-for-all **26.3 ± 0.5 to 23.7 ± 0.5**. Three research packets had produced nothing above
`greedy`; this clears the apparatus's resolution three times over.

🔥 **The *why* is a one-line contrast, and it is what P22 inherits.** `cautious` and `counting`
both put their new idea **underneath** `CoverScore.Potential` — they decide only what greedy had
already given up on, and that residue is worth about half a point. `outs` puts its key **above**
it: where the cover count ties, the outs count decides, and greedy's own tie-break is demoted to
breaking *its* ties. ⚠️ **P20's "change which question is asked" turns out to mean "and ask it
earlier than the question you are replacing."**

⚠️ **It is `Strength: 3`, and that re-bases the difficulty dial.** Every level is
`BotCatalog.Hardest` with an ε, so `easy`/`medium`/`hard`/`expert` are all `outs` now, and the ε
values **0.9 / 0.7 / 0.5 / 0.0 are no longer the ones that were measured** — they were spaced
against `greedy`. The dial is still *ordered* (`DifficultyCalibrationTests` at the reference
table, and the suite's standing monotonicity check), so nothing is broken and nothing fails;
what is stale is the **spacing**, and **P23 owns re-spacing it — it is no longer optional.**
⚠️ Re-run P19's ε sweep first: ε was violently non-linear on `greedy` and there is no reason the
curve has the same shape on a rung that looks ahead. ⚠️ **A console capture from before P21 no
longer compares**, because `expert` is by definition the strongest rung and the strongest rung
changed.

🔥 **The cost was the packet, and the profile was the surprise.** The naive rung came in at
**12.6× a greedy round**, over the budget the packet set itself. Four things, every one of them
*around* the evaluator and none inside it, brought it to **8.2×**: refine only the candidates
already tied at the top (**7.1 a turn, not 13**); prune values that could not enter a meld at all
(**34 searched of 53**); ask the search for a **bar to clear rather than a maximum**
(`PartialCover.CoversAtLeast` — the search fell from ~98 µs to ~10 µs); and build **one meld
index per candidate instead of one per probe** (`CoverProbe`). ⚠️ **`PartialCover.Best` was not
touched and `HandEvaluator` does not know any of it exists.**

🔥 **And then the finding that outlives the rung.** With the search cheap, **three quarters of
what was left was candidate generation — and it was a fixed cost that did not depend on the hand
at all.** `RunGenerator` allocated all ninety rank windows afresh for each of four suits on every
call, and both generators walked suits and ranks that could not hold a meld. A precomputed window
table, one slot buffer a length, and two feasibility checks made `PartialCover.Best` — and so
**every rung, every hint and every engine turn — about 45% faster** (`greedy` went 48.8 → 71.8
rounds a second serial, in the same process). **§3.7 item 4 said allocation was the thing to
attack and was right; what it got wrong was where.** ✅ **Proved to be a refactor and not a
change**: `scripts/drive-console.py` captured both scripts at `--pick 0` from `HEAD` and from
this tree before the promotion, **identical byte for byte, 8,763 and 92,172 bytes.**

⚠️ **Three things to know before touching this rung.** (1) **`OutsCache` keys on card *values*
and never on a `CardId`**, which is exactly why it may outlive a deal where `counting`'s memory
may not (P13.4) — and it earns only a **9% hit rate**, so it is kept for being free rather than
clever. (2) **Only the discard changed**; taking, claiming and declaring are greedy's, so the
difference attributes to one decision (P15). (3) 🔥 **The prune is a claim and is asserted, not
argued**: `ThePruneNeverThrowsAwayARealOut` counts the outs the long way — every value, through
`PartialCover.Best` — and demands the same number, and the same discipline covers the bar search,
the probe and the cache. That is P21 acceptance 3, and it is the reason to trust any of the
speed-ups.

🔥 **The promotion broke two concealment tests, and they were wrong rather than the code.**
`ConcealmentTests` (P13.2) and `SeatBoardTests` (P13.4) both asserted that four seats' hands,
taken over a whole round, are **pairwise disjoint by `CardId`**. With `outs` standing in for the
unanswered seats the fixture's round ran **67 turns and exhausted the draw pile**, so the
discards were shuffled back into it (`DiscardsReshuffled`, 54 cards, RULES.md §5) — and a king
one seat threw away on turn nine became a king another seat drew on turn fifty. ⚠️ **Public on
the way out, private on the way back, and a breach at neither end.** Both tests now allow exactly
the cards the table itself handed round (`Tests/Server/PublicRelease.cs`, which carries the whole
argument), and the strict form of the same rule —
`ConcealmentTests.EveryCardASeatIsSentIsOneItMaySee`, which asks what the fan-out *sent* rather
than what a hand happened to hold — **never needed relaxing and was green throughout.**
⚠️ **The lesson is not about concealment.** It is that a test over a *played round* can be
asserting a property of that round's length, and nothing says so until something changes how
long rounds are. **A stronger bot is a longer round.**

⚠️ **The suite is now an hour and three quarters** (105 minutes; 35 before P21), and 65
measurements. Head-to-head is `k(k−1)/2`, so six rungs is fifteen cells against ten; `outs` is in
five of them at ~8× a `greedy` round, and the dial's four cells went up with it because every
level is `outs` now. **`--pairs adjacent` is the escape** (P19) if it stops finishing in a
sitting. ⚠️ **The local test suite went the same way for the same reason** — 2m 16s → 6m 33s.

⚠️ **`Domain` now has an `InternalsVisibleTo BurmesePoker.Tests`, and it was needed.** The
prune, the cache and the probe search are *how* a rung is written rather than what it does, so
they are internal — but each is a shortcut past a search that already works, and a test cannot
assert what it cannot see. `BurmesePoker.Tests/Play/TurnContexts.cs` uses the same access to ask
a rung one question without a round around it.

✅ **P20 is done (2026-08-20) and the answer is no — a null result, published.** `counting` is
`cautious` with a **memory**: it estimates what is left in the shoe from every card it has been
shown this round rather than from its own thirteen. At 8,008 games a cell it measures
**`+0.3 ± 1.0` points to `greedy`'s side of the margin** — not separated, and the point estimate
pointing the *wrong way*; `cautious` is `+0.8 ± 1.0` ahead of it. `docs/STRATEGY.md` §8 carries
the entry, which is what P20 acceptance 1 asked for in advance.

🔥 **The finding is *why*, and it is the constraint P21 inherits.** The memory works — a test
shows the supply estimate falling below `cautious`'s for every card watched go by and holding at
the full two copies for every value never shown. It cannot pay for two measured reasons.
**(1) The information set is tiny.** Under the cautious default the memory runs **12 → 23 cards
across a whole round, out of 108** — about ten cards learned beyond its own hand, roughly one a
turn, a fifth of the shoe. **(2) It enters where nothing is paid.** It sharpens `ThreatScore`,
which *is* `cautious`'s tie-break, and P17 had already measured that tie-break at `−0.2 ± 1.0`.
⚠️ **A sharper input to a decision rule already shown not to matter is worth nothing, and the two
nulls compound rather than add.** So P21 must change **which question is asked**, not improve an
answer to a question that does not matter — which is exactly what P15's *"has to be
combinatorial"* meant. **Two of three research rungs have now returned nothing.** ✅ **P21 was the
third and it separated** — see the block above; the prediction in this paragraph was right about
*what to change* and wrong about the odds.

⚠️ **Two things P20 did that were not in its build list.** (1) **`ThreatScore` was extracted from
`CautiousBotAgent` unchanged**, because the two rungs differ in exactly one thing — a `Supply`
delegate — and two copies of that arithmetic would be two places for it to drift; P15's "one
change against the rung below" is a claim about code before it is a claim about results. (2) 🔥
**The ladder was written out in three places and a fifth rung made all three wrong at once**:
`tournament` and `suite` both defaulted `--strategies` to a hand-typed
`random,simple,greedy,cautious`, so a new rung was measured only when somebody remembered to name
it. **The default is now `BotCatalog` itself** — P18's defect one layer up, in a front end nobody
thinks of as one, and it moves half of P23's job forward.

✅ **The rules question was not decided in code.** RULES.md §9 #15 — *is a discard pile
inspectable, or only its top card?* — stays open at **rev 14**, `QUESTIONS-FOR-MYA-LAY.md` carries
it as a flat table situation, and the bot counts only what it has been shown: **wrong in the
direction that does not cheat.** ⚠️ **If the answer comes back that the piles may be read, this
rung gets a far larger information set and deserves re-measuring before it is written off** —
the 12 → 23 figure is exactly the cost of the cautious default.

✅ **Acceptance 5, throughput: the memory is free.** `counting` does **77 rounds/s** against
`cautious`'s **76** and `greedy`'s **88**, at 2,000 games, 4 seats (P12's baseline: 51 serial,
85–92 parallel). Only the idea costs anything.

⚠️ **The suite is 52 measurements in 35 minutes now** (41 in 34 before), and it is `k(k−1)/2`:
five rungs is 10 head-to-head cells where four was 6. **P21 makes it 15, and `outs` is the
expensive rung** — check the suite still finishes in a sitting before promoting it, and remember
`--pairs adjacent` exists. ✅ **Discharged, and the warning was justified**: P21 took it to 65
measurements in **105 minutes**, which is still one sitting and will not survive another rung
like it.

✅ **P19 is done (2026-08-19): the difficulty dial, calibrated — and §0's fifth goal now has its
*product* half finished.** `BurmesePoker.Domain/Agents/DifficultyLadder.cs` holds four
`DifficultyLevel`s — `easy`, `medium`, `hard`, `expert` — each the **strongest rung there is
(`greedy`) with a measured mistake rate**: with probability ε it throws the card that rung ranked
*second* rather than the one it ranked first, and nothing else about it changes.
`FallibleAgent` is the decorator, `IRanksDiscards` is what makes a plausible mistake possible,
and `CoverScore.Discard` is now **defined as the head of `CoverScore.Ranking`** rather than as a
second loop that agrees with one. Both front ends offer **levels only**; `BotCatalog` stays the
research instrument and is what the harness ranks (§3.12).

🔥 **The calibration, and it is the packet.** ε turns out to be a far bigger dial than anything
on the skill ladder — `greedy@0` against `greedy@1` is **+33.3 ± 1.6** points head to head,
three times the whole `simple`-to-`greedy` gulf — **and violently non-linear**: the sweep put
ε = 0 → 0.5 at about **8** points of win rate and ε = 0.5 → 1 at about **17**. ⚠️ **So levels
spaced evenly in ε would not be spaced evenly in play**, and the shipped values are spaced by the
measurement instead: **0.9, 0.7, 0.5, 0.0**. At the reference table (all four at one table, 256
assignments, 8,192 games) they measure **14.50 ± 0.69 / 22.17 ± 0.79 / 27.22 ± 0.83 /
36.11 ± 0.87**, and head to head at 8,008 games a step the margins are **+10.79 ± 1.00**,
**+5.69 ± 1.01**, **+9.90 ± 1.00** — **every one separated under Holm**, the narrowest of them
5.7× the half-width. **Four levels rather than three, and the count is a measurement.**

⚠️ **`--pairs adjacent` was needed and was not in the plan's build list.** Acceptance 1b asks
that the family be the k−1 steps rather than the round-robin — only "n+1 beats n" is a claim the
menu makes — so `TournamentOptions.Pairs` decides which pairs are *played* as well as which are
corrected, and a four-level dial costs three head-to-head cells instead of six. 🔥 **It found a
latent assumption**: `PairingChecks` read the *field* to choose the two cells it compares across
and threw `Sequence contains no matching element` on a run where most pairs never met. It reads
the **cells that were played** now, which is the same answer for a round-robin.

🔥 **A `TurnContext`'s hand is the engine's own list, and P13.1's finding arrived in the test
project.** The obvious way to test "the mistake is the card the rung ranked second" is to keep
the context and ask the rung for its ranking afterwards — which asks it about the **thirteen**
that were kept rather than the fourteen it chose from, and yields a confidently wrong expected
card. `RecordingAgent` records the ranking **during** the turn now. Found by writing the test.

⚠️ **The mistake has exactly one site, deliberately.** Taking the discard, claiming the turned-up
card and declaring are all strict-improvement or must-answer questions with no plausible
second-best, so ε is one dial rather than three — which is what makes "about seven points a step"
attributable to anything. A `FallibleAgent` **refuses an inner player that cannot rank its own
discards**, so `random` can never carry an ε that silently does nothing.

✅ **Acceptance 3 verified end to end.** `expert` (ε = 0) plays the match `greedy` plays, byte
for byte: **7,371 bytes for `--script bots` and 90,726 for `--script human`, identical from the
`Seating:` line on**, captured from `HEAD` and from this tree with `--pick 0` in both. ⚠️ **The
`--pick` map has changed**: it is now `0` expert, `1` hard, `2` medium, `3` easy.

⚠️ **`sim suite` gained a second standing check.** It already exited non-zero when the harness's
null test failed; it now also fails if **the dial stops being monotone** — a rung that raises the
ceiling (P20–P22) raises every level and moves the calibration, so the menu cannot go stale
quietly. The suite is **41 measurements in 34 minutes at `--games 8000`** (~120,000 games), and
**every ladder figure reproduced P17's exactly**.

⚠️ **Three things only pressing the buttons found** (§3.11 B — "only playing it finds these").
(1) The mixed-table **checkbox row inherited the form's ten-rem label column** — its label is the
question rather than the caption, and it wrapped onto three lines; `.opening .mixed` is the
override. (2) 🔥 **`--mixed` is silently ignored without a value.** The command-line
configuration provider records a switch only when it carries one, so the site booted, said
nothing, and gave every seat `expert`; it is **`--mixed true`**, exactly as `--hints false` is.
(3) A seat name is truncated with an ellipsis at the panel's width, and `Aung Aung (medium)` now
reaches it — the whole name is on a `title` so a hover says it, which helps every long name and
not only these.

⚠️ **Two things P19 deliberately left.** (1) A computer seat in the browser is now named for how
it plays — `Mya Lay (expert)` rather than `Mya Lay (bot)` — because a mixed table nobody can tell
apart is a mix nobody asks for twice; the console's bot names are untouched, which is what keeps
the byte-comparison above meaningful. (2) The console offers one level for the whole table; only
the browser can open a **mixed** one (`--mixed`, or the lobby form's checkbox), which is all
acceptance 6 asks for.

✅ **P17 is done (2026-08-19): the tournament✅ **P17 is done (2026-08-19): the tournament, and the first honest interval this harness has
printed.** `BurmesePoker.Sim -- tournament` plays every unordered pair head to head, reports the
free-for-all beside it, ranks the field, and puts every margin through a **Holm–Bonferroni**
correction; `-- suite` generates `docs/strategy/measurements.csv`; `docs/STRATEGY.md` is created
and quotes that file. **Two independent 15-minute, ~64,000-game runs of the suite wrote a
byte-identical file.**

🔥 **The finding that changed the code, and it was found by reading a number that should not have
moved.** P17 says "every figure becomes a `Measurement` — a mean over games, one value per game".
Built literally, the balanced headline came back **30.6% / 19.3%** where P16 published
**29.6% / 20.4%**. Neither is wrong: they are **two different estimands**. A strategy holds a
different number of seats in different games of a crossed run, so *the unweighted average of the
per-game ratios* is not *the ratio of the totals* — and it over-weights the games where the
strategy held fewest seats, which for a strong strategy are the games it does best in. ⚠️ **The
gap was 1.05 points, the size of P16's entire rotated-versus-balanced effect.** Adding an interval
to a figure must not change the figure (§3.8 item 4), so `GameValue` carries **a total and the
trials it is out of** and `Measurement.Of` is the **ratio estimator** — totals divided, with the
standard error built from the per-game residual `total − ratio × trials`. The game is still the
trial. Where the denominator is constant it reduces *exactly* to the per-game mean, and the
balanced headline now reads **29.75 ± 0.47 / 20.25 ± 0.47** — P16's number, with an interval on
it for the first time.

🔥 **"Paired is narrower" was half backwards, and the correction is a fact about this game.**
Pairing measures the correlation that is actually there. **Across cells** — one strategy at two
tables of one master seed — the shoes are shared, the correlation is positive, and the interval
**narrows** (ratios 0.57–0.95). **Within a cell** — two strategies at the *same* table —
**exactly one seat declares**, so the series are strongly negatively correlated and the interval
**widens**: measured at **1.408, 1.409, 1.414** across the three cells of thinking players, which
is √2 to three digits. ⚠️ **So the add-the-variances formula is *anti*-conservative on a
head-to-head margin, by 41%.** BUILD-PLAN P17 acceptance 3 is amended to say so.

**The measured answer, 8,008 games a head-to-head cell** (`docs/STRATEGY.md` has the matrix):
`random` loses by **49.7–49.9 ± 0.4**, `simple` loses to `greedy` by **11.2 ± 1.0** and to
`cautious` by **10.8 ± 1.0**, and 🔥 **`greedy` vs `cautious` is `−0.2 ± 1.0`, p = 0.70** — the
strongest confirmation of P15's negative result yet, from a design P15 never ran. **Three
separated skill levels, four rungs.** The ranking is `greedy` +20.30, `cautious` +20.28, `simple`
+9.25, `random` −49.83 mean margin; the free-for-all (8,192 games, 256 seatings) is 35.8 ± 0.9,
36.5 ± 0.9, 27.3 ± 0.8, 0.1 ± 0.1. ⚠️ **`cautious` is ahead of `greedy` in one table and behind
it in the other, both inside the interval** — which is precisely what the correction exists to
stop a reader promoting to a rung.

✅ **The harness's own null test passes and is checked on every run.** `cautious` against a copy
of itself under another name: **25.1 ± 0.5 and 24.9 ± 0.5**, margin **+0.3 ± 1.0**, against a
fair share of 25.0%, with each label sitting in each seat exactly 4,004 times. `sim suite` exits
non-zero if it ever fails.

⚠️ **What P17 can and cannot resolve, which is what P19–P22 have to budget against.** At 8,008
games a cell the 95% half-width on a margin between two thinking rungs is **1.02 points**. **A
rung worth less than about a point is not promotable at the default run size**, and half a point
costs about **34,000 games a cell** — four hours, and very nearly the 32,000 P15 had to run.

⚠️ **Three things P17 deliberately left.** (1) `neighbours` still uses `Measurement.Difference`,
whose own remark admits it is being conservative about a pairing it could take — `Paired` exists
now and would narrow P16's intervals, but re-running would change published numbers and belongs
with a re-measurement. (2) `NeighbourCell.TakeRate` averages per-game ratios unweighted, the
estimand P17 moved away from everywhere else; its *win* rate is unaffected, because a cell has
exactly one focal seat and so a constant denominator. (3) The suite's standing set is a hand-kept
list in `Suite.Run` — P23 owns making that a test.

⚠️ **One display defect fixed in passing**, because it is one shared helper and changes no
number: a .NET **two-section format picks its section from the *rounded* value**, so `-0.04`
under `"+0.0;-0.0"` prints as **`-+0.0`** — the positive section supplies the plus and the
runtime prepends the minus. `ReportNeighbours` had it in every signed column. Signed figures are
built by hand now.

**Everything built so far is done.** P0–P12, P13.1–P13.6 and P14–P16, and **all four of §0's
original goals are delivered**: solo play against the computer (P10), a console worth sitting at
(P11), simulation at scale (P12), and — as of P13.6 — **a multiplayer browser app with AI
seats.**

🔥 **A fifth goal was stated on 2026-08-19 and planned the same day: a designed difficulty
system, and a settled answer to what actually works.** It is **P17–P23**, and it is two jobs
kept deliberately apart — a *product* (difficulty as a table setting, per seat, in both front
ends) and a *programme* (enough analysis and simulation to say which ways of playing are better,
by how much, with an interval). **§3.12 is the design decision taken in advance**: *difficulty is
a dial, skill is a ladder, and they are not the same axis.*

**The two facts that shaped the plan, both established while writing it.**

1. 🔥 **The default report prints no interval at all.** A balanced 2,048-game run on 2026-08-19
   gave `random` 0.0%, `simple` 26.7%, `greedy` 36.1%, `cautious` 36.8% — and the last two are
   two numbers P15 needed **32,000 games** to establish are the same number. `Measurement`
   exists and is used by exactly one verb (`neighbours`). **P17 comes first because a ladder
   calibrated on point estimates is calibrated on nothing.**
2. ⚠️ **There are four independent notions of *which bot*** — `Sim.StrategyCatalog`,
   `Console.Program.Difficulty` (a private two-value enum), `Web.HostedTable` (`new
   GreedyBotAgent()`, hard-coded, twice) and `Server.TableOptions.StandIn`. **The browser has no
   difficulty setting at all.** P18 makes it one list, so every later rung reaches all three
   front ends for free.

⚠️ **P19 is where the difficulty system is finished** — with the rungs that exist today.
**P20–P22 are independently droppable, in preference order**; each adds one rung and one measured
answer, and P15's precedent (a plausible rung worth +0.5 ± 0.55 points) is why none of them is
allowed to be load-bearing.

✅ **P13.6 is done (2026-08-19): the lobby, a second person, and §0's goal 4.** `dotnet run
--project BurmesePoker.Web` opens a lobby at `/`, a table at `/table/{id}`, and **two people and
two bots play a round over a network** — P13's original "done when", reached with everything
under it already tested. **Verified twice: in a test (`TwoPeopleTests`) and in the real browser,
two tabs, two seats, each shown its own hand and neither shown the other's.**

**What it changed rather than added to.**

- **`TableHost` is gone.** `Lobby` is the singleton and holds `HostedTable`s by id; `TablePlan`
  is what one is opened with. `TableView` injects the lobby and finds its table by the id its
  URL carries; **nothing else in the client counts tables.**
- 🔥 **A `SeatBoard` belongs to a viewer, which is the thing P13.4 said had to change.**
  `TableView` sits down, holds its own seat, hands it to `YourSeat` as a parameter, and stands
  up in `Dispose`. `YourSeat` no longer injects anything at all.
- **`TableSession` learned who is sitting where** — `SitDown`, `StandUp`, `RemoteSeats`,
  `WaitingFor`, `IsFull`, plus `TableEvent.SeatTaken` and `SeatLeft`. ⚠️ **The claim is the
  table's and not the lobby's**: two viewers handed the same `SeatConnection` are two people
  answering one question, and a seat is a property of a table.
- 🔥 **The table deals while somebody is at it** — at least one viewer attending *and* every seat
  either the computer's or somebody's. That is P13.4's leftover finally answerable, because a
  lobby knows whether anybody is connected. **The patience was not shortened**, which is what
  P13.4 asked for.

🔥 **Eight findings a cold context needs.**

1. 🔥 **A test that a stood-up seat refuses an answer is vacuous unless a question is standing in
   front of it.** The first version evicted a ghost at a table that was not dealing and asserted
   the ghost refused — which it did, because there was nothing to refuse. ⚠️ **Found by mutating
   `SeatBoard.Dispose` and watching the test stay green**, not by reading it.
   `AGhostCannotAnswerTheQuestionItWasLookingAt` runs a live round now and waits until the seat
   really is being asked something. **`_left` + `Asking = null` is red when both are removed**;
   either alone is redundant, deliberately — the flag closes an in-flight-notification race a
   test cannot schedule.
2. 🔥 **Two `<AntiforgeryToken />`s is worse than none, and the page renders perfectly either
   way.** `EditForm` emits one itself for a static SSR post; a second put two inputs of the same
   name in the form, the two arrived comma-joined, and every post was rejected with a bare error
   page. ⚠️ **Found by pressing the button.** P13.3's rule generalises from URLs to verbs: *ask
   the server for everything the page says it will do*, not only for everything it names.
3. 🔥 **A marker that means "away" has to stop meaning it.** `SeatPlayedByTheComputer` is
   broadcast once per turn, and marking the seat for the rest of the *round* told a player who
   had timed out once and come back that he was still away — in his own seat, while he was
   answering. **Cleared at that seat's next `TurnBegan`**, the only moment that means *we are
   about to find out*. ⚠️ **A seat somebody walked away from is a different fact**:
   `TableBoard.Vacated` survives the deal, and a seat that was always the computer's is marked by
   neither — **nobody has gone anywhere at a bot's seat.**
4. ⚠️ **The lobby's sit-down form must not hide itself when the table is full.** Blazor keeps a
   dropped circuit for minutes before disposing it, so somebody who reloaded finds their own seat
   occupied by their own ghost, and a form that vanished would lock them out of the table they
   are sitting at. **Sitting down under a name already here takes that seat back.** ⚠️ **A name
   is not a credential and the code says so out loud** — two people who type the same name take
   each other's seat. A lobby with accounts is beyond this plan.
5. ⚠️ **A claim on a table must not be made while the page is only being prerendered** (§3.11
   C13, which was a warning about *joining* and is now about two things). Being counted as
   present and taking a seat are both claims; both sit behind `RendererInfo.IsInteractive`, and
   `ComponentDisposalTests.NothingIsClaimedWhileThePageIsOnlyBeingPrerendered` asserts the order.
6. ⚠️ **A parameter handed to an interactive root component is serialised**, so `Table.razor`
   passes the table's **id** and the island looks it up. A `HostedTable` is not serialisable and
   must not be.
7. ⚠️ **A settlement is not a resting state.** With a one-millisecond gap between rounds the
   acceptance test caught round *seventeen*, by which time the 240 lines the log keeps had
   trimmed away the two lines saying who sat down. A table that stops between rounds is also what
   a person actually sees.
8. ⚠️ **`FocusOnNavigate` beats §3.11 B7 on a reconnect, and that is left alone.** Landing on the
   table mid-turn puts focus on the `<h1>` rather than the question in front of you, because a
   fresh page load is navigation and focus belongs at the top of a document you have just arrived
   at. B7 is about the turn *moving*; the prompt is four tab stops away. **Written down so it is
   not rediscovered as a defect.**

✅ **P13.5 is done (2026-08-19), and the browser client is a table rather than a document about
one.** Every seat sits at a position on a felt, **you are at the front of it whichever seat you
were dealt**, the shared things — the deck, the money cards, the discard on offer — are in the
middle, your hand is the big pressable thing at the bottom, the four questions are one action
bar, and the narration is a round log you open.

**What it added.** `BurmesePoker.Presentation/TableRing.cs` — the rotation, as a pure function
with thirteen tests, because *an expression buried in Razor is unreachable from a test*. Three
new components (`TableCentre`, `TableLegend`, `AboutTable`) and every other table component
rewritten. **420 tests, up from 389, none removed.**

🔥 **Six findings a cold context needs.**

1. 🔥 **A focus call can kill the circuit, and a dead circuit is a page that looks perfect.** An
   `ElementReference` keeps its id after its element has gone, so §3.11 B7's focus-on-turn races
   the answer: the question is answered, the buttons become spans, and the interop call lands on
   nothing — and **Blazor turns an unhandled interop exception into a torn-down circuit**.
   ⚠️ **Found by playing 1,429 turns and reading the server log, not by looking at the page**:
   the browser showed a hand, a prompt and a spotlight, and every card refused every press in
   silence. A quick human wins the same race. **Focus is best-effort now** and
   `MarkupStandardsTests` asserts it. **After the fix: 39 rounds, 473 answers, no failures.**
2. 🔥 **Whose turn it is is now public, and it was the packet's one open design question.**
   `IsActing` meant *moved last*, which a felt with seats at positions cannot spotlight without
   lying. **The easy road was available and was not taken**: `TableEvent.TurnBegan` is broadcast
   to everybody, raised by `BoundedAgent` (the one decorator every seat passes through, so a
   bot's turn is as public as a person's), once per turn. ⚠️ **`ConcealmentTests` was extended to
   cover it**, not merely left passing.
3. 🔥 **A hidden live region announces nothing** — and this was the packet that would have done
   it, since it is the one that demoted the log to a panel you open. The list is **always
   rendered and always live**; closing it clips it the way `.said` is clipped. Two mutations red.
4. ⚠️ **A hidden twin is a duplicate.** Three shipped before the accessibility tree was read
   back: *"Cobra, Cobra is waiting"*, *"Round log, open the round log, 1 lines"*, *"36 turns, in
   36 turns"*. **A `.said` is for text the eye gets as a glyph — never for text the eye gets as
   text.** Read the page's own accessibility tree once per UI packet.
5. ⚠️ **A glyph is not automatically better than a word.** The 🃏 character is a colour photograph
   at chip size; a jester's cap in SVG replaced it and was wrong **twice** — three lobes above a
   band is a *crown* (and ♛ already means *won the round* two panels away), and hanging the side
   lobes made it an abstract blob at true size. **It is the letters `JKR`.** Settled by zooming
   into a screenshot, which is the only thing that was going to settle it.
6. ⚠️ **The draw count and the discard piles are derived from the public game, not sent.** 108
   less thirteen a seat less the turned-up cards, one off per blind draw, reset by a reshuffle —
   a watcher could do the same arithmetic, and the alternative was changing `IGameObserver` for a
   decoration. And a discard **pile** rather than a top card, because a card leaves it again.

⚠️ **One thing P13.5 left, and P13.6 did not take it either.** The legend promises that a money
card you own pays you *even after you throw it away* (RULES.md §4.4) — and the felt cannot show
those cards, because `SeatPrompt` carries ownership only for the cards currently in your hand. A
running **"★ 4 owned"** tally on your seat would need the server to send it. **Still the one
named piece of unfinished business in the browser client.**

✅ **P13.4 is done (2026-08-19), and this project has a game you can sit down and play in a
browser.** `dotnet run --project BurmesePoker.Web` deals you into seat 1 against three bots, and
all four questions `IPlayerAgent` asks are controls: take the discard or draw blind, claim the
turned-up card, throw one away, declare. **Verified by playing it with no pointer at all** —
five rounds in a headless browser driven by `Tab` and `Enter`, 86 questions answered, 393 tab
presses walking the hand, banks carrying over and summing to zero.

**What it added.** `SeatBoard` — *your seat, as you have it* — is the private counterpart of
`TableBoard`: the question standing, the hand it is about, and the hand you kept between turns,
folded out of **your own** `SeatConnection` and out of nothing else. `TableHost` seats you
(`--seat 1`, `--seat 0` for P13.3's room with nobody in it, `--name`, `--hints`, `--patience`),
and three components draw it — `YourSeat`, `TurnPrompt`, `HandPanel` — **all inside P13.3's one
interactive island**, so §3.11 C12 is untouched.

🔥 **Five findings a cold context needs.**

1. ⚠️ **A `CardId` names a card in a round's shoe, and the shoe is rebuilt every deal.** The
   first cross-seat leak assertion compared whole matches and went red at once — on the same
   physical eight of hearts being dealt to two people in two different rounds. **Anything
   comparing hands across seats compares them a round at a time.** `ConcealmentTests` never met
   this because it plays exactly one round.
2. 🔥 **Your hand between turns is not stale, and it is worth rebuilding.** After you discard,
   your thirteen are fixed until your next turn — nobody draws from your hand, and what somebody
   takes comes off your discard pile. So the resting hand is the prompt's hand minus the card
   thrown with the near-melds worked out again. ⚠️ **Ownership is read back off the `CardView`s
   you were sent**, never recomputed: a `SeatPrompt` carries the money registry but not
   `TurnContext.YouOwn`.
3. ⚠️ **A refusal must not raise "something changed".** `SeatBoard.Answer` returning false while
   the question still stands is the ordinary case, and a client that answers on the change event
   — which is exactly what a component does — would answer the same refused question for ever.
   It says something only if the question really moved on. **Found by writing the test that
   answers wrongly on purpose**, which recursed until it was fixed.
4. ⚠️ **Only the first control may capture an element reference.** Blazor invokes a `@ref`
   capture when the element is *inserted*, not on every diff, so a `@ref` inside a loop ends up
   holding the **last** card. The first card is rendered through its own branch and captures
   there. That is what makes §3.11 B7 work: focus was already on a `<button>` at every one of
   the 86 prompts, before a key was pressed.
5. ⚠️ **The seated table no longer deals from boot.** Every question an unanswered seat is asked
   spends the whole of its patience before the stand-in plays, so an unattended round is over an
   hour of nothing. A table with nobody in it still deals from boot (P13.3's *a table is a
   place, not a button*); a table with a seat waits for the first page. **Do not fix that by
   shortening the patience** — P13.5's lobby knows whether anybody is connected, which is the
   honest answer.

⚠️ **What P13.6 has to change rather than add to.** `TableHost.Yours` is **one** `SeatBoard`,
built when the table is opened, and every circuit is handed the same one. Right for solo play,
wrong the moment two people are here: **a `SeatBoard` belongs to a viewer, not to a host.** The
component is already written for it — `YourSeat` reads its seat once and unhooks in `Dispose`.


✅ **P13.3 is done (2026-08-19), and this project has a UI.** `BurmesePoker.Web` is a **seventh**
project — Blazor Server, Domain + Presentation + Server — and
`dotnet run --project BurmesePoker.Web` deals round after round at a table you can watch in a
browser: seats and banks, the turned-up money cards, each seat's top discard, the narration as a
polite live region, and the settlement with the winner's melds and what each seat took home.
**Three of the four §0 goals were already delivered; this is the first half of the fourth.**

🔥 **Four findings a cold context needs.**

1. **The framework's own files are static web assets, and `UseStaticFiles` does not serve them.**
   The page rendered, looked finished, and was **dead** — `_framework/blazor.web.js` 404'd, so no
   circuit ever started. ⚠️ **A prerendered Blazor Server page is a perfect photograph of a
   broken one**: ask the server for every URL the page references. `MapStaticAssets` is the fix,
   and `Properties/launchSettings.json` is checked in because a build's endpoints manifest is a
   *Development* one.
2. **A trimmed log must not take its `@key` from the length of the log.** `TableBoard` keeps the
   last 240 lines; a sequence taken from `Log.Count` repeats the moment it trims, and a repeated
   `@key` is Blazor reusing the wrong DOM node. `TableBoard.Narrated` counts every line ever
   said.
3. **240 visually-hidden spans made the document 10,295px tall** for a page whose body was 1,814.
   The standard `position: absolute` recipe needs a positioned ancestor, or a hidden span inside
   a scrolled box is measured from the page. ⚠️ **Found by measuring, not by looking** — the
   screenshot was correct.
4. **A source scan must read the markup, not the prose about it.** Four of six scans failed on
   their first run, each on a comment in the file that was obeying the standard.
   `Sources.Markup` strips commentary first. And **a `@key` check that looks *nearby* is not a
   `@key` check** — the first version let a nested `<CardChip @key>` cover for an unkeyed `<li>`,
   and the mutation survived. **Eleven mutations now, eleven red.**

⚠️ **Two decisions P13.3 took that differ from what the plan named.**

- **`PacedAgent` moved to `BurmesePoker.Presentation`, not to `Domain/Agents/`.** The domain is
  the pure rules and a wall-clock sleep is not one — and, deciding it, **`BurmesePoker.Sim`
  references Domain**, so a sleep in there would be reachable from the hot loop P11 wrote a
  layering test to protect. Presentation is reachable from both front ends and neither harness.
  **The console is byte-identical after the move** (two pty captures, two seeds, `cmp`).
- **`BurmesePoker.Tests` now references `BurmesePoker.Web`** — and still never
  `BurmesePoker.Console`. The rule was never *"no front end"*; it is that nothing is tested
  **through** one. A Spectre console is an interactive loop needing a terminal, verified by a
  pty; a component tree is data, and §3.11 A5 asks for a reflection test over it by name.

✅ **The §3.11 A list is finished.** A1 shipped in P13.2, A2 in P13.1, and **A3 (computed
contrast, both themes), A4 (real controls) and A5 (disposal) shipped here** — plus C12, C14, C15
and B8 as scans. ⚠️ **The contrast test reads `wwwroot/theme.css`, the file the browser loads**,
and **discovers its own pairs**: `--on-x` needs 4.5:1 against `--x`, `--edge-x` needs 3:1, and a
token's base is the longest declared name it begins with. A token added later is measured
without anybody listing it.

✅ **P13.2 is done (2026-08-19).** `BurmesePoker.Server` exists — a **sixth** project, Domain +
Presentation, **and no transport at all** — and a round is played by two scripted remote seats
and two bots through the server's own plumbing, in a test, with no sockets involved. 🔥 **The
leak test landed with it, which is the point of doing this packet before any UI**: for one
round played by four connected seats with somebody watching, no seat is shown a card from
another seat's hand, no seat is told what anybody else drew blind, every card in every event a
seat receives is one it may see, and **a watcher is sent nothing but the public game.**
⚠️ **It was mutation-tested, not assumed** — broadcasting the drawn card to every connection
turns three of the five concealment tests red.

🔥 **Two findings a cold context needs before P13.3.**

1. **Exactly one event in the whole narration is private — the blind draw.** A discard, a taken
   discard, a claimed money card and a declaration all happen in front of the table
   (RULES.md §4.5, §5, §6.3, §7.1). So the security boundary is one `if` in `TableFanOut`, and
   everything else in that file is care about lists rather than filtering.
2. **P13.1's snapshot rule generalised, and it caught two more live lists.**
   `TableState.TurnedUpOnTable` is aliased by *both* `TurnContext.TurnedUpMoneyCards` and
   `IGameObserver.RoundStarted`, and **the opening player's claim removes a card from it**. A
   prompt or an event that kept the reference would stop saying what it said when it was sent.
   ⚠️ **Assume every list the engine hands over is live until shown otherwise.**

⚠️ **One thing P13.2 deliberately did not do, and P13.3 inherits it.** The packet said to wrap
the takeover bot in P11's pacing decorator so a taken-over seat reads as a seat playing. It
does not: a sleep belongs to whatever is *drawing* the table, not to a server that may host
many of them, so **`TableOptions.StandIn` is a factory** and a host hands over an agent that is
already paced. ⚠️ **`PacedAgent` lives in `BurmesePoker.Console`, which `BurmesePoker.Web` may
not reference — move it to `BurmesePoker.Domain/Agents/` or write the browser's own. Do not
solve it by referencing the console.**

✅ **P13.1 is done (2026-08-19).** `BurmesePoker.Presentation` exists — a fifth project, Domain
only, no rendering technology — and the console renders a view model it did not build itself,
**byte-identically at the same `--seed`**. It is the first presentation code in this project a
test can reach at all. 🔥 **Its most useful finding was a defect the first new test caught: a
view model that aliases the engine's hand list is not a view model.** `TurnContext.Hand` is the
seat's own live list, and `RoundEngine` discards from it the instant the answer comes back — so
a view built at the discard said fourteen and then held thirteen. The console never showed it
(render and drop, one breath); **a Blazor component holding a view across a render is exactly
the case that does.** ✅ **P13.2 inherited the rule and found two more instances of it.**

⚠️ **P13 was re-planned on 2026-08-19 and is now five sequential sub-packets, P13.1–P13.5.**
Nick asked whether a rich browser UI or multiplayer made more sense first, and **the answer is
that they are one track, not two** — see `BUILD-PLAN.md` **§3.10**. The only thing that couples
them is *where the engine runs*, and concealed hands with money on them settle that in advance:
**server-side, always.** A browser client is a seat over a wire whether one person or four are
using it, so **solo browser play is multiplayer with one connection** and there is no separate
single-player UI to build and then throw away. **Blazor Server** is the choice; it also means
P13.2 has **no protocol to design**.

✅ **The re-plan's judgement was right, and P13.1 is the evidence.** It said the cut line is
**decision versus drawing**, not `IAnsiConsole` injection. Both halves shipped, and only one of
them was worth anything: the extraction gave thirty new tests and a shared vocabulary, while the
injection — done, and the console reaches for the static console exactly once now — **cannot
deliver its stated benefit.** "Drivable from a test" needs the test project to reference
`BurmesePoker.Console`, which §2 forbids and which is the better rule. ⚠️ **Do not resolve that
by adding the reference.** The console's verification is a pty, and `scripts/drive-console.py`
is now checked in so a future packet can prove its own refactor the same way — capture, change,
`cmp`.

⚠️ **`BUILD-PLAN.md` §3.11 is a seventeen-item UX standard taken before a single component
exists**, and it is not optional inside a sub-packet. **Five of the seventeen are mechanical
tests** — the concealment leak, colour-not-alone tokens, computed contrast in both themes, real
controls rather than clickable divs, and subscription disposal. ✅ **A2 (colour never alone)
shipped in P13.1 and A1 (the concealment leak) shipped in P13.2** — the two that could not be
retrofitted cheaply, both taken before there was anything to retrofit. **Three are left, all in
P13.3**: computed contrast, real controls, subscription disposal. The rest are reviewed by playing, the way P11
was. **The one item that cannot be walked back cheaply is C12: static SSR by default, with
`@rendermode InteractiveServer` on the table component and nowhere near the root.**

**P13 is still the only optional part of the plan — stopping now is a legitimate end state**,
and so is stopping after **P13.5**, which is a finished single-player browser game that looks
like one.

✅ **P16 is done (2026-08-19), and the whole P14–P16 branch with it.** The question Nick's
friend raised has an answer, an interval and a control:

> **Upstream skill is worth `+9.1 ± 2.1` points of win rate across the biggest gap on the
> ladder (`random` against `greedy`) and `−1.0 ± 2.1` points across the gap between two
> thinking players.** A weaker player *anywhere* at your table is worth 4–5 points to you;
> **which side of you they sit on is worth nothing** unless they are not really playing.
> 64,000 games, two seeds, with a downstream control arm that seats exactly the same four
> strategies.

🔥 **And it corrected P15's headline lead rather than confirming it.** P15 found greedy and
cautious 5.4 points apart in the ladder run and recorded that "all of it is who fed whom". **It
is not.** It is the same symmetric *neighbour* effect — a rotation moves both neighbours at
once, which is exactly what the downstream arm separates. The lead was real and its reading was
wrong, and the control is what caught it.

⚠️ **Two things every future measurement inherits from P16.** `--seating balanced` plays every
assignment of the strategies across the seats instead of rotating one pattern, and **every CSV
row now names `upstream_strategy` and `downstream_strategy`.** **P12's headline re-measured:
30.7% vs 19.3% rotated (reproduced exactly at 8,000 games) against 29.6% vs 20.4% balanced** —
so **the rotation flatters greedy by 1.1 points a seat, about a fifth of the gap.** Quote the
balanced figure for "which strategy is better" and the rotated one only for "what happens at
that table".

**P0 through P12 and P14–P16 are done. The game is playable alone, pleasant to sit at, and measurable in
bulk:** `dotnet run --project BurmesePoker.Console` fills the empty seats with bots, paces them
so a person can follow, shows a hand as the melds it nearly is, keeps a round log across the
concealment clear, and replays any match from `--seed`; `dotnet run -c Release --project
BurmesePoker.Sim` plays thousands of seeded games in parallel and reports how two strategies
compare. **Three of §0's four goals are delivered — solo play (P10), console UX (P11) and
simulation (P12). The multiplayer app (P13) remains, and P14–P16 were added on 2026-08-19 to
carry the simulation goal further; all three shipped the same day.**

✅ **There is a persistence layer now, and it is a file format rather than a store.** P14 built
**game journals**: `Play/{GameJournal, JournalFormat}` and `Agents/{JournalingAgent,
JournalPlayerAgent}` in Domain, `Replay`/`JournalReport` in Sim, `--journal <path>` on both front
ends and a `replay` verb on the harness. A journal is a header plus every answer every seat gave;
replaying is **playing the game with different seats**, so the engine did not change by a line.
**A journalled run and its replay produce byte-identical CSV.** `MatchEngine` still keeps no
history (§3.8) and the console's standings still die with the process — a journal is a
consumer's artifact, which is the point.

The 2023 implementation is gone from the tree and lives in history at `79d86bd` (⚠️ **P55
correction: there is no `pre-rewrite` tag**). The
solution is **seven** projects — `BurmesePoker.Domain` (pure rules), **`BurmesePoker.Presentation`**
(what a hand looks like, as data; Domain only, **and no rendering technology at all**),
**`BurmesePoker.Server`** (one table, hosted: a remote seat and a filtered fan-out; Domain +
Presentation, **and no transport at all**), `BurmesePoker.Console` (Spectre.Console 0.57.2, the
only project that prints), **`BurmesePoker.Sim`** (batch play, Domain only), **`BurmesePoker.Web`** (Blazor Server — the
second project that draws; Domain + Presentation + Server) and `BurmesePoker.Tests`.
⚠️ **The test project references Domain, Presentation, Server, Sim *and* Web.**
The rule that matters is unchanged in substance and worth restating in its true form: **tests
never reference `BurmesePoker.Console`**, so nothing is tested through the front end. The
harness's determinism is itself an acceptance criterion, so it has to be reachable from a test.

Domain holds `Cards/{Rank,Suit,CardColor,CardText,CardId,Card,Deck,DeckBuilder,
DeckExhaustedException}`, `Melds/{MeldKind,MeldSlot,Meld,RunGenerator,SetGenerator,
MeldCandidates,MeldIndex,HandEvaluator,PartialCover}`,
`Money/{MoneyCardRegistry,CardOwnership,Stakes,Settlement}`,
`Play/{PlayerId,TurnAction,PlayerState,TableState,TurnContext,RoundResult,RoundEngine,
MatchEngine}`, `Abstractions/{IPlayerAgent,IGameObserver}` and
`Agents/{CoverScore,RandomBotAgent,SimpleBotAgent,GreedyBotAgent,CautiousBotAgent}`. Console holds `{Program,CardFormatting,
SpectrePlayerAgent,ConsoleObserver}`. Sim holds `{Program,Simulator,GameRunner,
SimulationOptions,SimulationReport,StrategySummary,Strategy,SeedSequence,SeatRecorder,
SimObserver,RoundAbandonedException,Results,CsvReport,Replay}` **plus P16's
`{SeatingPlan,Measurement,NeighbourExperiment,NeighbourCsv}`**. **Console gained five files in
P11**: `{Options,Palette,RoundLog,HandView,PacedAgent}` — ⚠️ **`HandView` is gone as of P13.1**,
replaced by `HandPanel`, which draws a view model rather than building one. **P13.1 added
`BurmesePoker.Presentation/{CardDisplayState, DisplayTokens, CardOrder, CardView, MeldView,
HandView, ComputerAdvice}` and `scripts/drive-console.py`; ⚠️ **P13.3 moved `PacedAgent` in
beside them, out of the console.** **P13.3 added `BurmesePoker.Web/{Program, TableHost,
TableBoard, CardWords}`, `Components/{App, Routes, _Imports, Layout/MainLayout,
Pages/{Watch, Rules, Error}, Table/{TableView, SeatPanel, CardChip, RoundLogPanel,
SettlementPanel}}` — each table component with its own `.razor.css` — `wwwroot/{theme.css,
app.css}` and `Properties/launchSettings.json`. P13.2 added
`BurmesePoker.Server/{TableSession, TableSeat, TableOptions, TableFanOut, TableEvent,
SeatConnection, SeatPrompt, SeatQuestion, SeatAnswer, RemotePlayerAgent, BoundedAgent,
TableAbandonedException}` — and nothing anywhere else at all.** **P14 added `Play/{GameJournal,
JournalFormat}` and `Agents/{JournalingAgent,JournalPlayerAgent}` to Domain and `Replay.cs`
(which holds both `Replay` and `JournalReport`) to Sim. P15 added
`Agents/{RandomBotAgent,CautiousBotAgent}` and moved the discard loop into `CoverScore`, and
added nothing to Sim at all** — a strategy is an `IPlayerAgent` and the harness already knew
how to seat one. ⚠️ **Domain now references
`System.Text.Json`** — the first framework assembly in there beyond the base library. It is a
string API, not an I/O one: `JournalFormat` hands back `IEnumerable<string>` and the two front
ends own every `File` call, the same split `CsvReport` already had.

✅ **Baseline green** — `dotnet build` clean and warning-free, `dotnet test` **389 passed,
0 failed**, in twenty seconds. **Any red tree is a real problem.**

⚠️ **One hazard a cold context must know before writing a test or a strategy:** with the
reshuffle built, **a round in which nobody's hand ever improves never ends.** Only a
declaration ends a round (RULES.md §7.1) and the cards now circulate for ever, so a table of
passive agents loops until it is killed. Every round-level test needs a seat that eventually
declares. `GreedyBotAgent` and `SimpleBotAgent` are both safe in practice — neither has ever
failed to finish a round — but **the simulation harness is the only place that is bounded**:
`SimulationOptions.TurnCap` and `SeatRecorder` give up on a stalled round and report it. A
test that plays a round outside the harness has no such protection.

---

## Packets

| | Packet | Depends on | Notes |
|:-:|---|---|---|
| ☑ | **P0** Restructure and salvage | — | done 2026-08-18 |
| ☑ | **P1** Cards, deck, identity | P0 | done 2026-08-18 |
| ☑ | **P2** Money designation and ownership | P1 | done 2026-08-18 — turned-up-joker default taken |
| ☑ | **P3** Run candidate generation | P1 | done 2026-08-18 — spec updated with what it found |
| ☑ | **P4** Set candidate generation | P1 | done 2026-08-18 |
| ☑ | **P5** Exact-cover hand evaluator | P3, P4 | done 2026-08-18 |
| ☑ | **P6** Stakes and settlement | P1, P2 | done 2026-08-18 — settlement takes the *unshuffled* shoe |
| ☑ | **P7** Round and turn engine | P5, P6 | done 2026-08-18 — deals from a draw order, not a shuffle |
| ☑ | **P8** Console front end | P7 | done 2026-08-18 — hotseat; verified by driving a pty |
| ☑ | **P9** End-to-end play | P8 | done 2026-08-18 — the reshuffle lives inside `RoundEngine.TakeCard` |
| ☑ | **P10** Bot opponents — solo play | P9 | done 2026-08-18 — `PartialCover` + `GreedyBotAgent`; every seed terminates |
| ☑ | **P11** Console UX pass | P10 | done 2026-08-18 — round log, meld-grouped hand, hints, pacing, `--seed` |
| ☑ | **P12** Simulation at scale | P10 | done 2026-08-18 — `BurmesePoker.Sim`; the tie-break wins 30.7% to 19.3% |
| ☑ | **P13** The browser client and multiplayer | P10 | XL — **re-split 2026-08-19 into the six below**; done 2026-08-19, and the only one that changed the architecture |
| ☑ | **P13.1** A presentation view model, rendered two ways | P10 | done 2026-08-19 — a fifth project; the console is byte-identical, and a view model must be a **snapshot** |
| ☑ | **P13.2** The table server | P13.1 | done 2026-08-19 — a sixth project, no transport; **the concealment leak test shipped, and it fails when broken** |
| ☑ | **P13.3** A browser table you can watch | P13.2 | done 2026-08-19 — a seventh project; **the first UI**, and the rest of the §3.11 A list shipped with it |
| ☑ | **P13.4** A seat you can play | P13.3 | done 2026-08-19 — **solo browser play, complete**; 86 questions answered with no pointer at all |
| ☑ | **P13.5** A table, not a document | P13.4 | done 2026-08-19 — the layout pass; **a focus call can kill the circuit**, and whose turn it is was made public on purpose |
| ☑ | **P13.6** The lobby, and a second person | P13.5 | done 2026-08-19 — **§0's goal 4**: two people and two bots play a round over a network |
| ☑ | **P14** Game journals — record and replay | P12 | done 2026-08-19 — a run and its replay produce byte-identical CSV |
| ☑ | **P15** A skill ladder | P12 | done 2026-08-19 — four rungs, **three** separated skill levels |
| ☑ | **P16** Does the player before you decide your game? | P15 | done 2026-08-19 — **no**, not between thinking players: −1.0 ± 2.1 pts |
| ☑ | **P17** The tournament: a harness that ranks players | P15, P16 | done 2026-08-19 — **a win rate is a ratio over games, not a mean of ratios**; pairing widens within a cell by √2 |
| ☑ | **P18** One catalog: the same players everywhere | — | done 2026-08-19 — `BotCatalog` in Domain; **the console's difficulty default had been the *easy* bot since P10** |
| ☑ | **P19** Difficulty as a dial, not a list | P17, P18 | done 2026-08-19 — **goal 5's product, finished**: four levels, one mistake rate each, **calibrated to ~7 points a step** |
| ☑ | **P20** Memory: the card-counting rung | P17, P18 | done 2026-08-20 — **a null, published**: `counting` is `+0.3 ± 1.0` the *wrong* way against `greedy` |
| ☑ | **P21** Outs: the first rung that looks ahead | P17, P18 | done 2026-08-20 — **it separates**: `+3.1 ± 1.0` over `greedy`, and the whole solution got 45% faster on the way |
| ☑ | **P22** Money: is there a strategy in the side bet? | P17, P18 | done 2026-08-20 — **no at $5/$1, where the rule never fires at all; `+7.3 ± 3.3` a round at $5/$40** |
| ☑ | **P23** The standing answer | P17, P19 | done 2026-08-20 — **the last packet**: the dial re-fitted (one ε moved), **59 of 77 rows byte-identical**, and "a rung cannot be added without being measured" made a test |
| ☑ | **P24** ~~The computer's reasoning~~ — **split 2026-08-21** | — | **both halves done**: P24.1 2026-08-21, P24.2 2026-08-22 |
| ☑ | **P24.1** A journal for the hosted table · **Fable 5** | P13.6, P14 | **done 2026-08-21** — `TableOptions.Journal` opts the session into `JournalingAgent.Wrap` (**outermost**, so the record is the answer that reached the engine — a stand-in's or the clock's included); `TableSession.Journal()` hands the record back and **the file stays the host's** (`HostedTable` flushes after every settled round, on the dealer's own thread). `TableSeat.Strategy` is the attribution — a level's name for a Web bot, `human` for a remote seat. A hosted round **replays identically** through the ordinary `JournalPlayerAgent`; an abandoned round marks `Header.Abandoned`. ⚠️ The rev-13 stamp (R2) is untouched on purpose — P30.2's fix, one constant for all three writers. |
| ☑ | **P24.2** The computer's reasoning, said out loud · **Opus 5** | P24.1, P18, P21, P31, P32 | **done 2026-08-22** — `IExplainsDiscards` is the described sibling of P31's `IRanksDiscards`: `CoverScore.Scored` keeps the keys the ranking threw away, and `Ranking` is now *defined as* its projection, so an explanation costs no extra `PartialCover.Best` call (asserted by `ComputerAdvice.RankingsBought`). Every key carries a name, a direction and **a phrase for its sentinel** — `outs` negates its outs key and a joker's partnership is `int.MaxValue`, and neither ever reaches a screen. `AdviceRationale` assembles winner-versus-runner-up, says so when *nothing* separated them, reads the table's own `TableRules` for the closing clause, explains a banned card as a **rule** (and stops on the turn §5.1's floor yields), and never implies the computer plays for §7.3's bonus. The browser's five existing `<details>` gain a computed paragraph **gated on hints while the rule text around them is not**. The journal grows `JournalDecision.Advice` — an *opinion beside an answer*, human seats only, by `CardId` — and `DisagreedWithTheComputer` is the query. ⚠️ **The console is untouched and its capture is byte-identical.** |
| ☑ | **P25** The win condition is a function of the table size | — | **done 2026-08-21** — `TableRules.For(players)` is the §7.1.1 table as data; `HandEvaluator` takes it and **has no parameterless overload**. The search carries what is still owing **along** the partition; two-handed prunes sets out of the candidates; `Meld.IsClean` needs no case for the all-joker meld. 🔥 **The change is real and `drive-console.py` cannot see it** — both captures are byte-identical because neither script reaches a declaration. |
| ☑ | **P26** The money layer as it actually is | — | **done 2026-08-21** — `Permanent` is **three values and eight cards**, `Multiplier(Card)` returns 0/1/**3**, and the ×5 is `Multiplier(card, owner, MoneyOwnership)` under a configuration `Settlement` reads **once a round**. 🔥 **The packet's stated prediction held**: a designation on the 7♦ and one on an ordinary card leave *exactly the same* money loose in the shoe, now an equality assertion. 🔥 **The side-bet went from `$8.50` to `$11.58 ± 0.34` a round at five seats — 42.5% → 58% of the round prize — and play did not move at all.** ⚠️ **The ×5 is fenced to the 7♦/A♠ pair** (§9 #32) by two tests. |
| ☑ | **P27** The feeding ban | — | **done 2026-08-21** — §5.1 enforced **by construction**: `FeedingBan` is two rank sets a seat, `TurnContext.LegalDiscards` is the whole of the choice a turn presents and is **never empty**, and `CoverScore.Ranking` takes a context so **every rung including the runner-up is filtered without any rung remembering to**. 🔥 **The predicate is `Card.SameRankAs`, which is `Rank == other.Rank`** — nullable equality is what makes a joker close the other jokers (§9 #27), so the house ruling falls out of the type. 🔥 **A bot's cover count can now fall**, which breaks the monotonicity argument that says a table of bots terminates. ⚠️ Both front ends draw a closed card as **not a control** (`CardDisplayState.Unthrowable`). |
| ☑ | **P28** The claim, the permission, and the seat you sit in | **P27** ✅ | **done 2026-08-21** — `MatchEngine` draws the seats before every deal after the first (§3), and `IPlayerAgent.ObjectToClaim` is the game's fifth question and **the only one asked of a seat that is not on turn** (§4.5). 🔥 **The objection predicate was read, not written twice**: `ClaimRequest.MayBeRefusedBy` asks `Card.SameRankAs`. 🔥 **A refused claim arms nothing** — the opener falls through to a blind draw — which is the one line where this touches P27. 🔥 **The finding that cost the most is older than the packet**: `JournalFormat.Name` ended `_ => "declare"`, so the fifth question was written to file as a *declaration* and only the file round-trip test could see it. ⚠️ Re-seating lives in `PlayRound()` and not in the scripted overload; ⚠️ `BoundedAgent` must not announce a turn for the seat it asks. |
| ☑ | **P29** Re-measure, under the rules as they are | P25 ✅, P26 ✅, P27 ✅, P28 ✅ | **done 2026-08-21** — `measurements.csv` regenerated under P25–P28: **91 measurements in 9,981 s**, and **4 of 91 rows byte-identical — the four ε constants, which are the only rows a human chose.** 🔥 **Of three predictions written down in advance, two held and one was wrong**: the dial survives (all three steps separate; no ε moved), `prospector` separates a whole ratio lower ($5/$20 is `+5.32 ± 2.27`, was inside the interval) — and **`outs` did not narrow at all**, `+3.0 ± 1.0` against +3.1. 🔥 **What the win condition actually moved is `simple`**, which gained ~2 points on all three middle rungs: **a requirement no rung optimises for levels a ladder from the bottom.** ✅ **Refusing a claim is a null** (`+0.4 ± 1.0`), published. ✅ **Round length and abandoned counts are published for the first time** — the only non-zero abandoned count is the field containing `random`. |
| ☑ | **P30.1** A thorough code review · **Fable 5** | — | **done 2026-08-21** — `docs/REVIEW-2026-08.md`: **37 findings, every one triaged, nothing unassigned**; one main read over the whole Domain plus four parallel scoped reviews on the same model, against a green 672/0 baseline. 🔥 **Top of the list**: a human can throw a banned rank that was legal only as a declaring discard and then decline to declare (R1); every journal since 2026-08-21 stamps rules **rev 13** against a document at rev 24 (R2); a published P29 headline interval used the unpaired formula on a within-cell money margin (R3). 🔥 **The systemic hole is the fifth question** — `FallibleAgent.ObjectToClaim` is tested nowhere and no connected fixture ever *allows* a claim (R6). ✅ **No new rules question**; `RULES.md` stays rev 24. |
| ☑ | **P30.2** Conformance — the rules as *played* · **Fable 5** | **P30.1**, P24.1 | **done 2026-08-21** — `Tests/Conformance/`: `RuleConformance` audits 180 ordinary rounds at 4, 5 and 6 seats by independent re-derivation, one mutant per rule family proves the audit can go red, and `SettledRuleCoverageTests` binds `RULES.md`'s Settled sections to a check-or-exemption registry. Both front ends driven to a declaration: `drive-console.py` answers prompts adaptively and verifies the settlement panel; `BrowserRoundTests` closes board = engine = journal replay. **R1 fixed** (an exception-2 throw *is* the declaration), **R8 fixed** (`SeatChannel` — a connection is one occupancy), and all 29 P30.2-fix items landed. ⚠️ The "no capture has ever contained a win" premise was stale — the grep needle was `went out`; the console says `declares`. |
| ☑ | **P31** `warden` — the feeding ban as a weapon · **Opus** | P27 ✅, P30.2 ✅ | **done 2026-08-22** — the first rung to play §5.1 **offensively**, and **it lost by more than any rung has lost before**: `−9.3 ± 1.0` against `outs`, ~−6 against `greedy`/`cautious`/`counting`, `+2.5` over `simple`, all surviving Holm over a family of 21. 🔥 **The packet predicted a null; the mechanism variable is what makes the loss attributable** — §5.1 removed a held card from **30.5%** of all discards and **changed the seat's answer on 30.8% of those, 9.4% of every turn**, so the rule bites hard and the rung is what failed. ⚠️ **The why:** it prices a lock in melded cards and pays for it in **draws**, which nothing in its rule prices. 🔥 **71 of 88 shared CSV rows reproduced byte-identically.** 🔥 **Unplanned: 'the last rung named is the strongest' was a coincidence asserted as a law in three places** — two fixed, the null cell left alone on purpose. `sim suite` = **116 measurements in 11,020 s**. |
| ☑ | **P33** The clean bonus (§7.3) · **Opus 5** | rev 26 ✅ | **done 2026-08-22** — a **jokerless** declaration pays **×2** at 2/3/4 seats and **×3** at 5+; `TableRules.JokerlessMultiplier` beside §7.1.1's table, `Settlement.IsJokerless` the predicate, `Settlement.RoundPayment` the arithmetic. **§10 #19 discharged** — `RULES.md` again records nothing Settled that nothing implements. 🔥 **Regenerated at four seats: 119 measurements in 11,159 s, 111 of 116 shared rows byte-identical, and the five that moved are exactly the rows denominated in dollars a round.** The bonus is **collected in about one round in six** (`STRATEGY.md` §14, published as a floor) and it is **a tax on trading wins for money** — every `prospector` money margin fell. ⚠️ **§9 #33, #36 and #37 built on their recorded defaults, each fenced by a test.** |
| ☑ | **P32** Five-handed is the default table · **Opus** | P30.2 ✅, P31 ✅, **P33 ✅** | **done 2026-08-22.** The standing set moved to **5 seats**, which is the size the game is actually played at, and **the dial is re-fitted there**. ⚠️ **P33 did not fold it in, deliberately** — attribution over wall clock, and the amendment with the reasoning is in `BUILD-PLAN.md` §5 P32. ✅ **It inherits a complete, current four-handed baseline.** 🔥 **It now has a prediction to write down**: §7.1.1 asks for a clean series at four seats and nothing at five, while §7.3 pays ×2 at four and ×3 at five — **so the jokerless rate should fall and what it is worth should rise, and which wins is the most interesting number the packet can produce.** ⚠️ **The cost blow-up is unchanged**: 21 cells and `Balanced(7,5)` = **16,807** assignments. **Decide up front whether the free-for-all runs at the full crossing or a stated subsample, and if it is capped, say so in the file.** |
| ☑ | **P37** Asking the table to change seats · **Opus 5** | **P36 ✅** | **done 2026-08-22** — §10 **#23 discharged**, so §3 step 2 is built in both halves and §10 is empty again. `IPlayerAgent.AskAboutTheSeating` is a **sixth question** and the first that is not about cards: every seat is asked between rounds, and the seats move on **one `Ask` and no `Refuse`**. 🔥 **Consent is not desire** — a yes-or-no question would have re-seated an all-bot table every deal, so `SeatingOpinion` is three answers and *fail closed* fell out for free. ⚠️ **A public question is a standing answer, not a pending prompt** (§3.13). 🔥 **The first interface member with a default implementation, so six decorators had to forward it** — a wrapper that forgets answers *consent* in its own name and silently drops what it wraps. ✅ Replay was free; ✅ concealment asserted by type and by broadcast; ✅ P36's two leftovers taken. |
| ☑ | **P36** How long a seating holds · **Opus 5** | — | **done 2026-08-22** — `Domain/Play/SeatingPolicy.cs`: **held by default**, *N* rounds between seatings, **0 is never**, one condition in one place. §10 **#22 discharged** and the engine stops contradicting §3 step 2 for the second time in opposite directions. ⚠️ **Not a revert** — pre-P28 held a seating that could never change. Two fences named for §9 #45 and #47; a layering scan bans a second copy (it found one in `JournalFormat`); the journal writes `seating_rounds` only when the seating moved, so **every journal ever written is byte-identical**. ⚠️ **A seed from between P28 and P36 replays differently**, and `SeatBoardTests`' fixture went 3 → 5 rounds because of it. ✅ **No measurement moved, asserted rather than argued.** |
| ☑ | **P35** The two scoring rules that reach outside a round (§7.4, §7.5) · **Opus 5** | P33 ✅, P32 ✅, **P36 ✅** | **done 2026-08-23** — §10 **#20 and #21 discharged, and §10 is empty**: every rule `RULES.md` records as Settled is implemented. 🔥 **§7.4 changed the shape of a round** (a dealt thirteen that wins is offered the declaration before the first take; `Turns` is 0) and 🔥 **§7.5 put the first state in this game that reaches across rounds** (`MatchEngine.Streak`, handed **down** — settlement is told, never made to remember). ⚠️ **`Settlement.RoundPayments` replaced two re-derivations** of the round/side-bet split that had both assumed every loser pays the same. ✅ **Conformance gained its first multi-round case; no whole exemptions remain (ceiling 7 → 6).** 🔥 **Re-measured: 107 of 124 shared rows byte-identical, nine deal wins in 33,008 rounds, and no win rate moved at all** (`STRATEGY.md` §15). ⚠️ **§7.5 is not measurable here and §11 says so.** **rev 30. 819 / 0.** |
| ☑ | **P34** A front door, and docs that cannot go stale quietly | — | done 2026-08-23 on **Opus 5** — `README.md` exists; the three historical documents carry banners; **eight tests in `BurmesePoker.Tests/Docs/` hold the documents to the tree**, each proved able to fail by mutating the document. 🔥 **The test count is discovered by reflection, so a packet that adds a test and leaves the prose alone is a red build** — and only the *first* count and rev in each newest-first document are checked, so the narrative keeps every superseded figure. ⚠️ **Found: `PLAYING.md` was quoting a four-handed reference table on a five-handed page and `RULES-PRIMER.md` carried four divergence tags closed at P25–P28.** |

**P14, P15 and P16 are all done, and not one of them needed a line of the engine.** P14 cost
nothing measurable in throughput at either fidelity; P15 and P16 raised no rules question
either — how well a strategy plays, and who it plays next to, are not rules (`RULES.md` stays
at **rev 13**). **P16 changed `BurmesePoker.Sim` only**: `SimulationOptions` gained one property,
`CsvReport` two columns, and four new files went in beside them.

**P10 was the fork; P11 and P12 have both been taken through it.** ⚠️ **P12 opened a branch
rather than closing one** — having a harness is what makes journals and a strategy-comparison
programme worth building, so P14, P15 and P16 hung off P12 and not off P13. ✅ **That branch
closed with P16** — the game is finished as a game, the console as a console, the simulation goal
is delivered and the neighbour question has an answer with a control. **Stopping there was a
legitimate end state.**

⚠️ **It reopened on 2026-08-19 as goal 5, and it is now the only live branch.** P17–P23 hang off
P15/P16 (the measurement apparatus) and off nothing in P13. **P17 and P18 are independent of each
other**, both feed **P19** — where the difficulty product is finished — and **P20, P21 and P22 are
independent of one another and droppable in that preference order**. 🔥 **The dependency that
matters is P17 before P19**: calibrating a difficulty ladder with the interval-free report the
harness prints today would be a guess wearing a number.

⚠️ **Four things the roadmap changed for work that was already planned**, all recorded in
`BUILD-PLAN.md` — **all four are now discharged**:
1. ✅ **P10's stated heuristic was wrong, and the correction shipped.** "Never discard an owned
   money card" contradicts `RULES.md` §4.4 — ownership never transfers, so a money card pays
   you after you throw it. `GreedyBotAgent` consults neither ownership nor the registry
   anywhere, and `MoneyCardsDoNotChangeWhatABotThrowsAway` is the test that says so.
2. ✅ **§3.6 settles that agents stay synchronous**, taken *before* P10 and P13 add callers to
   `IPlayerAgent`. A remote player blocks in the agent; one table is one task. **P10 and P12
   both added callers and neither needed anything** — a bot answers the same four questions a
   person does, and a simulation runs a whole table inside one `Parallel.For` body.
3. ✅ **§3.7's speed question is closed, by P12's measurement pass.** `PartialCover.Best` is
   **140 µs** a hand and `HandEvaluator.TryFindCover` **91 µs**; a round is **~20 ms** and a run
   does **50–90 rounds a second**, so 2,000 rounds took 34 seconds with nothing optimised. The
   one surprise: the work is **allocation-bound, not compute-bound** — eight threads bought 25%
   under the workstation GC and 70% under the server GC.
4. ✅ **§3.8's statistics constraint — delivered by P9.** `MatchEngine.PlayRound` returns a
   `RoundRecord(RoundResult, TableState)`, so how close the losers were and how much of the
   money was the side bet both stay reachable, and `RoundResult.Turns` is carried at the
   source. The engine keeps **no** per-round history: a consumer derives what it wants and
   drops the table. **P10 built the third seam too** — `RecordingAgent`, a decorator over
   `IPlayerAgent`, in the test project.

---

## Notes for the next session

### What P60 leaves for P61 (2026-08-30)

🔥 **(1) Two defects, both found by pressing a thing on a device, and neither is a layout
redesign.**

**(a) The copy-link reveal is a dead rule.** `BurmesePoker.Web/Components/Pages/Tables.razor.css`
line ~141 says `:global(.can-copy) .link .copy { … }`. **`:global()` is CSS Modules; Blazor scoped
CSS has `::deep`**, and the Razor rewriter passes `:global(...)` through untouched, so the browser
discards the selector and only `.link .copy { display: none }` survives. ⚠️ **`::deep` is not the
fix either** — it scopes *descendants of the component's own root*, and `.can-copy` is on
`<html>`, outside any component. **Recommended shape: move the reveal rule into the unscoped
stylesheet** (`wwwroot/app.css`) beside the other document-level rules, leaving the `display:none`
default in the scoped file, **or** drop the class mechanism and have the script set the style it
needs. 🔥 **The fence is the packet, and it must not be a screenshot**: a test that reads the
**generated** scoped CSS (`obj/.../scopedcss/**`) and fails on any `:global(` is one line and
catches the whole class of mistake; a second that asserts a rule revealing `.copy` exists somewhere
in the served CSS is the positive twin. ⚠️ **This is only one site today** — `::deep` is used
correctly in `MainLayout.razor.css` and `TableView.razor.css`.

**(b) A18 is fitted to width and its argument depends on hover.** Above 56rem the seat name
ellipses on the stated grounds that `title=` puts the whole name a hover away; **a tablet in
landscape is 1316 px with `(any-hover: none)`**, so three of six names were unreadable with no way
to reach them. ⚠️ **Do not simply widen the ring's name column** — that is the platform-font trap
P58 refused, and it is the same mistake one axis over. **Recommended shape: make the wrap follow
the *capability* rather than the width** — `@media (any-hover: none) { .name { white-space: normal;
text-overflow: clip } }` — and **amend A18 to say so**, because the standard is what makes the fence
honest. ⚠️ **`@media` can read `(any-hover)` but still cannot read `var()`**, so P54's
same-width-named-twice idiom is unaffected.

⚠️ **(2) The phone half of P53/P60 has a prerequisite that is not in this repository.** Item 3's
source-IP evidence has **no instrument**: Traefik runs with no `accessLog` and the app logs no
remote address. **Enable Traefik's access log in `ansible-nas` before a cellular test**, or the
test cannot distinguish a carrier path from home wifi — which is the one thing it exists to prove.
✅ **The IPv6 half is already answered** (no `AAAA`; one record fixes it if it bites).
🔥 **This is now written up as `P62` and it is that packet's step 0** — ⚠️ **a packet that skips it
cannot be marked done**, because an unproved path is the failure mode rather than a missing
nicety.

⚠️ **(3) Operational note for any future device or deploy check.** `docker ps` shows the **tag**,
which is not the same as what the container is **running**: read
`docker inspect <container> --format '{{.Image}}'` and match it against
`docker images … --format '{{.Tag}} {{.ID}}'`. **P60 found the running container five commits
behind a `:latest` that had already been pulled.**

⚠️ **(4) How the tablet was driven, so the next session does not rediscover it.** USB debugging,
`adb forward tcp:9222 localabstract:chrome_devtools_remote`, then CDP over
`http://localhost:9222/json/list`. **The device must be unlocked and awake** — dozing Chrome
answers `ERR_INTERNET_DISCONNECTED` while the OS pings fine, which reads exactly like a network
fault. For physical taps, device px = CSS px × 1.75 + **222** (portrait, this device's browser
chrome); measure it again after a rotation rather than assuming.

### What P59 leaves for P60 (2026-08-30)

🔥 **(1) There is a circuit client in the test project now, and it is reusable.**
`BurmesePoker.Tests/Web/BlazorCircuit.cs` starts a real circuit, holds it, kills the socket with
no close frame, and asks for it back by name; `BlazorPack.cs` is the wire format it does that
over. ⚠️ **It is deliberately partial** — only what a circuit's opening needs is written down, and
everything else is *skipped by shape* so an unexpected message cannot desynchronise the stream. A
test that needs a message this cannot write should add it here rather than work around it. ⚠️ **It
does not draw**: render batches are acknowledged and thrown away, so **it cannot press a button** —
a UI event needs the handler ids inside a render batch, which is a decoder this does not have and
did not need.

⚠️ **(2) What P59 measured is a browser's *connection*, not a browser.** No layout, no font, no
touch, no Mobile Safari, and the socket was a pair of in-memory pipes rather than a radio. **P60's
trap is unchanged and P59 does not soften it**: emulation going green must not close P53.

🔥 **(3) The one number P60 should carry over.** The turn a person is asked about is **two**
questions, not one — take, then throw — so *the computer taking over* costs a patience and *the
turn moving on* costs two. **A device test watching a timeout should expect twice the patience**,
and `TableBoard.StoodInFor` is what says the computer answered for you.

⚠️ **(4) The deployed site does not have any of this**, and it does not need it: P59's diff is the
test project. **But P58 and P59 both shipped after the last deploy**, so P60's first job is still
P53 finding (8) — **read the deployed commit before testing a front end against the live site.**

### What P34 built, for the session that opens whatever comes next (2026-08-23)

**Five things a cold session needs.**

🔥 **(1) There is no next packet.** `BUILD-PLAN.md` §5 is empty for the first time since P0, so the
first job is choosing rather than orienting. **What is next** above ranks the four candidates; the
one this file would take is **§10 #7**, the table sizes nobody can deal, because it is the only
Settled rule the program does not play and it is the oldest entry in §10 that nobody owns.

⚠️ **(2) Adding a test now costs a documentation edit, and that is the design.**
`PublishedFigureTests.TheFirstCountAndRevisionEachDocumentQuotesAreTheCurrentOnes` counts
`[Fact]`s plus theory rows by reflection and compares against the **first** *"green at N"* in
`CLAUDE.md` and in this file. **A red build there is the count being wrong, not the test** — put
the new number in both places. ⚠️ **The counter knows `InlineData` and `MemberData` and throws by
name on anything else**, so a `ClassData` arriving needs one arm added rather than a silent
undercount.

🔥 **(3) The commands check reads the parser, not a list.** Each front end recognises switches its
own way — the harness's `Arguments`, the console's `case "--flag"`, the browser's
`GetValue<T>("key")` — and `DocumentationTests.Options` has one regex per shape. **A new front end,
or a new way of parsing in an old one, needs an arm there or its documented flags stop being
checked.** ⚠️ **The negative control is load-bearing**: the test asserts `--seat` still does *not*
resolve for the browser, because without it the recogniser could accept everything and every
assertion in the file would be vacuous.

⚠️ **(4) The figure checks fence the *tables*, never the prose.** `STRATEGY.md`'s matrix, ranking,
reference table and steps are located **by their header text** — rename a header and the test says
so rather than skipping — and the tolerance is derived from the printed precision. **Prose figures
are deliberately unchecked**, because the documents quote historical numbers on purpose and a check
over all of them would forbid the comparisons that are the findings.

🔥 **(5) The staleness that was actually there was in the two documents written for people.**
`PLAYING.md` and `RULES-PRIMER.md` are the pages a player reads and the ones no build session
opens; both were a rules change and a measurement behind. ⚠️ **The lesson is where to look**: the
documents that go stale are the ones nothing else in the tree depends on.

---

### What P35 built, for the session that opens P34 (2026-08-23)

**Five things a cold session needs.**

🔥 **(1) `TurnNumber` 0 now means two different questions, and anything keying on
`(Round, TurnNumber)` sees both.** P37 put the seating question there; P35 put the initial-deal
declaration there too. In a journal they appear in the order they are asked — seating first (it is
asked in the gap before the deal), then any turn-0 `Declare`. `JournalPlayerAgent.AskAboutTheSeating`
**peeks** and returns `Consent` when the next decision is not a `Seating`, which is what keeps the
two from colliding. ⚠️ **A third question at turn 0 would need that peek to be re-read before it is
written.**

⚠️ **(2) Replaying a pre-P35 journal that deals a winning thirteen diverges — loudly, on purpose.**
The engine now asks a question the file has no answer for, so `JournalPlayerAgent` throws
*"the replay has diverged"* rather than guessing. `CurrentRulesRevision` **30** is what makes that
diagnosable. **This is the opposite of P37's seating narrowing and deliberately so**: consent
changes nothing, a declaration ends the round.

🔥 **(3) `Settlement` is a parameter-list guarantee and it is worth keeping that way.** It takes no
table, no seat, no player state, **no match and no history** — asserted by
`SettlementTests.TheOnlyHandSettlementIsGivenIsTheWinnersDeclaredThirteen`, which now checks that
the seventh parameter is a `Win` and not a hand. **A sixth qualification on §7.2 step 1 should go on
`Win`, not into a new argument**, and `Win` is a reference type precisely so a caller cannot forget
it (a record struct would default to an ordinary flat win, in silence).

⚠️ **(4) The measurement instrument now has a row that is normally zero, and that is a feature.**
`bonus.deal-rate.*` was 9 rounds in 33,008. **If a published money figure ever moves and nobody
knows why, look there first** — and remember what P35's own falsifier got wrong: the column that
discriminates a mis-placed split is **`money.side-margin.*`**, not the win rate.

⚠️ **(5) No rung knows §7.4 or §7.5 exists**, and that is P15's discipline rather than an oversight.
A rung that declined a deal-bonus win, or that used the seating question around somebody's streak,
is a **new rung** — measured before it joins the ladder. 🔥 **And §7.5 is the first rule in this
game a player can act on *between* rounds**: asking to change seats before somebody's third win
moves who pays. **Nobody has measured whether it is worth doing, and no instrument here can** —
`STRATEGY.md` §11 says so.

---

### What P37 built, for the session that opens P35 (2026-08-22)

**Four things a cold session needs.**

🔥 **(1) §10 is empty: every Settled rule in `RULES.md` is implemented.** #22 (P36) and #23 (P37)
were the last two. **§3 step 2 is now checked in both halves** by `RuleConformance` — held by
default, changed only by agreement — at 4, 5 and 6 seats.

🔥 **(2) A default interface method is a silent trap, and this is the first one this project has.**
`IPlayerAgent.AskAboutTheSeating` has an implementation on the interface (every rung consents,
BUILD-PLAN §3.13), which means **a decorator that does not override it does not fail to compile and
does not throw — it answers in its own name and drops what it wraps.** Six wrappers needed
forwarding: the journalling agent, the replay seat, the dial's `FallibleAgent`, the claim-policy
agent, the pacing agent, the server's `BoundedAgent` and the harness's `SeatRecorder`.
`SeatingAgreementTests.EveryDecoratorForwardsTheSeatingQuestion` finds them **by type** — anything
taking an `IPlayerAgent` in its constructor — so the next one is covered. ⚠️ **Any future default
member on that interface inherits this trap.**

⚠️ **(3) The seats can now move mid-streak, which is P35's problem.** §7.5 blames *the seat above
the winner* for a third consecutive win, and P36 (a house policy) and P37 (an agreement) both make
the seat above you a thing that can change between the rounds of a streak. **§9 #46 is live rather
than hypothetical**, its recorded default is *settle from the round being settled*, and it needs a
test. 🔥 **And §7.5 is now a rule a player can act on**: asking to change seats before somebody's
third win moves who pays. **That is a strategy nobody has measured** — P35 should say so, not
measure it (`STRATEGY.md` §11, no silent caps).

⚠️ **(4) `JournalPlayerAgent` peeks rather than consuming, for one question only.** Absence of a
seating decision means `Consent`, because every journal written before P37 has none. It is a
deliberate narrowing of *divergence is loud, always*, safe only because consent changes nothing —
**do not copy the pattern to a question whose default answer does something.**

---

### What P36 built, for the session that opens P37 (2026-08-22)

**Four things a cold session needs, in the order it will meet them.**

🔥 **(1) `SeatingPolicy` is get-only on the match, and that is a decision rather than an
oversight.** `MatchEngine.SeatingPolicy` cannot be set after construction and
`SeatingPolicy.ReseatsBefore(int)` is a pure function of the round count — **a policy that could
be talked to would have answered §9 #45 by accident**. So P37 cannot express *the table agreed* as
a policy and must pick a shape. **Recommended: an explicit `MatchEngine.Reseat()` that the host
calls between rounds.** An agreement is an **event**; `ReseatsBefore` stays pure; the default stays
*held*; and `LayeringTests.NothingOutsideTheSeatingPolicyDecidesWhenTheSeatsAreDrawnAgain` stays
true, because `Reseat()` is the engine drawing rather than a second place deciding *when*. ⚠️ **A
settable policy is the cheap wrong answer** — *agreed once* is not *every N rounds*, and the type
would then mean two things.

⚠️ **(2) The journal records a policy and cannot record an agreement.** The header field is
`seating_rounds`, a number, written **only when the seating was not held** — so every existing
journal is byte-identical and absence means the rule. An agreed re-draw happens at a **round**, not
every *N* of them, so P37 needs a **decision kind**, and `JournalFormat.Name`'s default arm is
exactly where P28's mistranslation lived (a fifth question written to file as a *declaration*, and
only the round-trip test could see it). ⚠️ **A replay must re-seat at the same round**, which a
policy alone will not say.

🔥 **(3) A seed stopped meaning what it meant, and a test caught it.** A held seating takes **no
numbers at all** out of the match's generator, where the every-round draw took some the deal now
takes back (§3.9 point 2, for the second time). `SeatBoardTests`' fixture played **three** rounds
and `EverySeatIsAskedEveryQuestionOverAMatch` went red — the claim's permission needs a claim *and*
the seat above holding the rank, and it stopped turning up at that seed. It turns up again at
**five**, and the fixture is five rounds now. ✅ **Nothing was tuned to pass**: three was never the
number, and this is the assertion working. ⚠️ **Expect this from any packet that changes what the
match's generator is asked for**, and do not go looking for a bug in the thing that went red.

✅ **(4) Two fences stand where P37 has to build.**
`SeatingPolicyTests.NobodyIsAskedWhetherToChangeSeats` asserts `IPlayerAgent` still has **five**
questions and that the decision takes nothing but an `int`;
`WhatAgreementMeansIsNotDecidedByCountingRounds` asserts the shipped policy counts rounds and
nothing else (§9 #47 — everybody or most — is still open and still recommended **unanimous**).
**P37 is expected to change both**, and the coverage registry's §3 entry names them so it is
obvious which.

⚠️ **One leftover P36 did not take**: no view says what the seats are doing. `AboutTable` does not
carry the setting, so a person at a browser table cannot find out whether the seats hold — one
line, and P37's natural neighbour.

---

### What P24.2 built, for the session that opens P36 (2026-08-22)

**Read this before touching `CoverScore`, `ComputerAdvice`, `SeatPrompt` or the journal.**

🔥 **1. `CoverScore.Ranking` is now a projection of `CoverScore.Scored`, and that is load-bearing
rather than tidy.** Everything a rung compares is computed in `Scored` and thrown away a line
later; the whole claim that an explanation is *free* rests on there being one function, not two
that agree by inspection. **Do not add a second ordering.** ⚠️ The refinement key is `long?` and
**null means the key was never asked** — a candidate that lost on cover count is never scored on
the expensive one. Null and zero sort identically there, so nothing measured moved, but **reading a
null back as a zero would publish a number nobody took.**

🔥 **2. A rung must never hand a front end a bare `long`.** `DiscardKey` carries a name, a
direction and `BeyondMeasure` — the phrase to print when the value is a sentinel. Today there is
exactly one sentinel: `CoverScore.Potential` returns `int.MaxValue` for a joker, which is not a
partnership but a **refusal**. `CoverScore.Partnership` is the one place that is read back.
`BotCatalogTests.EveryKeyARungDeclaresCarriesANameAndADirection` fails the build if a fourth key
arrives without one, and `TheStrongestRungCanExplainItsOwnDiscards` fails it if a rung is promoted
to `BotCatalog.Hardest` that cannot explain itself. ⚠️ **It names `Hardest`, not `Ladder[^1]`** —
P31 found that coincidence written down as a law in three places at once, and this is a fourth
place somebody could "simplify" it back into.

🔥 **3. `ComputerAdvice` holds one decision, keyed on the identity of the `TurnContext`.** The
engine builds a fresh context per decision, so the memo remembers one and forgets it when the next
arrives. ⚠️ **It must never become a cache across turns**: a context's hand is the seat's own live
list, and an answer kept past the discard describes cards that have gone (P13.1's finding, which
this is one edit away from re-committing). It is safe for a whole table's seats to share **because
a table plays one turn at a time** (§3.6) — a table that ever played two would need this reworked
before anything else.

⚠️ **4. `SeatPrompt` carries a `Rationale` and a `TableEvent` must never.**
`ConcealmentTests.NoTableEventCanCarryTheComputersReasoning` asserts that over the **type**, not
over a played round. A rationale names cards of a hand and says what the computer would keep, so an
event carrying one hands every watcher a commentary on somebody else's thirteen (§3.11 A1).
🔥 **This is the constraint P37 will meet first**: the first *public* question this project asks
cannot ride on a `SeatPrompt`, and it cannot ride on a `TableEvent` carrying advice either.

⚠️ **5. A sixth `SeatQuestion` now fails quietly in two more places, both deliberately.**
`RemotePlayerAgent.Why` has a null arm and `TurnPrompt.razor` has an inert final arm. Both are the
`JournalFormat.Name` lesson applied — *a default arm is a mistranslation waiting for the next
case* — but **quiet is still quiet**. A packet adding a question must visit both.

⚠️ **6. Two test-fixture findings worth more than they look.** `ScriptedSeat`'s old no-hint
fallback — *throw the first loose card* — **does not terminate**: it leaves the hand it started
from rearranged, and a table of such seats runs until the round is abandoned on the clock. It
throws back the card just taken now, which stands still while the bots race to thirteen. And a
table with two person-seats needs **both** scripted, or the unattended one spends its whole
patience on every question and the round dies on the clock rather than on the assertion.

✅ **7. The prose the product now ships, and what could rot.**
`AdviceRationale.ForObjection` tells a player that refusing a claim *"has been measured, and is
worth nothing either way"* — P29's §12 null, said out loud, because **a null makes the explanation
more interesting rather than less**: it is a thing this project knows and no player does. It
**carries no number on purpose**, so it cannot rot into a wrong figure; if the measurement ever
separates, that sentence is the thing to change. **P34's staleness habit should know it is there.**

---

### What P33 built, for the session that opens P32 (2026-08-22)

**Read this before touching `Settlement`, `TableRules` or anything that reports money.**

🔥 **1. A table size now means two things, and they live together.** `TableRules.For(n)` used to
answer *what must a declared hand contain* (§7.1.1); it also answers *what is a jokerless
declaration worth* (§7.3). **The two split at exactly the same seam — 2/3/4 against 5+ — and
`TableRulesTests.TheTwoRulesThatMoveWithTheTableSizeSplitAtTheSameSeam` asserts the seam rather than
the cases**, so §9 #37's default (six-plus is ×3) is carried by the shape of the rule instead of by
an enumeration. **A third per-seat-count rule belongs in that file too.**

⚠️ **2. `Meld.IsClean` is a trap and the codebase now says so in four places.** It implements
§7.1.1's *required clean series*. §7.3's condition is *no joker in the declared thirteen* — a
property of the **cards**, not of the partition, which is why `HandEvaluator` needed nothing and why
the "which partition is the winner paid on?" problem rev 25 flagged never arose.

🔥 **3. The thing most likely to bite a later packet: a net delta is split in three places and only
one of them is the domain.** `Settlement.ForRound` returns one number a seat; **the console's
settlement panel and `Sim`'s `SeatRow.Flat`/`SideBet` each re-derive the round/side-bet split in
order to display it**, and before P33 both computed the round half as `stakes.RoundValue`. **Any
future change to what a round pays must go through `Settlement.RoundPayment`, or it lands silently
in the side-bet column that every `money.*` measurement reads.** ✅ **The unchanged
`money.side-margin.*` rows are how P33 proved it had not.**

⚠️ **4. `Settlement.ForRound` takes a seventh parameter now** — the winner's declared thirteen —
and it is **required rather than optional on purpose**: a default would pay flat in silence.
`RoundEngine` passes `seat.Hand` **after** the discard, which is what "the declared thirteen" means
(§7.1: the discard comes first and the reveal follows it). `RoundResult.Jokerless` is the same
question for anyone holding a result, read off the melds.

🔥 **5. What the regeneration is evidence *for*, methodologically.** A change that cannot reach a
seat's decision moves only the rows denominated in the unit it changed. **111 of 116 identical, 5
moved, and the 5 are all `net per round margin`.** P29 reproduced 4 of 91 across a rules change and
P31 reproduced 71 of 88 across a new rung; **this is a third shape, and the three together say what
kind of change a run has just made.** ⚠️ **Expect it and do not go looking for the bug.**

⚠️ **6. What P32 must not assume.** Everything in `STRATEGY.md` is still four-handed. At five seats
§7.1.1 requires nothing clean and §7.3 pays ×3, so **the bonus is the only thing cleanliness is ever
worth there** — the jokerless rate and its value move in opposite directions across the seam.
**Write the prediction down before the run.** The `bonus.jokerless-rate.*` rows and their standing
test already exist, so no harness work is needed to see it.

⚠️ **7. The obvious next rung, and it is not P32's job.** Nothing plays for the bonus, and the
arithmetic says it is worth about a sixth of a round's prize every round to turn one round in six
into one in three — **inside what the apparatus resolves**. A rung that sheds a joker for the
multiplier is a **new rung** under P15's discipline: measured, published, and only then on the
ladder. ⚠️ **It would be the first rung whose decision reads the *scoring* rather than the hand or
the money**, and `prospector` is the model for how such a rung is settled (`BotRung.Ranked`).


### What P30.2 built, for the session that opens P31 (2026-08-21)

1. 🔥 **The conformance harness lives in `Tests/Conformance/` and P31's rung will be audited by
   it for free.** `RuleConformanceTests.OrdinaryRoundsBreakNoSettledRule` seats **every entry in
   `BotCatalog.All` and `DifficultyLadder.All`** in rotation — so `warden` joins the audited
   field the moment it joins the catalog, with no test change. If `warden` breaks a Settled rule
   (the likely one: holding a lock by discarding differently is fine, but a bug in its take
   logic could trip the one-take-one-discard or ban mirror), the audit says which rule and on
   which event.
2. ⚠️ **Adding or renumbering a Settled section in `RULES.md` now fails the build on purpose** —
   `SettledRuleCoverageTests.Registry` must gain a matching entry (a check, or a written
   exemption). That is the acceptance working, not a flaky test. Same for
   `GameJournalTests.TheRevisionStampedIsTheRevisionRulesMdIsAt`: **bump
   `JournalHeader.CurrentRulesRevision` with any play-changing rev** — the `/poker` skill's
   Phase 6 says so now.
3. 🔥 **R1's fix changes engine behaviour in one narrow case**: a discard that was legal only
   under §5.1 exception 2 commits the seat — the engine declares on its behalf and
   `agent.Declare` is not asked. Bots are unaffected (`Declare` was `=> true` everywhere). A
   journal recorded before the fix that contains such a turn replays with a **loud divergence**
   (an unconsumed `declare` line), which is the designed failure. The journal rev bump (13 → 24)
   makes such artifacts distrustable by header.
4. 🔥 **R8's fix restructured the server's seat plumbing**: `SeatChannel` (new) holds the
   question/answer state per seat, stable across occupants; `SeatConnection` is one *occupancy*
   — `SitDown` mints a fresh one, the superseded one is dead server-side (no events, no prompt,
   answers refused), and a standing question **moves** to the new occupant, which is what the
   Web ghost-reclaim test always demanded. ⚠️ **Consequence**: a seat re-taken mid-round starts
   its event history at the handover, like a watcher who joined late — `SeatBoard` copes (its
   fold is per-connection), but anything new that replays a connection's events from the deal
   must use the watcher's, not a re-taken seat's.
5. ⚠️ **`drive-console.py` no longer replays key lists — it answers prompts by their text**, and
   verifies every settlement panel (zero-sum columns, `net = round + money`, one winner at
   `(n−1)·round-value`) before exiting zero. `--script bots|human` now means the people count;
   `--rounds N` plays N settlements. **The old captures' premise was stale**: both fixed lists
   had quietly started reaching round 1's settlement (the BUILD-PLAN grep used `went out`; the
   console prints `declares`). The adaptive bots capture is byte-identical to the old script's
   at the same seed; the human capture now ends whole instead of dying mid-round-2 prompt.
6. ⚠️ **Two review items were left as recorded decisions rather than code**: R30 — `DealBuilder`
   still deals jokers as filler; the remark now says so truthfully, because re-basing filler
   would re-derive every hand-computed payout in the suite (the review sanctioned the remark
   fix) — and R20/R33 stay with their named later packets. **R18/R34/R35 stay won't-fix** (R34
   got its "deliberate observable" remarks).
7. ⚠️ **P31's suite regeneration lands two CSV changes that are fixes arriving, not
   regressions**: `claim.permission.money.refuse-over-allow` becomes paired (±0.18 → ~±0.25,
   review R3 — the null survives) and four `money.side-margin.*` rows appear (review R13).
   `STRATEGY.md` §11/§12 carry annotations to update when the corrected rows exist.
8. ⚠️ **A capture from before this session does not compare for the `human` script** (different
   play: the adaptive driver declares and quits cleanly) but **does for `bots`** — proved by
   `cmp` this session. R5's console narration fix changes bytes for *both* against pre-P30.2
   trees; capture fresh baselines before any front-end refactor.

### What P24.1 built, for the session that opens P30.2 (2026-08-21)

1. 🔥 **The browser half of P30.2 now has its instrument.** A hosted table opened with
   `TableOptions.Journal` (the Web: `--journal <path>`, or `TablePlan.Journal` in a test via
   `HostedTableTests.Open(journal: …)`) records what every seat was asked and answered, and the
   file is rewritten whole after every settled round — so a browser round can be audited
   **without closing the table**. `Tests/Server/TableJournalTests.cs` and
   `Tests/Web/HostedTableJournalTests.cs` are the working examples.
2. ⚠️ **`TableSession.Journal()` must be asked between rounds.** The decisions are appended on
   the round's own thread; a build mid-round would be torn. `HostedTable` is on the right side
   by construction (it flushes on the dealer thread after `PlayRoundAsync` returns); a test
   that holds a `TableSession` directly must call it after `PlayRound`, not during.
3. ⚠️ **The journal wraps outermost — outside `BoundedAgent`** — so what it records is the
   answer that reached the engine, a stand-in's or the clock's included. That is the answer a
   replay has to give; do not move the wrap inward to record "what the person would have said".
4. ⚠️ **Every journal this tree writes still stamps rules rev 13** — the hosted one included,
   deliberately: R2 is P30.2's fix and `JournalHeader.CurrentRulesRevision` is one constant for
   all three writers. Fix it once there, in the P23 idiom (parse the rev out of `RULES.md`;
   fail the build on disagreement).
5. ⚠️ **A trap dodged, worth keeping dodged**: the lobby's *form* does not offer the journal
   flag. Two tables writing one path would take turns overwriting each other; the flag names
   the house table only. If P30.2 wants a second journalled table in one process, give each
   table its own path rather than surfacing the flag on the form.

### What P30.1 found, and what it leaves behind (2026-08-21)

1. 🔥 **The review's whole output is `docs/REVIEW-2026-08.md` — read it before starting P30.2;
   its triage table is P30.2's work list.** 37 findings: 1 CRITICAL, 4 HIGH, 10 MEDIUM, the rest
   LOW; buckets are *P30.2-checklist* (tests to write: R1, R6, R7, R8's browser half),
   *P30.2-fix* (29 small safe fixes), *named later packet* (R20 → P24.1's fold, R33 the ×5
   surfacing — still Nick's call), and *won't fix* (R18, R34, R35, each with its because).
2. 🔥 **R1 is the one genuine rules-conformance defect in the engine**: §5.1 exception 2 offers a
   banned rank whose throw would leave a winning thirteen, but nothing binds the throw to the
   declaration — both front ends let a human decline afterwards. Bots always declare, so no
   published figure moves. The fix belongs in P30.2 beside the conformance test that would have
   caught it.
3. ⚠️ **R2 needs to become a habit as well as a fix**: `JournalHeader.CurrentRulesRevision` sat
   at 13 through four play-changing revisions because nothing binds it to `RULES.md` and no
   checklist names it. P30.2 should bind it in the P23 idiom (parse the rev out of the document;
   fail the build on disagreement).
4. ⚠️ **R3 means one published number is annotated rather than wrong**: `STRATEGY.md` §12's
   claim-permission money margin `+0.02 ± 0.18` was computed with the independent formula on a
   within-cell comparison; the true paired interval is wider (~±0.25) and the null survives. Fix
   the code in P30.2 (a `NetPerRoundByGame` on `CellPlayer`); **the corrected row regenerates
   with P31's already-planned suite run — do not spend 2¾ hours on it alone.**
5. 🔥 **The review's method finding, for the next review**: the fan-out worked — four scoped
   parallel reviewers plus one deep read of the rules core, all briefed with the nine defect
   classes and the rev-24 rules facts — and the defect classes predicted from history were the
   ones found (a default arm rendering a sixth question as a declaration in two more places, a
   test that cannot fail guarding the one cross-deal property, teaching text stating dead
   rules). **Reading found what running never had**, which was the packet's premise.
6. ⚠️ **What the review did not cover, so nobody assumes it did**: the prose docs
   (`RULES-PRIMER.md`, `PLAYING.md`) were not checked against rev 24; the meld-generator test
   files were skipped as older-and-heavily-worked; CSS was read, not rendered. The review says
   so in its own "What was not read".

🔥 **(As the plan stood before the 2026-08-21 branch — kept as the record.) Every planned packet
is done. P24 is the only one left, and whether to build it is Nick's call** — it was re-sequenced behind P29 on 2026-08-21 as a *recommendation*, since its stated
reason for going first (*§5.1 is blocked, and P24 makes that conversation productive*) is spent
and P26–P28 changed decisions it would have explained.

### What P29 found, and what it leaves behind

1. 🔥 **The finding that outlives the packet: a prediction that was wrong located an effect that
   nobody had asked about.** P29 predicted `outs` would narrow against `greedy` because every rung
   maximises cover count and cover count no longer wins at four seats. **It did not move at all**
   (`+3.0 ± 1.0` against +3.1) — but **`simple` gained about two points on each of the three
   middle rungs**. ⚠️ **The four-handed condition requires a joker-free series and no rung is
   aimed at that, so the better melder pays the same tax**: a requirement nobody optimises for
   compresses a ladder from the bottom instead of tilting it. **A packet that only checked its own
   prediction would have recorded "no change" and missed the whole effect.**
2. 🔥 **"Does it reproduce" cannot be asked across a rules change, and the number proves it.**
   **4 of 91 rows byte-identical, and all four are the ε constants a human typed.** P23's 59-of-77
   was a claim about determinism; this run's 4-of-91 is not a failure of it. ⚠️ **The only rows
   that survive a rules change are the rows that are not measurements** — so a future packet that
   changes a rule should expect this and should *not* go looking for the bug.
3. ⚠️ **The five-hour suite budget was wrong in the cheap direction and had been quoted in three
   places.** It is **9,981 s — two and three quarter hours** — with a cell *added*, because `outs`
   costs **7.0× a `greedy` round** now against 8.2× at P21: P25's win condition prunes the cover
   search earlier than it lengthens the round. `CLAUDE.md`, `BUILD-PLAN.md` and `STRATEGY.md` all
   said five hours and all three are corrected. **Re-time with `sim bench` rather than trusting a
   number in prose.**
4. ✅ **A null was published, and the branch behind it was measured first.** Refusing a claim is
   worth `+0.4 ± 1.0` on win rate and `+0.02 ± 0.18` on money. **What makes it worth having is the
   denominator**: the opener asks in about a quarter of rounds and the upstream seat holds the
   rank about half the time, so the veto fires about one round in seven. ⚠️ **A null over a branch
   that never fires says nothing** — that is why `claim.attempt-rate.*` and `claim.refusal-rate.*`
   are published beside the margin rather than left implicit.
5. ⚠️ **The abandoned count is not zero and the honest statement is narrow.** 8 games of 9,072 in
   the ladder field — **all of it the field containing `random`**, which plays 196 turns a round
   against every thinking rung's 24 to 31. Both all-`outs` fields settled every game. **P27 broke
   the argument that a table of bots must converge; this says no table of thinking rungs has yet
   failed to, not that none can.**
6. ⚠️ **`SeatRow.Claims` counts claims *asked for*, and P28 is what made that different from
   claims *got*.** A claim can be refused, so `claims − claims_refused` is what actually left the
   table. **The CSV grew a `claims_refused` column** and the field is round-level rather than
   per-seat, because a refusal is one seat stopping another and attributing it to either reads as
   something that seat *did*.
7. 🔥 **`outs/refuse` is an experiment's arm and the control is that it is the shipped player.**
   Every rung in `BotCatalog` refuses whenever §4.5 allows, so a table of `greedy/refuse` must
   play the same rounds card for card as a table of `greedy` — asserted, along with the
   anti-vacuity half that `greedy/allow` does not. ⚠️ **Two homogeneous tables and never a
   head-to-head cell**, which is the trap P22 fell into: two labels of one player sit in different
   seats there and differ by seat luck however identical they are.
8. ⚠️ **A statistic the runner collects and the suite does not publish is invisible.** Round
   length, the abandoned count and the refusal rate were all *already computed* by
   `SimulationReport` and `TournamentCell` and none of them reached `docs/`. **Each needed a
   hand-written block in `Suite.Run`** — which is the habit `STRATEGY.md` §11 has been recording
   since P23 and which P29 paid rather than removed.
9. ✅ **The table-generating helper was validated against the document before it rewrote it.**
   `STRATEGY.md`'s §3 Holm column is recomputed from the CSV's means and standard errors; P29
   re-derived all fifteen rows from the *old* CSV first and got the published text back exactly,
   then re-ran it on the new one. **An instrument checked against a known answer before use** —
   the same move P26 made with the side-bet measurement. ⚠️ **It is a scratch script and not in
   the repo**; the tables are still typed by hand, which is the last transcription step in the
   chain and a candidate for a small packet of its own.

### What P28 found, and what it leaves behind

1. 🔥 **The finding that cost the most is older than the packet, and it is a rule about
   serializers.** `JournalFormat.Name(JournalQuestion)` ended `_ => "declare"`, so the moment a
   fifth question existed **every objection was written to file as a declaration** — a journal that
   reads back as a different game. ⚠️ **The in-memory replay test could not see it**, because it
   never crosses the format; only `AJournalWrittenToAFileReplaysFromIt` went red, and it went red
   at round 2 rather than round 1, which reads at first like a re-seating bug and is not.
   **A default arm in a serializer is a mistranslation waiting for the next case.** Every case is
   named now and the default throws.
2. 🔥 **Re-seating and the scripted deal are in tension, and P7's own argument settles it.** A deal
   written down card by card is a deal written down *for a seating*, so `MatchEngine.PlayRound()`
   draws the seats — shuffle, seat, deal, which is §3's order — and `PlayRound(drawOrder)` does
   not. ⚠️ **Putting the draw in the shared path hangs the suite rather than failing it**: the
   winning hand goes to whichever seat is dealt first, the agent that knows how to declare is
   somewhere else, and a table of passive seats plays for ever. Every production caller uses the
   parameterless overload; every scripted test uses the other.
3. ⚠️ **Three places in the server assumed *being asked* meant *being on turn*, and exactly one was
   load-bearing.** The permission carries the **opener's** turn number, so `BoundedAgent` announcing
   it would move every client's spotlight onto the wrong seat mid-turn — it checks the clock and
   stays quiet. `RemotePlayerAgent`'s takeover announcement is left as it is on purpose (the
   computer really did play that seat's decision), and `PacedAgent` beats once, which reads as the
   seat thinking.
4. 🔥 **A test that answers "no" can hide the question after it.** `ClickingPlayer` and
   `ScriptedSeat` both declined every claim, so the permission was unreachable from either fixture
   — and `SeatBoardTests.EverySeatIsAskedEveryQuestionOverAMatch` would have failed on a case it
   could never produce. **`ClickingPlayer` claims now**; pressing *Claim* is as much a thing a page
   can do as pressing *Leave it*.
5. ⚠️ **Whether to object is a decision, and half of it is unmodelled.** Every rung refuses whenever
   it may, on §4.5's own reasoning — the claim would close that rank in its hand — and **nothing
   prices the disclosure**, which is real: only a holder may refuse, so refusing says you hold one.
   It is the one place a future rung could differ without touching the engine. **P29 should measure
   it**: how often a claim is refused, and a head-to-head between an always-refusing and an
   always-allowing policy of the same rung. **If it is a null, publish it** (P20).
6. ⚠️ **The seating became narration, and that is what reached the front ends.**
   `IGameObserver.RoundStarted` and `TableEvent.RoundStarted` both carry the seating now, so the
   console prints the seats at the top of every round instead of once at setup and `TableBoard`
   re-lays the ring between deals. ✅ **P13.5's *you at the front whichever seat you were dealt*
   needed nothing** — `TableRing.Around` is already given a seating and a viewer, so it simply
   receives a different one.
7. ⚠️ **Two things a capture and a seed can no longer do.** A seed from before P28 does not play
   the same match (the seating draw takes numbers the deal used to take; round 1 is unaffected),
   and a `drive-console.py` capture from before P28 does not compare — the console prints a
   `Seats drawn:` line every round and a human seat can be asked for permission.
8. ✅ **The `git worktree` baseline trick was used again and is still worth it**: `git worktree add
   <scratch> HEAD && dotnet test` proved **642 / 0 in 9 m 09 s** while the packet was being
   written.

### What P27 found, and what it leaves behind

1. 🔥 **The finding that outlives the packet: a bot's cover count can now fall, and that breaks
   the argument that a table of bots terminates.** `GreedyBotAgent`'s own remarks say the score
   *"can never fall — throwing back the card just taken restores the hand exactly"*, and that is
   the stated reason a round of bots reaches a declaration at all. **§5.1 takes the just-taken card
   out of the choice.** A seat whose only legal discards are melded ones gives up a meld, so
   convergence is no longer guaranteed by construction. ⚠️ **In practice the tree finishes and the
   suite is green**; what stands behind it is `SimulationOptions.TurnCap` and the hosted table's
   `RoundTimeLimit`, both of which existed for `RandomBotAgent` and now carry a second job.
   **P29 should publish round lengths and abandoned-round counts, not only win rates** — nothing
   published has ever quoted the abandoned count, and if it is not zero that is a result.
2. 🔥 **The rank-only predicate is one line, and the house ruling falls out of the type.**
   `Card.SameRankAs` is `Rank == other.Rank` — nullable equality, so two jokers match and a joker
   matches nothing else, which *is* §9 #27's ruling that taking a joker closes the other jokers.
   ⚠️ **It reads like a trivial field comparison and it is not**: `SameValueAs` would compile, pass
   most of the tests, and implement the wrong rule. **P28 must reuse this method** for the claim's
   objection (§9 #30), not write a second one that agrees by inspection.
3. 🔥 **The filter became a property of the ladder rather than a line each rung remembers.**
   `CoverScore.Discard` and `CoverScore.Ranking` take a `TurnContext` instead of a hand, so every
   rung that ranks is filtered by construction — **and so is the runner-up a difficulty level
   throws as its mistake**, which was the thing §5.1 said was easiest to get wrong. Only
   `RandomBotAgent` needed a line of its own, because it is the one rung that does not rank.
   ✅ **A rung added later cannot forget**, and `LegalDiscardsTests` runs over `BotCatalog.All` and
   `DifficultyLadder.All` rather than over a sample.
4. ⚠️ **The floor is one line; the declaring-discard exception is not.** Exception 2 needs
   `HandEvaluator.IsWinning` per banned card, gated behind one
   `PartialCover.CoversAtLeast(hand, 13)` so it is asked only of a hand that could actually go out
   — and only when a rank is closed *and* held. **An ordinary turn returns the hand itself**, same
   reference, no allocation, no search.
5. ⚠️ **"Domain only" was false again, and in a new way.** §5.1 makes a card *unpressable*, so both
   front ends had to change: `CardDisplayState.Unthrowable` with the token `closed`, a legend
   entry, an accessible name, the console dropping the card from its `SelectionPrompt`, and the
   browser drawing it as a `<span>` rather than a disabled button — **a control that does nothing
   is exactly the failure P13.4 spent a packet chasing.** 🔥 **So a `drive-console.py` capture from
   before P27 no longer compares either**, for the second packet running.
6. ✅ **The server refuses a banned discard where it already refuses a card you do not hold.**
   `SeatPrompt.MayThrow` replaces `Holds` in `SeatAnswer.Discard.Fits`, so a bad client's answer
   does not fit, the question stays standing and the stand-in eventually plays — **rather than the
   engine's guard throwing and taking a hosted table down with it.** The engine guard stays, for
   anything that gets past that.
7. ⚠️ **A test that plays a round has to be given a way to end, and this bit twice.** Two of the
   new engine tests hung on the first run: a table of seats that draw blind and throw back what
   they drew never finishes, because only a declaration ends a round (§7.1) and §5's reshuffle
   keeps the cards circulating. **`ScriptedPlayerAgent` advances per turn *that seat* is asked**,
   not per table turn, which is the other half of the same trap.
8. ✅ **`FeedingBanInPlayTests.AnOrdinaryRoundClosesRanksAndNeverOffersOne` is the anti-vacuity
   test.** Everything else in that file scripts the ban into existence; this one plays a round of
   greedy bots and asserts §5.1 **binds somebody** in it. Without it a filter that quietly did
   nothing would pass the whole file.
9. 🔥 **Measured, and the headline is how little it costs: the ban is nearly free at four seats.**
   Same command before and after (`--games 200 --strategies greedy,simple --seats 4 --seed
   20260821`), *before* being the P26 tree in a worktree at `HEAD`:

   | | turns a round | rounds/s | greedy | simple |
   |---|---|---|---|---|
   | before (P26) | 26.6 | 69 | 31.3 ± 3.4 | 18.8 ± 3.4 |
   | after (P27) | **27.1** | **69** | 31.5 ± 3.4 | 18.5 ± 3.4 |

   **Play moved by one round out of 200 and throughput did not move at all** — so the exception-2
   gate is doing its job and the filter costs nothing measurable. ⚠️ **The reason it is this small
   is worth knowing before P29 reads too much into it: a seat takes in the open on only about a
   fifth of its turns** (`take %` is 20–23%), so a rank is closed for far less of a round than the
   rule's prominence suggests. **At a table of stronger rungs, which take more, expect more.**
10. ⚠️ **Two shipped tests went red and both for the same reason — a fixture that had been legal
   became an illegal move.** `RoundEngineTests.OnlyTheDealAndABlindDrawEverConferOwnership` had a
   passive seat draw a 3♠ and throw it straight back, one turn after Alice claimed the 3♣ in the
   open; the fixture draws a 4♠ now. And `SkillLadderRunTests.ThinkingBeatsNotThinkingByAMile`
   asserted `random < simple` over **eight** games, and `simple` drew a blank — it wins **29.0%**
   over two hundred, so eight was a coin toss the ban tipped. **Thirty-two games now.** 🔥 **Both
   are the same lesson P21 left about round length: a test over a *played round* can be asserting
   a property of that round nobody wrote down.**

### What P26 found, and what it leaves behind

1. 🔥 **The packet's own prediction was the deliverable and it came out right.** `RULES.md` §10 #17
   said, in advance, that
   `ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe`'s last assertion
   *"should invert to an equality"* — that under the ×3 a designation on the 7♦ and one on an
   ordinary card leave **exactly the same** money loose in the shoe — and that **it had not been
   run**. It was run. **They are equal to nine decimal places.** So §4.1's conservation arithmetic
   is sound and the rule did **not** need re-asking, which was the stated fallback.
2. ⚠️ **`STATUS.md` predicted that test would be *red* before P26 started. It was green at
   `HEAD`.** The code still implemented the withdrawn rule, so the test agreed with the code and
   disagreed only with the document. 🔥 **A test cannot go red for a rules change until somebody
   changes the code** — what a shipped assertion buys you is that the change *cannot be made
   quietly*, not that the tree tells you it is due.
3. 🔥 **Seven shipped tests went red and every one of them for the same reason: jokers are dealt.**
   Four of 108 cards are jokers, so an arbitrary four-handed deal hands out one or two of them —
   and five tests carried the comment *"nobody owns a money card, so only the flat round value
   moves"*, which stopped being true the moment `Permanent` grew. **The blast radius of rev 21 is
   not the money layer; it is every golden settlement number in the tree.** ✅ All seven are
   genuine expectation moves and each now names the jokers rather than restating a number.
4. 🔥 **The side-bet is `$11.58 ± 0.34` a round at five seats, against `$8.50 ± 0.26` before** —
   **58% of the $20 round prize, up from 42.5%**, and 37% of all the money that changes hands.
   ✅ **The instrument was validated before it was used**: the *before* run reproduces P12's
   rev-13 published figures ($8.43 / 42% / 30%) at a different seed, so the difference is the
   rules change. **Method** — `--games 600 --seats 5 --seed 20260821 --csv`, then sum each round's
   positive `side_bet` column; it is worth keeping, because nothing in `Sim` reports gross
   side-bet movement and the CSV does.
5. ✅ **Play did not move at all.** Same seed before and after: identical wins, turns and cover.
   **This is the first time §4.4's decoupling claim — money never distorts the melding game — has
   been measured *across* a change to the money layer**, and it held exactly.
6. ⚠️ **"Domain only" was true of the logic and false of the text.** `($$)` had to become `($$$)`,
   `CardDisplayState.PaysDouble` became `PaysTriple`, and four user-facing strings said *double*.
   🔥 **So a `drive-console.py` capture from before P26 no longer compares** — P23's promise that
   a pre-P23 capture still does is spent. Re-capture from `HEAD` first.
7. ⚠️ **No view can show a ×5, and no packet owns the gap.** `CardView.Multiplier` is 0, 1 or 3 by
   construction, because a marker beside one card cannot depend on the rest of the round's
   ownership. The jackpot is settled and never drawn — a player owning both partners sees `($$$)`
   and is paid ×5. **Candidate work for P24 or a UX packet; it is not a bug in P26.**
8. ⚠️ **`MoneyOdds` does not price the ×5 either**, and says so beside the two approximations it
   already had. It sums the value-only multiplier over the shoe; the turn-up is the 7♦/A♠ pair
   about **one round in 1,444**, and the understatement is against drawing blind, which is the
   conservative direction for `prospector`, the one rung that reads it.
9. ⚠️ **§9 #32 is fenced by two tests, not by a comment.**
   `TwoTripledJokersInOneHandAreNotAJackpot` (both colours, and both copies of one colour) and
   `ASevenAndAJokerTurnedUpAreNotAJackpotEither` assert ×3 for the combinations rev 21 *created*.
   **Widening the rule has to break them.**
10. ✅ **The baseline was verified in a `git worktree` at `HEAD` rather than by stashing** — 574/0
   in 9 m 34 s, run alongside the work. `git worktree add <scratch> HEAD && dotnet test` is worth
   reusing whenever a session has already started editing before the baseline is green.

### What P25 found, and what it leaves behind

1. 🔥 **The memo key was the interesting part, not the requirement.** The search state is
   `(covered, seriesStillOwed, cleanStillOwed)`, not `covered` alone — a covered-set from which no
   completion can supply *two* more clean series may perfectly well supply one, so the old key
   would have poisoned the second question with the first question's dead ends. Everything else in
   the search is unchanged: same index, same pin to the lowest uncovered card.
2. ✅ **`Meld.IsClean` needed no special case for the all-joker meld** (`RULES.md` §9 #29). It is
   `Kind == Run && JokerCount == 0`, and three jokers fail it because every slot is a substitute.
   **The rule and the definition agreed without either being bent.**
3. ✅ **Two-handed was the cheap case and cheaper than expected.** Sets are illegal *as melds*, a
   property of a meld rather than of the partition, so `MeldIndex.Build` takes `setsAllowed` and
   the search never sees one. ⚠️ **Filtering on `Meld.Kind` keeps more than it looks like it
   does** — `MeldCandidates` already emits `{9♦,🃏,🃏}` and `{🃏,🃏,🃏}` once, as the **run**
   interpretation, so they survive the filter and a two-handed player may lay them down.
4. 🔥 **`drive-console.py` is blind to this change, and that is the finding worth carrying.** Both
   captures at `--seed 20260819 --pick 0` are **byte-identical to the pre-P25 tree** —
   `--script bots` 8,417 bytes, `--script human` 90,251 — **not because play is unchanged but
   because both scripts quit in round 2 and neither capture contains a declaration at all**
   (`grep -c "went out"` is 0). ⚠️ **Do not read a clean `cmp` as evidence about play.** The
   instrument that proved P21 and P23 were refactors cannot see the win condition. A front-end
   check that covers going out needs the script extended to play a round to its end — new work,
   and it should be priced before it is promised.
5. ⚠️ **`PartialCover` was deliberately left alone and is now measuring something else.**
   `IsComplete` agrees with `HandEvaluator.IsWinning` **only at five or more players**; at two,
   three or four a hand can cover exactly and still lose. Every rung in `BotCatalog` maximises
   cover count, so **every rung's objective is now the wrong objective at the table every
   published figure was measured at.** That is prediction 2 of P29 and it is where it gets priced,
   not fixed.
6. ⚠️ **`RoundEngine.MinimumPlayers` is still 4, and no packet owns changing it** (`RULES.md`
   §10 #7). `TableRules.For(2)` and `For(3)` are correct and tested and **unreachable from a dealt
   game** — the two strictest rules in §7.1.1 never execute. Making a table smaller than four
   playable needs the deal, the money layer and both front ends to agree, which is a packet of its
   own rather than a line in someone else's.
7. ⚠️ **A console capture from before P25 still compares** (proved, above) — but only by accident,
   and the accident is item 4. **The comparability note in `CLAUDE.md` is about front-end
   rendering and has never been about play.**

**The older note below is kept for its account of where the work came from.**
 P0–P23 are all done and every goal in §0 is delivered — the rules core, a
console worth sitting at, simulation at scale, a browser lobby two people can play in, and a
difficulty system with a settled answer to what actually works — ⚠️ **but the plan stopped being
the whole of the work on 2026-08-20**, when a playtest produced both `RULES.md` §5.1 and the
reason P24 exists.

**What P24 is for, in one line: playing beside an expert and coming away with a record of where
she disagreed.** The scope is Nick's, taken 2026-08-20 — **browser only**, **all four questions**,
**winner versus runner-up** rather than a ranking table, and **written to a journal** as well as
drawn. Three things a cold session should know before opening the packet:

- 🔥 **`BurmesePoker.Web` and `BurmesePoker.Server` have no journal at all.** `--journal` is on the
  console and the harness; the hosted table has never written one. That is a piece of work inside
  P24, not a field on an existing writer.
- 🔥 **What gets recorded is an opinion beside an answer, not a rationale on a decision.** At a
  table with an expert in it the answer is *hers*; the adviser's card and reasoning go beside it,
  so **disagreement is a query rather than something a person notices**.
- ⚠️ **The keys the explanation renders are packed for sorting, not for reading** — `outs` stores
  its key negated, `Potential` returns `int.MaxValue` for a joker, `cautious` packs two into one
  `long`. A rung describes its own keys; presentation never reads a bare number.
- 🔥 **Never explain through `FallibleAgent`.** A difficulty level is `Hardest` with a mistake
  rate, and P19's mistake *is* the runner-up of the very ranking P24 renders — so a level in the
  advice path would produce a page confidently justifying a move chosen because it was second
  best. The adviser is the bare rung at ε = 0, and P24 is the first thing that could get this
  wrong. **Acceptance 6 asserts it at every level, `easy` included.**

⚠️ **P24 and the §5.1 packet touch the same code and P24 goes first.** §5.1 makes a banned card an
illegal discard, which filters every agent's ranking — the very ranking P24 renders — but it is
blocked on four unrecorded specification questions (§9 #16–#19 — ⚠️ **all since answered; this line is history**) and P24 is what makes that conversation
productive. See **Decisions needed from Nick**.

**If you are picking this up cold and want to change something, the two documents that say what is
true are `docs/RULES.md` (rev 15) and `docs/STRATEGY.md`.** Neither is prose to be edited by hand:
a rule carries a provenance tag and a measurement is generated by `sim suite`.

⚠️ **The three ways this tree can go quietly wrong, and what fails when they do.**

1. **Add a rung and never re-run the suite.** `StandingAnswerTests` goes red: every rung in
   `BotCatalog` must be the subject of a published row in `docs/strategy/measurements.csv`. The
   run is **two and three quarter hours** (9,981 s, P29; it was five at P23) — budget it before
   adding the rung, not after, and re-time it with `sim bench`.
2. **Move a level's ε.** The same file goes red: the ε values published are asserted to be the ε
   values `DifficultyLadder` offers. ⚠️ **Re-run the suite, do not edit the CSV.**
3. **Raise the ceiling.** A rung that separates becomes `BotCatalog.Hardest`, and every difficulty
   level, the browser's hint and the stand-in seat re-base onto it. `sim suite` exits non-zero if
   the dial stops being *monotone* — but 🔥 **P23's actual finding is that the failure mode is not
   inversion, it is a flat spot**: P21 left the dial ordered and passing every check with a middle
   step less than half the size of its neighbours. **Re-sweep ε and re-fit the spacing.** Expect
   to re-check rather than re-derive — the curve barely changed shape between `greedy` and `outs`.

⚠️ **The one saving left in the suite, and why it has not been taken.** `--pairs adjacent` on the
ladder would take fifteen head-to-head cells to five. It would also **throw away the matrix in
`STRATEGY.md` §3**, which is the document's centre and the only place `outs` is shown beating
every rung rather than the one below it. It stays available and stays unused. The saving that
*was* free — not measuring a rung in a cell where it is a known duplicate — was taken in P23 via
`BotRung.Ranked`, and cost nothing: every ladder figure reproduced to the digit without it.

⚠️ **What P21 and P22 changed about how to think about a new rung** — read this before writing
one. **P22 added items 5 and 6, and they are about cost and about scope rather than about odds.**

1. 🔥 **Where the new key goes decides whether it can pay.** Three research rungs are now
   measured. `cautious` and `counting` both slid their idea in *beneath* greedy's tie-break and
   both returned nothing (`+0.5 ± 0.55` and `+0.3 ± 1.0` the wrong way); `outs` put its idea
   *above* it and returned `+3.1 ± 1.0`. **Greedy's leftovers are worth about half a point and
   the apparatus cannot see half a point** (§7 of `STRATEGY.md`), so a rung that only refines
   the residue is unmeasurable before it is written.
2. ⚠️ **A rung is not free of the dial.** `Strength` is *known* strength, so a rung that
   separates gets a higher number, `BotCatalog.Hardest` moves, and all four levels re-base onto
   it — along with the browser's hint and the stand-in seat. That is correct and intended; it
   also costs, because `FallibleAgent` asks its inner rung to **rank**, so even `easy` pays the
   new rung's price.
3. ⚠️ **State the throughput budget in advance, and time the *decision*.** `sim bench` now plays
   whole rounds of one rung at four seats and prints rounds/s, µs/turn and a multiple of greedy;
   the primitives underneath it were never the whole story and for `outs` they were a quarter of
   it.
4. ⚠️ **A speed-up is a claim about answers.** Every one of P21's four is asserted against the
   search it replaces over real hands (`OutsBotAgentTests`), and the generator work was proved a
   refactor by capturing the console byte for byte at `--pick 0` from `HEAD` and from the tree.
   **Do not ship a shortcut whose only evidence is a comment explaining why it is safe.**
5. 🔥 **A rung that measures nothing still costs the standing suite for ever** (P22). A new name
   in `BotCatalog` is `k−1` new head-to-head cells in every future `sim suite` run, whatever it
   turns out to be worth — `prospector` added six and about three quarters of an hour, and all
   six are `outs` against itself in all but name. **Before adding a rung, ask whether the
   standing instrument is the right place to measure it**, and if it is not, say where is.
6. ⚠️ **A rung whose decision reads the *stakes* is not one player** (P22). The stakes are fixed
   at the start of a game and are not a rule (RULES.md §4.3), so `prospector` is literally
   `outs` at $5/$1 and a different player at $5/$40. That is why `BotCatalog.Strength` is an
   **ordinal of known strength** and not a score, why such a rung shares its neighbour's number,
   and why it is measured in a sweep of its own rather than ranked in a field played at one
   stakes.

⚠️ **P15's precedent is still designed into the plan** — a whole packet on a plausible rung
worth +0.5 ± 0.55 points — and P17 priced it: at 8,008 games a cell a rung worth less than about
a point is not promotable, and half a point costs ~34,000 games a cell.

⚠️ **What P19 leaves for whoever adds a rung.**

1. 🔥 **A new rung changes the difficulty dial, whether or not anybody meant it to.** Every level
   is `BotCatalog.Hardest` with an ε, so a rung stronger than `greedy` becomes the base of all
   four levels the day it lands, and the ε values stop being the ones that were measured. **`sim
   suite` fails the run if the dial stops being monotone**, which catches the worst case but not
   a dial that has merely gone uneven — re-run the calibration and re-space it (P23 owns this).
2. ⚠️ **A rung that a level can be built on has to implement `IRanksDiscards`.** Three of the
   four do; `random` does not, and `FallibleAgent` refuses it rather than accepting an ε that
   does nothing. A rung whose discard does not come out of `CoverScore.Ranking` gets the
   interface for free only if it is written that way.
3. ⚠️ **`ComputerAdvice` and `TableOptions.StandIn` are `BotCatalog.Hardest` — the *rung*, not a
   level — and now say so in three places.** A hint that got worse as you lowered the difficulty
   would be absurd, and a seat the computer took over for somebody should not start playing
   badly.
4. ⚠️ **`--strategies` resolves three vocabularies now**: a rung, a difficulty level, and a
   calibration probe such as `greedy@0.35`. `LayeringTests` bans constructing either a rung or a
   `FallibleAgent` outside `Domain/Agents`.

**What P19 built, in one paragraph.** Three new files in `BurmesePoker.Domain/Agents` —
`DifficultyLadder.cs` (with `DifficultyLevel`), `FallibleAgent.cs`, `IRanksDiscards.cs` — a
`Ranking` on `CoverScore` that `Discard` is now the head of, `PairFamily` and
`TournamentReport.Met` in the harness, a second tournament inside `Suite`, and the two front ends
rewired from `BotCatalog` to `DifficultyLadder`. **`TablePlan` gained `Difficulties`** — a level
per computer seat, with the existing single-value `Difficulty` as its shorthand. **Nothing in the
engine changed**, which is the fifth packet in a row to add a whole capability over seams that
already existed.

**How to prove a console change is a refactor, now that the prompt is a list.** Capture before
and after and compare **from the `Seating:` line on** — the prompt above it is allowed to differ
— and pass `--pick n` so both captures chose the same setting: `--pick 0` is `expert`, `1` `hard`,
`2` `medium`, `3` `easy`. ⚠️ **P19 changed that map** — before it the list was the ladder
(`greedy`, `cautious`, `simple`, `random`) — which is why a capture is only comparable with one
that made the same choice *in the same build*. ⚠️ **The difficulty question is the third prompt with
`--script bots` and the fourth with `--script human`**, because a match with a person in it asks
that person's name first; `DIFFICULTY_KEY` in the driver holds both, and getting it wrong makes
`--pick` do nothing at all rather than fail.

**What P17 built, in one paragraph.** Six new files in `BurmesePoker.Sim` — `GameValue`,
`Normal`, `Holm`, `StrategySeries`, `Tournament`, `TournamentCsv`, `Suite` (with `SuiteCsv`) —
plus a rewritten `Measurement`, `SeatingPlan.HeadToHead`, two verbs and intervals on the ordinary
report. **`BurmesePoker.Domain` is unchanged, and so are `Simulator`, `GameRunner` and
`Replay`** — the fourth packet in a row to add a whole capability over seams that already
existed (§3.7/§3.8 paying off again).

⚠️ **The two things most likely to be got wrong by a future session.**

1. 🔥 **A win rate is `Σwins / Σseat-rounds`, not the average of the per-game rates.** They differ
   by about a point at four seats whenever a strategy holds a different number of seats in
   different games, and only the first is comparable with P12, P15 and P16. `GameValue` carries a
   **total and its trials** so that a measurement cannot accidentally become a mean of ratios;
   `Measurement.Of` is the ratio estimator and reduces exactly to the per-game mean when the
   denominator is constant. `SuiteTests.AMeasuredWinRateIsTheSameNumberTheHarnessHasAlwaysPublished`
   is the pin, and it deliberately runs a **crossed** seating, because under a rotation the two
   readings agree and the test would be vacuous.
2. 🔥 **`Measurement.Paired` takes per-game values and not two finished `Measurement`s, and joins
   on the game's *seed* rather than its index.** Two cells of one master seed deal game *i* from
   the same shoe and may be paired; two runs of different master seeds share nothing, and pairing
   those by position is silently wrong. The seed join makes that mistake *fail* instead. ⚠️ And
   the across-cell comparison only buys anything if **both cells seat the strategy the same way
   round** — a head-to-head cell enumerates its seatings as an odometer over its two players, so
   comparing a cell it leads with one it follows in moves it round the table and throws the
   shared shoe away. That is why `Tournament.PairingChecks` picks two opponents from the same
   side of the field.

**Regenerating the published numbers** costs about 15 minutes and ~64,000 games:

```
dotnet run -c Release --project BurmesePoker.Sim -- suite --strategies random,simple,greedy,cautious --games 8000 --seed 20260819
```

**Do not hand-edit `docs/strategy/measurements.csv`.** `docs/STRATEGY.md` quotes it, and P23
turns "quotes it" into a test.

*Anything a cold context would need: decisions taken, surprises, deliberate leftovers.*

**From the 2026-08-19 planning session — read these before starting P17:**

- 🔥 **`Measurement` already exists and is used by one verb.** `Sim/Measurement.cs` is a mean over
  **games** with a standard error and a 95% interval, built for P16's cells. The ordinary
  `Report` table in `Sim/Program.cs` uses none of it. **P17 is mostly wiring what is there into
  the path everybody actually runs**, plus the paired form.
- 🔥 **Common random numbers are already available and are being thrown away on purpose.**
  `SeedSequence.GameSeed(master, game)` makes game *i* the same shoe in every cell of a master
  seed, whoever is seated, so a comparison between cells is **paired**. `Measurement.Difference`
  says in its own remark that it adds the variances anyway to be conservative. ⚠️ **The paired
  form must take per-game values, not two finished `Measurement`s** — pairing cells that did not
  share seeds is silently wrong, and only the signature can prevent it.
- ⚠️ **Four strategy names are load-bearing.** `random`, `simple`, `greedy`, `cautious` are half
  of every CSV row's join key (§3.8 item 4) and appear in P14 journal headers. ✅ **P18 did not
  rename them, and `BotCatalogTests` now freezes all four in a literal** — a test that read the
  catalog to check the catalog would agree with any rename at all.
- ✅ **`scripts/drive-console.py` + `cmp` is how P18 proved it was a refactor** — with the two
  amendments above (compare from `Seating:`, and pass `--pick`).
- ✅ **The hint does not follow the difficulty.** `ComputerAdvice` and `TableOptions.StandIn` both
  ask `BotCatalog.Hardest`, and all three sites say why out loud, because it is the obvious thing
  for a later session to "fix".
- ⚠️ **P20 raises a rules question and must not decide it in code.** RULES.md §6.3 makes the
  discards public and P13.5's client already draws a discard pile per seat to every watcher, but
  `TurnContext` shows a seat only the one discard it is offered. Whether a pile is inspectable,
  or only its top card, goes to §9 and to `QUESTIONS-FOR-MYA-LAY.md`. **Safe default: a seat
  counts only what it has actually been shown** — wrong in the direction that makes the bot weak
  rather than the direction that makes it cheat.
- **Measured while planning, for sizing:** a balanced 4-seat run of the four rungs did
  **2,040 rounds in 18.2 s (112 rounds/s)**, 27.9 turns a round, 8 games abandoned at the turn
  cap (the random-heavy tables). So a six-pair tournament at 2,000 games a cell is on the order
  of two minutes. `random` took 48.7% and claimed 46.2% — coin flips, which is a free sanity
  check that the harness is wiring the agents up the way it thinks it is.

⚠️ **Two tests are wall-clock budgets and fail on a busy machine.**
`HandEvaluatorTests.EvaluatingThirteenCardsIsFast` and `PartialCoverTests.ScoringThirteenCardsIsFast`
each assert that three pathological thirteen-card hands are searched in under a second. They
normally take single-digit milliseconds — P12 measured 91 µs and 140 µs a hand — so the budget
has three orders of magnitude of headroom, and they still fail if `dotnet test` is run while
something else is saturating the cores. **Reproduced deliberately on 2026-08-19** by running the
suite against three concurrent `BurmesePoker.Sim` runs: one or the other failed on two attempts
out of three, and six unloaded runs in a row were green. **They are the only two tests in the
suite that can fail for a reason other than a defect**, they predate P14 (P5 and P10 built them),
and a cold context following the `/poker` baseline rule — *any failure at all is a real problem* —
should re-run on a quiet machine before believing either of them. Nothing has been changed about
them; loosening a performance guard is a decision for whoever owns it, not a tidy-up.

**From P13.6 — the state of the finished thing:**

- 🔥 **Run it: `dotnet run --project BurmesePoker.Web`, then open `http://localhost:5188/`.**
  That is the **lobby**. Type a name, press **Sit down**, and you are at `/table/1?you=<name>`.
  **The table does not deal until every person seat is filled** — open a second browser (or a
  second profile) and sit down again to start it, or open a table with `--people 1`. `--people 0`
  is P13.3's room with nobody in it, which deals as soon as anybody watches.
- **The command line names the table opened at boot**: `--table`, `--seats`, `--people`,
  `--seed`, `--pace`, `--between`, `--patience`, `--name` (what the lobby's form suggests calling
  you), `--hints false`. ⚠️ **`--seat` is gone** — a lobby seats you.
- ⚠️ **`Properties/launchSettings.json` fixes the port at 5188 and beats `ASPNETCORE_URLS`.**
  Setting the environment variable and then curling the port you set is a wasted five minutes;
  read the startup line.
- ⚠️ **Do not `pkill -f BurmesePoker.Web` from a bash tool call** — the pattern matches the shell
  running the command and kills the tool call itself (exit 144, twice in this session). Kill by
  port: `ss -lptn 'sport = :5188'`.
- 🔥 **`Lobby` → `HostedTable` → `TableSession`, and that is the whole stack above the engine.**
  A component may reach the lobby and the table it names; `MarkupStandardsTests` still forbids
  every route round the fan-out, `ConnectionFor` included.
- ⚠️ **`SeatBoard` is per viewer now.** A component that wants one takes it as a parameter. The
  only thing that builds one is `HostedTable.SitDown`, and the only thing that disposes one is
  `HostedTable.StandUp` — which is why `TableView.Dispose` calls it.
- ⚠️ **`ClickingPlayer` is the tool for anything multi-seat.** It drives a `SeatBoard`, so
  everything it writes down is something a page would have drawn.

**From P13.4 — still true, and read them before touching the seat:**

- 🔥 **Run it: `dotnet run --project BurmesePoker.Web`.** You are seat 1. The options are plain
  configuration: `--seed`, `--pace`, `--between`, `--seats`, and now **`--seat` (which seat is
  yours, `0` to only watch), `--name`, `--hints false`, `--patience` (seconds a question waits
  for you; 90 by default rather than the server's 45).**
- 🔥 **`SeatBoard` is the only place a hand comes from, and `TableBoard` is the only place the
  public game comes from.** Two connections, and the difference between them is the whole of the
  concealment: the watcher is folded into the board every page draws, your seat is folded into
  the half only you draw. ⚠️ **`MarkupStandardsTests.NoComponentFindsASecondRouteToTheTable`
  now scans for that** — no component may name `TableSession`, `MatchEngine`, `TableState`,
  `TurnContext`, `PartialCover`, `HandEvaluator` or `ConnectionFor`.
- ⚠️ **The public board is still the watcher's, deliberately, even when you are seated.** So the
  round log never says what *you* drew — your own draw arrives as `SeatPrompt.Taken` and
  `TurnPrompt` says *"You took 7♦"* there. A second tab on the same page must not be able to
  read your hand out of the log.
- ⚠️ **`ClickingPlayer` (tests) is the browser's `ScriptedSeat`.** It drives a `SeatBoard` rather
  than a `SeatConnection`, so everything it sees is something a page would draw — which is why
  P13.4's acceptance is asserted through it. It answers inside `Changed`, on the round's own
  thread, so three whole rounds run with no sleeps and no polling.
- 🔥 **Read a control's *accessible name*, not just look at it.** `CardChip`'s hidden words now
  end in a full stop, because a card is always said next to something else — the per-card cost in
  a hand, the rest of a line in the log — and without one the button read *"five of clubs melds
  nothing"*, which means the opposite of itself. **Nothing on screen changed.**
- ⚠️ **`favicon.ico` 404s.** A browser asks for it unprompted; the page names no such URL. It is
  the only 404 in a run and it predates this packet. Not fixed — nobody asked for a favicon.
- **How the UI was verified, and it is worth checking in next time.** Headless Chromium over the
  DevTools protocol driven from `node` with the built-in `WebSocket`: navigate, poll for a
  question, `Input.dispatchKeyEvent` `Tab` until the focused control is the one wanted, `Enter`,
  repeat. It played five rounds. **It was not checked in** — three short scripts in a scratch
  directory — and P13.5 wants two people, which is two of them at once.

**From P13.3:**

- 🔥 **Run it: `dotnet run --project BurmesePoker.Web`.** It uses `Properties/launchSettings.json`,
  which sets `ASPNETCORE_ENVIRONMENT=Development`. ⚠️ **Running it with `--no-launch-profile` and
  no environment gives you Production, where `MapStaticAssets` cannot resolve the build's
  development endpoints manifest and `blazor.web.js` 500s** — the page still renders, because a
  Blazor Server page prerenders, and then never moves again. A *published* output is fine.
  Options are plain configuration: `--seed`, `--pace`, `--between`, `--seats`.
- 🔥 **The page draws from `TableBoard`, which is folded from `TableEvent`s and nothing else.**
  `TableHost` holds one `TableSession`, one watcher connection from `TableSession.Watch()`, and
  the board it folds. ⚠️ **Do not give a component the session.** The banks, the seating and the
  turned-up cards are all on the board *because they arrived as events* — a component that asked
  `TableSession` for its banks would be the second route round the fan-out that P13.3's
  acceptance calls "the thing to refuse", and `ConcealmentTests` would stop standing in front of
  the page.
- ⚠️ **Seating a person is `TableSeat.Person` plus that circuit's own connection.** Today every
  seat is `TableSeat.Computer(..., PacedAgent.Wrap(new GreedyBotAgent(), pace))`. A seated page
  gets its `SeatConnection` from `TableSession.ConnectionFor(player)` — **never the watcher's** —
  and draws its hand from `SeatPrompt.Hand`, which is a `HandView` and already carries the
  computer's suggestion when the table offers hints.
- ⚠️ **`TableHost.Start()` is idempotent because prerendering runs `OnInitialized` twice** (C13)
  and because every browser that opens the page runs it too. Keep it that way when it becomes a
  lobby.
- ✅ **Discharged by P13.4. `MarkupStandardsTests.EveryHandlerIsOnARealControl` passed vacuously**
  — there was not one control on the table. It now counts what it scanned and fails if the
  client ever goes quiet again. **A card you throw is a `<button>`**, and eight handlers are
  scanned.
- ⚠️ **`@key` comes from `LogLine.Sequence`, which is `TableBoard.Narrated` and not
  `Log.Count`** — the log trims at 240 lines and a key from the length would repeat. Cards are
  keyed on `CardView.Card.Id.Value` and seats on `PlayerId.Value`.
- ⚠️ **The visually-hidden spans (`.said`) need a positioned ancestor.** `.chip` and `.who` are
  `position: relative` for exactly that reason; a new one without it stretches the document by
  however far down the page its static position falls.
- **How the UI was verified, so the next packet can do the same.** Headless Chromium over the
  DevTools protocol, driven from `node` with the built-in `WebSocket` — open the page, wait,
  read the DOM twice, `Page.captureScreenshot`, and walk the tab order with
  `Input.dispatchKeyEvent`. It is the browser's equivalent of `scripts/drive-console.py` and it
  is how "the circuit really pushes updates" stopped being an assumption. **It was not checked
  in** — it is four short scripts in a scratch directory — and it is worth writing properly if
  P13.4 wants a regression test rather than a one-off.

**From P13.2:**

- 🔥 **Read this before P13.3. The concealment is done and it is one `if`.** Exactly one event
  the engine narrates is private — the blind draw — because a discard, a taken discard, a
  claimed money card and a declaration all happen in front of the table (RULES.md §4.5, §5,
  §6.3, §7.1). `TableFanOut.PlayerDrew` sends the card to the drawer and null to everyone else,
  and that is the whole filter. ⚠️ **A component must not find a second route to the session.**
  Everything a page renders comes from the `SeatConnection` it was given; a component that
  reached for `TableSession`'s `MatchEngine`, or for a `TableState`, would walk straight round
  the boundary these tests guard.
- 🔥 **The snapshot rule caught two more live lists, and it will catch more.**
  `TableState.TurnedUpOnTable` is aliased by *both* `TurnContext.TurnedUpMoneyCards` and
  `IGameObserver.RoundStarted`, and the opening player's claim removes a card from it (§4.5). A
  prompt or an event that kept the reference would stop saying what it said when it was sent.
  ⚠️ **Assume every list the engine hands over is live until shown otherwise** — `Deck.Cards`,
  `PlayerState.Hand` and `PlayerState.Discards` all say so in their own doc comments.
- 🔥 **How to test a table without threads.** `SeatConnection.Updated` is raised on the round's
  own thread *before* the seat begins waiting, so a handler that answers immediately latches the
  answer and the wait returns at once. `Tests/Server/ScriptedSeat.cs` is built on it, and it is
  why eighteen tests that play real rounds run in about four seconds with no sleeps, no polling
  and no flakiness. **A Blazor component is the other shape** — it answers later, from a circuit
  — and `AnAnswerFromAnotherThreadIsWaitedFor` covers that one deliberately.
- ✅ **`PacedAgent` moved in P13.3 — to `BurmesePoker.Presentation`, not to Domain.** P13.2 left
  the stand-in unpaced on purpose: a sleep belongs to whatever draws the table, not to a server
  that may host many of them, so `TableOptions.StandIn` is a `Func<IPlayerAgent>` a host wraps —
  and `TableHost` now does. **Domain was rejected because `BurmesePoker.Sim` references it**, so
  a sleep there is reachable from P12's hot loop. The console plays byte-identically after the
  move.
- ⚠️ **A client bug must not end a round.** `SeatConnection.Answer` returns **false** for an
  answer that does not fit the question or names a card the seat is not holding, and **the
  prompt stands** so the client may correct itself. `RoundEngine` throws in the same situation,
  which is right in a domain and wrong over a connection. `SeatAnswer.Fits` is where each case
  checks itself, so a new question type cannot skip the check.
- ⚠️ **The takeover is per question, announced once a turn.** Keyed on `(Round, TurnNumber)`,
  the same key and the same reasoning as P11's pacing decorator: a turn asks a varying number of
  questions, so announcing per question would make a silent seat noisier on some turns than
  others for no visible reason. §3.11 C16 wants the client to *say* it — *"Sable is playing your
  seat"* — and `TableEvent.SeatPlayedByTheComputer` is the event to say it from.
- **The wall-clock bound wraps every seat, bots included**, because what goes wrong at a hosted
  table is the people rather than the play — a table nobody is left at would otherwise play
  bot-perfect rounds for ever. `TableOptions.RoundTimeLimit` defaults to two hours; **a test sets
  it to `TimeSpan.Zero` to abandon deterministically**, and to a couple of minutes so a round
  that will not finish fails the suite rather than hanging it.
- **What P13.3 already has, and should not rebuild.** `TableSession.Watch()` is the watcher
  connection — public narration, never a question, nothing it may not see — and
  `TableSession.Leave(connection)` is its disposal. `SeatConnection.Updated` is the
  `InvokeAsync(StateHasChanged)` signal. `CardView.Card.Id.Value` is the `@key` and
  `CardOrder.Display` is a total order (P13.1), so C14 is already closed.
- **Deliberately not built:** any lobby (P13.5), any journal wiring into the server (a
  `JournalingAgent` wraps a seat the same way a `BoundedAgent` does, and nobody has asked),
  and any notion of a *player* separate from a seat. A `TableSeat` is a `PlayerId`, a name and
  optionally an agent, and that has been enough twice now.

**From P13.1:**

- 🔥 **Read this before P13.2. A view model that aliases the engine's list is not a view
  model.** `TurnContext.Hand` returns the seat's **own live list**, and `RoundEngine.TakeTurn`
  discards from it the moment `ChooseDiscard` returns. The first draft of the extracted
  `HandView` kept the reference and so reported fourteen cards while holding thirteen. **The
  console never showed the bug** — it draws the panel and drops the view in the same statement —
  and it only appeared because a test held the view past the decision. ⚠️ **A browser holds
  every view past the decision.** `HandView.Of` copies now; **anything P13.2's fan-out hands a
  seat must copy too.** ✅ *Done — and P13.2 found two more live lists doing it.*
- ⚠️ **The `IAnsiConsole` half of the packet is done and its stated benefit is unreachable.**
  `Program` reaches for `AnsiConsole.Console` exactly once and hands it to everything below, so
  the console *is* drivable by any `IAnsiConsole`. But the test project may not reference
  `BurmesePoker.Console` (§2), so no test can drive it — **and that rule is worth more than the
  benefit. Do not resolve the tension by adding the reference.**
- ✅ **`scripts/drive-console.py` is checked in, and it is how a front-end refactor gets
  proved.** It drives the built console under a pty and writes down every byte. ⚠️ **Keys are fed
  on *quiescence*, never on a clock** — that is the whole trick; a timed driver races the
  renderer and produces a different file every run. Capture at `HEAD` (a `git worktree` of the
  old tree builds in seconds), change, capture again, `cmp`. P13.1 did five runs across three
  seeds including `--no-hints`; all identical.
- ⚠️ **Two copies of the same card cost the same to throw, and that is the right answer.** A
  spare copy is a standing replacement for the one the cover used, so throwing either leaves the
  same thirteen melding. The instance identity that matters (§3.1) is that they are **two
  entries keyed on `CardId`** and that exactly one of them is `Melded` — not that they are
  priced differently. **A browser must not imply the melded copy is dearer than its twin.**
- **Costs are computed eagerly now, where the console memoised them lazily.** A view model is
  data a component iterates twice and a test asserts over, not a lazy graph that searches a hand
  when a renderer touches it. One `PartialCover.Best` per card, a few milliseconds a hand, once
  a turn per seat.
- ⚠️ **`DisplayTokens.States` is the enumeration §3.11 A2 asserts over**, and it is public
  because a legend wants it. A `CardDisplayState` added without a token fails
  `DisplayTokensTests`, which is the point — the standard keeps itself.
- **Where the boundary actually fell.** Presentation owns *what to show*: near-melds, per-card
  cost, display state, display order, the computer's suggestion. The console kept *how*: Spectre
  markup, colours, panels, label padding. `Palette` stayed in the console and is correct there —
  but its **glyphs** are now `DisplayTokens`', so a browser marks an owned money card with the
  same star.
- ⚠️ **`MoneyCardRegistry.Multiplier` doubles on the *overlap* of the two designations**, not on
  a repeat. Turning the Q♣ up twice still pays once; turning up the 7♦ — already permanent —
  pays twice (RULES.md §4.1, §4.3). A test was written on the wrong assumption first.

**From the 2026-08-19 planning session (P13 re-plan — docs only):**

- 🔥 **The question that drove it: "rich browser UI or multiplayer first?" The answer is that
  it is one question.** Only *where the engine runs* couples them, and concealment settles it:
  a hand is fully concealed with money on it (`RULES.md` §7.1), so an engine in the browser
  holds every player's hand in every player's client. **Not fixable by discipline and not
  retrofittable** — moving the engine later rewrites the client's whole relationship to the
  game. `BUILD-PLAN.md` §3.10.
- ⚠️ **What a cold context must not undo: no WASM engine, ever.** `InteractiveServer` keeps the
  C# server-side. WASM stays available for components holding **no concealed state**, and the
  burden of proof is on any component that wants to move there.
- ⚠️ **P13.1 was replaced rather than renumbered**, and the reasoning matters more than the
  packet: the old one assumed the second front end would also be Spectre. It will not be. The
  cut line is **decision versus drawing**, which is why `BurmesePoker.Presentation` exists.
- ⚠️ **A fifth project is a real cost and the alternatives are written down** (§2) so it can be
  argued with: Blazor referencing the Spectre console (rejected — `LayeringTests` exists to stop
  exactly that), or the browser re-deriving the hand view (rejected on drift — the same argument
  that produced `MeldIndex` and `CoverScore`).
- ✅ **Blazor Server means P13.2 has no protocol to design.** No wire format, no client-side
  state sync, no serialising a `TurnContext`. The "server" is a seat and a fan-out, **testable
  in-process with no sockets** — which is what gives P13.2 a mechanical acceptance criterion,
  the first a multiplayer packet in this plan has ever had.
- ⚠️ **§3.11's seventeen UX items are not advisory.** Five are tests and land before there is
  anything to retrofit; the rest are reviewed by playing. **The irreversible one is C12** —
  static SSR by default, `@rendermode InteractiveServer` per component, never at the root.
- **The vibe check on Blazor, since it was asked:** first-class inside .NET since the .NET 8
  render-mode unification, .NET 10 is LTS to 2028, and this repo is already on net10.0. It is
  invisible *outside* .NET, its ecosystem is component-suite-shaped rather than npm-shaped, and
  anything genuinely animated is JS interop. A concealed-hand card game is forms-and-state
  shaped, which is the centre of what Blazor is good at.
- **Nothing was built.** Still **294 passed / 0 failed**; `RULES.md` untouched at rev 13.

**From P16:**

- 🔥 **The result, in one line: a weaker player anywhere at your table is worth 4–5 points to
  you, and which side of you they sit on is worth nothing** — unless they are `random`, where
  the edge is `+9.1 ± 2.1` points. Between `simple` and `greedy`, a 12.9-point skill gap, the
  directional effect is `−1.0 ± 2.1`: an interval that contains zero. `BUILD-PLAN.md`
  "What P16 found" has every cell.
- ⚠️ **The downstream control arm is what makes the number mean anything, and it moved the
  answer by a factor of two.** The gross upstream effect of `random` over `greedy` is +19.4
  points; the *same* swap made downstream is worth +10.3. Without the second arm the packet
  would have reported 19.4 and been wrong. **Any future neighbour claim needs the same arm.**
- ⚠️ **P15's "all 5.4 points of that gap is who fed whom" was wrong, and is corrected in
  place below.** It is a *neighbour* effect, not an *upstream* one — a rotation moves both
  neighbours at once. Nothing about P15's own measurements changes; only the reading of the
  accident it found.
- ⚠️ **`SimulationOptions.Assignments` is the seating fix and it is opt-in.** Null keeps the old
  rotation, so nothing already measured moved. `SeatingPlan.Balanced(strategies, seats)` is
  every assignment (capped at 4,096 — six seats of the four-rung ladder is exactly that);
  `SeatingPlan.Rotations(pattern)` walks one pattern round the cycle, which is what a cell uses
  to average the opening seat away. **Both are validated**: an assignment naming a strategy the
  run did not list would play and then be missing from every total.
- ⚠️ **The CSV grew two columns and one existing test had to be taught about them.**
  `JournalReplayTests.Outcomes` blanks the strategy field so a renamed replay compares equal —
  it now blanks fields 5, 6 **and** 7, because `upstream_strategy` and `downstream_strategy`
  carry the same names. Both derive from the *seating*, never from `SimulationOptions`, which is
  what P14 warned about and is why `AJournalledRunReplaysToTheSameRows` still passes untouched.
- ⚠️ **The mechanism variable barely moved, and that is the open question this packet leaves.**
  `takes` was supposed to be the road the effect travels; across a contrast worth 9.1 points of
  win rate it moved **1.6 ± 0.9**. So upstream skill changes *what* is offered, not *how often*
  something worth taking is. **The tool for that is a rich journal** (§3.9/P14) — re-run the two
  `random`-upstream cells at `--fidelity rich` and read the hands. It is a question, not a
  packet, and nobody has asked for it.
- **The intervention came back null, and `cautious` did not pay for it either.** Focal win rate
  `−0.3 ± 1.4` and take rate `−0.3 ± 0.9` against a greedy upstream; cautious's own win rate in
  that seat was 31.8% against greedy's 31.3%. **A strategy built to deny cannot be shown to deny
  anything** — P15's "denial and self-interest coincide" in its strongest form.
- **What a run costs.** Eight cells × 4,000 games is **32,000 games in 8–12 minutes** on this
  machine (parallel, Release); the two seeds together took about twenty. A quick look is
  `--games 500` — about a minute, and it resolves nothing finer than ~10 points.
- ⚠️ **Two new test classes, and only one of them joins `WallClockBudgets.Collection`.**
  `NeighbourExperimentTests` plays simulations and joins it; `SeatingPlanTests` plays no games
  at all, because every claim it makes is a property of an assignment list — which is the level
  the confound actually lives at.

**From P15:**

- 🔥 **Read this before starting P16.** ✅ *P16 is done — see above. Kept for the reasoning, and
  ⚠️ **its last sentence is the part P16 corrected**.* `greedy` and `cautious` are **0.5 ± 0.6 points apart
  head to head over 32,000 games** and **5.4 points apart in the four-way ladder run**. The
  rotation feeds greedy from `simple` and cautious from `greedy` in every game, so the gap is
  the feeding and not the strategies. It is **a lead, not a result** — the downstream neighbour
  differs too, which is exactly what P16's control separates — but it sizes the effect at
  **points, not tenths**, which makes 2,000 games a cell comfortable.
- ⚠️ **The ladder has four rungs and three skill levels.** For P16's skill dial use
  `random` / `simple` / `greedy` (separated by 36.9 and 12.9 points). `cautious` is the
  **intervention arm**, not a fourth level.
- ⚠️ **P16's intervention is weaker than P16 assumed, and the prediction should be stated at
  the measured size.** P15 found that denial and self-interest coincide: the cards a hand least
  wants are already the cards an opponent can least use, so `greedy` is *already* nearly a
  maximal denier. Swapping a greedy upstream for a cautious one should move the focal seat's
  take rate a little and its win rate barely at all. **If it comes back large, something other
  than denial is doing the work.**
- ⚠️ **Do not try to beat greedy with another tie-break.** Partnership is symmetric, so the
  total partnership of the twelve cards kept is the hand's total less twice the thrown card's:
  "throw the fewest partners" and "keep the best-connected twelve" are the *same rule*, and
  every pairwise-additive measure collapses back into `GreedyBotAgent`. Both denial measures
  tried reduce to `Supply(rank) − Potential` to within a point or two. **A genuine rung above
  greedy has to be combinatorial** — counting live outs (unseen values that would raise the
  cover count of the thirteen kept) is the obvious candidate and costs a `PartialCover.Best`
  per value per candidate, roughly **100× a decision**. That is a packet of its own, and it is
  **not** in the plan; raise it with Nick before building it.
- **The rungs are one function apart, in code.** `CoverScore.Discard(hand, tieBreak)` is the
  loop all three thinking rungs throw through — simple passes `NoPreference`, greedy passes
  `Potential`, cautious packs `Potential` and the threat into one `long`. ⚠️ **Keep it that
  way**: P12's whole result rests on the rungs differing in exactly one decision, and the
  refactor was verified by the 259-test baseline staying byte-identical before anything new
  was added.
- ⚠️ **A random rung must never draw from `Random.Shared`** (§3.7 item 1).
  `SeedSequence.SeatSeed(gameSeed, seat)` is where a seat's generator comes from, and
  `Strategy.Create` takes that seed as its argument so there is nowhere else to get one.
- ⚠️ **A table of nothing but `random` essentially never declares.** Every game of it hits the
  turn cap and is reported abandoned — which is correct, and which is why the tests that use it
  read the **journal** rather than the CSV: an abandoned round produces no row.
- ⚠️ **`WallClockBudgets.cs` is new, and it is load-bearing.**
  `HandEvaluatorTests.EvaluatingThirteenCardsIsFast` and
  `PartialCoverTests.ScoringThirteenCardsIsFast` started failing on a *quiet* machine once the
  ladder tests ran simulations beside them. The four heavy Sim/ladder classes and the two
  budget classes now share one xunit collection, so they never run concurrently; four
  consecutive full runs are green. **Neither budget was loosened** — a performance guard is the
  owner's call, not a tidy-up. **A future test class that plays whole simulations should join
  that collection.**

**From P14:**

- ⚠️ **The one thing not to undo: `BurmesePoker.Console` now draws two `Random`s from
  `--seed`.** `setup` seats the table, and the match's generator is `new Random(setup.Next())`.
  A journal reproduces the deal by re-seeding the *match's* generator, so anything that draws
  from it before round 1 makes a replay deal a different game — which is exactly what the old
  single-generator arrangement did. **A consequence, stated plainly: a `--seed` from a build
  before P14 no longer plays the same console match.** Verified afterwards that two runs at
  `--seed 4242` are still byte-identical to each other, so P11's determinism survived.
- **The strongest acceptance the tree has: a replayed run's CSV is byte-identical to the played
  one's.** `Replay.Run` reuses `Simulator.Summarise` and `GameRunner`'s row-building, so it is a
  `diff` rather than a judgement. `dotnet run -c Release --project BurmesePoker.Sim -- --games 20
  --rounds 2 --journal run.jsonl --csv run.csv` then `-- replay run.jsonl --csv replay.csv`
  produces identical files.
- ⚠️ **Rich fidelity turned out to cost nothing measurable, which §3.9 did not expect.** Serial
  (the only quiet regime on this machine), three interleaved repetitions of 400 games:
  **46–49 rounds/s with no journal, 48–49 thin, 48–50 rich.** The arithmetic is why — a
  thirteen-`CardId` copy is tens of nanoseconds against a `PartialCover.Best` that P12 clocked at
  140 µs. **The expensive axis is bytes, not time**: rich is 9.6 KB a round against thin's
  5.0 KB. It stays off by default for what it costs to keep. ⚠️ **Parallel throughput could not
  be measured here** — the baseline itself swung 68–98 rounds/s run to run — so the serial figures
  are the honest ones.
- **`GameRunner` was refactored, not duplicated.** `Play` and `Replay` share one private `Run`,
  so a replayed game and a played one build their rows through the same code. The row builder now
  takes the players list rather than assuming seats are `PlayerId(0..n-1)` — **which is what lets
  a console match, whose seats are `PlayerId(1..n)`, replay under the harness at all.**
- **The journal is data, and its equality is its text.** `GameJournal`, `JournalHeader` and
  `DecisionSnapshot` are plain records whose list members compare by reference, so the round-trip
  tests compare *lines*. That is the right notion here — a journal is worth having because it
  survives being written down — but a later packet that wants to deduplicate or group decisions
  should add structural equality rather than assume it.
- **Answers are `CardId`s, never values** (§3.1), and `JournalPlayerAgent` fails loudly three
  ways: the journal running out, the next entry not being the question asked, and a discard the
  seat is not holding. All three are tested, and all three print a clean message at the CLI
  rather than a stack trace.
- **A console match replays under the harness.** Verified through a pty: four bot seats at
  `--seed 4242 --pace 0 --journal match.jsonl`, then `Sim -- replay match.jsonl` reporting the
  same two rounds the console printed (Sable out both times, +$16 in 29 turns then +$13 in 29).
- **Deliberately not built:** anything that indexes, queries or aggregates journals. The CSV is
  still what analysis reads; a journal is where you go when the CSV raises a question it cannot
  answer.

**Planning session, 2026-08-19 (docs only — no code, still 239 passed / 0 failed):**

- **Asked what the persistence layer is. The answer is that there isn't one**, and that turned
  out to be a defensible position rather than an oversight: `CsvReport.WriteTo` is the only
  write to disk in the tree, and a bot game is a pure function of its seed, so a log of one is
  redundant the moment it is written. **`BUILD-PLAN.md` §3.9** records why that stops being true
  — a person is not a function of a seed, a seed only replays against the code that produced it
  (P12 already edited `GreedyBotAgent`), and §3.8's one open ⚠️ row wants the decisions
  themselves. **P14** is the packet: journal types and two `IPlayerAgent` decorators in Domain,
  the format in one place, `File.WriteAllLines` in the consumers, and replay as *a seat that
  answers from a file* rather than a resumable engine.
- **A hypothesis worth testing arrived from outside** (Nick's friend): *the main thing that
  decides your game is the skill of the player before you.* It is well-posed — `RULES.md` §5
  makes a table a directed cycle, since only the immediately-previous player's discard is
  available — and it is **a strategy question, not a rules question, so it does not go in
  `RULES.md`.** **P15** builds a ladder of strategies to have a skill dial at all; **P16** runs
  the experiment.
- ⚠️ **The finding that matters most, and a cold context must not miss it:
  `SimulationOptions.Seating` cannot answer P16's question.** It rotates *one fixed pattern*,
  `Strategies[(seat + game) % Strategies.Count]`, so at two strategies and four seats it only
  ever produces `[A,B,A,B]` and `[B,A,B,A]` — **every A is fed by a B, always**. The pair
  *(my strategy, upstream strategy)* is perfectly confounded, not merely underpowered. P16 has
  to enumerate assignments rather than rotate one.
- ⚠️ **Which puts a caveat on P12's headline.** 30.7% against 19.3% was measured with **every
  greedy seat sitting downstream of a simple seat**. Nothing is wrong and nothing needs
  re-deriving — it is the honest answer to *"what happens at that table"* — but it is not a
  clean strategy-vs-strategy figure, and P16 owns separating the two.
- **Two design notes worth carrying into P16.** The mechanism variable is **already collected**:
  `takes` in the CSV is "how useful was what my upstream threw", so if the hypothesis is true
  that is the path the effect travels. And the packet's sharpest piece is the **control** — run
  the same design varying the *downstream* neighbour, which should move the focal win rate far
  less; without it the result is only "strong tables win more".
- **`docs/PLAYING.md` added** — a player-facing guide to solo play in the console. Listed in
  CLAUDE.md's documentation map.

**From P11:**

- **Five new files in `BurmesePoker.Console`, and every one of them is presentation.** `Options`
  (the command line), `Palette` (the colour and marker language), `RoundLog` (what survives the
  clear), `HandView` (a hand as its melds, and what each card costs to throw) and `PacedAgent`
  (a decorator that makes a bot wait). **Domain and Sim are byte-for-byte unchanged** — the
  packet needed nothing from either, which is the strongest evidence yet that §3.5's seams are
  the right ones.
- ⚠️ **The pause is a decorator in the console and must stay there.** `PacedAgent.Wrap(inner,
  pause)` sleeps once per `(Round, TurnNumber)` — the same pair `SpectrePlayerAgent` hands the
  keyboard over on, because a turn asks a varying number of questions and pausing per *call*
  would make some turns three times as long for no visible reason. **Putting the sleep in
  `GreedyBotAgent` would have put it inside P12's hot loop**; `LayeringTests` now exists partly
  to make that class of mistake fail loudly. `Wrap` returns the agent itself when the pause is
  zero, so `--pace 0` costs nothing.
- **The round log remembers markup, not events.** `ConsoleObserver.Say(markup)` prints a line
  *and* files it, so there is exactly one copy of every sentence the game says. Re-rendering
  from remembered events would have put a second copy of the wording next to the first.
  `SpectrePlayerAgent.BeginTurn` draws the log panel **first**, above the table and the hand,
  because it is the context the other two panels are read against. **The log is per round and
  nothing survives it** — the between-round history is `Program`'s own `List<RoundResult>`,
  since `MatchEngine` still deliberately keeps none (§3.8).
- **The hint is the computer's own answer, asked directly.** `SpectrePlayerAgent` holds a
  `GreedyBotAgent` and calls `ChooseAction` / `ClaimTurnedUpMoneyCard` / `ChooseDiscard` on the
  very `TurnContext` it was handed. ⚠️ **Do not re-derive a recommendation** — `CoverScore` is
  `internal` and the tie-break is not trivial, so a second implementation would be a different
  strategy wearing the first one's name. The per-card *cost* beside it is `HandView`'s own
  reading of `PartialCover` (`covered(13) − covered(12)`) and agrees with the advice by
  construction. `--no-hints` turns the lot off.
- ⚠️ **Spectre markup must balance, and the compiler will not tell you.** `Palette.Legend` was
  written with two opening `[grey]` tags and one `[/]`, built clean, passed 239 tests, and threw
  `InvalidOperationException: Unbalanced markup stack` on the first hand drawn. **Only playing
  it finds this.** Every constant that interpolates a colour needs looking at twice.
- ⚠️ **`Cover(...)` no longer takes a method group.** `CardFormatting.Meld` gained optional
  `MoneyCardRegistry` / ownership arguments (a declaration is public and shows plain cards; your
  own hand wants the markers), which broke `.Select(Meld)` — it now reads `.Select(meld =>
  Meld(meld))`. Harmless, but the error message blames a Spectre extension method and is
  thoroughly confusing.
- **Every match is seeded, whether or not one was asked for.** `Options` draws a seed when
  `--seed` is absent and `Program` prints it, so any match can be replayed after the fact. ⚠️
  **The seating comes from the match's own `Random`, not `Random.Shared`** — otherwise `--seed`
  would replay the same deals to a different table. **Verified:** two runs at `--seed 99` with
  identical keystrokes produce byte-identical output; `--seed 100` does not.
- **The difficulty prompt is a strategy name and nothing more.** *Hard* is `GreedyBotAgent`,
  *easy* is P12's `SimpleBotAgent`, and the prompt quotes the measured win rates (a third
  against a fifth). It is only asked when there is at least one bot.
- **What was actually played, since a UX packet is verified by playing it.** Through a pty
  (`script -qec "dotnet run --project BurmesePoker.Console --no-build -- --seed N --pace 0"`),
  fed newlines: **13+ rounds at four seats** on seed 4242 — settlements summing to zero,
  history truncating to "last 12 of 15", the log panel carrying three bot turns across the
  clear, hints reading *"(breaks a meld — costs 3)"* and *"← the computer would throw this"*;
  **seed 1 for the opening turn**, which is the only turn that offers the money-card claim, and
  the only way to see that advice line; **two six-seat runs**, where the reshuffle narration
  fires (it does not at four — P12's finding, confirmed from the other side). `--pace` measured
  by counting turns in a fixed window: ~163× fewer at 1,000 ms than at 0.
- **P11 shipped 4 tests (239 total), and they are the only mechanical check the packet allows.**
  `LayeringTests` asserts that Domain references no Spectre assembly, that Domain references no
  `System.Console` either, that Sim references no Spectre, and — guarding the other three —
  that the reference lists are not empty. **Still no Console tests, by construction.**
- **No new rules question. `RULES.md` is unchanged at rev 13.** Nothing in a UX pass is a rules
  decision; the two places one could have crept in — what the hint recommends, and what the
  claim prompt says a claimed card pays — were both already settled (§4.4, §4.5) and are quoted
  rather than re-decided.

**Still current, from P12:**

- **`BurmesePoker.Sim` is a fourth project and `BurmesePoker.Tests` now references it.** The
  rule to carry forward is **"tests never reference `BurmesePoker.Console`"**, which is what
  the Domain-only rule was always protecting. A fifth project existing only to test the fourth
  would have bought nothing, and the harness's determinism is an acceptance criterion.
- **The command line:** `dotnet run -c Release --project BurmesePoker.Sim -- --games 2000
  --strategies greedy,simple --seats 4 --csv out.csv`, plus `--rounds`, `--seed`, `--turn-cap`,
  `--serial`, `--threads`. There is also a **`bench`** verb that times the two searches over
  random hands. **Run it in Release** — Debug is roughly three times slower and says nothing
  useful about throughput.
- **The headline result: the tie-break is worth 1.6× the wins.** `SimpleBotAgent` is
  `GreedyBotAgent` with the discard tie-break removed and *nothing else* changed. Over **2,000
  four-seat rounds**: greedy **30.7%** of rounds and **+$1.24** a round, simple **19.3%** and
  **−$1.24**. P10's claim that the tie-break is what makes progress is now measured.
- ⚠️ **Seats are not equivalent, and a comparison has to rotate them.** Seat 0 opens every
  round and is the only seat ever offered the turned-up money card (RULES.md §4.5), because
  `RoundEngine.Play` starts turn 1 at `players[0]` every round. `SimulationOptions.Seating`
  puts strategy *(seat + game) mod k* in each seat. **A future harness that seats strategies
  any other way must still rotate them**, or it will be measuring the seat.
- **Determinism is per game, never per run.** `SeedSequence.GameSeed(master, index)` is
  SplitMix64's finaliser over the two packed into one word, so game 417 is the same game
  however the run was scheduled. Serial, parallel and two-thread runs give **byte-identical
  CSV**, which is the acceptance test the packet stands on. Never draw per-game seeds from one
  shared `Random`.
- **The turn cap lives in the agent, and has never fired in a real run.** `SeatRecorder` throws
  `RoundAbandonedException` past `SimulationOptions.TurnCap` (400 by default) and the game
  stops there and is *reported*, not dropped. Even a table of four `SimpleBotAgent`s — which
  has no tie-break to push it off a plateau — finished all 300 rounds tried, averaging 28.3
  turns. The only thing that has ever tripped the cap is `StallingAgent` in the test.
- ⚠️ **The reshuffle is a six-player phenomenon.** Over 300 bot rounds each: **0 reshuffles at
  four seats, 3 at five, 67 at six.** P10 recorded that no bot round had ever exhausted the
  draw pile; that was a four-seat observation. P9's rule is exercised by real play at a full
  table.
- **The money split, measured** — and it confirms `RULES.md` §4.3's `DERIVED` balance argument
  rather than upsetting it. At five seats the money cards moved **$8.43 a round** against the
  flat prize of $20 — **42%**, where the derivation guessed 40%. `RULES.md` is at **rev 13**
  for that one note; **no rule changed and no new question was raised.**
- **What P12 did *not* need from the domain: anything.** Win rate, the flat/side-bet split,
  turns, how close the losers were, take rate, claim rate and deck exhaustions are all derived
  by the harness from the three §3.8 seams. The only Domain additions are `Agents/CoverScore`
  (an extraction, shared so the two bots' scoring cannot drift) and `Agents/SimpleBotAgent`.
- **Speed, for the record:** `PartialCover.Best` **140 µs** a hand, `HandEvaluator.TryFindCover`
  **91 µs**, a round **~20 ms**, a run **51 rounds/s serial and 85–92 parallel** on a 4-core
  i7-1165G7. ⚠️ **The work is allocation-bound, not compute-bound** — the searches allocate an
  index, a memo and a list per call, and eight threads bought only 25% more throughput until
  `<ServerGarbageCollection>` went into the Sim csproj, which took it to 70%. If throughput
  ever matters, **attack allocation**, and still never inside the evaluator (§3.4).
- P12 shipped 10 tests (235 total). The suite went from 1.7 s to 4.3 s, almost all of it the
  16-game comparison the Sim tests share through a `Lazy<SimulationReport>`.

**Still current, from P10:**

- **`PartialCover.Best(hand)` → `{ Melds, Uncovered, CoveredCount, IsComplete }`** is the
  scored cover P5 deliberately did not build. It is `HandEvaluator`'s own search **plus one
  branch**: at the lowest card not yet settled it may take a meld covering it *or give the card
  up and move on*. Memoised on `(position, covered)`; a complete cover stops the search where
  it stands, so a winning hand is no dearer than it is in the evaluator. `IsComplete` and
  `HandEvaluator.IsWinning` are pinned as the same claim across 320 randomly dealt hands.
- ⚠️ **`Melds/MeldIndex` is an extraction, not an addition.** Both searches need the same
  candidate index — cards in `CardId` order, one bit each, candidates filed under the lowest
  card they consume — and keeping two copies of that would be two places for it to drift.
  `HandEvaluator` was rewritten over it in a separate step and the **208-test baseline
  re-run before anything else was touched**; it is the only proof its answers did not move.
  Do this again if either search is ever changed.
- **The whole strategy is one question asked three ways:** *of the thirteen I would be left
  holding, how many meld?* Take the discard iff it raises that count, claim the turned-up money
  card iff it raises that count, throw whichever card leaves it highest. **The take decision is
  deliberately asked of the fourteen rather than of the thirteen kept** — same answer, a
  fourteenth of the work, because any improvement must use the new card and every fourteen-card
  arrangement has a meld of four or more to give one back.
- **The tie-break is what actually makes progress**, since early on most discards score alike:
  prefer keeping cards with partners (another suit of the same rank, a neighbour in the same
  suit — with the ace a neighbour of both the two and the king, never through), and keep jokers
  over everything. **A tie on *taking* goes to the deck**, which is the only place money enters
  a decision at all: a blind draw confers ownership and a pickup does not (RULES.md §4.4).
- ⚠️ **Termination is a property, not a hope.** The score can never fall — throwing back the
  card just taken restores the hand exactly — so a bot's hand climbs monotonically to thirteen.
  Measured over twelve seeds and every table size: **every round terminated, 21–30 turns, ~40 ms
  a round, ~1.5 ms a turn**. No bot round has ever run the draw pile out, so **P9's reshuffle
  is unexercised by bots** — the tests that cover it are still `RoundEngineTests`'.
- **Two findings worth keeping for P12.** A freshly dealt hand covers **4 of 13 on average**
  (154 of 800 dealt hands cover nothing at all), and a bot reaches thirteen in seven or eight of
  its *own* turns — mostly by **taking discards**, because with two decks the card somebody
  throws away is very often somebody else's third of a rank. The take-the-discard rate is going
  to be the interesting statistic.
- **`RecordingAgent` (test project) is how a bot is tested at all.** `TurnContext` has an
  `internal` constructor, so a test cannot fabricate one — a strategy is only observable from
  inside a real round. The decorator wraps a bot, plays a scripted `DealBuilder` deal, and
  reads back what it was asked and what it answered. This is exactly BUILD-PLAN §3.8's item 2
  seam, and P12 should lift it rather than reinvent it.
- **The console asks two questions now** — *"how many at the table?"* then *"how many of you
  are people?"* (0 is allowed, and leaves the computer playing itself). Bots are named from a
  roster and marked: *Ruby (bot)*, *Sable (bot)*, … so narration reads as a table of players.
  **No Console tests, still by construction.** Verification was again a pty:
  `script -qec "dotnet run --project BurmesePoker.Console --no-build" /dev/null < keys` with a
  file of newlines — **20 full rounds** against three bots, every settlement summing to zero,
  bots declaring covers including joker substitutions.
- ⚠️ **A bot's turn is instantaneous, and that is a UX problem P11 inherits.** Three bot turns
  now flash past and are wiped by the human's screen clear, so the round-log panel P11 already
  wanted is the sorest thing in the console. **Do not put a sleep in `GreedyBotAgent`** — a
  domain type that waited would ruin P12.
- **No new rules question.** Everything the strategy needed was already settled: §4.4, §4.5,
  §5, §6, §7.1. `RULES.md` is unchanged at rev 12.
- P10 shipped 17 tests (225 total).

**Still current, from P9:**

- **`MatchEngine(players, agents, stakes, random, observer = null)`** holds the seating, the
  stakes, the banks and the one `Random`. **`PlayRound()`** shuffles a fresh shoe;
  **`PlayRound(drawOrder)`** is the scriptable twin, mirroring `RoundEngine`'s own pair. Both
  return a **`RoundRecord(RoundResult Result, TableState Table)`** — the §3.8 pair, which is
  also what the console's settlement report reads. `Banks` is a live view, everyone starts at
  zero, and `RoundsPlayed` counts. **Nothing is retained per round**; do not add a history to
  make a report easier (P11 remembers its own).
- ⚠️ **`RoundEngine`'s constructor now takes a `Random`, and it is required** — inserted after
  `drawOrder`, before `round` and `observer`. A round needs randomness for the reshuffle, and
  defaulting it would have made an exhausted round irreproducible in silence (BUILD-PLAN §3.7).
  Test call sites pass `new Random(seed)`; `RoundEngineTests.Engine(...)` takes an optional
  `seed` that only matters if the round actually reshuffles.
- **The reshuffle is in `RoundEngine.TakeCard`, at the moment of drawing** — `if
  (Table.DrawPile.IsEmpty)` → `TableState.ReplaceDrawPileWithTheDiscards(random)`, which sweeps
  **every** seat's pile, shuffles, and installs a new `Deck`. Safe to sweep all of them because
  the current player has already declined the offered discard by then. **The turned-up cards
  are not gathered** (RULES.md §9 #4's recommendation, taken). `DeckExhaustedException` now
  means what it says: nothing left anywhere, which is a real end state rather than a crash.
- **Ownership across the reshuffle is `CardOwnership.TryRecordFromDeck`** — keeps the first
  owner and returns `false` rather than throwing. The blind draw calls it; **the deal still
  calls the strict `RecordFromDeck`**, where a card leaving the deck twice really is a bug.
  RULES.md §5's "first acquisition wins" is now a method rather than a paragraph.
- ⚠️ **`TurnContext` gained `Round`, and it fixed a real concealment bug.** An agent lives for
  the whole match, so `SpectrePlayerAgent`'s "have I already begun this turn?" check — which
  compared `TurnNumber` alone — matched turn 1 of round 2 against turn 1 of round 1 and
  **skipped the screen clear at the start of every round after the first**, leaving the
  previous hand on screen. It now tracks `(Round, TurnNumber)`. The round number is public
  information, so nothing leaks; the P7 reflection test still passes.
- ⚠️ **A round nobody can win now runs for ever.** This is the biggest behavioural change in
  the packet and it deleted a test: `NobodyDeclaringRunsTheDrawPileOutAndSaysSo` used to assert
  the throw, and now hangs. Every round-level test needs a seat that declares —
  `RoundEngineTests.WaitingToDeclare(turns)` builds one that holds a winning hand and declines
  until its *n*th turn, which is how the long reshuffle rounds are driven (a 4-player round
  exhausts the pile on **turn 55**, having gathered exactly 54 cards).
- **`ReshuffleSeed = 2` is pinned deliberately.** Any seed reshuffles, but seed 2 is one where
  the 7♦ Bo threw away comes back to *somebody else*, which is the case the rule is about. If
  that test ever needs re-seeding, seeds 3, 4, 5, 7, 11… also work.
- **`RecordingObserver` grew** `Draws`, `Reshuffles`, `OwnersAtFirstReshuffle` and
  `DrawsAfterFirstReshuffle`, and `IGameObserver` gained **`DiscardsReshuffled(int cards)`** —
  which is also how P12 counts deck exhaustions (§3.8).
- **The console plays a match now**: `Program` loops `PlayRound()` → settlement report →
  standings → `AnsiConsole.Confirm("Another round?")`, and prints the rounds played on the way
  out. Standings is a small table of the running banks, title `Standings` with the round count
  as a caption (a `Title` long enough to exceed the table's width wraps ugly).
- **No Console tests, still by construction** — the test project references Domain only.
  Verification was again a throwaway harness under the scratchpad: it links
  `BurmesePoker.Console/*.cs` plus the test project's `Hands`, `DealBuilder` and
  `ScriptedPlayerAgent`, sets `<StartupObject>` to its own entry point so `Program.Main` does
  not clash, and **calls `Program`'s private `ReportSettlement` / `ReportStandings` by
  reflection** — same-assembly, so `BindingFlags.NonPublic` reaches them. Driven through a pty
  with `script -qec "dotnet run --no-build" /dev/null < keys`. **Two tricks worth keeping:**
  arrow-key escapes fed to a Spectre `SelectionPrompt` did not register, so rig the deal such
  that the card to discard **sorts first** (hearts sort first, jokers last) and plain Enter
  picks it; and give the winning hand no hearts so the drawn heart is that card. Two full
  rounds were played, banks reaching +$28 / −$4 / −$12 / −$12 — summing to zero.
- **Three rules defaults taken, all recorded in `RULES.md` rev 12 §9 and phrased neutrally in
  `QUESTIONS-FOR-MYA-LAY.md`:** **#4** the turned-up cards are not swept into the reshuffle;
  **#5** the money-card claim is offered every round, approved by nobody; **#14** (new) nothing
  moves between rounds — the seating the match was given is played all session.
- P9 shipped 16 tests and removed 1 (208 total).

**Still current, from P8:**

- **The console is a hotseat, and concealment is the screen clearing.** Every seat is a
  person at the same terminal, so `SpectrePlayerAgent.BeginTurn` clears the screen once per
  turn, names the player, and waits for *"are you at the keyboard?"* before drawing their
  hand. It fires on whichever of the four questions comes first, and tracks the turn by
  `TurnContext.TurnNumber` rather than by counting calls — **a turn asks a varying number of
  questions** (the opener is offered the money card, later turns the discard, only a winning
  hand the declaration).
- **`ConsoleObserver.PlayerDrew` deliberately does not print the card.** The domain narrates
  private information and says so; filtering is the front end's job (BUILD-PLAN §3.5). A
  pickup, a claim, a discard and a declaration are all public and are printed in full.
  A side effect of the per-turn clear: **public narration scrolls away**. The table panel
  reprints what still matters (turned-up cards, draw count, the takeable discard). If P9
  wants a running log it needs a panel that survives the clear, not more `WriteLine`s.
- **The star marks an owned *money* card, not any owned card.** Everything dealt is owned, so
  starring ownership alone marked all thirteen and said nothing. `CardFormatting.Of(card,
  registry, owned)` only stars when the multiplier is non-zero — a money card with no star
  came from a discard pile or off the table and pays somebody else.
- **The settlement breakdown lives in `Program`, not in the observer**, because settlement
  returns net deltas only and splitting them needs `TableState.Ownership` and
  `TableState.Shoe`, which are on the table rather than in the `RoundResult`. The round half
  is derived from the winner (flat, RULES.md §7.2) and the money-card half is the remainder,
  so **the two columns always add up to what the domain actually settled**. ⚠️ **This is the
  one thing P9 must re-plan around** — see BUILD-PLAN P9, amended.
- **Prompts are offered unconditionally and that is correct.** The engine only asks a question
  that has a legal answer, so neither the claim, the pickup nor the declaration needs a
  legality check on the console side; adding one would duplicate the rules outside the domain.
- **No tests were added — by construction.** The test project references Domain only, so
  nothing in `BurmesePoker.Console` is unit-testable, and P8 changed no Domain code: still
  **192 passed / 0 failed**. Verification was manual, and worth repeating the recipe:
  `script -qec "dotnet run --project BurmesePoker.Console" /dev/null < keys` gives Spectre a
  pty (piped stdin fails — `System.Console.ReadKey` throws when input is redirected, and
  `Program` refuses to start with a clear message rather than a stack trace). A second,
  throwaway harness under the scratchpad linked the test project's `DealBuilder`,
  `ScriptedPlayerAgent` and `Hands` sources, rigged a winning deal, and drove **a real
  `SpectrePlayerAgent` to a declaration** — that is how the declare prompt and the settlement
  table were seen working, arithmetic checked by hand (+16 / 0 / −8 / −8, summing to zero).
- **Spectre.Console is back at 0.57.2**, the current release, not the 2023 pin of 0.47.0.
  Only `BurmesePoker.Console` references it.
- **No new rules question.** Everything P8 needed was settled: §3 step 2 (seating is
  randomised in `Program`), §4.4/§4.5 (a claimed card is held but owned by nobody), §6.3
  (concealment), §7.2 (a flat round payment).

**Still current, from P7:**

- **`new RoundEngine(players, agents, stakes, drawOrder, round, observer)` deals in the
  constructor and `Play()` runs the turns**, so `engine.Table` is readable before the first
  turn and a setup test needs no play at all. `Play()` returns a `RoundResult` and refuses a
  second call. **`RoundEngine.Shuffled(..., Random)` is the real-game entry point.**
- **⚠️ A round is dealt from a `drawOrder`, not from a shuffle** — the 108 cards in the order
  they will leave the deck, validated as a permutation of the shoe. That is what makes a round
  scriptable: `BurmesePoker.Tests/Play/DealBuilder.cs` arranges the order so seat *s* gets
  positions *s*, *s+n*, *s+2n*…, then the turned-up top card, then the draw pile, with the
  turned-up bottom card last. **Its filler is never a money card**, so a settlement expectation
  can be worked out by hand; ask for a money card explicitly with `ThenDraw("7D")`.
- **The engine builds the shoe itself.** `Settlement.ForRound` needs the *unshuffled*
  index-aligned shoe, so `RoundEngine` calls `DeckBuilder.BuildTwoDecks()` at setup, keeps it
  as `TableState.Shoe`, and validates `drawOrder` against it. P6's warning that the caller must
  keep the builder list is now handled inside the engine — **callers pass nothing extra**.
- **`TurnContext` is the concealment rule expressed as a type**: own hand, available discard,
  draw-pile count, turned-up cards, stakes, registry, `TurnNumber`, `Taken`, `CanDeclare`,
  `YouOwn(card)`. **No `TableState`, no `PlayerState`, no `CardOwnership`** — exposing
  ownership would leak which money cards an opponent was dealt. A reflection test pins it.
- **The engine asks narrowly, so the front end needs no legality checks.** `ChooseAction` is
  asked only when a discard is available (so the opening turn just draws), `ClaimTurnedUpMoneyCard`
  only on turn 1, and `Declare` only when `HandEvaluator.TryFindCover` has already succeeded —
  the cover it found is what `RoundResult.Melds` carries, so it is never computed twice.
- **⚠️ Narrate *after* the card lands.** The conservation test caught a real ordering fault:
  raising `PlayerDrew` between `DrawFromTop` and `seat.Take` leaves an observer seeing 107
  cards. Every take now joins the hand before the event fires. Not a clone bug — but the same
  test that would have caught the 2023 clone bug caught this.
- **`IGameObserver` gained three events over BUILD-PLAN §3.5's sketch** — `PlayerTookDiscard`,
  `MoneyCardClaimed`, `PlayerDeclared` — because a pickup, a claim and a blind draw are three
  different things and only the draw confers ownership. **All methods are default no-ops.**
- **Ownership accounting is one invariant, and it is tested as one:** records == cards dealt +
  blind draws, ever. A claim records nothing, a pickup records nothing, a discard changes
  nothing. `OnlyTheDealAndABlindDrawEverConferOwnership` exercises all three routes in one
  round and asserts the count.
- ~~Deck exhaustion still propagates~~ — ✅ **done in P9**, inside `RoundEngine.TakeCard`
  exactly as P7 predicted it would have to be.
- **Seating is taken as given.** RULES.md §3 step 2 randomises it, but an engine that
  reshuffled its own seating could not be scripted, so P8's `Program.cs` randomises before
  constructing. The same list is what settlement is handed.
- **Two new rules questions, both defaulted, neither blocking** (`RULES.md` rev 11, and both
  phrased neutrally in `QUESTIONS-FOR-MYA-LAY.md`):
  **§9 #12** — when the opening player claims the turned-up money card, does that value still
  pay? **Default taken: yes**, designation is fixed at setup and does not move with the card;
  reversing it is one line in `TableState`'s constructor.
  **§9 #13** — may a player discard the very card they just took? **Default taken: yes**,
  nothing in §5 forbids it; it would be a single guard in `RoundEngine`.
- P7 shipped 18 tests (192 total).

**Still current, from P6:**

- **`Settlement.ForRound(players, winner, stakes, moneyCards, ownership, shoe)` →
  `IReadOnlyDictionary<PlayerId, int>`** of **net** deltas, positive to collect. Every player
  at the table appears, including zero deltas, and the deltas always sum to zero. It is a
  static class — settlement holds no state.
- **⚠️ The `shoe` parameter must be `DeckBuilder.BuildTwoDecks()` order, and this is checked.**
  Settlement resolves an owned `CardId` to a `Card` by *index*, and rejects any list where
  `shoe[i].Id.Value != i` — so **passing `Deck.Cards` throws**, because it is shuffled. That
  guard is deliberate: without it a shuffled shoe would settle the wrong cards in silence, and
  every arithmetic test would still pass. **P7 must keep the builder list it made the round's
  `Deck` from** and hand that over; `Deck` copies its cards, so the list is never disturbed.
- **Settlement is never given a hand, and a reflection test pins the parameter list** so it
  stays that way. This is RULES.md §4.4 encoded in a signature: the question is never who
  *holds* a card. The two headline tests — a money card its owner discarded, and one an
  opponent is holding — pass without any notion of a hand existing at all.
- **`Stakes` is a `sealed record`, not a struct.** A `readonly record struct` was the obvious
  reflex, but `default(Stakes)` would then be a silent $0/$0 game that still sums to zero and
  passes every property test. A class makes the omission a `NullReferenceException` instead.
  Both values must be **positive**; `Stakes.Standard` is $5 / $1.
- **The 7♦ pays *once* unless it is also turned up.** Permanent designation and turned-up
  designation are separate summands (P2), so an owned 7♦ in a round where the turned-up cards
  are something else pays `1 × MoneyCardValue`, not 2. A draft test asserted +8 for exactly
  that case and was wrong; the arithmetic is unchanged, the expectation was.
- **Guards, all `ArgumentException`:** winner not at the table, a player seated twice, an empty
  table, an ownership record naming somebody not at the table, an owned id outside the shoe,
  and the misaligned-shoe check above. **There is no no-winner settlement** — P7 rounds end on
  a declaration, and P9 reshuffles rather than ending one early.
- **Zero-sum is property-tested** over 500 randomised rounds (2–6 players, random stakes,
  random turned-up cards, up to 80 owned cards). Sample the deal with `Random.Shuffle` over a
  copy of the shoe — cards must be **distinct**, or `RecordFromDeck` rightly throws on the
  second owner of one physical card. This makes P9's match-level conservation test a test of
  *banking*, not of settlement; BUILD-PLAN P9 has been amended to say so.
- **P8 gets net deltas only** — no breakdown of round payment against side-bet, no per-card
  detail. If the console wants "−$5 for the round, +$3 in money cards" it computes the
  side-bet half itself from `ownership.Records` and `Multiplier`. BUILD-PLAN P8 amended.
- **No new rules question.** §4.3, §4.4 and §7.2 are all `PLAYER`/`EXPERT` Settled and the
  worked example is reproduced exactly; nothing needed a judgement call. `RULES.md` is
  untouched at rev 10.
- P6 shipped 26 tests (174 total).

**From P2:**

- **`MoneyCardRegistry(turnedUp).Multiplier(card)` → 0 / 1 / 2** is the whole designation API,
  and it is a pure function of the turned-up cards — no `Card` is written to, ever. The
  implementation is literally `(permanent ? 1 : 0) + (turnedUp ? 1 : 0)`, so **doubling is the
  overlap and the ceiling falls out with no clamp**: two copies of the 5♥ turned up still pay
  1, two copies of the 7♦ still pay 2.
- **The permanent designators (7♦, A♠) are two `Card`s carrying negative `CardId`s**, compared
  by `SameValueAs` like every other designator. They are values, never dealt; a negative id
  means a stray `==` against a real card can only be false.
- **The turned-up list is copied**, and any length is accepted — including empty, which the
  "permanent cards with nothing turned up" acceptance test needs. **No arity check.** If P7
  wants to insist there are exactly two, that is P7's rule to enforce at setup.
- **No joker branch was written**, per the P1 default: a turned-up red joker designates the two
  red jokers and neither black one, because `SameValueAs` discriminates jokers by colour.
  `ATurnedUpRedJokerDesignatesTheRedJokersAndNotTheBlackOnes` is the single test to change if
  `RULES.md` §9 #11 ever settles the other way.
- **`PlayerId` was brought forward from `Play/`** — `CardOwnership` needs it and P7 is a long
  way off. It is a `readonly record struct PlayerId(int Value)`. **P7 must not redefine it**;
  BUILD-PLAN P7's build list has been amended.
- **`RecordFromDeck` re-recording the *same* owner is a no-op; a *different* owner throws
  `InvalidOperationException`.** The packet allowed either "rejected or a no-op" — this splits
  it, because a genuine repeat is harmless while two owners for one physical card can only be
  a dealing bug worth surfacing. There is deliberately no transfer, clear or removal, and a
  reflection test asserts the public surface stays that way.
- **⚠️ P6 needs a card lookup, and this is the one thing that re-planned.** `Records` is keyed
  by **`CardId`** but `Multiplier` takes a **`Card`**, because designation is by value and
  ownership is by instance — the two identity notions meeting at exactly the seam BUILD-PLAN
  §3.1 predicted. So `Settlement.ForRound` must be given the shoe as well.
  **`DeckBuilder.BuildTwoDecks()` is index-aligned** (`CardId.Value` == list index), so the
  lookup is an array index — but **`Deck.Cards` is shuffled and is not**. Do *not* widen
  `RecordFromDeck` to take a whole `Card`; BUILD-PLAN §3.3 fixes its signature and ownership
  is about the physical card. BUILD-PLAN P6 has been amended.
- **`CardOwnership.Records` is a live read-only view** over the internal dictionary, in the
  same spirit as `Deck.Cards`. Snapshot it if you need one.
- **No new rules question.** The packet needed no judgement beyond the §9 #11 default P1 had
  already recorded and `QUESTIONS-FOR-MYA-LAY.md` already asks.
- P2 shipped 29 tests (148 total).

**Still current, from P5:**

- **`MeldCandidates.For(hand)` → `IReadOnlyList<Meld>`**: runs first, then the sets whose card
  set no run already covers. `HandEvaluator.IsWinning(hand)` and
  `HandEvaluator.TryFindCover(hand, out var cover)` are the win authority — **the only one**.
  Nothing else in the codebase may decide a hand has gone out.
- **`TryFindCover` returns *a* cover, never a canonical one.** Thirteen hearts in sequence come
  back as `3+3+3+4`, not as one thirteen-card run, because the search takes the first candidate
  containing the lowest uncovered card. `IsWinning` is what is settled; the shape of the cover
  is not. **P8 must not assume the tidy grouping** — BUILD-PLAN P8 has been amended.
- **A "wins only if a joker plays a card the hand holds" test cannot be built out of a run.**
  A joker can nearly always play *outward* from a run onto a rank the hand does not hold —
  below the bottom card or above the top — so a rival cover always exists in which the joker
  merely fills a gap, and a generator that never substituted for a held card would find it too.
  Blocking one end only moves the boundary a rank along, and the ace is the sole natural stop.
  **A set has no escape**: it is capped at four suits, so five fives (two decks) plus a joker
  can only cover as two three-card sets, and whichever suit the joker plays is one the hand
  holds. That is `AHandWinsOnlyByPlayingAJokerAsACardItAlsoHolds`. The run flavour is tested
  one level down, on the candidate `2♦ 3♦ [🃏] 5♦ 6♦` with the real 4♦ melded elsewhere.
- **Jokers make almost any hand winnable — mind this when writing negative tests.** With two
  spare jokers any orphan finds a set, so a hand that must evaluate `false` has to be
  joker-free or joker-poor. Two drafted "not winning" hands turned out to be winning that way.
- **Performance is a non-issue.** Three thirteen-card stress hands — the 4,032-candidate one,
  a two-deck hand holding every diamond twice, and a losing hand that forces full exhaustion —
  take about **100 ms** in total. The pinning does that work; the dead-end memo changed nothing
  measurable and is kept only as a bound.
- **No partial-cover search exists**, deliberately. `TryFindCover` is all-or-nothing. A bot's
  "largest cover found" and a player's "best so far" hint need a *scored* version of the same
  backtracking, and BUILD-PLAN P10 now says so.
- **No purity requirement is implemented** — `RULES.md` §7.1 leaves "is a pure sequence
  required" open and recommends treating it as not-a-rule. If it ever settles the other way it
  is a filter on the cover, not a change to the search.
- **Guards:** the same card instance twice throws `ArgumentException`; a hand over
  `HandEvaluator.MaximumHandSize` (64, one bit per card) throws; an empty hand is covered by no
  melds at all and returns `true`.
- **No new rules question.** P5 shipped 19 tests (119 total).

**Still current, from P4:**

- **`SetGenerator.Candidates(hand)` → `IReadOnlyList<Meld>`**, mirroring `RunGenerator` in
  every respect: eager, de-duplicated by card set, jokers taken in ascending index order.
  It walks the **four suits once per rank**, taking each suit as a held card, a joker, or
  nothing — a three-card set is a four-suit set with one suit left unfilled. That single
  formulation gives the ≥3-distinct-suits rule, the four-card maximum and joker substitution
  at once, with no subset enumeration anywhere.
- **⚠️ The two generators can emit the same card set — P5 must de-duplicate across them.**
  It happens for any meld holding **at most one real card**: `{9♦,🃏,🃏}` is a run (jokers as
  10♦ and J♦) *and* a set (jokers as 9♠ and 9♥), and `{🃏,🃏,🃏}` is both trivially. Not
  wrong — identity is the card set — but it makes the cover search try the same cover twice
  and makes `TryFindCover` report a kind arbitrarily. **`MeldCandidates.For` should
  de-duplicate with the usual set comparer and keep the run interpretation**, which is
  generated first. BUILD-PLAN P5 has been amended to say so.
- **Sets cannot explode the way runs can.** Measured worst case **639** candidates — nine
  cards of one rank split (3,2,2,2) across the suits plus all four jokers — against P3's
  4,032. The closed form is in the test: every choice of *k* real cards in distinct suits
  plus *j* jokers with 3 ≤ k+j ≤ 4. The §7 risk row now says the explosion risk is P3's alone.
- **The brute-force cross-check paid off again**, and was trivial for sets: a subset is a set
  iff it holds 3–4 cards whose real members share a rank and occupy distinct suits. Nothing
  had to be said about jokers at all — with at most four cards there is always a free suit.
- **All-joker sets are emitted**, matching P3 and `RULES.md` §9 #8's *unlimited jokers*
  recommendation. `AHandOfNothingButJokersStillMakesSets` and its `RunGenerator` twin are the
  pair to change together if that ever settles the other way.
- **No new rules question.** §6.2 was already `EXPERT`-confirmed and settled; the packet
  needed no judgement call beyond the all-joker default P3 had already taken.
- P4 shipped 18 tests (100 total).

**Still current, from P3:**

- **`RunGenerator.Candidates(hand)` returns `IReadOnlyList<Meld>`**, not the bare
  `IEnumerable<Meld>` of BUILD-PLAN §3.4 — generation is eager anyway, because
  de-duplication has to see every candidate. Deliberate; don't "fix" it to lazy.
- **`Meld` and `MeldSlot` are shared with P4 and P5.** A slot is
  `(Card Card, Rank PlaysAs, Suit InSuit)`: for a real card its own rank and suit, for a
  joker what it stands in for. A meld's **identity is `CardIds`** — never its display value.
  `Meld` validates only ≥ 3 cards and no card used twice; run/set legality is the
  generator's. `Meld.Overlaps` exists for P5.
- **Jokers are taken in ascending index order** inside the fill recursion. That is not a
  detail — it is what makes the search enumerate joker *combinations* once each instead of
  every permutation, and P4 needs the same trick.
- **De-duplicate with `new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer())`.**
  The BCL comparer does structural set equality; no custom comparer is needed.
- **Two numbers in the docs were wrong and are now measured.** All thirteen ranks of one suit
  gives **76** candidates, not 77 — `A-2-…-K` and `2-…-K-A` are the same thirteen cards. The
  joker-heavy worst case gives **4,032**, not "hundreds"; the spec and the §7 risk table both
  now say thousands. Neither is a bug: a brute-force enumeration of every subset agrees with
  the generator card set for card set.
- **A brute-force cross-check earns its keep.** `GeneratorFindsExactlyTheCardSetsThatCanFormARun`
  enumerates all 2ⁿ subsets of a hand and asks, backwards, which could fill some window. It
  is what proved the 4,032 correct rather than merely bounded. **Write the equivalent for P4.**
- **Assumption taken: all-joker melds are legal**, so `🃏🃏🃏` is a run candidate. This follows
  `RULES.md` §9 #8's *unlimited jokers* recommendation, which rev 10 widened to name the case;
  requiring one real card per meld would be a stricter rule that §6.1 nowhere states. A hand
  can hold at most four jokers, so the blast radius is five extra candidates.
  `QUESTIONS-FOR-MYA-LAY.md` asks it as a table situation. **P4 should match; P5 must not
  assume a meld contains a ranked card.**
- **Test hands come from `BurmesePoker.Tests/Hands.cs`** — `Hands.Of("2D", "3D", "RJ")`, with
  `CardId`s assigned in the order listed so duplicate copies stay distinguishable. `RJ`/`BJ`
  are the jokers. This is where `CardText.ParseRank` finally earns its keep.
- P3 shipped 22 tests (82 total).

**Still current, from P1:**

- **Build cards with `Card.Ranked(id, rank, suit)` and `Card.Joker(id, color)`**, not the
  positional constructor. They derive `Color` from `Suit`, so a card cannot be given a colour
  that contradicts its suit. The positional constructor stays public because §3.1 specifies
  it; the factories are the ergonomic path.
- **`Deck.TwoDecks()`** builds the 108-card shoe in one call. `Deck.Cards` is a **live**
  read-only view, top first — `.ToList()` it before drawing or shuffling if you need a
  snapshot. `Deck` copies the cards it is constructed with.
- **Index 0 is the top of the deck.** `DrawFromTop` takes index 0; `DrawFromBottom` takes the
  last. Both matter at setup: `RULES.md` §3 step 4 turns up one money card from the **bottom**
  and one from the **top**.
- **`DeckExhaustedException`** (in `Domain/Cards/`) derives from `Exception` directly, not
  `InvalidOperationException`, so an empty draw pile is distinguishable from a bug. **P7 must
  reuse it rather than inventing another** — BUILD-PLAN P7 has been amended to say so. P9 is
  where it actually gets caught and turned into the discard-pile reshuffle (`RULES.md` §5).
- **Shuffling is `Random.Shuffle(Span<T>)`** over `CollectionsMarshal.AsSpan`, not
  `OrderBy(r.Next())`. Don't "simplify" it back — the old form is not a uniform permutation.
- **New rules question, `RULES.md` §9 #11 — a turned-up joker.** `SameValueAs` compares
  colour as well as rank and suit, which is a no-op for ranked cards but discriminates
  jokers by colour. Nothing says what a joker turned up as one of the two money cards
  designates, and it will happen about one round in fourteen. **Safe default for P2: no
  special case — designate by `SameValueAs` like any other card**, so a turned-up red joker
  designates the two red jokers and neither black one. Phrased neutrally in
  `QUESTIONS-FOR-MYA-LAY.md`. **Do not block on it.**
- P1 shipped 32 tests (60 total). No surprises in the packet itself — it was mechanical.

**Still current, from P0:**

- **Run the app with `dotnet run --project BurmesePoker.Console`**. `Program.cs` is a
  one-line placeholder; P8 rebuilds the front end.
- `MoneyCardStatus` and `PlayerAction` were deliberately **not** ported — superseded by
  `MoneyCardRegistry.Multiplier` (P2) and `TurnAction` (P7). `CardPlayType` became
  `Melds/MeldKind`.
- `UserPromptFactory` was deleted with the rest of `Logic/`. **P8 should read it from the
  pre-rewrite snapshot:** `git show 79d86bd:BurmesePoker/Logic/Factories/UserPromptFactory.cs`.
  ⚠️ **Corrected by P55 (2026-08-30): this named a `pre-rewrite` tag that does not exist, so the
  command as written failed.**
- `CardText.ParseRank` is still uncalled by the domain. It is expected to earn its keep
  building hands in P3/P4 test fixtures.
- **All three projects target `net10.0`** and the solution file is **`BurmesePoker.slnx`**.
  Both were chosen deliberately: Nick's standing preference is the newest supported .NET
  tooling. Don't "fix" either back. Tests are **xunit v3 4.0.0** on
  **Microsoft.Testing.Platform**; `Microsoft.NET.Test.Sdk` and `coverlet.collector` are gone,
  and so are `xunit.runner.visualstudio` and the coverage collector — nothing needs them.
  The Console project has **no `Spectre.Console` reference**; **P8 adds it back** at the
  current version.
- **Two MTP consequences a cold session will trip over:**
  1. `global.json` opts `dotnet test` into MTP mode. **Do not delete it** — without it
     `dotnet test` fails outright.
  2. **Filtering is `--filter-method "*Name*"` / `--filter-class "*Name*"`.** VSTest's
     `--filter "FullyQualifiedName~Name"` is rejected.
- The test project is `<OutputType>Exe</OutputType>` — expected for xunit v3, not a mistake.
- **`BUILD-PLAN` §5 P3's "Done when" said 8 candidates** while the packet body said 5. It was
  amended to **5** in P0, along with the same stale 8 in the §8 risk table.
  `docs/spec/RUN-CANDIDATES.md` was already correct.

---

## Decisions needed from Nick

✅ **Nothing blocks a packet. Every rules question that ever blocked one is closed** — twenty-three
of them across four sessions with **Mya Lay and Aung Aung** on 2026-08-20/21, including all six of
§5.1's specification (§9 #16–#19, #25, #27), both of the win condition's (#22, #29) and the whole
money layer (#4, #5, #10–#14, #24, #26, #30, #31).

⚠️ **Five calls are Nick's rather than a rules matter, and none of them stops P29 starting.**

1. 🔥 **P24's position.** It was sequenced ahead of the §5.1 work deliberately, on the argument
   that §5.1 was blocked and P24 would make that conversation productive. **The conversation
   happened**, so the argument is spent, and P24 explains *why the computer chose this card* while
   **P25–P28 changed what a good card is at three of four table sizes, what a card is worth, which
   cards are legal to throw at all, and who is sitting where — all four have now shipped.**
   ⚠️ **P28 also gave P24 a fifth question to explain**, and it is the one where a *why* helps most:
   refusing a claim costs the seat a rank and discloses that it holds one. ⚠️ **`BUILD-PLAN.md` §4 records P24 after P29 as a
   recommendation, not a decision** — P24's scope was set by Nick on 2026-08-20 and moving it is
   his call.
2. ⚠️ **When to re-measure.** P29 regenerates `docs/strategy/measurements.csv` under the corrected
   rules, and `sim suite` was **five hours** at P23. ✅ **P25's share of the bill is now measured
   and it is small** — four-handed greedy-vs-simple went from 102 to 86 rounds/s at one seed,
   about 16%, roughly half of it the round being longer rather than the evaluator being slower.
   **Everything in `docs/STRATEGY.md` is wrong until it runs**, and it can only run once the other
   three are in.
3. 🔥 **Whether a table smaller than four is a goal at all.** P25 implemented the two- and
   three-handed win conditions because §7.1.1 states them; `RoundEngine.MinimumPlayers` is still
   **4** (`RULES.md` §10 #7), so they are correct, tested and **never executed**. Making a smaller
   table playable touches the deal, the money layer and both front ends — **a packet of its own,
   and nobody has asked for one.** It is fine to leave it exactly as it is; it is not fine to
   leave it undecided and unrecorded.

4. 🔥 **Whether refusing a claim is actually right, and whether anybody should be able to choose.**
   P28 made every rung refuse whenever it may, on §4.5's own reasoning — the claim would close that
   rank in its hand — but **nothing prices the disclosure**, and a person at the browser table is
   offered both buttons with no guidance beyond the *why*. It is a **policy inside a rung** rather
   than a rung of its own, which is the shape P22 warned about (a rung with a knob is a family of
   rungs). ✅ **The cheap version is a P29 measurement**, not a packet: one head-to-head between two
   policies of `outs`. **The expensive version is a new rung and a new row in the ladder**, and
   nobody has asked for one.

5. 🔥 **Whether the browser should ever show a ×5, and where.** P26 leaves the jackpot
   **settled but never drawn**: `CardView.Multiplier` is 0, 1 or 3 by construction, because a
   marker beside one card cannot depend on the rest of the round's ownership, so a player who owns
   both partners of a 7♦/A♠ turn-up sees `($$$)` and is paid ×5. **It is the largest single swing
   in the game and the only one the table cannot see coming.** Showing it means a per-hand fact
   rather than a per-card one — plausibly P24's territory, plausibly a UX packet of its own, and
   **no packet owns it today.**

✅ **One rules question is open and it blocks nothing** — `RULES.md` §9 **#32**: does the ×5 need
the 7♦/A♠ pair specifically, or would any two tripled values do (a 7♦ and a joker, two jokers of
opposite colours)? **The combination exists only because rev 21 made jokers permanent.** ✅ **P26
wrote the narrow rule and pinned it with two tests** — `TwoTripledJokersInOneHandAreNotAJackpot`
and `ASevenAndAJokerTurnedUpAreNotAJackpotEither` — so widening it later has to break a test.
`QUESTIONS-FOR-MYA-LAY.md` **Q9** has it phrased as a table situation.

⚠️ **Three rulings in the rules are `PLAYER` house rulings rather than recall, and stay flagged**:
that a §5.1 release survives the reshuffle (*"nobody really knows"*), that taking a joker closes
the other jokers (*"I'd assume"*), and that doubling is the ceiling — **superseded anyway by rev
20's triple.** A house ruling that later turns out wrong is a **rule change**, not a typo fix.

**Do not block on them.**

---

## Session log

| Date | Packet | Outcome |
| --- | --- | --- |
| 2026-08-31 | P62 | **Partial — step 0 only, and it is in another repository.** ⚠️ **Nothing in this repository changed**; the tree is green at **941**, unchanged, and no measurement can move. 🔥 **The rest of the packet cannot be executed from a session** — it is a round on a real phone over a real carrier, and the packet itself names emulation, a simulator, a responsive viewport and a tablet as ways of not being a person on a phone. ✅ **Built: Traefik's access log** (`ansible-nas` `c57a9ea4`) — `[accessLog]` in JSON to stdout, gated on a new `traefik_access_log_enabled` defaulting on. ⚠️ **Without it, a carrier request and a house-wifi request leave identical evidence**, which is the way P62 returns a false pass. 🔥 **`User-Agent` is kept explicitly** because Traefik drops headers by default and step 4 is a question about **WebKit specifically** — unanswerable if the log cannot say which browser made the request. ✅ The template was **rendered and TOML-parsed both ways** rather than eyeballed (a malformed `traefik.toml` takes the proxy down), and the role's molecule verify slurps the templated config back and fails without the access log. ⚠️ **Owed before travelling: run the playbook and see `ClientHost` populated on the real box** — an empty or bridge-local value is the failure step 0 exists to catch. ⚠️ **P61's redeploy is owed on the same trip.** |
| 2026-08-30 | P61 | **Done — the two defects a real device found, on Opus 5.** Tree green at **941**, from 938; ⚠️ **`Domain`, `Presentation`, `Server`, `Console` and `Sim` byte-identical** — the diff is two stylesheets, one new test class and one extended one, so **no measurement can move and no suite is owed.** 🔥 **The copy-link's fix needed nothing in `:global()`'s place**: the packet recommended moving the reveal into the unscoped `app.css`, and it was not needed, because Blazor's rewriter appends the scope attribute to the **last compound selector only** — `.can-copy .link .copy` is emitted as `.can-copy .link .copy[b-…]`, and an ancestor outside the component was never constrained. **`:global()` was not solving a problem; it was the problem.** ✅ The rule stays beside the `display: none` default it overrides, where the ordering that makes it win is visible; `::deep` was rejected for the packet's own reason; the `<a class="url">` fallback is untouched. ✅ **Measured in a browser engine** — headless Chromium over CDP against the running client: `can-copy` on `<html>`, computed **`display: block`** (a flex item blockifies `inline-block`) at **82 px**, and the live CSSOM holding **two** `.copy` rules where P60's tablet held exactly one. 🔥 **`ScopedCssTests` fences the class of mistake and reads the rewriter's output**: one fact fails on any `:global(` under `obj/<config>/net10.0/scopedcss/**` (configuration taken from where the assembly runs, so a stale `obj/Release` cannot fail a Debug build), the twin reads the **served bundle** and asserts the reveal exists **and is declared after** the default — the two weigh the same, so order is the whole of why one wins. ⚠️ **The scan strips comments first**, or it would fail on the one file that had learned the lesson. 🔥 **§3.11 A18 amended, not contradicted**: the argument for the ellipsis is *the whole name is a hover away* and the rule carrying it was `max-width: 56rem` — two axes, and a tablet in landscape (1316 px, `(any-hover: none)`) is where they come apart. `SeatPanel.razor.css` gains a second `@media` on `(any-hover: none)`, **after** the width one because on a narrow touch screen both match and weigh the same; `ViewportTests` gains the fact. ⚠️ **Widening the ring's name column is the fix this rejects** — P58's trap, one axis over. ⚠️ **Four mutations, all of the stylesheets and none of the tests**, each turning the right fact red. ⚠️ **Owed and said out loud: the redeploy and the device.** A work cycle does not push, so nothing here is proved on a real tablet until the image is rebuilt and the running container re-read (P60's *pulled is not running*); and **headless Chromium answers `(any-hover: none)` by default**, so only the no-hover branch was exercised in a browser. |
| 2026-08-30 | P60 | **Done — the table on real devices, on Opus 5.** ⚠️ **A verification packet, and the whole diff is documents** — nothing in the repository changed, no measurement can move, no suite owed; tree green at **938** (unchanged: no test was added, and none of what this packet establishes is reachable from the test project). ✅ **The deployed commit was read off the running container before anything was touched and is stated: `62ed294` (P59).** 🔥 **Found on the way there and worth more than it looks — *pulled* is not *running***: the NAS held `:latest` = `2881b65` **pulled four hours earlier** while the container still served **`f309a9d`**, so the role had fetched an image and never recreated the container. ⚠️ **P53's finding (8) is not “read the deployed tag”, it is “read what the running container is built from”**. ✅ **The device did what the packet said it would**: Samsung Galaxy Tab S9 FE (`SM-X510`), Android 16, Chrome 151, 1.75 dppx — **823 CSS px portrait, 1316 landscape** — so **one device rotated exercised both sides of the only breakpoint the application has**, with portrait landing *inside* P58's 600–896 defect band. ✅ **P58's fix holds in a font this workstation has never rendered**: four 181 px columns, every name `white-space: normal`, **none clipped** (`scrollWidth == clientWidth` on all six), `Khine Myat Zin (opportunist)` wrapping to two lines, and no horizontal overflow. 🔥 **The defect it found is above the line, and a tablet is where it lives**: in landscape the ring returns, the name reverts to `nowrap` + `ellipsis`, and **three of six are clipped** — *“Su Htwe (oppo…”* 77%, *“Myat Htwe (op…”* 70%, *“Khine Myat Zin (oppo…”* 80% — while the device answers `(hover: none)`, `(any-hover: none)`, `(pointer: coarse)` **at 1316 px**. ⚠️ **A18 is fitted to *width* while the argument that justifies the ellipsis depends on *hover*, and the `title=` is present and unreachable.** 🔥 **P54's copy-link has never been visible in any browser**: `Tables.razor.css` reveals it with **`:global(.can-copy) .link .copy`**, but `:global()` is a **CSS-Modules** construct rather than Blazor scoped CSS (which has `::deep`), the Razor rewriter emits it **verbatim**, and the browser drops the whole rule as an invalid selector — **the live CSSOM on the device holds exactly one `.copy` rule and it is `display: none`.** ⚠️ **Nothing is broken for a player** (the `<a class="url">` beside it is P54's designed fallback) — **the enhancement simply never appears**, which is precisely why *“it was never pressed in a browser”* never turned into a bug report. ✅ **The handler is sound**: revealed by an injected style and pressed **physically**, the delegated listener fires and the clipboard receives the correct **forwarded** absolute URL. 🔥 **And a synthesized touch is not a person either** — the identical press through CDP `Input.dispatchTouchEvent` fired the click handler but **silently failed the clipboard write** (no user activation) where `adb input tap` succeeded: a third instance of this project's scar, after `--no-restore` and `curl`. ✅ **A whole round was played from the tablet** — opened through **P56's two-step per-seat form**, which grew its four `Wanted.PerSeat[…]` selects on the device and produced `opportunist, sprinter, easy, warden`; sat down with a physical tap and the on-screen keyboard; **the claim on the turned-up 3♠ was refused** (*“Myat Htwe (warden) refused Nick the turned-up card — which they may only by holding that rank”*, P28 on a tablet); the blind draw arrived privately; **rotated mid-turn** without losing the circuit or the standing question; settled after **47 turns**. ✅ **Log-side evidence, P53's grade**: `negotiate` **200**, three circuits each closing **101**, the playing one alive **243 s**; touch targets measured on the device at **44 px** buttons and **66 × 76 px** cards (§3.11 B11 confirmed rather than assumed). ⚠️ **The phone half is NOT discharged and now has a named blocker**: no phone, no cellular, no Mobile Safari — **and item 3 cannot be satisfied even with a phone in hand, because nothing logs the client IP.** Traefik has **no `accessLog` configured** (checked on the box) and the app logs no remote address, so *“prove the session came over the carrier rather than silently over the home wifi”* has **no instrument**; that is a prerequisite living in `ansible-nas`. ✅ **The IPv6 question is answered without a device**: `poker.nickjones.dev` is `209.128.193.153` with **no `AAAA` record**, so the DNS64/NAT64 mode is live and unmitigated — **check it before blaming the browser**. No rules question; `RULES.md` stays rev 31. **`P61` is proposed: the two defects, each with a fence.** |
| 2026-08-30 | P59 | **Done — circuit survival: the connection that drops and comes back, on Opus 5.** ⚠️ **Tests and one package only**: `Domain`, `Presentation`, `Server`, `Console`, `Sim` **and `Web`** byte-identical, no measurement can move, no suite owed. Tree green at **938**, from 933. 🔥 **A real Blazor circuit can now be held from a test** — `AddInteractiveServerComponents` narrows `/_blazor` to **`blazorpack`**, which no client library speaks, so `BlazorPack.cs` writes down the slice of it a circuit's opening needs and `BlazorCircuit.cs` starts one from **the page's own component markers**, kills the socket with **no close frame**, and asks for the circuit back by name. ✅ **The instrument proves itself**: `TableView` sits down only when it is *really* interactive (§3.11 C13), so the person-seat ceasing to wait is proof a circuit started and rendered rather than that a page was fetched. 🔥 **Three answers and they differ** (retention 3 s against patience 8 s, the shipped ordering shrunk): *inside the window* the reconnection succeeds and nothing is lost; *past the window, inside the patience* the reconnection **fails** and the seat is **given up** — `TableView.Dispose` stands the player up — **while the turn is not**, and sitting down again by name takes the seat back with the question still standing; *past both* the computer has played the turn. 🔥 **So a seat and a turn are recovered by different mechanisms** — the seat by name (P13.6), the turn by the patience — and **losing the circuit is not losing the game.** 🔥 **P54's claim is exercised rather than asserted against itself**: with the patience **below** the retention period the computer plays the turn of somebody **the framework is still holding**, the reconnection still succeeds, and **nothing on screen says anything happened.** ✅ **Proved able to fail the way the packet asked** — shortening the patience below the window turns the *inside the window* test red. 🔥 **`TableBoard.Turn` turned out to be the wrong instrument and measuring found it**: a seat is asked **twice** in a turn, so a turn sits on a seat for **two** patiences; `StoodInFor` is the fact itself. ⚠️ **`tc netem` not done** — `TestServer`'s socket is in-memory pipes and shaping a real one needs root; **latency is P60's**. ⚠️ **The recorded obstacle (`SeatChannel`'s undisposed wait handle, `Ask` taking no token) was never reached** — nothing here tears a seat down. |
| 2026-08-30 | P58 | **Done — a viewport standard, a fence for the one breakpoint there is, and a defect the standard found, on Opus 5.** ⚠️ **Front-end only**: `Domain`, `Presentation`, `Server`, `Console` and `Sim` byte-identical, no measurement can move, no suite owed. Tree green at **933**, from 929. 🔥 **The sub-896 px question is answered *no*, and the evidence says the narrow end was never the problem**: measured in Chrome at eleven real viewports, **360/375/390 px resolve the stacked felt to a *single* column** — `minmax(9.5rem, 1fr)` cannot fit two inside 360 − 40 page padding − 24 felt padding — with nothing trimmed and nothing overflowing. **A phone is the safe end of this layout.** 🔥 **What was broken is the band above it, and it is the exact defect the 56rem line was drawn to prevent**: the pack packs to its floor, so a column is ~158 px at 412 px and ~154 px from 600 to 896 px — **narrower than at 360** — and there the computer's own seat names ellipsed (*“Aung Aung (exp…”*, which Nick saw on screen mid-session). The arithmetic agrees with the eye: the name measures 122.8 px and its chrome 39.8, so it needs a 163 px column — 158 trims, 167 does not. 🔥 **Fixed by wrapping and not by a wider floor**: `Khine Myat Zin (opportunist)` measures 183 px *in this machine's `system-ui`*, and **a floor fitted to one platform's font is a fence that passes where it was fitted**; ⚠️ **an ellipsis is honest only where the whole name is a hover away**, true above the line and false below it. Re-measured after: nothing trimmed at any width, no overflow, and 720 px went 4 columns → 3. ✅ **§3.11 gained item **A18** and `ViewportTests` is four facts, each proved able to fail by mutating the stylesheet rather than the test** — the two files disagreeing, `nowrap` put back, the floor raised to 19.5 rem (*“asks for 376px … has to be playable at 360px”*), and `width=device-width` deleted. ⚠️ **Not a device test**: no phone, no tablet, no touch, no Mobile Safari — **P60 still owns that**, and an iframe is not a person any more than `curl` is. |
| 2026-08-30 | P55 | **Done — Gitea primary, GitHub a mirror, on Opus 5.** ⚠️ **Ops, and the whole diff is documents** — `Domain`, `Presentation`, `Server`, `Console`, `Sim` and `Web` byte-identical, no measurement can move, no suite owed. ✅ **Refs first, and the check is the deliverable**: GitHub's one stale branch `p49-simulations-doc` was at **exactly `a12c7a3`**, the same object as GitHub `main` and zero commits ahead, so dropping it lost nothing — ⚠️ *“there was nothing to lose” is a measurement here rather than an assumption*, because this is the operation where a remote-only ref vanishes silently. No tags on either remote, before or after. 🔥 **`pre-rewrite` was settled by correcting the claim rather than minting the tag — Nick's call, against the plan's own recommendation — and it was nine sites across five documents, not the one sentence the plan costed**: `CLAUDE.md`, `RULES-TECHNICAL.md`'s HISTORICAL banner, three in `BUILD-PLAN.md` (**including P0's own acceptance line, *“`git tag pre-rewrite` exists”*, which was never met**) and three in `STATUS.md`. 🔥 **The one with teeth is `STATUS.md`'s runnable command** — `git show pre-rewrite:BurmesePoker/Logic/Factories/UserPromptFactory.cs`, written for P8 as the way to read the old Spectre prompts — **failing since P0 and never noticed, because P8 read the file before it was deleted.** All nine now name **`79d86bd`** (*“Pre-rewrite snapshot”*, the parent of `b32d08b`). ⚠️ **P0's log row and acceptance were annotated, not back-dated** — the record stands with the correction beside it, which is the rule for a newest-first narrative. 🔥 **The swap found the thing that would have broken the next session: a remote's name is documentation.** `origin` is Gitea, `github` is GitHub — and **`git push gitea main` was quoted as *the CI trigger* in seven places** across `CLAUDE.md`, `BUILD-PLAN.md` and `STATUS.md`, every one a command the rename makes fail. All seven now say `git push origin main`. ⚠️ **Nothing in the tree could have caught it**: `DocumentationTests` resolves a `bash`-fenced command against the parser that would accept it, and **a ref has no parser**. ✅ **The mirror is configured with sync-on-push** (`8h` interval as a backstop) against `github.com/nickjones33/BurmesePoker`, PAT fine-grained, `contents: write`, that repository only, ⚠️ **never entering a session** — the third credential in this track and the third to live only in a settings page. ✅ **Acceptance is a measurement rather than a settings-page claim**: P55's own commit `8d619bc` was pushed to `origin` and reached GitHub **in about ten seconds, with no second push** — ⚠️ **which is exactly what a manual *Synchronize Now* does not establish**, and the half the acceptance actually asks for. 🔥 **Its cost is stated rather than discovered later: a push mirror force-pushes, permanently.** The first sync was a fast-forward (`a12c7a3` is a strict ancestor of `b2c84c2`, checked *before* enabling it), but from here **a commit made straight on GitHub is erased by the next sync rather than conflicting** — which is why `CLAUDE.md`'s new rule is **never push to `github` by hand**, a rule and not a preference. ✅ **Nick's answer on pull requests: `tea`**, installed from Arch `extra` at **0.15.1**. ⚠️ **`gh` was never installed on this machine at all**, so the standing *“use the `gh` CLI”* guidance was unrunnable as well as aimed at the wrong forge — **and aiming it at the mirror is the dangerous half**, since a merge landing on GitHub would be overwritten by the next sync. `CLAUDE.md` now carries both rules and says out loud that **this project has never used a pull request**: fifty-seven packets, one commit each, straight onto `main`. ⚠️ **`git push origin main` ran during the packet**, so the deployed image is built from `b2c84c2` and **P57's deploy check is the only thing left of it**. No rules question; `RULES.md` stays rev 31. Tree green at **929** (unchanged — ⚠️ **no test was added, and none could be**: every fact here is about refs, remotes and a settings page, none of which the test project can reach). |
| 2026-08-30 | P57 | **Done — the lobby stops offering an opponent it cannot build, on Opus 5.** 🔥 **Nick's option 2**: fix the menu, not the invariant. `OpponentMenu.CanBeAskedForItsSecondBestMove(rung)` is `rung.Create(0) is IRanksDiscards` — ⚠️ **asked of the agent rather than declared on the rung**, because the `FallibleAgent` constructor that threw asks that exact question of that exact object — and `Advanced` filters on it beside the published-row rule. 🔥 **The menu now excludes on two grounds of different kinds**: *no published row → not offerable* is honesty (a price that cannot be stated must not be charged, which is what keeps `prospector` and `purist` out), *cannot name a second-best move → not offerable* is **P19's invariant** (a level is a rung wrapped in a mistake rate). ⚠️ **`random`'s row was deleted from `Published` rather than left to be filtered** — a row that can never be reached is dead data — **but the rule is what keeps it out**, not the deletion. 🔥 **The fence was amended in the same breath and is stronger, not weaker**: ⚠️ *amending a fence to make a build go green is the move this project distrusts most*, so `PublishedFigureTests.EveryOpponentTheLobbyOffersShowsTheMarginTheCsvMeasured` asserts the exclusion **about `random` by its inability rather than by its name** — it *has* a published row against the reference, *cannot* be asked for a second-best move, is *not* offered, and `Offers("random@0")` is false — while every rung that **is** offered must be able to answer. 🔥 **The new test is the packet**: `OpeningATableTests.EveryOpponentTheLobbyOffersCanActuallyBeBuilt` takes every name the form can post — the advanced rungs **and the four levels** — and **resolves it and then constructs it** (`FindOrProbe` *then* `Create`), because **resolution is exactly the step that succeeded** while construction threw; a test that stopped at resolution is the test that was already here. ✅ **Proved able to fail**: `random@0` is not offered **and** `Create` on it still throws `ArgumentException`, so putting it back in the menu is a red build. ⚠️ **Both lists, not just the advanced one** — every level is `BotCatalog.Hardest` through the same constructor. ⚠️ **Not verified on the deployed site**: the acceptance's last line wants `git push origin main`, the play re-run and each seat picked in a browser — it joins P53's phone round and P54's two browser checks. No rules question; `RULES.md` stays rev 31. `Domain`, `Presentation`, `Server`, `Console` and `Sim` byte-identical — no measurement can move, no suite owed. ⚠️ **One existing assertion moved with the list**: the fence's *at least nine opponents were priced* floor came down to **eight**, because it tracks what the menu may show rather than what the CSV measured. Tree green at **929** (was 928 — one new fact). |
| 2026-08-30 | P53 | **The deploy — still partial, on Opus 5. The table is up at `poker.nickjones.dev`; the browser sitting is not done.** ✅ **Nick ran `ansible-playbook nas.yml --tags burmesepoker --become --ask-become-pass`; the session did every check.** ⚠️ **Only one of the two recorded blockers was real**: the passphrase-protected key was already in the `ssh-agent` (`ansible nas -m ping` → pong first try, the fingerprint matching `ssh-keygen -lf ~/ubuntuServer22key.pub`), so P53's note that `ansible all -m ping` returns `Permission denied` had simply stopped being true — **check `ssh-add -l` before declaring an SSH blocker.** 🔥 **The WebSocket assumption is now a frame-level measurement**: a raw TLS handshake against `/_blazor?id=<token>` returns **`101 Switching Protocols`** from **Kestrel** through Traefik with the auth middleware attached, and negotiate offers `WebSockets, ServerSentEvents, LongPolling`. ⚠️ **The handshake set its own `Authorization` header**, so what is proved is that nothing in the path objects to an *authenticated* upgrade — **not** that a browser replays cached credentials on one, which is the one question the phone round still owns. 🔥 **The idle-timeout assumption too, on the harsher case**: the same socket completed the SignalR handshake and sat still for **7½ minutes — 29 server pings at ~15.5 s, no close frame, no reset.** ⚠️ **The hub speaks `blazorpack`, not `json`** — a `json` handshake is refused by the *app* (`"The protocol 'json' is not supported."`) and reads exactly like a proxy fault. ✅ **`UseForwardedHeaders` proved through the real proxy** rather than a synthetic header: the container logs `Request finished HTTP/1.1 GET https://poker.nickjones.dev/…` for requests arriving over the bridge at `172.17.0.13:8080` — ⚠️ **`Request starting` still shows `http://`**, because hosting diagnostics logs it before the middleware runs, so read the *finished* line or a working forwarder looks broken. ✅ **Every role decision confirmed on the running container**: memory `536870912` (512 MiB, not `64m`), `restart=unless-stopped`, **`ports=map[]`** (no published host port — what keeps P51's empty `KnownProxies` safe), non-root uid 1654, all **eight** Traefik labels with the bcrypt `$2b$10$…` intact and no `$$` escaping; resident at 42 MiB of 512. ✅ **The gating branch is right, not merely present**: unauthenticated `/healthz` is **401** with `www-authenticate: Basic realm="traefik"` — a locked door, not the 503 an empty users list produces — and with credentials `/healthz`, `/_framework/blazor.web.js` and `/` are all **200** over the wildcard `CN=nickjones.dev` Let's Encrypt certificate. ✅ **A real browser played a real round and the credential question is answered: yes** — Chrome past the basicauth dialog gets `_blazor/negotiate` **200**, and the server log shows `GET /_blazor?id=…` with a `Request starting` and **no `Request finished`** (an open connection — a WebSocket, not the repeated start/finish pairs long polling leaves); the claim was refused, the blind draw arrived privately over the circuit, and four bot seats took their turns by server push. ⚠️ **Still ◐**: the acceptance names a phone **off the home network**, and this was desktop Chrome on the LAN — carrier NAT and mobile Safari are untested. 🔥 **The finding that cost the most, found by pressing the lobby form: the deployed image is not built from `main`** — `:latest` was built 2026-08-28 21:50 from `860fb13` (**P52**) and `gitea/main` is **six commits behind**, so the running site predates **P54** and **P56** and still draws P19's *mixed table* checkbox. ⚠️ **`git push origin main` is the CI trigger and has not been run**, so **a packet marked done here is not thereby a packet that is running** — correcting this session's own earlier claim, one browser sitting does **not** close P53, P54 and P56; the latter two need a deploy first. **Read the deployed commit before testing a front end against the live site.** ⚠️ **The basicauth password entered the session transcript** — it exists on disk only as the bcrypt hash and was recovered from a `Basic …` header Nick read off his own browser; rotate with `htpasswd -nbB poker <new password>` and re-run the play if that matters. No rules question; `RULES.md` stays rev 31. **Nothing in this repository changed but the documents**; no measurement can move. Tree green at **928** (unchanged — no test was added, because this packet adds no code here). |
| 2026-08-29 | P56 | **Done — opening a table you actually want, on Opus 5.** 🔥 **§3.12 was amended rather than contradicted**: Nick's answer of 2026-08-29 was option (b), so `DifficultyLadder`'s remark (*"a menu with both in it would be the mistake this design exists to avoid"*), §3.12 itself and P19's remark were all rewritten to **levels are the menu; rungs are an advanced disclosure that states its price** — what the old rule forbade, *selling a measured-worse opponent as a matter of taste*, is unchanged, and **the margin beside the name is what pays that bill**. `BurmesePoker.Web/OpponentMenu.cs` offers ten rungs under the four levels, each drawn as *`sprinter` — +1.2 ± 0.8 points of win rate against `outs` — measurably stronger*. 🔥 **`PublishedFigureTests.EveryOpponentTheLobbyOffersShowsTheMarginTheCsvMeasured` fences it both ways**: each printed sentence is compared against `ladder.head-to-head.*` **with the sign turned round where the row is named the other way** (verdict word included), *and* a rung with a published row against the reference **must** be offered while one without **must not** — which keeps the money-ranked `prospector`/`purist` out by rule rather than by hand, and makes a newly-measured rung the menu never heard of a red build. ✅ **Nothing below the form was needed, as the plan predicted**: `HostedTable`'s seat resolution went `Find` → `FindOrProbe` (in a private `Seatable` that swallows the `ArgumentException` a malformed probe throws), ⚠️ **`Lobby`'s `--difficulty` stays `Find`**, and ⚠️ **the seat name and the journal attribution were split on purpose** — `Mya Lay (sprinter)` through `OpponentMenu.Called`, `sprinter@0` in the journal. 🔥 **Per-seat difficulty is a two-step post** (§3.11 C12: static SSR cannot grow a control per seat as the count is typed), and it cost one class: `NewTable` moved out of `Tables.razor` into its own file, **because nothing here renders a component in a test**; `SeatFill` replaces P19's checkbox with *same* / *mixed* / *each*, and `NeedsSeatChoices` is a **count check rather than a flag**, so changing the shape on the second step asks the seats again. 🔥 **Found by pressing the button, again**: a post with no `Wanted.PerSeat[…]` fields sets the property to **null** rather than to the initialiser's empty list — a **500 on the first post of the first step** that nothing in the tree could see; defended in the accessor now. ✅ **Proved end to end without a browser** — the site was started and posted to twice with `curl`, and the table it opened seats `(easy)`, `(sprinter)`, `(warden)` with the lobby reading *The computer plays easy, sprinter, warden*. ⚠️ **The quorum is said twice** (a note on the form, *Waiting for two more people to sit down; it deals when they are all here* on each row), the form's `Seats` default moved `MinimumPlayers` → **`DefaultPlayers`** (P32 one layer up), and the house table's `people` was already `1`. `docs/PLAYING.md` gained the advanced group and the quorum, **digit-free** — P39's one-home rule forbids a figure there. ⚠️ **Still no browser**: `curl` is not a person, so this packet adds a third outstanding browser check to P54's two. No rules question; `RULES.md` stays rev 31. `Domain` changed **in doc comments only**; `Presentation`, `Server`, `Console`, `Sim` byte-identical — no measurement can move, no suite owed. 🔥 **Three existing fences fired and were amended rather than deleted** — `MarkupStandardsTests.TheLobbyOffersEveryLevelAndKeepsNoListOfItsOwn` asserted *that no rung is offered in the lobby at all*, which is §3.12 written down as a test, so the decision could not be contradicted quietly; it now asserts what that rule was protecting (both lists generated, levels heading the menu, no level and no rung named in the markup). `StandingAnswerTests.NoFrontEndWritesOutWhatALevelIsCalled…` wanted `DifficultyLadder.ByStrength` in the razor and gets `OpponentMenu.Levels` plus `Assert.Same(DifficultyLadder.ByStrength, OpponentMenu.Levels)` — the chain rather than the name — and `LobbyTests`' seating scan followed `SeatingPolicy.Resolve` out of the markup into `NewTable.cs`. Tree green at **928** (was 920 — seven new facts and one new fence). |
| 2026-08-29 | P54 | **Done — long-lived-host hardening, on Opus 5.** 🔥 **The leak `HOSTING.md` §8 warned about was real, and confirmed rather than assumed**: `Lobby.Close` had existed since P13.6 and **only the tests ever called it**, so a hosted site accumulated a table per form press, hit `MostTables` (12), and from then on answered every *Open it* with an error — weeks after a deploy, reading as a broken form rather than a full site. ✅ **The parked bot loop was never the problem** (`HostedTable.Deal` breaks the moment `Ready` goes false, which the last viewer leaving makes it); what leaked was the slot and the memory. 🔥 **`HostedTable.IdleSince` starts at construction** — a table opened by a press nobody followed up has never had a viewer to lose and is exactly the case — cleared by `Arrive`, restarted by the **last** `Leave`; **`Lobby.House` is a named field** because once tables can be closed *the first table in the dictionary* and *the table this site was started with* stop being the same thing, and reaping the second leaves `dotnet run` an empty room and the deployed URL pointing at nothing. Window **30 min**, sweep **5**, by `TableSweeper` (a `BackgroundService`) — ⚠️ **registration fenced, because a correct reaper nobody runs leaks exactly as much as none**, which is the state this packet found. 🔥 **The patience number became a relation instead of a taste**: **90 s → 180 s**, against `CircuitOptions.DisconnectedCircuitRetentionPeriod` set explicitly to **2 minutes**, because inside that window the framework is *deliberately hiding a dropped connection from the player* — a shorter patience has the computer play the turn of somebody the framework is still expecting back. The two live in two files and are fenced against each other, one read from source and one off the real `Lobby`. ⚠️ **Copy-link is an enhancement over a real `<a>`** (absolute, `ToAbsoluteUri`, so behind Traefik it is P51's forwarded host): the reveal is a class on the **document** and the handler is delegated from it, because enhanced navigation replaces the markup and per-element wiring dies on the second visit. ✅ **The §7 gating decision was already landed by P53** and was inherited, not taken. ⚠️ **One finding recorded and not acted on**: `SeatChannel` never disposes its per-seat `ManualResetEventSlim` — not unbounded (the wait handle's `SafeHandle` finaliser releases it) and disposing it races the engine thread parked in `Ask`, which takes no cancellation token, so **`Server` was left byte-identical**. ⚠️ **Not verified in a browser** — these are unit and source fences plus a build; the acceptance's *"the lobby affordance works in a real browser"* is outstanding and belongs with P53's phone round. No rules question; `RULES.md` stays rev 31. `Domain`, `Presentation`, `Server`, `Console` and `Sim` byte-identical; no measurement can move. Tree green at **920** (was 914 — six new facts). |
| 2026-08-28 | P53 | **Partial — the `burmesepoker` Ansible role, on Opus 5. The role is built and committed; the table is not up.** Work in `~/source/repos/ansible-nas` (commit `7ffd645e`), which is why this repository's tree is untouched and still green at **914**: `roles/burmesepoker/{defaults,tasks}/main.yml` on the `mirroquest` two-file shape (fail fast on missing credentials → `docker_login` → `docker_container` with `pull: true` and the Traefik labels → the stop block), the include in `nas.yml` between `booksonic` and `calibre`, a page at `website/docs/applications/gaming/burmesepoker.md`, and the gitignored inventory enabled. `yamllint` clean, `ansible-lint` clean at that repo's `production` profile, `ansible-playbook nas.yml --syntax-check` passes. ✅ **P52's prediction held exactly**: `mirroquest_registry_password` is the `read:package` token `burmesepoker_registry_password` wants, and nothing was minted. ⚠️ **What is not done is not a judgment call**: the play needs `ssh-add ~/ubuntuServer22key` (the key is passphrase-protected — `ansible all -m ping` returns `Permission denied (publickey,password)`) **and** an interactive `--ask-become-pass`, and a session can type neither — so **the WebSocket and idle-timeout assumptions this packet exists to settle are still assumptions**, and the packet is ◐. 🔥 **The gating decision was taken — Task 5 option B, a Traefik `basicauth` middleware, which is the first auth pattern in that repo — and its shape is the finding**: the two labels are `combine`d in **only when `burmesepoker_basicauth_users` is non-empty**, because a router naming a middleware with an empty users list serves **503**, which reads as a broken deployment rather than as a locked door. Both branches proved with a throwaway play before committing (empty → six labels, set → eight); the bcrypt `$` signs survive intact, so `docker_container` needs none of compose's `$$` escaping. ⚠️ **It gives the phone round a second thing to settle**: a browser cannot set an `Authorization` header on a `WebSocket`, so the `/_blazor` upgrade depends on the browser replaying its cached basic credentials — if it does not, Blazor falls back to long polling and the table **works but feels slow**, so the acceptance is *check the network tab*, not merely *a round played*. ⚠️ **Two corrections to that repo's plan**: there is **no LAN check at `http://192.168.50.142:8080`** — this role publishes no host ports, exactly as `mirroquest` does (Traefik is `network_mode: host` and reaches containers over the bridge), and publishing 8080 is the one thing P51 forbids; and the plan's claim that there is no games category was wrong — **`gaming` exists**, with three servers in it. No rules question; `RULES.md` stays rev 31. Every project in this repository byte-identical; no measurement can move. Tree green at **914** (unchanged — **no test was added, because this packet added no code here**). |
| 2026-08-28 | P52 | **Done — a published image, on Opus 5.** The repo gained a **Gitea origin** (`gitea` remote beside GitHub `origin`) and `.gitea/workflows/publish-image.yml`: on a push to `main`, the `docker-builder` runner (the one carrying `gitea_actions_runner_mount_docker_sock`) builds **the repository's own `Dockerfile`, unrewritten**, and pushes `gitea.nickjones.dev/nickjones/burmesepoker` at **`:latest`** (what P53's role pulls) and **`:<sha>`** (what makes a rollback possible), authenticating with `--password-stdin` against a Gitea repo secret **`REGISTRY_TOKEN`** (`write:package`, `read:package`). 🔥 **CI gates on the request that separates a working image from a perfect-looking dead one**: the last step runs the freshly-pushed tag and curls `/healthz` **and `_framework/blazor.web.js`**, because P51's `--no-restore` image passes every other check ever written here. ⚠️ **A CI file is the obvious place for that trap to return** — `--no-restore` reads like a free speed-up to anyone who has not met the finding — so `ContainerTests` fences the workflow as well: the runner label (⚠️ **a wrong label queues rather than fails**, which reads as a slow build rather than a broken one), the absence of the flag, the image path P53 pulls, and the blazor check. ✅ **Acceptance on the published artifact, not the local build**: pulled `:latest` from the registry, ran it, `/healthz` + `_framework/blazor.web.js` + `/` + a proxied `/` (with `X-Forwarded-Proto`/`-Host`) all **200**, and dealt a browser round — **the same hand as P51's local build at the same seed**, a free reproduction check on the image. ⚠️ **No credential entered the session**: Nick minted the token and stored it as a repo secret, and the local pull used a `docker login` he performed himself. 🔥 **P53 turned out not to be blocked at all** — the `read:package` token already in `inventories/my-ansible-nas/group_vars/nas.yml` as `mirroquest_registry_password` is exactly what `burmesepoker_registry_password` wants; only P52 ever needed the new one. ⚠️ **The GitHub push-mirror was deliberately not set up** (it needs a *GitHub* PAT — a different credential — and nothing depends on it); `origin` is untouched. No rules question; `RULES.md` stays rev 31. `Domain`, `Presentation`, `Server`, `Console` and `Sim` byte-identical. ⚠️ **The first run was red at 913/914**: the new fence matched the workflow's own `#` comment explaining the trap — `Sources.Markup` strips `//`, `/* */`, `@* *@` and `<!-- -->` but not YAML's `#`, so the test strips it itself; re-proved able to fail on a real flag. Tree green at **914** (was 913 — one new fact). |
| 2026-08-28 | P51 | **Done — the browser table as a container, on Opus 5 — and the standard .NET Dockerfile idiom turned out to ship a dead one.** Root `Dockerfile` (multi-stage `sdk:10.0` → `aspnet:10.0`, publishing `BurmesePoker.Web` and its three project references only, non-root, `ASPNETCORE_URLS=http://0.0.0.0:8080`, **231 MB**) and `.dockerignore`; `app.UseForwardedHeaders()` first in `Program.cs` with `KnownIPNetworks`/`KnownProxies` cleared; `MapGet("/healthz")`. 🔥 **The finding, measured not reasoned:** copy csprojs → `dotnet restore` → copy sources → `dotnet publish --no-restore` publishes an app with **no `wwwroot/_framework/blazor.web.js` at all** — the published endpoint manifest names it **zero** times against **one** when the publish restores for itself (reproduced four ways inside the SDK image to isolate the step). `MapStaticAssets` then 404s the script that starts the circuit: **P13.3's failure by a different road**, and invisible from inside the tree because the app is correct and only the image is not. The publish restores for itself and the flag is fenced. **Acceptance met by playing, not loading**: sat down through the lobby's `EditForm` (the antiforgery post — the exact failure class), claimed off the table, discarded, four bot seats took their turns, drew blind with the private event arriving over the circuit. Forwarded headers proved live — with `X-Forwarded-Proto: https` / `X-Forwarded-Host: poker.nickjones.dev` the container logs `https://poker.nickjones.dev/`. ⚠️ **The app trusts whatever forwards those headers** (a proxy container's address is assigned at run time), so port 8080 must never be published straight to the internet — P53's labels and P54's gating sit on that. Fences: `BurmesePoker.Tests/Web/ContainerTests.cs`, five facts, source scans in `JackpotSpokenTests`' idiom — the image's SDK/runtime tags against the csproj's moniker (a `net11.0` bump reddens), `EXPOSE` against `ASPNETCORE_URLS` and the host being `0.0.0.0`, `UseForwardedHeaders` **ordered before** `UseAntiforgery`/`MapStaticAssets`/`MapRazorComponents` (*present* would have passed a useless call after the endpoints), `/healthz` reading no `Lobby`, and no `--no-restore` on the publish. Docs: `README.md` gained a *Hosting it* section, CLAUDE.md, BUILD-PLAN P51 ☑ + **P52 amended** (build the Dockerfile as it stands; acceptance now includes curling `_framework/blazor.web.js` for a 200). No rules question; `RULES.md` stays rev 31. `Domain`, `Presentation`, `Server`, `Console` and `Sim` byte-identical; no measurement can move. Tree green at **913** (was 908 — five new facts). |
| 2026-08-27 | P50 | **Done — the documentation cleanup (F10), on Opus 4.8 — `STRATEGY.md`'s prose caught up with its own tables and the class was fenced.** Pure-documentation packet: the diff is `docs/STRATEGY.md`, one new `[Fact]` in `PublishedFigureTests`, and the count/state bumps in `CLAUDE.md` + `STATUS.md` — every project byte-identical, no measurement moved. 🔥 **The finding: most of the 2026-08-23 F10 list was already current**, because P43–P46's blast radius had regenerated §3/§4/§6/§12; the genuine staleness was concentrated in the top block and the sections those packets never touched. Fixed, each re-derived from the CSV (never patched by eye): **top block** (`rev 24/26/29`, `§7.4/§7.5/§3-seating unbuilt`, `235 measurements`, `Last generated … P46`) → **rev 31, all built (§7.4 measured in §15, §7.5 audited, seating/visibility cannot reach a one-round figure), 372 measurements, P46 suite + P48 readouts**; **§7's resolution floor** (four-handed `SE 0.52 / half-width 1.02 / ~34,000 games`) → **`0.41 / 0.81 / ~21,000`**; **§8's map + `cautious`/`counting` bullets** (four-handed `± 1.0`) → **`± 0.8`**; **§3's greedy/cautious/counting paragraph** (`+0.1 ± 1.0`, `30.7/30.6/30.1`) → **`+0.0 ± 0.8`, `22.4/22.2/22.2`**; **§10's "The answer"** (`+$5.32 / +$14.63 / −$0.21`) → **`+$3.99 / +$14.20 / −$0.35`** matching its own table (also the table's `3.98 → 3.99` rounding, aligning it with §16.3); **§15's deal-win summary** (`28 in 75,250`) → **`52 in 116,201`**. Plus P48's own leftover, assigned to P50 in P48's log: **§3/§4/§8/§14's forward-references to P48 as future work** (`re-read under P48 before settled`, `flagged for P48`) reworded to the settled result now in §16 (§16.5 graduated `sprinter`, §16.1 held the `opportunist` null, `angler`-`opportunist` read inside). 🔥 **Fence choice: extend, not strip.** `PublishedFigureTests.TheProseFiguresTheStrategyDocumentQuotesAreTheFiguresInTheCsv` anchors seven current-claim prose margins (§3 sprinter-over-outs, §8 map's outs/cautious/counting/refuse, §10's two money cells) plus §7's *derived* floor (half-width = the head-to-head interval, SE = it over 1.96) to their CSV rows, in the proven HOW-TO-PLAY-WELL shape — printed sign carried by the scale, tolerance from printed precision. ⚠️ **Digit-free (P49's rule for the sims doc) was rejected on purpose**: `STRATEGY.md` is the measurement authority whose voice *is* inline figures, so stripping its digits would gut it; the fence keeps the voice and reddens a stale *current-claim* margin (never the deliberately-kept historical ones — P34's newest-first rule). **Proved able to fail** (a `+2.7 → +3.5` prose mutation reddened the new fence; reverted). ⚠️ **Left alone deliberately**: clearly-historical narratives naming their packet (§9's P23 four-handed re-fit, §10/§14's "at P33 it went X→Y", the P16 neighbour figures); and one possible P48 typo out of F10 scope (§16.3's *normal* money half-widths read `±2.28`/`±4.85` where the CSV says `±2.43`/`±4.81` — the *resampled* figures are correct). README/PLAYING/RULES-PRIMER swept clean (already pointer-only / rev 31). No rules question; `RULES.md` stays rev 31. **Every planned packet is now done**; only `P40` (blocked on Nick's Burmese text) remains. Tree green at **908** (was 907 — one new `[Fact]`). |
| 2026-08-27 | P49 | **Done — `docs/SIMULATIONS.md`, the measurement programme taught to a curious person, on Opus 4.8.** A pure-documentation packet: no production code touched, every project byte-identical. The document, in reading order: what a seeded/parallel/replayable **run** is; a **seed** (a pointer) against a **journal** (the artifact — §3.9 in plain words); why the **game is the trial** (counting the seat halves the interval); why a **win rate is the totals divided**, not the average of per-game rates; what **pairing** buys and why it *widens* within a cell (one seat declares → opposed → √2) and *narrows* across cells (shared shoes → common random numbers); why a round-robin needs **Holm** and what *survives* vs *raw only* buys; what the **null cell** proves; and a **tour of the experiment shapes** (head-to-head, crossed free-for-all, neighbours + control arm, money sweep, dial calibration) with the question each answers and the one it cannot. 🔥 **Digit-free by rule** (P39's one-home applied in advance): it holds no figure and points at `STRATEGY.md`/`HOW-TO-PLAY-WELL.md` for every number, so it cannot go stale the way F10's prose did. **Fence** `PublishedFigureTests.TheSimulationsGuideTeachesTheInstrumentAndCarriesNoFigureOfItsOwn` asserts no `±`, no percentage figure (`\d\s*%`), and both pointers present — **proved able to fail** by mutating in a `±` margin (caught) and a bare `0%` (caught), both reverted. Commands reused verbatim from CLAUDE.md so the existing command fence resolves them; lands in the map with no banner; `README.md` gained the pointer. No rules question; `RULES.md` stays rev 31. Tree green at **907** (was 906 — one new `[Fact]`). |
| 2026-08-27 | P48 | **Done — the verification-and-hardening run, on Opus 4.8 — the statistical review's findings F1–F7 discharged, and `sprinter`'s `+1.2` graduated from *measured* to *settled* at a fresh seed.** Not a rung packet: no agent changed, `BotCatalog` untouched, the whole diff is `BurmesePoker.Sim` + tests + two docs, so `Domain`/`Presentation`/`Console`/`Web` are byte-identical (front ends cannot see it). **F1** composition margins (`ladder.composition.{pair}.{1,4}-of-5`, `TournamentCell.MarginAtComposition`): `warden`'s loss is partly self-play compounding — `outs` beats a lone `warden` +6.3 but a table of four +10.6, `greedy`/`simple` the same way; **P43's `opportunist` null holds at every composition** (both extremes inside the interval — the instrument P43 asked for). ⚠️ The games partition by composition but the margin does not (a difference of ratios-of-sums over different seat-round denominators), verified on the degenerate `random`-`outs` cell — read the strata against each other, not the pool. **F2** money margins (`ladder.money-margin.*`, own Holm family of 45): the money ladder agrees in direction with the win-rate ladder **everywhere (0 of 45 disagree**; 38 separated, 5 inside, 2 raw only), and `sprinter` banks **+$0.45 ± 0.32/round over `outs`** — a second currency confirming the edge. **F5** the fresh-seed replication (`sim replicate` → `docs/strategy/replication.csv`; the *efficient* path, Nick's call, reads seed A from `measurements.csv` and computes only seed B, with a pre-flight id check after a dial-ordering bug cost the first ~4 h run): **written prediction — every Holm verdict holds ✅ (0 of 48 fell); every margin inside its interval ❌ (3 of 48 outside, all keeping their verdict, and ~8 were *expected* outside by the interval geometry, so the estimates are more stable than the prediction).** 🔥 **`sprinter` over `outs`: +1.23 (Holm) → +1.19 (Holm)** — the one verdict most likely to fall did not; the two raw-only casualties split (`opportunist`-`angler` fell inside → null, `sprinter`-`angler` firmed to Holm). **F6** bootstrap (`Bootstrap.PairedMargin`, `money.net-per-round-bootstrap.*`, deterministic, resamples whole games): the normal intervals on the two separated money cells **hold at money's heavy tails**. **F7** field-rate intervals (per-game `FieldSeries`): the rates §12/§13 compare across fields now carry a standard error — the ladder's lock-bite **30.0% ± 0.2 is separated from the dial's 21.9% ± 0.6**. 🔥 **Reproduction is the strongest recorded: 214 of 235 shared `measurements.csv` rows byte-identical and not one *mean* moved** — the 21 movers are exactly the field-rate rows that gained an interval (F7). Verification (item 1): `dotnet test` green including the conformance audit; journal replay byte-identical; `Domain` diff empty; a `drive-console` bots capture ran clean. ⚠️ **The Chrome browser round was not performed in this environment**; `Web`/`Domain`/`Presentation` are untouched (zero diff), so no browser behaviour changed. ⚠️ The wall-clock budget test (`HandEvaluatorTests`/`PartialCoverTests` …`IsFast`) flaked once under contention (the documented non-defect failure mode) and passed clean on re-run. `STRATEGY.md` gained §16 (§16.1–§16.5). ⚠️ Compute: suite ~5 h + replication ~4 h wall on this 24-core box — budget a full day. **P50 owns rewording `STRATEGY.md` §3's "re-read under P48 before settled" and "flagged for P48" notes, now discharged by §16.** BUILD-PLAN P48 ☑ + P49/P50 re-plan. Tree green at **906** (was 897). |
| 2026-08-25 | P46 | **Done — `sprinter`, the endgame played as a race, on Opus 4.8 — the first rung to separate above `outs` since `outs` was built, and the packet where the mechanism variable finally armed.** `Domain/Agents/SprinterBotAgent.cs` is `outs` with one change: the discard's last-resort key. Within one card of covering it stops maximising live outs and maximises **winning draws** (`LiveOuts.WinningDraws` — copies-weighted values that let 13 of the 14 it leaves meld, bar = the hand's own size); the key is lexicographic *winning draws then outs* packed in one `long`, so off the endgame it is `outs` card for card (trigger is existence, not a tuned threshold — P45's idiom). Public `Endgame.WithinOneCardOfCovering` is what the Sim reads. Take/claim/object/declare are `outs`'. Catalog last, `Strength: 3`, win-rate ranked, `Hardest` stays `outs`. **Bench 8.1× `greedy` / 1.21× `outs`**, no turn inflation. **Measured (prediction was modest-positive-or-null): `+1.2 ± 0.8` against `outs`, `p = 2.9e-03`, surviving Holm over the new family of 45** — confirmed on a second command (bare `tournament outs,sprinter`) and reproducing exactly; beats `opportunist` (+1.6, Holm) and `angler` (+1.1, raw only); tops both ranking columns (mean +8.1, crossed 25.5). ⚠️ **Small and fragile** — crossed table 25.5 vs `outs`' 25.3 is a dead heat (overlapping intervals), a one-point margin is the tightest the doc asks Holm to hold, so **P48's fresh-seed replication graduates it from measured to settled**. 🔥 **The mechanism armed** after three flat nulls (`opportunist` lock-bite, `purist` clean share, `angler` take rate): race-reach **26.6 ± 0.2% vs `outs`' 25.6 ± 0.2%** (~4 SE, new `ladder.race-reach.{rung}` rows) — it steers into more near-wins and the win rate follows; a moved mechanism + a moved margin together is why the +1.2 is believed. **Crossing cap paid, stated first**: `SeatingPlan.MaximumAssignments` 65,536 → 131,072 for the `10⁵ = 100,000` free-for-all (raise and pay, over dropping a rung or subsampling). ✅ **Reproduction held**: 9 old rungs' head-to-head cells byte-identical, the 38 moved rows all field-dependent, no Holm verdict fell, null cell → `sprinter` (4th time, holds −0.4). Two raw-only casualties at the top (`angler`-`sprinter` p=0.0079, `opportunist`-`angler` p=0.013), flagged for P48. One game of 100,000 abandoned (`random`'s field, P29). Suite ran **~5 h on this 24-core workstation** (the older ~6¼ h / ~22,500 s is a laptop figure; the P45 laptop slept mid-run). Fences: `SprinterBotAgentTests` (5 — trigger existence, WinningDraws vs the long way, off-endgame equals `outs`, everything-but-discard equals `outs`, and the divergence invariant proved non-vacuous), both hand-written ladder lists, `SeatingPlanTests` re-proved at the new cap. Docs: `STRATEGY.md` §1–§4/§6/§8/§11–§15, `HOW-TO-PLAY-WELL.md` (a cautious endgame paragraph), CLAUDE.md, BUILD-PLAN P46 ☑ + P48 re-plan; §12/§13 blast-radius F10 corrections; **the rest of F10 stays P50's**. ⚠️ **Owned follow-up**: the race-reach instrument recomputes an uncached cover search per crossed-table discard (measured cheap, ~54 µs/call, wasteful not pathological — a first suite run was wrongly killed on that hypothesis mid-session); a cache-sharing pass is queued and does not change the measurement. Tree green at **897** (was 886). |
| 2026-08-25 | P45 | **Done — `angler`, a draw priced in cards, on Fable 5 (the plan's own recommendation) — the predicted null arrived, and its mechanism came back flat, which is the finding.** `Domain/Agents/AnglerBotAgent.cs` is `outs` with the take changed at both places it arises (take and claim, `prospector`'s rule): acquire a known card iff `gain·unseen + outsAfter > 2·outsNow` — the plan's *live outs over cards unseen* as integer arithmetic over public facts, one stated model (a one-draw horizon), the numerator `LiveOuts.CardCount` (copies-weighted outs sharing `Count`'s loop, probes and cache), the denominator `MoneyOdds`' unseen pool. The new move is the **enrichment take**: a card that melds nothing, taken when it more than doubles the hand's out-cards. Catalog last, `Strength: 3`, win-rate ranked, `Hardest` stays `outs`. **Bench 9.3× `greedy` / 1.21× `outs`** — inside P21's budget after two stated cuts (lookahead skips the refinement's probes; enrichment requires the offered card meldable with the hand, which also kills a shed-a-blocked-duplicate artifact); an all-`angler` table runs 24.9 turns, same as `outs`. **Measured (prediction first): `+0.6 ± 0.8` against `outs` — inside the interval — beating the `greedy` trio `+2.6`–`+2.9` and `warden` `+6.9`, all separated over the new family of 36; take rate 24.66 ± 0.09% against `outs`' 24.71 ± 0.09%, so the enrichment take almost never arms** — the prediction expected 1–5% of acquisitions and was wrong, and the flat rate is the finding: under a one-draw horizon `outs`' improvement-only take already collects everything a card-priced model sees. Third zero/stated-price null in three packets. ⚠️ `angler` over `opportunist` `+1.0 ± 0.8` raw-only (`p = 0.013` vs Holm 0.0083) — the family's first raw-only casualty, flagged for P48, not claimed. **The crossing cap was paid measured**: `SeatingPlan.MaximumAssignments` 32,768 → 65,536, free-for-all one full pass of `9⁵ = 59,049`, nothing subsampled; ⚠️ a tenth win-rate rung is `10⁵ = 100,000` and breaks it again (P46's). **Mechanism instrument added**: `ladder.take-rate.{rung}` rows for every rung at the crossed table. ✅ **Reproduction repeated P43's exactly: 148/172 shared rows byte-identical outside the command column, all 24 movers field-dependent by construction, no verdict moved**; null cell changed hands to `angler` (third time, deliberate) and holds at −0.5; `LiveOuts` refactor proved byte-identical separately (300 seeded games, HEAD worktree vs tree). New §6 curiosity published: the across-cells pairing row `warden`: vs `opportunist` less vs `angler` reads **0.37** — nearly-identical comparators make the difference almost pure shared-shoe noise. Suite **~22,500 s (~6¼ h), estimated from CPU accounting — the laptop slept mid-run, so the 95,870 s wall clock is not the cost**. One game of 59,049 abandoned (`random`'s field, precedent P29). Fences: `AnglerBotAgentTests` (6 — the enrichment take asserted against `outs`' draw, the inert decline, the improving take, discard outs-card-for-card, the claim's same toll, `CardCount` against the long way), both hand-written ladder lists, `SeatingPlanTests` re-proved at the new cap. Docs: `STRATEGY.md` §1–§4/§6/§8/§11–§15, `HOW-TO-PLAY-WELL.md`'s clean rate 12.1 → 12.2, CLAUDE.md, BUILD-PLAN P45 ☑ + P46/P48 re-plan; §12/§13 blast-radius F10 corrections; **the rest of F10 stays P50's**. Tree green at **886** (was 874). |
| 2026-08-24 | P44 | **Done — `purist`, the clean bonus played for at zero price, on Fable 5 (the plan's own recommendation) — and the predicted positive came back a null whose mechanism never fired, which is the finding.** `Domain/Agents/PuristBotAgent.cs` is `outs` with one change: a *fewest-jokers-kept* key **between** `outs`' two ranking keys, so a joker is shed whenever that costs no melded card — the exchange rate stated lexicographically rather than tuned (never a meld, any number of live outs; a numeric rate would need a win-probability estimate nothing supplies). Catalog entry last, `Strength: 3`, `Ranked = RankedOn.Money` **for the second reason there is** — it reads no stakes (the run proves it: take rate and win margin byte-identical at all four ratios) but trades rounds for a multiplied prize, so win rate would misjudge it by construction; `Hardest` stays `outs`, no dial/ε/front-end change, and the ladder stays at eight win-rate rungs so the crossing cap is untouched and still P45's. **Measured (prediction written first, both halves wrong): at $5/$1 the sweep reads `−$0.23 ± 0.32` against a same-seed control cell of `−$0.228 ± 0.32` (`prospector` at $5/$1 *is* `outs`), so `purist`'s real effect is −$0.005 a round, −0.01 points — one round in eight thousand — and its clean-win share is 12.81 ± 1.04% against the control's 12.83% and the field's 12.22% floor.** Why: when the joker-throw is the unique winning discard, `outs` already throws it, so **the accidental floor already contains every clean win that costs nothing**; every remaining clean win costs melds — `warden`'s ruinous currency — so a *paying* clean-bonus rung is the second standing anti-recommendation (P47), with re-measure-if-#33-flips recorded. **The instrument generalised**: one sweep per money-ranked rung (`SuiteOptions.MoneyChallengers`, off the catalog — a single `StakesSensitive[^1]` default would have silently dropped `prospector`), the challenger joined every `money.*` id (P32's precedent; the twelve renamed rows byte-identical on values), per-cell `money.clean-win-rate.*`/`money.jokerless-rate.*` mechanism rows for both challengers, bare `sim money` sweeps each rung in turn. ✅ **Reproduction exact: 131/131 unrenamed shared rows byte-identical, no verdict moved, the new sweep its own Holm family of four (all inside).** Suite **18,600 s (5¼ h)**. Aside published in §14: `prospector`'s clean-win share collapses to ~5.6% where its rule fires — the first observed interaction between the draw decision and the clean bonus. Fences: `PuristBotAgentTests` (6, three proved able to fail by mutating the joker key), both hand-written ladder lists, `StandingAnswerTests`' challengers assertion, `SuiteTests`' id set, `PublishedFigureTests` re-anchored to the renamed ids. Docs: `STRATEGY.md` head/§2/§8/§10/§11/§14, `HOW-TO-PLAY-WELL.md`'s bonuses section rewritten (the jokerless bonus is priced now — its free half is worthless), CLAUDE.md, BUILD-PLAN P44 ☑ + P47/P48 re-plan; two more F10-stale prose figures corrected in the blast radius. ⚠️ One plan correction recorded: constraint (3) overstated §9 #33's built default — the engine restricts a joker's exit only while jokers are *locked*. Tree green at **874** (was 862). |
| 2026-08-23 | P43 | **Done — `opportunist`, the feeding ban at zero price, on Fable 5 (the plan recommended Opus 5; the session records what shipped it), and the predicted null arrived and is the finding.** `Domain/Agents/OpportunistBotAgent.cs` is `outs`' take exactly plus `warden`'s hold — the missing 2×2 corner (take-for-denial no, hold yes) — with the hold extracted to `Domain/Agents/HeldLocks.cs` and `warden` delegating to it, so the restraint has one home (P28's one-predicate rule applied to a strategy). Catalog entry after `warden`, `Strength: 3`, win-rate ranked; `Hardest` stays `outs`, no dial or front-end change. **Measured: `+0.1 ± 0.8` against `outs` (inside the interval) while beating `warden` `+6.2 ± 0.8` and the `greedy` trio `+2.0`–`+2.2`, all separated over the family of twenty-eight** — so `warden`'s whole loss lives in the paid take, and denial at the best possible price, zero, buys nothing this apparatus can see; the prediction (null to small positive) was written first (P20's discipline). ✅ **Reproduction repeated P31's exactly**: every shared cell byte-identical but for the `command` column, all twenty-one old verdicts survived the tightening to twenty-eight, the null cell changed hands to `opportunist` (`Ladder[^1]`, second time) and holds. ⚠️ **The suite is ~15,200 s now (~4¼ h)**: seven new `outs`-priced cells plus the free-for-all doubling to **8⁵ = 32,768 — exactly `SeatingPlan.MaximumAssignments`, so the ninth win-rate rung will not fit and P45/P46 must decide what gives** (noted in §4, both packets, and *What is next*). `STRATEGY.md` §1–§4, §6, §8 (a sixth kind of answer: a null that closes a question), §11–§15 updated from the CSV; four F10-stale prose figures in the blast radius corrected (`warden` −9.3 → −7.3 among them), the rest left for P50 with a note. Fences: `OpportunistBotAgentTests` (6 — the zero-price take asserted against `warden`'s, the hold, both escapes, the counterfactual instrument, outs-card-for-card when idle), `BotCatalogTests`' written-out ladder grew to nine, and the discovered-count fence caught STATUS's 850 exactly as designed. ✅ **No rules question arose; `RULES.md` stays rev 31.** 🔥 **Green at 862 / 0**, from 850. |
| 2026-08-23 | P42 | **Playtest readiness — done, the day it was written up, on Fable 5.** **(1) The console deals five**: the seat prompt's default is `RoundEngine.DefaultPlayers` with the floor untouched; both `drive-console.py` scripts re-captured clean at five seats (⚠️ the driver's default script *is* `human` — the two scripts are `bots` and the default). **(2) The ×5 is said out loud**: `RoundResult.JackpotOwner` (`PlayerId?`, **required not defaulted** — `Win`'s lesson) is filled in `RoundEngine.Settle` from the same `ConfigurationOf` the settlement reads, because **a watcher cannot compute it**; `MoneyCardRegistry.IsTheJackpotPair` is public static for the half that *is* public from the deal, and `TableBoard.JackpotPairUp` folds it **at `RoundStarted`, not off the live turn-up list** (a claimed top card leaves that list while its designation stands). Console settlement line + round-start narration; web `SettlementPanel` sentence from the result alone + a quiet table-centre note. `CardDisplayState` stays ×5-free; **§9 #32 not generalised, both fences untouched**. Fenced by `RoundEngineTests.AJackpotRoundCarriesItsOwnerOnTheResult` (constructed round: pair turned up, one seat given both partners **and all four jokers** so the hand-computed payouts are clean; split-pair twin asserts null) and `JackpotSpokenTests` (source scan: each front end must *read* the fact — deliberately not a wording fence), **both proved able to fail by mutation**. **(3) Byte-identity, P41's exact procedure**: seeded 300-game `Sim` (`--seed 20260823`) journal + CSV **byte-identical** either side; HEAD journal replays to a byte-identical CSV. **(4) The browser round was actually played** — Claude in Chrome, the real extension, `--people 1 --seed 20260823 --pace 300`: sat down, **four settlements** clicked through; claim **refused twice and granted once** (§4.5's holder-only disclosure seen working); §5.1 closed rank 3 and the card stopped being a control; **§9 #50 live** (concealed duplicate of a face-up 3♦ thrown, the ▲ stayed); P41's chips and piles from both sides; why?, legend, log, timeout stand-in. ⚠️ **UX observation, not a defect**: at pace 300 the settlement panel's on-screen window is ~one pace beat before the next deal replaces it — fenced by `BrowserRoundTests`, but a human playtester may want the round to linger. Green at **850 / 0**, from 845 — 5 new tests. |
| 2026-08-23 | P41 | **The table shows what the rules make public — done, the day it was written up.** One fold, written once: `Presentation/TableLook.cs` holds every seat's pile (§5) and every seat's face-up cards (§5.2), and the console's observer, the server's fan-out and the browser's board each hold one — the browser's pile fold moved *into* it, so pile logic has one home too. The blind draw has **no method on the type**; `ConcealmentTests.ABlindDrawnCardIsNeverShownFaceUp` holds the fan-out to it, **proved able to fail by mutating `PlayerDrew`**, with a positive twin that plays a real open take through a connection. §9 **#49** and **#50** built on their recommended defaults, each fenced in `TableLookTests` by a test named for the row, both mutation-proved; **the mark is by `CardId` throughout**, so the concealed duplicate stays concealed. Browser: face-up chips flat on each seat panel (`▲`), the whole pile behind the ▾ `<details>`, your own `▲` in your hand; console: a fourth panel *Everyone can see this*, `▲` in the hand and legend; `drive-console.py` re-captured clean (capture differs — presentation). ⚠️ **`TurnContext` deliberately not widened** — no rung sees any of it. ✅ **Byte-identity asserted**: seeded 300-game `Sim` journal + CSV **byte-identical** either side, HEAD journal replays; `git diff` empty on Domain and Sim. **§10 #24 discharged, §10 empty again (#7 standing); registry §5.2 → `Checked` (no ⚠ — both defaults are presentation-only), ceiling 7 → 6, no whole exemptions at all.** Green at **845 / 0**, from 832 — 13 new tests. |
| 2026-08-23 | rev 31 | **Two visibility rules recorded from Nick, and the whole cascade taken in one session.** **(1)** §5's public piles corroborated `PLAYER` (every card in every pile, rev 17's rule) — still exercisable in no front end. **(2)** §5.2 new: a card taken in the open lies **face up in front of its taker until that copy leaves their hand** — Settled, amends §6.3, and **changes what the table shows rather than what it knows** (open takes and discards are public events by `CardId`; the face-up set is a fold; journals replay identically; no measurement should move — with the one instance-level exception §5.2's notes record: the event log cannot pin *which copy* of a duplicate is still held, and the face-up card can). Opens **#49** (claimed turn-up face up too? recommend yes) and **#50** (which copy leaves? recommend the thrower chooses — what discard-by-`CardId` already does); `QUESTIONS-FOR-MYA-LAY.md` Q13. **§10 reopens with #24** (fourth reopening); registry gains §5.2 as a whole exemption naming **P41**, ceiling 6 → 7 with the finding recorded. `JournalHeader.CurrentRulesRevision` 30 → 31; **the rulebook moved in the same session** (stamp 31, the face-up rule taught, appendix rows 13–14) — the first time the rev fence's compelled maintenance was paid the same hour it was incurred. ⚠️ **P40 must translate the rev-31 rulebook.** 🔥 **P41 written up and next.** Green at **832 / 0** — no test count moved: the revision is rules text, one constant, and registry data. |
| 2026-08-23 | P39 | **Done — the strategy guide, on Fable 5: `docs/HOW-TO-PLAY-WELL.md` answers *how do I get better?* from what was measured, and every figure it quotes is fenced.** Organised by decision rather than by experiment — the whole game in one sentence, the discard tie-break (the headline pair), the one refinement that has ever worked (`outs`, +2.7 ± 0.8), the money (settle it, never chase it — the $5/$1 cell is a null and the crossover is a money card worth four rounds), **the nulls given as much room as the margins** (refusing the claim, counting cards, feeding-aware discards, `warden`'s loss, the seat-side question stated with no digits at all because P16's figures are not CSV rows), the three **unpriced** bonuses stated as unknown-rather-than-small, and the difficulty dial with its reference table. 🔥 **The fence moved home with the figures and now fences verdicts too**: `TheFiguresThePlayersGuideQuotesAreTheFiguresInTheCsv` reads the guide — dial quad, headline pair, nine anchored margins with **the printed sign in the anchor and the scale carrying it**, two interval-free rates, and the eight verdicts the prose asserts (nulls must stay `inside the interval`, separations `separated (Holm)`). 🔥 **A figure has one home, asserted as an absence**: `TheFiguresHaveOneHomeAndThePlayingGuidePointsAtIt` requires `PLAYING.md` to point at the guide and contain no `±`, no reference-table quad, no headline pair — its *Playing better* section is one pointer paragraph. Both fences proved able to fail by mutation. ⚠️ **Found on the way**: `PLAYING.md`'s difficulty prompt row still quoted the four-handed 36%/14%, unfenced because P34's regex targeted the other sentence — digit-free with a pointer now. `README.md` and the documentation map carry the guide. ✅ **No rules question; `RULES.md` stays rev 30.** 🔥 **Green at 832 / 0**, from 831. |
| 2026-08-23 | P38 | **Done — the rulebook, on Fable 5: `docs/RULEBOOK.md` teaches the game front to back and cannot fall behind the rules without a red build.** The whole game in reading order for somebody who has never seen it — setup, the turn, the feeding rule taught as table manners, the opening claim and its permission, melds, the win condition by table size, the three settlement bonuses, the money layer, a one-page table reference — **no provenance tags, no open questions, no packet numbers**, and `RULES.md` stays the sole authority. 🔥 **Four tests in `Tests/Docs/RulebookTests.cs`, each proved able to fail by mutating the document.** **(1) The rev stamp** equals `JournalHeader.CurrentRulesRevision`, which `GameJournalTests` already binds to `RULES.md`'s own header — so a play-changing revision is a red build until the rulebook is re-read, **which is the maintenance, compelled**. **(2) The worked round is replayed, not proofread**: `TheWorkedRoundIsTheRoundItsSeedActuallyPlays` re-runs the printed construction (seed **15**, five seats of `outs`, seat seed `seed × 100 + seat`) and asserts all five dealt hands, the turn-up, all seven owned money cards with multipliers, the 24 turns, the winner, the declared melds and all fifteen settlement cells with the round/money/net split asked of `Settlement.RoundPayments`. Seed 15 came from a 60-seed scan and earns its place twice: the winner declares **jokerless**, so the ×3 bonus appears in a real settlement ($60 against $15 a head), and **had discarded an owned A♠ mid-round and is still paid for it** — permanent ownership demonstrated by the engine. **(3) The house readings are fenced two ways with the citation set derived from `RULES.md` itself**: open §9 rows recognised by table shape (numbered, un-struck, five columns), and the appendix must cite exactly that set — #33, #36–#41, #44–#48 today. A question closing or opening moves the set and fails the build, which is the packet's hard problem — a rulebook silently promotes defaults to rules — made checkable. **(4) The voice is fenced**: no provenance tags, no confidence words, no packet ids, not the word *reconstruction*. `README.md` points at it as the way in; the documentation map carries it. ✅ **No rules question arose; `RULES.md` stays rev 30.** 🔥 **Green at 831 / 0**, from 827. |
| 2026-08-23 | P34 | **Done — a front door, and a documentation set that cannot go stale quietly, on Opus 5. Every packet in `BUILD-PLAN.md` §5 is now done.** `README.md` is the only current-only document in the repository — what the game is, the seven projects, how to run it, where the answers live, **no packet numbers and no history** — and the three wholly historical documents (`RULES-TECHNICAL.md`, `REVIEW-2026-08.md`, `RECONCILIATION-PLAN.md`) carry a banner in their first three lines saying what they were for and what replaced them. 🔥 **The habit is a test now, for the third time in this project's life** (a rung cannot be added without being measured, P18→P20→P23; a Settled rule cannot be recorded without being checked, P30.2): **eight tests in `BurmesePoker.Tests/Docs/`**, each **proved able to fail by mutating the document rather than the code** — the map complete both ways, banners asserted **both** ways (a current document flagged historical is the same lie the other way round), every command in a fenced `bash` block resolved against the source that parses it, every test `RULES.md` names as a fence existing, every figure `STRATEGY.md` tabulates and every figure `PLAYING.md` quotes agreeing with `measurements.csv`, and the product's one spoken measurement still a null. 🔥 **The test count is discovered rather than trusted** — `[Fact]`s plus theory rows by reflection, which is the number a run reports — **so a packet that adds a test and leaves the prose alone is a red build**. ⚠️ **Only the *first* count and *first* rev in each document are checked**: these files are newest-first and the log records 677, 697, 715 and 795, every one true when written — **a check demanding they all agree would ask the project to delete its own history**. ⚠️ **The staleness that was actually there was in the two documents written for people, and nothing else in the tree depends on either.** `PLAYING.md` told a player the four settings win **13.8 / 21.7 / 28.4 / 36.1%** — the **four-handed** reference table on a five-handed page — quoted a `headline.balanced.*` pair matching no row in either CSV, and said *"your neighbours change every round"*, false since P36; `RULES-PRIMER.md` carried **four `[⚠ code disagrees]` tags for divergences closed at P25–P28**, a settlement section that stopped at *flat*, and an open question answered a revision later. **Prose has no column to disagree with**, which is why both were fixed *and* fenced. ⚠️ **One documentation-accuracy finding in `RULES.md`**: §10 says *empty* and has a standing exception — **#7**, `RoundEngine.MinimumPlayers` is 4 against §2's Settled 2-to-6 — now said out loud above the list. **No rule changed, no play changed, `RULES.md` stays rev 30** and `JournalHeader.CurrentRulesRevision` is unmoved. ✅ **No new rules question arose.** 🔥 **Green at 827 / 0**, from 819. |
| 2026-08-23 | P35 | **Done — the two scoring rules that reach outside a round are played, on Opus 5, and `RULES.md` §10 #20 and #21 are discharged. §10 is empty: every rule the document records as Settled is implemented.** 🔥 **§7.4 changed the shape of a round, which nothing had done since P0.** §9 #38's recorded default is *the dealt thirteen alone*, so `RoundEngine.Play` offers the declaration to every seat whose **dealt** hand already covers, in turn order, **before the first take**; a seat may decline (§7.1) and there is no §5.1 exception in play, because nothing has been discarded. ⚠️ **A round can now run no turns at all** — `RoundResult.Turns` is 0 — and **`TurnNumber` 0 is a real value** reaching the journal, the console's turn heading and the server's `TurnBegan`. ⚠️ **It opened §9 #48** (two seats dealt a winning thirteen at once), defaulted to the earlier in turn order. 🔥 **§7.5 was cheap once the division was seen: settlement is *told*, never made to remember.** `MatchEngine.Streak` is **the only state in this project that reaches across rounds and is not money**; `Settlement` still holds no history and takes no match, asserted over its parameter list, and `Win` is the record it is told. ⚠️ **The reading matters and the first implementation had it backwards**: *"pays your whole payout"* means the winner collects **exactly what they would have collected**, out of one pocket — a test caught it, not a reading. 🔥 **The predicted consumer trap was real and the fix was to delete the re-derivation**: `Settlement.RoundPayments` is the round column, computed once in the domain, and the console's panel and `SeatRow.Flat` both read it — **both had assumed every loser pays the same amount**, true from rev 1 until rev 27, and a split at the wrong place posts the difference into the **side-bet** column with the totals still adding up. ⚠️ **`ScriptedPlayerAgent` was silently declining the new question by accident** — its script advances by turn number and starts at 0 — which is why 795 tests stayed green the moment the path went in; it is an explicit `DeclaresOnTheDeal` now, defaulted to no **as a decision**, and exactly two tests really changed behaviour. 🔥 **Conformance gained its first multi-round case**: the audit still watches one round but can be **told** what the rounds before it did, with the count kept by the driver rather than read off `MatchEngine`, and **fails if 120 rounds contain no streak at all**. ✅ **Both registry entries are `Checked` and there are now no whole exemptions at all** — a first since P30.2 — ceiling **7 → 6**. 🔥 **The re-measurement (13,257 s, 126 rows) is the strongest reproduction this project has recorded: 107 of 124 shared rows byte-identical, and the seventeen that moved all count turns or money.** **Nine rounds in 33,008 ended on the deal** (§15, about one in 3,700) and **not one win rate, margin, Holm verdict, ranking, pairing ratio or ε moved by a millionth** — §7.4 changed *when* those rounds ended, not *who won them*. ✅ **Three columns corroborate**: turns fell **502,830 → 502,812**, the feeding-ban denominator fell by the same 18, and two claim attempts disappeared against 7 × 28.6% = 2.0. ❌ **The written prediction that money moving without a win rate would mean a bug was wrong**; ✅ **the column that discriminates is the side bet, and all four `money.side-margin.*` rows are byte-identical** (acceptance 3). ⚠️ **§7.5 is not in the standing set and cannot be** while every experiment plays one round a game — `STRATEGY.md` §11 says so and says what it leaves unknown. ✅ **The console capture is byte-identical to `HEAD`.** ⚠️ **Two stale product sentences and two stale documents fixed on the way past**, including a **four-handed** figure left in §11 by P32. **`RULES.md` rev 30**, `JournalHeader.CurrentRulesRevision` **30**. 🔥 **Green at 819 / 0**, from 795. |
| 2026-08-22 | P37 | **Done — the table can agree to change seats, on Opus 5, and `RULES.md` §10 #23 is discharged. §10 is empty again: every rule the document records as Settled is implemented.** `IPlayerAgent.AskAboutTheSeating` is a **sixth question** and the first that is not about cards: every seat is asked in the gap before a round, and the seats move on **one `Ask` and no `Refuse`**. It is asked in `MatchEngine.NextSeating`, beside P36's policy — **the agreement first, and the policy not asked on top of it**. 🔥 **The finding is that consent is not desire.** The design decision going in was *a computer seat consents* (BUILD-PLAN **§3.13**, recorded there rather than invented in `RULES.md`) — but a consenting bot answering *yes* would re-seat an all-bot table every deal, which is the opposite of the rule. **`SeatingOpinion` is three answers**, `Consent` the default and a no-op, and the packet's *fail closed* build item then disappeared as a problem: silence, an unattended seat and every bot in the game all consent, and consent moves nothing. **No clock, no timeout, no special case.** ⚠️ **A public question is a standing answer, not a pending prompt** — blocking would have cost one patience per seat to settle one question, so it stands on the seat's `SeatChannel` and the engine **consumes** it: one press moves the seats once. 🔥 **The trap the packet did not name is the one that would have shipped quietly**: this is the first member of `IPlayerAgent` with a default implementation, so a decorator that does not override it answers *consent* in its own name and silently drops what it wraps — a re-seating that never reaches the journal, or a replay that deals to different seats. **Six decorators needed it**, found **by type** rather than by list. ✅ **Replay was free**, which is why the asking is an agent question and not a host call: `JournalingAgent` records it at turn 0, `JournalPlayerAgent` answers it, and `GameRunner.Replay` needed no new path; ⚠️ **it peeks rather than consuming**, because absence has to mean consent or no pre-P37 journal replays. ✅ **Concealment asserted rather than passed over**: three seating events carrying no card, hand or rationale, a watcher who holds no seat hearing every word, and a superseded connection unable to say anything in somebody else's name (R8). ✅ **§9 #47 built on its recorded default — unanimous — and fenced by a test named for it.** ✅ **Two leftovers taken**: `AboutTable` says what the seats are doing, and the console's round-start line stopped claiming *"the seats are re-drawn every round"*, which **P36 had left false for a day**. ⚠️ **The console capture changed and the driver did not**; it is only visible in a two-round capture. ✅ **No published measurement can move** — one round a game, and the question is never put before the first. **`RULES.md` stays rev 29. 795 passed, 0 failed.** |
| 2026-08-22 | P36 | **Done — a seating is drawn once and held, on Opus 5, and `RULES.md` §10 #22 is discharged.** The engine had contradicted the rules document in **both directions**: before P28 it held a seating that could never change, and between P28 and P36 it re-drew one before every deal (rev 19's reading, which rev 28 withdrew on the expert's own words). Neither is the rule, which is that a seating **holds until the players agree to change it**. 🔥 **`Domain/Play/SeatingPolicy.cs` is *when a re-draw happens* and it is one condition in one place**: `Held` is the default, *N* rounds between seatings re-draws every *N*, **0 is never**, and there is no flag beside the number. `MatchEngine` takes one, asks it in one place, and exposes it read-only — deliberately, because a policy that could be talked to would have answered §9 #45 by accident. ⚠️ **The setting is the mechanism and it is not the rule**: a number chosen when a table opens is not people agreeing, which is P37, and two tests named for §9 #45 and #47 fence it. 🔥 **Acceptance 2 is a source scan in the P18/P19 idiom — only `MatchEngine` may ask the question and only `SeatingPolicy` may do arithmetic on the number — and it caught a real second copy**: `JournalFormat` was deciding what 0 meant in order to omit the field. ✅ **The journal writes `seating_rounds` only when the seating moved, so every journal ever written is byte-identical and absence means the rule**; `GameRunner.Replay` reads the header's policy rather than this build's default. ⚠️ **The one journal that cannot say what it did is one from between P28 and P36** — no field, reads back as held, replays differently — which is what `CurrentRulesRevision` 28 is for. 🔥 **A seed stopped meaning what it meant for the second time in this project's life** (§3.9 point 2): a held seating takes **no** numbers out of the match's generator, and `SeatBoardTests`' three-round fixture went red because the claim's permission stopped turning up. It turns up at **five** rounds; nothing was tuned to pass, and the assertion was doing its job. ✅ **No published measurement moved and it is asserted rather than argued** (`AOneRoundGameIsTheSameGameUnderEveryPolicy`). ✅ **Both front ends offer the setting out of the domain's one list** — a console `SelectionPrompt`, a lobby `<select>` and `--seating` — and **the browser stops rearranging itself around a fixed viewer every deal**, checked rather than assumed. ⚠️ **The console capture changed by exactly the new prompt and one sentence**, 16 lines, everything from the deal on identical; `drive-console.py` needed no change. **`RULES.md` stays rev 29 — no rule changed. 778 passed, 0 failed.** |
| 2026-08-22 | P24.2 | **Done — the computer's reasoning, said out loud, on Opus 5. The arrow was a promise of a sentence that was not there, and Nick reported it as a bug from the browser the same day.** `Domain/Agents/IExplainsDiscards.cs` is written as the **described sibling** of P31's `IRanksDiscards` — the packet's own re-plan said what was missing was *the keys, not the ranking*, and that was exactly right. `CoverScore.Scored` returns the candidates with the three keys the sort computes and discards a line later, and `CoverScore.Ranking` is now **defined as its projection**, which is the same discipline `Discard` already keeps against `Ranking`. 🔥 **The trap the interface exists to close: the keys are packed for sorting, not for reading.** `outs` stores its second key as `-LiveOuts.Count(…)` because the sort takes the lowest first, and `CoverScore.Potential` returns `int.MaxValue` for a joker — a front end drawing the raw numbers would say *"−14 outs"* and *"2147483647 partners"*. So a rung supplies each key's **name**, **direction** and **the phrase to print in place of a sentinel** (`DiscardKey.BeyondMeasure`), and `AdviceRationale` never interprets a bare `long`. ⚠️ **One deliberate change with no effect on order**: `ScoredCandidate.Refined` is `long?` and **null where the refinement was never asked** — every candidate that had already lost on cover count. Null and zero sort identically there, so **no published measurement moves**; what the null buys is that nobody can read back a key nobody took. ✅ **Acceptance 2 is an assertion, not a hope**: `ComputerAdvice.RankingsBought` counts what was actually paid for, and the arrow, the sentence and the journal's second opinion are **one ranking between them** — memoised on the *identity* of the `TurnContext`, which is the right key because the engine builds a fresh one per decision and so the memo remembers one decision and forgets it when the next arrives. 🔥 **On screen: all five questions, inside the `<details>Why?</details>` blocks that already existed, and each disclosure now holds two kinds of sentence gated differently** — the rule **ungated** (a rule is not advice, `HandPanel.Words`' distinction) and the computed paragraph **gated on hints**. That is the fiddly half of the packet and it is fiddly in markup; `MarkupStandardsTests.TheComputersReasoningIsGatedOnHintsAndTheRuleBesideItIsNot` fixes it by test rather than by taste. 🔥 **The journal records an *opinion beside an answer*** — `JournalAdvice(CardId, Rung, Why)` on `JournalDecision`, with `DisagreedWithTheComputer` as the query the packet exists for. ⚠️ **That narrows `JournalingAgent`'s stated stance rather than repealing it**: what is recorded is a different agent's answer to the same `TurnContext`, taken before the seat replies, which is a fact about the game and not a guess at the player. **Human seats only**; **by `CardId`** (§3.1 — two decks hold two 5♥, and a value comparison would say *"she agreed"* on precisely the hands worth studying); **recorded with the hints box off**, because a record of disagreement must not depend on whether somebody wanted to be told; replay ignores the field. ⚠️ **Three named traps, all closed by assertion.** The explanation is the **bare rung's at ε = 0 and never a level's** — `FallibleAgent`'s mistake *is* the runner-up of the very ranking this renders. A banned card is explained as a **rule**, and on the turn §5.1's floor yields the sentence **stops saying it**. And no sentence implies the computer plays for §7.3's bonus — the true sentence is *"it will never throw a joker."* ✅ **P32's trap closed**: the closing clause reads the same `TableRules` the evaluator does — *"At 4 one of your melds must be a run…"* against *"At 5 any thirteen that all meld win"* — **asserted at both, because they are different games.** ⚠️ **Two test-fixture findings.** `ScriptedSeat`'s no-hint fallback (*throw the first loose card*) **does not terminate** — it leaves the hand it started from rearranged, and a table of such seats runs until the clock abandons the round; it throws back the card just taken now, which stands still while the bots race. And a hands-off table needs **every** person-seat scripted or the unattended one spends its whole patience per question. ✅ **The console is untouched by design and `drive-console.py` is byte-identical to `HEAD`** at `--seed 20260819 --pick 0`, which is also the proof that reshaping `CoverScore` was a refactor. ✅ **No rules question arose; `RULES.md` stays rev 29** and `JournalHeader.CurrentRulesRevision` is unchanged. 🔥 **Green at 757 / 0**, from 736. |
| 2026-08-22 | P33 | **Done — the clean bonus (`RULES.md` §7.3) is built, `§10 #19` is discharged, and the regeneration produced the cleanest reproduction this project has recorded.** `TableRules.JokerlessMultiplier` puts §7.3's table beside §7.1.1's — **×2 at 2/3/4 seats, ×3 at 5+** — in the one place a per-seat-count rule is written down; `Settlement.IsJokerless` is the predicate (a scan of the declared thirteen, and **`Meld.IsClean` is not consulted anywhere**, because it implements §7.1.1's *required clean series*, a different rule sharing a word); `Settlement.RoundPayment(stakes, rules, jokerless)` is the arithmetic; `Settlement.ForRound` takes the winner's declared thirteen and `RoundEngine` hands it that seat's hand **after** the discard (§7.1). 🔥 **The result: `sim suite --games 8000 --seed 20260819` = 119 measurements in 11,159 s, and 111 of the 116 shared rows came back byte-identical — the five that moved are exactly, and only, the five denominated in dollars a round.** The prediction was written down before the run (no rung reads the bonus, so play cannot move and only money can) and the file shows it with nothing left over. ⚠️ **Contrast P29's 4 of 91**: those packets changed what a winning hand *is*; this changed what winning *pays*. 🔥 **"Does it reproduce" has a third answer — *it reproduces everywhere the change could not reach*.** 🔥 **And the five rows are a finding: the clean bonus is a tax on trading wins for money.** Every one is a `prospector`-over-`outs` `net per round margin` and every one fell — **+5.32 → +4.13** at $5/$20 and **+14.63 → +13.43** at $5/$40 (both still separated under Holm), because the bonus multiplies the round prize and the round prize is what `prospector` sells; it wins 20 points fewer rounds, so `outs` collects the multiplier far more often. ✅ **The `money.side-margin.*` rows did not move at all**, which is the check that the bonus landed in the round column. 🔥 **The defect surface this packet is really about: two consumers re-derive the round/side-bet split in order to display it** — the console's settlement panel and `Sim`'s `SeatRow.Flat`/`SideBet` — and both computed the round half as `RoundValue`, so **the bonus would have landed silently in the side-bet column every money measurement reads**; `Settlement.RoundPayment` is public for exactly that reason. 🔥 **Measured: the bonus is collected in about one round in six** — 15.4% ladder, 16.2% dial, 17.4% two-arm cell — **by rungs that have never heard of the rule**, and it is published as a **floor** for two independent reasons (no rung will part with a joker; §9 #33's default may make it unreachable for a hand that must shed one early). ⚠️ **P32 was deliberately not folded in**, against `BUILD-PLAN`'s own recommendation, on this plan's P31-before-P32 argument: a run changing the scoring rule *and* the table size could not say which moved a number — **and the whole result above is a statement about which rows a change can reach.** It cost a second three-hour suite and the amendment is in `BUILD-PLAN.md` §5 P32. ⚠️ **Six tests changed their expected payouts and none was wrong before** — every hand-computed four-handed settlement in the suite was a jokerless declaration, so all six doubled, which hints the 15.4% floor understates a *player*. ✅ §7.3's registry entry is **`Checked(...)`** rather than `Exempt(...)`, with a mutant catching a clean hand paid flat, a jokered one paid the bonus, **and five seats paid at the four-seat multiplier**. ✅ **No rules question arose; `RULES.md` stays rev 26** — §7.3's *"nothing here is built"* replaced and §10 #19 discharged, status rather than a rule moving, so `JournalHeader.CurrentRulesRevision` is unchanged. ⚠️ **New in `docs/STRATEGY.md`: §14**, and `StandingAnswerTests.TheDocumentSaysHowOftenTheCleanBonusIsActuallyCollected` fails the build if the rate stops being published. 🔥 **Green at 733 / 0**, from 715. |
| 2026-08-22 | — | **Rules session: the expert corrected her own rule, unprompted, hours after giving it — and the correction is two rules.** Rev 25 had recorded §7.3 as a flat **×3** for declaring with *"all series clean"*. Mya Lay came back the same day: *"if you play two players, three players, or four players, you will only get two times of the winning prize … you got three times of the winning prize if we are playing five players. **Not just series, if you want to win with jokerless** … you can discard the joker."* 🔥 **(1) The multiplier is a function of the table size** — ×2 at 2/3/4 seats, ×3 at 5+ — making §7.3 **the second rule in the document whose content changes with the player count, splitting at exactly §7.1.1's seam** (2/3/4 require a series; 5+ require nothing). 🔥 **(2) The condition is *jokerless* over the whole declared thirteen**, not a property of its series — a joker in a **set** forfeits it too. ✅ **Closes §9 #34 and #35.** **#35 was the row with no safe default and the one blocking `P32`**, and it closed the expensive way: the bonus exists at five-plus and pays **most** there, so **cleanliness is relevant at every table size for the first time** and the win condition and the scoring stop being separable. **Both P32 and P33 are unblocked.** ⚠️ **Opens #37** (is six-plus also ×3? recommend yes, matching §7.1.1's grouping — safe). **#33 and #36 stay open, neither blocking**; #36's recommendation is *strengthened* — the new sentence names *the winning prize* twice more and never the money. 🔥 **P33 got smaller, and the reason is the finding**: rev 25 flagged its hardest problem as *which partition the winner is paid on* — **jokerless is a property of the hand, so the question does not arise**; `HandEvaluator` needs nothing and `Meld.IsClean` is **not** the predicate (it implements §7.1.1's *required clean series*, a different rule sharing a word). ⚠️ **The multiplier does need the seat count**, which `Settlement` has never taken; `TableRules.For(n)` is its home. ⚠️ **Rev 25's arithmetic corrected**: four-handed clean is **$30**, not $45, and the *"largest single swing, ahead of the ×5 jackpot"* claim is **withdrawn** — per opponent the bonus is +$5 a head at 2/3/4 and +$10 at 5+ against the jackpot's $10, so it is half the jackpot at small tables and level with it at five (still far more consequential: 1-in-1,444 against routine). ⚠️ **P33 and P32 are now one measurement** — at five seats the bonus is the only thing cleanliness is worth — **so fold P32's seat-count change into P33's regeneration**. 🔥 **Methodological finding: rev 25's own lesson was backwards.** It recommended the **narrow** reading of #34 because the two rules this document got wrong were *flat and later narrowed*; the answer was the **broad** one. What §6.2 and §7.1 share is not breadth — it is that **a broad rule was inferred from a narrow sentence**; §7.3 inferred a narrow rule from a narrow sentence and erred the other way. **Inference from silence is the variable.** ⚠️ **Six sessions running have answered past the question asked, and this one answered a question that was never put — do not treat a rules session as closed on the day it ends.** **rev 26**, `JournalHeader.CurrentRulesRevision` bumped to match. 🔥 **One test went red on the rev alone, and it is the mechanism working**: §7.3's heading claims Settled now, so `SettledRuleCoverageTests.EverySettledRuleIsCheckedOrNamesWhyItCannotBe` failed — registered as an `Exempt(...)` entry, **the only one in that registry excused because *the code is missing* rather than because no ordinary-play check could exist**; ⚠️ **P33 converts it to `Checked(...)` and must not delete it.** 🔥 **Green at 715 / 0, unchanged — this revision is documentation.** |
| 2026-08-22 | — | **Rules session: three answers, and the third one moved the plan.** Asked flat and all three confirmed their standing default — **#19** *does a release survive the reshuffle?* **yes**; **#32** *does the ×5 need the 7♦ and A♠ specifically?* **"specifically"**; **#27** *what does taking a joker close?* **"yeah"**, the other jokers. All three move `PLAYER`/Unknown → `EXPERT`, **no code changed and no play changed**, and §9 was momentarily empty. 🔥 **Then #27's answer carried a rule nobody had ever recorded**: *"Unless you want all series clean that got a 3-time winning game prize, you have a joker, so you discard the joker for the winning clean series."* **`RULES.md` §7.3 is new — an all-clean declaration pays ×3 the winning prize** — against §7.2's *flat*, `PLAYER`, Settled since rev 1. At standard stakes a four-handed win goes $15 → **$45**: **the largest single swing in the game**, and **nothing implements it** (§10 #19, a list P28 had emptied). ⚠️ **It also supplies the only reason this project has ever had for throwing a joker away**, which no rung can currently produce — so every figure in `STRATEGY.md` is measured in a world where that reason does not exist. **Four things unspecified (§9 #33–#36); #35 has no safe default** and decides whether the bonus exists at five-plus seats, **which blocks P32**. All four drafted flat in `QUESTIONS-FOR-MYA-LAY.md` Q10. **rev 25**, `JournalHeader.CurrentRulesRevision` bumped to match (the binding is unconditional; rev 25 is the first non-play-changing rev to move it). New packets: **P33** the clean bonus (blocked on the answers) and **P34** a front door plus anti-staleness tests (blocked on nothing). 🔥 **Green at 715 / 0, unchanged — this revision is documentation.** |
| 2026-08-22 | P31 | **Done — `warden`, the feeding ban played offensively, on Opus 5. It lost, and the packet's own mechanism variable is what makes that a finding.** `Domain/Agents/WardenBotAgent.cs` is `outs` with the **take** changed: it takes a card it does not want when doing so closes that rank against the seat that threw it (RULES.md §5.1), and then **holds** that rank rather than releasing it — one idea, not two, since the release is the taker's to give. Its restraint is §5.1 turned inward and keeps the rule's own two escapes (the declaring discard, and the floor). `TurnContext.ClosedByYou` is new: the half of the ban a seat **arms**, which nothing had ever read — until this packet the strongest rule in the game about *other people's hands* reached every computer player as a rule about its own. `ShoeMemory` lifts `counting`'s memory out whole so the denial estimate is reused rather than rewritten (a null rung's machinery earning its keep). 🔥 **The result: `−9.3 ± 1.0` against `outs`, ~−6 against `greedy`/`cautious`/`counting`, `+2.5` over `simple`; all six survive Holm over a family of 21. The packet predicted a null and got the largest separated loss in the document.** 🔥 **The mechanism variable is what turns that from a shrug into a diagnosis** — `IRanksDiscards.RankDiscards(context, candidates)` is an instrument (never a move) that asks a restricted seat what it *would* have thrown: §5.1 removed a held card from **30.5%** of all discards and **changed the answer on 30.8% of those — 9.4% of every turn**. The rule is one of the most active in the game; the rung is what failed. ⚠️ **The why:** it prices a lock in **melded cards** and pays for it in **draws**, which nothing in its rule prices — an all-`warden` table runs **31.9 turns a round against `outs`' 24.1**. A successor must price the draw. 🔥 **Reproduction: 71 of 88 shared CSV rows byte-identical** — every old head-to-head cell, every pairing ratio, the whole dial, the whole money sweep; the 17 that moved are exactly the rows a seventh rung must move. 🔥 **Unplanned finding: "the ladder's last entry is its strongest" was a coincidence asserted as a law in three places** — `MoneyReference` (fixed; would have swept the side bet against `warden`), `StandingAnswerTests` (fixed), and the tournament's null cell (**left alone on purpose**; it changed hands to `warden` and holds). R3 and R13's owed corrections landed: the claim-money interval is paired at **±0.25** (mean unmoved to six decimals) and four `money.side-margin.*` rows appeared. `sim suite` 8000/seed 20260819 = **116 measurements in 11,020 s (3 h 04)**. ⚠️ **No rules question raised; `RULES.md` stays at rev 24** — `warden` declines to lock jokers, because §9 #27 is an unconfirmed `PLAYER` ruling and the one rung resting wholly on the lock must not be built on it. 🔥 **Green at 715 / 0**, from 697. |
| 2026-08-21 | P30.2 | **Done — conformance: the rules as played, on Fable 5, and the verify branch is closed.** `Tests/Conformance/RuleConformance.cs` audits a round as it is played — **independent re-derivations, never calls into the code under audit**: the feeding ban mirrored from public events alone, settlement recomputed without `Settlement` or `MoneyCardRegistry`, melds validated straight from §6, conservation and write-once ownership checked at every event. Run over **180 ordinary rounds at 4/5/6 seats** with all of `BotCatalog` and `DifficultyLadder` seated (~27 s), plus **one mutant per rule family proving each check can go red** (P13.6's vacuity lesson, institutionalised). `SettledRuleCoverageTests` parses the Settled sections out of `RULES.md` into a check-or-exemption registry and fails the build both ways (unregistered Settled section, or stale registry entry). **The console driven to a declaration**: `drive-console.py` rewritten to answer prompts by their text — byte-repeatable (proved by `cmp`), verifies every settlement panel's arithmetic, exits by the console's own path; ⚠️ **the packet's premise was stale** — both fixed-key captures had quietly started reaching round 1's settlement (the grep needle was `went out`; the console says `declares`) — but the rewrite was still owed, since fixed lists answer by position and had drifted twice. **The browser driven to a declaration**: `BrowserRoundTests` plays a round through `SeatBoard`s, asserts the strict concealment form at the boards, and closes the loop **board = engine = journal replay**. 🔥 **R1 fixed in the engine**: a discard legal only under §5.1 exception 2 *is* the declaration — the seat is not asked and cannot decline. 🔥 **R8 fixed in the server**: new `SeatChannel` holds a seat's question state across occupants; each `SitDown` mints a fresh `SeatConnection`, the superseded one is dead server-side, and a standing question moves to the new occupant. **R2**: journals stamp rev 24, bound to `RULES.md`'s header by a test, and the `/poker` skill's Phase 6 names the bump. **R3/R13**: the claim money margin is paired (`CellPlayer.NetPerRoundByGame`) and `money.side-margin.*` is published — both reach the CSV at P31's regeneration, annotated in `STRATEGY.md` until then. **All 29 P30.2-fix items landed** (R30 by fixing the false remark — re-basing `DealBuilder`'s filler would re-derive every hand-computed payout to delete comments that are true). R6's four coverage holes are closed (the fifth question is exercised at the dial, over a connection with an *allowed* objection, in a forced journal round-trip, at the advice watcher's upstream seat, and in the audit field). No rules question; `RULES.md` stays rev 24. Tree green at **697 passed / 0 failed** in ~8m 30s. |
|---|---|---|
| 2026-08-21 | P24.1 | **Done — a journal for the hosted table, on Fable 5, and P30.2's browser half has what it is written against.** `--journal table.jsonl` on `BurmesePoker.Web` writes the file the console writes and `sim replay` reads. The split that shaped it: **`TableSession` owns the `GameJournalBuilder`** (the agents are built there; `JournalingAgent.Wrap` runs **outermost**, so what is recorded is the answer that reached the engine — a stand-in's or the clock's included) and **the file stays the host's** — `HostedTable` flushes `TablePlan.Journal`'s path **after every settled round, on the dealer's own thread**, because nothing ends a hosted match but the table closing, and a flush from any other thread — disposal included — could read a round mid-sentence. `TableSeat` gained `Strategy`/`Attribution` (a level's name for a Web bot, `human` for a remote seat — the CSV join key's other half, §3.8 item 4); an abandoned round sets `Header.Abandoned` with `Rounds` stopped at what settled; the lobby form does not offer the flag **on purpose** (two tables, one path, taking turns overwriting each other). ✅ **The core claim is a test**: a round played through the server's own plumbing — remote seats answering, bounded seats, the fan-out — **replays identically** through the ordinary `JournalPlayerAgent`. ⚠️ **Found on the way in: the previous session's "next packet is P30.2" had skipped P30.2's own unbuilt dependency** — the §4 diagram said `P24.1 ☐ ──► P30.2` all along; trust the diagram over the prose. ⚠️ The journal still stamps rules **rev 13** (R2) — left alone on purpose, P30.2 owns that fix and it is one constant for console, Sim and Web alike. No rules question; `RULES.md` stays rev 24. Tree green at **677** (672 + 4 server + 1 web). |
| 2026-08-21 | P30.1 | **Done — a thorough code review of the whole tree, on Fable 5, and the harness's list now exists.** `docs/REVIEW-2026-08.md`: **37 findings, every one carrying file, line, severity, class and triage; nothing unassigned.** Method: baseline verified green first (672/0 in 8m34s), then one deep read of all of `BurmesePoker.Domain` + `drive-console.py` with `RULES.md` rev 24 open, plus **four parallel scoped sub-reviews on the same model** (Server+Presentation, Web, Console+Sim, Tests), each briefed with the packet's nine defect classes. 🔥 **CRITICAL: §5.1 exception 2 is offered but never bound (R1)** — a banned rank legal only as a declaring discard can be thrown by a human who then declines to declare, in both front ends; bots always declare so no figure moves. 🔥 **HIGH: every journal written since 2026-08-21 stamps rules rev 13 (R2)** — four play-changing revisions passed and nothing bumped or guards the constant. 🔥 **HIGH: `STRATEGY.md` §12's claim-permission money margin `+0.02 ± 0.18` used the unpaired formula on a within-cell comparison (R3)** — the codebase's own `Measurement.Paired` remarks name this exact case anti-conservative; the null survives, the interval is understated. 🔥 **The systemic hole is the fifth question (R6)**: `FallibleAgent.ObjectToClaim` is exercised by no test, no connected fixture ever *allows* a claim, the journal's `"objection"` line is covered by seed-luck, and two more translators carry default arms in the `JournalFormat.Name` shape (a sixth `SeatQuestion` would render as the declaration prompt and softlock the seat). ✅ **The clean bills are recorded so P30.2 does not re-check them**: the §7.1.1 table exists once, every identity-notion call site is right, settlement/ban/concealment/determinism all verified. ✅ **No new rules question — `RULES.md` stays at rev 24.** No code changed; the tree is green at 672/0. |
| 2026-08-21 | P29 | **Done — the standing answer re-measured under the rules as they are, and the plan is empty.** `sim suite --games 8000 --seed 20260819` produced **91 measurements in 9,981 s**, and **4 of the 91 rows came back byte-identical — the four ε constants, the only numbers in the file a human chose.** ⚠️ **P23 reproduced 59 of 77 and called it the document's strongest reproducibility claim; this is not a failure of that** — *does it reproduce* cannot be asked across a rules change, because the only rows that survive one are the rows that are not measurements. 🔥 **Three predictions were written down before the run so the packet could be wrong. Two held; one did not, and the wrong one is worth the most.** ✅ **The dial survives** — every step still separates under Holm, **no ε moved**, reference spacing improved to 8.1 / 7.9 / 7.1. ✅ **`prospector` separates a whole ratio lower** — $5/$20 goes from `+0.95 ± 1.63` *inside the interval* to **`+5.32 ± 2.27` separated**, with the take rate collapsing **8.4% → 0.1%**, which is the mechanism variable and so a measurement rather than a coincidence; and the **`$5/$1` identity survived P26**, re-checked byte for byte at 400 games. ❌ **`outs` did not narrow — the point estimate did not even drift**, `+3.0 ± 1.0` over `greedy` against +3.1, mean margin +14.6 both times. 🔥 **What P25's win condition actually moved is `simple`**, which gained about two points on each of `greedy`, `cautious` and `counting`: **the four-handed condition demands a joker-free series, no rung is aimed at that, so the better melder pays the same tax — a requirement nobody optimises for compresses a ladder from the bottom rather than tilting it.** ✅ **Two things nothing had ever reported are published** (`STRATEGY.md` §12): round length and abandoned counts — 28.6 turns a round for the ladder, and **the only non-zero abandoned count is the field containing `random`** (8 of 9,072), with both all-`outs` fields settling every game; and **what refusing a claim is worth, which is nothing** — `outs/refuse` over `outs/allow` is `+0.4 ± 1.0` on win rate and `+0.02 ± 0.18` on money, **a null published with its denominator**, since the opener asks in about a quarter of rounds and the upstream seat holds the rank about half the time. New: `Domain/Agents/ClaimPolicy.cs` and `ClaimPolicyAgent.cs` (an experiment's arm, in no catalog and no menu), refusals counted through `SimObserver` → `RoundRow.ClaimsRefused` → a `claims_refused` CSV column, and `TournamentCell` carrying rounds, turns and the claim counts. Build clean, **672 passed / 0 failed** (666 before; five in `ClaimPolicyTests`, one in `StandingAnswerTests`). ⚠️ **The five-hour suite budget was wrong in the cheap direction and was quoted in three places** — `outs` costs **7.0×** a `greedy` round now against 8.2× at P21, so the suite is two and three quarter hours *with a cell added*; all three are corrected. ⚠️ **`SeatRow.Claims` counts claims *asked for*, not *got*** — P28 made those different and nothing had said so. **No rules question arose and `RULES.md` is untouched at rev 24** — the first packet since the rules sessions began to measure without discovering a rule, which is what it was for. |
| 2026-08-21 | P28 | **Done — the claim's permission and per-round seating, and with them the last settled rule that had no code.** `Domain/Play/ClaimRequest.cs` is the claim being put — a claimant, a card, and `MayBeRefusedBy`, which asks **`Card.SameRankAs`** and is therefore §5.1's own predicate rather than a second one (§9 #30). `IPlayerAgent.ObjectToClaim` is the game's fifth question and **the only one asked of a seat that is not on turn**; `RoundEngine.IsPermitted` asks it **only of a seat holding that rank**, so a question with one possible answer is never put and the disclosure is entirely in the answer. 🔥 **A refused claim arms nothing** — the opener falls through to a blind draw — which is the one line where this packet touches P27. `MatchEngine.PlayRound()` shuffles, draws the seats and deals, in §3's order; `PlayRound(drawOrder)` draws no seats, because a deal written down card by card is a deal written down *for a seating*. Build clean, **666 passed / 0 failed** in 8 m 28 s (642 before; baseline verified at `HEAD` in a worktree, 642/0 in 9 m 09 s). 🔥 **The finding that cost the most is older than the packet**: `JournalFormat.Name` ended `_ => "declare"`, so the fifth question was written to file as a **declaration** — a journal that reads back as a different game — and only `AJournalWrittenToAFileReplaysFromIt` could see it, because the in-memory replay never crosses the format. **A serializer's default arm is a mistranslation waiting for the next case**; every case is named now and the default throws. ⚠️ **Three places in the server assumed *asked* meant *on turn* and exactly one was load-bearing**: the permission carries the *opener's* turn number, so `BoundedAgent` must check the clock and **not** announce `TurnBegan`, or every client spotlights the wrong seat mid-turn. ⚠️ **A test that answers "no" can hide the question after it** — `ClickingPlayer` and `ScriptedSeat` declined every claim, making the permission unreachable from both fixtures; `ClickingPlayer` claims now. ⚠️ **Every rung refuses whenever it may**, on §4.5's own reasoning, and **no rung prices the disclosure** — a decision, not a derivation, and P29's to measure. ✅ **The seating became narration**: `IGameObserver.RoundStarted` and `TableEvent.RoundStarted` carry it, the console prints `Seats drawn:` every round instead of once at setup, and `TableBoard` re-lays the ring between deals — **P13.5's *you at the front* needed nothing**, because `TableRing.Around` already takes a seating and a viewer. ⚠️ **A seed from before P28 no longer plays the same match** and **a `drive-console.py` capture no longer compares**, for the third packet running. **No rules question arose**; `RULES.md` is rev 24 — §10 #16 and #18 discharged, status rather than a rule moving, and **every rule the document records as Settled is now implemented.** |
| 2026-08-21 | P27 | **Done — §5.1 is code, and a legal turn changed for the first time since P0.** `Domain/Play/FeedingBan.cs` is the whole rule: two `HashSet<Rank?>` a seat — taken-in-the-open and released — and one method, `LegalDiscards(hand, rules)`, carrying both exceptions and the floor. `PlayerState.MayNotBeFed` is the seat's own record, `TableState.SeatFedBy` is the one seat that reads it, and **`TurnContext.LegalDiscards` is the whole of the choice a turn presents** — never empty by construction, and on an ordinary turn the hand itself, same reference, no allocation. Build clean, **642 passed / 0 failed** in 9 m 42 s (590 before; baseline verified at `HEAD` in a worktree, 590/0 in 8 m 57 s). 🔥 **The filter became a property of the ladder rather than a line each rung remembers**: `CoverScore.Discard` and `CoverScore.Ranking` take a `TurnContext` instead of a hand, so every rung that ranks is filtered by construction **and so is the runner-up a difficulty level throws as its mistake** — the thing §5.1 warned was easiest to get wrong. Only `RandomBotAgent` needed a line of its own. `LegalDiscardsTests` runs over **all** of `BotCatalog` and `DifficultyLadder`, so a rung added later cannot forget. 🔥 **The rank-only predicate is `Card.SameRankAs`, and it is literally `Rank == other.Rank`** — nullable equality is what makes a joker close the other jokers, so §9 #27's `PLAYER` house ruling falls out of the type rather than being written as a case; ⚠️ **P28 must read it for the claim's objection (§9 #30), not write a second one.** ⚠️ **The floor is one line; exception 2 is not** — a `HandEvaluator.IsWinning` per banned card, gated on the hand being fourteen *and* on `PartialCover.CoversAtLeast(hand, 13)`, so it is asked only of a hand that could actually go out. 🔥 **The finding that outlives the packet: a bot's cover count can now fall.** `GreedyBotAgent`'s stated reason a table of bots terminates is that the score can never fall, *because throwing back the card just taken restores the hand* — §5.1 removes that card from the choice, so convergence is no longer guaranteed and `TurnCap` / `RoundTimeLimit` are what stand behind it. **P29 should publish round lengths and abandoned-round counts.** ⚠️ **It already broke two shipped tests, both fixtures rather than code**: a passive seat throwing back the 3♠ it drew became illegal one turn after Alice claimed the 3♣, and `SkillLadderRunTests` asserted an ordering over **eight** games in which `simple` — which wins 29.0% over two hundred — drew a blank. 🔥 **Measured, and the surprise is how cheap it is**: same seed, greedy vs simple, four seats, 200 games — **26.6 → 27.1 turns a round, 69 → 69 rounds/s, 31.3% → 31.5%**. ⚠️ **The reason is that a seat takes in the open on only about a fifth of its turns**, so a rank is closed for far less of a round than the rule's prominence suggests — **expect more at a table of stronger rungs.** ⚠️ **"Domain only" was false again, and in a new way**: a closed card is `CardDisplayState.Unthrowable` with the token `closed`, a legend entry and an accessible name, the console drops it from its `SelectionPrompt` and the browser draws it as a `<span>` rather than a disabled button — **a control that does nothing is exactly the failure P13.4 spent a packet chasing** — so **a `drive-console.py` capture from before P27 no longer compares**, for the second packet running. ✅ **The server refuses a banned answer where it already refuses a card you do not hold** (`SeatPrompt.MayThrow` in `SeatAnswer.Discard.Fits`), so a bad client does not take a hosted table down; the engine's guard stays behind it. **No rules question arose**; `RULES.md` is rev 23 — §5.1's *"none of this is implemented"* replaced and §10 #13 discharged, both status rather than a rule moving. |
| 2026-08-21 | P26 | **Done — the money layer is what `RULES.md` §4 actually says.** `MoneyCardRegistry.Permanent` holds **three values and eight cards** (the jokers joined), `Multiplier(Card)` returns 0/1/**3**, and the ×5 is `Multiplier(card, owner, MoneyOwnership)` under a configuration `Settlement` reads from `CardOwnership` **once a round**. ✅ **Design decision 2 held**: nothing is stored on a card, and `Multiplier(Card)` survives as the value-only question — which is what every view drawing one card at a time is really asking, and why no caller outside `Settlement` needed a parameter. Build clean, **590 passed / 0 failed** (574 before; 16 new). Baseline verified at `HEAD` **in a `git worktree`**, 574/0. 🔥 **The packet's own stated prediction was the deliverable and it came out right**: under the ×3 a designation on the 7♦ and one on an ordinary card leave **exactly the same** money loose in the shoe, so §4.1's conservation arithmetic is sound and the rule did not need re-asking; `ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe`'s last assertion is an equality now. ⚠️ **`STATUS.md` had predicted that test would be *red* before the packet started and it was green** — the code still implemented the withdrawn rule, so it agreed with the code and disagreed only with the document. 🔥 **Seven shipped tests went red and all seven for one reason: jokers are dealt.** Four of 108 cards, so an arbitrary four-handed deal hands out one or two, and five tests carried the comment *"nobody owns a money card"*. **The blast radius of rev 21 is every golden settlement number in the tree, not the money layer.** 🔥 **Measured: the side-bet moved `$8.50 ± 0.26` → `$11.58 ± 0.34` a round at five seats, 42.5% → 58% of the round prize**, with the *before* run reproducing P12's rev-13 figures at a different seed so the instrument is validated rather than asserted; `RULES.md` §4.3 and §4.4's stale `DERIVED` notes are re-derived (rev 22) and §10 #17 is discharged. ✅ **Play did not move** — same seed, same wins, turns and cover: §4.4's decoupling claim measured *across* a money-layer change for the first time. ⚠️ **"Domain only" was true of the logic and false of the text**: `($$)` → `($$$)`, `PaysDouble` → `PaysTriple`, four user-facing strings, **so a `drive-console.py` capture from before P26 no longer compares**. ⚠️ **No view can show a ×5** and no packet owns that gap; **`MoneyOdds` does not price it** either, and says so. ⚠️ **§9 #32 stays open and is fenced by two tests**, not a comment. |
| 2026-08-21 | P25 | **Done — the win condition is a function of the table size, and the engine finally asks the right question.** `Domain/Melds/TableRules.cs` is `RULES.md` §7.1.1 as data and the only place it is written down; `HandEvaluator.IsWinning(hand, rules)` and `TryFindCover(hand, rules, out melds)` are the whole surface and **the parameterless overloads are gone**, so no caller can ask the five-handed question by accident. `TableState.Rules` and `TurnContext.Rules` are where the engine and a seat read it. **574 passed / 0 failed**, up 26. 🔥 **The interesting part was the memo, not the requirement**: the search state is `(covered, seriesStillOwed, cleanStillOwed)`, because a covered-set from which no completion can supply *two* more clean series may perfectly well supply one, and the old key would have poisoned the second question with the first question's dead ends. The counts are carried **along** the partition and clamped at nought — a clean series discharges both, an impure one the series count alone (§9 #28, #29) — with one prune, that fewer than three uncovered cards per series still owed cannot pay for them however they fall. ✅ **Two-handed was the cheap case**: sets are illegal *as melds*, a property of a meld rather than of the partition, so `MeldIndex.Build` takes `setsAllowed` and the search never sees one — ⚠️ **and filtering on `Meld.Kind` keeps more than it looks like it does**, since `MeldCandidates` already emits `{9♦,🃏,🃏}` and `{🃏,🃏,🃏}` once, as the **run** interpretation. ✅ **`Meld.IsClean` needed no special case for the all-joker meld** — `Kind == Run && JokerCount == 0`, and three jokers fail it because every slot is a substitute, so §6.1 and §9 #29 agreed without either being bent. 🔥 **The change is real and `drive-console.py` cannot see it.** Four-handed greedy-vs-simple over 200 games at one seed goes from **25.1 to 26.6 turns a round** and **102 to 86 rounds/s** — strictly harder, longer rounds, as predicted — and yet **both console captures are byte-identical to `HEAD`** (8,417 and 90,251 bytes). ⚠️ **Not because nothing changed: both scripts quit in round 2 and neither capture contains a declaration at all.** The instrument that proved P21 and P23 were refactors is blind to the win condition by construction — **do not read a clean `cmp` as evidence about play.** ⚠️ **`PartialCover` was left alone on purpose and now measures something else**: `IsComplete` agrees with `IsWinning` only at five or more, so **every rung in `BotCatalog` is maximising cover count at a table where cover count is no longer the win condition** — P29's prediction 2, to be priced rather than fixed. ⚠️ **Left behind and owned by nobody: `RoundEngine.MinimumPlayers` is still 4** (§10 #7), so `TableRules.For(2)` and `For(3)` are correct, tested and unreachable from a dealt game. **No rules question arose and `RULES.md` stays at rev 21** — §10 #14 gained a ✅ discharge note, which is a status annotation and not a rule moving. |
| 2026-08-21 | P25–P29 | **Planned, not started — and the plan has a second half again.** 🔥 **`RULES.md` is rev 21, and the last two answers changed §4.1 twice over.** Asked how much a turned-up joker's partner pays, the answer went behind the question: **"7 of diamonds, ace of spades, AND jokers are always money cards"** — so the **permanent side-bet doubles, 4 cards to 8**, two `DERIVED` arguments built on the old count are stale (§4.3's measured *42% of the round prize*, §4.4's *~4 of 6 owned at the deal*), and rev 20's ×3 list stops looking arbitrary: **it was never a list of special cards, it is the list of permanent money cards.** 🔥 **And a rule nobody asked for — a jackpot**: if the two turned-up cards are the 7♦ and the A♠ and one player owns **both** partners, they pay **×5 each** rather than ×3, which at standard stakes is **$40 a head against a $5 round prize**. ⚠️ **It is the first rule in this game where a card's value depends on who holds what**, so a multiplier is a property of *(value, ownership)* and `MoneyCardRegistry.Multiplier(Card)` cannot answer alone. ✅ **The headline design decision survives** — money status is still *computed, never stored*; the inputs widen, the principle does not. ✅ **§9 #30 closed: an objection turns on the *rank*, which is §5.1's own predicate**, so the claim's test and the ban's test are one predicate and must not be written twice. ✅ **#31 closed by its premise turning out to be wrong** — asking it flat, without mentioning the 7♦, produced a correction instead of a confirmation. ⚠️ **One question left, §9 #32**: whether the ×5 needs the 7♦/A♠ pair specifically or any two tripled values — a combination that exists **only because jokers became permanent**. 🔥 **The pattern across four revisions is worth naming: every one of them answered past the question asked, and three changed a rule nobody was asking about** — rev 19 defined *a game*, rev 20 supplied the ownership framing and superseded a one-day-old `EXPERT` ruling, rev 21 made jokers permanent and produced the jackpot. **This game's rules are recalled as wholes, not as answers**, and asking narrowly has lost material three times. **Then the plan: P25 the win condition by table size, P26 the money layer as it actually is, P27 the feeding ban, P28 the claim's permission and per-round seating, P29 re-measure** — written into `BUILD-PLAN.md` §5 with a new dependency graph in §4. ⚠️ **P24 re-sequenced after P29 and flagged as a recommendation rather than a decision**: its stated reason for going first (*§5.1 is blocked and P24 makes that conversation productive*) is spent, and shipping an explanation of decisions that P25–P27 are about to change means writing the sentence twice and believing it once. 🔥 **P29 carries three predictions written down before the run**, so it can be wrong: the difficulty dial survives, `outs` **loses** margin at four seats (its objective is cover count, which is no longer sufficient for a win), and `prospector` separates at lower stakes than $5/$40. Docs only — `RULES.md`, `RULES-PRIMER.md`, `QUESTIONS-FOR-MYA-LAY.md`, `BUILD-PLAN.md`, `CLAUDE.md`, this file. **No code touched.** |
| 2026-08-21 | — | 🔥 **The money layer's last two questions closed — `RULES.md` is rev 20 — and both answers reached past the question.** **(1) Claiming the turned-up money card requires the permission of the player who goes *before* you in turn order**, who may object **only if they hold that card**: your take is public, so it arms §5.1 against exactly that seat and locks them into holding their copy all round. `EXPERT`, closing §9 #5, which the 2023 source carried as a `TODO` nobody could explain. 🔥 **It is the first rule tying the money layer to the feeding ban, and it independently confirms two §5.1 rulings that were only recommendations when they were made** — #16 (the ban binds the seat above you, not the table) and #17 (only a public take arms it, the §4.5 claim included). **A permission rule naming the upstream seat cannot be stated in a game where either is false**, which is the strongest structural evidence yet that §5.1 is an old rule rather than a one-off table ruling. ⚠️ **Two consequences nobody had noticed**: the claim is an **attack** — it pays nothing (§4.4) and buys a lock on the hand of the player who discards to you all round, which `prospector` models not at all — and an **objection is a disclosure**, the first thing in this concealed game a player reveals by choice. **(2) A turned-up joker designates the other joker of its own colour** — *"colour matters for jokers"* — closing §9 #11 and confirming that `SameValueAs` already computes it. ⚠️ **And an answer nobody asked for superseded an `EXPERT` ruling from the day before**: a shown 7♦, A♠ or joker **can never be owned, claimed or not, and its partner copy pays ×3**, not the *double* rev 19 recorded. **Both `EXPERT`, one day apart, not in agreement — the later and more specific is the rule and the earlier is struck rather than deleted.** 🔥 **The second time an answer given before the framing that governs it turned out not to be an answer** (§7.1's purity ruling failed identically); the framing here is **ownership**. ✅ **The arithmetic backs the later answer**: ×3 is 1 for the partner's own permanence, 1 for the designation and 1 inherited from the copy that can no longer be paid for, which makes a designation on a permanent money card worth exactly what an ordinary designation is worth — where ×2 would have made it worth *less*. ❌ **First rules change to invalidate a published measurement.** `MoneyCardRegistry.Multiplier` caps at 2 and must return 3; `MoneyOdds` prices blind draws from it; `prospector` is the one rung reading the money; **`STRATEGY.md` §10's money sweep was measured under the struck rule**; and P22's `DERIVED` note is **withdrawn** — the reasoning was sound and the premise moved. ✅ **`ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe` asserts the withdrawn direction and will go red — the derivation was written as an assertion rather than as prose, so this cannot pass silently.** ⚠️ **Two new §9 questions, both raised by these answers**: #30 (does an objection turn on the exact card or the rank — §5.1 is rank-only) and #31 (is a joker's partner really ×3, when the same multiplier conserves money on a 7♦ and creates it on a joker). Docs only — `RULES.md`, `RULES-PRIMER.md`, `QUESTIONS-FOR-MYA-LAY.md` (**new Q8**), `BUILD-PLAN.md`, `CLAUDE.md`, this file. **No code touched; the tree is still green, and it is green against a rule this document no longer holds.** |
| 2026-08-21 | — | ✅ **The money layer is confirmed by a person for the first time — `RULES.md` is rev 19, and §9 is down to two questions.** Nine put to **Mya Lay and Aung Aung**, nine answered, and **eight confirmed a standing default**: **7♦ and A♠ are money cards in both copies out of both decks** and a turned-up 7♦ makes it a **double, *"worth double"*** (§4.1, `CODE` `Probable` → `EXPERT`, and it promotes §4.3's `DERIVED` guess that a double counts as two); **a claimed money card's value goes on paying** — *"all 9s of hearts become money cards"*, confirming P7; **the turned-up cards stay out of the reshuffle**, confirming P9; **you may throw back the card you just took**; **a turned-up joker designates jokers**; and **7♦ and A♠ are what they are by "tradition"** — a question recorded as *likely unrecoverable* since rev 1, recovered in one word. 🔥 **One answer reversed a default and one arrived as a definition, and they are the same answer.** Asked *how often* the money-card claim is offered, the reply defined the unit: **a game is from the turn-up to somebody going out — a game is a round.** Asked what moves between rounds, the reply used it: **"you randomize seats between games."** 🔥 **So the seating is re-drawn before every deal, and `MatchEngine` — which holds one seating for a whole match, P9's choice while the question was open — is the first thing in this tree that actively contradicts a recorded rule rather than merely lacking one** (§10 #16). ✅ **No published figure moves**: every experiment in `BurmesePoker.Sim` runs `RoundsPerGame = 1`, checked rather than assumed, so no measured game has a second round to re-seat for. ⚠️ **The front ends are where it shows, and it is a UX question as much as a loop** — P13.5 puts *you at the front whichever seat you were dealt*, so the table would visibly rearrange itself around a fixed viewer every deal. 🔥 **The most useful answer was a rider.** #13 came back not as *"yes"* but **"yes, as long as you aren't violating any other discard rules"** — and §5.1 is the only other discard rule there is, so **the feeding ban outranks the right to throw a card straight back**, the just-taken card is filtered like any other, and the §5.1 packet needs no special case for it. ⚠️ **A player describing a legal-move filter unprompted is the same construction §5.1's enforcement already takes.** ⚠️ **Two answers were hedged and are tagged as house rulings with the hedge quoted** — *"maybe… i don't think that's mechanically possible/relevant"* on whether money cards stack past a double (#24c, `PLAYER`), and *"not that I know of"* on whether anything else changes with the player count (#26, `EXPERT` **Probable**, kept unsettled because §7.1.1 is proof the question is real). 🔥 **#24c is half wrong and the document can prove which half**: a triple *is* reachable — both 7♦s turned up together, about one round in 5,800 — and it *is* irrelevant, because both copies are then lying on the table as designators, nobody was dealt one, a card on the table is owned by nobody (§4.4), and the value pays nothing whatever it stacks to. **Not flattening the hedge is what left room to find that.** ✅ **What remains is #5's *who approves the claim* and #11's *which jokers*** — and #11's default sends a joker's money to two cards where §5.1 #27 sends its feeding ban to all four, deliberately, since designation is scoped narrow and feeding broad. Docs only — `RULES.md`, `RULES-PRIMER.md`, `QUESTIONS-FOR-MYA-LAY.md` (**new Q7**), `CLAUDE.md`, this file. **No code touched.** |
| 2026-08-21 | — | ✅ **The win condition is fully specified — `RULES.md` is rev 18.** Two questions put to **Mya Lay and Aung Aung**, the last two open against §7.1.1, both answered `EXPERT`, and **neither changed a rule**. **(1) §9 #22 — at two players a set is illegal *as a meld*, not merely unnecessary**: *"in 2 player, you must go out with ALL SERIES, no sets."* Rev 17 had recorded the strong form because it was what was said, and flagged that it had never been put back as a table situation; put back, it held. 🔥 **A confirmation is not nothing** — §9 #25's reasoning about the feeding ban head-to-head leant on sets being illegal two-handed, and until now it was leaning on an unconfirmed recording. **(2) §9 #29 — an all-joker meld is a series, *"but not clean"***, so it can discharge a **surplus** series and never a **required** one (§6.1, §7.1.1) — three-handed, three jokers can never be one of the two. 🔥 **That also closes §9 #8**, open since rev 1, and it is **the only question in this document that a piece of reasoning got right before anybody was asked**: the recommendation was *unlimited jokers, which permits an all-joker meld*, and **P3 has been emitting them since rev 10 on exactly that argument** — against three where reasoning ahead came out wrong (§6.2 duplicate suits, §7.1 purity, §5 discard piles). ⚠️ **One piece of it is `DERIVED`, not `EXPERT`**: nobody was asked *how many* jokers a meld may hold, and that a three-card meld may be entirely jokers merely forecloses any cap below a meld's own size. ⚠️ **No code changes and no measurement moves** — P3 already emits all-joker melds, and `HandEvaluator` was already wrong about the two-handed rule before rev 18 and is no more wrong after it (§10 #14). 🔥 **The finding is about asking, and it cost an exchange.** #22 was first put as *"illegal, or merely not required?"* — two named readings — and came back *"Why are you asking me about sets?"* **A rule can be described accurately in words that leave an implementer two choices, and the player who knows the rule hears no ambiguity at all.** What worked was not a sharper distinction but a **table situation** — *three of a kind in your hand at the end*. ⚠️ **Bookkeeping found while writing this up: rev 17 never got a session-log row**, so its twelve answers and §7.1.1 reached `RULES.md` but not this file; the **Current state** block above has been corrected (it still said §5.1's six details were unrecorded, and still called this file's rules pointer rev 16), and `CLAUDE.md`'s start-here block was corrected the same way. Docs only — `RULES.md`, `RULES-PRIMER.md`, `QUESTIONS-FOR-MYA-LAY.md` (**new Q6**), `CLAUDE.md`, this file. **No code touched.** |
| 2026-08-20 | P24 | **Planned, not started.** 🔥 **The first packet in the plan that came from a playtest rather than from §0**: the browser's arrow gets a *why*, so a session played beside an expert leaves a **record of where the expert disagreed**. Scope taken by Nick the same day — **browser only** (the console keeps its arrow, so `drive-console.py` captures stay comparable), **all four questions**, **winner versus runner-up** rather than a ranking table, and **written to a journal** as well as drawn. 🔥 **Two findings from the survey, both taken before a line is written.** **(1) `BurmesePoker.Web` and `BurmesePoker.Server` contain the string `journal` zero times** — `--journal` is on the console and the harness only, and `TableSession` hands its agents to `MatchEngine` with nothing wrapping them, so journalling the hosted table is a piece of work *inside* P24 rather than a field on an existing writer. **(2) A rationale field on a decision records the wrong thing.** `JournalingAgent` writes down the answer *the seat gave*, and at a table with Mya Lay in it that answer is hers; the computer's recommendation is a different agent's opinion about the same moment, so the record is the decision **plus the advice** — and **disagreement becomes a query (`Answer != Advice.Card`) instead of something a person notices and transcribes**. ⚠️ That contradicts `JournalingAgent`'s stated stance (*"it records answers, not intentions"*) deliberately and narrowly: the intention is a *different* agent's, taken on the same context. ⚠️ **The explanation costs nothing new if it is built right** — `CoverScore.Ranking` already computes every key and throws all of them away except the order, so the discard path calls the explaining ranking **once** and takes its head; `outs` is 8.2× a greedy round and a second call per turn would double every human turn for a sentence. ⚠️ **The keys are packed for sorting, not for reading**: `outs` stores its key negated, `Potential` is `int.MaxValue` for a joker, `cautious` packs two into one `long` — so a rung describes its own keys and presentation never interprets a bare number. ⚠️ **Two constraints are already tests**: the rationale rides on `SeatPrompt` and never on a `TableEvent` (`ConcealmentTests`, §3.11 A1), and three sentences of prose must live in a `<details>` (`MarkupStandardsTests.NoParagraphOnTheTableIsAWallOfText`, 80 characters, §3.11 B9). 🔥 **A third trap surfaced on the second pass and it is P19's, not P21's: never explain through `FallibleAgent`.** A difficulty level is `Hardest` wrapped in a mistake rate, and that mistake is **the runner-up of the very ranking this packet renders** — so a level in the advice path yields a page confidently justifying a move chosen *because it was second best*, which is the failure mode that still looks right. **Acceptance 6 asserts the bare rung at every level, `easy` included.** ⚠️ **Two smaller ones written down with it**: the explaining interface is **public while everything it reports on is `internal`** (the domain's only `InternalsVisibleTo` is the test project, P21), so the keys cross as a *described* result rather than by widening it; and the advised card is journalled **by `CardId`** exactly as the answer is (§3.1), because two decks hold two 5♥ and a value comparison would report agreement on precisely the hands worth studying. ⚠️ **The rejected options are recorded rather than dropped** — a full ranking table (renders the procedure, not the decision), a sentence-plus-table (the same table wearing a summary), a `--explain <path>` sidecar (a second format to join, when P14 already owns what happened at this table), and the console (cheapest place for a debug tool, not the place you sit with somebody, and it would spend P21's and P23's byte-identical captures for no reader). ⚠️ **Sequenced ahead of the §5.1 packet on purpose** — §5.1 filters the very ranking P24 renders, but it is blocked on §9 #16–#19 and P24 is what makes that conversation productive. Docs only: `BUILD-PLAN.md` §4 and new §5 packet, this file. **No code touched; the tree is unchanged at 548 passed / 0 failed.** |
| 2026-08-20 | — | 🔥 **A playtest with Mya Lay produced a new rule, and it is the first one that constrains *which* card you may discard.** `RULES.md` is **rev 16**; the rule is **§5.1, the feeding ban**, `EXPERT` and Settled: **once a player has taken a card in the open, the seat that discards to them may not discard that rank again for the rest of the round** — *rank only, suit irrelevant* — with two exceptions, **release** (the protected player discards that rank themselves, which frees it **permanently**, even if they pick another one up afterwards) and **going out** (the ban never stands between a player and a win, and costs nothing there because the round ends on that discard). 🔥 **The release is what makes it cheap**: it is a **set of ranks per seat**, one bit a rank — *"has she thrown a Queen this round?"* — rather than a history of who took what and when. ⚠️ **At a table you answer that by looking down her discards — legitimately, since rev 17 closed §9 #15 and the discards are public — but in code you must not**, because §5's reshuffle sweeps the pile away and would silently re-arm a released rank mid-round, so the set is kept rather than read back. 🔥 **And the match is *rank alone*, which is a third identity notion the domain hasn't got**: `==` on `Card` is instance identity and `SameValueAs` is rank **and** suit **and** colour (what §4.2's money designation needs) — **reaching for `SameValueAs` here would leave the Q♣ Mya Lay actually objected to perfectly legal.** 🔥 **The Q♦ in the playtest was a money card and that is a red herring**: by §4.4 a card *picked up* from a discard is held but never owned and pays nobody, so taking it moved no money at all — the advantage being denied is a **melding** advantage, which is why §9 #18 recommends the ban covers any rank. ⚠️ **Settled and wholly unimplemented.** `RoundEngine` accepts any discard, `TurnContext` does not even show a seat the information the rule is decided from, and no agent knows it exists — recorded as `RULES.md` §10 #13. ⚠️ **Six details are unrecorded and they are the rule's specification, not fidelity questions**: §9 **#16–#19, #25 and #27** — who is bound (recommend the seat above you only), what arms it (recommend public takes only; a blind draw is concealed and unenforceable, and the §4.5 claim must count, because Mya Lay **opened** and had no discard in front of her), money card or any card (recommend any rank), whether a release survives the deck-exhaustion reshuffle that sweeps away the pile carrying it (recommend yes — which means an implementation needs a released-rank set per seat and cannot lean on the pile alone), 🔥 **what taking a joker closes given that a joker has no rank at all** (#27, recommend *jokers*, added later the same day by checking §5.1's *"the same rank"* against `Card`), and whether the ban works **two-handed** where the seat that feeds you is the seat you feed (#25, arrived with rev 17's player-count answers). 🔥 **§9 #20 was raised and closed the same day, in two rulings that only work together.** *(a)* **A banned discard is not an infraction but an impossible move** — never offered, cannot be chosen — so there is no penalty and nothing to retract. *(b)* **Where the ban would leave a player no legal discard at all, the ban yields for that turn** — the discard is mandatory (§7.1), the ban is not. 🔥 **(b) is what makes (a) safe**: impossible-move enforcement takes away the social escape hatch a rare hand would otherwise rely on, so without the floor a fourteen-card hand of banned ranks is a turn that cannot be completed — an unreachable state that, unhandled, is a crash rather than a rare event. It is reachable on **two ranks**, because two decks put eight copies of a rank in the shoe. **Together they collapse to one line**: the legal discards are *the hand minus the banned ranks, **or the whole hand if that is empty***, so the choice presented to a player is **never empty by construction** and a turn cannot deadlock. ⚠️ **Enforcement by construction still has a bill**: the upstream seat has to be *told* which ranks are closed — free, and 🔥 **not a collision with §9 #15**, because the ban is computed from things done **in the open** (the seat below *took* its card in view; a release was *thrown* in view) rather than from reading a pile — safe even under #15's strict default, and rev 17 then closed #15 the permissive way anyway — and every agent's discard ranking must be filtered to legal cards **including `FallibleAgent`'s runner-up**, because a mistake still has to be a legal move. ⚠️ **One speculation raised and withdrawn inside the same revision**: while #20 was open this document wondered whether the deadlock was §9 #6's unrecovered exception to the mandatory discard. **It is the reverse** — nobody skips a discard under the floor; the *ban* is what gives way. ⚠️ **Rev 17 then closed #6 outright — there is no exception, you always discard** — which settles it the same way and makes the floor the only way §5.1 and the mandatory discard can both hold. ✅ `QUESTIONS-FOR-MYA-LAY.md` **Q4** has five of the six phrased as flat table situations (#25 is asked with the player-count set), plus a note saying which two questions not to ask. Docs only — `RULES.md`, `RULES-PRIMER.md`, `QUESTIONS-FOR-MYA-LAY.md`, this file. **No code touched; the tree is unchanged at 548 passed / 0 failed.** ⚠️ **There is no packet for this yet** — implementing §5.1 needs one, and it is the first work since P0 that changes what a legal turn *is*. |
| 2026-08-20 | P23 | ☑ Done. **The standing answer — and the last packet in the plan.** `docs/STRATEGY.md` answers *"which bot should I play, and what actually works in this game?"* in one document that regenerates from one command, and the two ways it could quietly stop being true are now assertions rather than habits. **548 passed, up from 543.** 🔥 **The headline is the reproduction: 59 of the suite's 77 rows came back byte-identical** — every ladder cell, the null test, all twelve pairing ratios and both of P12's headline rows, from a tree that had since gained a rung and changed what the standing field is. **The seven that moved are the difficulty dial and only the dial**; the twelve that are new are the money sweep, so §10 quotes `measurements.csv` like every other section and P22's join is closed. 🔥 **The re-calibration moved exactly one value.** P21 re-based the dial onto `outs` without re-spacing, leaving reference-table steps of **8.2 / 4.3 / 10.3** points — monotone, passing every standing check, and visibly not a dial. Re-sweeping ε on `outs` over P19's own seven probes moved `hard` from **0.5 to 0.4** and left the other three alone: **7.9 / 6.7 / 7.7** at the reference table, **+11.20 / +9.80 / +6.82 ± 1.00** head to head, all surviving Holm at 6.8× the half-width or better. ⚠️ **Only one moved because the ε curve barely changes shape between rungs** (0 → 0.5 costs 9.5 points on `outs` against ~8 on `greedy`; 0.5 → 1 costs 16.5 against ~17) — **a mistake rate is nearly a property of the mistake, not of the rung it is made against**, so re-check it after the next rung rather than re-deriving it. 🔥 **And two instruments disagree about spacing**: the reference table is narrowest in the middle, head-to-head narrowest at the top, because a cell holds two levels and a table holds four — the re-fit improved both, and §9 records that the reference table is the tie-break. 🔥 **P22's bill was paid structurally, not by typing a shorter field**: `BotRung.Ranked` (`RankedOn.WinRate` / `Money`) has each rung declare which instrument settles it, `BotCatalog.Ladder` and `StakesSensitive` are **filters of `All`**, and `prospector`'s six duplicate cells are gone — ⚠️ **the wall clock was the smaller half of that argument, because six null cells in a Holm family make every real verdict in it harder to reach.** ✅ Measured, not argued: every ladder figure reproduced to the digit without them. ⚠️ **Acceptance 2 is `Tests/Sim/StandingAnswerTests.cs`**, which reads the published CSV and goes red if the ε values it publishes are not the ones both front ends offer, if a step did not survive Holm, if a rung in the catalog is the subject of no row, if the ladder and the sweep are not between them the whole catalog, or if a front end writes out a description instead of asking for it — 🔥 **it failed on `prospector` first time**, which is exactly the drift §11 had been recording in prose. ✅ **Re-spacing is not a play change, proved byte for byte**: `--pick 0` captures from `HEAD` and this tree are identical from the `Seating:` line on, **7,025 and 88,805 bytes**. ⚠️ **The suite is now five hours** (17,539 s) and **the structural saving has been taken; there is not a second one** — `--pairs adjacent` on the ladder would cost §3's matrix and stays unused. No rules question raised; `RULES.md` stays at rev 15. |
| 2026-08-20 | P22 | ☑ Done. 🔥 **Money: is there a strategy in the side bet? — no at the stakes the game is played for, and `+7.3 ± 3.3` a round at eight times them.** `Domain/Agents/ProspectorBotAgent.cs` is `outs` with one change: a card taken from anywhere but the deck — the previous player's discard, *or* the turned-up money card — must be worth more than the **ownership** a blind draw would have conferred (RULES.md §4.4, §4.5). `Domain/Agents/MoneyOdds.cs` prices that from public information only: the stakes, the designation, how many players pay, and how much of the shoe this seat can see. `Sim/MoneySweep.cs` is the experiment — the challenger against `outs` at four stakes ratios, every cell dealt from the same shoes, **judged on `$/round` with the win rate beside it**, Holm-corrected over the four. **543 passed / 0 failed**, up from 529 (14 new: 6 in `Agents/ProspectorBotAgentTests`, 5 in `Sim/MoneySweepTests`, the rest catalog and ladder cases that widened with the field). 🔥 **At $5/$1 the rule never fires and this is an identity, not a measurement**: two tables of one rung each, dealt from the same shoes, play the same rounds card for card — so the standard-stakes cell is a **null cell**, and at **`+0.01 ± 0.22`** it is the tightest one this harness has produced. 🔥 **At $5/$40 it separates the other way**: take rate **0.1%** against `outs`' 24.9%, **20.1 ± 0.9 points fewer rounds won**, **`+7.34 ± 3.29` a round banked**, `p = 1.3e-05`, surviving Holm — the side bet alone moves `+11.36 ± 3.29`, buying the whole of the win rate it gives away. $5/$10 is `−0.86 ± 0.82` (raw only) and $5/$20 is `+0.95 ± 1.63` (break-even), so **the four cells are monotone in the stakes**. ⚠️ **The programme's first published divergence between money and win rate** — a reader ranking the $5/$40 cell by win rate would rank the better player last — and the report says so itself rather than leaving it to be noticed (acceptance 2). ✅ **Acceptance 3 holds**: the discard is `outs`' card at every stakes tried, `MoneyCardsDoNotChangeWhatABotThrowsAway` covers the new rung, and a sharper test shows the take differing between two designations while the discard does not. ✅ **The difficulty dial did not move and no front end needed a line**: `prospector` shares `outs`' `Strength: 3`, ladder order breaks the tie, `BotCatalog.Hardest` is still `outs` — and `drive-console.py` at `--seed 20260819 --pick 0` captured **90,251 bytes identical byte for byte** from `HEAD` and from this tree. 🔥 **A `DERIVED` rules note fell out of the arithmetic — RULES.md is rev 15**: a designation landing on a **permanent** money card leaves the deck with *less* money in it, not more, because doubling one value is not the same as designating a second and the designator itself leaves the deck (§3 step 4). **Found by writing a test that asserted the opposite.** 🔥 **And the finding that outlives the rung: a rung's strength stopped being a property of the rung.** `prospector`'s one decision reads the *stakes*, which are fixed per game and are not a rule, so *"how good is it"* has no answer until somebody says what the table is played for. ⚠️ **The bill, and it is P23's.** A new name in `BotCatalog` is `k−1` new head-to-head cells for ever: seven rungs is **21 cells against 15** and `sim suite` went from ~1h45 to about **3h15**, so it was **not re-run** — `docs/strategy/measurements.csv` is one rung behind the catalog, `STRATEGY.md` §10 is generated from the new `docs/strategy/money.csv` instead, and §11 records the gap. **Six of the 21 cells are `outs` against itself in all but name.** Amended BUILD-PLAN **P22 (done, with four findings), P23 (the suite cost, and the join §10 owes `measurements.csv`), §4's graph and prose, and two risk rows — the research-rung row gains P22's cost lesson, and a new row for a standing suite nobody re-runs**. |
| 2026-08-20 | P21 | ☑ Done. 🔥 **Outs: the first rung that looks ahead — and the first that beats `greedy`.** `Domain/Agents/OutsBotAgent.cs` is `greedy` with one thing inserted: where two discards leave the hand equally melded, it keeps the thirteen that **more of the pack would improve** — a count, per candidate, of how many of the values still out there would raise the cover count of what is left. **529 tests, up from 516.** 🔥 **It measures `+3.1 ± 1.0` points against `greedy` at 8,008 games a cell**, `p = 1.9e-09`, surviving Holm, and takes the free-for-all 26.3 ± 0.5 to 23.7 ± 0.5. **Three research packets had produced nothing above `greedy`; this clears the apparatus's resolution three times over.** 🔥 **The *why* is a contrast**: `cautious` and `counting` both put their new idea *underneath* `CoverScore.Potential` and decide only what greedy had already given up on — a residue worth about half a point, which is below what the harness can see; `outs` puts its key *above* it, and greedy's tie-break is demoted to breaking **its** ties. ⚠️ **P20's "change which question is asked" meant "and ask it earlier than the question you are replacing."** ⚠️ **It is `Strength: 3`, so the difficulty dial re-based onto it**: all four levels are `outs` with an ε now, the dial is still ordered but the ε spacing was measured against `greedy`, and **P23 owns re-spacing it and is no longer optional**. 🔥 **The cost was the packet.** Naive, it ran at **12.6× a greedy round**, over the stated budget; four shortcuts *around* the evaluator took it to **8.2×** — refine only what is tied at the top (7.1 candidates a turn, not 13), prune values that cannot enter a meld (34 searched of 53), ask the search for a **bar rather than a maximum** (`PartialCover.CoversAtLeast`, ~98 µs → ~10 µs), and build **one meld index a candidate instead of one a probe** (`CoverProbe`). 🔥 **Then the finding that outlives the rung**: three quarters of what remained was candidate generation, and it was a **fixed per-call allocation cost** — ninety window arrays × four suits every call, and both generators walking suits and ranks that could hold no meld. A window table, one slot buffer a length and two feasibility checks made **every rung, every hint and every engine turn about 45% faster** (greedy 48.8 → 71.8 rounds/s serial, same process). ✅ **Proved a refactor, not a change**: `drive-console.py` at `--pick 0`, both scripts, byte-identical from `HEAD` (8,763 and 92,172 bytes). ⚠️ **Two things not in the build list.** (1) `BurmesePoker.Domain` gained `InternalsVisibleTo BurmesePoker.Tests` — every one of the four shortcuts is a claim about answers and is asserted against the search it replaces, and a test cannot assert what it cannot see. (2) `sim bench` now times **the rung's decision** in whole rounds, and the `--help` ladder is read from `BotCatalog` rather than typed out, which was P20's defect surviving in one more place. |
| 2026-08-20 | P20 | ☑ Done. **Memory: the card-counting rung — and the answer is no, published as a null.** `Domain/Agents/CountingBotAgent.cs` is `cautious` with one substitution: what is left in the shoe is estimated from every card this seat has been shown this round rather than from its own thirteen. **516 tests, up from 506** (10 new — 7 in `Agents/CountingBotAgentTests`, the rest catalog and ladder cases that widened with the field). 🔥 **It measures `+0.3 ± 1.0` points to `greedy`'s side at 8,008 games a cell** — not separated, and the point estimate pointing the *wrong way*; `cautious` is `+0.8 ± 1.0` ahead of it. P20 acceptance 1 asked for a null to be publishable and it is: `docs/STRATEGY.md` §8. 🔥 **The finding is *why*, and it narrows P21 to one idea.** The memory demonstrably works — a test shows the supply estimate falling below `cautious`'s for every card watched go by and holding at the full two copies for every value never shown — but it cannot pay, for two reasons that are now measured rather than argued. **(1) The information set is tiny**: under the cautious default it runs **12 → 23 cards across a whole round out of 108**, about ten cards learned beyond its own hand, one a turn. **(2) It enters where nothing is paid**: it sharpens `ThreatScore`, which *is* `cautious`'s tie-break, and P17 measured that tie-break at `−0.2 ± 1.0`. ⚠️ **A sharper input to a decision rule already shown not to matter is worth nothing, and the two nulls compound rather than add** — so P21 must change *which question is asked*, which is what P15's "has to be combinatorial" always meant. **Two of three research rungs have now returned nothing.** ⚠️ **Two things not in the build list.** (1) **`ThreatScore` extracted from `CautiousBotAgent` unchanged** — the two rungs differ in exactly one thing, a `Supply` delegate, and two copies of that arithmetic would be two places for it to drift; P15's "one change against the rung below" is a claim about code before it is one about results. (2) 🔥 **The ladder was written out in three places and a fifth rung made all three wrong at once**: `tournament` and `suite` both defaulted `--strategies` to a hand-typed `random,simple,greedy,cautious`, so a new rung was measured only when somebody remembered to name it. **The default is now `BotCatalog` itself** — P18's defect one layer up, in a front end nobody thinks of as one, and it discharges half of P23's standing-set caveat. ✅ **The rules question was not decided in code**: RULES.md §9 #15 — *is a discard pile inspectable, or only its top card?* — stays open at **rev 14**, `QUESTIONS-FOR-MYA-LAY.md` carries it flat, and the bot counts only what it has been shown, **wrong in the direction that does not cheat**. ⚠️ If the answer comes back that the piles may be read, the rung gets a far larger information set and deserves re-measuring before it is written off — the 12 → 23 figure is exactly what the cautious default costs. ✅ **Acceptance 5, throughput: the memory is free** — **77 rounds/s** against `cautious`'s 76 and `greedy`'s 88 (P12's baseline: 51 serial, 85–92 parallel), nowhere near a budget. ⚠️ **Acceptance 3's round-boundary test is asserted on the *size* of the memory, not on ids across rounds**, because a `CardId` names a card in a *round's* shoe (P13.4) — so a memory that survived a deal would not be stale, it would be a memory of cards that no longer exist, and the same deal is replayed twice so that a surviving memory looks *plausible* rather than crashing. ⚠️ **`docs/strategy/measurements.csv` regenerated: 52 measurements in 35 minutes**, and the free-for-all and mean-margin columns all moved — five strategies crossed over four seats is a different field from four. **The head-to-head margins did not**, and every one reproduced to the digit; STRATEGY.md §4 now says which column to distrust when a rung is added. Amended BUILD-PLAN **P20 (done, with the finding), P21 (measure against `greedy` only — `counting` is not a distinct reference, and the suite is now k(k−1)/2 cells) and P23 (the standing-set caveat is half-discharged; make the default a test)**. |
| 2026-08-19 | P19 | ☑ Done. **Difficulty as a dial — four levels, one mistake rate each, and every step measured.** `Domain/Agents/DifficultyLadder.cs` holds `easy`, `medium`, `hard`, `expert`; each is `BotCatalog.Hardest` wrapped in a `FallibleAgent` that, with probability ε, throws the card that rung ranked **second** rather than first. `IRanksDiscards` is what makes that a *plausible* mistake rather than a random one (§3.12 item 3 — a bot that threw jokers away would read as broken, not weak), and `CoverScore.Discard` is now **defined as the head of `CoverScore.Ranking`** so the winner and the runner-up cannot drift apart. Both front ends offer levels only; the ladder stays the research instrument (§3.12). **506 tests, up from 477.** 🔥 **The calibration is the packet, and ε is violently non-linear**: `greedy@0` vs `greedy@1` is **+33.3 ± 1.6** points head to head — three times the whole `simple`-to-`greedy` gulf — with ε = 0→0.5 worth about 8 points and ε = 0.5→1 worth about 17. **So the four are spaced evenly in *results* and not in ε**: 0.9, 0.7, 0.5, 0.0, measuring **14.50 / 22.17 / 27.22 / 36.11** at the reference table and **+10.79 ± 1.00, +5.69 ± 1.01, +9.90 ± 1.00** head to head at 8,008 games a step — **all three separated under Holm**, the narrowest 5.7× the half-width. **Four levels rather than three, and the count is a measurement rather than a taste.** ⚠️ **`--pairs adjacent` had to be built and was not in the plan's build list**: acceptance 1b wants the family to be the k−1 steps, so `TournamentOptions.Pairs` decides which pairs are *played* as well as corrected — and 🔥 it exposed a latent assumption, `PairingChecks` reading the *field* rather than the *cells played* to pick its across-cell comparison, which threw on any run where most pairs never meet. 🔥 **Second finding, and it is P13.1's arriving in the test project: a `TurnContext`'s hand is the engine's own list**, so keeping a context and asking the rung for its ranking afterwards asks about the **thirteen** kept rather than the fourteen it chose from — `RecordingAgent` records the ranking during the turn now, and this was found by writing the test. ✅ **Acceptance 3 verified end to end**: `expert` at ε = 0 plays the match `greedy` plays, **7,371 bytes (`bots`) and 90,726 (`human`) identical from the `Seating:` line on**, captured from `HEAD` and from this tree at `--pick 0`. ⚠️ **The `--pick` map changed** — `0` expert, `1` hard, `2` medium, `3` easy. ⚠️ **`sim suite` gained a second standing check** and exits non-zero if the dial stops being monotone, beside the null test; the regenerated suite is **41 measurements, ~120,000 games, 34 minutes**, and every ladder figure reproduced P17's exactly. ⚠️ A computer seat in the browser is now named for how it plays (`Mya Lay (expert)`), because a mixed table nobody can tell apart is a mix nobody asks for twice; the console's names are untouched, which is what keeps the byte-comparison meaningful. ⚠️ **Three defects found only by opening the lobby in Chrome and pressing the buttons**: the new checkbox row inherited the form's ten-rem label column and wrapped onto three lines; 🔥 **`--mixed` without a value is silently ignored** by the command-line configuration provider, so it is `--mixed true` like `--hints false`, found by reading the seat names and seeing four `expert`s; and a seat name is ellipsised at the panel's width, which `Aung Aung (medium)` now reaches — the full name went onto a `title`. |
| 2026-08-19 | P18 | ☑ Done. **One catalog — a bot is named in one place, and every front end resolves the name.** `BurmesePoker.Domain/Agents/BotCatalog.cs` holds the four rungs as `BotRung`s (name, a line written for somebody choosing an opponent, a `Strength`, a factory taking a seat seed); `Sim.StrategyCatalog` is an adapter over it, the console's private two-value `Difficulty` enum is **deleted**, `ComputerAdvice` and `TableOptions.StandIn` ask it for the hardest rung, and the browser — which had **no difficulty setting at all**, just two hard-coded `new GreedyBotAgent()`s — gained `--difficulty`, `TablePlan.Difficulty` and **the whole ladder on the lobby's open-a-table form**. **477 tests, up from 461.** 🔥 **The finding: the console's difficulty default had been the *easy* bot since P10.** A `SelectionPrompt<T>` opens on `default(T)` when that value is one of the choices, and the enum was `Easy = 0` — so the list read *Hard* first, read as though hard were the default, and handed `SimpleBotAgent` to everybody who pressed return. **Found because the refactor's byte-comparison failed**: the pre- and post-P18 captures played different games at the same seed, and the journal header — which now writes the catalog name — is what finally said which bot was in the seat. Confirmed rather than guessed with a probe prompt offering `(7, 0, 5)`, which comes back `0`. A rung is a reference type, so `default` is null and the cursor stays on the first entry. ⚠️ **P19 inherits the bug the moment it makes a level a value type.** 🔥 **Second finding: a record's `with` does not re-run a property initialiser.** `BotRung.Name` refuses a `#` because the tournament's null cell labels a copy `"{name}#mirror"` and a front end offering `greedy#mirror` as an opponent would be absurd — but validating in an initialiser refused it in a constructor and *allowed* it in `rung with { Name = … }`, which is exactly how `Tournament` makes that copy. The check moved into the `init` accessor; **found by writing the test, not by reading the code.** ⚠️ **Acceptance 2 was amended twice**: the whole capture cannot be byte-identical because acceptance 1 changes the prompt, so the comparison is of the match — **6,625 bytes (`bots`) and 89,233 (`human`), identical from the `Seating:` line on** — and `--pick n` was added to `drive-console.py` so a capture names the rung it played. ⚠️ The difficulty question is the **third** prompt with `--script bots` and the **fourth** with `--script human` (a person is asked their name first), which made a `--pick` that worked for one silently do nothing for the other. Acceptance 4's grep test went into `LayeringTests` rather than `MarkupStandardsTests` — it is a claim about seven projects, not about markup — and it bans constructing a **rung**, with the types taken from the catalog so a fifth one is covered the day it is added. Verified end to end in the real browser: the house table opened with `--difficulty simple` says so, and a table opened from the form on `random` deals `random`. |
| 2026-08-19 | P17 | ☑ Done. **The tournament — and the first honest interval this harness has printed.** `BurmesePoker.Sim -- tournament` plays every unordered pair head to head at every seating in which both are at the table, reports the fully crossed free-for-all beside it (P16's lesson that they answer different questions), ranks the field by mean margin, and puts all six comparisons through **Holm–Bonferroni**; `-- suite` generates **`docs/strategy/measurements.csv`**, and **`docs/STRATEGY.md` is created and quotes that file rather than a session's console** (§3.12). **Two independent 15-minute, ~64,000-game runs of the suite wrote a byte-identical file.** 🔥 **The finding that changed the code was a number that should not have moved:** built as the packet literally said — "a mean over games, one value per game" — the balanced headline came back **30.6/19.3** where P16 published **29.6/20.4**. Neither is wrong; they are **two different estimands**. A strategy holds a different number of seats in different games of a crossed run, so the unweighted average of per-game *ratios* over-weights the games it held fewest seats in — which, for a strong strategy, are the games it does best in. ⚠️ **The gap was 1.05 points, the size of P16's whole seating effect**, and adding an interval to a figure must not change the figure (§3.8 item 4). So `GameValue` carries **a total and the trials it is out of**, and `Measurement.Of` is the **ratio estimator** — totals divided, standard error from the per-game residual `total − ratio × trials`, the game still the trial, and *exactly* the per-game mean when the denominator is constant. The balanced headline now reads **29.75 ± 0.47 / 20.25 ± 0.47**. 🔥 **The second finding: "paired is narrower" is half backwards.** Across cells (one strategy, two tables, same shoes) pairing narrows — ratios **0.57–0.95**. Within a cell (two strategies, one table) **exactly one seat declares**, the series are strongly negatively correlated, and pairing **widens** — measured **1.408 / 1.409 / 1.414**, √2 to three digits — so the add-the-variances formula is **anti**-conservative on a head-to-head margin by 41%. P17 acceptance 3 amended to say so. **The measured ladder at 8,008 games a cell:** `random` loses by 49.7–49.9 ± 0.4; `simple` loses to `greedy` by **11.2 ± 1.0** and to `cautious` by **10.8 ± 1.0**; 🔥 **`greedy` vs `cautious` is `−0.2 ± 1.0`, p = 0.70 — P15's negative result confirmed under a design P15 never ran.** Free-for-all (8,192 games, 256 seatings): 0.1/27.3/35.8/36.5 ± 0.9. ⚠️ `cautious` leads `greedy` in one table and trails it in the other, both inside the interval — **exactly what the correction exists to stop a reader promoting to a rung.** ✅ **The null test passes**: `cautious` against a copy of itself is 25.1 ± 0.5 / 24.9 ± 0.5 against a fair 25.0%, margin +0.3 ± 1.0, each label in each seat exactly 4,004 times — and `sim suite` exits non-zero if it ever fails. **Resolution stated for P19–P22: 1.02 points at 8,000 games a cell; half a point costs ~34,000, four hours** — so a rung worth less than a point is not promotable at the default size. Seven new files in Sim (`GameValue`, `Normal`, `Holm`, `StrategySeries`, `Tournament`, `TournamentCsv`, `Suite`), plus `SeatingPlan.HeadToHead` and a rewritten `Measurement`; **`BurmesePoker.Domain`, `Simulator`, `GameRunner` and `Replay` are all unchanged.** Build clean, **461 passed / 0 failed** (23 new: 9 in `Sim/StatisticsTests`, 10 in `Sim/TournamentTests`, 4 in `Sim/SuiteTests`); ⚠️ the suite now takes **~60 s** against 18 s, because the tournament tests play real simulations. ⚠️ **One display defect fixed in passing** — a .NET two-section format picks its section from the **rounded** value, so `-0.04` under `"+0.0;-0.0"` prints `-+0.0`; `ReportNeighbours` had it in every signed column and signed figures are built by hand now. No new rules question — measurement is not a rule, so `RULES.md` stays at **rev 14**. Amended BUILD-PLAN §0, P17 (acceptance 3 and a "What P17 found" section), **P18 (the mirror label is not a rung), P19 (the family is the adjacent pairs, and the spacing has a 1-point floor), P20 (the promotion bar and its price) and P23 (STRATEGY.md now exists; what it still owes)**. |
| 2026-08-19 | — (planning) | 🔥 **Goal 5 stated and planned: a designed difficulty system and a settled answer to what works.** No packet executed and no code changed — the deliverable is the plan. Added **§3.12** (*difficulty is a dial, skill is a ladder, and they are not the same axis*), **§0's fifth goal** with the three architecture rows it demands, **seven packets P17–P23** in §5, a second branch on the §4 graph, and **five risk rows** in §7. Baseline verified first: build clean, **438 passed / 0 failed**. 🔥 **Two findings drove the ordering.** (1) A balanced 2,048-game run gave `random` 0.0% / `simple` 26.7% / `greedy` 36.1% / `cautious` 36.8% — **and the ordinary report prints no interval at all**, so P17 (statistics) precedes P19 (calibration); `Measurement` already exists and is reachable from one verb only. (2) **Four independent notions of *which bot*** across Sim, Console, Web and Server, and **the browser has no difficulty setting** — so P18 unifies the catalog before any new rung is written. ⚠️ **P19 finishes the difficulty product with today's rungs**; P20–P22 are droppable in preference order, which is P15's +0.5 ± 0.55 lesson applied in advance. ⚠️ **P20 will need a rules answer** — whether a discard pile is inspectable or only its top card (`RULES.md` §9 #9 stops being moot once a bot counts cards); the safe default is *only what this seat has been shown*, which errs towards a weak bot rather than a cheating one. |
| 2026-08-19 | P13.6 | ☑ Done. **The lobby, a second person, and §0's goal 4 — the plan is finished.** `Lobby` (singleton) holds `HostedTable`s by id, opened from a `TablePlan`; `Pages/Tables.razor` is the lobby at `/` (static SSR, two real forms) and `/table/{Id}` names one table. **`TableHost` is gone.** 🔥 **A `SeatBoard` belongs to a viewer**: `TableView` sits down, holds it, hands it to `YourSeat` as a parameter and stands up in `Dispose` — the one thing P13.4 said had to change rather than be added to. **`TableSession` learned who is sitting where** (`SitDown`, `StandUp`, `RemoteSeats`, `WaitingFor`, `IsFull`, `SeatTaken`/`SeatLeft`), because two viewers handed one `SeatConnection` are two people answering one question and a seat is a property of a table. 🔥 **The table deals while somebody is at it** — a viewer attending *and* every seat accounted for — which is P13.4's leftover, answerable at last, **without shortening the patience**. **Acceptance met:** `TwoPeopleTests.TwoPeopleAndTwoBotsPlayARound` settles round 1 with both people asked their own questions, hands pairwise disjoint a round at a time, banks summing to zero; **and it was played for real in two browser tabs**, Nick and Mya Lay, a whole round each answering their own prompts with `Tab`/`Enter`. §3.11 C16 finished: the felt marks a seat the computer is standing in at, and a reload takes your own seat back off your own ghost. **18 new tests, 438 passed / 0 failed, 0 warnings; five mutations applied, five red.** 🔥 **Findings: a test that a stood-up seat refuses is vacuous unless a question is standing — found by mutating `Dispose` and watching it stay green; two `<AntiforgeryToken />`s are worse than none and the page renders perfectly either way, found by pressing the button; a marker that means "away" has to stop meaning it, so standing-in is per turn and cleared at the next `TurnBegan`; the sit-down form must not hide itself when the table is full, because the person locked out is the one who just reloaded; a claim on a table must not be made while prerendering, and there are two of them now; a parameter to an interactive root is serialised, so the page passes an id; a settlement is not a resting state; and `FocusOnNavigate` beats §3.11 B7 on a reconnect, deliberately left alone.** |
| 2026-08-19 | P13.4 | ☑ Done. **A seat you can play — solo browser play, complete, and a legitimate stopping point.** `BurmesePoker.Web` gains `SeatBoard` (your seat, folded out of **your own** `SeatConnection`: the question standing, the hand it is about, and the hand you kept between turns) and three components — `YourSeat`, `TurnPrompt`, `HandPanel` — **all inside P13.3's single interactive island**, so §3.11 C12 is untouched. `TableHost` seats you: `--seat` (1 by default, 0 to only watch), `--name`, `--hints`, `--patience`. **All three acceptance criteria met.** ✅ **1:** a match played from the seats themselves — three rounds, four connected seats, banks carrying over and summing to zero, asserted in `SeatBoardTests` through `ClickingPlayer` (the browser's `ScriptedSeat`, which drives a `SeatBoard` so everything it sees is something a page would draw); and five rounds played for real in headless Chromium. ✅ **2 — a round played end to end with no pointer at all:** 86 questions answered with `Tab` and `Enter` over five rounds, **393 tab presses walking the hand**, and focus was already on a `<button>` at every prompt before a key was pressed (§3.11 B7); a second run on the final build played three rounds, **went out from the seat**, and so exercised all four branches of `TurnPrompt` in a browser. ✅ **3:** `NoSeatEverHeldACardFromAnotherSeatsHand` asserts every hand that was ever on any of four pages pairwise disjoint **a round at a time**, and `MarkupStandardsTests.NoComponentFindsASecondRouteToTheTable` forbids any component naming `TableSession`, `MatchEngine`, `TableState`, `TurnContext`, `PartialCover`, `HandEvaluator` or `ConnectionFor`. **§3.11 B6, B7 and B11 shipped**, and **A4 stopped passing vacuously** — it counts what it scanned now (eight handlers). **18 new tests, 389 passed / 0 failed**, and **eight mutations applied, eight red.** 🔥 **Findings: a `CardId` names a card in a *round's* shoe, which is rebuilt every deal, so hands are compared across seats a round at a time; your hand between turns is not stale and is worth rebuilding, with ownership read back off the `CardView`s you were sent rather than recomputed; a refusal must not raise "something changed", or a client answering on the change event answers the same refused question for ever; and only the first control may capture a `@ref`, because Blazor captures on insertion and not on every diff.** ⚠️ **A seated table no longer deals from boot** — an unanswered seat spends its whole patience on every question — and `TableHost.Yours` is one `SeatBoard` shared by every circuit, which P13.5 must change. 🔥 **And one thing only reading a control's accessible name out of the browser found: `CardChip`'s hidden words needed a full stop.** A card sits next to whatever else is said about it, and without one a screen reader ran the two together into *"five of clubs melds nothing"* — a sentence that means the opposite of itself. `docs/PLAYING.md` gained a browser section; `BUILD-PLAN.md` P13.4, P13.5, §2 and §3.11 (A4, B6, B7, B11) amended. |
| 2026-08-19 | P13.3 | ☑ Done. **The first UI in this project's history — a seventh project, and a browser table you can watch.** `BurmesePoker.Web` (Blazor Server; Domain + Presentation + Server): `TableHost` (one table, dealing itself round after round, one watcher connection, idempotent `Start`), `TableBoard` (the public game folded out of `TableEvent`s and **nothing else**), `CardWords` (a card said out loud, for a screen reader), and eleven components — a static SSR shell and rules page, and one interactive island: `TableView`, `SeatPanel`, `CardChip`, `RoundLogPanel`, `SettlementPanel`, each with its own `.razor.css`. **All three acceptance criteria met.** ✅ **1:** bot-only rounds play out in a browser start to settlement — verified in headless Chromium over the DevTools protocol, which watched the page go from Round 8 to Round 10 with no reload, no console errors and no reconnect modal, and screenshotted both themes at a settlement. ✅ **2 — the rest of the §3.11 A list shipped as tests:** `PaletteContrastTests` (A3 — computed contrast in **both themes**, read from `wwwroot/theme.css`, **pairs discovered by naming convention** rather than listed, plus a check that every `var(--…)` names a declared token), `MarkupStandardsTests` (A4 real controls, C12 no render mode at the root, C14 `@key`, C15 `InvokeAsync`, B8 one polite live region, and no `StateHasChanged` in a `Dispose`), `ComponentDisposalTests` (A5 — reflection over every `ComponentBase` for a table member, private ones included, plus that the subscription is actually unhooked). ✅ **3 — the B list reviewed by driving it:** tab order walked with `Input.dispatchKeyEvent` (skip link → nav, every stop visible and outlined 3px), the log made keyboard-reachable and named, `prefers-reduced-motion` and `prefers-color-scheme` honoured, 24px minimum targets, and every card carrying words as well as a glyph. 🔥 **Finding 1: `UseStaticFiles` does not serve the framework's own files.** `_framework/blazor.web.js` 404'd and the page looked perfect — **a prerendered Blazor Server page is a photograph of a broken one.** `MapStaticAssets`, and `launchSettings.json` because a build's endpoints manifest is a Development one. 🔥 **Finding 2: a trimmed log must not key on the length of the log** — `TableBoard.Narrated` counts every line ever said; `Log.Count` repeats the moment it trims, and a repeated `@key` is Blazor reusing the wrong DOM node. 🔥 **Finding 3: 240 visually-hidden spans made the document 10,295px tall** for a body of 1,814 — the absolute-positioning recipe needs a positioned ancestor. **Measured, not seen.** 🔥 **Finding 4: a source scan must read markup, not the prose about it** — four of six scans failed on comments in the files obeying the standard; and a `@key` check that looks *nearby* let a nested key cover for a missing one. **Eleven mutations applied, eleven red.** ⚠️ **`PacedAgent` moved to Presentation, not Domain** — Sim references Domain and must not be able to reach a sleep; **console byte-identical after the move** (two pty captures, two seeds, `cmp`). ⚠️ **`BurmesePoker.Tests` now references `BurmesePoker.Web`** and still never the console: the rule is that nothing is tested *through* a front end, and a component tree is data. **Build clean and warning-free, 371 passed / 0 failed** (29 new). **Domain, Server and Sim untouched** — the engine now stands unaltered across five consecutive packets. No new rules question — `RULES.md` stays at **rev 13**. Amended BUILD-PLAN §2, §3.11 (A3, A4, A5, B8, C12–C15, C17 all marked), P13.3 (a "What P13.3 found" section) and **P13.4 and P13.5, each of which inherits something now built.** |
| 2026-08-19 | P13.2 | ☑ Done. **The table server — a sixth project, and no transport anywhere in it.** `BurmesePoker.Server` (Domain + Presentation): `TableSession` (one hosted table: seats, connections, rounds, banks, its own seed), `TableSeat`/`TableOptions`, `SeatConnection` (a mailbox — the events a connection may hear and the question it is being asked), `SeatPrompt`/`SeatQuestion`/`SeatAnswer`, `TableFanOut` (the `IGameObserver` that applies the concealment), `TableEvent` (narration as a closed record hierarchy), `RemotePlayerAgent` (blocks on the connection, stands in with a bot when nobody answers), `BoundedAgent`/`TableClock`/`TableAbandonedException`. **All three acceptance criteria met.** ✅ **1:** two scripted remote seats and two `GreedyBotAgent`s play a full round through the server's own plumbing, in a test, with no sockets. ✅ **2 — the packet's most important test:** `ConcealmentTests` plays one round with **four connected seats and a watcher** and asserts pairwise-disjoint hands, no seat told what anybody else drew blind, a sweep over every card in every event against what that seat may see, and a watcher sent nothing but the public game. ⚠️ **Mutation-tested rather than assumed** — broadcasting the drawn card to everyone turns **three of the five red**. ✅ **3:** a seat that stops answering is played by the computer, the round finishes, and the takeover is broadcast (once a turn, keyed on `(Round, TurnNumber)` exactly as P11's pacing decorator is); a player who misses one prompt and comes back for the next one is simply back. 🔥 **Finding 1: exactly one event in the whole narration is private — the blind draw.** A discard, a taken discard, a claimed money card and a declaration all happen in front of the table, so the security boundary is one `if` and everything else in `TableFanOut` is care about lists. 🔥 **Finding 2: P13.1's snapshot rule generalised and caught two more live lists** — `TableState.TurnedUpOnTable` is aliased by *both* `TurnContext.TurnedUpMoneyCards` and `IGameObserver.RoundStarted`, and the opening player's claim removes a card from it; `EverythingASeatWasSentStaysWhatItWasWhenItWasSent` forces the claim and fails if either copy is removed. 🔥 **Finding 3: answering inside `SeatConnection.Updated` makes a whole round deterministic** — the event is raised on the round's thread before the seat waits, so eighteen tests that play real rounds cost four seconds and no flakiness; one test drives the genuinely cross-thread path because that is the shape a circuit has. ⚠️ **A client bug must not end a round:** `SeatConnection.Answer` **refuses** an answer that does not fit the question or names a card the seat is not holding — returns false, the prompt stands — where the engine would throw. ⚠️ **Deliberate deviation, and P13.3 inherits it: the stand-in is not paced.** A sleep belongs to whatever draws the table, so `TableOptions.StandIn` is a factory; **`PacedAgent` lives in `BurmesePoker.Console` and must move (Domain's `Agents/` is the obvious home) rather than be reached by a reference.** **The wall clock bounds the table, and every seat is wrapped including the bots** — what goes wrong at a hosted table is the people, not the play; the round is announced abandoned before the exception leaves and the session survives it. **Build clean and warning-free, 342 passed / 0 failed** (18 new: 12 `TableSessionTests`, 5 `ConcealmentTests`, 1 new `LayeringTests` row forbidding Spectre, ASP.NET, `System.Console` and `System.Net` in the server). **Domain, Presentation, Console and Sim were not touched at all** — the only edits outside the new project are one line of `BurmesePoker.slnx`, one `ProjectReference` and the layering row. No new rules question — `RULES.md` stays at **rev 13**, unchanged across six packets. Amended BUILD-PLAN §2, §3.10 (items 2 and 4 cashed), §3.11 A1, P13.2 (a "What P13.2 found" section) and **P13.3 and P13.4 — each inherits something already built, and P13.4's leak acceptance is narrowed because P13.2 already covers the seated case.** |
| 2026-08-19 | P13.1 | ☑ Done. **A presentation view model — a fifth project, and the first presentation code this project can test.** `BurmesePoker.Presentation` (Domain only): `CardDisplayState` (flags, not colours), `DisplayTokens` (a non-colour token for every one of them — §3.11 **A2, shipped a packet early**), `CardOrder` (the display sort, moved out of the console and now proved *total*), `CardView`, `MeldView`, `HandView` and `ComputerAdvice`. The console was rewritten *onto* it: its own `HandView` is gone, replaced by `HandPanel`, which draws and decides nothing; `CardFormatting` takes its ordering and its glyphs from Presentation, and `Palette.OwnedMark`/`AdviceMark` are now aliases of `DisplayTokens`, so the two front ends cannot drift to two stars. Secondary item done: the console takes `IAnsiConsole` everywhere and reaches for the static one **exactly once**, in `Main`. 🔥 **The test caught a real defect on its first run: a view model that aliases the engine's list is not a view model.** `TurnContext.Hand` is the seat's *own live list* and `RoundEngine` discards from it as soon as the answer comes back, so a view built at the discard reported fourteen cards and then held thirteen. The console never showed it — it renders and drops the view in one breath — and **a Blazor component holding a view across a render is exactly the case that does.** `HandView.Of` now copies; **P13.2 inherits the rule for everything the fan-out hands a seat.** ⚠️ **Two copies of the same card cost the same to throw, and that is correct** — a spare is a standing replacement for the one the cover used — so a browser must not imply otherwise; what makes them two cards is the `CardId` key, not the price. ⚠️ **The `IAnsiConsole` benefit is still unreachable and should not be chased**: "drivable from a test" cannot follow while §2 forbids the test project referencing `BurmesePoker.Console`, and that rule is worth more. **So the pty is the console's verification, and `scripts/drive-console.py` is checked in to make it repeatable.** Build clean and warning-free, **324 passed / 0 failed** (30 new: 13 `HandViewTests`, 6 `DisplayTokensTests`, 5 `CardOrderTests`, 4 `ComputerAdviceTests`, 2 new `LayeringTests` rows). **Acceptance 3 met the hard way: five scripted pty matches across three seeds, one under `--no-hints`, every one `cmp`-identical to the same match at `HEAD`.** No new rules question — `RULES.md` stays at **rev 13**, unchanged across five packets. Amended BUILD-PLAN §2, §3.11 A2, P13.1 (a "What P13.1 found" section) and **P13.2, P13.3 and P13.4 — each inherits something already built or already closed.** |
| 2026-08-19 | — | **P13 re-planned; the browser UI and multiplayer are one track** (docs only, no code — still **294 passed / 0 failed**, `RULES.md` at rev 13). Nick asked whether a rich JS browser UI or multiplayer made more sense first, and whether they were related. **They are related through exactly one variable — where the engine runs — and concealment decides it in advance.** `BUILD-PLAN.md` **§3.10**: a hand is fully concealed with money on it, so an engine in the browser holds every player's hand in every player's client; that is a security property, not a courtesy, and it is not retrofittable. **Engine server-side, Blazor Server, no WASM engine ever**; a browser client is a `RemotePlayerAgent` (§3.6 finally cashed), and **solo browser play is multiplayer with one connection** — there is no single-player client to build and replace. ✅ **The framework choice buys one concrete thing: P13.2 has no protocol to design**, so the "server" is a seat and a fan-out, testable in-process with no sockets. **§3.11** is new too — **seventeen UX standards taken before a single component exists**, split by how each is *checked*: **five are mechanical tests** (the concealment leak; colour never alone; contrast computed in both themes; real controls, no clickable divs; subscriptions disposed — and never `StateHasChanged` in `Dispose`) and land in P13.2/P13.3 before anything can be retrofitted; the rest are reviewed by playing, as P11 was. ⚠️ **The irreversible item is C12** — static SSR by default, `@rendermode InteractiveServer` per component and never at the root. ⚠️ **P13.1 was replaced rather than renumbered:** the old one ("lift the front end off `AnsiConsole`") was right for a second *Spectre* front end and a Razor client is not one — the cut line is **decision versus drawing**, so P13.1 is now an extraction into a fifth project, **`BurmesePoker.Presentation`** (Domain only, no rendering technology, one new `LayeringTests` row). Alternatives to the fifth project are recorded in §2 so it can be argued with. **P13 re-split three ways → five, strictly sequential**, each ending green and more playable than the last: P13.1 view model → P13.2 table server → **P13.3 a table you can watch** (first UI) → **P13.4 a seat you can play** (solo browser play, a legitimate stopping point) → P13.5 the lobby. Amended BUILD-PLAN §0, §2, §3.10 (new), §3.11 (new), §4 (graph + six table rows) and §7 (three new risk rows, and the scope-growth row updated for the longest sequential chain in the plan). |
| 2026-08-19 | P16 | ☑ Done. **Does the player before you decide your game? No — not between players who are both thinking.** 🔥 **The answer, with an interval and a control: upstream skill is worth `+9.1 ± 2.1` points of win rate across the `random`-to-`greedy` gulf and `−1.0 ± 2.1` across the `simple`-to-`greedy` gap.** A weaker player *anywhere* at the table is worth 4–5 points to you; **which side of you they sit on is worth nothing** unless they are not really playing. ⚠️ **The packet had to fix the harness before it could ask the question**: `SimulationOptions.Seating` rotates one pattern, so *(my strategy, the one feeding me)* was **perfectly confounded** — the cell was never played, at any run size. `SimulationOptions.Assignments` (opt-in; null keeps the rotation) plus `SeatingPlan.{Balanced, Rotations}` fixes it, and `CsvReport` gained **`upstream_strategy` and `downstream_strategy`**, derived from the *seating* and never from the options — which is why P14's replay identity survived untouched (one test, `JournalReplayTests.Outcomes`, had to blank three name fields instead of one). **The design:** focal `greedy`, filler `simple`, the ladder in one varied seat, **two arms — varied *before* the focal seat and varied *after* it, seating the identical four strategies** — 4,000 games a cell, 8 cells, two seeds, **64,000 games**; each cell cycles the four rotations of its pattern so the focal seat opens exactly a quarter of the time. ⚠️ **The downstream control moved the answer by a factor of two**: the gross upstream effect of `random` is +19.4 points and the same swap downstream is +10.3, so without the second arm the packet would have reported 19.4 and been wrong. 🔥 **It also corrected P15's own headline**: the 5.4-point ladder gap is a *neighbour* effect, not an *upstream* one — a rotation moves both neighbours at once. ⚠️ **The mechanism barely moves**: across a contrast worth 9.1 points of win rate, `takes` moves **1.6 ± 0.9** — so upstream skill changes *what* is offered, not *how often* something worth taking is, and a rich journal is what would say more. **The intervention, predicted in advance by P15, came back null**: `cautious` upstream costs the focal seat `−0.3 ± 1.4` points and costs `cautious` nothing (31.8% against greedy's 31.3% in the same seat). **P12's headline re-run at 8,000 games: 30.7%/19.3% rotated — reproduced exactly — against 29.6%/20.4% balanced, so the rotation flatters greedy by 1.1 points a seat, about a fifth of the gap.** Four new files in Sim (`SeatingPlan`, `Measurement`, `NeighbourExperiment`, `NeighbourCsv`), a `neighbours` verb and a `--seating balanced` flag; **`BurmesePoker.Domain` unchanged, and so are `Simulator`, `GameRunner` and `Replay`.** Build clean, **294 passed / 0 failed** (16 new: 9 in `Sim/NeighbourExperimentTests` — one of them the interval arithmetic itself — 6 in `Sim/SeatingPlanTests`, and 1 in `Sim/SimulationTests` for the two new columns). No new rules question — who you sit next to is not a rule, so `RULES.md` stays at **rev 13**. Amended BUILD-PLAN §0, §4, P12's caveat, P16 (a "What P16 found" section) and §7 (the seating-artifact risk retired, with its size measured). |
| 2026-08-19 | P15 | ☑ Done. **A skill ladder — four rungs, and only three skill levels.** Domain gained `Agents/RandomBotAgent` (the floor: legal moves, no thought, ⚠️ **a `Random` handed in and never `Random.Shared`** — `SeedSequence.SeatSeed(gameSeed, seat)` derives it, so a run is still a pure function of its master seed and two random seats do not play in lockstep) and `Agents/CautiousBotAgent` (greedy, plus a last-resort tie-break towards the card least useful to whoever picks it up). `CoverScore` grew the **shared discard loop** every rung throws through, so simple/greedy/cautious now differ in *one function argument* and nothing else — a refactor verified by the 259-test baseline staying byte-identical. `Strategy.Create` became `Func<int, IPlayerAgent>`; `StrategyCatalog` is now `random, simple, greedy, cautious` **in ladder order**. **Sim gained no file.** ⚠️ **The headline is a negative result, and it is the useful part: `cautious` is not distinguishable from `greedy` — +0.48 ± 0.55 points over 32,000 head-to-head games across two seeds.** Head to head at four seats: random 0.1% vs greedy 49.9%; simple 18.5% vs greedy 31.5% (a confirmation of P12's 30.7/19.3 at twice the games); simple 18.4% vs cautious 31.6%. **Why: denial and self-interest coincide.** The partners a hand holds are exactly the ones an opponent cannot hold, so both natural ways of measuring "least use to them" reduce to `Supply(rank) − Potential` to within a point — and ⚠️ **every *pairwise-additive* tie-break is greedy again**, because partnership is symmetric. A rung above greedy has to be combinatorial (live outs), which costs ~100× a decision — a packet, not a tie-break. 🔥 **And an accident worth more than the packet: in the four-way run the rungs came out random 0.1%, simple 27.9%, cautious 33.3%, greedy 38.7% — 5.4 points between two strategies that are level head to head, and the rotation feeds greedy from simple and cautious from greedy in every game. All of it is who fed whom.** Build clean, **278 passed / 0 failed** (19 new: 10 in `Agents/SkillLadderTests`, 9 in `Sim/SkillLadderRunTests`). ⚠️ **Added `WallClockBudgets.cs`** — the two timing-budget tests began failing on a *quiet* machine because the new ladder tests run simulations beside them, so the heavy classes and the budgets now share one xunit collection and never run concurrently; **neither budget was loosened**. No new rules question — `RULES.md` stays at **rev 13**. Amended BUILD-PLAN §0, §4, P15 (a "What P15 found" section) and **P16 (three amendments: the lead, a much weaker intervention than it assumed, and three usable skill levels rather than four)**. |
| 2026-08-19 | P14 | ☑ Done. **Game journals — record and replay.** The tree's first persistence layer, and it is a *format* rather than a store. Domain gained `Play/{GameJournal, JournalFormat}` — pure record types plus JSON Lines in and out, `IEnumerable<string>` exactly as `CsvReport.Rows` already did — and `Agents/{JournalingAgent, JournalPlayerAgent}`, a decorator that writes down every answer and a seat that answers from a file. **`RoundEngine` and `MatchEngine` are byte-for-byte unchanged**: replaying is playing the game with different seats, so no second engine and no resumable state machine. Sim gained `Replay` (which reuses `Simulator.Summarise` and `GameRunner`'s row builder) and `JournalReport`; both front ends gained `--journal <path>` and `--fidelity thin|rich`, and the harness a `replay` verb. **The headline acceptance is a `diff`: a 20-game, 40-round journalled run and its replay produce byte-identical CSV.** ⚠️ **The console now draws two `Random`s from `--seed`** — one to seat the table, one for the match — because a journal reproduces the deal by re-seeding the match's generator, and the old single generator had the seating consuming from it first; **a pre-P14 `--seed` no longer plays the same console match**, and two runs at the same seed are still byte-identical to each other. ⚠️ **Rich fidelity costs nothing measurable, which §3.9 expected to be false**: 400 games serially, three interleaved repetitions, **46–49 rounds/s with no journal, 48–49 thin, 48–50 rich** — a thirteen-`CardId` copy is tens of nanoseconds against a 140 µs cover search. **The expensive axis is bytes** (9.6 KB a round against 5.0), so rich stays opt-in for what it costs to keep. Divergence is loud three ways — journal exhausted, wrong question, card not in hand — each with a clean CLI message. Build clean, **259 passed / 0 failed** (20 new: 14 in `Play/GameJournalTests`, 6 in `Sim/JournalReplayTests`). Verified a console match through a pty and replayed it under the harness to the same two rounds. **No new rules question — `RULES.md` stays at rev 13.** Amended BUILD-PLAN §0, §2, §3.9, §4, P14, **P15 (a new acceptance: every rung journals and replays) and P16 (rich journals are now affordable; ⚠️ a new CSV column derived from `SimulationOptions` rather than from the seating would break replay identity)**. |
| 2026-08-19 | — | **Persistence answered and three packets added** (docs only, no code — still 239 passed / 0 failed). The tree has **no persistence layer**: `CsvReport.WriteTo` is its only write to disk, and that is an outcome table. `BUILD-PLAN.md` **§3.9** records why that has been fine (a bot game is a pure function of its seed — P12 proved it byte-identical) and why it stops being fine: **a person is not a function of a seed, and a seed only replays against the code that produced it.** **P14** — game journals, as record types plus a journalling decorator and a replaying agent over `IPlayerAgent`, with the format in one place and file writing left to the consumers; replay is *a seat that answers from a file*, not a resumable engine, and the rich fidelity level is opt-in because §3.7 measured this work allocation-bound. **P15** — a skill ladder, ≥4 separated strategies including a `RandomBotAgent` (⚠️ must take a seeded `Random`, never `Random.Shared`) and a `CautiousBotAgent` that throws what least helps the seat it feeds. **P16** — the upstream-neighbour hypothesis, raised by Nick's friend: *the skill of the player before you is what decides your game.* Well-posed, because `RULES.md` §5 makes a table a directed cycle; **a strategy question, not a rules question, so `RULES.md` is untouched at rev 13.** ⚠️ **The finding of the session: `SimulationOptions.Seating` cannot answer it** — it rotates one fixed pattern, so at two strategies and four seats *(me, upstream)* is perfectly confounded, every greedy fed by a simple. That also puts a caveat on P12's 30.7%-vs-19.3% headline, which P16 owns separating. Also added `docs/PLAYING.md`, a player-facing guide to solo play, and listed it in CLAUDE.md's documentation map. Amended BUILD-PLAN §3.9 (new), §4, P12, P14–P16 (new) and §7 (two risk rows). |
| 2026-08-18 | P11 | ☑ Done. **Console UX pass — the terminal is the UI, so this is the UI (§0).** Five new presentation files and **not one line of Domain or Sim changed**. `RoundLog` fixes the sorest thing in the console: the per-turn concealment clear used to destroy every public thing said while a player was away, and with bots those turns pass in milliseconds — `ConsoleObserver` now says each line *and* files the same markup, and the panel is drawn above the table and the hand. `HandView` shows a hand as the melds it nearly is plus its deadwood, off one `PartialCover.Best` call, and prices every card by `covered(13) − covered(12)`. **The discard hint is `GreedyBotAgent`'s own answer**, asked of the very `TurnContext` in hand, so it cannot drift from how the table actually plays; `--no-hints` turns it off. `PacedAgent` — a decorator, **deliberately not a sleep in the bot**, which would have sat inside P12's hot loop — makes computer seats wait once per `(Round, TurnNumber)`. `Palette` gathers P8's three files' worth of ad-hoc colour into one language. A difficulty prompt (`SimpleBotAgent` vs `GreedyBotAgent`) came free out of P12. **Every match is seeded and says so**: one is drawn if `--seed` is absent, and the seating is taken from the match's own `Random` — two runs at `--seed 99` are byte-identical, `--seed 100` is not. ⚠️ **Found by playing, not by building:** `Palette.Legend` shipped with an unbalanced markup tag, compiled clean, passed every test, and threw on the first hand drawn. Build clean, **239 passed / 0 failed** (4 new — `LayeringTests`, the one mechanical check a console packet allows: Domain references neither Spectre nor `System.Console`, Sim references no Spectre). Verified through a pty: **13+ rounds at four seats**, a six-seat table for the reshuffle narration, and seed 1 for the opening turn, which is the only one that offers the money-card claim. No new rules question — `RULES.md` stays at **rev 13**. Amended BUILD-PLAN §0, §2, §3.5, P11 and **P13 (split into three sub-packets, with what P11 proved about the seams)**. |
| 2026-08-18 | P12 | ☑ Done. **Simulation at scale.** A fourth project, `BurmesePoker.Sim` (Domain only): seeded games run in parallel, a strategy per seat rotated by game, per-round rows carrying their own join keys, and a CSV writer. Per-game seeds are `SplitMix64(master, index)` so a game is the same game however the run was scheduled — **serial, parallel and two-thread runs are byte-identical**. The turn cap lives in a `SeatRecorder` decorator over `IPlayerAgent` and **reports** abandonment rather than dropping it, because the domain will not invent a rule the game does not have. Domain gained only `Agents/CoverScore` (extracted, so the bots' scoring cannot drift) and **`Agents/SimpleBotAgent`** — the greedy bot with the discard tie-break removed and nothing else changed. **The measured answer: greedy takes 30.7% of 2,000 four-seat rounds against simple's 19.3%, +$1.24 a round against −$1.24** — P10's claim about the tie-break, measured. Build clean, **235 passed / 0 failed** (10 new), including determinism, per-round money conservation, a reflection pin on mutable static state, and the abandonment path. ⚠️ Measurement pass first, as the packet required: `PartialCover.Best` **140 µs**, `TryFindCover` **91 µs**, a round **~20 ms**, **51 rounds/s serial and 85–92 parallel**; **nothing was optimised**, but the work turns out to be **allocation-bound** — the server GC took eight-thread scaling from 25% to 70%. ⚠️ Two findings: **the reshuffle is a six-player phenomenon** (0/3/67 reshuffles per 300 rounds at 4/5/6 seats), and the **turn cap has never fired in a real run**. `RULES.md` → **rev 13**: §4.3's `DERIVED` 40% side-bet estimate measured at 42% over 600 five-player rounds — a confirmation, not a rule change, and no new question raised. Amended BUILD-PLAN §0, §2, §3.7, §4, P11, P12, P13 and §7 (two risk rows retired). |
| 2026-08-18 | P10 | ☑ Done. **Solo play.** `Melds/PartialCover` — the evaluator's search with one extra branch (give a card up and move on), memoised on `(position, covered)` and short-circuited on a complete cover — and `Agents/GreedyBotAgent`, whose entire strategy is *of the thirteen I would keep, how many meld?* asked of the discard, the claim and every candidate throw. `Melds/MeldIndex` extracted so both searches share one candidate index; `HandEvaluator` rewritten over it and the 208-test baseline re-run before anything else changed. **Money is absent from every decision** (RULES.md §4.4) except the take tie-break, which favours the deck because a blind draw confers ownership. `Program` asks *"how many of you are people?"* and fills the rest with named bots. Build clean, **225 passed / 0 failed** (17 new). ⚠️ **Termination measured, not assumed**: twelve seeds × 4–6 players, every round finished in 21–30 turns at ~40 ms a round. Verified the console by playing **20 rounds** against bots through a pty. No new rules question — `RULES.md` stays at rev 12. Amended BUILD-PLAN §0, §2, §3.7, P11, P12, P13, §4 and §7 (the "never-improving strategy" risk retired; the hot loop re-aimed at `PartialCover`). |
| 2026-08-18 | P9 | ☑ Done. `MatchEngine` — repeated rounds, banks carrying over, no automatic end — returning a `RoundRecord(RoundResult, TableState)` per round and keeping no history. Deck exhaustion now **reshuffles inside `RoundEngine.TakeCard`**: every discard pile gathered and shuffled into a new draw pile, the turned-up cards left alone, ownership held by whoever acquired the card first (`CardOwnership.TryRecordFromDeck`). `RoundResult.Turns` added; `IGameObserver.DiscardsReshuffled` added; `RoundEngine` now **requires** a `Random`. ⚠️ Found and fixed a real concealment bug the match loop exposed — `SpectrePlayerAgent` compared turn numbers alone and so skipped its screen clear on turn 1 of every round after the first; `TurnContext` gained `Round`. `Program` now loops rounds, asks *"another round?"* and prints standings. Build clean, **208 passed / 0 failed** (16 new, 1 removed — a passive round no longer terminates, so the old exhaustion test would hang). Verified the console by driving two full rounds through a pty. Rules defaults taken for `RULES.md` §9 #4 and #5, and new #14 raised (rev 12). Amended BUILD-PLAN §2, P10, P11 and P12. |
| 2026-08-18 | — | **Statistics added as a design constraint** (doc-only, no code). `BUILD-PLAN.md` **§3.8**: the domain gains no notion of a statistic, and everything a strategy comparison wants is derived by the consumer from three seams — the observer stream, the per-round `(RoundResult, TableState)` pair, and a **recording decorator over `IPlayerAgent`** for anything decision-level (which needs no domain change and serves human replay too). Four constraints recorded, the sharpest being that ⚠️ **P9 must surface each round's table or two of the five stat families become unreachable**. P9 also gains `Turns` on `RoundResult`. §3.5 now says the observer event set is open but **hot** — events pass what the engine holds and never allocate. P12's build list rewritten against §3.8; §0, CLAUDE.md and the `/poker` skill's stale P0 baseline exception brought current. Build clean, **192 passed / 0 failed** — unchanged. |
| 2026-08-18 | — | **Roadmap extended** (doc-only, no code). Nick named four goals beyond a playable game; written up as `BUILD-PLAN.md` **§0**, with **§3.6** (agents stay synchronous — a remote player blocks in the agent, one table is one task) and **§3.7** (simulation is a first-class consumer: determinism ✅, no I/O ✅, no mutable statics ✅, speed ⚠️ unmeasured) taken now rather than discovered later. **P10 promoted out of "optional" and rewritten** — its "never discard an owned money card" heuristic contradicted §4.4 and is corrected, bots move to `Domain/Agents/` so they are testable and reusable, and the scored partial cover P5 left unbuilt is specified here. Added **P11** (console UX), **P12** (simulation at scale), **P13** (multiplayer). §4 graph and §7 risks updated. Build clean, **192 passed / 0 failed** — unchanged, nothing was built. |
| 2026-08-18 | P8 | ☑ Done. `CardFormatting`, `SpectrePlayerAgent`, `ConsoleObserver` and a `Program` that asks for players and stakes, randomises the seating, plays a round and reports the settlement split into its round and money-card halves. Hotseat concealment: clear and hand over the keyboard once a turn; blind draws are narrated without the card. Spectre.Console 0.57.2 added back. **No Domain change, so no new tests — 192 passed / 0 failed**, build clean; verified manually by driving the real binary and a rigged winning deal through a pty. Amended BUILD-PLAN P9 (the console's settlement report needs each round's `TableState`, so `MatchEngine` must surface it; between-round "stop playing" is the console's to ask) and P10. |
| 2026-08-18 | P7 | ☑ Done. `TurnAction`, `PlayerState`, `TableState`, `TurnContext`, `RoundResult`, `RoundEngine`, `IPlayerAgent`, `IGameObserver`. A round deals from a validated draw order (so it is scriptable), turns up bottom-then-top, offers the claim on the opening turn only, records ownership on deals and blind draws alone, discards before revealing, and settles on a declaration. Build clean, **192 passed / 0 failed** (18 new), including the four acceptance tests — expected settlement, 13/14 hand sizes, 108 distinct cards at every event, and a claim that grants no ownership. Raised `RULES.md` §9 #12 and #13 (rev 11), both defaulted. Amended BUILD-PLAN §2, §3.5, P8 (what the engine asks the console) and **P9 (the reshuffle must go inside the engine — catching `Play()` cannot resume a round)**. |
| 2026-08-18 | P6 | ☑ Done. `Stakes` (sealed record, positive-only, `Standard` = $5/$1) and `Settlement.ForRound` → per-player net deltas: flat round value from every loser to the winner, then each **owned** money card paying its owner `multiplier × money card value` from every other player. Walks ownership records and is never given a hand — pinned by a reflection test on the parameter list. Resolves an owned `CardId` through the **unshuffled** shoe by index and rejects a shuffled one outright. Build clean, **174 passed / 0 failed** (26 new), including the §4.3 worked example and a 500-round zero-sum property test. Amended BUILD-PLAN §2, P7 (keep the builder list; one roster; a round always has a winner), P8 (net deltas only) and P9 (conservation is a banking test). |
| 2026-08-18 | P2 | ☑ Done. `MoneyCardRegistry` (pure function of the turned-up cards; permanent 7♦/A♠ as negative-id value designators; multiplier is permanent + turned-up, so doubling is the overlap and its own ceiling) and `CardOwnership` (append-only, write-once, no transfer/clear/remove — enforced by a reflection test). `PlayerId` brought forward into `Play/`. Build clean, **148 passed / 0 failed** (29 new). Re-planned P6: `Records` is keyed by `CardId` while `Multiplier` takes a `Card`, so settlement needs the shoe passed in — `DeckBuilder.BuildTwoDecks()` is index-aligned, `Deck.Cards` is not. Amended BUILD-PLAN §2, P2, P6 and P7. |
| 2026-08-18 | P5 | ☑ Done. `MeldCandidates.For` (runs, then the sets no run already consumes) and `HandEvaluator.IsWinning` / `TryFindCover` — backtracking pinned to the lowest uncovered card, candidates indexed by their lowest card, coverage carried as a bitmask so dead ends memoise. Build clean, **119 passed / 0 failed** (19 new). Found that the joker-substitution acceptance hand has to be built from a set rather than a run, and that `TryFindCover`'s cover is not canonical; amended BUILD-PLAN P5, P8, P10 and the §7 risk table. |
| 2026-08-18 | P4 | ☑ Done. `SetGenerator` — one walk over the four suits per rank, each taken as a held card, a specific joker, or nothing; de-duplicated by card set. Duplicate suits impossible by construction, so a set is at most four cards. Build clean, **100 passed / 0 failed** (18 new), including a brute-force cross-check over every subset. Measured the worst case at 639 candidates and amended the §7 risk row. Re-planned P5: the two generators collide on any meld with ≤1 real card, so `MeldCandidates.For` must de-duplicate across them. |
| 2026-08-18 | P3 | ☑ Done. `MeldSlot`, `Meld` (identity is its `CardId` set) and `RunGenerator` — window-based generation with joker substitution, jokers chosen as combinations. Reference hand yields the specified **5** candidates. Build clean, **82 passed / 0 failed** (22 new). Corrected two counts in `docs/spec/RUN-CANDIDATES.md` (76, not 77; 4,032, not "hundreds"), widened `RULES.md` §9 #8 to cover all-joker melds (rev 10), and re-planned P4 and P5 around the shared `Meld` vocabulary. |
| 2026-08-18 | P1 | ☑ Done. `CardId`, `Card` (record struct: `==` is instance identity, `SameValueAs` is value identity), `DeckBuilder.BuildTwoDecks()` → 108 cards with sequential ids, `Deck` (draw from either end, Fisher–Yates shuffle), `DeckExhaustedException`. Build clean, **60 passed / 0 failed** (32 new). Raised `RULES.md` §9 #11 (turned-up joker) and amended BUILD-PLAN P1, P2 and P7. |
| 2026-08-18 | P0 | ☑ Done. ⚠️ **P55 correction (2026-08-30): the tag claimed here was never pushed and does not exist on any remote — the pre-rewrite tree is `79d86bd`.** Tagged `pre-rewrite`, then deleted `Models/`, `Logic/` and `Common.cs`. Solution restructured to Domain/Console/Tests. Salvaged the enums and display tables into `Cards/{Rank,Suit,CardColor,CardText}` and `Melds/MeldKind`. Build clean, **28 passed / 0 failed**. Amended P0's acceptance (tests, not zero tests) and P3's "Done when" (5 candidates, not 8). |
| 2026-08-18 | — | Rules reconstructed from a codebase abandoned in 2023. `RULES.md` reached rev 8 with all blocking questions closed. Rewrite decided (`BUILD-PLAN.md` §1); 11 packets defined. `docs/spec/RUN-CANDIDATES.md` written, correcting P3's acceptance count from 8 to 5. `/poker` skill created. No code written. |
