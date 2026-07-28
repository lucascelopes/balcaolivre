# Design QA — Popover de agendamentos do painel

## Evidências

- Source visual truth: `C:\Users\isabe\.codex\generated_images\019f8f89-84e9-7f92-926a-99ad02d8f52c\call_0bhUERqUPfA3dZYX7FXBvHjJ.png`
- Implementation screenshot: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\AgendaLivre.Windows\home-metric-popover-option13-final-v2.png`
- Full-view comparison: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\AgendaLivre.Windows\home-metric-popover-comparison-full.png`
- Focused comparison: `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\AgendaLivre.Windows\home-metric-popover-comparison-focused.png`
- Source pixels: 1704 × 923.
- Implementation pixels: 1366 × 768.
- Application audit viewport requested: 1366 × 900; the physical desktop capture was bounded by the 1366 × 768 work surface.
- Focused implementation crop: 1065 × 420.
- Density normalization: both captures were compared at 1×; each side of the combined comparisons was resized proportionally without changing aspect ratio.
- State: Painel > “Agendamentos hoje” selected > popover open > filtro “Todos”.

## Findings

- No actionable P0, P1, or P2 mismatch remains.
- The selected dark metric, white anchored card, pointer, title/date hierarchy, segmented filters, client rows, status badges, pending actions, close control, shadow, radii, and agenda footer follow the selected visual direction.
- The production screen uses real appointment data, so names and totals intentionally differ from the mock.

## Required fidelity surfaces

- Fonts and typography: Segoe UI Variable Text/Segoe UI matches the application design system and closely matches the source’s neutral UI type. Title, metric values, labels, helper text, and row hierarchy retain the intended weights and wrapping.
- Spacing and layout rhythm: the four-column strip, active outline, anchored card, 18 px card radius, section gaps, row dividers, action alignment, and footer rhythm match the source. The list height adapts to the available desktop work area so the footer is never hidden.
- Colors and visual tokens: the implementation uses the existing Agenda Livre ink, panel, line, coral accent, warm-soft, and semantic green/red brushes. Contrast and semantic states match the source.
- Image quality and asset fidelity: this component contains no raster imagery or brand illustration. All visible icons come from the existing Material Design icon library; no placeholder art, emoji, handcrafted SVG, or CSS-style drawing was introduced.
- Copy and content: “Agendamentos de hoje”, the localized date, “Todos”, “Confirmados”, “A confirmar”, “Confirmar”, and “Abrir agenda completa” match the selected design. Counts and customer/service content come from the real data model.

## Interaction verification

- Clicking “Agendamentos hoje” opens the anchored popover and highlights the selected metric.
- “Todos”, “Confirmados”, and “A confirmar” filter the real appointments.
- Pending rows expose an enabled WhatsApp action when a valid phone exists and a “Confirmar” action.
- Confirming an isolated audit appointment updated real model-derived counts from 4 to 5 confirmed and from 3 to 2 pending, persisted through the app store, refreshed the dashboard, and closed the popover.
- Escape closes the popover.
- Clicking “Abrir agenda completa” navigates to “Agenda de hoje”.
- Outside-click closing is provided by the native WPF popup behavior.
- The WhatsApp control and message route were verified without triggering an external send during QA.

## Comparison history

1. Initial implementation — `home-metric-popover-option13-v1.png`
   - [P2] On a 1366 × 768 desktop, a fixed 310 px list height placed the footer below the usable work area.
   - Fix: reduced the default list height and then made it responsive to the actual space below the selected metric.
2. Intermediate implementation — `home-metric-popover-option13-final.png`
   - [P2] The fixed 230 px list still allowed the footer to fall behind the taskbar on the shorter desktop.
   - Fix: calculate the list maximum from the work area, clamped between 96 and 230 px. Large screens retain more rows; short screens keep the footer visible with scrolling.
3. Final implementation — `home-metric-popover-option13-final-v2.png`
   - Post-fix evidence shows the full title, tabs, visible rows, sticky agenda footer, and selected metric within the usable desktop area.

## Follow-up polish

- [P3] On short 768 px screens, only two full rows are visible at once. This is an intentional responsive trade-off; the list scrolls and all appointments remain accessible.

## Implementation checklist

- [x] Faithful selected visual structure.
- [x] Real appointment counts and rows.
- [x] Functional filters.
- [x] Functional confirmation with persistence and refresh.
- [x] WhatsApp action wired to the existing app flow.
- [x] Escape, outside click, close button, and agenda navigation.
- [x] Responsive work-area fit.
- [x] Release build passed.

final result: passed
