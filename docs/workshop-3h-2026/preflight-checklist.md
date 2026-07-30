# Preflight checklist

Complete this before the workshop. The labs assume the C# AppHost track; the canonical lessons also document the TypeScript AppHost alternative.

## Required software

- [ ] Git
- [ ] [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [ ] [Aspire CLI 13.4.6 or later](https://aspire.dev/get-started/install-cli/)
- [ ] [Docker Desktop](https://www.docker.com/products/docker-desktop/) or another running OCI-compatible container runtime
- [ ] [Node.js 20.19 or later](https://nodejs.org/) with npm
- [ ] Visual Studio 2026, or VS Code with C# Dev Kit and the [Aspire extension](https://aspire.dev/get-started/aspire-vscode-extension/)
- [ ] 8 GB free disk space for SDKs, npm packages, and emulator images
- [ ] Optional: GitHub Copilot CLI or another agent host supported by `aspire agent init`

## Clone and verify

```bash
git clone https://github.com/tamirdresher/aspire-workshop.git
cd aspire-workshop
dotnet --version
node --version
npm --version
aspire --version
aspire doctor
docker version
```

Expected:

- `.NET 10.x`, Node `20.19+`, and Aspire `13.4.6+` are reported.
- `aspire doctor` reports a usable SDK, container runtime, and development certificate.
- `docker version` shows both client and server information.

If the Aspire CLI is missing, follow the [official installation guide](https://aspire.dev/get-started/install-cli/). Do not install the retired Aspire workload.

## npm proxy and deck setup

Use the required package feed proxy without storing credentials:

```bash
npm ci --prefix docs/workshop-3h-2026 --registry=https://packagefeedproxy.microsoft.io/npm/
npm start --prefix docs/workshop-3h-2026
```

Expected: the Reveal.js deck opens at <http://127.0.0.1:8080/docs/workshop-3h-2026/reveal/>.

If npm is centrally configured, confirm the active registry:

```bash
npm config get registry
```

The expected workshop proxy URL is `https://packagefeedproxy.microsoft.io/npm/`. Do not put tokens, passwords, or authenticated registry URLs in repository files.

## Warm dependencies before class

```bash
dotnet restore Exercise/workshop/Lesson-01/code/Bookstore.sln
npm --prefix Exercise/workshop/Lesson-01/code/Bookstore.Admin ci --registry=https://packagefeedproxy.microsoft.io/npm/
dotnet restore Exercise/workshop/Lesson-02/code/Bookstore.sln
npm --prefix Exercise/workshop/Lesson-02/code/Bookstore.Admin ci --registry=https://packagefeedproxy.microsoft.io/npm/
docker pull redis:latest
```

Docker may pull additional emulator images during Lab 3. On restricted networks, the facilitator should warm the complete Lesson 2 AppHost once before class.

## Final five-minute check

- [ ] Docker Desktop is running.
- [ ] No previous workshop AppHost is running: `aspire ps`.
- [ ] Ports are not hardcoded in notes or scripts; use the dashboard URL and endpoints printed by Aspire.
- [ ] The repository is clean or students have a disposable branch.
- [ ] Browser pop-ups are allowed for localhost.
- [ ] The AI agent is signed in, if participating in Lab 4.
- [ ] An Azure subscription is **not required**; Lab 5 inspects a deployment plan and generated artifacts only.

See the canonical [Lesson 1 prerequisites](../../Exercise/workshop/Lesson-01/README.md#prerequisites) and [deployment prerequisites](../../Examples/AspirePublish/README.md#common-prerequisites) for additional detail.
