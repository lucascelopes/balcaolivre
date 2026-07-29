import 'dart:ui';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/responsive_shell.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/pdv/pdv_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() => initializeDateFormatting('pt_BR'));

  testWidgets('PDV desktop replica a operação WPF e expõe os painéis', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(1366, 720);
    addTearDown(tester.view.reset);
    final controller = _pdvController();

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('').toThemeData(),
        home: PdvPage(
          controller: controller,
          referenceNow: _referenceDate,
          onExit: () {},
          onNavigate: (_) {},
        ),
      ),
    );
    await tester.pump();

    expect(find.byKey(const Key('pdv-desktop')), findsOneWidget);
    expect(find.byKey(const Key('pdv-topbar')), findsOneWidget);
    expect(find.byKey(const Key('pdv-navigation-rail')), findsOneWidget);
    expect(find.byKey(const Key('pdv-active-ribbon')), findsOneWidget);
    expect(find.byKey(const Key('pdv-appointment-a-running')), findsOneWidget);
    expect(tester.takeException(), isNull);

    await tester.tap(find.byKey(const Key('pdv-appointment-a-running')));
    await tester.pump();
    expect(find.byKey(const Key('pdv-panel-details')), findsOneWidget);

    await tester.tap(find.byKey(const Key('pdv-action-products')));
    await tester.pump();
    expect(find.byKey(const Key('pdv-panel-products')), findsOneWidget);

    final timerAction = find.byKey(const Key('pdv-action-timer'));
    final before = _animatedSurfaceColor(tester, timerAction);
    final mouse = await tester.createGesture(kind: PointerDeviceKind.mouse);
    await mouse.addPointer(location: tester.getCenter(timerAction));
    await mouse.moveTo(tester.getCenter(timerAction));
    await tester.pump(const Duration(milliseconds: 180));
    final after = _animatedSurfaceColor(tester, timerAction);
    expect(after, isNot(before));
    await mouse.removePointer();

    await tester.tap(find.byKey(const Key('pdv-view-semana')));
    await tester.pump();
    expect(find.byKey(const Key('pdv-view-semana')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('PDV mobile mantém agenda e ações sem transbordar', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.reset);
    final controller = _pdvController();

    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('').toThemeData(),
        home: PdvPage(
          controller: controller,
          referenceNow: _referenceDate,
          onExit: () {},
          onNavigate: (_) {},
        ),
      ),
    );
    await tester.pump();

    expect(find.byKey(const Key('pdv-mobile')), findsOneWidget);
    expect(find.byKey(const Key('pdv-mobile-agenda-list')), findsOneWidget);
    expect(
      find.byKey(const Key('pdv-mobile-appointment-a-running')),
      findsOneWidget,
    );
    expect(find.byKey(const Key('pdv-mobile-actions')), findsOneWidget);
    expect(tester.takeException(), isNull);

    await tester.tap(find.byKey(const Key('pdv-mobile-appointment-a-running')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('pdv-mobile-action-details')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('pdv-panel-details')), findsOneWidget);
    expect(tester.takeException(), isNull);

    await tester.tap(find.byTooltip('Fechar'));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('pdv-panel-details')), findsNothing);
  });

  testWidgets('shell entra e encerra o PDV no desktop e no mobile', (
    tester,
  ) async {
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    final controller = _pdvController();

    tester.view.physicalSize = const Size(1366, 720);
    await tester.pumpWidget(
      MaterialApp(
        theme: AgendaThemes.byId('').toThemeData(),
        home: ResponsiveAgendaShell(
          controller: controller,
          referenceNow: _referenceDate,
        ),
      ),
    );
    await tester.pump();

    await tester.tap(find.byKey(const Key('desktop-enter-pdv')));
    await tester.pump();
    expect(find.byKey(const Key('pdv-desktop')), findsOneWidget);
    controller.data.appointments.first.status = AppointmentStatus.done;
    await tester.tap(find.text('Encerrar PDV'));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('pdv-cash-closing-dialog')), findsOneWidget);
    await tester.tap(find.byKey(const Key('pdv-cash-closing-confirm')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('desktop-topbar')), findsOneWidget);

    tester.view.physicalSize = const Size(390, 844);
    await tester.pumpAndSettle();
    await tester.tap(find.byTooltip('Menu'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('mobile-enter-pdv')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('pdv-cash-opening-dialog')), findsOneWidget);
    await tester.tap(find.byKey(const Key('pdv-cash-opening-skip')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('pdv-mobile')), findsOneWidget);
    await tester.tap(find.byKey(const Key('pdv-mobile-exit')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('pdv-cash-closing-dialog')), findsOneWidget);
    await tester.drag(
      find.byKey(const Key('pdv-cash-closing-dialog')),
      const Offset(0, -700),
    );
    await tester.pumpAndSettle();
    await tester.tap(
      find.byKey(const Key('pdv-cash-closing-confirm')).hitTestable(),
    );
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('mobile-quick-navigation')), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'navegação lateral preserva o PDV e Início retorna à sessão aberta',
    (tester) async {
      tester.view.devicePixelRatio = 1;
      tester.view.physicalSize = const Size(1366, 720);
      addTearDown(tester.view.reset);
      final controller = _pdvController();

      await tester.pumpWidget(
        MaterialApp(
          theme: AgendaThemes.byId('').toThemeData(),
          home: ResponsiveAgendaShell(
            controller: controller,
            referenceNow: _referenceDate,
          ),
        ),
      );
      await tester.pump();

      await tester.tap(find.byKey(const Key('desktop-enter-pdv')));
      await tester.pump();
      await tester.tap(find.byKey(const Key('pdv-appointment-a-running')));
      await tester.pump();
      await tester.tap(find.byKey(const Key('pdv-action-products')));
      await tester.pump();
      expect(find.byKey(const Key('pdv-panel-products')), findsOneWidget);

      await tester.tap(find.byTooltip('Financeiro'));
      await tester.pumpAndSettle();
      expect(controller.page, AgendaPage.finance);
      expect(find.byKey(const Key('desktop-topbar')), findsOneWidget);
      expect(find.text('Voltar ao PDV'), findsOneWidget);

      await tester.tap(find.byKey(const Key('sidebar-destination-home')));
      await tester.pumpAndSettle();
      expect(controller.page, AgendaPage.home);
      expect(find.byKey(const Key('pdv-desktop')), findsOneWidget);
      expect(
        find.byKey(const Key('pdv-panel-products')),
        findsOneWidget,
        reason: 'O painel e o atendimento do PDV devem continuar montados.',
      );

      await tester.tap(find.byTooltip('Início'));
      await tester.pump();
      expect(find.byKey(const Key('pdv-desktop')), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
}

Color? _animatedSurfaceColor(WidgetTester tester, Finder ancestor) {
  final animated = tester.widget<AnimatedContainer>(
    find
        .descendant(of: ancestor, matching: find.byType(AnimatedContainer))
        .first,
  );
  return (animated.decoration as BoxDecoration?)?.color;
}

final _referenceDate = DateTime(2026, 7, 14, 10);

AgendaController _pdvController() {
  final controller = AgendaController(_MemoryAgendaRepository())
    ..data = AgendaData(
      settings: AgendaSettings(
        accountFullName: 'Lucas Cesar Lopes',
        businessName: 'Lucas Barbearia',
        businessSegment: 'Centro de Estética',
        onboardingCompleted: true,
        workdayStartHour: 8,
        workdayEndHour: 20,
        resources: const ['Cadeira 1', 'Cadeira 2'],
      ),
      services: [
        ServiceItem(
          id: 'service-1',
          name: 'Corte premium',
          segment: 'Barbearia',
          durationMinutes: 45,
          price: 65,
        ),
      ],
      professionals: [
        Professional(
          id: 'professional-1',
          name: 'Lucas',
          role: 'Barbeiro',
          segments: const ['Barbearia'],
        ),
        Professional(
          id: 'professional-2',
          name: 'Marcos',
          role: 'Barbeiro',
          segments: const ['Barbearia'],
        ),
      ],
      appointments: [
        Appointment(
          id: 'a-running',
          segment: 'Barbearia',
          customerName: 'Rafael Martins',
          customerPhone: '(11) 99999-1234',
          serviceId: 'service-1',
          serviceName: 'Corte premium',
          professionalId: 'professional-1',
          professionalName: 'Lucas',
          resourceName: 'Cadeira 1',
          start: DateTime(2026, 7, 14, 9, 30),
          durationMinutes: 45,
          price: 65,
          status: AppointmentStatus.inService,
          serviceStartedAt: DateTime(2026, 7, 14, 9, 35),
          serviceLines: [
            AppointmentServiceLine(
              serviceId: 'service-1',
              serviceName: 'Corte premium',
              segment: 'Barbearia',
              durationMinutes: 45,
              unitPrice: 65,
            ),
          ],
          productLines: [
            AppointmentProductLine(
              productId: 'product-1',
              productName: 'Pomada modeladora',
              quantity: 1,
              unitPrice: 35,
            ),
          ],
        ),
        Appointment(
          id: 'a-next',
          segment: 'Barbearia',
          customerName: 'Felipe Costa',
          serviceId: 'service-1',
          serviceName: 'Barba e acabamento',
          professionalId: 'professional-2',
          professionalName: 'Marcos',
          resourceName: 'Cadeira 2',
          start: DateTime(2026, 7, 14, 11),
          durationMinutes: 45,
          price: 50,
          status: AppointmentStatus.confirmed,
        ),
      ],
      products: [
        ProductItem(
          id: 'product-1',
          name: 'Pomada modeladora',
          category: 'Finalização',
          price: 35,
          stockQuantity: 8,
          minimumStock: 2,
        ),
      ],
      cashSessions: [
        CashSession(
          id: 'cash-open',
          operatorName: 'Lucas Cesar Lopes',
          openingBalance: 100,
          openedAt: DateTime(2026, 7, 14, 8),
        ),
      ],
    )
    ..loading = false
    ..selectedDate = DateUtils.dateOnly(_referenceDate);
  return controller;
}

class _MemoryAgendaRepository implements AgendaRepository {
  AgendaData? value;

  @override
  Future<void> clear() async => value = null;

  @override
  Future<bool> hasData() async => value != null;

  @override
  Future<AgendaData?> load() async => value;

  @override
  Future<AgendaData> loadOrCreate() async => value ?? AgendaData();

  @override
  Future<void> save(AgendaData data) async {
    value = AgendaData.fromJson(data.toJson());
  }
}
