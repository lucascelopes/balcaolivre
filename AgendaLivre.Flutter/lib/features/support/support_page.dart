import 'package:flutter/material.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';

class SupportPage extends StatefulWidget {
  const SupportPage({super.key, required this.controller});

  final AgendaController controller;

  @override
  State<SupportPage> createState() => _SupportPageState();
}

class _SupportPageState extends State<SupportPage> {
  static const _topics = <_SupportTopic>[
    _SupportTopic(
      'Agenda e horários',
      'Agendamentos, disponibilidade e bloqueios',
      Icons.calendar_month_outlined,
      [
        'Criar um novo agendamento',
        'Reagendar um cliente',
        'Cancelar um atendimento',
        'Bloquear um horário',
      ],
    ),
    _SupportTopic(
      'Financeiro e pagamentos',
      'Recebimentos, repasses e formas de pagamento',
      Icons.account_balance_wallet_outlined,
      [
        'Receber um atendimento',
        'Conferir pagamentos pendentes',
        'Registrar uma despesa',
        'Fechar o caixa do dia',
      ],
    ),
    _SupportTopic(
      'Clientes e serviços',
      'Cadastros, histórico e catálogo de serviços',
      Icons.people_outline,
      [
        'Cadastrar um cliente',
        'Consultar o histórico',
        'Criar ou editar um serviço',
        'Organizar a equipe',
      ],
    ),
    _SupportTopic(
      'Conta e sincronização',
      'Acesso, segurança e dados entre dispositivos',
      Icons.cloud_sync_outlined,
      [
        'Entrar em outro dispositivo',
        'Conferir a sincronização',
        'Alterar a senha',
        'Proteger os dados da conta',
      ],
    ),
  ];

  final _searchController = TextEditingController();
  String _query = '';

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final desktop = MediaQuery.sizeOf(context).width >= 900;
    return ColoredBox(
      color: const Color(0xFFFAF9F7),
      child: SingleChildScrollView(
        key: const Key('support-page-scroll'),
        padding: EdgeInsets.fromLTRB(
          desktop ? 20 : 14,
          desktop ? 20 : 14,
          desktop ? 18 : 14,
          84,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _header(t, desktop),
            const SizedBox(height: 18),
            _search(t, desktop),
            if (_query.isNotEmpty) ...[
              const SizedBox(height: 16),
              _searchResult(t),
            ],
            const SizedBox(height: 16),
            if (desktop)
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(flex: 138, child: _topicsPanel(t)),
                  const SizedBox(width: 16),
                  Expanded(flex: 92, child: _contactPanel(t)),
                ],
              )
            else ...[
              _topicsPanel(t),
              const SizedBox(height: 16),
              _contactPanel(t),
            ],
            const SizedBox(height: 16),
            _diagnostics(t, desktop),
          ],
        ),
      ),
    );
  }

  Widget _header(AgendaThemeTokens t, bool desktop) => Row(
    crossAxisAlignment: CrossAxisAlignment.center,
    children: [
      Expanded(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Text(
                  'SUPORTE',
                  style: TextStyle(
                    color: t.accent,
                    fontSize: 10,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(width: 12),
                Container(width: 44, height: 1, color: t.accent),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              'Central de ajuda',
              style: TextStyle(
                color: t.ink,
                fontSize: 28,
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: 3),
            Text(
              'Encontre respostas rápidas ou fale com a nossa equipe.',
              style: TextStyle(color: t.muted, fontSize: 12.5),
            ),
          ],
        ),
      ),
      if (desktop)
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          decoration: BoxDecoration(
            color: const Color(0xFFEAF8EF),
            borderRadius: BorderRadius.circular(14),
          ),
          child: const Row(
            children: [
              CircleAvatar(radius: 4, backgroundColor: Color(0xFF24A45A)),
              SizedBox(width: 7),
              Text(
                'Serviços online',
                style: TextStyle(
                  color: Color(0xFF17683A),
                  fontSize: 11.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
    ],
  );

  Widget _search(AgendaThemeTokens t, bool desktop) => _panel(
    t,
    padding: const EdgeInsets.all(18),
    child: desktop
        ? Row(
            children: [
              Expanded(child: _searchField()),
              const SizedBox(width: 10),
              _searchButton(),
            ],
          )
        : Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _searchField(),
              const SizedBox(height: 10),
              _searchButton(),
            ],
          ),
  );

  Widget _searchField() => SizedBox(
    height: 46,
    child: TextField(
      key: const Key('support-search-field'),
      controller: _searchController,
      textInputAction: TextInputAction.search,
      onSubmitted: (_) => _runSearch(),
      decoration: const InputDecoration(
        hintText: 'Como podemos ajudar?',
        prefixIcon: Icon(Icons.search_rounded, size: 20),
      ),
    ),
  );

  Widget _searchButton() => SizedBox(
    height: 46,
    child: FilledButton.icon(
      key: const Key('support-search-button'),
      onPressed: _runSearch,
      iconAlignment: IconAlignment.end,
      icon: const Icon(Icons.arrow_forward, size: 17),
      label: const Text('Buscar'),
    ),
  );

  void _runSearch() => setState(() => _query = _searchController.text.trim());

  Widget _searchResult(AgendaThemeTokens t) {
    final normalized = _query.toLowerCase();
    final matches = <String>[
      for (final topic in _topics)
        for (final guide in topic.guides)
          if ('${topic.title} ${topic.subtitle} $guide'.toLowerCase().contains(
            normalized,
          ))
            guide,
    ];
    final message = matches.isEmpty
        ? 'Não encontramos um guia exato para “$_query”. Fale com a equipe pelo chat para receber ajuda.'
        : 'Encontramos ${matches.length} guia(s): ${matches.take(3).join(' · ')}.';
    return Container(
      key: const Key('support-search-result'),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: t.accentSoft,
        border: Border.all(color: const Color(0xFFF4B08D)),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.lightbulb_outline, color: t.accent, size: 18),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              message,
              style: TextStyle(color: t.ink, fontSize: 12.5),
            ),
          ),
          TextButton(
            onPressed: () {
              _searchController.clear();
              setState(() => _query = '');
            },
            child: const Text('Limpar'),
          ),
        ],
      ),
    );
  }

  Widget _topicsPanel(AgendaThemeTokens t) => _panel(
    t,
    padding: const EdgeInsets.fromLTRB(20, 18, 20, 14),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Tópicos mais acessados',
          style: TextStyle(
            color: t.ink,
            fontSize: 17,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 10),
        for (final topic in _topics) _topicButton(t, topic),
      ],
    ),
  );

  Widget _topicButton(AgendaThemeTokens t, _SupportTopic topic) => InkWell(
    key: Key('support-topic-${topic.title}'),
    borderRadius: BorderRadius.circular(12),
    onTap: () => _openTopic(topic),
    child: SizedBox(
      height: 59,
      child: Row(
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: t.accentSoft,
              borderRadius: BorderRadius.circular(11),
            ),
            child: Icon(topic.icon, color: t.accent, size: 18),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  topic.title,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  topic.subtitle,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: t.muted, fontSize: 11.5),
                ),
              ],
            ),
          ),
          Icon(Icons.chevron_right, color: t.muted, size: 18),
        ],
      ),
    ),
  );

  Future<void> _openTopic(_SupportTopic topic) => showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (context) => SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 0, 24, 30),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(topic.title, style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: 6),
            Text(topic.subtitle),
            const SizedBox(height: 18),
            for (var index = 0; index < topic.guides.length; index++)
              ListTile(
                leading: CircleAvatar(radius: 15, child: Text('${index + 1}')),
                title: Text(topic.guides[index]),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => Navigator.pop(context),
              ),
          ],
        ),
      ),
    ),
  );

  Widget _contactPanel(AgendaThemeTokens t) => _panel(
    t,
    padding: EdgeInsets.zero,
    clip: true,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 14, 16, 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Container(
                    width: 9,
                    height: 9,
                    decoration: BoxDecoration(
                      color: t.accent,
                      shape: BoxShape.circle,
                    ),
                  ),
                  const SizedBox(width: 9),
                  Text(
                    'Atendimento disponível',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 13.5,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              Row(
                children: [
                  Icon(Icons.schedule_outlined, color: t.ink, size: 19),
                  const SizedBox(width: 9),
                  Text(
                    'Horários da equipe',
                    style: TextStyle(
                      color: t.ink,
                      fontSize: 14,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              _hours(t, 'Seg–Sex', '09:00 — 22:00'),
              _hours(t, 'Sábado', '09:00 — 20:00'),
              _hours(t, 'Domingo', '10:00 — 20:00'),
            ],
          ),
        ),
        Divider(height: 1, color: t.line),
        _contactRow(
          t,
          key: const Key('support-open-chat'),
          icon: Icons.chat_bubble_outline,
          iconColor: t.accent,
          iconBackground: const Color(0xFFFFF0E7),
          title: 'Chat no Agenda Livre',
          subtitle: 'Fale com nossa equipe dentro do app',
          onTap: _openChat,
        ),
        Divider(height: 1, indent: 14, endIndent: 14, color: t.line),
        _contactRow(
          t,
          key: const Key('support-open-whatsapp'),
          iconWidget: const FaIcon(
            FontAwesomeIcons.whatsapp,
            color: Color(0xFF138A3D),
            size: 21,
          ),
          iconBackground: const Color(0xFFEAF8EF),
          title: 'Conversar pelo WhatsApp',
          subtitle: '(33) 99131-4125',
          onTap: _openWhatsApp,
        ),
      ],
    ),
  );

  Widget _hours(AgendaThemeTokens t, String day, String value) => Container(
    height: 30,
    decoration: BoxDecoration(
      border: Border(bottom: BorderSide(color: t.line)),
    ),
    child: Row(
      children: [
        Expanded(child: Text(day, style: const TextStyle(fontSize: 11.5))),
        Text(
          value,
          style: const TextStyle(fontSize: 11.5, fontWeight: FontWeight.w700),
        ),
      ],
    ),
  );

  Widget _contactRow(
    AgendaThemeTokens t, {
    required Key key,
    IconData? icon,
    Widget? iconWidget,
    required Color iconBackground,
    Color? iconColor,
    required String title,
    required String subtitle,
    required VoidCallback onTap,
  }) => InkWell(
    key: key,
    onTap: onTap,
    child: Padding(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      child: Row(
        children: [
          Container(
            width: 42,
            height: 42,
            decoration: BoxDecoration(
              color: iconBackground,
              borderRadius: BorderRadius.circular(12),
            ),
            alignment: Alignment.center,
            child: iconWidget ?? Icon(icon, color: iconColor, size: 21),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: TextStyle(
                    color: t.ink,
                    fontSize: 13.5,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 2),
                Text(subtitle, style: TextStyle(color: t.muted, fontSize: 11)),
              ],
            ),
          ),
          Icon(Icons.chevron_right, color: t.muted, size: 18),
        ],
      ),
    ),
  );

  Future<void> _openChat() => showDialog<void>(
    context: context,
    builder: (context) => const _SupportChatDialog(),
  );

  Future<void> _openWhatsApp() async {
    final uri = Uri.parse(
      'https://wa.me/5533991314125?text=${Uri.encodeComponent('Olá! Preciso de ajuda com o Agenda Livre.')}',
    );
    final opened = await launchUrl(uri, mode: LaunchMode.platformDefault);
    if (!mounted || opened) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Não foi possível abrir o WhatsApp.')),
    );
  }

  Widget _diagnostics(AgendaThemeTokens t, bool desktop) => _panel(
    t,
    padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 15),
    child: desktop
        ? Row(
            children: [
              _diagnosticIntro(t),
              const SizedBox(width: 20),
              Expanded(
                child: _diagnostic(t, Icons.wifi, 'Internet', 'Conectado'),
              ),
              Expanded(
                child: _diagnostic(
                  t,
                  Icons.sync,
                  'Sincronização',
                  widget.controller.isSyncing
                      ? 'Sincronizando'
                      : 'Sincronizado',
                ),
              ),
              Expanded(
                child: _diagnostic(
                  t,
                  Icons.system_update_outlined,
                  'Versão do aplicativo',
                  'Atualizado',
                ),
              ),
            ],
          )
        : Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _diagnosticIntro(t),
              const SizedBox(height: 12),
              _diagnostic(t, Icons.wifi, 'Internet', 'Conectado'),
              _diagnostic(
                t,
                Icons.sync,
                'Sincronização',
                widget.controller.isSyncing ? 'Sincronizando' : 'Sincronizado',
              ),
              _diagnostic(
                t,
                Icons.system_update_outlined,
                'Versão do aplicativo',
                'Atualizado',
              ),
            ],
          ),
  );

  Widget _diagnosticIntro(AgendaThemeTokens t) => Row(
    children: [
      Container(
        width: 34,
        height: 34,
        decoration: BoxDecoration(
          color: t.accentSoft,
          borderRadius: BorderRadius.circular(11),
        ),
        child: Icon(
          Icons.health_and_safety_outlined,
          color: t.accent,
          size: 17,
        ),
      ),
      const SizedBox(width: 9),
      Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Diagnóstico rápido',
            style: TextStyle(
              color: t.ink,
              fontSize: 13,
              fontWeight: FontWeight.w800,
            ),
          ),
          Text(
            'Status deste dispositivo',
            style: TextStyle(color: t.muted, fontSize: 10.5),
          ),
        ],
      ),
    ],
  );

  Widget _diagnostic(
    AgendaThemeTokens t,
    IconData icon,
    String title,
    String status,
  ) => Padding(
    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
    child: Row(
      children: [
        const SizedBox(
          width: 30,
          child: Icon(Icons.check_circle, color: Color(0xFF24A45A), size: 19),
        ),
        const SizedBox(width: 4),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: TextStyle(color: t.muted, fontSize: 10.5)),
              Text(
                status,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
      ],
    ),
  );

  Widget _panel(
    AgendaThemeTokens t, {
    required Widget child,
    required EdgeInsetsGeometry padding,
    bool clip = false,
  }) => Container(
    padding: padding,
    clipBehavior: clip ? Clip.antiAlias : Clip.none,
    decoration: BoxDecoration(
      color: Colors.white,
      border: Border.all(color: t.line),
      borderRadius: BorderRadius.circular(16),
      boxShadow: const [
        BoxShadow(
          color: Color(0x0D000000),
          blurRadius: 14,
          offset: Offset(0, 5),
        ),
      ],
    ),
    child: child,
  );
}

class _SupportChatDialog extends StatefulWidget {
  const _SupportChatDialog();

  @override
  State<_SupportChatDialog> createState() => _SupportChatDialogState();
}

class _SupportChatDialogState extends State<_SupportChatDialog> {
  final _controller = TextEditingController();
  final _messages = <String>[];

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: const Text('Chat no Agenda Livre'),
    content: SizedBox(
      width: 480,
      height: 340,
      child: Column(
        children: [
          const Align(
            alignment: Alignment.centerLeft,
            child: Text(
              'Olá! Conte para a gente como podemos ajudar.',
              style: TextStyle(fontSize: 13),
            ),
          ),
          const SizedBox(height: 12),
          Expanded(
            child: ListView.builder(
              itemCount: _messages.length,
              itemBuilder: (context, index) => Align(
                alignment: Alignment.centerRight,
                child: Container(
                  margin: const EdgeInsets.only(bottom: 8),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 9,
                  ),
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.primaryContainer,
                    borderRadius: BorderRadius.circular(14),
                  ),
                  child: Text(_messages[index]),
                ),
              ),
            ),
          ),
          Row(
            children: [
              Expanded(
                child: TextField(
                  key: const Key('support-chat-message'),
                  controller: _controller,
                  decoration: const InputDecoration(
                    hintText: 'Digite sua mensagem...',
                  ),
                  onSubmitted: (_) => _send(),
                ),
              ),
              IconButton(
                key: const Key('support-chat-send'),
                onPressed: _send,
                icon: const Icon(Icons.send),
              ),
            ],
          ),
        ],
      ),
    ),
    actions: [
      TextButton(
        onPressed: () => Navigator.pop(context),
        child: const Text('Fechar'),
      ),
    ],
  );

  void _send() {
    final value = _controller.text.trim();
    if (value.isEmpty) return;
    setState(() {
      _messages.add(value);
      _controller.clear();
    });
  }
}

class _SupportTopic {
  const _SupportTopic(this.title, this.subtitle, this.icon, this.guides);

  final String title;
  final String subtitle;
  final IconData icon;
  final List<String> guides;
}
