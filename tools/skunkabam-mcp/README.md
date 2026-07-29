# SkunKabam Codex MCP

MCP local para registrar no Supabase o que acontece no Codex e aparecer no Kanban do SkunKabam.

## Como funciona

- O MCP roda localmente via stdio.
- Ele chama a Edge Function `skunkabam-codex`.
- A function valida `deviceId` e `deviceSecret` do arquivo local do PC.
- Nessa versao nao usa licenca; os registros ficam separados pelo `deviceId`.
- As mensagens, cards, acoes e links ficam nas tabelas `skunkabam_codex_*`.

## Configuracao

Use variaveis de ambiente ou crie um arquivo de link em:

```text
%APPDATA%\SkunKabam\codex-link.json
```

Exemplo:

```json
{
  "supabaseUrl": "https://hzvplpotsdzxygkxrgyi.supabase.co",
  "deviceId": "pc-do-user",
  "deviceSecret": "segredo_gerado_no_pc",
  "machineCode": "PC-CAIXA-01",
  "storeName": "Skun Kabam Centro"
}
```

Tambem pode usar:

```text
SKUN_KABAM_SUPABASE_URL
SKUN_KABAM_DEVICE_ID
SKUN_KABAM_DEVICE_SECRET
SKUN_KABAM_MACHINE_CODE
SKUN_KABAM_STORE_NAME
SKUN_KABAM_CODEX_FUNCTION_URL
```

## Ferramentas MCP

- `skunkabam_registrar_chat`
- `skunkabam_atualizar_card`
- `skunkabam_registrar_acao`
- `skunkabam_registrar_link`
- `skunkabam_listar_cards`
- `skunkabam_obter_thread`

## Rodar

```powershell
cd tools\skunkabam-mcp
npm install
npm run check
node .\src\index.js
```

## Gerar arquivo de link no Windows

```powershell
.\write-codex-link.ps1 `
  -MachineCode "PC-CAIXA-01" `
  -StoreName "Skun Kabam Centro"
```

O script gera um `deviceSecret` e mostra o comando `supabase secrets set` para salvar o mesmo segredo na Edge Function. Esse arquivo nao deve receber `service_role`.
