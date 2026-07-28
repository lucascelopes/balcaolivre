# Design QA — rolagem do quadro e contagem semanal

## Artefatos e estado

- Source visual truth: `C:\Users\isabe\AppData\Local\Temp\codex-clipboard-2709348e-8f3c-4257-80d8-e240e9b810a6.png`.
- Implementation screenshot: `home-board-scroll-final.png`.
- Implementation normalized: `home-board-scroll-final-normalized.png`.
- Full-view comparison: `home-board-scroll-comparison.png`.
- Focused comparison: `home-board-scroll-focused-comparison.png`.
- Viewport: desktop WPF em 1366 × 768 DIPs; comparação normalizada para 1364 × 719 px, densidade 1×.
- Source pixels: 1364 × 719. Implementation pixels: 1366 × 768.
- State: Painel, seção inferior, visualização Dia, quadro com atendimentos e Desempenho da semana.

## Findings

- Nenhuma diferença P0, P1 ou P2 permanece nos dois elementos solicitados.
- A rolagem vertical pertence ao quadro de horários e não desloca o `HomeDashboardView` enquanto o cursor está sobre o quadro.
- A barra de rolagem usa o estilo fino e a cor de destaque já existentes no aplicativo.
- A contagem diária aparece imediatamente acima da barra correspondente e usa os dados reais da semana; dias sem agendamentos não exibem zero para reduzir ruído.
- Diferenças de nomes, horários e quantidade (`7` no estado de auditoria, `2` na referência) são dados do cenário, não diferenças de implementação.

## Required fidelity surfaces

- Fonts and typography: Segoe UI, pesos e tamanhos existentes foram preservados; a nova contagem usa 10,5 DIPs em negrito, legível sem competir com o faturamento.
- Spacing and layout rhythm: o quadro mantém a largura, o alinhamento das colunas e a altura do card; a contagem fica 5 DIPs acima da barra.
- Colors and visual tokens: barra selecionada e sua contagem usam `Accent`/`AccentText`; demais barras preservam o coral suave existente.
- Image quality and asset fidelity: não há novos ativos rasterizados; ícones Material Design e componentes nativos existentes foram preservados.
- Copy and content: rótulos do Painel permanecem iguais; os números são derivados da quantidade real de agendamentos por dia.

## Interaction verification

- Controle testado por UI Automation: `Rolagem do quadro de horários`.
- `VerticallyScrollable`: `true`.
- Percentual vertical antes do teste: `0`.
- Percentual vertical após `LargeIncrement`: `100`.
- Build Release isolado: 0 avisos e 0 erros.

## Comparison history

1. Primeira captura
   - P2: a altura fixa inicial deixava espaço vazio dentro do card.
   - Fix: o card passou a dimensionar o quadro de forma compacta e o viewport interno foi ajustado para 500 DIPs.
2. Segunda captura
   - P2: ao preencher toda a altura disponível, o conteúdo diário cabia inteiro e a rolagem deixava de ser necessária.
   - Fix: o card ganhou altura própria, alinhamento superior e viewport interno finito, preservando a rolagem sem criar vazio interno relevante.
3. Comparação final
   - O scroll interno aparece na mesma região indicada na referência.
   - A contagem semanal está centralizada sobre a barra e acompanha o dia selecionado.

## Implementation checklist

- [x] Adicionar `ScrollViewer` interno ao quadro.
- [x] Impedir que o wheel sobre o quadro role a página inteira.
- [x] Preservar estilos e interações do quadro.
- [x] Exibir a contagem real de agendamentos sobre cada barra com movimento.
- [x] Ocultar contagens zero.
- [x] Validar visualmente e por UI Automation.
- [x] Compilar sem avisos ou erros.

## Follow-up polish

- Nenhum P3 necessário para esta alteração.

final result: passed
