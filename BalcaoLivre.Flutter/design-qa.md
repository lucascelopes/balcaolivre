# Design QA — Balcão Livre PDV Online

## Comparison target

- Source visual truth:
  - `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\01-wpf-main.png`
  - `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\02-wpf-comanda-open.png`
- Browser-rendered implementation:
  - `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\09-flutter-desktop-after-rail-fix.png`
  - `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\10-flutter-mobile-after-rail-fix.png`
- Full-view comparison:
  - `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\11-wpf-flutter-final-side-by-side.png`
- Focused comparison:
  - `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\12-final-focused-regions.png`

## Normalization

- Desktop source pixels: 1366 x 720.
- Desktop implementation pixels: 1366 x 720.
- Desktop CSS viewport: 1366 x 720.
- Mobile implementation pixels: 390 x 844.
- Mobile CSS viewport: 390 x 844.
- Device scale factor: 1.
- No density resampling was required.
- State: caixa aberto, área Comanda. The WPF QA fixture intentionally has no mesas; the Flutter visual-QA fixture contains seeded mesas so the operational card grid can also be evaluated. Shell geometry was compared at the same viewport and operational state; data-dependent content was not treated as a fidelity mismatch.

## Findings

- No actionable P0, P1, or P2 findings remain.
- P3 / accepted platform difference: native Windows caption controls visible in the WPF title strip are not duplicated in the Flutter web build. The web build keeps the same 32 px title strip without fake window controls.
- P3 / expected fixture difference: operator, store, cash total, elapsed time, and mesa counts differ because each QA fixture carries different sample data.

## Required fidelity surfaces

- Fonts and typography: hierarchy, weight, sizing, wrapping, truncation, and compact labels match the WPF density. Flutter retains its cross-platform font fallback; no material hierarchy or wrapping drift remains.
- Spacing and layout rhythm: 204 px sidebar, 88 px two-tier header, 84 px ribbon, 12/10/12/12 content inset, 420 px minimum command inspector, footer height, separators, cards, and persistent controls match the WPF composition.
- Colors and visual tokens: black/charcoal shell, orange active states, warm off-white surfaces, red cash action, green connectivity/state accents, borders, and muted secondary text are visually aligned.
- Image quality and asset fidelity: the compared shell uses code-native standard icons and the existing BL brand mark in both implementations; no visible source image asset was replaced with a placeholder.
- Copy and content: operational section names, ribbon actions, filters, command labels, payment area, cash actions, and keyboard-hint footer match the WPF information architecture. Data values differ only by fixture.

## Comparison history

### Pass 1 — blocked

Evidence:

- `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\03-flutter-desktop-before.png`
- `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\04-flutter-mobile-before.png`

Earlier P2 findings:

- Sidebar was 254 px instead of 204 px.
- Header was a single 70 px row instead of the WPF 32 + 56 px structure.
- Ribbon was 105 px instead of 84 px.
- Command inspector proportions drifted from the WPF clamp.
- Command panel showed a visible bottom overflow warning.

Fixes:

- Matched the desktop rail, header, ribbon, content insets, inspector clamp, and command-panel spacing to the WPF measurements.
- Preserved the existing mobile information architecture and two-column mesa grid.

### Pass 2 — blocked

Evidence:

- `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\05-flutter-desktop-after.png`
- `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\07-wpf-flutter-side-by-side.png`
- `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\08-focused-regions.png`

Remaining P2 finding:

- The active primary rail item was inset and rounded while the WPF active item spans the full 204 px rail.

Fix:

- Extended primary rail actions across the full sidebar width, removed the active-item radius, and retained local margins only for the operator card and utility cards.

### Pass 3 — passed

Post-fix evidence:

- `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\09-flutter-desktop-after-rail-fix.png`
- `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\10-flutter-mobile-after-rail-fix.png`
- `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\11-wpf-flutter-final-side-by-side.png`
- `C:\Users\isabe\Downloads\balcaolivre-main\balcaolivre-main\artifacts\pdv-parity-current\12-final-focused-regions.png`

No P0/P1/P2 finding remains in the compared shell.

## Runtime verification

- Browser-rendered URL: `http://127.0.0.1:4175/`
- Browser viewports checked: 1366 x 720 and 390 x 844.
- Primary interactions covered: login bypass for visual QA, closed/open cash states, desktop and mobile navigation, command flow, kitchen, delivery, settings, backup, WhatsApp, iFood, fiscal/TEF, discount authorization, stock, team, cash opening, reconciliation, and closing.
- Automated verification: `flutter analyze` passed; full `flutter test` passed with 50 tests.
- Console/runtime check: Flutter web-server stderr was empty and neither desktop nor mobile browser render showed an exception or overflow overlay.

## Implementation checklist

- [x] Match WPF desktop shell geometry.
- [x] Match sidebar active-state behavior.
- [x] Remove command-panel overflow.
- [x] Preserve mobile layout and navigation.
- [x] Verify full Flutter test suite.
- [x] Capture full-view and focused post-fix comparisons.

final result: passed
