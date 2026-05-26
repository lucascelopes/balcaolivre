# Balcao Livre PDV Web

PWA offline-first do PDV. A venda sempre grava primeiro no IndexedDB local do navegador e so depois entra na fila de sincronizacao.

## Rodar local

```powershell
python -m http.server 4174 --bind 127.0.0.1
```

Abra:

```text
http://127.0.0.1:4174
```

## Bancos locais

- `products`
- `customers`
- `cash_sessions`
- `sales`
- `sale_items`
- `payments`
- `sync_queue`
- `sync_state`
- `terminal_settings`

## Sync

O botao `Config` abre as mesmas configuracoes principais do app Windows e salva localmente:

- ID da loja
- ID do terminal
- URL do admin
- Supabase URL e anon key para login compartilhado com o app Windows

Quando houver internet, o PDV envia lotes pequenos:

```json
{
  "store_id": "loja_demo",
  "terminal_id": "caixa_01",
  "events": [
    {
      "event_id": "event_uuid",
      "type": "sale_created",
      "created_at": "2026-05-25T00:00:00.000Z",
      "payload": {}
    }
  ]
}
```

Sem endpoint ou sem internet, a venda continua salva no navegador e fica pendente em `sync_queue`.

## Login Supabase

No Vercel, configure as variaveis:

- `SUPABASE_URL`
- `SUPABASE_ANON_KEY`

O endpoint `/api/supabase-config` entrega somente a URL e a anon key publica para o PDV Web. Nunca coloque `service_role` no navegador.

## Funcoes implementadas

- Pesquisa de produtos com inclusao direta na comanda.
- Transferencia de comanda para outra mesa.
- Desconto percentual por venda.
- Cadastro local de clientes.
- Reabertura de vendas locais finalizadas.
- Cadastro local de equipe/operadores.
- Cadastro local de produtos.
- Caixa com suprimento/sangria e abrir/fechar.
- Novo delivery.
- Estoque/receitas com ajuste de saldo e minimo.
- Monitor cozinha com comandas abertas.
