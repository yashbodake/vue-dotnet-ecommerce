# NEXT TASK — paste into a small-model chat

**Slice:** S05_yarp  
**Queue:** See `EXECUTION_QUEUE.md` — mark T05.1 done after acceptance.

## Attach these files only
1. `agent-kit/00_GLOBAL/RULES.md`
2. `agent-kit/slices/S05_yarp/CONTEXT.md`
3. `agent-kit/slices/S05_yarp/FILES.md`
4. This file (or paste below)

---

### T05.1 — Configure YARP routes + health
- Model tier: small
- Goal: api-route + vue-route clusters; gateway health JSON
- Allowed write paths: `Ecommerce.Gateway/**`
- Acceptance: With api+vite running, `GET http://127.0.0.1:5000/api/health` and `GET http://127.0.0.1:5000/` succeed via `modern-up` or manual
- Stop condition: Gateway proxies both

---

After success: check off T05.1 in `EXECUTION_QUEUE.md` and replace this file's TASK with **T05.2** from `slices/S05_yarp/TASKS.md`.