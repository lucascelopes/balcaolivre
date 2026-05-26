# Balcão Livre Landing Page

Landing page em Next.js, sem Tailwind e sem bibliotecas visuais externas para manter o projeto leve.

## Preview sem baixar dependências

Abra `preview.html` no navegador para conferir o visual sem rodar `npm install`.

Na publicacao pela raiz do repositorio, o `vercel.json` deixa a landing em `/`, o admin em `/admin` e o PDV web em `/pdv`. O link `Login` da landing aponta para `/pdv/`.

## Rodar local

```powershell
npm install
npm run dev
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

## Build

```powershell
npm run build
npm start
```

As imagens usadas ficam em `public/` e vieram dos arquivos locais do projeto.
