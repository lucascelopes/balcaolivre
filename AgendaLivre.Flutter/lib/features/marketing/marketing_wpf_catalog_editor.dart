import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';

import '../../app/theme/agenda_theme.dart';
import '../../core/business_profile.dart';
import '../../domain/models/models.dart';

class MarketingWpfCatalogEditor extends StatefulWidget {
  const MarketingWpfCatalogEditor({
    super.key,
    required this.businessName,
    required this.profile,
    required this.catalog,
    required this.onBack,
    required this.onPublish,
  });

  final String businessName;
  final AgendaBusinessProfile profile;
  final MarketingCatalogPublication? catalog;
  final VoidCallback onBack;
  final Future<void> Function(MarketingCatalogPublication catalog) onPublish;

  @override
  State<MarketingWpfCatalogEditor> createState() =>
      _MarketingWpfCatalogEditorState();
}

class _MarketingWpfCatalogEditorState extends State<MarketingWpfCatalogEditor> {
  static const _legacyHeroImagePath =
      'assets/branding/marketing-site-hero-hair.png';

  late final TextEditingController _title;
  late final TextEditingController _support;
  late final TextEditingController _button;
  int _device = 0;
  int _inspectorTab = 0;
  int _alignment = 0;
  bool _showButton = true;
  bool _publishing = false;
  bool _saved = true;
  bool _didChooseInitialDevice = false;
  bool _didApplyMobileDefaults = false;

  String get _legacyDefaultTitle => switch (widget.profile.segment) {
    'Clínica médica' => 'Cuidado organizado, no seu tempo',
    'Petshop' => 'Cuidado para cada fase do seu pet',
    'Oficina' => 'Seu veículo em boas mãos',
    _ => 'Sua beleza, do seu jeito',
  };

  String get _mobileDefaultTitle => switch (widget.profile.segment) {
    'Clínica médica' => 'Cuidado organizado, no seu tempo',
    'Petshop' => 'Cuidado para cada fase do seu pet',
    'Oficina' => 'Seu veículo em boas mãos',
    'Barbearia' => 'Seu estilo, no seu tempo',
    'Serviços' => 'Serviços para facilitar sua rotina',
    _ => 'Seu próximo cuidado começa aqui',
  };

  bool get _isMobile => MediaQuery.sizeOf(context).width < 760;
  String get _mobileHeroImagePath => switch (widget.profile.segment) {
    'Oficina' => 'assets/branding/onboarding-team-workshop.png',
    'Barbearia' => 'assets/branding/onboarding-team-barber.png',
    _ => 'assets/branding/onboarding-segment.png',
  };
  String get _activeHeroImagePath =>
      _isMobile ? _mobileHeroImagePath : _legacyHeroImagePath;

  String get _defaultSupport => switch (widget.profile.segment) {
    'Salão de Beleza' =>
      'Realce sua essência com cuidados personalizados para você se sentir '
          'incrível todos os dias.',
    _ =>
      'Confira ${widget.profile.servicePlural}, valores e duração e escolha '
          'o melhor horário para ${widget.profile.activitySingular}.',
  };

  @override
  void initState() {
    super.initState();
    final catalog = widget.catalog;
    _title = TextEditingController(
      text: catalog?.title.trim().isNotEmpty == true
          ? catalog!.title
          : _legacyDefaultTitle,
    );
    _support = TextEditingController(
      text: catalog?.supportText.trim().isNotEmpty == true
          ? catalog!.supportText
          : _defaultSupport,
    );
    _button = TextEditingController(
      text: catalog?.buttonText.trim().isNotEmpty == true
          ? catalog!.buttonText
          : 'Agendar agora',
    );
    _showButton = catalog?.showButton ?? true;
    _alignment = switch (catalog?.alignment) {
      'center' => 1,
      'right' => 2,
      _ => 0,
    };
    _title.addListener(_changed);
    _support.addListener(_changed);
    _button.addListener(_changed);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_didApplyMobileDefaults) return;
    _didApplyMobileDefaults = true;
    if (_isMobile &&
        widget.catalog == null &&
        _title.text == _legacyDefaultTitle) {
      _title.text = _mobileDefaultTitle;
      _saved = true;
    }
  }

  @override
  void dispose() {
    _title
      ..removeListener(_changed)
      ..dispose();
    _support
      ..removeListener(_changed)
      ..dispose();
    _button
      ..removeListener(_changed)
      ..dispose();
    super.dispose();
  }

  void _changed() {
    if (mounted) setState(() => _saved = false);
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return LayoutBuilder(
      builder: (context, constraints) {
        final desktop = constraints.maxWidth >= 1040;
        final tablet = constraints.maxWidth >= 720;
        // On a narrow device the desktop preview navigation cannot be made
        // usable. Start with the phone canvas instead of rendering a clipped
        // desktop frame inside a 390 px screen.
        if (!_didChooseInitialDevice) {
          _device = tablet ? 0 : 2;
          _didChooseInitialDevice = true;
        }
        final horizontalPadding = desktop ? 18.0 : (tablet ? 16.0 : 12.0);
        return SingleChildScrollView(
          key: const Key('marketing-catalog-editor'),
          padding: EdgeInsets.fromLTRB(
            horizontalPadding,
            desktop ? 0 : 8,
            horizontalPadding,
            24,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _titleBlock(t, desktop),
              const SizedBox(height: 8),
              _toolbar(t, desktop),
              const SizedBox(height: 8),
              if (desktop)
                SizedBox(
                  height: 510,
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Expanded(child: _previewWorkspace(t, desktop: true)),
                      const SizedBox(width: 12),
                      SizedBox(width: 430, child: _inspector(t, true)),
                    ],
                  ),
                )
              else ...[
                _previewWorkspace(t, desktop: false),
                const SizedBox(height: 12),
                _inspector(t, false),
              ],
            ],
          ),
        );
      },
    );
  }

  Widget _titleBlock(AgendaThemeTokens t, bool desktop) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        'Editar catálogo',
        style: TextStyle(
          color: t.ink,
          fontSize: desktop ? 28 : 25,
          height: 1.05,
          fontWeight: FontWeight.w800,
        ),
      ),
      const SizedBox(height: 8),
      Container(
        width: 32,
        height: 3,
        decoration: BoxDecoration(
          color: t.accent,
          borderRadius: BorderRadius.circular(2),
        ),
      ),
    ],
  );

  Widget _toolbar(AgendaThemeTokens t, bool desktop) {
    final breadcrumb = Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        TextButton(
          key: const Key('marketing-catalog-back'),
          onPressed: widget.onBack,
          style: TextButton.styleFrom(
            foregroundColor: t.ink,
            minimumSize: const Size(0, 32),
            padding: const EdgeInsets.symmetric(horizontal: 12),
          ),
          child: const Text('Marketing'),
        ),
        Text('/', style: TextStyle(color: t.muted)),
        const SizedBox(width: 8),
        Text('Meu catálogo', style: TextStyle(color: t.ink, fontSize: 12)),
      ],
    );
    final devices = Container(
      height: 36,
      padding: const EdgeInsets.all(2),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          _deviceButton(t, 0, Icons.desktop_windows_outlined, 'Desktop'),
          _deviceButton(t, 1, Icons.tablet_mac_outlined, 'Tablet'),
          _deviceButton(t, 2, Icons.phone_iphone_outlined, 'Celular'),
        ],
      ),
    );
    final actions = Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        OutlinedButton.icon(
          onPressed: () => setState(() => _device = (_device + 1) % 3),
          icon: const Icon(Icons.visibility_outlined, size: 17),
          label: const Text('Visualizar'),
          style: OutlinedButton.styleFrom(
            foregroundColor: t.ink,
            minimumSize: const Size(0, 36),
          ),
        ),
        const SizedBox(width: 8),
        FilledButton.icon(
          key: const Key('marketing-catalog-publish'),
          onPressed: _publishing ? null : _publish,
          icon: _publishing
              ? const SizedBox(
                  width: 15,
                  height: 15,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const FaIcon(FontAwesomeIcons.globe, size: 14),
          label: const Text('Publicar'),
        ),
      ],
    );

    if (desktop) {
      return Row(
        children: [
          SizedBox(width: 330, child: breadcrumb),
          Expanded(child: Center(child: devices)),
          actions,
        ],
      );
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        breadcrumb,
        const SizedBox(height: 8),
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(children: [devices, const SizedBox(width: 10), actions]),
        ),
      ],
    );
  }

  Widget _deviceButton(
    AgendaThemeTokens t,
    int index,
    IconData icon,
    String label,
  ) {
    final selected = _device == index;
    return InkWell(
      key: Key('marketing-catalog-device-$index'),
      onTap: () => setState(() => _device = index),
      borderRadius: BorderRadius.circular(9),
      child: Container(
        height: 30,
        padding: const EdgeInsets.symmetric(horizontal: 14),
        decoration: BoxDecoration(
          color: selected ? t.accentSoft : Colors.transparent,
          border: Border.all(color: selected ? t.accent : Colors.transparent),
          borderRadius: BorderRadius.circular(9),
        ),
        child: Row(
          children: [
            Icon(icon, size: 16, color: t.ink),
            const SizedBox(width: 7),
            Text(label, style: TextStyle(color: t.ink, fontSize: 11)),
          ],
        ),
      ),
    );
  }

  Widget _previewWorkspace(AgendaThemeTokens t, {required bool desktop}) {
    final frameWidth = switch (_device) {
      1 => 660.0,
      2 => 375.0,
      _ => 1000.0,
    };
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFFF2EFEC),
        borderRadius: BorderRadius.circular(12),
      ),
      padding: EdgeInsets.all(desktop ? 0 : 8),
      alignment: Alignment.topCenter,
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: frameWidth),
        child: Container(
          decoration: BoxDecoration(
            color: const Color(0xFFFFFDFB),
            border: Border.all(color: t.line),
            borderRadius: BorderRadius.circular(12),
          ),
          clipBehavior: Clip.antiAlias,
          child: desktop
              ? Column(
                  children: [
                    _browserBar(t),
                    Expanded(
                      child: SingleChildScrollView(
                        child: _previewContent(t, desktop),
                      ),
                    ),
                  ],
                )
              : Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [_browserBar(t), _previewContent(t, desktop)],
                ),
        ),
      ),
    );
  }

  Widget _previewContent(AgendaThemeTokens t, bool desktop) => Column(
    mainAxisSize: MainAxisSize.min,
    children: [
      _catalogHeader(t),
      _hero(t, desktop),
      _addSection(t),
      _servicesPreview(t),
    ],
  );

  Widget _browserBar(AgendaThemeTokens t) => Container(
    height: 34,
    padding: const EdgeInsets.symmetric(horizontal: 11),
    decoration: BoxDecoration(
      color: const Color(0xFFF7F5F3),
      border: Border(bottom: BorderSide(color: t.line)),
    ),
    child: Row(
      children: [
        for (final color in const [
          Color(0xFFFF5F57),
          Color(0xFFFFBD2E),
          Color(0xFF28C840),
        ])
          Container(
            width: 10,
            height: 10,
            margin: const EdgeInsets.only(right: 6),
            decoration: BoxDecoration(color: color, shape: BoxShape.circle),
          ),
        const Spacer(),
        Flexible(
          flex: 5,
          child: Container(
            height: 23,
            constraints: const BoxConstraints(maxWidth: 360),
            padding: const EdgeInsets.symmetric(horizontal: 10),
            decoration: BoxDecoration(
              color: const Color(0xFFEFEDEA),
              borderRadius: BorderRadius.circular(7),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.lock, size: 12),
                const SizedBox(width: 7),
                Flexible(
                  child: Text(
                    'studio-${_slug(widget.businessName)}'
                    '.minhaagendalivre.com.br',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: t.muted, fontSize: 10),
                  ),
                ),
              ],
            ),
          ),
        ),
        const Spacer(),
        Icon(Icons.refresh_rounded, color: t.muted, size: 16),
      ],
    ),
  );

  Widget _catalogHeader(AgendaThemeTokens t) {
    final compact = _device == 2;
    return Container(
      height: compact ? 62 : 54,
      padding: EdgeInsets.symmetric(horizontal: compact ? 12 : 18),
      decoration: BoxDecoration(
        color: const Color(0xFFFFFDFB),
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          Expanded(
            flex: compact ? 1 : 2,
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  widget.businessName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Color(0xFF5B3329),
                    fontFamily: 'Georgia',
                    fontStyle: FontStyle.italic,
                    fontSize: 18,
                  ),
                ),
                Text(
                  widget.profile.segment.toUpperCase(),
                  style: const TextStyle(
                    color: Color(0xFF8C6E63),
                    fontSize: 6.5,
                  ),
                ),
              ],
            ),
          ),
          if (!compact) ...[
            Expanded(
              flex: 3,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  for (final label in const [
                    'Início',
                    'Serviços',
                    'Equipe',
                    'Contato',
                  ])
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 10),
                      child: Text(
                        label,
                        style: TextStyle(color: t.ink, fontSize: 10),
                      ),
                    ),
                ],
              ),
            ),
          ],
          if (_showButton)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 7),
              decoration: BoxDecoration(
                color: t.accent,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text(
                _button.text.trim().isEmpty ? 'Agendar agora' : _button.text,
                style: TextStyle(
                  color: Theme.of(context).colorScheme.onPrimary,
                  fontSize: 9.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
        ],
      ),
    );
  }

  Widget _hero(AgendaThemeTokens t, bool desktop) {
    if (_isMobile) return _mobileHero(t);
    final compact = _device == 2;
    final height = compact ? 440.0 : 300.0;
    final align = switch (_alignment) {
      1 => Alignment.center,
      2 => Alignment.centerRight,
      _ => Alignment.centerLeft,
    };
    return InkWell(
      onTap: () => setState(() => _inspectorTab = 0),
      child: Container(
        key: const Key('marketing-catalog-hero'),
        height: height,
        decoration: BoxDecoration(
          border: Border.all(color: t.accent, width: 1.5),
        ),
        clipBehavior: Clip.antiAlias,
        child: Stack(
          fit: StackFit.expand,
          children: [
            Image.asset(_activeHeroImagePath, fit: BoxFit.cover),
            Align(
              alignment: compact
                  ? Alignment.bottomCenter
                  : Alignment.centerLeft,
              child: Container(
                width: compact ? double.infinity : 470,
                height: compact ? 350 : double.infinity,
                color: const Color(0xD923201E),
              ),
            ),
            Align(
              alignment: align,
              child: Container(
                width: compact ? double.infinity : 350,
                padding: EdgeInsets.fromLTRB(
                  compact ? 24 : 42,
                  24,
                  compact ? 24 : 20,
                  24,
                ),
                child: FittedBox(
                  fit: BoxFit.scaleDown,
                  alignment: align,
                  child: SizedBox(
                    width: compact ? 327 : 288,
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: switch (_alignment) {
                        1 => CrossAxisAlignment.center,
                        2 => CrossAxisAlignment.end,
                        _ => CrossAxisAlignment.start,
                      },
                      children: [
                        Text(
                          _title.text.trim().isEmpty
                              ? (_isMobile
                                    ? _mobileDefaultTitle
                                    : _legacyDefaultTitle)
                              : _title.text,
                          textAlign: switch (_alignment) {
                            1 => TextAlign.center,
                            2 => TextAlign.right,
                            _ => TextAlign.left,
                          },
                          style: TextStyle(
                            color: const Color(0xFFFFFDFB),
                            fontFamily: 'Georgia',
                            fontSize: compact ? 31 : 34,
                            height: 1.08,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 10),
                        Container(width: 28, height: 2, color: t.accent),
                        const SizedBox(height: 10),
                        Text(
                          _support.text,
                          textAlign: switch (_alignment) {
                            1 => TextAlign.center,
                            2 => TextAlign.right,
                            _ => TextAlign.left,
                          },
                          maxLines: compact ? 4 : 3,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: Color(0xFFF2EAE5),
                            fontSize: 10.5,
                            height: 1.42,
                          ),
                        ),
                        const SizedBox(height: 15),
                        Wrap(
                          spacing: 18,
                          runSpacing: 8,
                          crossAxisAlignment: WrapCrossAlignment.center,
                          children: [
                            if (_showButton)
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 18,
                                  vertical: 9,
                                ),
                                decoration: BoxDecoration(
                                  color: t.accent,
                                  borderRadius: BorderRadius.circular(7),
                                ),
                                child: Text(
                                  _button.text.trim().isEmpty
                                      ? 'Agendar agora'
                                      : _button.text,
                                  style: TextStyle(
                                    color: Theme.of(
                                      context,
                                    ).colorScheme.onPrimary,
                                    fontSize: 10.5,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                              ),
                            const Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(
                                  'Conhecer serviços',
                                  style: TextStyle(
                                    color: Color(0xFFFFFDFB),
                                    fontFamily: 'Georgia',
                                    fontSize: 10.5,
                                  ),
                                ),
                                Icon(
                                  Icons.chevron_right_rounded,
                                  color: Color(0xFFFFFDFB),
                                  size: 14,
                                ),
                              ],
                            ),
                          ],
                        ),
                      ],
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

  Widget _mobileHero(AgendaThemeTokens t) {
    final title = _title.text.trim().isEmpty
        ? _mobileDefaultTitle
        : _title.text.trim();
    final previewItems = _mobilePreviewItems;
    final icon = switch (widget.profile.segment) {
      'Oficina' => FontAwesomeIcons.screwdriverWrench,
      'Petshop' => FontAwesomeIcons.paw,
      'Clínica médica' => FontAwesomeIcons.stethoscope,
      'Barbearia' => FontAwesomeIcons.scissors,
      _ => FontAwesomeIcons.calendarCheck,
    };
    return InkWell(
      onTap: () => setState(() => _inspectorTab = 0),
      borderRadius: BorderRadius.circular(18),
      child: Container(
        key: const Key('marketing-catalog-hero'),
        padding: const EdgeInsets.fromLTRB(22, 22, 22, 20),
        decoration: BoxDecoration(
          color: t.ink,
          border: Border.all(color: t.accent, width: 1.5),
          borderRadius: BorderRadius.circular(18),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
              decoration: BoxDecoration(
                color: t.accent.withValues(alpha: .18),
                borderRadius: BorderRadius.circular(999),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  FaIcon(icon, color: t.accent, size: 14),
                  const SizedBox(width: 7),
                  Text(
                    widget.profile.segment.toUpperCase(),
                    style: TextStyle(
                      color: t.accent,
                      fontSize: 9,
                      fontWeight: FontWeight.w800,
                      letterSpacing: .7,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            Text(
              title,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: Color(0xFFFFFDFB),
                fontFamily: 'Georgia',
                fontSize: 30,
                height: 1.08,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 10),
            Container(width: 34, height: 3, color: t.accent),
            const SizedBox(height: 10),
            Text(
              _support.text,
              maxLines: 3,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: Color(0xFFEFE9E5),
                fontSize: 11,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 16),
            if (previewItems.isEmpty)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: const Color(0x14FFFFFF),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Row(
                  children: [
                    FaIcon(
                      FontAwesomeIcons.layerGroup,
                      color: Color(0xFFFFFDFB),
                      size: 15,
                    ),
                    SizedBox(width: 9),
                    Expanded(
                      child: Text(
                        'Adicione serviços ao catálogo para exibir valores.',
                        style: TextStyle(
                          color: Color(0xFFEFE9E5),
                          fontSize: 10.5,
                        ),
                      ),
                    ),
                  ],
                ),
              )
            else
              for (final item in previewItems) ...[
                _mobilePreviewRow(t, item),
                const SizedBox(height: 8),
              ],
            const SizedBox(height: 8),
            if (_showButton)
              FilledButton.icon(
                onPressed: () {},
                icon: const FaIcon(FontAwesomeIcons.calendarCheck, size: 15),
                label: Text(
                  _button.text.trim().isEmpty ? 'Agendar agora' : _button.text,
                ),
              ),
          ],
        ),
      ),
    );
  }

  List<MarketingCatalogSectionItem> get _mobilePreviewItems {
    final sections =
        widget.catalog?.sections ?? const <MarketingCatalogSection>[];
    return sections
        .where((section) => section.enabled)
        .expand((section) => section.items)
        .where((item) => item.title.trim().isNotEmpty)
        .take(2)
        .toList(growable: false);
  }

  Widget _mobilePreviewRow(
    AgendaThemeTokens t,
    MarketingCatalogSectionItem item,
  ) {
    final detail = item.detail.trim().isNotEmpty
        ? item.detail.trim()
        : item.text.trim();
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: const Color(0x14FFFFFF),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0x24FFFFFF)),
      ),
      child: Row(
        children: [
          FaIcon(FontAwesomeIcons.circleCheck, color: t.accent, size: 15),
          const SizedBox(width: 9),
          Expanded(
            child: Text(
              item.title,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: Color(0xFFFFFDFB),
                fontSize: 11,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          if (detail.isNotEmpty) ...[
            const SizedBox(width: 8),
            Flexible(
              child: Text(
                detail,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                textAlign: TextAlign.right,
                style: const TextStyle(color: Color(0xFFEFE9E5), fontSize: 9.5),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _addSection(AgendaThemeTokens t) => Padding(
    padding: const EdgeInsets.fromLTRB(18, 10, 18, 0),
    child: OutlinedButton.icon(
      onPressed: () => setState(() => _inspectorTab = 2),
      icon: const Icon(Icons.add_rounded, size: 14),
      label: const Text('Adicionar seção'),
      style: OutlinedButton.styleFrom(
        foregroundColor: t.ink,
        minimumSize: const Size(0, 30),
        visualDensity: VisualDensity.compact,
      ),
    ),
  );

  Widget _servicesPreview(AgendaThemeTokens t) => Padding(
    padding: const EdgeInsets.fromLTRB(18, 8, 18, 14),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Agende ${widget.profile.activitySingular}',
          style: TextStyle(
            color: t.ink,
            fontFamily: 'Georgia',
            fontSize: 17,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 3),
        Text(
          'Confira ${widget.profile.servicePlural} disponíveis, valores e duração antes de '
          'escolher o melhor horário.',
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(color: t.muted, fontSize: 8.5),
        ),
      ],
    ),
  );

  Widget _inspector(AgendaThemeTokens t, bool desktop) => Container(
    decoration: BoxDecoration(
      color: t.panel,
      border: Border.all(color: t.line),
      borderRadius: BorderRadius.circular(14),
      boxShadow: [
        BoxShadow(
          color: Colors.black.withValues(alpha: .04),
          blurRadius: 16,
          offset: const Offset(0, 4),
        ),
      ],
    ),
    padding: const EdgeInsets.all(18),
    child: Column(
      mainAxisSize: desktop ? MainAxisSize.max : MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                'Editar seção',
                style: TextStyle(
                  color: t.ink,
                  fontSize: 17,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
            IconButton.outlined(
              onPressed: widget.onBack,
              icon: const Icon(Icons.close_rounded, size: 17),
              tooltip: 'Fechar editor',
              visualDensity: VisualDensity.compact,
            ),
          ],
        ),
        const SizedBox(height: 10),
        Row(
          children: [
            Expanded(
              child: Text(
                'Capa principal',
                style: TextStyle(
                  color: t.ink,
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              decoration: BoxDecoration(
                color: const Color(0xFFF1EFED),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                'Seção',
                style: TextStyle(color: t.muted, fontSize: 8.5),
              ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        _inspectorTabs(t),
        const SizedBox(height: 10),
        if (desktop)
          Expanded(child: SingleChildScrollView(child: _inspectorContent(t)))
        else
          _inspectorContent(t),
        const SizedBox(height: 8),
        Row(
          children: [
            Container(
              width: 9,
              height: 9,
              decoration: const BoxDecoration(
                color: Color(0xFF27C266),
                shape: BoxShape.circle,
              ),
            ),
            const SizedBox(width: 8),
            Text(
              _saved ? 'Salvo automaticamente' : 'Alterações pendentes',
              style: TextStyle(color: t.muted, fontSize: 9.5),
            ),
          ],
        ),
      ],
    ),
  );

  Widget _inspectorTabs(AgendaThemeTokens t) => Container(
    height: 38,
    decoration: BoxDecoration(
      border: Border(bottom: BorderSide(color: t.line)),
    ),
    child: Row(
      children: [
        for (var index = 0; index < 3; index++)
          Expanded(
            child: InkWell(
              key: Key('marketing-catalog-tab-$index'),
              onTap: () => setState(() => _inspectorTab = index),
              borderRadius: BorderRadius.circular(9),
              child: Container(
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: _inspectorTab == index
                      ? t.accentSoft
                      : Colors.transparent,
                  border: Border.all(
                    color: _inspectorTab == index
                        ? t.accent
                        : Colors.transparent,
                  ),
                  borderRadius: BorderRadius.circular(9),
                ),
                child: Text(
                  const ['Conteúdo', 'Estilo', 'Seções'][index],
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 10.5,
                    fontWeight: _inspectorTab == index
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

  Widget _inspectorContent(AgendaThemeTokens t) => switch (_inspectorTab) {
    1 => _styleInspector(t),
    2 => _sectionsInspector(t),
    _ => _contentInspector(t),
  };

  Widget _contentInspector(AgendaThemeTokens t) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      _label(t, 'Título'),
      _field(
        key: const Key('marketing-catalog-title'),
        controller: _title,
        maxLines: 1,
      ),
      const SizedBox(height: 8),
      _label(t, 'Texto de apoio'),
      _field(
        key: const Key('marketing-catalog-support'),
        controller: _support,
        maxLines: 3,
      ),
      const SizedBox(height: 8),
      _label(t, 'Imagem de fundo'),
      const SizedBox(height: 5),
      Row(
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(6),
            child: Image.asset(
              _activeHeroImagePath,
              width: 62,
              height: 58,
              fit: BoxFit.cover,
            ),
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Imagem padrão do Agenda Livre',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Arte neutra que funciona em ${widget.profile.segment}.',
                  style: TextStyle(color: t.muted, fontSize: 9.5),
                ),
              ],
            ),
          ),
        ],
      ),
      Align(
        alignment: Alignment.centerRight,
        child: Text(
          'Incluída no app e pronta para publicar',
          style: TextStyle(color: t.muted, fontSize: 8.5),
        ),
      ),
      const SizedBox(height: 9),
      _label(t, 'Alinhamento do conteúdo'),
      const SizedBox(height: 5),
      Container(
        height: 34,
        decoration: BoxDecoration(
          border: Border.all(color: t.line),
          borderRadius: BorderRadius.circular(7),
        ),
        child: Row(
          children: [
            for (var index = 0; index < 3; index++)
              Expanded(
                child: InkWell(
                  onTap: () => setState(() {
                    _alignment = index;
                    _saved = false;
                  }),
                  borderRadius: BorderRadius.circular(7),
                  child: Container(
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: _alignment == index
                          ? t.accentSoft
                          : Colors.transparent,
                      border: Border.all(
                        color: _alignment == index
                            ? t.accent
                            : Colors.transparent,
                      ),
                      borderRadius: BorderRadius.circular(7),
                    ),
                    child: Icon(
                      const [
                        Icons.format_align_left_rounded,
                        Icons.format_align_center_rounded,
                        Icons.format_align_right_rounded,
                      ][index],
                      size: 17,
                      color: t.ink,
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
      const SizedBox(height: 9),
      Row(
        children: [
          Expanded(child: _label(t, 'Cor de destaque')),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 4),
            decoration: BoxDecoration(
              border: Border.all(color: t.line),
              borderRadius: BorderRadius.circular(7),
            ),
            child: Row(
              children: [
                Container(
                  width: 22,
                  height: 22,
                  decoration: BoxDecoration(
                    color: t.accent,
                    borderRadius: BorderRadius.circular(5),
                  ),
                ),
                const SizedBox(width: 7),
                Text(
                  _isMobile ? _colorHex(t.accent) : '#FC601D',
                  style: TextStyle(color: t.ink, fontSize: 10),
                ),
              ],
            ),
          ),
        ],
      ),
      const SizedBox(height: 8),
      SwitchListTile.adaptive(
        contentPadding: EdgeInsets.zero,
        dense: true,
        title: Text(
          'Mostrar botão',
          style: TextStyle(color: t.ink, fontSize: 10.5),
        ),
        value: _showButton,
        onChanged: (value) => setState(() {
          _showButton = value;
          _saved = false;
        }),
      ),
      _label(t, 'Texto do botão'),
      _field(controller: _button, maxLines: 1),
    ],
  );

  Widget _styleInspector(AgendaThemeTokens t) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      _label(t, 'Tipografia dos títulos'),
      const SizedBox(height: 5),
      DropdownButtonFormField<String>(
        initialValue: 'Georgia',
        isExpanded: true,
        items: const [
          DropdownMenuItem(value: 'Georgia', child: Text('Georgia elegante')),
          DropdownMenuItem(value: 'Inter', child: Text('Inter moderna')),
          DropdownMenuItem(value: 'Serif', child: Text('Serif clássica')),
        ],
        onChanged: (_) => setState(() => _saved = false),
      ),
      const SizedBox(height: 12),
      _label(t, 'Contraste da imagem'),
      Slider(
        value: 64,
        min: 20,
        max: 90,
        divisions: 14,
        label: '64%',
        onChanged: (_) => setState(() => _saved = false),
      ),
      const SizedBox(height: 8),
      _label(t, 'Estilo dos botões'),
      const SizedBox(height: 5),
      SegmentedButton<int>(
        segments: const [
          ButtonSegment(value: 0, label: Text('Arredondado')),
          ButtonSegment(value: 1, label: Text('Reto')),
        ],
        selected: const {0},
        onSelectionChanged: (_) => setState(() => _saved = false),
      ),
      const SizedBox(height: 14),
      Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: t.accentSoft,
          border: Border.all(color: t.line),
          borderRadius: BorderRadius.circular(9),
        ),
        child: Text(
          'O estilo segue as mesmas cores e contrastes escolhidos para o '
          'Agenda Livre.',
          style: TextStyle(color: t.ink, fontSize: 10, height: 1.35),
        ),
      ),
    ],
  );

  Widget _sectionsInspector(AgendaThemeTokens t) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      _label(t, 'Seções do catálogo'),
      const SizedBox(height: 7),
      for (final row in const [
        ('Cabeçalho', FontAwesomeIcons.windowMaximize, true),
        ('Capa principal', FontAwesomeIcons.image, true),
        ('Serviços', FontAwesomeIcons.scissors, true),
        ('Equipe', FontAwesomeIcons.userGroup, false),
        ('Contato e rodapé', FontAwesomeIcons.addressCard, true),
      ])
        Container(
          margin: const EdgeInsets.only(bottom: 7),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          decoration: BoxDecoration(
            color: const Color(0xFFFFFDFB),
            border: Border.all(color: t.line),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Row(
            children: [
              FaIcon(row.$2, color: t.ink, size: 15),
              const SizedBox(width: 9),
              Expanded(
                child: Text(
                  row.$1,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 10.5,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              Switch.adaptive(
                value: row.$3,
                onChanged: (_) => setState(() => _saved = false),
              ),
            ],
          ),
        ),
      OutlinedButton.icon(
        onPressed: () => setState(() => _saved = false),
        icon: const Icon(Icons.add_rounded, size: 16),
        label: const Text('Adicionar nova seção'),
      ),
    ],
  );

  Widget _label(AgendaThemeTokens t, String text) => Text(
    text,
    style: TextStyle(color: t.ink, fontSize: 10.5, fontWeight: FontWeight.w700),
  );

  Widget _field({
    Key? key,
    required TextEditingController controller,
    required int maxLines,
  }) => TextField(
    key: key,
    controller: controller,
    minLines: maxLines == 1 ? 1 : 2,
    maxLines: maxLines,
    style: const TextStyle(fontSize: 11.5),
    decoration: const InputDecoration(
      isDense: true,
      contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 10),
    ),
  );

  Future<void> _publish() async {
    setState(() => _publishing = true);
    final previous = widget.catalog;
    final catalog = MarketingCatalogPublication(
      addressSnapshotVersion: previous?.addressSnapshotVersion ?? 0,
      slug: previous?.slug ?? _slug(widget.businessName),
      customDomain: previous?.customDomain ?? '',
      title: _title.text.trim().isEmpty
          ? (_isMobile ? _mobileDefaultTitle : _legacyDefaultTitle)
          : _title.text.trim(),
      supportText: _support.text.trim(),
      buttonText: _button.text.trim().isEmpty
          ? 'Agendar agora'
          : _button.text.trim(),
      heroImagePath: _activeHeroImagePath,
      accentColor: _isMobile
          ? _colorHex(AgendaThemeTokens.of(context).accent)
          : previous?.accentColor ?? '#FC601D',
      alignment: const ['left', 'center', 'right'][_alignment],
      spacing: previous?.spacing ?? 'compact',
      titleFont: previous?.titleFont ?? 'Georgia',
      imageContrast: previous?.imageContrast ?? 64,
      showButton: _showButton,
      header: previous?.header,
      footer: previous?.footer,
      design: previous?.design,
      sections: previous?.sections,
      seoTitle: previous?.seoTitle ?? '',
      seoDescription: previous?.seoDescription ?? '',
      promotion: previous?.promotion,
      publishedAt: DateTime.now(),
    );
    try {
      await widget.onPublish(catalog);
      if (mounted) setState(() => _saved = true);
    } finally {
      if (mounted) setState(() => _publishing = false);
    }
  }

  String _colorHex(Color color) {
    final rgb = color.toARGB32() & 0x00FFFFFF;
    return '#${rgb.toRadixString(16).padLeft(6, '0').toUpperCase()}';
  }

  String _slug(String value) {
    final slug = value
        .toLowerCase()
        .replaceAll(RegExp(r'[^a-z0-9]+'), '-')
        .replaceAll(RegExp(r'^-+|-+$'), '');
    return slug.isEmpty ? 'meu-catalogo' : slug;
  }
}
