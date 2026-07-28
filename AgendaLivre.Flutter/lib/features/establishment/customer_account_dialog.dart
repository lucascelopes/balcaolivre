import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../domain/models/models.dart';
import '../../services/mercado_pago_service.dart';
import '../agenda/appointment_payment_dialog.dart';

Future<bool> showCustomerAccountDialog(
  BuildContext context, {
  required AgendaController controller,
  required Customer customer,
}) async {
  return await showDialog<bool>(
        context: context,
        barrierColor: const Color(0xC0000000),
        barrierDismissible: false,
        builder: (_) =>
            _CustomerAccountDialog(controller: controller, customer: customer),
      ) ??
      false;
}

class _CustomerAccountDialog extends StatefulWidget {
  const _CustomerAccountDialog({
    required this.controller,
    required this.customer,
  });

  final AgendaController controller;
  final Customer customer;

  @override
  State<_CustomerAccountDialog> createState() => _CustomerAccountDialogState();
}

class _CustomerAccountDialogState extends State<_CustomerAccountDialog> {
  static const _methods = <String>[
    'Pix',
    'Dinheiro',
    'Débito na Point',
    'Crédito na Point',
  ];

  String _method = _methods.first;
  bool _saving = false;
  String? _error;

  List<CustomerReceivable> get _items =>
      widget.controller.openCustomerReceivables(
        customerId: widget.customer.id,
        customerName: widget.customer.name,
      );

  double get _total =>
      _items.fold(0, (total, item) => total + item.remainingValue);

  Future<void> _submit() async {
    final items = _items;
    final total = items.fold(0.0, (sum, item) => sum + item.remainingValue);
    if (items.isEmpty || total <= 0) {
      setState(() => _error = 'Esse saldo já foi quitado ou alterado.');
      return;
    }

    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      final payment = await _receive(total, items);
      if (payment == null) {
        if (mounted) setState(() => _saving = false);
        return;
      }
      final error = await widget.controller.settleCustomerReceivables(
        items.map((item) => item.id),
        paymentMethod: _method,
        paymentProvider: payment.provider,
        paymentReference: payment.reference,
        paymentStatus: payment.status,
      );
      if (!mounted) return;
      if (error != null) {
        setState(() {
          _saving = false;
          _error = error;
        });
        return;
      }
      Navigator.of(context).pop(true);
    } on MercadoPagoException catch (error) {
      if (mounted) {
        setState(() {
          _saving = false;
          _error = error.message;
        });
      }
    } on Object catch (error) {
      if (mounted) {
        setState(() {
          _saving = false;
          _error = 'Não foi possível receber a conta: $error';
        });
      }
    }
  }

  Future<_AccountPayment?> _receive(
    double total,
    List<CustomerReceivable> items,
  ) async {
    final now = DateTime.now();
    if (_method == 'Dinheiro') {
      return _AccountPayment(
        provider: 'Manual',
        reference: 'manual_${now.millisecondsSinceEpoch}',
      );
    }
    if (_method == 'Pix') {
      final settings = widget.controller.data.settings;
      if (settings.mercadoPagoEnabled && settings.mercadoPagoConnected) {
        return _receiveByMercadoPagoPix(total, items);
      }
      return _receiveByPixKey(total, now);
    }
    return _receiveByPoint(total, items);
  }

  Future<_AccountPayment?> _receiveByPixKey(double total, DateTime now) async {
    final key = widget.controller.data.settings.pixKey.trim();
    if (key.isEmpty) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'Cadastre uma chave Pix nas configurações de pagamento.',
      );
    }
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Receber por Pix'),
        content: SelectableText(
          'Chave Pix do estabelecimento:\n\n$key\n\nValor: ${money(total)}\n\nConfirme somente depois que o valor aparecer na conta.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Ainda não recebi'),
          ),
          FilledButton(
            key: const Key('customer-account-confirm-pix-key'),
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Pagamento recebido'),
          ),
        ],
      ),
    );
    if (confirmed != true) return null;
    return _AccountPayment(
      provider: 'Chave Pix',
      reference: 'pix_key_${now.millisecondsSinceEpoch}',
    );
  }

  Future<_AccountPayment?> _receiveByMercadoPagoPix(
    double total,
    List<CustomerReceivable> items,
  ) async {
    final service = widget.controller.mercadoPagoService;
    if (service == null) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'O serviço Mercado Pago não está disponível neste dispositivo.',
      );
    }
    final amountInCents = (total * 100).round();
    final charge = await service.createPixCharge(
      MercadoPagoPixChargeRequest(
        amountInCents: amountInCents,
        description: 'Conta do cliente | ${widget.customer.name}',
        payerName: widget.customer.name,
        items: _chargeItems(items),
      ),
    );
    if (!charge.ok) {
      throw MercadoPagoException(
        MercadoPagoFailure.invalidResponse,
        charge.message.trim().isEmpty
            ? 'Mercado Pago recusou a criação do Pix.'
            : charge.message,
        statusCode: charge.statusCode,
      );
    }
    if (!mounted) return null;
    final outcome = await showMercadoPagoPixProgressDialog(
      context,
      service: service,
      charge: charge,
      amount: total,
    );
    if (outcome == null) return null;
    return _AccountPayment(
      provider: 'Mercado Pago',
      reference: outcome.reference,
      status: outcome.status,
    );
  }

  Future<_AccountPayment?> _receiveByPoint(
    double total,
    List<CustomerReceivable> items,
  ) async {
    final settings = widget.controller.data.settings;
    final service = widget.controller.mercadoPagoService;
    if (!settings.mercadoPagoEnabled ||
        !settings.mercadoPagoConnected ||
        settings.mercadoPagoDefaultTerminalId.trim().isEmpty ||
        service == null) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'Ative o Mercado Pago, conecte a conta e escolha uma maquininha Point em Configurações.',
      );
    }
    final pointMethod = _method == 'Débito na Point'
        ? MercadoPagoPointMethod.debit
        : MercadoPagoPointMethod.credit;
    final charge = await service.createPointCharge(
      MercadoPagoPointChargeRequest(
        amountInCents: (total * 100).round(),
        method: pointMethod,
        terminalId: settings.mercadoPagoDefaultTerminalId,
        description: 'Conta do cliente | ${widget.customer.name}',
        items: _chargeItems(items),
      ),
    );
    if (!charge.ok) {
      throw MercadoPagoException(
        MercadoPagoFailure.invalidResponse,
        charge.message.trim().isEmpty
            ? 'Mercado Pago recusou a cobrança.'
            : charge.message,
        statusCode: charge.statusCode,
      );
    }
    if (!mounted) return null;
    final outcome = await showMercadoPagoPointProgressDialog(
      context,
      service: service,
      charge: charge,
      amount: total,
      method: pointMethod,
      terminalLabel: settings.mercadoPagoDefaultTerminalLabel.trim().isEmpty
          ? settings.mercadoPagoDefaultTerminalId
          : settings.mercadoPagoDefaultTerminalLabel,
    );
    if (outcome == null) return null;
    return _AccountPayment(
      provider: 'Mercado Pago',
      reference: outcome.reference,
      status: outcome.status,
    );
  }

  List<MercadoPagoChargeItem> _chargeItems(List<CustomerReceivable> items) =>
      items
          .map(
            (item) => MercadoPagoChargeItem(
              code: item.id,
              title: item.description.trim().isEmpty
                  ? 'Atendimento'
                  : item.description,
              quantity: 1,
              unitPriceInCents: (item.remainingValue * 100).round(),
              description: 'Conta de ${widget.customer.name}',
            ),
          )
          .toList(growable: false);

  @override
  Widget build(BuildContext context) {
    final size = MediaQuery.sizeOf(context);
    final desktop = size.width >= 720;
    final frameWidth = math.max(0.0, math.min(620.0, size.width - 32));
    final frameHeight = math.max(
      0.0,
      math.min(desktop ? 620.0 : 760.0, size.height - 32),
    );
    final t = AgendaThemeTokens.of(context);
    final items = _items;
    final total = _total;
    return Dialog(
      key: const Key('customer-account-dialog'),
      backgroundColor: Colors.transparent,
      elevation: 0,
      insetPadding: const EdgeInsets.all(16),
      child: Container(
        width: frameWidth,
        height: frameHeight,
        clipBehavior: Clip.antiAlias,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: t.line),
          boxShadow: const [
            BoxShadow(
              color: Color(0x290F172A),
              blurRadius: 22,
              offset: Offset(0, 5),
            ),
          ],
        ),
        child: Column(
          children: [
            _header(t, desktop),
            Expanded(
              child: SingleChildScrollView(
                padding: EdgeInsets.fromLTRB(
                  desktop ? 24 : 16,
                  18,
                  desktop ? 24 : 16,
                  16,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _balanceCard(t, items, total),
                    const SizedBox(height: 18),
                    Text(
                      'Forma de pagamento',
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 13,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 7),
                    DropdownButtonFormField<String>(
                      key: const Key('customer-account-method'),
                      initialValue: _method,
                      isExpanded: true,
                      decoration: const InputDecoration(
                        prefixIcon: Icon(Icons.payments_outlined),
                      ),
                      items: [
                        for (final method in _methods)
                          DropdownMenuItem(value: method, child: Text(method)),
                      ],
                      onChanged: _saving
                          ? null
                          : (value) => setState(
                              () => _method = value ?? _methods.first,
                            ),
                    ),
                    const SizedBox(height: 10),
                    Text(
                      'O saldo só será baixado após a confirmação do pagamento.',
                      style: TextStyle(color: t.muted, fontSize: 12),
                    ),
                    if (_error != null) ...[
                      const SizedBox(height: 12),
                      Container(
                        padding: const EdgeInsets.all(11),
                        decoration: BoxDecoration(
                          color: const Color(0xFFFEF2F2),
                          border: Border.all(color: const Color(0xFFFECACA)),
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: Text(
                          _error!,
                          style: const TextStyle(
                            color: Color(0xFF991B1B),
                            fontSize: 12.5,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),
            _footer(t, desktop, items.isNotEmpty),
          ],
        ),
      ),
    );
  }

  Widget _header(AgendaThemeTokens t, bool desktop) {
    return Container(
      padding: EdgeInsets.fromLTRB(22, desktop ? 18 : 14, 14, 14),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          Container(
            width: 46,
            height: 46,
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(13),
            ),
            alignment: Alignment.center,
            child: Icon(Icons.account_balance_wallet_outlined, color: t.accent),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Receber conta',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: desktop ? 21 : 19,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  'Quite o saldo em aberto de ${widget.customer.name}.',
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 12.5),
                ),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Fechar',
            onPressed: _saving ? null : () => Navigator.of(context).pop(false),
            icon: const Icon(Icons.close_rounded),
          ),
        ],
      ),
    );
  }

  Widget _balanceCard(
    AgendaThemeTokens t,
    List<CustomerReceivable> items,
    double total,
  ) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: total > 0 ? t.accentSoft : t.graySoft,
        border: Border.all(color: total > 0 ? t.accent : t.line),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  'Conta do cliente',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              Text(
                money(total),
                key: const Key('customer-account-total'),
                style: TextStyle(
                  color: total > 0 ? t.accentDark : t.muted,
                  fontSize: 18,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          Text(
            items.isEmpty
                ? 'Sem saldo em aberto.'
                : items.length == 1
                ? '1 atendimento aguardando pagamento.'
                : '${items.length} atendimentos aguardando pagamento.',
            style: TextStyle(color: t.muted, fontSize: 12),
          ),
          if (items.isNotEmpty) ...[
            const SizedBox(height: 12),
            for (var index = 0; index < items.length; index++) ...[
              if (index > 0) Divider(height: 13, color: t.line),
              Row(
                children: [
                  Icon(Icons.event_note_outlined, size: 17, color: t.muted),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      items[index].description.trim().isEmpty
                          ? 'Atendimento'
                          : items[index].description,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: t.ink, fontSize: 12),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Text(
                    money(items[index].remainingValue),
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ],
          ],
        ],
      ),
    );
  }

  Widget _footer(AgendaThemeTokens t, bool desktop, bool canSubmit) {
    final cancel = OutlinedButton(
      onPressed: _saving ? null : () => Navigator.of(context).pop(false),
      child: const Text('Cancelar'),
    );
    final receive = ElevatedButton.icon(
      key: const Key('customer-account-receive'),
      onPressed: _saving || !canSubmit ? null : _submit,
      icon: _saving
          ? const SizedBox(
              width: 16,
              height: 16,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : const Icon(Icons.arrow_forward_rounded, size: 18),
      label: Text(_saving ? 'Aguarde...' : 'Continuar'),
    );
    return Container(
      padding: EdgeInsets.symmetric(
        horizontal: desktop ? 24 : 16,
        vertical: 14,
      ),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: t.line)),
      ),
      child: desktop
          ? Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [cancel, const SizedBox(width: 10), receive],
            )
          : Row(
              children: [
                Expanded(child: cancel),
                const SizedBox(width: 10),
                Expanded(child: receive),
              ],
            ),
    );
  }
}

class _AccountPayment {
  const _AccountPayment({
    required this.provider,
    required this.reference,
    this.status = 'approved',
  });

  final String provider;
  final String reference;
  final String status;
}
