# Design QA — Mercado Pago em configuração e conectado

## Evidências

- Referência de configuração: `C:\Users\isabe\AppData\Local\Temp\codex-clipboard-edd81e2f-6392-4572-95ae-0722f1bf9658.png`
- Referência de maquininha conectada: `C:\Users\isabe\AppData\Local\Temp\codex-clipboard-dcf52d8b-e873-457c-9872-f48f1d14f33d.png`
- Implementação de configuração: `AgendaLivre.Windows\artifacts\mercadopago-two-states-qa\setup-final.png`
- Implementação conectada: `AgendaLivre.Windows\artifacts\mercadopago-two-states-qa\connected-final.png`
- Comparação combinada: `AgendaLivre.Windows\artifacts\mercadopago-two-states-qa\comparison.png`

## Estado e escopo comparados

- Modal desktop WPF em 960 × 680.
- Estado 1: Mercado Pago ativado, conta ainda não conectada e nenhuma Point selecionada.
- Estado 2: conta conectada e terminal `PAX_Q92__Q92-1734055152` identificado pela API.
- A referência e as duas capturas finais foram abertas juntas na comparação combinada.

## Resultado visual

- A estrutura principal da opção escolhida foi preservada: cabeçalho, trilho lateral de progresso, ativação, conteúdo por estado, ajuda e rodapé fixo.
- Os textos não cortam nem invadem outros controles no tamanho final.
- Hierarquia, bordas, raios, espaçamento, cores de sucesso e botões seguem os tokens existentes do Agenda Livre.
- A chave Pix manual foi removida quando Mercado Pago está ativo, conforme a regra definida pelo produto.
- No estado conectado, o nome do modelo e o número de série vêm do identificador real retornado pelo Mercado Pago. Não há foto fixa de outro modelo.
- Para um modelo sem imagem oficial local validada, o componente usa o ícone Material Design da categoria e mostra o código exato do equipamento, evitando uma representação incorreta.

## Verificação funcional

- Compilação WPF Release concluída com 0 erros e 0 avisos.
- Alternância entre estado de configuração e estado conectado renderizada por capturas de auditoria.
- Botões de conectar, verificar, buscar Point, atualizar status, trocar maquininha e salvar continuam ligados aos fluxos existentes.
- Ao salvar com Mercado Pago ativo, `PixKey` é limpo; pagamentos Pix seguem a integração Mercado Pago já existente.
- O backend agora devolve `modelCode`, `modelName` e `serial`, derivados do formato oficial `TIPO_DO_TERMINAL__SERIAL`.

## Pendências

- P3 opcional: adicionar imagens oficiais específicas para cada código de hardware quando houver um catálogo de assets confiável. A ausência da foto não bloqueia o uso e evita mostrar um modelo errado.

Nenhum P0, P1 ou P2 permanece.

final result: passed
