# Design QA — ativação e renovação da assinatura

## Fonte e estado validado

- Fonte selecionada: `C:\Users\isabe\.codex\generated_images\019fa06c-04e2-7033-a6fb-83932b02519e\call_naoErYR7UCMlW75LURcrLQAk.png`
- Implementação desktop: `artifacts/subscription-activation-desktop-final-v5.png`
- Comparação lado a lado: `artifacts/subscription-activation-comparison-final-v2.png`
- Implementação mobile: `artifacts/subscription-activation-mobile-top-final-v4.png`
- Continuação mobile: `artifacts/subscription-activation-mobile-lower-final-v4.png`
- Cadastro acionado pelo CTA: `artifacts/subscription-auth-mobile-final-v4.png`
- Renovação desktop com cartão salvo: `artifacts/subscription-renewal-desktop-final-v2.png`
- Renovação mobile com cartão salvo: `artifacts/subscription-renewal-mobile-final-v2.png`
- Estado: pagamento confirmado, assinatura ainda não vinculada, usuário sem sessão.
- Desktop: 1280 × 911, DPR 1.
- Mobile: 390 × 844, DPR 1.
- A fonte foi normalizada para 1280 × 911 antes da comparação.

## Regiões comparadas

- Cabeçalho e marca.
- Confirmação do pagamento e e-mail mascarado.
- Linha horizontal de três etapas.
- Título, descrição e CTAs de cadastro/entrada.
- Escolha Web/Windows e nota de segurança.
- Fluxo mobile completo, incluindo rolagem e abertura do cadastro.
- Bloqueio de licença expirada, resumo seguro do cartão e CTAs de renovação.

## Histórico de correções

1. P1: proporções verticais, hierarquia e progresso divergentes. Corrigidos com layout dividido, tipografia editorial e progresso horizontal.
2. P2: fonte do título e densidade visual. Corrigidos com Libre Baskerville, espaçamento e escala equivalentes à referência.
3. P1 mobile: título quebrava no meio das palavras e o espaçamento vertical era excessivo. Corrigidos com escala e paddings responsivos.
4. P2: texto técnico mencionava Supabase para o usuário. Trocado por linguagem de produto.

## Verificação funcional e visual final

- Nenhum erro ou aviso no console do preview.
- CTA “Criar minha conta” abre o cadastro correto.
- CTA “Já tenho conta” compartilha o mesmo fluxo autenticado.
- Layout mobile rola sem corte e mantém os dois CTAs e as escolhas de acesso visíveis.
- Renovação mostra somente bandeira, quatro últimos dígitos e validade; os dados completos continuam no Stripe.
- Desvio P3 intencional: a implementação usa a marca oficial real do Agenda Livre e omite as curvas decorativas geradas da referência.

final result: passed
