---
description: Quick code review of current changes
agent: Reviewer
tools: ["search/codebase", "search", "search/usages"]
---

Review the code changes in this conversation or the files I specify.

**Review for:**

1. **Scope creep**: Did implementation add unnecessary changes?
2. **Security**: Auth checks, input validation, secrets exposure
3. **Multi-tenant**: Tenant isolation, query filtering
4. **Performance**: N+1 queries, missing indexes
5. **Architecture**: Business logic in correct layer
6. **Testing**: Missing tests for critical logic
7. **Token waste**: Unnecessary file reads, redundant operations

**Focus on:**

- Changes that were NOT explicitly requested
- Security/multi-tenant violations (CRITICAL)
- Performance issues
- Missing tests

**Output format:**

- ✅ What was done correctly
- 🔴 Critical issues (security, multi-tenant)
- 🟡 Important issues (performance, architecture)
- ⚠️ Scope creep detected (if any)
- 📊 Summary: Files changed, issues found, risk level

Be specific: reference file:line for each issue.
