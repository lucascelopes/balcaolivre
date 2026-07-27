# Deploy do iFood distribuido

Este fluxo prepara a App Teste (D) sem substituir as credenciais centralizadas existentes.

## Estado validado

- Edge Function: `deno check` aprovado com `@supabase/supabase-js` fixado em `2.110.7`.
- Migration: validada em Postgres 15 com duplicata real de teste, indices, deduplicacao e RLS.
- Cliente Windows: `IFoodCloudClient.cs` compilado com `IFoodModels.cs` e `IFoodIntegrationSettings.cs`.
- Wiring do modal Windows: inicio, conclusao, retry `awaiting_merchant` e desconexao conferidos.
- Compatibilidade: clientes anteriores continuam em `/connect/start`; somente o novo cliente usa `/connect/distributed/start`.
- Workflow de validacao: https://github.com/lucascelopes/balcaolivre/actions/runs/29587746385

O build completo do snapshot remoto do PDV possui falhas antigas em WhatsApp/mobile e nao e usado como prova para este deploy.

## Secrets necessarios no GitHub

Cadastre em **Settings > Secrets and variables > Actions**:

- `SUPABASE_ACCESS_TOKEN`
- `SUPABASE_DB_PASSWORD`
- `IFOOD_DISTRIBUTED_CLIENT_ID`
- `IFOOD_DISTRIBUTED_CLIENT_SECRET`

Nao grave valores em arquivos, commits ou logs. Se o client secret apareceu em uma captura de tela, gere um novo antes do deploy.

O job usa o ambiente GitHub **production**. Configure um reviewer obrigatorio nesse ambiente antes de liberar o workflow.

## Execucao manual protegida

Abra o workflow **Deploy iFood distributed to Supabase** e use **Run workflow**.

Preencha:

- `confirm_project_ref`: `hzvplpotsdzxygkxrgyi`
- `confirm_migration_version`: `20260717000000`
- `confirm_deploy`: marcado

O workflow:

1. confirma o project ref e a migration exata;
2. exige os quatro secrets;
3. fixa a Supabase CLI em `2.109.1` e confere os comandos usados;
4. mostra o historico remoto e aplica somente `20260717000000_ifood_event_dedup.sql`;
5. valida deduplicacao, definicao do indice unico, indices de polling e RLS;
6. registra `20260717000000` no historico de migrations do Supabase;
7. configura os secrets distribuidos no Supabase;
8. publica apenas a funcao `ifood`, usando `verify_jwt=false` do `supabase/config.toml` porque a funcao valida licenca e maquina;
9. confirma funcao, nomes dos secrets e resposta publica esperada da rota.

O deploy usa a CLI oficial conforme a documentacao do Supabase:

- https://supabase.com/docs/guides/deployment/database-migrations
- https://supabase.com/docs/guides/deployment/managing-environments
- https://supabase.com/docs/guides/functions/examples/github-actions

## Teste da App Teste (D)

1. No PDV, abra **iFood > Conectar iFood**.
2. Gere um novo codigo de usuario.
3. Autorize uma unica loja no Portal do Parceiro.
4. Cole o authorization code no mesmo modal.
5. Se aparecer `awaiting_merchant`, aguarde a propagacao e clique **Verificar liberacao** sem gerar outro codigo.
6. Confirme que exatamente um merchant foi vinculado.
7. Valide polling a cada 30 segundos, persistencia do evento antes do ACK e entrada do pedido no Delivery.

Depois da homologacao, a App distribuida de producao tera outro clientId. Cada loja precisara autorizar novamente; tokens da App Teste (D) nao migram.
