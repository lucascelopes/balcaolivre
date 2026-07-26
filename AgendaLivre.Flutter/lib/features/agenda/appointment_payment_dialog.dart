import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../domain/models/models.dart';
import '../../services/mercado_pago_service.dart';
import '../payments/mercado_pago_terminal_visuals.dart';
import 'appointment_visuals.dart';

Future<void> showAppointmentPaymentDialog(
  BuildContext context,
  AgendaController controller,
  Appointment appointment, {
  VoidCallback? onEdit,
  bool includeProductLines = false,
}) {
  return showGeneralDialog<void>(
    context: context,
    barrierDismissible: true,
    barrierLabel: 'Fechar cobrança',
    barrierColor: Colors.black.withValues(alpha: 0.38),
    transitionDuration: const Duration(milliseconds: 160),
    pageBuilder: (context, animation, secondaryAnimation) => SafeArea(
      child: Center(
        child: Material(
          color: Colors.transparent,
          child: _AppointmentPaymentDialog(
            controller: controller,
            appointment: appointment,
            onEdit: onEdit,
            includeProductLines: includeProductLines,
          ),
        ),
      ),
    ),
    transitionBuilder: (context, animation, secondaryAnimation, child) {
      final curved = CurvedAnimation(
        parent: animation,
        curve: Curves.easeOutCubic,
      );
      return FadeTransition(
        opacity: curved,
        child: ScaleTransition(
          scale: Tween<double>(begin: 0.97, end: 1).animate(curved),
          child: child,
        ),
      );
    },
  );
}

enum _AppointmentChargeKind { pixKey, pixMercadoPago, debit, credit, account }

class _AppointmentChargeOption {
  const _AppointmentChargeOption(
    this.kind,
    this.label,
    this.actionText, {
    this.supportingText,
  });

  final _AppointmentChargeKind kind;
  final String label;
  final String actionText;
  final String? supportingText;
}

class _AppointmentPaymentDialog extends StatefulWidget {
  const _AppointmentPaymentDialog({
    required this.controller,
    required this.appointment,
    required this.includeProductLines,
    this.onEdit,
  });

  final AgendaController controller;
  final Appointment appointment;
  final bool includeProductLines;
  final VoidCallback? onEdit;

  @override
  State<_AppointmentPaymentDialog> createState() =>
      _AppointmentPaymentDialogState();
}

class _AppointmentPaymentDialogState extends State<_AppointmentPaymentDialog> {
  late final List<_AppointmentChargeOption> _options;
  late final Map<_AppointmentChargeKind, String> _remoteChargeReferences;
  late final FixedExtentScrollController _wheelController;
  int _selectedIndex = 1;
  bool _busy = false;

  AgendaController get controller => widget.controller;
  Appointment get appointment => widget.appointment;
  double get _amount => widget.includeProductLines
      ? controller.pdvAppointmentTotal(appointment)
      : appointment.price;

  @override
  void initState() {
    super.initState();
    final settings = controller.data.settings;
    final remotePix =
        settings.mercadoPagoEnabled &&
        settings.mercadoPagoConnected &&
        controller.mercadoPagoService != null;
    _options = <_AppointmentChargeOption>[
      _AppointmentChargeOption(
        remotePix
            ? _AppointmentChargeKind.pixMercadoPago
            : _AppointmentChargeKind.pixKey,
        'Pix',
        remotePix ? 'Gerar Pix' : 'Mostrar chave Pix',
      ),
      const _AppointmentChargeOption(
        _AppointmentChargeKind.debit,
        'Débito',
        'Enviar débito para a Point',
      ),
      const _AppointmentChargeOption(
        _AppointmentChargeKind.credit,
        'Crédito',
        'Enviar crédito para a Point',
      ),
      if (!widget.includeProductLines)
        const _AppointmentChargeOption(
          _AppointmentChargeKind.account,
          'Conta do cliente',
          'Adicionar à conta do cliente',
          supportingText: 'Pagar depois',
        ),
    ];
    _remoteChargeReferences = <_AppointmentChargeKind, String>{
      _AppointmentChargeKind.pixMercadoPago:
          MercadoPagoService.createLocalReference(),
      _AppointmentChargeKind.debit: MercadoPagoService.createLocalReference(),
      _AppointmentChargeKind.credit: MercadoPagoService.createLocalReference(),
    };
    _wheelController = FixedExtentScrollController(initialItem: _selectedIndex);
  }

  @override
  void dispose() {
    _wheelController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth < 580;
        final width = compact
            ? (constraints.maxWidth - 20).clamp(300.0, 540.0)
            : 540.0;
        final height = compact
            ? (constraints.maxHeight - 20).clamp(560.0, 650.0)
            : 470.0;
        final content = compact
            ? Column(
                children: [
                  SizedBox(height: 126, child: _leftPanel(t, compact: true)),
                  Expanded(child: _rightPanel(t, compact: true)),
                ],
              )
            : Row(
                children: [
                  SizedBox(width: 176, child: _leftPanel(t, compact: false)),
                  Expanded(child: _rightPanel(t, compact: false)),
                ],
              );
        return Container(
          key: const Key('appointment-payment-dialog'),
          width: width,
          height: height,
          clipBehavior: Clip.antiAlias,
          decoration: BoxDecoration(
            color: t.panel,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: t.line),
            boxShadow: const [
              BoxShadow(
                color: Color(0x221C1612),
                blurRadius: 24,
                offset: Offset(0, 5),
              ),
            ],
          ),
          child: content,
        );
      },
    );
  }

  Widget _leftPanel(AgendaThemeTokens t, {required bool compact}) {
    final status = appointmentStatusStyle(context, appointment.status);
    return Stack(
      fit: StackFit.expand,
      children: [
        Image.asset(
          'assets/branding/appointment-payment-left-panel.png',
          fit: BoxFit.fill,
          alignment: Alignment.topLeft,
        ),
        if (compact)
          Padding(
            padding: const EdgeInsets.fromLTRB(22, 15, 22, 14),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(
                        appointment.customerName.trim().isEmpty
                            ? 'Cliente'
                            : appointment.customerName.trim(),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 19,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(height: 7),
                      Text(
                        '${hour(appointment.start)} — ${hour(appointment.end)}  ·  ${appointment.durationMinutes} min',
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 14,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: status.background,
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(color: status.foreground),
                  ),
                  child: Text(
                    appointmentStatusLabel(appointment.status),
                    style: TextStyle(
                      color: status.foreground,
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ],
            ),
          )
        else
          Align(
            alignment: Alignment.centerLeft,
            child: Container(
              width: 140,
              margin: const EdgeInsets.only(left: 24, bottom: 54),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    appointment.customerName.trim().isEmpty
                        ? 'Cliente'
                        : appointment.customerName.trim(),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 20,
                      height: 1.05,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  _accentRule(t, const EdgeInsets.only(top: 14, bottom: 18)),
                  Text(
                    '${hour(appointment.start)} — ${hour(appointment.end)}',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 18.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 5),
                  Text(
                    '${appointment.durationMinutes} min',
                    style: TextStyle(color: t.ink, fontSize: 14),
                  ),
                  _accentRule(t, const EdgeInsets.only(top: 18, bottom: 18)),
                  Text(
                    appointmentStatusLabel(appointment.status),
                    style: TextStyle(
                      color: status.foreground,
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
          ),
      ],
    );
  }

  Widget _accentRule(AgendaThemeTokens t, EdgeInsets margin) =>
      Container(width: 20, height: 1, margin: margin, color: t.accent);

  Widget _rightPanel(AgendaThemeTokens t, {required bool compact}) {
    return Padding(
      padding: EdgeInsets.fromLTRB(
        compact ? 16 : 20,
        compact ? 10 : 16,
        compact ? 16 : 20,
        compact ? 12 : 16,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            height: compact ? 30 : 26,
            child: Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: _busy ? null : _edit,
                  style: TextButton.styleFrom(
                    minimumSize: Size.zero,
                    padding: const EdgeInsets.symmetric(horizontal: 5),
                    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  ),
                  child: Text(
                    'Editar atendimento',
                    style: TextStyle(
                      color: t.accentDark,
                      fontSize: 11.5,
                      decoration: TextDecoration.underline,
                    ),
                  ),
                ),
                const SizedBox(width: 6),
                IconButton(
                  tooltip: 'Fechar cobrança',
                  onPressed: _busy ? null : () => Navigator.of(context).pop(),
                  padding: EdgeInsets.zero,
                  constraints: const BoxConstraints.tightFor(
                    width: 24,
                    height: 24,
                  ),
                  icon: const Icon(Icons.close_rounded, size: 18),
                ),
              ],
            ),
          ),
          SizedBox(
            height: compact ? 198 : 210,
            child: Column(
              children: [
                _detailRow(t, '01', 'Serviço', _serviceName, height: 44),
                _detailRow(t, '02', 'Cliente', _customerName, height: 44),
                _detailRow(t, '03', 'Local', _resourceName, height: 44),
                _detailRow(
                  t,
                  '04',
                  'A receber',
                  money(_amount),
                  height: compact ? 66 : 78,
                  emphasize: true,
                ),
              ],
            ),
          ),
          SizedBox(
            height: 26,
            child: Align(
              alignment: Alignment.centerLeft,
              child: Text(
                'Como cobrar?',
                style: TextStyle(
                  color: t.ink,
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
          SizedBox(
            key: const Key('appointment-payment-wheel'),
            height: 112,
            child: ListWheelScrollView.useDelegate(
              controller: _wheelController,
              itemExtent: 32,
              diameterRatio: 2.1,
              perspective: 0.003,
              squeeze: 1,
              physics: const FixedExtentScrollPhysics(),
              onSelectedItemChanged: (index) {
                final selected = index % _options.length;
                if (selected != _selectedIndex) {
                  setState(() => _selectedIndex = selected);
                }
              },
              childDelegate: ListWheelChildLoopingListDelegate(
                children: [
                  for (var index = 0; index < _options.length; index++)
                    _wheelOption(t, index),
                ],
              ),
            ),
          ),
          SizedBox(
            height: 20,
            child: Center(
              child: Text(
                '${_selectedIndex + 1} de ${_options.length}  •  Role para cima ou para baixo',
                style: TextStyle(color: t.muted, fontSize: 9.5),
              ),
            ),
          ),
          SizedBox(
            height: 42,
            child: FilledButton(
              key: const Key('appointment-payment-action'),
              onPressed: _busy ? null : _execute,
              style: FilledButton.styleFrom(
                backgroundColor: t.accent,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              child: Text(
                _busy ? 'Aguarde...' : _options[_selectedIndex].actionText,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _wheelOption(AgendaThemeTokens t, int index) {
    final option = _options[index];
    final selected = index == _selectedIndex;
    return GestureDetector(
      key: Key('appointment-payment-option-${option.kind.name}'),
      behavior: HitTestBehavior.opaque,
      onTap: () => _wheelController.animateToItem(
        index,
        duration: const Duration(milliseconds: 190),
        curve: Curves.easeOutCubic,
      ),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 160),
        width: double.infinity,
        decoration: BoxDecoration(
          border: Border(
            bottom: BorderSide(
              color: selected ? t.accent : t.line,
              width: selected ? 2 : 1,
            ),
          ),
        ),
        child: FittedBox(
          fit: BoxFit.scaleDown,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                option.label,
                style: TextStyle(
                  color: selected ? t.ink : t.muted,
                  fontSize: selected ? 17 : 12.5,
                  height: 1,
                  fontWeight: selected ? FontWeight.w700 : FontWeight.w400,
                ),
              ),
              if (option.supportingText != null)
                Text(
                  option.supportingText!,
                  style: TextStyle(color: t.muted, fontSize: 8.5, height: 0.9),
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _detailRow(
    AgendaThemeTokens t,
    String number,
    String label,
    String value, {
    required double height,
    bool emphasize = false,
  }) {
    return SizedBox(
      height: height,
      child: DecoratedBox(
        decoration: BoxDecoration(
          border: Border(bottom: BorderSide(color: t.line)),
        ),
        child: Row(
          children: [
            SizedBox(
              width: 32,
              child: Align(
                alignment: emphasize ? Alignment.topLeft : Alignment.centerLeft,
                child: Padding(
                  padding: EdgeInsets.only(top: emphasize ? 13 : 0),
                  child: Text(
                    number,
                    style: TextStyle(
                      color: t.accentDark,
                      fontSize: 11,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ),
            ),
            Container(width: 1, height: emphasize ? 46 : 30, color: t.line),
            const SizedBox(width: 10),
            if (emphasize)
              Expanded(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      label,
                      style: TextStyle(color: t.muted, fontSize: 10.5),
                    ),
                    Text(
                      value,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 27,
                        height: 1.05,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
              )
            else ...[
              SizedBox(
                width: 68,
                child: Text(
                  label,
                  style: TextStyle(color: t.muted, fontSize: 10.5),
                ),
              ),
              Expanded(
                child: Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  String get _serviceName => appointment.serviceName.trim().isEmpty
      ? 'Atendimento'
      : appointment.serviceName.trim();
  String get _customerName => appointment.customerName.trim().isEmpty
      ? 'Cliente'
      : appointment.customerName.trim();
  String get _resourceName => appointment.resourceName.trim().isEmpty
      ? 'Não informado'
      : appointment.resourceName.trim();

  void _edit() {
    Navigator.of(context).pop();
    widget.onEdit?.call();
  }

  Future<String?> _confirmPayment({
    required String paymentMethod,
    String paymentProvider = 'Manual',
    String paymentReference = '',
    String paymentStatus = 'approved',
    DateTime? confirmedAt,
  }) {
    if (widget.includeProductLines) {
      return controller.confirmPdvAppointmentPayment(
        appointment,
        paymentMethod: paymentMethod,
        paymentProvider: paymentProvider,
        paymentReference: paymentReference,
        paymentStatus: paymentStatus,
        confirmedAt: confirmedAt,
      );
    }
    return controller.confirmAppointmentPayment(
      appointment,
      paymentMethod: paymentMethod,
      paymentProvider: paymentProvider,
      paymentReference: paymentReference,
      paymentStatus: paymentStatus,
      confirmedAt: confirmedAt,
    );
  }

  Future<void> _execute() async {
    if (_busy) return;
    setState(() => _busy = true);
    try {
      final option = _options[_selectedIndex];
      if (option.kind == _AppointmentChargeKind.account) {
        final error = await controller.addAppointmentToCustomerAccount(
          appointment,
        );
        if (error != null) return _showError(error);
        _closeWithMessage(
          '${money(_amount)} adicionado à conta de $_customerName.',
        );
        return;
      }

      if (_amount <= 0) {
        final error = await _confirmPayment(
          paymentMethod: 'Sem cobrança',
          paymentStatus: 'not_required',
        );
        if (error != null) return _showError(error);
        _closeWithMessage('Atendimento finalizado sem cobrança.');
        return;
      }

      switch (option.kind) {
        case _AppointmentChargeKind.pixKey:
          await _chargeByPixKey();
        case _AppointmentChargeKind.pixMercadoPago:
          await _chargeByMercadoPagoPix();
        case _AppointmentChargeKind.debit:
          await _chargeByPoint(MercadoPagoPointMethod.debit);
        case _AppointmentChargeKind.credit:
          await _chargeByPoint(MercadoPagoPointMethod.credit);
        case _AppointmentChargeKind.account:
          break;
      }
    } on MercadoPagoException catch (error) {
      _showError(error.message);
    } on Object catch (error) {
      _showError('Não foi possível concluir a cobrança: $error');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _chargeByPixKey() async {
    final key = controller.data.settings.pixKey.trim();
    if (key.isEmpty) {
      _showError('Cadastre uma chave Pix nas configurações de pagamento.');
      return;
    }
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Receber por Pix'),
        content: SelectableText(
          'Chave Pix do estabelecimento:\n\n$key\n\nValor: ${money(_amount)}\n\nConfirme somente depois que o valor aparecer na conta.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Ainda não recebi'),
          ),
          FilledButton(
            key: const Key('confirm-pix-key-payment'),
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Pagamento recebido'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    final now = DateTime.now();
    final error = await _confirmPayment(
      paymentMethod: 'Pix por chave',
      paymentProvider: 'Chave Pix',
      paymentReference: 'pix_key_${now.millisecondsSinceEpoch}',
      paymentStatus: 'approved',
      confirmedAt: now,
    );
    if (error != null) return _showError(error);
    _closeWithMessage('Pagamento Pix confirmado.');
  }

  Future<void> _chargeByMercadoPagoPix() async {
    final service = controller.mercadoPagoService;
    if (service == null) {
      _showError(
        'O serviço Mercado Pago não está disponível neste dispositivo.',
      );
      return;
    }
    final charge = await service.createPixCharge(
      MercadoPagoPixChargeRequest(
        amountInCents: (_amount * 100).round(),
        localReference:
            _remoteChargeReferences[_AppointmentChargeKind.pixMercadoPago]!,
        description:
            '${controller.businessName} | $_serviceName | ${hour(appointment.start)}',
        payerName: _customerName,
      ),
    );
    if (!charge.ok) {
      _showError(
        charge.message.trim().isEmpty
            ? 'Mercado Pago recusou a criação do Pix.'
            : charge.message,
      );
      return;
    }
    if (!mounted) return;
    final outcome = await showDialog<MercadoPagoPaymentOutcome>(
      context: context,
      barrierDismissible: false,
      builder: (context) => _MercadoPagoPixProgressDialog(
        service: service,
        charge: charge,
        amount: _amount,
      ),
    );
    if (outcome == null) return;
    final error = await _confirmPayment(
      paymentMethod: 'Pix',
      paymentProvider: 'Mercado Pago',
      paymentReference: outcome.reference,
      paymentStatus: outcome.status,
    );
    if (error != null) return _showError(error);
    _closeWithMessage('Pix confirmado pelo Mercado Pago.');
  }

  Future<void> _chargeByPoint(MercadoPagoPointMethod method) async {
    final settings = controller.data.settings;
    final service = controller.mercadoPagoService;
    if (!settings.mercadoPagoEnabled ||
        !settings.mercadoPagoConnected ||
        settings.mercadoPagoDefaultTerminalId.trim().isEmpty ||
        service == null) {
      _showError(
        'Ative o Mercado Pago em Configurações, conecte a conta e escolha a maquininha Point.',
      );
      return;
    }
    final charge = await service.createPointCharge(
      MercadoPagoPointChargeRequest(
        amountInCents: (_amount * 100).round(),
        method: method,
        terminalId: settings.mercadoPagoDefaultTerminalId,
        localReference:
            _remoteChargeReferences[method == MercadoPagoPointMethod.debit
                ? _AppointmentChargeKind.debit
                : _AppointmentChargeKind.credit]!,
        description: '$_serviceName | ${hour(appointment.start)}',
      ),
    );
    if (!charge.ok) {
      _showError(
        charge.message.trim().isEmpty
            ? 'Mercado Pago recusou a cobrança.'
            : charge.message,
      );
      return;
    }
    if (!mounted) return;
    final outcome = await showDialog<MercadoPagoPaymentOutcome>(
      context: context,
      barrierDismissible: false,
      builder: (context) => _MercadoPagoPointProgressDialog(
        service: service,
        charge: charge,
        amount: _amount,
        method: method,
        terminalId: settings.mercadoPagoDefaultTerminalId,
        terminalLabel: settings.mercadoPagoDefaultTerminalLabel.trim().isEmpty
            ? settings.mercadoPagoDefaultTerminalId
            : settings.mercadoPagoDefaultTerminalLabel,
      ),
    );
    if (outcome == null) return;
    final paymentMethod = method == MercadoPagoPointMethod.debit
        ? 'Débito na Point'
        : 'Crédito na Point';
    final error = await _confirmPayment(
      paymentMethod: paymentMethod,
      paymentProvider: 'Mercado Pago',
      paymentReference: outcome.reference,
      paymentStatus: outcome.status,
    );
    if (error != null) return _showError(error);
    _closeWithMessage('$paymentMethod confirmado pelo Mercado Pago.');
  }

  void _showError(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  void _closeWithMessage(String message) {
    if (!mounted) return;
    final messenger = ScaffoldMessenger.of(context);
    Navigator.of(context).pop();
    messenger
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }
}

class MercadoPagoPaymentOutcome {
  const MercadoPagoPaymentOutcome(this.reference, this.status);

  final String reference;
  final String status;
}

Future<MercadoPagoPaymentOutcome?> showMercadoPagoPointProgressDialog(
  BuildContext context, {
  required MercadoPagoService service,
  required MercadoPagoChargeResult charge,
  required double amount,
  required MercadoPagoPointMethod method,
  String terminalId = '',
  required String terminalLabel,
}) => showDialog<MercadoPagoPaymentOutcome>(
  context: context,
  barrierDismissible: false,
  builder: (_) => _MercadoPagoPointProgressDialog(
    service: service,
    charge: charge,
    amount: amount,
    method: method,
    terminalId: terminalId,
    terminalLabel: terminalLabel,
  ),
);

Future<MercadoPagoPaymentOutcome?> showMercadoPagoPixProgressDialog(
  BuildContext context, {
  required MercadoPagoService service,
  required MercadoPagoPixChargeResult charge,
  required double amount,
}) => showDialog<MercadoPagoPaymentOutcome>(
  context: context,
  barrierDismissible: false,
  builder: (_) => _MercadoPagoPixProgressDialog(
    service: service,
    charge: charge,
    amount: amount,
  ),
);

class _MercadoPagoPointProgressDialog extends StatefulWidget {
  const _MercadoPagoPointProgressDialog({
    required this.service,
    required this.charge,
    required this.amount,
    required this.method,
    required this.terminalId,
    required this.terminalLabel,
  });

  final MercadoPagoService service;
  final MercadoPagoChargeResult charge;
  final double amount;
  final MercadoPagoPointMethod method;
  final String terminalId;
  final String terminalLabel;

  @override
  State<_MercadoPagoPointProgressDialog> createState() =>
      _MercadoPagoPointProgressDialogState();
}

class _MercadoPagoPointProgressDialogState
    extends State<_MercadoPagoPointProgressDialog> {
  bool _stopped = false;
  bool _cancelling = false;
  String _status = 'Cobrança enviada para a maquininha.';
  String _technicalStatus = 'AT_TERMINAL';
  int _attempt = 0;

  @override
  void initState() {
    super.initState();
    unawaited(_poll());
  }

  Future<void> _poll() async {
    var lastStatus = widget.charge.status.trim().isEmpty
        ? 'criado'
        : widget.charge.status.trim();
    for (var attempt = 0; attempt < 45 && mounted && !_stopped; attempt++) {
      await Future<void>.delayed(
        attempt == 0
            ? const Duration(milliseconds: 1200)
            : const Duration(milliseconds: 2500),
      );
      if (!mounted || _stopped) return;
      try {
        final status = await widget.service.fetchPointStatus(
          attemptId: widget.charge.attemptId,
          orderId: widget.charge.orderId,
          localReference: widget.charge.localReference,
        );
        if (!mounted || _stopped) return;
        if (!status.ok) {
          setState(() {
            _attempt = attempt + 1;
            _status = status.message.trim().isEmpty
                ? 'Aguardando retorno. Último status: $lastStatus'
                : status.message;
          });
          continue;
        }
        lastStatus = status.status.trim().isEmpty
            ? lastStatus
            : status.status.trim();
        _technicalStatus = lastStatus.toUpperCase();
        if (status.paid) {
          _stopped = true;
          if (!mounted) return;
          Navigator.of(context).pop(
            MercadoPagoPaymentOutcome(
              _firstFilled([
                status.paymentId,
                widget.charge.paymentId,
                widget.charge.orderId,
                widget.charge.localReference,
              ]),
              status.status.trim().isEmpty ? 'approved' : status.status,
            ),
          );
          return;
        }
        if (_isFinalPaymentFailure(lastStatus)) {
          _stopped = true;
          if (mounted) {
            setState(() => _status = 'Pagamento não aprovado: $lastStatus');
          }
          return;
        }
        setState(() {
          _attempt = attempt + 1;
          _status = 'Status: $lastStatus';
        });
      } on Object catch (error) {
        if (mounted && !_stopped) {
          setState(() {
            _attempt = attempt + 1;
            _status = 'Aguardando retorno da Point: $error';
          });
        }
      }
    }
    if (mounted && !_stopped) {
      setState(() {
        _status =
            'Ainda não houve confirmação. Confira a maquininha antes de registrar novamente.';
      });
    }
  }

  Future<void> _cancel() async {
    if (_cancelling) return;
    if (_stopped) {
      if (mounted) Navigator.of(context).pop();
      return;
    }
    setState(() {
      _cancelling = true;
      _status = 'Cancelando a cobrança na Point...';
    });
    try {
      await widget.service.cancelPointCharge(
        attemptId: widget.charge.attemptId,
        orderId: widget.charge.orderId,
        localReference: widget.charge.localReference,
      );
    } finally {
      _stopped = true;
      if (mounted) Navigator.of(context).pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final compact = MediaQuery.sizeOf(context).width < 560;
    final terminal = MercadoPagoTerminalVisual.resolve(
      terminalId: widget.terminalId,
      terminalLabel: widget.terminalLabel,
    );
    final attempt = _attempt == 0 ? 1 : _attempt;
    return PopScope(
      canPop: false,
      child: Dialog(
        key: const Key('mercado-pago-point-progress'),
        insetPadding: EdgeInsets.all(compact ? 10 : 24),
        backgroundColor: Colors.transparent,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 580),
          child: RepaintBoundary(
            key: const Key('mercado-pago-point-progress-capture'),
            child: Material(
              color: t.panel,
              elevation: 20,
              clipBehavior: Clip.antiAlias,
              borderRadius: BorderRadius.circular(16),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Padding(
                    padding: EdgeInsets.fromLTRB(compact ? 16 : 22, 14, 12, 14),
                    child: Row(
                      children: [
                        Container(
                          width: 44,
                          height: 44,
                          decoration: BoxDecoration(
                            color: t.accentSoft,
                            borderRadius: BorderRadius.circular(13),
                          ),
                          child: Icon(
                            Icons.credit_card_outlined,
                            color: t.accentDark,
                            size: 21,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Mercado Pago Point',
                                style: TextStyle(
                                  color: t.ink,
                                  fontSize: compact ? 17 : 20,
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                'Acompanhe a confirmação da cobrança enviada para a maquininha.',
                                maxLines: 2,
                                style: TextStyle(
                                  color: t.muted,
                                  fontSize: compact ? 10.5 : 11.5,
                                  fontWeight: FontWeight.w500,
                                ),
                              ),
                            ],
                          ),
                        ),
                        IconButton(
                          tooltip: 'Cancelar cobrança',
                          onPressed: _cancelling ? null : _cancel,
                          icon: const Icon(Icons.close_rounded, size: 21),
                        ),
                      ],
                    ),
                  ),
                  Divider(height: 1, color: t.line),
                  Padding(
                    padding: EdgeInsets.fromLTRB(
                      compact ? 16 : 26,
                      compact ? 18 : 24,
                      compact ? 16 : 26,
                      compact ? 16 : 20,
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Align(
                          child: SizedBox.square(
                            dimension: 29,
                            child: CircularProgressIndicator(
                              strokeWidth: 2.7,
                              color: t.accentDark,
                            ),
                          ),
                        ),
                        const SizedBox(height: 15),
                        Text(
                          _stopped
                              ? 'Pagamento não aprovado'
                              : 'Aguardando o cartão',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            color: t.ink,
                            fontSize: compact ? 19 : 22,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        const SizedBox(height: 5),
                        Text(
                          _stopped
                              ? _status
                              : 'Peça ao cliente para inserir, aproximar ou passar o cartão na maquininha.',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            color: t.muted,
                            fontSize: compact ? 11.5 : 12.5,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                        const SizedBox(height: 18),
                        Text(
                          'Tentativa $attempt de 45',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            color: t.muted,
                            fontSize: 12.5,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 8),
                        ClipRRect(
                          borderRadius: BorderRadius.circular(4),
                          child: LinearProgressIndicator(
                            minHeight: 5,
                            value: _stopped ? 1 : null,
                            color: _stopped ? Colors.red.shade400 : t.accent,
                            backgroundColor: const Color(0xFFE7E2DE),
                          ),
                        ),
                        const SizedBox(height: 20),
                        _PointTerminalCard(
                          terminal: terminal,
                          statusLabel: _stopped ? 'Não aprovado' : 'Aguardando',
                          technicalStatus: _technicalStatus,
                          compact: compact,
                        ),
                      ],
                    ),
                  ),
                  Divider(height: 1, color: t.line),
                  Padding(
                    padding: EdgeInsets.fromLTRB(
                      compact ? 16 : 22,
                      16,
                      compact ? 16 : 22,
                      18,
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        OutlinedButton(
                          onPressed: _cancelling ? null : _cancel,
                          style: OutlinedButton.styleFrom(
                            minimumSize: const Size(110, 42),
                            side: BorderSide(color: t.line),
                            foregroundColor: t.ink,
                          ),
                          child: const Text('Cancelar'),
                        ),
                        const SizedBox(width: 10),
                        FilledButton(
                          onPressed: _cancelling ? null : _cancel,
                          style: FilledButton.styleFrom(
                            minimumSize: Size(compact ? 130 : 150, 42),
                            backgroundColor: t.accent,
                            foregroundColor: Colors.black,
                          ),
                          child: Text(
                            _cancelling
                                ? 'Cancelando...'
                                : _stopped
                                ? 'Fechar'
                                : 'Parar espera',
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
      ),
    );
  }
}

class _PointTerminalCard extends StatelessWidget {
  const _PointTerminalCard({
    required this.terminal,
    required this.statusLabel,
    required this.technicalStatus,
    required this.compact,
  });

  final MercadoPagoTerminalVisual terminal;
  final String statusLabel;
  final String technicalStatus;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      key: const Key('mercado-pago-terminal-card'),
      padding: EdgeInsets.all(compact ? 10 : 14),
      decoration: BoxDecoration(
        color: t.warmSoft,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        children: [
          Container(
            width: compact ? 58 : 70,
            height: compact ? 58 : 70,
            padding: const EdgeInsets.all(7),
            decoration: BoxDecoration(
              color: Colors.white,
              border: Border.all(color: t.line),
              borderRadius: BorderRadius.circular(12),
            ),
            child: terminal.assetPath.isEmpty
                ? Icon(
                    Icons.point_of_sale_rounded,
                    color: t.accentDark,
                    size: 30,
                  )
                : Image.asset(
                    terminal.assetPath,
                    key: const Key('mercado-pago-terminal-image'),
                    fit: BoxFit.contain,
                  ),
          ),
          SizedBox(width: compact ? 10 : 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  terminal.modelName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: compact ? 14 : 16,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  '${terminal.serial} · Modo PDV',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.muted,
                    fontSize: compact ? 10.5 : 12,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 11,
                  vertical: 5,
                ),
                decoration: BoxDecoration(
                  color: t.accentSoft,
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Text(
                  statusLabel,
                  style: TextStyle(
                    color: t.accentDark,
                    fontSize: 10.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              const SizedBox(height: 6),
              Text(
                technicalStatus.isEmpty ? 'AT_TERMINAL' : technicalStatus,
                style: TextStyle(color: t.muted, fontSize: 10),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _MercadoPagoPixProgressDialog extends StatefulWidget {
  const _MercadoPagoPixProgressDialog({
    required this.service,
    required this.charge,
    required this.amount,
  });

  final MercadoPagoService service;
  final MercadoPagoPixChargeResult charge;
  final double amount;

  @override
  State<_MercadoPagoPixProgressDialog> createState() =>
      _MercadoPagoPixProgressDialogState();
}

class _MercadoPagoPixProgressDialogState
    extends State<_MercadoPagoPixProgressDialog> {
  bool _stopped = false;
  String _status = 'Aguardando a confirmação do pagamento...';
  int _attempt = 0;

  @override
  void initState() {
    super.initState();
    unawaited(_poll());
  }

  Future<void> _poll() async {
    var lastStatus = widget.charge.status.trim().isEmpty
        ? 'pending'
        : widget.charge.status.trim();
    for (var attempt = 0; attempt < 72 && mounted && !_stopped; attempt++) {
      await Future<void>.delayed(
        attempt == 0
            ? const Duration(milliseconds: 1200)
            : const Duration(milliseconds: 2500),
      );
      if (!mounted || _stopped) return;
      try {
        final status = await widget.service.fetchPixStatus(
          attemptId: widget.charge.attemptId,
          paymentId: widget.charge.paymentId,
          localReference: widget.charge.localReference,
        );
        if (!mounted || _stopped) return;
        if (!status.ok) {
          setState(() {
            _attempt = attempt + 1;
            _status = status.message.trim().isEmpty
                ? 'Aguardando retorno. Último status: $lastStatus'
                : status.message;
          });
          continue;
        }
        lastStatus = status.status.trim().isEmpty
            ? lastStatus
            : status.status.trim();
        if (status.paid) {
          _stopped = true;
          if (!mounted) return;
          Navigator.of(context).pop(
            MercadoPagoPaymentOutcome(
              _firstFilled([
                status.paymentId,
                widget.charge.paymentId,
                widget.charge.localReference,
              ]),
              status.status.trim().isEmpty ? 'approved' : status.status,
            ),
          );
          return;
        }
        if (_isFinalPaymentFailure(lastStatus)) {
          _stopped = true;
          setState(() => _status = 'Pix não aprovado: $lastStatus');
          return;
        }
        setState(() {
          _attempt = attempt + 1;
          _status = 'Status: $lastStatus';
        });
      } on Object catch (error) {
        if (mounted && !_stopped) {
          setState(() {
            _attempt = attempt + 1;
            _status = 'Aguardando retorno do Pix: $error';
          });
        }
      }
    }
  }

  Uint8List? _qrBytes() {
    var source = widget.charge.qrCodeBase64.trim();
    if (source.isEmpty) return null;
    final comma = source.indexOf(',');
    if (source.startsWith('data:') && comma >= 0) {
      source = source.substring(comma + 1);
    }
    try {
      return base64Decode(source);
    } on FormatException {
      return null;
    }
  }

  Future<void> _copyCode() async {
    await Clipboard.setData(ClipboardData(text: widget.charge.qrCode));
    if (mounted) {
      setState(() => _status = 'Código Pix copiado. Aguardando confirmação...');
    }
  }

  void _stop() {
    _stopped = true;
    Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    final qr = _qrBytes();
    final paymentUrl = Uri.tryParse(
      widget.charge.paymentUrl.trim().isEmpty
          ? widget.charge.ticketUrl
          : widget.charge.paymentUrl,
    );
    return PopScope(
      canPop: false,
      child: AlertDialog(
        key: const Key('mercado-pago-pix-progress'),
        title: const Text('Pix Mercado Pago'),
        content: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 470),
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Mostre o QR ao cliente ou copie o código Pix. O recebimento só será confirmado após a aprovação.',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 12),
                Center(
                  child: qr == null
                      ? const Icon(Icons.qr_code_2_rounded, size: 92)
                      : Image.memory(
                          qr,
                          width: 190,
                          height: 190,
                          fit: BoxFit.contain,
                          gaplessPlayback: true,
                        ),
                ),
                const SizedBox(height: 10),
                Text(
                  money(widget.amount),
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 10),
                if (widget.charge.qrCode.trim().isNotEmpty)
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: Theme.of(context).colorScheme.surfaceContainerLow,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: SelectableText(widget.charge.qrCode, maxLines: 4),
                  ),
                const SizedBox(height: 10),
                LinearProgressIndicator(value: _stopped ? 1 : null),
                const SizedBox(height: 8),
                Text(_status, textAlign: TextAlign.center),
                if (_attempt > 0)
                  Text(
                    'Verificação $_attempt/72',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
              ],
            ),
          ),
        ),
        actions: [
          if (paymentUrl != null && paymentUrl.hasScheme)
            TextButton(
              onPressed: () =>
                  launchUrl(paymentUrl, mode: LaunchMode.externalApplication),
              child: const Text('Abrir página do Pix'),
            ),
          if (widget.charge.qrCode.trim().isNotEmpty)
            FilledButton.tonal(
              onPressed: _copyCode,
              child: const Text('Copiar código Pix'),
            ),
          TextButton(onPressed: _stop, child: const Text('Parar espera')),
        ],
      ),
    );
  }
}

String _firstFilled(Iterable<String> values) => values
    .map((value) => value.trim())
    .firstWhere((value) => value.isNotEmpty, orElse: () => '');

bool _isFinalPaymentFailure(String status) {
  final value = status.trim().toLowerCase();
  return <String>[
    'cancel',
    'reject',
    'refus',
    'fail',
    'expired',
    'erro',
  ].any(value.contains);
}
