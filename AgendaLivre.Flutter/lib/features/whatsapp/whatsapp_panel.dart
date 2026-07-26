import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/formatters.dart';
import '../../core/ui.dart';
import '../../domain/models/models.dart';
import '../../services/whatsapp_service.dart';

class AgendaWhatsAppFab extends StatelessWidget {
  const AgendaWhatsAppFab({
    super.key,
    required this.controller,
    this.compact = false,
  });

  final AgendaController controller;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final unread = controller.data.whatsAppMessages
        .where(
          (item) =>
              item.direction.toLowerCase() == 'entrada' && item.readAt == null,
        )
        .length;
    return Stack(
      clipBehavior: Clip.none,
      children: [
        FloatingActionButton(
          key: const Key('agenda-whatsapp-fab'),
          heroTag: 'agenda-whatsapp',
          tooltip: 'WhatsApp',
          backgroundColor: const Color(0xFF08A84E),
          foregroundColor: Colors.white,
          elevation: 1,
          focusElevation: 1,
          hoverElevation: 2,
          highlightElevation: 1,
          shape: const CircleBorder(),
          mini: compact,
          onPressed: () => showAgendaWhatsAppPanel(context, controller),
          child: FaIcon(FontAwesomeIcons.whatsapp, size: compact ? 20 : 24),
        ),
        if (unread > 0)
          Positioned(
            right: -4,
            top: -5,
            child: Container(
              constraints: const BoxConstraints(minWidth: 20, minHeight: 20),
              padding: const EdgeInsets.symmetric(horizontal: 5),
              decoration: BoxDecoration(
                color: const Color(0xFFDC2626),
                shape: unread < 10 ? BoxShape.circle : BoxShape.rectangle,
                borderRadius: unread < 10 ? null : BorderRadius.circular(12),
                border: Border.all(color: Colors.white, width: 2),
              ),
              alignment: Alignment.center,
              child: Text(
                '$unread',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 9,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
          ),
      ],
    );
  }
}

Future<void> showAgendaWhatsAppPanel(
  BuildContext context,
  AgendaController controller,
) async {
  if (MediaQuery.sizeOf(context).width < 760) {
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: Colors.transparent,
      builder: (context) => FractionallySizedBox(
        heightFactor: .92,
        child: _WhatsAppPanel(controller: controller, mobile: true),
      ),
    );
    return;
  }
  await showGeneralDialog<void>(
    context: context,
    barrierDismissible: true,
    barrierLabel: 'Fechar WhatsApp',
    barrierColor: const Color(0x61000000),
    transitionDuration: const Duration(milliseconds: 190),
    pageBuilder: (context, _, _) => Align(
      alignment: Alignment.centerRight,
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(0, 12, 16, 12),
          child: SizedBox(
            width: 420,
            child: _WhatsAppPanel(controller: controller, mobile: false),
          ),
        ),
      ),
    ),
    transitionBuilder: (context, animation, _, child) => SlideTransition(
      position: Tween(
        begin: const Offset(1, 0),
        end: Offset.zero,
      ).animate(CurvedAnimation(parent: animation, curve: Curves.easeOutCubic)),
      child: FadeTransition(opacity: animation, child: child),
    ),
  );
}

class _WhatsAppPanel extends StatefulWidget {
  const _WhatsAppPanel({required this.controller, required this.mobile});

  final AgendaController controller;
  final bool mobile;

  @override
  State<_WhatsAppPanel> createState() => _WhatsAppPanelState();
}

class _WhatsAppPanelState extends State<_WhatsAppPanel> {
  final _service = const WhatsAppService();
  late final TextEditingController _phone;
  late final TextEditingController _message;
  bool _sending = false;
  bool _compose = false;
  String? _selectedPhone;

  @override
  void initState() {
    super.initState();
    final settings = widget.controller.data.settings;
    _phone = TextEditingController(text: settings.whatsAppStorePhone);
    _message = TextEditingController();
  }

  @override
  void dispose() {
    _phone.dispose();
    _message.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final settings = widget.controller.data.settings;
    final conversations = _buildConversations(
      widget.controller.data.whatsAppMessages,
      widget.controller.data.whatsAppLeads,
    );
    final selected = _selectedPhone == null
        ? null
        : conversations
              .where((item) => item.phoneKey == _selectedPhone)
              .firstOrNull;

    return Material(
      color: t.panel,
      elevation: 12,
      shadowColor: const Color(0x30171411),
      shape: RoundedRectangleBorder(
        side: BorderSide(color: t.line),
        borderRadius: BorderRadius.circular(widget.mobile ? 20 : 16),
      ),
      clipBehavior: Clip.antiAlias,
      child: AnimatedBuilder(
        animation: widget.controller,
        builder: (context, _) => Column(
          children: [
            _PanelHeader(
              subtitle: 'Conversas e confirmações',
              onClose: () => Navigator.of(context).pop(),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(10, 8, 10, 6),
              child: Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 8),
                decoration: BoxDecoration(
                  color: t.warmSoft,
                  border: Border.all(color: t.line),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Text(
                  settings.whatsAppLinked
                      ? 'Confirmações, retornos e mensagens aparecem aqui.'
                      : 'Linke o WhatsApp da loja.',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 11.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ),
            Expanded(
              child: _compose
                  ? _ComposeView(
                      phone: _phone,
                      message: _message,
                      sending: _sending,
                      onBack: _showConversationList,
                      onSend: () => _send(),
                    )
                  : selected != null
                  ? _ConversationView(
                      conversation: selected,
                      messageController: _message,
                      sending: _sending,
                      onBack: _showConversationList,
                      onSend: () => _send(phone: selected.phone),
                    )
                  : _ConversationList(
                      conversations: conversations,
                      linked: settings.whatsAppLinked,
                      onConnection: _connectionAction,
                      onCompose: () => setState(() {
                        _compose = true;
                        _selectedPhone = null;
                        _phone.clear();
                        _message.clear();
                      }),
                      onOpen: (item) => setState(() {
                        _selectedPhone = item.phoneKey;
                        _compose = false;
                        _message.clear();
                      }),
                    ),
            ),
          ],
        ),
      ),
    );
  }

  void _showConversationList() => setState(() {
    _compose = false;
    _selectedPhone = null;
    _message.clear();
  });

  void _connectionAction() {
    final linked = widget.controller.data.settings.whatsAppLinked;
    _show(
      linked
          ? 'A conexão automática é gerenciada pelo serviço seguro do Agenda Livre.'
          : 'A conexão automática requer o serviço seguro do Agenda Livre. Você ainda pode enviar manualmente.',
    );
  }

  Future<void> _send({String? phone}) async {
    setState(() => _sending = true);
    try {
      final targetPhone = phone ?? _phone.text;
      final result = await _service.sendText(
        phone: targetPhone,
        message: _message.text,
      );
      final opened = await launchUrl(
        result.fallbackUri,
        mode: LaunchMode.externalApplication,
      );
      if (!opened) {
        throw const WhatsAppValidationException(
          'Não foi possível abrir o WhatsApp neste dispositivo.',
        );
      }
      final displayName = phone == null
          ? 'Contato'
          : _buildConversations(
                      widget.controller.data.whatsAppMessages,
                      widget.controller.data.whatsAppLeads,
                    )
                    .where((item) => item.phoneKey == _phoneKey(targetPhone))
                    .firstOrNull
                    ?.name ??
                'Contato';
      await widget.controller.addWhatsAppMessage(
        WhatsAppMessage(
          customerName: displayName,
          phone: targetPhone,
          message: _message.text.trim(),
          direction: 'saida',
          status: result.sent ? 'enviado' : 'aberto',
          category: 'Atendimento',
          createdAt: DateTime.now(),
          sentAt: DateTime.now(),
        ),
      );
      _message.clear();
      if (mounted) {
        setState(() {
          _compose = false;
          _selectedPhone = _phoneKey(targetPhone);
        });
      }
    } on WhatsAppValidationException catch (error) {
      if (mounted) _show(error.message);
    } catch (_) {
      if (mounted) _show('Não foi possível abrir o WhatsApp agora.');
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }

  void _show(String value) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(value)));
  }
}

class _PanelHeader extends StatelessWidget {
  const _PanelHeader({required this.subtitle, required this.onClose});

  final String subtitle;
  final VoidCallback onClose;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        border: Border(bottom: BorderSide(color: t.line)),
      ),
      child: Row(
        children: [
          const AgendaIconBadge(
            Icons.chat_bubble_outline_rounded,
            color: Color(0xFF16A34A),
            background: Color(0xFFDCFCE7),
            size: 34,
            iconSize: 19,
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Conversas',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 15,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                Text(
                  subtitle,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 11),
                ),
              ],
            ),
          ),
          IconButton.outlined(
            tooltip: 'Fechar WhatsApp',
            onPressed: onClose,
            icon: const Icon(Icons.close_rounded, size: 18),
          ),
        ],
      ),
    );
  }
}

class _ConversationList extends StatelessWidget {
  const _ConversationList({
    required this.conversations,
    required this.linked,
    required this.onConnection,
    required this.onCompose,
    required this.onOpen,
  });

  final List<_WhatsAppConversation> conversations;
  final bool linked;
  final VoidCallback onConnection;
  final VoidCallback onCompose;
  final ValueChanged<_WhatsAppConversation> onOpen;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(10, 3, 10, 8),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Conversas recentes',
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 13,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      conversations.isEmpty
                          ? 'Nenhuma conversa ainda'
                          : '${conversations.length} conversa${conversations.length == 1 ? '' : 's'}',
                      style: TextStyle(color: t.muted, fontSize: 11),
                    ),
                  ],
                ),
              ),
              TextButton.icon(
                onPressed: onCompose,
                icon: const Icon(Icons.add_rounded, size: 16),
                label: const Text('Nova'),
              ),
              const SizedBox(width: 4),
              OutlinedButton(
                onPressed: onConnection,
                child: Text(linked ? 'Deslinkar' : 'Linkar'),
              ),
            ],
          ),
        ),
        Expanded(
          child: conversations.isEmpty
              ? _EmptyConversations(onLink: onConnection, linked: linked)
              : ListView.separated(
                  padding: const EdgeInsets.fromLTRB(10, 0, 10, 12),
                  itemCount: conversations.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 8),
                  itemBuilder: (context, index) => _ConversationCard(
                    conversation: conversations[index],
                    onTap: () => onOpen(conversations[index]),
                  ),
                ),
        ),
      ],
    );
  }
}

class _EmptyConversations extends StatelessWidget {
  const _EmptyConversations({required this.onLink, required this.linked});

  final VoidCallback onLink;
  final bool linked;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'Nenhuma conversa ainda. Quando um cliente chamar no WhatsApp, aparece aqui.',
              textAlign: TextAlign.center,
              style: TextStyle(color: t.muted, fontSize: 11.5, height: 1.35),
            ),
            if (!linked) ...[
              const SizedBox(height: 14),
              ElevatedButton(
                onPressed: onLink,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF159447),
                  foregroundColor: Colors.white,
                ),
                child: const Text('Linkar WhatsApp'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _ConversationCard extends StatelessWidget {
  const _ConversationCard({required this.conversation, required this.onTap});

  final _WhatsAppConversation conversation;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final latest = conversation.messages.last;
    return AgendaPanel(
      radius: 12,
      padding: const EdgeInsets.all(12),
      onTap: onTap,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  conversation.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 12.5,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
              Text(
                '${shortDate(latest.createdAt)} ${hour(latest.createdAt)}',
                style: TextStyle(color: t.muted, fontSize: 9.5),
              ),
              if (conversation.unread > 0) ...[
                const SizedBox(width: 6),
                AgendaPill(
                  label: '${conversation.unread}',
                  color: const Color(0xFF16A34A),
                  textColor: Colors.white,
                ),
              ],
            ],
          ),
          const SizedBox(height: 10),
          Text(
            latest.message,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(color: t.ink, fontSize: 11.5, height: 1.25),
          ),
          const SizedBox(height: 7),
          Align(
            alignment: Alignment.centerRight,
            child: Text(
              'Abrir conversa',
              style: TextStyle(
                color: t.accentDark,
                fontSize: 10.5,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ConversationView extends StatelessWidget {
  const _ConversationView({
    required this.conversation,
    required this.messageController,
    required this.sending,
    required this.onBack,
    required this.onSend,
  });

  final _WhatsAppConversation conversation;
  final TextEditingController messageController;
  final bool sending;
  final VoidCallback onBack;
  final VoidCallback onSend;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(10, 2, 10, 7),
          child: Row(
            children: [
              IconButton.outlined(
                tooltip: 'Voltar para conversas',
                onPressed: onBack,
                icon: const Icon(Icons.arrow_back_rounded, size: 18),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      conversation.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: t.ink,
                        fontSize: 13,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    Text(
                      conversation.phone,
                      style: TextStyle(color: t.muted, fontSize: 11),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
        if (conversation.lead != null) _LeadSummary(lead: conversation.lead!),
        Expanded(
          child: ListView.builder(
            padding: const EdgeInsets.fromLTRB(12, 8, 12, 12),
            itemCount: conversation.messages.length,
            itemBuilder: (context, index) =>
                _MessageBubble(message: conversation.messages[index]),
          ),
        ),
        Container(
          padding: const EdgeInsets.all(10),
          margin: const EdgeInsets.fromLTRB(12, 0, 12, 10),
          decoration: BoxDecoration(
            color: t.warmSoft,
            border: Border.all(color: t.line),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Row(
            children: [
              Expanded(
                child: TextField(
                  controller: messageController,
                  decoration: const InputDecoration(
                    hintText: 'Digite a resposta',
                    isDense: true,
                  ),
                  onSubmitted: (_) {
                    if (!sending) onSend();
                  },
                ),
              ),
              const SizedBox(width: 8),
              IconButton.filled(
                tooltip: 'Enviar resposta',
                onPressed: sending ? null : onSend,
                style: IconButton.styleFrom(
                  backgroundColor: const Color(0xFF16A34A),
                  foregroundColor: Colors.white,
                ),
                icon: sending
                    ? const SizedBox.square(
                        dimension: 16,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Icon(Icons.send_rounded, size: 18),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _LeadSummary extends StatelessWidget {
  const _LeadSummary({required this.lead});

  final WhatsAppLead lead;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Container(
      margin: const EdgeInsets.fromLTRB(12, 0, 12, 6),
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 7),
      decoration: BoxDecoration(
        border: Border.all(color: t.line),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        children: [
          Text('Lead', style: TextStyle(color: t.muted, fontSize: 10.5)),
          const SizedBox(width: 7),
          AgendaPill(
            label: _leadStage(lead.stage),
            color: const Color(0xFFECFDF3),
            textColor: const Color(0xFF166534),
          ),
          const SizedBox(width: 7),
          Text(
            '${lead.score} pts',
            style: TextStyle(color: t.muted, fontSize: 10),
          ),
          if (lead.summary.trim().isNotEmpty) ...[
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                lead.summary,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: t.muted, fontSize: 10),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _MessageBubble extends StatelessWidget {
  const _MessageBubble({required this.message});

  final WhatsAppMessage message;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final incoming = message.direction.toLowerCase() == 'entrada';
    return Align(
      alignment: incoming ? Alignment.centerLeft : Alignment.centerRight,
      child: Container(
        constraints: const BoxConstraints(maxWidth: 300),
        margin: const EdgeInsets.only(bottom: 7),
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        decoration: BoxDecoration(
          color: incoming ? t.panel : const Color(0xFFE2F8EA),
          border: Border.all(color: t.line),
          borderRadius: BorderRadius.circular(11),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              message.message,
              style: TextStyle(color: t.ink, fontSize: 11.5, height: 1.3),
            ),
            const SizedBox(height: 3),
            Text(
              '${shortDate(message.createdAt)} ${hour(message.createdAt)}',
              style: TextStyle(color: t.muted, fontSize: 9),
            ),
          ],
        ),
      ),
    );
  }
}

class _ComposeView extends StatelessWidget {
  const _ComposeView({
    required this.phone,
    required this.message,
    required this.sending,
    required this.onBack,
    required this.onSend,
  });

  final TextEditingController phone;
  final TextEditingController message;
  final bool sending;
  final VoidCallback onBack;
  final VoidCallback onSend;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return ListView(
      padding: const EdgeInsets.fromLTRB(12, 2, 12, 16),
      children: [
        Row(
          children: [
            IconButton.outlined(
              onPressed: onBack,
              icon: const Icon(Icons.arrow_back_rounded, size: 18),
            ),
            const SizedBox(width: 8),
            Text(
              'Nova mensagem',
              style: TextStyle(
                color: t.ink,
                fontSize: 15,
                fontWeight: FontWeight.w800,
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        TextField(
          controller: phone,
          keyboardType: TextInputType.phone,
          decoration: const InputDecoration(
            labelText: 'Telefone com DDD',
            prefixIcon: Icon(Icons.phone_outlined, size: 19),
          ),
        ),
        const SizedBox(height: 10),
        TextField(
          controller: message,
          minLines: 4,
          maxLines: 8,
          decoration: const InputDecoration(
            labelText: 'Mensagem',
            alignLabelWithHint: true,
          ),
        ),
        const SizedBox(height: 12),
        ElevatedButton.icon(
          onPressed: sending ? null : onSend,
          style: ElevatedButton.styleFrom(
            backgroundColor: const Color(0xFF16A34A),
            foregroundColor: Colors.white,
          ),
          icon: sending
              ? const SizedBox.square(
                  dimension: 16,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: Colors.white,
                  ),
                )
              : const Icon(Icons.send_rounded, size: 17),
          label: const Text('Abrir no WhatsApp'),
        ),
      ],
    );
  }
}

class _WhatsAppConversation {
  const _WhatsAppConversation({
    required this.phoneKey,
    required this.phone,
    required this.name,
    required this.messages,
    required this.unread,
    this.lead,
  });

  final String phoneKey;
  final String phone;
  final String name;
  final List<WhatsAppMessage> messages;
  final int unread;
  final WhatsAppLead? lead;
}

List<_WhatsAppConversation> _buildConversations(
  List<WhatsAppMessage> messages,
  List<WhatsAppLead> leads,
) {
  final grouped = <String, List<WhatsAppMessage>>{};
  for (final message in messages) {
    final key = _phoneKey(message.phone);
    if (key.isEmpty) continue;
    grouped.putIfAbsent(key, () => <WhatsAppMessage>[]).add(message);
  }
  final result = <_WhatsAppConversation>[];
  for (final entry in grouped.entries) {
    final items = entry.value
      ..sort((a, b) => a.createdAt.compareTo(b.createdAt));
    final lead = leads
        .where((item) => _phoneKey(item.phone) == entry.key)
        .firstOrNull;
    final named = items.reversed
        .map((item) => item.customerName.trim())
        .where((item) => item.isNotEmpty)
        .firstOrNull;
    result.add(
      _WhatsAppConversation(
        phoneKey: entry.key,
        phone: items.last.phone,
        name: lead?.customerName.trim().isNotEmpty == true
            ? lead!.customerName
            : (named ?? items.last.phone),
        messages: items,
        unread: items
            .where(
              (item) =>
                  item.direction.toLowerCase() == 'entrada' &&
                  item.readAt == null,
            )
            .length,
        lead: lead,
      ),
    );
  }
  result.sort(
    (a, b) => b.messages.last.createdAt.compareTo(a.messages.last.createdAt),
  );
  return result;
}

String _phoneKey(String value) => value.replaceAll(RegExp(r'\D'), '');

String _leadStage(String value) => switch (value.toLowerCase()) {
  'qualified' => 'Qualificado',
  'won' => 'Ganho',
  'lost' => 'Perdido',
  _ => 'Novo',
};

extension _FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
