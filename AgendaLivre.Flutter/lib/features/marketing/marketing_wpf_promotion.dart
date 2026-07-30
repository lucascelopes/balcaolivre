import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../app/theme/agenda_theme.dart';
import '../../core/business_profile.dart';
import '../../core/motion.dart';
import '../../domain/models/marketing_catalog_publication.dart';
import '../../domain/models/service_item.dart';

class MarketingWpfPromotion extends StatefulWidget {
  const MarketingWpfPromotion({
    super.key,
    required this.services,
    required this.profile,
    required this.onBack,
    required this.onPublish,
  });

  final List<ServiceItem> services;
  final AgendaBusinessProfile profile;
  final VoidCallback onBack;
  final ValueChanged<MarketingSitePromotion> onPublish;

  @override
  State<MarketingWpfPromotion> createState() => _MarketingWpfPromotionState();
}

class _MarketingWpfPromotionState extends State<MarketingWpfPromotion> {
  late final List<_PromotionService> _services;
  final _search = TextEditingController();
  late final TextEditingController _name;
  final _limit = TextEditingController(text: '1');
  final Set<int> _selected = <int>{};
  DateTime _start = DateUtils.dateOnly(DateTime.now());
  late DateTime _end;
  bool _featured = true;
  String _category = 'Todas as categorias';

  @override
  void initState() {
    super.initState();
    _end = _start.add(const Duration(days: 7));
    _services = _buildServices(widget.services);
    _name = TextEditingController(text: widget.profile.marketingReturnTitle);
    _selected.addAll(
      List<int>.generate(
        _services.length.clamp(0, 2).toInt(),
        (index) => index,
      ),
    );
    _search.addListener(_refresh);
    _name.addListener(_refresh);
  }

  @override
  void dispose() {
    _search.removeListener(_refresh);
    _name.removeListener(_refresh);
    _search.dispose();
    _name.dispose();
    _limit.dispose();
    super.dispose();
  }

  void _refresh() => setState(() {});

  List<_PromotionService> _buildServices(List<ServiceItem> source) {
    final active = source.where((item) => item.isActive).take(8).toList();
    if (active.isNotEmpty) {
      return [
        for (final item in active)
          _PromotionService(
            id: item.id,
            name: item.name.trim().isEmpty ? 'Serviço' : item.name.trim(),
            category: item.category.trim().isEmpty
                ? 'Serviços'
                : item.category.trim(),
            durationMinutes: item.durationMinutes,
            price: item.price,
          ),
      ];
    }
    return const <_PromotionService>[];
  }

  List<int> get _visibleIndexes {
    final query = _search.text.trim().toLowerCase();
    return [
      for (var index = 0; index < _services.length; index++)
        if ((_category == 'Todas as categorias' ||
                _services[index].category == _category) &&
            (query.isEmpty ||
                '${_services[index].name} ${_services[index].category}'
                    .toLowerCase()
                    .contains(query)))
          index,
    ];
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final width = MediaQuery.sizeOf(context).width;
    final desktop = width >= 980;
    return ColoredBox(
      color: t.appBackground,
      child: SingleChildScrollView(
        key: const Key('marketing-promotion-scroll'),
        padding: EdgeInsets.fromLTRB(
          desktop ? 18 : 12,
          desktop ? 18 : 12,
          desktop ? 18 : 12,
          96,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AgendaReveal(child: _header(t, desktop)),
            const SizedBox(height: 18),
            if (desktop)
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: AgendaReveal(
                      delay: const Duration(milliseconds: 50),
                      child: _selectionPanel(t, desktop),
                    ),
                  ),
                  const SizedBox(width: 16),
                  SizedBox(
                    width: 320,
                    child: AgendaReveal(
                      delay: const Duration(milliseconds: 90),
                      child: _summaryPanel(t, desktop),
                    ),
                  ),
                ],
              )
            else ...[
              AgendaReveal(
                delay: const Duration(milliseconds: 45),
                child: _selectionPanel(t, desktop),
              ),
              const SizedBox(height: 14),
              AgendaReveal(
                delay: const Duration(milliseconds: 85),
                child: _summaryPanel(t, desktop),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _header(AgendaThemeTokens t, bool desktop) => Row(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      SizedBox(
        width: desktop ? 96 : 44,
        height: 42,
        child: OutlinedButton(
          key: const Key('marketing-promotion-back'),
          onPressed: widget.onBack,
          style: OutlinedButton.styleFrom(
            padding: EdgeInsets.zero,
            foregroundColor: t.ink,
          ),
          child: const Icon(Icons.arrow_back_rounded),
        ),
      ),
      const SizedBox(width: 14),
      Expanded(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Criar promoção no site',
              style: TextStyle(
                color: t.ink,
                fontSize: desktop ? 27 : 23,
                height: 1.08,
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              'Escolha ${widget.profile.servicePlural}, defina os novos preços e publique no catálogo online.',
              style: TextStyle(color: t.muted, fontSize: 11.5),
            ),
          ],
        ),
      ),
      if (desktop)
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 9),
          decoration: BoxDecoration(
            color: t.accentSoft,
            border: Border.all(color: t.accent.withValues(alpha: .52)),
            borderRadius: BorderRadius.circular(18),
          ),
          child: Row(
            children: [
              Icon(Icons.public_rounded, color: t.ink, size: 16),
              const SizedBox(width: 7),
              Text(
                'Exclusivo para o site',
                style: TextStyle(
                  color: t.ink,
                  fontSize: 10.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
    ],
  );

  Widget _selectionPanel(AgendaThemeTokens t, bool desktop) => _surface(
    t,
    padding: EdgeInsets.all(desktop ? 20 : 14),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Seleção de ${widget.profile.servicePlural}',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 17,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Selecione um ou mais itens para incluir na promoção.',
                    style: TextStyle(color: t.muted, fontSize: 11),
                  ),
                ],
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 9),
              decoration: BoxDecoration(
                color: t.appBackground,
                borderRadius: BorderRadius.circular(12),
              ),
              child: AgendaAnimatedValue(
                value: '${_selected.length}',
                builder: (context, value) => Text(
                  '$value selecionado${value == '1' ? '' : 's'}',
                  style: TextStyle(color: t.muted, fontSize: 9.5),
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        if (desktop)
          Row(
            children: [
              Expanded(child: _searchField(t)),
              const SizedBox(width: 12),
              SizedBox(width: 190, child: _categoryField(t)),
            ],
          )
        else ...[
          _searchField(t),
          const SizedBox(height: 9),
          _categoryField(t),
        ],
        const SizedBox(height: 12),
        if (desktop) _tableHeader(t),
        ConstrainedBox(
          constraints: BoxConstraints(maxHeight: desktop ? 290 : 520),
          child: _services.isEmpty
              ? Container(
                  key: const Key('marketing-promotion-empty-services'),
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: t.appBackground,
                    border: Border.all(color: t.line),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(widget.profile.icon, color: t.accentDark, size: 26),
                      const SizedBox(height: 8),
                      Text(
                        'Cadastre ${widget.profile.servicePlural} antes de criar uma promoção.',
                        textAlign: TextAlign.center,
                        style: TextStyle(color: t.ink, fontSize: 11.5),
                      ),
                    ],
                  ),
                )
              : ListView.builder(
                  shrinkWrap: true,
                  itemCount: _visibleIndexes.length,
                  itemBuilder: (_, row) {
                    final index = _visibleIndexes[row];
                    return desktop
                        ? _desktopServiceRow(t, index)
                        : _mobileServiceCard(t, index);
                  },
                ),
        ),
        const SizedBox(height: 10),
        Row(
          children: [
            Icon(
              _selected.isEmpty
                  ? Icons.check_box_outline_blank_rounded
                  : Icons.check_box_rounded,
              color: _selected.isEmpty ? t.muted : t.ink,
              size: 19,
            ),
            const SizedBox(width: 8),
            Expanded(
              child: AgendaAnimatedValue(
                value: '${_selected.length}',
                builder: (context, value) => Text(
                  '$value selecionado${value == '1' ? '' : 's'}',
                  style: TextStyle(color: t.ink, fontSize: 10.5),
                ),
              ),
            ),
            TextButton(
              onPressed: _selected.isEmpty
                  ? null
                  : () => setState(_selected.clear),
              child: const Text('Limpar seleção'),
            ),
          ],
        ),
      ],
    ),
  );

  Widget _searchField(AgendaThemeTokens t) => TextField(
    key: const Key('marketing-promotion-search'),
    controller: _search,
    decoration: InputDecoration(
      labelText: 'Buscar ${widget.profile.serviceSingular}',
      prefixIcon: const Icon(Icons.search_rounded, size: 19),
    ),
  );

  Widget _categoryField(AgendaThemeTokens t) => DropdownButtonFormField<String>(
    initialValue: _category,
    isExpanded: true,
    decoration: const InputDecoration(labelText: 'Categoria'),
    items:
        [
              'Todas as categorias',
              ...{for (final service in _services) service.category},
            ]
            .map((value) => DropdownMenuItem(value: value, child: Text(value)))
            .toList(),
    onChanged: (value) => setState(() {
      _category = value ?? 'Todas as categorias';
    }),
  );

  Widget _tableHeader(AgendaThemeTokens t) => Container(
    height: 38,
    decoration: BoxDecoration(
      color: t.appBackground,
      border: Border.all(color: t.line),
    ),
    child: Row(
      children: [
        const SizedBox(width: 62),
        Expanded(flex: 250, child: _headerLabel(t, 'SERVIÇO')),
        Expanded(flex: 90, child: _headerLabel(t, 'DURAÇÃO')),
        Expanded(flex: 100, child: _headerLabel(t, 'PREÇO ATUAL')),
        Expanded(flex: 120, child: _headerLabel(t, 'PREÇO PROMO')),
        Expanded(flex: 90, child: _headerLabel(t, 'DESCONTO')),
      ],
    ),
  );

  Widget _headerLabel(AgendaThemeTokens t, String value) =>
      Text(value, style: TextStyle(color: t.ink, fontSize: 8.5));

  Widget _desktopServiceRow(AgendaThemeTokens t, int index) {
    final service = _services[index];
    final selected = _selected.contains(index);
    final promotional = service.price * .85;
    return InkWell(
      key: Key('marketing-promotion-service-$index'),
      onTap: () => _toggle(index),
      child: AnimatedContainer(
        duration: AgendaMotion.duration(context, AgendaMotion.fast),
        height: 64,
        decoration: BoxDecoration(
          color: selected ? t.accentSoft.withValues(alpha: .35) : t.panel,
          border: Border(
            left: BorderSide(color: selected ? t.accent : t.line),
            right: BorderSide(color: t.line),
            bottom: BorderSide(color: t.line),
          ),
        ),
        child: Row(
          children: [
            SizedBox(
              width: 62,
              child: Checkbox(
                value: selected,
                onChanged: (_) => _toggle(index),
              ),
            ),
            Expanded(
              flex: 250,
              child: Row(
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(5),
                    child: Image.asset(
                      'assets/branding/marketing-campaign-hair.png',
                      width: 46,
                      height: 42,
                      fit: BoxFit.cover,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          service.name,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(color: t.ink, fontSize: 10.8),
                        ),
                        Text(
                          service.category,
                          style: TextStyle(color: t.muted, fontSize: 8.5),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            Expanded(
              flex: 90,
              child: Text(
                '${service.durationMinutes} min',
                style: TextStyle(color: t.ink, fontSize: 10.5),
              ),
            ),
            Expanded(
              flex: 100,
              child: Text(
                _currency(service.price),
                style: TextStyle(color: t.ink, fontSize: 10.5),
              ),
            ),
            Expanded(
              flex: 120,
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    promotional.toStringAsFixed(2).replaceAll('.', ','),
                    style: TextStyle(color: t.ink, fontSize: 9.5),
                  ),
                  SizedBox(
                    width: 84,
                    child: Divider(height: 4, color: t.muted),
                  ),
                  Text(
                    'antes ${_currency(service.price)}',
                    style: TextStyle(color: t.muted, fontSize: 6.5),
                  ),
                ],
              ),
            ),
            Expanded(
              flex: 90,
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 4,
                    ),
                    decoration: BoxDecoration(
                      color: t.accentSoft,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      '-15%',
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 9,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    selected ? 'Você selecionou' : 'Selecionar',
                    style: TextStyle(color: t.muted, fontSize: 7),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _mobileServiceCard(AgendaThemeTokens t, int index) {
    final service = _services[index];
    final selected = _selected.contains(index);
    final imagePath = switch (widget.profile.segment) {
      'Oficina' => 'assets/branding/onboarding-team-workshop.png',
      'Barbearia' => 'assets/branding/onboarding-team-barber.png',
      _ => 'assets/branding/onboarding-segment.png',
    };
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: InkWell(
        key: Key('marketing-promotion-service-$index'),
        borderRadius: BorderRadius.circular(12),
        onTap: () => _toggle(index),
        child: AnimatedContainer(
          duration: AgendaMotion.duration(context, AgendaMotion.fast),
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            color: selected ? t.accentSoft : t.panel,
            border: Border.all(color: selected ? t.accent : t.line),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Row(
            children: [
              Checkbox(value: selected, onChanged: (_) => _toggle(index)),
              ClipRRect(
                borderRadius: BorderRadius.circular(7),
                child: Image.asset(
                  imagePath,
                  width: 52,
                  height: 52,
                  fit: BoxFit.contain,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      service.name,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      '${service.category} · ${service.durationMinutes} min',
                      style: TextStyle(color: t.muted, fontSize: 9.5),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${_currency(service.price)}  →  ${_currency(service.price * .85)}',
                      style: TextStyle(color: t.ink, fontSize: 10.5),
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
                decoration: BoxDecoration(
                  color: t.appBackground,
                  borderRadius: BorderRadius.circular(11),
                ),
                child: const Text('-15%', style: TextStyle(fontSize: 9)),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _toggle(int index) => setState(() {
    if (!_selected.add(index)) _selected.remove(index);
  });

  Widget _summaryPanel(AgendaThemeTokens t, bool desktop) => _surface(
    t,
    padding: const EdgeInsets.fromLTRB(18, 18, 18, 16),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Resumo da promoção',
          style: TextStyle(
            color: t.ink,
            fontSize: 17,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 5),
        Text(
          'Confira os detalhes e veja como sua oferta aparecerá para ${widget.profile.customerSingular}.',
          style: TextStyle(color: t.muted, fontSize: 10.5, height: 1.3),
        ),
        const SizedBox(height: 14),
        Row(
          children: [
            Expanded(
              flex: 3,
              child: TextField(
                key: const Key('marketing-promotion-name'),
                controller: _name,
                decoration: const InputDecoration(
                  labelText: 'Nome da promoção',
                ),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: TextField(
                key: const Key('marketing-promotion-limit'),
                controller: _limit,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: 'Limite'),
              ),
            ),
          ],
        ),
        const SizedBox(height: 11),
        Text(
          'Período da promoção',
          style: TextStyle(color: t.muted, fontSize: 9.5),
        ),
        const SizedBox(height: 5),
        Row(
          children: [
            Expanded(child: _dateButton(t, _start, true)),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 7),
              child: Icon(
                Icons.arrow_forward_rounded,
                color: t.muted,
                size: 17,
              ),
            ),
            Expanded(child: _dateButton(t, _end, false)),
          ],
        ),
        const SizedBox(height: 10),
        CheckboxListTile(
          value: _featured,
          onChanged: (value) => setState(() => _featured = value ?? true),
          contentPadding: EdgeInsets.zero,
          controlAffinity: ListTileControlAffinity.leading,
          title: Text(
            'Destacar no topo do catálogo',
            style: TextStyle(color: t.ink, fontSize: 10.5),
          ),
          dense: true,
        ),
        Divider(height: 18, color: t.line),
        Text('Prévia no site', style: TextStyle(color: t.muted, fontSize: 9.5)),
        const SizedBox(height: 5),
        _promotionPreview(t),
        const SizedBox(height: 9),
        Container(
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            color: t.appBackground,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 34,
                height: 34,
                decoration: BoxDecoration(
                  color: t.accentSoft,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(Icons.sell_outlined, color: t.ink, size: 18),
              ),
              const SizedBox(width: 9),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    AgendaAnimatedValue(
                      value: '${_selected.length}',
                      builder: (context, value) => Text(
                        '$value item${value == '1' ? '' : 's'} · desconto de 15%',
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 10,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      'Economia média de ${_currency(_averageSaving)} por item.',
                      style: TextStyle(color: t.muted, fontSize: 8.5),
                    ),
                  ],
                ),
              ),
              Icon(Icons.expand_less_rounded, color: t.ink, size: 18),
            ],
          ),
        ),
        const SizedBox(height: 16),
        SizedBox(
          height: 48,
          child: FilledButton.icon(
            key: const Key('marketing-promotion-publish'),
            onPressed: _selected.isEmpty
                ? null
                : () => widget.onPublish(_promotion()),
            icon: const Icon(Icons.public_rounded, size: 18),
            label: const Text('Publicar promoção'),
          ),
        ),
        const SizedBox(height: 7),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.lock_outline_rounded, color: t.muted, size: 13),
            const SizedBox(width: 5),
            Flexible(
              child: Text(
                'A promoção só ficará visível após a publicação.',
                textAlign: TextAlign.center,
                style: TextStyle(color: t.muted, fontSize: 8.5),
              ),
            ),
          ],
        ),
      ],
    ),
  );

  Widget _dateButton(AgendaThemeTokens t, DateTime date, bool start) => InkWell(
    onTap: () async {
      final picked = await showDatePicker(
        context: context,
        initialDate: date,
        firstDate: DateTime(2025),
        lastDate: DateTime(2035),
      );
      if (picked == null) return;
      setState(() {
        if (start) {
          _start = picked;
          if (_end.isBefore(_start)) {
            _end = _start.add(const Duration(days: 7));
          }
        } else {
          _end = picked.isBefore(_start) ? _start : picked;
        }
      });
    },
    child: Container(
      height: 42,
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: t.muted)),
      ),
      child: Row(
        children: [
          Expanded(
            child: Text(
              DateFormat('dd/MM/yyyy').format(date),
              style: TextStyle(color: t.ink, fontSize: 9.5),
            ),
          ),
          Icon(Icons.calendar_month_outlined, color: t.muted, size: 17),
        ],
      ),
    ),
  );

  Widget _promotionPreview(AgendaThemeTokens t) => Container(
    height: 110,
    clipBehavior: Clip.antiAlias,
    decoration: BoxDecoration(
      color: t.accentSoft,
      border: Border.all(color: t.accent.withValues(alpha: .45)),
      borderRadius: BorderRadius.circular(12),
    ),
    child: Row(
      children: [
        Expanded(
          flex: 3,
          child: Padding(
            padding: const EdgeInsets.all(11),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  _name.text.trim().isEmpty
                      ? widget.profile.marketingReturnTitle
                      : _name.text.trim(),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13.5,
                    height: 1.1,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const Spacer(),
                Text(
                  widget.profile.defaultPromotionOffer,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.ink, fontSize: 8),
                ),
                const SizedBox(height: 4),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 7,
                    vertical: 3,
                  ),
                  decoration: BoxDecoration(
                    color: t.accent,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: const Text(
                    'OFERTA POR TEMPO LIMITADO',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 5.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
        Expanded(
          flex: 2,
          child: ColoredBox(
            color: t.warmSoft,
            child: Center(
              child: Container(
                width: 58,
                height: 58,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: t.accent,
                  shape: BoxShape.circle,
                ),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(widget.profile.icon, color: Colors.white, size: 17),
                    const SizedBox(height: 2),
                    const Text(
                      '15% OFF',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 8,
                        height: 1.12,
                        fontWeight: FontWeight.w900,
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
  );

  double get _averageSaving {
    final rows = _selected.where((index) => index < _services.length);
    if (rows.isEmpty) return 0;
    return rows
            .map((index) => _services[index].price * .15)
            .reduce((a, b) => a + b) /
        rows.length;
  }

  MarketingSitePromotion _promotion() {
    final limit = int.tryParse(_limit.text.trim()) ?? 1;
    return MarketingSitePromotion(
      name: _name.text.trim().isEmpty
          ? widget.profile.marketingReturnTitle
          : _name.text.trim(),
      startDate: _start,
      endDate: _end,
      limitPerCustomer: limit.clamp(1, 99).toInt(),
      highlightInCatalog: _featured,
      isPublished: true,
      publishedAt: DateTime.now(),
      items: [
        for (final index in _selected.where((row) => row < _services.length))
          MarketingSitePromotionItem(
            serviceId: _services[index].id,
            serviceName: _services[index].name,
            originalPrice: _services[index].price,
            promotionalPrice: _services[index].price * .85,
          ),
      ],
    );
  }

  Widget _surface(
    AgendaThemeTokens t, {
    required Widget child,
    required EdgeInsetsGeometry padding,
  }) => Container(
    padding: padding,
    decoration: BoxDecoration(
      color: t.panel,
      border: Border.all(color: t.line),
      borderRadius: BorderRadius.circular(16),
      boxShadow: const [
        BoxShadow(
          color: Color(0x0C000000),
          blurRadius: 18,
          offset: Offset(0, 6),
        ),
      ],
    ),
    child: child,
  );

  String _currency(double value) =>
      NumberFormat.currency(locale: 'pt_BR', symbol: 'R\$').format(value);
}

class _PromotionService {
  const _PromotionService({
    required this.id,
    required this.name,
    required this.category,
    required this.durationMinutes,
    required this.price,
  });

  final String id;
  final String name;
  final String category;
  final int durationMinutes;
  final double price;
}
