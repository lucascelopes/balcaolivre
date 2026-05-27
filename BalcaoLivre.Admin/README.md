# Balcao Livre PDV Admin

Painel web interno para controle de licencas, clientes instalados e uso do app Windows.

## Login admin

O login do painel nao tem senha padrao no codigo. Configure sempre por variaveis de ambiente no servidor:

```powershell
$env:BVPDV_ADMIN_USER = "seu-login-admin"
$env:BVPDV_ADMIN_PASSWORD = "sua-senha-forte"
```

## Rodar local

```powershell
dotnet run --project .\BalcaoLivre.Admin\BalcaoLivre.Admin.csproj
```

URL padrao:

```text
http://localhost:5188
```

No Render, crie como Docker Web Service. O admin usa automaticamente a variavel `PORT` do Render.

## Vercel

O `vercel.json` da raiz publica:

- `/admin` para este painel estatico.
- `/admin-api/*` como proxy para `https://balcaolivrepdv.onrender.com/api/*`.
- `/pdv` para o PDV Web.
- `/` para a landing page.

## Supabase

O admin usa Supabase Storage como armazenamento central. Configure no servidor do admin:

```powershell
$env:BVPDV_SUPABASE_URL = "https://hzvplpotsdzxygkxrgyi.supabase.co"
$env:BVPDV_SUPABASE_SECRET_KEY = "sua-secret-key-ou-service-role"
```

O admin cria automaticamente um bucket privado chamado `balcao-livre-admin` e salva `admin-store.json` nele.

Nao coloque `secret key`/`service_role` dentro do app Windows do cliente. Essa chave fica somente no servidor/admin.

O fallback em JSON local fica desligado por padrao. Para desenvolvimento isolado, da para liberar explicitamente:

```powershell
$env:BVPDV_REQUIRE_SUPABASE = "0"
$env:BVPDV_ADMIN_DATA = "C:\BalcaoLivreAdminData"
```

## Como funciona

- O admin cria chaves no formato `BLV-...`.
- O PDV valida a assinatura localmente para continuar funcionando offline.
- Quando ha internet e o admin esta acessivel, o PDV chama `/api/app/activate` e `/api/app/checkin`.
- Com Supabase configurado, licencas, clientes, perfil do restaurante, configuracoes e check-ins de uso ficam no Supabase.
- O admin nao recebe totais de venda, caixa ou itens vendidos.
- O admin vincula a chave ao primeiro computador que ativar.
- Se a mesma chave for usada em outro PC com o admin online, ela e bloqueada pela API.
