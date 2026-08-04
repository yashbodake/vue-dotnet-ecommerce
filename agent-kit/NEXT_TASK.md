# NEXT TASK

**Slice:** S07_checkout
**Queue:** See `EXECUTION_QUEUE.md` — mark T07.1 done after acceptance.

## Attach these files only
1. `agent-kit/00_GLOBAL/RULES.md`
2. `agent-kit/slices/S07_checkout/CONTEXT.md`
3. `agent-kit/slices/S07_checkout/FILES.md`
4. This file (or paste below)

---

### T07.1 — CheckoutService + endpoints
- Model tier: small
- Goal: shipping-options + place-order with validation
- Allowed write paths: Api checkout*
- Acceptance: Authenticated POST place-order with cart creates order; second GET cart empty
- Stop condition: API order created

---

After success: check off T07.1 in `EXECUTION_QUEUE.md` and replace this file's TASK with **T07.2** from `slices/S07_checkout/TASKS.md`.