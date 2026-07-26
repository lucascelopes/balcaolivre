import 'dart:convert';
import 'dart:math' as math;

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:image/image.dart' as img;

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../services/via_cep_service.dart';
import 'wpf_onboarding_templates.dart';

class OnboardingPage extends StatefulWidget {
  const OnboardingPage({
    super.key,
    required this.controller,
    this.pickBusinessLogo,
    this.normalizeBusinessLogo,
  });

  final AgendaController controller;
  final Future<PlatformFile?> Function()? pickBusinessLogo;
  final Future<Uint8List> Function(Uint8List source)? normalizeBusinessLogo;

  @override
  State<OnboardingPage> createState() => _OnboardingPageState();
}

class _OnboardingPageState extends State<OnboardingPage> {
  final _viaCep = ViaCepService();
  final _name = TextEditingController();
  final _phone = TextEditingController();
  final _email = TextEditingController();
  final _business = TextEditingController();
  final _cep = TextEditingController();
  final _neighborhood = TextEditingController();
  final _street = TextEditingController();
  final _number = TextEditingController();
  final _complement = TextEditingController();

  final _nameFocus = FocusNode();
  final _phoneFocus = FocusNode();
  final _emailFocus = FocusNode();
  final _businessFocus = FocusNode();

  int _step = 0;
  bool _showingThemeSelection = false;
  String _segment = '';
  String _themeId = '';
  String _teamSize = '';
  String _objective = '';
  String _lastLookupCep = '';
  bool _lookingUpCep = false;
  bool _finishing = false;
  bool _pickingLogo = false;
  Uint8List? _logoBytes;
  String _logoFileName = '';
  String _pendingLogoDataUrl = '';

  static const _segments = <_SegmentChoice>[
    _SegmentChoice('Salão de Beleza', Icons.content_cut_rounded, 'salao'),
    _SegmentChoice(
      'Barbearia',
      Icons.face_retouching_natural_rounded,
      'barbearia',
    ),
    _SegmentChoice(
      'Centro de Estética',
      Icons.auto_awesome_rounded,
      'centro-estetica',
    ),
    _SegmentChoice('Podologia', Icons.accessibility_new_rounded, 'podologia'),
    _SegmentChoice('Spa', Icons.spa_outlined, 'spa'),
    _SegmentChoice(
      'Clínica médica',
      Icons.medical_services_outlined,
      'clinica-medica',
    ),
    _SegmentChoice('Petshop', Icons.pets_rounded, 'petshop'),
    _SegmentChoice('Oficina', Icons.car_repair_rounded, 'oficina'),
  ];

  static const _teamChoices = <_TextChoice>[
    _TextChoice('1 profissional', '1', 'Só eu', 'one'),
    _TextChoice('2 profissionais', '2', 'pessoas', 'two'),
    _TextChoice('3 a 4 profissionais', '3–4', 'pessoas', 'three-four'),
    _TextChoice('5 a 9 profissionais', '5–9', 'pessoas', 'five-nine'),
    _TextChoice('10 ou mais profissionais', '10+', 'pessoas', 'ten-plus'),
  ];

  static const _objectiveChoices = <_ObjectiveChoice>[
    _ObjectiveChoice(
      'Divulgar serviços',
      'Atrair mais clientes',
      Icons.campaign_outlined,
      'services',
    ),
    _ObjectiveChoice(
      'Organizar agenda',
      'Organizar minha agenda',
      Icons.calendar_month_outlined,
      'agenda',
    ),
    _ObjectiveChoice(
      'Implementar agendamento online',
      'Oferecer agendamento online',
      Icons.schedule_rounded,
      'online',
    ),
    _ObjectiveChoice(
      'Dar autonomia aos profissionais',
      'Facilitar a gestão da equipe',
      Icons.groups_outlined,
      'autonomy',
    ),
    _ObjectiveChoice(
      'Administrar financeiro',
      'Controlar finanças e pagamentos',
      Icons.stacked_line_chart_rounded,
      'finance',
    ),
    _ObjectiveChoice(
      'Fidelizar clientes',
      'Fidelizar clientes',
      Icons.favorite_border_rounded,
      'loyalty',
    ),
  ];

  AgendaThemeSpec get _activeTheme => AgendaThemes.byId(_themeId);
  AgendaThemeTokens get _tokens => _activeTheme.tokens;
  String get _sideIllustrationAsset {
    if (_step == 1 && _showingThemeSelection) {
      return 'assets/branding/onboarding-theme.png';
    }
    if (_step == 2 && _segment == 'Barbearia') {
      return 'assets/branding/onboarding-team-barber.png';
    }
    if (_step == 2 && _segment == 'Oficina') {
      return 'assets/branding/onboarding-team-workshop.png';
    }
    return switch (_step) {
      0 => 'assets/branding/onboarding-store-calendar.png',
      1 => 'assets/branding/onboarding-segment.png',
      2 => 'assets/branding/onboarding-team.png',
      3 => 'assets/branding/onboarding-goal.png',
      4 => 'assets/branding/onboarding-address.png',
      _ => 'assets/branding/onboarding-review.png',
    };
  }

  String get _sideTitle {
    if (_step == 1 && _showingThemeSelection) {
      return 'Agora, escolha seu estilo.';
    }
    return switch (_step) {
      0 => 'Tudo pronto para começar.',
      1 => 'Qual é o seu negócio?',
      2 => 'Quem atende com você?',
      3 => 'Onde você quer chegar?',
      4 => 'Onde fica o seu negócio?',
      _ => 'Tudo certo por aqui.',
    };
  }

  @override
  void initState() {
    super.initState();
    final settings = widget.controller.data.settings;
    _name.text = settings.accountFullName;
    _phone.text = settings.accountPhone;
    _email.text = widget.controller.accountEmail;
    _business.text = _isDefaultBusinessName(settings.businessName)
        ? ''
        : settings.businessName;
    _cep.text = settings.postalCode;
    _neighborhood.text = settings.neighborhood;
    _street.text = settings.street;
    _number.text = settings.addressNumber;
    _complement.text = settings.addressComplement;
    _segment = settings.businessSegment == 'Mecânica'
        ? 'Oficina'
        : settings.businessSegment;
    _themeId = settings.themeId;
    _teamSize = _normalizedTeamSize(settings.professionalCountRange);
    _objective = _normalizedObjective(settings.mainObjective);
    final storedLogo = settings.businessLogoPath.trim();
    if (storedLogo.startsWith('data:image/') && storedLogo.contains(',')) {
      try {
        _logoBytes = base64Decode(
          storedLogo.substring(storedLogo.indexOf(',') + 1),
        );
        _pendingLogoDataUrl = storedLogo;
        _logoFileName = 'Alterar logo';
      } on FormatException {
        _logoBytes = null;
      }
    } else if (storedLogo.isNotEmpty) {
      _logoFileName = 'Alterar logo';
    }
  }

  @override
  void dispose() {
    for (final controller in [
      _name,
      _phone,
      _email,
      _business,
      _cep,
      _neighborhood,
      _street,
      _number,
      _complement,
    ]) {
      controller.dispose();
    }
    for (final focusNode in [
      _nameFocus,
      _phoneFocus,
      _emailFocus,
      _businessFocus,
    ]) {
      focusNode.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Theme(
      data: _activeTheme.toThemeData(),
      child: Scaffold(
        backgroundColor: _tokens.appBackground,
        body: LayoutBuilder(
          builder: (context, constraints) {
            if (constraints.maxWidth < 1000) return _mobileLayout();
            return _desktopLayout(constraints);
          },
        ),
      ),
    );
  }

  Widget _desktopLayout(BoxConstraints constraints) {
    final sidebarWidth = constraints.maxWidth * .41;

    return SizedBox.expand(
      key: const Key('onboarding-card'),
      child: Row(
        children: [
          SizedBox(
            key: const Key('onboarding-sidebar'),
            width: sidebarWidth,
            child: _studioSidePanel(),
          ),
          Container(width: 1, color: const Color(0xFFF2E5DC)),
          Expanded(
            key: const Key('onboarding-content'),
            child: ColoredBox(color: _tokens.panel, child: _desktopContent()),
          ),
        ],
      ),
    );
  }

  Widget _studioSidePanel() {
    final t = _tokens;
    return Container(
      color: t.warmSoft,
      child: LayoutBuilder(
        builder: (context, constraints) {
          return Padding(
            padding: const EdgeInsets.fromLTRB(52, 34, 56, 19),
            child: Stack(
              children: [
                Align(
                  alignment: Alignment.topLeft,
                  child: Image.asset(
                    'assets/branding/agenda-livre-mark.png',
                    key: const Key('onboarding-brand-mark'),
                    width: 106,
                    height: 56,
                    fit: BoxFit.contain,
                    alignment: Alignment.centerLeft,
                    filterQuality: FilterQuality.high,
                  ),
                ),
                Positioned.fill(
                  top: 80,
                  bottom: 62,
                  child: Transform.translate(
                    offset: const Offset(-10, -8),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Flexible(
                          child: ConstrainedBox(
                            constraints: const BoxConstraints(
                              maxWidth: 440,
                              maxHeight: 330,
                            ),
                            child: Image.asset(
                              _sideIllustrationAsset,
                              key: const Key('onboarding-illustration'),
                              fit: BoxFit.contain,
                              filterQuality: FilterQuality.high,
                            ),
                          ),
                        ),
                        const SizedBox(height: 18),
                        Text(
                          _sideTitle,
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            color: t.ink,
                            fontSize: 31,
                            height: 1.12,
                            fontWeight: FontWeight.w600,
                            letterSpacing: -.25,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                Align(
                  alignment: Alignment.bottomLeft,
                  child: SizedBox(
                    width: math.min(390, constraints.maxWidth),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Etapa ${_step + 1} de 6',
                          style: TextStyle(
                            color: t.accentDark,
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const SizedBox(height: 14),
                        ClipRRect(
                          borderRadius: BorderRadius.circular(3),
                          child: LinearProgressIndicator(
                            key: const Key('onboarding-side-progress'),
                            value: (_step + 1) / 6,
                            minHeight: 5,
                            backgroundColor: t.accentSoft,
                            valueColor: AlwaysStoppedAnimation<Color>(
                              t.accentDark,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _mobileLayout() {
    final t = _tokens;
    return SafeArea(
      child: Column(
        children: [
          Container(
            key: const Key('onboarding-mobile-header'),
            color: t.sidebarBackground,
            padding: const EdgeInsets.fromLTRB(18, 14, 18, 12),
            child: Column(
              children: [
                Row(
                  children: [
                    _brandIcon(size: 42, iconSize: 23, radius: 12),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        'Agenda Livre',
                        style: TextStyle(
                          color: t.ink,
                          fontSize: 19,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    Text(
                      '${_step + 1}/6',
                      key: const Key('onboarding-progress'),
                      style: TextStyle(
                        color: t.accent,
                        fontSize: 13,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                _progressDots(compact: true),
              ],
            ),
          ),
          if (_step > 0) _topBar(desktop: false),
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(20, 24, 20, 28),
              child: _stepWidget(desktop: false),
            ),
          ),
        ],
      ),
    );
  }

  Widget _brandIcon({
    required double size,
    required double iconSize,
    required double radius,
  }) {
    final t = _tokens;
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(radius),
        boxShadow: const [
          BoxShadow(
            color: Color(0x10000000),
            blurRadius: 12,
            offset: Offset(0, 4),
          ),
        ],
      ),
      alignment: Alignment.center,
      child: Icon(
        Icons.calendar_month_rounded,
        color: t.accent,
        size: iconSize,
      ),
    );
  }

  Widget _progressDots({bool compact = false}) {
    final t = _tokens;
    final circle = compact ? 20.0 : 24.0;
    final line = compact ? 13.0 : 18.0;
    return Semantics(
      label: 'Etapa ${_step + 1} de 6',
      child: Row(
        mainAxisSize: compact ? MainAxisSize.max : MainAxisSize.min,
        children: [
          for (var index = 0; index < 6; index++) ...[
            Semantics(
              button: true,
              selected: index == _step,
              label: 'Etapa ${index + 1} de 6',
              child: InkResponse(
                onTap: _finishing ? null : () => _goToStep(index),
                radius: circle,
                child: Container(
                  width: circle,
                  height: circle,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: index <= _step ? t.accent : t.panel,
                    border: Border.all(
                      color: index <= _step ? t.accent : t.line,
                    ),
                  ),
                  alignment: Alignment.center,
                  child: Text(
                    '${index + 1}',
                    style: TextStyle(
                      color: index <= _step ? Colors.white : t.muted,
                      fontSize: compact ? 9 : 11,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ),
            ),
            if (index < 5)
              Expanded(
                flex: compact ? 1 : 0,
                child: Container(
                  width: compact ? null : line,
                  height: 2,
                  color: index < _step ? t.accent : t.line,
                ),
              ),
          ],
        ],
      ),
    );
  }

  Widget _desktopContent() {
    if (_step == 0) {
      final scale = (MediaQuery.sizeOf(context).width / 1365)
          .clamp(1.0, 1.3)
          .toDouble();
      return Stack(
        fit: StackFit.expand,
        children: [
          Positioned(
            top: 0,
            left: 0,
            child: IgnorePointer(
              child: Opacity(
                opacity: 0,
                child: Text(
                  '${_step + 1}/6',
                  key: const Key('onboarding-progress'),
                ),
              ),
            ),
          ),
          Positioned.fill(child: _desktopInitialBody()),
          Positioned(top: 38 * scale, right: 42 * scale, child: _skipLink()),
        ],
      );
    }
    return Stack(
      fit: StackFit.expand,
      children: [
        Positioned(
          top: 0,
          left: 0,
          child: IgnorePointer(
            child: Opacity(
              opacity: 0,
              child: Text(
                '${_step + 1}/6',
                key: const Key('onboarding-progress'),
              ),
            ),
          ),
        ),
        Positioned.fill(child: _desktopStepBody()),
        Positioned(top: 20, left: 34, child: _desktopBackLink()),
        if (!_showingThemeSelection && _step < 5)
          Positioned(top: 20, right: 34, child: _skipLink()),
      ],
    );
  }

  Widget _desktopBackLink() {
    final t = _tokens;
    return TextButton.icon(
      key: const Key('onboarding-back'),
      onPressed: _finishing ? null : _back,
      style: TextButton.styleFrom(
        foregroundColor: t.accentDark,
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
        textStyle: const TextStyle(
          fontFamily: 'Segoe UI',
          fontSize: 14,
          fontWeight: FontWeight.w600,
        ),
      ),
      icon: const Icon(Icons.arrow_back_rounded, size: 18),
      label: const Text('Voltar'),
    );
  }

  Widget _desktopInitialBody() {
    return LayoutBuilder(
      builder: (context, constraints) {
        return SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(74, 125, 20, 30),
          child: ConstrainedBox(
            constraints: BoxConstraints(
              minHeight: math.max(0, constraints.maxHeight - 155),
            ),
            child: Align(
              alignment: Alignment.topCenter,
              child: _desktopInitialStep(scale: 1),
            ),
          ),
        );
      },
    );
  }

  Widget _skipLink() {
    final t = _tokens;
    return TextButton.icon(
      key: const Key('onboarding-skip'),
      onPressed: _finishing ? null : _skip,
      style: TextButton.styleFrom(
        foregroundColor: t.ink,
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
        textStyle: const TextStyle(
          fontFamily: 'Segoe UI',
          fontSize: 15,
          fontWeight: FontWeight.w600,
        ),
      ),
      iconAlignment: IconAlignment.end,
      icon: Icon(Icons.arrow_forward_ios_rounded, size: 15, color: t.accent),
      label: const Text('Pular'),
    );
  }

  Widget _desktopStepBody() {
    return LayoutBuilder(
      builder: (context, constraints) {
        return SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(74, 140, 20, 30),
          child: ConstrainedBox(
            constraints: BoxConstraints(
              minHeight: math.max(0, constraints.maxHeight - 170),
            ),
            child: Align(
              alignment: Alignment.topCenter,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  _desktopStepStrip(),
                  SizedBox(height: _showingThemeSelection ? 16 : 38),
                  _stepWidget(desktop: true),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  Widget _topBar({required bool desktop}) {
    final t = _tokens;
    final progress = Text(
      '${_step + 1}/6',
      key: desktop ? const Key('onboarding-progress') : null,
      style: TextStyle(
        color: t.muted,
        fontSize: 13,
        fontWeight: FontWeight.w600,
      ),
    );

    if (!desktop) {
      return Container(
        height: 56,
        padding: const EdgeInsets.symmetric(horizontal: 14),
        decoration: BoxDecoration(
          color: t.panel,
          border: Border(bottom: BorderSide(color: t.line)),
        ),
        child: Row(
          children: [
            _backButton(compact: true),
            const Spacer(),
            if (!_showingThemeSelection && _step < 5) ...[
              _skipButton(compact: true),
              const SizedBox(width: 10),
            ],
            progress,
          ],
        ),
      );
    }

    return Container(
      height: 64,
      padding: const EdgeInsets.symmetric(horizontal: 40),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          SizedBox(
            width: 150,
            child: Align(alignment: Alignment.centerLeft, child: _backButton()),
          ),
          Expanded(
            child: Center(
              child: Text(
                'Cadastro do negócio',
                style: TextStyle(
                  color: t.ink,
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ),
          SizedBox(
            width: 150,
            child: Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                if (!_showingThemeSelection && _step < 5) ...[
                  _skipButton(compact: true),
                  const SizedBox(width: 10),
                ],
                progress,
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _backButton({bool compact = false}) {
    final t = _tokens;
    return SizedBox(
      width: compact ? 44 : 132,
      height: 32,
      child: OutlinedButton(
        key: const Key('onboarding-back'),
        onPressed: _finishing ? null : _back,
        style: OutlinedButton.styleFrom(
          padding: EdgeInsets.symmetric(horizontal: compact ? 0 : 12),
          foregroundColor: t.ink,
          side: BorderSide(color: t.line),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(9)),
        ),
        child: compact
            ? const Icon(Icons.arrow_back_rounded, size: 19)
            : const FittedBox(
                fit: BoxFit.scaleDown,
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.arrow_back_rounded, size: 19),
                    SizedBox(width: 9),
                    Text('Voltar', style: TextStyle(fontSize: 14)),
                  ],
                ),
              ),
      ),
    );
  }

  Widget _skipButton({bool compact = false}) {
    final t = _tokens;
    return SizedBox(
      width: compact ? 74 : 98,
      height: compact ? 32 : 34,
      child: OutlinedButton(
        key: const Key('onboarding-skip'),
        onPressed: _finishing ? null : _skip,
        style: OutlinedButton.styleFrom(
          padding: EdgeInsets.zero,
          foregroundColor: t.ink,
          side: BorderSide(color: t.line),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
        ),
        child: const FittedBox(
          fit: BoxFit.scaleDown,
          child: Text(
            'Pular',
            style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
          ),
        ),
      ),
    );
  }

  Widget _stepWidget({required bool desktop}) {
    if (_step == 0) return _initialStep(desktop: desktop);
    if (_step == 1 && _showingThemeSelection) {
      return _themeStep(desktop: desktop);
    }
    return switch (_step) {
      1 => _segmentStep(desktop: desktop),
      2 => _teamStep(desktop: desktop),
      3 => _objectiveStep(desktop: desktop),
      4 => _addressStep(desktop: desktop),
      _ => _reviewStep(desktop: desktop),
    };
  }

  Widget _initialStep({required bool desktop}) {
    if (desktop) return _desktopInitialStep(scale: 1);

    final t = _tokens;
    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 600),
      child: Column(
        children: [
          Align(alignment: Alignment.centerRight, child: _skipButton()),
          const SizedBox(height: 16),
          Text(
            'Cadastre seu negócio',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: t.ink,
              fontSize: 29,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            'Comece configurando os dados do seu negócio.',
            textAlign: TextAlign.center,
            style: TextStyle(color: t.muted, fontSize: 15),
          ),
          const SizedBox(height: 23),
          _initialField(
            key: const Key('onboarding-name-field'),
            label: 'Nome completo',
            controller: _name,
            focusNode: _nameFocus,
            icon: Icons.person_outline_rounded,
            textInputAction: TextInputAction.next,
            textCapitalization: TextCapitalization.words,
          ),
          const SizedBox(height: 16),
          _initialField(
            key: const Key('onboarding-phone-field'),
            label: 'Celular',
            controller: _phone,
            focusNode: _phoneFocus,
            icon: Icons.phone_rounded,
            keyboardType: TextInputType.phone,
            textInputAction: TextInputAction.next,
            inputFormatters: const [_PhoneInputFormatter()],
          ),
          const SizedBox(height: 16),
          _initialField(
            key: const Key('onboarding-email-field'),
            label: 'E-mail',
            controller: _email,
            focusNode: _emailFocus,
            icon: Icons.mail_outline_rounded,
            keyboardType: TextInputType.emailAddress,
            textInputAction: TextInputAction.next,
            readOnly: widget.controller.authenticatedEmail.trim().isNotEmpty,
          ),
          const SizedBox(height: 16),
          _initialField(
            key: const Key('onboarding-business-field'),
            label: 'Nome do negócio',
            controller: _business,
            focusNode: _businessFocus,
            icon: Icons.store_rounded,
            textCapitalization: TextCapitalization.words,
            textInputAction: TextInputAction.done,
            onSubmitted: (_) => _continue(),
          ),
          const SizedBox(height: 20),
          _businessLogoButton(width: double.infinity),
          const SizedBox(height: 12),
          _primaryButton(
            label: 'Continuar',
            width: double.infinity,
            onPressed: _continue,
          ),
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                Icons.lock_outline_rounded,
                color: t.muted.withValues(alpha: .62),
                size: 17,
              ),
              const SizedBox(width: 8),
              Flexible(
                child: Text(
                  'Seus dados estão seguros conosco.',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: t.muted.withValues(alpha: .72),
                    fontSize: 13,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _desktopInitialStep({required double scale}) {
    final t = _tokens;
    const typeScale = 1.0;
    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 610),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _desktopStepStrip(scale: typeScale),
          const SizedBox(height: 42),
          Text(
            'Cadastre seu negócio',
            style: TextStyle(
              color: t.ink,
              fontSize: 34,
              height: 1.12,
              fontWeight: FontWeight.w600,
              letterSpacing: .8,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            'Comece configurando os dados do seu negócio.',
            style: TextStyle(
              color: t.muted,
              fontSize: 15,
              height: 1.35,
              letterSpacing: .85,
            ),
          ),
          const SizedBox(height: 44),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: _initialField(
                  key: const Key('onboarding-name-field'),
                  label: 'Nome completo',
                  controller: _name,
                  focusNode: _nameFocus,
                  icon: Icons.person_outline_rounded,
                  textInputAction: TextInputAction.next,
                  textCapitalization: TextCapitalization.words,
                  height: 64,
                  plainIcon: true,
                  iconSize: 24,
                  iconSlotWidth: 50,
                  labelFontSize: 14,
                  labelGap: 8,
                ),
              ),
              const SizedBox(width: 50),
              Expanded(
                child: _initialField(
                  key: const Key('onboarding-phone-field'),
                  label: 'Celular',
                  controller: _phone,
                  focusNode: _phoneFocus,
                  icon: Icons.phone_outlined,
                  keyboardType: TextInputType.phone,
                  textInputAction: TextInputAction.next,
                  inputFormatters: const [_PhoneInputFormatter()],
                  height: 64,
                  plainIcon: true,
                  iconSize: 23,
                  iconSlotWidth: 50,
                  labelFontSize: 14,
                  labelGap: 8,
                ),
              ),
            ],
          ),
          const SizedBox(height: 22),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: _initialField(
                  key: const Key('onboarding-email-field'),
                  label: 'E-mail',
                  controller: _email,
                  focusNode: _emailFocus,
                  icon: Icons.mail_outline_rounded,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  readOnly: widget.controller.authenticatedEmail
                      .trim()
                      .isNotEmpty,
                  height: 64,
                  plainIcon: true,
                  iconSize: 24,
                  iconSlotWidth: 50,
                  labelFontSize: 14,
                  labelGap: 8,
                ),
              ),
              const SizedBox(width: 50),
              Expanded(
                child: _initialField(
                  key: const Key('onboarding-business-field'),
                  label: 'Nome do negócio',
                  controller: _business,
                  focusNode: _businessFocus,
                  icon: Icons.storefront_outlined,
                  textCapitalization: TextCapitalization.words,
                  textInputAction: TextInputAction.done,
                  onSubmitted: (_) => _continue(),
                  height: 64,
                  plainIcon: true,
                  iconSize: 24,
                  iconSlotWidth: 50,
                  labelFontSize: 14,
                  labelGap: 8,
                ),
              ),
            ],
          ),
          const SizedBox(height: 38),
          Row(
            children: [
              _businessLogoButton(width: 180),
              const SizedBox(width: 14),
              _primaryButton(
                label: 'Continuar',
                width: 416,
                height: 48,
                backgroundColor: t.accent,
                fontSize: 14,
                onPressed: _continue,
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _desktopStepStrip({double scale = 1}) {
    final t = _tokens;
    return Semantics(
      label: 'Etapa ${_step + 1} de 6',
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          for (var index = 0; index < 6; index++) ...[
            Semantics(
              button: true,
              selected: index == _step,
              label: 'Etapa ${index + 1} de 6',
              child: InkWell(
                key: Key('onboarding-step-${index + 1}'),
                onTap: _finishing ? null : () => _goToStep(index),
                borderRadius: BorderRadius.circular(7),
                child: SizedBox(
                  width: 30 * scale,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        '${index + 1}',
                        style: TextStyle(
                          color: index <= _step ? t.accentDark : t.muted,
                          fontSize: 15 * scale,
                          fontWeight: index == _step
                              ? FontWeight.w700
                              : FontWeight.w500,
                        ),
                      ),
                      SizedBox(height: 10 * scale),
                      AnimatedContainer(
                        duration: const Duration(milliseconds: 180),
                        width: index == _step ? 22 * scale : 0,
                        height: 3,
                        decoration: BoxDecoration(
                          color: t.accentDark,
                          borderRadius: BorderRadius.circular(99),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            if (index < 5) SizedBox(width: 28 * scale),
          ],
        ],
      ),
    );
  }

  Widget _initialField({
    required Key key,
    required String label,
    required TextEditingController controller,
    required FocusNode focusNode,
    required IconData icon,
    TextInputType? keyboardType,
    TextInputAction? textInputAction,
    TextCapitalization textCapitalization = TextCapitalization.none,
    List<TextInputFormatter>? inputFormatters,
    ValueChanged<String>? onSubmitted,
    double height = 54,
    bool plainIcon = false,
    double iconSize = 19,
    double iconSlotWidth = 54,
    double labelFontSize = 14,
    double labelGap = 7,
    bool readOnly = false,
  }) {
    final t = _tokens;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: TextStyle(
            color: const Color(0xFF334155),
            fontSize: labelFontSize,
            fontWeight: FontWeight.w600,
          ),
        ),
        SizedBox(height: labelGap),
        AnimatedBuilder(
          animation: focusNode,
          builder: (context, child) => AnimatedContainer(
            key: key,
            duration: const Duration(milliseconds: 160),
            height: height,
            decoration: BoxDecoration(
              color: t.panel,
              border: Border.all(
                color: focusNode.hasFocus ? t.accent : const Color(0xFFE9DED7),
                width: focusNode.hasFocus ? 1.4 : 1,
              ),
              borderRadius: BorderRadius.circular(12),
            ),
            child: child,
          ),
          child: Row(
            children: [
              SizedBox(
                width: iconSlotWidth,
                child: Center(
                  child: plainIcon
                      ? Icon(icon, size: iconSize, color: t.accentDark)
                      : _softBadge(icon, size: 34, iconSize: iconSize),
                ),
              ),
              Expanded(
                child: TextField(
                  controller: controller,
                  focusNode: focusNode,
                  keyboardType: keyboardType,
                  textInputAction: textInputAction,
                  textCapitalization: textCapitalization,
                  inputFormatters: inputFormatters,
                  onSubmitted: onSubmitted,
                  readOnly: readOnly,
                  style: TextStyle(color: t.ink, fontSize: 15),
                  decoration: const InputDecoration(
                    filled: false,
                    border: InputBorder.none,
                    enabledBorder: InputBorder.none,
                    focusedBorder: InputBorder.none,
                    contentPadding: EdgeInsets.fromLTRB(2, 15, 14, 14),
                    isDense: true,
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _businessLogoButton({required double width}) {
    final t = _tokens;
    final hasLogo = _logoBytes != null || _logoFileName.isNotEmpty;
    return SizedBox(
      key: const Key('onboarding-logo-button'),
      width: width,
      height: 48,
      child: OutlinedButton(
        onPressed: _pickingLogo || _finishing ? null : _pickBusinessLogo,
        style: OutlinedButton.styleFrom(
          foregroundColor: t.ink,
          backgroundColor: t.panel,
          padding: const EdgeInsets.symmetric(horizontal: 10),
          side: BorderSide(color: t.line),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
          textStyle: const TextStyle(
            fontFamily: 'Segoe UI',
            fontSize: 12.5,
            fontWeight: FontWeight.w600,
          ),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              key: const Key('onboarding-logo-preview'),
              width: 30,
              height: 30,
              decoration: BoxDecoration(
                color: t.accentSoft,
                border: Border.all(color: t.accent),
                borderRadius: BorderRadius.circular(8),
              ),
              clipBehavior: Clip.antiAlias,
              alignment: Alignment.center,
              child: _pickingLogo
                  ? SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: t.accentDark,
                      ),
                    )
                  : _logoBytes == null
                  ? Icon(
                      Icons.add_photo_alternate_outlined,
                      color: t.accentDark,
                      size: 17,
                    )
                  : Image.memory(
                      _logoBytes!,
                      width: 26,
                      height: 26,
                      fit: BoxFit.contain,
                      filterQuality: FilterQuality.high,
                      gaplessPlayback: true,
                    ),
            ),
            const SizedBox(width: 8),
            Flexible(
              child: Text(
                hasLogo ? 'Alterar logo' : 'Escolher logo',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _pickBusinessLogo() async {
    setState(() => _pickingLogo = true);
    try {
      final selected = widget.pickBusinessLogo == null
          ? await _pickBusinessLogoFromDevice()
          : await widget.pickBusinessLogo!();
      if (!mounted || selected == null) return;

      final extension = (selected.extension ?? '').toLowerCase();
      if (!const <String>{'png', 'jpg', 'jpeg', 'bmp'}.contains(extension)) {
        _message('Escolha uma imagem PNG, JPG, JPEG ou BMP.');
        return;
      }
      if (selected.size <= 0 || selected.bytes == null) {
        _message('O arquivo escolhido não contém uma imagem válida.');
        return;
      }
      if (selected.size > 25 * 1024 * 1024) {
        _message('A imagem escolhida deve ter no máximo 25 MB.');
        return;
      }

      final normalized = widget.normalizeBusinessLogo == null
          ? await _normalizeBusinessLogo(selected.bytes!)
          : await widget.normalizeBusinessLogo!(selected.bytes!);
      if (!mounted) return;
      setState(() {
        _logoBytes = normalized;
        _logoFileName = selected.name;
        _pendingLogoDataUrl =
            'data:image/png;base64,${base64Encode(normalized)}';
      });
      _message(
        'Logo selecionada. Ela aparecerá na página pública e nos relatórios.',
      );
    } on PlatformException catch (error) {
      if (mounted) {
        _message(
          'Não foi possível abrir a imagem: ${error.message ?? error.code}',
        );
      }
    } on FormatException catch (error) {
      if (mounted) _message(error.message);
    } catch (error) {
      if (mounted) _message('Não foi possível salvar a logo: $error');
    } finally {
      if (mounted) setState(() => _pickingLogo = false);
    }
  }

  Future<PlatformFile?> _pickBusinessLogoFromDevice() async {
    final result = await FilePicker.platform.pickFiles(
      dialogTitle: 'Escolher logo do estabelecimento',
      type: FileType.custom,
      allowedExtensions: const <String>['png', 'jpg', 'jpeg', 'bmp'],
      allowMultiple: false,
      withData: true,
    );
    return result?.files.singleOrNull;
  }

  Future<Uint8List> _normalizeBusinessLogo(Uint8List source) async {
    try {
      // dart:ui image codecs can throw an internal LateInitializationError in
      // optimized Flutter Web builds. A pure-Dart codec behaves consistently
      // on web, desktop and mobile while keeping the same validation/resize.
      final decodedImage = img.decodeImage(source);
      if (decodedImage == null) {
        throw const FormatException(
          'O arquivo escolhido não contém uma imagem válida.',
        );
      }
      final width = decodedImage.width;
      final height = decodedImage.height;
      if (width <= 0 || height <= 0) {
        throw const FormatException(
          'A imagem escolhida não possui dimensões válidas.',
        );
      }

      final longestSide = math.max(width, height);
      final scale = longestSide > 1200 ? 1200 / longestSide : 1.0;
      final normalizedImage = img.copyResize(
        decodedImage,
        width: math.max(1, (width * scale).round()),
        height: math.max(1, (height * scale).round()),
        interpolation: img.Interpolation.linear,
      );
      final png = img.encodePng(normalizedImage);
      if (png.isEmpty) {
        throw const FormatException('Não foi possível ler a imagem escolhida.');
      }
      return png;
    } catch (error) {
      if (error is FormatException) rethrow;
      throw const FormatException(
        'O arquivo escolhido não contém uma imagem válida.',
      );
    }
  }

  Widget _segmentStep({required bool desktop}) {
    final t = _tokens;
    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 610),
      child: Column(
        crossAxisAlignment: desktop
            ? CrossAxisAlignment.start
            : CrossAxisAlignment.center,
        children: [
          Text(
            'Escolha o seu segmento',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(
              color: t.ink,
              fontSize: desktop ? 34 : 24,
              height: 1.16,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            'Vamos adaptar serviços, recursos e a agenda à sua rotina.',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(color: t.muted, fontSize: 15),
          ),
          SizedBox(height: desktop ? 46 : 30),
          LayoutBuilder(
            builder: (context, constraints) {
              final columns = desktop
                  ? 4
                  : (constraints.maxWidth >= 500 ? 4 : 2);
              final gap = 12.0;
              final width =
                  (constraints.maxWidth - gap * (columns - 1)) / columns;
              return Wrap(
                spacing: gap,
                runSpacing: 12,
                children: [
                  for (final choice in _segments)
                    SizedBox(
                      width: width,
                      height: desktop ? 100 : 122,
                      child: _SelectionCard(
                        key: Key('onboarding-segment-${choice.slug}'),
                        selected: _segment == choice.label,
                        onTap: () => setState(() {
                          _segment = choice.label;
                          _themeId = '';
                          _showingThemeSelection = true;
                        }),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(
                              choice.icon,
                              color: _segment == choice.label
                                  ? t.accentDark
                                  : t.ink,
                              size: 26,
                            ),
                            const SizedBox(height: 6),
                            Text(
                              choice.label,
                              textAlign: TextAlign.center,
                              maxLines: 2,
                              style: TextStyle(
                                color: t.ink,
                                fontSize: 12.5,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                ],
              );
            },
          ),
          SizedBox(height: desktop ? 38 : 28),
          _primaryButton(
            label: 'Continuar',
            width: desktop ? 415 : double.infinity,
            onPressed: _segment.isEmpty
                ? null
                : () => setState(() => _showingThemeSelection = true),
          ),
        ],
      ),
    );
  }

  Widget _themeStep({required bool desktop}) {
    final t = _tokens;
    final themes = _themesForSegment(_segment);
    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 610),
      child: Column(
        crossAxisAlignment: desktop
            ? CrossAxisAlignment.start
            : CrossAxisAlignment.center,
        children: [
          Text(
            'Agora, escolha o seu estilo',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(
              color: t.ink,
              fontSize: desktop ? 34 : 24,
              height: 1.16,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            'Use o visual clássico ou aplique um tema pronto para o seu negócio.',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(color: t.muted, fontSize: 15),
          ),
          SizedBox(height: desktop ? 46 : 24),
          LayoutBuilder(
            builder: (context, constraints) {
              final columns = desktop ? 4 : 2;
              final outerPadding = desktop ? 6.0 : 0.0;
              final gap = desktop ? 12.0 : 12.0;
              final available = constraints.maxWidth - (outerPadding * 2);
              final width = (available - gap * (columns - 1)) / columns;
              final horizontalCards = width >= 260;
              return Padding(
                padding: EdgeInsets.symmetric(horizontal: outerPadding),
                child: Wrap(
                  spacing: gap,
                  runSpacing: 12,
                  alignment: desktop
                      ? WrapAlignment.start
                      : WrapAlignment.center,
                  children: [
                    for (final theme in themes)
                      SizedBox(
                        key: Key(
                          'onboarding-theme-${theme.id.isEmpty ? 'default' : theme.id}',
                        ),
                        width: width,
                        height: desktop ? 146 : 142,
                        child: _ThemeCard(
                          theme: theme,
                          description: _themeDescription(theme.id),
                          selected: _themeId == theme.id,
                          horizontal: horizontalCards,
                          onTap: () => setState(() => _themeId = theme.id),
                        ),
                      ),
                  ],
                ),
              );
            },
          ),
          SizedBox(height: desktop ? 18 : 24),
          if (desktop)
            Row(
              children: [
                _primaryButton(
                  label: 'Continuar',
                  width: 415,
                  onPressed: _continue,
                ),
                const Spacer(),
                SizedBox(
                  width: 150,
                  height: 48,
                  child: OutlinedButton(
                    key: const Key('onboarding-theme-skip'),
                    onPressed: _finishing ? null : _skipTheme,
                    child: const Text('Pular tema'),
                  ),
                ),
              ],
            )
          else
            Wrap(
              spacing: 12,
              runSpacing: 12,
              alignment: WrapAlignment.center,
              children: [
                _primaryButton(
                  label: 'Continuar',
                  width: 180,
                  height: 44,
                  onPressed: _continue,
                ),
                SizedBox(
                  width: 150,
                  height: 44,
                  child: OutlinedButton(
                    key: const Key('onboarding-theme-skip'),
                    onPressed: _finishing ? null : _skipTheme,
                    child: const Text('Pular tema'),
                  ),
                ),
              ],
            ),
        ],
      ),
    );
  }

  Widget _teamStep({required bool desktop}) {
    final t = _tokens;
    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 610),
      child: Column(
        crossAxisAlignment: desktop
            ? CrossAxisAlignment.start
            : CrossAxisAlignment.center,
        children: [
          Text(
            'Quantas pessoas atendem?',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(
              color: t.ink,
              fontSize: desktop ? 34 : 24,
              height: 1.16,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            'Vamos preparar uma agenda que funcione para o tamanho da sua equipe.',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(color: t.muted, fontSize: 15),
          ),
          SizedBox(height: desktop ? 70 : 38),
          LayoutBuilder(
            builder: (context, constraints) {
              final columns = desktop
                  ? 5
                  : (constraints.maxWidth >= 520 ? 3 : 1);
              final gap = 12.0;
              final width =
                  (constraints.maxWidth - gap * (columns - 1)) / columns;
              return Wrap(
                spacing: gap,
                runSpacing: 12,
                children: [
                  for (final choice in _teamChoices)
                    SizedBox(
                      width: width,
                      height: desktop ? 100 : 80,
                      child: _SelectionCard(
                        key: Key('onboarding-team-${choice.slug}'),
                        selected: _teamSize == choice.label,
                        onTap: () => setState(() => _teamSize = choice.label),
                        verticalPadding: 4,
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Container(
                              width: 42,
                              height: 42,
                              alignment: Alignment.center,
                              decoration: BoxDecoration(
                                color: t.accentSoft,
                                shape: BoxShape.circle,
                              ),
                              child: Text(
                                choice.badge,
                                style: TextStyle(
                                  color: t.accentDark,
                                  fontSize: 16,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                            const SizedBox(height: 8),
                            Text(
                              choice.caption,
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                color: t.ink,
                                fontSize: 13,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                ],
              );
            },
          ),
          SizedBox(height: desktop ? 38 : 24),
          Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.info_outline_rounded, color: t.accent, size: 18),
              const SizedBox(width: 9),
              Flexible(
                child: Text(
                  'Você poderá ajustar o tamanho da equipe depois.',
                  style: TextStyle(color: t.muted, fontSize: 14),
                ),
              ),
            ],
          ),
          SizedBox(height: desktop ? 66 : 30),
          _primaryButton(
            label: 'Continuar',
            width: desktop ? 415 : double.infinity,
            onPressed: _teamSize.isEmpty ? null : _continue,
          ),
        ],
      ),
    );
  }

  Widget _objectiveStep({required bool desktop}) {
    final t = _tokens;
    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 610),
      child: Column(
        crossAxisAlignment: desktop
            ? CrossAxisAlignment.start
            : CrossAxisAlignment.center,
        children: [
          Text(
            'O que você quer conquistar?',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(
              color: t.ink,
              fontSize: desktop ? 34 : 24,
              height: 1.16,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            'Escolha o objetivo mais importante agora. Você poderá mudar depois.',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(color: t.muted, fontSize: 15),
          ),
          SizedBox(height: desktop ? 62 : 26),
          LayoutBuilder(
            builder: (context, constraints) {
              final columns = desktop || constraints.maxWidth >= 640 ? 2 : 1;
              final gap = 10.0;
              final width =
                  (constraints.maxWidth - gap * (columns - 1)) / columns;
              return Wrap(
                spacing: gap,
                runSpacing: 10,
                children: [
                  for (final choice in _objectiveChoices)
                    SizedBox(
                      width: width,
                      height: 52,
                      child: _SelectionCard(
                        key: Key('onboarding-objective-${choice.slug}'),
                        selected: _objective == choice.label,
                        onTap: () => setState(() => _objective = choice.label),
                        horizontalPadding: 18,
                        child: Row(
                          children: [
                            SizedBox(
                              width: 34,
                              child: Icon(
                                choice.icon,
                                color: t.accent,
                                size: 23,
                              ),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Text(
                                choice.displayLabel,
                                maxLines: 2,
                                style: TextStyle(
                                  color: t.ink,
                                  fontSize: 14,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                ],
              );
            },
          ),
          SizedBox(height: desktop ? 62 : 24),
          _primaryButton(
            label: 'Continuar',
            width: desktop ? 415 : double.infinity,
            onPressed: _objective.isEmpty ? null : _continue,
          ),
        ],
      ),
    );
  }

  Widget _addressStep({required bool desktop}) {
    final t = _tokens;
    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 610),
      child: Column(
        crossAxisAlignment: desktop
            ? CrossAxisAlignment.start
            : CrossAxisAlignment.center,
        children: [
          Text(
            'Onde fica o seu negócio?',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(
              color: t.ink,
              fontSize: desktop ? 34 : 24,
              height: 1.16,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            'O endereço ajuda a organizar a operação e orientar seus clientes.',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(color: t.muted, fontSize: 15),
          ),
          SizedBox(height: desktop ? 20 : 28),
          _addressField(
            key: const Key('onboarding-cep-field'),
            controller: _cep,
            hint: 'CEP',
            keyboardType: TextInputType.number,
            inputFormatters: const [_CepInputFormatter()],
            suffix: _lookingUpCep
                ? const SizedBox(
                    width: 17,
                    height: 17,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : null,
            onChanged: _onCepChanged,
          ),
          const SizedBox(height: 15),
          LayoutBuilder(
            builder: (context, constraints) {
              if (!desktop && constraints.maxWidth < 520) {
                return Column(
                  children: [
                    _addressField(
                      key: const Key('onboarding-neighborhood-field'),
                      controller: _neighborhood,
                      hint: 'Bairro',
                      warm: true,
                    ),
                    const SizedBox(height: 15),
                    _addressField(
                      key: const Key('onboarding-street-field'),
                      controller: _street,
                      hint: 'Logradouro',
                      warm: true,
                    ),
                    const SizedBox(height: 15),
                    _addressField(
                      key: const Key('onboarding-number-field'),
                      controller: _number,
                      hint: 'Número',
                    ),
                    const SizedBox(height: 15),
                    _addressField(
                      key: const Key('onboarding-complement-field'),
                      controller: _complement,
                      hint: 'Complemento',
                    ),
                  ],
                );
              }
              return Column(
                children: [
                  Row(
                    children: [
                      SizedBox(
                        width: 230,
                        child: _addressField(
                          key: const Key('onboarding-neighborhood-field'),
                          controller: _neighborhood,
                          hint: 'Bairro',
                          warm: true,
                        ),
                      ),
                      const SizedBox(width: 20),
                      Expanded(
                        child: _addressField(
                          key: const Key('onboarding-street-field'),
                          controller: _street,
                          hint: 'Logradouro',
                          warm: true,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 15),
                  Row(
                    children: [
                      SizedBox(
                        width: 190,
                        child: _addressField(
                          key: const Key('onboarding-number-field'),
                          controller: _number,
                          hint: 'Número',
                        ),
                      ),
                      const SizedBox(width: 20),
                      Expanded(
                        child: _addressField(
                          key: const Key('onboarding-complement-field'),
                          controller: _complement,
                          hint: 'Complemento',
                        ),
                      ),
                    ],
                  ),
                ],
              );
            },
          ),
          const SizedBox(height: 14),
          _primaryButton(
            label: 'Continuar',
            width: desktop ? 415 : double.infinity,
            onPressed: _continue,
          ),
        ],
      ),
    );
  }

  Widget _addressField({
    required Key key,
    required TextEditingController controller,
    required String hint,
    bool warm = false,
    TextInputType? keyboardType,
    List<TextInputFormatter>? inputFormatters,
    Widget? suffix,
    ValueChanged<String>? onChanged,
  }) {
    final t = _tokens;
    final icon = switch (hint) {
      'CEP' => Icons.location_on_outlined,
      'Bairro' => Icons.home_outlined,
      'Logradouro' => Icons.add_road_rounded,
      'Número' => Icons.numbers_rounded,
      _ => Icons.short_text_rounded,
    };
    return Column(
      key: key,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          hint,
          style: TextStyle(
            color: t.ink,
            fontSize: 13,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 6),
        Container(
          height: 54,
          decoration: BoxDecoration(
            color: warm ? t.warmSoft : t.panel,
            border: Border.all(color: t.line),
            borderRadius: BorderRadius.circular(10),
          ),
          child: TextField(
            controller: controller,
            keyboardType: keyboardType,
            inputFormatters: inputFormatters,
            onChanged: onChanged,
            style: TextStyle(color: t.ink, fontSize: 15),
            decoration: InputDecoration(
              prefixIcon: Icon(icon, color: t.accent, size: 20),
              filled: false,
              border: InputBorder.none,
              enabledBorder: InputBorder.none,
              focusedBorder: InputBorder.none,
              isDense: true,
              contentPadding: const EdgeInsets.fromLTRB(0, 18, 14, 15),
              suffixIcon: suffix == null
                  ? null
                  : Padding(padding: const EdgeInsets.all(16), child: suffix),
            ),
          ),
        ),
      ],
    );
  }

  Widget _reviewStep({required bool desktop}) {
    final t = _tokens;
    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 610),
      child: Column(
        crossAxisAlignment: desktop
            ? CrossAxisAlignment.start
            : CrossAxisAlignment.center,
        children: [
          Text(
            'Revise e conclua',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(
              color: t.ink,
              fontSize: desktop ? 34 : 24,
              height: 1.16,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            'Confira os dados principais antes de entrar no Agenda Livre.',
            textAlign: desktop ? TextAlign.left : TextAlign.center,
            style: TextStyle(color: t.muted, fontSize: 15),
          ),
          SizedBox(height: desktop ? 36 : 24),
          Container(
            key: const Key('onboarding-review-panel'),
            padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 8),
            decoration: BoxDecoration(
              color: t.panel,
              border: Border.all(color: t.line),
              borderRadius: BorderRadius.circular(12),
              boxShadow: const [
                BoxShadow(
                  color: Color(0x0F000000),
                  blurRadius: 14,
                  offset: Offset(0, 4),
                ),
              ],
            ),
            child: Column(
              children: [
                _reviewRow('Negócio', _business.text.trim()),
                _reviewDivider(),
                _reviewRow('Segmento', _segment),
                _reviewDivider(),
                _reviewRow(
                  'Equipe e objetivo',
                  '${_teamReviewLabel()} | ${_objectiveReviewLabel()}',
                ),
                _reviewDivider(),
                _reviewRow('Endereço', _formattedAddress()),
              ],
            ),
          ),
          const SizedBox(height: 28),
          _primaryButton(
            label: 'Concluir configuração',
            width: desktop ? 415 : double.infinity,
            onPressed: _finishing ? null : _continue,
            loading: _finishing,
          ),
        ],
      ),
    );
  }

  Widget _reviewRow(String label, String value) {
    final t = _tokens;
    final icon = switch (label) {
      'Negócio' => Icons.storefront_outlined,
      'Segmento' => Icons.category_outlined,
      'Equipe e objetivo' => Icons.groups_outlined,
      _ => Icons.location_on_outlined,
    };
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _softBadge(icon, size: 30, iconSize: 17),
          const SizedBox(width: 12),
          SizedBox(
            width: 124,
            child: Text(
              label,
              style: const TextStyle(color: Color(0xFF64748B), fontSize: 13),
            ),
          ),
          Expanded(
            child: Text(
              value.trim().isEmpty ? 'Não informado' : value,
              style: TextStyle(
                color: t.ink,
                fontSize: 13.5,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _reviewDivider() => const Divider(height: 1, color: Color(0xFFF1E7DE));

  Widget _softBadge(
    IconData icon, {
    double size = 34,
    double iconSize = 19,
    Color? iconColor,
  }) {
    final t = _tokens;
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: t.accentSoft,
        borderRadius: BorderRadius.circular(9),
      ),
      alignment: Alignment.center,
      child: Icon(icon, color: iconColor ?? t.accent, size: iconSize),
    );
  }

  Widget _primaryButton({
    required String label,
    required double width,
    required VoidCallback? onPressed,
    double height = 48,
    bool loading = false,
    Color? backgroundColor,
    double fontSize = 16,
  }) {
    final t = _tokens;
    return SizedBox(
      key: const Key('onboarding-primary'),
      width: width,
      height: height,
      child: ElevatedButton(
        onPressed: onPressed,
        style: ElevatedButton.styleFrom(
          elevation: 0,
          backgroundColor: backgroundColor ?? t.accentDark,
          foregroundColor: Colors.white,
          disabledBackgroundColor: t.accentSoft,
          disabledForegroundColor: t.muted,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
          textStyle: TextStyle(
            fontFamily: 'Segoe UI',
            fontSize: fontSize,
            fontWeight: FontWeight.w600,
          ),
        ),
        child: loading
            ? const SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(
                  strokeWidth: 2,
                  color: Colors.white,
                ),
              )
            : Text(label),
      ),
    );
  }

  List<AgendaThemeSpec> _themesForSegment(String segment) {
    final prefix = switch (segment) {
      'Salão de Beleza' => 'salon-',
      'Barbearia' => 'barber-',
      'Clínica médica' => 'medical-',
      'Petshop' => 'pet-',
      'Oficina' => 'workshop-',
      'Podologia' => 'podology-',
      'Spa' => 'spa-',
      _ => 'aesthetic-',
    };
    return [
      AgendaThemes.byId(''),
      ...AgendaThemes.all.where((theme) => theme.id.startsWith(prefix)).take(3),
    ];
  }

  String _normalizedTeamSize(String value) => switch (value.trim()) {
    '5 a 10 profissionais' || '5-10 profissionais' => '5 a 9 profissionais',
    '11 a 30 profissionais' ||
    'Mais de 30 profissionais' ||
    '10+ profissionais' => '10 ou mais profissionais',
    _ => value.trim(),
  };

  String _normalizedObjective(String value) => switch (value.trim()) {
    'Gerenciar a parte fiscal' ||
    'Facilitar pagamentos da equipe' => 'Administrar financeiro',
    'Gerenciar agendas das unidades' ||
    'Acompanhar todas as unidades' => 'Organizar agenda',
    _ => value.trim(),
  };

  String _teamReviewLabel() {
    for (final choice in _teamChoices) {
      if (choice.label == _teamSize) {
        return choice.label == '1 profissional'
            ? choice.caption
            : '${choice.badge} pessoas';
      }
    }
    return _teamSize;
  }

  String _objectiveReviewLabel() {
    for (final choice in _objectiveChoices) {
      if (choice.label == _objective) return choice.displayLabel;
    }
    return _objective;
  }

  void _skipTheme() {
    setState(() {
      _themeId = '';
      _showingThemeSelection = false;
      _step = 2;
    });
  }

  String _themeDescription(String id) => switch (id) {
    '' => 'Laranjinha original.',
    'salon-classic-gold' => 'Claro e elegante.',
    'salon-lilac-glow' => 'Delicado e moderno.',
    'salon-rose-luxe' => 'Rosé sofisticado.',
    'barber-midnight' => 'Forte e direto.',
    'barber-emerald' => 'Elegante e profissional.',
    'barber-navy' => 'Limpo e sofisticado.',
    'medical-teal' => 'Cuidado limpo e moderno.',
    'medical-green' => 'Saúde leve e acolhedora.',
    'medical-blue' => 'Clínico e tecnológico.',
    'pet-coral' => 'Quente e divertido.',
    'pet-lilac' => 'Fofo e delicado.',
    'pet-teal' => 'Vivo e organizado.',
    'workshop-gold' => 'Forte e premium.',
    'workshop-olive' => 'Robusto e organizado.',
    'workshop-graphite' => 'Direto e técnico.',
    'aesthetic-lavender' => 'Suave e relaxante.',
    'aesthetic-sage' => 'Natural e acolhedor.',
    'aesthetic-coral' => 'Leve e sofisticado.',
    'podology-terracotta' => 'Acolhedor e profissional.',
    'podology-mint' => 'Leve e natural.',
    'podology-blue' => 'Limpo e clínico.',
    'spa-aqua' => 'Azul leve e relaxante.',
    'spa-sand' => 'Natural e acolhedor.',
    'spa-forest' => 'Verde orgânico e calmo.',
    _ => 'Identidade pronta para o negócio.',
  };

  void _back() {
    setState(() {
      if (_step == 1 && _showingThemeSelection) {
        _showingThemeSelection = false;
        return;
      }
      if (_step > 0) {
        _step--;
        _showingThemeSelection = false;
      }
    });
  }

  Future<void> _goToStep(int target) async {
    final next = target.clamp(0, 5);
    if (next == _step) return;

    if (next > _step) {
      if (_step == 0) {
        if (!_validateInitialData()) return;
        _commitPendingLogo();
      }
      if (next >= 2) _ensureSegmentDefault();
      if (next >= 3) _ensureTeamDefault();
      if (next >= 4) _ensureObjectiveDefault();
    }

    if (!mounted) return;
    setState(() {
      _step = next;
      _showingThemeSelection = false;
    });
  }

  void _skip() {
    setState(() {
      switch (_step) {
        case 0:
          _ensureInitialDefaults();
          _step = 1;
        case 1:
          _ensureSegmentDefault();
          _showingThemeSelection = false;
          _step = 2;
        case 2:
          _ensureTeamDefault();
          _step = 3;
        case 3:
          _ensureObjectiveDefault();
          _step = 4;
        case 4:
          _step = 5;
        case 5:
          break;
      }
    });
  }

  void _ensureInitialDefaults() {
    final settings = widget.controller.data.settings;
    if (_name.text.trim().isEmpty) _name.text = settings.accountFullName.trim();
    if (_phone.text.trim().isEmpty) {
      _phone.text = settings.accountPhone.trim().isNotEmpty
          ? settings.accountPhone.trim()
          : settings.businessPhone.trim();
    }
    if (!_looksLikeEmail(_email.text.trim())) {
      _email.text = _looksLikeEmail(widget.controller.accountEmail)
          ? widget.controller.accountEmail
          : '';
    }
    if (_business.text.trim().isEmpty ||
        _isDefaultBusinessName(_business.text)) {
      _business.text = wpfDefaultBusinessNameForSegment(
        _segment.isEmpty ? 'Salão de Beleza' : _segment,
      );
    }
  }

  void _ensureSegmentDefault() {
    if (_segment.isEmpty) _segment = 'Salão de Beleza';
    if (!_themesForSegment(_segment).any((theme) => theme.id == _themeId)) {
      _themeId = '';
    }
  }

  void _ensureTeamDefault() {
    if (_teamSize.isEmpty) _teamSize = '1 profissional';
  }

  void _ensureObjectiveDefault() {
    if (_objective.isEmpty) _objective = 'Organizar agenda';
  }

  void _commitPendingLogo() {
    if (_pendingLogoDataUrl.isNotEmpty) {
      widget.controller.data.settings.businessLogoPath = _pendingLogoDataUrl;
    }
  }

  Future<void> _continue() async {
    switch (_step) {
      case 0:
        if (!_validateInitialData()) return;
        _commitPendingLogo();
        setState(() {
          _step = 1;
          _showingThemeSelection = false;
        });
        return;
      case 1:
        if (_segment.isEmpty) {
          _message('Escolha o segmento do seu negócio antes de continuar.');
          return;
        }
        if (!_showingThemeSelection) {
          setState(() => _showingThemeSelection = true);
          return;
        }
        setState(() {
          _showingThemeSelection = false;
          _step = 2;
        });
        return;
      case 2:
        if (_teamSize.isEmpty) return;
        setState(() => _step = 3);
        return;
      case 3:
        if (_objective.isEmpty) return;
        setState(() => _step = 4);
        return;
      case 4:
        if (_cep.text.trim().isEmpty && _street.text.trim().isEmpty) {
          _message('Informe pelo menos o CEP ou o logradouro do negócio.');
          return;
        }
        setState(() => _step = 5);
        return;
      case 5:
        await _finish();
    }
  }

  bool _validateInitialData() {
    final fullName = _name.text.trim();
    if (fullName.isEmpty) {
      _message('Informe o nome completo antes de continuar.');
      _nameFocus.requestFocus();
      return false;
    }
    if (_phone.text.trim().isEmpty) {
      _message('Informe o celular antes de continuar.');
      _phoneFocus.requestFocus();
      return false;
    }
    final phoneDigits = _phone.text.replaceAll(RegExp(r'\D'), '');
    if (phoneDigits.length != 10 && phoneDigits.length != 11) {
      _message('Informe telefone com DDD e 10 ou 11 dígitos.');
      _phoneFocus.requestFocus();
      return false;
    }
    if (!_looksLikeEmail(_email.text.trim())) {
      _message('Informe um e-mail válido antes de continuar.');
      _emailFocus.requestFocus();
      return false;
    }
    if (_business.text.trim().isEmpty) {
      _message('Informe o nome do negócio antes de continuar.');
      _businessFocus.requestFocus();
      return false;
    }
    _name.text = _toNameCase(fullName);
    _phone.text = _formatPhone(phoneDigits);
    _business.text = _toNameCase(_business.text);
    return true;
  }

  bool _looksLikeEmail(String value) =>
      RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(value);

  String _toNameCase(String value) {
    const lowerWords = <String>{'da', 'das', 'de', 'do', 'dos', 'e'};
    final words = value.trim().split(RegExp(r'\s+'));
    return <String>[
      for (var index = 0; index < words.length; index++)
        if (words[index].isNotEmpty)
          index > 0 && lowerWords.contains(words[index].toLowerCase())
              ? words[index].toLowerCase()
              : '${words[index][0].toUpperCase()}'
                    '${words[index].substring(1).toLowerCase()}',
    ].join(' ');
  }

  String _formatPhone(String digits) => digits.length == 10
      ? '(${digits.substring(0, 2)}) ${digits.substring(2, 6)}-'
            '${digits.substring(6)}'
      : '(${digits.substring(0, 2)}) ${digits.substring(2, 7)}-'
            '${digits.substring(7)}';

  bool _isDefaultBusinessName(String value) {
    final normalized = value.trim().toLowerCase();
    return normalized.isEmpty ||
        normalized == 'agenda livre' ||
        normalized == 'balcão livre';
  }

  Future<void> _finish() async {
    _ensureInitialDefaults();
    _ensureSegmentDefault();
    _ensureTeamDefault();
    _ensureObjectiveDefault();
    _commitPendingLogo();
    applyWpfOnboardingTemplate(
      widget.controller.data,
      segment: _segment,
      teamSize: _teamSize,
    );
    setState(() => _finishing = true);
    try {
      await widget.controller.completeOnboarding(
        accountName: _name.text,
        phone: _phone.text,
        email: _email.text,
        businessName: _business.text,
        segment: _segment,
        themeId: _themeId,
        teamSize: _teamSize,
        objective: _objective,
        postalCode: _cep.text,
        neighborhood: _neighborhood.text,
        street: _street.text,
        number: _number.text,
        complement: _complement.text,
      );
    } catch (error) {
      if (mounted) _message('Não foi possível concluir a configuração: $error');
    } finally {
      if (mounted) setState(() => _finishing = false);
    }
  }

  void _onCepChanged(String value) {
    final digits = value.replaceAll(RegExp(r'\D'), '');
    if (digits.length < 8) _lastLookupCep = '';
    if (digits.length == 8 && digits != _lastLookupCep) {
      _lastLookupCep = digits;
      _lookupCep();
    }
  }

  Future<void> _lookupCep() async {
    if (_lookingUpCep) return;
    setState(() => _lookingUpCep = true);
    try {
      final address = await _viaCep.lookup(_cep.text);
      if (!mounted) return;
      if (address == null) {
        _message('CEP não encontrado. Confira os números informados.');
        return;
      }
      setState(() {
        _cep.text = address.formattedCep;
        _street.text = address.street;
        _neighborhood.text = address.neighborhood;
        if (_complement.text.isEmpty) _complement.text = address.complement;
      });
    } on ViaCepException catch (error) {
      if (mounted) _message(error.message);
    } finally {
      if (mounted) setState(() => _lookingUpCep = false);
    }
  }

  String _formattedAddress() {
    final streetAndNumber = [
      _street.text.trim(),
      _number.text.trim(),
    ].where((item) => item.isNotEmpty).join(', ');
    return [
      streetAndNumber,
      _neighborhood.text.trim(),
      _complement.text.trim(),
      if (_cep.text.trim().isNotEmpty) 'CEP ${_cep.text.trim()}',
    ].where((item) => item.isNotEmpty).join(' | ');
  }

  void _message(String value) {
    final messenger = ScaffoldMessenger.maybeOf(context);
    if (messenger == null) return;
    final viewWidth = MediaQuery.sizeOf(context).width;
    final desktop = viewWidth >= 700;
    final centeredMargin = math.max(16.0, (viewWidth - 356) / 2);
    messenger
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          behavior: SnackBarBehavior.floating,
          backgroundColor: _tokens.accent,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
          margin: desktop
              ? EdgeInsets.fromLTRB(
                  centeredMargin - 8,
                  0,
                  centeredMargin + 8,
                  69,
                )
              : const EdgeInsets.fromLTRB(16, 0, 16, 16),
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
          content: Row(
            children: [
              const Icon(
                Icons.info_outline_rounded,
                color: Colors.white,
                size: 19,
              ),
              const SizedBox(width: 10),
              Expanded(
                child: desktop
                    ? FittedBox(
                        fit: BoxFit.scaleDown,
                        alignment: Alignment.centerLeft,
                        child: Text(
                          value,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 14,
                          ),
                        ),
                      )
                    : Text(
                        value,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 14,
                        ),
                      ),
              ),
            ],
          ),
        ),
      );
  }
}

class _SelectionCard extends StatelessWidget {
  const _SelectionCard({
    super.key,
    required this.selected,
    required this.onTap,
    required this.child,
    this.horizontalPadding = 12,
    this.verticalPadding = 12,
  });

  final bool selected;
  final VoidCallback onTap;
  final Widget child;
  final double horizontalPadding;
  final double verticalPadding;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Semantics(
      selected: selected,
      button: true,
      child: Material(
        color: selected ? t.accentSoft : t.panel,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: BorderSide(
            color: selected ? t.accentDark : t.line,
            width: selected ? 2 : 1,
          ),
        ),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onTap,
          hoverColor: t.accentSoft,
          child: Padding(
            padding: EdgeInsets.symmetric(
              horizontal: selected
                  ? math.max(0, horizontalPadding - 1)
                  : horizontalPadding,
              vertical: selected
                  ? math.max(0, verticalPadding - 1)
                  : verticalPadding,
            ),
            child: child,
          ),
        ),
      ),
    );
  }
}

class _ThemeCard extends StatelessWidget {
  const _ThemeCard({
    required this.theme,
    required this.description,
    required this.selected,
    required this.horizontal,
    required this.onTap,
  });

  final AgendaThemeSpec theme;
  final String description;
  final bool selected;
  final bool horizontal;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Semantics(
      selected: selected,
      button: true,
      child: Material(
        color: t.panel,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: BorderSide(
            color: selected ? t.accent : t.line,
            width: selected ? 2 : 1,
          ),
        ),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onTap,
          hoverColor: t.accentSoft,
          child: horizontal
              ? _horizontalContent(context)
              : _verticalContent(context),
        ),
      ),
    );
  }

  Widget _horizontalContent(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.all(10),
      child: Row(
        children: [
          SizedBox(
            width: 122,
            height: double.infinity,
            child: ClipRRect(
              borderRadius: BorderRadius.circular(9),
              child: Image.asset(
                theme.previewAsset,
                fit: BoxFit.cover,
                filterQuality: FilterQuality.high,
                errorBuilder: (context, error, stackTrace) =>
                    _ThemeMiniPreview(theme: theme),
              ),
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        theme.id.isEmpty ? 'Modo padrão' : theme.name,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: selected ? t.accentDark : t.ink,
                          fontSize: 13.5,
                          height: 1.15,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                    if (selected) ...[
                      const SizedBox(width: 4),
                      Container(
                        width: 20,
                        height: 20,
                        decoration: BoxDecoration(
                          color: t.accent,
                          shape: BoxShape.circle,
                        ),
                        alignment: Alignment.center,
                        child: Icon(
                          Icons.check_rounded,
                          size: 13,
                          color: Theme.of(context).colorScheme.onPrimary,
                        ),
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: 3),
                Text(
                  description,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 11.5, height: 1.2),
                ),
                const Spacer(),
                _themeSwatches(theme),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _verticalContent(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: const EdgeInsets.all(7),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            height: 66,
            width: double.infinity,
            child: ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: Image.asset(
                theme.previewAsset,
                fit: BoxFit.cover,
                filterQuality: FilterQuality.medium,
                errorBuilder: (context, error, stackTrace) =>
                    _ThemeMiniPreview(theme: theme),
              ),
            ),
          ),
          const SizedBox(height: 8),
          Text(
            theme.id.isEmpty ? 'Modo padrão' : theme.name,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              color: selected ? t.accentDark : t.ink,
              fontSize: 12.5,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            description,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(color: t.muted, fontSize: 10.5),
          ),
        ],
      ),
    );
  }

  Widget _themeSwatches(AgendaThemeSpec value) {
    final colors = <Color>[
      value.tokens.accent,
      value.tokens.accentSoft,
      value.tokens.warmSoft,
      value.tokens.panel,
    ];
    return Row(
      children: [
        for (final color in colors) ...[
          Container(
            width: 18,
            height: 18,
            decoration: BoxDecoration(
              color: color,
              shape: BoxShape.circle,
              border: Border.all(color: value.tokens.line),
            ),
          ),
          if (color != colors.last) const SizedBox(width: 6),
        ],
      ],
    );
  }
}

class _ThemeMiniPreview extends StatelessWidget {
  const _ThemeMiniPreview({required this.theme});

  final AgendaThemeSpec theme;

  @override
  Widget build(BuildContext context) {
    final c = theme.tokens;
    return ClipRRect(
      borderRadius: BorderRadius.circular(13),
      child: ColoredBox(
        color: c.appBackground,
        child: Row(
          children: [
            Container(
              width: 31,
              color: c.sidebarBackground,
              padding: const EdgeInsets.fromLTRB(5, 10, 5, 8),
              child: Column(
                children: [
                  Container(
                    height: 8,
                    decoration: BoxDecoration(
                      color: c.accent,
                      borderRadius: BorderRadius.circular(3),
                    ),
                  ),
                  const SizedBox(height: 8),
                  Container(
                    height: 5,
                    decoration: BoxDecoration(
                      color: c.sidebarActive,
                      borderRadius: BorderRadius.circular(3),
                    ),
                  ),
                  const SizedBox(height: 5),
                  Container(
                    height: 5,
                    decoration: BoxDecoration(
                      color: c.line,
                      borderRadius: BorderRadius.circular(3),
                    ),
                  ),
                ],
              ),
            ),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.all(7),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(width: 54, height: 7, color: c.ink),
                    const SizedBox(height: 7),
                    Expanded(
                      child: Row(
                        children: [
                          Expanded(
                            child: Container(
                              decoration: BoxDecoration(
                                color: c.panel,
                                border: Border.all(color: c.line),
                                borderRadius: BorderRadius.circular(4),
                              ),
                            ),
                          ),
                          const SizedBox(width: 5),
                          Expanded(
                            child: Container(
                              decoration: BoxDecoration(
                                color: c.accentSoft,
                                borderRadius: BorderRadius.circular(4),
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
          ],
        ),
      ),
    );
  }
}

class _SegmentChoice {
  const _SegmentChoice(this.label, this.icon, this.slug);
  final String label;
  final IconData icon;
  final String slug;
}

class _TextChoice {
  const _TextChoice(this.label, this.badge, this.caption, this.slug);
  final String label;
  final String badge;
  final String caption;
  final String slug;
}

class _ObjectiveChoice {
  const _ObjectiveChoice(this.label, this.displayLabel, this.icon, this.slug);
  final String label;
  final String displayLabel;
  final IconData icon;
  final String slug;
}

class _PhoneInputFormatter extends TextInputFormatter {
  const _PhoneInputFormatter();

  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    final digits = newValue.text.replaceAll(RegExp(r'\D'), '');
    final limited = digits.substring(0, math.min(11, digits.length));
    final buffer = StringBuffer();
    if (limited.isNotEmpty) {
      buffer.write('(');
      buffer.write(limited.substring(0, math.min(2, limited.length)));
      if (limited.length >= 2) buffer.write(') ');
    }
    if (limited.length > 2) {
      final body = limited.substring(2);
      final firstLength = limited.length == 11 ? 5 : math.min(4, body.length);
      buffer.write(body.substring(0, math.min(firstLength, body.length)));
      if (body.length > firstLength) {
        buffer.write('-');
        buffer.write(body.substring(firstLength));
      }
    }
    final formatted = buffer.toString();
    return TextEditingValue(
      text: formatted,
      selection: TextSelection.collapsed(offset: formatted.length),
    );
  }
}

class _CepInputFormatter extends TextInputFormatter {
  const _CepInputFormatter();

  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    final digits = newValue.text.replaceAll(RegExp(r'\D'), '');
    final limited = digits.substring(0, math.min(8, digits.length));
    final formatted = limited.length <= 5
        ? limited
        : '${limited.substring(0, 5)}-${limited.substring(5)}';
    return TextEditingValue(
      text: formatted,
      selection: TextSelection.collapsed(offset: formatted.length),
    );
  }
}
