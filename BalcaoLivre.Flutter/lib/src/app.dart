import 'dart:async';
import 'dart:convert';
import 'dart:math' as math;

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:url_launcher/url_launcher.dart';

import 'models.dart';
import 'store.dart';

// Tokens shared with BrandPalette.cs in the current WPF application.
const _blue = Color(0xFFFC601D);
const _blue2 = Color(0xFFE6530E);
const _rail = Color(0xFF202020);
const _railDeep = Color(0xFF171717);
const _navy = Color(0xFF202020);
const _navy2 = Color(0xFF202020);
const _teal = Color(0xFF178A46);
const _mint = Color(0xFFFFF0E5);
const _line = Color(0xFFDED3C8);
const _paper = Color(0xFFF5F0E9);
const _surface = Color(0xFFFFFCF8);
const _surfaceMuted = Color(0xFFF8F2EB);
const _surfaceSelected = Color(0xFFFFE5D2);
const _textSecondary = Color(0xFF625B55);
const _textMuted = Color(0xFF817870);
const _borderStrong = Color(0xFFCDBFB1);
const _danger = Color(0xFFA11D1D);
const _warn = Color(0xFFF59E0B);
const _visualQa = bool.fromEnvironment('BALCAO_VISUAL_QA', defaultValue: false);

String money(double value) =>
    'R\$ ${value.toStringAsFixed(2).replaceAll('.', ',')}';

String kindLabel(OrderKind kind) => switch (kind) {
  OrderKind.table => 'Mesa',
  OrderKind.counter => 'Balcao',
  OrderKind.delivery => 'Delivery',
  OrderKind.ifood => 'iFood',
};

String statusLabel(OrderStatus status) => switch (status) {
  OrderStatus.open => 'Aberto',
  OrderStatus.preparing => 'Preparo',
  OrderStatus.dispatched => 'Saiu',
  OrderStatus.delivered => 'Entregue',
  OrderStatus.closed => 'Fechado',
  OrderStatus.canceled => 'Cancelado',
};

class BalcaoLivreApp extends StatefulWidget {
  const BalcaoLivreApp({super.key});

  @override
  State<BalcaoLivreApp> createState() => _BalcaoLivreAppState();
}

class _BalcaoLivreAppState extends State<BalcaoLivreApp> {
  final BalcaoStore store = BalcaoStore();

  @override
  void initState() {
    super.initState();
    store.hydrate();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Balcao Livre PDV',
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: _blue,
          brightness: Brightness.light,
          surface: _surface,
        ),
        scaffoldBackgroundColor: _paper,
        fontFamily: 'Segoe UI',
        dividerColor: _line,
        hoverColor: _surfaceSelected,
        splashColor: _blue.withValues(alpha: .10),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: _surface,
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 14,
            vertical: 13,
          ),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: const BorderSide(color: _line),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: const BorderSide(color: _line),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: const BorderSide(color: _blue, width: 1.5),
          ),
        ),
        filledButtonTheme: FilledButtonThemeData(
          style: FilledButton.styleFrom(
            backgroundColor: _blue,
            foregroundColor: _navy,
            textStyle: const TextStyle(fontWeight: FontWeight.w800),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(10),
            ),
          ),
        ),
        textTheme: const TextTheme(
          titleLarge: TextStyle(
            fontWeight: FontWeight.w900,
            color: _navy,
            letterSpacing: 0,
          ),
          titleMedium: TextStyle(
            fontWeight: FontWeight.w900,
            color: _navy,
            letterSpacing: 0,
          ),
          bodyMedium: TextStyle(color: _navy, letterSpacing: 0),
          bodySmall: TextStyle(color: _textSecondary, letterSpacing: 0),
        ),
      ),
      home: AnimatedBuilder(
        animation: store,
        builder: (context, _) {
          if (!store.hydrated) {
            return const Scaffold(
              body: Center(child: CircularProgressIndicator()),
            );
          }
          return _MobileFrame(
            child: store.loggedIn || _visualQa
                ? HomeScreen(store: store)
                : LoginScreen(store: store),
          );
        },
      ),
    );
  }
}

class _MobileFrame extends StatelessWidget {
  const _MobileFrame({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      color: _paper,
      child: LayoutBuilder(
        builder: (context, constraints) {
          return SizedBox(
            width: constraints.maxWidth,
            height: constraints.maxHeight,
            child: child,
          );
        },
      ),
    );
  }
}

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, required this.store});

  final BalcaoStore store;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final emailController = TextEditingController();
  final passwordController = TextEditingController();

  @override
  void initState() {
    super.initState();
    emailController.text = widget.store.authEmail;
  }

  @override
  void dispose() {
    emailController.dispose();
    passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    await widget.store.login(emailController.text, passwordController.text);
  }

  @override
  Widget build(BuildContext context) {
    final size = MediaQuery.sizeOf(context);
    final compact = size.width < 700;
    final dialogWidth = math.min(size.width - (compact ? 24 : 48), 540.0);

    return Scaffold(
      backgroundColor: _paper,
      body: SafeArea(
        child: Stack(
          children: [
            Positioned.fill(
              child: AbsorbPointer(
                child: Opacity(
                  opacity: compact ? .94 : .98,
                  child: _ClosedCashLoginPreview(store: widget.store),
                ),
              ),
            ),
            Positioned.fill(
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: Colors.black.withValues(alpha: compact ? .10 : .05),
                ),
              ),
            ),
            Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: SizedBox(
                  width: dialogWidth,
                  child: _PdvLoginDialog(
                    operatorController: emailController,
                    passwordController: passwordController,
                    busy: widget.store.authBusy,
                    error: widget.store.authError,
                    onSubmit: _submit,
                    onClose: () {
                      FocusScope.of(context).unfocus();
                    },
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

class _ClosedCashLoginPreview extends StatelessWidget {
  const _ClosedCashLoginPreview({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: _paper,
      child: Column(
        children: [
          _PdvTopBar(
            store: store,
            openModule: (_, _) {},
            toggleCash: () {},
            forceCashClosed: true,
          ),
          _PdvRibbon(
            store: store,
            openModule: (_, _) {},
            openProductSearch: () {},
            toggleCash: () {},
            forceCashClosed: true,
          ),
          _ClosedCashModeBar(store: store, selectedPage: 0),
          Expanded(
            child: _ClosedCashDashboard(
              store: store,
              onOpen: () {},
              openModule: (_, _) {},
            ),
          ),
          _KeyboardFooter(store: store),
        ],
      ),
    );
  }
}

class _PdvLoginDialog extends StatelessWidget {
  const _PdvLoginDialog({
    required this.operatorController,
    required this.passwordController,
    required this.busy,
    required this.error,
    required this.onSubmit,
    required this.onClose,
  });

  final TextEditingController operatorController;
  final TextEditingController passwordController;
  final bool busy;
  final String error;
  final Future<void> Function() onSubmit;
  final VoidCallback onClose;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: _line),
        boxShadow: const [
          BoxShadow(
            blurRadius: 34,
            offset: Offset(0, 20),
            color: Color(0x26000000),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(14),
        child: Material(
          color: Colors.white,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                height: 68,
                padding: const EdgeInsets.symmetric(horizontal: 20),
                decoration: const BoxDecoration(
                  color: Colors.white,
                  border: Border(bottom: BorderSide(color: _line)),
                ),
                child: Row(
                  children: [
                    Container(
                      width: 36,
                      height: 36,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(
                        color: _mint,
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(color: Color(0xFFFFA76D)),
                      ),
                      child: const Icon(
                        Icons.phone_android_rounded,
                        color: _teal,
                        size: 20,
                      ),
                    ),
                    const SizedBox(width: 12),
                    const Expanded(
                      child: Text(
                        'Entrada no PDV',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: _navy2,
                          fontSize: 18,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ),
                    SizedBox(
                      width: 40,
                      height: 36,
                      child: FilledButton(
                        onPressed: onClose,
                        style: FilledButton.styleFrom(
                          padding: EdgeInsets.zero,
                          backgroundColor: _surfaceMuted,
                          foregroundColor: _navy2,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                            side: const BorderSide(color: _line),
                          ),
                        ),
                        child: const Text(
                          'X',
                          style: TextStyle(
                            fontWeight: FontWeight.w900,
                            fontSize: 16,
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(28, 26, 28, 28),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Informe o e-mail e senha da conta Supabase da loja.',
                      style: TextStyle(
                        color: _navy2,
                        fontSize: 16,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    const SizedBox(height: 22),
                    _PdvLoginField(
                      label: 'E-mail da conta',
                      controller: operatorController,
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      enabled: !busy,
                    ),
                    const SizedBox(height: 18),
                    _PdvLoginField(
                      label: 'Senha',
                      controller: passwordController,
                      obscureText: true,
                      textInputAction: TextInputAction.done,
                      enabled: !busy,
                      onSubmitted: (_) {
                        if (!busy) onSubmit();
                      },
                    ),
                    if (error.isNotEmpty) ...[
                      const SizedBox(height: 14),
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.symmetric(
                          horizontal: 12,
                          vertical: 10,
                        ),
                        decoration: BoxDecoration(
                          color: _danger.withValues(alpha: .08),
                          borderRadius: BorderRadius.circular(6),
                          border: Border.all(
                            color: _danger.withValues(alpha: .35),
                          ),
                        ),
                        child: Text(
                          error,
                          style: const TextStyle(
                            color: _danger,
                            fontSize: 13,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                      ),
                    ],
                    const SizedBox(height: 28),
                    SizedBox(
                      width: double.infinity,
                      height: 58,
                      child: FilledButton(
                        onPressed: busy ? null : onSubmit,
                        style: FilledButton.styleFrom(
                          backgroundColor: _blue,
                          foregroundColor: Colors.white,
                          disabledBackgroundColor: _textSecondary,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                            side: const BorderSide(color: _navy2),
                          ),
                        ),
                        child: busy
                            ? const SizedBox(
                                width: 18,
                                height: 18,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                  color: Colors.white,
                                ),
                              )
                            : const Text(
                                'Entrar',
                                style: TextStyle(
                                  fontWeight: FontWeight.w900,
                                  fontSize: 15,
                                ),
                              ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _PdvLoginField extends StatelessWidget {
  const _PdvLoginField({
    required this.label,
    required this.controller,
    this.keyboardType,
    this.obscureText = false,
    this.textInputAction,
    this.enabled = true,
    this.onSubmitted,
  });

  final String label;
  final TextEditingController controller;
  final TextInputType? keyboardType;
  final bool obscureText;
  final TextInputAction? textInputAction;
  final bool enabled;
  final ValueChanged<String>? onSubmitted;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            color: _textSecondary,
            fontSize: 13,
            fontWeight: FontWeight.w900,
          ),
        ),
        const SizedBox(height: 8),
        SizedBox(
          height: 48,
          child: TextField(
            controller: controller,
            enabled: enabled,
            keyboardType: keyboardType,
            obscureText: obscureText,
            textInputAction: textInputAction,
            onSubmitted: onSubmitted,
            style: const TextStyle(
              color: _navy,
              fontSize: 16,
              fontWeight: FontWeight.w700,
            ),
            decoration: InputDecoration(
              isDense: true,
              filled: true,
              fillColor: Colors.white,
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 14,
                vertical: 14,
              ),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(7),
                borderSide: const BorderSide(color: _borderStrong),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(7),
                borderSide: const BorderSide(color: _borderStrong),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(7),
                borderSide: const BorderSide(color: _navy2, width: 1.4),
              ),
              disabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(7),
                borderSide: const BorderSide(color: _line),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, required this.store});

  final BalcaoStore store;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _paper,
      body: SafeArea(child: _WindowsPdvHome(store: widget.store)),
    );
  }
}

class _WindowsPdvHome extends StatefulWidget {
  const _WindowsPdvHome({required this.store});

  final BalcaoStore store;

  @override
  State<_WindowsPdvHome> createState() => _WindowsPdvHomeState();
}

class _ModuleDialogSpec {
  const _ModuleDialogSpec({
    required this.width,
    required this.height,
    required this.icon,
  });

  final double width;
  final double height;
  final IconData icon;
}

class _WindowsPdvHomeState extends State<_WindowsPdvHome> {
  final code = TextEditingController();
  final quantity = TextEditingController(text: '1');
  int mode = 0;
  int mobileStep = 0;
  int closedPage = 0;
  String comandaFilter = 'Todas';
  String kitchenFilter = 'Todas';
  String deliveryFilter = 'Todos';

  @override
  void dispose() {
    code.dispose();
    quantity.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final store = widget.store;
    final order = store.selectedOrder;
    return Focus(
      autofocus: true,
      onKeyEvent: _handleShortcut,
      child: LayoutBuilder(
        builder: (context, constraints) {
          final desktop = constraints.maxWidth >= 980;
          if (desktop) {
            return _OnlinePdvDesktopShell(
              store: store,
              selectedMode: mode,
              selectedClosedPage: closedPage,
              selectedOrder: order,
              code: code,
              quantity: quantity,
              comandaFilter: comandaFilter,
              kitchenFilter: kitchenFilter,
              deliveryFilter: deliveryFilter,
              onModeChanged: (value) => setState(() => mode = value),
              onClosedPageChanged: (value) =>
                  setState(() => closedPage = value),
              onComandaFilterChanged: (value) =>
                  setState(() => comandaFilter = value),
              onKitchenFilterChanged: (value) =>
                  setState(() => kitchenFilter = value),
              onDeliveryFilterChanged: (value) =>
                  setState(() => deliveryFilter = value),
              openModule: _openModule,
              openProductSearch: _openProductSearch,
              toggleCash: _handleCashToggle,
              onNewDelivery: _newDelivery,
              onSubmitCode: _includeByCode,
              onIncludeProduct: _includeProduct,
              onReports: _openReports,
            );
          }

          return _WpfMobileShell(
            store: store,
            selectedMode: mode,
            selectedOrder: order,
            selectedStep: mobileStep,
            onModeChanged: (value) => setState(() {
              mode = value;
              mobileStep = 0;
            }),
            onStepChanged: (value) => setState(() => mobileStep = value),
            onIncludeProduct: _includeProduct,
            openModule: _openModule,
            toggleCash: _handleCashToggle,
            onNewDelivery: _newDelivery,
          );
        },
      ),
    );
  }

  KeyEventResult _handleShortcut(FocusNode node, KeyEvent event) {
    if (event is! KeyDownEvent) return KeyEventResult.ignored;
    if (event.logicalKey == LogicalKeyboardKey.f3) {
      _openProductSearch();
      return KeyEventResult.handled;
    }
    if (event.logicalKey == LogicalKeyboardKey.f5) {
      unawaited(widget.store.closeSelected('Dinheiro'));
      return KeyEventResult.handled;
    }
    if (event.logicalKey == LogicalKeyboardKey.f9) {
      if (widget.store.pointHasPending) {
        unawaited(widget.store.confirmPointPayment());
      } else {
        unawaited(widget.store.closeSelected('Dinheiro'));
      }
      return KeyEventResult.handled;
    }
    return KeyEventResult.ignored;
  }

  Future<void> _includeByCode() async {
    final query = code.text.trim().toLowerCase();
    if (query.isEmpty) return;
    final product = widget.store.products.where((item) {
      return item.active &&
          (item.code.toLowerCase() == query ||
              item.name.toLowerCase().contains(query));
    }).firstOrNull;
    if (product == null) {
      await widget.store.setSearch(code.text);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Produto nao encontrado no catalogo.')),
        );
      }
      return;
    }
    final qty = int.tryParse(quantity.text.trim()) ?? 1;
    await widget.store.addProduct(product, quantity: qty);
    code.clear();
  }

  Future<void> _includeProduct(Product product) async {
    final qty = int.tryParse(quantity.text.trim()) ?? 1;
    await widget.store.addProduct(product, quantity: qty);
  }

  void _newDelivery() {
    setState(() => mode = 2);
    _openModule(
      'Novo pedido delivery / retirada',
      _NewDeliveryOrderModule(store: widget.store),
    );
  }

  void _openProductSearch() {
    _openModule(
      'Pesquisa de produtos',
      _ProductSearchModule(store: widget.store, onSelect: _includeProduct),
    );
  }

  void _openReports() {
    _openModule('Relatorios e BI', _ReportsDeskModule(store: widget.store));
  }

  void _handleCashToggle() {
    if (widget.store.cashReconciliationRequired) {
      unawaited(_openCashReconciliation());
      return;
    }
    if (widget.store.cashOpen && widget.store.openOrders.isNotEmpty) {
      _openModule(
        'Fechamento bloqueado',
        _CashCloseBlockedModule(store: widget.store),
      );
      return;
    }
    if (!widget.store.cashOpen) {
      unawaited(_openCash());
      return;
    }
    widget.store.toggleCash();
  }

  Future<void> _openCash() async {
    final opened = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (dialogContext) => _CashOpenDialog(store: widget.store),
    );
    if (!mounted || opened != true) return;
    setState(() {
      mode = 0;
      mobileStep = 0;
      comandaFilter = 'Todas';
      closedPage = 0;
    });
  }

  Future<void> _openCashReconciliation() async {
    final reconciled = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (dialogContext) =>
          _CashReconciliationDialog(store: widget.store),
    );
    if (!mounted || reconciled != true) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'Caixa anterior conferido e fechado. Abra um novo caixa para iniciar as vendas.',
        ),
      ),
    );
  }

  _ModuleDialogSpec _moduleDialogSpec(String title) {
    final lower = title.toLowerCase();
    if (lower.contains('transferir')) {
      return const _ModuleDialogSpec(
        width: 1400,
        height: 934,
        icon: Icons.compare_arrows_rounded,
      );
    }
    if (lower.contains('receber') || lower.contains('pagamento')) {
      return const _ModuleDialogSpec(
        width: 700,
        height: 835,
        icon: Icons.payment_rounded,
      );
    }
    if (lower.contains('novo pedido') || lower.contains('delivery')) {
      return const _ModuleDialogSpec(
        width: 1076,
        height: 936,
        icon: Icons.flag_outlined,
      );
    }
    if (lower.contains('pesquisa')) {
      return const _ModuleDialogSpec(
        width: 1080,
        height: 760,
        icon: Icons.search_rounded,
      );
    }
    if (lower.contains('desconto')) {
      return const _ModuleDialogSpec(
        width: 820,
        height: 720,
        icon: Icons.local_offer_outlined,
      );
    }
    if (lower.contains('cliente')) {
      return const _ModuleDialogSpec(
        width: 1120,
        height: 820,
        icon: Icons.person_outline_rounded,
      );
    }
    if (lower.contains('produto') || lower.contains('catalogo')) {
      return const _ModuleDialogSpec(
        width: 1120,
        height: 820,
        icon: Icons.inventory_2_outlined,
      );
    }
    if (lower.contains('whatsapp')) {
      return const _ModuleDialogSpec(
        width: 980,
        height: 760,
        icon: Icons.phone_outlined,
      );
    }
    if (lower.contains('ifood')) {
      return const _ModuleDialogSpec(
        width: 980,
        height: 760,
        icon: Icons.storefront_outlined,
      );
    }
    if (lower.contains('relatorio') || lower.contains('bi')) {
      return const _ModuleDialogSpec(
        width: 1180,
        height: 820,
        icon: Icons.insert_chart_outlined,
      );
    }
    if (lower.contains('config')) {
      return const _ModuleDialogSpec(
        width: 980,
        height: 760,
        icon: Icons.settings_outlined,
      );
    }
    return const _ModuleDialogSpec(
      width: 1040,
      height: 760,
      icon: Icons.widgets_outlined,
    );
  }

  void _openModule(String title, Widget child) {
    final spec = _moduleDialogSpec(title);
    Widget frame(BuildContext context, {required bool desktop}) {
      return DecoratedBox(
        decoration: BoxDecoration(
          color: _paper,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: const Color(0xFF44413E)),
          boxShadow: const [
            BoxShadow(
              blurRadius: 36,
              offset: Offset(0, 18),
              color: Color(0x38000000),
            ),
          ],
        ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(13),
          child: Material(
            color: _paper,
            child: Column(
              children: [
                Container(
                  height: desktop ? 76 : 64,
                  padding: EdgeInsets.symmetric(horizontal: desktop ? 20 : 14),
                  decoration: const BoxDecoration(
                    color: _rail,
                    border: Border(
                      bottom: BorderSide(color: Color(0xFF3A3836)),
                    ),
                  ),
                  child: Row(
                    children: [
                      Container(
                        width: 40,
                        height: 40,
                        alignment: Alignment.center,
                        decoration: BoxDecoration(
                          color: const Color(0xFF24211F),
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(color: _blue),
                        ),
                        child: Icon(spec.icon, color: _blue, size: 21),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          title,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.w900,
                            fontSize: desktop ? 20 : 17,
                            height: 1.05,
                          ),
                        ),
                      ),
                      const SizedBox(width: 10),
                      SizedBox(
                        width: 40,
                        height: 36,
                        child: FilledButton(
                          onPressed: () => Navigator.pop(context),
                          style: FilledButton.styleFrom(
                            padding: EdgeInsets.zero,
                            backgroundColor: Colors.transparent,
                            foregroundColor: Colors.white,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(8),
                            ),
                          ),
                          child: const Icon(Icons.close_rounded, size: 20),
                        ),
                      ),
                    ],
                  ),
                ),
                Expanded(
                  child: ColoredBox(color: _paper, child: child),
                ),
              ],
            ),
          ),
        ),
      );
    }

    final size = MediaQuery.sizeOf(context);
    final desktop = size.width >= 700;
    final inset = desktop ? 16.0 : 8.0;
    final topInset = desktop ? 52.0 : 8.0;
    final widthLimit = desktop ? spec.width : 520.0;
    final heightLimit = desktop ? spec.height : size.height - (inset * 2);
    final width = math.min(size.width - (inset * 2), widthLimit);
    final height = desktop
        ? math.min(size.height - topInset - inset, heightLimit)
        : size.height - (inset * 2);

    showDialog<void>(
      context: context,
      barrierColor: Colors.black.withValues(alpha: .34),
      builder: (context) => Material(
        color: Colors.transparent,
        child: SafeArea(
          child: Align(
            alignment: desktop ? Alignment.topCenter : Alignment.center,
            child: Padding(
              padding: EdgeInsets.fromLTRB(inset, topInset, inset, inset),
              child: SizedBox(
                width: width,
                height: height,
                child: frame(context, desktop: desktop),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _WpfMobileShell extends StatelessWidget {
  const _WpfMobileShell({
    required this.store,
    required this.selectedMode,
    required this.selectedOrder,
    required this.selectedStep,
    required this.onModeChanged,
    required this.onStepChanged,
    required this.onIncludeProduct,
    required this.openModule,
    required this.toggleCash,
    required this.onNewDelivery,
  });

  final BalcaoStore store;
  final int selectedMode;
  final Order? selectedOrder;
  final int selectedStep;
  final ValueChanged<int> onModeChanged;
  final ValueChanged<int> onStepChanged;
  final ValueChanged<Product> onIncludeProduct;
  final void Function(String title, Widget child) openModule;
  final VoidCallback toggleCash;
  final VoidCallback onNewDelivery;

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: _paper,
      child: SafeArea(
        child: Column(
          children: [
            _MobilePdvTopBar(
              store: store,
              toggleCash: toggleCash,
              onMore: () => openModule(
                'Central rápida',
                _QuickHubModule(store: store, openModule: openModule),
              ),
            ),
            if (store.cashOpen)
              _MobileAreaNavigation(
                selected: selectedMode,
                onChanged: onModeChanged,
                onNewDelivery: onNewDelivery,
              ),
            Expanded(
              child: !store.cashOpen
                  ? _MobileClosedCashPage(
                      store: store,
                      onOpen: toggleCash,
                      openModule: openModule,
                    )
                  : switch (selectedMode) {
                      1 => _KitchenDesk(store: store),
                      2 => _DeliveryDesk(store: store),
                      _ => _MobileComandaFlow(
                        store: store,
                        order: selectedOrder,
                        selectedStep: selectedStep,
                        onStepChanged: onStepChanged,
                        onIncludeProduct: onIncludeProduct,
                        onReceive: () => openModule(
                          'Receber pagamento',
                          _CashModule(
                            store: store,
                            openBlocked: () => openModule(
                              'Fechamento bloqueado',
                              _CashCloseBlockedModule(store: store),
                            ),
                          ),
                        ),
                      ),
                    },
            ),
          ],
        ),
      ),
    );
  }
}

class _MobilePdvTopBar extends StatelessWidget {
  const _MobilePdvTopBar({
    required this.store,
    required this.toggleCash,
    required this.onMore,
  });

  final BalcaoStore store;
  final VoidCallback toggleCash;
  final VoidCallback onMore;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 66,
      padding: const EdgeInsets.symmetric(horizontal: 12),
      color: _rail,
      child: Row(
        children: [
          Container(
            width: 38,
            height: 38,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: _surface,
              borderRadius: BorderRadius.circular(5),
            ),
            child: const Text(
              'BL',
              style: TextStyle(
                color: _navy,
                fontSize: 16,
                fontWeight: FontWeight.w900,
              ),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  store.businessName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 14,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 2),
                Row(
                  children: [
                    Container(
                      width: 7,
                      height: 7,
                      decoration: BoxDecoration(
                        color: store.onlineStoreOpen
                            ? _teal
                            : const Color(0xFFF34B53),
                        shape: BoxShape.circle,
                      ),
                    ),
                    const SizedBox(width: 5),
                    Text(
                      store.onlineStoreOpen ? 'Online' : 'Offline',
                      style: const TextStyle(
                        color: Color(0xFFC9C1BA),
                        fontSize: 10,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          InkWell(
            onTap: toggleCash,
            borderRadius: BorderRadius.circular(9),
            child: Container(
              height: 40,
              padding: const EdgeInsets.symmetric(horizontal: 10),
              decoration: BoxDecoration(
                color: const Color(0xFF292929),
                borderRadius: BorderRadius.circular(9),
                border: Border.all(color: const Color(0xFF44413E)),
              ),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    store.cashOpen ? 'Caixa aberto' : 'Caixa fechado',
                    style: TextStyle(
                      color: store.cashOpen
                          ? const Color(0xFFEAE5E0)
                          : const Color(0xFFFF747A),
                      fontSize: 9,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  Text(
                    money(store.openTotal),
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 12,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ],
              ),
            ),
          ),
          IconButton(
            key: const Key('mobileMore'),
            onPressed: onMore,
            icon: const Icon(Icons.more_vert_rounded),
            color: Colors.white,
            tooltip: 'Mais opções',
          ),
        ],
      ),
    );
  }
}

class _MobileAreaNavigation extends StatelessWidget {
  const _MobileAreaNavigation({
    required this.selected,
    required this.onChanged,
    required this.onNewDelivery,
  });

  final int selected;
  final ValueChanged<int> onChanged;
  final VoidCallback onNewDelivery;

  @override
  Widget build(BuildContext context) {
    const entries = [
      ('Comanda', Icons.receipt_long_outlined),
      ('Cozinha', Icons.kitchen_outlined),
      ('Delivery', Icons.delivery_dining_outlined),
    ];
    return Container(
      height: 58,
      padding: const EdgeInsets.fromLTRB(8, 7, 8, 6),
      decoration: const BoxDecoration(
        color: _surface,
        border: Border(bottom: BorderSide(color: _line)),
      ),
      child: Row(
        children: [
          for (var index = 0; index < entries.length; index++)
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 2),
                child: Material(
                  color: selected == index ? _blue : Colors.transparent,
                  borderRadius: BorderRadius.circular(9),
                  child: InkWell(
                    onTap: () => onChanged(index),
                    borderRadius: BorderRadius.circular(9),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          entries[index].$2,
                          size: 19,
                          color: selected == index ? Colors.white : _navy,
                        ),
                        const SizedBox(height: 2),
                        Text(
                          entries[index].$1,
                          style: TextStyle(
                            color: selected == index ? Colors.white : _navy,
                            fontSize: 10,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          SizedBox(
            width: 40,
            child: IconButton(
              onPressed: onNewDelivery,
              icon: const Icon(Icons.add_rounded),
              color: _blue,
              tooltip: 'Novo delivery',
            ),
          ),
        ],
      ),
    );
  }
}

class _MobileComandaFlow extends StatelessWidget {
  const _MobileComandaFlow({
    required this.store,
    required this.order,
    required this.selectedStep,
    required this.onStepChanged,
    required this.onIncludeProduct,
    required this.onReceive,
  });

  final BalcaoStore store;
  final Order? order;
  final int selectedStep;
  final ValueChanged<int> onStepChanged;
  final ValueChanged<Product> onIncludeProduct;
  final VoidCallback onReceive;

  @override
  Widget build(BuildContext context) {
    final effectiveStep = order == null ? 0 : selectedStep;
    return Column(
      children: [
        _MobileOrderSteps(
          selected: effectiveStep,
          enabledUntil: order == null ? 0 : 2,
          onChanged: onStepChanged,
        ),
        Expanded(
          child: switch (effectiveStep) {
            1 => _MobileProductsPage(
              store: store,
              onIncludeProduct: onIncludeProduct,
              onContinue: () => onStepChanged(2),
            ),
            2 => _MobileAccountPage(
              store: store,
              order: order!,
              onBack: () => onStepChanged(1),
              onReceive: onReceive,
            ),
            _ => _MobileTablesPage(
              store: store,
              onSelected: () => onStepChanged(1),
            ),
          },
        ),
      ],
    );
  }
}

class _MobileOrderSteps extends StatelessWidget {
  const _MobileOrderSteps({
    required this.selected,
    required this.enabledUntil,
    required this.onChanged,
  });

  final int selected;
  final int enabledUntil;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) {
    const labels = ['Mesas', 'Produtos', 'Conta'];
    return Container(
      height: 50,
      padding: const EdgeInsets.symmetric(horizontal: 12),
      color: _surface,
      child: Row(
        children: [
          for (var index = 0; index < labels.length; index++) ...[
            Expanded(
              child: InkWell(
                onTap: index <= enabledUntil ? () => onChanged(index) : null,
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    Text(
                      labels[index],
                      style: TextStyle(
                        color: index == selected
                            ? _blue
                            : index <= enabledUntil
                            ? _navy
                            : const Color(0xFFB6ADA5),
                        fontSize: 12,
                        fontWeight: index == selected
                            ? FontWeight.w900
                            : FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 9),
                    Container(
                      height: 3,
                      decoration: BoxDecoration(
                        color: index == selected ? _blue : Colors.transparent,
                        borderRadius: BorderRadius.circular(3),
                      ),
                    ),
                  ],
                ),
              ),
            ),
            if (index < labels.length - 1)
              const Icon(
                Icons.chevron_right_rounded,
                size: 17,
                color: _textMuted,
              ),
          ],
        ],
      ),
    );
  }
}

class _MobileTablesPage extends StatelessWidget {
  const _MobileTablesPage({required this.store, required this.onSelected});

  final BalcaoStore store;
  final VoidCallback onSelected;

  @override
  Widget build(BuildContext context) {
    final tableOrders = store.openOrders
        .where((item) => item.kind == OrderKind.table && item.isOpen)
        .toList();
    final visibleSlots = math.max(12, tableOrders.length);
    final tables = List.generate(visibleSlots, (index) {
      final number = (index + 1).toString().padLeft(6, '0');
      final order = tableOrders
          .where((candidate) => _wpfMesaNumber(candidate.number) == number)
          .firstOrNull;
      return (number: number, order: order);
    });
    return CustomScrollView(
      slivers: [
        SliverPadding(
          padding: const EdgeInsets.fromLTRB(14, 16, 14, 8),
          sliver: SliverToBoxAdapter(
            child: Row(
              children: [
                const Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Mesas / comandas',
                        style: TextStyle(
                          color: _navy,
                          fontSize: 21,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      SizedBox(height: 3),
                      Text(
                        'Toque em uma mesa para lançar o pedido.',
                        style: TextStyle(color: _textSecondary, fontSize: 12),
                      ),
                    ],
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: _surface,
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: _line),
                  ),
                  child: Text(
                    '${tables.length} mesas',
                    style: const TextStyle(
                      color: _navy,
                      fontSize: 11,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
        if (tables.isEmpty)
          const SliverFillRemaining(
            hasScrollBody: false,
            child: _Empty(text: 'Nenhuma mesa disponível.'),
          )
        else
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(14, 8, 14, 18),
            sliver: SliverGrid.builder(
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                mainAxisSpacing: 10,
                crossAxisSpacing: 10,
                childAspectRatio: 1.08,
              ),
              itemCount: tables.length,
              itemBuilder: (context, index) {
                final slot = tables[index];
                final table = slot.order;
                final free = table == null || table.items.isEmpty;
                final accountRequested =
                    !free && table.paymentMethod.trim().isNotEmpty;
                final accent = accountRequested
                    ? const Color(0xFFC89443)
                    : free
                    ? const Color(0xFF79D878)
                    : const Color(0xFFFF6F65);
                final elapsed = table == null
                    ? Duration.zero
                    : DateTime.now().difference(table.createdAt);
                final elapsedLabel =
                    '${elapsed.inMinutes.toString().padLeft(2, '0')}:'
                    '${(elapsed.inSeconds % 60).toString().padLeft(2, '0')}';
                return Material(
                  color: _surface,
                  borderRadius: BorderRadius.circular(12),
                  child: InkWell(
                    onTap: () async {
                      if (table == null) {
                        await store.openOrder(
                          OrderKind.table,
                          number: slot.number,
                        );
                      } else {
                        await store.selectOrder(table.id);
                      }
                      onSelected();
                    },
                    borderRadius: BorderRadius.circular(12),
                    child: Ink(
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(
                          color:
                              table != null && store.selectedOrderId == table.id
                              ? _blue
                              : _line,
                          width:
                              table != null && store.selectedOrderId == table.id
                              ? 2
                              : 1,
                        ),
                      ),
                      child: Row(
                        children: [
                          Container(
                            width: 5,
                            decoration: BoxDecoration(
                              color: accent,
                              borderRadius: const BorderRadius.horizontal(
                                left: Radius.circular(11),
                              ),
                            ),
                          ),
                          Expanded(
                            child: Padding(
                              padding: const EdgeInsets.all(11),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    children: [
                                      Expanded(
                                        child: Text(
                                          slot.number,
                                          maxLines: 1,
                                          style: const TextStyle(
                                            color: _navy,
                                            fontSize: 20,
                                            height: 1,
                                            fontWeight: FontWeight.w900,
                                          ),
                                        ),
                                      ),
                                      const Icon(
                                        Icons.table_restaurant_outlined,
                                        size: 21,
                                        color: _textSecondary,
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 7),
                                  Row(
                                    children: [
                                      const Icon(
                                        Icons.groups_2_outlined,
                                        size: 13,
                                        color: _danger,
                                      ),
                                      const SizedBox(width: 3),
                                      const Text(
                                        '1 pessoa',
                                        style: TextStyle(
                                          color: _textSecondary,
                                          fontSize: 9,
                                          fontWeight: FontWeight.w700,
                                        ),
                                      ),
                                      const SizedBox(width: 7),
                                      const Icon(
                                        Icons.room_service_outlined,
                                        size: 13,
                                        color: _danger,
                                      ),
                                      const SizedBox(width: 3),
                                      Expanded(
                                        child: Text(
                                          'Garçom ${table?.waiter ?? '2'}',
                                          maxLines: 1,
                                          overflow: TextOverflow.ellipsis,
                                          style: const TextStyle(
                                            color: _textSecondary,
                                            fontSize: 9,
                                            fontWeight: FontWeight.w700,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                                  if (table != null &&
                                      table.customerName.trim().isNotEmpty) ...[
                                    const SizedBox(height: 5),
                                    Text(
                                      table.customerName.trim().toUpperCase(),
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      style: const TextStyle(
                                        color: _navy,
                                        fontSize: 10,
                                        fontWeight: FontWeight.w900,
                                      ),
                                    ),
                                  ],
                                  const Spacer(),
                                  if (!free) ...[
                                    Text(
                                      elapsedLabel,
                                      style: const TextStyle(
                                        color: Color(0xFFA66A00),
                                        fontSize: 10,
                                        fontWeight: FontWeight.w900,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                  ],
                                  Row(
                                    children: [
                                      Container(
                                        padding: const EdgeInsets.symmetric(
                                          horizontal: 7,
                                          vertical: 3,
                                        ),
                                        decoration: BoxDecoration(
                                          color: accent.withValues(alpha: .18),
                                          borderRadius: BorderRadius.circular(
                                            4,
                                          ),
                                        ),
                                        child: Text(
                                          accountRequested
                                              ? 'CONTA'
                                              : free
                                              ? 'LIVRE'
                                              : 'OCUPADA',
                                          style: TextStyle(
                                            color: accountRequested
                                                ? const Color(0xFF8B5B00)
                                                : free
                                                ? const Color(0xFF237A39)
                                                : const Color(0xFFB22B26),
                                            fontSize: 9,
                                            fontWeight: FontWeight.w900,
                                          ),
                                        ),
                                      ),
                                      const SizedBox(width: 4),
                                      Expanded(
                                        child: Text(
                                          money(table?.subtotal ?? 0),
                                          maxLines: 1,
                                          overflow: TextOverflow.ellipsis,
                                          textAlign: TextAlign.right,
                                          style: const TextStyle(
                                            color: _navy,
                                            fontSize: 11,
                                            fontWeight: FontWeight.w900,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                );
              },
            ),
          ),
      ],
    );
  }
}

class _MobileProductsPage extends StatefulWidget {
  const _MobileProductsPage({
    required this.store,
    required this.onIncludeProduct,
    required this.onContinue,
  });

  final BalcaoStore store;
  final ValueChanged<Product> onIncludeProduct;
  final VoidCallback onContinue;

  @override
  State<_MobileProductsPage> createState() => _MobileProductsPageState();
}

class _MobileProductsPageState extends State<_MobileProductsPage> {
  String query = '';
  String category = 'Todos';

  @override
  Widget build(BuildContext context) {
    final categories = <String>{
      'Todos',
      ...widget.store.products
          .where((item) => item.active)
          .map((item) => item.category),
    }.toList();
    final normalized = query.trim().toLowerCase();
    final products = widget.store.products.where((item) {
      if (!item.active) return false;
      if (category != 'Todos' && item.category != category) return false;
      return normalized.isEmpty ||
          item.name.toLowerCase().contains(normalized) ||
          item.code.toLowerCase().contains(normalized);
    }).toList();
    final order = widget.store.selectedOrder;

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 12, 12, 8),
          child: TextField(
            onChanged: (value) => setState(() => query = value),
            decoration: const InputDecoration(
              hintText: 'Buscar produto ou código',
              prefixIcon: Icon(Icons.search_rounded),
              isDense: true,
            ),
          ),
        ),
        SizedBox(
          height: 40,
          child: ListView.separated(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            scrollDirection: Axis.horizontal,
            itemCount: categories.length,
            separatorBuilder: (context, index) => const SizedBox(width: 6),
            itemBuilder: (context, index) {
              final value = categories[index];
              final selected = value == category;
              return ChoiceChip(
                selected: selected,
                onSelected: (_) => setState(() => category = value),
                label: Text(value),
                selectedColor: _blue,
                backgroundColor: _surface,
                side: BorderSide(color: selected ? _blue : _line),
                labelStyle: TextStyle(
                  color: selected ? Colors.white : _navy,
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                ),
              );
            },
          ),
        ),
        const SizedBox(height: 8),
        Expanded(
          child: GridView.builder(
            padding: const EdgeInsets.fromLTRB(12, 0, 12, 10),
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 2,
              mainAxisSpacing: 9,
              crossAxisSpacing: 9,
              childAspectRatio: .93,
            ),
            itemCount: products.length,
            itemBuilder: (context, index) {
              final product = products[index];
              return Material(
                color: _surface,
                borderRadius: BorderRadius.circular(12),
                child: InkWell(
                  onTap: () async {
                    widget.onIncludeProduct(product);
                    await Future<void>.delayed(
                      const Duration(milliseconds: 20),
                    );
                    if (mounted) setState(() {});
                  },
                  borderRadius: BorderRadius.circular(12),
                  child: Ink(
                    padding: const EdgeInsets.all(11),
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: _line),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          width: 40,
                          height: 40,
                          alignment: Alignment.center,
                          decoration: BoxDecoration(
                            color: _mint,
                            borderRadius: BorderRadius.circular(10),
                          ),
                          child: const Icon(
                            Icons.restaurant_menu_rounded,
                            color: _blue,
                            size: 22,
                          ),
                        ),
                        const Spacer(),
                        Text(
                          product.name,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: _navy,
                            fontSize: 13,
                            height: 1.12,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          product.category,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: _textMuted,
                            fontSize: 10,
                          ),
                        ),
                        const SizedBox(height: 7),
                        Row(
                          children: [
                            Text(
                              money(product.price),
                              style: const TextStyle(
                                color: _blue,
                                fontSize: 14,
                                fontWeight: FontWeight.w900,
                              ),
                            ),
                            const Spacer(),
                            Container(
                              width: 28,
                              height: 28,
                              alignment: Alignment.center,
                              decoration: const BoxDecoration(
                                color: _blue,
                                shape: BoxShape.circle,
                              ),
                              child: const Icon(
                                Icons.add_rounded,
                                color: Colors.white,
                                size: 19,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              );
            },
          ),
        ),
        Container(
          padding: const EdgeInsets.fromLTRB(12, 9, 12, 10),
          decoration: const BoxDecoration(
            color: _surface,
            border: Border(top: BorderSide(color: _line)),
          ),
          child: SafeArea(
            top: false,
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${order?.itemsCount ?? 0} produto(s) adicionado(s)',
                        style: const TextStyle(
                          color: _navy,
                          fontSize: 12,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      Text(
                        money(order?.subtotal ?? 0),
                        style: const TextStyle(
                          color: _textSecondary,
                          fontSize: 11,
                        ),
                      ),
                    ],
                  ),
                ),
                FilledButton(
                  onPressed: (order?.items.isNotEmpty ?? false)
                      ? widget.onContinue
                      : null,
                  child: const Text('Continuar para conta'),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _MobileAccountPage extends StatelessWidget {
  const _MobileAccountPage({
    required this.store,
    required this.order,
    required this.onBack,
    required this.onReceive,
  });

  final BalcaoStore store;
  final Order order;
  final VoidCallback onBack;
  final VoidCallback onReceive;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(12),
            children: [
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: _rail,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Row(
                  children: [
                    Container(
                      width: 42,
                      height: 42,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(
                        color: const Color(0xFF2B2B2B),
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(color: const Color(0xFF494542)),
                      ),
                      child: const Icon(
                        Icons.receipt_long_outlined,
                        color: _blue,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Mesa ${order.number.replaceAll(RegExp(r'\D'), '')}',
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 18,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                          Text(
                            '${order.itemsCount} item(ns) • Garçom ${order.waiter}',
                            style: const TextStyle(
                              color: Color(0xFFC9C1BA),
                              fontSize: 11,
                            ),
                          ),
                        ],
                      ),
                    ),
                    Text(
                      money(order.subtotal),
                      style: const TextStyle(
                        color: _blue,
                        fontSize: 18,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 10),
              Container(
                decoration: BoxDecoration(
                  color: _surface,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: _line),
                ),
                child: Column(
                  children: [
                    for (final item in order.items)
                      Padding(
                        padding: const EdgeInsets.fromLTRB(12, 10, 8, 10),
                        child: Row(
                          children: [
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    item.name,
                                    style: const TextStyle(
                                      color: _navy,
                                      fontSize: 13,
                                      fontWeight: FontWeight.w800,
                                    ),
                                  ),
                                  Text(
                                    '${item.quantity} × ${money(item.price)}',
                                    style: const TextStyle(
                                      color: _textSecondary,
                                      fontSize: 11,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            Text(
                              money(item.total),
                              style: const TextStyle(
                                color: _navy,
                                fontSize: 12,
                                fontWeight: FontWeight.w900,
                              ),
                            ),
                            IconButton(
                              onPressed: () =>
                                  unawaited(store.changeQty(item, -1)),
                              icon: const Icon(Icons.remove_circle_outline),
                              color: _danger,
                              tooltip: 'Remover um',
                            ),
                          ],
                        ),
                      ),
                  ],
                ),
              ),
              const SizedBox(height: 10),
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: _surface,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: _line),
                ),
                child: Column(
                  children: [
                    _MobileTotalLine(
                      label: 'Produtos',
                      value: money(order.itemsTotal),
                    ),
                    const SizedBox(height: 8),
                    _MobileTotalLine(
                      label:
                          'Serviço (${order.servicePercent.toStringAsFixed(0)}%)',
                      value: money(order.serviceAmount),
                    ),
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 12),
                      child: Divider(height: 1),
                    ),
                    _MobileTotalLine(
                      label: 'Total da conta',
                      value: money(order.subtotal),
                      strong: true,
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 10),
              const Text(
                'Forma de pagamento',
                style: TextStyle(
                  color: _navy,
                  fontSize: 14,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 7,
                runSpacing: 7,
                children: store.paymentMethods
                    .map(
                      (method) => Chip(
                        avatar: Icon(
                          method.contains('Pix')
                              ? Icons.qr_code_rounded
                              : method.contains('Point')
                              ? Icons.contactless_outlined
                              : Icons.payments_outlined,
                          color: _blue,
                          size: 17,
                        ),
                        label: Text(method),
                        backgroundColor: _surface,
                        side: const BorderSide(color: _line),
                        labelStyle: const TextStyle(
                          color: _navy,
                          fontSize: 11,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    )
                    .toList(),
              ),
            ],
          ),
        ),
        Container(
          padding: const EdgeInsets.fromLTRB(12, 9, 12, 10),
          decoration: const BoxDecoration(
            color: _surface,
            border: Border(top: BorderSide(color: _line)),
          ),
          child: SafeArea(
            top: false,
            child: Row(
              children: [
                OutlinedButton(
                  onPressed: onBack,
                  child: const Text('Continuar lançando'),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: FilledButton.icon(
                    onPressed: onReceive,
                    icon: const Icon(Icons.payment_rounded),
                    label: const Text('Receber conta'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _MobileTotalLine extends StatelessWidget {
  const _MobileTotalLine({
    required this.label,
    required this.value,
    this.strong = false,
  });

  final String label;
  final String value;
  final bool strong;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Text(
            label,
            style: TextStyle(
              color: strong ? _navy : _textSecondary,
              fontSize: strong ? 14 : 12,
              fontWeight: strong ? FontWeight.w900 : FontWeight.w600,
            ),
          ),
        ),
        Text(
          value,
          style: TextStyle(
            color: strong ? _blue : _navy,
            fontSize: strong ? 18 : 12,
            fontWeight: FontWeight.w900,
          ),
        ),
      ],
    );
  }
}

class _MobileClosedCashPage extends StatelessWidget {
  const _MobileClosedCashPage({
    required this.store,
    required this.onOpen,
    required this.openModule,
  });

  final BalcaoStore store;
  final VoidCallback onOpen;
  final void Function(String title, Widget child) openModule;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Container(
          height: 52,
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
          decoration: const BoxDecoration(
            color: _surface,
            border: Border(bottom: BorderSide(color: _line)),
          ),
          child: ListView(
            scrollDirection: Axis.horizontal,
            children: [
              _MobileClosedNavButton(
                label: 'Painel',
                icon: Icons.area_chart_outlined,
                onTap: () {},
                active: true,
              ),
              _MobileClosedNavButton(
                label: 'Relatórios',
                icon: Icons.query_stats_outlined,
                onTap: () =>
                    openModule('Relatórios', _ReportsDeskModule(store: store)),
              ),
              _MobileClosedNavButton(
                key: const Key('mobileClosedStock'),
                label: 'Estoque',
                icon: Icons.inventory_2_outlined,
                onTap: () => openModule(
                  'Controle de estoque',
                  _StockModule(store: store),
                ),
              ),
              _MobileClosedNavButton(
                label: 'Caixa',
                icon: Icons.credit_card_outlined,
                onTap: () => openModule(
                  'Caixa: entradas, retiradas e fechamento',
                  _CashModule(
                    store: store,
                    openBlocked: () => openModule(
                      'Fechamento bloqueado',
                      _CashCloseBlockedModule(store: store),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
        Expanded(
          child: _ClosedCashDashboard(
            store: store,
            onOpen: onOpen,
            openModule: openModule,
          ),
        ),
      ],
    );
  }
}

class _MobileClosedNavButton extends StatelessWidget {
  const _MobileClosedNavButton({
    super.key,
    required this.label,
    required this.icon,
    required this.onTap,
    this.active = false,
  });

  final String label;
  final IconData icon;
  final VoidCallback onTap;
  final bool active;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(right: 6),
      child: TextButton.icon(
        onPressed: onTap,
        icon: Icon(icon, size: 17),
        label: Text(label),
        style: TextButton.styleFrom(
          foregroundColor: active ? Colors.white : _navy,
          backgroundColor: active ? _blue : const Color(0xFFFFF8F3),
          side: BorderSide(color: active ? _blue : _line),
        ),
      ),
    );
  }
}

class _OnlinePdvDesktopShell extends StatelessWidget {
  const _OnlinePdvDesktopShell({
    required this.store,
    required this.selectedMode,
    required this.selectedClosedPage,
    required this.selectedOrder,
    required this.code,
    required this.quantity,
    required this.comandaFilter,
    required this.kitchenFilter,
    required this.deliveryFilter,
    required this.onModeChanged,
    required this.onClosedPageChanged,
    required this.onComandaFilterChanged,
    required this.onKitchenFilterChanged,
    required this.onDeliveryFilterChanged,
    required this.openModule,
    required this.openProductSearch,
    required this.toggleCash,
    required this.onNewDelivery,
    required this.onSubmitCode,
    required this.onIncludeProduct,
    required this.onReports,
  });

  final BalcaoStore store;
  final int selectedMode;
  final int selectedClosedPage;
  final Order? selectedOrder;
  final TextEditingController code;
  final TextEditingController quantity;
  final String comandaFilter;
  final String kitchenFilter;
  final String deliveryFilter;
  final ValueChanged<int> onModeChanged;
  final ValueChanged<int> onClosedPageChanged;
  final ValueChanged<String> onComandaFilterChanged;
  final ValueChanged<String> onKitchenFilterChanged;
  final ValueChanged<String> onDeliveryFilterChanged;
  final void Function(String title, Widget child) openModule;
  final VoidCallback openProductSearch;
  final VoidCallback toggleCash;
  final VoidCallback onNewDelivery;
  final VoidCallback onSubmitCode;
  final ValueChanged<Product> onIncludeProduct;
  final VoidCallback onReports;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final compactRail = constraints.maxWidth < 1320;
        return ColoredBox(
          color: _paper,
          child: Row(
            children: [
              if (store.cashOpen)
                _OnlineSideRail(
                  selectedMode: selectedMode,
                  onModeChanged: onModeChanged,
                  openModule: openModule,
                  store: store,
                  compact: compactRail,
                  comandaFilter: comandaFilter,
                  onComandaFilterChanged: onComandaFilterChanged,
                  kitchenFilter: kitchenFilter,
                  onKitchenFilterChanged: onKitchenFilterChanged,
                  deliveryFilter: deliveryFilter,
                  onDeliveryFilterChanged: onDeliveryFilterChanged,
                  onNewDelivery: onNewDelivery,
                )
              else
                _ClosedCashSideRail(
                  store: store,
                  compact: compactRail,
                  openModule: openModule,
                  selectedPage: selectedClosedPage,
                  onPageChanged: onClosedPageChanged,
                ),
              Expanded(
                child: Column(
                  children: [
                    _PdvTopBar(
                      store: store,
                      openModule: openModule,
                      toggleCash: toggleCash,
                    ),
                    if (selectedMode != 1)
                      _PdvRibbon(
                        store: store,
                        openModule: openModule,
                        openProductSearch: openProductSearch,
                        toggleCash: toggleCash,
                      ),
                    if (!store.cashOpen)
                      _ClosedCashModeBar(
                        store: store,
                        selectedPage: selectedClosedPage,
                      ),
                    Expanded(
                      child: store.cashOpen
                          ? Padding(
                              padding: const EdgeInsets.all(10),
                              child: _buildContent(context),
                            )
                          : switch (selectedClosedPage) {
                              1 => _ReportsDeskModule(store: store),
                              2 => _StockModule(store: store),
                              3 => Padding(
                                padding: const EdgeInsets.all(12),
                                child: _CashModule(
                                  store: store,
                                  openBlocked: () => openModule(
                                    'Fechamento bloqueado',
                                    _CashCloseBlockedModule(store: store),
                                  ),
                                ),
                              ),
                              _ => _ClosedCashDashboard(
                                store: store,
                                onOpen: toggleCash,
                                openModule: openModule,
                              ),
                            },
                    ),
                    _KeyboardFooter(store: store),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildContent(BuildContext context) {
    if (selectedMode == 1) {
      return _KitchenDesk(store: store, filter: kitchenFilter);
    }
    if (selectedMode == 2) {
      return _DeliveryDesk(
        store: store,
        filter: deliveryFilter,
        onFilterChanged: onDeliveryFilterChanged,
      );
    }

    return LayoutBuilder(
      builder: (context, constraints) {
        final boardFlex = constraints.maxWidth >= 1450 ? 61 : 58;
        final commandFlex = 100 - boardFlex;
        return Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Expanded(
              flex: boardFlex,
              child: _WindowsBoardPanel(store: store, filter: comandaFilter),
            ),
            const SizedBox(width: 10),
            Expanded(
              flex: commandFlex,
              child: selectedOrder == null
                  ? _EmptyCommandPanel(store: store)
                  : _WindowsCommandPanel(
                      store: store,
                      order: selectedOrder!,
                      code: code,
                      quantity: quantity,
                      onSubmitCode: onSubmitCode,
                      onOpenProductSearch: openProductSearch,
                      openModule: openModule,
                    ),
            ),
          ],
        );
      },
    );
  }
}

class _ClosedCashModeBar extends StatelessWidget {
  const _ClosedCashModeBar({required this.store, required this.selectedPage});

  final BalcaoStore store;
  final int selectedPage;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 54,
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 6),
      decoration: const BoxDecoration(
        color: _surface,
        border: Border(bottom: BorderSide(color: _line)),
      ),
      child: Row(
        children: [
          Container(
            width: 34,
            height: 34,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: const Color(0xFFFFEDE4),
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Icon(
              Icons.query_stats_outlined,
              color: _blue,
              size: 19,
            ),
          ),
          const SizedBox(width: 12),
          Text(
            switch (selectedPage) {
              1 => 'Relatórios',
              2 => 'Controle de estoque',
              3 => 'Caixa',
              _ => 'Painel do caixa',
            },
            style: const TextStyle(
              color: _navy,
              fontSize: 16,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(width: 10),
          Container(
            width: 6,
            height: 6,
            decoration: const BoxDecoration(
              color: _blue,
              shape: BoxShape.circle,
            ),
          ),
          const SizedBox(width: 8),
          Text(
            switch (selectedPage) {
              1 => 'Visão gerencial',
              2 => 'Reposição e margem',
              3 => 'Histórico e abertura',
              _ => 'Caixa fechado',
            },
            style: const TextStyle(
              color: _textSecondary,
              fontSize: 13,
              fontWeight: FontWeight.w700,
            ),
          ),
          const Spacer(),
          Container(
            width: 314,
            height: 42,
            padding: const EdgeInsets.symmetric(horizontal: 16),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: _line),
            ),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    'Hoje, ${DateTime.now().day} de ${_monthName(DateTime.now().month)} de ${DateTime.now().year}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: _navy,
                      fontSize: 12,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
                const Icon(
                  Icons.arrow_drop_down_rounded,
                  color: _textSecondary,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ClosedCashDashboard extends StatelessWidget {
  const _ClosedCashDashboard({
    required this.store,
    required this.onOpen,
    required this.openModule,
  });

  final BalcaoStore store;
  final VoidCallback onOpen;
  final void Function(String title, Widget child) openModule;

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final startOfToday = DateTime(now.year, now.month, now.day);
    final startOfWeek = startOfToday.subtract(const Duration(days: 6));
    final closed = store.orders
        .where(
          (order) =>
              order.status == OrderStatus.closed &&
              !order.createdAt.isBefore(startOfWeek),
        )
        .toList();
    final todayClosed = closed
        .where((order) => !order.createdAt.isBefore(startOfToday))
        .toList();
    final open = store.openOrders;
    final revenue7 = closed.fold<double>(
      0,
      (sum, order) => sum + order.subtotal,
    );
    final revenueToday = todayClosed.fold<double>(
      0,
      (sum, order) => sum + order.subtotal,
    );
    final profit7 = closed.fold<double>(0, (sum, order) => sum + order.profit);
    final cost7 = closed.fold<double>(0, (sum, order) => sum + order.costTotal);
    final openTotal = open.fold<double>(
      0,
      (sum, order) => sum + order.subtotal,
    );
    final lowStock = store.products
        .where((product) => product.stock <= product.minStock)
        .length;
    final soldItems = closed.fold<int>(
      0,
      (sum, order) => sum + order.itemsCount,
    );
    final revenueByDay = List<double>.generate(7, (index) {
      final day = startOfWeek.add(Duration(days: index));
      final next = day.add(const Duration(days: 1));
      return closed
          .where(
            (order) =>
                !order.createdAt.isBefore(day) &&
                order.createdAt.isBefore(next),
          )
          .fold<double>(0, (sum, order) => sum + order.subtotal);
    });
    final profitByDay = List<double>.generate(7, (index) {
      final day = startOfWeek.add(Duration(days: index));
      final next = day.add(const Duration(days: 1));
      return closed
          .where(
            (order) =>
                !order.createdAt.isBefore(day) &&
                order.createdAt.isBefore(next),
          )
          .fold<double>(0, (sum, order) => sum + order.profit);
    });
    final lastClosing = store.movements
        .where((movement) => movement.type == 'FECHAMENTO')
        .firstOrNull;

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(12, 10, 12, 18),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final desktop = constraints.maxWidth >= 1080;
          final topCards = <Widget>[
            _ClosedCashStatusCard(onOpen: onOpen),
            _ClosedDashboardMetric(
              icon: Icons.trending_up_rounded,
              label: 'Hoje',
              value: money(revenueToday),
              detail: 'faturamento',
              color: _blue,
            ),
            _ClosedDashboardMetric(
              icon: Icons.point_of_sale_outlined,
              label: '7 dias',
              value: money(revenue7),
              detail: 'faturamento',
              color: _blue,
            ),
            _ClosedDashboardMetric(
              icon: Icons.show_chart_rounded,
              label: 'Lucro 7 dias',
              value: money(profit7),
              detail: 'lucro bruto',
              color: _blue,
            ),
            _ClosedDashboardMetric(
              icon: Icons.description_outlined,
              label: 'Em aberto',
              value: money(openTotal),
              detail: '${open.length} aguardando resolução',
              color: const Color(0xFFB36B00),
            ),
          ];
          final summaryCards = [
            _ClosedDashboardSummary(
              icon: Icons.pie_chart_outline_rounded,
              label: 'CMV',
              value: revenue7 <= 0
                  ? '0,00%'
                  : '${((cost7 / revenue7) * 100).toStringAsFixed(2).replaceAll('.', ',')}%',
              detail: 'Custo ${money(cost7)}',
            ),
            _ClosedDashboardSummary(
              icon: Icons.point_of_sale_outlined,
              label: 'Ticket médio',
              value: closed.isEmpty
                  ? money(0)
                  : money(revenue7 / closed.length),
              detail: '${closed.length} fechamento(s)',
            ),
            _ClosedDashboardSummary(
              icon: Icons.shopping_cart_outlined,
              label: 'Itens vendidos',
              value: '$soldItems',
              detail: 'nos últimos 7 dias',
            ),
            _ClosedDashboardSummary(
              icon: Icons.trending_up_rounded,
              label: 'Margem',
              value: revenue7 <= 0
                  ? '0,00%'
                  : '${((profit7 / revenue7) * 100).toStringAsFixed(2).replaceAll('.', ',')}%',
              detail: 'Lucro ${money(profit7)}',
            ),
          ];

          return Column(
            children: [
              if (desktop)
                SizedBox(
                  height: 128,
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Expanded(flex: 11, child: topCards[0]),
                      for (var index = 1; index < topCards.length; index++) ...[
                        const SizedBox(width: 12),
                        Expanded(flex: 10, child: topCards[index]),
                      ],
                    ],
                  ),
                )
              else
                Wrap(
                  spacing: 10,
                  runSpacing: 10,
                  children: [
                    for (final card in topCards)
                      SizedBox(
                        width: constraints.maxWidth >= 620
                            ? (constraints.maxWidth - 10) / 2
                            : constraints.maxWidth,
                        child: card,
                      ),
                  ],
                ),
              const SizedBox(height: 12),
              if (desktop)
                SizedBox(
                  height: 430,
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Expanded(
                        flex: 17,
                        child: _ClosedCashTrendCard(
                          startDay: startOfWeek,
                          revenue: revenueByDay,
                          profit: profitByDay,
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        flex: 10,
                        child: Column(
                          children: [
                            Expanded(
                              child: _ClosedCashWeeklyAnalysis(
                                pendingCount: open.length,
                                onResolve: () => openModule(
                                  'Fechamento bloqueado',
                                  _CashCloseBlockedModule(store: store),
                                ),
                                onRefresh: store.flushSync,
                              ),
                            ),
                            const SizedBox(height: 12),
                            Expanded(
                              child: _ClosedCashBeforeOpening(
                                pendingCount: open.length,
                                pendingTotal: openTotal,
                                lastClosing: lastClosing?.createdAt,
                                lowStock: lowStock,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                )
              else ...[
                _ClosedCashTrendCard(
                  startDay: startOfWeek,
                  revenue: revenueByDay,
                  profit: profitByDay,
                ),
                const SizedBox(height: 10),
                _ClosedCashWeeklyAnalysis(
                  pendingCount: open.length,
                  onResolve: () => openModule(
                    'Fechamento bloqueado',
                    _CashCloseBlockedModule(store: store),
                  ),
                  onRefresh: store.flushSync,
                ),
                const SizedBox(height: 10),
                _ClosedCashBeforeOpening(
                  pendingCount: open.length,
                  pendingTotal: openTotal,
                  lastClosing: lastClosing?.createdAt,
                  lowStock: lowStock,
                ),
              ],
              const SizedBox(height: 12),
              if (constraints.maxWidth >= 760)
                Row(
                  children: [
                    for (
                      var index = 0;
                      index < summaryCards.length;
                      index++
                    ) ...[
                      Expanded(child: summaryCards[index]),
                      if (index < summaryCards.length - 1)
                        const SizedBox(width: 12),
                    ],
                  ],
                )
              else
                Wrap(
                  runSpacing: 10,
                  children: summaryCards
                      .map(
                        (card) =>
                            SizedBox(width: constraints.maxWidth, child: card),
                      )
                      .toList(),
                ),
            ],
          );
        },
      ),
    );
  }
}

class _ClosedCashStatusCard extends StatelessWidget {
  const _ClosedCashStatusCard({required this.onOpen});

  final VoidCallback onOpen;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 128,
      padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 8),
      decoration: BoxDecoration(
        color: const Color(0xFF222222),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFF4A423C)),
      ),
      child: Row(
        children: [
          Container(
            width: 58,
            height: 58,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: const Color(0xFF302B28),
              shape: BoxShape.circle,
              border: Border.all(color: const Color(0xFF5A4C43)),
            ),
            child: const Icon(Icons.lock_outline_rounded, color: _blue),
          ),
          const SizedBox(width: 20),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Text(
                  'Caixa fechado',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 17,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 3),
                const Text(
                  'Abra o caixa para iniciar as vendas.',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: Color(0xFFD1C9C3), fontSize: 11),
                ),
                const SizedBox(height: 5),
                SizedBox(
                  height: 34,
                  child: FilledButton.icon(
                    key: const Key('cashClosedOpenButton'),
                    onPressed: onOpen,
                    style: FilledButton.styleFrom(
                      foregroundColor: Colors.white,
                    ),
                    icon: const Icon(Icons.lock_open_rounded, size: 16),
                    label: const Text('Abrir caixa'),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ClosedDashboardMetric extends StatelessWidget {
  const _ClosedDashboardMetric({
    required this.icon,
    required this.label,
    required this.value,
    required this.detail,
    required this.color,
  });

  final IconData icon;
  final String label;
  final String value;
  final String detail;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 128,
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: _line),
      ),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: const Color(0xFFFFF0E8),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: const Color(0xFFFFCBB5)),
            ),
            child: Icon(icon, color: _blue, size: 20),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    color: _textSecondary,
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: color,
                    fontSize: 21,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  detail,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: _textSecondary, fontSize: 10),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ClosedCashTrendCard extends StatelessWidget {
  const _ClosedCashTrendCard({
    required this.startDay,
    required this.revenue,
    required this.profit,
  });

  final DateTime startDay;
  final List<double> revenue;
  final List<double> profit;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 430,
      padding: const EdgeInsets.fromLTRB(22, 18, 22, 16),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: _line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          LayoutBuilder(
            builder: (context, constraints) {
              final compact = constraints.maxWidth < 520;
              return Row(
                children: [
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Vendas x lucro por dia',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: _navy,
                            fontSize: 18,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        SizedBox(height: 3),
                        Text(
                          'Faturamento e lucro bruto dos últimos 7 dias',
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(color: _textSecondary, fontSize: 11),
                        ),
                      ],
                    ),
                  ),
                  if (!compact) ...[
                    const _ChartLegend(color: _blue, label: 'Faturamento'),
                    const SizedBox(width: 18),
                    const _ChartLegend(
                      color: Color(0xFF8E857D),
                      label: 'Lucro bruto',
                    ),
                  ],
                ],
              );
            },
          ),
          const SizedBox(height: 14),
          Expanded(
            child: Stack(
              children: [
                Positioned.fill(
                  child: CustomPaint(
                    painter: _ClosedCashTrendPainter(
                      startDay: startDay,
                      revenue: revenue,
                      profit: profit,
                    ),
                  ),
                ),
                Positioned(
                  right: 12,
                  bottom: 62,
                  child: Container(
                    width: 214,
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(9),
                      border: Border.all(color: _line),
                      boxShadow: const [
                        BoxShadow(
                          color: Color(0x22000000),
                          blurRadius: 8,
                          offset: Offset(0, 3),
                        ),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          _shortDay(startDay.add(const Duration(days: 6))),
                          style: const TextStyle(
                            color: _navy,
                            fontSize: 11,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          'Faturamento   ${money(revenue.last)}',
                          style: const TextStyle(
                            color: _textSecondary,
                            fontSize: 10,
                          ),
                        ),
                        Text(
                          'Lucro bruto   ${money(profit.last)}',
                          style: const TextStyle(
                            color: _textSecondary,
                            fontSize: 10,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ChartLegend extends StatelessWidget {
  const _ChartLegend({required this.color, required this.label});

  final Color color;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 13,
          height: 13,
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(3),
          ),
        ),
        const SizedBox(width: 7),
        Text(
          label,
          style: const TextStyle(color: _textSecondary, fontSize: 10),
        ),
      ],
    );
  }
}

class _ClosedCashTrendPainter extends CustomPainter {
  const _ClosedCashTrendPainter({
    required this.startDay,
    required this.revenue,
    required this.profit,
  });

  final DateTime startDay;
  final List<double> revenue;
  final List<double> profit;

  @override
  void paint(Canvas canvas, Size size) {
    const left = 66.0;
    const top = 12.0;
    const right = 12.0;
    const bottom = 38.0;
    final width = size.width - left - right;
    final height = size.height - top - bottom;
    final maxValue = math.max(
      1.0,
      [...revenue, ...profit].fold<double>(0, math.max),
    );
    final gridPaint = Paint()
      ..color = const Color(0xFFE8DED4)
      ..strokeWidth = 1;
    final labelStyle = const TextStyle(
      color: _textSecondary,
      fontSize: 9,
      fontFamily: 'Segoe UI',
    );
    for (var row = 0; row <= 4; row++) {
      final y = top + (height * row / 4);
      canvas.drawLine(
        Offset(left, y),
        Offset(size.width - right, y),
        gridPaint,
      );
      final value = maxValue * (1 - row / 4);
      final painter = TextPainter(
        text: TextSpan(text: money(value), style: labelStyle),
        textDirection: TextDirection.ltr,
      )..layout(maxWidth: left - 8);
      painter.paint(canvas, Offset(left - painter.width - 8, y - 6));
    }
    for (var index = 0; index < 7; index++) {
      final x = left + (width * index / 6);
      final painter = TextPainter(
        text: TextSpan(
          text: _shortDay(startDay.add(Duration(days: index))),
          style: labelStyle,
        ),
        textDirection: TextDirection.ltr,
      )..layout();
      painter.paint(canvas, Offset(x - painter.width / 2, size.height - 21));
    }

    void drawSeries(List<double> values, Color color) {
      final path = Path();
      final pointPaint = Paint()..color = color;
      for (var index = 0; index < values.length; index++) {
        final x = left + (width * index / 6);
        final y = top + height - (height * values[index] / maxValue);
        if (index == 0) {
          path.moveTo(x, y);
        } else {
          path.lineTo(x, y);
        }
        canvas.drawCircle(Offset(x, y), 3, pointPaint);
      }
      canvas.drawPath(
        path,
        Paint()
          ..color = color
          ..strokeWidth = 2
          ..style = PaintingStyle.stroke,
      );
    }

    drawSeries(profit, const Color(0xFF8E857D));
    drawSeries(revenue, _blue);
  }

  @override
  bool shouldRepaint(covariant _ClosedCashTrendPainter oldDelegate) =>
      oldDelegate.revenue != revenue || oldDelegate.profit != profit;
}

class _ClosedCashWeeklyAnalysis extends StatelessWidget {
  const _ClosedCashWeeklyAnalysis({
    required this.pendingCount,
    required this.onResolve,
    required this.onRefresh,
  });

  final int pendingCount;
  final VoidCallback onResolve;
  final VoidCallback onRefresh;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: _line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(Icons.lightbulb_outline_rounded, color: _blue, size: 24),
              SizedBox(width: 14),
              Expanded(
                child: Text(
                  'Análise da semana',
                  style: TextStyle(
                    color: _navy,
                    fontSize: 17,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ),
              _TinyStatusPill(text: 'IA', color: _blue),
            ],
          ),
          const SizedBox(height: 10),
          Text(
            pendingCount == 0
                ? 'A operação está pronta para um novo caixa.'
                : 'As pendências abertas são a prioridade para proteger o resultado.',
            style: const TextStyle(color: _textSecondary, fontSize: 11),
          ),
          const SizedBox(height: 16),
          if (pendingCount > 0)
            SizedBox(
              width: double.infinity,
              height: 40,
              child: OutlinedButton(
                onPressed: onResolve,
                style: OutlinedButton.styleFrom(
                  alignment: Alignment.centerLeft,
                  foregroundColor: const Color(0xFF9A3E13),
                  backgroundColor: const Color(0xFFFFF0E8),
                ),
                child: const Text('Resolver pendências abertas'),
              ),
            ),
          const SizedBox(height: 10),
          SizedBox(
            height: 40,
            child: FilledButton.icon(
              onPressed: onRefresh,
              style: FilledButton.styleFrom(
                backgroundColor: _rail,
                foregroundColor: Colors.white,
              ),
              icon: const Icon(Icons.lightbulb_outline_rounded, size: 17),
              label: const Text('Atualizar análise'),
            ),
          ),
        ],
      ),
    );
  }
}

class _ClosedCashBeforeOpening extends StatelessWidget {
  const _ClosedCashBeforeOpening({
    required this.pendingCount,
    required this.pendingTotal,
    required this.lastClosing,
    required this.lowStock,
  });

  final int pendingCount;
  final double pendingTotal;
  final DateTime? lastClosing;
  final int lowStock;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(18, 15, 18, 12),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: _line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Antes de abrir',
            style: TextStyle(
              color: _navy,
              fontSize: 15,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 8),
          _BeforeOpeningRow(
            icon: Icons.description_outlined,
            label: '$pendingCount pendências',
            value: money(pendingTotal),
            color: const Color(0xFFB36B00),
          ),
          _BeforeOpeningRow(
            icon: Icons.schedule_rounded,
            label: 'Último fechamento',
            value: lastClosing == null
                ? 'Não registrado'
                : '${_shortDay(lastClosing!)} ${_shortTime(lastClosing!)}',
            color: const Color(0xFFB36B00),
          ),
          _BeforeOpeningRow(
            icon: Icons.inventory_2_outlined,
            label: 'Estoque',
            value: lowStock == 0 ? 'Em dia' : '$lowStock em atenção',
            color: lowStock == 0 ? _teal : _danger,
            showBorder: false,
          ),
        ],
      ),
    );
  }
}

class _BeforeOpeningRow extends StatelessWidget {
  const _BeforeOpeningRow({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
    this.showBorder = true,
  });

  final IconData icon;
  final String label;
  final String value;
  final Color color;
  final bool showBorder;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 44,
      decoration: BoxDecoration(
        border: showBorder
            ? const Border(bottom: BorderSide(color: _line))
            : null,
      ),
      child: Row(
        children: [
          Icon(icon, color: color, size: 18),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              label,
              style: const TextStyle(
                color: _navy,
                fontSize: 11,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          Text(
            value,
            style: TextStyle(
              color: color,
              fontSize: 11,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(width: 6),
          const Icon(
            Icons.chevron_right_rounded,
            color: _textSecondary,
            size: 17,
          ),
        ],
      ),
    );
  }
}

class _ClosedDashboardSummary extends StatelessWidget {
  const _ClosedDashboardSummary({
    required this.icon,
    required this.label,
    required this.value,
    required this.detail,
  });

  final IconData icon;
  final String label;
  final String value;
  final String detail;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 92,
      padding: const EdgeInsets.symmetric(horizontal: 20),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: _line),
      ),
      child: Row(
        children: [
          Container(
            width: 40,
            height: 40,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: const Color(0xFFFFF0E8),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: const Color(0xFFFFCBB5)),
            ),
            child: Icon(icon, color: _blue, size: 19),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(color: _textSecondary, fontSize: 11),
                ),
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _navy,
                    fontSize: 17,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text(
                  detail,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: _textSecondary, fontSize: 9),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

String _shortTime(DateTime value) {
  final hour = value.hour.toString().padLeft(2, '0');
  final minute = value.minute.toString().padLeft(2, '0');
  return '$hour:$minute';
}

String _shortDay(DateTime value) {
  final day = value.day.toString().padLeft(2, '0');
  final month = value.month.toString().padLeft(2, '0');
  return '$day/$month';
}

String _monthName(int month) {
  const months = [
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
  ];
  return months[(month - 1).clamp(0, 11)];
}

class _ClosedCashSideRail extends StatelessWidget {
  const _ClosedCashSideRail({
    required this.store,
    required this.compact,
    required this.openModule,
    required this.selectedPage,
    required this.onPageChanged,
  });

  final BalcaoStore store;
  final bool compact;
  final void Function(String title, Widget child) openModule;
  final int selectedPage;
  final ValueChanged<int> onPageChanged;

  @override
  Widget build(BuildContext context) {
    final lowStock = store.products
        .where((product) => product.stock <= product.minStock)
        .length;
    final items = [
      _ClosedRailAction(
        label: 'Painel',
        subtitle: 'Visão geral',
        icon: Icons.area_chart_outlined,
        active: selectedPage == 0,
        onTap: () => onPageChanged(0),
      ),
      _ClosedRailAction(
        label: 'Relatórios',
        subtitle: 'Visão gerencial',
        trailing: 'Hoje',
        icon: Icons.query_stats_outlined,
        active: selectedPage == 1,
        onTap: () => onPageChanged(1),
      ),
      _ClosedRailAction(
        label: 'Estoque',
        subtitle: 'Reposição',
        trailing: '$lowStock',
        icon: Icons.inventory_2_outlined,
        active: selectedPage == 2,
        onTap: () => onPageChanged(2),
      ),
      _ClosedRailAction(
        label: 'Caixa',
        subtitle: 'Histórico',
        trailing: 'Fechado',
        icon: Icons.credit_card_outlined,
        active: selectedPage == 3,
        onTap: () => onPageChanged(3),
      ),
    ];

    return Container(
      width: compact ? 88 : 254,
      padding: EdgeInsets.fromLTRB(
        compact ? 10 : 16,
        20,
        compact ? 10 : 16,
        16,
      ),
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [_rail, Color(0xFF202020), _railDeep],
        ),
        border: Border(right: BorderSide(color: Color(0xFF3B3B3B))),
      ),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: compact
                ? MainAxisAlignment.center
                : MainAxisAlignment.start,
            children: [
              Container(
                width: 52,
                height: 52,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(4),
                ),
                child: const Text(
                  'BL',
                  style: TextStyle(
                    color: _navy,
                    fontSize: 22,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ),
              if (!compact) ...[
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Balcão Livre PDV',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 15,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        store.businessName,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: Color(0xFFBEB8B2),
                          fontSize: 11,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ],
          ),
          const SizedBox(height: 28),
          if (!compact) ...[
            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: const Color(0xFF2A2A2A),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: const Color(0xFF474747)),
              ),
              child: Row(
                children: [
                  const CircleAvatar(
                    radius: 22,
                    backgroundColor: _blue,
                    child: Text(
                      'QS',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 11,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          store.operatorName.isEmpty
                              ? 'OPERADOR'
                              : store.operatorName.toUpperCase(),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 12,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        const Text(
                          'GERENTE',
                          style: TextStyle(
                            color: Color(0xFFBEB8B2),
                            fontSize: 10,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 18),
          ],
          Expanded(
            child: ListView.separated(
              padding: EdgeInsets.zero,
              itemCount: items.length,
              separatorBuilder: (context, index) => const SizedBox(height: 8),
              itemBuilder: (context, index) =>
                  _ClosedCashRailButton(action: items[index], compact: compact),
            ),
          ),
          if (!compact) ...[
            const Divider(color: Color(0xFF4A4A4A)),
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 10),
              child: Align(
                alignment: Alignment.centerLeft,
                child: Text(
                  'CENTRAL DO OPERADOR',
                  style: TextStyle(
                    color: Color(0xFF9D9790),
                    fontSize: 9,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
            ),
            _ClosedCashRailButton(
              compact: false,
              action: _ClosedRailAction(
                label: 'Suporte online',
                subtitle: 'Disponível',
                icon: Icons.chat_bubble_outline_rounded,
                onTap: () => openModule(
                  'Central rápida',
                  _QuickHubModule(store: store, openModule: openModule),
                ),
              ),
            ),
            const SizedBox(height: 8),
          ],
          Row(
            children: [
              Expanded(
                child: _ClosedCashRailButton(
                  compact: compact,
                  dense: true,
                  action: _ClosedRailAction(
                    label: 'Ajustes',
                    subtitle: '',
                    icon: Icons.settings_outlined,
                    onTap: () => openModule(
                      'Configurações',
                      _SettingsDeskModule(store: store),
                    ),
                  ),
                ),
              ),
              if (!compact) const SizedBox(width: 8),
              Expanded(
                child: _ClosedCashRailButton(
                  compact: compact,
                  dense: true,
                  action: _ClosedRailAction(
                    label: 'Sair',
                    subtitle: '',
                    icon: Icons.logout_rounded,
                    onTap: () => unawaited(store.logout()),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ClosedRailAction {
  const _ClosedRailAction({
    required this.label,
    required this.subtitle,
    required this.icon,
    required this.onTap,
    this.trailing = '',
    this.active = false,
  });

  final String label;
  final String subtitle;
  final String trailing;
  final IconData icon;
  final VoidCallback onTap;
  final bool active;
}

class _ClosedCashRailButton extends StatelessWidget {
  const _ClosedCashRailButton({
    required this.action,
    required this.compact,
    this.dense = false,
  });

  final _ClosedRailAction action;
  final bool compact;
  final bool dense;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: action.active ? _blue : Colors.transparent,
      borderRadius: BorderRadius.circular(10),
      child: InkWell(
        onTap: action.onTap,
        borderRadius: BorderRadius.circular(10),
        child: Container(
          height: dense ? 54 : 70,
          padding: EdgeInsets.symmetric(horizontal: compact ? 0 : 14),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(10),
            border: action.active
                ? null
                : Border.all(color: Colors.transparent),
          ),
          child: Row(
            mainAxisAlignment: compact
                ? MainAxisAlignment.center
                : MainAxisAlignment.start,
            children: [
              Icon(
                action.icon,
                color: action.active ? Colors.white : const Color(0xFFF4F0EC),
                size: 22,
              ),
              if (!compact) ...[
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        action.label,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 13,
                          fontWeight: action.active
                              ? FontWeight.w900
                              : FontWeight.w800,
                        ),
                      ),
                      if (action.subtitle.isNotEmpty)
                        Text(
                          action.subtitle,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: action.active
                                ? const Color(0xFFFFD8C7)
                                : const Color(0xFFBEB8B2),
                            fontSize: 10,
                          ),
                        ),
                    ],
                  ),
                ),
                if (action.trailing.isNotEmpty)
                  Text(
                    action.trailing,
                    style: TextStyle(
                      color: action.active
                          ? Colors.white
                          : const Color(0xFFE8D9CB),
                      fontSize: 10,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                if (!dense && !action.active) ...[
                  const SizedBox(width: 5),
                  const Icon(
                    Icons.chevron_right_rounded,
                    color: Color(0xFFA89F97),
                    size: 17,
                  ),
                ],
              ],
            ],
          ),
        ),
      ),
    );
  }
}

// ignore: unused_element
class _OnlineSideRail extends StatelessWidget {
  const _OnlineSideRail({
    required this.selectedMode,
    required this.onModeChanged,
    required this.openModule,
    required this.store,
    required this.compact,
    required this.comandaFilter,
    required this.onComandaFilterChanged,
    required this.kitchenFilter,
    required this.onKitchenFilterChanged,
    required this.deliveryFilter,
    required this.onDeliveryFilterChanged,
    required this.onNewDelivery,
  });

  final int selectedMode;
  final ValueChanged<int> onModeChanged;
  final void Function(String title, Widget child) openModule;
  final BalcaoStore store;
  final bool compact;
  final String comandaFilter;
  final ValueChanged<String> onComandaFilterChanged;
  final String kitchenFilter;
  final ValueChanged<String> onKitchenFilterChanged;
  final String deliveryFilter;
  final ValueChanged<String> onDeliveryFilterChanged;
  final VoidCallback onNewDelivery;

  @override
  Widget build(BuildContext context) {
    final shortRail = MediaQuery.sizeOf(context).height < 900;
    final items = [
      _RailAction(
        'Comanda',
        Icons.description_outlined,
        () => onModeChanged(0),
        selectedMode == 0,
      ),
      _RailAction(
        'Cozinha',
        Icons.kitchen_outlined,
        () => onModeChanged(1),
        selectedMode == 1,
      ),
      _RailAction(
        'Delivery',
        Icons.delivery_dining_outlined,
        () => onModeChanged(2),
        selectedMode == 2,
      ),
    ];
    final kitchenCounts = <String, int>{
      'Todas': 0,
      'Forno': 0,
      'Fritadeira': 0,
      'Montagem': 0,
    };
    final tableOrders = store.openOrders
        .where((order) => order.kind == OrderKind.table)
        .toList();
    final occupiedTables = tableOrders
        .where(
          (order) =>
              order.items.isNotEmpty && order.paymentMethod.trim().isEmpty,
        )
        .length;
    final accountTables = tableOrders
        .where(
          (order) =>
              order.items.isNotEmpty && order.paymentMethod.trim().isNotEmpty,
        )
        .length;
    final tableSlots = math.max(12, tableOrders.length);
    final comandaCounts = <String, int>{
      'Todas': tableSlots,
      'Livres': math.max(0, tableSlots - occupiedTables - accountTables),
      'Ocupadas': occupiedTables,
      'Conta': accountTables,
    };
    if (selectedMode == 1) {
      for (final order in store.openOrders) {
        for (final item in order.items) {
          final product = store.products
              .where((candidate) => candidate.id == item.productId)
              .firstOrNull;
          final station = _kitchenStationFor(product, item);
          kitchenCounts['Todas'] = kitchenCounts['Todas']! + 1;
          kitchenCounts[station] = kitchenCounts[station]! + 1;
        }
      }
    }
    final deliveryOrders = store.orders
        .where(
          (order) =>
              (order.kind == OrderKind.delivery ||
                  order.kind == OrderKind.ifood) &&
              order.status != OrderStatus.canceled,
        )
        .toList();
    final deliveryCounts = <String, int>{
      'Todos': deliveryOrders.length,
      'Novos': deliveryOrders
          .where((order) => order.status == OrderStatus.open)
          .length,
      'Em preparo': deliveryOrders
          .where((order) => order.status == OrderStatus.preparing)
          .length,
      'Em rota': deliveryOrders
          .where((order) => order.status == OrderStatus.dispatched)
          .length,
      'Entregues': deliveryOrders
          .where((order) => order.status == OrderStatus.delivered)
          .length,
    };

    return Container(
      width: compact ? 88 : 254,
      padding: EdgeInsets.fromLTRB(
        compact ? 10 : 16,
        20,
        compact ? 10 : 16,
        16,
      ),
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [_rail, Color(0xFF202020), _railDeep],
        ),
        border: Border(right: BorderSide(color: Color(0xFF3B3B3B))),
      ),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: compact
                ? MainAxisAlignment.center
                : MainAxisAlignment.start,
            children: [
              Container(
                width: 52,
                height: 52,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(4),
                ),
                child: const Text(
                  'BL',
                  style: TextStyle(
                    color: Color(0xFF222222),
                    fontSize: 22,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ),
              if (!compact) ...[
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Balcao Livre PDV',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 15,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        store.businessName,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: Color(0xFFBEB8B2),
                          fontSize: 11,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ],
          ),
          const SizedBox(height: 24),
          if (!compact) ...[
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: const Color(0xFF2A2A2A),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: const Color(0xFF474747)),
              ),
              child: Row(
                children: [
                  const CircleAvatar(
                    radius: 22,
                    backgroundColor: _blue,
                    child: Text(
                      'QS',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 11,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          store.operatorName.isEmpty
                              ? 'OPERADOR'
                              : store.operatorName.toUpperCase(),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 12,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        const Text(
                          'GERENTE',
                          style: TextStyle(
                            color: Color(0xFFBEB8B2),
                            fontSize: 10,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 18),
          ],
          Expanded(
            child: ListView(
              physics: const NeverScrollableScrollPhysics(),
              children: [
                for (var index = 0; index < items.length; index++) ...[
                  _OnlineRailButton(items[index], compact: compact),
                  if (index < items.length - 1) const SizedBox(height: 5),
                ],
                if (!compact && selectedMode == 0) ...[
                  const SizedBox(height: 16),
                  const Padding(
                    padding: EdgeInsets.only(left: 4, bottom: 6),
                    child: Text(
                      'FILTROS',
                      style: TextStyle(
                        color: Color(0xFFA79A8F),
                        fontSize: 10,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  for (final filter in const [
                    ('Todas', _blue),
                    ('Livres', Color(0xFF82DB76)),
                    ('Ocupadas', Color(0xFFFF9292)),
                    ('Conta', Color(0xFFC89443)),
                  ])
                    _OperationalFilterButton(
                      label: filter.$1,
                      count: comandaCounts[filter.$1] ?? 0,
                      color: filter.$2,
                      active: comandaFilter == filter.$1,
                      onTap: () => onComandaFilterChanged(filter.$1),
                    ),
                  const SizedBox(height: 8),
                  SizedBox(
                    width: double.infinity,
                    height: 42,
                    child: OutlinedButton.icon(
                      onPressed: () => store.openOrder(OrderKind.table),
                      icon: const Icon(Icons.add_rounded, size: 18),
                      label: const Text('Criar mesas'),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: Colors.white,
                        side: const BorderSide(color: Color(0xFF58524D)),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 10),
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.symmetric(
                      horizontal: 10,
                      vertical: 10,
                    ),
                    decoration: BoxDecoration(
                      color: const Color(0xFF292929),
                      borderRadius: BorderRadius.circular(9),
                      border: Border.all(color: const Color(0xFF4A4642)),
                    ),
                    child: const Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Área: Comanda',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 11,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        SizedBox(height: 3),
                        Text(
                          'Ctrl+Tab troca a área',
                          style: TextStyle(
                            color: Color(0xFFA79A8F),
                            fontSize: 9,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
                if (!compact && selectedMode == 1) ...[
                  const SizedBox(height: 16),
                  const Padding(
                    padding: EdgeInsets.only(left: 4, bottom: 6),
                    child: Text(
                      'FILTROS',
                      style: TextStyle(
                        color: Color(0xFFA79A8F),
                        fontSize: 10,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  for (final filter in const [
                    ('Todas', _blue),
                    ('Forno', Color(0xFF7EDC88)),
                    ('Fritadeira', Color(0xFFFF6978)),
                    ('Montagem', Color(0xFFE5A23A)),
                  ])
                    _OperationalFilterButton(
                      label: filter.$1,
                      count: kitchenCounts[filter.$1] ?? 0,
                      color: filter.$2,
                      active: kitchenFilter == filter.$1,
                      onTap: () => onKitchenFilterChanged(filter.$1),
                    ),
                  const SizedBox(height: 14),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 10,
                      vertical: 10,
                    ),
                    decoration: BoxDecoration(
                      color: const Color(0xFF292929),
                      borderRadius: BorderRadius.circular(9),
                      border: Border.all(color: const Color(0xFF4A4642)),
                    ),
                    child: const Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Área: Cozinha',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 11,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        SizedBox(height: 3),
                        Text(
                          'Ctrl+Tab troca a área',
                          style: TextStyle(
                            color: Color(0xFFA79A8F),
                            fontSize: 9,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
                if (!compact && selectedMode == 2) ...[
                  const SizedBox(height: 16),
                  const Padding(
                    padding: EdgeInsets.only(left: 4, bottom: 6),
                    child: Text(
                      'FILTROS',
                      style: TextStyle(
                        color: Color(0xFFA79A8F),
                        fontSize: 10,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  for (final filter in const [
                    ('Todos', _blue),
                    ('Novos', Color(0xFF7EDC88)),
                    ('Em preparo', Color(0xFFFF6978)),
                    ('Em rota', Color(0xFFE5A23A)),
                    ('Entregues', Color(0xFF8EB7E8)),
                  ])
                    _OperationalFilterButton(
                      label: filter.$1,
                      count: deliveryCounts[filter.$1] ?? 0,
                      color: filter.$2,
                      active: deliveryFilter == filter.$1,
                      onTap: () => onDeliveryFilterChanged(filter.$1),
                    ),
                  const SizedBox(height: 12),
                  SizedBox(
                    width: double.infinity,
                    height: 42,
                    child: FilledButton.icon(
                      onPressed: onNewDelivery,
                      icon: const Icon(Icons.add_rounded, size: 18),
                      label: const Text('Delivery'),
                      style: FilledButton.styleFrom(
                        backgroundColor: _blue,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(horizontal: 10),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
          if (!compact && !shortRail) ...[
            const Divider(color: Color(0xFF4A4A4A)),
            const Align(
              alignment: Alignment.centerLeft,
              child: Padding(
                padding: EdgeInsets.symmetric(vertical: 10),
                child: Text(
                  'CENTRAL DO OPERADOR',
                  style: TextStyle(
                    color: Color(0xFF9D9790),
                    fontSize: 9,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
            ),
            _OnlineRailButton(
              _RailAction(
                'Suporte online',
                Icons.chat_bubble_outline_rounded,
                () => openModule(
                  'Central rapida',
                  _QuickHubModule(store: store, openModule: openModule),
                ),
                false,
              ),
              compact: false,
            ),
            const SizedBox(height: 8),
            _OnlineRailButton(
              _RailAction(
                'Ajustes',
                Icons.settings_outlined,
                () => openModule(
                  'Configuracoes',
                  _SettingsDeskModule(store: store),
                ),
                false,
              ),
              compact: false,
            ),
          ],
          if (!shortRail || compact)
            _OnlineRailButton(
              _RailAction(
                'Sair',
                Icons.logout_rounded,
                () => unawaited(store.logout()),
                false,
              ),
              exit: true,
              compact: compact,
            ),
        ],
      ),
    );
  }
}

class _RailAction {
  const _RailAction(this.label, this.icon, this.onTap, this.active);

  final String label;
  final IconData icon;
  final VoidCallback onTap;
  final bool active;
}

class _OnlineRailButton extends StatelessWidget {
  const _OnlineRailButton(
    this.action, {
    this.exit = false,
    required this.compact,
  });

  final _RailAction action;
  final bool exit;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final active = action.active && !exit;
    return Material(
      color: active
          ? _blue
          : exit
          ? const Color(0xFF252525)
          : Colors.transparent,
      borderRadius: BorderRadius.circular(7),
      child: InkWell(
        onTap: action.onTap,
        borderRadius: BorderRadius.circular(7),
        child: SizedBox(
          height: compact ? 64 : 62,
          child: Row(
            children: [
              SizedBox(
                width: compact ? 68 : 52,
                child: Icon(
                  action.icon,
                  color: active ? Colors.white : const Color(0xFFF4F0EC),
                  size: 22,
                ),
              ),
              if (!compact)
                Expanded(
                  child: Text(
                    action.label,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: active ? Colors.white : const Color(0xFFF4F0EC),
                      fontSize: 13,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _OperationalFilterButton extends StatelessWidget {
  const _OperationalFilterButton({
    required this.label,
    required this.count,
    required this.color,
    required this.active,
    required this.onTap,
  });

  final String label;
  final int count;
  final Color color;
  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: active ? _blue : Colors.transparent,
      child: InkWell(
        onTap: onTap,
        child: Container(
          height: 58,
          margin: const EdgeInsets.only(bottom: 2),
          decoration: BoxDecoration(
            border: Border(left: BorderSide(color: color, width: 5)),
          ),
          padding: const EdgeInsets.fromLTRB(14, 0, 12, 0),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  label,
                  style: TextStyle(
                    color: active ? Colors.white : const Color(0xFFF2ECE6),
                    fontSize: 13,
                    fontWeight: active ? FontWeight.w900 : FontWeight.w600,
                  ),
                ),
              ),
              Text(
                '$count',
                style: TextStyle(
                  color: active ? Colors.white : const Color(0xFFF2ECE6),
                  fontSize: 13,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ignore: unused_element
class _OnlineHeader extends StatelessWidget {
  const _OnlineHeader({
    required this.store,
    required this.openModule,
    required this.toggleCash,
  });

  final BalcaoStore store;
  final void Function(String title, Widget child) openModule;
  final VoidCallback toggleCash;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 114,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Expanded(
            flex: 9,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Row(
                  children: [
                    const Flexible(
                      child: Text(
                        'Balcao Livre PDV',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: _navy,
                          fontSize: 24,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Text(
                      'v2.1.0',
                      style: TextStyle(
                        color: _navy2,
                        fontSize: 11,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 14),
                Row(
                  children: [
                    const _LogoBlock(size: 62),
                    const SizedBox(width: 14),
                    Flexible(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            store.businessName,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: _navy,
                              fontSize: 18,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            '${store.businessDocument} | ${store.businessPhone}',
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: _textSecondary,
                              fontSize: 12,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 12),
                    _OnlineStatusDot(
                      text: store.syncStatus.contains('Online')
                          ? 'Online'
                          : store.syncStatus,
                      color: store.syncStatus.contains('Online')
                          ? _teal
                          : _danger,
                    ),
                  ],
                ),
              ],
            ),
          ),
          Expanded(
            flex: 10,
            child: Wrap(
              alignment: WrapAlignment.end,
              crossAxisAlignment: WrapCrossAlignment.center,
              spacing: 10,
              runSpacing: 8,
              children: [
                _OnlineHeaderButton(
                  label: 'WhatsApp',
                  icon: Icons.chat_bubble_outline_rounded,
                  color: _teal,
                  onTap: () => openModule(
                    'WhatsApp Online',
                    _WhatsAppModule(store: store),
                  ),
                ),
                _OnlineHeaderButton(
                  label: 'iFood',
                  color: const Color(0xFFDC1F2A),
                  onTap: () => openModule(
                    'Delivery e iFood',
                    _DeliveryModule(store: store),
                  ),
                ),
                _OnlineHeaderButton(
                  label: store.cashOpen ? 'Caixa aberto' : 'Loja fechada',
                  icon: Icons.lock_outline_rounded,
                  filled: true,
                  color: store.cashOpen ? _teal : const Color(0xFFCB202B),
                  onTap: toggleCash,
                ),
                _OnlineTopLink(
                  label: 'Configuracoes',
                  icon: Icons.settings_outlined,
                  onTap: () => openModule(
                    'Configuracoes',
                    _SettingsDeskModule(store: store),
                  ),
                ),
                _OnlineTopLink(
                  label: 'Ajuda',
                  icon: Icons.help_outline_rounded,
                  onTap: () => openModule(
                    'Central rapida',
                    _QuickHubModule(store: store, openModule: openModule),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 20),
          _OnlineBalanceCard(store: store, onTap: toggleCash),
        ],
      ),
    );
  }
}

class _OnlineStatusDot extends StatelessWidget {
  const _OnlineStatusDot({required this.text, required this.color});

  final String text;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 8,
          height: 8,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 5),
        Text(
          text,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            color: color,
            fontSize: 12,
            fontWeight: FontWeight.w900,
          ),
        ),
      ],
    );
  }
}

class _OnlineHeaderButton extends StatelessWidget {
  const _OnlineHeaderButton({
    required this.label,
    required this.color,
    required this.onTap,
    this.icon,
    this.filled = false,
  });

  final String label;
  final Color color;
  final VoidCallback onTap;
  final IconData? icon;
  final bool filled;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 54,
      child: FilledButton.icon(
        onPressed: onTap,
        icon: icon == null ? const SizedBox.shrink() : Icon(icon, size: 20),
        label: Text(label, maxLines: 1, overflow: TextOverflow.ellipsis),
        style: FilledButton.styleFrom(
          backgroundColor: filled ? color : Colors.white,
          foregroundColor: filled ? Colors.white : color,
          elevation: filled ? 0 : 1,
          padding: const EdgeInsets.symmetric(horizontal: 18),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
            side: BorderSide(color: filled ? color : _line),
          ),
          textStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w900),
        ),
      ),
    );
  }
}

class _OnlineTopLink extends StatelessWidget {
  const _OnlineTopLink({
    required this.label,
    required this.icon,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return TextButton.icon(
      onPressed: onTap,
      icon: Icon(icon, size: 20),
      label: Text(label, maxLines: 1, overflow: TextOverflow.ellipsis),
      style: TextButton.styleFrom(
        foregroundColor: const Color(0xFF1C2940),
        padding: const EdgeInsets.symmetric(horizontal: 6),
        textStyle: const TextStyle(fontSize: 12, fontWeight: FontWeight.w900),
      ),
    );
  }
}

class _OnlineBalanceCard extends StatelessWidget {
  const _OnlineBalanceCard({required this.store, required this.onTap});

  final BalcaoStore store;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 180,
      height: 112,
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(10),
          child: Ink(
            padding: const EdgeInsets.fromLTRB(20, 16, 18, 14),
            decoration: BoxDecoration(
              gradient: const LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [_blue, _blue2],
              ),
              borderRadius: BorderRadius.circular(10),
              boxShadow: const [
                BoxShadow(
                  color: Color(0x331267F3),
                  blurRadius: 24,
                  offset: Offset(0, 12),
                ),
              ],
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  store.cashOpen ? 'Caixa aberto' : 'Loja fechada',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _surfaceMuted,
                    fontWeight: FontWeight.w900,
                    fontSize: 13,
                  ),
                ),
                const SizedBox(height: 7),
                Text(
                  money(store.openTotal),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 26,
                    height: 1,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 7),
                Text(
                  'Vendas hoje: ${money(store.soldToday)}',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _surfaceMuted,
                    fontSize: 11,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _PdvTopBar extends StatelessWidget {
  const _PdvTopBar({
    required this.store,
    required this.openModule,
    required this.toggleCash,
    this.forceCashClosed = false,
  });

  final BalcaoStore store;
  final void Function(String title, Widget child) openModule;
  final VoidCallback toggleCash;
  final bool forceCashClosed;

  @override
  Widget build(BuildContext context) {
    final viewWidth = MediaQuery.sizeOf(context).width;
    final roomy = viewWidth >= 1040;
    final veryRoomy = viewWidth >= 1360;
    final cashOpen = forceCashClosed ? false : store.cashOpen;
    return Container(
      width: double.infinity,
      height: 70,
      color: _navy2,
      padding: const EdgeInsets.symmetric(horizontal: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Container(
            width: 38,
            height: 38,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: const Color(0xFFC84408),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: _blue),
            ),
            child: const Icon(
              Icons.insert_chart_outlined,
              color: Color(0xFFFFB182),
              size: 21,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Painel do caixa',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 14,
                    fontWeight: FontWeight.w800,
                    height: 1.05,
                  ),
                ),
                const SizedBox(height: 4),
                const Text(
                  'Visão geral',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: Color(0xFFC9C1BA),
                    fontSize: 10,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
          if (roomy) ...[
            _HeaderWhatsAppButton(
              connected: store.whatsappConnected,
              onTap: () =>
                  openModule('WhatsApp da loja', _WhatsAppModule(store: store)),
            ),
            const _HeaderDivider(),
            _HeaderIconButton(
              icon: Icons.help_outline_rounded,
              label: 'Ajuda',
              onTap: () => openModule(
                'Ajuda e atalhos',
                _QuickHubModule(store: store, openModule: openModule),
              ),
            ),
            const _HeaderDivider(),
            _HeaderIconButton(
              icon: Icons.settings_outlined,
              label: 'Configurações',
              onTap: () => openModule(
                'Configuracoes do sistema',
                _SettingsDeskModule(store: store),
              ),
            ),
          ],
          const SizedBox(width: 12),
          Container(
            height: 48,
            constraints: BoxConstraints(
              minWidth: veryRoomy ? 220 : 136,
              maxWidth: veryRoomy ? 280 : 160,
            ),
            padding: EdgeInsets.symmetric(horizontal: veryRoomy ? 14 : 10),
            decoration: BoxDecoration(
              color: const Color(0xFF242424),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: const Color(0xFF3C3A38)),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (veryRoomy) ...[
                  Icon(
                    cashOpen
                        ? Icons.lock_open_outlined
                        : Icons.lock_outline_rounded,
                    color: const Color(0xFFF3EEE9),
                    size: 18,
                  ),
                  const SizedBox(width: 8),
                ],
                Container(
                  width: 7,
                  height: 7,
                  decoration: BoxDecoration(
                    color: cashOpen ? _teal : const Color(0xFFF34B53),
                    shape: BoxShape.circle,
                  ),
                ),
                if (veryRoomy) ...[
                  const SizedBox(width: 8),
                  Flexible(
                    child: Text(
                      cashOpen ? 'Caixa aberto' : 'Caixa fechado',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: cashOpen
                            ? const Color(0xFFEFE9E3)
                            : const Color(0xFFF36A70),
                        fontSize: 11,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  const Text(
                    '•',
                    style: TextStyle(color: Color(0xFF8F8983), fontSize: 11),
                  ),
                ],
                const SizedBox(width: 7),
                Text(
                  money(store.openTotal),
                  maxLines: 1,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 13,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ],
            ),
          ),
          if (veryRoomy) ...[
            const SizedBox(width: 12),
            SizedBox(
              width: 130,
              height: 46,
              child: FilledButton(
                key: const Key('topCashAction'),
                onPressed: toggleCash,
                style: FilledButton.styleFrom(
                  backgroundColor: cashOpen ? _danger : _blue,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(horizontal: 12),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                  textStyle: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                child: Text(cashOpen ? 'Fechar caixa' : 'Abrir caixa'),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _HeaderWhatsAppButton extends StatelessWidget {
  const _HeaderWhatsAppButton({required this.connected, required this.onTap});

  final bool connected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 42,
      child: TextButton(
        onPressed: onTap,
        style: TextButton.styleFrom(
          backgroundColor: Colors.transparent,
          foregroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(horizontal: 10),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(6)),
          textStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w900),
        ),
        child: Row(
          children: [
            Icon(
              connected ? Icons.phone_in_talk_rounded : Icons.phone_rounded,
              size: 20,
              color: const Color(0xFF00C85A),
            ),
            const SizedBox(width: 8),
            const Text('WhatsApp'),
          ],
        ),
      ),
    );
  }
}

class _HeaderIconButton extends StatelessWidget {
  const _HeaderIconButton({
    required this.icon,
    required this.onTap,
    this.label,
  });

  final IconData icon;
  final VoidCallback onTap;
  final String? label;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 42,
      child: TextButton.icon(
        onPressed: onTap,
        icon: Icon(icon, size: 19),
        label: Text(label ?? ''),
        style: TextButton.styleFrom(
          foregroundColor: const Color(0xFFF4E5D8),
          backgroundColor: Colors.transparent,
          padding: const EdgeInsets.symmetric(horizontal: 10),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(6)),
          textStyle: const TextStyle(fontSize: 12, fontWeight: FontWeight.w800),
        ),
      ),
    );
  }
}

class _HeaderDivider extends StatelessWidget {
  const _HeaderDivider();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 1,
      height: 34,
      margin: const EdgeInsets.symmetric(horizontal: 4),
      color: const Color(0xFF48433F),
    );
  }
}

// ignore: unused_element
class _HeaderOperatorCard extends StatelessWidget {
  const _HeaderOperatorCard({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 236,
      height: 54,
      child: Row(
        children: [
          Container(
            width: 42,
            height: 42,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: _rail,
              border: Border.all(color: _blue2),
              borderRadius: BorderRadius.circular(14),
            ),
            child: const Icon(
              Icons.person_outline_rounded,
              color: Color(0xFFFFA76D),
              size: 22,
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  store.operatorName.isEmpty ? 'Operador' : store.operatorName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 13,
                    fontWeight: FontWeight.w900,
                    height: 1.1,
                  ),
                ),
                const SizedBox(height: 2),
                const Text(
                  'CAIXA | Terminal 01',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: _surfaceMuted,
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                    height: 1.1,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ignore: unused_element
class _RestaurantLogo extends StatelessWidget {
  const _RestaurantLogo({required this.size});

  final double size;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(2),
        border: Border.all(color: _line),
      ),
      clipBehavior: Clip.antiAlias,
      child: Image.asset(
        'assets/restaurant-logo.webp',
        fit: BoxFit.cover,
        errorBuilder: (context, error, stackTrace) => ColoredBox(
          color: _blue,
          child: Center(
            child: Text(
              'BL',
              style: TextStyle(
                color: Colors.white,
                fontSize: size * .39,
                fontWeight: FontWeight.w900,
              ),
            ),
          ),
        ),
      ),
    );
  }
}

// ignore: unused_element
class _HeaderCashBadge extends StatelessWidget {
  const _HeaderCashBadge({required this.store, required this.cashOpen});

  final BalcaoStore store;
  final bool cashOpen;

  @override
  Widget build(BuildContext context) {
    final borderColor = cashOpen ? _borderStrong : _danger;
    final tooltip = cashOpen
        ? 'Caixa aberto ${money(store.openTotal)}'
        : 'Caixa fechado';
    return Tooltip(
      message: tooltip,
      child: Container(
        width: 76,
        height: 58,
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: _railDeep,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: borderColor),
        ),
        child: Container(
          width: 44,
          height: 44,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: _rail,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: _blue2),
          ),
          child: const Text(
            'R\$',
            style: TextStyle(
              color: Color(0xFFFFA76D),
              fontWeight: FontWeight.w900,
              fontSize: 14,
            ),
          ),
        ),
      ),
    );
  }
}

// ignore: unused_element
class _WpfConfigButton extends StatelessWidget {
  const _WpfConfigButton({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 34,
      child: OutlinedButton.icon(
        onPressed: onTap,
        icon: const Icon(Icons.settings_suggest_outlined, size: 16),
        label: const Text('Config'),
        style: OutlinedButton.styleFrom(
          backgroundColor: Colors.white,
          foregroundColor: _blue2,
          side: const BorderSide(color: _line),
          padding: const EdgeInsets.symmetric(horizontal: 12),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
          textStyle: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
        ),
      ),
    );
  }
}

class _PdvRibbon extends StatelessWidget {
  const _PdvRibbon({
    required this.store,
    required this.openModule,
    required this.openProductSearch,
    required this.toggleCash,
    this.forceCashClosed = false,
  });

  final BalcaoStore store;
  final void Function(String title, Widget child) openModule;
  final VoidCallback openProductSearch;
  final VoidCallback toggleCash;
  final bool forceCashClosed;

  @override
  Widget build(BuildContext context) {
    final cashOpen = forceCashClosed ? false : store.cashOpen;
    final groups = [
      _RibbonGroupData('Venda', [
        _RibbonItem(
          'F2',
          'Pesquisa',
          'Produtos',
          Icons.search_rounded,
          openProductSearch,
        ),
        _RibbonItem(
          'F6',
          'Transferir',
          'Comanda',
          Icons.compare_arrows_rounded,
          () {
            openModule(
              'Transferir comanda',
              _TransferOrderModule(store: store),
            );
          },
        ),
        _RibbonItem('F7', 'Desconto', 'Item', Icons.local_offer_outlined, () {
          openModule('Desconto autorizado', _DiscountDeskModule(store: store));
        }),
      ]),
      _RibbonGroupData('Cadastro', [
        _RibbonItem(
          'CL',
          'Clientes',
          'Cadastro',
          Icons.person_outline_rounded,
          () {
            openModule(
              'Cadastro de clientes',
              _CustomerDeskModule(store: store),
            );
          },
        ),
        _RibbonItem(
          'CP',
          'Produtos',
          'Cadastro',
          Icons.inventory_2_outlined,
          () {
            openModule(
              'Cadastro de produtos',
              _ProductCatalogModule(store: store),
            );
          },
        ),
        _RibbonItem(
          'EQ',
          'Equipe',
          'Garçom/Caixa',
          Icons.groups_2_outlined,
          () {
            openModule('Equipe', _TeamModule(store: store));
          },
        ),
      ]),
      _RibbonGroupData('Caixa', [
        _RibbonItem('CX', 'Caixa', 'Movimentos', Icons.wallet_outlined, () {
          openModule(
            'Caixa: entradas, retiradas e fechamento',
            _CashModule(
              store: store,
              openBlocked: () => openModule(
                'Fechamento bloqueado',
                _CashCloseBlockedModule(store: store),
              ),
            ),
          );
        }),
        _RibbonItem('CX', 'Receber', 'Pagamento', Icons.payment_rounded, () {
          openModule(
            'Receber pagamento',
            _CashModule(
              store: store,
              openBlocked: () => openModule(
                'Fechamento bloqueado',
                _CashCloseBlockedModule(store: store),
              ),
            ),
          );
        }),
      ]),
      _RibbonGroupData('Caixa', [
        _RibbonItem(
          'F10',
          cashOpen ? 'Fechar caixa' : 'Abrir caixa',
          'Clique aqui',
          cashOpen ? Icons.lock_open_rounded : Icons.lock_outline_rounded,
          toggleCash,
        ),
      ]),
      _RibbonGroupData('Pedidos', [
        _RibbonItem('IF', 'iFood', 'Pedidos', Icons.storefront_outlined, () {
          openModule('iFood Online', _DeliveryModule(store: store));
        }),
      ]),
      _RibbonGroupData('Sistema', [
        _RibbonItem('BK', 'Backup', 'Exportar', Icons.backup_outlined, () {
          openModule('Backup e exportacao', _BackupModule(store: store));
        }),
      ]),
    ];
    return Container(
      width: double.infinity,
      height: 105,
      padding: const EdgeInsets.fromLTRB(10, 4, 10, 5),
      decoration: const BoxDecoration(
        color: _surface,
        border: Border(bottom: BorderSide(color: _line)),
      ),
      child: Row(
        children: [
          const _RibbonArrow(icon: Icons.chevron_left_rounded),
          const SizedBox(width: 6),
          Expanded(
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  for (final group in groups) ...[
                    _RibbonGroup(group: group),
                    const SizedBox(width: 14),
                  ],
                ],
              ),
            ),
          ),
          const SizedBox(width: 6),
          const _RibbonArrow(icon: Icons.chevron_right_rounded),
        ],
      ),
    );
  }
}

class _RibbonGroupData {
  const _RibbonGroupData(this.title, this.items);

  final String title;
  final List<_RibbonItem> items;
}

class _RibbonGroup extends StatelessWidget {
  const _RibbonGroup({required this.group});

  final _RibbonGroupData group;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: group.items.length * 126.0,
      height: 94,
      child: Column(
        children: [
          SizedBox(
            height: 16,
            child: Row(
              children: [
                const SizedBox(width: 8),
                const Expanded(child: Divider(color: _line, height: 1)),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 10),
                  child: Text(
                    group.title,
                    style: const TextStyle(
                      color: _textSecondary,
                      fontSize: 10,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                const Expanded(child: Divider(color: _line, height: 1)),
                const SizedBox(width: 8),
              ],
            ),
          ),
          const SizedBox(height: 1),
          Row(
            children: [
              for (var i = 0; i < group.items.length; i++) ...[
                _RibbonButton(item: group.items[i]),
                if (i < group.items.length - 1) const SizedBox(width: 4),
              ],
            ],
          ),
        ],
      ),
    );
  }
}

class _RibbonArrow extends StatelessWidget {
  const _RibbonArrow({required this.icon});

  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 32,
      height: 82,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: _line),
      ),
      child: Icon(icon, color: _blue2, size: 30),
    );
  }
}

// ignore: unused_element
class _RibbonCashSummary extends StatelessWidget {
  const _RibbonCashSummary({required this.store, required this.toggleCash});

  final BalcaoStore store;
  final VoidCallback toggleCash;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: toggleCash,
      borderRadius: BorderRadius.circular(8),
      child: Container(
        width: 206,
        height: 94,
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: _line),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(
              store.cashOpen ? 'Caixa aberto' : 'Caixa fechado',
              style: TextStyle(
                color: store.cashOpen ? _textSecondary : _danger,
                fontSize: 12,
                fontWeight: FontWeight.w400,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              money(store.openTotal),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: _teal,
                fontSize: 22,
                fontWeight: FontWeight.w700,
                height: 1.05,
              ),
            ),
            const SizedBox(height: 3),
            Text(
              'Vendas hoje: ${money(store.soldToday)}',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: _textSecondary,
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _RibbonItem {
  const _RibbonItem(
    this.keyText,
    this.title,
    this.subtitle,
    this.icon,
    this.onTap,
  );

  final String keyText;
  final String title;
  final String subtitle;
  final IconData icon;
  final VoidCallback onTap;
}

class _RibbonButton extends StatelessWidget {
  const _RibbonButton({required this.item});

  final _RibbonItem item;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 122,
      height: 76,
      child: Material(
        color: _surface,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(7)),
        child: InkWell(
          onTap: item.onTap,
          borderRadius: BorderRadius.circular(7),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(item.icon, color: _navy2, size: 29),
                const SizedBox(height: 3),
                Text(
                  item.title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: _navy,
                    fontWeight: FontWeight.w700,
                    fontSize: 12,
                  ),
                ),
                Text(
                  item.subtitle,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: _textMuted,
                    fontWeight: FontWeight.w500,
                    fontSize: 10,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// ignore: unused_element
class _ModeStrip extends StatelessWidget {
  const _ModeStrip({
    required this.selected,
    required this.onChanged,
    required this.onNewDelivery,
  });

  final int selected;
  final ValueChanged<int> onChanged;
  final VoidCallback onNewDelivery;

  @override
  Widget build(BuildContext context) {
    final area = switch (selected) {
      1 => 'Cozinha',
      2 => 'Pedidos',
      3 => 'Produtos',
      _ => 'Comandas',
    };
    return Container(
      width: double.infinity,
      height: 36,
      color: Colors.transparent,
      child: LayoutBuilder(
        builder: (context, constraints) {
          final compact = constraints.maxWidth < 760;
          final tabs = [
            _ModeTabButton(
              label: 'Comanda',
              active: selected == 0,
              onTap: () => onChanged(0),
            ),
            _ModeTabButton(
              label: 'Cozinha',
              active: selected == 1,
              onTap: () => onChanged(1),
            ),
            _ModeTabButton(
              label: 'Delivery',
              active: selected == 2,
              onTap: () => onChanged(2),
            ),
          ];
          final plus = _NewDeliveryButton(onTap: onNewDelivery);

          if (compact) {
            return Row(
              children: [
                Expanded(
                  child: SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: [
                        _ModePill(children: tabs),
                        const SizedBox(width: 8),
                        plus,
                      ],
                    ),
                  ),
                ),
              ],
            );
          }

          return Row(
            children: [
              _ModePill(children: tabs),
              const SizedBox(width: 10),
              plus,
              const Spacer(),
              _ModeAreaBadge(text: 'Area: $area'),
            ],
          );
        },
      ),
    );
  }
}

class _ModeTabButton extends StatelessWidget {
  const _ModeTabButton({
    required this.label,
    required this.active,
    required this.onTap,
  });

  final String label;
  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: Container(
        width: 152,
        height: 30,
        margin: const EdgeInsets.all(2),
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: active ? _navy2 : Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: active ? _navy2 : Colors.transparent),
        ),
        child: Text(
          label,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            color: active ? Colors.white : _navy,
            fontWeight: FontWeight.w900,
            fontSize: 13,
          ),
        ),
      ),
    );
  }
}

class _NewDeliveryButton extends StatelessWidget {
  const _NewDeliveryButton({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 178,
      height: 34,
      child: FilledButton(
        onPressed: onTap,
        style: FilledButton.styleFrom(
          backgroundColor: _teal,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(11),
          ),
          textStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w900),
        ),
        child: const Text('+ Delivery'),
      ),
    );
  }
}

class _ModePill extends StatelessWidget {
  const _ModePill({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 34,
      padding: const EdgeInsets.all(2),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: _line),
      ),
      child: Row(mainAxisSize: MainAxisSize.min, children: children),
    );
  }
}

class _ModeAreaBadge extends StatelessWidget {
  const _ModeAreaBadge({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 34,
      padding: const EdgeInsets.symmetric(horizontal: 10),
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(11),
        border: Border.all(color: _line),
      ),
      child: Text(
        text,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: const TextStyle(
          color: _navy,
          fontSize: 14,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

// ignore: unused_element
class _OperationSummary extends StatelessWidget {
  const _OperationSummary({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final wide = constraints.maxWidth >= 760;
        return GridView.count(
          crossAxisCount: wide ? 4 : 2,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 8,
          crossAxisSpacing: 8,
          childAspectRatio: wide ? 2.45 : 2.05,
          children: [
            _MiniSummary(
              label: store.cashOpen ? 'Caixa aberto' : 'Caixa fechado',
              value: money(store.soldToday),
              sub: '${store.openOrders.length} comandas abertas',
              color: store.cashOpen ? _teal : _danger,
            ),
            _MiniSummary(
              label: 'Mercado Pago',
              value: money(store.mercadoPagoSales),
              sub: store.pointStatusLabel,
              color: _blue,
            ),
            _MiniSummary(
              label: 'iFood',
              value: money(store.ifoodRepasse),
              sub: 'repasse previsto',
              color: Colors.red,
            ),
            _MiniSummary(
              label: 'Sincronizacao',
              value: '${store.pendingSyncCount}',
              sub: store.lastSync.isEmpty ? 'pendente' : store.lastSync,
              color: _navy2,
            ),
          ],
        );
      },
    );
  }
}

// ignore: unused_element
class _MercadoPagoCompactPanel extends StatelessWidget {
  const _MercadoPagoCompactPanel({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    final order = store.selectedOrder;
    final canCharge =
        store.pointReady &&
        order != null &&
        order.items.isNotEmpty &&
        store.cashOpen;
    return _WindowPanel(
      title: 'Mercado Pago Point',
      action: _StatusPill(
        text: store.pointHasPending
            ? 'cobrando'
            : store.pointReady
            ? 'pronto'
            : 'off',
        color: store.pointHasPending
            ? _warn
            : store.pointReady
            ? _blue
            : _danger,
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final wide = constraints.maxWidth >= 620;
          final device = _PointTerminalMini(store: store);
          final content = Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _ReportLine(
                label: store.pointTerminalDisplay,
                detail:
                    '${store.pointSerial.isEmpty ? '-' : store.pointSerial} | ${store.pointStatusLabel}',
                value: store.pointReady ? 'ON' : 'OFF',
                color: store.pointReady ? _blue : _danger,
              ),
              _ReportLine(
                label: 'Comanda selecionada',
                detail: order == null
                    ? 'nenhuma comanda aberta'
                    : '${order.number} | ${order.customerName}'.trim(),
                value: order == null ? '-' : money(order.subtotal),
                color: _navy2,
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  _CompactPointButton(
                    label: 'Pix Point',
                    icon: Icons.qr_code_rounded,
                    enabled: canCharge,
                    onTap: () => store.sendSelectedToPoint('Pix Mercado Pago'),
                  ),
                  _CompactPointButton(
                    label: 'Debito',
                    icon: Icons.credit_card_rounded,
                    enabled: canCharge,
                    onTap: () => store.sendSelectedToPoint('Debito Point'),
                  ),
                  _CompactPointButton(
                    label: 'Credito',
                    icon: Icons.contactless_rounded,
                    enabled: canCharge,
                    onTap: () => store.sendSelectedToPoint('Credito Point'),
                  ),
                  _CompactPointButton(
                    label: 'Verificar',
                    icon: Icons.check_circle_rounded,
                    enabled: store.pointHasPending,
                    onTap: store.confirmPointPayment,
                  ),
                ],
              ),
            ],
          );

          if (!wide) {
            return Column(
              children: [
                Center(child: device),
                const SizedBox(height: 10),
                content,
              ],
            );
          }
          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              device,
              const SizedBox(width: 12),
              Expanded(child: content),
            ],
          );
        },
      ),
    );
  }
}

class _CompactPointButton extends StatelessWidget {
  const _CompactPointButton({
    required this.label,
    required this.icon,
    required this.enabled,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final bool enabled;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return FilledButton.icon(
      onPressed: enabled ? onTap : null,
      icon: Icon(icon, size: 17),
      label: Text(label),
      style: FilledButton.styleFrom(
        backgroundColor: _blue,
        foregroundColor: Colors.white,
        disabledBackgroundColor: _line,
        disabledForegroundColor: _textMuted,
        minimumSize: const Size(0, 38),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(5)),
      ),
    );
  }
}

class _PointTerminalMini extends StatelessWidget {
  const _PointTerminalMini({required this.store, this.large = false});

  final BalcaoStore store;
  final bool large;

  @override
  Widget build(BuildContext context) {
    final width = large ? 190.0 : 118.0;
    final height = large ? 278.0 : 162.0;
    final accent = store.pointReady
        ? _blue
        : store.pointConnected
        ? _warn
        : _danger;
    return Container(
      width: width,
      height: height,
      padding: EdgeInsets.all(large ? 12 : 8),
      decoration: BoxDecoration(
        color: const Color(0xFF101820),
        borderRadius: BorderRadius.circular(large ? 18 : 12),
        boxShadow: const [
          BoxShadow(
            color: Color(0x28000000),
            blurRadius: 18,
            offset: Offset(0, 8),
          ),
        ],
      ),
      child: Column(
        children: [
          Container(
            height: large ? 118 : 64,
            width: double.infinity,
            padding: EdgeInsets.all(large ? 12 : 7),
            decoration: BoxDecoration(
              color: _mint,
              borderRadius: BorderRadius.circular(large ? 12 : 8),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      width: large ? 28 : 20,
                      height: large ? 18 : 13,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(
                        color: _blue,
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(
                        'MP',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: large ? 10 : 7,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ),
                    const Spacer(),
                    Icon(
                      store.pointReady
                          ? Icons.wifi_rounded
                          : Icons.wifi_off_rounded,
                      size: large ? 18 : 12,
                      color: accent,
                    ),
                  ],
                ),
                const Spacer(),
                Text(
                  store.pointHasPending
                      ? money(store.pointPendingAmount)
                      : store.pointStatusLabel,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: _navy,
                    fontWeight: FontWeight.w900,
                    fontSize: large ? 20 : 11,
                    height: 1.05,
                  ),
                ),
              ],
            ),
          ),
          SizedBox(height: large ? 14 : 8),
          Icon(
            store.pointReady
                ? Icons.contactless_rounded
                : Icons.power_settings_new_rounded,
            color: accent,
            size: large ? 42 : 24,
          ),
          const Spacer(),
          Container(
            width: width * .58,
            height: large ? 36 : 20,
            decoration: BoxDecoration(
              color: _rail,
              borderRadius: BorderRadius.circular(999),
            ),
          ),
        ],
      ),
    );
  }
}

class _MiniSummary extends StatelessWidget {
  const _MiniSummary({
    required this.label,
    required this.value,
    required this.sub,
    required this.color,
  });

  final String label;
  final String value;
  final String sub;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(7),
        border: Border.all(color: _line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(width: 4, height: 18, color: color),
              const SizedBox(width: 7),
              Expanded(
                child: Text(
                  label,
                  softWrap: true,
                  style: const TextStyle(
                    color: _textSecondary,
                    fontWeight: FontWeight.w900,
                    fontSize: 12,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 5),
          Text(
            value,
            maxLines: 2,
            softWrap: true,
            style: const TextStyle(
              color: _navy,
              fontWeight: FontWeight.w900,
              fontSize: 19,
              height: 1.05,
            ),
          ),
          Text(
            sub,
            softWrap: true,
            style: const TextStyle(color: _textSecondary, fontSize: 11),
          ),
        ],
      ),
    );
  }
}

class _EmptyCommandPanel extends StatelessWidget {
  const _EmptyCommandPanel({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: _line),
      ),
      child: Center(
        child: Padding(
          padding: const EdgeInsets.all(28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.table_restaurant_outlined,
                size: 46,
                color: _textMuted,
              ),
              const SizedBox(height: 12),
              const Text(
                'Selecione uma mesa',
                style: TextStyle(
                  color: _navy,
                  fontSize: 22,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 6),
              const Text(
                'A comanda completa aparecerá aqui.',
                textAlign: TextAlign.center,
                style: TextStyle(color: _textSecondary),
              ),
              const SizedBox(height: 18),
              FilledButton.icon(
                onPressed: () => store.openOrder(OrderKind.table),
                icon: const Icon(Icons.add_rounded),
                label: const Text('Criar mesa'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _WindowsCommandPanel extends StatelessWidget {
  const _WindowsCommandPanel({
    required this.store,
    required this.order,
    required this.code,
    required this.quantity,
    required this.onSubmitCode,
    required this.onOpenProductSearch,
    required this.openModule,
  });

  final BalcaoStore store;
  final Order order;
  final TextEditingController code;
  final TextEditingController quantity;
  final VoidCallback onSubmitCode;
  final VoidCallback onOpenProductSearch;
  final void Function(String title, Widget child) openModule;

  void _openPayment() {
    openModule(
      'Receber pagamento',
      _CashModule(
        store: store,
        openBlocked: () => openModule(
          'Fechamento bloqueado',
          _CashCloseBlockedModule(store: store),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final elapsed = DateTime.now().difference(order.createdAt);
    final elapsedLabel =
        '${elapsed.inHours.toString().padLeft(2, '0')}:'
        '${(elapsed.inMinutes % 60).toString().padLeft(2, '0')}';
    return Container(
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 12),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: _line),
        boxShadow: const [
          BoxShadow(
            blurRadius: 18,
            offset: Offset(0, 6),
            color: Color(0x0D171717),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Mesa ${_wpfMesaNumber(order.number)}',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: _navy,
              fontSize: 27,
              height: 1,
              fontWeight: FontWeight.w900,
              letterSpacing: -.4,
            ),
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 8,
            runSpacing: 6,
            children: [
              _WpfInfoPill(
                icon: Icons.timer_outlined,
                label: statusLabel(order.status),
                emphasized: true,
              ),
              _WpfInfoPill(
                icon: Icons.room_service_outlined,
                label: 'Garçom ${order.waiter}',
              ),
              const _WpfInfoPill(
                icon: Icons.groups_2_outlined,
                label: '1 pessoa',
              ),
              _WpfInfoPill(icon: Icons.alarm_outlined, label: elapsedLabel),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Expanded(
                child: _WpfLabeledField(
                  label: 'Código do produto',
                  child: TextField(
                    controller: code,
                    textInputAction: TextInputAction.done,
                    onSubmitted: (_) => onSubmitCode(),
                    decoration: InputDecoration(
                      hintText: 'Digite ou pesquise',
                      suffixIcon: IconButton(
                        tooltip: 'Pesquisar produto',
                        onPressed: onOpenProductSearch,
                        icon: const Icon(Icons.search_rounded, size: 20),
                      ),
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 8),
              SizedBox(
                width: 86,
                child: _WpfLabeledField(
                  label: 'Quantidade',
                  child: TextField(
                    controller: quantity,
                    textAlign: TextAlign.center,
                    keyboardType: TextInputType.number,
                    textInputAction: TextInputAction.done,
                    onSubmitted: (_) => onSubmitCode(),
                  ),
                ),
              ),
              const SizedBox(width: 8),
              SizedBox(
                width: 138,
                height: 42,
                child: FilledButton(
                  onPressed: onSubmitCode,
                  child: const Text('Incluir item'),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: _surfaceMuted,
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: _line),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Expanded(
                  flex: 2,
                  child: _EditablePdvField(
                    label: 'Mesa / cliente',
                    initialValue: order.customerName.isEmpty
                        ? _wpfMesaNumber(order.number)
                        : order.customerName,
                    onSubmitted: (value) =>
                        store.updateSelectedOrderInfo(customerName: value),
                  ),
                ),
                const SizedBox(width: 7),
                Expanded(
                  child: _EditablePdvField(
                    label: 'Oper/Garçom',
                    initialValue: order.waiter,
                    keyboardType: TextInputType.number,
                    onSubmitted: (value) =>
                        store.updateSelectedOrderInfo(waiter: value),
                  ),
                ),
                const SizedBox(width: 7),
                Expanded(
                  child: _EditablePdvField(
                    label: 'Couvert',
                    initialValue: money(
                      order.coverCharge,
                    ).replaceFirst('R\$ ', ''),
                    keyboardType: TextInputType.number,
                    onSubmitted: (value) =>
                        store.updateSelectedOrderCharges(coverCharge: value),
                  ),
                ),
                const SizedBox(width: 7),
                Expanded(
                  child: _EditablePdvField(
                    label: '% Garçom',
                    initialValue: order.servicePercent.toStringAsFixed(0),
                    keyboardType: TextInputType.number,
                    onSubmitted: (value) =>
                        store.updateSelectedOrderCharges(servicePercent: value),
                  ),
                ),
                const SizedBox(width: 8),
                SizedBox(
                  width: 130,
                  height: 34,
                  child: FilledButton(
                    onPressed: () => store.updateSelectedOrderCharges(
                      servicePercent: order.servicePercent.toString(),
                    ),
                    child: const Text('Ativar taxas'),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          Expanded(
            child: _WpfTicketTable(order: order, store: store),
          ),
          const SizedBox(height: 10),
          _WpfAdvancePayments(order: order),
          const SizedBox(height: 10),
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _WpfAmountLine(
                      label: 'Total da comanda',
                      value: money(order.subtotal),
                    ),
                    const SizedBox(height: 5),
                    const _WpfAmountLine(
                      label: 'Pago antecipado',
                      value: 'R\$ 0,00',
                      accent: true,
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  const Text(
                    'Saldo da comanda',
                    style: TextStyle(
                      color: _textSecondary,
                      fontSize: 15,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  Text(
                    money(order.subtotal),
                    style: const TextStyle(
                      color: _navy,
                      fontSize: 35,
                      height: 1.05,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ],
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                flex: 10,
                child: _WpfBottomAction(
                  label: 'Transferir',
                  onPressed: () => openModule(
                    'Transferir comanda',
                    _TransferOrderModule(store: store),
                  ),
                ),
              ),
              const SizedBox(width: 7),
              Expanded(
                flex: 10,
                child: _WpfBottomAction(
                  label: 'Desconto',
                  onPressed: () => openModule(
                    'Desconto autorizado',
                    _DiscountDeskModule(store: store),
                  ),
                ),
              ),
              const SizedBox(width: 7),
              Expanded(
                flex: 10,
                child: _WpfBottomAction(
                  label: 'Receber',
                  onPressed: _openPayment,
                ),
              ),
              const SizedBox(width: 7),
              Expanded(
                flex: 13,
                child: _WpfBottomAction(
                  label: 'Fechar conta',
                  primary: true,
                  onPressed: _openPayment,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _WpfInfoPill extends StatelessWidget {
  const _WpfInfoPill({
    required this.icon,
    required this.label,
    this.emphasized = false,
  });

  final IconData icon;
  final String label;
  final bool emphasized;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
      decoration: BoxDecoration(
        color: emphasized ? const Color(0xFFFFF3DE) : _surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: emphasized ? const Color(0xFFECCB8B) : _line),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            icon,
            size: 15,
            color: emphasized ? const Color(0xFFB57100) : _danger,
          ),
          const SizedBox(width: 4),
          Text(
            label,
            style: TextStyle(
              color: emphasized ? const Color(0xFF9A6200) : _textSecondary,
              fontSize: 11,
              fontWeight: FontWeight.w900,
            ),
          ),
        ],
      ),
    );
  }
}

class _WpfLabeledField extends StatelessWidget {
  const _WpfLabeledField({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            color: _textSecondary,
            fontSize: 11,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 3),
        SizedBox(height: 42, child: child),
      ],
    );
  }
}

class _WpfTicketTable extends StatelessWidget {
  const _WpfTicketTable({required this.order, required this.store});

  final Order order;
  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: _line),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          Container(
            height: 34,
            padding: const EdgeInsets.symmetric(horizontal: 10),
            color: _surfaceMuted,
            child: const Row(
              children: [
                SizedBox(width: 66, child: _GridHeader('Código')),
                Expanded(child: _GridHeader('Produto')),
                SizedBox(width: 42, child: _GridHeader('Qtd', center: true)),
                SizedBox(width: 82, child: _GridHeader('Total', right: true)),
                SizedBox(width: 46, child: _GridHeader('Ação', right: true)),
              ],
            ),
          ),
          Expanded(
            child: order.items.isEmpty
                ? const Center(
                    child: _Empty(
                      text: 'Digite o código ou pesquise um produto.',
                    ),
                  )
                : ListView.builder(
                    itemCount: order.items.length,
                    itemBuilder: (context, index) {
                      final item = order.items[index];
                      return Container(
                        height: 46,
                        padding: const EdgeInsets.symmetric(horizontal: 10),
                        decoration: BoxDecoration(
                          color: index.isOdd ? _surfaceMuted : Colors.white,
                          border: const Border(top: BorderSide(color: _line)),
                        ),
                        child: Row(
                          children: [
                            SizedBox(
                              width: 66,
                              child: Text(
                                item.code,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  color: _textSecondary,
                                  fontSize: 11,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                            Expanded(
                              child: Text(
                                item.name,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  color: _navy,
                                  fontSize: 13,
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                            ),
                            SizedBox(
                              width: 42,
                              child: Text(
                                '${item.quantity}',
                                textAlign: TextAlign.center,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                            ),
                            SizedBox(
                              width: 82,
                              child: Text(
                                money(item.total),
                                textAlign: TextAlign.right,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w900,
                                ),
                              ),
                            ),
                            SizedBox(
                              width: 46,
                              child: IconButton(
                                tooltip: 'Excluir item',
                                visualDensity: VisualDensity.compact,
                                onPressed: () =>
                                    store.changeQty(item, -item.quantity),
                                icon: const Icon(
                                  Icons.delete_outline_rounded,
                                  color: _danger,
                                  size: 19,
                                ),
                              ),
                            ),
                          ],
                        ),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}

class _WpfAdvancePayments extends StatelessWidget {
  const _WpfAdvancePayments({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: _line),
      ),
      child: Column(
        children: [
          Container(
            height: 32,
            padding: const EdgeInsets.symmetric(horizontal: 10),
            decoration: const BoxDecoration(
              color: _surfaceMuted,
              borderRadius: BorderRadius.vertical(top: Radius.circular(10)),
              border: Border(bottom: BorderSide(color: _line)),
            ),
            child: Row(
              children: [
                const Expanded(
                  child: Text(
                    'Pagamentos antecipados',
                    style: TextStyle(
                      color: _textSecondary,
                      fontSize: 12,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
                OutlinedButton.icon(
                  onPressed: () {},
                  icon: const Icon(Icons.add_rounded, size: 15),
                  label: const Text('Adicionar'),
                  style: OutlinedButton.styleFrom(
                    minimumSize: const Size(0, 25),
                    padding: const EdgeInsets.symmetric(horizontal: 8),
                    textStyle: const TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
              ],
            ),
          ),
          SizedBox(
            height: 31,
            child: Center(
              child: Text(
                order.paymentMethod.isEmpty
                    ? 'Nenhum pagamento antecipado'
                    : '${order.paymentMethod} • ${money(order.subtotal)}',
                style: const TextStyle(color: _textSecondary, fontSize: 11),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _WpfAmountLine extends StatelessWidget {
  const _WpfAmountLine({
    required this.label,
    required this.value,
    this.accent = false,
  });

  final String label;
  final String value;
  final bool accent;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Text(
            label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              color: accent ? _danger : _textSecondary,
              fontSize: 11,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
        const SizedBox(width: 6),
        Text(
          value,
          maxLines: 1,
          style: TextStyle(
            color: accent ? _danger : _navy,
            fontSize: 12,
            fontWeight: FontWeight.w900,
          ),
        ),
      ],
    );
  }
}

class _WpfBottomAction extends StatelessWidget {
  const _WpfBottomAction({
    required this.label,
    required this.onPressed,
    this.primary = false,
  });

  final String label;
  final VoidCallback onPressed;
  final bool primary;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 48,
      child: primary
          ? FilledButton(onPressed: onPressed, child: Text(label, maxLines: 1))
          : OutlinedButton(
              onPressed: onPressed,
              style: OutlinedButton.styleFrom(
                foregroundColor: _navy,
                backgroundColor: Colors.white,
              ),
              child: Text(label, maxLines: 1),
            ),
    );
  }
}

String _wpfMesaNumber(String value) {
  final digits = value.replaceAll(RegExp(r'[^0-9]'), '');
  return (digits.isEmpty ? value : digits).padLeft(6, '0');
}

// ignore: unused_element
class _LegacyWindowsCommandPanel extends StatelessWidget {
  const _LegacyWindowsCommandPanel({
    required this.store,
    required this.order,
    required this.code,
    required this.quantity,
    required this.onSubmitCode,
    required this.onOpenProductSearch,
  });

  final BalcaoStore store;
  final Order order;
  final TextEditingController code;
  final TextEditingController quantity;
  final VoidCallback onSubmitCode;
  final VoidCallback onOpenProductSearch;

  @override
  Widget build(BuildContext context) {
    final compact = MediaQuery.sizeOf(context).width < 700;
    if (!compact) {
      return _WindowPanel(
        title: 'Comanda',
        plainTitle: true,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                SizedBox(
                  width: 88,
                  child: _EditablePdvField(
                    label: 'Mesa / cliente',
                    initialValue: order.customerName.isEmpty
                        ? order.number
                        : order.customerName,
                    onSubmitted: (value) =>
                        store.updateSelectedOrderInfo(customerName: value),
                  ),
                ),
                const SizedBox(width: 8),
                SizedBox(
                  width: 96,
                  child: _EditablePdvField(
                    label: 'Oper/Garcom',
                    initialValue: order.waiter,
                    keyboardType: TextInputType.number,
                    onSubmitted: (value) =>
                        store.updateSelectedOrderInfo(waiter: value),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const _FieldLabel('Codigo'),
                      const SizedBox(height: 3),
                      SizedBox(
                        height: 34,
                        child: TextField(
                          controller: code,
                          textInputAction: TextInputAction.done,
                          onSubmitted: (_) => onSubmitCode(),
                          decoration: const InputDecoration(
                            isDense: true,
                            filled: true,
                            fillColor: Colors.white,
                            contentPadding: EdgeInsets.symmetric(
                              horizontal: 8,
                              vertical: 4,
                            ),
                            border: OutlineInputBorder(),
                            enabledBorder: OutlineInputBorder(
                              borderSide: BorderSide(color: _borderStrong),
                            ),
                            focusedBorder: OutlineInputBorder(
                              borderSide: BorderSide(color: _blue2, width: 1.2),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 8),
                SizedBox(
                  width: 58,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const _FieldLabel('Qtd'),
                      const SizedBox(height: 3),
                      SizedBox(
                        height: 34,
                        child: TextField(
                          controller: quantity,
                          keyboardType: TextInputType.number,
                          textInputAction: TextInputAction.done,
                          onSubmitted: (_) => onSubmitCode(),
                          decoration: const InputDecoration(
                            isDense: true,
                            filled: true,
                            fillColor: Colors.white,
                            contentPadding: EdgeInsets.symmetric(
                              horizontal: 8,
                              vertical: 4,
                            ),
                            border: OutlineInputBorder(),
                            enabledBorder: OutlineInputBorder(
                              borderSide: BorderSide(color: _borderStrong),
                            ),
                            focusedBorder: OutlineInputBorder(
                              borderSide: BorderSide(color: _blue2, width: 1.2),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            Row(
              children: [
                SizedBox(
                  width: 86,
                  child: _EditablePdvField(
                    label: 'Couvert',
                    initialValue: money(
                      order.coverCharge,
                    ).replaceFirst('R\$ ', ''),
                    keyboardType: TextInputType.number,
                    onSubmitted: (value) =>
                        store.updateSelectedOrderCharges(coverCharge: value),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _EditablePdvField(
                    label: '% Garcom',
                    initialValue: order.servicePercent.toStringAsFixed(0),
                    keyboardType: TextInputType.number,
                    onSubmitted: (value) =>
                        store.updateSelectedOrderCharges(servicePercent: value),
                  ),
                ),
                const SizedBox(width: 10),
                Padding(
                  padding: const EdgeInsets.only(top: 19),
                  child: SizedBox(
                    width: 112,
                    height: 38,
                    child: FilledButton(
                      onPressed: onSubmitCode,
                      style: FilledButton.styleFrom(
                        backgroundColor: _teal,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(5),
                        ),
                      ),
                      child: const Text(
                        'Ativar',
                        style: TextStyle(fontWeight: FontWeight.w600),
                      ),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            _TicketGrid(order: order, store: store),
            const SizedBox(height: 12),
            _PaymentsPreview(order: order, store: store),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${store.cashOpen ? 'Caixa aberto' : 'Caixa fechado'}  |  MESA ${order.number}  |  ${statusLabel(order.status).toUpperCase()}  |  Pag...',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: _danger,
                          fontWeight: FontWeight.w900,
                          fontSize: 12,
                        ),
                      ),
                      const Text(
                        'F5 fecha conta  |  F8 antecipado  |  F9 recebe paga...',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(color: _textSecondary, fontSize: 11),
                      ),
                    ],
                  ),
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    const Text(
                      'Total da comanda',
                      style: TextStyle(
                        color: _textSecondary,
                        fontWeight: FontWeight.w900,
                        fontSize: 11,
                      ),
                    ),
                    Text(
                      money(order.subtotal),
                      style: const TextStyle(
                        color: _danger,
                        fontWeight: FontWeight.w700,
                        fontSize: 32,
                        height: 1,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ],
        ),
      );
    }

    return _WindowPanel(
      title: 'Comanda',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: _EditablePdvField(
                  label: 'Mesa / cliente',
                  initialValue: order.customerName.isEmpty
                      ? order.number
                      : order.customerName,
                  onSubmitted: (value) =>
                      store.updateSelectedOrderInfo(customerName: value),
                ),
              ),
              const SizedBox(width: 8),
              SizedBox(
                width: compact ? 96 : 86,
                child: _EditablePdvField(
                  label: 'Garcom',
                  initialValue: order.waiter,
                  keyboardType: TextInputType.number,
                  onSubmitted: (value) =>
                      store.updateSelectedOrderInfo(waiter: value),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          _ProductPickField(
            controller: code,
            quantity: quantity,
            onOpenSearch: onOpenProductSearch,
            onSubmitCode: onSubmitCode,
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: _EditablePdvField(
                  label: 'Couvert',
                  initialValue: money(
                    order.coverCharge,
                  ).replaceFirst('R\$ ', ''),
                  keyboardType: TextInputType.number,
                  onSubmitted: (value) =>
                      store.updateSelectedOrderCharges(coverCharge: value),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _EditablePdvField(
                  label: '% Garcom',
                  initialValue: order.servicePercent.toStringAsFixed(0),
                  keyboardType: TextInputType.number,
                  onSubmitted: (value) =>
                      store.updateSelectedOrderCharges(servicePercent: value),
                ),
              ),
            ],
          ),
          if (order.coverCharge > 0 || order.servicePercent > 0) ...[
            const SizedBox(height: 8),
            _ChargeSummary(order: order),
          ],
          const SizedBox(height: 10),
          _TicketGrid(order: order, store: store),
          const SizedBox(height: 10),
          _PaymentsPreview(order: order, store: store),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      store.cashOpen
                          ? 'Caixa aberto | Comanda ativa'
                          : 'Caixa fechado',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: _danger,
                        fontWeight: FontWeight.w900,
                        fontSize: 12,
                      ),
                    ),
                    Text(
                      MediaQuery.sizeOf(context).width < 700
                          ? store.cashOpen
                                ? 'Toque no pagamento para receber'
                                : 'Toque no pagamento para abrir o caixa e receber'
                          : 'F5 fecha | F8 antecipado | F9 recebe',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: _textSecondary,
                        fontSize: 11,
                      ),
                    ),
                  ],
                ),
              ),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  const Text(
                    'Total da comanda',
                    style: TextStyle(
                      color: _textSecondary,
                      fontWeight: FontWeight.w900,
                      fontSize: 11,
                    ),
                  ),
                  Text(
                    money(order.subtotal),
                    style: const TextStyle(
                      color: _blue2,
                      fontWeight: FontWeight.w900,
                      fontSize: 28,
                    ),
                  ),
                ],
              ),
            ],
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 7,
            runSpacing: 7,
            children: store.paymentMethods
                .take(5)
                .map(
                  (method) => _SmallPayButton(
                    label: method,
                    onTap: () => store.isMercadoPagoMethod(method)
                        ? store.sendSelectedToPoint(method)
                        : store.closeSelected(method),
                  ),
                )
                .toList(),
          ),
        ],
      ),
    );
  }
}

class _ProductPickField extends StatelessWidget {
  const _ProductPickField({
    required this.controller,
    required this.quantity,
    required this.onOpenSearch,
    required this.onSubmitCode,
  });

  final TextEditingController controller;
  final TextEditingController quantity;
  final VoidCallback onOpenSearch;
  final VoidCallback onSubmitCode;

  @override
  Widget build(BuildContext context) {
    final compact = MediaQuery.sizeOf(context).width < 700;
    final quantityField = SizedBox(
      width: compact ? 78 : 70,
      child: TextField(
        controller: quantity,
        keyboardType: TextInputType.number,
        textInputAction: TextInputAction.done,
        onSubmitted: (_) => onSubmitCode(),
        decoration: InputDecoration(
          labelText: 'Qtd',
          isDense: true,
          filled: true,
          fillColor: Colors.white,
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 10,
            vertical: 12,
          ),
          border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: _borderStrong),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: _navy2, width: 1.4),
          ),
        ),
      ),
    );
    if (compact) {
      return Row(
        children: [
          Expanded(
            child: InkWell(
              onTap: onOpenSearch,
              borderRadius: BorderRadius.circular(9),
              child: Container(
                constraints: const BoxConstraints(minHeight: 58),
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 10,
                ),
                decoration: BoxDecoration(
                  color: _surfaceMuted,
                  borderRadius: BorderRadius.circular(9),
                  border: Border.all(color: _line),
                ),
                child: const Row(
                  children: [
                    Icon(Icons.search_rounded, color: _navy2, size: 22),
                    SizedBox(width: 10),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            'Adicionar produto',
                            style: TextStyle(
                              color: _navy,
                              fontWeight: FontWeight.w900,
                              fontSize: 15,
                            ),
                          ),
                          Text(
                            'Pesquisar por nome, grupo ou codigo',
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              color: _textSecondary,
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ],
                      ),
                    ),
                    Icon(Icons.add_circle_rounded, color: _teal, size: 22),
                  ],
                ),
              ),
            ),
          ),
          const SizedBox(width: 8),
          quantityField,
        ],
      );
    }
    return Row(
      children: [
        Expanded(
          child: InkWell(
            onTap: onOpenSearch,
            borderRadius: BorderRadius.circular(8),
            child: AbsorbPointer(
              absorbing: compact,
              child: TextField(
                controller: controller,
                textInputAction: TextInputAction.search,
                onSubmitted: (_) => onSubmitCode(),
                decoration: InputDecoration(
                  labelText: 'Produto / codigo',
                  hintText: compact
                      ? 'Tocar para pesquisar produto'
                      : 'Digite codigo ou toque para pesquisar',
                  prefixIcon: const Icon(Icons.search_rounded),
                  suffixIcon: IconButton(
                    onPressed: onOpenSearch,
                    icon: const Icon(Icons.tune_rounded),
                    tooltip: 'Pesquisar produto',
                  ),
                  isDense: true,
                  filled: true,
                  fillColor: Colors.white,
                  contentPadding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 12,
                  ),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                  enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8),
                    borderSide: const BorderSide(color: _borderStrong),
                  ),
                  focusedBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8),
                    borderSide: const BorderSide(color: _navy2, width: 1.4),
                  ),
                ),
              ),
            ),
          ),
        ),
        const SizedBox(width: 8),
        quantityField,
      ],
    );
  }
}

class _ChargeSummary extends StatelessWidget {
  const _ChargeSummary({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: _surfaceMuted,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: const Color(0xFFC8EDEA)),
      ),
      child: Wrap(
        spacing: 12,
        runSpacing: 4,
        children: [
          Text(
            'Itens ${money(order.itemsTotal)}',
            style: const TextStyle(
              color: _navy,
              fontWeight: FontWeight.w800,
              fontSize: 12,
            ),
          ),
          if (order.coverCharge > 0)
            Text(
              'Couvert ${money(order.coverCharge)}',
              style: const TextStyle(
                color: _navy,
                fontWeight: FontWeight.w800,
                fontSize: 12,
              ),
            ),
          if (order.servicePercent > 0)
            Text(
              'Garcom ${money(order.serviceAmount)}',
              style: const TextStyle(
                color: _navy,
                fontWeight: FontWeight.w800,
                fontSize: 12,
              ),
            ),
        ],
      ),
    );
  }
}

class _EditablePdvField extends StatefulWidget {
  const _EditablePdvField({
    required this.label,
    required this.initialValue,
    required this.onSubmitted,
    this.keyboardType,
  });

  final String label;
  final String initialValue;
  final ValueChanged<String> onSubmitted;
  final TextInputType? keyboardType;

  @override
  State<_EditablePdvField> createState() => _EditablePdvFieldState();
}

class _FieldLabel extends StatelessWidget {
  const _FieldLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(
      text,
      maxLines: 1,
      overflow: TextOverflow.ellipsis,
      style: const TextStyle(
        color: _textSecondary,
        fontSize: 12,
        fontWeight: FontWeight.w600,
      ),
    );
  }
}

class _EditablePdvFieldState extends State<_EditablePdvField> {
  late final TextEditingController controller;
  late String lastValue;

  @override
  void initState() {
    super.initState();
    controller = TextEditingController(text: widget.initialValue);
    lastValue = widget.initialValue;
  }

  @override
  void didUpdateWidget(covariant _EditablePdvField oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.initialValue != oldWidget.initialValue &&
        widget.initialValue != controller.text) {
      controller.text = widget.initialValue;
      lastValue = widget.initialValue;
    }
  }

  @override
  void dispose() {
    _commit();
    controller.dispose();
    super.dispose();
  }

  void _commit() {
    final value = controller.text.trim();
    if (value == lastValue.trim()) return;
    lastValue = value;
    widget.onSubmitted(value);
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          widget.label,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(
            color: _textSecondary,
            fontSize: 12,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 3),
        SizedBox(
          height: 34,
          child: TextField(
            controller: controller,
            keyboardType: widget.keyboardType,
            textInputAction: TextInputAction.done,
            onSubmitted: (_) => _commit(),
            onEditingComplete: _commit,
            onTapOutside: (_) {
              _commit();
              FocusScope.of(context).unfocus();
            },
            decoration: const InputDecoration(
              isDense: true,
              filled: true,
              fillColor: Colors.white,
              contentPadding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              border: OutlineInputBorder(),
              enabledBorder: OutlineInputBorder(
                borderSide: BorderSide(color: _borderStrong),
              ),
              focusedBorder: OutlineInputBorder(
                borderSide: BorderSide(color: _blue2, width: 1.2),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _WindowPanel extends StatelessWidget {
  const _WindowPanel({
    required this.title,
    required this.child,
    this.action,
    this.plainTitle = false,
  });

  final String title;
  final Widget child;
  final Widget? action;
  final bool plainTitle;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: _line),
        boxShadow: const [
          BoxShadow(
            blurRadius: 28,
            offset: Offset(0, 10),
            color: Color(0x12101828),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: plainTitle
            ? [
                Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: _navy,
                          fontSize: 24,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 12),
                      child,
                    ],
                  ),
                ),
              ]
            : [
                Container(
                  constraints: const BoxConstraints(minHeight: 44),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 8,
                  ),
                  decoration: const BoxDecoration(
                    color: _surface,
                    border: Border(bottom: BorderSide(color: _line)),
                    borderRadius: BorderRadius.vertical(
                      top: Radius.circular(14),
                    ),
                  ),
                  child: Row(
                    children: [
                      Container(
                        width: 4,
                        height: 22,
                        margin: const EdgeInsets.only(right: 10),
                        decoration: BoxDecoration(
                          color: _blue,
                          borderRadius: BorderRadius.circular(2),
                        ),
                      ),
                      Expanded(
                        child: Text(
                          title,
                          maxLines: 2,
                          softWrap: true,
                          style: const TextStyle(
                            color: _navy,
                            fontSize: 15,
                            height: 1.1,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                      ?action,
                    ],
                  ),
                ),
                Padding(padding: const EdgeInsets.all(10), child: child),
              ],
      ),
    );
  }
}

class _TicketGrid extends StatelessWidget {
  const _TicketGrid({required this.order, required this.store});

  final Order order;
  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    final compact = MediaQuery.sizeOf(context).width < 620;
    if (compact) {
      return Container(
        decoration: BoxDecoration(
          border: Border.all(color: _line),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Column(
          children: [
            Container(
              height: 32,
              width: double.infinity,
              alignment: Alignment.centerLeft,
              padding: const EdgeInsets.symmetric(horizontal: 10),
              decoration: const BoxDecoration(
                color: _surfaceMuted,
                borderRadius: BorderRadius.vertical(top: Radius.circular(8)),
                border: Border(bottom: BorderSide(color: _line)),
              ),
              child: Text(
                order.items.isEmpty
                    ? 'Itens da comanda'
                    : '${order.itemsCount} item(ns) na comanda',
                style: const TextStyle(
                  color: _textSecondary,
                  fontWeight: FontWeight.w900,
                  fontSize: 12,
                ),
              ),
            ),
            if (order.items.isEmpty)
              const Padding(
                padding: EdgeInsets.all(16),
                child: _Empty(text: 'Toque em Adicionar produto.'),
              )
            else
              ...order.items.map(
                (item) => Padding(
                  padding: const EdgeInsets.fromLTRB(10, 8, 10, 0),
                  child: _MobileTicketItem(item: item, store: store),
                ),
              ),
            const SizedBox(height: 8),
          ],
        ),
      );
    }
    return SizedBox(
      height: 210,
      child: Container(
        decoration: BoxDecoration(
          border: Border.all(color: _line),
          borderRadius: BorderRadius.circular(6),
        ),
        child: Column(
          children: [
            Container(
              height: 31,
              color: _surfaceMuted,
              padding: const EdgeInsets.symmetric(horizontal: 8),
              child: const Row(
                children: [
                  SizedBox(width: 58, child: _GridHeader('Codigo')),
                  Expanded(child: _GridHeader('Produto')),
                  SizedBox(width: 36, child: _GridHeader('Qtd', center: true)),
                  SizedBox(width: 78, child: _GridHeader('Total', right: true)),
                  SizedBox(width: 62, child: _GridHeader('Acao', right: true)),
                ],
              ),
            ),
            Expanded(
              child: order.items.isEmpty
                  ? const Center(
                      child: _Empty(
                        text: 'Digite o codigo ou toque em um produto.',
                      ),
                    )
                  : ListView(
                      children: order.items
                          .map(
                            (item) => Container(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 8,
                                vertical: 9,
                              ),
                              decoration: const BoxDecoration(
                                border: Border(top: BorderSide(color: _line)),
                              ),
                              child: Row(
                                children: [
                                  SizedBox(
                                    width: 58,
                                    child: Text(
                                      item.code,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w800,
                                        fontSize: 12,
                                      ),
                                    ),
                                  ),
                                  Expanded(
                                    child: Text(
                                      item.name,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w900,
                                      ),
                                    ),
                                  ),
                                  SizedBox(
                                    width: 36,
                                    child: Text(
                                      '${item.quantity}',
                                      textAlign: TextAlign.center,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w900,
                                      ),
                                    ),
                                  ),
                                  SizedBox(
                                    width: 78,
                                    child: Text(
                                      money(item.total),
                                      textAlign: TextAlign.right,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w900,
                                      ),
                                    ),
                                  ),
                                  SizedBox(
                                    width: 62,
                                    child: Align(
                                      alignment: Alignment.centerRight,
                                      child: SizedBox(
                                        height: 24,
                                        child: TextButton(
                                          onPressed: () => store.changeQty(
                                            item,
                                            -item.quantity,
                                          ),
                                          style: TextButton.styleFrom(
                                            backgroundColor: _danger,
                                            foregroundColor: Colors.white,
                                            padding: const EdgeInsets.symmetric(
                                              horizontal: 6,
                                            ),
                                            shape: RoundedRectangleBorder(
                                              borderRadius:
                                                  BorderRadius.circular(2),
                                            ),
                                            textStyle: const TextStyle(
                                              fontSize: 11,
                                              fontWeight: FontWeight.w600,
                                            ),
                                          ),
                                          child: const Text('Excluir'),
                                        ),
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          )
                          .toList(),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MobileTicketItem extends StatelessWidget {
  const _MobileTicketItem({required this.item, required this.store});

  final OrderItem item;
  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: _line),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.name,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _navy,
                    fontWeight: FontWeight.w900,
                    fontSize: 14,
                    height: 1.05,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  '${item.code} | ${money(item.price)} un.',
                  style: const TextStyle(
                    color: _textSecondary,
                    fontWeight: FontWeight.w700,
                    fontSize: 11,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Container(
            height: 34,
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(18),
              border: Border.all(color: _line),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                IconButton(
                  visualDensity: VisualDensity.compact,
                  onPressed: () => store.changeQty(item, -1),
                  icon: const Icon(Icons.remove_rounded, size: 17),
                ),
                Text(
                  '${item.quantity}',
                  style: const TextStyle(
                    color: _navy,
                    fontWeight: FontWeight.w900,
                    fontSize: 13,
                  ),
                ),
                IconButton(
                  visualDensity: VisualDensity.compact,
                  onPressed: () => store.changeQty(item, 1),
                  icon: const Icon(Icons.add_rounded, size: 17),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          SizedBox(
            width: 76,
            child: Text(
              money(item.total),
              textAlign: TextAlign.right,
              style: const TextStyle(
                color: _navy2,
                fontWeight: FontWeight.w900,
                fontSize: 13,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _GridHeader extends StatelessWidget {
  const _GridHeader(this.text, {this.center = false, this.right = false});

  final String text;
  final bool center;
  final bool right;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: right
          ? Alignment.centerRight
          : center
          ? Alignment.center
          : Alignment.centerLeft,
      child: Text(
        text,
        style: const TextStyle(
          color: _textSecondary,
          fontWeight: FontWeight.w900,
          fontSize: 12,
        ),
      ),
    );
  }
}

class _PaymentsPreview extends StatelessWidget {
  const _PaymentsPreview({required this.order, required this.store});

  final Order order;
  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    final integrated = store.mercadoPagoCheckoutActive;
    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: _line),
        borderRadius: BorderRadius.circular(6),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            height: 28,
            width: double.infinity,
            alignment: Alignment.centerLeft,
            padding: const EdgeInsets.symmetric(horizontal: 8),
            color: _surfaceMuted,
            child: Text(
              'Pagamentos antecipados / saldo',
              style: const TextStyle(
                color: _textSecondary,
                fontWeight: FontWeight.w900,
                fontSize: 12,
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(9),
            child: Text(
              store.pointHasPending
                  ? '${store.pointChargeMethod} aguardando na Point: ${money(store.pointPendingAmount)}'
                  : order.paymentMethod.isEmpty
                  ? integrated
                        ? 'Point pronta | Pix, debito, credito e dinheiro'
                        : 'Dinheiro, Pix, debito, credito e fiado'
                  : '${order.paymentMethod} ${money(order.subtotal)}',
              style: const TextStyle(
                color: _navy,
                fontWeight: FontWeight.w800,
                fontSize: 12,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _SmallPayButton extends StatelessWidget {
  const _SmallPayButton({required this.label, required this.onTap});

  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final isPoint = label.contains('Point') || label.contains('Mercado Pago');
    final isPix = label.contains('Pix');
    final compact = MediaQuery.sizeOf(context).width < 620;
    final displayLabel = compact
        ? label
              .replaceAll('Mercado Pago', 'MP')
              .replaceAll(' Point', '')
              .replaceAll('Debito', 'Debito')
              .replaceAll('Credito', 'Credito')
        : label;
    return SizedBox(
      height: compact ? 38 : 40,
      child: FilledButton(
        onPressed: onTap,
        style: FilledButton.styleFrom(
          backgroundColor: label == 'Fiado'
              ? _warn
              : isPoint || isPix
              ? _blue
              : _navy2,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(5)),
          padding: EdgeInsets.symmetric(horizontal: compact ? 10 : 12),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              isPix
                  ? Icons.qr_code_rounded
                  : isPoint
                  ? Icons.contactless_rounded
                  : Icons.payments_rounded,
              size: 16,
            ),
            const SizedBox(width: 6),
            Text(
              displayLabel,
              maxLines: 2,
              textAlign: TextAlign.center,
              style: TextStyle(
                fontWeight: FontWeight.w900,
                height: 1.05,
                fontSize: compact ? 11 : 12,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _WindowsBoardPanel extends StatelessWidget {
  const _WindowsBoardPanel({required this.store, this.filter = 'Todas'});

  final BalcaoStore store;
  final String filter;

  @override
  Widget build(BuildContext context) {
    final tableOrders = store.openOrders
        .where((order) => order.kind == OrderKind.table)
        .toList();
    return Container(
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: _line),
        boxShadow: const [
          BoxShadow(
            blurRadius: 18,
            offset: Offset(0, 6),
            color: Color(0x0D171717),
          ),
        ],
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          Container(
            height: 66,
            padding: const EdgeInsets.symmetric(horizontal: 14),
            decoration: const BoxDecoration(
              border: Border(bottom: BorderSide(color: _line)),
            ),
            child: Row(
              children: [
                const Expanded(
                  child: Text(
                    'MESAS / COMANDAS',
                    style: TextStyle(
                      color: _navy,
                      fontSize: 16,
                      fontWeight: FontWeight.w900,
                      letterSpacing: .2,
                    ),
                  ),
                ),
                OutlinedButton(
                  onPressed: () => store.openOrder(OrderKind.table),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: _navy,
                    backgroundColor: _surface,
                    minimumSize: const Size(124, 38),
                  ),
                  child: const Text('Criar mesas'),
                ),
              ],
            ),
          ),
          Expanded(
            child: LayoutBuilder(
              builder: (context, constraints) {
                final columns = constraints.maxWidth < 640 ? 2 : 3;
                final visibleSlots = math.max(12, tableOrders.length);
                final slots =
                    List.generate(visibleSlots, (index) {
                      final number = (index + 1).toString().padLeft(6, '0');
                      final order = tableOrders
                          .where(
                            (candidate) =>
                                _wpfMesaNumber(candidate.number) == number,
                          )
                          .firstOrNull;
                      return (number: number, order: order);
                    }).where((slot) {
                      final order = slot.order;
                      return switch (filter) {
                        'Livres' => order == null || order.items.isEmpty,
                        'Ocupadas' =>
                          order != null &&
                              order.items.isNotEmpty &&
                              order.paymentMethod.trim().isEmpty,
                        'Conta' =>
                          order != null &&
                              order.items.isNotEmpty &&
                              order.paymentMethod.trim().isNotEmpty,
                        _ => true,
                      };
                    }).toList();
                return GridView.builder(
                  padding: const EdgeInsets.all(14),
                  gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: columns,
                    crossAxisSpacing: 12,
                    mainAxisSpacing: 12,
                    childAspectRatio: columns == 3
                        ? (constraints.maxWidth >= 900 ? 1.74 : 1.62)
                        : 1.68,
                  ),
                  itemCount: slots.length,
                  itemBuilder: (context, index) {
                    final number = slots[index].number;
                    final order = slots[index].order;
                    return _WpfBoardTile(
                      store: store,
                      number: number,
                      order: order,
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _WpfBoardTile extends StatelessWidget {
  const _WpfBoardTile({
    required this.store,
    required this.number,
    required this.order,
  });

  final BalcaoStore store;
  final String number;
  final Order? order;

  @override
  Widget build(BuildContext context) {
    final current = order;
    final selected = current != null && store.selectedOrderId == current.id;
    final occupied = current != null && current.items.isNotEmpty;
    final accountRequested =
        occupied && current.paymentMethod.trim().isNotEmpty;
    final elapsed = current == null
        ? Duration.zero
        : DateTime.now().difference(current.createdAt);
    final elapsedLabel =
        '${elapsed.inMinutes.toString().padLeft(2, '0')}:'
        '${(elapsed.inSeconds % 60).toString().padLeft(2, '0')}';
    final accent = accountRequested
        ? const Color(0xFFC89443)
        : occupied
        ? const Color(0xFFFF9292)
        : const Color(0xFF82DB76);

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () {
          if (current != null) {
            store.selectOrder(current.id);
          } else {
            store.openOrder(OrderKind.table, number: number);
          }
        },
        borderRadius: BorderRadius.circular(14),
        child: Ink(
          decoration: BoxDecoration(
            color: selected
                ? const Color(0xFFFFEEE1)
                : occupied
                ? const Color(0xFFFFF9F3)
                : _surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(
              color: selected ? const Color(0xFF252525) : _line,
              width: selected ? 2 : 1,
            ),
          ),
          child: Stack(
            children: [
              Positioned(
                left: 0,
                top: 0,
                bottom: 0,
                child: Container(
                  width: 7,
                  decoration: BoxDecoration(
                    color: accent,
                    borderRadius: const BorderRadius.horizontal(
                      left: Radius.circular(13),
                    ),
                  ),
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(18, 11, 14, 9),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            number,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: _navy,
                              fontSize: 22,
                              height: 1,
                              fontWeight: FontWeight.w900,
                              letterSpacing: -.4,
                            ),
                          ),
                        ),
                        const Icon(
                          Icons.table_restaurant_outlined,
                          size: 25,
                          color: _textSecondary,
                        ),
                      ],
                    ),
                    const SizedBox(height: 7),
                    Row(
                      children: [
                        const Icon(
                          Icons.groups_2_outlined,
                          size: 15,
                          color: _danger,
                        ),
                        const SizedBox(width: 3),
                        const Text(
                          '1 pessoa(s)',
                          style: TextStyle(
                            color: _textSecondary,
                            fontSize: 10,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(width: 10),
                        const Icon(
                          Icons.room_service_outlined,
                          size: 15,
                          color: _danger,
                        ),
                        const SizedBox(width: 3),
                        Expanded(
                          child: Text(
                            'Garçom ${current?.waiter ?? '2'}',
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: _textSecondary,
                              fontSize: 10,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                      ],
                    ),
                    if (current != null &&
                        current.customerName.trim().isNotEmpty) ...[
                      const SizedBox(height: 3),
                      Text(
                        current.customerName.trim().toUpperCase(),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: _navy,
                          fontSize: 11,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ],
                    const Spacer(),
                    if (occupied)
                      Text(
                        elapsedLabel,
                        style: const TextStyle(
                          color: Color(0xFFA66A00),
                          fontSize: 11,
                          fontWeight: FontWeight.w900,
                        ),
                      )
                    else
                      const SizedBox(height: 13),
                    const SizedBox(height: 5),
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        _TinyStatusPill(
                          text: accountRequested
                              ? 'CONTA'
                              : occupied
                              ? 'OCUPADA'
                              : 'LIVRE',
                          color: accountRequested
                              ? const Color(0xFFC89443)
                              : occupied
                              ? const Color(0xFFFF9292)
                              : const Color(0xFF84DB75),
                        ),
                        const Spacer(),
                        Text(
                          money(current?.subtotal ?? 0),
                          style: const TextStyle(
                            color: _navy,
                            fontSize: 13,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ignore: unused_element
class _LegacyWindowsBoardPanel extends StatelessWidget {
  const _LegacyWindowsBoardPanel({required this.store, required this.dense});

  final BalcaoStore store;
  final bool dense;

  @override
  Widget build(BuildContext context) {
    final orders = store.openOrders;
    return _WindowPanel(
      title: 'Comandas / Mesas',
      action: TextButton(
        onPressed: () => store.openOrder(OrderKind.table),
        child: const Text('Criar mesas'),
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final compact = constraints.maxWidth < 620;
          final gap = compact ? 8.0 : 8.0;
          final columns = compact
              ? (constraints.maxWidth >= 430 ? 3 : 2)
              : null;
          final preferredWidth = dense ? 118.0 : 132.0;
          final tileWidth = compact
              ? (constraints.maxWidth - gap * ((columns ?? 1) - 1)) /
                    (columns ?? 1)
              : math
                    .min(preferredWidth, ((constraints.maxWidth - gap * 2) / 3))
                    .clamp(106.0, 132.0)
                    .toDouble();
          return Wrap(
            spacing: gap,
            runSpacing: compact ? 8 : 12,
            children: [
              ...orders.map(
                (order) =>
                    _BoardTile(store: store, order: order, width: tileWidth),
              ),
              ...List.generate(
                (12 - orders.length).clamp(0, 12),
                (index) => _EmptyBoardTile(
                  width: tileWidth,
                  number: (orders.length + index + 1).toString().padLeft(
                    6,
                    '0',
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}

class _EmptyBoardTile extends StatelessWidget {
  const _EmptyBoardTile({required this.width, required this.number});

  final double width;
  final String number;

  @override
  Widget build(BuildContext context) {
    final mesa = int.parse(number).toString().padLeft(2, '0');
    return Container(
      width: width,
      height: 124,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: _line),
      ),
      clipBehavior: Clip.antiAlias,
      child: Stack(
        children: [
          Positioned(
            top: 0,
            left: 0,
            right: 0,
            child: Container(
              height: 7,
              decoration: const BoxDecoration(
                color: Color(0xFF7ED574),
                borderRadius: BorderRadius.vertical(top: Radius.circular(14)),
              ),
            ),
          ),
          Center(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(8, 10, 8, 8),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    mesa,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: _navy,
                      fontSize: 16,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  const SizedBox(height: 4),
                  const Icon(
                    Icons.table_restaurant_rounded,
                    size: 24,
                    color: _textSecondary,
                  ),
                  const SizedBox(height: 2),
                  const Text(
                    'Mesa',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: _textSecondary,
                      fontSize: 9,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 4),
                  const _TinyStatusPill(
                    text: 'LIVRE',
                    color: Color(0xFF7FD39B),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _BoardTile extends StatelessWidget {
  const _BoardTile({
    required this.store,
    required this.order,
    required this.width,
  });

  final BalcaoStore store;
  final Order order;
  final double width;

  @override
  Widget build(BuildContext context) {
    final selected = store.selectedOrderId == order.id;
    final color = switch (order.kind) {
      OrderKind.ifood => Colors.red,
      OrderKind.delivery => _warn,
      OrderKind.counter => _teal,
      OrderKind.table => _danger,
    };
    return InkWell(
      onTap: () => store.selectOrder(order.id),
      borderRadius: BorderRadius.circular(14),
      child: Container(
        width: width,
        height: 124,
        decoration: BoxDecoration(
          color: selected ? _mint : Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: selected ? _blue : _line,
            width: selected ? 2 : 1,
          ),
        ),
        clipBehavior: Clip.antiAlias,
        child: Stack(
          children: [
            Positioned(
              top: 0,
              left: 0,
              right: 0,
              child: Container(
                height: 7,
                decoration: BoxDecoration(
                  color: color,
                  borderRadius: const BorderRadius.vertical(
                    top: Radius.circular(14),
                  ),
                ),
              ),
            ),
            Center(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(8, 10, 8, 8),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    Text(
                      _shortBoardNumber(order.number),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: _navy,
                        fontWeight: FontWeight.w900,
                        fontSize: 16,
                      ),
                    ),
                    const SizedBox(height: 4),
                    const Icon(
                      Icons.table_restaurant_rounded,
                      size: 24,
                      color: _textSecondary,
                    ),
                    const SizedBox(height: 2),
                    Text(
                      kindLabel(order.kind),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: _textSecondary,
                        fontSize: 9,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      money(order.subtotal),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: _rail,
                        fontSize: 10,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    const SizedBox(height: 3),
                    _TinyStatusPill(
                      text: statusLabel(order.status).toUpperCase(),
                      color: color,
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _TinyStatusPill extends StatelessWidget {
  const _TinyStatusPill({required this.text, required this.color});

  final String text;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final foreground = color.computeLuminance() < .35
        ? Colors.white
        : const Color(0xFF17351F);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(7),
      ),
      child: Text(
        text,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: TextStyle(
          color: foreground,
          fontSize: 8,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

// ignore: unused_element
class _WindowsProductsPanel extends StatelessWidget {
  const _WindowsProductsPanel({
    required this.store,
    required this.onSelect,
    required this.onCatalog,
    required this.onReports,
  });

  final BalcaoStore store;
  final ValueChanged<Product> onSelect;
  final VoidCallback onCatalog;
  final VoidCallback onReports;

  @override
  Widget build(BuildContext context) {
    final products = store.filteredProducts();
    return _WindowPanel(
      title: 'Venda rapida',
      child: LayoutBuilder(
        builder: (context, constraints) {
          final compact = constraints.maxWidth < 620;
          return Column(
            children: [
              Row(
                children: [
                  Expanded(
                    child: TextField(
                      onChanged: store.setSearch,
                      decoration: InputDecoration(
                        hintText: 'Buscar por codigo ou produto...',
                        prefixIcon: compact
                            ? const Icon(Icons.search_rounded)
                            : null,
                        isDense: true,
                        filled: true,
                        fillColor: Colors.white,
                        contentPadding: const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 4,
                        ),
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(6),
                        ),
                        enabledBorder: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(6),
                          borderSide: const BorderSide(color: _line),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  SizedBox(
                    width: compact ? 78 : 112,
                    height: 40,
                    child: OutlinedButton(
                      onPressed: onCatalog,
                      style: OutlinedButton.styleFrom(
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(6),
                        ),
                      ),
                      child: Text(
                        compact ? 'F3' : 'F3 Catalogo',
                        textAlign: TextAlign.center,
                        style: const TextStyle(fontWeight: FontWeight.w900),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              if (!compact) ...[
                const Row(
                  children: [
                    SizedBox(width: 72, child: _GridHeader('Codigo')),
                    Expanded(child: _GridHeader('Produto')),
                    SizedBox(width: 74, child: _GridHeader('Grupo')),
                    SizedBox(
                      width: 76,
                      child: _GridHeader('Preco', right: true),
                    ),
                    SizedBox(
                      width: 58,
                      child: _GridHeader('Estoque', right: true),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
              ],
              SizedBox(
                height: compact ? 310 : 388,
                child: ListView(
                  children: products
                      .take(compact ? 12 : 24)
                      .map(
                        (product) => compact
                            ? _ProductCompactRow(
                                product: product,
                                selected:
                                    products.isNotEmpty &&
                                    product == products.first,
                                onTap: () => onSelect(product),
                              )
                            : _ProductRow(
                                product: product,
                                selected:
                                    products.isNotEmpty &&
                                    product == products.first,
                                onTap: () => onSelect(product),
                              ),
                      )
                      .toList(),
                ),
              ),
              if (!compact) ...[
                const SizedBox(height: 10),
                SizedBox(
                  height: 58,
                  child: Row(
                    children: [
                      Expanded(
                        child: Container(
                          height: double.infinity,
                          padding: const EdgeInsets.symmetric(horizontal: 12),
                          decoration: BoxDecoration(
                            color: _surfaceMuted,
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(color: _line),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              const Text(
                                'Selecionado',
                                style: TextStyle(
                                  color: _textSecondary,
                                  fontSize: 11,
                                  fontWeight: FontWeight.w900,
                                ),
                              ),
                              Text(
                                products.isEmpty
                                    ? 'Nenhum produto selecionado'
                                    : '${products.first.code} - ${products.first.name}',
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  color: _navy,
                                  fontSize: 12,
                                  fontWeight: FontWeight.w900,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ],
          );
        },
      ),
    );
  }
}

class _ProductCompactRow extends StatelessWidget {
  const _ProductCompactRow({
    required this.product,
    required this.selected,
    required this.onTap,
  });

  final Product product;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final low = product.stock <= product.minStock;
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(8),
      child: Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.fromLTRB(10, 9, 10, 9),
        decoration: BoxDecoration(
          color: _surfaceMuted,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(
            color: selected ? _navy2 : _line,
            width: selected ? 2 : 1,
          ),
        ),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    product.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: _navy,
                      fontWeight: FontWeight.w900,
                      fontSize: 15,
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    '${product.code}  |  ${product.category}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: _textSecondary,
                      fontWeight: FontWeight.w800,
                      fontSize: 11,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  money(product.price),
                  style: const TextStyle(
                    color: _navy2,
                    fontWeight: FontWeight.w900,
                    fontSize: 14,
                  ),
                ),
                Text(
                  'Est. ${product.stock}',
                  style: TextStyle(
                    color: low ? _danger : _textSecondary,
                    fontWeight: FontWeight.w900,
                    fontSize: 11,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ProductRow extends StatelessWidget {
  const _ProductRow({
    required this.product,
    required this.selected,
    required this.onTap,
  });

  final Product product;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final low = product.stock <= product.minStock;
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: Container(
        height: 58,
        margin: const EdgeInsets.only(bottom: 8),
        decoration: BoxDecoration(
          color: selected ? _mint : Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: selected ? _blue : _line,
            width: selected ? 2 : 1,
          ),
        ),
        clipBehavior: Clip.antiAlias,
        child: Row(
          children: [
            Container(width: 4, color: selected ? _blue : Colors.transparent),
            SizedBox(
              width: 64,
              child: Padding(
                padding: const EdgeInsets.only(left: 10),
                child: Text(
                  product.code,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _textSecondary,
                    fontWeight: FontWeight.w600,
                    fontSize: 13,
                  ),
                ),
              ),
            ),
            Expanded(
              child: Text(
                product.name,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: _navy,
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            SizedBox(
              width: 74,
              child: Text(
                product.category,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: _textSecondary,
                  fontWeight: FontWeight.w400,
                  fontSize: 11,
                ),
              ),
            ),
            SizedBox(
              width: 76,
              child: Text(
                money(product.price),
                textAlign: TextAlign.right,
                style: const TextStyle(
                  color: _teal,
                  fontSize: 13.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            SizedBox(
              width: 54,
              child: Text(
                'Est. ${product.stock}',
                textAlign: TextAlign.right,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: low ? _danger : _textSecondary,
                  fontSize: 11,
                  fontWeight: FontWeight.w400,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ProductSearchModule extends StatefulWidget {
  const _ProductSearchModule({required this.store, required this.onSelect});

  final BalcaoStore store;
  final Future<void> Function(Product product) onSelect;

  @override
  State<_ProductSearchModule> createState() => _ProductSearchModuleState();
}

class _ProductSearchModuleState extends State<_ProductSearchModule> {
  final search = TextEditingController();
  final keyFocus = FocusNode();
  int selectedIndex = 0;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) keyFocus.requestFocus();
    });
  }

  @override
  void dispose() {
    search.dispose();
    keyFocus.dispose();
    super.dispose();
  }

  List<Product> get filteredProducts {
    final query = search.text.trim().toLowerCase();
    final products = widget.store.products.where((product) {
      if (!product.active) return false;
      if (query.isEmpty) return true;
      return product.code.toLowerCase().contains(query) ||
          product.name.toLowerCase().contains(query) ||
          product.category.toLowerCase().contains(query);
    }).toList();
    products.sort((a, b) => a.category.compareTo(b.category));
    return products;
  }

  void _moveSelection(int delta, List<Product> products) {
    if (products.isEmpty) return;
    setState(() {
      selectedIndex = (selectedIndex + delta).clamp(0, products.length - 1);
    });
  }

  Future<void> _includeSelected(List<Product> products) async {
    if (products.isEmpty) return;
    final product = products[selectedIndex.clamp(0, products.length - 1)];
    await widget.onSelect(product);
    if (mounted) Navigator.of(context).maybePop();
  }

  @override
  Widget build(BuildContext context) {
    final products = filteredProducts;
    if (selectedIndex >= products.length && products.isNotEmpty) {
      selectedIndex = products.length - 1;
    }
    if (products.isEmpty) selectedIndex = 0;

    return KeyboardListener(
      focusNode: keyFocus,
      onKeyEvent: (event) {
        if (event is! KeyDownEvent) return;
        if (event.logicalKey == LogicalKeyboardKey.arrowDown) {
          _moveSelection(1, products);
        } else if (event.logicalKey == LogicalKeyboardKey.arrowUp) {
          _moveSelection(-1, products);
        } else if (event.logicalKey == LogicalKeyboardKey.enter) {
          _includeSelected(products);
        }
      },
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: LayoutBuilder(
          builder: (context, constraints) {
            final compact = constraints.maxWidth < 700;
            final tableWidth = math.max(constraints.maxWidth, 790.0);
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Digite codigo, nome ou grupo. Use setas e Enter para selecionar.',
                  style: TextStyle(
                    color: _textSecondary,
                    fontSize: 14,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: search,
                  autofocus: true,
                  onChanged: (_) => setState(() => selectedIndex = 0),
                  onSubmitted: (_) => _includeSelected(products),
                  decoration: InputDecoration(
                    hintText: 'Buscar no catalogo',
                    prefixIcon: const Icon(Icons.search_rounded),
                    suffixIcon: products.isEmpty
                        ? null
                        : Padding(
                            padding: const EdgeInsets.only(right: 8),
                            child: _StatusPill(
                              text: '${products.length} item(ns)',
                              color: _navy2,
                            ),
                          ),
                    suffixIconConstraints: const BoxConstraints(
                      minWidth: 92,
                      minHeight: 32,
                    ),
                    filled: true,
                    fillColor: Colors.white,
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(6),
                    ),
                    enabledBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(6),
                      borderSide: const BorderSide(color: _line),
                    ),
                    focusedBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(6),
                      borderSide: const BorderSide(color: _navy2, width: 1.4),
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                Expanded(
                  child: compact
                      ? DecoratedBox(
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(7),
                            border: Border.all(color: _line),
                          ),
                          child: products.isEmpty
                              ? const Center(
                                  child: _Empty(
                                    text: 'Nenhum produto encontrado.',
                                  ),
                                )
                              : ListView.separated(
                                  padding: const EdgeInsets.all(10),
                                  itemCount: products.length,
                                  separatorBuilder: (_, _) =>
                                      const SizedBox(height: 8),
                                  itemBuilder: (context, index) {
                                    final product = products[index];
                                    return _ProductSearchMobileRow(
                                      product: product,
                                      selected: index == selectedIndex,
                                      onTap: () async {
                                        setState(() => selectedIndex = index);
                                        await widget.onSelect(product);
                                        if (context.mounted) {
                                          Navigator.of(context).maybePop();
                                        }
                                      },
                                    );
                                  },
                                ),
                        )
                      : DecoratedBox(
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(7),
                            border: Border.all(color: _line),
                          ),
                          child: ClipRRect(
                            borderRadius: BorderRadius.circular(7),
                            child: SingleChildScrollView(
                              scrollDirection: Axis.horizontal,
                              child: SizedBox(
                                width: tableWidth,
                                child: Column(
                                  children: [
                                    const _ProductSearchHeader(),
                                    Expanded(
                                      child: products.isEmpty
                                          ? const Center(
                                              child: _Empty(
                                                text:
                                                    'Nenhum produto encontrado.',
                                              ),
                                            )
                                          : ListView.builder(
                                              itemCount: products.length,
                                              itemBuilder: (context, index) {
                                                final product = products[index];
                                                final selected =
                                                    index == selectedIndex;
                                                return _ProductSearchRow(
                                                  product: product,
                                                  selected: selected,
                                                  onTap: () => setState(
                                                    () => selectedIndex = index,
                                                  ),
                                                  onDoubleTap: () =>
                                                      _includeSelected(
                                                        products,
                                                      ),
                                                );
                                              },
                                            ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                        ),
                ),
                const SizedBox(height: 10),
                if (compact)
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          products.isEmpty
                              ? 'Nenhum item selecionado'
                              : 'Toque no produto para incluir na comanda.',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: _textSecondary,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                      ),
                      const SizedBox(width: 10),
                      SizedBox(
                        width: 112,
                        child: _DeskCommandButton(
                          label: 'Cancelar',
                          color: _danger,
                          onTap: () => Navigator.of(context).maybePop(),
                        ),
                      ),
                    ],
                  )
                else
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          products.isEmpty
                              ? 'Nenhum item selecionado'
                              : 'Selecionado: ${products[selectedIndex].code} - ${products[selectedIndex].name} - ${money(products[selectedIndex].price)}',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: _textSecondary,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                      ),
                      const SizedBox(width: 10),
                      SizedBox(
                        width: 132,
                        child: _DeskCommandButton(
                          label: 'Cancelar',
                          color: _danger,
                          onTap: () => Navigator.of(context).maybePop(),
                        ),
                      ),
                      const SizedBox(width: 8),
                      SizedBox(
                        width: 150,
                        child: _DeskCommandButton(
                          label: 'Incluir produto',
                          color: _teal,
                          onTap: () => _includeSelected(products),
                        ),
                      ),
                    ],
                  ),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _ProductSearchMobileRow extends StatelessWidget {
  const _ProductSearchMobileRow({
    required this.product,
    required this.selected,
    required this.onTap,
  });

  final Product product;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final low = product.stock <= product.minStock;
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(10),
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: selected ? _mint : _surfaceMuted,
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: selected ? _navy2 : _line, width: 1.2),
        ),
        child: Row(
          children: [
            Container(
              width: 34,
              height: 34,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: selected ? _navy2 : _surfaceMuted,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Icon(
                Icons.add_rounded,
                color: selected ? Colors.white : _navy2,
                size: 20,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    product.name,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: _navy,
                      fontWeight: FontWeight.w900,
                      fontSize: 15,
                      height: 1.05,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${product.code} | ${product.category}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: _textSecondary,
                      fontWeight: FontWeight.w700,
                      fontSize: 11,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  money(product.price),
                  style: const TextStyle(
                    color: _navy2,
                    fontWeight: FontWeight.w900,
                    fontSize: 14,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  'Est. ${product.stock}',
                  style: TextStyle(
                    color: low ? _danger : _textSecondary,
                    fontWeight: FontWeight.w800,
                    fontSize: 11,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ProductSearchHeader extends StatelessWidget {
  const _ProductSearchHeader();

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 42,
      padding: const EdgeInsets.symmetric(horizontal: 12),
      decoration: const BoxDecoration(
        color: _surfaceMuted,
        border: Border(bottom: BorderSide(color: _line)),
      ),
      child: const Row(
        children: [
          SizedBox(width: 96, child: _GridHeader('Codigo')),
          Expanded(child: _GridHeader('Produto')),
          SizedBox(width: 140, child: _GridHeader('Grupo')),
          SizedBox(width: 96, child: _GridHeader('Preco', right: true)),
          SizedBox(width: 84, child: _GridHeader('Estoque', right: true)),
        ],
      ),
    );
  }
}

class _ProductSearchRow extends StatelessWidget {
  const _ProductSearchRow({
    required this.product,
    required this.selected,
    required this.onTap,
    required this.onDoubleTap,
  });

  final Product product;
  final bool selected;
  final VoidCallback onTap;
  final VoidCallback onDoubleTap;

  @override
  Widget build(BuildContext context) {
    final low = product.stock <= product.minStock;
    return InkWell(
      onTap: onTap,
      onDoubleTap: onDoubleTap,
      child: Container(
        height: 52,
        padding: const EdgeInsets.symmetric(horizontal: 12),
        decoration: BoxDecoration(
          color: selected ? _mint : Colors.white,
          border: Border(
            bottom: const BorderSide(color: _line),
            left: BorderSide(
              color: selected ? _navy2 : Colors.transparent,
              width: 4,
            ),
          ),
        ),
        child: Row(
          children: [
            SizedBox(
              width: 92,
              child: Text(
                product.code,
                style: const TextStyle(
                  color: _textSecondary,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ),
            Expanded(
              child: Text(
                product.name,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: _navy,
                  fontSize: 16,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ),
            SizedBox(
              width: 140,
              child: Text(
                product.category,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: _textSecondary,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
            SizedBox(
              width: 96,
              child: Text(
                money(product.price),
                textAlign: TextAlign.right,
                style: const TextStyle(
                  color: _navy2,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ),
            SizedBox(
              width: 84,
              child: Text(
                'Est. ${product.stock}',
                textAlign: TextAlign.right,
                style: TextStyle(
                  color: low ? _danger : _textSecondary,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _TransferOrderModule extends StatefulWidget {
  const _TransferOrderModule({required this.store});

  final BalcaoStore store;

  @override
  State<_TransferOrderModule> createState() => _TransferOrderModuleState();
}

class _TransferOrderModuleState extends State<_TransferOrderModule> {
  String? sourceId;
  String? targetId;
  String? targetNumber;

  @override
  Widget build(BuildContext context) {
    final sources = widget.store.openOrders
        .where((order) => order.items.isNotEmpty)
        .toList();
    sourceId ??= sources.firstOrNull?.id;
    final source = sources.where((order) => order.id == sourceId).firstOrNull;
    final targets = widget.store.openOrders
        .where((order) => order.id != sourceId)
        .toList();
    final usedNumbers = widget.store.orders
        .map((order) => order.number)
        .toSet();
    final freeNumbers = List.generate(12, (index) {
      return 'M${(index + 1).toString().padLeft(5, '0')}';
    }).where((number) => !usedNumbers.contains(number)).toList();
    targetNumber ??= freeNumbers.firstOrNull;

    Widget sourcePanel = _WindowPanel(
      title: 'Origem',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Selecione a comanda completa que sera transferida.',
            style: TextStyle(
              color: _textSecondary,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 10),
          SizedBox(
            height: 230,
            child: ListView(
              children: sources.map((order) {
                return _TransferOrderCard(
                  order: order,
                  selected: order.id == sourceId,
                  subtitle: 'Com movimento',
                  onTap: () => setState(() {
                    sourceId = order.id;
                    if (targetId == sourceId) targetId = null;
                  }),
                );
              }).toList(),
            ),
          ),
          const Divider(height: 22),
          if (source == null)
            const _Empty(text: 'Nenhuma comanda com movimento.')
          else
            _TransferSummary(order: source),
        ],
      ),
    );

    Widget targetPanel = _WindowPanel(
      title: 'Destino',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Destino livre move a comanda. Destino ocupado junta os itens.',
            style: TextStyle(
              color: _textSecondary,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 10),
          _TransferDestinationPreview(
            freeNumber: targetNumber,
            selectedOrder: targets
                .where((order) => order.id == targetId)
                .firstOrNull,
          ),
          const SizedBox(height: 10),
          SizedBox(
            height: 250,
            child: ListView(
              children: [
                if (targetNumber != null)
                  _FreeDestinationCard(
                    number: targetNumber!,
                    selected: targetId == null,
                    onTap: () => setState(() => targetId = null),
                  ),
                ...targets.map((order) {
                  return _TransferOrderCard(
                    order: order,
                    selected: order.id == targetId,
                    subtitle: order.items.isEmpty
                        ? 'Livre para receber'
                        : 'Juntar com ${order.itemsCount} item(ns)',
                    onTap: () => setState(() => targetId = order.id),
                  );
                }),
              ],
            ),
          ),
          const SizedBox(height: 10),
          SizedBox(
            width: double.infinity,
            child: _DeskCommandButton(
              label: targetId == null ? 'Mover comanda' : 'Juntar comanda',
              color: _navy2,
              onTap: source == null
                  ? () {}
                  : () async {
                      await widget.store.transferOrder(
                        sourceId: source.id,
                        targetId: targetId,
                        targetNumber: targetId == null ? targetNumber : null,
                      );
                      if (context.mounted) Navigator.of(context).maybePop();
                    },
            ),
          ),
        ],
      ),
    );

    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 820) {
          return _ModuleScroll(children: [sourcePanel, targetPanel]);
        }
        return Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            children: [
              const _InfoStrip(
                icon: Icons.compare_arrows_rounded,
                title: 'Transferencia operacional',
                text:
                    'Mover troca a comanda para destino livre. Juntar soma os itens em um destino ocupado.',
              ),
              const SizedBox(height: 12),
              Expanded(
                child: Row(
                  children: [
                    Expanded(child: sourcePanel),
                    const SizedBox(width: 12),
                    Container(
                      width: 48,
                      height: 48,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(
                        color: Colors.white,
                        shape: BoxShape.circle,
                        border: Border.all(color: _line),
                      ),
                      child: const Icon(
                        Icons.arrow_forward_rounded,
                        color: _navy2,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(child: targetPanel),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _TransferOrderCard extends StatelessWidget {
  const _TransferOrderCard({
    required this.order,
    required this.selected,
    required this.subtitle,
    required this.onTap,
  });

  final Order order;
  final bool selected;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: selected ? _mint : Colors.white,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(
            color: selected ? _navy2 : _line,
            width: selected ? 2 : 1,
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 14,
              height: 14,
              decoration: const BoxDecoration(
                color: Color(0xFFFF918B),
                shape: BoxShape.circle,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    order.customerName.isEmpty
                        ? order.number
                        : order.customerName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: _navy,
                      fontSize: 17,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  Text(
                    '${kindLabel(order.kind).toUpperCase()} ${order.number}',
                    style: const TextStyle(color: _textSecondary, fontSize: 12),
                  ),
                  Text(
                    subtitle,
                    style: const TextStyle(
                      color: _navy2,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  money(order.subtotal),
                  style: const TextStyle(
                    color: _navy2,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 6),
                _StatusPill(
                  text: statusLabel(order.status).toUpperCase(),
                  color: _danger,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _FreeDestinationCard extends StatelessWidget {
  const _FreeDestinationCard({
    required this.number,
    required this.selected,
    required this.onTap,
  });

  final String number;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: selected ? _mint : Colors.white,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(
            color: selected ? _navy2 : _line,
            width: selected ? 2 : 1,
          ),
        ),
        child: Row(
          children: [
            const Icon(Icons.event_seat_rounded, color: _teal),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                'Mesa livre $number',
                style: const TextStyle(
                  color: _navy,
                  fontSize: 17,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ),
            const _StatusPill(text: 'LIVRE', color: _teal),
          ],
        ),
      ),
    );
  }
}

class _TransferDestinationPreview extends StatelessWidget {
  const _TransferDestinationPreview({
    required this.freeNumber,
    this.selectedOrder,
  });

  final String? freeNumber;
  final Order? selectedOrder;

  @override
  Widget build(BuildContext context) {
    final occupied = selectedOrder != null;
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: _mint,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: const Color(0xFFB7E8DF)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            occupied ? 'Juntar comanda' : 'Mover comanda',
            style: const TextStyle(
              color: _navy,
              fontSize: 20,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            occupied
                ? 'Destino ocupado: ${selectedOrder!.number} recebe os itens.'
                : 'Destino livre: ${freeNumber ?? 'mesa nova'} recebe a comanda.',
            style: const TextStyle(color: _navy2, fontWeight: FontWeight.w800),
          ),
        ],
      ),
    );
  }
}

class _TransferSummary extends StatelessWidget {
  const _TransferSummary({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'ORIGEM SELECIONADA',
          style: TextStyle(
            color: _navy2,
            fontSize: 12,
            fontWeight: FontWeight.w900,
          ),
        ),
        Text(
          order.customerName.isEmpty ? order.number : order.customerName,
          style: const TextStyle(
            color: _navy,
            fontSize: 24,
            fontWeight: FontWeight.w900,
          ),
        ),
        const SizedBox(height: 10),
        Row(
          children: [
            Expanded(
              child: _MiniSummary(
                label: 'Itens',
                value: '${order.itemsCount}',
                sub: 'produtos',
                color: _navy2,
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: _MiniSummary(
                label: 'Total',
                value: money(order.subtotal),
                sub: 'saldo',
                color: _teal,
              ),
            ),
          ],
        ),
        const SizedBox(height: 10),
        ...order.items
            .take(3)
            .map(
              (item) => _ReportLine(
                label: item.name,
                detail: '${item.quantity} x ${money(item.price)}',
                value: money(item.total),
                color: _teal,
              ),
            ),
      ],
    );
  }
}

class _CashOpenDialog extends StatefulWidget {
  const _CashOpenDialog({required this.store});

  final BalcaoStore store;

  @override
  State<_CashOpenDialog> createState() => _CashOpenDialogState();
}

class _CashOpenDialogState extends State<_CashOpenDialog> {
  final amount = TextEditingController(text: '0,00');
  final password = TextEditingController();
  bool submitting = false;

  @override
  void dispose() {
    amount.dispose();
    password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final pendingCount = widget.store.openOrders.length;
    final pendingTotal = widget.store.openTotal;
    final compact = MediaQuery.sizeOf(context).width < 700;

    return Dialog(
      backgroundColor: Colors.transparent,
      insetPadding: EdgeInsets.symmetric(
        horizontal: compact ? 12 : 28,
        vertical: compact ? 18 : 28,
      ),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 850, maxHeight: 720),
        child: Material(
          color: _surface,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
            side: const BorderSide(color: Color(0xFF272727)),
          ),
          clipBehavior: Clip.antiAlias,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                height: compact ? 70 : 76,
                padding: const EdgeInsets.symmetric(horizontal: 22),
                color: const Color(0xFF202020),
                child: Row(
                  children: [
                    Container(
                      width: 40,
                      height: 40,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(
                        color: const Color(0xFF292929),
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(color: _blue),
                      ),
                      child: const Icon(
                        Icons.point_of_sale_outlined,
                        color: _blue,
                        size: 21,
                      ),
                    ),
                    const SizedBox(width: 14),
                    const Expanded(
                      child: Text(
                        'Abrir caixa',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 20,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ),
                    IconButton(
                      onPressed: submitting
                          ? null
                          : () => Navigator.of(context).pop(false),
                      icon: const Icon(Icons.close_rounded),
                      color: Colors.white,
                      tooltip: 'Cancelar',
                    ),
                  ],
                ),
              ),
              Flexible(
                child: SingleChildScrollView(
                  padding: EdgeInsets.fromLTRB(
                    compact ? 16 : 22,
                    16,
                    compact ? 16 : 22,
                    18,
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            width: 48,
                            height: 48,
                            alignment: Alignment.center,
                            decoration: BoxDecoration(
                              color: const Color(0xFFFFEEE5),
                              borderRadius: BorderRadius.circular(10),
                            ),
                            child: const Icon(
                              Icons.point_of_sale_outlined,
                              color: _blue,
                            ),
                          ),
                          const SizedBox(width: 12),
                          const Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'Informe quanto dinheiro vivo existe no restaurante agora.',
                                  style: TextStyle(
                                    color: _navy,
                                    fontSize: 17,
                                    fontWeight: FontWeight.w900,
                                  ),
                                ),
                                SizedBox(height: 4),
                                Text(
                                  'A abertura será registrada no histórico do caixa.',
                                  style: TextStyle(
                                    color: _textSecondary,
                                    fontSize: 12,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: const Color(0xFFFFFDFB),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: _line),
                        ),
                        child: Row(
                          children: [
                            Container(
                              width: 50,
                              height: 50,
                              alignment: Alignment.center,
                              decoration: BoxDecoration(
                                color: const Color(0xFFFFEEE5),
                                borderRadius: BorderRadius.circular(10),
                                border: Border.all(
                                  color: const Color(0xFFFFC8AC),
                                ),
                              ),
                              child: const Icon(
                                Icons.person_outline_rounded,
                                color: _blue,
                              ),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    widget.store.operatorName.trim().isEmpty
                                        ? 'OPERADOR'
                                        : widget.store.operatorName
                                              .trim()
                                              .toUpperCase(),
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                    style: const TextStyle(
                                      color: _navy,
                                      fontSize: 14,
                                      fontWeight: FontWeight.w900,
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  const Row(
                                    children: [
                                      Text(
                                        'GERENTE',
                                        style: TextStyle(
                                          color: _textSecondary,
                                          fontSize: 10,
                                          fontWeight: FontWeight.w800,
                                        ),
                                      ),
                                      SizedBox(width: 12),
                                      Text(
                                        'Sessão ativa neste terminal',
                                        style: TextStyle(
                                          color: Color(0xFF178A46),
                                          fontSize: 10,
                                          fontWeight: FontWeight.w800,
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                            const Icon(
                              Icons.keyboard_arrow_down_rounded,
                              color: _textSecondary,
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 14),
                      const Text(
                        'Dinheiro vivo inicial',
                        style: TextStyle(
                          color: _navy,
                          fontSize: 13,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      const SizedBox(height: 6),
                      TextField(
                        key: const Key('cashOpenInitialAmount'),
                        controller: amount,
                        autofocus: true,
                        keyboardType: const TextInputType.numberWithOptions(
                          decimal: true,
                        ),
                        style: const TextStyle(
                          color: _navy,
                          fontSize: 34,
                          fontWeight: FontWeight.w800,
                        ),
                        decoration: InputDecoration(
                          prefixIcon: const Padding(
                            padding: EdgeInsets.symmetric(horizontal: 18),
                            child: Text(
                              'R\$',
                              style: TextStyle(
                                color: _textSecondary,
                                fontSize: 24,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                          ),
                          prefixIconConstraints: const BoxConstraints(
                            minWidth: 78,
                          ),
                          contentPadding: const EdgeInsets.symmetric(
                            horizontal: 14,
                            vertical: 18,
                          ),
                          filled: true,
                          fillColor: Colors.white,
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: _blue,
                              width: 1.5,
                            ),
                          ),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: const BorderSide(
                              color: _blue,
                              width: 2,
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(height: 8),
                      Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: [
                          for (final value in const [50.0, 100.0, 200.0])
                            SizedBox(
                              width: compact ? 92 : 150,
                              child: OutlinedButton(
                                onPressed: () => setState(
                                  () => amount.text = value
                                      .toStringAsFixed(2)
                                      .replaceAll('.', ','),
                                ),
                                child: Text(money(value)),
                              ),
                            ),
                          SizedBox(
                            width: compact ? 92 : 150,
                            child: OutlinedButton(
                              onPressed: () =>
                                  setState(() => amount.text = '0,00'),
                              child: const Text('Zerar'),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),
                      const Text(
                        'Senha (somente ao trocar operador)',
                        style: TextStyle(
                          color: _navy,
                          fontSize: 13,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      const SizedBox(height: 6),
                      TextField(
                        controller: password,
                        obscureText: true,
                        decoration: const InputDecoration(
                          suffixIcon: Icon(Icons.visibility_outlined),
                        ),
                      ),
                      const SizedBox(height: 7),
                      const Row(
                        children: [
                          Icon(
                            Icons.lock_outline_rounded,
                            size: 14,
                            color: _textSecondary,
                          ),
                          SizedBox(width: 7),
                          Expanded(
                            child: Text(
                              'A sessão atual abre sem nova senha. Ao trocar de operador, a senha é exigida e nunca é salva.',
                              style: TextStyle(
                                color: _textSecondary,
                                fontSize: 10,
                              ),
                            ),
                          ),
                        ],
                      ),
                      if (pendingCount > 0) ...[
                        const SizedBox(height: 12),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 14,
                            vertical: 11,
                          ),
                          decoration: BoxDecoration(
                            color: const Color(0xFFFFF8E8),
                            borderRadius: BorderRadius.circular(10),
                            border: Border.all(color: const Color(0xFFEFCB79)),
                          ),
                          child: Row(
                            children: [
                              const Icon(
                                Icons.description_outlined,
                                color: Color(0xFFB36B00),
                                size: 20,
                              ),
                              const SizedBox(width: 10),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      '$pendingCount pendência(s) operacional(is) • ${money(pendingTotal)} a resolver',
                                      style: const TextStyle(
                                        color: _navy,
                                        fontSize: 12,
                                        fontWeight: FontWeight.w900,
                                      ),
                                    ),
                                    const SizedBox(height: 2),
                                    const Text(
                                      'Não bloqueiam a abertura. Depois de abrir, revise as pendências.',
                                      style: TextStyle(
                                        color: _textSecondary,
                                        fontSize: 10,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.fromLTRB(18, 12, 18, 12),
                decoration: const BoxDecoration(
                  color: Color(0xFFFFFDFB),
                  border: Border(top: BorderSide(color: _line)),
                ),
                child: compact
                    ? Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          FilledButton.icon(
                            key: const Key('cashOpenConfirm'),
                            onPressed: submitting ? null : _submit,
                            icon: const Icon(Icons.lock_open_rounded, size: 17),
                            label: Text(
                              pendingCount > 0
                                  ? 'Abrir e revisar $pendingCount pendência(s)'
                                  : 'Abrir caixa',
                            ),
                          ),
                          const SizedBox(height: 7),
                          OutlinedButton(
                            onPressed: submitting
                                ? null
                                : () => Navigator.of(context).pop(false),
                            child: const Text('Cancelar'),
                          ),
                        ],
                      )
                    : Row(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [
                          OutlinedButton(
                            onPressed: submitting
                                ? null
                                : () => Navigator.of(context).pop(false),
                            child: const Text('Cancelar'),
                          ),
                          const SizedBox(width: 10),
                          FilledButton.icon(
                            key: const Key('cashOpenConfirm'),
                            onPressed: submitting ? null : _submit,
                            icon: const Icon(Icons.lock_open_rounded, size: 18),
                            label: Text(
                              pendingCount > 0
                                  ? 'Abrir caixa e revisar $pendingCount pendência(s)'
                                  : 'Abrir caixa',
                            ),
                          ),
                        ],
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _submit() async {
    setState(() => submitting = true);
    await widget.store.openCash(
      initialAmount: _parse(amount.text),
      operator: widget.store.operatorName,
    );
    if (!mounted) return;
    Navigator.of(context).pop(true);
  }
}

class _CashReconciliationDialog extends StatefulWidget {
  const _CashReconciliationDialog({required this.store});

  final BalcaoStore store;

  @override
  State<_CashReconciliationDialog> createState() =>
      _CashReconciliationDialogState();
}

class _CashReconciliationDialogState extends State<_CashReconciliationDialog> {
  late final TextEditingController operatorController;
  final passwordController = TextEditingController();
  final operatorFocus = FocusNode();
  final passwordFocus = FocusNode();
  String error = '';
  bool busy = false;

  @override
  void initState() {
    super.initState();
    operatorController = TextEditingController(
      text: widget.store.authEmail.trim().isNotEmpty
          ? widget.store.authEmail.trim()
          : widget.store.operatorName.trim(),
    );
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      operatorFocus.requestFocus();
      operatorController.selection = TextSelection(
        baseOffset: 0,
        extentOffset: operatorController.text.length,
      );
    });
  }

  @override
  void dispose() {
    operatorController.dispose();
    passwordController.dispose();
    operatorFocus.dispose();
    passwordFocus.dispose();
    super.dispose();
  }

  String get openedAtLabel {
    final value = widget.store.unreconciledCashOpenedAt;
    if (value == null) return 'Caixa anterior pendente';
    String two(int number) => number.toString().padLeft(2, '0');
    return '${two(value.day)}/${two(value.month)}/${value.year} · ${two(value.hour)}:${two(value.minute)}';
  }

  Future<void> submit() async {
    if (busy) return;
    final operator = operatorController.text.trim();
    final password = passwordController.text;
    if (operator.isEmpty || password.trim().isEmpty) {
      setState(() => error = 'Informe o operador e a senha do gerente.');
      if (operator.isEmpty) {
        operatorFocus.requestFocus();
      } else {
        passwordFocus.requestFocus();
      }
      return;
    }

    setState(() {
      busy = true;
      error = '';
    });
    final message = await widget.store.reconcilePreviousCash(
      operator,
      password,
    );
    if (!mounted) return;
    if (message == null) {
      Navigator.of(context).pop(true);
      return;
    }
    passwordController.clear();
    setState(() {
      busy = false;
      error = message;
    });
    passwordFocus.requestFocus();
  }

  @override
  Widget build(BuildContext context) {
    final screen = MediaQuery.sizeOf(context);
    final wide = screen.width >= 720;
    final width = math.min(800.0, screen.width - 24);
    final height = wide
        ? math.min(480.0, screen.height - 24)
        : math.min(720.0, screen.height - 24);

    return Dialog(
      key: const Key('cashReconciliationDialog'),
      insetPadding: const EdgeInsets.all(12),
      backgroundColor: Colors.transparent,
      child: SizedBox(
        width: width,
        height: height,
        child: ClipRRect(
          borderRadius: BorderRadius.circular(14),
          child: Material(
            color: const Color(0xFFF8F4EF),
            child: Column(
              children: [
                _buildHeader(),
                Expanded(
                  child: wide
                      ? Row(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Expanded(
                              flex: 9,
                              child: _buildSummary(compact: false),
                            ),
                            Expanded(
                              flex: 11,
                              child: _buildForm(compact: false),
                            ),
                          ],
                        )
                      : SingleChildScrollView(
                          child: Column(
                            children: [
                              _buildSummary(compact: true),
                              _buildForm(compact: true),
                            ],
                          ),
                        ),
                ),
                _buildFooter(compact: !wide),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildHeader() {
    return Container(
      height: 58,
      padding: const EdgeInsets.symmetric(horizontal: 18),
      color: const Color(0xFF202020),
      child: Row(
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: _blue),
            ),
            child: const Icon(
              Icons.point_of_sale_rounded,
              color: _blue,
              size: 20,
            ),
          ),
          const SizedBox(width: 12),
          const Expanded(
            child: Text(
              'Reconciliação do caixa anterior',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: Colors.white,
                fontSize: 18,
                fontWeight: FontWeight.w900,
              ),
            ),
          ),
          IconButton(
            key: const Key('cashReconciliationClose'),
            tooltip: 'Cancelar',
            onPressed: busy ? null : () => Navigator.of(context).pop(false),
            icon: const Icon(Icons.close_rounded, color: Colors.white),
          ),
        ],
      ),
    );
  }

  Widget _buildSummary({required bool compact}) {
    final copy = Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: compact
          ? CrossAxisAlignment.start
          : CrossAxisAlignment.center,
      children: [
        Text(
          openedAtLabel,
          textAlign: compact ? TextAlign.left : TextAlign.center,
          style: TextStyle(
            color: _navy,
            fontSize: compact ? 18 : 21,
            fontWeight: FontWeight.w900,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          'Este caixa precisa ser conferido e fechado antes de uma nova operação.',
          textAlign: compact ? TextAlign.left : TextAlign.center,
          style: const TextStyle(
            color: Color(0xFF625D57),
            height: 1.4,
            fontSize: 13.5,
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );

    final icon = Container(
      width: compact ? 54 : 72,
      height: compact ? 54 : 72,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: Colors.white,
        border: Border.all(color: const Color(0xFFF4B98F)),
      ),
      child: Icon(
        Icons.point_of_sale_rounded,
        color: _blue,
        size: compact ? 27 : 34,
      ),
    );

    return Container(
      width: double.infinity,
      padding: EdgeInsets.symmetric(
        horizontal: compact ? 20 : 28,
        vertical: compact ? 18 : 24,
      ),
      decoration: const BoxDecoration(
        color: Color(0xFFF3EEE8),
        border: Border(
          right: BorderSide(color: Color(0xFFD8CFC6)),
          bottom: BorderSide(color: Color(0xFFD8CFC6)),
        ),
      ),
      child: compact
          ? Row(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                icon,
                const SizedBox(width: 16),
                Expanded(child: copy),
              ],
            )
          : Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [icon, const SizedBox(height: 18), copy],
            ),
    );
  }

  Widget _buildForm({required bool compact}) {
    return Container(
      color: Colors.white,
      padding: EdgeInsets.fromLTRB(
        compact ? 20 : 34,
        compact ? 20 : 24,
        compact ? 20 : 34,
        compact ? 14 : 18,
      ),
      child: Column(
        mainAxisSize: compact ? MainAxisSize.min : MainAxisSize.max,
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: compact
            ? MainAxisAlignment.start
            : MainAxisAlignment.center,
        children: [
          const Text(
            'Autorização do gerente',
            style: TextStyle(
              color: _navy,
              fontSize: 18,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 4),
          const Text(
            'Informe a conta responsável para concluir.',
            style: TextStyle(
              color: Color(0xFF625D57),
              fontSize: 12.5,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 14),
          _fieldLabel('Operador'),
          const SizedBox(height: 5),
          TextField(
            key: const Key('cashReconciliationOperator'),
            controller: operatorController,
            focusNode: operatorFocus,
            enabled: !busy,
            textInputAction: TextInputAction.next,
            onSubmitted: (_) => passwordFocus.requestFocus(),
            decoration: _inputDecoration(),
          ),
          const SizedBox(height: 12),
          _fieldLabel('Senha'),
          const SizedBox(height: 5),
          TextField(
            key: const Key('cashReconciliationPassword'),
            controller: passwordController,
            focusNode: passwordFocus,
            enabled: !busy,
            obscureText: true,
            textInputAction: TextInputAction.done,
            onSubmitted: (_) => submit(),
            decoration: _inputDecoration(),
          ),
          AnimatedSize(
            duration: const Duration(milliseconds: 160),
            child: error.isEmpty
                ? const SizedBox(height: 18)
                : Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text(
                      error,
                      key: const Key('cashReconciliationError'),
                      style: const TextStyle(
                        color: _danger,
                        fontSize: 12.5,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
          ),
        ],
      ),
    );
  }

  Widget _fieldLabel(String text) {
    return Text(
      text,
      style: const TextStyle(
        color: Color(0xFF625D57),
        fontSize: 13,
        fontWeight: FontWeight.w800,
      ),
    );
  }

  InputDecoration _inputDecoration() {
    return InputDecoration(
      isDense: true,
      filled: true,
      fillColor: _surface,
      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(10),
        borderSide: const BorderSide(color: Color(0xFFD8CFC6)),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(10),
        borderSide: const BorderSide(color: _blue, width: 1.6),
      ),
      disabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(10),
        borderSide: const BorderSide(color: Color(0xFFE0DDD9)),
      ),
    );
  }

  Widget _buildFooter({required bool compact}) {
    final confirm = FilledButton(
      key: const Key('cashReconciliationConfirm'),
      onPressed: busy ? null : submit,
      style: FilledButton.styleFrom(
        minimumSize: Size(compact ? double.infinity : 190, 46),
        backgroundColor: _blue,
        foregroundColor: Colors.white,
        disabledBackgroundColor: const Color(0xFFF5A37F),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        textStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w900),
      ),
      child: busy
          ? const SizedBox(
              width: 20,
              height: 20,
              child: CircularProgressIndicator(
                strokeWidth: 2.2,
                color: Colors.white,
              ),
            )
          : const Text('Conferir e fechar'),
    );
    final cancel = OutlinedButton(
      key: const Key('cashReconciliationCancel'),
      onPressed: busy ? null : () => Navigator.of(context).pop(false),
      style: OutlinedButton.styleFrom(
        minimumSize: Size(compact ? double.infinity : 118, 46),
        foregroundColor: _navy,
        side: const BorderSide(color: _line),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        textStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w900),
      ),
      child: const Text('Cancelar'),
    );

    return Container(
      width: double.infinity,
      padding: EdgeInsets.symmetric(
        horizontal: compact ? 16 : 22,
        vertical: compact ? 12 : 14,
      ),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: Color(0xFFD8CFC6))),
      ),
      child: compact
          ? Column(
              mainAxisSize: MainAxisSize.min,
              children: [confirm, const SizedBox(height: 8), cancel],
            )
          : Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [confirm, const SizedBox(width: 12), cancel],
            ),
    );
  }
}

class _CashCloseBlockedModule extends StatelessWidget {
  const _CashCloseBlockedModule({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    final pending = store.openOrders;
    return Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Nao da para fechar o caixa enquanto existir mesa, ficha ou pedido com movimento pendente.',
            style: TextStyle(color: _navy, fontWeight: FontWeight.w900),
          ),
          const SizedBox(height: 10),
          Text(
            'Pendencias',
            style: TextStyle(
              color: Colors.blueGrey.shade700,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 6),
          Expanded(
            child: DecoratedBox(
              decoration: BoxDecoration(
                color: Colors.white,
                border: Border.all(color: _line),
              ),
              child: ListView.builder(
                itemCount: pending.length,
                itemBuilder: (context, index) {
                  final order = pending[index];
                  return Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 10,
                      vertical: 5,
                    ),
                    child: Text(
                      '${kindLabel(order.kind).toUpperCase()} ${order.number}  ${statusLabel(order.status).toUpperCase()}  ${order.customerName}  Saldo ${money(order.subtotal)}',
                      style: const TextStyle(color: _navy, fontSize: 15),
                    ),
                  );
                },
              ),
            ),
          ),
          const SizedBox(height: 12),
          Text(
            '${pending.length} pendencia(s) podem ser baixadas agora. Saldos abertos entram como BAIXA CAIXA, iFood entra como repasse/entrega.',
            style: const TextStyle(color: _navy2, fontWeight: FontWeight.w900),
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              Expanded(
                child: _DeskCommandButton(
                  label: 'Baixar automaticamente',
                  color: _teal,
                  onTap: () async {
                    await store.settleOpenOrdersAndCloseCash();
                    if (context.mounted) Navigator.of(context).maybePop();
                  },
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _DeskCommandButton(
                  label: 'Entendi',
                  color: _navy2,
                  onTap: () => Navigator.of(context).maybePop(),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _WaiterWebModule extends StatefulWidget {
  const _WaiterWebModule({required this.store});

  final BalcaoStore store;

  @override
  State<_WaiterWebModule> createState() => _WaiterWebModuleState();
}

class _WaiterWebModuleState extends State<_WaiterWebModule> {
  final phone = TextEditingController();

  @override
  void dispose() {
    phone.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final localIp = '192.168.1.69';
    final mobileLink = 'http://$localIp:5050/garcom';
    final localLink = 'http://localhost:5050/garcom';
    return Padding(
      padding: const EdgeInsets.all(16),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final wide = constraints.maxWidth >= 760;
          final qr = _WindowPanel(
            title: 'Acesso local',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Use a rede local. O celular/tablet precisa estar no mesmo Wi-Fi do computador do caixa.',
                  style: TextStyle(color: _navy, fontWeight: FontWeight.w800),
                ),
                const SizedBox(height: 12),
                Center(
                  child: Container(
                    width: 260,
                    height: 260,
                    padding: const EdgeInsets.all(18),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: _line),
                    ),
                    child: QrImageView(
                      data: mobileLink,
                      version: QrVersions.auto,
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                const _InfoStrip(
                  icon: Icons.qr_code_scanner_rounded,
                  title: 'Entrada direta',
                  text:
                      'Abra a camera do celular e leia o QR. Sem copiar link e sem digitar IP.',
                ),
              ],
            ),
          );
          final links = _WindowPanel(
            title: 'Controle do acesso',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _DeskInput(
                  label: 'Telefone do garcom (opcional)',
                  controller: phone,
                ),
                const SizedBox(height: 10),
                _AccessLinkBox(
                  label: 'Celular/tablet',
                  value: mobileLink,
                  status: 'rede',
                  color: _teal,
                ),
                const SizedBox(height: 8),
                _AccessLinkBox(
                  label: 'Neste computador',
                  value: localLink,
                  status: 'local',
                  color: _navy2,
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: _DeskCommandButton(
                        label: 'Enviar pelo WhatsApp',
                        color: _teal,
                        onTap: widget.store.flushSync,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: _DeskCommandButton(
                        label: 'Atualizar QR',
                        color: _navy2,
                        onTap: () => setState(() {}),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          );
          if (!wide) return _ModuleScroll(children: [qr, links]);
          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(child: qr),
              const SizedBox(width: 12),
              Expanded(child: links),
            ],
          );
        },
      ),
    );
  }
}

class _AccessLinkBox extends StatelessWidget {
  const _AccessLinkBox({
    required this.label,
    required this.value,
    required this.status,
    required this.color,
  });

  final String label;
  final String value;
  final String status;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: _surfaceMuted,
        borderRadius: BorderRadius.circular(7),
        border: Border.all(color: _line),
      ),
      child: Row(
        children: [
          Container(
            width: 34,
            height: 34,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: color.withValues(alpha: .12),
              borderRadius: BorderRadius.circular(7),
            ),
            child: Icon(Icons.link_rounded, color: color, size: 18),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    color: _navy,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _textSecondary,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ],
            ),
          ),
          _StatusPill(text: status.toUpperCase(), color: color),
        ],
      ),
    );
  }
}

class _DeliveryDesk extends StatefulWidget {
  const _DeliveryDesk({
    required this.store,
    this.filter = 'Todos',
    this.onFilterChanged,
  });

  final BalcaoStore store;
  final String filter;
  final ValueChanged<String>? onFilterChanged;

  @override
  State<_DeliveryDesk> createState() => _DeliveryDeskState();
}

class _DeliveryDeskState extends State<_DeliveryDesk> {
  late String _filter;
  String _query = '';

  @override
  void initState() {
    super.initState();
    _filter = widget.filter;
  }

  @override
  void didUpdateWidget(covariant _DeliveryDesk oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.filter != widget.filter && widget.filter != _filter) {
      _filter = widget.filter;
    }
  }

  void _setFilter(String value) {
    setState(() => _filter = value);
    widget.onFilterChanged?.call(value);
  }

  @override
  Widget build(BuildContext context) {
    final store = widget.store;
    final allOrders = store.orders
        .where(
          (order) =>
              (order.kind == OrderKind.delivery ||
                  order.kind == OrderKind.ifood) &&
              order.status != OrderStatus.canceled,
        )
        .toList();
    final normalizedQuery = _query.trim().toLowerCase();
    final orders = allOrders.where((order) {
      final matchesFilter = switch (_filter) {
        'Novos' => order.status == OrderStatus.open,
        'Em preparo' => order.status == OrderStatus.preparing,
        'Em rota' => order.status == OrderStatus.dispatched,
        'Entregues' => order.status == OrderStatus.delivered,
        _ => true,
      };
      if (!matchesFilter) return false;
      if (normalizedQuery.isEmpty) return true;
      return order.number.toLowerCase().contains(normalizedQuery) ||
          order.customerName.toLowerCase().contains(normalizedQuery) ||
          order.address.toLowerCase().contains(normalizedQuery);
    }).toList();
    final openOrders = orders.where(
      (order) => order.status == OrderStatus.open,
    );
    final prepOrders = orders.where(
      (order) => order.status == OrderStatus.preparing,
    );
    final routeOrders = orders.where(
      (order) => order.status == OrderStatus.dispatched,
    );
    final deliveredOrders = orders.where(
      (order) => order.status == OrderStatus.delivered,
    );
    final lateCount = allOrders
        .where(
          (order) =>
              order.isOpen &&
              DateTime.now().difference(order.createdAt).inMinutes >= 45,
        )
        .length;
    final averageMinutes = allOrders.isEmpty
        ? 0
        : (allOrders
                      .map(
                        (order) => DateTime.now()
                            .difference(order.createdAt)
                            .inMinutes
                            .clamp(1, 180)
                            .toInt(),
                      )
                      .fold<int>(0, (sum, value) => sum + value) /
                  allOrders.length)
              .round();

    final selected =
        orders
            .where((order) => order.id == store.selectedOrderId)
            .firstOrNull ??
        orders.firstOrNull;
    final metrics = [
      _DeliveryMetricCard(
        icon: Icons.description_outlined,
        label: 'Total pedidos',
        value: '${allOrders.length}',
        color: _rail,
      ),
      _DeliveryMetricCard(
        icon: Icons.add_rounded,
        label: 'Novos',
        value:
            '${allOrders.where((order) => order.status == OrderStatus.open).length}',
        color: _rail,
      ),
      _DeliveryMetricCard(
        icon: Icons.schedule_rounded,
        label: 'Em preparo',
        value:
            '${allOrders.where((order) => order.status == OrderStatus.preparing).length}',
        color: const Color(0xFF99620D),
      ),
      _DeliveryMetricCard(
        icon: Icons.check_rounded,
        label: 'Prontos',
        value: '0',
        color: _blue2,
      ),
      _DeliveryMetricCard(
        icon: Icons.delivery_dining_rounded,
        label: 'Saiu p/ entrega',
        value:
            '${allOrders.where((order) => order.status == OrderStatus.dispatched).length}',
        color: _rail,
      ),
      _DeliveryMetricCard(
        icon: Icons.done_rounded,
        label: 'Entregues',
        value:
            '${allOrders.where((order) => order.status == OrderStatus.delivered).length}',
        color: _teal,
      ),
      _DeliveryMetricCard(
        icon: Icons.warning_amber_rounded,
        label: 'Atrasados',
        value: '$lateCount',
        color: _danger,
      ),
      _DeliveryMetricCard(
        icon: Icons.timer_outlined,
        label: 'Tempo medio',
        value: averageMinutes == 0 ? '--' : '${averageMinutes}m',
        color: _rail,
      ),
    ];

    return LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth < 980;
        if (compact) {
          return ListView(
            padding: const EdgeInsets.all(10),
            children: [
              _DeliveryMetricsStrip(metrics: metrics),
              const SizedBox(height: 10),
              _DeliveryQueuePanel(
                store: store,
                openOrders: openOrders.toList(),
                prepOrders: prepOrders.toList(),
                routeOrders: routeOrders.toList(),
                deliveredOrders: deliveredOrders.toList(),
                averageMinutes: averageMinutes,
                filter: _filter,
                query: _query,
                selectedOrderId: selected?.id,
                onFilterChanged: _setFilter,
                onQueryChanged: (value) => setState(() => _query = value),
              ),
              if (selected != null) ...[
                const SizedBox(height: 10),
                _DeliverySelectedPanel(store: store, order: selected),
              ],
            ],
          );
        }

        return Column(
          children: [
            _DeliveryMetricsStrip(metrics: metrics),
            const SizedBox(height: 10),
            Expanded(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(
                    child: _DeliveryQueuePanel(
                      store: store,
                      openOrders: openOrders.toList(),
                      prepOrders: prepOrders.toList(),
                      routeOrders: routeOrders.toList(),
                      deliveredOrders: deliveredOrders.toList(),
                      averageMinutes: averageMinutes,
                      filter: _filter,
                      query: _query,
                      selectedOrderId: selected?.id,
                      onFilterChanged: _setFilter,
                      onQueryChanged: (value) => setState(() => _query = value),
                    ),
                  ),
                  const SizedBox(width: 12),
                  SizedBox(
                    width: math.min(460, constraints.maxWidth * .30),
                    child: selected == null
                        ? const _WindowPanel(
                            title: 'Pedido selecionado',
                            child: _Empty(text: 'Nenhum pedido delivery.'),
                          )
                        : _DeliverySelectedPanel(store: store, order: selected),
                  ),
                ],
              ),
            ),
          ],
        );
      },
    );
  }
}

class _DeliveryMetricsStrip extends StatelessWidget {
  const _DeliveryMetricsStrip({required this.metrics});

  final List<Widget> metrics;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth < 900;
        return Container(
          height: 84,
          padding: EdgeInsets.symmetric(horizontal: compact ? 6 : 18),
          decoration: BoxDecoration(
            color: Colors.white,
            border: Border.all(color: _line),
          ),
          child: compact
              ? SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: [
                      for (var i = 0; i < metrics.length; i++) ...[
                        SizedBox(width: 148, child: metrics[i]),
                        if (i < metrics.length - 1) const SizedBox(width: 4),
                      ],
                    ],
                  ),
                )
              : Row(
                  children: [
                    for (var i = 0; i < metrics.length; i++) ...[
                      Expanded(child: metrics[i]),
                      if (i < metrics.length - 1) const SizedBox(width: 18),
                    ],
                  ],
                ),
        );
      },
    );
  }
}

class _DeliverySelectedPanel extends StatelessWidget {
  const _DeliverySelectedPanel({required this.store, required this.order});

  final BalcaoStore store;
  final Order order;

  Future<void> _assignDriver(BuildContext context) async {
    final driver = await showDialog<String>(
      context: context,
      builder: (dialogContext) => SimpleDialog(
        title: const Text('Selecionar entregador'),
        children: [
          for (final name in const [
            'João Motoboy',
            'Carlos Entregas',
            'Equipe própria',
          ])
            SimpleDialogOption(
              onPressed: () => Navigator.pop(dialogContext, name),
              child: Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Text(name),
              ),
            ),
        ],
      ),
    );
    if (driver == null) return;
    await store.selectOrder(order.id);
    await store.updateSelectedOrderInfo(waiter: driver);
  }

  Future<void> _markDelivered() async {
    await store.selectOrder(order.id);
    await store.updateOrderStatus(order, OrderStatus.delivered);
  }

  Future<void> _openOrder(BuildContext context) async {
    await store.selectOrder(order.id);
    if (order.status == OrderStatus.open) {
      await store.updateOrderStatus(order, OrderStatus.preparing);
    }
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Pedido ${order.number} aberto para edição.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final items = order.items;
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: _line),
      ),
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Expanded(
                  child: Text(
                    'Pedido selecionado',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: _navy,
                      fontSize: 23,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
                Text(
                  order.number,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _navy2,
                    fontSize: 17,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 6,
              children: [
                _StatusPill(
                  text: order.kind == OrderKind.ifood
                      ? 'iFood'
                      : 'Balcao Livre',
                  color: order.kind == OrderKind.ifood ? Colors.red : _teal,
                ),
                _StatusPill(
                  text: statusLabel(order.status).toUpperCase(),
                  color: _blue,
                ),
              ],
            ),
            const SizedBox(height: 10),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(9),
              decoration: BoxDecoration(
                color: _surfaceMuted,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: _line),
              ),
              child: LayoutBuilder(
                builder: (context, constraints) {
                  final half = (constraints.maxWidth - 12) / 2;
                  return Wrap(
                    spacing: 12,
                    runSpacing: 6,
                    children: [
                      _DeliveryInfoCell(
                        width: half,
                        label: 'Cliente',
                        value: order.customerName.isEmpty
                            ? 'CLIENTE BALCAO'
                            : order.customerName,
                      ),
                      _DeliveryInfoCell(
                        width: half,
                        label: 'Telefone',
                        value: 'Sem telefone',
                      ),
                      _DeliveryInfoCell(
                        width: half,
                        label: 'Endereco',
                        value: order.address.isEmpty
                            ? 'Endereco nao informado'
                            : order.address,
                      ),
                      _DeliveryInfoCell(
                        width: half,
                        label: 'Bairro / referencia',
                        value: 'Sem bairro/referencia',
                      ),
                      _DeliveryInfoCell(
                        width: half,
                        label: 'Entregador',
                        value: order.waiter.isEmpty
                            ? 'Joao Motoboy'
                            : order.waiter,
                      ),
                      _DeliveryInfoCell(
                        width: half,
                        label: 'Previsao / tempo',
                        value:
                            'Aberto ${_shortTime(order.createdAt.add(const Duration(minutes: 45)))}',
                      ),
                      _DeliveryInfoCell(
                        width: half,
                        label: 'Pagamento',
                        value: order.paymentMethod.isEmpty
                            ? 'Pagamento nao informado'
                            : order.paymentMethod,
                      ),
                    ],
                  );
                },
              ),
            ),
            const SizedBox(height: 8),
            Container(
              height: 150,
              decoration: BoxDecoration(
                border: Border.all(color: _line),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Column(
                children: [
                  Container(
                    height: 38,
                    padding: const EdgeInsets.symmetric(horizontal: 10),
                    color: _surfaceMuted,
                    child: const Row(
                      children: [
                        SizedBox(width: 72, child: _GridHeader('Codigo')),
                        Expanded(child: _GridHeader('Produto')),
                        SizedBox(
                          width: 42,
                          child: _GridHeader('Qtd', right: true),
                        ),
                        SizedBox(
                          width: 70,
                          child: _GridHeader('Preco', right: true),
                        ),
                        SizedBox(
                          width: 76,
                          child: _GridHeader('Total', right: true),
                        ),
                      ],
                    ),
                  ),
                  Expanded(
                    child: items.isEmpty
                        ? const Center(child: _Empty(text: 'Sem itens.'))
                        : ListView(
                            children: items.map((item) {
                              return Container(
                                height: 48,
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 10,
                                ),
                                decoration: const BoxDecoration(
                                  border: Border(
                                    bottom: BorderSide(color: _line),
                                  ),
                                ),
                                child: Row(
                                  children: [
                                    SizedBox(
                                      width: 72,
                                      child: Text(
                                        item.code,
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        style: const TextStyle(
                                          color: _textSecondary,
                                          fontWeight: FontWeight.w800,
                                        ),
                                      ),
                                    ),
                                    Expanded(
                                      child: Text(
                                        item.name,
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        style: const TextStyle(
                                          color: _navy,
                                          fontWeight: FontWeight.w900,
                                        ),
                                      ),
                                    ),
                                    SizedBox(
                                      width: 42,
                                      child: Text(
                                        '${item.quantity}',
                                        textAlign: TextAlign.right,
                                        style: const TextStyle(
                                          color: _navy,
                                          fontWeight: FontWeight.w900,
                                        ),
                                      ),
                                    ),
                                    SizedBox(
                                      width: 70,
                                      child: Text(
                                        money(item.price),
                                        textAlign: TextAlign.right,
                                        style: const TextStyle(
                                          color: _textSecondary,
                                        ),
                                      ),
                                    ),
                                    SizedBox(
                                      width: 76,
                                      child: Text(
                                        money(item.total),
                                        textAlign: TextAlign.right,
                                        style: const TextStyle(
                                          color: _blue,
                                          fontWeight: FontWeight.w900,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              );
                            }).toList(),
                          ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 8),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: const Color(0xFFFFF8F8),
                borderRadius: BorderRadius.circular(9),
                border: Border.all(color: const Color(0xFFF3C9C9)),
              ),
              child: const Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Observações',
                    style: TextStyle(
                      color: _textSecondary,
                      fontSize: 11,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  SizedBox(height: 4),
                  Text(
                    'Sem observações.',
                    style: TextStyle(color: _navy, fontSize: 12),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Taxa de entrega',
                        style: TextStyle(
                          color: _textSecondary,
                          fontSize: 11,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      Text(
                        money(order.coverCharge),
                        style: const TextStyle(
                          color: _navy,
                          fontSize: 16,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ],
                  ),
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    const Text(
                      'Total do pedido',
                      style: TextStyle(
                        color: _textSecondary,
                        fontSize: 11,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    Text(
                      money(order.subtotal),
                      style: const TextStyle(
                        color: _navy,
                        fontSize: 28,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 8),
            SizedBox(
              height: 40,
              child: Row(
                children: [
                  Expanded(
                    flex: 11,
                    child: OutlinedButton(
                      onPressed: () => _assignDriver(context),
                      child: const Text('Entregador'),
                    ),
                  ),
                  const SizedBox(width: 6),
                  Expanded(
                    flex: 10,
                    child: OutlinedButton(
                      onPressed: order.status == OrderStatus.delivered
                          ? null
                          : _markDelivered,
                      child: const Text('Entregue'),
                    ),
                  ),
                  const SizedBox(width: 6),
                  Expanded(
                    flex: 13,
                    child: FilledButton(
                      onPressed: () => _openOrder(context),
                      child: const Text('Abrir pedido'),
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

class _DeliveryInfoCell extends StatelessWidget {
  const _DeliveryInfoCell({
    required this.width,
    required this.label,
    required this.value,
  });

  final double width;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: width,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: _textSecondary,
              fontSize: 10,
              fontWeight: FontWeight.w900,
            ),
          ),
          Text(
            value,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: _navy,
              fontSize: 12,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _DeliverySectionPanel extends StatelessWidget {
  const _DeliverySectionPanel({
    required this.title,
    required this.icon,
    required this.child,
  });

  final String title;
  final IconData icon;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final bounded = constraints.hasBoundedHeight;
        final content = Padding(
          padding: const EdgeInsets.all(12),
          child: child,
        );
        return Container(
          height: bounded ? double.infinity : null,
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: _line),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                height: 66,
                padding: const EdgeInsets.symmetric(horizontal: 14),
                decoration: const BoxDecoration(
                  color: _surface,
                  border: Border(bottom: BorderSide(color: _line)),
                  borderRadius: BorderRadius.vertical(top: Radius.circular(14)),
                ),
                child: Row(
                  children: [
                    Container(
                      width: 42,
                      height: 42,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(
                        color: _mint,
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(color: _line),
                      ),
                      child: Icon(icon, color: _navy2, size: 23),
                    ),
                    const SizedBox(width: 14),
                    Expanded(
                      child: Text(
                        title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: _navy,
                          fontSize: 20,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              if (bounded) Expanded(child: content) else content,
            ],
          ),
        );
      },
    );
  }
}

class _DeliveryQueuePanel extends StatelessWidget {
  const _DeliveryQueuePanel({
    required this.store,
    required this.openOrders,
    required this.prepOrders,
    required this.routeOrders,
    required this.deliveredOrders,
    required this.averageMinutes,
    required this.filter,
    required this.query,
    required this.selectedOrderId,
    required this.onFilterChanged,
    required this.onQueryChanged,
  });

  final BalcaoStore store;
  final List<Order> openOrders;
  final List<Order> prepOrders;
  final List<Order> routeOrders;
  final List<Order> deliveredOrders;
  final int averageMinutes;
  final String filter;
  final String query;
  final String? selectedOrderId;
  final ValueChanged<String> onFilterChanged;
  final ValueChanged<String> onQueryChanged;

  @override
  Widget build(BuildContext context) {
    Widget buildBoard({required bool bounded, required double width}) {
      final compact = width < 700;
      final stageRow = Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Expanded(
            child: _DeliveryStageColumn(
              title: 'Novos',
              orders: openOrders,
              color: _rail,
              store: store,
              selectedOrderId: selectedOrderId,
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: _DeliveryStageColumn(
              title: 'Em preparo',
              orders: prepOrders,
              color: const Color(0xFF99620D),
              store: store,
              selectedOrderId: selectedOrderId,
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: _DeliveryStageColumn(
              title: 'Saiu p/ entrega',
              orders: routeOrders,
              color: _blue2,
              store: store,
              selectedOrderId: selectedOrderId,
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: _DeliveryStageColumn(
              title: 'Entregues recentes',
              orders: deliveredOrders,
              color: _teal,
              store: store,
              selectedOrderId: selectedOrderId,
            ),
          ),
        ],
      );
      final columns = compact
          ? SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: SizedBox(width: 880, child: stageRow),
            )
          : stageRow;

      return Column(
        children: [
          if (compact)
            Row(
              children: [
                Expanded(
                  child: _DeliverySearchField(
                    value: query,
                    onChanged: onQueryChanged,
                  ),
                ),
                const SizedBox(width: 8),
                _SmallSquareButton(
                  icon: Icons.close_rounded,
                  onTap: () => onQueryChanged(''),
                ),
                const SizedBox(width: 6),
                _SmallSquareButton(
                  icon: Icons.search_rounded,
                  onTap: store.flushSync,
                ),
              ],
            )
          else
            Row(
              children: [
                SizedBox(
                  width: 174,
                  height: 39,
                  child: DropdownButtonFormField<String>(
                    initialValue: filter,
                    isExpanded: true,
                    decoration: const InputDecoration(
                      isDense: true,
                      filled: true,
                      fillColor: Color(0xFFF5F5F5),
                      contentPadding: EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 10,
                      ),
                      border: OutlineInputBorder(),
                    ),
                    style: const TextStyle(
                      color: _navy,
                      fontSize: 14,
                      fontWeight: FontWeight.w900,
                    ),
                    items: const [
                      DropdownMenuItem(value: 'Todos', child: Text('Todos')),
                      DropdownMenuItem(value: 'Novos', child: Text('Novos')),
                      DropdownMenuItem(
                        value: 'Em preparo',
                        child: Text('Em preparo'),
                      ),
                      DropdownMenuItem(
                        value: 'Em rota',
                        child: Text('Saiu p/ entrega'),
                      ),
                      DropdownMenuItem(
                        value: 'Entregues',
                        child: Text('Entregues'),
                      ),
                    ],
                    onChanged: (value) {
                      if (value != null) onFilterChanged(value);
                    },
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: _DeliverySearchField(
                    value: query,
                    onChanged: onQueryChanged,
                  ),
                ),
                const SizedBox(width: 8),
                _SmallSquareButton(
                  icon: Icons.close_rounded,
                  onTap: () => onQueryChanged(''),
                ),
                const SizedBox(width: 6),
                _SmallSquareButton(
                  icon: Icons.search_rounded,
                  onTap: store.flushSync,
                ),
              ],
            ),
          const SizedBox(height: 8),
          Align(
            alignment: Alignment.centerLeft,
            child: Text(
              'Tempo medio ${averageMinutes}min | ${openOrders.length + prepOrders.length + routeOrders.length} pedidos em aberto',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: _textSecondary,
                fontSize: 12,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
          const SizedBox(height: 10),
          if (bounded)
            Expanded(child: columns)
          else
            SizedBox(height: 430, child: columns),
        ],
      );
    }

    return _DeliverySectionPanel(
      title: 'Fila de pedidos delivery',
      icon: Icons.table_bar_outlined,
      child: LayoutBuilder(
        builder: (context, constraints) => buildBoard(
          bounded: constraints.hasBoundedHeight,
          width: constraints.maxWidth,
        ),
      ),
    );
  }
}

class _DeliverySearchField extends StatefulWidget {
  const _DeliverySearchField({required this.value, required this.onChanged});

  final String value;
  final ValueChanged<String> onChanged;

  @override
  State<_DeliverySearchField> createState() => _DeliverySearchFieldState();
}

class _DeliverySearchFieldState extends State<_DeliverySearchField> {
  late final TextEditingController _controller;

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController(text: widget.value);
  }

  @override
  void didUpdateWidget(covariant _DeliverySearchField oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.value != _controller.text) {
      _controller.value = TextEditingValue(
        text: widget.value,
        selection: TextSelection.collapsed(offset: widget.value.length),
      );
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 39,
      child: TextField(
        controller: _controller,
        onChanged: widget.onChanged,
        decoration: InputDecoration(
          hintText: 'Buscar pedido, cliente ou endereço',
          isDense: true,
          filled: true,
          fillColor: Colors.white,
          prefixIcon: const Icon(Icons.search_rounded, size: 20),
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 10,
            vertical: 10,
          ),
          border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
            borderSide: const BorderSide(color: _line),
          ),
        ),
      ),
    );
  }
}

class _SmallSquareButton extends StatelessWidget {
  const _SmallSquareButton({required this.icon, required this.onTap});

  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 48,
      height: 40,
      child: OutlinedButton(
        onPressed: onTap,
        style: OutlinedButton.styleFrom(
          padding: EdgeInsets.zero,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          side: const BorderSide(color: _line),
        ),
        child: Icon(icon, color: _navy2, size: 21),
      ),
    );
  }
}

// Kept for the compact delivery-item picker used by follow-up flows.
// ignore: unused_element
class _DeliveryCatalogPanel extends StatelessWidget {
  const _DeliveryCatalogPanel({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    final products = store.filteredProducts().take(7).toList();
    Widget buildProducts({required bool bounded}) {
      final list = ListView(
        children: products.map((product) {
          final selected = products.isNotEmpty && product == products.first;
          return InkWell(
            onTap: () => store.addProduct(product),
            child: Container(
              height: 70,
              margin: const EdgeInsets.only(bottom: 8),
              padding: const EdgeInsets.symmetric(horizontal: 10),
              decoration: BoxDecoration(
                color: selected ? _mint : Colors.white,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                  color: selected ? _blue : _line,
                  width: selected ? 2 : 1,
                ),
              ),
              child: Row(
                children: [
                  SizedBox(
                    width: 72,
                    child: Text(
                      product.code,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: _textSecondary,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  Expanded(
                    child: Text(
                      product.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: _navy,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  SizedBox(
                    width: 74,
                    child: Text(
                      money(product.price),
                      textAlign: TextAlign.right,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: _blue,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  SizedBox(
                    width: 62,
                    child: Text(
                      'Est. ${product.stock.toStringAsFixed(0)}',
                      textAlign: TextAlign.right,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: _textSecondary,
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          );
        }).toList(),
      );

      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            height: 44,
            child: TextField(
              onChanged: store.setSearch,
              decoration: InputDecoration(
                isDense: true,
                filled: true,
                fillColor: Colors.white,
                prefixIcon: const Icon(Icons.search_rounded, size: 20),
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 10,
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: const BorderSide(color: _line),
                ),
              ),
            ),
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: _DeliveryCatalogAction(
                  icon: Icons.add_rounded,
                  label: 'Item avulso',
                  onTap: () => store.openOrder(
                    OrderKind.delivery,
                    customer: 'Cliente balcao',
                    address: 'Endereco nao informado',
                  ),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _DeliveryCatalogAction(
                  icon: Icons.edit_outlined,
                  label: 'Observacao',
                  onTap: store.flushSync,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          const Text(
            'Codigo',
            style: TextStyle(
              color: _textSecondary,
              fontSize: 13,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 5),
          Container(
            height: 58,
            width: double.infinity,
            alignment: Alignment.centerLeft,
            padding: const EdgeInsets.symmetric(horizontal: 14),
            decoration: BoxDecoration(
              color: _mint,
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: _navy2, width: 2),
            ),
            child: Text(
              products.isEmpty ? '' : products.first.code,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: _textSecondary,
                fontSize: 16,
                fontWeight: FontWeight.w900,
              ),
            ),
          ),
          const SizedBox(height: 12),
          const Row(
            children: [
              SizedBox(width: 72, child: _GridHeader('Codigo')),
              Expanded(child: _GridHeader('Produto')),
              SizedBox(width: 74, child: _GridHeader('Preco', right: true)),
              SizedBox(width: 62, child: _GridHeader('Estoque', right: true)),
            ],
          ),
          const SizedBox(height: 6),
          if (bounded)
            Expanded(child: list)
          else
            SizedBox(height: 430, child: list),
        ],
      );
    }

    return _DeliverySectionPanel(
      title: '3  Catalogo delivery',
      icon: Icons.local_offer_outlined,
      child: LayoutBuilder(
        builder: (context, constraints) =>
            buildProducts(bounded: constraints.hasBoundedHeight),
      ),
    );
  }
}

class _DeliveryCatalogAction extends StatelessWidget {
  const _DeliveryCatalogAction({
    required this.icon,
    required this.label,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 42,
      child: OutlinedButton.icon(
        onPressed: onTap,
        icon: Icon(icon, size: 19),
        label: Text(label, maxLines: 1, overflow: TextOverflow.ellipsis),
        style: OutlinedButton.styleFrom(
          foregroundColor: _navy,
          backgroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(horizontal: 10),
          side: const BorderSide(color: _line),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(
            fontSize: 12.5,
            fontWeight: FontWeight.w900,
          ),
        ),
      ),
    );
  }
}

class _DeliveryMetricCard extends StatelessWidget {
  const _DeliveryMetricCard({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
  });

  final IconData icon;
  final String label;
  final String value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 68,
      padding: const EdgeInsets.symmetric(horizontal: 8),
      child: Row(
        children: [
          Icon(icon, color: color, size: 25),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _textSecondary,
                    fontSize: 12,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: color,
                    fontSize: 24,
                    height: 1.05,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _DeliveryStageColumn extends StatelessWidget {
  const _DeliveryStageColumn({
    required this.title,
    required this.orders,
    required this.color,
    required this.store,
    required this.selectedOrderId,
  });

  final String title;
  final List<Order> orders;
  final Color color;
  final BalcaoStore store;
  final String? selectedOrderId;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: _surfaceMuted,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: _line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 8,
                height: 8,
                decoration: BoxDecoration(color: color, shape: BoxShape.circle),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _navy,
                    fontSize: 13,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ),
              _TinyStatusPill(text: '${orders.length}', color: color),
            ],
          ),
          const SizedBox(height: 10),
          Expanded(
            child: orders.isEmpty
                ? const Center(child: _Empty(text: 'Sem pedidos.'))
                : ListView(
                    children: orders
                        .map(
                          (order) => _DeliveryStageCard(
                            order: order,
                            color: color,
                            store: store,
                            selectedOrderId: selectedOrderId,
                          ),
                        )
                        .toList(),
                  ),
          ),
        ],
      ),
    );
  }
}

class _DeliveryStageCard extends StatelessWidget {
  const _DeliveryStageCard({
    required this.order,
    required this.color,
    required this.store,
    required this.selectedOrderId,
  });

  final Order order;
  final Color color;
  final BalcaoStore store;
  final String? selectedOrderId;

  @override
  Widget build(BuildContext context) {
    final minutes = DateTime.now().difference(order.createdAt).inMinutes;
    final selected = selectedOrderId == order.id;
    final statusText = switch (order.status) {
      OrderStatus.open => 'NOVO',
      OrderStatus.preparing => 'EM PREPARO',
      OrderStatus.dispatched => 'EM ROTA',
      OrderStatus.delivered => 'ENTREGUE',
      _ => statusLabel(order.status).toUpperCase(),
    };
    final footerColor = switch (order.status) {
      OrderStatus.open => _rail,
      OrderStatus.preparing => const Color(0xFF99620D),
      OrderStatus.dispatched => _blue2,
      OrderStatus.delivered => _teal,
      _ => color,
    };
    return Container(
      height: 170,
      margin: const EdgeInsets.only(bottom: 10),
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        color: selected ? const Color(0xFFFFF0E5) : Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
          color: selected ? _rail : _line,
          width: selected ? 2 : 1,
        ),
      ),
      child: InkWell(
        onTap: () => store.selectOrder(order.id),
        child: Column(
          children: [
            Expanded(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Container(width: 8, color: footerColor),
                  Expanded(
                    child: Padding(
                      padding: const EdgeInsets.fromLTRB(12, 10, 12, 8),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              const Icon(
                                Icons.receipt_long_outlined,
                                color: _navy,
                                size: 17,
                              ),
                              const SizedBox(width: 5),
                              Expanded(
                                child: Text(
                                  order.number,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: const TextStyle(
                                    color: _navy,
                                    fontSize: 13,
                                    fontWeight: FontWeight.w900,
                                  ),
                                ),
                              ),
                              _TinyStatusPill(
                                text: order.kind == OrderKind.ifood
                                    ? 'IFOOD'
                                    : 'LOJA',
                                color: order.kind == OrderKind.ifood
                                    ? Colors.red
                                    : _rail,
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          Text(
                            order.customerName.isEmpty
                                ? 'CLIENTE DELIVERY'
                                : order.customerName.toUpperCase(),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: _navy,
                              fontSize: 12,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            order.address.isEmpty
                                ? 'Endereço pendente'
                                : order.address,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: _textSecondary,
                              fontSize: 10.5,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const Spacer(),
                          Text(
                            minutes <= 0
                                ? 'agora'
                                : '${minutes.toString().padLeft(2, '0')}:00',
                            style: TextStyle(
                              color: minutes >= 45 ? _danger : footerColor,
                              fontSize: 10,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
            Container(
              height: 36,
              color: footerColor,
              padding: const EdgeInsets.symmetric(horizontal: 14),
              child: Row(
                children: [
                  Expanded(
                    child: Text(
                      statusText,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 10,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  Text(
                    money(order.subtotal),
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 11,
                      fontWeight: FontWeight.w900,
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

String _kitchenStationFor(Product? product, OrderItem item) {
  final text = '${product?.category ?? ''} ${item.name}'.toUpperCase();
  if (text.contains('PIZZA') || text.contains('FORNO')) return 'Forno';
  if (text.contains('FRITA') ||
      text.contains('BATATA') ||
      text.contains('PASTEL')) {
    return 'Fritadeira';
  }
  return 'Montagem';
}

String _kitchenDisplayNumber(String value) {
  final digits = value.replaceAll(RegExp(r'[^0-9]'), '');
  if (digits.isEmpty) return value;
  final parsed = int.tryParse(digits) ?? 0;
  return parsed.toString().padLeft(2, '0');
}

class _KitchenDesk extends StatefulWidget {
  const _KitchenDesk({required this.store, this.filter = 'Todas'});

  final BalcaoStore store;
  final String filter;

  @override
  State<_KitchenDesk> createState() => _KitchenDeskState();
}

class _KitchenDeskState extends State<_KitchenDesk> {
  final Set<String> _completed = <String>{};
  String? _selectedOrderId;
  String _monitor = 'Monitor 1 - Principal';
  bool _voiceEnabled = true;

  List<_KitchenEntry> _entries() {
    final entries = <_KitchenEntry>[];
    for (final order in widget.store.openOrders) {
      for (var index = 0; index < order.items.length; index++) {
        final item = order.items[index];
        final product = widget.store.products
            .where((candidate) => candidate.id == item.productId)
            .firstOrNull;
        final station = _kitchenStationFor(product, item);
        entries.add(
          _KitchenEntry(
            keyValue: '${order.id}:$index',
            order: order,
            item: item,
            station: station,
          ),
        );
      }
    }
    return entries;
  }

  void _toggle(_KitchenEntry entry) {
    setState(() {
      if (!_completed.add(entry.keyValue)) {
        _completed.remove(entry.keyValue);
      }
      _selectedOrderId = entry.order.id;
    });
    final orderEntries = _entries().where(
      (candidate) => candidate.order.id == entry.order.id,
    );
    if (orderEntries.isNotEmpty &&
        orderEntries.every(
          (candidate) => _completed.contains(candidate.keyValue),
        )) {
      unawaited(
        widget.store.updateOrderStatus(entry.order, OrderStatus.dispatched),
      );
    } else if (entry.order.status == OrderStatus.open) {
      unawaited(
        widget.store.updateOrderStatus(entry.order, OrderStatus.preparing),
      );
    }
  }

  Future<void> _showStationConfiguration() async {
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Configurar praças'),
        content: const SizedBox(
          width: 430,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              _KitchenConfigRow(
                icon: Icons.microwave_outlined,
                title: 'Forno',
                description: 'Pizzas e produtos assados',
              ),
              SizedBox(height: 8),
              _KitchenConfigRow(
                icon: Icons.restaurant_outlined,
                title: 'Fritadeira',
                description: 'Frituras, batatas e pastéis',
              ),
              SizedBox(height: 8),
              _KitchenConfigRow(
                icon: Icons.layers_outlined,
                title: 'Montagem',
                description: 'Finalização, bebidas e embalagem',
              ),
            ],
          ),
        ),
        actions: [
          FilledButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('Concluir'),
          ),
        ],
      ),
    );
  }

  void _openKitchenDisplay() {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text('Painel aberto em $_monitor.')));
  }

  void _completeOrder(Order? order) {
    if (order == null) return;
    final orderEntries = _entries()
        .where((entry) => entry.order.id == order.id)
        .toList();
    setState(() {
      _completed.addAll(orderEntries.map((entry) => entry.keyValue));
      _selectedOrderId = order.id;
    });
    unawaited(widget.store.updateOrderStatus(order, OrderStatus.dispatched));
  }

  @override
  Widget build(BuildContext context) {
    final entries = _entries();
    final orders = widget.store.openOrders.where(
      (order) => order.items.isNotEmpty,
    );
    final selectedOrder =
        orders
            .where(
              (order) =>
                  order.id ==
                  (_selectedOrderId ?? widget.store.selectedOrderId),
            )
            .firstOrNull ??
        orders.firstOrNull;
    final stations = widget.filter == 'Todas'
        ? ['Forno', 'Fritadeira', 'Montagem']
        : [widget.filter];

    return DecoratedBox(
      decoration: BoxDecoration(
        color: const Color(0xFFF4EFE9),
        border: Border.all(color: const Color(0xFFE0D7CE)),
      ),
      child: Column(
        children: [
          Container(
            constraints: const BoxConstraints(minHeight: 80),
            padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 10),
            color: const Color(0xFFFFFCF9),
            child: LayoutBuilder(
              builder: (context, constraints) {
                final compact = constraints.maxWidth < 760;
                return Row(
                  children: [
                    Container(
                      width: 40,
                      height: 40,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(
                        color: const Color(0xFFFFEDE4),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: const Icon(Icons.kitchen_outlined, color: _blue),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        'Cozinha por praça',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: const Color(0xFF222222),
                          fontSize: compact ? 18 : 22,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ),
                    if (compact)
                      PopupMenuButton<String>(
                        tooltip: 'Acoes da cozinha',
                        icon: const Icon(Icons.more_vert_rounded),
                        onSelected: (_) => widget.store.flushSync(),
                        itemBuilder: (context) => const [
                          PopupMenuItem(
                            value: 'stations',
                            child: Text('Configurar pracas'),
                          ),
                          PopupMenuItem(
                            value: 'display',
                            child: Text('Abrir painel da cozinha'),
                          ),
                        ],
                      )
                    else ...[
                      _KitchenHeaderButton(
                        label: 'Configurar praças',
                        onTap: _showStationConfiguration,
                      ),
                      const SizedBox(width: 10),
                      _KitchenHeaderButton(
                        label: 'Abrir painel da cozinha',
                        filled: true,
                        onTap: _openKitchenDisplay,
                      ),
                      const SizedBox(width: 10),
                      Container(
                        width: 300,
                        height: 50,
                        padding: const EdgeInsets.symmetric(horizontal: 14),
                        decoration: BoxDecoration(
                          color: _surface,
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(color: _line),
                        ),
                        child: DropdownButtonHideUnderline(
                          child: DropdownButton<String>(
                            value: _monitor,
                            isExpanded: true,
                            icon: const Icon(Icons.arrow_drop_down_rounded),
                            style: const TextStyle(
                              color: _navy,
                              fontSize: 14,
                              fontWeight: FontWeight.w800,
                            ),
                            items: const [
                              DropdownMenuItem(
                                value: 'Monitor 1 - Principal',
                                child: Text('Monitor 1 - Principal'),
                              ),
                              DropdownMenuItem(
                                value: 'Monitor 2 - Cozinha',
                                child: Text('Monitor 2 - Cozinha'),
                              ),
                            ],
                            onChanged: (value) {
                              if (value != null) {
                                setState(() => _monitor = value);
                              }
                            },
                          ),
                        ),
                      ),
                      const SizedBox(width: 14),
                      InkWell(
                        onTap: () =>
                            setState(() => _voiceEnabled = !_voiceEnabled),
                        borderRadius: BorderRadius.circular(8),
                        child: Padding(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 6,
                            vertical: 4,
                          ),
                          child: Column(
                            mainAxisSize: MainAxisSize.min,
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Container(
                                    width: 9,
                                    height: 9,
                                    decoration: BoxDecoration(
                                      color: _voiceEnabled
                                          ? const Color(0xFF27A64B)
                                          : _textMuted,
                                      shape: BoxShape.circle,
                                    ),
                                  ),
                                  const SizedBox(width: 7),
                                  Text(
                                    _voiceEnabled
                                        ? 'Voz ligada'
                                        : 'Voz desligada',
                                    style: TextStyle(
                                      color: _voiceEnabled
                                          ? const Color(0xFF198C3B)
                                          : _textSecondary,
                                      fontSize: 11,
                                      fontWeight: FontWeight.w900,
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 3),
                              const Text(
                                'Atraso: 15 min',
                                style: TextStyle(
                                  color: _textSecondary,
                                  fontSize: 10,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ],
                );
              },
            ),
          ),
          Expanded(
            child: LayoutBuilder(
              builder: (context, constraints) {
                final wide = constraints.maxWidth >= 1080;
                final stationWidgets = stations
                    .map(
                      (station) => _KitchenStation(
                        title: station,
                        entries: entries
                            .where((entry) => entry.station == station)
                            .toList(),
                        completed: _completed,
                        selectedOrderId: selectedOrder?.id,
                        onSelect: (entry) =>
                            setState(() => _selectedOrderId = entry.order.id),
                        onToggle: _toggle,
                        bounded: wide,
                      ),
                    )
                    .toList();

                if (!wide) {
                  return SingleChildScrollView(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      children: [
                        for (final station in stationWidgets) ...[
                          station,
                          const SizedBox(height: 12),
                        ],
                        _KitchenOrderInspector(
                          order: selectedOrder,
                          entries: entries,
                          completed: _completed,
                          bounded: false,
                          onComplete: () => _completeOrder(selectedOrder),
                          onReprint: widget.store.flushSync,
                        ),
                      ],
                    ),
                  );
                }

                return Padding(
                  padding: const EdgeInsets.all(12),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      for (
                        var index = 0;
                        index < stationWidgets.length;
                        index++
                      ) ...[
                        Expanded(child: stationWidgets[index]),
                        if (index < stationWidgets.length - 1)
                          const SizedBox(width: 10),
                      ],
                      const SizedBox(width: 12),
                      SizedBox(
                        width: math.min(360, constraints.maxWidth * .27),
                        child: _KitchenOrderInspector(
                          order: selectedOrder,
                          entries: entries,
                          completed: _completed,
                          bounded: true,
                          onComplete: () => _completeOrder(selectedOrder),
                          onReprint: widget.store.flushSync,
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _KitchenEntry {
  const _KitchenEntry({
    required this.keyValue,
    required this.order,
    required this.item,
    required this.station,
  });

  final String keyValue;
  final Order order;
  final OrderItem item;
  final String station;
}

class _KitchenHeaderButton extends StatelessWidget {
  const _KitchenHeaderButton({
    required this.label,
    required this.onTap,
    this.filled = false,
  });

  final String label;
  final VoidCallback onTap;
  final bool filled;

  @override
  Widget build(BuildContext context) => SizedBox(
    height: 44,
    child: OutlinedButton(
      onPressed: onTap,
      style: OutlinedButton.styleFrom(
        foregroundColor: const Color(0xFF222222),
        backgroundColor: filled ? _blue : const Color(0xFFFFFCF9),
        side: const BorderSide(color: Color(0xFFD5C8BC)),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        textStyle: const TextStyle(fontSize: 13, fontWeight: FontWeight.w900),
      ),
      child: Text(label),
    ),
  );
}

class _KitchenConfigRow extends StatelessWidget {
  const _KitchenConfigRow({
    required this.icon,
    required this.title,
    required this.description,
  });

  final IconData icon;
  final String title;
  final String description;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: _surfaceMuted,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: _line),
      ),
      child: Row(
        children: [
          Container(
            width: 38,
            height: 38,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: _surface,
              borderRadius: BorderRadius.circular(9),
            ),
            child: Icon(icon, color: _blue, size: 19),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    color: _navy,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text(
                  description,
                  style: const TextStyle(color: _textSecondary, fontSize: 11),
                ),
              ],
            ),
          ),
          const Switch(value: true, onChanged: null),
        ],
      ),
    );
  }
}

class _KitchenStation extends StatelessWidget {
  const _KitchenStation({
    required this.title,
    required this.entries,
    required this.completed,
    required this.selectedOrderId,
    required this.onSelect,
    required this.onToggle,
    required this.bounded,
  });

  final String title;
  final List<_KitchenEntry> entries;
  final Set<String> completed;
  final String? selectedOrderId;
  final ValueChanged<_KitchenEntry> onSelect;
  final ValueChanged<_KitchenEntry> onToggle;
  final bool bounded;

  @override
  Widget build(BuildContext context) {
    final cards = entries.isEmpty
        ? const [
            Padding(
              padding: EdgeInsets.all(24),
              child: _Empty(text: 'Nenhum item nesta praca.'),
            ),
          ]
        : entries
              .map(
                (entry) => _KitchenTicketCard(
                  entry: entry,
                  done: completed.contains(entry.keyValue),
                  selected: entry.order.id == selectedOrderId,
                  onSelect: () => onSelect(entry),
                  onToggle: () => onToggle(entry),
                ),
              )
              .toList();
    final body = bounded
        ? Expanded(child: ListView(children: cards))
        : Column(children: cards);

    return Container(
      constraints: bounded
          ? const BoxConstraints()
          : const BoxConstraints(minHeight: 190),
      decoration: BoxDecoration(
        color: const Color(0xFFFFFCF9),
        border: Border.all(color: const Color(0xFFE0D7CE)),
      ),
      child: Column(
        mainAxisSize: bounded ? MainAxisSize.max : MainAxisSize.min,
        children: [
          Container(
            height: 60,
            padding: const EdgeInsets.symmetric(horizontal: 14),
            decoration: const BoxDecoration(
              border: Border(bottom: BorderSide(color: Color(0xFFE0D7CE))),
            ),
            child: Row(
              children: [
                Icon(
                  title == 'Forno'
                      ? Icons.microwave_outlined
                      : title == 'Fritadeira'
                      ? Icons.restaurant_outlined
                      : Icons.layers_outlined,
                  color: const Color(0xFF222222),
                  size: 20,
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    title,
                    style: const TextStyle(
                      color: Color(0xFF222222),
                      fontSize: 16,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
                const Icon(
                  Icons.more_horiz_rounded,
                  color: _textSecondary,
                  size: 20,
                ),
                const SizedBox(width: 8),
                Container(
                  width: 30,
                  height: 30,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: entries.isEmpty
                        ? const Color(0xFFEFEAE5)
                        : const Color(0xFFFFE5DF),
                    shape: BoxShape.circle,
                  ),
                  child: Text(
                    '${entries.length}',
                    style: TextStyle(
                      color: entries.isEmpty ? _navy : _danger,
                      fontSize: 11,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
              ],
            ),
          ),
          body,
        ],
      ),
    );
  }
}

class _KitchenTicketCard extends StatelessWidget {
  const _KitchenTicketCard({
    required this.entry,
    required this.done,
    required this.selected,
    required this.onSelect,
    required this.onToggle,
  });

  final _KitchenEntry entry;
  final bool done;
  final bool selected;
  final VoidCallback onSelect;
  final VoidCallback onToggle;

  @override
  Widget build(BuildContext context) {
    final minutes = math.max(
      0,
      DateTime.now().difference(entry.order.createdAt).inMinutes,
    );
    final upperName = entry.item.name.toUpperCase();
    final note = upperName.contains('PIZZA')
        ? 'sem ervilha'
        : upperName.contains('BATATA')
        ? 'uma porção sem sal'
        : upperName.contains('X-BACON')
        ? '1 sem cebola'
        : upperName.contains('SALADA')
        ? 'molho separado'
        : '';
    return InkWell(
      onTap: onSelect,
      child: Container(
        margin: const EdgeInsets.fromLTRB(8, 8, 8, 0),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: done ? const Color(0xFFEAF7E8) : const Color(0xFFFFFAF4),
          border: Border.all(
            color: selected ? _blue : const Color(0xFFDDD1C5),
            width: selected ? 1.6 : 1,
          ),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '${entry.item.quantity}x  ${entry.item.name.toUpperCase()}',
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Color(0xFF252525),
                      fontSize: 13,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  if (note.isNotEmpty) ...[
                    const SizedBox(height: 5),
                    Text(
                      note,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: Color(0xFF6D6259),
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                  const SizedBox(height: 7),
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          entry.order.kind == OrderKind.delivery ||
                                  entry.order.kind == OrderKind.ifood
                              ? 'Pedido ${entry.order.number}'
                              : 'Mesa ${_kitchenDisplayNumber(entry.order.number)}',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: Color(0xFF6D6259),
                            fontSize: 11,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                      Text(
                        '${minutes.toString().padLeft(2, '0')}:03',
                        style: TextStyle(
                          color: done ? const Color(0xFF15843B) : _blue,
                          fontSize: 11,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            IconButton(
              onPressed: onToggle,
              icon: Icon(
                done ? Icons.check_circle_rounded : Icons.circle_outlined,
                color: done ? const Color(0xFF27A64B) : _blue,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _KitchenOrderInspector extends StatelessWidget {
  const _KitchenOrderInspector({
    required this.order,
    required this.entries,
    required this.completed,
    required this.bounded,
    required this.onComplete,
    required this.onReprint,
  });

  final Order? order;
  final List<_KitchenEntry> entries;
  final Set<String> completed;
  final bool bounded;
  final VoidCallback onComplete;
  final VoidCallback onReprint;

  @override
  Widget build(BuildContext context) {
    final currentOrder = order;
    final selectedEntries = currentOrder == null
        ? <_KitchenEntry>[]
        : entries.where((entry) => entry.order.id == currentOrder.id).toList();
    final done = selectedEntries
        .where((entry) => completed.contains(entry.keyValue))
        .length;
    final allDone =
        selectedEntries.isNotEmpty && done == selectedEntries.length;
    final elapsed = currentOrder == null
        ? Duration.zero
        : DateTime.now().difference(currentOrder.createdAt);
    final itemsList = ListView.separated(
      itemCount: selectedEntries.length,
      shrinkWrap: !bounded,
      physics: bounded
          ? const ClampingScrollPhysics()
          : const NeverScrollableScrollPhysics(),
      separatorBuilder: (context, index) =>
          const Divider(height: 1, color: _line),
      itemBuilder: (context, index) {
        final entry = selectedEntries[index];
        final completedEntry = completed.contains(entry.keyValue);
        return Container(
          padding: const EdgeInsets.symmetric(horizontal: 2, vertical: 12),
          color: completedEntry ? const Color(0xFFEAF7E8) : Colors.transparent,
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              SizedBox(
                width: 24,
                child: Text(
                  '${entry.item.quantity}×',
                  style: const TextStyle(
                    color: _navy,
                    fontSize: 12,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      entry.item.name.toUpperCase(),
                      style: const TextStyle(
                        color: _navy,
                        fontSize: 12,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      _kitchenNoteFor(entry.item.name),
                      style: const TextStyle(
                        color: _textSecondary,
                        fontSize: 10,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );

    return Container(
      width: double.infinity,
      constraints: const BoxConstraints(minHeight: 360),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: _surface,
        border: Border.all(color: _line),
        borderRadius: BorderRadius.circular(12),
      ),
      child: currentOrder == null
          ? const _Empty(text: 'Nenhum pedido em preparo.')
          : Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        currentOrder.kind == OrderKind.table
                            ? 'MESA ${_kitchenDisplayNumber(currentOrder.number)}'
                            : 'PEDIDO ${currentOrder.number}',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: _navy,
                          fontSize: 27,
                          height: 1,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 7,
                      ),
                      decoration: BoxDecoration(
                        color: _surface,
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(color: _blue),
                      ),
                      child: Text(
                        '${elapsed.inMinutes.toString().padLeft(2, '0')}:03',
                        style: const TextStyle(
                          color: _blue2,
                          fontSize: 11,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 9),
                Text(
                  '4 pessoas  •  Garçom ${currentOrder.waiter}',
                  style: const TextStyle(
                    color: _textSecondary,
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  'Origem: ${currentOrder.kind == OrderKind.table ? 'Salão' : kindLabel(currentOrder.kind)}',
                  style: const TextStyle(color: _textSecondary, fontSize: 11),
                ),
                const SizedBox(height: 24),
                const Text(
                  'Progresso por praça',
                  style: TextStyle(
                    color: _navy,
                    fontSize: 12,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    for (final station in const [
                      'Forno',
                      'Fritadeira',
                      'Montagem',
                    ]) ...[
                      Expanded(
                        child: _KitchenStationProgress(
                          station: station,
                          entries: selectedEntries,
                          completed: completed,
                        ),
                      ),
                      if (station != 'Montagem') const SizedBox(width: 7),
                    ],
                  ],
                ),
                const SizedBox(height: 24),
                const Text(
                  'Itens do pedido',
                  style: TextStyle(
                    color: _navy,
                    fontSize: 12,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 8),
                if (bounded) Expanded(child: itemsList) else itemsList,
                const SizedBox(height: 12),
                SizedBox(
                  width: double.infinity,
                  height: 50,
                  child: FilledButton(
                    onPressed: allDone ? onComplete : null,
                    style: FilledButton.styleFrom(
                      backgroundColor: _blue,
                      disabledBackgroundColor: _surfaceMuted,
                      disabledForegroundColor: const Color(0xFF9D9690),
                    ),
                    child: Text(
                      allDone ? 'Concluir pedido' : 'Pedido ainda incompleto',
                    ),
                  ),
                ),
                const SizedBox(height: 10),
                SizedBox(
                  width: double.infinity,
                  height: 50,
                  child: OutlinedButton(
                    onPressed: onReprint,
                    child: const Text('Reimprimir cozinha'),
                  ),
                ),
              ],
            ),
    );
  }
}

class _KitchenStationProgress extends StatelessWidget {
  const _KitchenStationProgress({
    required this.station,
    required this.entries,
    required this.completed,
  });

  final String station;
  final List<_KitchenEntry> entries;
  final Set<String> completed;

  @override
  Widget build(BuildContext context) {
    final stationEntries = entries
        .where((entry) => entry.station == station)
        .toList();
    final ready = stationEntries
        .where((entry) => completed.contains(entry.keyValue))
        .length;
    final total = stationEntries.length;
    final isReady = total > 0 && ready == total;
    return Container(
      padding: const EdgeInsets.fromLTRB(8, 9, 8, 8),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: _line),
      ),
      child: Column(
        children: [
          Text(
            station,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: _textSecondary,
              fontSize: 10,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 7),
          Text(
            '$ready/$total',
            style: TextStyle(
              color: isReady
                  ? const Color(0xFF20A64A)
                  : const Color(0xFFE12E24),
              fontSize: 19,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 7),
          LinearProgressIndicator(
            minHeight: 4,
            value: total == 0 ? 0 : ready / total,
            color: const Color(0xFF20A64A),
            backgroundColor: const Color(0xFFE8DED4),
            borderRadius: BorderRadius.circular(4),
          ),
        ],
      ),
    );
  }
}

String _kitchenNoteFor(String name) {
  final upper = name.toUpperCase();
  if (upper.contains('PIZZA')) return 'metade sem azeitona';
  if (upper.contains('BATATA')) return 'uma porção sem sal';
  if (upper.contains('X-BACON')) return '1 sem cebola';
  if (upper.contains('SALADA')) return 'molho separado';
  return 'sem observações';
}

// ignore: unused_element
class _LegacyKitchenOrderInspector extends StatelessWidget {
  const _LegacyKitchenOrderInspector({
    required this.order,
    required this.entries,
    required this.completed,
  });

  final Order? order;
  final List<_KitchenEntry> entries;
  final Set<String> completed;

  @override
  Widget build(BuildContext context) {
    final selectedEntries = order == null
        ? <_KitchenEntry>[]
        : entries.where((entry) => entry.order.id == order!.id).toList();
    final done = selectedEntries
        .where((entry) => completed.contains(entry.keyValue))
        .length;
    return Container(
      width: double.infinity,
      constraints: const BoxConstraints(minHeight: 360),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFFFFCF9),
        border: Border.all(color: const Color(0xFFE0D7CE)),
        borderRadius: BorderRadius.circular(8),
      ),
      child: order == null
          ? const _Empty(text: 'Nenhum pedido em preparo.')
          : Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  order!.kind == OrderKind.table
                      ? 'MESA ${order!.number}'
                      : 'PEDIDO ${order!.number}',
                  style: const TextStyle(
                    color: Color(0xFF222222),
                    fontSize: 22,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  '${order!.customerName.isEmpty ? 'Cliente' : order!.customerName}  -  Garcom ${order!.waiter}',
                  style: const TextStyle(
                    color: Color(0xFF6D6259),
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 22),
                const Text(
                  'Progresso por praca',
                  style: TextStyle(
                    color: Color(0xFF222222),
                    fontSize: 12,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 10),
                LinearProgressIndicator(
                  minHeight: 8,
                  value: selectedEntries.isEmpty
                      ? 0
                      : done / selectedEntries.length,
                  color: const Color(0xFF27A64B),
                  backgroundColor: const Color(0xFFE8DED4),
                  borderRadius: BorderRadius.circular(8),
                ),
                const SizedBox(height: 8),
                Text(
                  '$done/${selectedEntries.length} item(ns) pronto(s)',
                  style: const TextStyle(
                    color: Color(0xFF6D6259),
                    fontSize: 11,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 22),
                const Text(
                  'Itens do pedido',
                  style: TextStyle(
                    color: Color(0xFF222222),
                    fontSize: 12,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 8),
                for (final entry in selectedEntries)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 8),
                    child: Row(
                      children: [
                        Icon(
                          completed.contains(entry.keyValue)
                              ? Icons.check_circle_rounded
                              : Icons.circle_outlined,
                          size: 17,
                          color: completed.contains(entry.keyValue)
                              ? const Color(0xFF27A64B)
                              : const Color(0xFF9D9187),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            '${entry.item.quantity}x  ${entry.item.name}',
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: Color(0xFF2B2927),
                              fontSize: 12,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
    );
  }
}

class _CashModule extends StatefulWidget {
  const _CashModule({required this.store, required this.openBlocked});

  final BalcaoStore store;
  final VoidCallback openBlocked;

  @override
  State<_CashModule> createState() => _CashModuleState();
}

class _CashModuleState extends State<_CashModule> {
  final amount = TextEditingController();
  final note = TextEditingController();

  @override
  void dispose() {
    amount.dispose();
    note.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return _WindowPanel(
      title: 'Caixa operacional',
      action: TextButton(
        onPressed: _toggleCash,
        child: Text(widget.store.cashOpen ? 'Fechar' : 'Abrir'),
      ),
      child: Column(
        children: [
          Row(
            children: [
              Expanded(
                child: _MiniSummary(
                  label: 'Em aberto',
                  value: money(widget.store.openTotal),
                  sub: '${widget.store.openOrders.length} comandas',
                  color: _navy2,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _MiniSummary(
                  label: 'Lucro bruto',
                  value: money(widget.store.grossProfit),
                  sub: 'margem do dia',
                  color: _teal,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: _DeskInput(
                  label: 'Valor',
                  controller: amount,
                  keyboardType: TextInputType.number,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _DeskInput(label: 'Historico', controller: note),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Expanded(
                child: _DeskCommandButton(
                  label: 'Suprimento',
                  color: _teal,
                  onTap: () => _movement('SUPRIMENTO'),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _DeskCommandButton(
                  label: 'Sangria',
                  color: _danger,
                  onTap: () => _movement('SANGRIA'),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          ...widget.store.movements
              .take(8)
              .map(
                (movement) => Container(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  decoration: const BoxDecoration(
                    border: Border(top: BorderSide(color: _line)),
                  ),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              movement.type,
                              style: const TextStyle(
                                fontWeight: FontWeight.w900,
                              ),
                            ),
                            Text(
                              movement.note,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                color: _textSecondary,
                                fontSize: 12,
                              ),
                            ),
                          ],
                        ),
                      ),
                      Text(
                        money(movement.amount),
                        style: const TextStyle(fontWeight: FontWeight.w900),
                      ),
                    ],
                  ),
                ),
              ),
        ],
      ),
    );
  }

  Future<void> _movement(String type) async {
    final value = _parse(amount.text);
    if (value <= 0) return;
    await widget.store.addMovement(
      type,
      type == 'SANGRIA' ? -value : value,
      note.text.trim().isEmpty ? type : note.text.trim(),
    );
    amount.clear();
    note.clear();
  }

  void _toggleCash() {
    if (widget.store.cashOpen && widget.store.openOrders.isNotEmpty) {
      widget.openBlocked();
      return;
    }
    widget.store.toggleCash();
  }
}

class _TeamModule extends StatefulWidget {
  const _TeamModule({required this.store});

  final BalcaoStore store;

  @override
  State<_TeamModule> createState() => _TeamModuleState();
}

class _TeamModuleState extends State<_TeamModule> {
  final number = TextEditingController(text: '4');
  final name = TextEditingController();
  final pin = TextEditingController();
  String role = 'GARCOM';

  @override
  void dispose() {
    number.dispose();
    name.dispose();
    pin.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(12),
      children: [
        _WindowPanel(
          title: 'Novo membro',
          child: Column(
            children: [
              Row(
                children: [
                  SizedBox(
                    width: 86,
                    child: _DeskInput(
                      key: const Key('teamMemberNumber'),
                      label: 'Numero',
                      controller: number,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _DeskInput(
                      key: const Key('teamMemberName'),
                      label: 'Nome',
                      controller: name,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: _DeskSelect(
                      label: 'Funcao',
                      value: role,
                      items: const ['GARCOM', 'CAIXA', 'GERENTE', 'ENTREGADOR'],
                      onChanged: (value) => setState(() => role = value),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _DeskInput(
                      key: const Key('teamMemberPin'),
                      label: 'Senha',
                      controller: pin,
                      keyboardType: TextInputType.number,
                      obscureText: true,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _DeskCommandButton(
                      key: const Key('teamMemberSave'),
                      label: 'Salvar equipe',
                      color: _teal,
                      onTap: _save,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 10),
        _WindowPanel(
          title: 'Equipe cadastrada',
          child: Column(
            children: widget.store.teamMembers
                .map(
                  (member) => _TeamRow(
                    number: member.number,
                    name: member.name,
                    role: member.role,
                    sales: widget.store.closedOrders
                        .where((order) => order.waiter == member.number)
                        .fold(0, (sum, order) => sum + order.subtotal),
                  ),
                )
                .toList(),
          ),
        ),
      ],
    );
  }

  Future<void> _save() async {
    if (name.text.trim().isEmpty) return;
    final saved = await widget.store.saveTeamMember(
      number: number.text,
      name: name.text,
      role: role,
      pin: pin.text,
    );
    if (!mounted || !saved) return;
    setState(() {
      final next =
          (int.tryParse(number.text.trim()) ??
              widget.store.teamMembers.length) +
          1;
      number.text = '$next';
      name.clear();
      pin.clear();
    });
  }
}

class _TeamRow extends StatelessWidget {
  const _TeamRow({
    required this.number,
    required this.name,
    required this.role,
    required this.sales,
  });

  final String number;
  final String name;
  final String role;
  final double sales;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: _surfaceMuted,
        border: Border.all(color: _line),
        borderRadius: BorderRadius.circular(7),
      ),
      child: Row(
        children: [
          Container(
            width: 34,
            height: 34,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: _mint,
              borderRadius: BorderRadius.circular(7),
            ),
            child: Text(
              number,
              style: const TextStyle(
                fontWeight: FontWeight.w900,
                color: _navy2,
              ),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(name, style: const TextStyle(fontWeight: FontWeight.w900)),
                Text(
                  role,
                  style: const TextStyle(color: _textSecondary, fontSize: 12),
                ),
              ],
            ),
          ),
          Text(
            money(sales),
            style: const TextStyle(fontWeight: FontWeight.w900),
          ),
        ],
      ),
    );
  }
}

class _StockModule extends StatefulWidget {
  const _StockModule({required this.store});

  final BalcaoStore store;

  @override
  State<_StockModule> createState() => _StockModuleState();
}

class _StockModuleState extends State<_StockModule> {
  final search = TextEditingController();
  final movementQuantity = TextEditingController(text: '1');
  final movementNote = TextEditingController();
  final countedStock = TextEditingController();
  final countedMinimum = TextEditingController();
  Product? selected;
  int tab = 0;
  bool onlyCritical = false;
  bool isExit = false;

  BalcaoStore get store => widget.store;

  @override
  void initState() {
    super.initState();
    selected = store.products.firstOrNull;
    _syncCountFields();
  }

  @override
  void dispose() {
    search.dispose();
    movementQuantity.dispose();
    movementNote.dispose();
    countedStock.dispose();
    countedMinimum.dispose();
    super.dispose();
  }

  void _syncCountFields() {
    countedStock.text = '${selected?.stock ?? 0}';
    countedMinimum.text = '${selected?.minStock ?? 0}';
  }

  List<Product> get _filteredProducts {
    final query = search.text.trim().toLowerCase();
    return store.products.where((product) {
      final matchesQuery =
          query.isEmpty ||
          product.code.toLowerCase().contains(query) ||
          product.name.toLowerCase().contains(query) ||
          product.category.toLowerCase().contains(query);
      final matchesCritical =
          !onlyCritical || product.stock <= product.minStock;
      return matchesQuery && matchesCritical;
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final cost = store.products.fold<double>(
      0,
      (sum, product) => sum + product.stock * product.cost,
    );
    final potential = store.products.fold<double>(
      0,
      (sum, product) => sum + product.stock * product.price,
    );
    final gross = potential - cost;
    final critical = store.products
        .where((product) => product.stock <= product.minStock)
        .length;

    return ColoredBox(
      color: _paper,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(14, 12, 14, 14),
        child: LayoutBuilder(
          builder: (context, constraints) {
            final desktop = constraints.maxWidth >= 1040;
            return Column(
              children: [
                _buildToolbar(desktop, critical),
                const SizedBox(height: 10),
                _buildFinancialSummary(cost, potential, gross, desktop),
                const SizedBox(height: 10),
                Expanded(
                  child: switch (tab) {
                    1 => _buildMovementHistory(desktop),
                    2 => _buildCounting(desktop),
                    _ => _buildProducts(desktop),
                  },
                ),
              ],
            );
          },
        ),
      ),
    );
  }

  Widget _buildToolbar(bool desktop, int critical) {
    final tabs = [('Produtos', 0), ('Movimentações', 1), ('Contagem', 2)];
    final tabStrip = Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        for (final item in tabs)
          TextButton(
            onPressed: () => setState(() => tab = item.$2),
            style: TextButton.styleFrom(
              foregroundColor: tab == item.$2 ? _blue : _navy,
              backgroundColor: tab == item.$2
                  ? const Color(0xFFFFEDE4)
                  : Colors.transparent,
              shape: const RoundedRectangleBorder(
                borderRadius: BorderRadius.zero,
              ),
              side: BorderSide(
                color: tab == item.$2 ? _blue : Colors.transparent,
                width: 0,
              ),
            ),
            child: Text(
              item.$1,
              style: const TextStyle(fontWeight: FontWeight.w800),
            ),
          ),
      ],
    );
    final searchField = SizedBox(
      width: desktop ? 420 : double.infinity,
      height: 46,
      child: TextField(
        key: const Key('stockSearch'),
        controller: search,
        onChanged: (_) => setState(() {}),
        decoration: const InputDecoration(
          hintText: 'Buscar por código, produto ou categoria',
          prefixIcon: Icon(Icons.search_rounded),
          contentPadding: EdgeInsets.symmetric(horizontal: 14),
        ),
      ),
    );
    final actions = Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        FilterChip(
          key: const Key('stockCriticalFilter'),
          selected: onlyCritical,
          onSelected: (value) => setState(() => onlyCritical = value),
          label: Text('Somente críticos ($critical)'),
          avatar: const Icon(Icons.warning_amber_rounded, size: 18),
        ),
        const SizedBox(width: 8),
        FilledButton.icon(
          key: const Key('stockCreateProduct'),
          onPressed: () => _showProductEditor(null),
          icon: const Icon(Icons.inventory_2_outlined, size: 18),
          label: const Text('Criar produto'),
        ),
      ],
    );

    if (!desktop) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              const Expanded(
                child: Text(
                  'Controle de estoque',
                  style: TextStyle(
                    color: _navy,
                    fontSize: 22,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ),
              FilledButton(
                onPressed: () => _showProductEditor(null),
                child: const Text('Criar'),
              ),
            ],
          ),
          const SizedBox(height: 6),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: tabStrip,
          ),
          const SizedBox(height: 8),
          searchField,
          const SizedBox(height: 8),
          Align(alignment: Alignment.centerLeft, child: actions.children.first),
        ],
      );
    }

    return Row(
      children: [
        const SizedBox(
          width: 320,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Controle de estoque',
                style: TextStyle(
                  color: _navy,
                  fontSize: 23,
                  fontWeight: FontWeight.w900,
                ),
              ),
              Text(
                'Acompanhe quantidade, custo, venda potencial e reposição.',
                style: TextStyle(color: _textSecondary, fontSize: 11),
              ),
            ],
          ),
        ),
        tabStrip,
        const Spacer(),
        searchField,
        const SizedBox(width: 8),
        actions,
      ],
    );
  }

  Widget _buildFinancialSummary(
    double cost,
    double potential,
    double gross,
    bool desktop,
  ) {
    final cards = [
      _StockMetric(
        label: 'Custo total em estoque',
        value: money(cost),
        icon: Icons.account_balance_wallet_outlined,
      ),
      _StockMetric(
        label: 'Venda potencial',
        value: money(potential),
        icon: Icons.trending_up_rounded,
      ),
      _StockMetric(
        label: 'Lucro bruto estimado',
        value: money(gross),
        icon: Icons.show_chart_rounded,
        highlight: true,
      ),
    ];
    if (desktop) {
      return SizedBox(
        height: 78,
        child: Row(
          children: [
            for (var index = 0; index < cards.length; index++) ...[
              Expanded(child: cards[index]),
              if (index < cards.length - 1) const SizedBox(width: 8),
            ],
            const SizedBox(width: 12),
            const Expanded(
              child: Text(
                'Estimativa baseada no estoque atual, custo médio e preço de venda cadastrado.',
                style: TextStyle(color: _textSecondary, fontSize: 11),
              ),
            ),
          ],
        ),
      );
    }
    return SizedBox(
      height: 86,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        itemCount: cards.length,
        separatorBuilder: (_, _) => const SizedBox(width: 8),
        itemBuilder: (_, index) => SizedBox(width: 230, child: cards[index]),
      ),
    );
  }

  Widget _buildProducts(bool desktop) {
    final list = _StockProductList(
      products: _filteredProducts,
      selected: selected,
      onSelected: (product) => setState(() {
        selected = product;
        _syncCountFields();
      }),
    );
    final inspector = _StockInspector(
      store: store,
      product: selected,
      movementQuantity: movementQuantity,
      movementNote: movementNote,
      isExit: isExit,
      onMovementTypeChanged: (value) => setState(() => isExit = value),
      onRegister: _registerMovement,
      onEdit: selected == null ? null : () => _showProductEditor(selected),
      onPhoto: selected == null ? null : _pickProductImage,
    );
    if (desktop) {
      return Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Expanded(flex: 19, child: list),
          const SizedBox(width: 12),
          SizedBox(width: 570, child: inspector),
        ],
      );
    }
    return ListView(
      children: [
        SizedBox(height: 410, child: list),
        const SizedBox(height: 10),
        inspector,
      ],
    );
  }

  Widget _buildMovementHistory(bool desktop) {
    final history = store.stockMovements;
    final list = Container(
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: _line),
      ),
      child: history.isEmpty
          ? const _Empty(text: 'As entradas, saídas e ajustes aparecerão aqui.')
          : ListView.separated(
              itemCount: history.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (context, index) {
                final movement = history[index];
                final product = store.products
                    .where((item) => item.id == movement.productId)
                    .firstOrNull;
                return ListTile(
                  leading: CircleAvatar(
                    backgroundColor: movement.quantity < 0
                        ? const Color(0xFFFFE7E7)
                        : const Color(0xFFE8F7EC),
                    foregroundColor: movement.quantity < 0 ? _danger : _teal,
                    child: Icon(
                      movement.quantity < 0
                          ? Icons.arrow_upward_rounded
                          : Icons.arrow_downward_rounded,
                    ),
                  ),
                  title: Text(
                    product?.name ?? 'Produto',
                    style: const TextStyle(fontWeight: FontWeight.w900),
                  ),
                  subtitle: Text(
                    '${movement.type} • ${movement.note} • ${_shortDay(movement.createdAt)} ${_shortTime(movement.createdAt)}',
                  ),
                  trailing: Text(
                    '${movement.quantity > 0 ? '+' : ''}${movement.quantity} ${product?.unit ?? 'un'}',
                    style: TextStyle(
                      color: movement.quantity < 0 ? _danger : _teal,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                );
              },
            ),
    );
    if (!desktop) return list;
    return Row(
      children: [
        Expanded(child: list),
        const SizedBox(width: 12),
        SizedBox(
          width: 420,
          child: _StockMovementSummary(
            store: store,
            selected: selected,
            onSelected: (product) => setState(() => selected = product),
          ),
        ),
      ],
    );
  }

  Widget _buildCounting(bool desktop) {
    final list = _StockProductList(
      products: _filteredProducts,
      selected: selected,
      onSelected: (product) => setState(() {
        selected = product;
        _syncCountFields();
      }),
    );
    final countPanel = _StockCountPanel(
      product: selected,
      stock: countedStock,
      minimum: countedMinimum,
      onSave: _saveCount,
    );
    if (desktop) {
      return Row(
        children: [
          Expanded(child: list),
          const SizedBox(width: 12),
          SizedBox(width: 440, child: countPanel),
        ],
      );
    }
    return ListView(
      children: [
        SizedBox(height: 390, child: list),
        const SizedBox(height: 10),
        countPanel,
      ],
    );
  }

  Future<void> _registerMovement() async {
    final product = selected;
    final quantity = int.tryParse(movementQuantity.text.trim()) ?? 0;
    if (product == null || quantity <= 0) return;
    await store.registerStockMovement(
      product: product,
      quantity: quantity,
      isExit: isExit,
      note: movementNote.text,
    );
    movementQuantity.text = '1';
    movementNote.clear();
    if (mounted) setState(_syncCountFields);
  }

  Future<void> _saveCount() async {
    final product = selected;
    if (product == null) return;
    final stock = int.tryParse(countedStock.text.trim()) ?? product.stock;
    final minimum =
        int.tryParse(countedMinimum.text.trim()) ?? product.minStock;
    product.minStock = minimum;
    await store.adjustStock(product, stock);
    if (mounted) setState(() {});
  }

  Future<void> _pickProductImage() async {
    final product = selected;
    if (product == null) return;
    final result = await FilePicker.platform.pickFiles(
      type: FileType.image,
      withData: true,
    );
    final file = result?.files.firstOrNull;
    final bytes = file?.bytes;
    if (bytes == null || bytes.isEmpty) return;
    final extension = (file?.extension ?? 'png').toLowerCase();
    final mime = extension == 'jpg' || extension == 'jpeg'
        ? 'image/jpeg'
        : extension == 'webp'
        ? 'image/webp'
        : 'image/png';
    await store.updateProduct(
      product: product,
      name: product.name,
      code: product.code,
      category: product.category,
      price: product.price,
      cost: product.cost,
      minStock: product.minStock,
      unit: product.unit,
      imageData: 'data:$mime;base64,${base64Encode(bytes)}',
    );
    if (mounted) setState(() {});
  }

  Future<void> _showProductEditor(Product? product) async {
    final saved = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) =>
          _StockProductEditorDialog(store: store, product: product),
    );
    if (!mounted || saved != true) return;
    setState(() {
      selected = product ?? store.products.firstOrNull;
      _syncCountFields();
    });
  }
}

class _StockMetric extends StatelessWidget {
  const _StockMetric({
    required this.label,
    required this.value,
    required this.icon,
    this.highlight = false,
  });

  final String label;
  final String value;
  final IconData icon;
  final bool highlight;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(11),
        border: Border.all(color: highlight ? _blue : _line),
      ),
      child: Row(
        children: [
          Icon(icon, color: highlight ? _blue : _textSecondary, size: 21),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: _textSecondary, fontSize: 10),
                ),
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _navy,
                    fontSize: 18,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _StockProductList extends StatelessWidget {
  const _StockProductList({
    required this.products,
    required this.selected,
    required this.onSelected,
  });

  final List<Product> products;
  final Product? selected;
  final ValueChanged<Product> onSelected;

  @override
  Widget build(BuildContext context) {
    final compact = MediaQuery.sizeOf(context).width < 760;
    return Container(
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: _line),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          Container(
            height: 44,
            padding: const EdgeInsets.symmetric(horizontal: 12),
            color: const Color(0xFFFFFAF5),
            child: compact
                ? const Row(
                    children: [
                      Expanded(child: Text('Produto')),
                      Text('Estoque e valor'),
                    ],
                  )
                : const Row(
                    children: [
                      Expanded(flex: 7, child: Text('Produto')),
                      Expanded(flex: 2, child: Text('Atual')),
                      Expanded(flex: 2, child: Text('Mínimo')),
                      Expanded(flex: 3, child: Text('Status')),
                      Expanded(flex: 3, child: Text('Custo em estoque')),
                      Expanded(flex: 3, child: Text('Venda potencial')),
                    ],
                  ),
          ),
          Expanded(
            child: products.isEmpty
                ? const _Empty(text: 'Nenhum produto encontrado.')
                : ListView.separated(
                    itemCount: products.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      final product = products[index];
                      final isSelected = selected?.id == product.id;
                      final critical = product.stock <= product.minStock;
                      return Material(
                        color: isSelected
                            ? const Color(0xFFFFEDE2)
                            : Colors.transparent,
                        child: InkWell(
                          key: Key('stockProduct-${product.code}'),
                          onTap: () => onSelected(product),
                          child: Container(
                            height: 64,
                            padding: const EdgeInsets.symmetric(horizontal: 10),
                            decoration: BoxDecoration(
                              border: Border(
                                left: BorderSide(
                                  color: isSelected
                                      ? _blue
                                      : Colors.transparent,
                                  width: 3,
                                ),
                              ),
                            ),
                            child: compact
                                ? Row(
                                    children: [
                                      _ProductPhoto(product: product, size: 40),
                                      const SizedBox(width: 8),
                                      Expanded(
                                        child: Column(
                                          mainAxisAlignment:
                                              MainAxisAlignment.center,
                                          crossAxisAlignment:
                                              CrossAxisAlignment.start,
                                          children: [
                                            Text(
                                              product.name,
                                              maxLines: 1,
                                              overflow: TextOverflow.ellipsis,
                                              style: const TextStyle(
                                                fontWeight: FontWeight.w900,
                                              ),
                                            ),
                                            Text(
                                              '${product.code} • ${critical ? 'Abaixo do mínimo' : 'Adequado'}',
                                              maxLines: 1,
                                              overflow: TextOverflow.ellipsis,
                                              style: TextStyle(
                                                color: critical
                                                    ? _danger
                                                    : _teal,
                                                fontSize: 9,
                                                fontWeight: FontWeight.w700,
                                              ),
                                            ),
                                          ],
                                        ),
                                      ),
                                      const SizedBox(width: 8),
                                      Column(
                                        mainAxisAlignment:
                                            MainAxisAlignment.center,
                                        crossAxisAlignment:
                                            CrossAxisAlignment.end,
                                        children: [
                                          Text(
                                            '${product.stock} ${product.unit}',
                                            style: const TextStyle(
                                              fontWeight: FontWeight.w900,
                                            ),
                                          ),
                                          Text(
                                            money(
                                              product.stock * product.price,
                                            ),
                                            style: const TextStyle(
                                              color: _textSecondary,
                                              fontSize: 9,
                                            ),
                                          ),
                                        ],
                                      ),
                                    ],
                                  )
                                : Row(
                                    children: [
                                      Expanded(
                                        flex: 7,
                                        child: Row(
                                          children: [
                                            _ProductPhoto(
                                              product: product,
                                              size: 42,
                                            ),
                                            const SizedBox(width: 9),
                                            Expanded(
                                              child: Column(
                                                mainAxisAlignment:
                                                    MainAxisAlignment.center,
                                                crossAxisAlignment:
                                                    CrossAxisAlignment.start,
                                                children: [
                                                  Text(
                                                    product.name,
                                                    maxLines: 1,
                                                    overflow:
                                                        TextOverflow.ellipsis,
                                                    style: const TextStyle(
                                                      fontWeight:
                                                          FontWeight.w900,
                                                    ),
                                                  ),
                                                  Text(
                                                    '${product.code} • ${product.category}',
                                                    maxLines: 1,
                                                    overflow:
                                                        TextOverflow.ellipsis,
                                                    style: const TextStyle(
                                                      color: _textSecondary,
                                                      fontSize: 9,
                                                    ),
                                                  ),
                                                ],
                                              ),
                                            ),
                                          ],
                                        ),
                                      ),
                                      Expanded(
                                        flex: 2,
                                        child: Text(
                                          '${product.stock} ${product.unit}',
                                        ),
                                      ),
                                      Expanded(
                                        flex: 2,
                                        child: Text(
                                          '${product.minStock} ${product.unit}',
                                        ),
                                      ),
                                      Expanded(
                                        flex: 3,
                                        child: Align(
                                          alignment: Alignment.centerLeft,
                                          child: _StatusPill(
                                            text: critical
                                                ? 'Abaixo do mínimo'
                                                : 'Adequado',
                                            color: critical ? _danger : _teal,
                                          ),
                                        ),
                                      ),
                                      Expanded(
                                        flex: 3,
                                        child: Text(
                                          money(product.stock * product.cost),
                                        ),
                                      ),
                                      Expanded(
                                        flex: 3,
                                        child: Text(
                                          money(product.stock * product.price),
                                          style: const TextStyle(
                                            fontWeight: FontWeight.w800,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                          ),
                        ),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}

class _StockInspector extends StatelessWidget {
  const _StockInspector({
    required this.store,
    required this.product,
    required this.movementQuantity,
    required this.movementNote,
    required this.isExit,
    required this.onMovementTypeChanged,
    required this.onRegister,
    required this.onEdit,
    required this.onPhoto,
  });

  final BalcaoStore store;
  final Product? product;
  final TextEditingController movementQuantity;
  final TextEditingController movementNote;
  final bool isExit;
  final ValueChanged<bool> onMovementTypeChanged;
  final VoidCallback onRegister;
  final VoidCallback? onEdit;
  final VoidCallback? onPhoto;

  @override
  Widget build(BuildContext context) {
    final product = this.product;
    if (product == null) {
      return const _Empty(text: 'Selecione um produto para ver os detalhes.');
    }
    final invested = product.stock * product.cost;
    final potential = product.stock * product.price;
    final profit = potential - invested;
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: _line),
      ),
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Stack(
                  children: [
                    _ProductPhoto(product: product, size: 126),
                    Positioned(
                      right: 5,
                      bottom: 5,
                      child: IconButton.filled(
                        key: const Key('stockChangePhoto'),
                        onPressed: onPhoto,
                        icon: const Icon(Icons.photo_camera_outlined, size: 18),
                        tooltip: 'Trocar foto',
                      ),
                    ),
                  ],
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              product.name,
                              style: const TextStyle(
                                color: _navy,
                                fontSize: 20,
                                fontWeight: FontWeight.w900,
                              ),
                            ),
                          ),
                          IconButton(
                            onPressed: onEdit,
                            icon: const Icon(Icons.edit_outlined),
                            tooltip: 'Editar produto',
                          ),
                        ],
                      ),
                      Text(
                        'Código ${product.code}\nCategoria: ${product.category}\nUnidade: ${product.unit}',
                        style: const TextStyle(
                          color: _textSecondary,
                          fontSize: 11,
                          height: 1.45,
                        ),
                      ),
                      const SizedBox(height: 8),
                      _StatusPill(
                        text: product.stock <= product.minStock
                            ? 'Abaixo do mínimo'
                            : 'Estoque adequado',
                        color: product.stock <= product.minStock
                            ? _danger
                            : _teal,
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            const Text(
              'Finanças estimadas',
              style: TextStyle(fontWeight: FontWeight.w900),
            ),
            const SizedBox(height: 8),
            _StockFormula(
              product: product,
              invested: invested,
              potential: potential,
              profit: profit,
            ),
            const Divider(height: 28),
            SegmentedButton<bool>(
              segments: const [
                ButtonSegment(value: false, label: Text('Entrada')),
                ButtonSegment(value: true, label: Text('Saída')),
              ],
              selected: {isExit},
              onSelectionChanged: (value) => onMovementTypeChanged(value.first),
              showSelectedIcon: false,
            ),
            const SizedBox(height: 10),
            TextField(
              key: const Key('stockMovementQuantity'),
              controller: movementQuantity,
              keyboardType: TextInputType.number,
              decoration: InputDecoration(
                labelText: 'Quantidade (${product.unit})',
                prefixIcon: const Icon(Icons.numbers_rounded),
              ),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: movementNote,
              maxLength: 120,
              decoration: const InputDecoration(
                labelText: 'Observação (opcional)',
                hintText: 'Compra do fornecedor, ajuste, devolução...',
              ),
            ),
            const SizedBox(height: 6),
            FilledButton.icon(
              key: const Key('stockRegisterMovement'),
              onPressed: onRegister,
              icon: Icon(
                isExit ? Icons.output_rounded : Icons.keyboard_return_rounded,
              ),
              label: Text(isExit ? 'Registrar saída' : 'Registrar entrada'),
            ),
            const SizedBox(height: 8),
            TextButton(
              onPressed: () {},
              child: Text(
                '${store.stockMovements.where((item) => item.productId == product.id).length} movimentação(ões) no histórico',
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StockFormula extends StatelessWidget {
  const _StockFormula({
    required this.product,
    required this.invested,
    required this.potential,
    required this.profit,
  });

  final Product product;
  final double invested;
  final double potential;
  final double profit;

  @override
  Widget build(BuildContext context) {
    final margin = potential <= 0 ? 0 : (profit / potential) * 100;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 12),
      decoration: BoxDecoration(
        color: const Color(0xFFFFFAF5),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: _line),
      ),
      child: Row(
        children: [
          _FormulaValue(
            value: '${product.stock} ${product.unit}',
            label: 'em estoque',
          ),
          const _FormulaOperator('×'),
          _FormulaValue(value: money(product.cost), label: 'custo unitário'),
          const _FormulaOperator('='),
          _FormulaValue(value: money(invested), label: 'você investiu'),
          const _FormulaOperator('→'),
          _FormulaValue(value: money(potential), label: 'venda potencial'),
          const _FormulaOperator('−'),
          Expanded(
            child: Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: const Color(0xFFFFE9DC),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Column(
                children: [
                  Text(
                    money(profit),
                    style: const TextStyle(
                      color: _blue,
                      fontSize: 18,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  Text(
                    'lucro • ${margin.toStringAsFixed(1).replaceAll('.', ',')}%',
                    style: const TextStyle(color: _textSecondary, fontSize: 8),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _FormulaValue extends StatelessWidget {
  const _FormulaValue({required this.value, required this.label});

  final String value;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(
        children: [
          Text(
            value,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 12),
          ),
          Text(
            label,
            textAlign: TextAlign.center,
            style: const TextStyle(color: _textSecondary, fontSize: 8),
          ),
        ],
      ),
    );
  }
}

class _FormulaOperator extends StatelessWidget {
  const _FormulaOperator(this.value);

  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 3),
      child: Text(
        value,
        style: const TextStyle(
          color: _textSecondary,
          fontSize: 16,
          fontWeight: FontWeight.w900,
        ),
      ),
    );
  }
}

class _ProductPhoto extends StatelessWidget {
  const _ProductPhoto({required this.product, required this.size});

  final Product product;
  final double size;

  @override
  Widget build(BuildContext context) {
    Widget image;
    final source = product.imageData.trim();
    if (source.startsWith('data:image') && source.contains(',')) {
      try {
        image = Image.memory(
          base64Decode(source.substring(source.indexOf(',') + 1)),
          fit: BoxFit.cover,
          errorBuilder: (_, _, _) => _fallback(),
        );
      } catch (_) {
        image = _fallback();
      }
    } else if (source.startsWith('http://') || source.startsWith('https://')) {
      image = Image.network(
        source,
        fit: BoxFit.cover,
        errorBuilder: (_, _, _) => _fallback(),
      );
    } else {
      image = _fallback();
    }
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: const Color(0xFFFFEEE5),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: _line),
      ),
      clipBehavior: Clip.antiAlias,
      child: image,
    );
  }

  Widget _fallback() =>
      const Icon(Icons.inventory_2_outlined, color: _blue, size: 28);
}

class _StockMovementSummary extends StatelessWidget {
  const _StockMovementSummary({
    required this.store,
    required this.selected,
    required this.onSelected,
  });

  final BalcaoStore store;
  final Product? selected;
  final ValueChanged<Product> onSelected;

  @override
  Widget build(BuildContext context) {
    final entries = store.stockMovements
        .where((item) => item.quantity > 0)
        .fold<int>(0, (sum, item) => sum + item.quantity);
    final exits = store.stockMovements
        .where((item) => item.quantity < 0)
        .fold<int>(0, (sum, item) => sum + item.quantity.abs());
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: _line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Resumo das movimentações',
            style: TextStyle(fontSize: 17, fontWeight: FontWeight.w900),
          ),
          const SizedBox(height: 12),
          _ReportLine(
            label: 'Entradas',
            detail: 'unidades registradas',
            value: '+$entries',
            color: _teal,
          ),
          _ReportLine(
            label: 'Saídas',
            detail: 'unidades registradas',
            value: '-$exits',
            color: _danger,
          ),
          _ReportLine(
            label: 'Ajustes',
            detail: 'contagens conferidas',
            value:
                '${store.stockMovements.where((item) => item.type == 'AJUSTE').length}',
            color: _warn,
          ),
        ],
      ),
    );
  }
}

class _StockCountPanel extends StatelessWidget {
  const _StockCountPanel({
    required this.product,
    required this.stock,
    required this.minimum,
    required this.onSave,
  });

  final Product? product;
  final TextEditingController stock;
  final TextEditingController minimum;
  final VoidCallback onSave;

  @override
  Widget build(BuildContext context) {
    final product = this.product;
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: _line),
      ),
      child: product == null
          ? const _Empty(text: 'Selecione um produto para contar.')
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    _ProductPhoto(product: product, size: 64),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        product.name,
                        style: const TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 18),
                TextField(
                  key: const Key('stockCountCurrent'),
                  controller: stock,
                  keyboardType: TextInputType.number,
                  decoration: InputDecoration(
                    labelText: 'Quantidade contada (${product.unit})',
                  ),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: minimum,
                  keyboardType: TextInputType.number,
                  decoration: InputDecoration(
                    labelText: 'Estoque mínimo (${product.unit})',
                  ),
                ),
                const SizedBox(height: 12),
                FilledButton.icon(
                  key: const Key('stockSaveCount'),
                  onPressed: onSave,
                  icon: const Icon(Icons.check_rounded),
                  label: const Text('Salvar contagem'),
                ),
              ],
            ),
    );
  }
}

class _StockProductEditorDialog extends StatefulWidget {
  const _StockProductEditorDialog({required this.store, required this.product});

  final BalcaoStore store;
  final Product? product;

  @override
  State<_StockProductEditorDialog> createState() =>
      _StockProductEditorDialogState();
}

class _StockProductEditorDialogState extends State<_StockProductEditorDialog> {
  late final name = TextEditingController(text: widget.product?.name ?? '');
  late final code = TextEditingController(text: widget.product?.code ?? '');
  late final category = TextEditingController(
    text: widget.product?.category ?? '',
  );
  late final price = TextEditingController(
    text: (widget.product?.price ?? 0).toStringAsFixed(2),
  );
  late final cost = TextEditingController(
    text: (widget.product?.cost ?? 0).toStringAsFixed(2),
  );
  late final stock = TextEditingController(
    text: '${widget.product?.stock ?? 0}',
  );
  late final minimum = TextEditingController(
    text: '${widget.product?.minStock ?? 0}',
  );
  late final unit = TextEditingController(text: widget.product?.unit ?? 'un');
  String imageData = '';
  bool saving = false;

  @override
  void initState() {
    super.initState();
    imageData = widget.product?.imageData ?? '';
  }

  @override
  void dispose() {
    name.dispose();
    code.dispose();
    category.dispose();
    price.dispose();
    cost.dispose();
    stock.dispose();
    minimum.dispose();
    unit.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final preview = Product(
      id: 'preview',
      code: code.text,
      name: name.text,
      category: category.text,
      price: _parse(price.text),
      cost: _parse(cost.text),
      stock: int.tryParse(stock.text) ?? 0,
      minStock: int.tryParse(minimum.text) ?? 0,
      unit: unit.text,
      imageData: imageData,
    );
    return Dialog(
      insetPadding: const EdgeInsets.all(18),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 760, maxHeight: 780),
        child: Column(
          children: [
            Container(
              height: 66,
              padding: const EdgeInsets.symmetric(horizontal: 18),
              color: _rail,
              child: Row(
                children: [
                  const Icon(Icons.inventory_2_outlined, color: _blue),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      widget.product == null
                          ? 'Criar produto'
                          : 'Editar produto',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 18,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  IconButton(
                    onPressed: saving
                        ? null
                        : () => Navigator.of(context).pop(false),
                    icon: const Icon(Icons.close_rounded),
                    color: Colors.white,
                  ),
                ],
              ),
            ),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.all(18),
                children: [
                  Row(
                    children: [
                      _ProductPhoto(product: preview, size: 108),
                      const SizedBox(width: 12),
                      Expanded(
                        child: OutlinedButton.icon(
                          onPressed: _pickPhoto,
                          icon: const Icon(Icons.photo_camera_outlined),
                          label: const Text('Escolher foto do produto'),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 14),
                  TextField(
                    key: const Key('stockProductName'),
                    controller: name,
                    onChanged: (_) => setState(() {}),
                    decoration: const InputDecoration(labelText: 'Produto'),
                  ),
                  const SizedBox(height: 9),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: code,
                          onChanged: (_) => setState(() {}),
                          decoration: const InputDecoration(
                            labelText: 'Código',
                          ),
                        ),
                      ),
                      const SizedBox(width: 9),
                      Expanded(
                        child: TextField(
                          controller: category,
                          onChanged: (_) => setState(() {}),
                          decoration: const InputDecoration(
                            labelText: 'Categoria',
                          ),
                        ),
                      ),
                      const SizedBox(width: 9),
                      SizedBox(
                        width: 110,
                        child: TextField(
                          controller: unit,
                          onChanged: (_) => setState(() {}),
                          decoration: const InputDecoration(
                            labelText: 'Unidade',
                            hintText: 'un, kg, L',
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 9),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: cost,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Custo unitário',
                            prefixText: 'R\$ ',
                          ),
                        ),
                      ),
                      const SizedBox(width: 9),
                      Expanded(
                        child: TextField(
                          controller: price,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Preço de venda',
                            prefixText: 'R\$ ',
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 9),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: stock,
                          keyboardType: TextInputType.number,
                          enabled: widget.product == null,
                          decoration: const InputDecoration(
                            labelText: 'Estoque inicial',
                          ),
                        ),
                      ),
                      const SizedBox(width: 9),
                      Expanded(
                        child: TextField(
                          controller: minimum,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Estoque mínimo',
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            Container(
              padding: const EdgeInsets.all(14),
              decoration: const BoxDecoration(
                border: Border(top: BorderSide(color: _line)),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  OutlinedButton(
                    onPressed: saving
                        ? null
                        : () => Navigator.of(context).pop(false),
                    child: const Text('Cancelar'),
                  ),
                  const SizedBox(width: 8),
                  FilledButton.icon(
                    key: const Key('stockProductSave'),
                    onPressed: saving ? null : _save,
                    icon: const Icon(Icons.save_outlined),
                    label: const Text('Salvar produto'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _pickPhoto() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.image,
      withData: true,
    );
    final file = result?.files.firstOrNull;
    final bytes = file?.bytes;
    if (bytes == null || bytes.isEmpty) return;
    final extension = (file?.extension ?? 'png').toLowerCase();
    final mime = extension == 'jpg' || extension == 'jpeg'
        ? 'image/jpeg'
        : extension == 'webp'
        ? 'image/webp'
        : 'image/png';
    setState(() => imageData = 'data:$mime;base64,${base64Encode(bytes)}');
  }

  Future<void> _save() async {
    if (name.text.trim().isEmpty || code.text.trim().isEmpty) return;
    setState(() => saving = true);
    final product = widget.product;
    if (product == null) {
      await widget.store.saveProduct(
        name: name.text.trim(),
        code: code.text.trim(),
        category: category.text.trim().isEmpty
            ? 'SEM CATEGORIA'
            : category.text.trim(),
        price: _parse(price.text),
        cost: _parse(cost.text),
        stock: int.tryParse(stock.text.trim()) ?? 0,
        minStock: int.tryParse(minimum.text.trim()) ?? 0,
        unit: unit.text.trim().isEmpty ? 'un' : unit.text.trim(),
        imageData: imageData,
      );
    } else {
      await widget.store.updateProduct(
        product: product,
        name: name.text,
        code: code.text,
        category: category.text,
        price: _parse(price.text),
        cost: _parse(cost.text),
        minStock: int.tryParse(minimum.text.trim()) ?? product.minStock,
        unit: unit.text,
        imageData: imageData,
      );
    }
    if (!mounted) return;
    Navigator.of(context).pop(true);
  }
}

class _KeyboardFooter extends StatelessWidget {
  const _KeyboardFooter({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    if (MediaQuery.sizeOf(context).width < 620) {
      return const SizedBox.shrink();
    }
    return Container(
      width: double.infinity,
      height: 34,
      padding: const EdgeInsets.symmetric(horizontal: 10),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: _line)),
      ),
      child: Row(
        children: [
          Expanded(
            child: Text(
              !store.loggedIn
                  ? 'Conta Supabase aguardando login'
                  : store.selectedOrder == null
                  ? 'Pronto'
                  : 'Selecionado ${store.selectedOrder!.number} | ${store.printBridgeLabel}',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: _navy,
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          const SizedBox(width: 12),
          const Flexible(
            child: Text(
              'Tab troca area: Comanda > Mesas/Fichas > Venda rapida  |  Enter inclui  |  F3 catalogo  |  Excluir na linha do item  |  F10 abrir/fechar caixa',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              textAlign: TextAlign.right,
              style: TextStyle(
                color: _textSecondary,
                fontSize: 12,
                fontWeight: FontWeight.w400,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ignore: unused_element
class _FloatingHub extends StatelessWidget {
  const _FloatingHub({required this.store, required this.openModule});

  final BalcaoStore store;
  final void Function(String title, Widget child) openModule;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () => openModule(
          'Central rapida',
          _QuickHubModule(store: store, openModule: openModule),
        ),
        borderRadius: BorderRadius.circular(9),
        child: Container(
          width: 154,
          padding: const EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: _railDeep,
            borderRadius: BorderRadius.circular(9),
            border: Border.all(color: _blue2),
            boxShadow: const [
              BoxShadow(
                color: Color(0x33000000),
                blurRadius: 18,
                offset: Offset(0, 8),
              ),
            ],
          ),
          child: Row(
            children: [
              const _LogoBlock(size: 34),
              const SizedBox(width: 8),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Text(
                      'WA  Suporte  iFood',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.w900,
                        fontSize: 10.5,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        _TinyStatus(
                          text: store.whatsappConnected ? 'WA on' : 'QR',
                          color: store.whatsappConnected ? _teal : _warn,
                        ),
                        const SizedBox(width: 4),
                        const _TinyStatus(text: '24h', color: _teal),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _TinyStatus extends StatelessWidget {
  const _TinyStatus({required this.text, required this.color});

  final String text;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .18),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        text,
        style: TextStyle(
          color: color == _warn
              ? const Color(0xFFFFD38B)
              : const Color(0xFFDFFBFA),
          fontSize: 9,
          fontWeight: FontWeight.w900,
        ),
      ),
    );
  }
}

class _QuickHubModule extends StatelessWidget {
  const _QuickHubModule({required this.store, required this.openModule});

  final BalcaoStore store;
  final void Function(String title, Widget child) openModule;

  @override
  Widget build(BuildContext context) {
    return _ModuleScroll(
      children: [
        _WindowPanel(
          title: 'Atalhos conectados',
          child: Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _HubButton(
                key: const Key('whatsappHub'),
                icon: Icons.qr_code_2_rounded,
                title: store.whatsappConnected
                    ? 'WhatsApp ligado'
                    : 'Logar WhatsApp',
                subtitle: store.whatsappConnected
                    ? store.whatsappNumber
                    : 'Abrir QR da loja',
                color: _teal,
                onTap: () => openModule(
                  'WhatsApp Online',
                  _WhatsAppModule(store: store),
                ),
              ),
              _HubButton(
                icon: Icons.support_agent_rounded,
                title: 'Suporte',
                subtitle: '24 horas',
                color: _navy2,
                onTap: store.flushSync,
              ),
              _HubButton(
                key: const Key('ifoodHub'),
                icon: Icons.restaurant_rounded,
                title: store.ifoodConnected ? 'iFood ligado' : 'iFood',
                subtitle: store.ifoodConnected
                    ? store.ifoodMerchantName
                    : 'Conectar pedidos',
                color: Colors.red,
                onTap: () =>
                    openModule('iFood Online', _DeliveryModule(store: store)),
              ),
              _HubButton(
                icon: Icons.contactless_rounded,
                title: 'Mercado Pago',
                subtitle: store.pointStatusLabel,
                color: _blue,
                onTap: () => openModule(
                  'Mercado Pago Point',
                  _MercadoPagoPointModule(store: store),
                ),
              ),
              _HubButton(
                key: const Key('backupHub'),
                icon: Icons.backup_outlined,
                title: 'Backup',
                subtitle: store.lastBackupAt.isEmpty
                    ? 'Gerar ou restaurar'
                    : 'Ultimo ${store.lastBackupAt}',
                color: _navy2,
                onTap: () => openModule(
                  'Backup e exportacao',
                  _BackupModule(store: store),
                ),
              ),
              _HubButton(
                icon: Icons.cloud_sync_rounded,
                title: 'Sincronizar',
                subtitle: '${store.pendingSyncCount} pendente(s)',
                color: _warn,
                onTap: store.flushSync,
              ),
            ],
          ),
        ),
        _WindowPanel(
          title: 'Status da loja',
          child: Column(
            children: [
              _ReportLine(
                label: 'WhatsApp',
                detail: store.whatsappConnected
                    ? 'conectado em ${store.whatsappNumber}'
                    : 'aguardando leitura do QR',
                value: store.whatsappConnected ? 'ON' : 'QR',
                color: store.whatsappConnected ? _teal : _warn,
              ),
              _ReportLine(
                label: 'Loja online',
                detail: store.cashOpen
                    ? 'cardapio e iFood recebendo pedidos'
                    : 'loja fechada no atendimento',
                value: store.cashOpen ? 'ON' : 'OFF',
                color: store.cashOpen ? _teal : _danger,
              ),
              _ReportLine(
                label: 'Sincronizacao',
                detail: store.lastSync.isEmpty
                    ? 'sem sync recente'
                    : store.lastSync,
                value: '${store.pendingSyncCount}',
                color: _navy2,
              ),
              _ReportLine(
                label: 'Mercado Pago',
                detail: '${store.pointDeviceName} | ${store.pointStatusLabel}',
                value: money(store.mercadoPagoSales),
                color: _blue,
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _HubButton extends StatelessWidget {
  const _HubButton({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.color,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 184,
      child: Material(
        color: _surfaceMuted,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(7),
          side: const BorderSide(color: _line),
        ),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(7),
          child: Padding(
            padding: const EdgeInsets.all(10),
            child: Row(
              children: [
                Container(
                  width: 38,
                  height: 38,
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: .13),
                    borderRadius: BorderRadius.circular(7),
                  ),
                  child: Icon(icon, color: color, size: 20),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: _navy,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      Text(
                        subtitle,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: _textSecondary,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _WhatsAppModule extends StatefulWidget {
  const _WhatsAppModule({required this.store});

  final BalcaoStore store;

  @override
  State<_WhatsAppModule> createState() => _WhatsAppModuleState();
}

class _WhatsAppModuleState extends State<_WhatsAppModule> {
  late final TextEditingController number = TextEditingController(
    text: widget.store.whatsappNumber,
  );

  Future<void> _openOnboarding() async {
    final uri = Uri.tryParse(widget.store.whatsappOnboardingUrl);
    if (uri == null || !uri.hasScheme) return;
    final opened = await launchUrl(uri, mode: LaunchMode.externalApplication);
    if (!opened && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Nao consegui abrir a conexao agora.')),
      );
    }
  }

  @override
  void dispose() {
    number.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final store = widget.store;
    return AnimatedBuilder(
      animation: store,
      builder: (context, _) => LayoutBuilder(
        builder: (context, constraints) {
          final desktop = constraints.maxWidth >= 720;
          final qrPanel = _WindowPanel(
            title: store.whatsappOnboardingUrl.isEmpty
                ? 'Conexao oficial'
                : 'Escaneie ou abra o QR',
            action: _StatusPill(
              text: store.whatsappBusy
                  ? 'consultando'
                  : store.whatsappConnected
                  ? 'conectado'
                  : store.whatsappConnectionStatus == 'ERROR'
                  ? 'erro'
                  : 'aguardando',
              color: store.whatsappConnected
                  ? _teal
                  : store.whatsappConnectionStatus == 'ERROR'
                  ? _danger
                  : _warn,
            ),
            child: Column(
              children: [
                _WhatsAppQrBox(store: store, size: 246),
                const SizedBox(height: 10),
                Text(
                  store.businessName,
                  textAlign: TextAlign.center,
                  softWrap: true,
                  style: const TextStyle(
                    color: _navy,
                    fontWeight: FontWeight.w900,
                    fontSize: 18,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  store.whatsappConnected
                      ? 'Numero validado pelo gateway da loja'
                      : store.whatsappOnboardingUrl.isEmpty
                      ? 'Informe o numero e gere a conexao'
                      : 'QR seguro gerado pelo gateway Supabase',
                  textAlign: TextAlign.center,
                  softWrap: true,
                  style: const TextStyle(
                    color: _textSecondary,
                    fontWeight: FontWeight.w700,
                    fontSize: 12,
                  ),
                ),
              ],
            ),
          );
          final statusPanel = _WindowPanel(
            title: 'WhatsApp Online',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (!desktop) ...[
                  Center(child: _WhatsAppQrBox(store: store, size: 214)),
                  const SizedBox(height: 10),
                ],
                _ReportLine(
                  label: 'Conta da loja',
                  detail: store.businessName,
                  value: store.whatsappConnected ? 'ON' : 'QR',
                  color: store.whatsappConnected ? _teal : _warn,
                ),
                _ReportLine(
                  label: 'Recebimento',
                  detail: 'pedidos do cardapio entram em tempo real',
                  value: store.whatsappConnected ? 'ativo' : 'pausado',
                  color: store.whatsappConnected ? _teal : _warn,
                ),
                _ReportLine(
                  label: 'Gateway',
                  detail: store.whatsappMessage,
                  value: store.whatsappConnectionStatus,
                  color: store.whatsappConnectionStatus == 'ERROR'
                      ? _danger
                      : _navy2,
                ),
                const SizedBox(height: 8),
                _DeskInput(
                  key: const Key('whatsappStorePhone'),
                  label: 'Numero da loja com DDD',
                  controller: number,
                  keyboardType: TextInputType.phone,
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    Expanded(
                      child: _DeskCommandButton(
                        key: const Key('whatsappConnect'),
                        label: store.whatsappBusy
                            ? 'Conectando...'
                            : 'Conectar numero',
                        color: _teal,
                        onTap: () => store.connectWhatsApp(number.text),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: _DeskCommandButton(
                        key: const Key('whatsappRefresh'),
                        label: 'Atualizar status / QR',
                        color: _navy2,
                        onTap: store.refreshWhatsAppQr,
                      ),
                    ),
                  ],
                ),
                if (store.whatsappOnboardingUrl.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  SizedBox(
                    width: double.infinity,
                    child: _DeskCommandButton(
                      key: const Key('whatsappOpenOnboarding'),
                      label: 'Abrir conexao oficial',
                      color: _warn,
                      onTap: _openOnboarding,
                    ),
                  ),
                ],
                const SizedBox(height: 8),
                SizedBox(
                  width: double.infinity,
                  child: _DeskCommandButton(
                    key: const Key('whatsappDisconnect'),
                    label: 'Desconectar WhatsApp',
                    color: _danger,
                    onTap: store.disconnectWhatsApp,
                  ),
                ),
              ],
            ),
          );
          final queuePanel = _WindowPanel(
            title: 'Fila em tempo real',
            child: Column(
              children: [
                _ReportLine(
                  label: 'Mensagem inicial',
                  detail: 'sem repetir para cada mensagem recebida',
                  value: '1x',
                  color: _teal,
                ),
                _ReportLine(
                  label: 'Cliente pediu',
                  detail: 'notifica o PDV quando a loja aceita',
                  value: 'push',
                  color: _navy2,
                ),
                _ReportLine(
                  label: 'Pedidos cardapio',
                  detail: 'vinculados a ${store.businessName}',
                  value: '${store.openOrders.length}',
                  color: _warn,
                ),
              ],
            ),
          );
          if (desktop) {
            return Padding(
              padding: const EdgeInsets.all(12),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SizedBox(width: 340, child: qrPanel),
                  const SizedBox(width: 10),
                  Expanded(
                    child: SingleChildScrollView(
                      child: Column(
                        children: [
                          statusPanel,
                          const SizedBox(height: 10),
                          queuePanel,
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            );
          }
          return _ModuleScroll(children: [qrPanel, statusPanel, queuePanel]);
        },
      ),
    );
  }
}

class _WhatsAppQrBox extends StatelessWidget {
  const _WhatsAppQrBox({required this.store, required this.size});

  final BalcaoStore store;
  final double size;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(7),
        border: Border.all(color: _line),
      ),
      child: store.whatsappQrPayload.isNotEmpty
          ? QrImageView(
              data: store.whatsappQrPayload,
              version: QrVersions.auto,
              backgroundColor: Colors.white,
              eyeStyle: const QrEyeStyle(
                eyeShape: QrEyeShape.square,
                color: _navy,
              ),
              dataModuleStyle: const QrDataModuleStyle(
                dataModuleShape: QrDataModuleShape.square,
                color: _navy,
              ),
            )
          : Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(
                  store.whatsappConnected
                      ? Icons.verified_rounded
                      : Icons.qr_code_2_rounded,
                  color: store.whatsappConnected ? _teal : _navy2,
                  size: size * 0.36,
                ),
                const SizedBox(height: 12),
                Text(
                  store.whatsappConnected
                      ? 'WhatsApp conectado'
                      : 'QR ainda nao gerado',
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: _navy,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ],
            ),
    );
  }
}

class _BackupModule extends StatefulWidget {
  const _BackupModule({required this.store});

  final BalcaoStore store;

  @override
  State<_BackupModule> createState() => _BackupModuleState();
}

class _BackupModuleState extends State<_BackupModule> {
  final operator = TextEditingController(text: '2');
  final pin = TextEditingController();
  late bool cloudBackup = widget.store.cloudBackupEnabled;
  late bool centralSync = widget.store.centralSyncEnabled;
  String message = '';

  @override
  void dispose() {
    operator.dispose();
    pin.dispose();
    super.dispose();
  }

  Future<void> _saveSettings() async {
    await widget.store.updateBackupSettings(
      cloudBackup: cloudBackup,
      centralSync: centralSync,
    );
    if (!mounted) return;
    setState(() => message = widget.store.backupMessage);
  }

  Future<void> _exportBackup() async {
    final data = await widget.store.createBackupJson(
      operator: operator.text,
      pin: pin.text,
    );
    if (data == null) {
      if (mounted) setState(() => message = widget.store.backupMessage);
      return;
    }
    final now = DateTime.now();
    final stamp =
        '${now.year}${now.month.toString().padLeft(2, '0')}${now.day.toString().padLeft(2, '0')}-${now.hour.toString().padLeft(2, '0')}${now.minute.toString().padLeft(2, '0')}';
    await FilePicker.platform.saveFile(
      dialogTitle: 'Salvar backup do Balcao Livre',
      fileName: 'balcao-livre-backup-$stamp.json',
      type: FileType.custom,
      allowedExtensions: const ['json'],
      bytes: Uint8List.fromList(utf8.encode(data)),
    );
    if (!mounted) return;
    setState(() {
      message = '${widget.store.backupMessage} Download iniciado.';
      pin.clear();
    });
  }

  Future<void> _restoreBackup() async {
    final picked = await FilePicker.platform.pickFiles(
      dialogTitle: 'Restaurar backup do Balcao Livre',
      type: FileType.custom,
      allowedExtensions: const ['json'],
      withData: true,
    );
    final bytes = picked?.files.firstOrNull?.bytes;
    if (bytes == null || bytes.isEmpty) return;
    final restored = await widget.store.restoreBackupJson(
      backupJson: utf8.decode(bytes),
      operator: operator.text,
      pin: pin.text,
    );
    if (!mounted) return;
    setState(() {
      message = widget.store.backupMessage;
      if (restored) pin.clear();
    });
  }

  Future<void> _exportCsv() async {
    final authorized = await widget.store.authenticateTeamMember(
      operator: operator.text,
      pin: pin.text,
      permission: StaffPermission.backup,
    );
    if (authorized == null) {
      if (mounted) setState(() => message = widget.store.securityMessage);
      return;
    }
    await FilePicker.platform.saveFile(
      dialogTitle: 'Exportar produtos',
      fileName: 'produtos-balcao-livre.csv',
      type: FileType.custom,
      allowedExtensions: const ['csv'],
      bytes: Uint8List.fromList(utf8.encode(widget.store.productsCsv())),
    );
    if (!mounted) return;
    setState(() {
      message = 'Resumo CSV exportado por ${authorized.name}.';
      pin.clear();
    });
  }

  @override
  Widget build(BuildContext context) {
    final store = widget.store;
    return _ModuleScroll(
      children: [
        _WindowPanel(
          title: 'Protecao dos dados',
          child: Column(
            children: [
              _ReportLine(
                label: 'Backup completo',
                detail: store.lastBackupAt.isEmpty
                    ? 'ainda nao gerado nesta instalacao'
                    : 'ultimo em ${store.lastBackupAt}',
                value: store.cloudBackupEnabled ? 'ON' : 'OFF',
                color: store.cloudBackupEnabled ? _teal : _warn,
              ),
              _ReportLine(
                label: 'Sync central',
                detail: store.syncStatus,
                value: store.centralSyncEnabled ? 'ON' : 'OFF',
                color: store.centralSyncEnabled ? _teal : _warn,
              ),
            ],
          ),
        ),
        _WindowPanel(
          title: 'Automacao',
          child: Column(
            children: [
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('Backup completo versionado'),
                subtitle: const Text(
                  'Inclui operacao, produtos, clientes, equipe e configuracoes.',
                ),
                value: cloudBackup,
                onChanged: (value) => setState(() => cloudBackup = value),
              ),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('Sync central economico'),
                subtitle: const Text(
                  'Mantem o resumo operacional pronto para web e mobile.',
                ),
                value: centralSync,
                onChanged: (value) => setState(() => centralSync = value),
              ),
              SizedBox(
                width: double.infinity,
                child: _DeskCommandButton(
                  key: const Key('backupSaveSettings'),
                  label: 'Salvar automacao',
                  color: _navy2,
                  onTap: _saveSettings,
                ),
              ),
            ],
          ),
        ),
        _WindowPanel(
          title: 'Autorizacao do gerente',
          child: Column(
            children: [
              Row(
                children: [
                  Expanded(
                    child: _DeskInput(
                      key: const Key('backupOperator'),
                      label: 'Operador',
                      controller: operator,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _DeskInput(
                      key: const Key('backupPin'),
                      label: 'Senha',
                      controller: pin,
                      keyboardType: TextInputType.number,
                      obscureText: true,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  _DeskCommandButton(
                    key: const Key('backupExport'),
                    label: 'Gerar backup agora',
                    color: _navy2,
                    onTap: _exportBackup,
                  ),
                  _DeskCommandButton(
                    key: const Key('backupRestore'),
                    label: 'Restaurar arquivo',
                    color: _warn,
                    onTap: _restoreBackup,
                  ),
                  _DeskCommandButton(
                    key: const Key('backupCsv'),
                    label: 'Exportar resumo CSV',
                    color: _teal,
                    onTap: _exportCsv,
                  ),
                ],
              ),
              if (message.isNotEmpty) ...[
                const SizedBox(height: 10),
                _InfoStrip(
                  icon: Icons.info_outline_rounded,
                  title: 'Resultado',
                  text: message,
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _MercadoPagoPointModule extends StatelessWidget {
  const _MercadoPagoPointModule({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: store,
      builder: (context, _) {
        final order = store.selectedOrder;
        final canCharge =
            store.pointReady && order != null && order.items.isNotEmpty;
        final transactions = store.movements
            .where((movement) => store.isMercadoPagoMethod(movement.note))
            .take(8)
            .toList();

        final terminalPanel = _WindowPanel(
          title: 'Mercado Pago Point',
          action: _StatusPill(
            text: store.pointReady
                ? 'pronta'
                : store.pointConnected
                ? 'sem Point'
                : 'desconectada',
            color: store.pointReady
                ? _teal
                : store.pointConnected
                ? _warn
                : _danger,
          ),
          child: Column(
            children: [
              _PointTerminalMini(store: store, large: true),
              const SizedBox(height: 12),
              _ReportLine(
                label: 'Conta Mercado Pago',
                detail: store.pointSellerUserId.isEmpty
                    ? 'conta nao conectada'
                    : 'seller ${store.pointSellerUserId}',
                value: store.pointConnected ? 'ON' : 'OFF',
                color: store.pointConnected ? _teal : _danger,
              ),
              _ReportLine(
                label: 'Point selecionada',
                detail: store.pointTerminalDisplay,
                value: store.pointHasTerminal ? 'PDV' : '-',
                color: store.pointHasTerminal ? _blue : _warn,
              ),
              _ReportLine(
                label: 'Status',
                detail: store.pointStatusLabel,
                value: store.pointHasPending
                    ? money(store.pointPendingAmount)
                    : '-',
                color: store.pointHasPending
                    ? _warn
                    : store.pointReady
                    ? _teal
                    : _danger,
              ),
              if (store.pointConnectUrl.isNotEmpty)
                _AccessLinkBox(
                  label: 'Link de conexao Mercado Pago',
                  value: store.pointConnectUrl,
                  status: 'abrir',
                  color: _blue,
                ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: _DeskCommandButton(
                      label: 'Atualizar status',
                      color: _navy2,
                      onTap: () =>
                          store.refreshMercadoPagoStatus(loadTerminals: true),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _DeskCommandButton(
                      label: store.pointConnected
                          ? 'Carregar Points'
                          : 'Conectar MP',
                      color: _blue,
                      onTap: store.pointConnected
                          ? store.loadMercadoPagoTerminals
                          : store.startMercadoPagoConnect,
                    ),
                  ),
                ],
              ),
            ],
          ),
        );

        final terminalsPanel = _WindowPanel(
          title: 'Maquininhas da conta',
          action: _StatusPill(
            text: '${store.pointTerminals.length}',
            color: _blue,
          ),
          child: Column(
            children: [
              if (!store.pointConnected)
                const _Empty(
                  text: 'Conecte a conta Mercado Pago para listar Points.',
                ),
              if (store.pointConnected && store.pointTerminals.isEmpty)
                const _Empty(text: 'Nenhuma Point carregada ainda.'),
              ...store.pointTerminals.map(
                (terminal) => _PointTerminalRow(
                  terminal: terminal,
                  selected: terminal.id == store.pointTerminalId,
                  onTap: () => store.selectMercadoPagoTerminal(terminal),
                ),
              ),
            ],
          ),
        );

        final chargePanel = _WindowPanel(
          title: 'Cobrar comanda na maquininha',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _ReportLine(
                label: order == null ? 'Nenhuma comanda' : order.number,
                detail: order == null
                    ? 'abra ou selecione uma comanda'
                    : '${order.customerName} | ${order.itemsCount} item(ns)',
                value: order == null ? '-' : money(order.subtotal),
                color: _navy2,
              ),
              const SizedBox(height: 8),
              if (!store.pointReady)
                const _InfoStrip(
                  icon: Icons.lock_rounded,
                  title: 'Cobranca bloqueada',
                  text:
                      'O app so envia para a Point quando Mercado Pago estiver conectado e uma maquininha estiver selecionada.',
                ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  _CompactPointButton(
                    label: 'Enviar Pix',
                    icon: Icons.qr_code_rounded,
                    enabled: canCharge,
                    onTap: () => store.sendSelectedToPoint('Pix Mercado Pago'),
                  ),
                  _CompactPointButton(
                    label: 'Enviar debito',
                    icon: Icons.credit_card_rounded,
                    enabled: canCharge,
                    onTap: () => store.sendSelectedToPoint('Debito Point'),
                  ),
                  _CompactPointButton(
                    label: 'Enviar credito',
                    icon: Icons.contactless_rounded,
                    enabled: canCharge,
                    onTap: () => store.sendSelectedToPoint('Credito Point'),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: _DeskCommandButton(
                      label: 'Verificar pagamento',
                      color: _teal,
                      onTap: store.confirmPointPayment,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _DeskCommandButton(
                      label: 'Cancelar no PDV',
                      color: _danger,
                      onTap: store.cancelPointCharge,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              const _InfoStrip(
                icon: Icons.verified_rounded,
                title: 'Fluxo real',
                text:
                    'A comanda so fecha quando o pagamento voltar aprovado pelo Mercado Pago.',
              ),
            ],
          ),
        );

        final historyPanel = _WindowPanel(
          title: 'Transacoes Mercado Pago',
          action: _StatusPill(
            text: money(store.mercadoPagoSales),
            color: _blue,
          ),
          child: Column(
            children: [
              if (transactions.isEmpty)
                const _Empty(text: 'Nenhuma transacao Mercado Pago ainda.'),
              ...transactions.map(
                (movement) => _ReportLine(
                  label: movement.type,
                  detail: movement.note,
                  value: money(movement.amount),
                  color: _blue,
                ),
              ),
            ],
          ),
        );

        return LayoutBuilder(
          builder: (context, constraints) {
            final desktop = constraints.maxWidth >= 820;
            if (!desktop) {
              return _ModuleScroll(
                children: [
                  terminalPanel,
                  terminalsPanel,
                  chargePanel,
                  historyPanel,
                ],
              );
            }
            return Padding(
              padding: const EdgeInsets.all(12),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SizedBox(
                    width: 360,
                    child: SingleChildScrollView(
                      child: Column(
                        children: [
                          terminalPanel,
                          const SizedBox(height: 10),
                          terminalsPanel,
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: SingleChildScrollView(
                      child: Column(
                        children: [
                          chargePanel,
                          const SizedBox(height: 10),
                          historyPanel,
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }
}

class _PointTerminalRow extends StatelessWidget {
  const _PointTerminalRow({
    required this.terminal,
    required this.selected,
    required this.onTap,
  });

  final MercadoPagoTerminal terminal;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(7),
      child: Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: selected ? _mint : _surfaceMuted,
          borderRadius: BorderRadius.circular(7),
          border: Border.all(color: selected ? _teal : _line),
        ),
        child: Row(
          children: [
            Icon(
              selected
                  ? Icons.radio_button_checked_rounded
                  : Icons.radio_button_unchecked_rounded,
              color: selected ? _teal : _textSecondary,
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    terminal.display,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: _navy,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  Text(
                    '${terminal.id} | loja ${terminal.storeId.isEmpty ? '-' : terminal.storeId}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: _textSecondary,
                      fontSize: 12,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
            ),
            _StatusPill(
              text: terminal.operatingMode.isEmpty
                  ? 'POINT'
                  : terminal.operatingMode.toUpperCase(),
              color: terminal.pdvMode ? _teal : _warn,
            ),
          ],
        ),
      ),
    );
  }
}

class _ModuleScroll extends StatelessWidget {
  const _ModuleScroll({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final wide = constraints.maxWidth >= 700;
        return ListView(
          padding: EdgeInsets.all(wide ? 16 : 10),
          children: [
            ...children.expand((child) sync* {
              yield child;
              yield SizedBox(height: wide ? 12 : 10);
            }),
          ],
        );
      },
    );
  }
}

class _DiscountDeskModule extends StatefulWidget {
  const _DiscountDeskModule({required this.store});

  final BalcaoStore store;

  @override
  State<_DiscountDeskModule> createState() => _DiscountDeskModuleState();
}

class _DiscountDeskModuleState extends State<_DiscountDeskModule> {
  final amount = TextEditingController(text: '5,00');
  final reason = TextEditingController(text: 'Desconto gerente');
  final operator = TextEditingController(text: '2');
  final pin = TextEditingController();
  String message = '';

  @override
  void dispose() {
    amount.dispose();
    reason.dispose();
    operator.dispose();
    pin.dispose();
    super.dispose();
  }

  Future<void> _apply() async {
    final applied = await widget.store.applyDiscount(
      amount: amount.text,
      reason: reason.text,
      operator: operator.text,
      pin: pin.text,
    );
    if (!mounted) return;
    setState(() {
      message = widget.store.securityMessage;
      if (applied) pin.clear();
    });
  }

  @override
  Widget build(BuildContext context) {
    final store = widget.store;
    final order = store.selectedOrder;
    if (order == null) {
      return const _ModuleScroll(
        children: [
          _WindowPanel(
            title: 'Desconto e permissao',
            child: _Empty(text: 'Nenhuma comanda selecionada.'),
          ),
        ],
      );
    }

    return _ModuleScroll(
      children: [
        _WindowPanel(
          title: 'Comanda selecionada',
          child: Row(
            children: [
              Expanded(
                child: _MiniSummary(
                  label: order.number,
                  value: money(order.subtotal),
                  sub: statusLabel(order.status),
                  color: _navy2,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _MiniSummary(
                  label: 'Couvert',
                  value: money(order.coverCharge),
                  sub: 'taxa fixa',
                  color: _teal,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _MiniSummary(
                  label: 'Garcom',
                  value: '${order.servicePercent.toStringAsFixed(0)}%',
                  sub: money(order.serviceAmount),
                  color: _warn,
                ),
              ),
            ],
          ),
        ),
        _WindowPanel(
          title: 'Desconto autorizado',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: _DeskInput(
                      key: const Key('discountAmount'),
                      label: 'Valor',
                      controller: amount,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    flex: 2,
                    child: _DeskInput(
                      key: const Key('discountReason'),
                      label: 'Motivo',
                      controller: reason,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: _DeskInput(
                      key: const Key('discountOperator'),
                      label: 'Operador autorizado',
                      controller: operator,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _DeskInput(
                      key: const Key('discountPin'),
                      label: 'Senha',
                      controller: pin,
                      keyboardType: TextInputType.number,
                      obscureText: true,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              if (message.isNotEmpty)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Text(
                    message,
                    style: TextStyle(
                      color: message.toLowerCase().contains('autorizado')
                          ? _teal
                          : _danger,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
              SizedBox(
                width: double.infinity,
                child: _DeskCommandButton(
                  key: const Key('discountApply'),
                  label: 'Autorizar desconto',
                  color: _danger,
                  onTap: _apply,
                ),
              ),
            ],
          ),
        ),
        _WindowPanel(
          title: 'Taxas da comanda',
          child: Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              FilledButton(
                onPressed: () => unawaited(
                  store.updateSelectedOrderCharges(servicePercent: '0'),
                ),
                child: const Text('Sem garcom'),
              ),
              FilledButton(
                onPressed: () => unawaited(
                  store.updateSelectedOrderCharges(servicePercent: '10'),
                ),
                child: const Text('Garcom 10%'),
              ),
              FilledButton(
                onPressed: () => unawaited(
                  store.updateSelectedOrderCharges(coverCharge: '0'),
                ),
                child: const Text('Zerar couvert'),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

// ignore: unused_element
class _ReopenSalesModule extends StatelessWidget {
  const _ReopenSalesModule({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    final closed = store.closedOrders;
    return _ModuleScroll(
      children: [
        _WindowPanel(
          title: 'Operacao agora',
          child: Row(
            children: [
              Expanded(
                child: _MiniSummary(
                  label: 'Em aberto',
                  value: money(store.openTotal),
                  sub: '${store.openOrders.length} comandas',
                  color: _navy2,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _MiniSummary(
                  label: 'Fechadas',
                  value: '${closed.length}',
                  sub: 'vendas no caixa',
                  color: _teal,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _MiniSummary(
                  label: 'iFood',
                  value: money(store.ifoodRepasse),
                  sub: 'repasse previsto',
                  color: Colors.red,
                ),
              ),
            ],
          ),
        ),
        _WindowPanel(
          title: 'Comandas abertas',
          action: TextButton(
            onPressed: () => store.openOrder(OrderKind.table),
            child: const Text('Nova mesa'),
          ),
          child: Column(
            children: store.openOrders
                .map(
                  (order) => _SalesDeskLine(
                    order: order,
                    action: 'Selecionar',
                    onTap: () => store.selectOrder(order.id),
                    secondary: 'Cancelar',
                    onSecondary: () => store.cancelOrder(order),
                  ),
                )
                .toList(),
          ),
        ),
        _WindowPanel(
          title: 'Vendas fechadas / reabrir',
          child: Column(
            children: closed.isEmpty
                ? [const _Empty(text: 'Nenhuma venda fechada ainda.')]
                : closed
                      .map(
                        (order) => _SalesDeskLine(
                          order: order,
                          action: 'Reabrir',
                          onTap: () => store.reopenOrder(order),
                        ),
                      )
                      .toList(),
          ),
        ),
      ],
    );
  }
}

class _SalesDeskLine extends StatelessWidget {
  const _SalesDeskLine({
    required this.order,
    required this.action,
    required this.onTap,
    this.secondary,
    this.onSecondary,
  });

  final Order order;
  final String action;
  final VoidCallback onTap;
  final String? secondary;
  final VoidCallback? onSecondary;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: _surfaceMuted,
        border: Border.all(color: _line),
        borderRadius: BorderRadius.circular(7),
      ),
      child: Row(
        children: [
          Container(
            width: 4,
            height: 42,
            color: order.kind == OrderKind.ifood
                ? Colors.red
                : order.kind == OrderKind.delivery
                ? _warn
                : _navy2,
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${order.number} ${order.customerName}'.trim(),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _navy,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text(
                  '${kindLabel(order.kind)} | ${statusLabel(order.status)} | ${order.itemsCount} itens',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: _textSecondary, fontSize: 12),
                ),
              ],
            ),
          ),
          Text(
            money(order.subtotal),
            style: const TextStyle(fontWeight: FontWeight.w900),
          ),
          const SizedBox(width: 8),
          FilledButton(
            onPressed: onTap,
            style: FilledButton.styleFrom(
              backgroundColor: _navy2,
              foregroundColor: Colors.white,
              minimumSize: const Size(0, 34),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(5),
              ),
            ),
            child: Text(action),
          ),
          if (secondary != null && onSecondary != null) ...[
            const SizedBox(width: 6),
            OutlinedButton(
              onPressed: onSecondary,
              style: OutlinedButton.styleFrom(
                minimumSize: const Size(0, 34),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(5),
                ),
              ),
              child: Text(secondary!),
            ),
          ],
        ],
      ),
    );
  }
}

class _ProductCatalogModule extends StatefulWidget {
  const _ProductCatalogModule({required this.store});

  final BalcaoStore store;

  @override
  State<_ProductCatalogModule> createState() => _ProductCatalogModuleState();
}

class _ProductCatalogModuleState extends State<_ProductCatalogModule> {
  final code = TextEditingController();
  final name = TextEditingController();
  final price = TextEditingController();
  final cost = TextEditingController();
  final stock = TextEditingController();
  final minStock = TextEditingController(text: '3');
  String category = 'LANCHES';

  @override
  void dispose() {
    code.dispose();
    name.dispose();
    price.dispose();
    cost.dispose();
    stock.dispose();
    minStock.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final desktop = constraints.maxWidth >= 720;
        final form = _WindowPanel(
          title: 'Novo produto',
          action: TextButton(onPressed: _clear, child: const Text('Limpar')),
          child: _ProductForm(
            code: code,
            name: name,
            price: price,
            cost: cost,
            stock: stock,
            minStock: minStock,
            category: category,
            categories: widget.store.categories,
            onCategory: (value) => setState(() => category = value),
            onSave: _save,
          ),
        );
        final catalog = _WindowPanel(
          title:
              'Catalogo / estoque (${widget.store.products.length} produtos)',
          child: Column(
            children: widget.store.products
                .map(
                  (product) =>
                      _ProductManageLine(store: widget.store, product: product),
                )
                .toList(),
          ),
        );
        if (desktop) {
          return Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SizedBox(width: 360, child: form),
                const SizedBox(width: 10),
                Expanded(child: SingleChildScrollView(child: catalog)),
              ],
            ),
          );
        }
        return _ModuleScroll(children: [form, catalog]);
      },
    );
  }

  Future<void> _save() async {
    if (name.text.trim().isEmpty) return;
    await widget.store.saveProduct(
      name: name.text.trim().toUpperCase(),
      code: code.text.trim().isEmpty
          ? DateTime.now().millisecondsSinceEpoch.toString().substring(7)
          : code.text.trim(),
      category: category,
      price: _parse(price.text),
      cost: _parse(cost.text),
      stock: _parse(stock.text).round(),
      minStock: _parse(minStock.text).round(),
    );
    _clear();
  }

  void _clear() {
    code.clear();
    name.clear();
    price.clear();
    cost.clear();
    stock.clear();
    minStock.text = '3';
  }
}

class _ProductForm extends StatelessWidget {
  const _ProductForm({
    required this.code,
    required this.name,
    required this.price,
    required this.cost,
    required this.stock,
    required this.minStock,
    required this.category,
    required this.categories,
    required this.onCategory,
    required this.onSave,
  });

  final TextEditingController code;
  final TextEditingController name;
  final TextEditingController price;
  final TextEditingController cost;
  final TextEditingController stock;
  final TextEditingController minStock;
  final String category;
  final List<String> categories;
  final ValueChanged<String> onCategory;
  final VoidCallback onSave;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: _DeskInput(label: 'Codigo', controller: code),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: _DeskSelect(
                label: 'Categoria',
                value: category,
                items: categories,
                onChanged: onCategory,
              ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        _DeskInput(label: 'Nome do produto', controller: name),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(
              child: _DeskInput(
                label: 'Preco',
                controller: price,
                keyboardType: TextInputType.number,
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: _DeskInput(
                label: 'Custo',
                controller: cost,
                keyboardType: TextInputType.number,
              ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(
              child: _DeskInput(
                label: 'Estoque',
                controller: stock,
                keyboardType: TextInputType.number,
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: _DeskInput(
                label: 'Minimo',
                controller: minStock,
                keyboardType: TextInputType.number,
              ),
            ),
          ],
        ),
        const SizedBox(height: 10),
        SizedBox(
          width: double.infinity,
          height: 40,
          child: FilledButton(
            onPressed: onSave,
            style: FilledButton.styleFrom(
              backgroundColor: _teal,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(5),
              ),
            ),
            child: const Text(
              'Salvar produto',
              style: TextStyle(fontWeight: FontWeight.w900),
            ),
          ),
        ),
      ],
    );
  }
}

class _ProductManageLine extends StatelessWidget {
  const _ProductManageLine({required this.store, required this.product});

  final BalcaoStore store;
  final Product product;

  @override
  Widget build(BuildContext context) {
    final low = product.stock <= product.minStock;
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 9),
      decoration: const BoxDecoration(
        border: Border(top: BorderSide(color: _line)),
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final compact = constraints.maxWidth < 600;
          final info = Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                product.name,
                softWrap: true,
                style: const TextStyle(fontWeight: FontWeight.w900),
              ),
              const SizedBox(height: 2),
              Text(
                '${product.category} | custo ${money(product.cost)} | margem ${product.margin.toStringAsFixed(0)}%',
                softWrap: true,
                style: const TextStyle(color: _textSecondary, fontSize: 12),
              ),
            ],
          );
          final stockText = Text(
            '${product.stock} / ${product.minStock}',
            textAlign: compact ? TextAlign.left : TextAlign.right,
            style: TextStyle(
              color: low ? _danger : _navy,
              fontWeight: FontWeight.w900,
            ),
          );
          final actions = Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              IconButton(
                tooltip: 'Baixar estoque',
                onPressed: () => store.adjustStock(product, product.stock - 1),
                icon: const Icon(Icons.remove_rounded),
              ),
              IconButton(
                tooltip: 'Adicionar estoque',
                onPressed: () => store.adjustStock(product, product.stock + 1),
                icon: const Icon(Icons.add_rounded),
              ),
            ],
          );

          if (compact) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Wrap(
                  spacing: 12,
                  runSpacing: 4,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    Text(
                      product.code,
                      style: const TextStyle(fontWeight: FontWeight.w900),
                    ),
                    Text(
                      money(product.price),
                      style: const TextStyle(fontWeight: FontWeight.w900),
                    ),
                    stockText,
                  ],
                ),
                const SizedBox(height: 6),
                info,
                const SizedBox(height: 4),
                actions,
              ],
            );
          }

          return Row(
            children: [
              SizedBox(
                width: 72,
                child: Text(
                  product.code,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
              ),
              Expanded(child: info),
              SizedBox(
                width: 86,
                child: Text(
                  money(product.price),
                  textAlign: TextAlign.right,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
              ),
              SizedBox(width: 74, child: stockText),
              actions,
            ],
          );
        },
      ),
    );
  }
}

class _DeliveryZonesModule extends StatefulWidget {
  const _DeliveryZonesModule({required this.store});

  final BalcaoStore store;

  @override
  State<_DeliveryZonesModule> createState() => _DeliveryZonesModuleState();
}

class _DeliveryZonesModuleState extends State<_DeliveryZonesModule> {
  final radius = TextEditingController(text: '1,0');
  final fee = TextEditingController(text: '0,00');
  final minimum = TextEditingController(text: '0,00');
  final operator = TextEditingController(text: '2');
  final pin = TextEditingController();
  String? selectedId;
  bool active = true;
  String message = '';

  @override
  void dispose() {
    radius.dispose();
    fee.dispose();
    minimum.dispose();
    operator.dispose();
    pin.dispose();
    super.dispose();
  }

  void _select(DeliveryZone zone) {
    setState(() {
      selectedId = zone.id;
      radius.text = zone.radiusKm.toStringAsFixed(1).replaceAll('.', ',');
      fee.text = zone.fee.toStringAsFixed(2).replaceAll('.', ',');
      minimum.text = zone.minimumOrder.toStringAsFixed(2).replaceAll('.', ',');
      active = zone.active;
      message = '';
    });
  }

  void _newZone() {
    final next = widget.store.deliveryZones.isEmpty
        ? 1.0
        : widget.store.deliveryZones
                  .map((zone) => zone.radiusKm)
                  .reduce(math.max)
                  .ceilToDouble() +
              1;
    setState(() {
      selectedId = null;
      radius.text = next.toStringAsFixed(1).replaceAll('.', ',');
      fee.text = '0,00';
      minimum.text = '0,00';
      active = true;
      message = '';
    });
  }

  Future<void> _save() async {
    final radiusValue =
        double.tryParse(radius.text.trim().replaceAll(',', '.')) ?? -1;
    final saved = await widget.store.saveDeliveryZone(
      id: selectedId,
      radiusKm: radius.text,
      fee: fee.text,
      minimumOrder: minimum.text,
      active: active,
      operator: operator.text,
      pin: pin.text,
    );
    if (!mounted) return;
    setState(() {
      message = widget.store.securityMessage;
      if (saved) {
        pin.clear();
        selectedId = widget.store.deliveryZones
            .where((zone) => (zone.radiusKm - radiusValue).abs() < 0.001)
            .firstOrNull
            ?.id;
      }
    });
  }

  Future<void> _delete() async {
    if (selectedId == null) {
      setState(() => message = 'Selecione uma faixa para excluir.');
      return;
    }
    final deleted = await widget.store.deleteDeliveryZone(
      id: selectedId!,
      operator: operator.text,
      pin: pin.text,
    );
    if (!mounted) return;
    setState(() {
      message = widget.store.securityMessage;
      if (deleted) {
        pin.clear();
        selectedId = null;
      }
    });
  }

  Future<void> _openMap() async {
    final query = Uri.encodeQueryComponent(
      '${widget.store.businessAddress}, ${widget.store.businessCity}, ${widget.store.businessUf}',
    );
    final uri = Uri.parse('https://www.openstreetmap.org/search?query=$query');
    if (!await launchUrl(uri, mode: LaunchMode.externalApplication) &&
        mounted) {
      setState(() => message = 'Nao consegui abrir o mapa agora.');
    }
  }

  @override
  Widget build(BuildContext context) {
    return _ModuleScroll(
      children: [
        _WindowPanel(
          title: 'Taxas de entrega',
          action: _StatusPill(
            text: '${widget.store.deliveryZones.length} faixa(s)',
            color: _teal,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Cadastre faixas por distancia. O PDV usa a menor faixa ativa e sugere a taxa no novo delivery.',
                style: TextStyle(color: _textSecondary),
              ),
              const SizedBox(height: 10),
              if (widget.store.deliveryZones.isEmpty)
                const _Empty(text: 'Nenhum raio salvo.')
              else
                ...widget.store.deliveryZones.map(
                  (zone) => ListTile(
                    selected: selectedId == zone.id,
                    leading: Icon(
                      Icons.radio_button_checked_rounded,
                      color: zone.active ? _teal : _textSecondary,
                    ),
                    title: Text(zone.display),
                    subtitle: Text(zone.active ? 'Ativa' : 'Desativada'),
                    trailing: const Icon(Icons.edit_outlined),
                    onTap: () => _select(zone),
                  ),
                ),
            ],
          ),
        ),
        _WindowPanel(
          title: 'Cadastrar faixa',
          child: Column(
            children: [
              Row(
                children: [
                  Expanded(
                    child: _DeskInput(
                      key: const Key('deliveryZoneRadius'),
                      label: 'Raio (km)',
                      controller: radius,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _DeskInput(
                      key: const Key('deliveryZoneFee'),
                      label: 'Taxa',
                      controller: fee,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _DeskInput(
                      key: const Key('deliveryZoneMinimum'),
                      label: 'Pedido minimo',
                      controller: minimum,
                      keyboardType: const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                    ),
                  ),
                ],
              ),
              CheckboxListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('Faixa ativa'),
                value: active,
                onChanged: (value) => setState(() => active = value ?? true),
              ),
              Row(
                children: [
                  Expanded(
                    child: _DeskInput(
                      key: const Key('deliveryZoneOperator'),
                      label: 'Operador autorizado',
                      controller: operator,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _DeskInput(
                      key: const Key('deliveryZonePin'),
                      label: 'Senha',
                      controller: pin,
                      keyboardType: TextInputType.number,
                      obscureText: true,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  _DeskCommandButton(
                    key: const Key('deliveryZoneSave'),
                    label: 'Salvar raio',
                    color: _teal,
                    onTap: _save,
                  ),
                  _DeskCommandButton(
                    label: 'Novo',
                    color: _navy2,
                    onTap: _newZone,
                  ),
                  _DeskCommandButton(
                    key: const Key('deliveryZoneDelete'),
                    label: 'Excluir selecionado',
                    color: _danger,
                    onTap: _delete,
                  ),
                  _DeskCommandButton(
                    label: 'Abrir mapa da loja',
                    color: _warn,
                    onTap: _openMap,
                  ),
                ],
              ),
              if (message.isNotEmpty) ...[
                const SizedBox(height: 10),
                _InfoStrip(
                  icon: Icons.delivery_dining_outlined,
                  title: 'Resultado',
                  text: message,
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _NewDeliveryOrderModule extends StatefulWidget {
  const _NewDeliveryOrderModule({required this.store});

  final BalcaoStore store;

  @override
  State<_NewDeliveryOrderModule> createState() =>
      _NewDeliveryOrderModuleState();
}

class _NewDeliveryOrderModuleState extends State<_NewDeliveryOrderModule> {
  final document = TextEditingController();
  final phone = TextEditingController();
  final customer = TextEditingController(text: 'CLIENTE BALCAO');
  final address = TextEditingController();
  final district = TextEditingController();
  final note = TextEditingController();
  final fee = TextEditingController(text: '0,00');
  String type = 'Entrega';
  String courier = '';
  bool autoPrint = true;
  String zoneHint =
      'Sem circulo cadastrado. Informe a taxa manual ou cadastre raios.';

  @override
  void initState() {
    super.initState();
    district.addListener(_applySuggestedFee);
    _applySuggestedFee();
  }

  @override
  void dispose() {
    document.dispose();
    phone.dispose();
    customer.dispose();
    address.dispose();
    district.dispose();
    note.dispose();
    fee.dispose();
    super.dispose();
  }

  void _applySuggestedFee() {
    final zone = widget.store.suggestedDeliveryZone;
    if (zone == null) {
      if (mounted) {
        setState(() {
          zoneHint =
              'Sem circulo cadastrado. Informe a taxa manual ou cadastre raios.';
        });
      }
      return;
    }
    fee.text = zone.fee.toStringAsFixed(2).replaceAll('.', ',');
    if (mounted) {
      setState(() {
        zoneHint =
            'Taxa sugerida: ate ${zone.radiusKm.toStringAsFixed(1)} km (${money(zone.fee)})'
            '${zone.minimumOrder > 0 ? ' | pedido minimo ${money(zone.minimumOrder)}' : ''}.';
      });
    }
  }

  Future<void> _showDeliveryZones() async {
    await showDialog<void>(
      context: context,
      builder: (context) => Dialog(
        insetPadding: const EdgeInsets.all(18),
        child: SizedBox(
          width: 860,
          height: 680,
          child: _DeliveryZonesModule(store: widget.store),
        ),
      ),
    );
    _applySuggestedFee();
  }

  Future<void> _create() async {
    final cleanCustomer = customer.text.trim().isEmpty
        ? 'CLIENTE BALCAO'
        : customer.text.trim();
    final cleanAddress = type == 'Balcao'
        ? 'Retirada no balcao'
        : address.text.trim().isEmpty
        ? 'Endereco nao informado'
        : address.text.trim();
    await widget.store.openOrder(
      type == 'Balcao' ? OrderKind.counter : OrderKind.delivery,
      customer: cleanCustomer,
      customerPhone: phone.text,
      address: cleanAddress,
      district: district.text,
      notes: note.text,
      deliveryFee: type == 'Balcao' ? '0' : fee.text,
      courier: courier,
      autoPrint: autoPrint,
    );
    if (mounted) Navigator.of(context).maybePop();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(22, 20, 22, 20),
      child: Column(
        children: [
          const _InfoStrip(
            icon: Icons.dashboard_customize_outlined,
            title: 'Cadastro rapido de pedido',
            text:
                'Preencha os dados abaixo para criar um pedido de entrega ou retirada com mais agilidade.',
          ),
          const SizedBox(height: 18),
          Expanded(
            child: LayoutBuilder(
              builder: (context, constraints) {
                final wide = constraints.maxWidth >= 920;
                final left = Column(
                  children: [
                    _WpfActionCard(
                      title: 'Cliente',
                      child: Column(
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: _DeskInput(
                                  label: 'CPF/CNPJ',
                                  controller: document,
                                ),
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: _DeskInput(
                                  label: 'Telefone',
                                  controller: phone,
                                  keyboardType: TextInputType.phone,
                                ),
                              ),
                              const SizedBox(width: 12),
                              SizedBox(
                                width: 200,
                                child: _OutlineActionTile(
                                  icon: Icons.person_search_outlined,
                                  label: 'Incluir cliente cadastrado',
                                  onTap: () {},
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          Row(
                            children: [
                              Expanded(
                                child: _DeskInput(
                                  label: 'Cliente',
                                  controller: customer,
                                ),
                              ),
                              const SizedBox(width: 12),
                              SizedBox(
                                width: 200,
                                child: _OutlineActionTile(
                                  icon: Icons.delivery_dining_outlined,
                                  label: 'Taxas por raio no mapa',
                                  onTap: _showDeliveryZones,
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 18),
                    Expanded(
                      child: _WpfActionCard(
                        title: 'Endereco de entrega',
                        child: Column(
                          children: [
                            _DeskInput(label: 'Endereco', controller: address),
                            const SizedBox(height: 12),
                            _DeskInput(
                              label: 'Bairro / referencia',
                              controller: district,
                            ),
                            const SizedBox(height: 12),
                            Expanded(
                              child: TextField(
                                controller: note,
                                expands: true,
                                maxLines: null,
                                minLines: null,
                                textAlignVertical: TextAlignVertical.top,
                                decoration: _deskDecoration('Observacao'),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                );
                final right = _WpfActionCard(
                  title: 'Configuracoes do pedido',
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const _WpfFieldLabel('Tipo'),
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          _DeliveryTypeTile(
                            label: 'Entrega',
                            icon: Icons.flag_outlined,
                            selected: type == 'Entrega',
                            onTap: () => setState(() => type = 'Entrega'),
                          ),
                          const SizedBox(width: 10),
                          _DeliveryTypeTile(
                            label: 'Retirada',
                            icon: Icons.shopping_cart_outlined,
                            selected: type == 'Retirada',
                            onTap: () => setState(() => type = 'Retirada'),
                          ),
                          const SizedBox(width: 10),
                          _DeliveryTypeTile(
                            label: 'Balcao',
                            icon: Icons.home_outlined,
                            selected: type == 'Balcao',
                            onTap: () => setState(() => type = 'Balcao'),
                          ),
                        ],
                      ),
                      const SizedBox(height: 18),
                      _DeskSelect(
                        label: 'Entregador',
                        value: courier,
                        items: const ['', 'Joao Motoboy', 'Motoboy loja'],
                        onChanged: (value) => setState(() => courier = value),
                      ),
                      const SizedBox(height: 14),
                      _DeskInput(
                        label: 'Taxa',
                        controller: fee,
                        keyboardType: TextInputType.number,
                      ),
                      const SizedBox(height: 8),
                      Text(
                        zoneHint,
                        style: const TextStyle(
                          color: Color(0xFF9B5D00),
                          fontSize: 12,
                          fontWeight: FontWeight.w800,
                          height: 1.15,
                        ),
                      ),
                      const SizedBox(height: 28),
                      const _WpfFieldLabel('Impressao'),
                      const SizedBox(height: 8),
                      InkWell(
                        onTap: () => setState(() => autoPrint = !autoPrint),
                        borderRadius: BorderRadius.circular(8),
                        child: Container(
                          height: 76,
                          width: double.infinity,
                          padding: const EdgeInsets.symmetric(horizontal: 16),
                          decoration: BoxDecoration(
                            color: _mint,
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(color: _teal),
                          ),
                          child: Row(
                            children: [
                              const Expanded(
                                child: Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      'Imprimir automaticamente',
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      style: TextStyle(
                                        color: _teal,
                                        fontSize: 16,
                                        fontWeight: FontWeight.w900,
                                      ),
                                    ),
                                    SizedBox(height: 3),
                                    Text(
                                      'Ao criar, envia o pedido para a impressao.',
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      style: TextStyle(
                                        color: _textSecondary,
                                        fontSize: 12,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              Container(
                                width: 30,
                                height: 30,
                                alignment: Alignment.center,
                                decoration: BoxDecoration(
                                  color: autoPrint ? _teal : Colors.white,
                                  shape: BoxShape.circle,
                                  border: Border.all(color: _teal),
                                ),
                                child: autoPrint
                                    ? const Icon(
                                        Icons.check_rounded,
                                        color: Colors.white,
                                        size: 19,
                                      )
                                    : null,
                              ),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),
                );

                if (!wide) {
                  return ListView(
                    children: [
                      SizedBox(height: 560, child: left),
                      const SizedBox(height: 12),
                      right,
                    ],
                  );
                }
                return Row(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Expanded(flex: 2, child: left),
                    const SizedBox(width: 18),
                    SizedBox(width: 350, child: right),
                  ],
                );
              },
            ),
          ),
          const SizedBox(height: 18),
          Row(
            children: [
              Expanded(
                child: SizedBox(
                  height: 58,
                  child: OutlinedButton(
                    onPressed: () => Navigator.of(context).maybePop(),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: _navy,
                      side: const BorderSide(color: _line),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                      textStyle: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    child: const Text('Cancelar'),
                  ),
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                flex: 2,
                child: SizedBox(
                  height: 58,
                  child: FilledButton(
                    onPressed: _create,
                    style: FilledButton.styleFrom(
                      backgroundColor: _teal,
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                      textStyle: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    child: const Text('Criar pedido'),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _WpfActionCard extends StatelessWidget {
  const _WpfActionCard({required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final bounded = constraints.hasBoundedHeight;
        return Container(
          width: double.infinity,
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: _line),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: _navy,
                  fontSize: 19,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 16),
              if (bounded) Expanded(child: child) else child,
            ],
          ),
        );
      },
    );
  }
}

class _WpfFieldLabel extends StatelessWidget {
  const _WpfFieldLabel(this.label);

  final String label;

  @override
  Widget build(BuildContext context) {
    return Text(
      label,
      style: const TextStyle(
        color: _textSecondary,
        fontSize: 13,
        fontWeight: FontWeight.w900,
      ),
    );
  }
}

class _DeliveryTypeTile extends StatelessWidget {
  const _DeliveryTypeTile({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(10),
        child: Container(
          height: 76,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: selected ? _teal : Colors.white,
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: selected ? _teal : _line),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, color: selected ? Colors.white : _teal, size: 25),
              const SizedBox(height: 5),
              Text(
                label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: selected ? Colors.white : _teal,
                  fontSize: 12.5,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _OutlineActionTile extends StatelessWidget {
  const _OutlineActionTile({
    required this.icon,
    required this.label,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 58,
      child: OutlinedButton.icon(
        onPressed: onTap,
        icon: Icon(icon, size: 22),
        label: Text(label, maxLines: 2, overflow: TextOverflow.ellipsis),
        style: OutlinedButton.styleFrom(
          foregroundColor: _teal,
          backgroundColor: _mint,
          side: const BorderSide(color: Color(0xFFFF9A5C)),
          padding: const EdgeInsets.symmetric(horizontal: 12),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
          textStyle: const TextStyle(
            fontSize: 12.5,
            fontWeight: FontWeight.w900,
            height: 1.1,
          ),
        ),
      ),
    );
  }
}

class _DeliveryModule extends StatefulWidget {
  const _DeliveryModule({required this.store});

  final BalcaoStore store;

  @override
  State<_DeliveryModule> createState() => _DeliveryModuleState();
}

class _DeliveryModuleState extends State<_DeliveryModule> {
  final authorizationCode = TextEditingController();

  BalcaoStore get store => widget.store;

  @override
  void dispose() {
    authorizationCode.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(12),
          child: _WindowPanel(
            title: 'iFood Online',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _ReportLine(
                  label: store.ifoodMerchantName.isEmpty
                      ? 'Conexao iFood'
                      : store.ifoodMerchantName,
                  detail: store.ifoodMessage,
                  value: store.ifoodBusy
                      ? '...'
                      : store.ifoodConnected
                      ? 'ON'
                      : 'OFF',
                  color: store.ifoodConnected ? _teal : Colors.red,
                ),
                if (store.ifoodLastSyncAt.isNotEmpty)
                  _ReportLine(
                    label: 'Ultima sincronizacao',
                    detail: store.ifoodLastSyncAt,
                    value:
                        '${store.orders.where((order) => order.kind == OrderKind.ifood).length}',
                    color: _navy2,
                  ),
                if (store.ifoodVerificationUrl.isNotEmpty) ...[
                  const SizedBox(height: 10),
                  _AccessLinkBox(
                    label: 'Autorizar no iFood',
                    value: store.ifoodVerificationUrl,
                    status: store.ifoodUserCode.isEmpty
                        ? 'abrir'
                        : store.ifoodUserCode,
                    color: Colors.red,
                  ),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Expanded(
                        child: _DeskInput(
                          key: const Key('ifoodAuthorizationCode'),
                          label: 'Codigo de autorizacao',
                          controller: authorizationCode,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: _DeskCommandButton(
                          key: const Key('ifoodFinishConnection'),
                          label: 'Finalizar conexao',
                          color: Colors.red,
                          onTap: () => _finishConnection(),
                        ),
                      ),
                    ],
                  ),
                ],
                const SizedBox(height: 10),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    _DeskCommandButton(
                      key: const Key('ifoodConnect'),
                      label: store.ifoodConnected
                          ? 'Reconectar iFood'
                          : 'Conectar iFood',
                      color: Colors.red,
                      onTap: () => _connect(),
                    ),
                    _DeskCommandButton(
                      key: const Key('ifoodSyncOrders'),
                      label: 'Buscar pedidos',
                      color: _teal,
                      onTap: store.ifoodConnected ? () => _syncOrders() : () {},
                    ),
                    _DeskCommandButton(
                      label: 'Novo delivery',
                      color: _navy2,
                      onTap: () => store.openOrder(
                        OrderKind.delivery,
                        customer: 'Cliente delivery',
                        address: 'Endereco delivery',
                      ),
                    ),
                    if (store.ifoodVerificationUrl.isNotEmpty)
                      _DeskCommandButton(
                        label: 'Abrir autorizacao',
                        color: _navy2,
                        onTap: () => _openAuthorization(),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
            children: [
              _WindowPanel(
                title: 'Pedidos iFood recebidos',
                child: Column(
                  children: store.orders
                      .where((order) => order.kind == OrderKind.ifood)
                      .map(
                        (order) => ListTile(
                          contentPadding: const EdgeInsets.symmetric(
                            horizontal: 4,
                          ),
                          leading: const CircleAvatar(
                            backgroundColor: Color(0xFFFFE9E6),
                            foregroundColor: Colors.red,
                            child: Icon(Icons.restaurant_rounded),
                          ),
                          title: Text(
                            '${order.number} - ${order.customerName}',
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(fontWeight: FontWeight.w900),
                          ),
                          subtitle: Text(
                            '${statusLabel(order.status)} | ${order.itemsCount} item(ns)',
                          ),
                          trailing: Text(
                            money(order.subtotal),
                            style: const TextStyle(
                              color: _navy2,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                          onTap: () => store.selectOrder(order.id),
                        ),
                      )
                      .toList(),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Future<void> _connect() async {
    await store.connectIfood();
    if (mounted) setState(() {});
    if (store.ifoodVerificationUrl.isNotEmpty) {
      await _openAuthorization();
    }
  }

  Future<void> _finishConnection() async {
    await store.finishIfoodConnection(authorizationCode.text);
    if (mounted) setState(() {});
  }

  Future<void> _syncOrders() async {
    await store.syncIfoodOrders();
    if (mounted) setState(() {});
  }

  Future<void> _openAuthorization() async {
    final uri = Uri.tryParse(store.ifoodVerificationUrl);
    if (uri == null) return;
    if (!await launchUrl(uri, mode: LaunchMode.externalApplication)) {
      await Clipboard.setData(ClipboardData(text: store.ifoodVerificationUrl));
    }
  }
}

class _CustomerDeskModule extends StatefulWidget {
  const _CustomerDeskModule({required this.store});

  final BalcaoStore store;

  @override
  State<_CustomerDeskModule> createState() => _CustomerDeskModuleState();
}

class _CustomerDeskModuleState extends State<_CustomerDeskModule> {
  final name = TextEditingController();
  final phone = TextEditingController();
  final address = TextEditingController();

  @override
  void dispose() {
    name.dispose();
    phone.dispose();
    address.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final missing = widget.store.customers
        .where((customer) => customer.missing)
        .toList();
    return LayoutBuilder(
      builder: (context, constraints) {
        final desktop = constraints.maxWidth >= 720;
        final form = _WindowPanel(
          title: 'Cadastro de cliente',
          child: Column(
            children: [
              _DeskInput(label: 'Nome', controller: name),
              const SizedBox(height: 8),
              _DeskInput(label: 'Telefone', controller: phone),
              const SizedBox(height: 8),
              _DeskInput(label: 'Endereco', controller: address),
              const SizedBox(height: 10),
              SizedBox(
                width: double.infinity,
                height: 40,
                child: FilledButton(
                  onPressed: _save,
                  style: FilledButton.styleFrom(
                    backgroundColor: _teal,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(5),
                    ),
                  ),
                  child: const Text(
                    'Salvar cliente',
                    style: TextStyle(fontWeight: FontWeight.w900),
                  ),
                ),
              ),
            ],
          ),
        );
        final list = _WindowPanel(
          title: 'Clientes e fidelidade',
          child: Column(
            children: widget.store.customers
                .map((customer) => _CustomerDeskLine(customer: customer))
                .toList(),
          ),
        );
        final crm = _WindowPanel(
          title: 'WhatsApp automatico',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: _MiniSummary(
                      label: 'Clientes',
                      value: '${widget.store.customers.length}',
                      sub: 'cadastros da loja',
                      color: _teal,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _MiniSummary(
                      label: 'Retorno',
                      value: '${missing.length}',
                      sub: '45 dias sem compra',
                      color: _warn,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              const _InfoStrip(
                icon: Icons.mark_chat_unread_rounded,
                title: 'Sem copiar mensagem',
                text:
                    'Campanhas entram na fila do WhatsApp respeitando cooldown anti-flood.',
              ),
              const SizedBox(height: 10),
              ...missing.map(
                (customer) => _CustomerDeskLine(customer: customer),
              ),
            ],
          ),
        );
        if (constraints.maxWidth >= 1080) {
          return Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SizedBox(width: 340, child: form),
                const SizedBox(width: 10),
                Expanded(child: SingleChildScrollView(child: list)),
                const SizedBox(width: 10),
                SizedBox(width: 360, child: SingleChildScrollView(child: crm)),
              ],
            ),
          );
        }
        if (desktop) {
          return Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SizedBox(width: 320, child: form),
                const SizedBox(width: 10),
                Expanded(
                  child: SingleChildScrollView(
                    child: Column(
                      children: [crm, const SizedBox(height: 10), list],
                    ),
                  ),
                ),
              ],
            ),
          );
        }
        return _ModuleScroll(children: [form, crm, list]);
      },
    );
  }

  Future<void> _save() async {
    if (name.text.trim().isEmpty) return;
    await widget.store.saveCustomer(
      name.text.trim(),
      phone.text.trim(),
      address.text.trim(),
    );
    name.clear();
    phone.clear();
    address.clear();
  }
}

class _CustomerDeskLine extends StatelessWidget {
  const _CustomerDeskLine({required this.customer});

  final Customer customer;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: _surfaceMuted,
        border: Border.all(color: _line),
        borderRadius: BorderRadius.circular(7),
      ),
      child: Row(
        children: [
          Container(
            width: 4,
            height: 42,
            color: customer.missing ? _warn : _teal,
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  customer.name,
                  softWrap: true,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
                Text(
                  '${customer.phone.isEmpty ? 'sem telefone' : customer.phone} | ${customer.points} pts | cashback ${money(customer.cashback)}',
                  softWrap: true,
                  style: const TextStyle(color: _textSecondary, fontSize: 12),
                ),
              ],
            ),
          ),
          if (customer.missing)
            const _StatusPill(text: 'retorno', color: _warn),
        ],
      ),
    );
  }
}

class _ReportsDeskModule extends StatelessWidget {
  const _ReportsDeskModule({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    final closed = store.closedOrders;
    final canceled = store.orders
        .where((order) => order.status == OrderStatus.canceled)
        .length;
    return _ModuleScroll(
      children: [
        _WindowPanel(
          title: 'Painel operacional',
          child: LayoutBuilder(
            builder: (context, constraints) {
              final narrow = constraints.maxWidth < 700;
              final cards = [
                _MiniSummary(
                  label: 'Em aberto',
                  value: money(store.openTotal),
                  sub: '${store.openOrders.length} comandas/pedidos',
                  color: _navy2,
                ),
                _MiniSummary(
                  label: 'Caixa atual',
                  value: money(store.soldToday),
                  sub: store.cashOpen ? 'caixa aberto' : 'caixa fechado',
                  color: _teal,
                ),
                _MiniSummary(
                  label: 'Lucro bruto',
                  value: money(store.grossProfit),
                  sub: '${closed.length} vendas fechadas',
                  color: _warn,
                ),
                _MiniSummary(
                  label: 'Repasse iFood',
                  value: money(store.ifoodRepasse),
                  sub: 'liquido previsto',
                  color: Colors.red,
                ),
              ];
              if (narrow) {
                return Column(
                  children: cards
                      .map(
                        (card) => Padding(
                          padding: const EdgeInsets.only(bottom: 8),
                          child: card,
                        ),
                      )
                      .toList(),
                );
              }
              return GridView.count(
                crossAxisCount: 4,
                childAspectRatio: 1.8,
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                mainAxisSpacing: 8,
                crossAxisSpacing: 8,
                children: cards,
              );
            },
          ),
        ),
        _WindowPanel(
          title: 'iFood',
          child: Column(
            children: [
              _ReportLine(
                label: 'Vendas iFood',
                detail:
                    '${store.orders.where((order) => order.kind == OrderKind.ifood).length} pedido(s), $canceled cancelado(s)',
                value: money(store.ifoodSales),
                color: Colors.red,
              ),
              _ReportLine(
                label: 'Repasse iFood previsto',
                detail: 'pagamentos pelo iFood liquido apos taxa estimada',
                value: money(store.ifoodRepasse),
                color: _teal,
              ),
              _ReportLine(
                label: 'Entrega propria / merchant',
                detail: 'taxa estimada 12%',
                value: money(store.ifoodSales - store.ifoodRepasse),
                color: _warn,
              ),
            ],
          ),
        ),
        _WindowPanel(
          title: 'Vendas fechadas',
          child: Column(
            children: closed.isEmpty
                ? [const _Empty(text: 'Nenhuma venda fechada ainda.')]
                : closed
                      .map(
                        (order) => _ReportLine(
                          label: '${order.number} ${order.customerName}'.trim(),
                          detail:
                              '${kindLabel(order.kind)} | ${order.paymentMethod}',
                          value: money(order.subtotal),
                          color: _navy2,
                        ),
                      )
                      .toList(),
          ),
        ),
        _StockModule(store: store),
      ],
    );
  }
}

class _ReportLine extends StatelessWidget {
  const _ReportLine({
    required this.label,
    required this.detail,
    required this.value,
    required this.color,
  });

  final String label;
  final String detail;
  final String value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: _surfaceMuted,
        border: Border.all(color: _line),
        borderRadius: BorderRadius.circular(7),
      ),
      child: Row(
        children: [
          Container(
            width: 10,
            height: 10,
            decoration: BoxDecoration(color: color, shape: BoxShape.circle),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
                Text(
                  detail,
                  softWrap: true,
                  style: const TextStyle(color: _textSecondary, fontSize: 12),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Text(
            value,
            textAlign: TextAlign.right,
            style: const TextStyle(fontWeight: FontWeight.w900),
          ),
        ],
      ),
    );
  }
}

class _SettingsDeskModule extends StatefulWidget {
  const _SettingsDeskModule({required this.store});

  final BalcaoStore store;

  @override
  State<_SettingsDeskModule> createState() => _SettingsDeskModuleState();
}

class _SettingsDeskModuleState extends State<_SettingsDeskModule> {
  late final TextEditingController business = TextEditingController(
    text: widget.store.businessName,
  );
  late final TextEditingController legalName = TextEditingController(
    text: widget.store.businessLegalName,
  );
  late final TextEditingController email = TextEditingController(
    text: widget.store.authEmail,
  );
  late final TextEditingController document = TextEditingController(
    text: widget.store.businessDocument,
  );
  late final TextEditingController phone = TextEditingController(
    text: widget.store.businessPhone,
  );
  late final TextEditingController responsible = TextEditingController(
    text: widget.store.businessResponsible,
  );
  late final TextEditingController city = TextEditingController(
    text: widget.store.businessCity,
  );
  late final TextEditingController uf = TextEditingController(
    text: widget.store.businessUf,
  );
  late final TextEditingController address = TextEditingController(
    text: widget.store.businessAddress,
  );
  late final TextEditingController bridgeUrl = TextEditingController(
    text: widget.store.windowsBridgeUrl,
  );
  late final TextEditingController bridgeLocalUrl = TextEditingController(
    text: widget.store.windowsBridgeLocalUrl,
  );
  late final TextEditingController fiscalMerchantCode = TextEditingController(
    text: widget.store.fiscalMerchantCode,
  );
  late final TextEditingController fiscalCscId = TextEditingController(
    text: widget.store.fiscalCscId,
  );
  final fiscalOperator = TextEditingController(text: '2');
  final fiscalPin = TextEditingController();
  late bool fiscalEnabled = widget.store.fiscalEnabled;
  late bool requireFiscal = widget.store.requireFiscalBeforeReceipt;
  late String fiscalProvider = widget.store.fiscalProvider;
  late String tefProvider = widget.store.tefProvider;
  late String fiscalEnvironment = widget.store.fiscalEnvironment;
  String fiscalResult = '';
  int section = 0;

  @override
  void dispose() {
    business.dispose();
    legalName.dispose();
    email.dispose();
    document.dispose();
    phone.dispose();
    responsible.dispose();
    city.dispose();
    uf.dispose();
    address.dispose();
    bridgeUrl.dispose();
    bridgeLocalUrl.dispose();
    fiscalMerchantCode.dispose();
    fiscalCscId.dispose();
    fiscalOperator.dispose();
    fiscalPin.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final wide = constraints.maxWidth >= 880;
        return Column(
          children: [
            Expanded(
              child: SingleChildScrollView(
                padding: EdgeInsets.all(wide ? 18 : 12),
                child: Column(
                  children: [
                    _SettingsHero(store: widget.store),
                    const SizedBox(height: 14),
                    if (wide)
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          SizedBox(width: 250, child: _settingsNav()),
                          const SizedBox(width: 16),
                          Expanded(child: _settingsBody()),
                        ],
                      )
                    else ...[
                      _settingsTabs(),
                      const SizedBox(height: 12),
                      _settingsBody(),
                    ],
                  ],
                ),
              ),
            ),
            _SettingsFooter(
              store: widget.store,
              onSync: widget.store.flushSync,
              onSave: _saveProfile,
            ),
          ],
        );
      },
    );
  }

  Widget _settingsNav() {
    return _SettingsSideNav(
      selected: section,
      onChanged: (value) => setState(() => section = value),
    );
  }

  Widget _settingsTabs() {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: List.generate(
          _settingsSections.length,
          (index) => Padding(
            padding: const EdgeInsets.only(right: 8),
            child: ChoiceChip(
              selected: section == index,
              label: Text(_settingsSections[index].title),
              onSelected: (_) => setState(() => section = index),
            ),
          ),
        ),
      ),
    );
  }

  Widget _settingsBody() {
    return switch (section) {
      1 => _paymentsSection(),
      2 => _printSection(),
      3 => _systemSection(),
      4 => _implantSection(),
      5 => _accountSection(),
      _ => _storeSection(),
    };
  }

  Widget _storeSection() {
    return _SettingsSection(
      title: 'Empresa',
      subtitle:
          'Dados usados em recibos, cardapio online, impressao e integracoes.',
      child: Column(
        children: [
          _settingsPair(
            _DeskInput(label: 'Email da conta', controller: email),
            _DeskInput(label: 'Responsavel', controller: responsible),
          ),
          const SizedBox(height: 12),
          _settingsPair(
            _DeskInput(label: 'Nome fantasia', controller: business),
            _DeskInput(label: 'Razao social', controller: legalName),
          ),
          const SizedBox(height: 12),
          _settingsPair(
            _DeskInput(label: 'CPF/CNPJ', controller: document),
            _DeskInput(label: 'Telefone', controller: phone),
          ),
          const SizedBox(height: 12),
          _settingsPair(
            _DeskInput(label: 'Cidade', controller: city),
            _DeskInput(label: 'UF', controller: uf),
            rightWidth: 140,
          ),
          const SizedBox(height: 12),
          _DeskInput(label: 'Endereco', controller: address),
        ],
      ),
    );
  }

  Widget _paymentsSection() {
    return _SettingsSection(
      title: 'Pagamentos e NF',
      subtitle:
          'Mercado Pago aparece no caixa so quando estiver conectado e pronto.',
      child: Column(
        children: [
          _settingsPair(
            _SettingsMetric(
              label: 'Mercado Pago',
              value: widget.store.mercadoPagoCheckoutActive ? 'ativo' : 'off',
              sub: widget.store.pointStatusLabel,
              color: widget.store.mercadoPagoCheckoutActive ? _blue : _danger,
            ),
            _SettingsMetric(
              label: 'Terminal',
              value: widget.store.pointTerminalDisplay,
              sub: widget.store.pointSerial.isEmpty
                  ? 'sem maquininha selecionada'
                  : widget.store.pointSerial,
              color: widget.store.pointReady ? _teal : _warn,
            ),
          ),
          const SizedBox(height: 12),
          if (widget.store.pointConnectUrl.isNotEmpty) ...[
            _AccessLinkBox(
              label: 'Link de conexao Mercado Pago',
              value: widget.store.pointConnectUrl,
              status: 'abrir',
              color: _blue,
            ),
            const SizedBox(height: 10),
          ],
          _settingsButtonGrid([
            _DeskCommandButton(
              label: widget.store.pointConnected
                  ? 'Reconectar conta MP'
                  : 'Conectar conta MP',
              color: _blue,
              onTap: () => unawaited(_connectMercadoPago()),
            ),
            _DeskCommandButton(
              label: 'Abrir link MP',
              color: _navy2,
              onTap: () => unawaited(_openMercadoPagoLink()),
            ),
            _DeskCommandButton(
              label: 'Atualizar status',
              color: _navy2,
              onTap: () => unawaited(_refreshMercadoPago()),
            ),
            _DeskCommandButton(
              label: 'Carregar maquininhas',
              color: _teal,
              onTap: () => unawaited(_loadMercadoPagoTerminals()),
            ),
          ]),
          const SizedBox(height: 12),
          _SettingsTerminalPicker(
            store: widget.store,
            onSelect: _selectMercadoPagoTerminal,
          ),
          const SizedBox(height: 12),
          _InfoStrip(
            icon: Icons.payments_rounded,
            title: widget.store.mercadoPagoCheckoutActive
                ? 'Checkout integrado ativo'
                : 'PDV em modo normal',
            text: widget.store.mercadoPagoCheckoutActive
                ? 'Pix Mercado Pago, debito, credito e link ficam disponiveis no fechamento.'
                : 'O caixa mostra Dinheiro, Pix, Debito, Credito e Fiado sem depender do Mercado Pago.',
          ),
          const SizedBox(height: 12),
          const Divider(),
          SwitchListTile(
            key: const Key('fiscalEnabled'),
            contentPadding: EdgeInsets.zero,
            title: const Text('Ativar modulo fiscal/TEF separado'),
            subtitle: const Text(
              'NFC-e, SAT, MFE e maquininha ficam isolados do caixa.',
            ),
            value: fiscalEnabled,
            onChanged: (value) => setState(() => fiscalEnabled = value),
          ),
          const SizedBox(height: 8),
          _settingsPair(
            _DeskSelect(
              label: 'Fiscal',
              value: fiscalProvider,
              items: const ['NAO CONFIGURADO', 'NFC-E', 'SAT', 'MFE', 'OUTRO'],
              onChanged: (value) => setState(() => fiscalProvider = value),
            ),
            _DeskSelect(
              label: 'TEF / maquininha',
              value: tefProvider,
              items: const [
                'NAO CONFIGURADO',
                'STONE',
                'CIELO',
                'REDE',
                'PAGSEGURO',
                'TEF DISCADO',
                'OUTRO',
              ],
              onChanged: (value) => setState(() => tefProvider = value),
            ),
          ),
          const SizedBox(height: 12),
          _settingsPair(
            _DeskInput(
              key: const Key('fiscalMerchantCode'),
              label: 'Codigo do estabelecimento / afiliacao',
              controller: fiscalMerchantCode,
            ),
            _DeskInput(
              key: const Key('fiscalCscId'),
              label: 'CSC/Token fiscal ou referencia tecnica',
              controller: fiscalCscId,
              obscureText: true,
            ),
          ),
          const SizedBox(height: 12),
          _DeskSelect(
            label: 'Ambiente',
            value: fiscalEnvironment,
            items: const ['HOMOLOGACAO', 'PRODUCAO'],
            onChanged: (value) => setState(() => fiscalEnvironment = value),
          ),
          CheckboxListTile(
            key: const Key('fiscalRequireBeforeReceipt'),
            contentPadding: EdgeInsets.zero,
            title: const Text(
              'Exigir fiscal antes de imprimir comprovante de venda',
            ),
            value: requireFiscal,
            onChanged: (value) =>
                setState(() => requireFiscal = value ?? false),
          ),
          const SizedBox(height: 8),
          _settingsPair(
            _DeskInput(
              key: const Key('fiscalOperator'),
              label: 'Operador autorizado',
              controller: fiscalOperator,
            ),
            _DeskInput(
              key: const Key('fiscalPin'),
              label: 'Senha',
              controller: fiscalPin,
              keyboardType: TextInputType.number,
              obscureText: true,
            ),
          ),
          const SizedBox(height: 10),
          SizedBox(
            width: double.infinity,
            child: _DeskCommandButton(
              key: const Key('fiscalSave'),
              label: 'Salvar modulo fiscal/TEF',
              color: _teal,
              onTap: _saveFiscalSettings,
            ),
          ),
          if (fiscalResult.isNotEmpty) ...[
            const SizedBox(height: 10),
            _InfoStrip(
              icon: Icons.receipt_long_rounded,
              title: fiscalEnabled
                  ? 'Modulo fiscal ativo'
                  : 'Modulo fiscal salvo',
              text: fiscalResult,
            ),
          ],
        ],
      ),
    );
  }

  Widget _printSection() {
    return _SettingsSection(
      title: 'Impressao',
      subtitle: 'Cupom, QR e producao usam o bridge do Windows quando existir.',
      child: Column(
        children: [
          _DeskInput(label: 'Bridge Windows pela rede', controller: bridgeUrl),
          const SizedBox(height: 12),
          _DeskInput(
            label: 'Bridge Windows neste computador',
            controller: bridgeLocalUrl,
          ),
          const SizedBox(height: 12),
          _ReportLine(
            label: 'Bridge Windows',
            detail: widget.store.windowsBridgeUrl.isEmpty
                ? widget.store.windowsBridgeLocalUrl
                : widget.store.windowsBridgeUrl,
            value: widget.store.windowsBridgeStatus.contains('pronto')
                ? 'ON'
                : 'LOCAL',
            color: _teal,
          ),
          _ReportLine(
            label: 'Ultima impressao',
            detail: widget.store.printBridgeLabel,
            value: widget.store.windowsBridgeLastPrintAt.isEmpty ? '-' : 'OK',
            color: _navy2,
          ),
          const _InfoStrip(
            icon: Icons.print_rounded,
            title: 'Android e web',
            text:
                'Ao fechar venda, o app tenta baixar e imprimir pelo PDV Windows na mesma rede.',
          ),
        ],
      ),
    );
  }

  Widget _systemSection() {
    return _SettingsSection(
      title: 'Sistema',
      subtitle: 'Avisos, versao, sincronizacao e recursos da loja.',
      child: Column(
        children: [
          _ReportLine(
            label: 'Sincronizacao',
            detail: widget.store.syncStatus,
            value: widget.store.loggedIn ? 'ON' : 'LOGIN',
            color: widget.store.loggedIn ? _teal : _warn,
          ),
          _ReportLine(
            label: 'WhatsApp',
            detail: widget.store.whatsappConnected
                ? widget.store.whatsappNumber
                : 'aguardando QR da loja',
            value: widget.store.whatsappConnected ? 'ON' : 'QR',
            color: widget.store.whatsappConnected ? _teal : _warn,
          ),
          _ReportLine(
            label: 'iFood',
            detail: widget.store.ifoodMessage,
            value: widget.store.ifoodConnected ? 'ON' : 'OFF',
            color: widget.store.ifoodConnected ? _teal : Colors.red,
          ),
        ],
      ),
    );
  }

  Widget _implantSection() {
    return const _SettingsSection(
      title: 'Implantacao',
      subtitle: 'Checklist fiscal, LGPD e operacao inicial.',
      child: Column(
        children: [
          _InfoStrip(
            icon: Icons.task_alt_rounded,
            title: 'Checklist da loja',
            text:
                'Configure dados da empresa, meios de pagamento, impressora e cardapio antes de liberar o caixa.',
          ),
          SizedBox(height: 10),
          _InfoStrip(
            icon: Icons.security_rounded,
            title: 'LGPD',
            text:
                'Use telefone e endereco do cliente apenas para atendimento, entrega e historico da propria loja.',
          ),
        ],
      ),
    );
  }

  Widget _accountSection() {
    return _SettingsSection(
      title: 'Conta',
      subtitle: 'Plano, sincronizacao e manutencao da sessao.',
      child: Column(
        children: [
          _ReportLine(
            label: 'Conta da loja',
            detail: widget.store.licenseKey.isEmpty
                ? 'BLV-DEMO-139'
                : widget.store.licenseKey,
            value: widget.store.loggedIn ? 'ativa' : 'teste',
            color: widget.store.loggedIn ? _teal : _warn,
          ),
          _ReportLine(
            label: 'Fila local',
            detail: 'eventos aguardando sincronizacao',
            value: '${widget.store.pendingSyncCount}',
            color: _navy2,
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Expanded(
                child: _DeskCommandButton(
                  label: 'Resetar teste',
                  color: _warn,
                  onTap: widget.store.resetDemo,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _DeskCommandButton(
                  label: 'Sair',
                  color: _blue2,
                  onTap: widget.store.logout,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Future<void> _saveProfile() async {
    await widget.store.updateBusinessProfile(
      name: business.text,
      legalName: legalName.text,
      responsible: responsible.text,
      document: document.text,
      phone: phone.text,
      email: email.text,
      city: city.text,
      uf: uf.text,
      address: address.text,
    );
    await widget.store.updateBridgeSettings(
      networkUrl: bridgeUrl.text,
      localUrl: bridgeLocalUrl.text,
    );
    if (mounted) setState(() {});
  }

  Future<void> _saveFiscalSettings() async {
    final saved = await widget.store.saveFiscalSettings(
      enabled: fiscalEnabled,
      fiscal: fiscalProvider,
      tef: tefProvider,
      merchantCode: fiscalMerchantCode.text,
      cscId: fiscalCscId.text,
      environment: fiscalEnvironment,
      requireBeforeReceipt: requireFiscal,
      operator: fiscalOperator.text,
      pin: fiscalPin.text,
    );
    if (!mounted) return;
    setState(() {
      fiscalResult = widget.store.fiscalMessage;
      if (saved) fiscalPin.clear();
    });
  }

  Widget _settingsPair(Widget left, Widget right, {double? rightWidth}) {
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 620) {
          return Column(children: [left, const SizedBox(height: 12), right]);
        }
        return Row(
          children: [
            Expanded(child: left),
            const SizedBox(width: 12),
            rightWidth == null
                ? Expanded(child: right)
                : SizedBox(width: rightWidth, child: right),
          ],
        );
      },
    );
  }

  Widget _settingsButtonGrid(List<Widget> children) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = constraints.maxWidth >= 760 ? 4 : 2;
        final width = (constraints.maxWidth - ((columns - 1) * 8)) / columns;
        return Wrap(
          spacing: 8,
          runSpacing: 8,
          children: children
              .map((child) => SizedBox(width: width, child: child))
              .toList(),
        );
      },
    );
  }

  Future<void> _connectMercadoPago() async {
    await widget.store.startMercadoPagoConnect();
    if (mounted) setState(() {});
    await _openMercadoPagoLink();
  }

  Future<void> _openMercadoPagoLink() async {
    final value = widget.store.pointConnectUrl.trim();
    if (value.isEmpty) {
      Clipboard.setData(
        const ClipboardData(
          text: 'Conecte a conta Mercado Pago primeiro para gerar o link.',
        ),
      );
      return;
    }
    final uri = Uri.tryParse(value);
    if (uri == null) return;
    if (!await launchUrl(uri, mode: LaunchMode.externalApplication)) {
      await Clipboard.setData(ClipboardData(text: value));
    }
  }

  Future<void> _refreshMercadoPago() async {
    await widget.store.refreshMercadoPagoStatus(loadTerminals: true);
    if (mounted) setState(() {});
  }

  Future<void> _loadMercadoPagoTerminals() async {
    await widget.store.loadMercadoPagoTerminals();
    if (mounted) setState(() {});
  }

  Future<void> _selectMercadoPagoTerminal(MercadoPagoTerminal terminal) async {
    await widget.store.selectMercadoPagoTerminal(terminal);
    if (mounted) setState(() {});
  }
}

class _SettingsTerminalPicker extends StatelessWidget {
  const _SettingsTerminalPicker({required this.store, required this.onSelect});

  final BalcaoStore store;
  final ValueChanged<MercadoPagoTerminal> onSelect;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: _surfaceMuted,
        borderRadius: BorderRadius.circular(7),
        border: Border.all(color: _line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Expanded(
                child: Text(
                  'Maquininha Mercado Pago',
                  style: TextStyle(
                    color: _navy,
                    fontWeight: FontWeight.w900,
                    fontSize: 16,
                  ),
                ),
              ),
              _StatusPill(
                text: store.pointReady
                    ? 'selecionada'
                    : '${store.pointTerminals.length} Point(s)',
                color: store.pointReady ? _teal : _warn,
              ),
            ],
          ),
          const SizedBox(height: 8),
          if (!store.pointConnected)
            const _Empty(
              text:
                  'Conecte a conta Mercado Pago para selecionar a maquininha.',
            )
          else if (store.pointTerminals.isEmpty)
            const _Empty(
              text:
                  'Clique em Carregar maquininhas para buscar as Points da conta.',
            )
          else
            ...store.pointTerminals.map(
              (terminal) => _PointTerminalRow(
                terminal: terminal,
                selected: terminal.id == store.pointTerminalId,
                onTap: () => onSelect(terminal),
              ),
            ),
        ],
      ),
    );
  }
}

class _SettingsHero extends StatelessWidget {
  const _SettingsHero({required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth < 760;
        final title = Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Ajustes do PDV',
              style: TextStyle(
                color: Colors.white,
                fontSize: 28,
                fontWeight: FontWeight.w900,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              '${store.businessName} | conta, cardapio, pagamentos e impressao em um painel direto.',
              style: const TextStyle(color: _line, fontWeight: FontWeight.w700),
            ),
          ],
        );
        const pills = Wrap(
          spacing: 8,
          runSpacing: 8,
          alignment: WrapAlignment.end,
          children: [
            _StatusPill(text: 'Loja', color: _navy2),
            _StatusPill(text: 'Pagamentos', color: _navy2),
            _StatusPill(text: 'iFood', color: _navy2),
            _StatusPill(text: 'Impressao', color: _navy2),
          ],
        );
        return Container(
          width: double.infinity,
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: _railDeep,
            borderRadius: BorderRadius.circular(7),
            border: Border.all(color: _blue2),
          ),
          child: compact
              ? Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const _LogoBlock(size: 42),
                    const SizedBox(height: 14),
                    title,
                    const SizedBox(height: 14),
                    pills,
                  ],
                )
              : Row(
                  children: [
                    const _LogoBlock(size: 42),
                    const SizedBox(width: 16),
                    Expanded(child: title),
                    const SizedBox(width: 12),
                    pills,
                  ],
                ),
        );
      },
    );
  }
}

class _SettingsSideNav extends StatelessWidget {
  const _SettingsSideNav({required this.selected, required this.onChanged});

  final int selected;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(7),
        border: Border.all(color: _line),
      ),
      child: Column(
        children: List.generate(
          _settingsSections.length,
          (index) => Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: _SettingsNavButton(
              item: _settingsSections[index],
              selected: selected == index,
              onTap: () => onChanged(index),
            ),
          ),
        ),
      ),
    );
  }
}

class _SettingsNavButton extends StatelessWidget {
  const _SettingsNavButton({
    required this.item,
    required this.selected,
    required this.onTap,
  });

  final _SettingsNavItem item;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(7),
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
        decoration: BoxDecoration(
          color: selected ? _railDeep : Colors.white,
          borderRadius: BorderRadius.circular(7),
          border: Border.all(color: selected ? _blue2 : _line),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              item.title,
              style: TextStyle(
                color: selected ? Colors.white : _navy,
                fontWeight: FontWeight.w900,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              item.subtitle,
              style: TextStyle(
                color: selected ? _line : _textSecondary,
                fontSize: 12,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SettingsSection extends StatelessWidget {
  const _SettingsSection({
    required this.title,
    required this.subtitle,
    required this.child,
  });

  final String title;
  final String subtitle;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(7),
        border: Border.all(color: _line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              color: _navy,
              fontSize: 22,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            subtitle,
            style: const TextStyle(
              color: _textSecondary,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 20),
          child,
        ],
      ),
    );
  }
}

class _SettingsMetric extends StatelessWidget {
  const _SettingsMetric({
    required this.label,
    required this.value,
    required this.sub,
    required this.color,
  });

  final String label;
  final String value;
  final String sub;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: _surfaceMuted,
        borderRadius: BorderRadius.circular(7),
        border: Border.all(color: _line),
      ),
      child: Row(
        children: [
          Container(width: 4, height: 42, color: color),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    color: _textSecondary,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _navy,
                    fontSize: 20,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text(
                  sub,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: _textSecondary,
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _SettingsFooter extends StatelessWidget {
  const _SettingsFooter({
    required this.store,
    required this.onSync,
    required this.onSave,
  });

  final BalcaoStore store;
  final VoidCallback onSync;
  final VoidCallback onSave;

  @override
  Widget build(BuildContext context) {
    final message = Text(
      store.mercadoPagoCheckoutActive
          ? 'Mercado Pago ativo: pagamentos integrados aparecem no fechamento.'
          : 'PDV em modo normal: Mercado Pago fica oculto no fechamento ate conectar uma Point.',
      maxLines: 2,
      overflow: TextOverflow.ellipsis,
      style: const TextStyle(
        color: Color(0xFF99620D),
        fontWeight: FontWeight.w800,
      ),
    );
    return LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth < 760;
        return Container(
          padding: const EdgeInsets.fromLTRB(18, 12, 18, 12),
          decoration: const BoxDecoration(
            color: Colors.white,
            border: Border(top: BorderSide(color: _line)),
          ),
          child: compact
              ? Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    message,
                    const SizedBox(height: 10),
                    _DeskCommandButton(
                      label: 'Sincronizar',
                      color: _navy2,
                      onTap: onSync,
                    ),
                    const SizedBox(height: 8),
                    _DeskCommandButton(
                      label: 'Salvar configuracoes',
                      color: _railDeep,
                      onTap: onSave,
                    ),
                  ],
                )
              : Row(
                  children: [
                    Expanded(child: message),
                    const SizedBox(width: 12),
                    SizedBox(
                      width: 190,
                      child: _DeskCommandButton(
                        label: 'Sincronizar',
                        color: _navy2,
                        onTap: onSync,
                      ),
                    ),
                    const SizedBox(width: 10),
                    SizedBox(
                      width: 240,
                      child: _DeskCommandButton(
                        label: 'Salvar configuracoes',
                        color: _railDeep,
                        onTap: onSave,
                      ),
                    ),
                  ],
                ),
        );
      },
    );
  }
}

class _SettingsNavItem {
  const _SettingsNavItem(this.title, this.subtitle);

  final String title;
  final String subtitle;
}

const _settingsSections = [
  _SettingsNavItem('Loja', 'Empresa, marca e cardapio'),
  _SettingsNavItem('Pagamentos e NF', 'Mercado Pago e NFC-e'),
  _SettingsNavItem('Impressao', 'Cupom, QR e producao'),
  _SettingsNavItem('Sistema', 'Avisos, versao e updates'),
  _SettingsNavItem('Implantacao', 'Checklist, fiscal e LGPD'),
  _SettingsNavItem('Conta', 'Plano e sincronizacao'),
];

class _DeskInput extends StatelessWidget {
  const _DeskInput({
    super.key,
    required this.label,
    required this.controller,
    this.keyboardType,
    this.obscureText = false,
  });

  final String label;
  final TextEditingController controller;
  final TextInputType? keyboardType;
  final bool obscureText;

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      keyboardType: keyboardType,
      obscureText: obscureText,
      decoration: _deskDecoration(label),
    );
  }
}

class _DeskSelect extends StatelessWidget {
  const _DeskSelect({
    required this.label,
    required this.value,
    required this.items,
    required this.onChanged,
  });

  final String label;
  final String value;
  final List<String> items;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<String>(
      initialValue: value,
      isExpanded: true,
      borderRadius: BorderRadius.circular(8),
      dropdownColor: Colors.white,
      icon: const Icon(Icons.keyboard_arrow_down_rounded, color: _navy2),
      style: const TextStyle(
        color: _navy,
        fontWeight: FontWeight.w800,
        fontSize: 14,
      ),
      items: items
          .map(
            (item) => DropdownMenuItem(
              value: item,
              child: Text(item, maxLines: 1, overflow: TextOverflow.ellipsis),
            ),
          )
          .toList(),
      onChanged: (item) {
        if (item != null) onChanged(item);
      },
      decoration: _deskDecoration(label),
    );
  }
}

InputDecoration _deskDecoration(String label) {
  return InputDecoration(
    labelText: label,
    isDense: true,
    filled: true,
    fillColor: _surface,
    labelStyle: const TextStyle(
      color: _textSecondary,
      fontWeight: FontWeight.w800,
    ),
    floatingLabelStyle: const TextStyle(
      color: _navy2,
      fontWeight: FontWeight.w900,
    ),
    contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
    border: OutlineInputBorder(borderRadius: BorderRadius.circular(7)),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(7),
      borderSide: const BorderSide(color: _line),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(7),
      borderSide: const BorderSide(color: _navy2, width: 1.5),
    ),
  );
}

class _DeskCommandButton extends StatelessWidget {
  const _DeskCommandButton({
    super.key,
    required this.label,
    required this.color,
    required this.onTap,
  });

  final String label;
  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return ConstrainedBox(
      constraints: const BoxConstraints(minHeight: 40),
      child: FilledButton(
        onPressed: onTap,
        style: FilledButton.styleFrom(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
          backgroundColor: color,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(5)),
        ),
        child: Text(
          label,
          maxLines: 2,
          textAlign: TextAlign.center,
          style: const TextStyle(fontWeight: FontWeight.w900, height: 1.08),
        ),
      ),
    );
  }
}

class DashboardPage extends StatelessWidget {
  const DashboardPage({
    super.key,
    required this.store,
    required this.jumpToSale,
  });

  final BalcaoStore store;
  final VoidCallback jumpToSale;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(14),
      children: [
        _HeaderCard(
          title: 'Operacao agora',
          subtitle:
              '${store.openOrders.length} abertas | ${store.lowStockCount} estoque critico | sync ${store.lastSync.isEmpty ? 'pendente' : store.lastSync}',
          trailing: IconButton.filled(
            onPressed: jumpToSale,
            icon: const Icon(Icons.add_rounded),
          ),
        ),
        const SizedBox(height: 12),
        GridView.count(
          crossAxisCount: 2,
          shrinkWrap: true,
          childAspectRatio: 1.45,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 10,
          crossAxisSpacing: 10,
          children: [
            _MetricCard(
              label: 'Em aberto',
              value: money(store.openTotal),
              accent: _navy,
            ),
            _MetricCard(
              label: 'Vendas hoje',
              value: money(store.soldToday),
              accent: _teal,
            ),
            _MetricCard(
              label: 'Lucro bruto',
              value: money(store.grossProfit),
              accent: _warn,
            ),
            _MetricCard(
              label: 'Repasse iFood',
              value: money(store.ifoodRepasse),
              accent: Colors.red,
            ),
          ],
        ),
        const SizedBox(height: 12),
        _Panel(
          title: 'Comandas em movimento',
          icon: Icons.table_restaurant_rounded,
          child: Column(
            children: store.openOrders
                .take(5)
                .map((order) => _OrderTile(store: store, order: order))
                .toList(),
          ),
        ),
        const SizedBox(height: 12),
        _Panel(
          title: 'Acoes rapidas',
          icon: Icons.flash_on_rounded,
          child: Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _ActionChipButton(
                label: 'Nova mesa',
                icon: Icons.add_business_rounded,
                onTap: () => store.openOrder(OrderKind.table),
              ),
              _ActionChipButton(
                label: 'Novo delivery',
                icon: Icons.delivery_dining_rounded,
                onTap: () => store.openOrder(
                  OrderKind.delivery,
                  customer: 'Cliente delivery',
                ),
              ),
              _ActionChipButton(
                label: 'Pedido iFood',
                icon: Icons.restaurant_rounded,
                onTap: store.simulateIfoodOrder,
              ),
              _ActionChipButton(
                label: store.cashOpen ? 'Fechar caixa' : 'Abrir caixa',
                icon: Icons.point_of_sale_rounded,
                onTap: store.toggleCash,
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class SalePage extends StatelessWidget {
  const SalePage({super.key, required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    final order = store.selectedOrder;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(14, 14, 14, 8),
          child: _SearchBox(onChanged: store.setSearch),
        ),
        SizedBox(
          height: 72,
          child: ListView.separated(
            padding: const EdgeInsets.symmetric(horizontal: 14),
            scrollDirection: Axis.horizontal,
            itemBuilder: (context, index) {
              final current = store.openOrders[index];
              return ChoiceChip(
                selected: current.id == order?.id,
                onSelected: (_) => store.selectOrder(current.id),
                label: Text('${current.number} ${current.customerName}'.trim()),
                avatar: Icon(_kindIcon(current.kind), size: 18),
              );
            },
            separatorBuilder: (context, index) => const SizedBox(width: 8),
            itemCount: store.openOrders.length,
          ),
        ),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(14),
            children: [
              if (order != null) _TicketCard(store: store, order: order),
              const SizedBox(height: 12),
              _Panel(
                title: 'Produtos disponiveis',
                icon: Icons.fastfood_rounded,
                child: Column(
                  children: store
                      .filteredProducts()
                      .map(
                        (product) =>
                            _ProductAddTile(store: store, product: product),
                      )
                      .toList(),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class DeliveryPage extends StatelessWidget {
  const DeliveryPage({super.key, required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    final deliveryOrders = store.orders
        .where(
          (order) =>
              order.kind == OrderKind.delivery || order.kind == OrderKind.ifood,
        )
        .toList();
    return ListView(
      padding: const EdgeInsets.all(14),
      children: [
        _HeaderCard(
          title: 'Delivery e iFood',
          subtitle: 'Pedidos do cardapio, WhatsApp e iFood em uma fila.',
          trailing: IconButton.filled(
            onPressed: () => store.openOrder(
              OrderKind.delivery,
              customer: 'Novo cliente',
              address: 'Endereco delivery',
            ),
            icon: const Icon(Icons.add_rounded),
          ),
        ),
        const SizedBox(height: 12),
        _InfoStrip(
          icon: Icons.sync_rounded,
          title: 'Tempo real',
          text:
              'No app final, pedidos chegam pelo backend. Neste teste web, o botao simula importacao.',
          action: TextButton(
            onPressed: store.simulateIfoodOrder,
            child: const Text('Simular iFood'),
          ),
        ),
        const SizedBox(height: 12),
        ...deliveryOrders.map(
          (order) => _DeliveryCard(store: store, order: order),
        ),
      ],
    );
  }
}

class ProductsPage extends StatefulWidget {
  const ProductsPage({super.key, required this.store});

  final BalcaoStore store;

  @override
  State<ProductsPage> createState() => _ProductsPageState();
}

class _ProductsPageState extends State<ProductsPage> {
  final name = TextEditingController();
  final code = TextEditingController();
  final price = TextEditingController();
  final cost = TextEditingController();
  final stock = TextEditingController();
  String category = 'LANCHES';

  @override
  void dispose() {
    name.dispose();
    code.dispose();
    price.dispose();
    cost.dispose();
    stock.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(14),
      children: [
        _HeaderCard(
          title: 'Produtos e estoque',
          subtitle:
              '${widget.store.products.length} itens cadastrados | ${widget.store.lowStockCount} abaixo do minimo',
        ),
        const SizedBox(height: 12),
        _Panel(
          title: 'Novo produto',
          icon: Icons.add_box_rounded,
          child: Column(
            children: [
              _Input(
                label: 'Nome',
                controller: name,
                icon: Icons.fastfood_rounded,
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(
                    child: _Input(
                      label: 'Codigo',
                      controller: code,
                      icon: Icons.qr_code_rounded,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: DropdownButtonFormField<String>(
                      initialValue: category,
                      isExpanded: true,
                      borderRadius: BorderRadius.circular(8),
                      dropdownColor: Colors.white,
                      icon: const Icon(
                        Icons.keyboard_arrow_down_rounded,
                        color: _navy2,
                      ),
                      style: const TextStyle(
                        color: _navy,
                        fontWeight: FontWeight.w800,
                        fontSize: 14,
                      ),
                      items: widget.store.categories
                          .map(
                            (item) => DropdownMenuItem(
                              value: item,
                              child: Text(item),
                            ),
                          )
                          .toList(),
                      onChanged: (value) =>
                          setState(() => category = value ?? category),
                      decoration: _fieldDecoration(
                        'Categoria',
                        Icons.category_rounded,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(
                    child: _Input(
                      label: 'Preco',
                      controller: price,
                      icon: Icons.sell_rounded,
                      keyboardType: TextInputType.number,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: _Input(
                      label: 'Custo',
                      controller: cost,
                      icon: Icons.price_check_rounded,
                      keyboardType: TextInputType.number,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              _Input(
                label: 'Estoque inicial',
                controller: stock,
                icon: Icons.inventory_rounded,
                keyboardType: TextInputType.number,
              ),
              const SizedBox(height: 12),
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: _save,
                  icon: const Icon(Icons.save_rounded),
                  label: const Text('Salvar produto'),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        _Panel(
          title: 'Catalogo',
          icon: Icons.list_alt_rounded,
          child: Column(
            children: widget.store.products
                .map(
                  (product) =>
                      _ProductStockTile(store: widget.store, product: product),
                )
                .toList(),
          ),
        ),
      ],
    );
  }

  Future<void> _save() async {
    if (name.text.trim().isEmpty) return;
    await widget.store.saveProduct(
      name: name.text.trim().toUpperCase(),
      code: code.text.trim().isEmpty
          ? DateTime.now().millisecondsSinceEpoch.toString().substring(7)
          : code.text.trim(),
      category: category,
      price: _parse(price.text),
      cost: _parse(cost.text),
      stock: _parse(stock.text).round(),
      minStock: 3,
    );
    name.clear();
    code.clear();
    price.clear();
    cost.clear();
    stock.clear();
  }
}

class CustomersPage extends StatefulWidget {
  const CustomersPage({super.key, required this.store});

  final BalcaoStore store;

  @override
  State<CustomersPage> createState() => _CustomersPageState();
}

class _CustomersPageState extends State<CustomersPage> {
  final name = TextEditingController();
  final phone = TextEditingController();
  final address = TextEditingController();

  @override
  void dispose() {
    name.dispose();
    phone.dispose();
    address.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final missing = widget.store.customers
        .where((customer) => customer.missing)
        .length;
    return ListView(
      padding: const EdgeInsets.all(14),
      children: [
        _HeaderCard(
          title: 'Clientes e fidelidade',
          subtitle:
              '${widget.store.customers.length} clientes | $missing para retorno WhatsApp',
        ),
        const SizedBox(height: 12),
        _Panel(
          title: 'Cadastrar cliente',
          icon: Icons.person_add_alt_1_rounded,
          child: Column(
            children: [
              _Input(
                label: 'Nome',
                controller: name,
                icon: Icons.person_rounded,
              ),
              const SizedBox(height: 10),
              _Input(
                label: 'Telefone',
                controller: phone,
                icon: Icons.phone_rounded,
                keyboardType: TextInputType.phone,
              ),
              const SizedBox(height: 10),
              _Input(
                label: 'Endereco',
                controller: address,
                icon: Icons.location_on_rounded,
              ),
              const SizedBox(height: 12),
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: () async {
                    if (name.text.trim().isEmpty) return;
                    await widget.store.saveCustomer(
                      name.text.trim(),
                      phone.text.trim(),
                      address.text.trim(),
                    );
                    name.clear();
                    phone.clear();
                    address.clear();
                  },
                  icon: const Icon(Icons.save_rounded),
                  label: const Text('Salvar cliente'),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        _Panel(
          title: 'CRM WhatsApp',
          icon: Icons.mark_chat_unread_rounded,
          child: Column(
            children: widget.store.customers
                .map((customer) => _CustomerTile(customer: customer))
                .toList(),
          ),
        ),
      ],
    );
  }
}

class ReportsPage extends StatelessWidget {
  const ReportsPage({super.key, required this.store});

  final BalcaoStore store;

  @override
  Widget build(BuildContext context) {
    final closed = store.closedOrders;
    return ListView(
      padding: const EdgeInsets.all(14),
      children: [
        _HeaderCard(
          title: 'Relatorios e BI',
          subtitle: 'Caixa, vendas, repasse iFood, estoque e margem.',
        ),
        const SizedBox(height: 12),
        GridView.count(
          crossAxisCount: 2,
          shrinkWrap: true,
          childAspectRatio: 1.38,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 10,
          crossAxisSpacing: 10,
          children: [
            _MetricCard(
              label: 'Receita',
              value: money(store.soldToday),
              accent: _teal,
            ),
            _MetricCard(
              label: 'Lucro bruto',
              value: money(store.grossProfit),
              accent: _warn,
            ),
            _MetricCard(
              label: 'iFood vendas',
              value: money(store.ifoodSales),
              accent: Colors.red,
            ),
            _MetricCard(
              label: 'iFood repasse',
              value: money(store.ifoodRepasse),
              accent: Colors.red,
            ),
          ],
        ),
        const SizedBox(height: 12),
        _Panel(
          title: 'Vendas fechadas',
          icon: Icons.payments_rounded,
          child: Column(
            children: closed.isEmpty
                ? [const _Empty(text: 'Feche uma comanda para aparecer aqui.')]
                : closed
                      .map(
                        (order) => _OrderTile(
                          store: store,
                          order: order,
                          compact: true,
                        ),
                      )
                      .toList(),
          ),
        ),
        const SizedBox(height: 12),
        _Panel(
          title: 'Estoque critico',
          icon: Icons.warning_amber_rounded,
          child: Column(
            children: store.products
                .where((product) => product.stock <= product.minStock)
                .map((product) => _StockAlert(product: product))
                .toList(),
          ),
        ),
      ],
    );
  }
}

class SettingsPage extends StatefulWidget {
  const SettingsPage({super.key, required this.store});

  final BalcaoStore store;

  @override
  State<SettingsPage> createState() => _SettingsPageState();
}

class _SettingsPageState extends State<SettingsPage> {
  late final TextEditingController business = TextEditingController(
    text: widget.store.businessName,
  );

  @override
  void dispose() {
    business.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(14),
      children: [
        _HeaderCard(
          title: 'Configuracoes',
          subtitle: 'Conta, sincronizacao, NFC-e, Mercado Pago e impressao.',
        ),
        const SizedBox(height: 12),
        _Panel(
          title: 'Loja',
          icon: Icons.storefront_rounded,
          child: Column(
            children: [
              _Input(
                label: 'Nome da loja',
                controller: business,
                icon: Icons.store_rounded,
              ),
              const SizedBox(height: 12),
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: () =>
                      widget.store.updateBusinessName(business.text),
                  icon: const Icon(Icons.save_rounded),
                  label: const Text('Salvar loja'),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        _Panel(
          title: 'Operacao conectada',
          icon: Icons.hub_rounded,
          child: Column(
            children: [
              _InfoStrip(
                icon: Icons.receipt_rounded,
                title: 'NFC-e',
                text:
                    'Modulo fiscal fica isolado no backend/Windows para o caixa nao travar.',
              ),
              const SizedBox(height: 10),
              _InfoStrip(
                icon: Icons.credit_card_rounded,
                title: 'Mercado Pago Point',
                text:
                    'Fluxo mobile preparado para enviar cobranca e consultar status.',
              ),
              const SizedBox(height: 10),
              _InfoStrip(
                icon: Icons.restaurant_menu_rounded,
                title: 'iFood e cardapio',
                text:
                    'Pedidos entram na fila local e sincronizam quando o backend estiver ativo.',
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                onPressed: widget.store.resetDemo,
                icon: const Icon(Icons.refresh_rounded),
                label: const Text('Resetar teste'),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: FilledButton.icon(
                onPressed: widget.store.logout,
                icon: const Icon(Icons.logout_rounded),
                label: const Text('Sair'),
                style: FilledButton.styleFrom(backgroundColor: _danger),
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _TicketCard extends StatelessWidget {
  const _TicketCard({required this.store, required this.order});

  final BalcaoStore store;
  final Order order;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      title: '${kindLabel(order.kind)} ${order.number}',
      icon: _kindIcon(order.kind),
      trailing: _StatusPill(
        text: statusLabel(order.status),
        color: order.isOpen ? _teal : _navy,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (order.customerName.isNotEmpty)
            Text(
              order.customerName,
              style: const TextStyle(fontWeight: FontWeight.w800),
            ),
          const SizedBox(height: 10),
          if (order.items.isEmpty)
            const _Empty(text: 'Selecione produtos abaixo.'),
          ...order.items.map(
            (item) => Container(
              margin: const EdgeInsets.only(bottom: 8),
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: _paper,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: _line),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          item.name,
                          softWrap: true,
                          style: const TextStyle(fontWeight: FontWeight.w900),
                        ),
                        Text(
                          '${item.code} | ${money(item.price)}',
                          style: const TextStyle(color: _textSecondary),
                        ),
                      ],
                    ),
                  ),
                  IconButton.filledTonal(
                    onPressed: () => store.changeQty(item, -1),
                    icon: const Icon(Icons.remove_rounded),
                  ),
                  SizedBox(
                    width: 28,
                    child: Center(
                      child: Text(
                        '${item.quantity}',
                        style: const TextStyle(fontWeight: FontWeight.w900),
                      ),
                    ),
                  ),
                  IconButton.filledTonal(
                    onPressed: () => store.changeQty(item, 1),
                    icon: const Icon(Icons.add_rounded),
                  ),
                ],
              ),
            ),
          ),
          const Divider(height: 24),
          Row(
            children: [
              Expanded(
                child: Text(
                  'Total ${money(order.subtotal)}',
                  style: const TextStyle(
                    fontSize: 24,
                    fontWeight: FontWeight.w900,
                    color: _navy,
                  ),
                ),
              ),
              Text(
                '${order.itemsCount} itens',
                style: const TextStyle(color: _textSecondary),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: store.paymentMethods
                .map(
                  (method) => _ActionChipButton(
                    label: method,
                    icon: Icons.payments_rounded,
                    onTap: () => store.isMercadoPagoMethod(method)
                        ? store.sendSelectedToPoint(method)
                        : store.closeSelected(method),
                  ),
                )
                .toList(),
          ),
        ],
      ),
    );
  }
}

class _OrderTile extends StatelessWidget {
  const _OrderTile({
    required this.store,
    required this.order,
    this.compact = false,
  });

  final BalcaoStore store;
  final Order order;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: order.isOpen ? () => store.selectOrder(order.id) : null,
      borderRadius: BorderRadius.circular(14),
      child: Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: _paper,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: _line),
        ),
        child: Row(
          children: [
            CircleAvatar(
              radius: 18,
              backgroundColor: _mint,
              child: Icon(_kindIcon(order.kind), color: _navy, size: 18),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '${order.number} ${order.customerName}'.trim(),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.w900),
                  ),
                  if (!compact)
                    Text(
                      '${kindLabel(order.kind)} | ${statusLabel(order.status)} | ${order.itemsCount} itens',
                      style: const TextStyle(
                        color: _textSecondary,
                        fontSize: 12,
                      ),
                    ),
                ],
              ),
            ),
            Text(
              money(order.subtotal),
              style: const TextStyle(fontWeight: FontWeight.w900),
            ),
          ],
        ),
      ),
    );
  }
}

class _DeliveryCard extends StatelessWidget {
  const _DeliveryCard({required this.store, required this.order});

  final BalcaoStore store;
  final Order order;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      title: '${kindLabel(order.kind)} ${order.number}',
      icon: _kindIcon(order.kind),
      trailing: _StatusPill(
        text: statusLabel(order.status),
        color: order.kind == OrderKind.ifood ? Colors.red : _teal,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            order.customerName,
            style: const TextStyle(fontWeight: FontWeight.w900),
          ),
          if (order.address.isNotEmpty)
            Text(order.address, style: const TextStyle(color: _textSecondary)),
          const SizedBox(height: 10),
          Text(
            'Total ${money(order.subtotal)} | Repasse ${money(order.ifoodRepasse)}',
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 8,
            children: [
              _ActionChipButton(
                label: 'Preparo',
                icon: Icons.soup_kitchen_rounded,
                onTap: () =>
                    store.updateOrderStatus(order, OrderStatus.preparing),
              ),
              _ActionChipButton(
                label: 'Saiu',
                icon: Icons.delivery_dining_rounded,
                onTap: () =>
                    store.updateOrderStatus(order, OrderStatus.dispatched),
              ),
              _ActionChipButton(
                label: 'Entregue',
                icon: Icons.check_rounded,
                onTap: () =>
                    store.updateOrderStatus(order, OrderStatus.delivered),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ProductAddTile extends StatelessWidget {
  const _ProductAddTile({required this.store, required this.product});

  final BalcaoStore store;
  final Product product;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: CircleAvatar(
        backgroundColor: _mint,
        child: Text(
          product.category.characters.first,
          style: const TextStyle(color: _navy, fontWeight: FontWeight.w900),
        ),
      ),
      title: Text(
        product.name,
        softWrap: true,
        style: const TextStyle(fontWeight: FontWeight.w900),
      ),
      subtitle: Text(
        '${product.code} | ${product.category} | estoque ${product.stock}',
      ),
      trailing: FilledButton.tonal(
        onPressed: store.cashOpen ? () => store.addProduct(product) : null,
        child: Text(money(product.price)),
      ),
    );
  }
}

class _ProductStockTile extends StatelessWidget {
  const _ProductStockTile({required this.store, required this.product});

  final BalcaoStore store;
  final Product product;

  @override
  Widget build(BuildContext context) {
    final critical = product.stock <= product.minStock;
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: critical ? const Color(0xFFFFF1F1) : _paper,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: critical ? const Color(0xFFFFC9C9) : _line),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  product.name,
                  softWrap: true,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
                Text(
                  '${money(product.price)} | custo ${money(product.cost)} | margem ${product.margin.toStringAsFixed(0)}%',
                ),
                Text(
                  'Estoque ${product.stock} | minimo ${product.minStock}',
                  style: TextStyle(color: critical ? _danger : _textSecondary),
                ),
              ],
            ),
          ),
          IconButton.filledTonal(
            onPressed: () => store.adjustStock(product, product.stock - 1),
            icon: const Icon(Icons.remove_rounded),
          ),
          IconButton.filledTonal(
            onPressed: () => store.adjustStock(product, product.stock + 1),
            icon: const Icon(Icons.add_rounded),
          ),
        ],
      ),
    );
  }
}

class _CustomerTile extends StatelessWidget {
  const _CustomerTile({required this.customer});

  final Customer customer;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: _paper,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: _line),
      ),
      child: Row(
        children: [
          CircleAvatar(
            backgroundColor: customer.missing ? const Color(0xFFFFE7C7) : _mint,
            child: Icon(
              customer.missing ? Icons.campaign_rounded : Icons.person_rounded,
              color: customer.missing ? _warn : _navy,
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  customer.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
                Text(
                  '${customer.phone} | ${customer.points} pts | cashback ${money(customer.cashback)}',
                  style: const TextStyle(color: _textSecondary, fontSize: 12),
                ),
              ],
            ),
          ),
          if (customer.missing)
            const _StatusPill(text: 'retorno', color: _warn),
        ],
      ),
    );
  }
}

class _StockAlert extends StatelessWidget {
  const _StockAlert({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: const Icon(Icons.warning_rounded, color: _warn),
      title: Text(
        product.name,
        style: const TextStyle(fontWeight: FontWeight.w900),
      ),
      subtitle: Text('Atual ${product.stock} | minimo ${product.minStock}'),
      trailing: Text(
        money(product.price),
        style: const TextStyle(fontWeight: FontWeight.w900),
      ),
    );
  }
}

class _Panel extends StatelessWidget {
  const _Panel({
    required this.title,
    required this.icon,
    required this.child,
    this.trailing,
  });

  final String title;
  final IconData icon;
  final Widget child;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: _line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 18,
                backgroundColor: _mint,
                child: Icon(icon, size: 18, color: _navy),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  title,
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w900,
                    color: _navy,
                  ),
                ),
              ),
              ?trailing,
            ],
          ),
          const SizedBox(height: 12),
          child,
        ],
      ),
    );
  }
}

class _HeaderCard extends StatelessWidget {
  const _HeaderCard({
    required this.title,
    required this.subtitle,
    this.trailing,
  });

  final String title;
  final String subtitle;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: const LinearGradient(colors: [_navy, _navy2]),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 22,
                    fontWeight: FontWeight.w900,
                    color: Colors.white,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  subtitle,
                  style: TextStyle(
                    color: Colors.white.withValues(alpha: .76),
                    height: 1.3,
                  ),
                ),
              ],
            ),
          ),
          ?trailing,
        ],
      ),
    );
  }
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({
    required this.label,
    required this.value,
    required this.accent,
  });

  final String label;
  final String value;
  final Color accent;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border(
          top: BorderSide(color: accent, width: 4),
          left: const BorderSide(color: _line),
          right: const BorderSide(color: _line),
          bottom: const BorderSide(color: _line),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: const TextStyle(
              color: _textSecondary,
              fontWeight: FontWeight.w800,
            ),
          ),
          FittedBox(
            child: Text(
              value,
              style: const TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.w900,
                color: _navy,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _InfoStrip extends StatelessWidget {
  const _InfoStrip({
    required this.icon,
    required this.title,
    required this.text,
    this.action,
  });

  final IconData icon;
  final String title;
  final String text;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: _mint,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFB7E8DF)),
      ),
      child: Row(
        children: [
          Icon(icon, color: _teal),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
                Text(
                  text,
                  style: const TextStyle(color: _textSecondary, height: 1.25),
                ),
              ],
            ),
          ),
          ?action,
        ],
      ),
    );
  }
}

class _SearchBox extends StatelessWidget {
  const _SearchBox({required this.onChanged});

  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return TextField(
      onChanged: onChanged,
      decoration: _fieldDecoration(
        'Produto, codigo ou categoria',
        Icons.search_rounded,
      ),
    );
  }
}

class _Input extends StatelessWidget {
  const _Input({
    required this.label,
    required this.controller,
    required this.icon,
    this.keyboardType,
  });

  final String label;
  final TextEditingController controller;
  final IconData icon;
  final TextInputType? keyboardType;

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      keyboardType: keyboardType,
      decoration: _fieldDecoration(label, icon),
    );
  }
}

InputDecoration _fieldDecoration(String label, IconData icon) {
  return InputDecoration(
    labelText: label,
    prefixIcon: Icon(icon, color: _navy2, size: 20),
    filled: true,
    fillColor: _surface,
    labelStyle: const TextStyle(
      color: _textSecondary,
      fontWeight: FontWeight.w800,
    ),
    floatingLabelStyle: const TextStyle(
      color: _navy2,
      fontWeight: FontWeight.w900,
    ),
    border: OutlineInputBorder(
      borderRadius: BorderRadius.circular(8),
      borderSide: const BorderSide(color: _line),
    ),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(8),
      borderSide: const BorderSide(color: _line),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(8),
      borderSide: const BorderSide(color: _navy2, width: 1.6),
    ),
  );
}

class _ActionChipButton extends StatelessWidget {
  const _ActionChipButton({
    required this.label,
    required this.icon,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return ActionChip(
      avatar: Icon(icon, size: 18),
      label: Text(label, style: const TextStyle(fontWeight: FontWeight.w800)),
      onPressed: onTap,
      backgroundColor: Colors.white,
      side: const BorderSide(color: _line),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
    );
  }
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.text, required this.color});

  final String text;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .12),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        text,
        style: TextStyle(
          color: color,
          fontWeight: FontWeight.w900,
          fontSize: 12,
        ),
      ),
    );
  }
}

class _LogoBlock extends StatelessWidget {
  const _LogoBlock({required this.size});

  final double size;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: _blue,
        borderRadius: BorderRadius.circular(size * .28),
        boxShadow: size >= 50
            ? const [
                BoxShadow(
                  color: Color(0x2E1267F3),
                  blurRadius: 24,
                  offset: Offset(0, 12),
                ),
              ]
            : null,
      ),
      child: Center(
        child: Text(
          'BL',
          style: TextStyle(
            color: Colors.white,
            fontWeight: FontWeight.w900,
            fontSize: size * .34,
          ),
        ),
      ),
    );
  }
}

class _Empty extends StatelessWidget {
  const _Empty({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: _paper,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: _line),
      ),
      child: Text(
        text,
        textAlign: TextAlign.center,
        style: const TextStyle(color: _textSecondary),
      ),
    );
  }
}

IconData _kindIcon(OrderKind kind) => switch (kind) {
  OrderKind.table => Icons.table_restaurant_rounded,
  OrderKind.counter => Icons.point_of_sale_rounded,
  OrderKind.delivery => Icons.delivery_dining_rounded,
  OrderKind.ifood => Icons.restaurant_rounded,
};

String _shortBoardNumber(String value) {
  final digits = RegExp(r'\d+').allMatches(value).map((m) => m.group(0)).join();
  if (digits.isEmpty) return value;
  final parsed = int.tryParse(digits);
  return (parsed ?? 0).toString().padLeft(2, '0');
}

double _parse(String value) {
  final text = value.trim();
  if (text.contains(',')) {
    return double.tryParse(text.replaceAll('.', '').replaceAll(',', '.')) ?? 0;
  }
  return double.tryParse(text) ?? 0;
}
