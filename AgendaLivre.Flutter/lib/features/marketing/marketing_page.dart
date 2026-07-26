import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../domain/models/models.dart';
import '../../services/whatsapp_service.dart';
import 'marketing_wpf_studio.dart';

const _defaultPromotionName = 'Promoção da semana';
const _defaultPromotionOffer = '20% de desconto em serviços selecionados';
const _defaultPromotionMessage =
    'Oi, {nome}! Tudo bem? Aqui é da {empresa}. A {promocao} está ativa: '
    '{oferta}. Quer que eu veja um horário para você?';

class MarketingPage extends StatefulWidget {
  const MarketingPage({super.key, required this.controller});

  final AgendaController controller;

  @override
  State<MarketingPage> createState() => _MarketingPageState();
}

class _MarketingPageState extends State<MarketingPage> {
  final _promotionName = TextEditingController(text: _defaultPromotionName);
  final _promotionOffer = TextEditingController(text: _defaultPromotionOffer);
  final _promotionMessage = TextEditingController(
    text: _defaultPromotionMessage,
  );
  final WhatsAppService _whatsApp = const WhatsAppService();

  String _lastAppliedPromotionName = '';
  String _lastAppliedPromotionOffer = '';

  AgendaController get controller => widget.controller;

  @override
  void initState() {
    super.initState();
    _promotionName.addListener(_refreshMarketing);
    _promotionOffer.addListener(_refreshMarketing);
    _promotionMessage.addListener(_refreshMarketing);
  }

  @override
  void dispose() {
    _promotionName.removeListener(_refreshMarketing);
    _promotionOffer.removeListener(_refreshMarketing);
    _promotionMessage.removeListener(_refreshMarketing);
    _promotionName.dispose();
    _promotionOffer.dispose();
    _promotionMessage.dispose();
    super.dispose();
  }

  void _refreshMarketing() {
    if (mounted) setState(() {});
  }

  DateTime get _today => DateUtils.dateOnly(DateTime.now());

  String get _businessDisplayName {
    final value = controller.businessName.trim();
    final normalized = value.toLowerCase();
    if (value.isEmpty ||
        normalized == 'agenda livre' ||
        normalized == 'balcão livre' ||
        normalized == 'balcao livre') {
      return 'Balcão Livre';
    }
    return value;
  }

  String get _promotionDisplayName => _promotionName.text.trim().isEmpty
      ? 'Oferta da semana'
      : _promotionName.text.trim();

  String get _promotionDisplayOffer => _promotionOffer.text.trim().isEmpty
      ? 'Configure a oferta da campanha ativa.'
      : _promotionOffer.text.trim();

  String get _previewCustomerName {
    final customers = controller.data.customers;
    return customers.isEmpty || customers.first.name.trim().isEmpty
        ? 'Cliente'
        : customers.first.name.trim();
  }

  String get _previewMessage => _buildMarketingMessage(_previewCustomerName);

  int get _staleCustomerCount => controller.data.customers.where((customer) {
    final lastSeen = DateUtils.dateOnly(customer.lastSeenAt);
    return !lastSeen.isAfter(_today.subtract(const Duration(days: 30)));
  }).length;

  int get _noShowCount => controller.data.appointments.where((appointment) {
    return appointment.status == AppointmentStatus.noShow &&
        !DateUtils.dateOnly(
          appointment.start,
        ).isBefore(_today.subtract(const Duration(days: 60)));
  }).length;

  int get _pendingConfirmationCount =>
      controller.data.appointments.where((appointment) {
        return appointment.status == AppointmentStatus.scheduled &&
            !DateUtils.dateOnly(appointment.start).isBefore(_today);
      }).length;

  List<_MarketingContactRow> get _marketingContacts {
    final rows = <_MarketingContactRow>[];
    final seen = <String>{};

    final scheduled =
        controller.data.appointments
            .where(
              (appointment) =>
                  appointment.status == AppointmentStatus.scheduled &&
                  !DateUtils.dateOnly(appointment.start).isBefore(_today),
            )
            .toList()
          ..sort((a, b) => a.start.compareTo(b.start));
    for (final appointment in scheduled) {
      _addMarketingContact(
        rows,
        seen,
        name: appointment.customerName,
        phone: appointment.customerPhone,
        reason:
            'Confirmar horário de ${_twoDigits(appointment.start.day)}/'
            '${_twoDigits(appointment.start.month)} '
            '${_twoDigits(appointment.start.hour)}:'
            '${_twoDigits(appointment.start.minute)}',
        context: appointment.serviceName,
        badge: 'Confirmação',
      );
    }

    final noShows =
        controller.data.appointments
            .where(
              (appointment) =>
                  appointment.status == AppointmentStatus.noShow &&
                  !DateUtils.dateOnly(
                    appointment.start,
                  ).isBefore(_today.subtract(const Duration(days: 60))),
            )
            .toList()
          ..sort((a, b) => b.start.compareTo(a.start));
    for (final appointment in noShows) {
      _addMarketingContact(
        rows,
        seen,
        name: appointment.customerName,
        phone: appointment.customerPhone,
        reason:
            'Faltou em ${_twoDigits(appointment.start.day)}/'
            '${_twoDigits(appointment.start.month)}',
        context: appointment.serviceName,
        badge: 'Retorno',
      );
    }

    final staleCustomers = controller.data.customers.where((customer) {
      final lastSeen = DateUtils.dateOnly(customer.lastSeenAt);
      return !lastSeen.isAfter(_today.subtract(const Duration(days: 30)));
    }).toList()..sort((a, b) => a.lastSeenAt.compareTo(b.lastSeenAt));
    for (final customer in staleCustomers) {
      _addMarketingContact(
        rows,
        seen,
        name: customer.name,
        phone: customer.phone,
        reason:
            'Último atendimento em ${_twoDigits(customer.lastSeenAt.day)}/'
            '${_twoDigits(customer.lastSeenAt.month)}',
        context: customer.profile,
        badge: 'Sem retorno',
      );
    }

    return rows.take(12).toList(growable: false);
  }

  void _addMarketingContact(
    List<_MarketingContactRow> rows,
    Set<String> seen, {
    required String name,
    required String phone,
    required String reason,
    required String context,
    required String badge,
  }) {
    final cleanName = name.trim();
    final cleanPhone = phone.trim();
    if (cleanName.isEmpty) return;

    final key = cleanPhone.isEmpty
        ? cleanName.toLowerCase()
        : '${cleanName.toLowerCase()}|${_onlyDigits(cleanPhone)}';
    if (!seen.add(key)) return;

    rows.add(
      _MarketingContactRow(
        name: cleanName,
        detail: [
          reason,
          if (cleanPhone.isNotEmpty) cleanPhone,
          if (context.trim().isNotEmpty) context.trim(),
        ].join(' | '),
        phone: cleanPhone,
        badge: badge,
        messagePreview: _buildMarketingMessage(cleanName),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, _) {
        final contacts = _marketingContacts;
        return MarketingWpfStudio(
          businessName: _businessDisplayName,
          titleController: _promotionName,
          copyController: _promotionMessage,
          previewMessage: _previewMessage,
          publicationCount: controller.data.appointments.length,
          clientCount: controller.data.customers.length,
          contactQueue: _ContactsPanel(
            contacts: contacts,
            onRefresh: _refreshContacts,
            onOpen: _openMarketingWhatsApp,
          ),
          onUpdate: _updatePromotion,
          onCopy: _copyMarketingMessage,
          onWhatsApp: _openFirstMarketingWhatsApp,
          onInstagram: _openInstagram,
        );
      },
    );
  }

  // Kept temporarily as the legacy campaign layout while the WPF studio is
  // rolled out; its small presentation widgets are still useful references.
  // ignore: unused_element
  Widget _buildHero(bool desktop) {
    final t = AgendaThemeTokens.of(context);
    final heading = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'AGENDA LIVRE',
              style: TextStyle(
                color: t.accent,
                fontSize: 10,
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(width: 10),
            Container(width: 28, height: 1, color: t.accent),
          ],
        ),
        const SizedBox(height: 5),
        Text(
          'Marketing',
          style: TextStyle(
            color: t.ink,
            fontSize: 28,
            height: 1.05,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 3),
        Text(
          'Crie mensagens, fale pelo WhatsApp e prepare publicações para o Instagram.',
          style: TextStyle(color: t.muted, fontSize: 13),
        ),
        const SizedBox(height: 7),
        Text(
          _businessDisplayName,
          style: TextStyle(
            color: t.accent,
            fontSize: 14,
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );

    final action = SizedBox(
      width: 154,
      height: 42,
      child: ElevatedButton(
        key: const Key('marketing-new-promotion'),
        onPressed: _updatePromotion,
        style: _primaryButtonStyle(t, minimumSize: const Size(154, 42)),
        child: _buttonContent(
          Icon(Icons.campaign, size: 18, color: Colors.white),
          'Nova promoção',
          gap: 8,
        ),
      ),
    );

    return _MarketingSurface(
      key: const Key('marketing-hero'),
      radius: 24,
      minHeight: 140,
      padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 17),
      clip: true,
      child: Stack(
        children: [
          Positioned(
            right: -2,
            top: -6,
            width: 300,
            height: 104,
            child: IgnorePointer(
              child: Opacity(opacity: .045, child: const _MarketingWatermark()),
            ),
          ),
          ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 92),
            child: desktop
                ? Row(
                    children: [
                      Expanded(child: heading),
                      const SizedBox(width: 20),
                      action,
                    ],
                  )
                : Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      heading,
                      const SizedBox(height: 15),
                      Align(alignment: Alignment.centerLeft, child: action),
                    ],
                  ),
          ),
        ],
      ),
    );
  }

  // ignore: unused_element
  Widget _buildWorkspace(bool desktop, List<_MarketingContactRow> contacts) {
    final activeCampaign = _ActiveCampaignPanel(
      desktop: desktop,
      promotionName: _promotionName,
      promotionOffer: _promotionOffer,
      promotionMessage: _promotionMessage,
      previewMessage: _previewMessage,
      onUpdate: _updatePromotion,
      onCopy: _copyMarketingMessage,
      onWhatsApp: _openFirstMarketingWhatsApp,
      onInstagram: _openInstagram,
    );
    final contactsPanel = _ContactsPanel(
      contacts: contacts,
      onRefresh: _refreshContacts,
      onOpen: _openMarketingWhatsApp,
    );
    final readyMessages = _ReadyMessagesPanel(onCopy: _copyMarketingMessage);
    final campaigns = _CampaignsPanel(
      staleCustomers: _staleCustomerCount,
      noShows: _noShowCount,
      pendingConfirmations: _pendingConfirmationCount,
      promotionName: _promotionDisplayName,
      promotionOffer: _promotionDisplayOffer,
      onOpen: _openCampaign,
    );

    if (!desktop) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          activeCampaign,
          const SizedBox(height: 10),
          contactsPanel,
          const SizedBox(height: 10),
          readyMessages,
          const SizedBox(height: 10),
          campaigns,
        ],
      );
    }

    return Column(
      children: [
        Table(
          columnWidths: const {
            0: FlexColumnWidth(130),
            1: FixedColumnWidth(10),
            2: FlexColumnWidth(95),
          },
          defaultVerticalAlignment: TableCellVerticalAlignment.top,
          children: [
            TableRow(
              children: [
                contacts.isEmpty
                    ? activeCampaign
                    : TableCell(
                        verticalAlignment: TableCellVerticalAlignment.fill,
                        child: activeCampaign,
                      ),
                const SizedBox.shrink(),
                contacts.isEmpty
                    ? TableCell(
                        verticalAlignment: TableCellVerticalAlignment.fill,
                        child: contactsPanel,
                      )
                    : contactsPanel,
              ],
            ),
          ],
        ),
        const SizedBox(height: 10),
        Table(
          columnWidths: const {
            0: FlexColumnWidth(130),
            1: FixedColumnWidth(10),
            2: FlexColumnWidth(95),
          },
          defaultVerticalAlignment: TableCellVerticalAlignment.top,
          children: [
            TableRow(
              children: [
                readyMessages,
                const SizedBox.shrink(),
                TableCell(
                  verticalAlignment: TableCellVerticalAlignment.fill,
                  child: campaigns,
                ),
              ],
            ),
          ],
        ),
      ],
    );
  }

  void _ensureDefaultPromotionMessage() {
    if (_promotionMessage.text.trim().isEmpty) {
      _promotionMessage.text = _defaultPromotionMessage;
    }
  }

  String _buildMarketingMessage(String customerName) {
    final offer = _promotionOffer.text.trim().isEmpty
        ? 'uma condição especial'
        : _promotionOffer.text.trim();
    final promotionName = _promotionName.text.trim().isEmpty
        ? 'promoção'
        : _promotionName.text.trim();
    final template = _promotionMessage.text.trim().isEmpty
        ? 'Oi, {nome}! Aqui é da {empresa}. Temos {oferta}. Quer reservar um horário?'
        : _promotionMessage.text.trim();

    return _replaceToken(
      _replaceToken(
        _replaceToken(
          _replaceToken(template, '{nome}', customerName),
          '{empresa}',
          _businessDisplayName,
        ),
        '{oferta}',
        offer,
      ),
      '{promocao}',
      promotionName,
    );
  }

  void _applyPromotionToMessageEditor() {
    _ensureDefaultPromotionMessage();
    final promotionName = _promotionName.text.trim().isEmpty
        ? 'promoção'
        : _promotionName.text.trim();
    final promotionOffer = _promotionOffer.text.trim().isEmpty
        ? 'uma condição especial'
        : _promotionOffer.text.trim();
    var message = _promotionMessage.text.trim();
    final containsPromotionToken = RegExp(
      RegExp.escape('{promocao}'),
      caseSensitive: false,
    ).hasMatch(message);
    final containsOfferToken = RegExp(
      RegExp.escape('{oferta}'),
      caseSensitive: false,
    ).hasMatch(message);

    if (containsPromotionToken || containsOfferToken) {
      message = _replaceToken(message, '{promocao}', promotionName);
      message = _replaceToken(message, '{oferta}', promotionOffer);
    } else {
      if (_lastAppliedPromotionName.isNotEmpty) {
        message = _replaceToken(
          message,
          _lastAppliedPromotionName,
          promotionName,
        );
      }
      if (_lastAppliedPromotionOffer.isNotEmpty) {
        message = _replaceToken(
          message,
          _lastAppliedPromotionOffer,
          promotionOffer,
        );
      }
    }

    _promotionMessage.text = message;
    _promotionMessage.selection = TextSelection.collapsed(
      offset: _promotionMessage.text.length,
    );
    _lastAppliedPromotionName = promotionName;
    _lastAppliedPromotionOffer = promotionOffer;
  }

  void _updatePromotion() {
    _applyPromotionToMessageEditor();
    final name = _promotionName.text.trim().isEmpty
        ? 'Promoção'
        : _promotionName.text.trim();
    _showMessage('Prévia atualizada: $name.');
  }

  Future<void> _copyMarketingMessage() async {
    _ensureDefaultPromotionMessage();
    await Clipboard.setData(ClipboardData(text: _previewMessage));
    if (!mounted) return;
    _showMessage('Mensagem de marketing copiada para a área de transferência.');
  }

  Future<void> _openFirstMarketingWhatsApp() async {
    for (final row in _marketingContacts) {
      if (row.phone.trim().isNotEmpty) {
        await _openMarketingWhatsApp(row);
        return;
      }
    }
    _showMessage('Nenhum cliente com telefone disponível para WhatsApp.');
  }

  Future<void> _openMarketingWhatsApp(_MarketingContactRow row) async {
    if (row.phone.trim().isEmpty) {
      _showMessage('Telefone não cadastrado para ${row.name}.');
      return;
    }

    final message = _buildMarketingMessage(row.name);
    try {
      final result = await _whatsApp.sendText(
        phone: row.phone,
        message: message,
      );
      var opened = result.sent;
      if (!result.sent) {
        opened = await launchUrl(
          result.fallbackUri,
          mode: LaunchMode.platformDefault,
        );
      }
      if (!mounted) return;
      if (!opened) {
        _showMessage('Não foi possível abrir o WhatsApp para ${row.name}.');
        return;
      }

      final normalizedPhone = WhatsAppService.normalizePhone(row.phone);
      await controller.addWhatsAppMessage(
        WhatsAppMessage(
          provider: result.sent ? 'evolution' : 'wa.me',
          customerName: row.name,
          phone: normalizedPhone,
          message: message,
          direction: 'saida',
          status: result.sent ? 'enviado' : 'aberto',
          category: 'Marketing',
          sentAt: result.sent ? DateTime.now() : null,
        ),
      );
      if (!mounted) return;
      _showMessage(
        result.sent
            ? 'WhatsApp enviado para ${row.name} pelo canal linkado.'
            : 'WhatsApp aberto para ${row.name}. A conversa ficou no painel.',
      );
    } on WhatsAppValidationException {
      _showMessage('Telefone inválido para ${row.name}.');
    }
  }

  Future<void> _openInstagram() async {
    final caption = _previewMessage;
    await Clipboard.setData(ClipboardData(text: caption));
    final username = controller.data.settings.instagramUsername
        .trim()
        .replaceFirst(RegExp(r'^@'), '');
    final uri = username.isEmpty
        ? Uri.parse('https://www.instagram.com/')
        : Uri.parse(
            'https://www.instagram.com/${Uri.encodeComponent(username)}/',
          );
    final opened = await launchUrl(uri, mode: LaunchMode.platformDefault);
    if (!mounted) return;
    _showMessage(
      opened
          ? 'Texto da campanha copiado. O Instagram foi aberto para você publicar.'
          : 'Texto copiado, mas não foi possível abrir o Instagram.',
    );
  }

  Future<void> _openCampaign(_MarketingCampaignRow campaign) async {
    if (campaign.badge == 'Promoção') {
      _ensureDefaultPromotionMessage();
      await Clipboard.setData(ClipboardData(text: _previewMessage));
      if (!mounted) return;
      _showMessage('${campaign.name} copiada para a área de transferência.');
      return;
    }

    final badge = switch (campaign.name) {
      'Volta para agenda' => 'Sem retorno',
      'Confirmar horários' => 'Confirmação',
      'Recuperar faltas' => 'Retorno',
      _ => '',
    };
    for (final contact in _marketingContacts) {
      if (contact.badge == badge && contact.phone.trim().isNotEmpty) {
        await _openMarketingWhatsApp(contact);
        return;
      }
    }
    _showMessage('Nenhum contato disponível para a campanha ${campaign.name}.');
  }

  void _refreshContacts() {
    setState(() {});
    _showMessage('Marketing atualizado com os dados mais recentes.');
  }

  void _showMessage(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }
}

class _ActiveCampaignPanel extends StatelessWidget {
  const _ActiveCampaignPanel({
    required this.desktop,
    required this.promotionName,
    required this.promotionOffer,
    required this.promotionMessage,
    required this.previewMessage,
    required this.onUpdate,
    required this.onCopy,
    required this.onWhatsApp,
    required this.onInstagram,
  });

  final bool desktop;
  final TextEditingController promotionName;
  final TextEditingController promotionOffer;
  final TextEditingController promotionMessage;
  final String previewMessage;
  final VoidCallback onUpdate;
  final VoidCallback onCopy;
  final VoidCallback onWhatsApp;
  final VoidCallback onInstagram;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return _MarketingSurface(
      key: const Key('marketing-active-campaign'),
      radius: 16,
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          LayoutBuilder(
            builder: (context, constraints) {
              final heading = Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Campanha ativa',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 19,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    'Monte uma oferta curta e veja como ela vai chegar no cliente.',
                    maxLines: desktop ? 1 : 2,
                    softWrap: !desktop,
                    overflow: desktop
                        ? TextOverflow.clip
                        : TextOverflow.ellipsis,
                    style: TextStyle(color: t.muted, fontSize: 12),
                  ),
                ],
              );
              final channel = const _ChannelPill();
              if (constraints.maxWidth < 430) {
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [heading, const SizedBox(height: 10), channel],
                );
              }
              return Row(
                children: [
                  Expanded(child: heading),
                  const SizedBox(width: 10),
                  channel,
                ],
              );
            },
          ),
          const SizedBox(height: 12),
          if (desktop)
            IntrinsicHeight(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(
                    flex: 105,
                    child: _CampaignEditor(
                      promotionName: promotionName,
                      promotionOffer: promotionOffer,
                      promotionMessage: promotionMessage,
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    flex: 95,
                    child: _MessagePreview(message: previewMessage),
                  ),
                ],
              ),
            )
          else
            Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _CampaignEditor(
                  promotionName: promotionName,
                  promotionOffer: promotionOffer,
                  promotionMessage: promotionMessage,
                ),
                const SizedBox(height: 14),
                _MessagePreview(message: previewMessage),
              ],
            ),
          const SizedBox(height: 14),
          _CampaignActions(
            desktop: desktop,
            onUpdate: onUpdate,
            onCopy: onCopy,
            onWhatsApp: onWhatsApp,
            onInstagram: onInstagram,
          ),
        ],
      ),
    );
  }
}

class _ChannelPill extends StatelessWidget {
  const _ChannelPill();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF2E8),
        borderRadius: BorderRadius.circular(16),
      ),
      child: const Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          FaIcon(FontAwesomeIcons.whatsapp, color: Color(0xFF16A34A), size: 15),
          SizedBox(width: 6),
          Flexible(
            child: Text(
              'WhatsApp + Instagram manual',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                color: Color(0xFF9A4A15),
                fontSize: 11,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _CampaignEditor extends StatelessWidget {
  const _CampaignEditor({
    required this.promotionName,
    required this.promotionOffer,
    required this.promotionMessage,
  });

  final TextEditingController promotionName;
  final TextEditingController promotionOffer;
  final TextEditingController promotionMessage;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _MarketingTextField(
          fieldKey: const Key('marketing-promotion-name'),
          label: 'Nome da promoção',
          hint: 'Ex.: Promoção da semana',
          controller: promotionName,
          height: 42,
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 12,
            vertical: 2,
          ),
        ),
        const SizedBox(height: 12),
        _MarketingTextField(
          fieldKey: const Key('marketing-promotion-offer'),
          label: 'Oferta',
          hint: 'Ex.: 20% de desconto',
          controller: promotionOffer,
          height: 50,
          maxLines: 2,
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 12,
            vertical: 6,
          ),
        ),
        const SizedBox(height: 12),
        _MarketingTextField(
          fieldKey: const Key('marketing-promotion-message'),
          label: 'Texto da mensagem',
          hint: 'Escreva a mensagem que será enviada ao cliente',
          controller: promotionMessage,
          height: 116,
          expands: true,
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 12,
            vertical: 10,
          ),
        ),
      ],
    );
  }
}

class _MarketingTextField extends StatefulWidget {
  const _MarketingTextField({
    required this.fieldKey,
    required this.label,
    required this.hint,
    required this.controller,
    required this.height,
    required this.contentPadding,
    this.maxLines = 1,
    this.expands = false,
  });

  final Key fieldKey;
  final String label;
  final String hint;
  final TextEditingController controller;
  final double height;
  final EdgeInsets contentPadding;
  final int maxLines;
  final bool expands;

  @override
  State<_MarketingTextField> createState() => _MarketingTextFieldState();
}

class _MarketingTextFieldState extends State<_MarketingTextField> {
  late final FocusNode _focusNode;
  bool _selectOnFocus = true;

  @override
  void initState() {
    super.initState();
    _focusNode = FocusNode()..addListener(_handleFocus);
  }

  @override
  void dispose() {
    _focusNode
      ..removeListener(_handleFocus)
      ..dispose();
    super.dispose();
  }

  void _handleFocus() {
    if (mounted) setState(() {});
    if (!_focusNode.hasFocus) {
      _selectOnFocus = true;
      return;
    }
    if (!_selectOnFocus) return;
    _selectOnFocus = false;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted || !_focusNode.hasFocus) return;
      widget.controller.selection = TextSelection(
        baseOffset: 0,
        extentOffset: widget.controller.text.length,
      );
    });
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          widget.label,
          style: TextStyle(
            color: t.muted,
            fontSize: 12,
            height: 1.25,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 6),
        Container(
          height: widget.height,
          clipBehavior: Clip.antiAlias,
          decoration: BoxDecoration(
            color: t.panel,
            border: Border.all(color: _focusNode.hasFocus ? t.accent : t.line),
            borderRadius: BorderRadius.circular(16),
          ),
          child: TextField(
            key: widget.fieldKey,
            controller: widget.controller,
            focusNode: _focusNode,
            maxLines: widget.expands ? null : widget.maxLines,
            minLines: widget.expands ? null : 1,
            expands: widget.expands,
            textAlignVertical: widget.expands
                ? TextAlignVertical.top
                : TextAlignVertical.center,
            style: TextStyle(color: t.ink, fontSize: 13.5, height: 1.25),
            decoration: InputDecoration(
              filled: false,
              isDense: true,
              hintText: widget.hint,
              hintStyle: TextStyle(color: t.muted, fontSize: 13.5),
              contentPadding: widget.contentPadding,
              border: InputBorder.none,
              enabledBorder: InputBorder.none,
              focusedBorder: InputBorder.none,
            ),
          ),
        ),
      ],
    );
  }
}

class _MessagePreview extends StatelessWidget {
  const _MessagePreview({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      key: const Key('marketing-message-preview'),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: t.warmSoft,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 30,
                height: 30,
                alignment: Alignment.center,
                decoration: const BoxDecoration(
                  color: Color(0xFFDCFCE7),
                  shape: BoxShape.circle,
                ),
                child: const FaIcon(
                  FontAwesomeIcons.whatsapp,
                  color: Color(0xFF16A34A),
                  size: 17,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Prévia da mensagem',
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 13.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      'Use {nome}, {empresa}, {promocao} e {oferta}',
                      style: TextStyle(
                        color: t.muted,
                        fontSize: 10.5,
                        height: 14 / 10.5,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Container(
            constraints: const BoxConstraints(minHeight: 122),
            padding: const EdgeInsets.all(13),
            decoration: BoxDecoration(
              color: const Color(0xFFDCFCE7),
              border: Border.all(color: const Color(0xFFBBF7D0)),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  message.isEmpty ? 'Mensagem de prévia' : message,
                  style: TextStyle(color: t.ink, fontSize: 13, height: 1.3),
                ),
                const SizedBox(height: 8),
                const Align(
                  alignment: Alignment.centerRight,
                  child: Text(
                    '11:30 ✓✓',
                    style: TextStyle(color: Color(0xFF166534), fontSize: 10.5),
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

class _CampaignActions extends StatelessWidget {
  const _CampaignActions({
    required this.desktop,
    required this.onUpdate,
    required this.onCopy,
    required this.onWhatsApp,
    required this.onInstagram,
  });

  final bool desktop;
  final VoidCallback onUpdate;
  final VoidCallback onCopy;
  final VoidCallback onWhatsApp;
  final VoidCallback onInstagram;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final buttons = <Widget>[
      ElevatedButton(
        key: const Key('marketing-update-promotion'),
        onPressed: onUpdate,
        style: _primaryButtonStyle(t),
        child: _buttonContent(
          const Icon(Icons.campaign, color: Colors.white, size: 17),
          'Atualizar',
        ),
      ),
      OutlinedButton(
        key: const Key('marketing-copy-message'),
        onPressed: onCopy,
        style: _ghostButtonStyle(t),
        child: _buttonContent(
          Icon(Icons.copy_outlined, color: t.ink, size: 17),
          'Copiar',
        ),
      ),
      OutlinedButton(
        key: const Key('marketing-open-whatsapp'),
        onPressed: onWhatsApp,
        style: _ghostButtonStyle(t),
        child: _buttonContent(
          const FaIcon(
            FontAwesomeIcons.whatsapp,
            color: Color(0xFF16A34A),
            size: 17,
          ),
          'WhatsApp',
        ),
      ),
      OutlinedButton(
        key: const Key('marketing-open-instagram'),
        onPressed: onInstagram,
        style: _ghostButtonStyle(t),
        child: _buttonContent(
          const FaIcon(
            FontAwesomeIcons.instagram,
            color: Color(0xFFC13584),
            size: 17,
          ),
          'Instagram',
        ),
      ),
    ];

    if (desktop) {
      return Row(
        children: [
          for (var index = 0; index < buttons.length; index++) ...[
            if (index > 0) const SizedBox(width: 10),
            Expanded(child: SizedBox(height: 40, child: buttons[index])),
          ],
        ],
      );
    }

    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = constraints.maxWidth >= 500 ? 2 : 1;
        const gap = 8.0;
        final width = (constraints.maxWidth - gap * (columns - 1)) / columns;
        return Wrap(
          spacing: gap,
          runSpacing: gap,
          children: [
            for (final button in buttons)
              SizedBox(width: width, height: 40, child: button),
          ],
        );
      },
    );
  }
}

class _ContactsPanel extends StatelessWidget {
  const _ContactsPanel({
    required this.contacts,
    required this.onRefresh,
    required this.onOpen,
  });

  final List<_MarketingContactRow> contacts;
  final VoidCallback onRefresh;
  final ValueChanged<_MarketingContactRow> onOpen;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return _MarketingSurface(
      key: const Key('marketing-contacts-panel'),
      radius: 16,
      padding: const EdgeInsets.all(16),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final header = Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Fila de contatos',
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 19,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    FittedBox(
                      fit: BoxFit.scaleDown,
                      alignment: Alignment.centerLeft,
                      child: Text(
                        'Clientes com mais chance de responder hoje.',
                        maxLines: 1,
                        softWrap: false,
                        style: TextStyle(color: t.muted, fontSize: 12),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 10),
              SizedBox(
                height: 40,
                child: OutlinedButton(
                  key: const Key('marketing-refresh-contacts'),
                  onPressed: onRefresh,
                  style: _subtleButtonStyle(t, minimumSize: const Size(82, 40)),
                  child: const Text('Atualizar'),
                ),
              ),
            ],
          );
          final list = contacts.isEmpty
              ? const _MarketingEmptyContact()
              : ScrollConfiguration(
                  behavior: ScrollConfiguration.of(
                    context,
                  ).copyWith(scrollbars: false),
                  child: ListView.builder(
                    padding: EdgeInsets.zero,
                    primary: false,
                    shrinkWrap: true,
                    itemCount: contacts.length,
                    itemBuilder: (context, index) => _MarketingContactCard(
                      contact: contacts[index],
                      onOpen: onOpen,
                    ),
                  ),
                );

          final boundedList = ConstrainedBox(
            constraints: const BoxConstraints(maxHeight: 410),
            child: list,
          );

          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              header,
              const SizedBox(height: 10),
              if (constraints.hasBoundedHeight)
                Flexible(fit: FlexFit.loose, child: boundedList)
              else
                boundedList,
            ],
          );
        },
      ),
    );
  }
}

class _MarketingContactCard extends StatelessWidget {
  const _MarketingContactCard({required this.contact, required this.onOpen});

  final _MarketingContactRow contact;
  final ValueChanged<_MarketingContactRow> onOpen;

  @override
  Widget build(BuildContext context) {
    if (contact.phone.trim().isEmpty) {
      return _MarketingEmptyContact(contact: contact);
    }

    final t = AgendaThemeTokens.of(context);
    final background = t.accentSoft;
    final foreground = t.accent;
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 42,
            child: Container(
              width: 34,
              height: 34,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: background,
                borderRadius: BorderRadius.circular(14),
              ),
              child: FaIcon(
                FontAwesomeIcons.whatsapp,
                color: foreground,
                size: 18,
              ),
            ),
          ),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(8, 0, 12, 0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Text(
                          contact.name,
                          style: TextStyle(
                            color: t.ink,
                            fontSize: 14.5,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      _DataBadge(
                        label: contact.badge,
                        background: background,
                        foreground: foreground,
                        radius: 16,
                      ),
                    ],
                  ),
                  const SizedBox(height: 3),
                  Text(
                    contact.detail,
                    style: TextStyle(color: t.muted, fontSize: 12),
                  ),
                  const SizedBox(height: 8),
                  Container(
                    padding: const EdgeInsets.all(9),
                    decoration: BoxDecoration(
                      color: t.panel,
                      border: Border.all(color: t.line),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      contact.messagePreview,
                      style: TextStyle(color: t.ink, fontSize: 12),
                    ),
                  ),
                ],
              ),
            ),
          ),
          SizedBox(
            height: 40,
            child: OutlinedButton(
              onPressed: contact.phone.isEmpty ? null : () => onOpen(contact),
              style: _subtleButtonStyle(
                t,
                minimumSize: const Size(72, 40),
                padding: const EdgeInsets.symmetric(horizontal: 10),
              ),
              child: const Text('Abrir'),
            ),
          ),
        ],
      ),
    );
  }
}

class _MarketingEmptyContact extends StatelessWidget {
  const _MarketingEmptyContact({this.contact});

  final _MarketingContactRow? contact;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      key: Key(
        contact == null
            ? 'marketing-empty-contact'
            : 'marketing-contact-empty-${contact!.name}',
      ),
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(18, 14, 18, 4),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 88,
              height: 88,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: t.warmSoft,
                shape: BoxShape.circle,
              ),
              child: Icon(Icons.person_outline, color: t.muted, size: 42),
            ),
            const SizedBox(height: 20),
            Wrap(
              alignment: WrapAlignment.center,
              crossAxisAlignment: WrapCrossAlignment.center,
              spacing: 12,
              runSpacing: 6,
              children: [
                Text(
                  contact?.name ?? 'Nenhum cliente prioritário',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                _DataBadge(
                  label: contact?.badge ?? 'Sem ação',
                  background: contact == null ? t.graySoft : t.yellowSoft,
                  foreground: contact == null ? t.muted : t.ink,
                  radius: 13,
                  horizontalPadding: 9,
                ),
              ],
            ),
            const SizedBox(height: 12),
            ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 330),
              child: Text(
                contact?.detail ??
                    'Clientes sem retorno, faltas ou confirmações pendentes aparecerão aqui.',
                textAlign: TextAlign.center,
                style: TextStyle(color: t.muted, fontSize: 12),
              ),
            ),
            const SizedBox(height: 14),
            ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 360),
              child: Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(
                  horizontal: 13,
                  vertical: 11,
                ),
                decoration: BoxDecoration(
                  color: t.accentSoft,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  contact?.messagePreview ??
                      'Cadastre clientes com telefone para abrir WhatsApp.',
                  maxLines: contact == null ? 1 : null,
                  softWrap: contact != null,
                  overflow: contact == null
                      ? TextOverflow.clip
                      : TextOverflow.visible,
                  style: TextStyle(
                    color: t.accent,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ReadyMessagesPanel extends StatelessWidget {
  const _ReadyMessagesPanel({required this.onCopy});

  final VoidCallback onCopy;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final messages = <_ReadyMessageRow>[
      _ReadyMessageRow(
        name: 'Promoção',
        detail: 'Texto para divulgar oferta, desconto ou pacote especial.',
        badge: 'oferta',
        icon: Icons.campaign,
        background: t.warmSoft,
        foreground: t.accent,
      ),
      _ReadyMessageRow(
        name: 'Confirmação',
        detail: 'Mensagem curta para confirmar presença antes do horário.',
        badge: 'agenda',
        icon: Icons.calendar_month_outlined,
        background: t.accentSoft,
        foreground: t.accent,
      ),
      const _ReadyMessageRow(
        name: 'Pós-atendimento',
        detail: 'Agradeça o cliente e incentive retorno ou avaliação.',
        badge: 'retorno',
        icon: Icons.check_circle_outline,
        background: Color(0xFFDCFCE7),
        foreground: Color(0xFF16A34A),
      ),
      _ReadyMessageRow(
        name: 'Cliente sumido',
        detail: 'Convide clientes sem atendimento recente para voltar.',
        badge: '30 dias',
        icon: Icons.person_outline,
        background: t.yellowSoft,
        foreground: t.ink,
      ),
    ];

    return _MarketingSurface(
      key: const Key('marketing-ready-messages'),
      radius: 16,
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _PanelHeading(
            title: 'Mensagens prontas',
            subtitle: 'Modelos objetivos para copiar, adaptar e enviar.',
          ),
          const SizedBox(height: 12),
          for (final message in messages)
            _ReadyMessageTile(message: message, onCopy: onCopy),
        ],
      ),
    );
  }
}

class _ReadyMessageTile extends StatelessWidget {
  const _ReadyMessageTile({required this.message, required this.onCopy});

  final _ReadyMessageRow message;
  final VoidCallback onCopy;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 9),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          SizedBox(
            width: 48,
            child: Container(
              width: 36,
              height: 36,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: message.background,
                shape: BoxShape.circle,
              ),
              child: Icon(message.icon, color: message.foreground, size: 19),
            ),
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  message.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  message.detail,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 11.5),
                ),
              ],
            ),
          ),
          const SizedBox(width: 10),
          _DataBadge(
            label: message.badge,
            background: message.background,
            foreground: message.foreground,
            radius: 14,
          ),
          const SizedBox(width: 10),
          SizedBox(
            width: 40,
            height: 40,
            child: OutlinedButton(
              onPressed: onCopy,
              style: _ghostButtonStyle(
                t,
                minimumSize: const Size(40, 40),
                padding: EdgeInsets.zero,
              ),
              child: Icon(Icons.copy_outlined, color: t.ink, size: 16),
            ),
          ),
        ],
      ),
    );
  }
}

class _CampaignsPanel extends StatelessWidget {
  const _CampaignsPanel({
    required this.staleCustomers,
    required this.noShows,
    required this.pendingConfirmations,
    required this.promotionName,
    required this.promotionOffer,
    required this.onOpen,
  });

  final int staleCustomers;
  final int noShows;
  final int pendingConfirmations;
  final String promotionName;
  final String promotionOffer;
  final ValueChanged<_MarketingCampaignRow> onOpen;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final campaigns = <_MarketingCampaignRow>[
      const _MarketingCampaignRow(
        name: 'Volta para agenda',
        detail: '',
        badge: 'WhatsApp',
        icon: Icons.person_outline,
        background: Color(0xFFDCFCE7),
        foreground: Color(0xFF16A34A),
      ).copyWith(detail: '$staleCustomers cliente(s) sem retorno para chamar.'),
      _MarketingCampaignRow(
        name: 'Confirmar horários',
        detail: '$pendingConfirmations agendamento(s) aguardando confirmação.',
        badge: 'Hoje',
        icon: Icons.calendar_month_outlined,
        background: t.accentSoft,
        foreground: t.accent,
      ),
      _MarketingCampaignRow(
        name: 'Recuperar faltas',
        detail: '$noShows falta(s) recente(s) para remarcar.',
        badge: 'Retorno',
        icon: Icons.error_outline,
        background: t.redSoft,
        foreground: const Color(0xFFDC2626),
      ),
      _MarketingCampaignRow(
        name: promotionName,
        detail: promotionOffer,
        badge: 'Promoção',
        icon: Icons.campaign,
        background: t.yellowSoft,
        foreground: t.ink,
      ),
    ];

    return _MarketingSurface(
      key: const Key('marketing-suggested-campaigns'),
      radius: 16,
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const _PanelHeading(
            title: 'Campanhas sugeridas',
            subtitle:
                'Ideias simples para iniciar sem lotar o cliente de informação.',
          ),
          const SizedBox(height: 12),
          for (final campaign in campaigns)
            _CampaignTile(campaign: campaign, onOpen: onOpen),
        ],
      ),
    );
  }
}

class _CampaignTile extends StatelessWidget {
  const _CampaignTile({required this.campaign, required this.onOpen});

  final _MarketingCampaignRow campaign;
  final ValueChanged<_MarketingCampaignRow> onOpen;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Material(
      color: t.panel,
      child: InkWell(
        onTap: () => onOpen(campaign),
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: 9),
          decoration: BoxDecoration(
            border: Border(bottom: BorderSide(color: t.line)),
          ),
          child: Row(
            children: [
              SizedBox(
                width: 48,
                child: Container(
                  width: 36,
                  height: 36,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: campaign.background,
                    borderRadius: BorderRadius.circular(14),
                  ),
                  child: Icon(
                    campaign.icon,
                    color: campaign.foreground,
                    size: 19,
                  ),
                ),
              ),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      campaign.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 13.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      campaign.detail,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: t.muted, fontSize: 11.5),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 10),
              _DataBadge(
                label: campaign.badge,
                background: campaign.background,
                foreground: campaign.foreground,
                radius: 14,
              ),
              const SizedBox(width: 8),
              Icon(Icons.chevron_right, color: t.muted, size: 20),
            ],
          ),
        ),
      ),
    );
  }
}

class _PanelHeading extends StatelessWidget {
  const _PanelHeading({required this.title, required this.subtitle});

  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: TextStyle(
            color: t.ink,
            fontSize: 18,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 2),
        Text(
          subtitle,
          maxLines: 1,
          softWrap: false,
          overflow: TextOverflow.clip,
          style: TextStyle(color: t.muted, fontSize: 12),
        ),
      ],
    );
  }
}

class _MarketingWatermark extends StatelessWidget {
  const _MarketingWatermark();

  // Mirrors the WPF ImageBrush viewbox (328,194,1021,543) followed by
  // UniformToFill inside the 300 x 104 watermark slot.
  static const _scale = 300 / 1021;

  @override
  Widget build(BuildContext context) {
    return ClipRect(
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned(
            left: -328 * _scale,
            top: -(194 * _scale) - ((543 * _scale - 104) / 2),
            width: 1365 * _scale,
            height: 1365 * _scale,
            child: Image.asset(
              'assets/branding/agenda-livre-logo-source.png',
              fit: BoxFit.fill,
            ),
          ),
        ],
      ),
    );
  }
}

class _DataBadge extends StatelessWidget {
  const _DataBadge({
    required this.label,
    required this.background,
    required this.foreground,
    required this.radius,
    this.horizontalPadding = 9,
  });

  final String label;
  final Color background;
  final Color foreground;
  final double radius;
  final double horizontalPadding;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: horizontalPadding, vertical: 4),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(radius),
      ),
      child: Text(
        label,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: TextStyle(
          color: foreground,
          fontSize: 10.5,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _MarketingSurface extends StatelessWidget {
  const _MarketingSurface({
    super.key,
    required this.child,
    required this.radius,
    required this.padding,
    this.minHeight,
    this.clip = false,
  });

  final Widget child;
  final double radius;
  final EdgeInsets padding;
  final double? minHeight;
  final bool clip;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final body = Padding(padding: padding, child: child);
    return Container(
      constraints: minHeight == null
          ? null
          : BoxConstraints(minHeight: minHeight!),
      decoration: BoxDecoration(
        color: t.panel,
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(radius),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0C171411),
            blurRadius: 24,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: clip
          ? ClipRRect(borderRadius: BorderRadius.circular(radius), child: body)
          : body,
    );
  }
}

class _MarketingContactRow {
  const _MarketingContactRow({
    required this.name,
    required this.detail,
    required this.phone,
    required this.badge,
    required this.messagePreview,
  });

  final String name;
  final String detail;
  final String phone;
  final String badge;
  final String messagePreview;
}

class _ReadyMessageRow {
  const _ReadyMessageRow({
    required this.name,
    required this.detail,
    required this.badge,
    required this.icon,
    required this.background,
    required this.foreground,
  });

  final String name;
  final String detail;
  final String badge;
  final IconData icon;
  final Color background;
  final Color foreground;
}

class _MarketingCampaignRow {
  const _MarketingCampaignRow({
    required this.name,
    required this.detail,
    required this.badge,
    required this.icon,
    required this.background,
    required this.foreground,
  });

  final String name;
  final String detail;
  final String badge;
  final IconData icon;
  final Color background;
  final Color foreground;

  _MarketingCampaignRow copyWith({String? detail}) => _MarketingCampaignRow(
    name: name,
    detail: detail ?? this.detail,
    badge: badge,
    icon: icon,
    background: background,
    foreground: foreground,
  );
}

ButtonStyle _primaryButtonStyle(
  AgendaThemeTokens t, {
  Size minimumSize = const Size(98, 40),
}) {
  return ElevatedButton.styleFrom(
    elevation: 0,
    minimumSize: minimumSize,
    padding: const EdgeInsets.symmetric(horizontal: 14),
    backgroundColor: t.accentDark,
    foregroundColor: Colors.white,
    disabledBackgroundColor: t.accentDark.withValues(alpha: .45),
    textStyle: const TextStyle(
      fontFamily: 'Segoe UI',
      fontSize: 14,
      fontWeight: FontWeight.w600,
    ),
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
  );
}

ButtonStyle _ghostButtonStyle(
  AgendaThemeTokens t, {
  Size minimumSize = const Size(98, 40),
  EdgeInsets padding = const EdgeInsets.symmetric(horizontal: 14),
}) {
  return OutlinedButton.styleFrom(
    minimumSize: minimumSize,
    padding: padding,
    backgroundColor: t.panel,
    foregroundColor: t.ink,
    side: BorderSide(color: t.line),
    textStyle: const TextStyle(
      fontFamily: 'Segoe UI',
      fontSize: 14,
      fontWeight: FontWeight.w600,
    ),
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
  );
}

ButtonStyle _subtleButtonStyle(
  AgendaThemeTokens t, {
  Size minimumSize = const Size(82, 40),
  EdgeInsets padding = const EdgeInsets.symmetric(horizontal: 12),
}) {
  return OutlinedButton.styleFrom(
    minimumSize: minimumSize,
    padding: padding,
    backgroundColor: t.warmSoft,
    foregroundColor: t.ink,
    side: BorderSide(color: t.line),
    textStyle: const TextStyle(
      fontFamily: 'Segoe UI',
      fontSize: 14,
      fontWeight: FontWeight.w600,
    ),
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
  );
}

Widget _buttonContent(Widget icon, String text, {double gap = 7}) {
  return Row(
    mainAxisSize: MainAxisSize.min,
    mainAxisAlignment: MainAxisAlignment.center,
    children: [
      icon,
      SizedBox(width: gap),
      Flexible(
        fit: FlexFit.loose,
        child: Text(
          text,
          maxLines: 1,
          softWrap: false,
          overflow: TextOverflow.clip,
        ),
      ),
    ],
  );
}

String _replaceToken(String source, String token, String value) {
  return source.replaceAll(
    RegExp(RegExp.escape(token), caseSensitive: false),
    value,
  );
}

String _onlyDigits(String value) => value.replaceAll(RegExp(r'\D'), '');

String _twoDigits(int value) => value.toString().padLeft(2, '0');
