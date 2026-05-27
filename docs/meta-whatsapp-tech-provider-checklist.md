# Meta WhatsApp Tech Provider checklist

Status atual:

- O PDV Windows ja abre o modulo WhatsApp automatico.
- A Edge Function `whatsapp` ja gera onboarding por loja e callback no Supabase.
- O app Meta bloqueou o fluxo com a mensagem de que Embedded Signup so esta disponivel para BSPs ou TPs.
- Entao o proximo passo real e liberar a empresa/app como Tech Provider ou usar um BSP/Tech Provider pronto.

## Antes de enviar para a Meta

- Verificar empresa no Meta Business Manager.
- Confirmar dominio do app:
  - `hzvplpotsdzxygkxrgyi.supabase.co`
  - dominio publico final do Balcao Livre, quando estiver pronto.
- Configurar OAuth redirect URI:
  - `https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/whatsapp/onboarding/callback`
- Manter webhook WhatsApp:
  - `https://hzvplpotsdzxygkxrgyi.supabase.co/functions/v1/whatsapp/webhook`
- App em modo live somente quando a revisao estiver pronta.
- Pagina de politica de privacidade publicada.
- Pagina de termos publicada.
- URL de exclusao de dados publicada, se a Meta solicitar.

## Permissoes para App Review

Pedir apenas o necessario:

- `whatsapp_business_management`
  - Usada para conectar WABA/numero da loja, ler status do numero e configurar webhook.
- `whatsapp_business_messaging`
  - Usada para enviar mensagens do PDV para clientes e receber mensagens/eventos via webhook.
- `business_management`
  - Pode ser exigida dependendo do fluxo de Tech Provider e assets do cliente.

Evitar pedir permissoes extras sem tela demonstrando uso real.

## Videos para revisao

Gravar videos separados, em velocidade normal, sem cortes bruscos.

### Video 1 - Conexao WhatsApp por loja

Objetivo: provar uso de `whatsapp_business_management`.

Roteiro:

1. Abrir o Balcao Livre PDV Online.
2. Abrir o modulo WhatsApp.
3. Informar o numero da loja.
4. Clicar em `Conectar numero`.
5. Mostrar a tela Meta de conexao.
6. Concluir o fluxo de conexao.
7. Voltar ao PDV e mostrar status conectado.
8. Mostrar que o PDV salvou phone number id/WABA no backend.

### Video 2 - Envio de mensagem pelo PDV

Objetivo: provar uso de `whatsapp_business_messaging`.

Roteiro:

1. Abrir um pedido no PDV.
2. Informar cliente com telefone.
3. Fechar ou atualizar o pedido.
4. Mostrar o PDV enviando mensagem pelo WhatsApp Cloud API.
5. Mostrar a mensagem chegando no WhatsApp do cliente.
6. Mostrar webhook/status de entrega retornando ao sistema.

### Video 3 - Recebimento e resposta

Objetivo: provar leitura de mensagens e webhook.

Roteiro:

1. Cliente envia uma mensagem para o numero da loja.
2. PDV recebe ou registra a mensagem via webhook.
3. PDV gera resposta/script do atendimento.
4. Mensagem sai pela API, sem WhatsApp Web.
5. Mostrar historico/status no PDV.

## Texto base para enviar na revisao

O Balcao Livre PDV Online e um sistema de ponto de venda para restaurantes. Cada loja conecta seu proprio numero de WhatsApp Business pela Meta. O PDV usa a permissao `whatsapp_business_management` para vincular o numero/WABA da loja e usa `whatsapp_business_messaging` para enviar mensagens operacionais ao cliente, como confirmacao de pedido, preparo, despacho e suporte. O sistema tambem recebe webhooks para mensagens recebidas e status de entrega. O envio e feito pela WhatsApp Cloud API; o PDV nao automatiza WhatsApp Web.

## Atalho para lancar mais rapido

Se a aprovacao propria demorar, usar um BSP/Tech Provider pronto para onboarding dos clientes:

- Twilio
- 360dialog
- Zenvia
- Blip
- Gupshup
- SendPulse, se oferecer WhatsApp Business API com onboarding por cliente

Nesse caminho, o Balcao Livre integra com a API do provedor agora e a aprovacao propria da Meta fica como etapa paralela.
