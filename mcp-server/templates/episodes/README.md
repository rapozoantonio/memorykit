# Project Episodes

<!--
  Historical events, bugs, incidents, and their resolutions.
  Episodes capture what went wrong, why, and how it was fixed.
  Helps prevent repeating past mistakes.

  Entries use MML (Memory Markup Language) format.
-->

---

## Example Entry

### Race condition in payment processing

- **what**: race condition in OrderService.ProcessPayment() causing duplicate charges
- **symptom**: multiple charges for same order in production logs
- **root-cause**: concurrent requests modifying same order row without isolation
- **fix**: added IsolationLevel.Serializable to transaction scope
- **file**: src/Services/OrderService.cs#L142
- **tags**: bug, race-condition, payment, concurrency
- **importance**: 0.75
- **created**: 2026-02-17

### Auth token expiry not handled

- **what**: JWT tokens expiring mid-session caused crashes
- **symptom**: 401 errors with cryptic "Invalid token" message
- **workaround**: frontend now refreshes tokens every 50 minutes
- **why-happened**: refresh token rotation not implemented
- **tags**: auth, jwt, bug, token-expiry
- **importance**: 0.65
- **created**: 2026-02-10

---

**MML Keys for Episodes:**

- **what** (required): Brief description of the event
- **symptom**: User-visible manifestation of the problem
- **root-cause**: Why it happened
- **fix**: How it was resolved
- **workaround**: Temporary mitigation
- **why-happened**: Broader context or mistake that led to it
- **file**: File path and line number if applicable
- **tags** (required): Comma-separated keywords
- **importance** (auto): Calculated by Amygdala engine
- **created** (auto): Date stored
