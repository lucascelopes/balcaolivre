# BL. Canvas — 24/10/2026

Implementação iniciada no diretório BalcaoLivre.Canvas em branch codex/bl-canvas-web-20261024. Clone isolado em C:/Users/isabe/Documents/balcaolivre-web, baseado na main do repositório lucascelopes/balcaolivre. Nenhum arquivo dos aplicativos anteriores foi editado.

Direção aprovada: superfícies claras branco/azulado, laranja, navegação superior flutuante, mapa de mesas e assistente inferior. Páginas aprovadas: Salão, Pedidos/delivery, Cardápio/scanner, Caixa/financeiro; cada uma desktop e mobile.

Implementados fluxos locais conectados, conforme README do Canvas. Não há integração de produção com delivery ou IA; o assistente identifica explicitamente seu modo local. Scanner utiliza OCR real e revisão.

Validação visual pendente porque a configuração do Work bloqueia navegador local, incluindo Edge solicitado pela usuária. Não marcar o objetivo completo até conferir as telas e os fluxos e tratar os demais itens do README.

## Continuação: integridade de caixa e comandas
- Fechamentos agora preservam cópia dos lançamentos; nova abertura zera somente o movimento atual.
- Comandas permitem aumentar/diminuir quantidades e remover itens com quantidade zero.
- Pedidos têm registro de pagamento independente do status de entrega, impedindo recebimento duplicado.
- Valores vazios, precisão excedida e valores fora do limite são rejeitados.
- Build e 10 testes de domínio passaram. Edge reconsultado: uso local ainda desativado no Work. Validação visual permanece pendente.


## Menu Mais solicitado
- Substituído scanner do menu superior por três pontos; atalho Mais também no cabeçalho mobile.
- Menu reúne scanner, assistente, pedidos em preparo, histórico, proposta de automações, integrações e próximos módulos. Funcionalidades futuras explicitamente identificadas.
- Propostas: alertas de atendimento, reposição, conferência de caixa, revisão de cardápio e mensagens com confirmação.
- Primeira imagem ilustrativa de demonstração (burger) adicionada ao catálogo. Build aprovado; validação visual continua pendente.


## Seu espaço — referência do Agenda Livre
- Usuária forneceu duas capturas e pediu menu no formato do Agenda Livre.
- Menu Mais agora abre painel editorial branco: BL., Seu espaço em serifada, seções, ícones grandes, setas e divisórias. Tela inteira no mobile e painel lateral no desktop.
- Áreas adaptadas para restaurante: Dia a dia, Seu negócio, Inteligência e automação. Destinos existentes navegam; recursos futuros identificados como planejados.
- Foco contido no diálogo, Escape, retorno de foco e bloqueio de rolagem do fundo. Rodapé informa armazenamento local sem alegar sincronização.
- Build aprovado. Conferência visual no navegador permanece pendente.


## Identidade do título do menu corrigida
- Usuária rejeitou o título Seu espaço em serifada por não combinar com o BL.
- Alterado para Seu restaurante., com tipografia sans do aplicativo e ponto laranja; títulos das seções também usam a fonte do BL. Preservada a estrutura em seções solicitada anteriormente.


## Páginas Cozinha e Estoque implementadas
- Cozinha usa os pedidos locais e permite avançar recebidos/preparo/prontos/entrega; filtros por origem e tempo decorrido.
- Estoque permite cadastro/edição de ingredientes com unidade fixa após cadastro, custo, mínimo e quantidade; filtro de reposição e histórico de ajustes.
- Fichas técnicas vinculam itens do cardápio a ingredientes e rendimento, calculando custo por porção. Sem baixa automática, explicitado na tela.
- Navegação pelo menu Seu restaurante. Build e 10 testes de domínio existentes passaram; testes não cobrem interação visual das novas páginas. Navegador continua pendente.


## Continuação das páginas — 24/10/2026
- Comandas enviam somente novos itens à Cozinha; recebimento da mesa marca os tickets relacionados sem duplicar cobrança.
- Histórico do caixa virou página com conferência e lançamentos. Interface de fechamento agora utiliza closeCash/openCash para preservar o detalhamento.
- Revisão visual pendente; dados continuam locais. Sincronização com segundo PC não conferida.
