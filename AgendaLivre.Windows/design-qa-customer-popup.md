# Design QA — popup de cliente

## Referência

- Mockup selecionado: `C:\Users\isabe\.codex\generated_images\019f8a5d-441f-7692-9066-6593b749d98b\exec-c8157999-9fe5-4ace-ad54-4cbd0285d7b5.png`
- Captura WPF compacta: `customer-popup-compact-final.png`
- Viewport validado: 1366 × 768.
- Dimensões do popup: 520 × 529 px na captura validada.

## Verificações

- Cabeçalho preto com avatar, nome, status do WhatsApp e ações: passou.
- Resumo de próximo atendimento e conta do cliente: passou.
- Abas Perfil, Histórico e Notas: passou; as três foram acionadas por UI Automation.
- Telefone, segmento, perfil e histórico recente: passou.
- Ações Editar cliente e WhatsApp: preservadas e reposicionadas no rodapé.
- Popup centralizado, sem cortes no viewport validado: passou.
- Densidade visual compactada: cabeçalho, resumo, campos, histórico e rodapé tiveram alturas e espaçamentos reduzidos.
- Botões do cabeçalho com área de 34 × 34 px e alinhamento uniforme: passou.
- Build isolado de Release: passou com 0 avisos e 0 erros.

## Observações de baixa prioridade

- A conta mantém a mensagem “Sem saldo em aberto”, informação útil do produto real que não aparece no mockup final.
- O ícone de mensagem usa o ícone do WhatsApp já adotado pelo aplicativo.

final result: passed
