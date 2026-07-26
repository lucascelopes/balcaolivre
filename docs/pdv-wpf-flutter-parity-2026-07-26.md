# Matriz de paridade — Balcão Livre PDV WPF, PDV Web e Flutter

Data da verificação: 26 de julho de 2026.

## Objetivo preservado

O destino é um único produto Flutter com paridade funcional e visual com o
`BalcaoLivre.Online.Windows`, executável em Web, Android/iOS e desktop, com
operação offline e sincronização em nuvem. Esta matriz não reduz esse escopo;
ela registra o que já existe e o que ainda impede afirmar “tudo igual”.

## Evidências desta execução

- Git: `HEAD` e `origin/main` apontavam para `0b0b324`; a árvore local contém
  muitas mudanças ainda não commitadas e o diretório `AgendaLivre.Flutter`
  ainda estava não rastreado.
- PDV Web executado em `http://127.0.0.1:4174`.
- Flutter: `flutter analyze` sem problemas.
- Flutter: suíte completa terminou com `249` testes aprovados e `23` falhas
  antes da primeira correção desta rodada.
- Contrato WPF/Flutter: a divergência de `MarketingSitePromotion` foi corrigida;
  os testes focados de contrato e serialização passaram (`4/4`).
- O WPF não pôde ser recompilado nesta máquina porque o projeto usa
  `net9.0-windows` e só há SDK .NET 8 instalado. A análise do WPF abaixo usa o
  código-fonte atual; a execução visual do WPF continua como verificação
  pendente.

Capturas atuais:

1. `artifacts/pdv-parity-audit-2026-07-26/01-pdv-web-activation.png`
2. `artifacts/pdv-parity-audit-2026-07-26/02-pdv-web-license-blocker.png`
3. `artifacts/pdv-parity-audit-2026-07-26/03-flutter-pdv-desktop-current.png`

## Veredito

Ainda não há paridade do PDV. O WPF/PDV Web é um caixa de
restaurante/comércio com comandas, balcão, delivery, cozinha, estoque,
operadores e fechamento. O `PdvPage` do Flutter atual é uma central de
atendimentos da agenda com cronômetro, serviços, produtos e recebimento.

O Flutter já tem uma base multiplataforma forte e contratos de agenda/nuvem,
mas precisa receber o domínio operacional do Balcão Livre PDV e não apenas
reproduzir sua cor laranja.

## Matriz funcional

| Capacidade | WPF laranjão | PDV Web | Flutter atual | Situação necessária |
|---|---:|---:|---:|---|
| Shell visual escuro/laranja e atalhos | Sim | Sim | Parcial | Replicar hierarquia, navegação, densidade e estados |
| Comandas/mesas | Sim | Sim | Não | Portar domínio, grade, ocupação e seleção |
| Venda de balcão | Sim | Sim | Não | Portar carrinho e fechamento sem agendamento |
| Delivery | Sim | Sim | Não | Portar pedido, cliente, endereço, taxa e status |
| Monitor de cozinha | Sim | Sim | Não | Portar fila e transições de preparo |
| Pesquisa e inclusão por código | Sim | Sim | Parcial | Lançamento rápido com quantidade e foco por teclado |
| Catálogo por categoria/touch | Sim | Sim | Não | Grade responsiva para desktop, tablet e celular |
| Transferir comanda | Sim | Sim | Não | Transferência atômica entre mesas/comandas |
| Desconto na venda | Sim | Sim | Não | Percentual/valor, permissão e auditoria |
| Couvert e percentual de garçom | Sim | Sim | Não | Regras e composição do total |
| Clientes vinculados à venda | Sim | Sim | Parcial | Reusar CRM, preservando vínculo com a venda |
| Reabrir venda/comanda | Sim | Sim | Não | Reversão auditável e idempotente |
| Equipe, operador, garçom e caixa | Sim | Sim | Parcial | Perfis, PIN, permissões e vínculo operacional |
| Cadastro de produtos | Sim | Sim | Sim | Integrar o cadastro existente ao novo caixa |
| Estoque e estoque mínimo | Sim | Sim | Parcial | Movimentação por venda, ajuste e alerta |
| Abertura/fechamento de caixa | Sim | Sim | Não | Sessão de caixa, conferência e divergência |
| Suprimento e sangria | Sim | Sim | Não | Movimentos com operador, motivo e horário |
| Dinheiro, Pix, crédito e débito | Sim | Sim | Parcial | Checkout dividido, troco e status |
| Recebimento antecipado/saldo | Sim | Sim | Parcial | Unificar com recebíveis da agenda |
| Comprovante/recibo | Sim | Sim | Não | Impressão/compartilhamento multiplataforma |
| Fiscal/NFC-e/SAT/TEF | Sim | Não | Não | Adaptadores por plataforma e operação assistida no Web |
| iFood | Sim | Não | Não | Consumir o mesmo backend e mapear pedidos |
| WhatsApp de pedido/comprovante | Sim | Não | Parcial | Conectar os eventos do caixa ao serviço existente |
| Zonas e taxas de entrega | Sim | Não | Não | Portar regras e persistência |
| LGPD/exportar/anonimizar cliente | Sim | Não | Parcial | Completar o fluxo no CRM compartilhado |
| Offline-first | Local WPF | IndexedDB | SharedPreferences | Repositório transacional adequado por plataforma |
| Fila de sincronização do PDV | Sim | Sim | Não | Portar eventos idempotentes e retentativas |
| Backup/resumo no admin | Sim | Sim | Agenda apenas | Unificar contrato de conta, loja e terminal |
| Licença/identidade de dispositivo | Sim | Sim | Web/Android parcial | Um fluxo por conta/dispositivo em todas as plataformas |
| Promoção do site no contrato de nuvem | Sim | Sim | **Sim** | Corrigido nesta rodada, incluindo catálogo publicado |

## Gaps visuais comprovados

1. **PDV Web — entrada bloqueada por licença:** a tela real apresenta a
   liberação por e-mail, chave e operador. Sem credencial válida, o fluxo
   operacional não pode ser capturado além do bloqueio.
2. **Flutter — produto diferente:** a captura atual mostra uma agenda diária
   de salão, não o painel do caixa/comandas do WPF. A paleta laranja sozinha
   não caracteriza paridade.
3. **WPF — captura atual pendente:** o SDK .NET 9 é requisito para recompilar e
   executar a versão atual. Imagens existentes no repositório servem como
   referência de design, mas não foram tratadas como prova de execução nesta
   auditoria.

## Arquitetura-alvo

1. **Domínio compartilhado:** loja, terminal, operador, sessão de caixa,
   comanda, item, pagamento, cliente, produto, estoque, delivery e cozinha.
2. **Casos de uso puros:** abrir caixa, incluir item, transferir, aplicar
   desconto, receber, finalizar, reabrir e sincronizar.
3. **Persistência por plataforma:** banco transacional no desktop/mobile e
   IndexedDB no Web, atrás do mesmo repositório Dart.
4. **Outbox idempotente:** toda mutação local cria um evento com `event_id`,
   `store_id`, `terminal_id`, versão e data.
5. **Nuvem compartilhada:** autenticação e licença existentes, endpoints de
   sync/backup/admin e resolução explícita de conflitos.
6. **Apresentação adaptativa:** mesma operação e identidade em desktop,
   tablet e celular; diferenças apenas de composição responsiva.
7. **Adaptadores nativos:** impressão, fiscal e TEF isolados por plataforma,
   com estados claros quando não forem suportados no Web.

## Ordem de implementação

### P0 — integridade e base comum

1. Manter todos os contratos WPF/Flutter em paridade automática.
2. Criar o domínio Dart do PDV e os testes dos cálculos/transições.
3. Criar persistência offline e outbox idempotente.
4. Ligar conta, loja, terminal e licença ao mesmo backend.

### P1 — operação principal

1. Comandas, balcão, catálogo, carrinho, desconto, couvert e garçom.
2. Caixa, pagamentos, troco, fechamento e comprovante.
3. Produtos, estoque e clientes reutilizando os cadastros existentes.
4. Delivery e monitor de cozinha.

### P2 — integrações e acabamento

1. iFood, WhatsApp, impressão, fiscal e TEF.
2. Relatórios, auditoria, LGPD e permissões.
3. Comparações visuais automatizadas em desktop e mobile.
4. Testes reais de Web, Android/iOS e Windows/macOS/Linux suportados.

## Critério de conclusão

“Tudo igual” só estará provado quando cada linha da matriz tiver:

- caso de uso implementado;
- persistência e sincronização testadas;
- estado offline/online testado;
- teste de contrato com o WPF/backend;
- teste funcional em Web, mobile e desktop;
- comparação visual aceita no mesmo viewport/estado;
- nenhuma ação visível que seja apenas demonstrativa.
