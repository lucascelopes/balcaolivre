# Netlify e dominios do Balcao Livre

O site Netlify publica todas as superficies no mesmo deploy:

- `balcaolivrepdv.com.br`: landing page.
- `www.balcaolivrepdv.com.br`: landing page.
- `admin.balcaolivrepdv.com.br`: admin estatico.
- `pdv.balcaolivrepdv.com.br`: PDV web.
- `cardapio.balcaolivrepdv.com.br`: cardapio publico de cada loja por slug.

O cardapio usa o subdominio como slug. Exemplo:

```text
cardapio.balcaolivrepdv.com.br/balcao-livre-pdv-online-7011ff
```

Esse host carrega os arquivos de `BalcaoLivre.Cardapio.Web` e busca os dados publicados no Supabase pelas tabelas `bv_public_menus` e `bv_public_menu_items`.

## DNS

No provedor do dominio ou no Netlify DNS, configure:

```text
balcaolivrepdv.com.br        -> site Netlify
www                         -> CNAME do site Netlify
admin                       -> CNAME do site Netlify
pdv                         -> CNAME do site Netlify
cardapio                    -> CNAME do site Netlify
```

Depois, no painel da Netlify, adicione os dominios acima no mesmo site e emita HTTPS para eles. Esta estrutura evita depender de wildcard `*.balcaolivrepdv.com.br` no plano/painel da Netlify.

## Build

O deploy usa o `netlify.toml` da raiz:

```powershell
node scripts/build-netlify-site.mjs
```

O script gera `dist/netlify-site` com:

```text
/index.html      landing
/admin           admin
/pdv             PDV web
/cardapio        cardapio publico
/_redirects      roteamento por subdominio
```
