import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../core/motion.dart';
import '../../core/ui.dart';
import '../../domain/models/models.dart';
import 'appointment_visuals.dart';

Future<void> showAppointmentDialog(
  BuildContext context,
  AgendaController controller, {
  Appointment? appointment,
  Appointment? template,
  DateTime? initialStart,
  String? initialProfessionalId,
}) async {
  assert(
    appointment == null || template == null,
    'Use appointment para editar ou template para criar um novo agendamento.',
  );
  final result = await showAgendaDialog<String>(
    context: context,
    barrierDismissible: false,
    barrierColor: const Color(0xC0000000),
    builder: (dialogContext) {
      final mediaSize = MediaQuery.sizeOf(dialogContext);
      final compact = mediaSize.width < 720;
      final content = _AppointmentDialog(
        controller: controller,
        appointment: appointment,
        template: template,
        initialStart: initialStart,
        initialProfessionalId: initialProfessionalId,
      );
      if (compact) {
        return Dialog.fullscreen(child: SafeArea(child: content));
      }
      return Dialog(
        insetPadding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
        clipBehavior: Clip.antiAlias,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
        child: SizedBox(
          width: math.min(900, mediaSize.width - 48),
          height: math.min(620, mediaSize.height - 32),
          child: content,
        ),
      );
    },
  );
  if (result != null && context.mounted) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(result)));
  }
}

enum _AppointmentEditAction { duplicate, noShow, cancel, delete }

class _ScheduleAssistantIssue {
  const _ScheduleAssistantIssue(this.code, this.message);

  final String code;
  final String message;
}

class _AppointmentDialog extends StatefulWidget {
  const _AppointmentDialog({
    required this.controller,
    required this.appointment,
    required this.template,
    required this.initialStart,
    required this.initialProfessionalId,
  });

  final AgendaController controller;
  final Appointment? appointment;
  final Appointment? template;
  final DateTime? initialStart;
  final String? initialProfessionalId;

  @override
  State<_AppointmentDialog> createState() => _AppointmentDialogState();
}

class _AppointmentDialogState extends State<_AppointmentDialog> {
  final _scheduleFormKey = GlobalKey<FormState>();
  final _clientFormKey = GlobalKey<FormState>();
  late final TextEditingController _customerController;
  late final TextEditingController _phoneController;
  late final TextEditingController _profileController;
  late final TextEditingController _priceController;
  late final TextEditingController _resourceController;
  late final TextEditingController _notesController;

  late DateTime _date;
  late TimeOfDay _time;
  late int _duration;
  late String _segment;
  String? _serviceId;
  String? _professionalId;
  String? _error;
  bool _saving = false;
  int _step = 0;
  int _stepDirection = 1;
  String _acknowledgedScheduleKey = '';

  bool get _editing => widget.appointment != null;

  AgendaController get _controller => widget.controller;

  @override
  void initState() {
    super.initState();
    final source = widget.appointment ?? widget.template;
    var start =
        widget.appointment?.start ??
        widget.initialStart ??
        widget.template?.start ??
        _controller.selectedDate;
    final now = DateTime.now();
    if (widget.appointment == null && !start.isAfter(now)) {
      final roundedMinute = ((now.minute + 14) ~/ 15 * 15);
      start = DateTime(
        now.year,
        now.month,
        now.day,
        now.hour + roundedMinute ~/ 60,
        roundedMinute % 60,
      );
    }
    _date = DateUtils.dateOnly(start);
    _time = TimeOfDay.fromDateTime(start);
    _duration = source?.durationMinutes ?? 30;
    _segment = (source?.segment.trim().isNotEmpty ?? false)
        ? source!.segment
        : _defaultSegment();
    _serviceId = source?.serviceId.trim().isEmpty ?? true
        ? null
        : source!.serviceId;
    _professionalId = source?.professionalId.trim().isNotEmpty == true
        ? source!.professionalId
        : widget.initialProfessionalId?.trim().isNotEmpty == true
        ? widget.initialProfessionalId!.trim()
        : null;
    _customerController = TextEditingController(
      text: source?.customerName ?? '',
    );
    _phoneController = TextEditingController(text: source?.customerPhone ?? '');
    _profileController = TextEditingController(
      text: source?.customerProfile ?? '',
    );
    _priceController = TextEditingController(
      text: (source?.price ?? 0).toStringAsFixed(2).replaceAll('.', ','),
    );
    _resourceController = TextEditingController(
      text: source?.resourceName ?? '',
    );
    _notesController = TextEditingController(
      text: widget.appointment?.notes ?? _returnNote(widget.template),
    );
    if (source?.scheduleExceptionAcknowledged ?? false) {
      _acknowledgedScheduleKey = _scheduleKey(source!.start, source.end);
    }
  }

  @override
  void dispose() {
    _customerController.dispose();
    _phoneController.dispose();
    _profileController.dispose();
    _priceController.dispose();
    _resourceController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  String _defaultSegment() {
    final configured = _controller.data.settings.businessSegment.trim();
    if (configured.isNotEmpty) return configured;
    if (_controller.activeServices.isNotEmpty) {
      return _controller.activeServices.first.segment;
    }
    return '';
  }

  String _returnNote(Appointment? source) {
    if (source == null) return '';
    final service = source.serviceName.trim();
    final date =
        '${source.start.day.toString().padLeft(2, '0')}/'
        '${source.start.month.toString().padLeft(2, '0')}/'
        '${source.start.year}';
    return service.isEmpty
        ? 'Retorno do atendimento realizado em $date.'
        : 'Retorno de $service realizado em $date.';
  }

  List<String> get _segments {
    final values = <String>{
      if (_segment.trim().isNotEmpty) _segment.trim(),
      if (_controller.data.settings.businessSegment.trim().isNotEmpty)
        _controller.data.settings.businessSegment.trim(),
      ..._controller.activeServices
          .map((item) => item.segment.trim())
          .where((item) => item.isNotEmpty),
      ..._controller.activeProfessionals
          .expand((item) => item.segments)
          .map((item) => item.trim())
          .where((item) => item.isNotEmpty),
    };
    return values.toList()..sort();
  }

  List<ServiceItem> get _services => _controller.activeServices
      .where(
        (item) =>
            _segment.isEmpty ||
            item.segment.isEmpty ||
            item.segment.toLowerCase() == _segment.toLowerCase(),
      )
      .toList();

  List<Professional> get _professionals => _controller.activeProfessionals
      .where(
        (item) =>
            _segment.isEmpty ||
            item.segments.isEmpty ||
            item.segments.any(
              (segment) => segment.toLowerCase() == _segment.toLowerCase(),
            ),
      )
      .toList();

  List<int> get _durations {
    final values = <int>{
      15,
      20,
      25,
      30,
      35,
      40,
      45,
      60,
      75,
      90,
      120,
      150,
      180,
      240,
      _duration,
    }.where((value) => value >= 5 && value <= 480).toList()..sort();
    return values;
  }

  List<TimeOfDay> get _times {
    final settings = _controller.data.settings;
    final values = <TimeOfDay>[];
    for (
      var minutes = settings.workdayStartHour * 60;
      minutes < settings.workdayEndHour * 60;
      minutes += 15
    ) {
      final value = TimeOfDay(hour: minutes ~/ 60, minute: minutes % 60);
      final start = DateTime(
        _date.year,
        _date.month,
        _date.day,
        value.hour,
        value.minute,
      );
      final end = start.add(Duration(minutes: _duration));
      if (_controller.validateBusinessWindow(start, end) == null) {
        values.add(value);
      }
    }
    if (!values.any(
      (item) => item.hour == _time.hour && item.minute == _time.minute,
    )) {
      values.add(_time);
      values.sort(
        (a, b) => (a.hour * 60 + a.minute).compareTo(b.hour * 60 + b.minute),
      );
    }
    return values;
  }

  String _timeKey(TimeOfDay value) =>
      '${value.hour.toString().padLeft(2, '0')}:${value.minute.toString().padLeft(2, '0')}';

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final compact = MediaQuery.sizeOf(context).width < 720;
    return Material(
      key: const Key('appointment-dialog'),
      color: t.panel,
      child: Column(
        children: [
          _dialogHeader(compact),
          Divider(height: 1, color: t.line),
          _stepper(compact),
          Divider(height: 1, color: t.line),
          AnimatedSize(
            duration: AgendaMotion.duration(context, AgendaMotion.standard),
            curve: AgendaMotion.enterCurve,
            alignment: Alignment.topCenter,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (_error != null)
                  KeyedSubtree(
                    key: ValueKey<String>(_error!),
                    child: _errorBanner(),
                  ),
                if (_editingScheduleOutsideWindow && _scheduleUnchanged)
                  _scheduleWarningBanner(),
              ],
            ),
          ),
          Expanded(
            child: ColoredBox(
              color: t.appBackground,
              child: SingleChildScrollView(
                key: const Key('appointment-dialog-scroll'),
                padding: EdgeInsets.all(compact ? 14 : 20),
                child: AnimatedSwitcher(
                  duration: AgendaMotion.duration(context, AgendaMotion.page),
                  reverseDuration: AgendaMotion.duration(
                    context,
                    AgendaMotion.standard,
                  ),
                  switchInCurve: AgendaMotion.enterCurve,
                  switchOutCurve: AgendaMotion.exitCurve,
                  transitionBuilder: (child, animation) {
                    final slide = Tween<Offset>(
                      begin: Offset(.08 * _stepDirection, 0),
                      end: Offset.zero,
                    ).animate(animation);
                    return FadeTransition(
                      opacity: animation,
                      child: SlideTransition(position: slide, child: child),
                    );
                  },
                  child: KeyedSubtree(
                    key: ValueKey<int>(_step),
                    child: switch (_step) {
                      0 => _scheduleStep(),
                      1 => _clientStep(),
                      _ => _reviewStep(compact),
                    },
                  ),
                ),
              ),
            ),
          ),
          Divider(height: 1, color: t.line),
          _wizardFooter(compact),
        ],
      ),
    );
  }

  Widget _dialogHeader(bool compact) {
    final t = AgendaThemeTokens.of(context);
    final source = widget.appointment;
    final subtitle = widget.template != null
        ? 'Cliente e serviço já preenchidos. Revise a data antes de salvar.'
        : source == null
        ? 'Preencha o horário, o serviço e os dados do cliente.'
        : '${appointmentStatusLabel(source.status)} • criado em '
              '${source.createdAt.day.toString().padLeft(2, '0')}/'
              '${source.createdAt.month.toString().padLeft(2, '0')} '
              '${source.createdAt.hour.toString().padLeft(2, '0')}:'
              '${source.createdAt.minute.toString().padLeft(2, '0')}';
    return SizedBox(
      height: compact ? 76 : 84,
      child: Padding(
        padding: EdgeInsets.fromLTRB(compact ? 16 : 22, 0, 10, 0),
        child: Row(
          children: [
            const AgendaIconBadge(Icons.event_available_rounded),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    _editing
                        ? 'Editar agendamento'
                        : widget.template != null
                        ? 'Agendar retorno'
                        : 'Novo agendamento',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: t.ink,
                      fontSize: compact ? 20 : 21,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    subtitle,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: t.muted, fontSize: 12),
                  ),
                ],
              ),
            ),
            IconButton(
              tooltip: 'Fechar agendamento',
              onPressed: _saving ? null : () => Navigator.pop(context),
              icon: const Icon(Icons.close_rounded),
            ),
          ],
        ),
      ),
    );
  }

  Widget _stepper(bool compact) {
    final t = AgendaThemeTokens.of(context);
    final row = Row(
      children: [
        _AppointmentStepButton(
          number: 1,
          label: 'Horário',
          active: _step == 0,
          reached: true,
          compact: compact,
          onTap: _saving ? null : () => _goToStep(0),
        ),
        _AppointmentStepConnector(active: _step >= 1, compact: compact),
        _AppointmentStepButton(
          number: 2,
          label: 'Cliente',
          active: _step == 1,
          reached: _step >= 1,
          compact: compact,
          onTap: _saving ? null : () => _goToStep(1),
        ),
        _AppointmentStepConnector(active: _step >= 2, compact: compact),
        _AppointmentStepButton(
          number: 3,
          label: 'Confirmar',
          active: _step == 2,
          reached: _step >= 2,
          compact: compact,
          onTap: _saving ? null : () => _goToStep(2),
        ),
      ],
    );
    return Container(
      height: compact ? 62 : 68,
      color: t.panel,
      alignment: Alignment.center,
      padding: EdgeInsets.symmetric(horizontal: compact ? 8 : 18, vertical: 7),
      child: compact
          ? row
          : ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 704),
              child: row,
            ),
    );
  }

  Widget _errorBanner() {
    final t = AgendaThemeTokens.of(context);
    return Container(
      width: double.infinity,
      color: t.panel,
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 9),
        decoration: BoxDecoration(
          color: t.redSoft,
          border: Border.all(color: const Color(0xFFFCA5A5)),
          borderRadius: BorderRadius.circular(14),
        ),
        child: Row(
          children: [
            const Icon(
              Icons.error_outline_rounded,
              color: Color(0xFFDC2626),
              size: 18,
            ),
            const SizedBox(width: 9),
            Expanded(
              child: Text(
                _error!,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 12.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _scheduleWarningBanner() {
    final t = AgendaThemeTokens.of(context);
    return Container(
      width: double.infinity,
      color: t.panel,
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 9),
        decoration: BoxDecoration(
          color: t.yellowSoft,
          border: Border.all(color: const Color(0xFFF4C76B)),
          borderRadius: BorderRadius.circular(14),
        ),
        child: Row(
          children: [
            const Icon(
              Icons.info_outline_rounded,
              color: Color(0xFF9A5A00),
              size: 18,
            ),
            const SizedBox(width: 9),
            Expanded(
              child: Text(
                'Este horário está fora da agenda atual. Você pode salvar os '
                'outros dados sem alterar o horário ou escolher uma nova data.',
                style: TextStyle(
                  color: t.ink,
                  fontSize: 12.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _scheduleStep() {
    return Form(
      key: _scheduleFormKey,
      child: _DialogSection(
        title: 'Horário e serviço',
        icon: Icons.schedule_rounded,
        child: Column(
          children: [
            _ResponsiveFields(
              children: [
                if (_segments.isEmpty)
                  TextFormField(
                    initialValue: _segment,
                    decoration: const InputDecoration(
                      labelText: 'Segmento *',
                      hintText: 'Ex.: Beleza',
                    ),
                    onChanged: (value) =>
                        setState(() => _segment = value.trim()),
                    validator: (value) => value == null || value.trim().isEmpty
                        ? 'Informe o segmento.'
                        : null,
                  )
                else
                  DropdownButtonFormField<String>(
                    isExpanded: true,
                    initialValue: _segments.contains(_segment)
                        ? _segment
                        : null,
                    decoration: const InputDecoration(labelText: 'Segmento *'),
                    items: [
                      for (final segment in _segments)
                        DropdownMenuItem(value: segment, child: Text(segment)),
                    ],
                    onChanged: _saving
                        ? null
                        : (value) {
                            setState(() {
                              _segment = value ?? '';
                              if (!_services.any(
                                (item) => item.id == _serviceId,
                              )) {
                                _serviceId = null;
                              }
                              if (!_professionals.any(
                                (item) => item.id == _professionalId,
                              )) {
                                _professionalId = null;
                              }
                            });
                          },
                    validator: (value) => value == null || value.isEmpty
                        ? 'Selecione o segmento.'
                        : null,
                  ),
                InkWell(
                  onTap: _saving ? null : _pickDate,
                  borderRadius: BorderRadius.circular(9),
                  child: InputDecorator(
                    decoration: const InputDecoration(
                      labelText: 'Data *',
                      suffixIcon: Icon(Icons.calendar_month_rounded),
                    ),
                    child: Text(_dateLabel),
                  ),
                ),
                DropdownButtonFormField<String>(
                  key: ValueKey('time-${_timeKey(_time)}'),
                  isExpanded: true,
                  initialValue: _timeKey(_time),
                  decoration: const InputDecoration(labelText: 'Horário *'),
                  items: [
                    for (final value in _times)
                      DropdownMenuItem(
                        value: _timeKey(value),
                        child: Text(_timeKey(value)),
                      ),
                  ],
                  onChanged: _saving
                      ? null
                      : (value) {
                          if (value == null) return;
                          final parts = value.split(':');
                          setState(
                            () => _time = TimeOfDay(
                              hour: int.parse(parts[0]),
                              minute: int.parse(parts[1]),
                            ),
                          );
                        },
                ),
                DropdownButtonFormField<int>(
                  key: ValueKey('duration-$_duration'),
                  isExpanded: true,
                  initialValue: _duration,
                  decoration: const InputDecoration(labelText: 'Duração *'),
                  items: [
                    for (final value in _durations)
                      DropdownMenuItem(value: value, child: Text('$value min')),
                  ],
                  onChanged: _saving
                      ? null
                      : (value) =>
                            setState(() => _duration = value ?? _duration),
                ),
              ],
            ),
            const SizedBox(height: 12),
            _ResponsiveFields(
              children: [
                DropdownButtonFormField<String>(
                  key: ValueKey('service-$_segment-${_serviceId ?? ''}'),
                  isExpanded: true,
                  initialValue: _services.any((item) => item.id == _serviceId)
                      ? _serviceId
                      : null,
                  decoration: const InputDecoration(labelText: 'Serviço *'),
                  items: [
                    for (final service in _services)
                      DropdownMenuItem(
                        value: service.id,
                        child: Text(
                          service.displayName,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                  ],
                  onChanged: _saving ? null : _selectService,
                  validator: (value) => value == null
                      ? _services.isEmpty
                            ? 'Cadastre um serviço ativo primeiro.'
                            : 'Selecione o serviço.'
                      : null,
                ),
                TextFormField(
                  controller: _priceController,
                  enabled: !_saving,
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(RegExp(r'[0-9,.]')),
                  ],
                  decoration: const InputDecoration(
                    labelText: 'Valor *',
                    prefixText: 'R\$ ',
                  ),
                  onChanged: (_) => setState(() {}),
                  validator: (value) {
                    final parsed = _parsePrice(value);
                    if (parsed == null) return 'Informe um valor válido.';
                    if (parsed < 0) return 'O valor não pode ser negativo.';
                    return null;
                  },
                ),
                DropdownButtonFormField<String>(
                  key: ValueKey(
                    'professional-$_segment-${_professionalId ?? ''}',
                  ),
                  isExpanded: true,
                  initialValue:
                      _professionals.any((item) => item.id == _professionalId)
                      ? _professionalId
                      : null,
                  decoration: const InputDecoration(
                    labelText: 'Profissional *',
                  ),
                  items: [
                    for (final professional in _professionals)
                      DropdownMenuItem(
                        value: professional.id,
                        child: Text(
                          professional.name,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                  ],
                  onChanged: _saving
                      ? null
                      : (value) => setState(() => _professionalId = value),
                  validator: (value) => value == null
                      ? _professionals.isEmpty
                            ? 'Cadastre um profissional ativo primeiro.'
                            : 'Selecione o profissional.'
                      : null,
                ),
                _resourceField(),
              ],
            ),
            const SizedBox(height: 14),
            _ScheduleSummaryBar(
              dateTime: _dateTimeSummary,
              service: _serviceSummary,
              professionalAndResource: _professionalResourceSummary,
              price: _priceSummary,
            ),
            if (_scheduleAssistantIssue case final issue?) ...[
              const SizedBox(height: 10),
              _scheduleAssistantCard(issue),
            ],
          ],
        ),
      ),
    );
  }

  Widget _clientStep() {
    final t = AgendaThemeTokens.of(context);
    return Form(
      key: _clientFormKey,
      child: _DialogSection(
        title: _controller.data.settings.clientLabel,
        icon: Icons.person_outline_rounded,
        child: Column(
          children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: t.ink,
                borderRadius: BorderRadius.circular(14),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          _serviceSummary,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 14,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 3),
                        Text(
                          '$_dateTimeSummary • $_professionalResourceSummary',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: Color(0xADFFFFFF),
                            fontSize: 11.5,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),
                  Text(
                    _priceSummary,
                    style: TextStyle(
                      color: t.accent,
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 14),
            _ResponsiveFields(
              children: [
                TextFormField(
                  controller: _customerController,
                  enabled: !_saving,
                  textCapitalization: TextCapitalization.words,
                  decoration: InputDecoration(
                    labelText: '${_controller.data.settings.clientLabel} *',
                  ),
                  onChanged: (value) {
                    _applyKnownCustomer(value);
                    setState(() {});
                  },
                  validator: (value) => value == null || value.trim().isEmpty
                      ? 'Informe o cliente.'
                      : null,
                ),
                TextFormField(
                  controller: _phoneController,
                  enabled: !_saving,
                  keyboardType: TextInputType.phone,
                  decoration: const InputDecoration(
                    labelText: 'Telefone / WhatsApp',
                    hintText: '(00) 00000-0000',
                  ),
                  onChanged: (_) => setState(() {}),
                  validator: (value) {
                    final digits = (value ?? '').replaceAll(RegExp(r'\D'), '');
                    if (digits.isNotEmpty &&
                        digits.length != 10 &&
                        digits.length != 11) {
                      return 'Use um telefone com 10 ou 11 dígitos.';
                    }
                    return null;
                  },
                ),
              ],
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _profileController,
              enabled: !_saving,
              decoration: InputDecoration(
                labelText: _controller.data.settings.clientDetailLabel,
              ),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _notesController,
              enabled: !_saving,
              minLines: 2,
              maxLines: 3,
              decoration: const InputDecoration(
                labelText: 'Observações',
                alignLabelWithHint: true,
              ),
              onChanged: (_) => setState(() {}),
            ),
          ],
        ),
      ),
    );
  }

  Widget _reviewStep(bool compact) {
    final scheduleCard = _ReviewCard(
      eyebrow: 'AGENDAMENTO',
      title: _dateTimeSummary,
      subtitle: _serviceSummary,
      detail: _professionalResourceSummary,
      price: _priceSummary,
    );
    final clientCard = _ReviewClientCard(
      label: _controller.data.settings.clientLabel.toUpperCase(),
      customer: _customerSummary,
      phone: _phoneController.text.trim(),
      status: widget.appointment?.status,
    );
    return _DialogSection(
      title: 'Revise antes de salvar',
      subtitle: 'Confira os dados principais do agendamento.',
      icon: Icons.verified_outlined,
      child: LayoutBuilder(
        builder: (context, constraints) {
          if (compact || constraints.maxWidth < 680) {
            return Column(
              children: [scheduleCard, const SizedBox(height: 12), clientCard],
            );
          }
          return IntrinsicHeight(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Expanded(flex: 112, child: scheduleCard),
                const SizedBox(width: 14),
                Expanded(flex: 88, child: clientCard),
              ],
            ),
          );
        },
      ),
    );
  }

  ServiceItem? get _selectedService =>
      _services.where((item) => item.id == _serviceId).firstOrNull;

  Professional? get _selectedProfessional =>
      _professionals.where((item) => item.id == _professionalId).firstOrNull;

  String get _dateLabel =>
      '${_date.day.toString().padLeft(2, '0')}/'
      '${_date.month.toString().padLeft(2, '0')}/${_date.year}';

  String get _dateTimeSummary =>
      '$_dateLabel • ${_timeKey(_time)} • $_duration min';

  String get _serviceSummary =>
      _selectedService?.displayName ?? 'Serviço ainda não selecionado';

  String get _professionalResourceSummary {
    final professional =
        _selectedProfessional?.name ?? 'Profissional não definido';
    final resource = _resourceController.text.trim().isEmpty
        ? 'Recurso não definido'
        : _resourceController.text.trim();
    return '$professional • $resource';
  }

  String get _customerSummary => _customerController.text.trim().isEmpty
      ? '${_controller.data.settings.clientLabel} não informado'
      : _customerController.text.trim();

  String get _priceSummary => money(_parsePrice(_priceController.text) ?? 0);

  void _goToStep(int target) {
    target = target.clamp(0, 2);
    if (target > 0 && !_validateScheduleStep()) return;
    if (target > 1 && !_validateClientStep()) return;
    setState(() {
      _stepDirection = target >= _step ? 1 : -1;
      _step = target;
      _error = null;
    });
  }

  void _continueStep() => _goToStep(_step + 1);

  bool _validateScheduleStep() {
    final valid =
        _scheduleFormKey.currentState?.validate() ?? _hasValidScheduleValues;
    final businessError = _businessValidation();
    if (valid && businessError == null) return true;
    setState(() {
      _stepDirection = -1;
      _step = 0;
      _error = businessError ?? 'Revise os campos de horário e serviço.';
    });
    return false;
  }

  bool _validateClientStep() {
    final valid =
        _clientFormKey.currentState?.validate() ?? _hasValidClientValues;
    if (valid) return true;
    setState(() {
      _stepDirection = -1;
      _step = 1;
      _error = 'Revise os dados do cliente.';
    });
    return false;
  }

  bool get _hasValidScheduleValues {
    final price = _parsePrice(_priceController.text);
    return _segment.trim().isNotEmpty &&
        _services.any((item) => item.id == _serviceId) &&
        _professionals.any((item) => item.id == _professionalId) &&
        price != null &&
        price >= 0;
  }

  bool get _hasValidClientValues {
    if (_customerController.text.trim().isEmpty) return false;
    final phoneDigits = _phoneController.text.replaceAll(RegExp(r'\D'), '');
    return phoneDigits.isEmpty ||
        phoneDigits.length == 10 ||
        phoneDigits.length == 11;
  }

  bool _validateAllSteps() {
    if (!_validateScheduleStep()) return false;
    if (!_validateClientStep()) return false;
    return true;
  }

  Widget _wizardFooter(bool compact) {
    final t = AgendaThemeTokens.of(context);
    final primary = ElevatedButton.icon(
      key: Key(
        _editing || _step == 2 ? 'appointment-save' : 'appointment-continue',
      ),
      style: ElevatedButton.styleFrom(
        minimumSize: const Size(128, 44),
        padding: const EdgeInsets.symmetric(horizontal: 14),
      ),
      onPressed: _saving
          ? null
          : (_editing || _step == 2 ? _save : _continueStep),
      icon: _saving
          ? const SizedBox(
              width: 16,
              height: 16,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : Icon(
              _editing || _step == 2
                  ? Icons.check_rounded
                  : Icons.arrow_forward_rounded,
              size: 18,
            ),
      label: Text(
        _saving
            ? 'Salvando...'
            : (_editing
                  ? 'Salvar alterações'
                  : (_step == 2 ? 'Salvar' : 'Continuar')),
      ),
    );
    final navigation = <Widget>[
      if (!_editing && _step == 0)
        OutlinedButton.icon(
          key: const Key('appointment-clear'),
          style: _footerButtonStyle(minWidth: 99),
          onPressed: _saving ? null : _clear,
          icon: const Icon(Icons.refresh_rounded, size: 17),
          label: const Text('Limpar'),
        )
      else if (_step > 0)
        OutlinedButton.icon(
          key: const Key('appointment-back'),
          style: _footerButtonStyle(minWidth: 88),
          onPressed: _saving ? null : () => _goToStep(_step - 1),
          icon: const Icon(Icons.arrow_back_rounded, size: 17),
          label: const Text('Voltar'),
        ),
      if (_editing && _step < 2) ...[
        if (_step > 0) const SizedBox(width: 8),
        OutlinedButton.icon(
          key: const Key('appointment-continue'),
          style: _footerButtonStyle(minWidth: 126),
          onPressed: _saving ? null : _continueStep,
          icon: const Icon(Icons.arrow_forward_rounded, size: 17),
          label: const Text('Próxima etapa'),
        ),
      ],
      const SizedBox(width: 8),
      primary,
    ];
    final editActions = _editing ? _editingActions() : const <Widget>[];

    return Container(
      key: const Key('appointment-dialog-footer'),
      color: t.panel,
      padding: EdgeInsets.all(compact ? 12 : 16),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final stacked = compact || constraints.maxWidth < 780;
          if (stacked) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (editActions.isNotEmpty) ...[
                  Wrap(spacing: 6, runSpacing: 6, children: editActions),
                  const SizedBox(height: 9),
                ],
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    for (final action in navigation)
                      action is SizedBox ? action : Expanded(child: action),
                  ],
                ),
              ],
            );
          }
          return Row(
            children: [
              if (editActions.isNotEmpty)
                Wrap(spacing: 7, runSpacing: 6, children: editActions),
              const Spacer(),
              ...navigation,
            ],
          );
        },
      ),
    );
  }

  List<Widget> _editingActions() => [
    PopupMenuButton<_AppointmentEditAction>(
      key: const Key('appointment-more-actions'),
      enabled: !_saving,
      tooltip: 'Mais ações do agendamento',
      onSelected: _handleEditAction,
      itemBuilder: (context) => const [
        PopupMenuItem(
          value: _AppointmentEditAction.duplicate,
          child: ListTile(
            dense: true,
            leading: Icon(Icons.copy_outlined),
            title: Text('Duplicar agendamento'),
          ),
        ),
        PopupMenuItem(
          value: _AppointmentEditAction.noShow,
          child: ListTile(
            dense: true,
            leading: Icon(Icons.person_off_outlined),
            title: Text('Marcar que faltou'),
          ),
        ),
        PopupMenuDivider(),
        PopupMenuItem(
          value: _AppointmentEditAction.cancel,
          child: ListTile(
            dense: true,
            leading: Icon(Icons.event_busy_outlined, color: Color(0xFFDC2626)),
            title: Text(
              'Cancelar agendamento',
              style: TextStyle(color: Color(0xFFDC2626)),
            ),
          ),
        ),
        PopupMenuItem(
          value: _AppointmentEditAction.delete,
          child: ListTile(
            dense: true,
            leading: Icon(
              Icons.delete_outline_rounded,
              color: Color(0xFFDC2626),
            ),
            title: Text(
              'Excluir definitivamente',
              style: TextStyle(color: Color(0xFFDC2626)),
            ),
          ),
        ),
      ],
      child: IgnorePointer(
        child: OutlinedButton.icon(
          style: _footerButtonStyle(minWidth: 122),
          onPressed: () {},
          icon: const Icon(Icons.more_horiz_rounded, size: 18),
          label: const Text('Mais ações'),
        ),
      ),
    ),
  ];

  Future<void> _handleEditAction(_AppointmentEditAction action) async {
    switch (action) {
      case _AppointmentEditAction.duplicate:
        await _duplicate();
        return;
      case _AppointmentEditAction.noShow:
        await _confirmStatusChange(
          target: AppointmentStatus.noShow,
          title: 'Marcar que o cliente faltou?',
          message:
              'O horário será encerrado como falta e não ficará mais em aberto.',
          confirmLabel: 'Marcar falta',
        );
        return;
      case _AppointmentEditAction.cancel:
        await _confirmStatusChange(
          target: AppointmentStatus.cancelled,
          title: 'Cancelar este agendamento?',
          message:
              'O horário será liberado e o atendimento ficará registrado como cancelado.',
          confirmLabel: 'Cancelar agendamento',
          danger: true,
        );
        return;
      case _AppointmentEditAction.delete:
        await _delete();
        return;
    }
  }

  Future<void> _confirmStatusChange({
    required AppointmentStatus target,
    required String title,
    required String message,
    required String confirmLabel,
    bool danger = false,
  }) async {
    final confirmed =
        await showAgendaDialog<bool>(
          context: context,
          builder: (confirmationContext) => AlertDialog(
            title: Text(title),
            content: Text(message),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(confirmationContext, false),
                child: const Text('Voltar'),
              ),
              FilledButton(
                style: danger
                    ? FilledButton.styleFrom(
                        backgroundColor: const Color(0xFFDC2626),
                      )
                    : null,
                onPressed: () => Navigator.pop(confirmationContext, true),
                child: Text(confirmLabel),
              ),
            ],
          ),
        ) ??
        false;
    if (confirmed && mounted) await _setStatus(target);
  }

  ButtonStyle _footerButtonStyle({
    required double minWidth,
    double height = 44,
    bool small = false,
    bool danger = false,
  }) {
    return OutlinedButton.styleFrom(
      foregroundColor: danger ? const Color(0xFFDC2626) : null,
      minimumSize: Size(minWidth, height),
      padding: EdgeInsets.symmetric(horizontal: small ? 9 : 13),
      textStyle: TextStyle(fontSize: small ? 12 : 14),
    );
  }

  Widget _resourceField() {
    final resources = _controller.data.settings.resources;
    if (resources.isEmpty) {
      return TextFormField(
        controller: _resourceController,
        enabled: !_saving,
        decoration: InputDecoration(
          labelText: _controller.data.settings.resourceLabel,
        ),
        onChanged: (_) => setState(() {}),
      );
    }
    final current = _resourceController.text.trim();
    final values = <String>{
      ...resources,
      if (current.isNotEmpty) current,
    }.toList();
    return DropdownButtonFormField<String>(
      key: ValueKey('resource-$current'),
      isExpanded: true,
      initialValue: current.isEmpty ? null : current,
      decoration: InputDecoration(
        labelText: '${_controller.data.settings.resourceLabel} *',
      ),
      items: [
        for (final resource in values)
          DropdownMenuItem(value: resource, child: Text(resource)),
      ],
      onChanged: _saving
          ? null
          : (value) {
              setState(() => _resourceController.text = value ?? '');
            },
      validator: (value) =>
          value == null || value.trim().isEmpty ? 'Selecione o recurso.' : null,
    );
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(2020),
      lastDate: DateTime(DateTime.now().year + 5, 12, 31),
    );
    if (picked != null && mounted) setState(() => _date = picked);
  }

  void _selectService(String? value) {
    setState(() {
      _serviceId = value;
      final service = _services.where((item) => item.id == value).firstOrNull;
      if (service == null) return;
      _duration = service.durationMinutes;
      _priceController.text = service.price
          .toStringAsFixed(2)
          .replaceAll('.', ',');
      if (service.defaultResource.trim().isNotEmpty) {
        _resourceController.text = service.defaultResource;
      }
    });
  }

  void _applyKnownCustomer(String value) {
    final normalized = value.trim().toLowerCase();
    final match = _controller.data.customers
        .where((item) => item.name.trim().toLowerCase() == normalized)
        .firstOrNull;
    if (match == null) return;
    if (_phoneController.text.trim().isEmpty) {
      _phoneController.text = match.phone;
    }
    if (_profileController.text.trim().isEmpty) {
      _profileController.text = match.profile;
    }
  }

  double? _parsePrice(String? value) {
    var normalized = (value ?? '').trim().replaceAll(' ', '');
    if (normalized.contains(',')) {
      normalized = normalized.replaceAll('.', '').replaceAll(',', '.');
    }
    return double.tryParse(normalized);
  }

  DateTime get _start =>
      DateTime(_date.year, _date.month, _date.day, _time.hour, _time.minute);

  bool get _scheduleUnchanged {
    final source = widget.appointment;
    if (source == null) return false;
    return source.start.isAtSameMomentAs(_start) &&
        source.durationMinutes == _duration;
  }

  bool get _editingScheduleOutsideWindow {
    final source = widget.appointment;
    if (source == null) return false;
    return _controller.validateBusinessWindow(source.start, source.end) != null;
  }

  String? _businessValidation() {
    final start = _start;
    final end = start.add(Duration(minutes: _duration));
    final businessError = _controller.validateBusinessWindow(start, end);
    if (businessError != null &&
        !_scheduleUnchanged &&
        !_isScheduleExceptionAcknowledged) {
      return 'Este horário é excepcional. Use a sugestão ou confirme abaixo que você foi avisado antes de continuar.';
    }
    if (!_editing && start.isBefore(DateTime.now())) {
      return 'Escolha um horário futuro para o novo agendamento.';
    }
    return null;
  }

  _ScheduleAssistantIssue? get _scheduleAssistantIssue =>
      _describeScheduleIssue(_start, _start.add(Duration(minutes: _duration)));

  bool get _isScheduleExceptionAcknowledged =>
      _acknowledgedScheduleKey ==
      _scheduleKey(_start, _start.add(Duration(minutes: _duration)));

  _ScheduleAssistantIssue? _describeScheduleIssue(
    DateTime start,
    DateTime end,
  ) {
    final settings = _controller.data.settings;
    final professional = _selectedProfessional?.name.trim().isNotEmpty ?? false
        ? _selectedProfessional!.name.trim()
        : 'O profissional';
    final workdayStart = DateTime(
      start.year,
      start.month,
      start.day,
      settings.workdayStartHour,
    );
    final workdayEnd = DateTime(
      start.year,
      start.month,
      start.day,
      settings.workdayEndHour,
    );

    if (!_controller.isConfiguredWorkday(start)) {
      return _ScheduleAssistantIssue(
        'closed_day',
        'O estabelecimento está fechado no dia selecionado. '
            '$professional precisará trabalhar em um dia sem expediente.',
      );
    }
    if (start.isBefore(workdayStart)) {
      final minutes = workdayStart.difference(start).inMinutes.clamp(1, 1440);
      return _ScheduleAssistantIssue(
        'before_opening',
        'O atendimento começa $minutes min antes da abertura '
            '(${_clock(workdayStart)}). $professional precisará chegar antes do expediente.',
      );
    }
    if (end.isAfter(workdayEnd)) {
      final minutes = end.difference(workdayEnd).inMinutes.clamp(1, 1440);
      return _ScheduleAssistantIssue(
        'after_closing',
        'O atendimento terminaria às ${_clock(end)}, $minutes min depois do '
            'fechamento (${_clock(workdayEnd)}). $professional poderá precisar ficar após o expediente.',
      );
    }
    if (_controller.overlapsConfiguredBreak(start, end)) {
      return _ScheduleAssistantIssue(
        'break_overlap',
        'O atendimento ocupa o intervalo de '
            '${settings.workdayBreakStartHour.toString().padLeft(2, '0')}:00 às '
            '${settings.workdayBreakEndHour.toString().padLeft(2, '0')}:00. '
            '$professional poderá ficar sem parte do horário de almoço.',
      );
    }
    return null;
  }

  DateTime? _findScheduleSuggestion() {
    final requested = _start;
    final professionalId = _professionalId ?? '';
    final resource = _resourceController.text.trim().toLowerCase();
    DateTime? best;
    var bestScore = double.infinity;

    for (var dayOffset = 0; dayOffset < 15; dayOffset++) {
      final day = requested.add(Duration(days: dayOffset));
      if (!_controller.isConfiguredWorkday(day)) continue;
      final settings = _controller.data.settings;
      final dayStart = DateTime(
        day.year,
        day.month,
        day.day,
        settings.workdayStartHour,
      );
      final dayEnd = DateTime(
        day.year,
        day.month,
        day.day,
        settings.workdayEndHour,
      );
      for (
        var candidate = dayStart;
        !candidate.add(Duration(minutes: _duration)).isAfter(dayEnd);
        candidate = candidate.add(const Duration(minutes: 15))
      ) {
        final candidateEnd = candidate.add(Duration(minutes: _duration));
        if (_controller.overlapsConfiguredBreak(candidate, candidateEnd) ||
            _hasScheduleConflict(
              candidate,
              candidateEnd,
              professionalId,
              resource,
            ) ||
            (!_editing &&
                candidate.isBefore(
                  DateTime.now().subtract(const Duration(minutes: 5)),
                ))) {
          continue;
        }
        final dayDistance =
            candidate.difference(requested).inDays.abs() * 1440.0;
        final timeDistance =
            (candidate.hour * 60 +
                    candidate.minute -
                    (requested.hour * 60 + requested.minute))
                .abs()
                .toDouble();
        final score = dayDistance + timeDistance;
        if (score < bestScore) {
          best = candidate;
          bestScore = score;
        }
      }
    }
    return best;
  }

  bool _hasScheduleConflict(
    DateTime start,
    DateTime end,
    String professionalId,
    String resource,
  ) => _controller.data.appointments.any((item) {
    if (item.id == widget.appointment?.id ||
        item.status == AppointmentStatus.cancelled ||
        item.status == AppointmentStatus.noShow) {
      return false;
    }
    final overlaps = start.isBefore(item.end) && end.isAfter(item.start);
    if (!overlaps) return false;
    final sameProfessional =
        professionalId.isNotEmpty && item.professionalId == professionalId;
    final sameResource =
        resource.isNotEmpty &&
        item.resourceName.trim().toLowerCase() == resource;
    return sameProfessional || sameResource;
  });

  Widget _scheduleAssistantCard(_ScheduleAssistantIssue issue) {
    final t = AgendaThemeTokens.of(context);
    final suggestion = _findScheduleSuggestion();
    final end = _start.add(Duration(minutes: _duration));
    final message = suggestion == null
        ? '${issue.message} Não encontrei outro encaixe livre nos próximos 15 dias.'
        : '${issue.message} Melhor encaixe livre: '
              '${DateUtils.isSameDay(suggestion, _start) ? 'hoje, ' : '${_shortDate(suggestion)}, '}'
              '${_clock(suggestion)}–${_clock(suggestion.add(Duration(minutes: _duration)))}.';

    return Container(
      key: const Key('schedule-assistant-card'),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF8F3),
        border: Border.all(color: t.accent),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Container(
                width: 28,
                height: 28,
                decoration: BoxDecoration(
                  color: t.accent,
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.auto_fix_high,
                  color: Colors.white,
                  size: 15,
                ),
              ),
              const SizedBox(width: 9),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Sugestão inteligente de horário',
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 12.5,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    Text(
                      _isScheduleExceptionAcknowledged
                          ? 'Exceção confirmada · o aviso ficará registrado'
                          : 'Regras da agenda verificadas',
                      style: TextStyle(color: t.muted, fontSize: 9.5),
                    ),
                  ],
                ),
              ),
            ],
          ),
          Padding(
            padding: const EdgeInsets.only(left: 37, top: 7),
            child: Text(
              message,
              style: TextStyle(color: t.ink, fontSize: 11.5, height: 1.35),
            ),
          ),
          const SizedBox(height: 9),
          LayoutBuilder(
            builder: (context, constraints) {
              final confirmation = CheckboxListTile(
                key: const Key('schedule-assistant-acknowledge'),
                contentPadding: EdgeInsets.zero,
                dense: true,
                controlAffinity: ListTileControlAffinity.leading,
                value: _isScheduleExceptionAcknowledged,
                onChanged: (value) => setState(() {
                  _acknowledgedScheduleKey = value == true
                      ? _scheduleKey(_start, end)
                      : '';
                  _error = null;
                }),
                title: Text(
                  'Fui avisado e quero manter ${_clock(_start)}–${_clock(end)}',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 10.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              );
              final useSuggestion = suggestion == null
                  ? null
                  : SizedBox(
                      height: 34,
                      child: FilledButton.icon(
                        key: const Key('schedule-assistant-use-suggestion'),
                        onPressed: () => setState(() {
                          _date = DateUtils.dateOnly(suggestion);
                          _time = TimeOfDay.fromDateTime(suggestion);
                          _acknowledgedScheduleKey = '';
                          _error = null;
                        }),
                        icon: const Icon(Icons.schedule, size: 15),
                        label: Text(
                          DateUtils.isSameDay(suggestion, _start)
                              ? 'Usar ${_clock(suggestion)}'
                              : 'Usar ${_shortDate(suggestion)} ${_clock(suggestion)}',
                        ),
                      ),
                    );
              if (constraints.maxWidth < 560) {
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [confirmation, ?useSuggestion],
                );
              }
              return Row(
                children: [
                  Expanded(child: confirmation),
                  if (useSuggestion != null) ...[
                    const SizedBox(width: 12),
                    useSuggestion,
                  ],
                ],
              );
            },
          ),
        ],
      ),
    );
  }

  String _scheduleKey(DateTime start, DateTime end) =>
      '${start.toIso8601String()}|${end.toIso8601String()}';

  String _clock(DateTime value) =>
      '${value.hour.toString().padLeft(2, '0')}:'
      '${value.minute.toString().padLeft(2, '0')}';

  String _shortDate(DateTime value) =>
      '${value.day.toString().padLeft(2, '0')}/'
      '${value.month.toString().padLeft(2, '0')}';

  Appointment _draft({DateTime? start}) {
    final source = widget.appointment ?? widget.template;
    final editingSource = widget.appointment;
    final service = _services
        .where((item) => item.id == _serviceId)
        .firstOrNull;
    final professional = _professionals
        .where((item) => item.id == _professionalId)
        .firstOrNull;
    final draftStart = start ?? _start;
    final draftEnd = draftStart.add(Duration(minutes: _duration));
    final issue = _describeScheduleIssue(draftStart, draftEnd);
    final acknowledged =
        issue != null &&
        _acknowledgedScheduleKey == _scheduleKey(draftStart, draftEnd);
    return Appointment(
      id: editingSource?.id,
      segment: _segment.trim(),
      customerId: source?.customerId ?? '',
      customerName: _customerController.text.trim(),
      customerPhone: _phoneController.text.trim(),
      customerProfile: _profileController.text.trim(),
      serviceId: service?.id ?? source?.serviceId ?? '',
      serviceName: service?.name ?? source?.serviceName ?? '',
      professionalId: professional?.id ?? source?.professionalId ?? '',
      professionalName: professional?.name ?? source?.professionalName ?? '',
      resourceName: _resourceController.text.trim(),
      start: draftStart,
      durationMinutes: _duration,
      price: _parsePrice(_priceController.text) ?? 0,
      status: editingSource?.status ?? AppointmentStatus.scheduled,
      notes: _notesController.text.trim(),
      externalSource: editingSource?.externalSource ?? '',
      externalReference: editingSource?.externalReference ?? '',
      bookingChannel: editingSource?.bookingChannel ?? '',
      channelConversationId: editingSource?.channelConversationId ?? '',
      channelExternalUserId: editingSource?.channelExternalUserId ?? '',
      channelUsername: editingSource?.channelUsername ?? '',
      scheduleExceptionAcknowledged: acknowledged,
      scheduleExceptionReason: acknowledged ? issue.message : '',
      scheduleExceptionAssistantSource: acknowledged ? 'local-rules' : '',
      scheduleExceptionAcknowledgedAt: acknowledged
          ? (editingSource?.scheduleExceptionAcknowledgedAt ?? DateTime.now())
          : null,
      createdAt: editingSource?.createdAt,
      updatedAt: DateTime.now(),
    );
  }

  Future<void> _save() async {
    FocusScope.of(context).unfocus();
    if (!_validateAllSteps()) return;
    setState(() {
      _saving = true;
      _error = null;
    });
    final error = await _controller.saveAppointment(_draft());
    if (!mounted) return;
    if (error != null) {
      setState(() {
        _saving = false;
        _error = error;
      });
      return;
    }
    Navigator.pop(
      context,
      _editing ? 'Agendamento atualizado.' : 'Agendamento criado.',
    );
  }

  void _clear() {
    setState(() {
      _customerController.clear();
      _phoneController.clear();
      _profileController.clear();
      _notesController.clear();
      _serviceId = null;
      _professionalId = null;
      _resourceController.clear();
      _duration = 30;
      _priceController.text = '0,00';
      _step = 0;
      _error = null;
    });
  }

  Future<void> _duplicate() async {
    if (!_validateAllSteps()) return;
    setState(() {
      _saving = true;
      _error = null;
    });
    final source = _draft(start: _start.add(const Duration(days: 7)));
    final duplicate = Appointment(
      segment: source.segment,
      customerName: source.customerName,
      customerPhone: source.customerPhone,
      customerProfile: source.customerProfile,
      serviceId: source.serviceId,
      serviceName: source.serviceName,
      professionalId: source.professionalId,
      professionalName: source.professionalName,
      resourceName: source.resourceName,
      start: source.start,
      durationMinutes: source.durationMinutes,
      price: source.price,
      status: AppointmentStatus.scheduled,
      notes: source.notes,
      externalSource: source.externalSource,
      bookingChannel: source.bookingChannel,
      channelExternalUserId: source.channelExternalUserId,
      channelUsername: source.channelUsername,
    );
    final error = await _controller.saveAppointment(duplicate);
    if (!mounted) return;
    if (error != null) {
      setState(() {
        _saving = false;
        _error = error;
      });
      return;
    }
    Navigator.pop(
      context,
      'Agendamento duplicado para ${shortDate(duplicate.start)}.',
    );
  }

  Future<void> _setStatus(AppointmentStatus target) async {
    final source = widget.appointment;
    if (source == null) return;
    setState(() {
      _saving = true;
      _error = null;
    });
    final error = await _controller.setAppointmentStatus(source, target);
    if (!mounted) return;
    if (error != null) {
      setState(() {
        _saving = false;
        _error = error;
      });
      return;
    }
    Navigator.pop(
      context,
      'Status alterado para ${appointmentStatusLabel(target)}.',
    );
  }

  Future<void> _delete() async {
    final source = widget.appointment;
    if (source == null) return;
    final confirmed =
        await showAgendaDialog<bool>(
          context: context,
          builder: (confirmationContext) => AlertDialog(
            title: const Text('Excluir agendamento?'),
            content: Text(
              'O horário de ${source.customerName} será removido definitivamente.',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(confirmationContext, false),
                child: const Text('Voltar'),
              ),
              FilledButton(
                style: FilledButton.styleFrom(
                  backgroundColor: const Color(0xFFDC2626),
                ),
                onPressed: () => Navigator.pop(confirmationContext, true),
                child: const Text('Excluir'),
              ),
            ],
          ),
        ) ??
        false;
    if (!confirmed || !mounted) return;
    setState(() => _saving = true);
    await _controller.deleteAppointment(source.id);
    if (mounted) Navigator.pop(context, 'Agendamento excluído.');
  }
}

class _AppointmentStepButton extends StatelessWidget {
  const _AppointmentStepButton({
    required this.number,
    required this.label,
    required this.active,
    required this.reached,
    required this.compact,
    required this.onTap,
  });

  final int number;
  final String label;
  final bool active;
  final bool reached;
  final bool compact;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final circle = AnimatedContainer(
      duration: AgendaMotion.duration(context, AgendaMotion.fast),
      curve: AgendaMotion.enterCurve,
      width: compact ? 28 : 32,
      height: compact ? 28 : 32,
      decoration: BoxDecoration(
        color: reached ? t.accentDark : t.panel,
        border: Border.all(color: reached ? t.accentDark : t.line),
        shape: BoxShape.circle,
      ),
      alignment: Alignment.center,
      child: AnimatedSwitcher(
        duration: AgendaMotion.duration(context, AgendaMotion.fast),
        child: reached && !active
            ? const Icon(
                Icons.check_rounded,
                key: ValueKey<String>('done'),
                color: Colors.white,
                size: 17,
              )
            : Text(
                '$number',
                key: const ValueKey<String>('number'),
                style: TextStyle(
                  color: reached ? Colors.white : t.muted,
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                ),
              ),
      ),
    );
    final labelWidget = AnimatedDefaultTextStyle(
      duration: AgendaMotion.duration(context, AgendaMotion.fast),
      curve: AgendaMotion.enterCurve,
      style: TextStyle(
        color: active ? t.ink : t.muted,
        fontSize: compact ? 10 : 13,
        fontWeight: active ? FontWeight.w700 : FontWeight.w500,
      ),
      child: Text(label, maxLines: 1, overflow: TextOverflow.ellipsis),
    );
    return Expanded(
      child: Semantics(
        button: true,
        selected: active,
        label: 'Etapa $number, $label',
        child: InkWell(
          key: Key('appointment-step-$number'),
          onTap: onTap,
          borderRadius: BorderRadius.circular(12),
          child: Padding(
            padding: EdgeInsets.symmetric(
              horizontal: compact ? 2 : 10,
              vertical: compact ? 1 : 8,
            ),
            child: compact
                ? Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [circle, const SizedBox(height: 1), labelWidget],
                  )
                : Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      circle,
                      const SizedBox(width: 10),
                      Flexible(child: labelWidget),
                    ],
                  ),
          ),
        ),
      ),
    );
  }
}

class _AppointmentStepConnector extends StatelessWidget {
  const _AppointmentStepConnector({
    required this.active,
    required this.compact,
  });

  final bool active;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return AnimatedContainer(
      duration: AgendaMotion.duration(context, AgendaMotion.standard),
      curve: AgendaMotion.enterCurve,
      width: compact ? 14 : 70,
      height: 2,
      decoration: BoxDecoration(
        color: active ? t.accentDark : t.line,
        borderRadius: BorderRadius.circular(1),
      ),
    );
  }
}

class _ScheduleSummaryBar extends StatelessWidget {
  const _ScheduleSummaryBar({
    required this.dateTime,
    required this.service,
    required this.professionalAndResource,
    required this.price,
  });

  final String dateTime;
  final String service;
  final String professionalAndResource;
  final String price;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: BoxDecoration(
        color: t.accentSoft,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(13),
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          if (constraints.maxWidth < 620) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: _summaryText(context, dateTime, strong: true),
                    ),
                    const SizedBox(width: 10),
                    Text(
                      price,
                      style: TextStyle(
                        color: t.accent,
                        fontSize: 14,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 5),
                _summaryText(context, '$service • $professionalAndResource'),
              ],
            );
          }
          return Row(
            children: [
              Expanded(
                flex: 11,
                child: _summaryText(context, dateTime, strong: true),
              ),
              const SizedBox(width: 12),
              Expanded(flex: 12, child: _summaryText(context, service)),
              const SizedBox(width: 12),
              Expanded(
                flex: 11,
                child: _summaryText(context, professionalAndResource),
              ),
              const SizedBox(width: 12),
              Text(
                price,
                style: TextStyle(
                  color: t.accent,
                  fontSize: 14,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _summaryText(
    BuildContext context,
    String value, {
    bool strong = false,
  }) {
    final t = AgendaThemeTokens.of(context);
    return Text(
      value,
      maxLines: 1,
      overflow: TextOverflow.ellipsis,
      style: TextStyle(
        color: strong ? t.ink : t.muted,
        fontSize: 11.5,
        fontWeight: strong ? FontWeight.w700 : FontWeight.w400,
      ),
    );
  }
}

class _ReviewCard extends StatelessWidget {
  const _ReviewCard({
    required this.eyebrow,
    required this.title,
    required this.subtitle,
    required this.detail,
    required this.price,
  });

  final String eyebrow;
  final String title;
  final String subtitle;
  final String detail;
  final String price;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 17),
      decoration: BoxDecoration(
        color: t.ink,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            eyebrow,
            style: TextStyle(
              color: t.accent,
              fontSize: 10,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            title,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 20,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 9),
          Text(
            subtitle,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 14,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            detail,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(color: Color(0xADFFFFFF), fontSize: 11.5),
          ),
          const SizedBox(height: 15),
          const Divider(height: 1, color: Color(0x24FFFFFF)),
          const SizedBox(height: 12),
          Text(
            price,
            style: TextStyle(
              color: t.accent,
              fontSize: 24,
              fontWeight: FontWeight.w800,
            ),
          ),
        ],
      ),
    );
  }
}

class _ReviewClientCard extends StatelessWidget {
  const _ReviewClientCard({
    required this.label,
    required this.customer,
    required this.phone,
    required this.status,
  });

  final String label;
  final String customer;
  final String phone;
  final AppointmentStatus? status;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final contact = phone.isEmpty ? 'Telefone não informado' : phone;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 17),
      decoration: BoxDecoration(
        color: t.accentSoft,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  label,
                  style: TextStyle(
                    color: t.accent,
                    fontSize: 10,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
              if (status != null)
                AppointmentStatusBadge(status: status!, compact: true),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            '$customer • $contact',
            style: TextStyle(
              color: t.ink,
              fontSize: 17,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 16),
          Divider(height: 1, color: t.line),
          const SizedBox(height: 14),
          Row(
            children: [
              Icon(Icons.shield_outlined, color: t.accent, size: 18),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Dados prontos para confirmação',
                  style: TextStyle(color: t.muted, fontSize: 11.5),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            'Ao salvar, o horário será validado contra conflitos de profissional e recurso.',
            style: TextStyle(color: t.muted, fontSize: 11),
          ),
        ],
      ),
    );
  }
}

class _DialogSection extends StatelessWidget {
  const _DialogSection({
    required this.title,
    required this.icon,
    required this.child,
    this.subtitle,
  });

  final String title;
  final IconData icon;
  final Widget child;
  final String? subtitle;

  @override
  Widget build(BuildContext context) {
    return AgendaPanel(
      padding: const EdgeInsets.all(15),
      radius: 16,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AgendaSectionTitle(title: title, subtitle: subtitle, icon: icon),
          const SizedBox(height: 15),
          child,
        ],
      ),
    );
  }
}

class _ResponsiveFields extends StatelessWidget {
  const _ResponsiveFields({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = constraints.maxWidth < 560 ? 1 : 2;
        const spacing = 12.0;
        final width =
            (constraints.maxWidth - spacing * (columns - 1)) / columns;
        return Wrap(
          spacing: spacing,
          runSpacing: spacing,
          children: [
            for (final child in children) SizedBox(width: width, child: child),
          ],
        );
      },
    );
  }
}
