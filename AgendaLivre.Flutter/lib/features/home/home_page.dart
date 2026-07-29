import 'package:flutter/material.dart';

import '../../app/agenda_controller.dart' as app;
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../core/ui.dart';
import '../../domain/models/models.dart';
import '../agenda/agenda_board.dart';
import '../agenda/appointment_dialog.dart';
import '../agenda/appointment_payment_dialog.dart';
import '../agenda/appointment_visuals.dart';

class HomePage extends StatelessWidget {
  const HomePage({
    super.key,
    required this.controller,
    this.onOpenAgenda,
    this.onNewAppointment,
    this.referenceNow,
  });

  final app.AgendaController controller;
  final VoidCallback? onOpenAgenda;
  final VoidCallback? onNewAppointment;
  final DateTime? referenceNow;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, _) {
        final now = referenceNow ?? DateTime.now();
        final targetDate = DateUtils.dateOnly(controller.selectedDate);
        final nextDate = targetDate.add(const Duration(days: 1));
        final items = controller.appointmentsBetween(targetDate, nextDate);
        return LayoutBuilder(
          builder: (context, constraints) {
            final compact = constraints.maxWidth < 720;
            return SingleChildScrollView(
              padding: EdgeInsets.fromLTRB(
                compact ? 16 : 28,
                compact ? 14 : 20,
                compact ? 16 : 36,
                compact ? 96 : 88,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _wpfHomeHero(context, now, targetDate, compact),
                  const SizedBox(height: 14),
                  _metrics(context, targetDate, nextDate, items),
                  const SizedBox(height: 14),
                  if (constraints.maxWidth >= 900)
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          flex: 175,
                          child: _scheduleCard(
                            context,
                            targetDate,
                            items,
                            false,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          flex: 105,
                          child: _rightColumn(context, targetDate, items),
                        ),
                      ],
                    )
                  else ...[
                    _scheduleCard(context, targetDate, items, compact),
                    const SizedBox(height: 12),
                    _rightColumn(context, targetDate, items),
                  ],
                ],
              ),
            );
          },
        );
      },
    );
  }

  Widget _wpfHomeHero(
    BuildContext context,
    DateTime now,
    DateTime targetDate,
    bool compact,
  ) {
    if (compact) return _homeHero(context, now, targetDate, true);
    final t = AgendaThemeTokens.of(context);
    final ownerName = controller.data.settings.accountFullName.trim().isEmpty
        ? 'Responsável'
        : controller.data.settings.accountFullName.trim();
    final ownerFirstName = ownerName
        .split(RegExp(r'\s+'))
        .firstWhere((part) => part.isNotEmpty, orElse: () => ownerName);
    final header = SizedBox(
      key: const Key('home-hero'),
      height: compact ? 124 : 138,
      child: Stack(
        clipBehavior: Clip.hardEdge,
        children: [
          Positioned(
            right: compact ? -70 : 0,
            top: compact ? 3 : -15,
            width: compact ? 250 : 360,
            height: 138,
            child: IgnorePointer(
              child: Opacity(
                opacity: .065,
                child: Image.asset(
                  'assets/branding/agenda-livre-logo-source.png',
                  fit: BoxFit.contain,
                  alignment: Alignment.centerRight,
                ),
              ),
            ),
          ),
          Align(
            alignment: Alignment.centerLeft,
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 660),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        'MINHA AGENDA',
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 10,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(width: 12),
                      Container(width: 44, height: 1, color: t.accent),
                    ],
                  ),
                  const SizedBox(height: 7),
                  Text(
                    '${_greeting(now)}, $ownerFirstName',
                    key: const Key('home-greeting'),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: compact ? 26 : 29,
                      fontWeight: FontWeight.w800,
                      height: 1.16,
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    fullDate(targetDate).toLowerCase(),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: t.muted, fontSize: 12.5),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
    if (!compact) return header;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        header,
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                onPressed: () => _openAgenda(targetDate),
                icon: const Icon(Icons.calendar_month_rounded, size: 18),
                label: const Text('Ver agenda'),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: ElevatedButton.icon(
                onPressed: () => _newAppointment(context, targetDate),
                icon: const Icon(Icons.add_rounded, size: 18),
                label: const Text('Agendar'),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _homeHero(
    BuildContext context,
    DateTime now,
    DateTime today,
    bool compact,
  ) {
    final t = AgendaThemeTokens.of(context);
    final businessName = controller.businessName.trim().isEmpty
        ? 'Balcão Livre'
        : controller.businessName.trim();
    final ownerName = controller.data.settings.accountFullName.trim().isEmpty
        ? 'Responsável não informado'
        : controller.data.settings.accountFullName.trim();
    return Container(
      key: const Key('home-hero'),
      // The WPF Border declares 148 DIP, but its real rendered card is 176 px
      // at the 1200x640 reference because the text stack drives the measure.
      constraints: BoxConstraints(minHeight: compact ? 0 : 176),
      padding: EdgeInsets.symmetric(
        horizontal: compact ? 18 : 24,
        vertical: 18,
      ),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(24),
      ),
      clipBehavior: Clip.antiAlias,
      child: Stack(
        children: [
          Positioned(
            right: compact ? -34 : 5,
            top: compact ? 42 : -18,
            width: compact ? 250 : 440,
            height: compact ? 118 : 138,
            child: IgnorePointer(
              child: Opacity(
                opacity: compact ? .035 : .065,
                child: Image.asset(
                  'assets/branding/agenda-livre-logo-source.png',
                  fit: BoxFit.cover,
                  alignment: const Alignment(0, -.55),
                ),
              ),
            ),
          ),
          Padding(
            padding: EdgeInsets.only(top: compact ? 0 : 8),
            child: ConstrainedBox(
              constraints: BoxConstraints(maxWidth: compact ? 600 : 640),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Container(
                        constraints: const BoxConstraints(minHeight: 22),
                        padding: const EdgeInsets.symmetric(horizontal: 9),
                        decoration: BoxDecoration(
                          color: t.accent,
                          borderRadius: BorderRadius.circular(9),
                        ),
                        alignment: Alignment.center,
                        child: Text(
                          'AGENDA LIVRE',
                          style: TextStyle(
                            color: Theme.of(context).colorScheme.onPrimary,
                            fontSize: 10,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Flexible(
                        child: Text(
                          fullDate(today).toLowerCase(),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(color: t.muted, fontSize: 11.5),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 7),
                  Text(
                    '${_greeting(now)}, $businessName',
                    key: const Key('home-greeting'),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: compact ? 28 : 31,
                      fontWeight: FontWeight.w600,
                      height: 1.12,
                    ),
                  ),
                  const SizedBox(height: 3),
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Flexible(
                        child: Text(
                          ownerName,
                          key: const Key('home-owner-name'),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: t.ink,
                            fontSize: 14,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                      const SizedBox(width: 6),
                      Icon(Icons.check_circle, color: t.ink, size: 15),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text.rich(
                    TextSpan(
                      children: [
                        TextSpan(
                          text: 'Sua agenda. Seu tempo. ',
                          style: TextStyle(color: t.ink),
                        ),
                        TextSpan(
                          text: 'Seu negócio.',
                          style: TextStyle(
                            color: t.ink,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                    style: const TextStyle(fontSize: 17),
                  ),
                  if (compact) ...[
                    const SizedBox(height: 16),
                    Row(
                      children: [
                        Expanded(
                          child: OutlinedButton.icon(
                            onPressed: () => _openAgenda(today),
                            icon: const Icon(
                              Icons.calendar_month_rounded,
                              size: 18,
                            ),
                            label: const Text('Ver agenda'),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: ElevatedButton.icon(
                            onPressed: () => _newAppointment(context, today),
                            icon: const Icon(Icons.add_rounded, size: 18),
                            label: const Text('Agendar'),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Align(
                      alignment: Alignment.centerLeft,
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 12,
                          vertical: 7,
                        ),
                        decoration: BoxDecoration(
                          color: const Color(0xFF171614),
                          borderRadius: BorderRadius.circular(15),
                        ),
                        child: const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(
                              Icons.event_available_outlined,
                              color: Colors.white,
                              size: 16,
                            ),
                            SizedBox(width: 7),
                            Flexible(
                              child: Text(
                                'Agenda organizada em tempo real',
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: TextStyle(
                                  fontFamily: 'Segoe UI',
                                  color: Colors.white,
                                  fontSize: 10.5,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
          if (!compact)
            Positioned(
              right: 0,
              bottom: 0,
              child: Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 14,
                  vertical: 8,
                ),
                decoration: BoxDecoration(
                  color: const Color(0xFF171614),
                  borderRadius: BorderRadius.circular(15),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      Icons.event_available_outlined,
                      color: Colors.white,
                      size: 18,
                    ),
                    const SizedBox(width: 8),
                    const Text(
                      'Agenda organizada em tempo real',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 11.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }

  Widget _metrics(
    BuildContext context,
    DateTime today,
    DateTime tomorrow,
    List<Appointment> items,
  ) {
    final confirmed = items
        .where(
          (item) => const {
            AppointmentStatus.confirmed,
            AppointmentStatus.waiting,
            AppointmentStatus.inService,
          }.contains(item.status),
        )
        .length;
    final pending = items
        .where((item) => item.status == AppointmentStatus.scheduled)
        .length;
    final finalized = items
        .where((item) => item.status == AppointmentStatus.done)
        .length;
    final confirmationRate = items.isEmpty
        ? 0
        : (confirmed / items.length * 100).round();
    final currentDate = DateUtils.dateOnly(DateTime.now());
    final dateSuffix = DateUtils.isSameDay(today, currentDate)
        ? 'hoje'
        : DateUtils.isSameDay(today, currentDate.add(const Duration(days: 1)))
        ? 'amanhã'
        : 'em ${shortDate(today)}';
    final isToday = DateUtils.isSameDay(today, currentDate);
    final forecast = items
        .where(
          (item) => !const {
            AppointmentStatus.cancelled,
            AppointmentStatus.noShow,
            AppointmentStatus.blocked,
          }.contains(item.status),
        )
        .fold<double>(0, (sum, item) => sum + item.price);
    final realized = controller.revenueBetween(today, tomorrow);
    final confirmationBase = confirmed + pending;
    final confirmedShare = confirmationBase == 0
        ? 0.0
        : confirmed / confirmationBase;
    final cashShare = forecast <= 0
        ? 0.0
        : (realized / forecast).clamp(0.0, 1.0);
    return AgendaDarkMetricStrip(
      key: const Key('home-metrics'),
      metrics: [
        AgendaDarkMetricData(
          label: 'Agendamentos $dateSuffix',
          value: '${items.length}',
          caption: '$confirmed confirmado${confirmed == 1 ? '' : '(s)'}',
          icon: Icons.calendar_month_rounded,
          tone: Colors.white,
        ),
        AgendaDarkMetricData(
          label: 'Confirmados',
          value: '$confirmed',
          caption: '$confirmationRate% do total',
          icon: Icons.check_circle_outline_rounded,
          tone: Colors.white,
        ),
        AgendaDarkMetricData(
          label: 'A confirmar',
          value: '$pending',
          caption: 'precisa de WhatsApp',
          icon: Icons.schedule_rounded,
          tone: Colors.white,
        ),
        AgendaDarkMetricData(
          label: isToday ? 'Caixa do dia' : 'Caixa previsto',
          value: money(
            isToday ? controller.revenueBetween(today, tomorrow) : forecast,
            cents: false,
          ),
          caption: '$finalized finalizado${finalized == 1 ? '' : '(s)'}',
          icon: Icons.account_balance_wallet_outlined,
          tone: Colors.white,
        ),
      ],
      footer: _homeMetricFooter(
        context,
        confirmed: confirmed,
        pending: pending,
        confirmedShare: confirmedShare,
        cashShare: cashShare,
      ),
    );
  }

  Widget _homeMetricFooter(
    BuildContext context, {
    required int confirmed,
    required int pending,
    required double confirmedShare,
    required double cashShare,
  }) {
    final t = AgendaThemeTokens.of(context);
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 760) return const SizedBox.shrink();
        return Padding(
          padding: const EdgeInsets.fromLTRB(18, 0, 18, 6),
          child: SizedBox(
            height: 45,
            child: Row(
              children: [
                Expanded(
                  flex: 3,
                  child: Padding(
                    padding: const EdgeInsets.only(right: 32),
                    child: Column(
                      children: [
                        ClipRRect(
                          borderRadius: BorderRadius.circular(7),
                          child: SizedBox(
                            height: 14,
                            child: Row(
                              children: [
                                Expanded(
                                  flex: (confirmedShare * 1000).round().clamp(
                                    1,
                                    999,
                                  ),
                                  child: ColoredBox(color: t.accent),
                                ),
                                Expanded(
                                  flex: ((1 - confirmedShare) * 1000)
                                      .round()
                                      .clamp(1, 999),
                                  child: const ColoredBox(
                                    color: Color(0xFFFAD8C2),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                        const SizedBox(height: 7),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Container(
                              width: 10,
                              height: 10,
                              decoration: BoxDecoration(
                                color: t.accent,
                                borderRadius: BorderRadius.circular(2),
                              ),
                            ),
                            const SizedBox(width: 7),
                            const Text(
                              'Confirmados',
                              style: TextStyle(
                                color: Color(0xFFD9D4CF),
                                fontSize: 10.5,
                              ),
                            ),
                            const SizedBox(width: 5),
                            Text(
                              '$confirmed',
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 10.5,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            const SizedBox(width: 22),
                            Container(
                              width: 10,
                              height: 10,
                              decoration: BoxDecoration(
                                color: const Color(0xFFFAD8C2),
                                borderRadius: BorderRadius.circular(2),
                              ),
                            ),
                            const SizedBox(width: 7),
                            const Text(
                              'A confirmar',
                              style: TextStyle(
                                color: Color(0xFFD9D4CF),
                                fontSize: 10.5,
                              ),
                            ),
                            const SizedBox(width: 5),
                            Text(
                              '$pending',
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 10.5,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
                const VerticalDivider(
                  width: 1,
                  thickness: 1,
                  color: Color(0x32FFFFFF),
                ),
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.only(left: 30, right: 10),
                    child: Column(
                      children: [
                        ClipRRect(
                          borderRadius: BorderRadius.circular(6),
                          child: LinearProgressIndicator(
                            value: cashShare,
                            minHeight: 12,
                            color: t.accent,
                            backgroundColor: const Color(0xFF353230),
                          ),
                        ),
                        const SizedBox(height: 7),
                        Text(
                          '${(cashShare * 100).round()}%',
                          style: const TextStyle(
                            color: Color(0xFFD9D4CF),
                            fontSize: 10.5,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _scheduleCard(
    BuildContext context,
    DateTime today,
    List<Appointment> items,
    bool compact,
  ) {
    final t = AgendaThemeTokens.of(context);
    final currentDate = DateUtils.dateOnly(DateTime.now());
    final scheduleTitle = DateUtils.isSameDay(today, currentDate)
        ? 'Hoje, ${_weekdayLong(today.weekday)}, ${shortDate(today)}'
        : '${_weekdayLong(today.weekday)}, ${shortDate(today)}';
    final weekStart = _startOfWeek(today);
    final weekDays = List<DateTime>.generate(
      7,
      (index) => weekStart.add(Duration(days: index)),
    );
    final toolbarButtonStyle = OutlinedButton.styleFrom(
      minimumSize: Size.zero,
      fixedSize: const Size.fromHeight(30),
      padding: const EdgeInsets.symmetric(horizontal: 12),
      textStyle: const TextStyle(
        fontFamily: 'Segoe UI',
        fontSize: 12.5,
        fontWeight: FontWeight.w600,
      ),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      side: BorderSide(color: t.line),
      foregroundColor: t.ink,
      backgroundColor: t.panel,
    );

    Widget navigation() => Container(
      width: 70,
      height: 32,
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        children: [
          Expanded(
            child: IconButton(
              key: const Key('home-previous-day'),
              tooltip: 'Período anterior',
              onPressed: () => controller.selectDate(
                today.subtract(const Duration(days: 1)),
              ),
              padding: EdgeInsets.zero,
              icon: const Icon(Icons.chevron_left_rounded, size: 17),
            ),
          ),
          Container(width: 1, height: 20, color: t.line),
          Expanded(
            child: IconButton(
              key: const Key('home-next-day'),
              tooltip: 'Próximo período',
              onPressed: () =>
                  controller.selectDate(today.add(const Duration(days: 1))),
              padding: EdgeInsets.zero,
              icon: const Icon(Icons.chevron_right_rounded, size: 17),
            ),
          ),
        ],
      ),
    );

    Widget todayButton() => OutlinedButton(
      key: const Key('home-today'),
      onPressed: () => controller.selectDate(currentDate),
      style: toolbarButtonStyle,
      child: const Text('Hoje'),
    );

    Widget modes() => Container(
      width: 212,
      height: 32,
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(16),
      ),
      clipBehavior: Clip.antiAlias,
      child: Row(
        children: [
          Expanded(
            child: Material(
              color: t.accentSoft,
              child: InkWell(
                onTap: () => _openAgenda(today),
                child: Center(
                  child: Text(
                    'Dia',
                    style: TextStyle(
                      color: t.accentDark,
                      fontSize: 12.5,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ),
            ),
          ),
          Expanded(
            child: InkWell(
              onTap: () => _openAgenda(today),
              child: Center(
                child: Text(
                  'Semana',
                  style: TextStyle(color: t.ink, fontSize: 12.5),
                ),
              ),
            ),
          ),
          Expanded(
            child: InkWell(
              onTap: () => _openAgenda(today),
              child: Center(
                child: Text(
                  'Mês',
                  style: TextStyle(color: t.ink, fontSize: 12.5),
                ),
              ),
            ),
          ),
        ],
      ),
    );

    return AgendaPanel(
      key: const Key('home-schedule-card'),
      radius: 16,
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (compact)
            Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    navigation(),
                    const SizedBox(width: 6),
                    todayButton(),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        scheduleTitle,
                        key: const Key('home-schedule-title'),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        textAlign: TextAlign.right,
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 15,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                Align(alignment: Alignment.centerRight, child: modes()),
              ],
            )
          else
            Row(
              children: [
                Expanded(
                  child: Row(
                    children: [
                      Container(
                        width: 34,
                        height: 34,
                        decoration: BoxDecoration(
                          color: t.accentSoft,
                          borderRadius: BorderRadius.circular(17),
                        ),
                        alignment: Alignment.center,
                        child: Icon(
                          Icons.calendar_month_rounded,
                          color: t.accentDark,
                          size: 17,
                        ),
                      ),
                      const SizedBox(width: 9),
                      Flexible(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              scheduleTitle,
                              key: const Key('home-schedule-title'),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: TextStyle(
                                color: t.ink,
                                fontSize: 15.5,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              'Agenda do dia',
                              style: TextStyle(color: t.muted, fontSize: 10.5),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                navigation(),
                const SizedBox(width: 6),
                todayButton(),
                const SizedBox(width: 8),
                modes(),
              ],
            ),
          const SizedBox(height: 10),
          LayoutBuilder(
            builder: (context, constraints) {
              final stripWidth = constraints.maxWidth;
              return ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: SizedBox(
                    width: stripWidth,
                    child: DecoratedBox(
                      decoration: BoxDecoration(
                        border: Border.all(color: t.line),
                      ),
                      child: Row(
                        children: [
                          for (var index = 0; index < weekDays.length; index++)
                            Expanded(
                              child: Material(
                                key: ValueKey<String>(
                                  'home-week-day-${weekDays[index].toIso8601String().substring(0, 10)}',
                                ),
                                color:
                                    DateUtils.isSameDay(weekDays[index], today)
                                    ? t.accentSoft
                                    : t.panel,
                                child: InkWell(
                                  onTap: () =>
                                      controller.selectDate(weekDays[index]),
                                  child: Container(
                                    padding: const EdgeInsets.symmetric(
                                      vertical: 6,
                                    ),
                                    decoration: BoxDecoration(
                                      border: index == weekDays.length - 1
                                          ? null
                                          : Border(
                                              right: BorderSide(color: t.line),
                                            ),
                                    ),
                                    child: Column(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        Text(
                                          _weekdayShort(
                                            weekDays[index].weekday,
                                          ),
                                          style: TextStyle(
                                            color:
                                                DateUtils.isSameDay(
                                                  weekDays[index],
                                                  today,
                                                )
                                                ? t.accentDark
                                                : t.ink,
                                            fontSize: compact ? 10.5 : 12,
                                            fontWeight:
                                                DateUtils.isSameDay(
                                                  weekDays[index],
                                                  today,
                                                )
                                                ? FontWeight.w700
                                                : FontWeight.w600,
                                          ),
                                        ),
                                        const SizedBox(height: 2),
                                        Text(
                                          shortDate(weekDays[index]),
                                          style: TextStyle(
                                            color:
                                                DateUtils.isSameDay(
                                                  weekDays[index],
                                                  today,
                                                )
                                                ? t.accentDark
                                                : t.muted,
                                            fontSize: compact ? 9 : 11,
                                            fontWeight:
                                                DateUtils.isSameDay(
                                                  weekDays[index],
                                                  today,
                                                )
                                                ? FontWeight.w700
                                                : FontWeight.normal,
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                ),
                              ),
                            ),
                        ],
                      ),
                    ),
                  ),
                ),
              );
            },
          ),
          const SizedBox(height: 10),
          AgendaScheduleBoard(
            date: today,
            appointments: items
                .where(
                  (item) => !const {
                    AppointmentStatus.cancelled,
                    AppointmentStatus.noShow,
                    AppointmentStatus.blocked,
                  }.contains(item.status),
                )
                .toList(),
            professionals: controller.activeProfessionals,
            settings: controller.data.settings,
            compact: compact,
            height: compact ? 350 : 320,
            slotMinutes: 60,
            rowHeight: compact ? 42 : 48,
            timeColumnWidth: compact ? 58 : 72,
            headerHeight: compact ? 46 : 44,
            radius: 10,
            emptyTitle: 'Nenhum atendimento hoje',
            emptyMessage:
                'A agenda está livre. Crie o primeiro horário ou clique diretamente na grade.',
            emptyActionLabel: '+ Agendar atendimento',
            onAppointmentTap: (item) => _openPayment(context, item),
            onEmptySlotTap: (start, _) => _newAppointment(context, start),
            onCreate: () => _newAppointment(context, today),
          ),
        ],
      ),
    );
  }

  Future<void> _openPayment(BuildContext context, Appointment appointment) {
    return showAppointmentPaymentDialog(
      context,
      controller,
      appointment,
      onEdit: () {
        showAppointmentDialog(context, controller, appointment: appointment);
      },
    );
  }

  Widget _rightColumn(
    BuildContext context,
    DateTime today,
    List<Appointment> items,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _occupancyCard(context, today, items),
        const SizedBox(height: 10),
        _weeklyPerformanceCard(context, today),
      ],
    );
  }

  Widget _occupancyCard(
    BuildContext context,
    DateTime today,
    List<Appointment> items,
  ) {
    final t = AgendaThemeTokens.of(context);
    final activeItems = items
        .where(
          (item) => !const {
            AppointmentStatus.cancelled,
            AppointmentStatus.noShow,
            AppointmentStatus.blocked,
          }.contains(item.status),
        )
        .toList();
    final settings = controller.data.settings;
    final professionals = controller.activeProfessionals.length.clamp(1, 999);
    var workMinutes =
        (settings.workdayEndHour - settings.workdayStartHour) * 60;
    if (settings.workdayBreakEnabled) {
      workMinutes -=
          (settings.workdayBreakEndHour - settings.workdayBreakStartHour) * 60;
    }
    final capacityMinutes = (workMinutes.clamp(30, 24 * 60)) * professionals;
    final occupiedMinutes = activeItems.fold<int>(
      0,
      (sum, item) => sum + item.durationMinutes.clamp(15, 24 * 60),
    );
    final occupancy = capacityMinutes == 0
        ? 0.0
        : (occupiedMinutes * 100 / capacityMinutes).clamp(0, 100).toDouble();
    final slots = _availableSlots(today, items, limit: 99);
    final nextTime = slots.isEmpty ? '--:--' : hour(slots.first.start);

    Widget stat(String value, String label) => Expanded(
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 7),
        decoration: BoxDecoration(
          color: t.appBackground,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              value,
              style: TextStyle(
                color: t.ink,
                fontWeight: FontWeight.w700,
                fontSize: 13,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              textAlign: TextAlign.center,
              style: TextStyle(color: t.muted, fontSize: 9.5),
            ),
          ],
        ),
      ),
    );

    return AgendaPanel(
      key: const Key('home-occupancy-card'),
      radius: 16,
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(
                Icons.event_available_outlined,
                color: t.accentDark,
                size: 20,
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Ocupação de hoje',
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      'Capacidade da sua agenda',
                      style: TextStyle(color: t.muted, fontSize: 12),
                    ),
                  ],
                ),
              ),
              OutlinedButton(
                onPressed: () => _openAgenda(today),
                style: OutlinedButton.styleFrom(
                  minimumSize: const Size(84, 30),
                  padding: const EdgeInsets.symmetric(horizontal: 10),
                ),
                child: const Text('Ver agenda'),
              ),
            ],
          ),
          const SizedBox(height: 11),
          Row(
            children: [
              Padding(
                padding: const EdgeInsets.only(right: 14),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '${occupancy.toStringAsFixed(0)}%',
                      key: const Key('home-occupancy-percent'),
                      style: TextStyle(
                        color: t.accentDark,
                        fontSize: 27,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    Text(
                      'preenchida',
                      style: TextStyle(color: t.muted, fontSize: 10.5),
                    ),
                  ],
                ),
              ),
              Expanded(
                child: LinearProgressIndicator(
                  value: occupancy / 100,
                  minHeight: 9,
                  borderRadius: BorderRadius.circular(8),
                  color: t.accent,
                  backgroundColor: t.accentSoft,
                ),
              ),
            ],
          ),
          const SizedBox(height: 9),
          Row(
            children: [
              stat('${activeItems.length}', 'agendados'),
              const SizedBox(width: 6),
              stat('${slots.length}', 'horários livres'),
              const SizedBox(width: 6),
              stat(slots.isEmpty ? '0' : '1', 'encaixes'),
            ],
          ),
          const SizedBox(height: 9),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
            decoration: BoxDecoration(
              color: t.accentSoft,
              border: Border.all(color: t.line),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Row(
              children: [
                Icon(Icons.schedule_outlined, color: t.accentDark, size: 17),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Próximo horário livre',
                        style: TextStyle(color: t.muted, fontSize: 9.5),
                      ),
                      const SizedBox(height: 1),
                      Text(
                        nextTime,
                        key: const Key('home-next-free-time'),
                        style: TextStyle(
                          color: t.accentDark,
                          fontWeight: FontWeight.w700,
                          fontSize: 13,
                        ),
                      ),
                    ],
                  ),
                ),
                SizedBox(
                  height: 31,
                  child: ElevatedButton(
                    onPressed: () => _newAppointment(
                      context,
                      slots.isEmpty ? today : slots.first.start,
                    ),
                    style: ElevatedButton.styleFrom(
                      minimumSize: const Size(72, 31),
                      padding: const EdgeInsets.symmetric(horizontal: 10),
                      textStyle: const TextStyle(
                        fontFamily: 'Segoe UI',
                        fontSize: 11.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    child: const Text('Agendar'),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _weeklyPerformanceCard(BuildContext context, DateTime targetDate) {
    final t = AgendaThemeTokens.of(context);
    final weekStart = _startOfWeek(targetDate);
    final weekEnd = weekStart.add(const Duration(days: 7));
    final previousStart = weekStart.subtract(const Duration(days: 7));
    final revenue = controller.revenueBetween(weekStart, weekEnd);
    final previousRevenue = controller.revenueBetween(previousStart, weekStart);
    final appointments = controller.data.appointments
        .where(
          (item) =>
              !item.start.isBefore(weekStart) &&
              item.start.isBefore(weekEnd) &&
              !const {
                AppointmentStatus.cancelled,
                AppointmentStatus.noShow,
                AppointmentStatus.blocked,
              }.contains(item.status),
        )
        .toList();
    final ticket = appointments.isEmpty ? 0.0 : revenue / appointments.length;
    final trend = previousRevenue <= 0
        ? 0.0
        : (revenue - previousRevenue) / previousRevenue * 100;
    final trendColor = trend < 0
        ? const Color(0xFFBE123C)
        : const Color(0xFF15803D);
    final trendBackground = trend < 0
        ? const Color(0xFFFFF1F2)
        : const Color(0xFFEAFBF2);
    final trendLabel = trend == 0
        ? 'Semana atual'
        : '${trend > 0 ? '+' : ''}${trend.toStringAsFixed(0)}% '
              '${trend > 0 ? '↑' : '↓'}';
    final dailyRevenue = List<double>.generate(
      7,
      (index) => controller.revenueBetween(
        weekStart.add(Duration(days: index)),
        weekStart.add(Duration(days: index + 1)),
      ),
    );
    final maxRevenue = dailyRevenue.fold<double>(
      1,
      (current, value) => value > current ? value : current,
    );
    final serviceCounts = <String, int>{};
    for (final appointment in appointments) {
      final service = appointment.serviceName.trim();
      if (service.isNotEmpty) {
        serviceCounts.update(service, (value) => value + 1, ifAbsent: () => 1);
      }
    }
    final topService = serviceCounts.entries.isEmpty
        ? null
        : (serviceCounts.entries.toList()..sort((a, b) {
                final byCount = b.value.compareTo(a.value);
                return byCount != 0 ? byCount : a.key.compareTo(b.key);
              }))
              .first;
    final topServiceTitle = topService == null
        ? 'Sem serviços na semana'
        : '${topService.key} foi o serviço mais vendido';
    final topServiceCaption = topService == null
        ? 'O destaque aparecerá aqui.'
        : 'Representou '
              '${(topService.value * 100 / appointments.length).floor()}% '
              'dos atendimentos da semana.';

    return AgendaPanel(
      key: const Key('home-week-performance-card'),
      radius: 16,
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(Icons.bar_chart_rounded, color: t.accentDark, size: 20),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  'Desempenho da semana',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Faturamento total',
                      style: TextStyle(color: t.muted, fontSize: 11),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      money(revenue, cents: false),
                      key: const Key('home-week-revenue'),
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 23,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 6),
                decoration: BoxDecoration(
                  color: trendBackground,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  trendLabel,
                  style: TextStyle(
                    color: trendColor,
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          SizedBox(
            height: 90,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                for (var index = 0; index < dailyRevenue.length; index++)
                  Expanded(
                    child: Align(
                      alignment: Alignment.bottomCenter,
                      child: AnimatedContainer(
                        duration: const Duration(milliseconds: 220),
                        curve: Curves.easeOutCubic,
                        width: 21,
                        height: 8 + (dailyRevenue[index] / maxRevenue * 72),
                        decoration: BoxDecoration(
                          color:
                              DateUtils.isSameDay(
                                weekStart.add(Duration(days: index)),
                                targetDate,
                              )
                              ? t.accent
                              : const Color(0xFFF8D7C4),
                          borderRadius: const BorderRadius.vertical(
                            top: Radius.circular(5),
                            bottom: Radius.circular(2),
                          ),
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(height: 5),
          Row(
            children: [
              for (var index = 0; index < 7; index++)
                Expanded(
                  child: Text(
                    _weekdayShort(index + 1),
                    textAlign: TextAlign.center,
                    style: TextStyle(color: t.muted, fontSize: 9.5),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 13),
          Row(
            children: [
              Expanded(
                child: _performanceStat(
                  context,
                  '${appointments.length}',
                  'atendimentos',
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _performanceStat(
                  context,
                  money(ticket, cents: false),
                  'ticket médio',
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.all(11),
            decoration: BoxDecoration(
              color: t.accentSoft,
              border: Border.all(color: t.line),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(Icons.star_outline_rounded, color: t.accentDark, size: 18),
                const SizedBox(width: 9),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        topServiceTitle,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: t.accentDark,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        topServiceCaption,
                        style: TextStyle(color: t.muted, fontSize: 10.5),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          Align(
            alignment: Alignment.centerLeft,
            child: OutlinedButton.icon(
              onPressed: () => controller.navigate(app.AgendaPage.finance),
              icon: const Icon(Icons.chevron_right_rounded, size: 17),
              label: const Text('Abrir financeiro'),
              style: OutlinedButton.styleFrom(
                foregroundColor: t.accentDark,
                minimumSize: const Size(0, 40),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _performanceStat(BuildContext context, String value, String label) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: t.appBackground,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            value,
            style: TextStyle(
              color: t.ink,
              fontSize: 18,
              fontWeight: FontWeight.w700,
            ),
          ),
          Text(label, style: TextStyle(color: t.muted, fontSize: 10.5)),
        ],
      ),
    );
  }

  List<_AvailableSlot> _availableSlots(
    DateTime today,
    List<Appointment> items, {
    required int limit,
  }) {
    final settings = controller.data.settings;
    final now = DateTime.now();
    final professionals = controller.activeProfessionals;
    final professionalIds = professionals.isEmpty
        ? const <String>['']
        : professionals.map((item) => item.id).toList();
    final result = <_AvailableSlot>[];
    for (
      var minutes = settings.workdayStartHour * 60;
      minutes + 30 <= settings.workdayEndHour * 60;
      minutes += 30
    ) {
      final start = DateTime(
        today.year,
        today.month,
        today.day,
        minutes ~/ 60,
        minutes % 60,
      );
      if (start.isBefore(now)) continue;
      final end = start.add(const Duration(minutes: 30));
      if (controller.validateBusinessWindow(start, end) != null) continue;
      for (final professionalId in professionalIds) {
        final conflict = items.any((item) {
          if (const {
            AppointmentStatus.cancelled,
            AppointmentStatus.noShow,
          }.contains(item.status)) {
            return false;
          }
          if (professionalId.isNotEmpty &&
              item.professionalId != professionalId) {
            return false;
          }
          return start.isBefore(item.end) && end.isAfter(item.start);
        });
        if (!conflict) {
          result.add(_AvailableSlot(start: start));
          if (result.length == limit) return result;
          break;
        }
      }
    }
    return result;
  }

  void _openAgenda(DateTime date) {
    controller.selectDate(date);
    if (onOpenAgenda != null) {
      onOpenAgenda!();
    } else {
      controller.navigate(app.AgendaPage.agenda);
    }
  }

  Future<void> _newAppointment(BuildContext context, DateTime initial) async {
    if (onNewAppointment != null) {
      onNewAppointment!();
      return;
    }
    var start = initial;
    if (DateUtils.isSameDay(initial, DateTime.now()) &&
        !initial.isAfter(DateTime.now())) {
      final now = DateTime.now();
      final roundedMinutes = ((now.minute + 29) ~/ 30 * 30);
      start = DateTime(
        now.year,
        now.month,
        now.day,
        now.hour + roundedMinutes ~/ 60,
        roundedMinutes % 60,
      );
    }
    await showAppointmentDialog(context, controller, initialStart: start);
  }
}

class _AvailableSlot {
  const _AvailableSlot({required this.start});

  final DateTime start;
}

String _greeting(DateTime now) {
  if (now.hour < 12) return 'Bom dia';
  if (now.hour < 18) return 'Boa tarde';
  return 'Boa noite';
}

DateTime _startOfWeek(DateTime value) => DateUtils.dateOnly(
  value.subtract(Duration(days: value.weekday - DateTime.monday)),
);

String _weekdayShort(int weekday) => const <String>[
  'Seg',
  'Ter',
  'Qua',
  'Qui',
  'Sex',
  'Sáb',
  'Dom',
][weekday - 1];

String _weekdayLong(int weekday) => const <String>[
  'segunda-feira',
  'terça-feira',
  'quarta-feira',
  'quinta-feira',
  'sexta-feira',
  'sábado',
  'domingo',
][weekday - 1];
