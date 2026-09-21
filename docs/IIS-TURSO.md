# Deploy API on Windows / IIS (fix Turso crash)

The error means the host started the API **without** Turso credentials.

`appsettings.Secrets.json` is **gitignored** (correct — never commit tokens). Your local machine has it; the server does not.

## Fix (pick one)

### A) Upload secrets file (fastest)

1. On your PC open: `D:\Other Project\Milk-Hisab\backend\appsettings.Secrets.json`
2. On the host, upload that file into the site root next to `MilkHisab.Api.dll`
   (your log said: `D:\Sites\site93026\wwwroot\`)
3. Recycle the app pool / restart the site.

### B) Hosting panel environment variables

Set either:

- `ConnectionStrings__Default` = `Data Source=libsql://…;Auth Token=…`

or both:

- `Turso__Url` = `libsql://…`
- `Turso__AuthToken` = `…`

ASP.NET Core maps `__` to `:` automatically.

### C) `web.config` on the server (if panel allows)

Inside `<aspNetCore>`:

```xml
<environmentVariables>
  <environmentVariable name="Turso__Url" value="libsql://YOUR-DB.turso.io" />
  <environmentVariable name="Turso__AuthToken" value="YOUR_TOKEN" />
</environmentVariables>
```

## After it starts

Health check your site `/swagger` or `/api/v1/...` health endpoint.

For Netlify UI later, allow your Netlify origin in API CORS.