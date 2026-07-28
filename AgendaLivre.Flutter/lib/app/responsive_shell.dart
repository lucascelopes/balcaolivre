import 'dart:async';

import 'package:flutter/material.dart';

import '../core/ui.dart';
import '../features/agenda/agenda_page.dart' as agenda_ui;
import '../features/agenda/appointment_dialog.dart';
import '../features/establishment/establishment_page.dart';
import '../features/finance/finance_page.dart';
import '../features/home/home_page.dart';
import '../features/marketing/marketing_page.dart';
import '../features/pdv/pdv_page.dart';
import '../features/reports/reports_page.dart';
import '../features/settings/settings_page.dart';
import '../features/whatsapp/whatsapp_panel.dart';
import 'agenda_controller.dart';
import 'theme/agenda_theme.dart';

String initials(String value) {
  final parts = value
      .trim()
      .split(RegExp(r'\s+'))
      .where((part) => part.isNotEmpty)
      .toList(growable: false);
  if (parts.isEmpty) return 'AL';
  if (parts.length == 1) {
    final word = parts.first;
    return word.substring(0, word.length.clamp(1, 2)).toUpperCase();
  }
  return '${parts.first[0]}${parts.last[0]}'.toUpperCase();
}

class ResponsiveAgendaShell extends StatefulWidget {
  const ResponsiveAgendaShell({
    super.key,
    required this.controller,
    this.referenceNow,
  });

  final AgendaController controller;
  final DateTime? referenceNow;

  @override
  State<ResponsiveAgendaShell> createState() => _ResponsiveAgendaShellState();
}

class _ResponsiveAgendaShellState extends State<ResponsiveAgendaShell> {
  final _scaffoldKey = GlobalKey<ScaffoldState>();
  late final TextEditingController _searchController;
  Timer? _searchDebounce;
  bool? _sidebarCollapsed;
  bool _pdvMode = false;
  bool _pdvSessionActive = false;
  late AgendaPage _lastObservedPage;

  AgendaController get controller => widget.controller;

  @override
  void initState() {
    super.initState();
    _searchController = TextEditingController(text: controller.searchQuery);
    _lastObservedPage = controller.page;
    controller.addListener(_handleControllerChange);
  }

  @override
  void didUpdateWidget(covariant ResponsiveAgendaShell oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.controller == controller) return;
    oldWidget.controller.removeListener(_handleControllerChange);
    _pdvMode = false;
    _pdvSessionActive = false;
    _lastObservedPage = controller.page;
    controller.addListener(_handleControllerChange);
  }

  @override
  void dispose() {
    controller.removeListener(_handleControllerChange);
    _searchDebounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  void _handleControllerChange() {
    final nextPage = controller.page;
    final openedSettings =
        _lastObservedPage != AgendaPage.settings &&
        nextPage == AgendaPage.settings;
    _lastObservedPage = nextPage;
    if (openedSettings && mounted) {
      ScaffoldMessenger.maybeOf(context)?.removeCurrentSnackBar();
    }
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final standardShell = constraints.maxWidth < 900
            ? _mobileShell()
            : _desktopShell(compactSidebar: constraints.maxWidth < 1100);
        return IndexedStack(
          key: const Key('agenda-shell-mode-stack'),
          index: _pdvMode ? 1 : 0,
          children: [
            standardShell,
            _pdvSessionActive
                ? PdvPage(
                    controller: controller,
                    referenceNow: widget.referenceNow,
                    onExit: _exitPdv,
                    onNavigate: _navigateFromPdv,
                  )
                : const SizedBox.shrink(),
          ],
        );
      },
    );
  }

  void _enterPdv() {
    setState(() {
      _pdvSessionActive = true;
      _pdvMode = true;
    });
  }

  void _exitPdv() {
    setState(() {
      _pdvMode = false;
      _pdvSessionActive = false;
    });
  }

  void _navigateFromPdv(AgendaPage page) {
    controller.navigate(page);
    if (page == AgendaPage.home) return;
    setState(() => _pdvMode = false);
  }

  void _navigateFromShell(AgendaPage page) {
    controller.navigate(page);
    if (page != AgendaPage.home || !_pdvSessionActive) return;
    setState(() => _pdvMode = true);
  }

  Widget _desktopShell({required bool compactSidebar}) {
    final t = AgendaThemeTokens.of(context);
    final sidebarCollapsed = _sidebarCollapsed ?? compactSidebar;
    return Scaffold(
      backgroundColor: t.appBackground,
      body: Row(
        children: [
          _AgendaSidebar(
            controller: controller,
            compact: sidebarCollapsed,
            onNavigate: _navigateFromShell,
            onToggle: () {
              setState(() => _sidebarCollapsed = !sidebarCollapsed);
            },
          ),
          Expanded(
            child: Column(
              children: [
                _AgendaTopBar(
                  controller: controller,
                  searchController: _searchController,
                  onSearch: _onSearch,
                  pdvActive: _pdvSessionActive,
                  onEnterPdv: _enterPdv,
                  onNew: () => showAppointmentDialog(context, controller),
                ),
                Expanded(child: _currentPage()),
              ],
            ),
          ),
        ],
      ),
      floatingActionButton: AgendaWhatsAppFab(controller: controller),
    );
  }

  Widget _mobileShell() {
    final t = AgendaThemeTokens.of(context);
    return Scaffold(
      key: _scaffoldKey,
      backgroundColor: t.appBackground,
      drawer: _MobileAgendaDrawer(
        controller: controller,
        onEnterPdv: () {
          Navigator.of(context).pop();
          _enterPdv();
        },
        onNavigate: (page) {
          Navigator.of(context).pop();
          _navigateFromShell(page);
        },
      ),
      appBar: AppBar(
        toolbarHeight: 68,
        backgroundColor: t.panel,
        foregroundColor: t.ink,
        elevation: 0,
        scrolledUnderElevation: 0,
        shape: Border(bottom: BorderSide(color: t.sidebarBorder)),
        leading: IconButton(
          tooltip: 'Menu',
          onPressed: () => _scaffoldKey.currentState?.openDrawer(),
          icon: const Icon(Icons.menu_rounded),
        ),
        titleSpacing: 2,
        title: Row(
          children: [
            const _AgendaBrandMark(compact: true),
            const SizedBox(width: 9),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Agenda Livre',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 15.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  Text(
                    controller.trialStatusLabel == null
                        ? '${controller.businessName} · ${_pageTitle(controller.page)}'
                        : '${controller.businessName} · '
                              '${controller.trialStatusLabel}',
                    key: controller.trialStatusLabel == null
                        ? null
                        : const Key('agenda-trial-status-mobile'),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: t.muted, fontSize: 10.5),
                  ),
                ],
              ),
            ),
          ],
        ),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 8),
            child: Tooltip(
              message: 'Novo agendamento',
              child: SizedBox(
                height: 38,
                child: ElevatedButton.icon(
                  key: const Key('mobile-new-appointment'),
                  onPressed: () => showAppointmentDialog(context, controller),
                  icon: const Icon(Icons.add_rounded, size: 18),
                  label: const Text('Novo'),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 12),
                    textStyle: const TextStyle(
                      fontFamily: 'Segoe UI',
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                    ),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          if (_usesAgendaSearch || _usesDateContext) _mobileContextBar(t),
          Expanded(child: _currentPage()),
        ],
      ),
      bottomNavigationBar: _MobileQuickNavigation(
        page: controller.page,
        onSelected: (page) {
          if (page == null) {
            _scaffoldKey.currentState?.openDrawer();
            return;
          }
          controller.navigate(page);
        },
      ),
      floatingActionButton: AgendaWhatsAppFab(
        controller: controller,
        compact: true,
      ),
    );
  }

  Widget _currentPage() => switch (controller.page) {
    AgendaPage.home => HomePage(
      controller: controller,
      referenceNow: widget.referenceNow,
    ),
    AgendaPage.agenda => agenda_ui.AgendaPage(controller: controller),
    AgendaPage.finance => FinancePage(controller: controller),
    AgendaPage.reports => ReportsPage(controller: controller),
    AgendaPage.establishment => EstablishmentPage(controller: controller),
    AgendaPage.marketing => MarketingPage(controller: controller),
    AgendaPage.settings => SettingsPage(controller: controller),
  };

  bool get _usesAgendaSearch => controller.page == AgendaPage.agenda;

  bool get _usesDateContext => const {
    AgendaPage.home,
    AgendaPage.agenda,
    AgendaPage.reports,
  }.contains(controller.page);

  Widget _mobileContextBar(AgendaThemeTokens t) {
    return Container(
      key: const Key('mobile-context-bar'),
      color: t.panel,
      padding: const EdgeInsets.fromLTRB(12, 8, 12, 10),
      child: Row(
        children: [
          if (_usesAgendaSearch)
            Expanded(
              child: SizedBox(
                height: 44,
                child: TextField(
                  controller: _searchController,
                  onChanged: _onSearch,
                  decoration: InputDecoration(
                    hintText: 'Buscar na agenda...',
                    prefixIcon: const Icon(Icons.search_rounded, size: 19),
                    suffixIcon: _searchController.text.isEmpty
                        ? null
                        : IconButton(
                            tooltip: 'Limpar busca',
                            onPressed: () {
                              _searchController.clear();
                              _onSearch('');
                              setState(() {});
                            },
                            icon: const Icon(Icons.close_rounded, size: 18),
                          ),
                    contentPadding: const EdgeInsets.symmetric(vertical: 11),
                  ),
                ),
              ),
            )
          else
            Expanded(
              child: Row(
                children: [
                  Icon(
                    controller.page == AgendaPage.reports
                        ? Icons.date_range_outlined
                        : Icons.today_outlined,
                    size: 18,
                    color: t.accentDark,
                  ),
                  const SizedBox(width: 8),
                  Flexible(
                    child: Text(
                      controller.page == AgendaPage.reports
                          ? 'Data final do relatório'
                          : 'Visão do dia',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: t.muted,
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          if (_usesDateContext) ...[
            const SizedBox(width: 8),
            SizedBox(
              width: 112,
              height: 44,
              child: OutlinedButton.icon(
                key: const Key('mobile-date-filter'),
                onPressed: _pickDate,
                icon: const Icon(Icons.calendar_today_rounded, size: 15),
                label: Text(
                  _dateButtonLabel(controller.selectedDate),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                style: OutlinedButton.styleFrom(
                  foregroundColor: t.ink,
                  backgroundColor: t.panel,
                  side: BorderSide(color: t.line),
                  padding: const EdgeInsets.symmetric(horizontal: 8),
                  textStyle: const TextStyle(
                    fontFamily: 'Segoe UI',
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  void _onSearch(String value) {
    _searchDebounce?.cancel();
    _searchDebounce = Timer(const Duration(milliseconds: 280), () {
      controller.setSearch(value);
    });
  }

  Future<void> _pickDate() async {
    final value = await showDatePicker(
      context: context,
      initialDate: controller.selectedDate,
      firstDate: DateTime.now().subtract(const Duration(days: 3650)),
      lastDate: DateTime.now().add(const Duration(days: 3650)),
    );
    if (value != null) controller.selectDate(value);
  }
}

class _MobileQuickNavigation extends StatelessWidget {
  const _MobileQuickNavigation({required this.page, required this.onSelected});

  final AgendaPage page;
  final ValueChanged<AgendaPage?> onSelected;

  int get _selectedIndex => switch (page) {
    AgendaPage.home => 0,
    AgendaPage.agenda => 1,
    AgendaPage.finance => 2,
    AgendaPage.marketing => 3,
    _ => 4,
  };

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 340) {
          return _compactNavigation(t);
        }
        return NavigationBar(
          key: const Key('mobile-quick-navigation'),
          height: 68,
          selectedIndex: _selectedIndex,
          backgroundColor: t.panel,
          indicatorColor: t.accentSoft,
          labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
          onDestinationSelected: (index) => onSelected(switch (index) {
            0 => AgendaPage.home,
            1 => AgendaPage.agenda,
            2 => AgendaPage.finance,
            3 => AgendaPage.marketing,
            _ => null,
          }),
          destinations: const [
            NavigationDestination(
              icon: Icon(Icons.home_outlined),
              selectedIcon: Icon(Icons.home_rounded),
              label: 'Painel',
            ),
            NavigationDestination(
              icon: Icon(Icons.calendar_today_outlined),
              selectedIcon: Icon(Icons.calendar_month_rounded),
              label: 'Agenda',
            ),
            NavigationDestination(
              icon: Icon(Icons.account_balance_wallet_outlined),
              selectedIcon: Icon(Icons.account_balance_wallet_rounded),
              label: 'Caixa',
            ),
            NavigationDestination(
              icon: Icon(Icons.campaign_outlined),
              selectedIcon: Icon(Icons.campaign_rounded),
              label: 'Marketing',
            ),
            NavigationDestination(
              icon: Icon(Icons.more_horiz_rounded),
              label: 'Mais',
            ),
          ],
        );
      },
    );
  }

  Widget _compactNavigation(AgendaThemeTokens t) {
    final items = <(IconData, IconData, String, AgendaPage?)>[
      (Icons.home_outlined, Icons.home_rounded, 'Painel', AgendaPage.home),
      (
        Icons.calendar_today_outlined,
        Icons.calendar_month_rounded,
        'Agenda',
        AgendaPage.agenda,
      ),
      (
        Icons.account_balance_wallet_outlined,
        Icons.account_balance_wallet_rounded,
        'Caixa',
        AgendaPage.finance,
      ),
      (
        Icons.campaign_outlined,
        Icons.campaign_rounded,
        'Marketing',
        AgendaPage.marketing,
      ),
      (Icons.more_horiz_rounded, Icons.more_horiz_rounded, 'Mais', null),
    ];
    return Material(
      key: const Key('mobile-quick-navigation'),
      color: t.panel,
      child: SizedBox(
        height: 68,
        child: Row(
          children: [
            for (var index = 0; index < items.length; index++)
              Expanded(
                child: Semantics(
                  label: items[index].$3,
                  button: true,
                  selected: index == _selectedIndex,
                  child: InkWell(
                    onTap: () => onSelected(items[index].$4),
                    child: Center(
                      child: Container(
                        width: 42,
                        height: 40,
                        alignment: Alignment.center,
                        decoration: BoxDecoration(
                          color: index == _selectedIndex
                              ? t.accentSoft
                              : Colors.transparent,
                          borderRadius: BorderRadius.circular(16),
                        ),
                        child: Icon(
                          index == _selectedIndex
                              ? items[index].$2
                              : items[index].$1,
                          color: index == _selectedIndex ? t.accentDark : t.ink,
                          size: 23,
                        ),
                      ),
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _TopBarContext extends StatelessWidget {
  const _TopBarContext({
    required this.icon,
    required this.title,
    required this.subtitle,
  });

  final IconData icon;
  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Row(
      key: const Key('desktop-page-context'),
      children: [
        AgendaIconBadge(icon, background: t.accentSoft, color: t.accentDark),
        const SizedBox(width: 11),
        Expanded(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                ),
              ),
              Text(
                subtitle,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: t.muted, fontSize: 11),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _AgendaTopBar extends StatefulWidget {
  const _AgendaTopBar({
    required this.controller,
    required this.searchController,
    required this.onSearch,
    required this.pdvActive,
    required this.onEnterPdv,
    required this.onNew,
  });

  final AgendaController controller;
  final TextEditingController searchController;
  final ValueChanged<String> onSearch;
  final bool pdvActive;
  final VoidCallback onEnterPdv;
  final VoidCallback onNew;

  @override
  State<_AgendaTopBar> createState() => _AgendaTopBarState();
}

class _AgendaTopBarState extends State<_AgendaTopBar> {
  final _dateButtonKey = GlobalKey();

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final usesSearch = widget.controller.page == AgendaPage.agenda;
    final usesDate = const {
      AgendaPage.home,
      AgendaPage.agenda,
      AgendaPage.reports,
    }.contains(widget.controller.page);
    return Container(
      key: const Key('desktop-topbar'),
      height: 68,
      padding: const EdgeInsets.symmetric(horizontal: 26),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border(bottom: BorderSide(color: t.sidebarBorder)),
      ),
      child: Row(
        children: [
          Expanded(
            child: usesSearch
                ? SizedBox(
                    height: 42,
                    child: DecoratedBox(
                      decoration: BoxDecoration(
                        color: t.panel,
                        border: Border.all(color: t.line),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: TextField(
                        controller: widget.searchController,
                        onChanged: widget.onSearch,
                        style: TextStyle(color: t.ink, fontSize: 13),
                        decoration: InputDecoration(
                          hintText:
                              'Buscar cliente, serviço ou profissional na agenda...',
                          hintStyle: TextStyle(color: t.muted, fontSize: 12.5),
                          prefixIcon: Icon(
                            Icons.search_rounded,
                            size: 19,
                            color: t.muted,
                          ),
                          border: InputBorder.none,
                          enabledBorder: InputBorder.none,
                          focusedBorder: InputBorder.none,
                          filled: false,
                          contentPadding: const EdgeInsets.symmetric(
                            vertical: 11,
                          ),
                        ),
                      ),
                    ),
                  )
                : _TopBarContext(
                    icon: _contextIcon(widget.controller.page),
                    title: _pageTitle(widget.controller.page),
                    subtitle: _contextSubtitle(widget.controller.page),
                  ),
          ),
          if (usesDate) ...[
            const SizedBox(width: 16),
            SizedBox(
              key: _dateButtonKey,
              width: 128,
              height: 40,
              child: OutlinedButton(
                key: const Key('topbar-date-button'),
                onPressed: _showDatePopover,
                style: _topBarOutlinedStyle(t),
                child: Text(_dateButtonLabel(widget.controller.selectedDate)),
              ),
            ),
          ],
          if (widget.controller.page != AgendaPage.settings) ...[
            const SizedBox(width: 14),
            SizedBox(
              height: 40,
              child: OutlinedButton(
                onPressed: () =>
                    widget.controller.navigate(AgendaPage.settings),
                style: _topBarOutlinedStyle(t),
                child: const Text('Configurações'),
              ),
            ),
          ],
          const SizedBox(width: 10),
          SizedBox(
            height: 40,
            child: OutlinedButton.icon(
              key: const Key('desktop-enter-pdv'),
              onPressed: widget.onEnterPdv,
              style: _topBarOutlinedStyle(t),
              icon: const Icon(Icons.point_of_sale_outlined, size: 17),
              label: Text(widget.pdvActive ? 'Voltar ao PDV' : 'Modo PDV'),
            ),
          ),
          const SizedBox(width: 10),
          SizedBox(
            height: 40,
            child: ElevatedButton.icon(
              onPressed: widget.onNew,
              icon: const Icon(Icons.add_rounded, size: 18),
              label: const Text('Novo agendamento'),
            ),
          ),
        ],
      ),
    );
  }

  IconData _contextIcon(AgendaPage page) => switch (page) {
    AgendaPage.home => Icons.dashboard_outlined,
    AgendaPage.finance => Icons.account_balance_wallet_outlined,
    AgendaPage.reports => Icons.insights_outlined,
    AgendaPage.establishment => Icons.storefront_outlined,
    AgendaPage.marketing => Icons.campaign_outlined,
    AgendaPage.settings => Icons.settings_outlined,
    AgendaPage.agenda => Icons.calendar_month_outlined,
  };

  String _contextSubtitle(AgendaPage page) => switch (page) {
    AgendaPage.home => 'Resumo e operação do dia',
    AgendaPage.finance => 'Entradas, pendências e despesas',
    AgendaPage.reports => 'Indicadores do período selecionado',
    AgendaPage.establishment => 'Clientes, equipe e serviços',
    AgendaPage.marketing => 'Campanhas e relacionamento',
    AgendaPage.settings => 'Preferências do negócio e integrações',
    AgendaPage.agenda => 'Horários e atendimentos',
  };

  Future<void> _showDatePopover() async {
    final buttonContext = _dateButtonKey.currentContext;
    if (buttonContext == null) return;
    final button = buttonContext.findRenderObject() as RenderBox?;
    if (button == null || !button.hasSize) return;

    final media = MediaQuery.of(context);
    final buttonOffset = button.localToGlobal(Offset.zero);
    const popoverWidth = 400.0;
    final left = (buttonOffset.dx + (button.size.width - popoverWidth) / 2)
        .clamp(8.0, media.size.width - popoverWidth - 8)
        .toDouble();
    final top = (buttonOffset.dy + button.size.height + 10)
        .clamp(media.padding.top + 8, media.size.height - 300)
        .toDouble();

    final value = await showGeneralDialog<DateTime>(
      context: context,
      barrierDismissible: true,
      barrierLabel: 'Fechar seletor de data',
      barrierColor: Colors.transparent,
      transitionDuration: const Duration(milliseconds: 140),
      transitionBuilder: (context, animation, secondaryAnimation, child) {
        final curved = CurvedAnimation(
          parent: animation,
          curve: Curves.easeOutCubic,
        );
        return FadeTransition(
          opacity: curved,
          child: ScaleTransition(
            scale: Tween<double>(begin: .97, end: 1).animate(curved),
            alignment: Alignment.topCenter,
            child: child,
          ),
        );
      },
      pageBuilder: (dialogContext, animation, secondaryAnimation) => Stack(
        children: [
          Positioned(
            left: left,
            top: top,
            width: popoverWidth,
            child: _AgendaDatePopover(
              initialDate: widget.controller.selectedDate,
            ),
          ),
        ],
      ),
    );
    if (value != null) widget.controller.selectDate(value);
  }

  ButtonStyle _topBarOutlinedStyle(AgendaThemeTokens t) {
    final base = OutlinedButton.styleFrom(
      foregroundColor: t.ink,
      backgroundColor: t.panel,
      minimumSize: const Size(98, 40),
      padding: const EdgeInsets.symmetric(horizontal: 14),
      textStyle: const TextStyle(
        fontFamily: 'Segoe UI',
        fontSize: 14,
        fontWeight: FontWeight.w600,
      ),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
    );
    return base.copyWith(
      side: WidgetStateProperty.resolveWith((states) {
        final highlighted =
            states.contains(WidgetState.hovered) ||
            states.contains(WidgetState.focused);
        return BorderSide(
          color: highlighted ? t.accent : t.line,
          width: highlighted ? 1.2 : 1,
        );
      }),
    );
  }
}

class _AgendaDatePopover extends StatefulWidget {
  const _AgendaDatePopover({required this.initialDate});

  final DateTime initialDate;

  @override
  State<_AgendaDatePopover> createState() => _AgendaDatePopoverState();
}

class _AgendaDatePopoverState extends State<_AgendaDatePopover> {
  late DateTime _rangeAnchor;

  @override
  void initState() {
    super.initState();
    _rangeAnchor = DateUtils.dateOnly(widget.initialDate);
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final rangeStart = _rangeAnchor.subtract(const Duration(days: 2));
    final days = List<DateTime>.generate(
      7,
      (index) => rangeStart.add(Duration(days: index)),
    );
    final rangeEnd = days.last;
    final today = DateUtils.dateOnly(DateTime.now());

    return Material(
      key: const Key('date-popover'),
      color: t.panel,
      elevation: 14,
      shadowColor: Colors.black.withValues(alpha: .18),
      borderRadius: BorderRadius.circular(16),
      clipBehavior: Clip.antiAlias,
      child: Container(
        decoration: BoxDecoration(
          border: Border.all(color: t.line),
          borderRadius: BorderRadius.circular(16),
        ),
        padding: const EdgeInsets.all(15),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              children: [
                _PopoverArrowButton(
                  tooltip: 'Semana anterior',
                  icon: Icons.chevron_left_rounded,
                  onPressed: () => setState(
                    () => _rangeAnchor = _rangeAnchor.subtract(
                      const Duration(days: 7),
                    ),
                  ),
                ),
                Expanded(
                  child: Text(
                    _rangeLabel(rangeStart, rangeEnd),
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                _PopoverArrowButton(
                  tooltip: 'Próxima semana',
                  icon: Icons.chevron_right_rounded,
                  onPressed: () => setState(
                    () => _rangeAnchor = _rangeAnchor.add(
                      const Duration(days: 7),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 9),
            Row(
              children: [
                for (var index = 0; index < days.length; index++) ...[
                  Expanded(
                    child: _DateStripDay(
                      date: days[index],
                      selected: DateUtils.isSameDay(
                        days[index],
                        widget.initialDate,
                      ),
                      today: DateUtils.isSameDay(days[index], today),
                      onPressed: () => Navigator.of(context).pop(days[index]),
                    ),
                  ),
                  if (index != days.length - 1) const SizedBox(width: 4),
                ],
              ],
            ),
            const SizedBox(height: 9),
            Row(
              children: [
                for (final shortcut in <(String, DateTime)>[
                  ('Ontem', today.subtract(const Duration(days: 1))),
                  ('Hoje', today),
                  ('Amanhã', today.add(const Duration(days: 1))),
                ])
                  Expanded(
                    child: SizedBox(
                      height: 32,
                      child: TextButton(
                        onPressed: () => Navigator.of(context).pop(shortcut.$2),
                        style: TextButton.styleFrom(
                          foregroundColor: t.accentDark,
                          minimumSize: const Size(0, 32),
                          padding: const EdgeInsets.symmetric(horizontal: 8),
                          textStyle: const TextStyle(
                            fontSize: 12.5,
                            fontWeight: FontWeight.w600,
                          ),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(9),
                          ),
                        ),
                        child: Text(shortcut.$1),
                      ),
                    ),
                  ),
              ],
            ),
            Padding(
              padding: const EdgeInsets.only(top: 10, bottom: 8),
              child: Divider(height: 1, color: t.line),
            ),
            SizedBox(
              width: double.infinity,
              height: 40,
              child: OutlinedButton.icon(
                onPressed: _openCalendar,
                style: OutlinedButton.styleFrom(
                  foregroundColor: t.accentDark,
                  side: BorderSide(color: t.line),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
                icon: const Icon(Icons.calendar_month_rounded, size: 17),
                label: const Text('Escolher outra data'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _openCalendar() async {
    final value = await showDatePicker(
      context: context,
      initialDate: widget.initialDate,
      firstDate: DateTime.now().subtract(const Duration(days: 3650)),
      lastDate: DateTime.now().add(const Duration(days: 3650)),
    );
    if (value != null && mounted) Navigator.of(context).pop(value);
  }
}

class _DateStripDay extends StatelessWidget {
  const _DateStripDay({
    required this.date,
    required this.selected,
    required this.today,
    required this.onPressed,
  });

  final DateTime date;
  final bool selected;
  final bool today;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final foreground = selected ? Colors.white : t.ink;
    return Tooltip(
      message:
          '${_weekdayLong(date.weekday)}, ${date.day} de '
          '${_monthName(date.month)} de ${date.year}',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(
            height: 15,
            child: today
                ? Text(
                    'Hoje',
                    style: TextStyle(
                      color: t.accentDark,
                      fontSize: 10.5,
                      fontWeight: FontWeight.w600,
                    ),
                  )
                : null,
          ),
          Material(
            color: selected ? t.accentDark : t.panel,
            borderRadius: BorderRadius.circular(11),
            child: InkWell(
              key: Key('date-strip-${date.year}-${date.month}-${date.day}'),
              onTap: onPressed,
              borderRadius: BorderRadius.circular(11),
              child: Container(
                height: 68,
                decoration: BoxDecoration(
                  border: Border.all(color: selected ? t.accentDark : t.line),
                  borderRadius: BorderRadius.circular(11),
                ),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      _weekdayShort(date.weekday),
                      style: TextStyle(color: foreground, fontSize: 11),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      date.day.toString().padLeft(2, '0'),
                      style: TextStyle(
                        color: foreground,
                        fontSize: 19,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _PopoverArrowButton extends StatelessWidget {
  const _PopoverArrowButton({
    required this.tooltip,
    required this.icon,
    required this.onPressed,
  });

  final String tooltip;
  final IconData icon;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return SizedBox(
      width: 34,
      height: 34,
      child: IconButton(
        tooltip: tooltip,
        onPressed: onPressed,
        padding: EdgeInsets.zero,
        constraints: const BoxConstraints.tightFor(width: 34, height: 34),
        style: IconButton.styleFrom(
          foregroundColor: t.ink,
          side: BorderSide(color: t.line),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
        ),
        icon: Icon(icon, size: 19),
      ),
    );
  }
}

class _AgendaSidebar extends StatelessWidget {
  const _AgendaSidebar({
    required this.controller,
    required this.compact,
    required this.onNavigate,
    required this.onToggle,
  });

  final AgendaController controller;
  final bool compact;
  final ValueChanged<AgendaPage> onNavigate;
  final VoidCallback onToggle;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final defaultTheme = controller.data.settings.themeId.trim().isEmpty;
    final palette = _SidebarPalette.fromTheme(t, defaultTheme: defaultTheme);
    return AnimatedContainer(
      key: const Key('desktop-sidebar'),
      duration: const Duration(milliseconds: 180),
      width: compact ? 72 : 260,
      decoration: BoxDecoration(
        color: palette.background,
        border: Border(right: BorderSide(color: t.accent, width: 3)),
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          // During the width animation, render the version that fits the
          // current frame instead of the final state. This keeps every item
          // usable and overflow-free in both animation directions.
          final contentCompact = constraints.maxWidth < 170;
          return SafeArea(
            child: Column(
              children: [
                _SidebarBrandHeader(
                  compact: contentCompact,
                  controller: controller,
                  onToggle: onToggle,
                  lineColor: t.line,
                ),
                Expanded(
                  child: ListView(
                    padding: EdgeInsets.fromLTRB(
                      contentCompact ? 10 : 18,
                      contentCompact ? 20 : 28,
                      contentCompact ? 10 : 18,
                      10,
                    ),
                    children: [
                      for (final destination in _destinations)
                        _SidebarDestination(
                          destination: destination,
                          compact: contentCompact,
                          selected: controller.page == destination.page,
                          onTap: () => onNavigate(destination.page),
                          palette: palette,
                        ),
                    ],
                  ),
                ),
                Padding(
                  padding: EdgeInsets.fromLTRB(
                    contentCompact ? 10 : 18,
                    8,
                    contentCompact ? 10 : 18,
                    18,
                  ),
                  child: _SidebarProfileToggle(
                    controller: controller,
                    compact: contentCompact,
                    palette: palette,
                    onToggle: onToggle,
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _SidebarBrandHeader extends StatelessWidget {
  const _SidebarBrandHeader({
    required this.compact,
    required this.controller,
    required this.onToggle,
    required this.lineColor,
  });

  final bool compact;
  final AgendaController controller;
  final VoidCallback onToggle;
  final Color lineColor;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final settings = controller.data.settings;
    final details = <String>[
      controller.businessName,
      if (settings.businessSegment.trim().isNotEmpty)
        settings.businessSegment.trim(),
      if (settings.businessDocument.trim().isNotEmpty)
        settings.businessDocument.trim(),
    ].join(' · ');
    final logo = Image.asset(
      'assets/branding/agenda-livre-mark.png',
      width: compact ? 48 : 48,
      height: compact ? 44 : 34,
      fit: BoxFit.contain,
      filterQuality: FilterQuality.high,
      semanticLabel: 'Agenda Livre',
    );
    return Container(
      key: const Key('sidebar-brand-header'),
      height: 68,
      padding: EdgeInsets.fromLTRB(compact ? 10 : 24, 0, compact ? 10 : 18, 0),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border(bottom: BorderSide(color: lineColor)),
      ),
      child: compact
          ? Center(child: logo)
          : Row(
              children: [
                logo,
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Agenda Livre',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 15.5,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 1),
                      Text(
                        details,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(color: t.muted, fontSize: 10.5),
                      ),
                    ],
                  ),
                ),
              ],
            ),
    );
  }
}

class _SidebarProfileToggle extends StatelessWidget {
  const _SidebarProfileToggle({
    required this.controller,
    required this.compact,
    required this.palette,
    required this.onToggle,
  });

  final AgendaController controller;
  final bool compact;
  final _SidebarPalette palette;
  final VoidCallback onToggle;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final trialLabel = controller.trialStatusLabel;
    if (compact) {
      return Tooltip(
        message: 'Expandir menu lateral',
        child: Material(
          key: const Key('sidebar-profile-surface'),
          color: palette.profileBackground,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
            side: BorderSide(color: palette.profileBorder),
          ),
          clipBehavior: Clip.antiAlias,
          child: InkWell(
            key: const Key('sidebar-profile-toggle'),
            onTap: onToggle,
            child: SizedBox(
              width: 48,
              height: 44,
              child: Icon(
                Icons.menu_rounded,
                color: palette.profileText,
                size: 22,
              ),
            ),
          ),
        ),
      );
    }
    final content = Material(
      key: const Key('sidebar-profile-surface'),
      color: palette.profileBackground,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: palette.profileBorder),
      ),
      clipBehavior: Clip.antiAlias,
      child: SizedBox(
        height: 72,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          child: Row(
            children: [
              _ProfileAvatar(
                name: controller.businessName,
                palette: palette,
                accent: t.accent,
                accentSoft: t.accentSoft,
              ),
              const SizedBox(width: 9),
              Expanded(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      controller.businessName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: palette.profileText,
                        fontSize: 13.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      controller.profileSubtitle,
                      key: trialLabel == null
                          ? null
                          : const Key('agenda-trial-status-desktop'),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: trialLabel == null
                            ? palette.profileMuted
                            : controller.isTrialExpired
                            ? const Color(0xFFDC2626)
                            : t.accent,
                        fontSize: 11.5,
                        fontWeight: trialLabel == null
                            ? FontWeight.normal
                            : FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 7),
              Material(
                key: const Key('sidebar-toggle-surface'),
                color: t.accentSoft,
                borderRadius: BorderRadius.circular(10),
                child: InkWell(
                  key: const Key('sidebar-toggle'),
                  onTap: onToggle,
                  borderRadius: BorderRadius.circular(10),
                  child: SizedBox(
                    width: 28,
                    height: 28,
                    child: Icon(
                      Icons.menu_open_rounded,
                      color: t.ink,
                      size: 18,
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
    return content;
  }
}

class _SidebarPalette {
  const _SidebarPalette({
    required this.background,
    required this.text,
    required this.selectedBackground,
    required this.selectedText,
    required this.selectedBorder,
    required this.profileBackground,
    required this.profileBorder,
    required this.profileText,
    required this.profileMuted,
  });

  factory _SidebarPalette.fromTheme(
    AgendaThemeTokens t, {
    required bool defaultTheme,
  }) {
    if (defaultTheme) {
      return _SidebarPalette(
        background: const Color(0xFF171614),
        text: const Color(0xFFF6F3F1),
        selectedBackground: t.accent,
        selectedText: Colors.white,
        selectedBorder: t.accentDark,
        profileBackground: const Color(0xFF24211F),
        profileBorder: const Color(0xFF35312E),
        profileText: Colors.white,
        profileMuted: const Color(0xFFBBB4AE),
      );
    }
    return _SidebarPalette(
      background: Colors.white,
      text: t.ink,
      selectedBackground: t.accentSoft,
      selectedText: t.ink,
      selectedBorder: t.accentDark,
      profileBackground: Colors.white,
      profileBorder: t.line,
      profileText: t.ink,
      profileMuted: t.muted,
    );
  }

  final Color background;
  final Color text;
  final Color selectedBackground;
  final Color selectedText;
  final Color selectedBorder;
  final Color profileBackground;
  final Color profileBorder;
  final Color profileText;
  final Color profileMuted;
}

class _SidebarDestination extends StatelessWidget {
  const _SidebarDestination({
    required this.destination,
    required this.compact,
    required this.selected,
    required this.onTap,
    this.palette,
  });

  final _AgendaDestination destination;
  final bool compact;
  final bool selected;
  final VoidCallback onTap;
  final _SidebarPalette? palette;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final color = selected
        ? palette?.selectedText ?? t.accent
        : palette?.text ?? t.sidebarText;
    final content = Material(
      key: Key('sidebar-destination-${destination.page.name}'),
      color: selected
          ? palette?.selectedBackground ?? t.sidebarActive
          : Colors.transparent,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: selected && palette != null
            ? BorderSide(color: palette!.selectedBorder)
            : BorderSide.none,
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: SizedBox(
          height: 44,
          child: Row(
            mainAxisAlignment: compact
                ? MainAxisAlignment.center
                : MainAxisAlignment.start,
            children: [
              if (!compact) const SizedBox(width: 15),
              Icon(destination.icon, color: color, size: 20),
              if (!compact) ...[
                const SizedBox(width: 13),
                Expanded(
                  child: Text(
                    destination.label,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: color,
                      fontSize: 14,
                      fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
              ],
            ],
          ),
        ),
      ),
    );
    final fittedContent = compact
        ? Center(child: SizedBox(width: 48, child: content))
        : content;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: compact
          ? Tooltip(message: destination.label, child: fittedContent)
          : fittedContent,
    );
  }
}

class _AgendaBrandMark extends StatelessWidget {
  const _AgendaBrandMark({required this.compact});

  final bool compact;

  @override
  Widget build(BuildContext context) {
    return Image.asset(
      'assets/branding/agenda-livre-mark.png',
      width: compact ? 36 : 48,
      height: compact ? 26 : 34,
      fit: BoxFit.contain,
      filterQuality: FilterQuality.high,
      semanticLabel: 'Agenda Livre',
    );
  }
}

class _ProfileAvatar extends StatelessWidget {
  const _ProfileAvatar({
    required this.name,
    this.palette,
    this.accent,
    this.accentSoft,
  });

  final String name;
  final _SidebarPalette? palette;
  final Color? accent;
  final Color? accentSoft;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final dark = palette?.background == const Color(0xFF171614);
    return Container(
      width: 42,
      height: 42,
      decoration: BoxDecoration(
        color: dark ? const Color(0xFF312D2A) : (accentSoft ?? t.accentSoft),
        borderRadius: BorderRadius.circular(13),
        border: Border.all(
          color: dark ? const Color(0xFF5A524C) : (accent ?? t.accent),
        ),
      ),
      alignment: Alignment.center,
      child: Text(
        initials(name),
        style: TextStyle(
          color: dark ? Colors.white : (accent ?? t.accentDark),
          fontSize: 13,
          fontWeight: FontWeight.w800,
        ),
      ),
    );
  }
}

class _MobileAgendaDrawer extends StatelessWidget {
  const _MobileAgendaDrawer({
    required this.controller,
    required this.onNavigate,
    required this.onEnterPdv,
  });

  final AgendaController controller;
  final ValueChanged<AgendaPage> onNavigate;
  final VoidCallback onEnterPdv;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final defaultTheme = controller.data.settings.themeId.trim().isEmpty;
    final palette = _SidebarPalette.fromTheme(t, defaultTheme: defaultTheme);
    final settings = controller.data.settings;
    final businessDetails = <String>[
      controller.businessName,
      if (settings.businessSegment.trim().isNotEmpty)
        settings.businessSegment.trim(),
    ].join(' · ');
    return Drawer(
      backgroundColor: palette.background,
      child: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(22, 18, 18, 18),
              child: Row(
                children: [
                  const _AgendaBrandMark(compact: false),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Agenda Livre',
                          style: TextStyle(
                            color: palette.text,
                            fontSize: 16,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        Text(
                          businessDetails,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: palette.profileMuted,
                            fontSize: 11,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            Divider(height: 1, color: palette.profileBorder),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.all(14),
                children: [
                  Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: FilledButton.icon(
                      key: const Key('mobile-enter-pdv'),
                      onPressed: onEnterPdv,
                      icon: const Icon(Icons.point_of_sale_outlined, size: 19),
                      label: const Text('Entrar no Modo PDV'),
                    ),
                  ),
                  for (final destination in _destinations)
                    _SidebarDestination(
                      destination: destination,
                      compact: false,
                      selected: controller.page == destination.page,
                      onTap: () => onNavigate(destination.page),
                      palette: palette,
                    ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  _ProfileAvatar(
                    name: controller.businessName,
                    palette: palette,
                    accent: t.accent,
                    accentSoft: t.accentSoft,
                  ),
                  const SizedBox(width: 11),
                  Expanded(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          controller.businessName,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: palette.profileText,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        Text(
                          controller.profileSubtitle,
                          key: controller.trialStatusLabel == null
                              ? null
                              : const Key('agenda-trial-status-drawer'),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: controller.trialStatusLabel == null
                                ? palette.profileMuted
                                : controller.isTrialExpired
                                ? const Color(0xFFDC2626)
                                : t.accent,
                            fontSize: 11,
                            fontWeight: controller.trialStatusLabel == null
                                ? FontWeight.normal
                                : FontWeight.w700,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _AgendaDestination {
  const _AgendaDestination(this.page, this.label, this.icon);
  final AgendaPage page;
  final String label;
  final IconData icon;
}

const _destinations = <_AgendaDestination>[
  _AgendaDestination(AgendaPage.home, 'Painel', Icons.home_rounded),
  _AgendaDestination(AgendaPage.agenda, 'Agenda', Icons.calendar_month_rounded),
  _AgendaDestination(
    AgendaPage.finance,
    'Financeiro',
    Icons.attach_money_rounded,
  ),
  _AgendaDestination(
    AgendaPage.reports,
    'Relatórios',
    Icons.query_stats_rounded,
  ),
  _AgendaDestination(
    AgendaPage.establishment,
    'Meu estabelecimento',
    Icons.storefront_rounded,
  ),
  _AgendaDestination(AgendaPage.marketing, 'Marketing', Icons.campaign_rounded),
  _AgendaDestination(
    AgendaPage.settings,
    'Configurações',
    Icons.settings_rounded,
  ),
];

String _pageTitle(AgendaPage page) => switch (page) {
  AgendaPage.home => 'Painel',
  AgendaPage.agenda => 'Agenda',
  AgendaPage.finance => 'Financeiro',
  AgendaPage.reports => 'Relatórios',
  AgendaPage.establishment => 'Meu estabelecimento',
  AgendaPage.marketing => 'Marketing',
  AgendaPage.settings => 'Configurações',
};

String _dateButtonLabel(DateTime date) {
  final value = DateUtils.dateOnly(date);
  final today = DateUtils.dateOnly(DateTime.now());
  final shortValue =
      '${value.day.toString().padLeft(2, '0')}/'
      '${value.month.toString().padLeft(2, '0')}';
  if (DateUtils.isSameDay(value, today)) return 'Hoje, $shortValue';
  if (DateUtils.isSameDay(value, today.add(const Duration(days: 1)))) {
    return 'Amanhã, $shortValue';
  }
  if (DateUtils.isSameDay(value, today.subtract(const Duration(days: 1)))) {
    return 'Ontem, $shortValue';
  }
  return '${_weekdayShort(value.weekday)}., $shortValue';
}

String _rangeLabel(DateTime start, DateTime end) {
  if (start.year == end.year && start.month == end.month) {
    return '${start.day} – ${end.day} de ${_monthName(start.month)}';
  }
  if (start.year == end.year) {
    return '${start.day} de ${_monthShort(start.month)} – '
        '${end.day} de ${_monthShort(end.month)}';
  }
  return '${start.day.toString().padLeft(2, '0')}/'
      '${start.month.toString().padLeft(2, '0')}/${start.year} – '
      '${end.day.toString().padLeft(2, '0')}/'
      '${end.month.toString().padLeft(2, '0')}/${end.year}';
}

String _monthName(int month) => const <String>[
  'janeiro',
  'fevereiro',
  'março',
  'abril',
  'maio',
  'junho',
  'julho',
  'agosto',
  'setembro',
  'outubro',
  'novembro',
  'dezembro',
][month - 1];

String _monthShort(int month) => const <String>[
  'jan',
  'fev',
  'mar',
  'abr',
  'mai',
  'jun',
  'jul',
  'ago',
  'set',
  'out',
  'nov',
  'dez',
][month - 1];

String _weekdayShort(int weekday) => const <String>[
  'seg',
  'ter',
  'qua',
  'qui',
  'sex',
  'sáb',
  'dom',
][weekday - 1];

String _weekdayLong(int weekday) => const <String>[
  'segunda',
  'terça',
  'quarta',
  'quinta',
  'sexta',
  'sábado',
  'domingo',
][weekday - 1];
