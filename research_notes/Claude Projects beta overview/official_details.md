# Claude "Projects" (Beta, 2026 redesign): official details

Scope note: "2026 Projects" means the redesigned coordinator-and-threads feature announced on 17 Sep 2026. "Legacy Projects (2024)" means the older claude.ai Projects (knowledge plus custom instructions), introduced in June 2024. Research date: 21 Oct 2026.

Primary official sources used:
- Anthropic launch post, "Projects redesigned: from folder to conversation", 17 Sep 2026: https://claude.com/blog/projects-redesigned (mirror: https://claude.com/resources/articles/projects-redesigned)
- Claude Code docs, "Let Claude coordinate ongoing work with Projects": https://code.claude.com/docs/en/claude-projects. This is the most detailed source.
- Help Center, "What are projects?" (updated around 14 Oct 2026; it covers both the new and the legacy versions): https://support.claude.com/en/articles/9517075-what-are-projects

## 1. When and where was it announced? Exact date and version

### Takeaway
Anthropic announced the 2026 Projects beta on **17 September 2026** in the launch post "Projects redesigned: from folder to conversation", which says it is "now available in beta in Claude Code". No Claude Code version number is tied to the launch. Access widened around 9 Oct 2026, and the migration of existing projects started around 14 Oct 2026. It did not launch in October.

### Cited Findings
- Launch post: dated 17 Sep 2026, category "Announcements", no author listed. Subtitle: "A new experience for Claude projects, now available in beta in Claude Code." — [Claude blog](https://claude.com/blog/projects-redesigned)
- The docs page calls it a "public beta on Pro and Max plans and rolling out gradually". — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- On 9 Oct 2026, the ClaudeDevs X account reportedly said every Pro and Max user on the waitlist now had access. This is a secondhand report: the X post URL is not given and I could not verify it. — [MACCUS](https://maccus.fr/en/claude-projects-migration/); [LaoZhang blog](https://blog.laozhang.ai/en/posts/claude-code-projects.md)
- Around 14 Oct 2026 the Help Center said: "We've started moving existing projects for some Pro and Max users to the new version." — [Help Center](https://support.claude.com/en/articles/9517075-what-are-projects); date from [MACCUS](https://maccus.fr/en/claude-projects-migration/)
- In the visible part of the Claude Code CLI changelog (2.1.295, 8 Oct, to 2.1.304, 19 Oct 2026), none of the entries mention the Projects feature. This fits: Projects is a claude.ai/desktop/cloud feature, not a CLI feature. — [Claude Code changelog](https://code.claude.com/docs/en/changelog)
- The only version requirement in the docs: running a thread on your own computer through Remote Control needs Claude Code v2.1.280 or later on that machine. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)

### Inferences
- The "Projects (Beta)" sidebar item the user saw in October is most likely the result of the September beta reaching their account, either through the waitlist expansion or the migration. It is probably not a separate October launch.

### Gaps
- I found no separate anthropic.com/news post, desktop-app release note or app version number for the launch.
- The 9 Oct ClaudeDevs post is not verified (no primary URL).

## 2. What is it officially? Coordinator, threads, where threads run, models

### Takeaway
A project is one long-running coordinator conversation. Claude starts a **thread** for each task. A thread is a separate Claude Code session, usually in the cloud, with its own context window, branch and copy of the repo. Threads run in parallel and report back to the coordinator. By default both the coordinator and the threads use Opus.

### Cited Findings
- "A project is one ongoing conversation where Claude coordinates a stream of related work for you. You tell it what needs doing and it starts a thread for each task." — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- The coordinator is "one long-running session where Claude acts as coordinator. It takes what you send, decides what becomes a thread, and keeps track of every thread it started. It sees what threads report back, not every step they take." — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Routing:
  - Quick questions are answered in the conversation.
  - New work goes to a new thread or to an existing thread in the same area.
  - Several unrelated tasks in one message become separate threads.
  — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- The blog describes a thread as "a Claude Code cloud session working on its own branch and copy of the repo". Threads can open PRs and run tests when repos are connected, and read and draft when documents are connected. Overlapping changes are handled as ordinary merge conflicts. A thread can use subagents, loops and workflows. — [Claude blog](https://claude.com/blog/projects-redesigned)
- The blog's framing: "Brief Claude in the project the way you'd brief a chief of staff." — [Claude blog](https://claude.com/blog/projects-redesigned)
- Where threads run:
  - Threads are usually cloud sessions and keep running after you close your laptop.
  - You can ask for a thread to run on your own computer through Remote Control, using **Work locally** in the + menu. Such a thread runs only while that computer is awake.
  - A local thread can optionally use a git worktree.
  - Cloud threads don't need worktrees. Each clones its repos into its own sandbox and works on its own branch.
  — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Conflict: the 17 Sep blog says threads are cloud-only for now and local is "coming very soon". The current docs already document local threads through Remote Control, so local support has shipped since launch. — [Claude blog](https://claude.com/blog/projects-redesigned); [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Pull requests: by default a thread branches from the repo's default branch and opens a PR when asked, or on its own for concrete fixes. It then watches the PR with auto-fix on, pushing CI fixes and addressing review comments. Thread cards show buttons: Resolve conflicts, Fix CI, Address comments, Merge it, Review PR, Create PR. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Permissions: threads run in auto mode when the model supports it. Approval prompts appear inside the thread, and saying "go ahead" in the coordinator chat does not reach them. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Models: a new project runs Opus everywhere, with **high** effort for threads and **low** effort for the coordinator. Both can be changed under Project settings > General (Thread model/effort and Coordinator model/effort). — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Context: threads auto-compact. The coordinator works from recent messages, recent threads and project memory rather than its full history. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Projects without code: a project doesn't need a repo. Threads can research and write documents, and they deliver files to the Library tab. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)

### Inferences
- The coordinator text the user saw ("answer directly or start threads to work on it in parallel") matches the documented routing behaviour.

### Gaps
- No exact model version is named for the coordinator or the threads; the docs say only "Opus".

## 3. Availability: plans, platforms, regions, link to Claude Code

### Takeaway
The feature is in public beta for **Pro and Max** only and is rolling out gradually. It is not available on Team or Enterprise yet. It lives in Claude Code surfaces: claude.ai/code, the **Code tab of the desktop app**, and the iOS/Android mobile app. It is not available in the terminal CLI, IDE extensions, Bedrock, Google Cloud or Foundry.

### Cited Findings
- From the docs: "Projects are in public beta on Pro and Max plans and rolling out gradually, starting with accounts that have used cloud sessions and don't have existing projects in claude.ai chat or Cowork. They aren't available on Team or Enterprise plans yet." If Projects doesn't appear in the sidebar, you can join the waitlist at claude.com/form/projects. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- The blog's rollout plan:
  - Over the following week, more Claude Code users on Pro and Max would get access.
  - Then updated projects come to all Claude plans and to Team and Enterprise.
  - The Chat and Cowork rollout comes later.
  — [Claude blog](https://claude.com/blog/projects-redesigned)
- Help Center: the feature is available to select Pro and Max subscribers who use Claude Code, and to some Pro and Max users whose projects were moved. Chat, Cowork, Team and Enterprise will follow. It is found under "Projects" in the sidebar at claude.ai/code and in the Code tab of the desktop app. — [Help Center](https://support.claude.com/en/articles/9517075-what-are-projects)
- Platforms: "Projects are available at claude.ai/code, in the desktop app, and in the Claude mobile app, not in the terminal CLI, the VS Code extension, or the JetBrains plugin, and not through Amazon Bedrock, Google Cloud's Agent Platform, or Microsoft Foundry." — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Code projects need GitHub prerequisites:
  - Repos must be on github.com. GitHub Enterprise Server, GitLab and Bitbucket are not supported.
  - Your account needs push access.
  - The Claude GitHub App must be installed on the repo. A `/web-setup` token is not enough.
  — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)

### Inferences
- The Free plan is not covered by the 2026 beta. The legacy Projects remain available on Free, with a maximum of five projects (see section 6).

### Gaps
- I found no region-specific availability statement.

## 4. Pricing and usage: the "$100 initial usage" offer and limits

### Takeaway
I could **not verify** the in-app banner offer ("Up to $100 of initial usage, including the automatic setup, won't count towards your usage limits") in any official public source. The terms, duration and eligibility are unknown. Officially, project usage counts against the same Claude Code plan limits and uses them faster. There is a hard cap of 200 new threads per day.

### Cited Findings
- From the docs: "Project usage counts against the same plan limits as your other Claude Code sessions, and a project can't spend past those limits on its own." Work goes past limits only if usage credits are turned on, and a thread can't turn them on. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- What draws on the plan:
  - Running threads: each is a full session.
  - Coordinator tokens.
  - Idle threads that wake when CI fails or a review arrives on a watched PR.
  An idle or archived project uses nothing. "The enforced limit is 200 new threads per day across your projects." — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- "On a Pro plan in particular you should expect to reach your limit sooner on days you run one." — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Automatic setup on the first project: Claude "takes a turn of its own as soon as the project is created ... That turn uses your plan." In that turn it may start one read-only exploration thread or post **Setup recommendations** (repos, scheduled tasks, suggested threads). — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
  - This contradicts the banner the user saw, which says the setup won't count. The banner is probably a newer or targeted promotion not yet in the docs (not verified).
- Hitting a limit: a thread at the 5-hour or weekly limit retries and continues automatically when the limit resets ("Service is busy"). Threads started by a scheduled task stop with an error instead. You can Stop a thread or Pause the project. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Usage visibility: Project settings > Usage shows tokens by thread and by model, and the coordinator's share. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- An unrelated earlier promo: one third-party guide mentions $100 (Pro) / $250 (Max) bonus credit for Claude Code cloud sessions with an Oct 7 deadline. Not verified, and not tied to Projects. — [WebSearch summary of startuphub.ai / verdent.ai guides](https://www.startuphub.ai/ai-news/artificial-intelligence/2026/how-to-use-claude-ai-free-2026)

### Gaps
- The $100 offer is not verified: no official terms, duration or eligibility were found on the blog, docs, Help Center or third-party coverage checked.

## 5. Features: Overview, thread statuses, notifications, scheduled updates, instructions, connectors, files, artifacts, memory

### Takeaway
The docs cover each of these in detail:
- The Overview pane has Threads, Library, Pull requests and Scheduled tasks tabs.
- Threads are grouped into six states.
- Desktop-only notifications.
- Scheduled work runs as cloud scheduled tasks.
- Project instructions of up to 16,000 characters.
- File-based project memory.
- Connectors come from your claude.ai account.

### Cited Findings
- Overview: the Threads tab groups threads as follows. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
  - **Ready for review**: the PR is open.
  - **Waiting on you**: the thread needs a reply or approval, or it failed.
  - **Working**
  - **Landing**: the PR is approved or queued to merge.
  - **Idle**
  - **Resolved**: marked done by you, by Claude after the last step, or automatically after a week of no activity. Resolved threads can be reopened.
  The Overview button shows a dot when a thread is waiting on you.
- Other Overview tabs:
  - **Library**: files you added and files threads produced.
  - **Pull requests**
  - **Scheduled tasks**
  — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Suggested threads: "Click the arrow on a suggestion to start that thread. When several are listed, a button under the list starts all of them." — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Notifications: the desktop app notifies you when Claude posts, when a thread errors, and when a thread needs input. You can opt in to "each turn finished" via Notifications in the project's sidebar menu. These are desktop-only; in the browser you watch the Overview dot. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Scheduled updates: ask Claude in the project and it creates a cloud scheduled task (routine) that runs as threads in the project. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Steering: you tell Claude how many threads to run at once, when to post updates, and whether to propose threads or start them. These preferences are saved to project memory but are "not enforced settings". — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
  - The blog adds that Claude learns your working style, and you can adjust check-in frequency and update detail. — [Claude blog](https://claude.com/blog/projects-redesigned)
- Instructions: up to 16,000 characters, sent to each new thread and to the coordinator. Set them in Project settings > Memory > Project instructions, or ask Claude. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Memory: project memory is stored as files with a `MEMORY.md` index that every cloud thread reads at start. It is editable in Project settings > Memory and is separate from local Claude Code auto memory and from repo `CLAUDE.md`. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Connectors, plugins and skills:
  - Cloud threads use the connectors on your claude.ai account. "The project conversation itself has no connectors."
  - Plugins are added in Project settings > Plugins.
  - Skills come from the repos and from your claude.ai account.
  - Local machine setup is not carried into cloud threads.
  — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Files:
  - Library: up to 100 files and 2 GB per pick, with single files up to 480 MB.
  - Folders: the first 100 files and 200 MB are copied.
  - At most 10 folders or Google Drive folders per project.
  - Uploads are copies.
  - Files are mounted at `/mnt/project-files`.
  — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Artifacts: the blog says a library collects files you add and artifacts Claude produces. — [Claude blog](https://claude.com/blog/projects-redesigned)
- Project controls: Pause (stops everything), Archive (hides it and stops threads), Delete (permanent; GitHub branches and PRs are unaffected). — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Ways to start a project:
  - New project dialog: a name is required; a goal and context are optional.
  - "Continue as project" or "Move to project" from an existing cloud session.
  — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)

### Gaps
- Pinning a project in the sidebar is not mentioned in the docs I read (not verified, though the user observed it).

## 6. How it differs from legacy claude.ai Projects and from Claude Code sessions

### Takeaway
Legacy Projects (2024) group chats, a knowledge base and custom instructions, with no coordinator or threads. The 2026 Projects make the project itself a conversation that launches and tracks Claude Code sessions. Unlike plain cloud sessions, local sessions, agent view or worktrees, Claude starts and tracks the parallel sessions for you.

### Cited Findings
- **Legacy Projects (2024)**, per the docs: "the earlier Projects experience, which groups conversations and reference files without threads or a coordinator. Those projects keep working as they do today until the redesigned experience reaches them." — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- **Legacy Projects (2024)**, per the Help Center:
  - Available on all plans; Free users can create up to five.
  - A knowledge base with uploads and custom instructions.
  - RAG mode on paid plans, which expands capacity up to 10x.
  - Sharing on Team and Enterprise ("Can view" / "Can edit").
  — [Help Center](https://support.claude.com/en/articles/9517075-what-are-projects)
- Migration: "Nothing is lost or deleted in the move." Instructions, files, full chat history and scheduled tasks carry over. Users who used a project in the last 30 days get an email before the move. — [Help Center](https://support.claude.com/en/articles/9517075-what-are-projects)
- The blog's line on existing projects: "Existing projects on Pro and Max plans keep working as they do today." — [Claude blog](https://claude.com/blog/projects-redesigned)
- Comparison with Claude Code features. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
  - "In a project, Claude starts and tracks the sessions instead of you, and each one starts from the same instructions."
  - Local sessions and agent view: you start each one yourself, and they cannot be added to a project.
  - Worktrees: not needed for cloud threads.
  - Agent teams and subagents: these live inside a single session.
  - Claude Tag: a team Slack feature, versus Projects, which belong to one user.

### Inferences
- Desktop-app sidebar groups of Claude Code sessions are not projects. Only cloud sessions can become or feed a project.

### Gaps
- Sidebar "groups" in the desktop app are not compared explicitly in the official docs.

## 7. Limitations, beta caveats, data and privacy

### Takeaway
Projects are single-user and can't be shared. There are no org-level controls during the beta. Cloud sandboxes can lose uncommitted work. Only Anthropic is the model provider. Data handling follows the cloud-session security and data-usage docs.

### Cited Findings
- From the docs: "A project belongs to one user. You can't share a project or its threads ... There are no organization-level controls for projects during the beta." — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Threads can't be moved between projects, and projects can't be merged. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- A cloud thread's sandbox pauses between turns. If it can't resume, the thread restarts from a fresh clone and uncommitted changes can be lost. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- Anthropic is the model provider for both cloud and Remote Control threads. Security and data retention are covered by the Security and Data usage docs. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- A project can't run a local thread while "Require trusted devices" is on. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- With several repos in an Anthropic-hosted environment, no repo's `.claude/settings.json` permission rules or hooks apply to threads. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)
- After a repo is added, Claude can only add more repos from GitHub owners the project already uses. — [Claude Code docs](https://code.claude.com/docs/en/claude-projects)

### Gaps
- I found no Projects-specific privacy or retention terms beyond the general cloud-session docs.
