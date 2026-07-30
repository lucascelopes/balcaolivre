import 'package:flutter/material.dart';

abstract final class AgendaMotion {
  static const fast = Duration(milliseconds: 140);
  static const standard = Duration(milliseconds: 220);
  static const page = Duration(milliseconds: 300);
  static const emphasized = Duration(milliseconds: 420);

  static bool reduced(BuildContext context) {
    final media = MediaQuery.maybeOf(context);
    return media?.disableAnimations == true ||
        media?.accessibleNavigation == true;
  }

  static Duration duration(BuildContext context, Duration value) =>
      reduced(context) ? Duration.zero : value;

  static Curve get enterCurve => Curves.easeOutCubic;
  static Curve get exitCurve => Curves.easeInCubic;
}

/// Gentle page/section entrance that respects the platform reduced-motion
/// preference. The same element does not replay when its parent rebuilds.
class AgendaReveal extends StatelessWidget {
  const AgendaReveal({
    super.key,
    required this.child,
    this.delay = Duration.zero,
    this.offset = const Offset(0, 10),
    this.duration = AgendaMotion.standard,
  });

  final Widget child;
  final Duration delay;
  final Offset offset;
  final Duration duration;

  @override
  Widget build(BuildContext context) {
    final reduced = AgendaMotion.reduced(context);
    if (reduced) return child;
    final total = duration + delay;
    return TweenAnimationBuilder<double>(
      tween: Tween<double>(begin: 0, end: 1),
      duration: total,
      curve: Curves.linear,
      child: child,
      builder: (context, rawValue, child) {
        final delayFraction = total.inMicroseconds == 0
            ? 0.0
            : delay.inMicroseconds / total.inMicroseconds;
        final progress = rawValue <= delayFraction
            ? 0.0
            : ((rawValue - delayFraction) / (1 - delayFraction))
                  .clamp(0, 1)
                  .toDouble();
        final eased = AgendaMotion.enterCurve.transform(progress);
        return Opacity(
          opacity: eased,
          child: Transform.translate(
            offset: Offset(offset.dx * (1 - eased), offset.dy * (1 - eased)),
            child: child,
          ),
        );
      },
    );
  }
}

/// Animates a changed metric without turning the whole card into motion.
class AgendaAnimatedValue extends StatelessWidget {
  const AgendaAnimatedValue({
    super.key,
    required this.value,
    required this.builder,
    this.alignment = Alignment.centerLeft,
  });

  final String value;
  final Widget Function(BuildContext context, String value) builder;
  final AlignmentGeometry alignment;

  @override
  Widget build(BuildContext context) {
    return AnimatedSwitcher(
      duration: AgendaMotion.duration(context, AgendaMotion.standard),
      reverseDuration: AgendaMotion.duration(context, AgendaMotion.fast),
      switchInCurve: AgendaMotion.enterCurve,
      switchOutCurve: AgendaMotion.exitCurve,
      layoutBuilder: (currentChild, previousChildren) => Stack(
        alignment: alignment,
        children: [...previousChildren, ?currentChild],
      ),
      transitionBuilder: (child, animation) {
        final slide = Tween<Offset>(
          begin: const Offset(0, .18),
          end: Offset.zero,
        ).animate(animation);
        return FadeTransition(
          opacity: animation,
          child: SlideTransition(position: slide, child: child),
        );
      },
      child: KeyedSubtree(
        key: ValueKey<String>(value),
        child: builder(context, value),
      ),
    );
  }
}
