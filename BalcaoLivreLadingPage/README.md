# Balcão Livre Landing Page

Landing page em Next.js, sem Tailwind e sem bibliotecas visuais externas para manter o projeto leve.

## Preview sem baixar dependências

Abra `preview.html` no navegador para conferir o visual sem rodar `npm install`.

Na publicacao pela raiz do repositorio, o build da Netlify deixa a landing no dominio principal, o admin em `admin.balcaolivrepdv.com.br`, o PDV web em `pdv.balcaolivrepdv.com.br` e cada cardapio em `cardapio.balcaolivrepdv.com.br/slug-da-loja`. O link `Login` da landing aponta para `https://pdv.balcaolivrepdv.com.br`.

Dominios esperados no mesmo site Netlify:

- `balcaolivrepdv.com.br`
- `www.balcaolivrepdv.com.br`
- `admin.balcaolivrepdv.com.br`
- `pdv.balcaolivrepdv.com.br`
- `cardapio.balcaolivrepdv.com.br`

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
