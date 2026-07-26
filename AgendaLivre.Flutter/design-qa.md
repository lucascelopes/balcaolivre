# Design QA — Agenda Livre (21/07/2026)

## Escopo e fontes

- Fonte visual anterior:
  - `C:/Users/isabe/.codex/state/plugins/product-design/assets/agenda-livre-ux-audit-desktop-overview-2026-07-21.jpg`
  - `C:/Users/isabe/.codex/state/plugins/product-design/assets/agenda-livre-ux-audit-mobile-overview-2026-07-21.jpg`
  - `C:/Users/isabe/.codex/state/plugins/product-design/assets/agenda-livre-ux-audit-critical-flows-2026-07-21.jpg`
- Implementação validada: `C:/Users/isabe/Downloads/balcaolivre-main/balcaolivre-main/AgendaLivre.Flutter/lib`
- Referência estrutural adicional: `C:/Users/isabe/Downloads/balcaolivre-main/balcaolivre-main/AgendaLivre.Windows`
- Viewports principais: `1366 × 768` e `390 × 844`, escala 1.
- Viewports de resiliência: `1200 × 640`, `844 × 390`, `800 × 600` e `320 × 568`.
- Estados: Home, Agenda vazia e preenchida, atendimento selecionado, edição de agendamento, horário legado fora da agenda atual, recebimento vinculado, Financeiro, Relatórios, Marketing, Estabelecimento e Configurações.

## Comparações combinadas inspecionadas

- Página completa desktop, antes/depois: `artifacts/ux-product-audit-2026-07-21/33-design-qa-desktop-before-after.jpg`
- Página completa mobile, antes/depois: `artifacts/ux-product-audit-2026-07-21/34-design-qa-mobile-before-after.jpg`
- Fluxos críticos, antes/depois: `artifacts/ux-product-audit-2026-07-21/35-design-qa-critical-before-after.jpg`
- Recortes focados de Home, Agenda e Financeiro: `artifacts/ux-product-audit-2026-07-21/36-design-qa-focused-home-agenda-finance.jpg`
- Capturas finais individuais: `artifacts/ux-product-audit-2026-07-21/01-home-desktop.png` a `29-editar-agendamento-legado-dia-fechado.png`.

As referências e a implementação foram colocadas lado a lado na mesma imagem antes da avaliação. Depois das correções, uma segunda inspeção confirmou a hierarquia visual, o ritmo, a densidade, os estados e a responsividade.

## Resultado visual e funcional

- Tipografia e hierarquia: títulos, métricas, legendas e ações mantêm a escala e os pesos do sistema visual existente. Rótulos longos do Marketing foram encurtados para eliminar cortes.
- Espaçamento e layout: a Home não estoura mais cartões curtos da agenda; a Agenda selecionada cria uma área de decisão clara; desktop, tablet e mobile não apresentam overflow.
- Cores e superfícies: paleta coral, faixa de métricas escura, cartões, estados semânticos, bordas e raios permanecem coerentes com o WPF e com os demais clientes.
- Ícones: Material Icons e Font Awesome continuam sendo usados de forma consistente; não foram introduzidos símbolos de texto, emojis ou ilustrações falsas.
- Imagens: somente os assets reais já existentes no produto foram usados; nenhuma imagem de produto foi simulada.
- Conteúdo: ações que prometiam comportamento inexistente foram corrigidas. Relatórios agora usam “Pré-visualizar” e “Copiar CSV”; Marketing indica publicação manual; Financeiro vincula o recebimento ao atendimento real.
- Responsividade: abaixo de 900 px o shell usa o modo mobile. A navegação inferior conserva rótulos em larguras normais e adota controles apenas com ícones e semântica em larguras muito estreitas.
- Acessibilidade: ações principais têm alvo mínimo de 44 px, estados selecionados possuem semântica, textos críticos aceitam truncamento seguro e a navegação permanece alcançável em telas pequenas.

## Correções confirmadas

1. P1 — tocar em um atendimento abria cobrança imediatamente. Agora seleciona o atendimento e mostra Confirmar/Chegou/Iniciar/Finalizar/Receber/WhatsApp/Editar.
2. P1 — recebimento pendente no Financeiro podia abrir um lançamento genérico. Agora abre o checkout do atendimento escolhido.
3. P1 — editar um agendamento antigo em um dia hoje fechado bloqueava qualquer alteração. Agora dados não temporais podem ser salvos sem mudar a data, com aviso claro.
4. P1 — a Home apresentava overflow em agendamentos curtos. O cartão ganhou composição densa e truncamento sem perda da informação essencial.
5. P1 — a barra superior desktop era aplicada como busca/data em áreas onde isso não fazia sentido. Agora cada área possui contexto coerente.
6. P1 — o shell desktop quebrava em tablets estreitos. O breakpoint foi recalibrado e o modo mobile passou a cobrir essa faixa.
7. P2 — o selo “WhatsApp + Instagram manual” estourava com fonte de teste ampliada. O texto agora reflowa/trunca com segurança.
8. P2 — botões de Marketing cortavam “Atualizar prévia”, “Abrir WhatsApp” e “Abrir Instagram”. Os rótulos ficaram curtos e completos.
9. P2 — os controles de data, modo e ações na Agenda tinham alvos pequenos. Foram ampliados e reorganizados para desktop e mobile.
10. P2 — a suíte visual podia travar ao decodificar a comparação WPF dentro do relógio falso do widget test. A decodificação agora roda em tempo real e o comparativo permanece verificável.

## Verificações finais

- `flutter test --concurrency=1 --reporter=compact`: 229 testes aprovados.
- `flutter analyze`: sem problemas.
- `flutter build web --release --no-wasm-dry-run`: concluído; saída em `build/web`.
- Capturas regressivas: 25 estados aprovados no conjunto `ux_product_audit_capture_test.dart`.
- Comparação WPF/Flutter normalizada: aprovada no conjunto `visual_audit_current_test.dart`.
- Preview local: `http://127.0.0.1:4185/`, HTTP 200.

final result: passed
