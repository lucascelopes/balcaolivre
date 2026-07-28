# Design QA — Gerenciar profissionais, opção 3

- Source visual truth: `C:\Users\isabe\.codex\generated_images\019f6b6a-fb86-7771-9417-e46f9f93020c\exec-ae831ad3-942c-44c5-b3cb-6c59431e2f53.png`
- Implementation screenshot: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\agenda-professionals-table-implementation.png`
- Viewport: 1366 × 768; modal: 920 × 367
- State: Meu estabelecimento > Gerenciar profissionais; one active professional

## Full-view comparison evidence

The implementation reproduces the selected compact management-table direction with search, fixed headers, a professional row, semantic active status, row edit action, count, close/cancel, and create action. It intentionally keeps the product's existing footer placement for the primary action instead of moving it into the toolbar.

## Focused region comparison evidence

- Search: full-width field accepts name, role, and segment queries.
- Table: columns align across header and rows; long values use ellipsis.
- Status/action: “Ativo” uses the application orange badge token and “Editar” remains a native application button.
- Empty filter state: querying `xyz` showed “Nenhum profissional encontrado.”; querying `Manicure` restored the edit row.

## Required fidelity surfaces

- Fonts and typography: existing Agenda Livre WPF typography retained; compact 10.5–12.5 px table hierarchy matches the selected direction.
- Spacing and layout rhythm: 58 px rows, 38 px header, compact toolbar, 16 px table radius, and aligned column tracks.
- Colors and visual tokens: reused `Ink`, `Muted`, `Line`, `GraySoft`, `AccentSoft`, `Accent`, and `AccentDark`.
- Image quality and asset fidelity: the component has no raster imagery; the professional icon uses the project's MaterialDesign icon library.
- Copy and content: Portuguese labels, Manicure 1, Manicure, Salão de Beleza, Ativo, Editar, and Novo profissional are preserved.

## Findings

No actionable P0, P1, or P2 differences remain. Keeping the create action in the standard application footer is an intentional system-consistency deviation from the generated mockup.

## Comparison history

The first rendered implementation passed the layout comparison. Functional QA additionally verified both filtered-empty and filtered-result states, so no visual correction iteration was required.

## Follow-up polish

- P3: for teams above roughly eight professionals, add an internal fixed-height row scroller while keeping the table header sticky.

final result: passed
