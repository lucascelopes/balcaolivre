# Design QA — desempenho da semana, opção 3

## Escopo

- Componente: card **Desempenho da semana** na página Painel.
- Referência selecionada: opção visual 3.
- Viewport validado: `1366x768`.
- Estado validado: `home-lower`, com dados reais de demonstração da semana atual.

## Evidências

- Referência: `C:\Users\isabe\.codex\generated_images\019f8a5d-441f-7692-9066-6593b749d98b\exec-388e6e7d-05c8-404e-a430-c57b901143d7.png`
- Captura final: `weekly-performance-option3-final.png`
- Recorte final do componente: `weekly-performance-option3-crop.png`
- Comparação normalizada lado a lado: `weekly-performance-option3-comparison.png`

## Verificações visuais

- [x] Cabeçalho preto, título branco e selo verde preservam a direção escolhida.
- [x] Faturamento, atendimentos e ticket médio aparecem em uma única faixa, sem cards aninhados.
- [x] O gráfico semanal mostra os sete dias e a quantidade acima do dia com agendamentos.
- [x] O dia selecionado recebe barra laranja, rótulo em negrito e número destacado.
- [x] O serviço mais vendido ocupa uma linha leve, com ícone e texto de apoio.
- [x] O botão **Abrir financeiro** usa toda a largura e não sofre corte.
- [x] Não há textos cortados, sobreposição, overflow ou quebra indevida no viewport validado.
- [x] Espaçamentos, divisores e proporções foram comparados diretamente com a referência normalizada.

## Verificações funcionais

- [x] Métricas continuam alimentadas por `RefreshHomeWeeklyPerformance`.
- [x] Altura das barras é limitada ao espaço disponível no novo gráfico.
- [x] Contagem diária e destaque do dia selecionado são atualizados dinamicamente.
- [x] Automação de interface encontrou **Abrir financeiro**, acionou o botão e confirmou `Página Financeiro` visível (`IsOffscreen=False`).

## Build

```text
Compilação com êxito.
0 Aviso(s)
0 Erro(s)
```

## Resultado

`passed`
