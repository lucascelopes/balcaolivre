import 'package:flutter/material.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/motion.dart';
import '../../core/formatters.dart';
import '../../core/ui.dart';
import '../../domain/models/models.dart';
import '../../services/mercado_pago_service.dart';
import '../agenda/appointment_payment_dialog.dart';

const _salePaymentMethods = <String>[
  'Pix',
  'Dinheiro',
  'Cartão de débito',
  'Cartão de crédito',
  'Mercado Pago - débito na maquininha',
  'Mercado Pago - crédito na maquininha',
  'Fiado',
];

Future<bool> showProductSaleDialog(
  BuildContext context, {
  required AgendaController controller,
}) async {
  return await showAgendaDialog<bool>(
        context: context,
        barrierDismissible: false,
        builder: (_) => _ProductSaleDialog(controller: controller),
      ) ??
      false;
}

class _ProductSaleDialog extends StatefulWidget {
  const _ProductSaleDialog({required this.controller});

  final AgendaController controller;

  @override
  State<_ProductSaleDialog> createState() => _ProductSaleDialogState();
}

class _ProductSaleDialogState extends State<_ProductSaleDialog> {
  final _formKey = GlobalKey<FormState>();
  final _quantity = TextEditingController(text: '1');
  final _discount = TextEditingController(text: '0,00');
  final _customer = TextEditingController();
  final _notes = TextEditingController();
  String? _productId;
  String _method = 'Pix';
  bool _saving = false;
  String? _error;

  List<ProductItem> get _products =>
      widget.controller.data.products
          .where((product) => product.isActive)
          .toList()
        ..sort((a, b) => a.name.compareTo(b.name));

  ProductItem? get _selectedProduct {
    final id = _productId;
    if (id == null) return null;
    for (final product in _products) {
      if (product.id == id) return product;
    }
    return null;
  }

  int get _parsedQuantity => int.tryParse(_quantity.text.trim()) ?? 0;
  double get _parsedDiscount => _parseMoney(_discount.text) ?? 0;

  double get _total {
    final product = _selectedProduct;
    if (product == null) return 0;
    final value = (product.price * _parsedQuantity) - _parsedDiscount;
    return value < 0 ? 0 : value;
  }

  @override
  void initState() {
    super.initState();
    final products = _products;
    _productId = products.isEmpty ? null : products.first.id;
    for (final controller in <TextEditingController>[
      _quantity,
      _discount,
      _customer,
      _notes,
    ]) {
      controller.addListener(_refresh);
    }
  }

  @override
  void dispose() {
    for (final controller in <TextEditingController>[
      _quantity,
      _discount,
      _customer,
      _notes,
    ]) {
      controller
        ..removeListener(_refresh)
        ..dispose();
    }
    super.dispose();
  }

  void _refresh() {
    if (mounted) setState(() {});
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false) || _saving) return;
    final product = _selectedProduct;
    if (product == null) {
      setState(() => _error = 'Selecione o produto vendido.');
      return;
    }
    final quantity = _parsedQuantity;
    final discount = _parsedDiscount;
    final gross = product.price * quantity;
    if (discount > gross) {
      setState(
        () => _error = 'O desconto não pode ser maior que o total da venda.',
      );
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      MercadoPagoPaymentOutcome? outcome;
      final mercadoPago = _method.startsWith('Mercado Pago');
      if (mercadoPago) {
        outcome = await _chargePoint(product, quantity, discount);
        if (outcome == null) {
          if (mounted) setState(() => _saving = false);
          return;
        }
      }
      final error = await widget.controller.registerProductSale(
        ProductSale(
          productId: product.id,
          productName: product.name,
          customerName: _customer.text.trim(),
          quantity: quantity,
          unitPrice: product.price,
          discount: discount,
          paymentMethod: _method,
          paymentProvider: mercadoPago ? 'Mercado Pago' : '',
          paymentReference: outcome?.reference ?? '',
          paymentStatus: outcome?.status ?? '',
          notes: _notes.text.trim(),
        ),
      );
      if (error != null) throw FormatException(error);
      if (mounted) Navigator.of(context).pop(true);
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
          _error = error is FormatException
              ? error.message
              : 'Não foi possível registrar a venda.';
        });
      }
    }
  }

  Future<MercadoPagoPaymentOutcome?> _chargePoint(
    ProductItem product,
    int quantity,
    double discount,
  ) async {
    final settings = widget.controller.data.settings;
    final service = widget.controller.mercadoPagoService;
    if (service == null ||
        !settings.mercadoPagoEnabled ||
        !settings.mercadoPagoConnected ||
        settings.mercadoPagoDefaultTerminalId.trim().isEmpty) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'Ative o Mercado Pago, conecte a conta e escolha uma maquininha Point antes de registrar esta venda.',
      );
    }
    final amount = (product.price * quantity) - discount;
    if (amount <= 0) {
      throw const MercadoPagoException(
        MercadoPagoFailure.validation,
        'O valor para cobrar no Mercado Pago precisa ser maior que zero.',
      );
    }
    final method = _method.contains('débito')
        ? MercadoPagoPointMethod.debit
        : MercadoPagoPointMethod.credit;
    final charge = await service.createPointCharge(
      MercadoPagoPointChargeRequest(
        amountInCents: (amount * 100).round(),
        method: method,
        terminalId: settings.mercadoPagoDefaultTerminalId,
        description: '$quantity x ${product.name}',
        items: <MercadoPagoChargeItem>[
          MercadoPagoChargeItem(
            code: product.sku.trim().isEmpty ? product.id : product.sku,
            title: product.name,
            quantity: quantity,
            unitPriceInCents: (product.price * 100).round(),
            description: _notes.text.trim(),
          ),
        ],
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
    return showMercadoPagoPointProgressDialog(
      context,
      service: service,
      charge: charge,
      amount: amount,
      method: method,
      terminalLabel: settings.mercadoPagoDefaultTerminalLabel.trim().isEmpty
          ? settings.mercadoPagoDefaultTerminalId
          : settings.mercadoPagoDefaultTerminalLabel,
    );
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final compact = MediaQuery.sizeOf(context).width < 700;
    final product = _selectedProduct;
    final mercadoPago = _method.startsWith('Mercado Pago');
    final pointReady =
        widget.controller.data.settings.mercadoPagoEnabled &&
        widget.controller.data.settings.mercadoPagoConnected &&
        widget.controller.data.settings.mercadoPagoDefaultTerminalId
            .trim()
            .isNotEmpty;

    return Dialog(
      key: const Key('product-sale-dialog'),
      insetPadding: EdgeInsets.all(compact ? 8 : 24),
      backgroundColor: Colors.transparent,
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: 850,
          maxHeight: MediaQuery.sizeOf(context).height * .92,
        ),
        child: Material(
          color: t.panel,
          elevation: 18,
          clipBehavior: Clip.antiAlias,
          borderRadius: BorderRadius.circular(compact ? 18 : 22),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Padding(
                padding: EdgeInsets.fromLTRB(
                  compact ? 14 : 22,
                  compact ? 14 : 18,
                  compact ? 8 : 12,
                  compact ? 14 : 18,
                ),
                child: Row(
                  children: [
                    AgendaIconBadge(
                      Icons.shopping_bag_outlined,
                      size: 44,
                      iconSize: 22,
                    ),
                    const SizedBox(width: 11),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Registrar venda',
                            style: TextStyle(
                              color: t.ink,
                              fontSize: compact ? 20 : 23,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          Text(
                            'Baixe estoque e registre o valor vendido.',
                            style: TextStyle(color: t.muted, fontSize: 12),
                          ),
                        ],
                      ),
                    ),
                    IconButton(
                      tooltip: 'Fechar',
                      onPressed: _saving
                          ? null
                          : () => Navigator.of(context).pop(false),
                      icon: const Icon(Icons.close_rounded),
                    ),
                  ],
                ),
              ),
              Divider(height: 1, color: t.line),
              Flexible(
                child: SingleChildScrollView(
                  padding: EdgeInsets.all(compact ? 14 : 22),
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Text(
                          'Produto vendido',
                          style: TextStyle(
                            color: t.ink,
                            fontSize: 16,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        const SizedBox(height: 3),
                        Text(
                          'Venda com baixa automática de estoque.',
                          style: TextStyle(color: t.muted, fontSize: 11.5),
                        ),
                        const SizedBox(height: 14),
                        DropdownButtonFormField<String>(
                          key: const Key('product-sale-product'),
                          initialValue: _productId,
                          isExpanded: true,
                          decoration: const InputDecoration(
                            labelText: 'Produto *',
                            prefixIcon: Icon(Icons.inventory_2_outlined),
                          ),
                          items: [
                            for (final item in _products)
                              DropdownMenuItem(
                                value: item.id,
                                child: Text(
                                  '${item.name} • ${item.stockQuantity} un. • ${money(item.price)}',
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                          ],
                          onChanged: _saving
                              ? null
                              : (value) => setState(() => _productId = value),
                          validator: (value) =>
                              value == null ? 'Selecione o produto.' : null,
                        ),
                        const SizedBox(height: 12),
                        _AdaptiveFields(
                          children: [
                            TextFormField(
                              key: const Key('product-sale-quantity'),
                              controller: _quantity,
                              keyboardType: TextInputType.number,
                              decoration: const InputDecoration(
                                labelText: 'Quantidade *',
                              ),
                              validator: (value) {
                                final parsed = int.tryParse(value ?? '');
                                return parsed == null || parsed < 1
                                    ? 'Informe uma quantidade válida.'
                                    : null;
                              },
                            ),
                            TextFormField(
                              key: const Key('product-sale-discount'),
                              controller: _discount,
                              keyboardType:
                                  const TextInputType.numberWithOptions(
                                    decimal: true,
                                  ),
                              decoration: const InputDecoration(
                                labelText: 'Desconto',
                                prefixText: 'R\$ ',
                              ),
                              validator: (value) {
                                final parsed = _parseMoney(value ?? '');
                                return parsed == null || parsed < 0
                                    ? 'Informe um desconto válido.'
                                    : null;
                              },
                            ),
                          ],
                        ),
                        const SizedBox(height: 12),
                        _AdaptiveFields(
                          children: [
                            Autocomplete<String>(
                              optionsBuilder: (value) {
                                final query = value.text.trim().toLowerCase();
                                final names = widget.controller.data.customers
                                    .map((customer) => customer.name.trim())
                                    .where((name) => name.isNotEmpty);
                                return query.isEmpty
                                    ? names
                                    : names.where(
                                        (name) =>
                                            name.toLowerCase().contains(query),
                                      );
                              },
                              onSelected: (value) => _customer.text = value,
                              fieldViewBuilder:
                                  (context, controller, focusNode, onSubmit) {
                                    if (_customer.text.isNotEmpty &&
                                        controller.text.isEmpty) {
                                      controller.text = _customer.text;
                                    }
                                    return TextFormField(
                                      controller: controller,
                                      focusNode: focusNode,
                                      decoration: const InputDecoration(
                                        labelText: 'Cliente',
                                        prefixIcon: Icon(Icons.person_outline),
                                      ),
                                      onChanged: (value) =>
                                          _customer.text = value,
                                    );
                                  },
                            ),
                            DropdownButtonFormField<String>(
                              key: const Key('product-sale-method'),
                              initialValue: _method,
                              isExpanded: true,
                              decoration: const InputDecoration(
                                labelText: 'Forma de pagamento *',
                                prefixIcon: Icon(
                                  Icons.account_balance_wallet_outlined,
                                ),
                              ),
                              items: [
                                for (final method in _salePaymentMethods)
                                  DropdownMenuItem(
                                    value: method,
                                    child: Text(
                                      method,
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                  ),
                              ],
                              onChanged: _saving
                                  ? null
                                  : (value) => setState(
                                      () => _method = value ?? _method,
                                    ),
                            ),
                          ],
                        ),
                        if (mercadoPago) ...[
                          const SizedBox(height: 12),
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: pointReady
                                  ? const Color(0xFFF0FDF4)
                                  : const Color(0xFFFFF7ED),
                              border: Border.all(
                                color: pointReady
                                    ? const Color(0xFFBBF7D0)
                                    : const Color(0xFFFED7AA),
                              ),
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Row(
                              children: [
                                Icon(
                                  pointReady
                                      ? Icons.check_circle_outline_rounded
                                      : Icons.info_outline_rounded,
                                  color: pointReady
                                      ? const Color(0xFF15803D)
                                      : const Color(0xFFB45309),
                                ),
                                const SizedBox(width: 9),
                                Expanded(
                                  child: Text(
                                    pointReady
                                        ? 'A cobrança só será salva depois da aprovação na Point.'
                                        : 'Conecte a conta e escolha a Point em Configurações.',
                                    style: TextStyle(
                                      color: pointReady
                                          ? const Color(0xFF166534)
                                          : const Color(0xFF9A3412),
                                      fontSize: 12,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _notes,
                          minLines: 2,
                          maxLines: 3,
                          decoration: const InputDecoration(
                            labelText: 'Observações',
                            hintText:
                                'Ex: retirada no balcão, venda junto ao atendimento',
                          ),
                        ),
                        const SizedBox(height: 14),
                        AgendaPanel(
                          radius: 14,
                          color: t.warmSoft,
                          child: Row(
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      product?.name ?? 'Produto',
                                      style: TextStyle(
                                        color: t.ink,
                                        fontWeight: FontWeight.w700,
                                      ),
                                    ),
                                    const SizedBox(height: 2),
                                    Text(
                                      'Estoque após a venda: ${product == null ? 0 : (product.stockQuantity - _parsedQuantity).clamp(0, 0x7fffffff)}',
                                      style: TextStyle(
                                        color: t.muted,
                                        fontSize: 11.5,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              Text(
                                money(_total),
                                style: TextStyle(
                                  color: t.accentDark,
                                  fontSize: 21,
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                            ],
                          ),
                        ),
                        if (_error != null) ...[
                          const SizedBox(height: 10),
                          Text(
                            _error!,
                            style: const TextStyle(
                              color: Color(0xFFB91C1C),
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                ),
              ),
              Divider(height: 1, color: t.line),
              Padding(
                padding: EdgeInsets.all(compact ? 12 : 16),
                child: compact
                    ? Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          ElevatedButton.icon(
                            key: const Key('product-sale-save'),
                            onPressed: _saving ? null : _submit,
                            icon: const Icon(Icons.check_rounded, size: 18),
                            label: Text(
                              _saving ? 'Registrando...' : 'Registrar venda',
                            ),
                          ),
                          const SizedBox(height: 8),
                          OutlinedButton(
                            onPressed: _saving
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
                            onPressed: _saving
                                ? null
                                : () => Navigator.of(context).pop(false),
                            child: const Text('Cancelar'),
                          ),
                          const SizedBox(width: 8),
                          ElevatedButton.icon(
                            key: const Key('product-sale-save'),
                            onPressed: _saving ? null : _submit,
                            icon: const Icon(Icons.check_rounded, size: 18),
                            label: Text(
                              _saving ? 'Registrando...' : 'Registrar venda',
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

class _AdaptiveFields extends StatelessWidget {
  const _AdaptiveFields({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 560) {
          return Column(
            children: [
              for (var index = 0; index < children.length; index++) ...[
                if (index > 0) const SizedBox(height: 12),
                children[index],
              ],
            ],
          );
        }
        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            for (var index = 0; index < children.length; index++) ...[
              if (index > 0) const SizedBox(width: 12),
              Expanded(child: children[index]),
            ],
          ],
        );
      },
    );
  }
}

double? _parseMoney(String source) {
  var normalized = source
      .trim()
      .replaceAll('R\$', '')
      .replaceAll(RegExp(r'\s'), '');
  if (normalized.isEmpty) return 0;
  if (normalized.contains(',') && normalized.contains('.')) {
    normalized = normalized.replaceAll('.', '').replaceAll(',', '.');
  } else {
    normalized = normalized.replaceAll(',', '.');
  }
  return double.tryParse(normalized);
}
