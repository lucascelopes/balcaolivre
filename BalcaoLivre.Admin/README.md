# Balcao Livre PDV Admin

Painel web interno para controle de licencas, clientes instalados e metricas enviadas pelo app Windows.

## Login padrao

- Login: `balcaoVirtualPDV`
- Senha: `BVPDV24055`

Em producao, da para sobrescrever com variaveis de ambiente:

```powershell
$env:BVPDV_ADMIN_USER = "seu-login"
$env:BVPDV_ADMIN_PASSWORD = "sua-senha"
```

## Rodar local

```powershell
dotnet run --project .\BalcaoLivre.Admin\BalcaoLivre.Admin.csproj
```

URL padrao:

```text
http://localhost:5188
```

## Dados

Sem Supabase configurado, o painel salva em:

```text
BalcaoLivre.Admin\App_Data\admin-store.json
```

Tambem pode usar:

```powershell
$env:BVPDV_ADMIN_DATA = "C:\BalcaoLivreAdminData"
```

## Supabase

Para producao, use Supabase como banco central do admin. Rode o SQL de `supabase-schema.sql` no SQL Editor do projeto e configure no servidor do admin:

```powershell
$env:BVPDV_SUPABASE_URL = "https://hzvplpotsdzxygkxrgyi.supabase.co"
$env:BVPDV_SUPABASE_SECRET_KEY = "sua-secret-key-ou-service-role"
```

Nao coloque `secret key`/`service_role` dentro do app Windows do cliente. Essa chave fica somente no servidor/admin.

## Como funciona

- O admin cria chaves no formato `BLV-...`.
- O PDV valida a assinatura localmente para continuar funcionando offline.
- Quando ha internet e o admin esta acessivel, o PDV chama `/api/app/activate` e `/api/app/checkin`.
- Com Supabase configurado, licencas, clientes, perfil do restaurante, configuracoes e metricas ficam no Supabase.
- O admin vincula a chave ao primeiro computador que ativar.
- Se a mesma chave for usada em outro PC com o admin online, ela e bloqueada pela API.
