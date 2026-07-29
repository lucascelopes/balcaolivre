import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';

import '../../app/theme/agenda_theme.dart';

class MarketingWpfStudio extends StatefulWidget {
  const MarketingWpfStudio({
    super.key,
    required this.businessName,
    required this.titleController,
    required this.copyController,
    required this.previewMessage,
    required this.publicationCount,
    required this.clientCount,
    required this.contactQueue,
    required this.onUpdate,
    required this.onCopy,
    required this.onWhatsApp,
    required this.onInstagram,
    required this.onBack,
    this.initialChannel = 2,
  });

  final String businessName;
  final TextEditingController titleController;
  final TextEditingController copyController;
  final String previewMessage;
  final int publicationCount;
  final int clientCount;
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
      'Agenda Livre',
      'Imagem própria',
    ),
    _MarketingImage(
      'assets/branding/marketing-campaign-hair.png',
      'Valeria Boltneva',
      'CC0',
    ),
    _MarketingImage(
      'assets/branding/marketing-campaign-nails.png',
      'Alexander Krivitskiy',
      'CC0',
    ),
    _MarketingImage(
      'assets/branding/marketing-campaign-spa.png',
      'Healthy Living',
      'CC0',
    ),
    _MarketingImage(
      'assets/branding/marketing-site-hero-hair.png',
      'Authentic Stock',
      'CC0',
    ),
  ];

  static const _times = <String>['08:00', '08:30', '09:00', '09:30', '10:00'];
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
  final Set<String> _selectedTimes = {..._times};
  final _search = TextEditingController(text: 'maquiagem');

  @override
  void initState() {
    super.initState();
    _channel = widget.initialChannel.clamp(0, 2);
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final compact = MediaQuery.sizeOf(context).width < 760;
    final t = AgendaThemeTokens.of(context);
    return ColoredBox(
      color: const Color(0xFFFAF9F7),
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
            _breadcrumb(t),
            const SizedBox(height: 8),
            _metricStrip(t, compact),
            const SizedBox(height: 10),
            _studio(t, compact),
            const SizedBox(height: 10),
            _collection(t, compact),
            const SizedBox(height: 10),
            widget.contactQueue,
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
        icon: Icons.schedule_rounded,
        label: 'Próxima publicação:',
        value: 'Hoje, 18:00',
      ),
      _Metric(
        icon: Icons.chat_outlined,
        label: 'Conversas WhatsApp:',
        value: '0 novas',
        valueColor: const Color(0xFF079447),
      ),
      _Metric(
        icon: Icons.show_chart_rounded,
        label: 'Publicações:',
        value: '${widget.publicationCount}',
      ),
      _Metric(
        icon: Icons.visibility_outlined,
        label: 'Clientes:',
        value: '${widget.clientCount}',
      ),
      const _Metric(
        icon: Icons.trending_up_rounded,
        label: 'Horários:',
        value: '5',
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
              height: 412,
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

  Widget _editor(AgendaThemeTokens t, bool compact) => Padding(
    padding: EdgeInsets.fromLTRB(14, compact ? 16 : 12, 14, compact ? 10 : 8),
    child: Column(
      mainAxisSize: compact ? MainAxisSize.min : MainAxisSize.max,
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
                    'Monte a mensagem e escolha os horários',
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
            Expanded(child: _label(t, 'Horários disponíveis')),
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
        SizedBox(
          height: 30,
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                for (final time in _times) ...[
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
                  if (time != _times.last) const SizedBox(width: 5),
                ],
              ],
            ),
          ),
        ),
      ],
    ),
  );

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
                duration: const Duration(milliseconds: 150),
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
    final image = _images[_selectedImage];
    return Padding(
      padding: EdgeInsets.fromLTRB(16, compact ? 14 : 8, 16, compact ? 18 : 8),
      child: Stack(
        alignment: Alignment.center,
        children: [
          Align(
            alignment: Alignment.topRight,
            child: OutlinedButton(
              key: const Key('marketing-update-promotion'),
              onPressed: () {
                setState(
                  () => _selectedImage = (_selectedImage + 1) % _images.length,
                );
                widget.onUpdate();
              },
              child: const Text('Editar arte'),
            ),
          ),
          Padding(
            padding: EdgeInsets.only(top: compact ? 48 : 0),
            child: SizedBox(
              width: compact ? 220 : 198,
              height: compact ? 378 : 342,
              child: _StoryPreview(
                key: const Key('marketing-message-preview'),
                image: image,
                businessName: widget.businessName,
                message: widget.previewMessage,
                times: _selectedTimes.toList(growable: false),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _publication(AgendaThemeTokens t, bool compact) {
    final image = _images[_selectedImage];
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
              const FaIcon(
                FontAwesomeIcons.whatsapp,
                size: 18,
                color: Color(0xFF16A34A),
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
          _label(t, 'Imagem selecionada'),
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
            'Foto: ${image.author} · ${image.license}',
            textAlign: TextAlign.center,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(color: t.muted, fontSize: 8.5),
          ),
          const SizedBox(height: 4),
          Align(
            child: TextButton(
              onPressed: widget.onCopy,
              style: TextButton.styleFrom(
                minimumSize: const Size(0, 26),
                padding: const EdgeInsets.symmetric(horizontal: 8),
                tapTargetSize: MaterialTapTargetSize.shrinkWrap,
              ),
              child: const Text('Ver foto e licença'),
            ),
          ),
          if (!compact) const Spacer() else const SizedBox(height: 4),
          SizedBox(
            height: 36,
            child: OutlinedButton(
              key: const Key('marketing-copy-message'),
              onPressed: widget.onCopy,
              child: const Text('Exportar PNG'),
            ),
          ),
          const SizedBox(height: 8),
          SizedBox(
            height: 40,
            child: FilledButton.icon(
              key: const Key('marketing-open-whatsapp'),
              onPressed: widget.onWhatsApp,
              icon: const FaIcon(FontAwesomeIcons.whatsapp, size: 15),
              label: const Text('Publicar no WhatsApp'),
            ),
          ),
        ],
      ),
    );
  }

  Widget _collection(AgendaThemeTokens t, bool compact) => Container(
    decoration: _surfaceDecoration(t, radius: 15),
    padding: const EdgeInsets.fromLTRB(14, 10, 14, 10),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (compact) ...[
          Row(
            children: [
              Icon(Icons.photo_library_outlined, size: 18, color: t.ink),
              const SizedBox(width: 7),
              Text(
                'Coleção editorial',
                style: TextStyle(
                  color: t.ink,
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: _topicRow(),
          ),
          const SizedBox(height: 8),
          _searchBox(t),
        ] else
          Row(
            children: [
              Icon(Icons.photo_library_outlined, size: 18, color: t.ink),
              const SizedBox(width: 7),
              Text(
                'Coleção editorial',
                style: TextStyle(
                  color: t.ink,
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(width: 14),
              Text('Temas', style: TextStyle(color: t.muted, fontSize: 9.5)),
              const SizedBox(width: 7),
              Expanded(child: _topicRow()),
              const SizedBox(width: 12),
              SizedBox(width: 310, child: _searchBox(t)),
            ],
          ),
        const SizedBox(height: 8),
        SizedBox(
          height: 68,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            itemCount: _images.length,
            separatorBuilder: (_, _) => const SizedBox(width: 9),
            itemBuilder: (_, index) => _ImageTile(
              image: _images[index],
              selected: index == _selectedImage,
              onTap: () => setState(() => _selectedImage = index),
            ),
          ),
        ),
        const SizedBox(height: 5),
        Text(
          'Foto aplicada. Agora ajuste o texto ou publique no WhatsApp.',
          style: TextStyle(color: t.muted, fontSize: 9),
        ),
      ],
    ),
  );

  Widget _topicRow() => SingleChildScrollView(
    scrollDirection: Axis.horizontal,
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        for (final topic in _topics) ...[
          _ChoicePill(
            label: topic,
            selected: topic == _topic,
            onTap: () => setState(() {
              _topic = topic;
              _search.text = topic.toLowerCase();
            }),
          ),
          const SizedBox(width: 5),
        ],
      ],
    ),
  );

  Widget _searchBox(AgendaThemeTokens t) => Row(
    children: [
      Expanded(
        child: SizedBox(
          height: 36,
          child: TextField(
            controller: _search,
            decoration: const InputDecoration(
              isDense: true,
              contentPadding: EdgeInsets.symmetric(horizontal: 14, vertical: 9),
            ),
          ),
        ),
      ),
      const SizedBox(width: 7),
      SizedBox(
        height: 36,
        child: FilledButton.icon(
          onPressed: () => setState(() {}),
          icon: const Icon(Icons.search_rounded, size: 16),
          label: const Text('Buscar'),
        ),
      ),
    ],
  );

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
    required this.message,
    required this.times,
  });

  final _MarketingImage image;
  final String businessName;
  final String message;
  final List<String> times;

  @override
  Widget build(BuildContext context) {
    final accent = AgendaThemeTokens.of(context).accent;
    final visibleTimes = times.take(5).toList(growable: false);
    return ClipRRect(
      borderRadius: BorderRadius.circular(14),
      child: Stack(
        fit: StackFit.expand,
        children: [
          Image.asset(image.path, fit: BoxFit.cover),
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
                const Spacer(),
                const Text(
                  'HORÁRIOS\nLIVRES',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: Color(0xFFA63712),
                    fontSize: 22,
                    height: .95,
                    fontFamily: 'Segoe UI',
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 10),
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
                const SizedBox(height: 6),
                Container(
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
                  child: Wrap(
                    alignment: WrapAlignment.center,
                    spacing: 8,
                    runSpacing: 4,
                    children: [
                      for (final time in visibleTimes)
                        Text(
                          time,
                          style: const TextStyle(
                            color: Color(0xFFA63712),
                            fontSize: 12,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                    ],
                  ),
                ),
                const SizedBox(height: 10),
                Container(
                  height: 22,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: accent,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Text(
                    'AGENDE SEU HORÁRIO',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 7.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
                const SizedBox(height: 7),
                Text(
                  '(33) 99800-7978',
                  style: TextStyle(
                    color: Colors.white.withValues(alpha: .9),
                    fontSize: 7.5,
                  ),
                ),
                Text(
                  'Foto: ${image.author} · ${image.license}',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: Colors.white.withValues(alpha: .76),
                    fontSize: 5.5,
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
            child: Text.rich(
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
      child: Container(
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

class _ImageTile extends StatelessWidget {
  const _ImageTile({
    required this.image,
    required this.selected,
    required this.onTap,
  });

  final _MarketingImage image;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(8),
    child: Container(
      width: 160,
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(8),
        border: Border.all(
          color: selected
              ? AgendaThemeTokens.of(context).accent
              : Colors.transparent,
          width: 2,
        ),
      ),
      child: Stack(
        fit: StackFit.expand,
        children: [
          Image.asset(image.path, fit: BoxFit.cover),
          Align(
            alignment: Alignment.bottomCenter,
            child: Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 4),
              color: Colors.black.withValues(alpha: .55),
              child: Text(
                '${image.author}\n${image.license}',
                maxLines: 2,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 7.5,
                  height: 1.1,
                ),
              ),
            ),
          ),
        ],
      ),
    ),
  );
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
