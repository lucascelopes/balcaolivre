# Balcao Livre PDV Online - instalador e atualizacao

Este projeto publica o PDV Online como instalador independente `.exe`. Ele usa nome, pasta, AppId e manifesto separados do RestaurantePro/offline, entao os dois podem ficar instalados na mesma maquina.

## Fluxo recomendado

1. Gerar publish do app:

```powershell
dotnet publish .\BalcaoLivre.Online.Windows\BalcaoLivre.Online.Windows.csproj -c Release -r win-x64 --self-contained true -o .\BalcaoLivre.Online.Windows\bin\Release\net9.0-windows\win-x64\publish-online-self-contained
```

2. Gerar o instalador com Inno Setup:

```powershell
winget install --id JRSoftware.InnoSetup -e --silent --accept-package-agreements --accept-source-agreements
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\installer\BalcaoLivrePDV.iss
```

3. Subir estes arquivos no Supabase Storage:

```text
bucket: balcao-livre-updates
path: windows-online/version.json
path: windows-online/BalcaoLivrePDVOnline-Setup-1.7.2026.exe
```

Ou publicar direto pelo script, usando a service role apenas no seu terminal:

```powershell
$env:SUPABASE_SERVICE_ROLE_KEY = "sua-service-role-key"
.\installer\publish-to-supabase.ps1
```

4. No app, a URL padrao de atualizacao fica em:

```text
https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/version.json
```

## Como publicar uma nova versao

1. Aumente a versao no `.csproj`.
2. Atualize `MyAppVersion` em `installer/BalcaoLivrePDV.iss`.
3. Publique o app e compile o instalador.
4. Suba o novo `.exe` no Supabase Storage.
5. Atualize `installer/version.json` com a nova versao e URL do instalador.
6. Suba o `version.json` por ultimo.

Subir o `version.json` por ultimo evita o cliente ver uma versao nova antes do instalador estar disponivel.

## Atualizacao obrigatoria sem perder dados

Os dados do cliente nao ficam na pasta instalada do programa. O instalador troca somente os arquivos do app em:

```text
%LOCALAPPDATA%\Programs\Balcao Livre PDV Online
```

Key, conta, produtos, vendas, estoque, configuracoes e backups ficam em:

```text
%LOCALAPPDATA%\BalcaoLivre.Online.Windows
```

Antes de abrir o instalador, o app salva os dados atuais e cria uma copia em:

```text
%LOCALAPPDATA%\BalcaoLivre.Online.Windows\backups\pre-update
```

Para inutilizar uma versao antiga, publique o instalador novo primeiro e depois atualize o `version.json`:

```json
{
  "version": "1.7.2026",
  "installerUrl": "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/BalcaoLivrePDVOnline-Setup-1.7.2026.exe",
  "minimumVersion": "1.7.2026",
  "required": true,
  "notes": "Atualizacao obrigatoria."
}
```

`minimumVersion` maior que a versao instalada bloqueia o app antigo. `required: true` tambem força a atualizacao quando o `version` publicado for mais novo.
