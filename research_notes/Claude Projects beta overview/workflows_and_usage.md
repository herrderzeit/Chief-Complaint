# Claude Projects (2026 beta): workflows, best practices, and relation to Claude Code

Scope note: everything here is about the redesigned Projects beta launched on 17 Sep 2026 inside Claude Code (claude.ai/code, the desktop app's Code tab, and mobile). The older claude.ai chat/Cowork "Projects" (2024 knowledge-base feature: grouped chats plus reference files, no threads or coordinator) is labelled **[legacy Projects]** wherever it appears. The primary source is Anthropic's official doc page, which is long and detailed. Most third-party posts found only restate that doc, and their authors say they did not use the product themselves. Reddit pages could not be fetched (blocked), so first-hand community reports are thin.

## 1. Setting up a project well (instructions, repositories, connectors, files, automatic setup)

### Takeaway
Setup has three steps. Create the project (only a name is required; a goal and context are optional). Write project instructions (up to 16k characters) that become the brief for every thread. Then pick models and the cloud environment. Repo-specific rules stay in each repo's CLAUDE.md. "Automatic setup" means Claude takes its own first turn on your first project: it may start a read-only exploration thread or post "Setup recommendations". An initial "$100 setup credit" exists only in one user report and is **not verified** in official docs.

### Cited Findings
- Availability: public beta on Pro and Max, rolling out gradually. It starts with accounts that have used cloud sessions and have no existing chat/Cowork projects. It is not on Team or Enterprise. If **Projects** isn't in the sidebar at claude.ai/code or in the desktop Code tab, join the waitlist at claude.com/form/projects — [Claude Code docs: Projects](https://code.claude.com/docs/en/claude-projects)
- Not available in the terminal CLI, the VS Code extension, the JetBrains plugin, Bedrock, Google Cloud or Foundry — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Launch date 17 Sep 2026. A secondhand report says the waitlisted Pro and Max users were let in on 9 Oct 2026, citing a ClaudeDevs post on X (original not opened) — [Anthropic launch article](https://claude.com/blog/projects-redesigned); [LaoZhang blog](https://blog.laozhang.ai/en/posts/claude-code-projects.md); [DevelopersDigest](https://www.developersdigest.tech/blog/claude-code-projects-coordinator-threads-pro-max-2026)
- Prerequisites for code work:
  - The code must be on github.com. GitHub Enterprise Server, GitLab and Bitbucket are not supported.
  - Your GitHub account needs push access to the repo.
  - The **Claude GitHub App** must be installed on the repo. A `/web-setup` token is not enough for project threads.
  - SSO-enforcing organizations need re-authorization.
  - [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- New project dialog: Name (required), Goal (optional one-liner Claude works toward), Context (optional GitHub repos, files, folders, Google Drive folders) — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- **Automatic setup / first turn**: "On your first project, Claude takes a turn of its own as soon as the project is created, unless you send a message first. That turn uses your plan." In that turn Claude may do either of the following — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - Start one thread that explores the repository read-only and proposes next steps.
  - Post **Setup recommendations** drawn from your recent cloud sessions: repos to add, scheduled tasks to create, and threads it could start. All recommended repos and scheduled tasks start switched ON, so switch off the ones you don't want, then click **Update setup**.
- Any cloud session can be turned into a project with **Continue as project**. Claude reads the session and proposes a setup — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- **$100 setup credit**: one GitHub issue (opened 9 Oct 2026) reports that the Projects UI showed a note saying "$100 setup credi[t] usage won't count toward limits". The reporter still saw session usage go from 10% to 24% and weekly from 2% to 4% while the credit was active. The issue was auto-labelled "invalid" and closed by a bot on 12 Oct with no maintainer reply — [GitHub issue #100900](https://github.com/anthropics/claude-code/issues/100900). Neither the official doc nor the launch article mentions any such credit — [Claude Code docs](https://code.claude.com/docs/en/claude-projects); [launch article](https://claude.com/blog/projects-redesigned). A third-party guide says its only "$100" figure is the Max 5x plan price — [LaoZhang blog](https://blog.laozhang.ai/en/posts/claude-code-projects.md). **Not verified officially.**
- **Project instructions**: text sent to every new thread and to the coordinator, up to 16,000 characters. Set them in Project settings > Memory > Project instructions. Recommended contents — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - What the project is for.
  - Which repos and starting branch, and how to name PRs.
  - How a thread checks its own work.
  - What to do when something it needs is missing.
  - What needs your go-ahead.
- Official example instructions include these rules — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - "Branch from main and open one draft pull request per thread."
  - Run the tests and lint and paste the summary lines.
  - If something is missing, "say exactly what's missing … and stop. Don't substitute, mock, or guess."
  - Don't merge, force-push or change CI without asking.
- Where context lives — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - **Project memory**: files with a `MEMORY.md` index that Claude writes when asked to remember something. These are separate from local auto-memory.
  - **Project instructions**: fixed rules.
  - **Repo `CLAUDE.md`**: per-repo rules such as build commands. Every cloud thread reads it from its clone.
- Changes to instructions, repos, plugins or environment only reach **new** threads, not threads already running — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Repositories: add the 1–2 repos that nearly every task touches. Every thread clones all added repos and loads their CLAUDE.md and skills. Threads can add other repos to themselves when needed, but only from a GitHub owner the project already uses — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- What a thread picks up from repo `.claude/` settings — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - In a **one-repo** project, permission rules, hooks and `env` apply.
  - In a **multi-repo** project on the Anthropic-hosted environment, they don't apply.
  - Plugins declared in `.claude/settings.json` never load. Add them in Project settings > Plugins instead.
- Files: these are copies, not live links. A changed local file has to be re-uploaded with Replace — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - Library tab: up to 100 files and 2 GB per pick, with a single file up to 480 MB.
  - New-project dialog: files over 30 MB are skipped.
  - Folders: only the first 100 files and 200 MB are copied, skipping hidden files and anything over 30 MB.
  - Limit of 10 folders per project.
- Connectors and tools — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - Cloud threads use the connectors (MCP servers) on your claude.ai account, plus `.mcp.json` in a one-repo project.
  - The coordinator conversation itself has **no** connectors.
  - Skills and agents must be committed in a project repo under `.claude/skills` and `.claude/agents`, or enabled on the claude.ai account.
  - CLI tools are installed through the cloud environment's setup script.
  - Network allowlist and secrets are configured in the cloud environment.
- Cloud threads "don't pick up anything from the Claude Code setup on your own machine" — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Recommended first batch — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  1. Write instructions.
  2. Send one small real task and inspect how it reports back.
  3. Check Thread model/effort. The default is **Opus at high effort for threads** and low effort for the coordinator, which draws on the plan fastest.
  4. Ask Claude to propose threads before starting them and to run only a few at a time.
- Third-party advice: test whether saved context persists by starting a small fresh thread and asking it to state the project rules before it edits anything (author did not use the product) — [omidsaffari.com](https://omidsaffari.com/blog/how-to-use-claude-code-projects-beta-2026)

### Inferences
- For this user, the repo is `github.com/herrderzeit/Chief-Complaint` (checked locally with `git remote -v`), so the github.com prerequisite is met once the Claude GitHub App is installed on it.
- The project's existing `CLAUDE.md` and `.claude/skills` (the cc-* persona skills, if committed) would load into every cloud thread automatically. Skills installed only on the local machine or through local plugins (e.g. the `unity:*` plugin skills) would not.
- Because the first auto turn uses plan quota, a cautious user can send a first message immediately (for example "don't start anything yet") to pre-empt it. This is an inference from the doc's "unless you send a message first".

### Gaps
- Official confirmation, amount, eligibility and expiry of the "$100 setup credit" were not found. Whether it really sits outside plan limits is contradicted by the one user report.
- Whether "automatic setup" also auto-creates a cloud environment or setup script was not documented.

## 2. How threads relate to Claude Code sessions, worktrees, branches and PRs; moving existing sessions

### Takeaway
A thread *is* a Claude Code cloud session (its own sandbox, clone, branch and context window) started and tracked by the coordinator. You can ask for a thread to run locally through Remote Control, optionally in its own worktree. Cloud sessions can be moved into a project. Local sessions (like the user's current desktop Code sessions) cannot. Code comes back as a branch plus a PR, which the thread then watches with auto-fix.

### Cited Findings
- "Each thread is usually a cloud session: Claude Code running in the cloud rather than on your machine." The coordinator "sees what threads report back, not every step they take" — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Running a thread locally — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - Turn it on in one of two ways, both needing Claude Code v2.1.280+:
    - Desktop app: Settings > Claude Code > "Use this computer from your phone and claude.ai", then add the folder to the list.
    - Terminal: `claude remote-control`, with `--spawn worktree` to give each thread its own worktree.
  - In the project, choose **Work locally** from the + menu. Claude shows an allow card where you can turn on **Worktree** for that thread.
  - A local thread uses the local files, tools, MCP servers, hooks and permission rules. It gets the project instructions but **not** project memory, and it runs only while the computer is awake.
- The doc warns: "Two threads working in one folder at the same time can overwrite each other's changes," which is why the Worktree option exists — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Worktrees: "Cloud threads don't need them: each clones its repositories into its own cloud sandbox and works on its own branch." — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Moving sessions in — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - Cloud sessions have **Continue as project** (new project) and **Move to project** (existing project). In both cases the original session stays in the list.
  - "A local session doesn't have these options." To continue local work, push its branch, add the repo to the project and name the branch in the task.
  - "A session you start yourself in your terminal, IDE, or the desktop app's local environment can't be added to a project."
- Threads are locked to their project. They can't be moved or copied to another project or out to stand alone, and projects can't be merged — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- PR delivery defaults — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - Each thread works on a new branch from the default branch.
  - It opens a PR when asked, or on its own for concrete fixes.
  - It then watches the PR with auto-fix ON: it pushes CI fixes, addresses review comments, and replies when the PR is ready.
- Thread cards offer buttons: Resolve conflicts, Fix CI, Address comments, Merge it, Review PR, and Create PR (when a branch is pushed but has no PR yet) — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- A cloud sandbox pauses between turns. If it can't resume, the thread continues from a fresh clone and "uncommitted changes can be lost". The doc says to ask Claude to commit and push work in progress on long tasks — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Projects are single-user. They can't be shared, and thread transcripts have no share option — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Anthropic's own summary: Projects is one of five parallel options. Subagents, agent view, agent teams and dynamic workflows/worktrees all run within or beside local sessions, while a project is Claude starting and tracking whole sessions for you — [Claude Code docs](https://code.claude.com/docs/en/claude-projects); [The Register](https://www.theregister.com/ai-and-ml/2026/09/18/claude-code-revamps-projects-so-you-can-work-and-pay-in-parallel/5297532)

### Inferences
- The user's current setup is local desktop Code sessions grouped by repo folder. That would coexist with a project rather than migrate into it. The project would mostly run *new* cloud work, and local sessions stay where they are.
- Any work that needs a live Unity Editor (the repo's CLAUDE.md requires `unity status` / `unity command` before scene or prefab edits) cannot happen in a cloud thread. It would need **Work locally** with Worktree on, or stay in normal local sessions.

### Gaps
- Whether the desktop app's sidebar groups (by repo folder) interact with Projects in any way was not documented.

## 3. Good use cases, anti-patterns, and avoiding conflicts between parallel threads

### Takeaway
Projects suit a goal that outlasts one session and keeps producing tasks: a backlog area you keep feeding, a multi-part build from a spec, multi-repo changes, or document analysis. They don't suit one-off fixes, work where every task needs the local machine, or many threads on one tightly coupled codebase. Separate branches prevent silent overwrites but not merge conflicts, so split the work by file or area.

### Cited Findings
- Official "good fit" examples — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - One goal across many repos.
  - "An area you keep feeding" (bugs and stack traces pasted in as they arrive).
  - "A build or migration bigger than a session" (e.g. "Build what docs/spec.md describes").
  - Non-code work on uploaded documents.
- Official "something else fits better" cases — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - A single task: use a cloud session.
  - Every task needs your machine (local DB, device emulator, VPN API): use local sessions or agent view.
  - A recurring task with no conversation around it: use a standalone scheduled task.
  - Team steering in Slack: use Claude Tag.
- Launch examples: profiling endpoints in parallel threads, and retiring a deprecated endpoint with one thread per repo that reports PR merge order — [Anthropic launch article](https://claude.com/blog/projects-redesigned)
- If two threads touch the same code, it shows up as a normal PR merge conflict rather than a silent overwrite — [DevelopersDigest](https://www.developersdigest.tech/blog/claude-code-projects-coordinator-threads-pro-max-2026); [The Register](https://www.theregister.com/ai-and-ml/2026/09/18/claude-code-revamps-projects-so-you-can-work-and-pay-in-parallel/5297532)
- Anti-patterns listed by third parties (neither author claims first-hand use) — [DevelopersDigest](https://www.developersdigest.tech/blog/claude-code-projects-coordinator-threads-pro-max-2026); [omidsaffari.com](https://omidsaffari.com/blog/how-to-use-claude-code-projects-beta-2026):
  - Many threads on one tightly coupled codebase.
  - Parallel threads on the same file or shared module.
  - Treating a cloud thread like your laptop.
  - Queuing more work than you can review.
  - Relying on a requested concurrency limit as a budget control.
  - Letting threads make destructive changes without asking.
- Tuning phrases the doc recommends — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - "Propose threads and wait for my go-ahead"
  - "Run at most two threads at a time"
  - "Reuse an existing thread for follow-ups in the same area"
  - "Tell me what's wrong … and don't fix anything yet"
  - "Answer that here instead of starting a thread"
  - "Don't open a pull request until I've seen the plan"
- These preferences are saved to memory but are "not enforced settings". Put a limit in project instructions to have it worded exactly and applied to every thread from the start — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Recovery when threads guessed or stalled — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  1. Ask the coordinator to list each thread's assumptions and blockers.
  2. Resolve the bad threads.
  3. Fix the gap once in instructions or the environment.
  4. Send one thread before re-sending the rest.
- Community scepticism: commenters flagged merge-conflict handling, approval friction and cloud-only/secret limits as open questions, and raised lock-in worries. This is reported secondhand from daily.dev and Reddit; the Reddit threads themselves could not be fetched — [DevelopersDigest](https://www.developersdigest.tech/blog/claude-code-projects-coordinator-threads-pro-max-2026)

### Inferences
- In a Unity project, conflicts are especially costly. Text-merged YAML (`.unity`, `.prefab`, `.asset`, `.meta`) conflicts are hard to resolve by hand. Card data assets such as `Assets/Data/Patients/*` would be a hotspot if two threads edited content at once. A safe split is one thread per system or folder (e.g. one C# manager per thread, card data in a single thread) with "don't touch scenes or prefabs" in the instructions.
- Good solo game-dev fits include:
  - A "bugs and polish" project that pasted bug reports feed.
  - EditMode test writing.
  - C#-only refactors.
  - Card-content and balance analysis on uploaded CSVs.
  - Design-doc or spec work delivered as files to the Library.
- Poor fits include anything that needs Play Mode, scene editing, or the Unity Editor.

### Gaps
- No first-hand reports were found from game developers or Unity users on Projects.
- Reddit first-hand threads exist but were not fetchable: "What is everyone using the new Projects for?" and "Question for those using the new Projects Beta" on r/ClaudeAI, linked from [DevelopersDigest](https://www.developersdigest.tech/blog/claude-code-projects-coordinator-threads-pro-max-2026).

## 4. Reviewing and approving work ("Waiting on you"), notifications, scheduled updates

### Takeaway
The Overview pane groups threads into six states: Ready for review, Waiting on you, Working, Landing, Idle and Resolved. Permission prompts must be answered *inside the thread*, not in the coordinator chat. The desktop app sends notifications. "Scheduled updates" are cloud scheduled tasks (routines) that run as threads in the project.

### Cited Findings
- Threads tab groups — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - **Ready for review**: the thread's PR is open.
  - **Waiting on you**: needs your reply or approval, or failed.
  - **Working**: still running.
  - **Landing**: the PR is approved or queued to merge.
  - **Idle**: finished and not waiting on anything.
  - **Resolved**: marked done by you, by Claude after the last step such as a merge, or automatically after a week of inactivity. A resolved thread can be reopened.
- Other Overview tabs: Library, Pull requests, Scheduled tasks — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Approvals — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - Threads run in auto mode when the model supports it.
  - When a thread needs approval, "the prompt is inside that thread … Telling Claude in the project conversation to go ahead doesn't reach it."
  - An approval covers that one prompt, or the rest of the thread if you pick the broader option.
  - To pre-allow commands, use `.claude/settings.json` permission rules. These apply in one-repo projects only (on the Anthropic-hosted environment).
- Steering: a message in a thread's own box goes straight to that thread, while a coordinator follow-up reaches it only if Claude routes it there. Stop with the Stop button or Esc — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Notifications — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - The desktop app notifies when Claude posts in the conversation, a thread errors, or a thread needs input.
  - Per-turn notifications are optional, and notifications can be turned off per project under the project's sidebar menu > Notifications.
  - In the browser there is only a dot on the Overview button.
  - Threads can also be checked and steered from the mobile app.
- Scheduled work — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - Asking for scheduled work creates a cloud scheduled task (routine) that runs as threads in the project and appears on the Scheduled tasks tab.
  - Setup recommendations may propose scheduled tasks, and these start switched on.
  - A thread started by a scheduled task does **not** wait at a usage limit. It stops with an error.
- Third-party advice: audit thread transcripts rather than trusting only the coordinator's summary, and check whether the promised tests actually ran (author did not use the product) — [omidsaffari.com](https://omidsaffari.com/blog/how-to-use-claude-code-projects-beta-2026)

### Inferences
- A daily rhythm suits a solo dev: queue tasks in the evening, then in the morning work Overview's "Waiting on you" first and "Ready for review" second, pulling each PR locally to check it in the Unity Editor before merging.

### Gaps
- Whether "scheduled updates" also covers coordinator status digests (as opposed to scheduled task threads) was not documented. Asking the coordinator for "a status update on every thread" is the documented on-demand route.

## 5. Cost and usage management

### Takeaway
Projects draw on the same 5-hour and weekly Claude Code plan limits, and use them faster. Several things cost usage: parallel threads, the coordinator's own reasoning, PR-watching wake-ups, and reviving old threads. Cost levers are a smaller thread model or lower effort, fewer concurrent threads, fresh threads instead of revived ones, stopping PR watching, and pausing or archiving the project. Threads that hit a limit resume automatically in the next window, which can quietly consume it.

### Cited Findings
- Project usage counts against the same plan limits as other sessions and "can't spend past those limits on its own". Work goes beyond them only if usage credits are enabled, and a thread can't enable them — [Claude Code docs](https://code.claude.com/docs/en/claude-projects); [usage credits help](https://support.claude.com/en/articles/12429409-manage-usage-credits-for-paid-claude-plans)
- What uses quota — [Claude Code docs](https://code.claude.com/docs/en/claude-projects):
  - Running threads, each a full session. There is no fixed concurrency cap; the hard limit is 200 new threads per day.
  - The coordinator conversation.
  - Threads watching PRs, which wake on CI failures or review comments.
- An idle project with no running threads, watched PRs or new messages uses nothing. Neither does an archived one — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Reviving a thread that has been idle longer than the cache lifetime ("an hour on Pro and Max") re-reads its whole conversation. Starting a fresh thread for new work can cost less — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Defaults are Opus everywhere, high effort for threads and low effort for the coordinator. Change them in Project settings > General. Usage is broken down by thread and model in Project settings > Usage — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- At a limit, threads show "Service is busy … will continue automatically" and resume when the window resets. Click Stop, or **Pause** the project, to stop that — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- "on a Pro plan in particular you should expect to reach your limit sooner on days you run one" — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Third-party cost tips (author did not use the product) — [DevelopersDigest](https://www.developersdigest.tech/blog/claude-code-projects-coordinator-threads-pro-max-2026):
  - Put the strong model on the coordinator and a cheaper one on the workers.
  - Measure usage for several days before scaling up.
  - Check usage before leaving batches running overnight.
- The Register framed the feature as a new way to "work and pay in parallel" — [The Register](https://www.theregister.com/ai-and-ml/2026/09/18/claude-code-revamps-projects-so-you-can-work-and-pay-in-parallel/5297532)
- A user report says the "$100 setup credit" period still increased their 5-hour and weekly limit usage (see section 1). **Not verified** — [GitHub issue #100900](https://github.com/anthropics/claude-code/issues/100900)

### Inferences
- Once any initial allowance is gone, a solo dev on Pro or Max could do the following:
  - Set Thread model to Sonnet, or lower the effort.
  - Put "max 2 concurrent threads; propose before starting" in the instructions.
  - Tell threads not to watch PRs ("open the PR then stop watching").
  - Pause the project when not actively using it, so limit-hit threads don't eat the next window.

### Gaps
- No published token or message numbers for plan limits were found, so there is no quantitative per-thread cost data.
- No official information was found on how the "$100 setup credit" is accounted for.

## 6. Guidance relevant to game dev / Unity and solo developers

### Takeaway
Nothing official or first-hand addresses Unity. The key practical constraints come from how cloud sessions work. Cloud sandboxes have no Unity Editor and no local tools. Git LFS reportedly does not work in Claude Code cloud sessions, which matters because this repo uses LFS. Projects are single-user, which suits a solo developer.

### Cited Findings
- Cloud threads only reach GitHub repos, uploaded files and the cloud environment. Anything needing local tools (the doc names "a device emulator" and "a local database") needs a local thread through Remote Control, or a local session — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Git LFS:
  - User bug reports say the Claude Code web/cloud sandbox git proxy "does not implement the Git LFS HTTP API". LFS files stay as pointer stubs, with HTTP 502 "invalid git path" errors.
  - Issue #62285 was opened 25 May 2026 and closed as a duplicate.
  - Other reports say the problem persists, and no working workaround is confirmed.
  - Sources: [claudeissues.com mirror of #62285](https://claudeissues.com/issue/62285-bug-claude-code-web-sandbox-cant-pull-git-lfs); [related report](https://claudeissues.com/issue/60593-bug-claude-code-on-the-web-git-lfs-not-supported-proxy-rejects-lfs-batch-api-wit)
  - Current status is **not verified**.
- Tools such as a Unity install can in principle be added through the cloud environment's setup script and network allowlist — [Claude Code docs](https://code.claude.com/docs/en/claude-projects). Whether a Unity Editor or batchmode license works in that sandbox is **not verified**.
- Projects belong to one user and cannot be shared, and there are no org controls in the beta — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- The default project instructions pattern "say exactly what's missing … and stop. Don't substitute, mock, or guess" is aimed at the case where threads lack tools — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)

### Inferences
- Local check of this repo: the remote is `github.com/herrderzeit/Chief-Complaint` and `.gitattributes` routes 3D model formats (fbx, blend, obj, etc.) through LFS. Cloud threads would therefore probably see pointer stubs for those assets. That is harmless for C# and ScriptableObject YAML work, but they could not inspect or modify the binary art.
- Suggested split for a Unity solo dev:
  - **Cloud project threads**: C# logic, NUnit EditMode tests (written, though possibly not runnable without Unity in the sandbox), data and balance analysis, docs and specs, card-content YAML (one thread at a time).
  - **Local (Work locally + Worktree, or ordinary local sessions)**: anything that needs `unity status` / `unity command`, Play Mode, scenes or prefabs.
- Suggested additions to project instructions:
  - "Never hand-edit .unity or .prefab files; if a task needs the Editor, say so and stop."
  - "One thread per folder or system."
  - "Draft PRs only; I merge after testing in the Editor."
- Unity `.meta` files must accompany any new asset or script a cloud thread creates, otherwise GUIDs get regenerated locally. This is general Unity practice and is not sourced from Projects material.

### Gaps
- No Unity- or game-dev-specific Projects reports, videos or blog posts were found.
- Whether Unity batchmode or tests can run in a Claude cloud environment is unknown.
