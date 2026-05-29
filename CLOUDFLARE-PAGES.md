# Deploy emergencial no Cloudflare Pages

Este pacote substitui o Netlify quando a cota acabar.

## Build

```powershell
node scripts\build-cloudflare-site.mjs
```

Saida:

```text
dist\cloudflare-site
outputs\balcaolivre-cloudflare-site.zip
```

## Publicacao rapida pelo painel

1. Abra Cloudflare > Workers & Pages.
2. Crie um Pages por Direct Upload.
3. Envie a pasta `dist\cloudflare-site` ou o zip `outputs\balcaolivre-cloudflare-site.zip`.
4. Adicione estes custom domains no mesmo projeto:
   - `balcaolivrepdv.com.br`
   - `www.balcaolivrepdv.com.br`
   - `admin.balcaolivrepdv.com.br`
   - `pdv.balcaolivrepdv.com.br`
   - `cardapio.balcaolivrepdv.com.br`

O arquivo `_worker.js` dentro do build faz as rotas por subdominio e o proxy de `/admin-api` para `https://balcaolivrepdv.onrender.com/api`.

## Publicacao por terminal

Depois de autenticar:

```powershell
npx wrangler login
npx wrangler pages deploy dist\cloudflare-site --project-name balcaolivrepdv
```
