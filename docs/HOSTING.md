# Taking Burmese Poker online — a hosting exploration and work brief

**A plan for the next work effort, not a record of one built.** The goal: turn today's
LAN / single-machine app into a **simple hosted app a handful of friends can play over the
internet**. This document frames the problem, states the one constraint that drives every
choice, lays out the hosting options with trade-offs, proposes a shippable build sequence, and
lists the decisions only Nick can make. Nothing here is built yet; `STATUS.md` and `BUILD-PLAN.md`
remain the authorities for what is.

Written 2026-08-27. Context: Nick has a basement homelab and is familiar with Azure.

---

## 1. The goal, and the non-goals

**Goal.** A small, private, cheap, always-on place where four-to-six friends can open a table,
share a link, and play — with the computer filling empty seats, exactly as the local app already
does.

**Non-goals** (say no to these on purpose; each is a much larger effort):

- Public matchmaking, ranked play, or a directory of strangers' tables.
- User accounts, profiles, or persistent bankrolls across sessions.
- Horizontal scale / high availability. A handful of friends is a handful of friends.
- Surviving a server restart mid-game (unless §7 decides otherwise — it is a real cost).

The whole of this is *"deploy the app we have, reachably and safely, for people I invite."*

---

## 2. What you already have — this is a deployment problem, not a rewrite

🔥 **The app is already a networked multiplayer server.** `BurmesePoker.Web` is **Blazor Server**:
each player holds a live SignalR/WebSocket **circuit** to the server, and every interaction is a
round trip. "Playing online" is what Blazor Server *is* — the missing piece is a public address,
not a new architecture.

- ✅ **Concealment is honoured over the wire by construction.** The engine runs **server-side,
  always** (`BUILD-PLAN.md` §3.10): a hand is fully concealed with money on it, so there is no
  client you must trust and nothing to leak by shipping logic to the browser. This is exactly the
  property that makes hosting it safe to strangers-with-the-link.
- ✅ **A lobby with shareable table links already exists** (P13.6): `/` is the lobby
  (`Tables.razor`), `/table/{id}` is one table, and the lobby can open more (`Lobby`, up to
  `MostTables = 12`).
- ✅ **Disconnects already degrade gracefully.** A seat nobody is answering spends its *patience*
  and a **bot stands in** (P10); the client has a themed **reconnect overlay** (P13.5). A friend
  who drops off wifi → a bot covers the seat → they reconnect and resume. Most of "robust online
  play" is already here.
- ✅ **Everything is configured from the command line / environment** already (`Lobby` reads
  `seed`, `seats`, `people`, `pace`, `patience`, `hints`, `between`, `journal` from
  `IConfiguration`), so a container needs no code change to be re-tuned for humans on the internet.

---

## 3. The one constraint that drives every decision

⚠️ **Blazor Server state is in-process and stateful, and so is the lobby.** Two facts:

1. Each player's **circuit lives in one server instance's memory** and cannot move to another.
2. The **`Lobby` is a per-process singleton** (`builder.Services.AddSingleton<Lobby>()` in
   `Program.cs`). Two instances would be **two separate lobbies** — a table opened on one is
   invisible on the other.

🔥 **Therefore: run exactly one instance.** No horizontal scale, no load-balanced replicas, and —
critically — **no scale-to-zero**, because a sleeping instance drops every circuit and ends every
game. For friends this is not a limitation, it is a *simplification*: the entire system is **one
small, always-on container.** Design every option below around that.

*(If multi-instance were ever wanted, it would mean externalising the `Lobby` and adding a SignalR
backplane — e.g. Azure SignalR Service or Redis. That is explicitly out of scope for "simple.")*

---

## 4. The delta — what "LAN app" is missing to be "friends online"

A checklist the build sequence in §6 works through:

- **a. A public HTTPS URL** (TLS + a hostname). Today the app is HTTP-only on `:5188`.
- **b. A container image.** There is **no Dockerfile yet**. A container is the portable unit that
  runs unchanged on the homelab *or* in Azure, so building it first keeps the hosting choice open.
- **c. Reverse-proxy / ingress correctness for Blazor.** Three things a proxy in front of a Blazor
  Server app must get right, or the page renders and then silently does nothing:
  - **WebSockets passed through** (not buffered, not downgraded).
  - **`UseForwardedHeaders`** in the app, so it sees the real `https` scheme and external host —
    antiforgery, redirects and the circuit's own URLs depend on it. *(This project has already
    been bitten once by a scheme/token mismatch that rendered a perfect page with a dead button,
    P13.6 — the same failure class.)*
  - **Generous idle timeouts.** A table between rounds is a quiet socket; a proxy that closes idle
    connections after 60s will kill games.
- **d. Table lifecycle for a long-lived host.** Today tables are opened up to `MostTables = 12`;
  the next effort should confirm whether idle tables are ever **reaped** and, if not, add cleanup
  (drop a table with no viewers after N minutes, and stop its parked bot loop), plus a friendly
  "create table → copy link" affordance on the lobby page.
- **e. Access gating (optional).** Anyone with the URL can sit down. For invited friends a shared
  link is often enough; a **per-table code** or a **single site password** is the cheap next step;
  **Cloudflare Access** (email allow-list in front of the whole site) is near-zero-effort real auth
  if you want *only* named friends.
- **f. Ops.** A `/healthz` endpoint, container auto-restart, structured logs, and resource limits.
  Backups only if §7 decides state must survive restarts.

---

## 5. Hosting options

All four consume the **same container image** from §6 Step 1, so the choice can be deferred until
the image runs.

| Option | Where | TLS / DNS | Home IP exposed? | Cost | Fit with §3 |
|---|---|---|---|---|---|
| **A. Homelab + Cloudflare Tunnel** | your basement | free, managed by Cloudflare | **no** (outbound tunnel) | ~free | one container — perfect |
| **B. Homelab + reverse proxy + port-forward** | your basement | Let's Encrypt via Caddy/Traefik | **yes** | ~free | one container — perfect |
| **C. Azure Container Apps** | Azure | managed cert + FQDN | no | small monthly (min 1 replica) | pin `min=max=1`, affinity on |
| **D. Azure App Service (Linux)** | Azure | managed cert + custom domain | no | small monthly (Always On) | single instance + ARR affinity |

**A — Homelab + Cloudflare Tunnel (recommended for private friend play).** Run the container on
the homelab; `cloudflared` opens an **outbound** tunnel that maps a hostname
(`poker.yourdomain.com`) to it. No inbound ports, no home-IP exposure, free managed TLS,
WebSockets supported. One container matches the single-instance model exactly, and Cloudflare
Access can gate it to invited emails for free. Downside: uptime is your basement's, and it depends
on Cloudflare. This is the lowest-friction, most private, cheapest path and the default
recommendation.

**B — Homelab + reverse proxy + port-forward + DDNS.** The classic self-host: **Caddy** (or
Traefik) terminates TLS with an automatic Let's Encrypt cert and reverse-proxies to the container;
you forward a port and point dynamic DNS at your home IP. Simple certs, full control — but it
**exposes your home IP** and puts firewall/renewal on you. Pick this if you'd rather not depend on
Cloudflare.

**C — Azure Container Apps (recommended "off my hardware" option).** External ingress, managed
cert, an `*.azurecontainerapps.io` FQDN (or custom domain). ⚠️ **Pin `minReplicas = maxReplicas =
1` and enable session affinity** — do **not** let it scale to zero (that drops every circuit) or
past one (that splits the lobby). WebSockets work out of the box. Cheap at this size but not free.

**D — Azure App Service (Linux).** Arguably the simplest managed *always-on single instance*:
enable **WebSockets**, enable **ARR affinity**, set **Always On = true**, deploy the container (or
`dotnet publish`), attach a custom domain with a free managed cert. Slightly higher price floor
than Container Apps depending on the plan.

> **Recommendation.** Build the container (Step 1) first. Then, given the homelab and the "private,
> for friends" goal, go **A (homelab + Cloudflare Tunnel)**; choose **C (Container Apps, 1 replica)**
> instead if you'd prefer it not live in your basement. B and D are solid fallbacks.

---

## 6. A build sequence (each step independently useful)

These are sized like `BUILD-PLAN.md` packets — small, shippable, tree-green at each end.

**Step 1 — Containerize (do this first; it unlocks every option).**
Multi-stage Dockerfile (.NET 10 SDK build → `aspnet` runtime), a `.dockerignore`, listen on
`0.0.0.0:8080` via `ASPNETCORE_URLS`. In `Program.cs` add `app.UseForwardedHeaders(...)` (before
routing) and a `/healthz` endpoint. **Acceptance:** the container runs and a real browser round
deals over `http://localhost:8080` (the P13.6 / P42 browser-round check, in a container). Report
the image size and the run command. Touch no `Domain` code.

```text
# sketch only — the packet writes the real file
docker build -t burmesepoker .
docker run --rm -p 8080:8080 -e ASPNETCORE_URLS=http://0.0.0.0:8080 burmesepoker
```

**Step 2 — Reachability.** Resolve the §7 hosting fork, then wire it:
- A: a `cloudflared` tunnel config mapping `poker.yourdomain.com` → the container; verify TLS **and
  a WebSocket round from a phone off your network**.
- C: `az containerapp create` with external ingress, `--transport auto`, session affinity, and
  `min/max-replicas 1`; verify the same.

**Step 3 — Long-lived-host hardening.** Confirm/implement **idle-table reaping** in `Lobby`
(remove a table with no viewers after N minutes and stop its bot loop — check `HostedTable` /
`TableSession` disposal), and add a **"create table → copy link"** affordance on the lobby page.
Optionally a per-table code (§7 e).

**Step 4 — Config for prod.** Drive `seats` / `pace` / `patience` / `hints` / `between` from
environment (already supported), with **longer patience** tuned for real humans on flaky internet,
and a sensible between-rounds pause. Keep secrets (if any) in the host's store (Azure) or the
tunnel's config file (homelab), never in the image.

**Step 5 — (optional) Durability & observability.** Decide ephemeral vs. persistent (§7). The
`--journal` already records every table as JSONL; *resuming* a live game from a journal after a
restart is a much larger effort and probably not worth it for friends. Add structured logging and a
couple of basic metrics regardless.

---

## 7. Open decisions for Nick (resolve these before Step 2)

1. **Where:** homelab (private, your hardware, ~free) **or** Azure (managed, off your hardware,
   small cost)? — drives Step 2 and the whole of §5.
2. **Gating:** open link · shared site password · per-table code · Cloudflare Access email
   allow-list? — drives §4 e.
3. **Durability:** ephemeral (a restart or redeploy ends the game — simplest, recommended) **or**
   some persistence? — drives whether Step 5 is real work.
4. **Domain:** a custom domain (`poker.yourdomain.com`) or a provider hostname?
5. **Budget:** how many concurrent tables / players to plan for? — sets resource limits and whether
   `MostTables = 12` is right.

---

## 8. Risks & gotchas (Blazor-Server-online-specific)

- ⚠️ **Scale-to-zero and multi-instance silently break this app** (dropped circuits, split lobby).
  One always-on instance, full stop, until §3 is deliberately revisited.
- ⚠️ **WebSockets must be allowed end to end**, and idle timeouts must be generous — a table
  between rounds is a quiet socket, and a proxy that closes it ends the game.
- ⚠️ **`UseForwardedHeaders` is mandatory** behind any TLS-terminating proxy/ingress, or the app
  computes URLs, redirects and antiforgery tokens against the wrong scheme/host — the "renders
  perfectly, button does nothing" failure this project has already met once (P13.6).
- ⚠️ **In-memory state means a deploy ends everyone's game.** Communicate it, or accept it (it is
  the "simple" choice).
- ⚠️ **Table leak:** without idle reaping the site fills `MostTables` and stops opening tables.
- ⚠️ **A public URL is a public URL.** Keep it link-gated at minimum; Cloudflare Access is the
  cheapest way to make it *invited-friends-only*.
- ⚠️ **Cost floor:** an always-on Azure instance has a small but nonzero monthly cost; the homelab
  trades that for electricity and your time.

---

## 9. A ready-to-run prompt for the next session

> Read `docs/HOSTING.md`. This is a **deployment/ops** effort, separate from the rules/strategy
> programme — do **not** touch `Domain`, and keep the tree green (the doc fences require any new
> doc to be mapped in `CLAUDE.md` and forbid `bash`-fenced non-`dotnet` commands).
>
> Execute **Step 1 (Containerize)** as a self-contained packet: add a multi-stage Dockerfile and
> `.dockerignore`, make the app listen on `0.0.0.0:8080` via `ASPNETCORE_URLS`, wire
> `UseForwardedHeaders` and a `/healthz` endpoint in `Program.cs`. **Prove** a real browser round
> deals over `http://localhost:8080` from inside the container (the P13.6 browser-round check).
> Report the image size and the exact run command. Then **stop and confirm the §7 hosting fork
> (A vs C) with Nick** before Step 2 — the hosting decisions are his to make.
