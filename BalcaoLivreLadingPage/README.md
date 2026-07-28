# Balcao Livre Landing Page

Landing page em Next.js, sem Tailwind e sem bibliotecas visuais externas para manter o projeto leve.

## Cloudflare Next.js

O deploy principal roda no Cloudflare Workers com OpenNext. O middleware do Next le o host e reescreve `nomedaloja.balcaolivrepdv.com.br` para `/agenda/nomedaloja`.

Dominios esperados no mesmo Worker:

- `balcaolivrepdv.com.br`
- `www.balcaolivrepdv.com.br`
- `*.balcaolivrepdv.com.br` para sites publicos de agendamento

## Agenda Livre publico

O site de agendamento usa o subdominio como slug da loja. A raiz de `nomedaloja.balcaolivrepdv.com.br` renderiza o site publico daquele parceiro.

Para colocar em producao:

1. Aponte um wildcard DNS `*.balcaolivrepdv.com.br` para o Worker da landing no Cloudflare.
2. Configure o wildcard route no `wrangler.jsonc`.
3. Rode a migration `supabase/migrations/*_agenda_public_booking.sql`.
4. Configure `SUPABASE_URL` e `SUPABASE_SERVICE_ROLE_KEY` como secrets do Worker.

As solicitacoes entram na tabela `public.agenda_public_booking_requests`. A tabela fica com RLS ativo e sem acesso publico direto; o insert passa pela rota server-side `/api/agenda/appointments`.

## Rodar local

```powershell
npm install
npm run dev
```

## Deploy Cloudflare

Use Node 22 ou superior, porque o Wrangler atual exige esse runtime.

```powershell
npm install
npm run build
npm run build:cloudflare
npx wrangler secret put SUPABASE_URL
npx wrangler secret put SUPABASE_SERVICE_ROLE_KEY
npm run deploy:cloudflare
```

## Stripe Checkout

Configure a chave secreta somente no servidor, em `.env.local`.
Nunca coloque `sk_live` no codigo, no HTML ou no GitHub.

```powershell
Copy-Item .env.example .env.local
# Edite .env.local e preencha STRIPE_SECRET_KEY
```

Os planos usam estes Price IDs:

- Mensal: `price_1Tb3fcGTOG08DTzfMZxooHqI`
- Anual: `price_1Tb3fcGTOG08DTzfsyFfmjRZ`

## Build local

```powershell
npm run build
npm start
```

As imagens usadas ficam em `public/` e vieram dos arquivos locais do projeto.
