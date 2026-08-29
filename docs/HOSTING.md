# Taking Burmese Poker online — a hosting exploration and work brief

**A plan for the next work effort, not a record of one built.** The goal: turn today's
LAN / single-machine app into a **simple hosted app a handful of friends can play over the
internet**. This document frames the problem, states the one constraint that drives every
choice, lays out the hosting options with trade-offs, proposes a shippable build sequence, and
lists the decisions only Nick can make. Nothing here is built yet; `STATUS.md` and `BUILD-PLAN.md`
remain the authorities for what is.

Written 2026-08-27; **revised 2026-08-28 after reviewing `~/source/repos/ansible-nas`** — the
homelab turns out to have already solved most of this, which moves the recommendation. See **§5a**,
and read it before §5. Context: Nick has a basement homelab managed by Ansible, and is familiar
with Azure.

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
- **d. Table lifecycle for a long-lived host. ✅ Done by `P54`, and the answer to the open question
  was *no*.** Nothing closed a table: `Lobby.Close` existed from P13.6 and only the tests called it,
  so the site filled `MostTables = 12` and thereafter refused to open another. A `TableSweeper`
  hosted service now closes tables nobody has been at for 30 minutes (never the house table), and
  each lobby row carries the table's absolute address with a copy button. ⚠️ **The parked bot loop
  was never the leak** — `HostedTable.Deal` stops the moment its last viewer leaves.
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

> **Recommendation — revised 2026-08-28, see §5a.** Build the container (Step 1) first; it is
> hosting-agnostic and unblocks all four. Then go **B — the homelab's Traefik, which is already
> running** with a wildcard certificate and 80/443 already forwarded, so the marginal cost of this
> app is a role and a DNS name. Choose **C (Container Apps, `min = max = 1`)** instead if you would
> rather the game's uptime not be your basement's. ⚠️ **A is no longer the default**: it buys only
> hiding the home IP, which the existing setup has already traded away for every other service, at
> the price of a second ingress path beside Traefik for one app. **D** stays a fallback.

*(The original recommendation was **A**. It was written without knowledge of `ansible-nas`; §5a is
what changed it.)*

---

## 5a. ⚠️ What the homelab already has — reviewed 2026-08-28

🔥 **`~/source/repos/ansible-nas` (origin `gitea.nickjones.dev/nickjones/ansible-homelab`, mirrored
to GitHub) already runs the whole of Option B.** What is there:

- **Traefik with a wildcard `*.nickjones.dev` certificate**, issued by Let's Encrypt over
  **DNS-01 through Cloudflare** (`roles/traefik`), with **ports 80 and 443 the only ones exposed**
  and every other service LAN-only. TLS, DNS and ingress are solved — for every app at once.
- **110 roles on one shape**, two of them written by hand for Nick's own applications:
  - `roles/nickjones-dev` — clone the app repo onto the server, `community.docker.docker_image`
    build it there, run it with Traefik labels.
  - 🔥 `roles/mirroquest` — the newer and better shape: **build nothing on the server.**
    `docker_login` to the Gitea registry, `pull: true`, run. Registry credentials fail fast with
    `ansible.builtin.fail` when the inventory has not set them.

  Both are exactly two files (`defaults/main.yml`, `tasks/main.yml`): an `*_enabled` flag, an
  `*_available_externally` flag, a memory cap, the six standard Traefik labels, and a stop block
  that removes the container when the flag is false. **A `burmesepoker` role is that, with a
  different port.**
- **A Gitea Actions runner with opt-in docker-socket passthrough for runners that build images**
  (`roles/gitea-actions-runner`, `gitea_actions_runner_mount_docker_sock`) — so there is already
  somewhere to build an image that is not the NAS.
- **A plans convention**, `docs/superpowers/plans/YYYY-MM-DD-*.md`; the `nickjones-dev` role was
  written from one. **The plan for this work lives there**, not in this repo:
  `docs/superpowers/plans/2026-08-28-burmesepoker-hosting.md`.

**Three consequences for this document.**

1. 🔥 **Option B is not a fallback; it is already built.** The delta for "friends online" collapses
   to a Dockerfile, a published image, a role and a DNS name.
2. ⚠️ **Option A is now mostly redundant** — see the revised recommendation in §5.
3. **The Azure options survive as the answer to one question only** — *should the game's uptime
   depend on the basement?* That is a preference, not a technical fork, so **§7's first decision is
   much less consequential than it was written to be.**

⚠️ **Four things a Blazor role needs that none of the 110 existing roles does.**

- **`*_memory: 64m` must not be copied.** Both hand-written roles front a tiny static site; a .NET
  server holding live circuits wants **512m** at least.
- **`UseForwardedHeaders` is on the *app* side and no Ansible label can substitute for it** (§8),
  which is why the container step comes first wherever this ends up hosted.
- **WebSockets and idle timeouts are assumptions here, not established facts.** Traefik proxies
  WebSockets transparently by default and does not close idle streams by default — but **the only
  role in the whole repo that mentions websockets is `bitwarden`**, so nothing there proves it for
  this app. ⚠️ **Test it with a real round from a phone off the home network**; do not rely on it.
- **There is no auth-middleware pattern in the repo** — roles are `available_externally` or they are
  not. So §7's gating decision is genuinely new work: a Traefik `basicauth` middleware label, or
  Cloudflare Access in front of the hostname.

---

## 6. A build sequence (each step independently useful)

These are sized like `BUILD-PLAN.md` packets — small, shippable, tree-green at each end.

**Step 1 — Containerize (`P51`; do this first — it unlocks every option). ✅ Built 2026-08-28** — see `BUILD-PLAN.md` §5 P51 for what shipped and for the `--no-restore` trap it found.
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

**Step 2 — A published image (`P52`). ✅ Built 2026-08-28** — and `P53`'s credential turned out to exist already (`mirroquest_registry_password`), so only this step ever needed a new token. Give the repo a **Gitea origin** beside GitHub — the
pattern every other personal repo here uses (push to `gitea.nickjones.dev`, push-mirror to GitHub) —
and a **Gitea Actions workflow** that builds the Dockerfile and pushes
`gitea.nickjones.dev/nickjones/burmesepoker:latest` on a push to `main`. ⚠️ **Build there, not on
the NAS**: a .NET SDK image plus a full restore on the Hyper-V VM every time the checkout moves is
exactly the cost `roles/mirroquest` was written to avoid, and `roles/gitea-actions-runner` already
carries the opt-in docker-socket passthrough for runners that build images. **Acceptance:**
pulling the published tag
runs the same container Step 1 verified. **Needs from Nick:** a Gitea PAT with package write.

**Step 3 — The Ansible role (`P53`, in `ansible-nas`).** `roles/burmesepoker/` on the
**`mirroquest` template**: `defaults/main.yml` (`burmesepoker_enabled: false`,
`burmesepoker_available_externally: false`, image / version / registry vars,
`burmesepoker_memory: 512m`, and the app's own `seats` / `pace` / `patience` / `hints` / `between`
carried as environment), `tasks/main.yml` (fail-fast on missing registry credentials,
`docker_login`, `docker_container` with `pull: true`, the six Traefik labels routing
`poker.{{ ansible_nas_domain }}` at container port **8080**, and the stop block), one entry in
`nas.yml` in alphabetical position, and a page under `website/docs/applications/`.
**Acceptance:** the playbook brings the table up at `https://poker.nickjones.dev`, and **a real
round is played from a phone off the home network** — which is what settles §5a's WebSocket and
idle-timeout assumptions.

**Step 4 — Long-lived-host hardening (`P54`, back in this repo). ✅ Built 2026-08-29.** Idle-table
reaping is in `Lobby` (30 minutes idle, swept every 5 by a `TableSweeper` hosted service; the house
table is never reaped), the lobby page writes each table's absolute address out as a link with a
copy button beside it, and patience is **180 s** against a `DisconnectedCircuitRetentionPeriod` of
**2 minutes** — the two are fenced against each other. ✅ **The §7 gating decision was landed by
P53** (a Traefik `basicauth` middleware). ⚠️ **The parked bot loop was never leaking**:
`HostedTable.Deal` stops the moment its last viewer leaves. ⚠️ **`SeatChannel` still does not
dispose its per-seat `ManualResetEventSlim`** — bounded, because the wait handle's own finaliser
releases it, and disposing it races the engine thread parked in `Ask`, which takes no cancellation
token. ⚠️ **Not yet verified in a browser or on a live site.**

**Step 5 — (optional) Durability & observability.** Decide ephemeral vs. persistent (§7). The
`--journal` already records every table as JSONL; *resuming* a live game from a journal after a
restart is a much larger effort and probably not worth it for friends. Add structured logging and a
couple of basic metrics regardless.

## 7. Open decisions for Nick (resolve these before Step 3)

1. ⚠️ **Where — largely settled by §5a, and now a preference rather than a fork.** The homelab
   already runs Traefik with a wildcard cert and 80/443 forwarded, so **B costs a role**; Azure (C)
   is the answer only to *"should the game be down when the basement is?"* **Default: homelab.**
2. 🔥 **Gating — the decision that actually costs work now.** Open link · a Traefik `basicauth`
   middleware · a per-table code · Cloudflare Access email allow-list? ⚠️ **`ansible-nas` has no
   auth-middleware pattern at all** (§5a), so whichever is chosen is new in that repo. Drives §4 e
   and Step 4.
3. **Durability:** ephemeral (a restart or redeploy ends the game — simplest, recommended) **or**
   some persistence? — drives whether Step 5 is real work.
4. **Domain:** the wildcard cert already covers `*.nickjones.dev`, so **`poker.nickjones.dev` is
   free and needs no new certificate** — confirm the hostname, or name another.
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
- ✅ **Table leak — this was real, and it is closed (`P54`, 2026-08-29).** Nothing in the client
  closed a table before that packet: `Lobby.Close` existed from P13.6 and only the tests called it,
  so the site filled `MostTables` and thereafter refused every *Open it* — weeks after a deploy, and
  looking like a broken form.
- ⚠️ **A public URL is a public URL.** Keep it link-gated at minimum; Cloudflare Access is the
  cheapest way to make it *invited-friends-only*.
- ⚠️ **Cost floor:** an always-on Azure instance has a small but nonzero monthly cost; the homelab
  trades that for electricity and your time.
- ⚠️ **A 64m memory cap will kill it.** The two hand-written `ansible-nas` roles are static sites
  and cap at `64m`; copying that number into a .NET role holding live circuits produces an OOM-kill
  loop that looks like a networking fault. **512m minimum** (§5a).
- ⚠️ **Building the image on the NAS is a trap**, not merely slow: a .NET SDK layer plus a full
  restore on the Hyper-V VM, on every playbook run that sees a new commit. Build in Gitea Actions
  and pull the tag (§6 Step 2).

---

## 9. A ready-to-run prompt for the next session

> Read `docs/HOSTING.md` (including **§5a**) and `BUILD-PLAN.md` §5 **P51**. This is a
> **deployment/ops** effort, separate from the rules/strategy programme — do **not** touch `Domain`,
> and keep the tree green (the doc fences require any new doc to be mapped in `CLAUDE.md` and forbid
> `bash`-fenced non-`dotnet` commands, so docker and ansible commands go in `text` fences).
>
> Execute **P51 (Containerize)** as a self-contained packet: add a multi-stage Dockerfile and
> `.dockerignore`, make the app listen on `0.0.0.0:8080` via `ASPNETCORE_URLS`, wire
> `UseForwardedHeaders` and a `/healthz` endpoint in `Program.cs`. **Prove** a real browser round
> deals over `http://localhost:8080` from inside the container (the P13.6 / P42 browser-round
> check). Report the image size and the exact run command. Then **stop**: P52 needs a Gitea PAT
> from Nick, and P53 is work in a different repository (`ansible-nas`, whose plan is already
> written at `docs/superpowers/plans/2026-08-28-burmesepoker-hosting.md`).
