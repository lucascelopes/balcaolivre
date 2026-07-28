# Deploy Cloudflare

O fluxo principal do site novo e Next.js no Cloudflare Workers via OpenNext, dentro de `BalcaoLivreLadingPage`.

## Next.js / Agenda Livre

Requisitos:

- Node 22+
- `SUPABASE_URL` como secret do Worker
- `SUPABASE_SERVICE_ROLE_KEY` como secret do Worker
- wildcard DNS/route para `*.balcaolivrepdv.com.br`

```powershell
cd BalcaoLivreLadingPage
npm install
npm run build
npm run build:cloudflare
npx wrangler secret put SUPABASE_URL
npx wrangler secret put SUPABASE_SERVICE_ROLE_KEY
npm run deploy:cloudflare
```

O subdominio `nomedaloja.balcaolivrepdv.com.br` cai no middleware do Next e renderiza `/agenda/nomedaloja`.

## Build estatico legado

Existe um build estatico antigo para emergencia:

```powershell
node scripts\build-cloudflare-site.mjs
```

Use esse caminho somente para fallback estatico, nao para o site de agendamento.
