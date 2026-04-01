---
description: Verify claims before implementing (anti-hallucination)
agent: agent
tools: ["search/codebase", "search", "search/usages", "web/githubRepo"]
---

**CRITICAL: Verify before implementing**

Before making ANY code changes, verify the following using available tools:

## File Existence

- [ ] Use #tool:search/codebase to confirm files exist at claimed paths
- [ ] Use #tool:search to verify functions/classes mentioned exist
- [ ] List actual files that will be modified with full paths

## Pattern Verification

- [ ] Search for similar existing patterns in the codebase
- [ ] Confirm the approach matches existing conventions
- [ ] Verify imports/dependencies are already used elsewhere

## API/Framework Usage

- [ ] If using a framework method, search codebase for examples
- [ ] Verify the API signature matches what you're claiming
- [ ] Don't assume methods exist - find evidence first

## Dependencies

- [ ] Check package.json / requirements.txt / go.mod for existing dependencies
- [ ] Don't suggest adding dependencies unless verified they're needed
- [ ] Search if functionality already exists in codebase

## Report Format

```
✅ Verification Complete:
- Files exist: [list]
- Patterns found: [examples from codebase]
- Dependencies present: [list]
- Similar implementations: [file:line references]

⚠️ Concerns:
- [Any gaps or uncertainties]

Ready to proceed: Yes/No
```

**Only proceed if verification passes.**

```

**Usage**: Type `/verify` before asking agent to implement complex features

---

## 4. Recommended Workflow

### Option A: Full Workflow (Complex Features)
```

1. Planner Agent: "Plan a feature for [description]"
2. Review plan mentally or with /review
3. Builder Agent: "Implement the plan above"
4. Builder hands off → Reviewer Agent (automatic via handoff)
5. Fix issues if needed

```

### Option B: Quick Implementation (Simple Tasks)
```

1. Builder Agent: "Add [simple feature]"
2. /review after completion

```

### Option C: Verification First (Uncertain Tasks)
```

1. /verify the approach first
2. Builder Agent: implement after verification passes
3. /review after completion

```

---

## 5. VS Code Agent Picker Configuration

After creating these files, your agent picker will show:
```

Agents:
├── 🎯 Planner (Generate implementation plans)
├── 🔨 Builder (Constrained implementation) ⭐ NEW
├── 👁️ Reviewer (Code review)
└── 💬 Agent (Default, less constrained)

Prompts:
├── /review (Quick code review) ⭐ NEW
├── /verify (Anti-hallucination check) ⭐ NEW
├── /vue-component (Generate Vue component)
└── /net-service (Generate .NET service)

```

---

## 6. Why This Architecture Works

### Problem: Default Agent
- ❌ No constraints
- ❌ Adds "improvements"
- ❌ Token waste from exploration
- ❌ Hallucinations about file existence

### Solution: Builder Agent
- ✅ Enforces constraints from global instructions
- ✅ Stops when uncertain
- ✅ Tracks tool usage
- ✅ Minimal changes only
- ✅ Hands off to Reviewer automatically

### Bonus: Prompt Files
- ✅ Quick reviews without full agent session
- ✅ Verification before implementation
- ✅ Less context switching

---
```
