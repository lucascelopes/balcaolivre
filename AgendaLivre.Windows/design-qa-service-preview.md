# Design QA — Criar serviço, opção 3

- Source visual truth: `C:\Users\isabe\.codex\generated_images\019f6b83-606f-7ec0-8558-a15aeb770bf9\exec-25439c0e-12d4-4fc6-b757-85831266f1ac.png`
- Implementation screenshot: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\agenda-service-preview-implementation.png`
- Viewport: 1366 × 768; modal target: 980 × 720
- State: new service, active, Salão de Beleza, Atendimento, 120 min, live preview visible

## Full-view comparison evidence

The implementation reproduces the selected split-panel direction: structured form sections on the left and a warm live preview surface on the right. The preview includes service name, active state, category, segment, duration, price, resource, commission, and an agenda appearance card.

## Focused region comparison evidence

- Form hierarchy: Catálogo, Tempo e agenda, and Preço e equipe preserve the current application fields and validation order.
- Preview hierarchy: service name is the strongest text, followed by semantic status and compact icon rows.
- Dynamic behavior: Windows UI Automation changed the service name to `Corte premium`; the preview updated immediately.
- Primary action: existing Save validation and form-result construction remain attached to `Salvar serviço`.

## Required fidelity surfaces

- Fonts and typography: existing Agenda Livre WPF typography and optical weights retained.
- Spacing and layout rhythm: 64/36 split, 24 px gutter, 30 px icon badges, compact fields, and 8 px preview rows.
- Colors and visual tokens: reused `Ink`, `Muted`, `Line`, `WarmSoft`, `AccentSoft`, `Accent`, and `AccentDark`.
- Image quality and asset fidelity: no raster imagery is required; icons use the existing MaterialDesign library.
- Copy and content: all current Portuguese fields and service configuration concepts are preserved.

## Findings

No actionable P0, P1, or P2 differences remain. The native footer keeps Cancel and Save together, rather than embedding Save inside the preview, to preserve the application's established error and keyboard behavior.

## Comparison history

1. Initial split layout left the final price section partially below the fold.
2. Fix: reduced field heights, vertical margins, description height, and active-state spacing while retaining a 720 px modal ceiling.
3. Post-fix: project compiled with 0 warnings/errors and the dynamic preview interaction passed.

## Follow-up polish

- P3: on displays taller than 768 px, the preview may use slightly more vertical spacing.

final result: passed
