// Fixed-window in-memory limiter. Use Redis or similar for multi-instance deployments.
function createRateLimiter({ max, windowMs, now = () => Date.now() }) {
  const hits = new Map(); // key -> {count, resetAt}
  return function allow(key) {
    const t = now();
    let h = hits.get(key);
    if (!h || h.resetAt <= t) { h = { count: 0, resetAt: t + windowMs }; hits.set(key, h); }
    h.count++;
    if (hits.size > 10000) for (const [k, v] of hits) if (v.resetAt <= t) hits.delete(k);
    return h.count <= max;
  };
}

module.exports = { createRateLimiter };
