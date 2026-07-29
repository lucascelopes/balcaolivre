import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../app/agenda_controller.dart';
import '../../domain/models/cash_session.dart';

const _orange = Color(0xFFFC601D);
const _ink = Color(0xFF282421);
const _muted = Color(0xFF766F69);
const _line = Color(0xFFE8E3DF);
const _soft = Color(0xFFFFFAF7);

final _money = NumberFormat.currency(locale: 'pt_BR', symbol: 'R\$');
final _shortMoney = NumberFormat.currency(
  locale: 'pt_BR',
  symbol: 'R\$',
  decimalDigits: 2,
);
final _longDate = DateFormat("EEEE, dd 'de' MMMM 'de' yyyy", 'pt_BR');
final _clock = DateFormat('HH:mm', 'pt_BR');

Future<bool> showPdvCashOpeningDialog(
  BuildContext context,
  AgendaController controller, {
  DateTime? referenceNow,
}) async {
  final result = await showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (context) => _PdvCashOpeningDialog(
      controller: controller,
      referenceNow: referenceNow,
    ),
  );
  return result ?? false;
}

Future<bool> showPdvCashClosingDialog(
  BuildContext context,
  AgendaController controller, {
  DateTime? referenceNow,
}) async {
  final session = controller.cashSessionForClosing(referenceNow);
  final snapshot = controller.buildPdvCashClosingSnapshot(
    session,
    reference: referenceNow,
  );
  final result = await showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (context) => _PdvCashClosingDialog(
      controller: controller,
      session: session,
      snapshot: snapshot,
      referenceNow: referenceNow,
    ),
  );
  return result ?? false;
}

class _PdvCashOpeningDialog extends StatefulWidget {
  const _PdvCashOpeningDialog({
    required this.controller,
    required this.referenceNow,
  });

  final AgendaController controller;
  final DateTime? referenceNow;

  @override
  State<_PdvCashOpeningDialog> createState() => _PdvCashOpeningDialogState();
}

class _PdvCashOpeningDialogState extends State<_PdvCashOpeningDialog> {
  final _notes = TextEditingController();
  int _cents = 0;
  bool _saving = false;

  @override
  void dispose() {
    _notes.dispose();
    super.dispose();
  }

  void _append(String digits) {
    var next = _cents;
    for (final digit in digits.characters) {
      if (next > 99999999 ~/ 10) break;
      next = (next * 10) + int.parse(digit);
    }
    setState(() => _cents = next.clamp(0, 99999999));
  }

  void _quick(int reais) => setState(() => _cents = reais * 100);

  Future<void> _open() async {
    if (_saving) return;
    setState(() => _saving = true);
    await widget.controller.openCashSession(
      openingBalance: _cents / 100,
      operatorName: widget.controller.accountName,
      notes: _notes.text,
      openedAt: widget.referenceNow,
    );
    if (mounted) Navigator.of(context).pop(true);
  }

  @override
  Widget build(BuildContext context) {
    final now = widget.referenceNow ?? DateTime.now();
    return Dialog(
      key: const Key('pdv-cash-opening-dialog'),
      insetPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 20),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
      clipBehavior: Clip.antiAlias,
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 520, maxHeight: 680),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(28, 24, 20, 18),
              child: Row(
                children: [
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      color: const Color(0xFFFFEEE5),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: const Icon(
                      Icons.point_of_sale_rounded,
                      color: _orange,
                      size: 25,
                    ),
                  ),
                  const SizedBox(width: 14),
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Abrir caixa',
                          style: TextStyle(
                            color: _ink,
                            fontSize: 20,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        SizedBox(height: 3),
                        Text(
                          'Informe o fundo de troco para iniciar o turno.',
                          style: TextStyle(color: _muted, fontSize: 12),
                        ),
                      ],
                    ),
                  ),
                  IconButton(
                    tooltip: 'Fechar',
                    onPressed: () => Navigator.of(context).pop(false),
                    icon: const Icon(Icons.close_rounded, size: 20),
                  ),
                ],
              ),
            ),
            const Divider(height: 1, color: _line),
            Flexible(
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(28, 18, 28, 22),
                child: Column(
                  children: [
                    Row(
                      children: [
                        const Icon(
                          Icons.person_outline_rounded,
                          size: 17,
                          color: _muted,
                        ),
                        const SizedBox(width: 7),
                        Expanded(
                          child: Text(
                            widget.controller.accountName,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: _ink,
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                        const Icon(
                          Icons.calendar_today_outlined,
                          size: 15,
                          color: _muted,
                        ),
                        const SizedBox(width: 6),
                        Text(
                          DateFormat('dd/MM/yyyy', 'pt_BR').format(now),
                          style: const TextStyle(color: _muted, fontSize: 11),
                        ),
                      ],
                    ),
                    const SizedBox(height: 18),
                    Text(
                      _money.format(_cents / 100),
                      key: const Key('pdv-cash-opening-amount'),
                      style: const TextStyle(
                        color: _ink,
                        fontSize: 36,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -.8,
                      ),
                    ),
                    const SizedBox(height: 16),
                    _OpeningKeypad(
                      onDigit: _append,
                      onBackspace: () => setState(() => _cents ~/= 10),
                      onQuick: _quick,
                    ),
                    const SizedBox(height: 18),
                    TextField(
                      key: const Key('pdv-cash-opening-notes'),
                      controller: _notes,
                      decoration: const InputDecoration(
                        labelText: 'Fundo de troco (opcional)',
                        hintText: 'Ex.: notas e moedas para troco',
                        border: OutlineInputBorder(),
                        isDense: true,
                      ),
                    ),
                    const SizedBox(height: 12),
                    const Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(
                          Icons.info_outline_rounded,
                          size: 16,
                          color: _muted,
                        ),
                        SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            'O saldo será enviado ao Financeiro e usado na conferência do fechamento.',
                            style: TextStyle(
                              color: _muted,
                              fontSize: 10.5,
                              height: 1.35,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
            const Divider(height: 1, color: _line),
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 14, 24, 16),
              child: Row(
                children: [
                  Expanded(
                    child: TextButton(
                      key: const Key('pdv-cash-opening-skip'),
                      onPressed: _saving
                          ? null
                          : () => Navigator.of(context).pop(true),
                      child: const Text('Entrar sem abrir caixa'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: FilledButton(
                      key: const Key('pdv-cash-opening-confirm'),
                      onPressed: _saving ? null : _open,
                      style: FilledButton.styleFrom(
                        backgroundColor: _orange,
                        foregroundColor: Colors.white,
                        minimumSize: const Size(0, 46),
                      ),
                      child: Text(
                        _saving ? 'Abrindo...' : 'Abrir caixa e iniciar',
                      ),
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

class _OpeningKeypad extends StatelessWidget {
  const _OpeningKeypad({
    required this.onDigit,
    required this.onBackspace,
    required this.onQuick,
  });

  final ValueChanged<String> onDigit;
  final VoidCallback onBackspace;
  final ValueChanged<int> onQuick;

  @override
  Widget build(BuildContext context) {
    const keys = <String>[
      '1',
      '2',
      '3',
      '4',
      '5',
      '6',
      '7',
      '8',
      '9',
      '00',
      '0',
    ];
    return Column(
      children: [
        GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          itemCount: 12,
          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: 3,
            childAspectRatio: 3.5,
            crossAxisSpacing: 9,
            mainAxisSpacing: 9,
          ),
          itemBuilder: (context, index) {
            if (index == 11) {
              return OutlinedButton(
                key: const Key('pdv-cash-opening-backspace'),
                onPressed: onBackspace,
                child: const Icon(Icons.backspace_outlined, size: 19),
              );
            }
            final value = keys[index];
            return OutlinedButton(
              key: Key('pdv-cash-opening-key-$value'),
              onPressed: () => onDigit(value),
              child: Text(
                value,
                style: const TextStyle(
                  color: _ink,
                  fontWeight: FontWeight.w700,
                ),
              ),
            );
          },
        ),
        const SizedBox(height: 10),
        Row(
          children: [
            for (final value in const [100, 200, 300]) ...[
              if (value != 100) const SizedBox(width: 9),
              Expanded(
                child: OutlinedButton(
                  onPressed: () => onQuick(value),
                  child: Text('+ R\$ $value'),
                ),
              ),
            ],
          ],
        ),
      ],
    );
  }
}

class _PdvCashClosingDialog extends StatefulWidget {
  const _PdvCashClosingDialog({
    required this.controller,
    required this.session,
    required this.snapshot,
    required this.referenceNow,
  });

  final AgendaController controller;
  final CashSession session;
  final PdvCashClosingSnapshot snapshot;
  final DateTime? referenceNow;

  @override
  State<_PdvCashClosingDialog> createState() => _PdvCashClosingDialogState();
}

class _PdvCashClosingDialogState extends State<_PdvCashClosingDialog> {
  late final TextEditingController _amount;
  final _notes = TextEditingController();
  bool _confirmed = true;
  bool _print = true;
  bool _saving = false;

  double get _counted =>
      double.tryParse(_amount.text.replaceAll('.', '').replaceAll(',', '.')) ??
      0;

  @override
  void initState() {
    super.initState();
    _amount = TextEditingController(
      text: NumberFormat(
        '#,##0.00',
        'pt_BR',
      ).format(widget.snapshot.expectedBalance),
    )..addListener(_refresh);
    _notes.text = widget.session.notes;
  }

  void _refresh() => setState(() {});

  @override
  void dispose() {
    _amount
      ..removeListener(_refresh)
      ..dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _close() async {
    if (_saving || !_confirmed || widget.snapshot.hasRunningAppointment) return;
    setState(() => _saving = true);
    await widget.controller.closeCashSession(
      widget.session,
      closingBalance: _counted,
      notes: _notes.text,
      printSummaryOnClose: _print,
      closedAt: widget.referenceNow,
    );
    if (mounted) Navigator.of(context).pop(true);
  }

  @override
  Widget build(BuildContext context) {
    final wide = MediaQuery.sizeOf(context).width >= 780;
    final body = wide
        ? Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(child: _summary()),
              const VerticalDivider(width: 1, color: _line),
              Expanded(child: _conference()),
            ],
          )
        : ListView(
            padding: EdgeInsets.zero,
            children: [
              _summary(scrollable: false),
              _conference(scrollable: false),
            ],
          );
    return Dialog(
      key: const Key('pdv-cash-closing-dialog'),
      insetPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 18),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      clipBehavior: Clip.antiAlias,
      child: SizedBox(
        width: 900,
        height: wide ? 650 : MediaQuery.sizeOf(context).height - 36,
        child: Column(
          children: [
            SizedBox(
              height: 47,
              child: Padding(
                padding: const EdgeInsets.only(left: 24, right: 12),
                child: Row(
                  children: [
                    const Expanded(
                      child: Text(
                        'Conferência lado a lado com recibo do turno',
                        style: TextStyle(
                          color: _ink,
                          fontSize: 14,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    IconButton(
                      tooltip: 'Cancelar',
                      onPressed: () => Navigator.of(context).pop(false),
                      icon: const Icon(Icons.close_rounded, size: 18),
                    ),
                  ],
                ),
              ),
            ),
            const Divider(height: 1, color: _line),
            Expanded(child: body),
          ],
        ),
      ),
    );
  }

  Widget _summary({bool scrollable = true}) {
    final snapshot = widget.snapshot;
    final session = widget.session;
    final now = widget.referenceNow ?? DateTime.now();
    final elapsed = now.difference(session.openedAt);
    final turn =
        '${_clock.format(session.openedAt)}–${_clock.format(now)}\nDuração: ${elapsed.inHours}h${(elapsed.inMinutes % 60).toString().padLeft(2, '0')}';
    final content = Padding(
      padding: const EdgeInsets.fromLTRB(24, 16, 20, 18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Resumo do turno',
            style: TextStyle(
              color: _ink,
              fontSize: 14,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 5),
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      session.operatorName,
                      style: const TextStyle(
                        color: _ink,
                        fontSize: 10.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    Text(
                      session.terminalName,
                      style: const TextStyle(color: _muted, fontSize: 9.5),
                    ),
                  ],
                ),
              ),
              const Icon(Icons.calendar_today_outlined, size: 13),
              const SizedBox(width: 5),
              Flexible(
                child: Text(
                  _longDate.format(now),
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: _ink, fontSize: 9.5),
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              _TurnMetric(icon: Icons.schedule, value: turn, label: 'Turno'),
              _TurnMetric(
                icon: Icons.person_outline,
                value: '${snapshot.appointmentCount}',
                label: 'Atendimentos',
              ),
              _TurnMetric(
                icon: Icons.check_circle_outline,
                value: '${snapshot.completedCount}',
                label: 'Concluídos',
              ),
              _TurnMetric(
                icon: Icons.cancel_outlined,
                value: '${snapshot.cancelledCount}',
                label: 'Cancelado',
              ),
              _TurnMetric(
                icon: Icons.person_off_outlined,
                value: '${snapshot.noShowCount}',
                label: 'Falta',
              ),
            ],
          ),
          const SizedBox(height: 14),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 9),
            decoration: BoxDecoration(
              color: _soft,
              border: Border.all(color: _line),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Row(
              children: [
                const Icon(Icons.payments_outlined, size: 20, color: _ink),
                const SizedBox(width: 13),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Total vendido',
                      style: TextStyle(color: _muted, fontSize: 9),
                    ),
                    Text(
                      _money.format(snapshot.totalSales),
                      style: const TextStyle(
                        color: _orange,
                        fontSize: 18,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          const Text(
            'Linha do tempo do turno',
            style: TextStyle(
              color: _ink,
              fontSize: 11,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 5),
          for (final entry in _timelineEntries)
            _TimelineRow(
              time: _clock.format(entry.at),
              title: entry.title,
              detail: entry.detail,
              value: entry.value,
              withdrawal: entry.withdrawal,
            ),
          const SizedBox(height: 10),
          const Text(
            'Totais por forma de pagamento',
            style: TextStyle(
              color: _ink,
              fontSize: 11,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 8),
          _PaymentRow(
            icon: Icons.money_outlined,
            label: 'Dinheiro',
            value: snapshot.cashSales,
          ),
          _PaymentRow(icon: Icons.pix, label: 'Pix', value: snapshot.pixSales),
          _PaymentRow(
            icon: Icons.credit_card,
            label: 'Crédito',
            value: snapshot.creditCardSales,
          ),
          _PaymentRow(
            icon: Icons.credit_card,
            label: 'Débito',
            value: snapshot.debitCardSales,
          ),
          const SizedBox(height: 16),
          TextButton.icon(
            onPressed: () {},
            icon: const Icon(Icons.description_outlined, size: 17),
            label: const Text(
              'Visualizar relatório completo',
              style: TextStyle(decoration: TextDecoration.underline),
            ),
          ),
        ],
      ),
    );
    return scrollable ? SingleChildScrollView(child: content) : content;
  }

  List<_CashTimelineEntry> get _timelineEntries {
    final session = widget.session;
    final entries = <_CashTimelineEntry>[
      _CashTimelineEntry(
        at: session.openedAt,
        title: 'Abertura do caixa',
        detail: 'Valor inicial · ${_money.format(session.openingBalance)}',
        value: session.openingBalance,
      ),
      for (final item in widget.controller.data.manualPayments.where(
        (item) => item.cashSessionId == session.id,
      ))
        _CashTimelineEntry(
          at: item.paidAt,
          title: item.description.trim().isEmpty
              ? 'Entrada de caixa'
              : item.description.trim(),
          detail: item.paymentMethod,
          value: item.value,
        ),
      for (final item in widget.controller.data.expenses.where(
        (item) => item.cashSessionId == session.id,
      ))
        _CashTimelineEntry(
          at: item.date,
          title: 'Retirada · ${item.description}',
          detail: item.category.trim().isEmpty ? 'Despesa' : item.category,
          value: item.value,
          withdrawal: true,
        ),
      for (final item in widget.controller.data.productSales.where(
        (item) => item.cashSessionId == session.id,
      ))
        _CashTimelineEntry(
          at: item.soldAt,
          title: 'Venda · ${item.productName}',
          detail: item.paymentMethod,
          value: item.total,
        ),
    ]..sort((a, b) => a.at.compareTo(b.at));
    if (entries.length <= 5) return entries;
    return <_CashTimelineEntry>[
      entries.first,
      entries[1],
      entries[entries.length ~/ 2],
      entries[entries.length - 2],
      entries.last,
    ];
  }

  Widget _conference({bool scrollable = true}) {
    final snapshot = widget.snapshot;
    final difference = _counted - snapshot.expectedBalance;
    final exact = difference.abs() < .005;
    final content = Padding(
      padding: const EdgeInsets.fromLTRB(18, 16, 20, 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Conferir e fechar caixa',
            style: TextStyle(
              color: _ink,
              fontSize: 14,
              fontWeight: FontWeight.w800,
            ),
          ),
          const Text(
            'Formação do dinheiro esperado',
            style: TextStyle(color: _muted, fontSize: 9.5),
          ),
          const SizedBox(height: 18),
          _CashFormula(snapshot: snapshot),
          const SizedBox(height: 10),
          const Text(
            'Quanto há no caixa agora?',
            style: TextStyle(
              color: _ink,
              fontSize: 10,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 6),
          TextField(
            key: const Key('pdv-cash-closing-amount'),
            controller: _amount,
            autofocus: true,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
            decoration: const InputDecoration(
              prefixText: 'R\$  ',
              border: OutlineInputBorder(),
              focusedBorder: OutlineInputBorder(
                borderSide: BorderSide(color: _orange, width: 1.5),
              ),
            ),
          ),
          const SizedBox(height: 13),
          Container(
            key: const Key('pdv-cash-closing-difference'),
            width: double.infinity,
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: exact ? const Color(0xFFEDF8F0) : const Color(0xFFFFF4EC),
              border: Border.all(
                color: exact
                    ? const Color(0xFFB7DFC3)
                    : const Color(0xFFF5B58F),
              ),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Row(
              children: [
                Icon(
                  exact ? Icons.check_circle_outline : Icons.error_outline,
                  color: exact
                      ? const Color(0xFF15803D)
                      : const Color(0xFFC2410C),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        exact
                            ? 'Tudo certo · diferença R\$ 0,00'
                            : 'Diferença de ${_shortMoney.format(difference.abs())}',
                        style: TextStyle(
                          color: exact
                              ? const Color(0xFF15803D)
                              : const Color(0xFFC2410C),
                          fontSize: 11,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      Text(
                        exact
                            ? 'O valor no caixa está conferido.'
                            : difference < 0
                            ? 'Há falta de dinheiro no caixa.'
                            : 'Há sobra de dinheiro no caixa.',
                        style: const TextStyle(color: _muted, fontSize: 9.5),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          const Text(
            'Observação do fechamento (opcional)',
            style: TextStyle(
              color: _ink,
              fontSize: 9.5,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 5),
          TextField(
            key: const Key('pdv-cash-closing-notes'),
            controller: _notes,
            maxLines: 2,
            decoration: const InputDecoration(
              hintText:
                  'Digite aqui alguma observação sobre o fechamento do turno...',
              border: OutlineInputBorder(),
            ),
          ),
          CheckboxListTile(
            contentPadding: EdgeInsets.zero,
            dense: true,
            visualDensity: VisualDensity.compact,
            value: _confirmed,
            activeColor: _orange,
            title: const Text(
              'Conferi dinheiro e comprovantes',
              style: TextStyle(fontSize: 10),
            ),
            onChanged: (value) => setState(() => _confirmed = value ?? false),
          ),
          CheckboxListTile(
            contentPadding: EdgeInsets.zero,
            dense: true,
            visualDensity: VisualDensity.compact,
            value: _print,
            activeColor: _orange,
            title: const Text(
              'Imprimir resumo ao fechar',
              style: TextStyle(fontSize: 10),
            ),
            onChanged: (value) => setState(() => _print = value ?? false),
          ),
          if (widget.snapshot.hasRunningAppointment)
            const Padding(
              padding: EdgeInsets.only(bottom: 7),
              child: Text(
                'Finalize o atendimento em andamento antes de fechar o caixa.',
                style: TextStyle(
                  color: Color(0xFFC2410C),
                  fontSize: 10,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          Row(
            children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: _saving
                      ? null
                      : () => Navigator.of(context).pop(false),
                  child: const Text('Cancelar'),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                flex: 2,
                child: FilledButton(
                  key: const Key('pdv-cash-closing-confirm'),
                  onPressed:
                      _saving ||
                          !_confirmed ||
                          widget.snapshot.hasRunningAppointment
                      ? null
                      : _close,
                  style: FilledButton.styleFrom(
                    backgroundColor: _orange,
                    foregroundColor: Colors.white,
                  ),
                  child: Text(
                    _saving ? 'Fechando...' : 'Fechar caixa e encerrar PDV',
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
            decoration: BoxDecoration(
              color: _soft,
              border: Border.all(color: const Color(0xFFF0D7C8)),
              borderRadius: BorderRadius.circular(7),
            ),
            child: const Row(
              children: [
                Icon(Icons.shield_outlined, size: 16, color: _muted),
                SizedBox(width: 7),
                Expanded(
                  child: Text(
                    'Após o fechamento, o caixa do dia ficará bloqueado para novos lançamentos.',
                    style: TextStyle(color: _muted, fontSize: 9),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
    return scrollable ? SingleChildScrollView(child: content) : content;
  }
}

class _TurnMetric extends StatelessWidget {
  const _TurnMetric({
    required this.icon,
    required this.value,
    required this.label,
  });

  final IconData icon;
  final String value;
  final String label;

  @override
  Widget build(BuildContext context) => Expanded(
    child: Column(
      children: [
        Icon(icon, size: 17, color: _muted),
        const SizedBox(height: 4),
        Text(
          value,
          textAlign: TextAlign.center,
          style: const TextStyle(
            color: _ink,
            fontSize: 10.5,
            fontWeight: FontWeight.w800,
            height: 1.05,
          ),
        ),
        Text(
          label,
          textAlign: TextAlign.center,
          style: const TextStyle(color: _muted, fontSize: 8),
        ),
      ],
    ),
  );
}

class _PaymentRow extends StatelessWidget {
  const _PaymentRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  final IconData icon;
  final String label;
  final double value;

  @override
  Widget build(BuildContext context) => Container(
    height: 27,
    padding: const EdgeInsets.symmetric(horizontal: 9),
    decoration: const BoxDecoration(
      border: Border(bottom: BorderSide(color: _line)),
    ),
    child: Row(
      children: [
        Icon(icon, size: 15, color: _muted),
        const SizedBox(width: 9),
        Expanded(
          child: Text(label, style: const TextStyle(fontSize: 10, color: _ink)),
        ),
        Text(
          _money.format(value),
          style: const TextStyle(
            fontSize: 10,
            color: _ink,
            fontWeight: FontWeight.w700,
          ),
        ),
      ],
    ),
  );
}

class _CashTimelineEntry {
  const _CashTimelineEntry({
    required this.at,
    required this.title,
    required this.detail,
    required this.value,
    this.withdrawal = false,
  });

  final DateTime at;
  final String title;
  final String detail;
  final double value;
  final bool withdrawal;
}

class _TimelineRow extends StatelessWidget {
  const _TimelineRow({
    required this.time,
    required this.title,
    required this.detail,
    required this.value,
    required this.withdrawal,
  });

  final String time;
  final String title;
  final String detail;
  final double value;
  final bool withdrawal;

  @override
  Widget build(BuildContext context) => SizedBox(
    height: 25,
    child: Row(
      children: [
        SizedBox(
          width: 35,
          child: Text(time, style: const TextStyle(color: _muted, fontSize: 8)),
        ),
        Icon(
          withdrawal ? Icons.remove_circle_outline : Icons.payments_outlined,
          size: 12,
          color: withdrawal ? _orange : _muted,
        ),
        const SizedBox(width: 7),
        Expanded(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: _ink,
                  fontSize: 8.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
              Text(
                detail,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(color: _muted, fontSize: 7.5),
              ),
            ],
          ),
        ),
        Text(
          '${withdrawal ? '− ' : ''}${_money.format(value)}',
          style: TextStyle(
            color: withdrawal ? _orange : _ink,
            fontSize: 8.5,
            fontWeight: FontWeight.w700,
          ),
        ),
      ],
    ),
  );
}

class _CashFormula extends StatelessWidget {
  const _CashFormula({required this.snapshot});

  final PdvCashClosingSnapshot snapshot;

  @override
  Widget build(BuildContext context) {
    final values = <({String label, double value, String operator})>[
      (label: 'Abertura', value: snapshot.session.openingBalance, operator: ''),
      (label: 'Vendas', value: snapshot.cashSales, operator: '+'),
      (label: 'Entradas', value: snapshot.cashEntries, operator: '+'),
      (label: 'Retiradas', value: snapshot.cashWithdrawals, operator: '−'),
      (label: 'Esperado', value: snapshot.expectedBalance, operator: '='),
    ];
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 12),
      decoration: BoxDecoration(
        color: _soft,
        border: Border.all(color: _line),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        children: [
          for (final item in values) ...[
            if (item.operator.isNotEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 3),
                child: Text(
                  item.operator,
                  style: const TextStyle(color: _muted, fontSize: 10),
                ),
              ),
            Expanded(
              child: Column(
                children: [
                  Text(
                    _shortMoney.format(item.value),
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: item.label == 'Esperado' ? _orange : _ink,
                      fontSize: 8.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  Text(
                    item.label,
                    style: const TextStyle(color: _muted, fontSize: 7.5),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}
