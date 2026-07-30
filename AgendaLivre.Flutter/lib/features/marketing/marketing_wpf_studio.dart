import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';

import '../../app/theme/agenda_theme.dart';
import '../../core/business_profile.dart';
import '../../core/motion.dart';

class MarketingWpfStudio extends StatefulWidget {
  const MarketingWpfStudio({
    super.key,
    required this.businessName,
    required this.profile,
    required this.titleController,
    required this.copyController,
    required this.previewMessage,
    required this.publicationCount,
    required this.clientCount,
    required this.contactQueueCount,
    required this.suggestedScheduleWindows,
    required this.contactPhone,
    required this.instagramLinked,
    required this.whatsAppLinked,
    required this.contactQueue,
    required this.onUpdate,
    required this.onCopy,
    required this.onWhatsApp,
    required this.onInstagram,
    required this.onBack,
    this.initialChannel = 2,
  });

  final String businessName;
  final AgendaBusinessProfile profile;
  final TextEditingController titleController;
  final TextEditingController copyController;
  final String previewMessage;
  final int publicationCount;
  final int clientCount;
  final int contactQueueCount;
  final List<String> suggestedScheduleWindows;
  final String contactPhone;
  final bool instagramLinked;
  final bool whatsAppLinked;
  final Widget contactQueue;
  final VoidCallback onUpdate;
  final VoidCallback onCopy;
  final VoidCallback onWhatsApp;
  final VoidCallback onInstagram;
  final VoidCallback onBack;
  final int initialChannel;

  @override
  State<MarketingWpfStudio> createState() => _MarketingWpfStudioState();
}

class _MarketingWpfStudioState extends State<MarketingWpfStudio> {
  static const _images = <_MarketingImage>[
    _MarketingImage(
      'assets/branding/marketing-story-background.png',
      'Modelo Agenda Livre',
      'Arte inclusa',
    ),
    _MarketingImage(
      'assets/branding/marketing-campaign-hair.png',
      'Agenda Livre',
      'Imagem própria',
    ),
    _MarketingImage(
      'assets/branding/marketing-campaign-nails.png',
      'Agenda Livre',
      'Imagem própria',
    ),
    _MarketingImage(
      'assets/branding/marketing-campaign-spa.png',
      'Agenda Livre',
      'Imagem própria',
    ),
    _MarketingImage(
      'assets/branding/marketing-site-hero-hair.png',
      'Agenda Livre',
      'Imagem própria',
    ),
  ];
  static const _topics = <String>[
    'Cabelo',
    'Unhas',
    'Estética',
    'Spa',
    'Maquiagem',
  ];

  late int _channel;
  int _selectedImage = 0;
  String _topic = 'Maquiagem';
  late Set<String> _selectedTimes;
  final _layerVisibility = List<bool>.filled(7, true);
  final _search = TextEditingController(text: 'beleza e autocuidado');
  late final TextEditingController _mobileSearch;
  double _artFontSize = 20;
  int _artAlignment = 1;

  @override
  void initState() {
    super.initState();
    _channel = widget.initialChannel.clamp(0, 2);
    _selectedTimes = widget.suggestedScheduleWindows.toSet();
    _mobileSearch = TextEditingController(
      text: '${widget.profile.segment} ${widget.profile.servicePlural}'
          .toLowerCase(),
    );
  }

  @override
  void didUpdateWidget(covariant MarketingWpfStudio oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.suggestedScheduleWindows != widget.suggestedScheduleWindows) {
      final valid = widget.suggestedScheduleWindows.toSet();
      _selectedTimes = _selectedTimes.intersection(valid);
      if (_selectedTimes.isEmpty) _selectedTimes = valid;
    }
  }

  @override
  void dispose() {
    _search.dispose();
    _mobileSearch.dispose();
    super.dispose();
  }

  String get _mobileSegmentImagePath => switch (widget.profile.segment) {
    'Oficina' => 'assets/branding/onboarding-team-workshop.png',
    'Barbearia' => 'assets/branding/onboarding-team-barber.png',
    _ => 'assets/branding/onboarding-segment.png',
  };

  List<_MarketingImage> _imagesFor(bool compact) => compact
      ? <_MarketingImage>[
          _MarketingImage(
            _mobileSegmentImagePath,
            'Agenda Livre',
            widget.profile.segment,
          ),
        ]
      : _images;

  List<String> _topicsFor(bool compact) {
    if (!compact) return _topics;
    return switch (widget.profile.segment) {
      'Oficina' => const ['Revisão', 'Manutenção', 'Veículo', 'Retorno'],
      'Petshop' => const ['Banho', 'Tosa', 'Vacina', 'Retorno'],
      'Clínica médica' => const ['Retorno', 'Avaliação', 'Cuidados', 'Agenda'],
      'Barbearia' => const ['Corte', 'Barba', 'Retorno', 'Horários'],
      'Serviços' => const ['Serviços', 'Agenda', 'Retorno', 'Novidades'],
      _ => const ['Cuidados', 'Agenda', 'Retorno', 'Novidades'],
    };
  }

  @override
  Widget build(BuildContext context) {
    final compact = MediaQuery.sizeOf(context).width < 760;
    final t = AgendaThemeTokens.of(context);
    return ColoredBox(
      color: compact ? t.appBackground : const Color(0xFFFAF9F7),
      child: SingleChildScrollView(
        padding: EdgeInsets.fromLTRB(
          compact ? 12 : 18,
          compact ? 12 : 14,
          compact ? 12 : 18,
          compact ? 28 : 72,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            AgendaReveal(child: _breadcrumb(t)),
            const SizedBox(height: 8),
            AgendaReveal(
              delay: const Duration(milliseconds: 45),
              child: _stageStrip(t, compact),
            ),
            const SizedBox(height: 10),
            AgendaReveal(
              delay: const Duration(milliseconds: 80),
              child: _studio(t, compact),
            ),
            const SizedBox(height: 10),
            AgendaReveal(
              delay: const Duration(milliseconds: 115),
              child: _collection(t, compact),
            ),
            const SizedBox(height: 10),
            AgendaReveal(
              delay: const Duration(milliseconds: 145),
              child: widget.contactQueue,
            ),
          ],
        ),
      ),
    );
  }

  Widget _breadcrumb(AgendaThemeTokens t) => Align(
    alignment: Alignment.centerLeft,
    child: FittedBox(
      fit: BoxFit.scaleDown,
      child: Row(
        children: [
          IconButton(
            key: const Key('marketing-studio-back'),
            onPressed: widget.onBack,
            tooltip: 'Voltar para Marketing',
            visualDensity: VisualDensity.compact,
            constraints: const BoxConstraints.tightFor(width: 30, height: 30),
            padding: EdgeInsets.zero,
            icon: Icon(Icons.arrow_back, color: t.ink, size: 18),
          ),
          const SizedBox(width: 5),
          Text(
            'ESTÚDIO DE CONTEÚDO',
            style: TextStyle(
              color: t.muted,
              fontSize: 10.5,
              fontWeight: FontWeight.w600,
            ),
          ),
          Text(
            ' / MARKETING',
            style: TextStyle(
              color: t.ink,
              fontSize: 10.5,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(width: 10),
          Container(width: 28, height: 1, color: t.accent),
        ],
      ),
    ),
  );

  Widget _metricStrip(AgendaThemeTokens t, bool compact) {
    final metrics = <Widget>[
      _Metric(
        icon: Icons.public_rounded,
        label: 'Catálogo:',
        value: widget.publicationCount > 0 ? 'Publicado' : 'Não publicado',
        valueColor: widget.publicationCount > 0
            ? const Color(0xFF15803D)
            : t.muted,
      ),
      _Metric(
        icon: Icons.chat_outlined,
        label: 'Fila de contatos:',
        value: '${widget.contactQueueCount}',
      ),
      _Metric(
        icon: Icons.visibility_outlined,
        label: '${_capitalize(widget.profile.customerPlural)}:',
        value: '${widget.clientCount}',
      ),
      _Metric(
        icon: Icons.event_available_outlined,
        label: 'Janelas livres:',
        value: '${widget.suggestedScheduleWindows.length}',
      ),
    ];
    return Container(
      height: 42,
      decoration: _surfaceDecoration(t, radius: 16),
      child: compact
          ? ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 12),
              itemCount: metrics.length,
              separatorBuilder: (_, _) => const SizedBox(width: 26),
              itemBuilder: (_, index) => metrics[index],
            )
          : Row(
              children: [for (final metric in metrics) Expanded(child: metric)],
            ),
    );
  }

  Widget _stageStrip(AgendaThemeTokens t, bool compact) {
    final steps = <Widget>[
      _stage(
        t,
        number: '1',
        title: '1. Criar',
        subtitle: 'Configure a campanha e os elementos.',
      ),
      _stage(
        t,
        number: '2',
        title: '2. Editar arte',
        subtitle: 'Edite cada elemento da sua arte.',
        active: true,
      ),
      _stage(
        t,
        number: '3',
        title: '3. Publicar',
        subtitle: 'Revise e publique no canal escolhido.',
      ),
    ];
    return Container(
      height: 56,
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(12),
      ),
      child: compact
          ? SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: SizedBox(
                width: 700,
                child: Row(
                  children: [for (final step in steps) Expanded(child: step)],
                ),
              ),
            )
          : Row(children: [for (final step in steps) Expanded(child: step)]),
    );
  }

  Widget _stage(
    AgendaThemeTokens t, {
    required String number,
    required String title,
    required String subtitle,
    bool active = false,
  }) => Padding(
    padding: const EdgeInsets.symmetric(horizontal: 18),
    child: Row(
      children: [
        Container(
          width: 25,
          height: 25,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: active ? t.accent : t.ink.withValues(alpha: .82),
            shape: BoxShape.circle,
          ),
          child: Text(
            number,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 10,
              fontWeight: FontWeight.w800,
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
                title,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 12.5,
                  fontWeight: FontWeight.w800,
                ),
              ),
              Text(
                subtitle,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: t.muted, fontSize: 8.5),
              ),
            ],
          ),
        ),
      ],
    ),
  );

  Widget _studio(AgendaThemeTokens t, bool compact) {
    final editor = _editor(t, compact);
    final canvas = _canvas(t, compact);
    final publication = _publication(t, compact);
    return Container(
      key: const Key('marketing-active-campaign'),
      decoration: _surfaceDecoration(t, radius: 15),
      clipBehavior: Clip.antiAlias,
      child: compact
          ? Column(
              children: [
                editor,
                Divider(height: 1, color: t.line),
                canvas,
                Divider(height: 1, color: t.line),
                publication,
              ],
            )
          : SizedBox(
              height: 446,
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(flex: 30, child: editor),
                  VerticalDivider(width: 1, color: t.line),
                  Expanded(flex: 51, child: canvas),
                  VerticalDivider(width: 1, color: t.line),
                  Expanded(flex: 25, child: publication),
                ],
              ),
            ),
    );
  }

  Widget _editor(AgendaThemeTokens t, bool compact) => SingleChildScrollView(
    primary: false,
    padding: EdgeInsets.fromLTRB(14, compact ? 16 : 12, 14, compact ? 10 : 8),
    child: Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Container(
              width: 30,
              height: 30,
              decoration: BoxDecoration(
                color: t.accentSoft,
                borderRadius: BorderRadius.circular(15),
              ),
              child: Icon(Icons.edit_outlined, size: 16, color: t.ink),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Criar publicação',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 16,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  Text(
                    'Monte a mensagem para ${widget.profile.customerPlural}',
                    style: TextStyle(color: t.muted, fontSize: 10.5),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 5),
        Text(
          'Canal de publicação',
          style: TextStyle(color: t.muted, fontSize: 10.5),
        ),
        const SizedBox(height: 4),
        _channelTabs(t),
        const SizedBox(height: 6),
        _label(t, 'Título da campanha'),
        const SizedBox(height: 4),
        _input(
          t,
          key: const Key('marketing-promotion-name'),
          controller: widget.titleController,
          height: 36,
        ),
        const SizedBox(height: 6),
        _label(t, 'Texto da publicação'),
        const SizedBox(height: 4),
        _input(
          t,
          key: const Key('marketing-promotion-message'),
          controller: widget.copyController,
          height: 82,
          maxLines: 4,
        ),
        const SizedBox(height: 6),
        Row(
          children: [
            Expanded(child: _label(t, 'Janelas sugeridas pela agenda')),
            const SizedBox(width: 6),
            Text(
              compact
                  ? '${_selectedTimes.length} sel.'
                  : '${_selectedTimes.length} selecionados',
              maxLines: 1,
              style: TextStyle(color: t.ink, fontSize: 9.5),
            ),
          ],
        ),
        const SizedBox(height: 5),
        if (widget.suggestedScheduleWindows.isEmpty)
          Container(
            height: 32,
            alignment: Alignment.centerLeft,
            child: Text(
              'Nenhuma janela livre detectada nos próximos 14 dias.',
              style: TextStyle(color: t.muted, fontSize: 9.5),
            ),
          )
        else
          SizedBox(
            height: 30,
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  for (final time in widget.suggestedScheduleWindows) ...[
                    _ChoicePill(
                      label: time,
                      selected: _selectedTimes.contains(time),
                      icon: _selectedTimes.contains(time)
                          ? Icons.check_rounded
                          : null,
                      horizontalPadding: 7,
                      onTap: () => setState(() {
                        if (!_selectedTimes.remove(time)) {
                          _selectedTimes.add(time);
                        }
                      }),
                    ),
                    if (time != widget.suggestedScheduleWindows.last)
                      const SizedBox(width: 5),
                  ],
                ],
              ),
            ),
          ),
        const SizedBox(height: 9),
        Text(
          'Elementos da arte',
          style: TextStyle(
            color: t.ink,
            fontSize: 10.5,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 4),
        for (var index = 0; index < _layerVisibility.length; index++)
          _layerRow(t, index),
      ],
    ),
  );

  Widget _layerRow(AgendaThemeTokens t, int index) {
    const labels = [
      'Nome da empresa',
      'Título',
      'Descrição',
      'Horários',
      'Botão',
      'Telefone',
      'Foto de fundo',
    ];
    const icons = [
      Icons.storefront_outlined,
      Icons.title_rounded,
      Icons.notes_rounded,
      Icons.format_list_bulleted_rounded,
      Icons.smart_button_outlined,
      Icons.phone_outlined,
      Icons.image_outlined,
    ];
    final visible = _layerVisibility[index];
    return InkWell(
      onTap: () =>
          setState(() => _layerVisibility[index] = !_layerVisibility[index]),
      child: AnimatedContainer(
        duration: AgendaMotion.duration(context, AgendaMotion.fast),
        height: 31,
        decoration: BoxDecoration(
          color: visible ? Colors.transparent : t.line.withValues(alpha: .18),
          border: Border(bottom: BorderSide(color: t.line)),
        ),
        child: Row(
          children: [
            SizedBox(
              width: 28,
              child: Icon(
                icons[index],
                size: 14,
                color: visible ? t.accentDark : t.muted,
              ),
            ),
            Expanded(
              child: Text(
                labels[index],
                style: TextStyle(color: t.ink, fontSize: 10.5),
              ),
            ),
            Icon(
              _layerVisibility[index]
                  ? Icons.visibility_outlined
                  : Icons.visibility_off_outlined,
              color: visible ? t.ink : t.muted,
              size: 16,
            ),
            const SizedBox(width: 7),
          ],
        ),
      ),
    );
  }

  Widget _channelTabs(AgendaThemeTokens t) => Container(
    height: 36,
    decoration: BoxDecoration(
      color: t.panel,
      borderRadius: BorderRadius.circular(18),
      border: Border.all(color: t.line),
    ),
    child: Row(
      children: [
        for (final entry in const <(String, int)>[
          ('Story', 0),
          ('Post', 1),
          ('WhatsApp', 2),
        ])
          Expanded(
            child: InkWell(
              key: Key('marketing-channel-${entry.$1.toLowerCase()}'),
              borderRadius: BorderRadius.circular(18),
              onTap: () => setState(() => _channel = entry.$2),
              child: AnimatedContainer(
                duration: AgendaMotion.duration(context, AgendaMotion.fast),
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: _channel == entry.$2
                      ? t.accentSoft
                      : Colors.transparent,
                  borderRadius: BorderRadius.circular(18),
                  border: _channel == entry.$2
                      ? Border.all(color: t.accent)
                      : null,
                ),
                child: Text(
                  entry.$1,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 12,
                    fontWeight: _channel == entry.$2
                        ? FontWeight.w700
                        : FontWeight.w500,
                  ),
                ),
              ),
            ),
          ),
      ],
    ),
  );

  Widget _canvas(AgendaThemeTokens t, bool compact) {
    final images = _imagesFor(compact);
    final image = images[_selectedImage.clamp(0, images.length - 1)];
    final preview = Column(
      children: [
        SizedBox(
          height: 44,
          child: Row(
            children: [
              Icon(Icons.preview_outlined, size: 18, color: t.ink),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Prévia da arte',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              OutlinedButton(
                key: const Key('marketing-update-promotion'),
                onPressed: widget.onUpdate,
                style: OutlinedButton.styleFrom(
                  minimumSize: const Size(72, 30),
                  padding: const EdgeInsets.symmetric(horizontal: 9),
                  visualDensity: VisualDensity.compact,
                ),
                child: const Text('Ajustar'),
              ),
            ],
          ),
        ),
        Expanded(
          child: Center(
            child: SizedBox(
              key: const Key('marketing-message-preview'),
              width: compact ? 220 : 198,
              height: compact ? 378 : 342,
              child: AnimatedSwitcher(
                duration: AgendaMotion.duration(context, AgendaMotion.standard),
                child: _StoryPreview(
                  key: ValueKey(
                    '${image.path}|${widget.titleController.text}|${widget.previewMessage}|${_selectedTimes.join(',')}|${_layerVisibility.join(',')}|$_artFontSize|$_artAlignment',
                  ),
                  image: image,
                  businessName: widget.businessName,
                  title: widget.titleController.text,
                  profile: widget.profile,
                  contactPhone: widget.contactPhone,
                  message: widget.previewMessage,
                  times: _selectedTimes.toList(growable: false),
                  layerVisibility: List<bool>.of(_layerVisibility),
                  titleFontSize: _artFontSize,
                  titleAlignment: _artAlignment,
                ),
              ),
            ),
          ),
        ),
      ],
    );
    final inspector = SingleChildScrollView(
      primary: false,
      child: _artInspector(t),
    );
    return Padding(
      padding: EdgeInsets.fromLTRB(12, compact ? 12 : 4, 12, 8),
      child: compact
          ? Column(
              children: [
                SizedBox(height: 430, child: preview),
                Divider(height: 1, color: t.line),
                inspector,
              ],
            )
          : Row(
              children: [
                Expanded(flex: 11, child: preview),
                VerticalDivider(width: 1, color: t.line),
                Expanded(flex: 10, child: inspector),
              ],
            ),
    );
  }

  Widget _artInspector(AgendaThemeTokens t) => Padding(
    padding: const EdgeInsets.fromLTRB(10, 5, 0, 0),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          'Ajustes do título',
          style: TextStyle(
            color: t.ink,
            fontSize: 13,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 6),
        _label(t, 'Texto deste elemento'),
        const SizedBox(height: 4),
        Container(
          height: 44,
          padding: const EdgeInsets.symmetric(horizontal: 12),
          alignment: Alignment.centerLeft,
          decoration: BoxDecoration(
            color: t.panel,
            border: Border.all(color: t.line),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Text(
            'HORÁRIOS LIVRES',
            style: TextStyle(color: t.ink, fontSize: 12.5),
          ),
        ),
        const SizedBox(height: 8),
        Text(
          'Fonte do modelo: Georgia',
          style: TextStyle(color: t.muted, fontSize: 10),
        ),
        const SizedBox(height: 7),
        Row(
          children: [
            Expanded(child: _label(t, 'Tamanho')),
            Text(
              '${_artFontSize.round()} px',
              style: TextStyle(color: t.ink, fontSize: 9),
            ),
          ],
        ),
        Slider(
          value: _artFontSize,
          min: 12,
          max: 32,
          onChanged: (value) => setState(() => _artFontSize = value),
        ),
        _label(t, 'Alinhamento'),
        const SizedBox(height: 4),
        Row(
          children: [
            for (var index = 0; index < 3; index++)
              Expanded(
                child: InkWell(
                  onTap: () => setState(() => _artAlignment = index),
                  child: Container(
                    height: 30,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: _artAlignment == index
                          ? t.accentSoft
                          : Colors.transparent,
                      border: Border.all(
                        color: _artAlignment == index ? t.accent : t.line,
                      ),
                      borderRadius: BorderRadius.circular(9),
                    ),
                    child: Icon(
                      const [
                        Icons.format_align_left,
                        Icons.format_align_center,
                        Icons.format_align_right,
                      ][index],
                      size: 16,
                    ),
                  ),
                ),
              ),
          ],
        ),
      ],
    ),
  );

  Widget _publication(AgendaThemeTokens t, bool compact) {
    final images = _imagesFor(compact);
    final image = images[_selectedImage.clamp(0, images.length - 1)];
    return Padding(
      padding: EdgeInsets.fromLTRB(15, compact ? 16 : 12, 15, 12),
      child: Column(
        mainAxisSize: compact ? MainAxisSize.min : MainAxisSize.max,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(Icons.event_available_outlined, size: 19, color: t.ink),
              const SizedBox(width: 7),
              Expanded(
                child: Text(
                  'Publicação',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 16,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
              IconButton(
                key: const Key('marketing-open-instagram'),
                onPressed: widget.onInstagram,
                tooltip: 'Abrir Instagram',
                visualDensity: VisualDensity.compact,
                constraints: const BoxConstraints.tightFor(
                  width: 30,
                  height: 30,
                ),
                padding: EdgeInsets.zero,
                icon: const FaIcon(FontAwesomeIcons.instagram, size: 15),
              ),
            ],
          ),
          const SizedBox(height: 10),
          _label(t, 'Canal'),
          const SizedBox(height: 5),
          Row(
            children: [
              FaIcon(
                _channel == 2
                    ? FontAwesomeIcons.whatsapp
                    : FontAwesomeIcons.instagram,
                size: 18,
                color: _channel == 2 ? const Color(0xFF16A34A) : t.ink,
              ),
              const SizedBox(width: 7),
              Text(
                _channel == 0
                    ? 'Story'
                    : _channel == 1
                    ? 'Instagram'
                    : 'WhatsApp',
                style: TextStyle(color: t.ink, fontWeight: FontWeight.w700),
              ),
            ],
          ),
          const SizedBox(height: 10),
          _label(t, 'Modelo visual'),
          const SizedBox(height: 6),
          Align(
            child: ClipRRect(
              borderRadius: BorderRadius.circular(7),
              child: Image.asset(
                image.path,
                width: 72,
                height: 96,
                fit: BoxFit.cover,
              ),
            ),
          ),
          const SizedBox(height: 5),
          Text(
            '${image.author} · ${image.license}',
            textAlign: TextAlign.center,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(color: t.muted, fontSize: 8.5),
          ),
          if (!compact) const Spacer() else const SizedBox(height: 4),
          SizedBox(
            height: 36,
            child: OutlinedButton(
              key: const Key('marketing-copy-message'),
              onPressed: widget.onCopy,
              child: Text(compact ? 'Copiar mensagem' : 'Exportar PNG'),
            ),
          ),
          const SizedBox(height: 8),
          SizedBox(
            height: 40,
            child: FilledButton.icon(
              key: const Key('marketing-open-whatsapp'),
              onPressed: _channel == 2 ? widget.onWhatsApp : widget.onInstagram,
              icon: FaIcon(
                _channel == 2
                    ? FontAwesomeIcons.whatsapp
                    : FontAwesomeIcons.instagram,
                size: 15,
              ),
              label: Text(
                _channel == 2
                    ? 'Publicar no WhatsApp'
                    : widget.instagramLinked
                    ? 'Abrir Instagram conectado'
                    : 'Abrir Instagram',
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _collection(AgendaThemeTokens t, bool compact) {
    final topicOptions = _topicsFor(compact);
    final images = _imagesFor(compact);
    final selectedTopic = topicOptions.contains(_topic)
        ? _topic
        : topicOptions.first;
    final searchController = compact ? _mobileSearch : _search;
    final topics = SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          Text('Temas', style: TextStyle(color: t.muted, fontSize: 9)),
          const SizedBox(width: 8),
          for (final topic in topicOptions) ...[
            _ChoicePill(
              label: topic,
              selected: topic == selectedTopic,
              onTap: () => setState(() {
                _topic = topic;
                searchController.text = topic.toLowerCase();
              }),
            ),
            const SizedBox(width: 5),
          ],
        ],
      ),
    );
    final search = Row(
      children: [
        Expanded(
          child: SizedBox(
            height: 34,
            child: TextField(
              controller: searchController,
              decoration: InputDecoration(
                isDense: true,
                hintText: 'Pesquisar imagens',
                contentPadding: const EdgeInsets.symmetric(horizontal: 14),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(18),
                ),
              ),
            ),
          ),
        ),
        const SizedBox(width: 8),
        SizedBox(
          height: 34,
          child: FilledButton.icon(
            onPressed: () => setState(() {}),
            icon: const Icon(Icons.search, size: 15),
            label: const Text('Buscar'),
          ),
        ),
      ],
    );
    final heading = Row(
      children: [
        Icon(Icons.collections_outlined, color: t.ink, size: 19),
        const SizedBox(width: 7),
        if (compact)
          Expanded(
            child: Text(
              'Modelos para ${widget.profile.segment}',
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: t.ink,
                fontSize: 15.5,
                fontWeight: FontWeight.w800,
              ),
            ),
          )
        else
          Text(
            'Coleção editorial',
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              color: t.ink,
              fontSize: 15.5,
              fontWeight: FontWeight.w800,
            ),
          ),
      ],
    );
    final gallery = SizedBox(
      height: 68,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        itemCount: images.length,
        separatorBuilder: (_, _) => const SizedBox(width: 8),
        itemBuilder: (context, index) {
          final image = images[index];
          return InkWell(
            key: Key('marketing-editorial-image-$index'),
            onTap: () => setState(() => _selectedImage = index),
            borderRadius: BorderRadius.circular(10),
            child: AnimatedContainer(
              duration: AgendaMotion.duration(context, AgendaMotion.fast),
              width: compact ? 126 : 145,
              padding: const EdgeInsets.all(4),
              decoration: BoxDecoration(
                color: t.panel,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                  color: _selectedImage == index ? t.accent : t.line,
                  width: _selectedImage == index ? 2 : 1,
                ),
              ),
              child: Row(
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(7),
                    child: Image.asset(
                      image.path,
                      width: 58,
                      height: 58,
                      fit: BoxFit.cover,
                    ),
                  ),
                  const SizedBox(width: 7),
                  Expanded(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          topicOptions[index % topicOptions.length],
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: t.ink,
                            fontSize: 9,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 3),
                        Text(
                          image.author,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(color: t.muted, fontSize: 7),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
    return Container(
      decoration: _surfaceDecoration(t, radius: 15),
      padding: const EdgeInsets.fromLTRB(14, 10, 14, 11),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (compact) ...[
            heading,
            const SizedBox(height: 9),
            topics,
            const SizedBox(height: 9),
            search,
          ] else
            Row(
              children: [
                heading,
                const SizedBox(width: 18),
                Expanded(flex: 3, child: topics),
                Text(
                  '${images.length} fotos da coleção editorial',
                  style: TextStyle(color: t.muted, fontSize: 8.5),
                ),
                const SizedBox(width: 16),
                Expanded(flex: 2, child: search),
              ],
            ),
          const SizedBox(height: 9),
          gallery,
        ],
      ),
    );
  }

  Widget _label(AgendaThemeTokens t, String text) =>
      Text(text, style: TextStyle(color: t.muted, fontSize: 10.5));

  Widget _input(
    AgendaThemeTokens t, {
    required Key key,
    required TextEditingController controller,
    required double height,
    int maxLines = 1,
  }) => SizedBox(
    height: height,
    child: TextField(
      key: key,
      controller: controller,
      onTap: () => controller.selection = TextSelection(
        baseOffset: 0,
        extentOffset: controller.text.length,
      ),
      maxLines: maxLines,
      style: TextStyle(color: t.ink, fontSize: 12.5, height: 1.25),
      decoration: InputDecoration(
        isDense: true,
        filled: true,
        fillColor: t.panel,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 14,
          vertical: 10,
        ),
      ),
    ),
  );
}

class _StoryPreview extends StatelessWidget {
  const _StoryPreview({
    super.key,
    required this.image,
    required this.businessName,
    required this.title,
    required this.profile,
    required this.contactPhone,
    required this.message,
    required this.times,
    required this.layerVisibility,
    required this.titleFontSize,
    required this.titleAlignment,
  });

  final _MarketingImage image;
  final String businessName;
  final String title;
  final AgendaBusinessProfile profile;
  final String contactPhone;
  final String message;
  final List<String> times;
  final List<bool> layerVisibility;
  final double titleFontSize;
  final int titleAlignment;

  @override
  Widget build(BuildContext context) {
    final accent = AgendaThemeTokens.of(context).accent;
    final visibleTimes = times.take(5).toList(growable: false);
    return ClipRRect(
      borderRadius: BorderRadius.circular(14),
      child: Stack(
        fit: StackFit.expand,
        children: [
          if (layerVisibility[6])
            Image.asset(image.path, fit: BoxFit.cover)
          else
            const ColoredBox(color: Color(0xFFF7E7DF)),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [
                  Colors.white.withValues(alpha: .08),
                  Colors.black.withValues(alpha: .20),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 10),
            child: Column(
              children: [
                if (layerVisibility[0]) ...[
                  Align(
                    alignment: Alignment.centerLeft,
                    child: Text(
                      businessName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: Color(0xFF795548),
                        fontSize: 10,
                      ),
                    ),
                  ),
                  Align(
                    alignment: Alignment.centerLeft,
                    child: Container(
                      width: 27,
                      height: 2,
                      margin: const EdgeInsets.only(top: 7),
                      color: accent,
                    ),
                  ),
                ],
                const Spacer(),
                if (layerVisibility[1])
                  Text(
                    title.trim().isEmpty
                        ? profile.activityPlural.toUpperCase()
                        : title.toUpperCase(),
                    textAlign: const [
                      TextAlign.left,
                      TextAlign.center,
                      TextAlign.right,
                    ][titleAlignment],
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: const Color(0xFFA63712),
                      fontSize: titleFontSize,
                      height: .95,
                      fontFamily: 'Georgia',
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                if (layerVisibility[2]) ...[
                  const SizedBox(height: 5),
                  Text(
                    message,
                    textAlign: TextAlign.center,
                    maxLines: 4,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Color(0xFF765348),
                      fontSize: 9,
                      height: 1.2,
                    ),
                  ),
                ],
                if (layerVisibility[3]) ...[
                  const SizedBox(height: 6),
                  AnimatedSwitcher(
                    duration: AgendaMotion.duration(
                      context,
                      AgendaMotion.standard,
                    ),
                    child: Container(
                      key: ValueKey(visibleTimes.join('|')),
                      width: double.infinity,
                      constraints: const BoxConstraints(minHeight: 62),
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 10,
                      ),
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: .92),
                        borderRadius: BorderRadius.circular(13),
                      ),
                      child: visibleTimes.isEmpty
                          ? const Center(
                              child: Text(
                                'CONSULTE A AGENDA',
                                style: TextStyle(
                                  color: Color(0xFFA63712),
                                  fontSize: 9,
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                            )
                          : Wrap(
                              alignment: WrapAlignment.center,
                              spacing: 8,
                              runSpacing: 4,
                              children: [
                                for (final time in visibleTimes)
                                  Text(
                                    time,
                                    style: const TextStyle(
                                      color: Color(0xFFA63712),
                                      fontSize: 10,
                                      fontWeight: FontWeight.w800,
                                    ),
                                  ),
                              ],
                            ),
                    ),
                  ),
                ],
                if (layerVisibility[4]) ...[
                  const SizedBox(height: 10),
                  Container(
                    height: 22,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: accent,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      profile.newActivityLabel.toUpperCase(),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 7.5,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                ],
                if (layerVisibility[5]) ...[
                  const SizedBox(height: 7),
                  Text(
                    contactPhone.trim().isEmpty
                        ? 'minhaagendalivre.com.br'
                        : contactPhone.trim(),
                    style: TextStyle(
                      color: Colors.white.withValues(alpha: .9),
                      fontSize: 7.5,
                    ),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Metric extends StatelessWidget {
  const _Metric({
    required this.icon,
    required this.label,
    required this.value,
    this.valueColor,
  });

  final IconData icon;
  final String label;
  final String value;
  final Color? valueColor;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 15, color: t.ink),
          const SizedBox(width: 7),
          Flexible(
            child: AgendaAnimatedValue(
              value: '$label|$value',
              builder: (context, _) => Text.rich(
                TextSpan(
                  style: TextStyle(color: t.muted, fontSize: 10.5),
                  children: [
                    TextSpan(text: '$label '),
                    TextSpan(
                      text: value,
                      style: TextStyle(
                        color: valueColor ?? t.ink,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ChoicePill extends StatelessWidget {
  const _ChoicePill({
    required this.label,
    required this.selected,
    required this.onTap,
    this.icon,
    this.horizontalPadding = 11,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;
  final IconData? icon;
  final double horizontalPadding;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(18),
      child: AnimatedContainer(
        duration: AgendaMotion.duration(context, AgendaMotion.fast),
        height: 30,
        padding: EdgeInsets.symmetric(horizontal: horizontalPadding),
        decoration: BoxDecoration(
          color: selected ? t.accentSoft : t.panel,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: selected ? t.accent : t.line),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (icon != null) ...[
              Icon(icon, size: 12, color: t.ink),
              const SizedBox(width: 3),
            ],
            Text(
              label,
              style: TextStyle(
                color: t.ink,
                fontSize: 10.5,
                fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MarketingImage {
  const _MarketingImage(this.path, this.author, this.license);

  final String path;
  final String author;
  final String license;
}

BoxDecoration _surfaceDecoration(
  AgendaThemeTokens t, {
  required double radius,
}) => BoxDecoration(
  color: t.panel,
  borderRadius: BorderRadius.circular(radius),
  border: Border.all(color: t.line),
  boxShadow: const [
    BoxShadow(color: Color(0x0A000000), blurRadius: 6, offset: Offset(0, 2)),
  ],
);

String _capitalize(String value) {
  final text = value.trim();
  if (text.isEmpty) return text;
  return '${text[0].toUpperCase()}${text.substring(1)}';
}
