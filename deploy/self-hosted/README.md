# Balcao Livre self-hosted

Este pacote roda os sites do Balcao Livre no mesmo VPS do Supabase self-hosted.

O Supabase self-hosted fica como backend: Postgres, Auth, Storage, REST API, Realtime e Edge Functions. A landing page, o admin Next.js, o PDV Web e o cardapio publico rodam atras do Caddy/Nginx. O container `admin-api` continua servindo as rotas de licenca/suporte enquanto o backend do admin nao for migrado para rotas API do Next.js.

## URLs usadas

- `https://seudominio.com.br` landing page
- `https://seudominio.com.br/pdv` PDV Web, tambem opcional em `https://pdv.seudominio.com.br`
- `https://seudominio.com.br/admin` admin Next.js, tambem opcional em `https://admin.seudominio.com.br/admin`
- `https://seudominio.com.br/cardapio/demo` cardapio publico, tambem opcional em `https://cardapio.seudominio.com.br/demo`
- `https://supabase.seudominio.com.br` Supabase API/Studio

## Passo a passo no VPS

1. Aponte o DNS `A` do dominio e dos subdominios para o IP do VPS.

2. Suba o Supabase seguindo a doc oficial:

```bash
git clone --depth 1 https://github.com/supabase/supabase.git
mkdir -p ~/balcao/supabase
cp -rf supabase/docker/* ~/balcao/supabase/
cd ~/balcao/supabase
cp .env.example .env
```

3. No `.env` do Supabase, troque todas as senhas e chaves. Use:

```text
SITE_URL=https://seudominio.com.br
API_EXTERNAL_URL=https://supabase.seudominio.com.br
SUPABASE_PUBLIC_URL=https://supabase.seudominio.com.br
```

4. Inicie o Supabase:

```bash
docker compose up -d
```

5. Aplique a tabela do cardapio publico:

```bash
docker compose exec -T db psql -U postgres -d postgres < /caminho/do/repositorio/supabase/migrations/20260526010000_public_menu.sql
```

6. Configure os containers web:

```bash
cd /caminho/do/repositorio
cp deploy/self-hosted/.env.example deploy/self-hosted/.env
nano deploy/self-hosted/.env
docker compose -f deploy/self-hosted/docker-compose.yml --env-file deploy/self-hosted/.env up -d --build
```

## Como o QR do cardapio deve ficar

No app Windows, cole como URL publica:

```text
https://seudominio.com.br/cardapio/slug-da-loja
```

ou:

```text
https://cardapio.seudominio.com.br/slug-da-loja
```

O cliente escaneia o QR e abre usando qualquer internet do celular. Nao depende do Wi-Fi do restaurante.

## Segurança

- Nunca coloque `service_role`, `SUPABASE_SECRET_KEY` ou `JWT_SECRET` no PDV Web, landing page ou cardapio.
- O navegador usa apenas `SUPABASE_PUBLISHABLE_KEY`.
- O admin Next.js no navegador nao deve receber `SUPABASE_SECRET_KEY`. O `admin-api` pode usar `SUPABASE_SECRET_KEY` porque roda no servidor.
- As tabelas publicas do cardapio tem RLS e liberam somente `select` de menu publicado.
- Troque as senhas padrao do Supabase antes de abrir porta 80/443.
