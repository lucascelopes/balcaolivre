# Balcao Livre PDV Admin

Painel web interno para controle de licencas, clientes instalados e uso do app Windows.

## Login admin

Em producao, o painel autentica diretamente pelo Supabase Auth. O email precisa
existir no Supabase e estar na lista `ADMIN_EMAILS` do `wrangler.jsonc`.
`BVPDV_ADMIN_USER` e `BVPDV_ADMIN_PASSWORD` sao usados apenas pelo backend .NET
local legado.

## Rodar local

```powershell
dotnet run --project .\BalcaoLivre.Admin\BalcaoLivre.Admin.csproj
```

URL padrao:

```text
http://localhost:5188
```

## Cloudflare — admin.balcaolivrepdv.com.br

O painel de produção é preparado como um Cloudflare Worker com Static Assets:

- `wwwroot` é publicado na borda da Cloudflare.
- `/admin-api/login` autentica pelo Supabase Auth.
- `/admin-api/*` le e grava diretamente no Supabase Storage e nas tabelas de licenca.
- `/admin-api/visits/plan` roda no próprio Worker e usa o secret do OpenRouter.
- Antes de qualquer operacao administrativa, o Worker valida a sessao no Supabase.
- Coordenadas exatas, telefone e CNPJ não são enviados ao OpenRouter.

Instale as dependências e valide o pacote:

```powershell
cd .\BalcaoLivre.Admin
npm install
npm run check
```

Cadastre os secrets de forma interativa — não coloque os valores no arquivo:

```powershell
npm run secret:supabase
npm run secret:openrouter
```

Depois publique:

```powershell
npm run deploy
```

O `wrangler.jsonc` associa o Worker à rota específica
`admin.balcaolivrepdv.com.br/*`. Essa rota tem prioridade sobre a rota curinga
`*.balcaolivrepdv.com.br/*` da landing page sem alterar os outros subdomínios.

O `OPENROUTER_API_KEY` é um secret opcional deste Worker. Sem ele, o painel
continua funcionando e usa o planejamento local de visitas. O
`SUPABASE_SERVICE_ROLE_KEY` é obrigatório e fica protegido nos secrets do Worker.

## Vercel

O `vercel.json` da raiz publica:

- `/admin` para este painel estatico.
- `/admin-api/*` como proxy para `https://balcaolivrepdv.onrender.com/api/*`.
- `/pdv` para o PDV Web.
- `/` para a landing page.

## Supabase

O admin usa Supabase Auth para login e Supabase Storage como armazenamento central.

O admin cria automaticamente um bucket privado chamado `balcao-livre-admin` e salva `admin-store.json` nele.

Nao coloque `secret key`/`service_role` dentro do app Windows do cliente. Essa chave fica somente no servidor/admin.

O fallback em JSON local fica desligado por padrao. Para desenvolvimento isolado, da para liberar explicitamente:

```powershell
$env:BVPDV_REQUIRE_SUPABASE = "0"
$env:BVPDV_ADMIN_DATA = "C:\BalcaoLivreAdminData"
```

## Planejamento inteligente de visitas

A rota usa três camadas independentes:

- Geolocalização do navegador para acompanhar a posição do usuário.
- OpenStreetMap + OSRM para calcular distância, duração e polyline pelas ruas.
- OpenRouter somente para priorizar as oportunidades e explicar a ordem sugerida.

Configure a chave apenas no servidor:

```powershell
$env:OPENROUTER_API_KEY = "sua-chave-openrouter"
```

No Cloudflare, use `npm run secret:openrouter` em vez da variável PowerShell. O modelo configurado é `openrouter/free`. Se a chave não existir, o limite gratuito for atingido ou a IA demorar, o painel aplica automaticamente o plano local por distância e potencial comercial. A chave nunca é enviada ao navegador.

## Como funciona

- O admin cria chaves no formato `BLV-...`.
- O PDV valida a assinatura localmente para continuar funcionando offline.
- Quando ha internet e o admin esta acessivel, o PDV chama `/api/app/activate` e `/api/app/checkin`.
- Com Supabase configurado, licencas, clientes, perfil do restaurante, configuracoes e check-ins de uso ficam no Supabase.
- O admin nao recebe totais de venda, caixa ou itens vendidos.
- O admin vincula a chave ao primeiro computador que ativar.
- Se a mesma chave for usada em outro PC com o admin online, ela e bloqueada pela API.
