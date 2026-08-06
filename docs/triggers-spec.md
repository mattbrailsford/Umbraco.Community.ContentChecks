# Triggers — when checks run

**Status:** Build sketch only (Phase 4). The stance here is deliberately narrow, because of finding E in the [Overview](overview.md).
**Part of:** [Overview](overview.md). Consumes [Core Contract](core-contract-spec.md)'s runner and [Policy Resolution](policy-resolution-spec.md)'s resolved rules; defines nothing new about either.

---

- [ ] **On demand:** always available. The default and the safe one.
- [ ] **On save:** advisory only. Never blocks.
- [ ] **On publish:** blocking allowed, but **only for interactive single-item publishes**.
- [ ] **Explicitly out of scope for blocking in v1:** scheduled publishing, bulk publish, publish-with-descendants, Umbraco Deploy transfers, API-driven publishing. These record a report where checks are configured, but never block. Rationale: there is no human to read the feedback, and an LLM round trip inside a 3am scheduled publish is indefensible.
- [ ] Document this asymmetry prominently — it will otherwise read as a bug.
- [ ] **Lesson from research** (see [research.md](research.md), Contentful's `setInvalid()` gap): a check that only shows a red badge but doesn't actually gate the publish action is a worse UX than no check at all — it looks like protection and isn't. Every `Blocking` entry must wire into the real publish pipeline (`IContentEditingService`/`IContentPublishingService`), never just a UI-layer flag.
