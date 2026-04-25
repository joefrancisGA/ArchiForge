> **Scope:** Buyer-facing first-30-minutes path — sign in on the cloud, pick a vertical, run a sample, read the first finding. No local install. The full marketing page with screenshots lives at `archlucid.com/get-started`; this stub is for evaluators arriving via GitHub.

> **Audience banner:** Prospective buyers and evaluators arriving via GitHub. **For internal contributor onboarding see [`docs/engineering/FIRST_30_MINUTES.md`](engineering/FIRST_30_MINUTES.md)** — that path is Docker / SQL / .NET / Node and is **not** for buyers.

# Your first 30 minutes with ArchLucid

ArchLucid is a SaaS product. You will not install anything to evaluate it.

## Where you are now

You found ArchLucid on GitHub. The repository is open so engineers can read the source, the architecture decisions, and the security posture before talking to us. **Evaluating the product itself happens on the hosted SaaS at [`archlucid.com`](https://archlucid.com)** — there is no Docker, SQL, .NET, Node, Terraform, or CLI on the buyer path.

For the same five steps with screenshots and links, open [`archlucid.com/get-started`](https://archlucid.com/get-started).

## What 30 minutes looks like

Five steps. Roughly thirty minutes end-to-end on a normal connection.

1. **Sign in.** Open [`archlucid.com`](https://archlucid.com) and sign in with your work identity (Microsoft Entra ID or a Google Workspace account). <<placeholder copy — replace before external use>>
2. **Pick a vertical.** A short picker asks which industry profile to start from. The defaults match the briefs in [`templates/briefs/`](../templates/briefs/) — `financial-services`, `healthcare`, `public-sector`, `public-sector-us`, `retail`, `saas`. Choose the closest match; you can change it later. <<placeholder copy — replace before external use>>
3. **Run a sample.** ArchLucid pre-populates a sample architecture request shaped for the vertical you picked, then runs the analysis pipeline. No upload required for the first run. <<placeholder copy — replace before external use>>
4. **Read your first finding.** Open the committed run and read the first typed finding — what was flagged, why it was flagged, what evidence backs it. This is the smallest unit of value the product produces. <<placeholder copy — replace before external use>>
5. **Decide what to do next.** Either invite a colleague and run a second sample, or hand off to a guided pilot. <<placeholder copy — replace before external use>>

## Where to go next

- **Screenshots and the same five steps with the live UI:** [`archlucid.com/get-started`](https://archlucid.com/get-started).
- **Operator path (after the sample run, when you are ready for a real pilot):** [`docs/CORE_PILOT.md`](CORE_PILOT.md).
- **What the product is and is not, in plain language:** [`docs/EXECUTIVE_SPONSOR_BRIEF.md`](EXECUTIVE_SPONSOR_BRIEF.md).
- **Pricing:** [`archlucid.com/pricing`](https://archlucid.com/pricing).

## No local install

Nothing on this page asks you to install Docker, SQL Server, .NET, Node, Terraform, or a CLI. If a document tells you to install one of those, you are reading **contributor** material — engineering docs that live under [`docs/engineering/`](engineering/) for ArchLucid contributors only.
