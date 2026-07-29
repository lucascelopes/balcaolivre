import 'dart:async';

import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../services/instagram_service.dart';
import '../../services/oauth_browser_window.dart';

typedef InstagramUrlLauncher = Future<bool> Function(Uri uri);
typedef InstagramOAuthWindowFactory = AgendaOAuthBrowserWindow? Function();

Future<bool?> showInstagramSettingsDialog(
  BuildContext context,
  AgendaController controller, {
  InstagramUrlLauncher? launchAuthorization,
  InstagramOAuthWindowFactory? openOAuthWindow,
  Duration pollInterval = const Duration(seconds: 5),
  int pollAttempts = 24,
}) {
  return showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (_) => _InstagramSettingsDialog(
      controller: controller,
      launchAuthorization:
          launchAuthorization ??
          (uri) => launchUrl(uri, mode: LaunchMode.externalApplication),
      openOAuthWindow: openOAuthWindow ?? openAgendaOAuthBrowserWindow,
      pollInterval: pollInterval,
      pollAttempts: pollAttempts,
    ),
  );
}

class _InstagramSettingsDialog extends StatefulWidget {
  const _InstagramSettingsDialog({
    required this.controller,
    required this.launchAuthorization,
    required this.openOAuthWindow,
    required this.pollInterval,
    required this.pollAttempts,
  });

  final AgendaController controller;
  final InstagramUrlLauncher launchAuthorization;
  final InstagramOAuthWindowFactory openOAuthWindow;
  final Duration pollInterval;
  final int pollAttempts;

  @override
  State<_InstagramSettingsDialog> createState() =>
      _InstagramSettingsDialogState();
}

class _InstagramSettingsDialogState extends State<_InstagramSettingsDialog> {
  final _reply = TextEditingController();
  bool _busy = false;
  bool _polling = false;
  bool _connected = false;
  String _username = '';
  String _displayName = '';
  String _error = '';
  String _message = '';
  String _selectedRecipientId = '';
  List<InstagramMessage> _messages = const <InstagramMessage>[];
  int _pollGeneration = 0;

  InstagramService? get _service => widget.controller.instagramService;

  @override
  void initState() {
    super.initState();
    final settings = widget.controller.data.settings;
    _connected = settings.instagramLinked;
    _username = settings.instagramUsername.trim().replaceFirst(
      RegExp(r'^@+'),
      '',
    );
    _displayName = settings.instagramDisplayName.trim();
    unawaited(_refresh(silent: true));
  }

  @override
  void dispose() {
    _pollGeneration++;
    _reply.dispose();
    super.dispose();
  }

  Future<void> _refresh({bool silent = false}) async {
    final service = _service;
    if (service == null || _busy) {
      if (!silent && mounted) {
        setState(() {
          _error =
              'A integração do Instagram não está disponível nesta sessão.';
        });
      }
      return;
    }
    if (mounted) {
      setState(() {
        _busy = true;
        if (!silent) {
          _error = '';
          _message = '';
        }
      });
    }
    try {
      final status = await service.fetchStatus();
      if (!status.ok) {
        throw InstagramException(
          InstagramFailure.invalidResponse,
          status.message.isEmpty
              ? 'Não foi possível consultar o Instagram.'
              : status.message,
          statusCode: status.statusCode,
        );
      }
      await _applyStatus(status);
      if (status.connected) {
        await _loadMessages(service);
      } else if (mounted) {
        setState(() {
          _messages = const <InstagramMessage>[];
          _selectedRecipientId = '';
        });
      }
    } on Object catch (error) {
      if (mounted && !silent) {
        setState(() => _error = _messageFor(error));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _connect() async {
    final service = _service;
    if (service == null || _busy) return;
    // The popup must be created synchronously from the click on Flutter Web.
    final oauthWindow = widget.openOAuthWindow();
    setState(() {
      _busy = true;
      _error = '';
      _message = '';
    });
    try {
      final result = await service.startOAuth();
      final url = result.authorizationUrl;
      if (!result.ok ||
          url == null ||
          !InstagramService.isTrustedAuthorizationUrl(url)) {
        throw InstagramException(
          InstagramFailure.invalidResponse,
          result.message.isEmpty
              ? 'Não foi possível iniciar a conexão com o Instagram.'
              : result.message,
          statusCode: result.statusCode,
        );
      }
      final opened =
          oauthWindow?.navigate(url) ?? await widget.launchAuthorization(url);
      if (!opened) {
        oauthWindow?.close();
        throw const InstagramException(
          InstagramFailure.invalidConfiguration,
          'Não foi possível abrir a autorização. Libere pop-ups e tente novamente.',
        );
      }
      await widget.controller.updateSettings((settings) {
        settings.instagramEnabled = true;
        settings.instagramState = 'aguardando_oauth';
        settings.instagramLastError = '';
        settings.instagramLastCheckedAt = DateTime.now();
      });
      if (!mounted) return;
      setState(() {
        _message =
            'Autorize a conta profissional na janela aberta. O status será atualizado automaticamente.';
      });
      unawaited(_pollConnection());
    } on Object catch (error) {
      oauthWindow?.close();
      if (!mounted) return;
      final text = _messageFor(error);
      setState(() => _error = text);
      await widget.controller.updateSettings((settings) {
        settings.instagramState = 'erro';
        settings.instagramLastError = text;
        settings.instagramLastCheckedAt = DateTime.now();
      });
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _pollConnection() async {
    final service = _service;
    if (service == null || _polling) return;
    final generation = ++_pollGeneration;
    setState(() => _polling = true);
    try {
      for (var attempt = 0; attempt < widget.pollAttempts; attempt++) {
        await Future<void>.delayed(widget.pollInterval);
        if (!mounted || generation != _pollGeneration) return;
        try {
          final status = await service.fetchStatus();
          if (!status.ok || !status.connected) continue;
          await _applyStatus(status);
          await _loadMessages(service);
          if (!mounted) return;
          setState(() {
            _message =
                _accountLabel(status.username, status.displayName).isEmpty
                ? 'Instagram conectado.'
                : 'Instagram conectado: ${_accountLabel(status.username, status.displayName)}.';
          });
          return;
        } on Object {
          // A transient polling failure must not cancel the OAuth flow.
        }
      }
      if (mounted) {
        setState(() {
          _message =
              'A autorização ainda não apareceu. Use “Atualizar status” depois de concluir na Meta.';
        });
      }
    } finally {
      if (mounted && generation == _pollGeneration) {
        setState(() => _polling = false);
      }
    }
  }

  Future<void> _disconnect() async {
    final service = _service;
    if (service == null || _busy) return;
    setState(() {
      _busy = true;
      _error = '';
      _message = '';
    });
    try {
      final result = await service.disconnect();
      if (!result.ok) {
        throw InstagramException(
          InstagramFailure.invalidResponse,
          result.message.isEmpty
              ? 'Não foi possível desconectar o Instagram.'
              : result.message,
          statusCode: result.statusCode,
        );
      }
      _pollGeneration++;
      await widget.controller.updateSettings((settings) {
        settings.instagramLinked = false;
        settings.instagramUsername = '';
        settings.instagramDisplayName = '';
        settings.instagramAccountId = '';
        settings.instagramState = 'desconectado';
        settings.instagramLastError = '';
        settings.instagramLastCheckedAt = DateTime.now();
      });
      if (!mounted) return;
      setState(() {
        _connected = false;
        _username = '';
        _displayName = '';
        _messages = const <InstagramMessage>[];
        _selectedRecipientId = '';
        _message = 'Instagram desconectado do Agenda Livre.';
      });
    } on Object catch (error) {
      if (mounted) setState(() => _error = _messageFor(error));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _loadMessages(InstagramService service) async {
    final result = await service.fetchMessages();
    if (!result.ok) {
      throw InstagramException(
        InstagramFailure.invalidResponse,
        result.message.isEmpty
            ? 'Não foi possível carregar o Direct.'
            : result.message,
        statusCode: result.statusCode,
      );
    }
    final rows = [...result.messages]
      ..sort((a, b) => a.createdAt.compareTo(b.createdAt));
    await widget.controller.mergeInstagramMessages(rows);
    final inbound = rows.where((message) => message.inbound).toList();
    if (!mounted) return;
    setState(() {
      _messages = rows;
      if (_selectedRecipientId.isEmpty && inbound.isNotEmpty) {
        _selectedRecipientId = inbound.last.instagramScopedId;
      }
    });
  }

  Future<void> _sendReply() async {
    final service = _service;
    if (service == null || _busy) return;
    final text = _reply.text.trim();
    if (_selectedRecipientId.isEmpty || text.isEmpty) {
      setState(() {
        _error = _selectedRecipientId.isEmpty
            ? 'Selecione uma mensagem recebida do cliente.'
            : 'Digite a resposta antes de enviar.';
      });
      return;
    }
    setState(() {
      _busy = true;
      _error = '';
      _message = '';
    });
    try {
      final result = await service.sendMessage(
        recipientId: _selectedRecipientId,
        text: text,
        messageId: DateTime.now().microsecondsSinceEpoch.toString(),
      );
      if (!result.ok) {
        throw InstagramException(
          InstagramFailure.invalidResponse,
          result.message.isEmpty
              ? 'Não foi possível enviar a resposta.'
              : result.message,
          statusCode: result.statusCode,
        );
      }
      final now = DateTime.now();
      final outgoing = InstagramMessage(
        id: result.remoteMessageId,
        instagramScopedId: _selectedRecipientId,
        senderName: _displayName,
        senderUsername: _username,
        text: text,
        direction: 'saida',
        createdAt: now,
        status: 'enviado',
      );
      await widget.controller.mergeInstagramMessages(<InstagramMessage>[
        outgoing,
      ]);
      if (!mounted) return;
      setState(() {
        _messages = <InstagramMessage>[..._messages, outgoing];
        _reply.clear();
        _message = 'Resposta enviada pelo Instagram.';
      });
    } on Object catch (error) {
      if (mounted) setState(() => _error = _messageFor(error));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _applyStatus(InstagramStatusResult status) async {
    await widget.controller.updateSettings((settings) {
      settings.instagramEnabled = true;
      settings.instagramLinked = status.connected;
      settings.instagramUsername = status.username;
      settings.instagramDisplayName = status.displayName;
      settings.instagramAccountId = status.instagramUserId;
      settings.instagramState = status.status;
      settings.instagramLastError = status.connected ? '' : status.message;
      settings.instagramLastCheckedAt = DateTime.now();
      if (status.connected) {
        settings.instagramLinkedAt ??= DateTime.now();
      }
    });
    if (!mounted) return;
    setState(() {
      _connected = status.connected;
      _username = status.username;
      _displayName = status.displayName;
    });
  }

  @override
  Widget build(BuildContext context) {
    final size = MediaQuery.sizeOf(context);
    final compact = size.width < 700;
    final tokens = AgendaThemeTokens.of(context);
    return Dialog(
      key: const Key('instagram-settings-dialog'),
      insetPadding: EdgeInsets.all(compact ? 8 : 24),
      backgroundColor: Colors.transparent,
      child: Container(
        width: compact ? double.infinity : 860,
        constraints: BoxConstraints(
          maxHeight: compact ? size.height - 16 : 720,
        ),
        decoration: BoxDecoration(
          color: tokens.appBackground,
          borderRadius: BorderRadius.circular(compact ? 18 : 24),
          boxShadow: const [
            BoxShadow(
              color: Color(0x33000000),
              blurRadius: 34,
              offset: Offset(0, 16),
            ),
          ],
        ),
        clipBehavior: Clip.antiAlias,
        child: Column(
          children: [
            _header(tokens, compact),
            Expanded(
              child: SingleChildScrollView(
                padding: EdgeInsets.all(compact ? 16 : 22),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _statusCard(tokens, compact),
                    if (_error.isNotEmpty) ...[
                      const SizedBox(height: 12),
                      _notice(_error, error: true),
                    ],
                    if (_message.isNotEmpty) ...[
                      const SizedBox(height: 12),
                      _notice(_message),
                    ],
                    const SizedBox(height: 18),
                    if (_connected)
                      _directWorkspace(tokens, compact)
                    else
                      _connectionGuide(tokens, compact),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _header(AgendaThemeTokens tokens, bool compact) {
    return Container(
      color: const Color(0xFF231915),
      padding: EdgeInsets.fromLTRB(
        compact ? 16 : 22,
        compact ? 15 : 18,
        compact ? 10 : 14,
        compact ? 15 : 18,
      ),
      child: Row(
        children: [
          Container(
            width: 42,
            height: 42,
            decoration: BoxDecoration(
              color: const Color(0x33C13584),
              borderRadius: BorderRadius.circular(13),
            ),
            child: const Icon(
              Icons.camera_alt_outlined,
              color: Color(0xFFFF8BC4),
            ),
          ),
          const SizedBox(width: 12),
          const Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Instagram profissional',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 19,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                SizedBox(height: 2),
                Text(
                  'Conexão da Meta e mensagens do Direct',
                  style: TextStyle(color: Color(0xFFBFB4AF), fontSize: 12),
                ),
              ],
            ),
          ),
          IconButton(
            key: const Key('instagram-close'),
            onPressed: _busy ? null : () => Navigator.of(context).pop(false),
            tooltip: 'Fechar',
            icon: const Icon(Icons.close_rounded, color: Colors.white),
          ),
        ],
      ),
    );
  }

  Widget _statusCard(AgendaThemeTokens tokens, bool compact) {
    final account = _accountLabel(_username, _displayName);
    final statusColor = _connected
        ? const Color(0xFF15803D)
        : _polling
        ? const Color(0xFFD97706)
        : tokens.muted;
    final actions = Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        if (_connected)
          OutlinedButton.icon(
            key: const Key('instagram-disconnect'),
            onPressed: _busy ? null : _disconnect,
            icon: const Icon(Icons.link_off_rounded, size: 17),
            label: const Text('Desconectar'),
          )
        else
          FilledButton.icon(
            key: const Key('instagram-connect'),
            onPressed: _busy ? null : _connect,
            style: FilledButton.styleFrom(
              backgroundColor: const Color(0xFFC13584),
              foregroundColor: Colors.white,
            ),
            icon: const Icon(Icons.link_rounded, size: 17),
            label: Text(_polling ? 'Aguardando...' : 'Conectar'),
          ),
        OutlinedButton.icon(
          key: const Key('instagram-refresh'),
          onPressed: _busy ? null : () => _refresh(),
          icon: _busy
              ? const SizedBox.square(
                  dimension: 15,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Icon(Icons.refresh_rounded, size: 17),
          label: const Text('Atualizar status'),
        ),
      ],
    );
    return Container(
      padding: EdgeInsets.all(compact ? 16 : 18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: tokens.line),
      ),
      child: compact
          ? Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _statusIdentity(statusColor, account),
                const SizedBox(height: 14),
                actions,
              ],
            )
          : Row(
              children: [
                Expanded(child: _statusIdentity(statusColor, account)),
                const SizedBox(width: 16),
                actions,
              ],
            ),
    );
  }

  Widget _statusIdentity(Color statusColor, String account) {
    return Row(
      children: [
        Container(
          width: 10,
          height: 10,
          decoration: BoxDecoration(color: statusColor, shape: BoxShape.circle),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                _connected
                    ? account.isEmpty
                          ? 'Instagram conectado'
                          : 'Conectado: $account'
                    : _polling
                    ? 'Aguardando autorização'
                    : 'Não conectado',
                key: const Key('instagram-status'),
                style: TextStyle(
                  color: statusColor,
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 3),
              Text(
                _connected
                    ? 'Direct e respostas estão disponíveis no Agenda Livre.'
                    : 'Conecte uma conta profissional Business ou Creator.',
                style: const TextStyle(color: Color(0xFF6B625D), fontSize: 12),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _connectionGuide(AgendaThemeTokens tokens, bool compact) {
    final cards = <Widget>[
      _feature(
        Icons.verified_user_outlined,
        'OAuth oficial',
        'A autorização acontece diretamente na Meta.',
      ),
      _feature(
        Icons.forum_outlined,
        'Instagram Direct',
        'Leia mensagens iniciadas pelos clientes.',
      ),
      _feature(
        Icons.reply_rounded,
        'Respostas no prazo',
        'Responda conversas elegíveis sem sair da agenda.',
      ),
    ];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Conecte o Instagram ao mesmo fluxo do Windows',
          style: TextStyle(
            color: tokens.ink,
            fontSize: compact ? 20 : 23,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 6),
        Text(
          'Use uma conta profissional Business ou Creator. O Agenda Livre não recebe sua senha.',
          style: TextStyle(color: tokens.muted, height: 1.45),
        ),
        const SizedBox(height: 16),
        if (compact)
          ...cards.expand((card) => <Widget>[card, const SizedBox(height: 10)])
        else
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: cards
                .expand(
                  (card) => <Widget>[
                    Expanded(child: card),
                    if (!identical(card, cards.last)) const SizedBox(width: 10),
                  ],
                )
                .toList(),
          ),
      ],
    );
  }

  Widget _feature(IconData icon, String title, String detail) {
    return Container(
      padding: const EdgeInsets.all(15),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF7FB),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFF3D8E7)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: const Color(0xFFC13584), size: 23),
          const SizedBox(height: 10),
          Text(
            title,
            style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14),
          ),
          const SizedBox(height: 4),
          Text(
            detail,
            style: const TextStyle(
              color: Color(0xFF6B625D),
              fontSize: 12,
              height: 1.4,
            ),
          ),
        ],
      ),
    );
  }

  Widget _directWorkspace(AgendaThemeTokens tokens, bool compact) {
    final conversations = _messages
        .where((message) => message.inbound)
        .toList(growable: false);
    final list = _messageList(tokens, conversations);
    final reply = _replyPanel(tokens);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                'Instagram Direct',
                style: TextStyle(
                  color: tokens.ink,
                  fontSize: 20,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
            Text(
              '${_messages.length} mensagens',
              style: TextStyle(color: tokens.muted, fontSize: 12),
            ),
          ],
        ),
        const SizedBox(height: 12),
        if (compact)
          Column(children: [list, const SizedBox(height: 12), reply])
        else
          SizedBox(
            height: 350,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Expanded(flex: 12, child: list),
                const SizedBox(width: 12),
                Expanded(flex: 10, child: reply),
              ],
            ),
          ),
      ],
    );
  }

  Widget _messageList(
    AgendaThemeTokens tokens,
    List<InstagramMessage> conversations,
  ) {
    if (conversations.isEmpty) {
      return Container(
        key: const Key('instagram-direct-empty'),
        constraints: const BoxConstraints(minHeight: 190),
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(15),
          border: Border.all(color: tokens.line),
        ),
        child: const Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.mark_chat_unread_outlined, size: 34),
            SizedBox(height: 10),
            Text(
              'Nenhuma conversa recebida',
              textAlign: TextAlign.center,
              style: TextStyle(fontWeight: FontWeight.w800),
            ),
            SizedBox(height: 5),
            Text(
              'As mensagens iniciadas pelos clientes aparecerão aqui.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Color(0xFF6B625D), fontSize: 12),
            ),
          ],
        ),
      );
    }
    return Container(
      key: const Key('instagram-direct-list'),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: tokens.line),
      ),
      clipBehavior: Clip.antiAlias,
      child: ListView.separated(
        itemCount: conversations.length,
        separatorBuilder: (_, _) => Divider(height: 1, color: tokens.line),
        itemBuilder: (context, index) {
          final item = conversations[index];
          final selected = item.instagramScopedId == _selectedRecipientId;
          final sender = item.senderName.trim().isNotEmpty
              ? item.senderName.trim()
              : item.senderUsername.trim().isNotEmpty
              ? '@${item.senderUsername.trim()}'
              : 'Cliente do Instagram';
          return Material(
            color: selected ? const Color(0xFFFFF0F7) : Colors.transparent,
            child: ListTile(
              key: Key('instagram-conversation-${item.instagramScopedId}'),
              selected: selected,
              onTap: () =>
                  setState(() => _selectedRecipientId = item.instagramScopedId),
              leading: CircleAvatar(
                backgroundColor: const Color(0xFFF7D9E8),
                foregroundColor: const Color(0xFFC13584),
                child: Text(
                  sender.characters.first.toUpperCase(),
                  style: const TextStyle(fontWeight: FontWeight.w800),
                ),
              ),
              title: Text(
                sender,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              subtitle: Text(
                item.text,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _replyPanel(AgendaThemeTokens tokens) {
    final selectedMessages = _messages
        .where((message) => message.instagramScopedId == _selectedRecipientId)
        .toList(growable: false);
    return Container(
      key: const Key('instagram-reply-panel'),
      padding: const EdgeInsets.all(15),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: tokens.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'Resposta',
            style: TextStyle(fontWeight: FontWeight.w800, fontSize: 15),
          ),
          const SizedBox(height: 8),
          if (selectedMessages.isNotEmpty)
            Container(
              constraints: const BoxConstraints(maxHeight: 92),
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: const Color(0xFFF7F4F2),
                borderRadius: BorderRadius.circular(10),
              ),
              child: SingleChildScrollView(
                child: Text(
                  selectedMessages.last.text,
                  style: const TextStyle(fontSize: 12.5, height: 1.4),
                ),
              ),
            )
          else
            const Text(
              'Selecione uma conversa recebida.',
              style: TextStyle(color: Color(0xFF6B625D), fontSize: 12),
            ),
          const SizedBox(height: 10),
          TextField(
            key: const Key('instagram-reply-field'),
            controller: _reply,
            enabled: !_busy && _selectedRecipientId.isNotEmpty,
            minLines: 3,
            maxLines: 5,
            maxLength: 1000,
            decoration: const InputDecoration(
              hintText: 'Digite uma resposta para o cliente...',
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 8),
          FilledButton.icon(
            key: const Key('instagram-send-reply'),
            onPressed: _busy || _selectedRecipientId.isEmpty
                ? null
                : _sendReply,
            style: FilledButton.styleFrom(
              backgroundColor: const Color(0xFFC13584),
              foregroundColor: Colors.white,
            ),
            icon: const Icon(Icons.send_rounded, size: 17),
            label: const Text('Responder no Instagram'),
          ),
          const SizedBox(height: 8),
          Text(
            'A Meta permite responder somente dentro da janela válida da conversa.',
            style: TextStyle(color: tokens.muted, fontSize: 10.5),
          ),
        ],
      ),
    );
  }

  Widget _notice(String text, {bool error = false}) {
    final color = error ? const Color(0xFFB91C1C) : const Color(0xFF166534);
    final background = error
        ? const Color(0xFFFFF1F2)
        : const Color(0xFFECFDF3);
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(11),
      ),
      child: Text(
        text,
        style: TextStyle(color: color, fontWeight: FontWeight.w600),
      ),
    );
  }

  String _messageFor(Object error) {
    if (error is InstagramException) return error.message;
    return 'Não foi possível concluir a operação do Instagram.';
  }

  static String _accountLabel(String username, String displayName) {
    final cleanUsername = username.trim().replaceFirst(RegExp(r'^@+'), '');
    if (cleanUsername.isNotEmpty) return '@$cleanUsername';
    return displayName.trim();
  }
}
