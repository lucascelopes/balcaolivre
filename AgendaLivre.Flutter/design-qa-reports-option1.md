# Design QA — Relatórios mobile diagnóstico acionável (opção 1)

## Evidência

- Fonte selecionada pelo usuário: `C:\Users\isabe\.codex\attachments\c39664be-7f23-48ca-83ec-5d2684ec424c\image-1.png` (853 × 1844 px).
- Fonte normalizada: `artifacts/mobile-reports-option1-2026-07-30/reference-normalized-393x852.png`.
- Implementação: `artifacts/mobile-reports-option1-2026-07-30/estetica-coral-393x852.png` (CSS/viewport 393 × 852, DPR 1).
- Implementação 320 px e tema clínico: `artifacts/mobile-reports-option1-2026-07-30/clinica-azul-320x568.png`.
- Continuação após rolagem: `artifacts/mobile-reports-option1-2026-07-30/estetica-coral-393x852-lower.png`.
- Comparação combinada: `artifacts/mobile-reports-option1-2026-07-30/comparison-reference-implementation-393x852.png`.
- Estado: semana de 27/07 a 02/08/2026; 22 agendamentos, 18 confirmados, 15 realizados, 3 faltas, 4 confirmações pendentes e R$ 3.480 recebidos.

## Verificação

- Tipografia: Segoe UI e pesos 500–800 preservam a hierarquia editorial sem truncamento.
- Ritmo: período, diagnóstico, funil, tendência, ação, meta e explicação mantêm a progressão da referência.
- Tokens: superfícies, linhas, destaque e textos usam `AgendaThemeTokens`; vermelho/verde são apenas estados semânticos.
- Ícones: Font Awesome, sem SVG artesanal, emoji ou arte substituta.
- Imagens: fotos genéricas da fonte foram omitidas porque não há fotos reais no modelo; clientes reais aparecem pelo nome, sem avatares falsos.
- Conteúdo: todas as métricas são calculadas do período; Beleza, Clínica, Petshop e Oficina têm vocabulário próprio.
- Interações: período, indicador do gráfico, compartilhamento, Agenda e exportação em PDF estão ligados aos fluxos existentes.
- Responsividade: 320 × 568 e 393 × 852 sem overflow; desktop continua legado.
- Isolamento: a captura, os testes e a análise estática foram repetidos em worktree limpo sobre `bd582cd`, sem depender das demais alterações locais.

## Histórico

1. P2: a primeira comparação usava uma semana anterior subamostrada e gerava percentuais irreais. O cenário foi corrigido para períodos completos (+22%, +13%, +7%).
2. Pós-correção: a comparação combinada não apresentou P0/P1/P2.
3. P3 aceito: cabeçalho e barra de data são do shell compartilhado.
4. P3 aceito: meta e explicação ficam após rolagem em 393 × 852; a captura inferior confirma o estado completo.

O comparativo completo permaneceu legível em densidade 1:1; não foi necessário ampliar regiões. A continuação foi inspecionada separadamente.

final result: passed
