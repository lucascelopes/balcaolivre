# Design QA — foto da maquininha PAX Q92

## Evidências

- Estado reportado: `C:\Users\isabe\AppData\Local\Temp\codex-clipboard-c6f3dc5f-bf1e-4064-a37d-edf41c647bbd.png`
- Fonte do produto: página oficial `PAX Q92` da PAX Technology.
- Implementação final: `AgendaLivre.Windows\artifacts\mercadopago-pax-q92-qa\connected-pax-q92.png`
- Comparação combinada: `AgendaLivre.Windows\artifacts\mercadopago-pax-q92-qa\comparison.png`

## Verificação

- A comparação combinada confirma que o ícone genérico foi substituído pela fotografia correta do hardware PAX Q92.
- A fotografia só aparece quando `ResolvedModelCode` é exatamente `PAX_Q92`.
- Outros modelos continuam usando o fallback, evitando exibir a maquininha errada.
- Nome do modelo, série, Point ID, loja e status continuam sendo preenchidos pelos dados vinculados.
- A imagem foi recortada preservando transparência, proporção e legibilidade dentro do card.
- O modal não apresenta cortes, sobreposição ou mudança indevida no layout.
- Compilação Release concluída com 0 erros e 0 avisos.

Nenhum P0, P1 ou P2 permanece.

final result: passed
