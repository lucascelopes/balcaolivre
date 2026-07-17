# Deploy do iFood distribuido

Este fluxo prepara a App Teste (D) sem substituir as credenciais centralizadas existentes.

## Estado validado

- Edge Function: `deno check` aprovado.
- Cliente Windows: `IFoodCloudClient.cs` compilado com `IFoodModels.cs` e `IFoodIntegrationSettings.cs`.
- Wiring do modal Windows: inicio, conclusao, retry `awaiting_merchant` e desconexao conferidos.
- Workflow de validacao: https://github.com/lucascelopes/balcaolivre/actions/runs/29585222380

O build completo do snapshot remoto do PDV possui falhas antigas em WhatsApp/mobile e nao e usado como prova para este deploy.

## Secrets necessarios no GitHub

Cadastre em **Settings > Secrets and variables > Actions**:

- `SUPABASE_ACCESS_TOKEN`
- `IFOOD_DISTRIBUTED_CLIENT_ID`
- `IFOOD_DISTRIBUTED_CLIENT_SECRET`

Nao grave valores em arquivos, commits ou logs. Se o client secret apareceu em uma captura de tela, gere um novo antes do deploy.

## Execucao manual protegida

Abra o workflow **Deploy iFood distributed to Supabase** e use **Run workflow**.

Preencha:

- `confirm_project_ref`: `hzvplpotsdzxygkxrgyi`
- `confirm_deploy`: marcado

O workflow:

1. confirma o project ref;
2. exige os tres secrets;
3. aplica somente `20260717000000_ifood_event_dedup.sql`;
4. configura os secrets distribuídos no Supabase;
5. publica apenas a funcao `ifood` com `verify_jwt=false`, mantendo a autenticacao propria por licenca e maquina;
6. confirma o indice, a funcao e os nomes dos secrets.

O deploy usa a CLI oficial conforme a documentacao do Supabase:
- https://supabase.com/docs/guides/functions/examples/github-actions
- https://supabase.com/docs/reference/cli/supabase-bootstrap

## Teste da App Teste (D)

1. No PDV, abra **iFood > Conectar iFood**.
2. Gere um novo codigo de usuario.
3. Autorize uma unica loja no Portal do Parceiro.
4. Cole o authorization code no mesmo modal.
5. Se aparecer `awaiting_merchant`, aguarde a propagacao e clique **Verificar liberacao** sem gerar outro codigo.
6. Confirme que exatamente um merchant foi vinculado.
7. Valide polling a cada 30 segundos, persistencia do evento antes do ACK e entrada do pedido no Delivery.

Depois da homologacao, a App distribuida de producao tera outro clientId. Cada loja precisara autorizar novamente; tokens da App Teste (D) nao migram.
