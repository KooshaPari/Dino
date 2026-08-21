export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (url.pathname === '/health') return fetch(env.ORIGIN + '/health');
    if (url.pathname === '/tools') {
      const cacheKey = new Request(url.toString(), request);
      const cached = await caches.default.match(cacheKey);
      if (cached) return cached;
      const response = await fetch(env.ORIGIN + '/tools');
      const cloned = response.clone();
      cloned.headers.set('Cache-Control', 'public, max-age=60');
      await caches.default.put(cacheKey, cloned);
      return response;
    }
    return fetch(request);
  },
};