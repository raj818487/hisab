# Deploy frontend to Netlify

1. Open [Netlify](https://app.netlify.com) → **Add new site** → **Import an existing project**.
2. Pick GitHub repo `raj818487/hisab`.
3. Settings (usually auto-read from `netlify.toml`):
   - **Base directory:** `frontend`
   - **Build command:** `npm ci && npm run build`
   - **Publish directory:** `frontend/dist/milk-hisab/browser` (or `dist/milk-hisab/browser` when base is `frontend`)
4. Deploy.

## API note

Netlify hosts **only the Angular UI**. The .NET API must run elsewhere (Railway, Azure, your VPS, etc.).

Until the API is online:
- Local: UI at `:4200` talks to API at `http://localhost:8080`.
- Production: set `apiBaseUrl` in `frontend/src/environments/environment.production.ts` to your public API (e.g. `https://api.example.com/api/v1`), **or** uncomment the `/api/*` proxy in `netlify.toml`.

CORS: allow your Netlify site origin on the API.