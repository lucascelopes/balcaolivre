import 'package:flutter/material.dart';

import '../app/theme/agenda_theme.dart';

class AgendaPanel extends StatelessWidget {
  const AgendaPanel({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(16),
    this.margin = EdgeInsets.zero,
    this.color,
    this.onTap,
    this.radius = 8,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final EdgeInsetsGeometry margin;
  final Color? color;
  final VoidCallback? onTap;
  final double radius;

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return Container(
      margin: margin,
      decoration: BoxDecoration(
        color: color ?? tokens.panel,
        border: Border.all(color: tokens.line),
        borderRadius: BorderRadius.circular(radius),
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(radius),
          child: Padding(padding: padding, child: child),
        ),
      ),
    );
  }
}

class AgendaIconBadge extends StatelessWidget {
  const AgendaIconBadge(
    this.icon, {
    super.key,
    this.color,
    this.background,
    this.size = 42,
    this.iconSize = 20,
  });

  final IconData icon;
  final Color? color;
  final Color? background;
  final double size;
  final double iconSize;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: background ?? t.accentSoft,
        borderRadius: BorderRadius.circular(size * .28),
      ),
      alignment: Alignment.center,
      child: Icon(icon, color: color ?? t.accent, size: iconSize),
    );
  }
}

class AgendaPageHeader extends StatelessWidget {
  const AgendaPageHeader({
    super.key,
    required this.title,
    required this.subtitle,
    this.eyebrow,
    this.icon,
    this.actions = const [],
    this.panel = false,
  });

  final String title;
  final String subtitle;
  final String? eyebrow;
  final IconData? icon;
  final List<Widget> actions;
  final bool panel;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final content = LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth < 680;
        final heading = Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            if (icon != null) ...[
              AgendaIconBadge(icon!, size: 48, iconSize: 24),
              const SizedBox(width: 14),
            ],
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: compact ? 23 : 27,
                      fontWeight: FontWeight.w800,
                      height: 1.1,
                    ),
                  ),
                  const SizedBox(height: 5),
                  Text(
                    subtitle,
                    style: TextStyle(color: t.muted, fontSize: 13),
                  ),
                  if (eyebrow != null) ...[
                    const SizedBox(height: 6),
                    Text(
                      eyebrow!,
                      style: TextStyle(
                        color: t.accent,
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ],
        );

        if (compact || actions.isEmpty) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              heading,
              if (actions.isNotEmpty) ...[
                const SizedBox(height: 15),
                Wrap(spacing: 8, runSpacing: 8, children: actions),
              ],
            ],
          );
        }
        return Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            Expanded(flex: 4, child: heading),
            const SizedBox(width: 16),
            Flexible(
              flex: 5,
              child: Align(
                alignment: Alignment.centerRight,
                child: Wrap(
                  alignment: WrapAlignment.end,
                  spacing: 8,
                  runSpacing: 8,
                  children: actions,
                ),
              ),
            ),
          ],
        );
      },
    );

    if (!panel) return content;
    return AgendaPanel(padding: const EdgeInsets.all(17), child: content);
  }
}

class AgendaMetricCard extends StatelessWidget {
  const AgendaMetricCard({
    super.key,
    required this.label,
    required this.value,
    required this.caption,
    required this.icon,
    this.tone,
    this.softTone,
    this.badge,
  });

  final String label;
  final String value;
  final String caption;
  final IconData icon;
  final Color? tone;
  final Color? softTone;
  final String? badge;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final activeTone = tone ?? t.accent;
    return AgendaPanel(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 13),
      child: Row(
        children: [
          AgendaIconBadge(
            icon,
            color: activeTone,
            background: softTone ?? t.accentSoft,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        label,
                        style: TextStyle(color: t.muted, fontSize: 12),
                      ),
                    ),
                    if (badge != null)
                      AgendaPill(
                        label: badge!,
                        color: t.graySoft,
                        textColor: t.muted,
                      ),
                  ],
                ),
                const SizedBox(height: 3),
                Text(
                  value,
                  style: TextStyle(
                    color: t.ink,
                    fontWeight: FontWeight.w800,
                    fontSize: 21,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  caption,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 11),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class AgendaPill extends StatelessWidget {
  const AgendaPill({
    super.key,
    required this.label,
    this.color,
    this.textColor,
    this.icon,
  });

  final String label;
  final Color? color;
  final Color? textColor;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 4),
      decoration: BoxDecoration(
        color: color ?? t.accentSoft,
        borderRadius: BorderRadius.circular(30),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 13, color: textColor ?? t.accentDark),
            const SizedBox(width: 4),
          ],
          Text(
            label,
            style: TextStyle(
              color: textColor ?? t.accentDark,
              fontSize: 10.5,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class AgendaSectionTitle extends StatelessWidget {
  const AgendaSectionTitle({
    super.key,
    required this.title,
    this.subtitle,
    this.icon,
    this.trailing,
  });

  final String title;
  final String? subtitle;
  final IconData? icon;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (icon != null) ...[
          AgendaIconBadge(icon!, size: 38, iconSize: 19),
          const SizedBox(width: 10),
        ],
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 17,
                  fontWeight: FontWeight.w800,
                ),
              ),
              if (subtitle != null) ...[
                const SizedBox(height: 3),
                Text(subtitle!, style: TextStyle(color: t.muted, fontSize: 12)),
              ],
            ],
          ),
        ),
        ?trailing,
      ],
    );
  }
}

class AgendaEmptyState extends StatelessWidget {
  const AgendaEmptyState({
    super.key,
    required this.icon,
    required this.title,
    required this.message,
    this.actionLabel,
    this.onAction,
    this.compact = false,
  });

  final IconData icon;
  final String title;
  final String message;
  final String? actionLabel;
  final VoidCallback? onAction;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return LayoutBuilder(
      builder: (context, constraints) {
        // The schedule board keeps its time gutter on very small phones. In
        // that slot the empty state can be narrower than 160 px, so reduce its
        // chrome instead of letting the copy and CTA overflow vertically.
        final short =
            constraints.hasBoundedHeight && constraints.maxHeight <= 260;
        final narrow = compact && (constraints.maxWidth < 240 || short);
        final badgeSize = narrow ? 38.0 : (compact ? 46.0 : 58.0);
        return Center(
          child: Padding(
            padding: EdgeInsets.symmetric(
              horizontal: narrow ? 10 : 20,
              vertical: narrow ? 8 : (compact ? 14 : 28),
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                AgendaIconBadge(
                  icon,
                  size: badgeSize,
                  iconSize: narrow ? 19 : (compact ? 22 : 28),
                ),
                SizedBox(height: narrow ? 8 : 13),
                Text(
                  title,
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: narrow ? 14 : (compact ? 15 : 17),
                    height: narrow ? 1.15 : null,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                SizedBox(height: narrow ? 4 : 5),
                ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 430),
                  child: Text(
                    message,
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: t.muted,
                      fontSize: narrow ? 11.5 : 12.5,
                      height: narrow ? 1.25 : 1.35,
                    ),
                  ),
                ),
                if (actionLabel != null && onAction != null) ...[
                  SizedBox(height: narrow ? 10 : 15),
                  SizedBox(
                    width: narrow ? double.infinity : null,
                    child: ElevatedButton(
                      onPressed: onAction,
                      style: narrow
                          ? ElevatedButton.styleFrom(
                              minimumSize: const Size(0, 38),
                              padding: const EdgeInsets.symmetric(
                                horizontal: 9,
                              ),
                              textStyle: const TextStyle(
                                fontFamily: 'Segoe UI',
                                fontSize: 12,
                                fontWeight: FontWeight.w600,
                              ),
                            )
                          : null,
                      child: FittedBox(
                        fit: BoxFit.scaleDown,
                        child: Text(actionLabel!, maxLines: 1),
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
        );
      },
    );
  }
}

class AgendaResponsiveGrid extends StatelessWidget {
  const AgendaResponsiveGrid({
    super.key,
    required this.children,
    this.minItemWidth = 220,
    this.spacing = 10,
    this.maxColumns = 4,
    this.childAspectRatio,
    this.equalRowHeights = false,
  });

  final List<Widget> children;
  final double minItemWidth;
  final double spacing;
  final int maxColumns;
  final double? childAspectRatio;
  final bool equalRowHeights;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final calculated =
            ((constraints.maxWidth + spacing) / (minItemWidth + spacing))
                .floor();
        final columns = calculated.clamp(1, maxColumns);
        if (childAspectRatio != null) {
          return GridView.count(
            crossAxisCount: columns,
            mainAxisSpacing: spacing,
            crossAxisSpacing: spacing,
            childAspectRatio: childAspectRatio!,
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            children: children,
          );
        }
        if (equalRowHeights && columns > 1) {
          final rows = <Widget>[];
          for (var start = 0; start < children.length; start += columns) {
            final proposedEnd = start + columns;
            final end = proposedEnd < children.length
                ? proposedEnd
                : children.length;
            final rowChildren = children.sublist(start, end);
            rows.add(
              IntrinsicHeight(
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    for (var index = 0; index < columns; index++) ...[
                      if (index > 0) SizedBox(width: spacing),
                      Expanded(
                        child: index < rowChildren.length
                            ? rowChildren[index]
                            : const SizedBox.shrink(),
                      ),
                    ],
                  ],
                ),
              ),
            );
            if (end < children.length) {
              rows.add(SizedBox(height: spacing));
            }
          }
          return Column(children: rows);
        }
        return Wrap(
          spacing: spacing,
          runSpacing: spacing,
          children: [
            for (final child in children)
              SizedBox(
                width:
                    (constraints.maxWidth - spacing * (columns - 1)) / columns,
                child: child,
              ),
          ],
        );
      },
    );
  }
}
