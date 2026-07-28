# Design QA — Login do Admin

## Evidências

- Source visual truth: `C:\Users\isabe\.codex\generated_images\019fa3cf-32a2-74d3-b221-12c15382b4e3\call_BZsUS16RXGtD3vRQmwsYD1t3.png`
- Implementação desktop: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivre.Admin\artifacts\admin-login-implementation-final-1488x940.png`
- Implementação mobile animada: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivre.Admin\artifacts\admin-login-mobile-underlay-strong-412x600.png`
- Comparação completa: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivre.Admin\artifacts\admin-login-design-qa-comparison.png`
- Comparação focada no formulário: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivre.Admin\artifacts\admin-login-design-qa-form-comparison.png`
- Comparação focada no painel visual: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivre.Admin\artifacts\admin-login-design-qa-visual-comparison.png`

## Normalização

- Source: 1486 × 1058 px, densidade 1x.
- Viewport CSS desktop: 1488 × 1058.
- Captura visível do navegador: 1488 × 940 px, densidade 1x.
- Para a comparação completa, o source recebeu 1 px de margem lateral e foi recortado em 1488 × 940, igualando a captura visível da implementação.
- Viewport mobile: 430 × 850, densidade 1x.
- Estado desktop comparado: terceiro estágio, “Visitados”, ativo durante o loop; chart e polyline em progressão.

## Avaliação das superfícies obrigatórias

- Fontes e tipografia: hierarquia, peso e escala equivalentes ao mock. Os fallbacks do sistema mantêm boa leitura e não causam quebra ou truncamento.
- Espaçamento e layout: divisão 35,7%/64,3%, margens do formulário, largura dos campos e alinhamento vertical correspondem ao source. Sem overflow horizontal em 1488, 820 ou 430 px.
- Cores e tokens: preto, branco quente e laranja `#FC601D` preservados. Estados de foco têm contraste e identificação visível.
- Imagens e fidelidade: logo, gráfico, funil e mapa usam assets raster derivados do source; não há SVG artesanal, emoji ou ilustração substituta. Recortes, transparência e proporções foram verificados.
- Copy e conteúdo: os textos do mock foram mantidos em português e as mensagens funcionais de recuperação e autenticação são coerentes.
- Acessibilidade: labels associados, mensagem com `aria-live`, toggle de senha com estado anunciado, foco visível e `prefers-reduced-motion` implementado.

## Interações verificadas

- Exibir/ocultar senha altera corretamente o tipo do campo e o nome acessível.
- “Esqueci minha senha” apresenta orientação no `role="status"`.
- Formulário continua usando os IDs e o endpoint de autenticação existentes.
- Login local validado ponta a ponta: entrada, carregamento do dashboard, persistência após recarregar e logout.
- Loop de 7 segundos verificado em estágios distintos do funil.
- Chart e polyline possuem revelação progressiva sincronizada ao loop.
- Mobile usa gráfico, funil e rota como underlay animado, atrás do formulário, sem uma segunda seção.
- `node --check` passou para `app.js`; não foram observadas exceções durante os testes de interação no navegador.

## Histórico de comparação e correções

1. P2 — as regras legadas de grid aplicavam margem lateral aos dois painéis e deslocavam a divisão principal.
   - Correção: `gap`, alinhamento, largura máxima e margens foram normalizados na nova variação do login.
   - Evidência pós-fix: a divisória ocorre em 531 px, igual ao source, na comparação completa.
2. P2 — os recortes iniciais do funil apresentavam linhas e fragmentos transparentes.
   - Correção: os dez assets de estado foram normalizados a partir do maior componente do asset original.
   - Evidência pós-fix: cinco trapézios limpos e proporcionais nas comparações desktop e mobile.
3. P2 — título, gráfico e funil do painel direito estavam aproximadamente 30 px acima do source.
   - Correção: ritmo vertical do painel visual e do conteúdo de acesso foi ajustado.
   - Evidência pós-fix: títulos, gráfico e funil estão alinhados na comparação focada.
4. P2 — no primeiro breakpoint mobile o painel visual ficava oculto.
   - Correção inicial: painel visual passou a ser exibido no mobile, com transparência, loop e ausência de overflow.
   - O primeiro underlay ficou excessivamente presente e foi rejeitado por prejudicar a composição.
   - Correção final: o formulário foi compactado, a arte foi confinada ao viewport e ganhou presença suficiente para que chart, funil e rota fiquem claramente visíveis por debaixo; textos e labels do painel visual foram removidos no mobile.
   - Evidência pós-fix: captura mobile 412 × 600 mostra os três elementos atrás do formulário, com inputs sólidos, sem segunda seção, sem rolagem e sem overflow horizontal.

## Achados finais

- Nenhum P0, P1 ou P2 acionável permanece.
- P3 aceitável: o gráfico e a polyline variam visualmente ao longo do loop, portanto uma captura estática representa apenas um instante da animação.

final result: passed

---

# Design QA — Visitas manuais, opção 3

## Evidências

- Referência escolhida: `C:\Users\isabe\.codex\generated_images\019fa6d2-a551-79d1-8415-b8d2a746488e\call_Isz6gvbhK4iNoW3eG8lK8HPE.png`
- Implementação: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivre.Admin\visits-option3-implementation.png`
- Viewport principal validado: 1440 × 1024.

## Avaliação

- O header existente foi preservado sem alterações estruturais.
- A tela ativa de Visitas não exibe mapa, localização, rota, distância, tráfego ou IA.
- A composição segue a referência: faixa escura, navegação de data, filtro por bairro, linha horizontal de 08:00 a 17:00, cartões alternados, formulário manual e empresas recentes.
- O filtro foi verificado: Pérola mostra somente empresas do Pérola; Centro mostra somente Papelaria Central.
- Edição, cancelamento, seleção de empresa recente, avanço de data, retorno para hoje e estado vazio foram verificados no navegador.
- A interface possui labels, nomes acessíveis, foco visível e controles nativos para data e seleções.
- Não houve erro de JavaScript da tela. Os avisos observados pertencem somente aos endpoints de dados que a prévia local deliberadamente não implementa.
- `node --check wwwroot/app.js`, `git diff --check` e `npm run check` passaram.

## Correções durante a comparação

1. P2 — o cartão de 15:00 estava na faixa inferior.
   - Correção: o Açaí da Praça foi reposicionado na faixa superior, igual à referência.
2. P2 — o cartão inferior encostava no formulário.
   - Correção: altura da linha do dia e conectores foram recalibrados, mantendo respiro antes do formulário.
3. P2 — o filtro de bairro precisava garantir isolamento manual.
   - Correção: a agenda e as empresas recentes agora respeitam o bairro selecionado, sem misturar Centro e Pérola.

## Achados finais

- Nenhum P0, P1 ou P2 acionável permanece.

final result: passed
