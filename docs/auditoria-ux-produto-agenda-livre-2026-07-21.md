# Auditoria completa de UX e produto — Agenda Livre

**Data:** 21/07/2026  
**Escopo:** Flutter Web/Desktop/Mobile — Painel, Agenda, edição de agendamento e cliente, Financeiro, Marketing, Meu estabelecimento, Relatórios e Configurações.  
**Objetivo:** verificar se o produto está fácil de entender e operar, se os fluxos principais fazem sentido e o que ainda falta para competir com outros sistemas de agendamento.

## Veredito executivo

O Agenda Livre já tem uma base visual coerente, uma Agenda fácil de ler e uma boa adaptação estrutural entre desktop e celular. O produto, porém, ainda transmite mais maturidade visual do que funcional em Financeiro, Marketing, Relatórios e Configurações. Há botões que prometem uma ação completa, mas entregam um atalho parcial ou diferente — por exemplo, **Receber agora** abre um lançamento avulso, **Vender produto** exibe apenas um aviso, **Exportar** copia CSV e **Imprimir** copia texto.

O principal fluxo de agenda funciona e é compreensível, mas a edição é mais longa do que deveria e pode bloquear um agendamento já existente quando a data cai em um dia hoje configurado como fechado. No uso real com dados preenchidos, a Home também apresenta estouro de layout em desktop e mobile. Esses pontos devem ser resolvidos antes de ampliar a divulgação, porque afetam confiança e operação diária.

**Leitura geral:** boa fundação, experiência principal promissora, mas ainda não está completa nem tão simples quanto os líderes da categoria.

| Área | Nota direcional | Estado |
|---|---:|---|
| Home/Painel | 6/10 | útil, mas quebra com dados reais e mistura controles globais |
| Agenda | 7/10 | melhor área do produto; clara e operacional |
| Edição de agendamento | 6/10 | compreensível, porém longa e com bloqueios/ações ambíguas |
| Cadastro do cliente | 6/10 | simples, mas CRM raso |
| Financeiro | 4/10 | aparência boa, operação ainda incompleta |
| Marketing | 4/10 | editor útil, integrações e envio ainda superficiais |
| Meu estabelecimento | 6/10 | bom começo, pouca profundidade administrativa |
| Relatórios | 5/10 | leitura rápida, filtros e saídas insuficientes |
| Configurações | 5/10 | organizada, mas há promessas e rótulos incorretos |
| Mobile | 6/10 | responsivo, mas exige muita rolagem e navegação repetitiva |
| Prontidão competitiva | 4,5/10 | agenda básica boa; faltam automação, checkout, CRM e retenção |

As notas são direcionais, usadas para priorização, e não uma medição científica.

## Evidências atuais

Foram geradas e inspecionadas **22 capturas automatizadas de estados reais de widget**, mais uma captura específica do bloqueio em dia fechado, em desktop e mobile, com dados realistas de cliente e agendamento. Também foram executados **41 testes de interação**, todos aprovados. Os testes confirmam que os principais componentes abrem e respondem; eles não anulam as falhas lógicas e de produto descritas abaixo.

### Visão geral desktop

![Visão geral das telas desktop](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/24-desktop-overview.jpg)

### Visão geral mobile

![Visão geral das telas mobile](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/25-mobile-overview.jpg)

### Fluxos críticos

![Etapas da edição, bloqueio em dia fechado e registro de pagamento](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/26-critical-flows.jpg)

## O que já está bom

- A identidade visual é consistente: laranja, superfícies claras, cartões escuros de resumo e tipografia hierárquica.
- A Agenda tem três visualizações — Quadro, Lista e Semana — e uma leitura direta por profissional e horário.
- Os estados do atendimento — aguardando chegada, em atendimento e pronto para cobrar — ajudam a operação do dia.
- O modal de agendamento tem progressão visível, resumo final e rodapé fixo no celular.
- As telas mobile não são apenas uma miniatura do desktop; os blocos são reorganizados e os modais ocupam a tela.
- Home, Financeiro e Relatórios destacam números importantes sem exigir navegação profunda.
- Meu estabelecimento reúne cliente, profissional e serviço em uma área coerente.
- O editor de campanha oferece mensagem pronta, variáveis e prévia, uma boa base para automação futura.

## Jornada auditada, passo a passo

### 1. Entrar no Painel — atenção

O resumo do dia é compreensível e os CTAs **Ver agenda** e **Agendar** são claros. Com um agendamento real preenchido, entretanto, a linha do horário estoura horizontalmente em desktop e em mobile. O bug deixa uma faixa de overflow visível e reduz a sensação de acabamento.

O campo global **Buscar clientes, agendamentos, serviços...** parece funcionar em qualquer página, mas o filtro é aplicado apenas à lista de agendamentos do dia. Em Financeiro, Marketing, Relatórios e Configurações, o controle continua visível sem produzir um resultado correspondente. O seletor de data também permanece em telas em que seu efeito não é claro.

Evidências: [Home desktop](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/01-home-desktop.png) e [Home mobile](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/10-home-mobile.png).

### 2. Consultar a Agenda — saudável com ajustes

A grade é fácil de interpretar, a alternância Quadro/Lista/Semana é visível e o painel de fluxo dá contexto operacional. No celular, o fluxo de atendimento fica abaixo da dobra, então o usuário precisa rolar para enxergar uma informação que no desktop aparece ao lado.

A capacidade exibida não usa sempre a mesma regra em Home e Agenda: em um dia histórico/fechado, uma área pode indicar nenhum horário livre enquanto outra mostra capacidade do dia. O usuário não deve precisar descobrir qual número é o correto.

Evidências: [Agenda desktop](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/02-agenda-desktop.png) e [Agenda mobile](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/11-agenda-mobile.png).

### 3. Criar ou editar um agendamento — atenção

A sequência Horário → Cliente → Confirmar é clara para uma criação nova. Para uma edição simples, como trocar apenas o horário, obrigar a passar novamente pelas três etapas gera atrito. O ideal é editar diretamente os campos e salvar, mantendo a revisão completa opcional.

Um agendamento existente em um domingo atualmente configurado como fechado não consegue avançar sem alterar a data; a validação acusa que o estabelecimento não atende naquele dia. Dados antigos ou importados precisam permanecer editáveis, com aviso e opção consciente de manter ou mover.

No rodapé, **Duplicar**, **Faltou**, **Cancelar** e **Excluir** têm peso visual semelhante e ficam juntos. “Cancelar” pode significar fechar a janela ou cancelar o horário; o sistema deve dizer **Cancelar agendamento** e separar ações destrutivas. Alguns alvos de toque usam altura aproximada de 32 px, abaixo da meta prática de 44 px.

Evidências: [Etapa Horário](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/03-editar-agendamento-desktop.png), [Etapa Cliente](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/21-editar-agendamento-cliente-desktop.png), [Confirmação](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/22-editar-agendamento-confirmar-desktop.png) e [bloqueio em dia fechado](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/23-editar-agendamento-bloqueado-dia-fechado.png).

### 4. Editar o cliente — básico e fácil

O formulário é curto e objetivo. Nome, WhatsApp, tags, segmento, preferência de horário e observações são suficientes para um cadastro inicial. Para um negócio que depende de recorrência e relacionamento, faltam histórico de serviços e compras, fotos, documentos/formulários, aniversário, origem do cliente, consentimento de comunicação e preferências editáveis.

Campos existentes como e-mail, documento e aceite de WhatsApp ficam preservados nos dados, mas não aparecem para edição. Isso limita correção cadastral e gestão de consentimento.

Evidências: [Cliente desktop](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/04-editar-cliente-desktop.png) e [Cliente mobile](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/13-editar-cliente-mobile.png).

### 5. Receber e controlar o Financeiro — crítico

O painel financeiro é visualmente claro, com entradas, pendências, gastos e resultado. A ação mais importante, porém, não faz o que o contexto sugere: o botão **Receber** de uma pendência chama o mesmo modal de **Lançar entrada** e cria um recebimento avulso. Ele não carrega nem quita automaticamente o agendamento selecionado. Isso pode manter a pendência aberta e duplicar a receita.

**Vender produto** não abre uma venda; mostra “Cadastre as vendas de produto em Meu estabelecimento”. A área Meu estabelecimento, por sua vez, não possui cadastro ou estoque de produtos. Também faltam checkout, pagamentos parciais/divididos, estorno, desconto, gorjeta, taxa, fechamento de caixa, conciliação Mercado Pago, comissão e recibo.

Evidências: [Financeiro desktop](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/05-financeiro-desktop.png), [Financeiro mobile](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/14-financeiro-mobile.png), [Registrar pagamento desktop](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/19-registrar-pagamento-desktop.png) e [mobile](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/20-registrar-pagamento-mobile.png).

### 6. Criar campanha — atenção

O editor é amigável e a prévia ajuda. A promessa **WhatsApp + Instagram**, entretanto, é maior do que a entrega atual: o Instagram recebe apenas o texto copiado e abre o site/aplicativo para publicação manual. Nas Configurações, **Conectar/Gerenciar** e **Abrir Direct** levam à própria página de Marketing, sem um fluxo real de conexão.

No WhatsApp, abrir uma conversa individual é útil, mas não equivale a campanha automatizada. Faltam consentimento e descadastro, segmentação confiável, agendamento, fila de envio, status entregue/lido, limites, tentativas e métricas. A mensagem vazia “Cadastre clientes com telefone...” também pode aparecer quando já existe cliente com telefone, mas nenhum contato prioritário foi formado — o diagnóstico da tela fica incorreto.

No mobile, o formulário, a prévia e os CTAs ficam muito distantes entre si por causa da rolagem longa.

Evidências: [Marketing desktop](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/08-marketing-desktop.png) e [Marketing mobile](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/15-marketing-mobile.png).

### 7. Administrar o estabelecimento — saudável como base

Clientes, profissionais e serviços estão agrupados de forma lógica e os botões de criação são fáceis de encontrar. Para crescer, a área precisa de pesquisa/listagem completa, importação, permissões por profissional, comissões, horários individuais, recursos/salas, produtos e estoque. O padrão de muitos cartões e modais pode ficar lento quando a empresa tiver centenas de clientes e serviços.

Evidências: [Estabelecimento desktop](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/07-estabelecimento-desktop.png) e [mobile](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/16-estabelecimento-mobile.png).

### 8. Consultar relatórios — atenção

Os indicadores principais têm boa leitura, mas o período é fixo nos últimos sete dias em torno da data selecionada e não há filtro evidente por profissional, serviço, status ou forma de pagamento.

Os rótulos também exageram a ação: **Exportar** copia o CSV para a área de transferência em vez de baixar um arquivo; **Imprimir** abre uma prévia e oferece “Copiar para imprimir”, sem acionar impressão ou PDF. É melhor entregar download/impressão reais ou renomear os botões.

Evidências: [Relatórios desktop](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/06-relatorios-desktop.png) e [mobile](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/18-relatorios-mobile.png).

### 9. Configurar e sair — atenção

A página agrupa negócio, operação e integrações de forma compreensível. Há, porém, dois textos que orientam mal:

- **Sair do sistema — Reinicie a configuração inicial** sugere que logout apaga ou reinicia a conta.
- **Cadastro inicial — Revise setor, dados e senha** promete revisão de senha sem apresentar claramente essa função na tela.

Faltam políticas de cancelamento e sinal, regras de lembrete, configurações da página pública, funções/permissões, trilha de auditoria, notificações e gestão clara das integrações.

Evidências: [Configurações desktop](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/09-configuracoes-desktop.png) e [mobile](../AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/17-configuracoes-mobile.png).

## Lista completa priorizada

### P0 — corrigir antes de ampliar o uso

| ID | Problema | Impacto | Recomendação |
|---|---|---|---|
| P0-01 | Home estoura horizontalmente com um agendamento normal, em desktop e mobile | quebra visual e perda de confiança | tornar linha responsiva, truncar textos com tooltip e testar nomes/serviços longos |
| P0-02 | “Receber” não quita a pendência selecionada; cria entrada avulsa | risco financeiro e duplicidade | abrir checkout já vinculado ao agendamento/cliente e atualizar status/pendência em uma transação |
| P0-03 | “Vender produto” não vende e aponta para área sem produtos | fluxo morto | criar catálogo, estoque e venda ou remover o CTA até a função existir |
| P0-04 | Agendamento existente em dia fechado pode ficar bloqueado na edição | impede manutenção de dado legítimo | permitir manter com aviso, validar somente mudanças ou oferecer remarcação explícita |
| P0-05 | Ações destrutivas de agendamento ficam juntas e “Cancelar” é ambíguo | erro operacional | separar menu “Mais ações”, usar “Cancelar agendamento” e confirmação com consequência |
| P0-06 | Produção ainda aceita entrada por HTTP antes do redirecionamento | risco de sessão e confiança | forçar HTTPS no edge, HSTS e revisar cabeçalhos CSP; ver auditoria técnica anterior |

### P1 — tornar o produto confiável para operação diária

| ID | Problema/gap | Impacto | Recomendação |
|---|---|---|---|
| P1-01 | Busca global aparece onde não filtra conteúdo | controle enganoso | torná-la realmente global ou contextual por página; ocultar onde não tiver função |
| P1-02 | Data global aparece em páginas sem efeito claro | carga cognitiva | exibir somente em Agenda/Home/Relatórios ou explicar o período aplicado |
| P1-03 | “+ Novo” sempre cria agendamento em qualquer página | contexto ambíguo | renomear para “Novo agendamento” ou usar ação contextual por módulo |
| P1-04 | Editar um único campo exige três etapas | lentidão no balcão | modo de edição direta com salvar; manter assistente apenas para criação |
| P1-05 | Alvos de toque pequenos nas ações secundárias | acessibilidade e erro | mínimo prático de 44×44 px, foco visível e navegação por teclado |
| P1-06 | Regras/capacidade divergem entre Home e Agenda | decisão errada | centralizar cálculo e usar a mesma explicação de horários livres |
| P1-07 | Não há confirmação/lembrete automatizado e rastreável | faltas e trabalho manual | WhatsApp/e-mail com modelos, horário, status, tentativas e confirmação do cliente |
| P1-08 | Cliente não remarca/cancela sozinho | trabalho manual | link seguro com política, prazo e vagas disponíveis |
| P1-09 | Não há sinal/depósito ou política de no-show | perda de receita | depósito opcional por serviço/cliente e regras de cancelamento |
| P1-10 | Checkout não suporta parcial, dividido, desconto, gorjeta ou estorno | operação financeira incompleta | tela única de fechamento vinculada ao atendimento |
| P1-11 | Sem fechamento de caixa e conciliação Mercado Pago | saldo pouco confiável | abertura/fechamento, divergência, taxas e conciliação por transação |
| P1-12 | Sem comissão por profissional/serviço/produto | cálculo manual | regras configuráveis e relatório de repasse |
| P1-13 | Sem catálogo/estoque/movimentação de produto | perde venda e controle | CRUD, estoque mínimo, entrada/saída e vínculo com checkout |
| P1-14 | Instagram não está realmente conectado/publicando | promessa frustrada | OAuth/Meta API quando permitido; mostrar claramente quando a etapa é manual |
| P1-15 | Campanha WhatsApp é conversa manual, sem governança | risco e pouca escala | consentimento, opt-out, segmentos, fila, limites, status e relatório |
| P1-16 | Aceite de WhatsApp existe nos dados, mas não é editável | risco LGPD | controle visível com origem/data do consentimento e descadastro |
| P1-17 | Empty state de Marketing diagnostica errado | usuário não sabe resolver | explicar o filtro/critério real e oferecer ação correta |
| P1-18 | “Atualizar promoção” não deixa claro se salva, envia ou apenas pré-visualiza | insegurança | separar “Salvar rascunho”, “Agendar envio” e “Enviar agora” |
| P1-19 | Relatórios sem período e filtros reais | análise limitada | hoje/semana/mês/customizado e filtros por unidade, profissional, serviço e status |
| P1-20 | Exportar/Imprimir não baixam nem imprimem | quebra de expectativa | CSV/XLSX/PDF e impressão nativos; enquanto isso, usar “Copiar CSV” |
| P1-21 | Perfis e permissões de equipe ausentes | segurança operacional | proprietário, gerente, recepção e profissional com acesso granular |
| P1-22 | Texto de logout sugere reiniciar configuração | medo de perda de dados | usar “Sair desta conta” e explicar que os dados permanecem salvos |
| P1-23 | Mobile depende do menu hambúrguer para toda troca de módulo | navegação repetitiva | barra inferior com 4 destinos principais e “Mais” |
| P1-24 | Marketing/Configurações exigem rolagem muito longa no celular | ações ficam escondidas | seções recolhíveis, sumário fixo e CTA persistente |

### P2 — aproximar-se dos concorrentes completos

| ID | Gap | Valor | Recomendação |
|---|---|---|---|
| P2-01 | Lista de espera | ocupa cancelamentos | fila por serviço/profissional com convite automático |
| P2-02 | Agendamentos recorrentes, em grupo e com múltiplos serviços | reduz trabalho repetitivo | recorrência, participantes e composição de serviços |
| P2-03 | Arrastar/soltar, intervalos e bloqueios rápidos | acelera remarcação | drag-and-drop com validação e desfazer |
| P2-04 | Recursos/salas/equipamentos | evita conflito | disponibilidade conjunta profissional + recurso |
| P2-05 | Histórico completo do cliente | melhora atendimento | timeline de agenda, compras, pagamentos, faltas e campanhas |
| P2-06 | Fotos, fichas, anamnese e formulários | atende beleza/saúde/pet | modelos, assinatura, anexos e controle de acesso |
| P2-07 | Importação/exportação de clientes | reduz barreira de migração | CSV com mapeamento, validação e desfazer |
| P2-08 | Fidelidade, pontos e indicação | aumenta retorno | regras simples, saldo e recompensas |
| P2-09 | Pacotes, assinaturas e vale-presente | receita recorrente | venda, consumo, validade e saldo do cliente |
| P2-10 | Avaliações, portfólio e fotos de serviços | melhora conversão | solicitar avaliação e exibir página pública |
| P2-11 | Agendamento por Google/Instagram/widget/QR | aumenta aquisição | links rastreáveis e origem do agendamento |
| P2-12 | Relatórios de retenção, faltas, canal, comissão e margem | gestão real | dashboards acionáveis e comparação de períodos |
| P2-13 | Satisfação/NPS pós-serviço | detecta problema cedo | automação com resposta vinculada ao atendimento |
| P2-14 | Multiunidade e visão consolidada | suporta crescimento | permissões, estoque, agenda e relatórios por unidade |
| P2-15 | API/webhooks e integrações contábeis/fiscais | reduz retrabalho | eventos e integrações documentadas |

## Comparação com o mercado

Os concorrentes mais maduros não se limitam a uma agenda bonita; conectam aquisição, atendimento, pagamento, retenção e gestão.

| Capacidade | Agenda Livre atual | Referência de mercado |
|---|---|---|
| Agenda online | boa grade e estados; automações limitadas | Fresha e Booksy combinam regras, lembretes, lista de espera, formulários e reservas multicanal |
| CRM | cadastro e observações básicas | Fresha/Booksy mantêm histórico, fichas, formulários, fotos, preferências e relacionamento |
| Financeiro/POS | resumo e lançamentos avulsos | Fresha e Trinks incluem checkout, pagamentos divididos, estornos, comissões e conciliação |
| Produtos | fluxo inexistente | Fresha, Booksy, Trinks e AppBarber oferecem estoque e venda |
| Marketing | editor e abertura manual de canais | Fresha/AppBarber trabalham segmentação, automação e métricas de campanha |
| Retenção | sem pacote/fidelidade/assinatura | Booksy, Trinks e AppBarber oferecem pacotes, assinaturas, gift cards ou fidelidade |
| Gestão de equipe | profissionais básicos | Fresha/Booksy/Trinks incluem turnos, permissões e comissões |
| Relatórios | sete dias e indicadores básicos | Trinks anuncia mais de 130 relatórios; demais cobrem receita, equipe, estoque e retenção |

Fontes oficiais consultadas:

- [Fresha — recursos para negócios](https://www.fresha.com/pt/for-business/features)
- [Booksy — recursos](https://biz.booksy.com/pt-br/recursos)
- [Booksy — preços e estrutura do plano](https://biz.booksy.com/pt-br/precos)
- [Trinks — sistema para negócios](https://negocios.trinks.com/)
- [Trinks — salões de beleza](https://negocios.trinks.com/negocios/saloes-de-beleza/)
- [AppBarber — funcionalidades](https://www.appbarber.com.br/funcionalidades/)

## Sequência recomendada de evolução

### 0–30 dias: confiança e operação básica

1. Corrigir overflow da Home e alvos de toque.
2. Fazer Receber quitar a pendência correta, com idempotência.
3. Resolver edição de agendamento em dia fechado e separar ações destrutivas.
4. Corrigir busca/data/Novo globais e textos enganosos.
5. Corrigir Exportar/Imprimir e o texto de logout.
6. Forçar HTTPS e cabeçalhos de segurança.

### 31–60 dias: produto utilizável no dia a dia

1. Checkout completo, caixa, Mercado Pago e comissões.
2. Produtos e estoque integrados ao atendimento.
3. Lembretes, confirmação, cancelamento e remarcação pelo cliente.
4. Equipe com funções/permissões e horários individuais.
5. Relatórios com períodos/filtros e exportação real.
6. Marketing com consentimento, segmentação e estados confiáveis.

### 61–90 dias: diferenciação e retenção

1. Lista de espera, recorrência, grupo e múltiplos serviços.
2. CRM com histórico, fotos, formulários e importação.
3. Pacotes, assinatura, vale-presente e fidelidade.
4. Avaliações, portfólio e canais de agendamento Google/Instagram/widget.
5. Indicadores de retenção, no-show, aquisição e margem.

## Critério de conclusão sugerido

O produto pode ser considerado pronto para expansão quando um negócio conseguir, sem controles paralelos:

1. receber um agendamento por link;
2. confirmar/remarcar/cancelar com regras claras;
3. atender e registrar serviço/produto;
4. cobrar e conciliar o pagamento correto;
5. calcular comissão e fechar o caixa;
6. reativar o cliente com consentimento;
7. medir receita, retorno e faltas por período.

## Limites desta auditoria

- As capturas foram produzidas nesta execução a partir dos widgets atuais e dados semeados realistas; não houve acesso a uma conta real de produção do usuário.
- Os blocos pretos em alguns textos das capturas são artefatos de rasterização do ambiente headless e foram excluídos das conclusões.
- Não foi executado um ensaio completo com leitor de tela, teclado em todos os controles ou auditoria WCAG automatizada; os achados de acessibilidade são visuais e estruturais.
- Envios reais de WhatsApp/Instagram, pagamento Mercado Pago, e-mail e impressão externa não foram disparados.
- A segurança, autenticação e paridade técnica completa estão registradas em [auditoria-produção-paridade-2026-07-21.md](auditoria-producao-paridade-2026-07-21.md).

## Artefatos gerados

- Capturas: `AgendaLivre.Flutter/artifacts/ux-product-audit-2026-07-21/`
- Teste de captura reproduzível: `AgendaLivre.Flutter/test/ux_product_audit_capture_test.dart`
- Auditoria técnica anterior: `docs/auditoria-producao-paridade-2026-07-21.md`
