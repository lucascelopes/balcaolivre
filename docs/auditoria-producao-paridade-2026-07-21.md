# Auditoria de produção e paridade — Agenda Livre

Data: 21 de julho de 2026

## Veredito

A produção está funcionando e visualmente próxima do WPF, mas ainda não existe paridade completa. Foram encontrados três fluxos funcionais ausentes no Flutter/Web, pequenas diferenças visuais nos snapshots e uma falha importante de transporte: os domínios ainda entregam conteúdo por HTTP sem redirecionar para HTTPS.

## Produção e landing

- Landing, app Web, download Windows, Android e privacidade responderam corretamente.
- Preços, Point Pro 3, telefone `(33) 99131-4125`, Instagram `@minhaagendalivre`, MinhaLoja e footer estão publicados.
- Os prints são telas reais do produto, mas algumas imagens da landing estão ligeiramente desatualizadas em relação ao Flutter publicado.
- A imagem Windows tem 1200×608 e é apresentada em um espaço declarado como 1200×640, causando leve esticamento.

![Landing desktop](../artifacts/production-audit-2026-07-21/landing-desktop-hero.png)

![Seção Web no celular](../artifacts/production-audit-2026-07-21/landing-mobile-android-final.jpg)

## Autenticação e isolamento de contas

- 56 testes de autenticação, sessão, troca de conta, recuperação de senha e quarentena de fixtures passaram.
- Não foi encontrado caminho que abra automaticamente a antiga conta aleatória de testes.
- Sessões antigas são descartadas; token, cache e dados locais são isolados por usuário.
- O backend lê e grava dados usando o usuário autenticado.
- Entrar, criar conta e recuperar senha estão responsivos em desktop e celular.

![Login mobile](../artifacts/production-audit-2026-07-21/app-login-mobile.png)

## Risco de segurança

- `http://app.minhaagendalivre.com.br/` e a landing respondem `200` em HTTP, sem redirecionar para HTTPS.
- As respostas HTTPS não incluem HSTS nem CSP.
- O Supabase está com confirmação automática de e-mail habilitada, permitindo criar conta sem comprovar controle do endereço informado.

## Paridade visual

- 80 testes estruturais e responsivos passaram.
- A escolha de tema passou nos snapshots desktop e mobile.
- Home mobile divergiu 0,70% do snapshot salvo.
- Agenda divergiu 0,33% no desktop e 0,55% no celular.
- Financeiro apresentou as mesmas diferenças de 0,33% e 0,55%.
- As diferenças visíveis concentram-se em textos de data, rótulos financeiros e estilo do botão do WhatsApp.

Esquerda: WPF. Direita: Flutter atual.

![Comparação WPF e Flutter](../artifacts/production-audit-2026-07-21/comparison-wpf-flutter-desktop.jpg)

Esquerda: print mobile usado na landing. Direita: Flutter atual.

![Comparação mobile](../artifacts/production-audit-2026-07-21/comparison-landing-current-mobile.jpg)

## Lacunas funcionais de paridade

1. **Venda de produtos:** o Flutter não conclui venda nem baixa estoque.
2. **Instagram:** o Flutter oferece links visuais, mas não possui OAuth, Direct, status e desconexão como o WPF.
3. **Conta do cliente:** é possível criar saldo a receber, mas não existe fluxo para quitar o saldo no Flutter.
4. **Faturamento da Home:** o cálculo não usa exatamente a mesma regra do WPF.
5. **Logo personalizada:** a experiência de exibição e edição não acompanha a versão WPF.

## Acessibilidade e SEO

- Há textos de 7–9 px e exemplos de contraste abaixo de 4.5:1.
- Alguns alvos do footer têm aproximadamente 27 px de altura.
- O sitemap retorna 404.
- Esta verificação não equivale a uma auditoria completa de conformidade WCAG.

## Limites

Não foi criada uma conta real nem enviada uma recuperação de senha real. A entrega de e-mail e o painel autenticado em produção não foram verificados ponta a ponta com credenciais reais.
