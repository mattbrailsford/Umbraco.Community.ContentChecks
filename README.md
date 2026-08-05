# Umbraco.Community.ContentChecks

> 🧪 **Pre-release.** In active development — Phase 0/7 of the [implementation plan](https://github.com/mattbrailsford/Umbraco.Community.ContentChecks). Not yet usable.

Content Checks turns "what good content looks like" into an executable, triggerable check that returns a status plus actionable feedback — for editors, and for AI agents that generate content and need to validate their own work before a human sees it.

- **Content Checks core** — a check contract, a runner, and editor-authored check sets. Like Umbraco's Health Checks, but for content, and with settings editors configure through a UI rather than a developer configuring through code.
- **Content Checks AI** (`Umbraco.Community.ContentChecks.AI`) — an AI-judged check type built on [Umbraco.AI](https://github.com/umbraco/Umbraco.AI), for rubric-scored criteria a deterministic rule can't express (tone, brand voice, overall quality).

## Status

Nothing here yet works. See the implementation plan for the phased build-out, the design decisions already made, and the open questions still on the table.

## License

MIT — see [LICENSE](LICENSE).
