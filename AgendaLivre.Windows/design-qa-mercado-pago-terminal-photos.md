# Design QA — fotos das maquininhas Mercado Pago

## Escopo

- Modal WPF de configuração do Mercado Pago, estado conectado.
- Correspondência automática entre o código retornado pela API e a foto comercial da Point.
- Modelos cobertos: `PAX_Q92`, `NEWLAND_N950`, `PAX_A910`, `GERTEC_MP35P` e `INGENICO_MOVE2500`.

## Correspondências verificadas

| Código do terminal | Nome comercial exibido | Foto usada |
| --- | --- | --- |
| `PAX_Q92` | Point Pro 3 | Point Pro 3 amarela, com teclado físico |
| `NEWLAND_N950` | Point Smart 2 | Point Smart 2 amarela, tela touch |
| `PAX_A910` | Point Smart | Point Smart branca e azul |
| `GERTEC_MP35P` | Point Pro 2 | Point Pro 2 azul |
| `INGENICO_MOVE2500` | Point Pro | Point Pro azul baseada na Move/2500 |

## Verificação visual

- Comparação conjunta: `artifacts/mercadopago-terminal-photos-qa/comparison-all-terminal-models.png`.
- Todos os cinco estados foram renderizados pelo aplicativo WPF em `1366x768`.
- A foto permanece dentro do cartão, sem esticar, cortar texto ou cobrir nome, série, status ou ações.
- A foto é trocada quando o código do modelo muda; códigos desconhecidos continuam usando o ícone de fallback.
- O modelo comercial aparece como título e o código do fabricante permanece visível abaixo da foto.

## Verificação técnica

- `dotnet build AgendaLivre.Windows/AgendaLivre.Windows.csproj -c Release`
- Resultado: 0 avisos e 0 erros.

final result: passed
