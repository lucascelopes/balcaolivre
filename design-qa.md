# Design QA — hover do gráfico do estabelecimento

## Evidências

- Verdade visual: `C:\Users\isabe\AppData\Local\Temp\codex-clipboard-0e5c45c3-7411-4878-83d7-67076257d1a5.png`.
- Referência: 409 × 153 px.
- Implementação em repouso: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\AgendaLivre.Windows\artifacts\establishment-hover-verify\qa\implementation-establishment-default.png`.
- Implementação com hover: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\AgendaLivre.Windows\artifacts\establishment-hover-verify\qa\implementation-establishment-hover.png`.
- Comparação conjunta: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\AgendaLivre.Windows\artifacts\establishment-hover-verify\qa\comparison-establishment-hover.png`.
- Captura da implementação: janela WPF maximizada em 1366 × 720 px, densidade física 1:1.
- Estado verificado: página “Meu estabelecimento”, terceira coluna “Serviços” em hover.

## Comparação de tela inteira

- A interação foi adicionada somente às três colunas do gráfico, preservando cabeçalho, métricas, painéis e ações da página.
- O estado em repouso mantém as proporções e cores anteriores.
- O hover não desloca o layout porque a transformação usa como origem o centro inferior da barra.

## Comparação focada

- A comparação conjunta apresenta referência, estado padrão e estado hover no mesmo quadro.
- No hover, a coluna cresce 12% na largura e 8% na altura, aumenta a opacidade e recebe sombra laranja.
- O tooltip aparece imediatamente e mostra categoria e valor real, por exemplo “Serviços no catálogo: 9”.

## Superfícies de fidelidade

- Tipografia: o tooltip usa a tipografia nativa do WPF e mantém boa legibilidade.
- Espaçamento e layout: a elevação permanece dentro da faixa e não altera a linha-base do gráfico.
- Cores e tokens: a sombra usa o laranja principal `#F26722`; as cores individuais das barras foram preservadas.
- Imagens e ativos: não há novos ativos raster ou desenhos substitutos.
- Cópia e conteúdo: os tooltips exibem “Clientes cadastrados”, “Profissionais ativos” e “Serviços no catálogo” com valores reais.

## Interações verificadas

- Entrada do mouse anima opacidade, escala X, escala Y e sombra em 140 ms.
- Saída do mouse retorna todos os valores ao estado normal em 120 ms.
- O cursor muda para mão sobre qualquer uma das três colunas.
- O tooltip abre sem atraso e permanece disponível durante o hover.
- Build Release concluído com 0 avisos e 0 erros.

## Histórico de comparação

- Solicitação inicial: as barras não tinham resposta visual suficientemente perceptível.
- Correção: hover animado, elevação, sombra, cursor e tooltip imediato foram adicionados.
- Evidência pós-correção: a terceira imagem do comparativo mostra a barra de Serviços elevada e iluminada.

## Achados finais

- Não restam achados P0, P1 ou P2 acionáveis.

## Follow-up polish

- Nenhum refinamento obrigatório.

final result: passed

---

# Design QA — promoção no site (opção 3)

## Evidências

- Verdade visual: `C:\Users\isabe\.codex\generated_images\019f9f7e-482c-78a1-b9b7-cb2a03261c7f\call_xOEBfQ0TyT3inCO4UT98wNLM.png`.
- Implementação WPF: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\AgendaLivre.Windows\marketing-promotion-option3-final-v5.png`.
- Comparação conjunta: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\AgendaLivre.Windows\marketing-promotion-option3-comparison-final.png`.
- Implementação no site — serviços: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\promotion-site-services.png`.
- Implementação no site — carrinho: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\promotion-site-cart.png`.
- Fonte: 1487 × 1058 px. Implementação WPF: cliente capturado em 1366 × 736 px dentro de uma janela solicitada em 1366 × 768, densidade 1:1.
- Site: viewport do navegador em 1265 × 712 px, densidade 1:1.
- Normalização: a fonte e a implementação WPF foram escaladas para 1366 × 768 e reunidas lado a lado no comparativo; diferenças de barra de título e proporção original foram desconsideradas.
- Estado: três serviços selecionados, desconto médio de 15%, período de 26/07/2026 a 02/08/2026 e destaque ativo no catálogo.

## Comparação de tela inteira

- A hierarquia principal coincide com a opção escolhida: título e contexto do canal, catálogo à esquerda, configuração fixa à direita, resumo, prévia e ação primária laranja.
- A implementação adapta os chips de categoria do conceito para busca + categoria única, aproveitando o padrão de campos já usado no WPF e preservando espaço no cliente de 1366 × 736.
- A coluna de serviços usa dados reais do Agenda Livre e mantém a densidade da tabela sem quebrar a navegação lateral ou o cabeçalho do produto.

## Comparação focada

- A comparação conjunta permitiu verificar a tabela, os preços, a seleção, o painel de período e a prévia no mesmo estado.
- No site, a captura focada confirmou o banner “Semana do autocuidado”, preços originais riscados, preços promocionais, selo `-15%` e ações de adicionar.
- A captura do carrinho confirmou que “Coloração completa” usa R$ 221,00, e não o preço-base de R$ 260,00.

## Superfícies de fidelidade

- Tipografia: a implementação mantém Segoe UI na interface WPF e serifada editorial no site; pesos e tamanhos preservam a hierarquia do conceito sem truncar ações essenciais.
- Espaçamento e layout: os dois painéis principais, raios, separadores e densidade de linha seguem o produto existente. A ação primária permanece visível em 1366 × 736; a ação secundária continua acessível por rolagem curta.
- Cores e tokens: laranja de ação, superfícies brancas, fundo quente, linhas neutras e estados promocionais usam os tokens existentes do Agenda Livre.
- Imagens e ativos: miniaturas e prévia usam ativos raster reais do projeto; nenhum ativo do conceito foi substituído por emoji, SVG artesanal ou desenho em CSS.
- Cópia e conteúdo: “Exclusivo para o site” e “O preço do PDV não será alterado” removem a ambiguidade de canal; período, limite, resumo e publicação têm textos específicos.

## Interações e integração verificadas

- Selecionar e remover serviços atualiza contagem, desconto médio e prévia.
- Busca, filtro de categoria, datas, limite, destaque, rascunho e publicação estão conectados ao estado persistido.
- Publicar grava a promoção no catálogo sincronizado sem alterar `ServiceItem.Price`.
- Teste persistido: R$ 260, R$ 95 e R$ 48 permaneceram no PDV; o site recebeu R$ 221, R$ 80,75 e R$ 40,80.
- O Modo PDV abriu com o mesmo conjunto de dados sem falhas.
- O catálogo compilado abriu no navegador com banco local isolado; seleção do primeiro serviço gerou carrinho de R$ 221,00.
- Console do navegador: nenhum erro ou aviso no fluxo final.
- Builds finais: WPF e Vinext concluídos com zero erros.

## Histórico de comparação

- Iteração 1 — achados P2: seleção pouco visível, campos sem dicas aparentando estar vazios e selo da prévia cortado. Correções: seleção explícita, hints, contagem dinâmica, cultura pt-BR e contenção da prévia.
- Iteração 2 — achados P2: prévia sem imagem e botão principal encoberto pelo lançador flutuante do WhatsApp. Correções: ativo raster real na prévia, altura limitada e ocultação do lançador nesta tela.
- Evidência pós-correção: `marketing-promotion-option3-final-v5.png` mostra seleção clara, painel íntegro e ação primária sem sobreposição.

## Achados finais

- Não restam achados P0, P1 ou P2 acionáveis.

## Follow-up polish

- [P3] Em altura de cliente menor que 736 px, “Salvar rascunho” exige uma rolagem curta no painel direito.
- [P3] A implementação usa um seletor único de categoria no lugar dos chips do conceito para manter a tabela mais larga.

final result: passed
 
---

# Design QA - data do cabecalho

## Evidencias

- Verdade visual: `C:\Users\isabe\AppData\Local\Temp\codex-clipboard-61ab6e3f-41d3-4b6d-9c12-ae5d0a54bdd3.png`.
- Implementacao: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\AgendaLivre.Windows\header-date-full-final.png`.
- Recorte focado: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\AgendaLivre.Windows\header-date-focused-final.png`.
- Comparacao conjunta: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\AgendaLivre.Windows\header-date-clipping-comparison.png`.
- Fonte: 133 x 78 px. Implementacao: cliente WPF de 1366 x 736 px em janela solicitada de 1366 x 768, densidade 1:1.
- Normalizacao: o recorte da implementacao foi capturado com 78 px de altura, igual a fonte, e colocado lado a lado sem redimensionamento.
- Estado: tela inicial, data de hoje em 26/07/2026, cabecalho padrao.

## Comparacao de tela inteira

- O cabecalho preserva a barra de busca, os botoes de Modo PDV e Novo, a identidade visual e o alinhamento vertical.
- A coluna da data aumentou de 92 px para 128 px sem causar sobreposicao ou quebra na busca.

## Comparacao focada

- A fonte mostra `Hoje, 26/0` cortado no limite direito.
- A implementacao revisada mostra `Hoje, 26/07` por inteiro, com respiro antes da busca.

## Superficies de fidelidade

- Tipografia: Segoe UI, peso, tamanho e alinhamento vertical existentes foram preservados; nao ha truncamento.
- Espacamento e layout: a largura de 128 px comporta tambem `Amanha, 27/07` e mantem o intervalo de 16 px para a busca.
- Cores e tokens: fundo, texto, bordas e estados do cabecalho nao foram alterados.
- Imagens e ativos: nao ha ativos de imagem nesse componente; o restante do cabecalho foi preservado.
- Copia e conteudo: a data completa `Hoje, 26/07` esta visivel e correta.

## Historico de comparacao

- Iteracao 1 - achado P2: texto da data cortado por uma largura fixa de 92 px, deixando apenas 72 px uteis depois do padding.
- Correcao: largura do estilo e da coluna alterada para 128 px.
- Evidencia pos-correcao: a comparacao conjunta mostra a data completa e a busca sem sobreposicao.

## Achados finais

- Nao restam achados P0, P1 ou P2 acionaveis.

## Follow-up polish

- Nenhum refinamento obrigatorio.

final result: passed
