# Design QA final — compra, conta e destino Web/Windows

Este bloco substitui o relatório histórico de pós-compra abaixo. O fluxo final não exibe nem copia chave de ativação: a liberação é vinculada à conta e ao dispositivo por handoff descartável.

## Alvo final

- Referência aprovada: `C:\Users\isabe\.codex\generated_images\019fa5f3-5365-73a3-95ed-ec5c7704a2c8\call_eNfqxcphWag47NUoW2hPuDYv.png`
- Captura da implementação: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\payment-success-qa.png`
- Comparação conjunta: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\payment-success-reference-vs-implementation.jpg`
- Estado: pagamento aprovado, criação/entrada de conta e escolha entre navegador e Windows.
- Captura real do navegador: `1265 × 712`, DPR `1`.

## Resultado

- A hierarquia da referência foi preservada: status da compra, progresso em três etapas, formulário de conta à esquerda e os destinos Web/Windows à direita.
- Paleta consistente com o produto laranjado: destaque `#FC601D`, papel/creme, preto quente e verde somente no status de pagamento aprovado.
- Os dois destinos usam capturas reais do PDV laranja; não há placeholder, carcaça falsa nem superfície azul.
- A versão final inclui CTA funcional para abrir o Web e handoff descartável para abrir/instalar o Windows.
- Nenhuma chave de licença fica visível na interface ou na URL pública.
- Sem imagem quebrada, recorte indevido, overflow horizontal ou desalinhamento P0/P1/P2 na captura final.
- Build Vinext aprovado após a captura.

final result: passed

---

# Design QA — pós-compra Balcão Livre PDV, identidade laranja

## Alvo

- Anotação visual do usuário: `C:\Users\isabe\AppData\Local\Temp\codex-clipboard-805a9cb8-66a6-48d6-acda-88266e87edfd.png`
- Anotação sobre a moldura de celular: `C:\Users\isabe\AppData\Local\Temp\codex-clipboard-a490f452-db44-4374-a943-54ff6eb7f82f.png`
- Referência Links laranja: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\balcao-links-narrow-production-final.png`
- Referência Flutter mobile laranja: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\10-flutter-mobile-after-rail-fix.png`
- Referência WPF laranja: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\public\brand\pdv-orange-dashboard.png`
- Implementação desktop: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\payment-choice-orange-desktop-v1.png`
- Implementação mobile: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\payment-choice-orange-mobile-v1.png`
- Implementação desktop sem aparelho falso: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\payment-choice-clean-preview-desktop-v2.png`
- Implementação mobile sem aparelho falso: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\payment-choice-clean-preview-mobile-v2.png`
- Comparação antes/depois da prévia Web: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\payment-choice-phone-frame-before-after.png`
- Comparação azul versus laranja: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\payment-choice-blue-vs-orange-comparison.png`
- Viewport desktop: `1487 × 1058` pixels CSS, DPR `1`, captura `1487 × 1058` pixels
- Viewport mobile: `390 × 844` pixels CSS, DPR `1`, captura `390 × 844` pixels
- Fonte e implementação foram comparadas sem redimensionamento ou normalização de densidade.
- Estado: pagamento aprovado, chave gerada, escolha entre Web e Windows.

## Findings

- Nenhum problema P0, P1 ou P2 permanece.
- Nenhuma superfície azul permanece na interface pós-compra.
- A estrutura, hierarquia, largura das duas opções, CTA principal, CTA Windows e faixa de garantias continuam iguais à opção 2 escolhida.
- A opção Web usa o print real do Flutter mobile laranja; a opção Windows usa o print real do WPF laranja.
- A moldura preta de celular, o alto-falante e a carcaça genérica foram removidos; a captura real aparece diretamente, ampliada e com recorte limpo.
- O logo azul foi substituído pelo símbolo oficial BL preto com ponto laranja.
- A chave de ativação foi adicionada ao cabeçalho para preservar o fluxo funcional já existente sem empurrar os cards para baixo.

## Fidelidade

- Tipografia: Segoe UI com pesos 700–900 reproduz a hierarquia compacta e forte do mockup; título, subtítulo, badges, cards e CTAs mantêm proporções equivalentes.
- Espaçamento e layout: cabeçalho, introdução, grade 50/50, cards, CTAs e faixa inferior cabem exatamente em `1487 × 1058`, sem rolagem ou overflow.
- Cores: preto quente `#211F1D`, papel/creme e o destaque correto `#FC601D`; o status de pagamento também usa laranja.
- Imagens: `pdv-orange-dashboard.png` é o print real do WPF; `pdv-flutter-mobile-orange.png` é o print real do Flutter em viewport mobile. O ativo mobile azul antigo foi removido.
- Conteúdo: textos de acesso Web, instalação Windows, conta existente, ajuda e validade do plano permanecem claros e consistentes com o fluxo pós-compra.

## Comparação focada

- O comparativo lado a lado permitiu verificar o cabeçalho, o bloco de título, os dois cards, os CTAs e a faixa de garantias no mesmo tamanho.
- Os prints reais são legíveis e mantêm o recorte correto dentro de seus containers.
- A comparação `598 × 584` confirma a remoção do logo, fundo, textos e screenshot azulados.
- A captura mobile confirma que a opção recomendada, o print Flutter laranja e o CTA principal permanecem íntegros em `390px`.

## Histórico de comparação

### Iteração 1 — blocked

- [P2] A chave de ativação abaixo do subtítulo empurrava os cards e deixava a faixa de garantias fora do primeiro viewport.
- Correção: a chave foi movida para o cabeçalho, preservando acesso e função de cópia.

### Iteração 2 — passed

- Cards voltaram à mesma posição vertical do mockup.
- A página mede `1487 × 1058` sem overflow e exibe a composição completa.
- Prints reais desktop e mobile substituem as imagens conceituais conforme solicitado.

### Iteração 3 — blocked

- [P1] O logo azul, os textos navy, o fundo azul-claro e o print antigo do PDV Web destoavam da identidade real da Landing, Links, WPF e Flutter.
- Correção: toda a paleta foi alinhada a preto, creme e `#FC601D`; o print mobile foi substituído pela captura real do Flutter laranja.

### Iteração 4 — passed

- Comparação azul versus laranja confirma que nenhuma superfície azul permanece.
- Flutter mobile e WPF usam capturas reais atuais, sem ilustração ou placeholder.
- Aba limpa: zero erros ou avisos de console.

### Iteração 5 — blocked

- [P2] A carcaça preta de celular reduzia demais a captura do Flutter e introduzia um aparelho genérico que não pertence à identidade do produto.
- Correção: remoção completa da moldura, alto-falante, padding preto e sombra de aparelho.

### Iteração 6 — passed

- A captura real do Flutter agora ocupa diretamente o espaço da prévia, com `222 × 258px` no desktop e `136 × 238px` no mobile.
- A comparação antes/depois confirma que o conteúdo do PDV ficou maior e que nenhum elemento de aparelho falso permanece.
- Zero imagens quebradas, zero seletor antigo de moldura no DOM e zero overflow horizontal em desktop e mobile.

## Verificação funcional

- Build Next/Vinext: aprovado.
- Links verificados: criação/entrada Web, instalador Windows e guia de instalação.
- Chave de ativação visível; função de cópia inclui Clipboard API e fallback legado.
- Responsividade validada em desktop e mobile.
- Console do navegador: zero erros e zero avisos na captura final.
- Lacuna P3: o navegador local isolado não concedeu leitura do clipboard para confirmar o conteúdo copiado, mas a chave permanece visível e o fallback foi implementado.

final result: passed

---

# Relatório anterior — Balcão Livre `/links` em celulares estreitos

## Alvo

- Verdade visual do defeito no hero: `C:\Users\isabe\AppData\Local\Temp\codex-clipboard-616bd0ef-4e1e-43c3-8aad-85b5d3132d56.png`
- Verdade visual do defeito na oferta: `C:\Users\isabe\AppData\Local\Temp\codex-clipboard-9c7a76b3-d710-4bec-a103-e8df75b04ae0.png`
- Implementação do hero: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\balcao-links-narrow-hero-final.png`
- Implementação da oferta: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\balcao-links-narrow-point-final.png`
- Produção final: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\balcao-links-narrow-production-point-final.png`
- Comparação do hero: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\balcao-links-narrow-hero-comparison.png`
- Comparação da oferta: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\BalcaoLivreLadingPage\artifacts\balcao-links-narrow-point-comparison.png`
- Viewport de teste: `314 × 844` pixels CSS com `299px` úteis de conteúdo e DPR `1`
- Referência do hero: `299 × 397` pixels
- Referência da oferta: `296 × 177` pixels
- Estado: página pública, sem autenticação, breakpoint `max-width: 360px`

## Findings

- Nenhum problema P0, P1 ou P2 permanece.
- O título do hero termina em `x=152,45px`; a imagem começa em `x=161,45px`. Há uma separação visual de `9px`, sem invasão.
- O título da oferta termina em `x=155,47px`; a Point começa em `x=144px`, mas permanece atrás da área de texto somente na margem final, sem cobrir nenhuma letra.
- A página mede `299px` úteis, sem rolagem horizontal.

## Fidelidade

- Tipografia: a família Barlow Condensed e os pesos originais foram preservados. O título usa `25px` somente abaixo de `360px`.
- Espaçamento: o hero muda para colunas `54% / 46%` no breakpoint estreito, preservando a hierarquia e separando texto e imagem.
- Cores: preto, papel e laranja `#FC601D` permanecem inalterados.
- Imagens: a captura real do PDV e a Point Pro 3 transparente permanecem nos mesmos ativos, sem distorção.
- Conteúdo: nenhuma frase, link ou chamada foi removida.

## Histórico de comparação

### Iteração 1 — blocked

- [P1] `RESTAURANTE` avançava para dentro da captura do PDV no hero.
- [P1] A Point Pro 3 cobria `NO CAIXA` e parte da oferta.
- Primeira correção: redução do título para `31px` e reposicionamento da Point.
- Evidência: a Point deixou de cobrir o texto, mas o título do hero ainda invadia a imagem.

### Iteração 2 — passed

- Correção final: hero em `54% / 46%`, título em `25px`, área de texto da oferta em `52%` e Point com `220px`, deslocada para a direita.
- As comparações lado a lado mostram o texto integralmente separado das imagens.
- Verificação em aba limpa: zero erros de console e zero overflow horizontal.

## Verificação funcional

- Build Next/Vinext: aprovado.
- Dry-run do Worker Cloudflare: aprovado.
- Deploy Cloudflare: `f32c8df7-b604-4faf-aae3-9e7ef01a3b1f`.
- Verificação pós-deploy: zero erros de console, zero overflow horizontal e as mesmas medidas da prévia aprovada.
- Links, faixa social e oferta anual preservados.

final result: passed
