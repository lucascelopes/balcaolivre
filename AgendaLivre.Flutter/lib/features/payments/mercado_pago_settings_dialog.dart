import 'dart:async';

import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../app/agenda_controller.dart';
import '../../app/theme/agenda_theme.dart';
import '../../core/motion.dart';
import '../../core/ui.dart';
import '../../services/mercado_pago_service.dart';
import '../../services/oauth_browser_window.dart';
import 'mercado_pago_wpf_settings_layout.dart';

Future<bool> showMercadoPagoSettingsDialog(
  BuildContext context,
  AgendaController controller,
) async {
  return await showAgendaDialog<bool>(
        context: context,
        barrierDismissible: false,
        builder: (_) => _MercadoPagoSettingsDialog(controller: controller),
      ) ??
      false;
}

class _MercadoPagoSettingsDialog extends StatefulWidget {
  const _MercadoPagoSettingsDialog({required this.controller});

  final AgendaController controller;

  @override
  State<_MercadoPagoSettingsDialog> createState() =>
      _MercadoPagoSettingsDialogState();
}

class _MercadoPagoSettingsDialogState
    extends State<_MercadoPagoSettingsDialog> {
  bool _enabled = false;
  bool _busy = false;
  bool _polling = false;
  bool _showSetup = false;
  String _error = '';
  String _message = '';
  String? _selectedTerminalId;
  List<MercadoPagoTerminal> _terminals = const <MercadoPagoTerminal>[];
  int _pollGeneration = 0;

  MercadoPagoService? get _service => widget.controller.mercadoPagoService;

  @override
  void initState() {
    super.initState();
    final settings = widget.controller.data.settings;
    _enabled = settings.mercadoPagoEnabled;
    _selectedTerminalId = settings.mercadoPagoDefaultTerminalId.trim().isEmpty
        ? null
        : settings.mercadoPagoDefaultTerminalId.trim();
    WidgetsBinding.instance.addPostFrameCallback(
      (_) => _refresh(showBusy: false),
    );
  }

  @override
  void dispose() {
    _pollGeneration++;
    super.dispose();
  }

  Future<void> _refresh({bool showBusy = true}) async {
    final service = _service;
    if (service == null) {
      if (mounted) {
        setState(() {
          _error =
              'O serviço Mercado Pago não está disponível neste dispositivo.';
        });
      }
      return;
    }
    if (showBusy) {
      setState(() {
        _busy = true;
        _error = '';
        _message = '';
      });
    }
    try {
      final status = await service.fetchConnectionStatus();
      if (!status.ok) {
        throw MercadoPagoException(
          MercadoPagoFailure.invalidResponse,
          status.message.isEmpty
              ? 'Não foi possível consultar a conta Mercado Pago.'
              : status.message,
          statusCode: status.statusCode,
        );
      }
      await _applyStatus(status);
      if (status.connected) await _loadTerminals(service);
      if (mounted && showBusy) {
        setState(() {
          _message = status.connected
              ? 'Conta Mercado Pago conferida.'
              : 'Conta ainda não conectada.';
        });
      }
    } on Object catch (error) {
      if (mounted) setState(() => _error = _messageFor(error));
    } finally {
      if (mounted && showBusy) setState(() => _busy = false);
    }
  }

  Future<void> _applyStatus(MercadoPagoConnectionStatusResult status) async {
    await widget.controller.updateSettings((settings) {
      settings.mercadoPagoConnected = status.connected;
      settings.mercadoPagoSellerUserId = status.sellerUserId;
      settings.mercadoPagoLastError = status.lastError;
      settings.mercadoPagoLastSyncAt = status.lastSyncAt ?? DateTime.now();
      if (status.selectedTerminalId.trim().isNotEmpty) {
        settings.mercadoPagoDefaultTerminalId = status.selectedTerminalId
            .trim();
        settings.mercadoPagoDefaultTerminalLabel =
            status.selectedTerminalLabel.trim().isEmpty
            ? status.selectedTerminalId.trim()
            : status.selectedTerminalLabel.trim();
      }
    });
    if (!mounted) return;
    final selected = status.selectedTerminalId.trim();
    setState(() {
      if (selected.isNotEmpty) _selectedTerminalId = selected;
    });
  }

  Future<void> _loadTerminals(MercadoPagoService service) async {
    final result = await service.fetchTerminals();
    if (!result.ok) {
      throw MercadoPagoException(
        MercadoPagoFailure.invalidResponse,
        result.message.isEmpty
            ? 'Não foi possível buscar as maquininhas Point.'
            : result.message,
        statusCode: result.statusCode,
      );
    }
    final available = result.terminals
        .where((terminal) => terminal.id.trim().isNotEmpty)
        .toList(growable: false);
    final remoteSelected = result.selectedTerminalId.trim();
    var selected = remoteSelected.isEmpty
        ? _selectedTerminalId
        : remoteSelected;
    if (selected != null &&
        !available.any((terminal) => terminal.id == selected)) {
      selected = null;
    }
    selected ??= available.isEmpty ? null : available.first.id;
    if (!mounted) return;
    setState(() {
      _terminals = available;
      _selectedTerminalId = selected;
    });
  }

  Future<void> _connect() async {
    final service = _service;
    if (service == null || _busy) return;
    // On Web the popup must be created synchronously from the user's click.
    // Opening it only after the network request makes browsers block OAuth.
    final oauthWindow = openAgendaOAuthBrowserWindow();
    setState(() {
      _busy = true;
      _enabled = true;
      _error = '';
      _message = '';
    });
    try {
      await widget.controller.updateSettings(
        (settings) => settings.mercadoPagoEnabled = true,
      );
      final result = await service.startConnect();
      final authUrl = result.authUrl;
      if (!result.ok || authUrl == null) {
        throw MercadoPagoException(
          MercadoPagoFailure.invalidResponse,
          result.message.isEmpty
              ? 'Não foi possível iniciar a conexão com o Mercado Pago.'
              : result.message,
          statusCode: result.statusCode,
        );
      }
      final opened =
          oauthWindow?.navigate(authUrl) ??
          await launchUrl(authUrl, mode: LaunchMode.externalApplication);
      if (!opened) {
        oauthWindow?.close();
        throw const MercadoPagoException(
          MercadoPagoFailure.invalidConfiguration,
          'Não foi possível abrir a autorização. Libere pop-ups para este site e tente novamente.',
        );
      }
      if (!mounted) return;
      setState(() {
        _message =
            'Autorize a conta no navegador. Esta tela atualizará automaticamente.';
      });
      unawaited(_pollConnection());
    } on Object catch (error) {
      oauthWindow?.close();
      if (mounted) setState(() => _error = _messageFor(error));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _pollConnection() async {
    final generation = ++_pollGeneration;
    if (mounted) setState(() => _polling = true);
    for (var attempt = 0; attempt < 30; attempt++) {
      await Future<void>.delayed(const Duration(seconds: 3));
      if (!mounted || generation != _pollGeneration) return;
      await _refresh(showBusy: false);
      if (!mounted || generation != _pollGeneration) return;
      if (widget.controller.data.settings.mercadoPagoConnected) {
        setState(() {
          _polling = false;
          _message =
              'Conta conectada. Agora escolha a maquininha Point da loja.';
        });
        return;
      }
    }
    if (mounted && generation == _pollGeneration) {
      setState(() {
        _polling = false;
        _message =
            'Se já autorizou a conta, use “Checar conta” para atualizar.';
      });
    }
  }

  Future<void> _save() async {
    final service = _service;
    if (service == null || _busy) return;
    final settings = widget.controller.data.settings;
    setState(() {
      _busy = true;
      _error = '';
      _message = '';
    });
    try {
      if (!_enabled) {
        if (settings.mercadoPagoConnected &&
            settings.mercadoPagoDefaultTerminalId.trim().isNotEmpty) {
          final released = await service.releaseTerminal();
          if (!released.ok) {
            throw MercadoPagoException(
              MercadoPagoFailure.invalidResponse,
              released.message.isEmpty
                  ? 'Não foi possível liberar a maquininha Point.'
                  : released.message,
              statusCode: released.statusCode,
            );
          }
        }
        await widget.controller.updateSettings((value) {
          value.mercadoPagoEnabled = false;
          value.mercadoPagoDefaultTerminalId = '';
          value.mercadoPagoDefaultTerminalLabel = '';
          value.mercadoPagoLastError = '';
          value.mercadoPagoLastSyncAt = DateTime.now();
        });
        if (mounted) Navigator.of(context).pop(true);
        return;
      }
      if (!settings.mercadoPagoConnected) {
        throw const MercadoPagoException(
          MercadoPagoFailure.validation,
          'Conecte a conta Mercado Pago antes de salvar.',
        );
      }
      final terminalId = _selectedTerminalId?.trim() ?? '';
      if (terminalId.isEmpty) {
        throw const MercadoPagoException(
          MercadoPagoFailure.validation,
          'Escolha a maquininha Point da loja.',
        );
      }
      final terminal = _terminals.firstWhere(
        (item) => item.id == terminalId,
        orElse: () => MercadoPagoTerminal(id: terminalId),
      );
      final result = await service.selectTerminal(
        terminalId: terminal.id,
        terminalLabel: terminal.display,
      );
      if (!result.ok) {
        throw MercadoPagoException(
          MercadoPagoFailure.invalidResponse,
          result.message.isEmpty
              ? 'Não foi possível configurar a Point em modo PDV.'
              : result.message,
          statusCode: result.statusCode,
        );
      }
      await widget.controller.updateSettings((value) {
        value.mercadoPagoEnabled = true;
        value.mercadoPagoConnected = true;
        value.mercadoPagoDefaultTerminalId = terminal.id;
        value.mercadoPagoDefaultTerminalLabel = terminal.display;
        value.mercadoPagoLastError = '';
        value.mercadoPagoLastSyncAt = DateTime.now();
      });
      if (mounted) Navigator.of(context).pop(true);
    } on Object catch (error) {
      if (mounted) {
        setState(() {
          _error = _messageFor(error);
          _busy = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    final settings = widget.controller.data.settings;
    final compact = MediaQuery.sizeOf(context).width < 700;
    final connected = settings.mercadoPagoConnected;
    final terminalId = settings.mercadoPagoDefaultTerminalId.trim();
    final ready = _enabled && connected && terminalId.isNotEmpty;
    final statusLabel = !_enabled
        ? 'Desativado'
        : !connected
        ? 'Ativado, falta conectar'
        : terminalId.isEmpty
        ? 'Conectado, sem Point'
        : 'Maquininha pronta';
    final statusColor = ready
        ? const Color(0xFF16A34A)
        : _enabled
        ? const Color(0xFFD97706)
        : t.muted;

    if (MediaQuery.sizeOf(context).height >= 360) {
      return MercadoPagoWpfSettingsLayout(
        enabled: _enabled,
        busy: _busy,
        polling: _polling,
        connected: connected,
        ready: ready,
        showSetup: _showSetup,
        businessName: settings.businessName,
        terminalId: terminalId,
        terminalLabel: settings.mercadoPagoDefaultTerminalLabel,
        terminals: _terminals,
        selectedTerminalId: _selectedTerminalId,
        error: _error,
        message: _message,
        onEnabledChanged: (value) => setState(() => _enabled = value),
        onConnect: _connect,
        onRefresh: () => _refresh(),
        onTerminalChanged: (value) =>
            setState(() => _selectedTerminalId = value),
        onChangeTerminal: () => setState(() => _showSetup = true),
        onSave: _save,
        onClose: () => Navigator.of(context).pop(false),
      );
    }

    return Dialog(
      insetPadding: EdgeInsets.all(compact ? 8 : 24),
      backgroundColor: Colors.transparent,
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: 880,
          maxHeight: MediaQuery.sizeOf(context).height * .92,
        ),
        child: Material(
          color: t.panel,
          elevation: 18,
          shadowColor: Colors.black.withValues(alpha: .18),
          clipBehavior: Clip.antiAlias,
          borderRadius: BorderRadius.circular(compact ? 18 : 22),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              _DialogHeader(
                compact: compact,
                onClose: _busy ? null : () => Navigator.of(context).pop(false),
              ),
              Divider(height: 1, color: t.line),
              Flexible(
                child: SingleChildScrollView(
                  padding: EdgeInsets.all(compact ? 14 : 22),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Container(
                        padding: EdgeInsets.all(compact ? 14 : 18),
                        decoration: BoxDecoration(
                          color: t.warmSoft,
                          border: Border.all(color: t.line),
                          borderRadius: BorderRadius.circular(16),
                        ),
                        child: LayoutBuilder(
                          builder: (context, constraints) {
                            final terminalName =
                                settings.mercadoPagoDefaultTerminalLabel.isEmpty
                                ? terminalId
                                : settings.mercadoPagoDefaultTerminalLabel;
                            final summary = Row(
                              children: [
                                AgendaIconBadge(
                                  Icons.credit_card_rounded,
                                  size: 46,
                                  iconSize: 23,
                                  color: t.ink,
                                  background: const Color(0xFFEEF1F4),
                                ),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        'Mercado Pago na agenda',
                                        style: TextStyle(
                                          color: t.ink,
                                          fontSize: 17,
                                          fontWeight: FontWeight.w800,
                                        ),
                                      ),
                                      const SizedBox(height: 3),
                                      Text(
                                        ready
                                            ? 'Pronto para cobrar em $terminalName.'
                                            : 'Conecte a conta e escolha a Point que receberá crédito e débito.',
                                        style: TextStyle(
                                          color: t.muted,
                                          fontSize: 12,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ],
                            );
                            final toggle = SwitchListTile.adaptive(
                              key: const Key('mercado-pago-enabled-switch'),
                              contentPadding: EdgeInsets.zero,
                              dense: true,
                              title: const Text(
                                'Usar Mercado Pago',
                                style: TextStyle(fontWeight: FontWeight.w700),
                              ),
                              subtitle: Text(
                                statusLabel,
                                style: TextStyle(
                                  color: statusColor,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              value: _enabled,
                              onChanged: _busy
                                  ? null
                                  : (value) => setState(() => _enabled = value),
                            );
                            if (constraints.maxWidth < 610) {
                              return Column(
                                crossAxisAlignment: CrossAxisAlignment.stretch,
                                children: [
                                  summary,
                                  const SizedBox(height: 10),
                                  toggle,
                                ],
                              );
                            }
                            return Row(
                              children: [
                                Expanded(flex: 3, child: summary),
                                const SizedBox(width: 24),
                                SizedBox(width: 230, child: toggle),
                              ],
                            );
                          },
                        ),
                      ),
                      const SizedBox(height: 14),
                      _SetupStageCard(
                        number: '1',
                        title: 'Conectar conta',
                        subtitle:
                            'Autorize a conta Mercado Pago da loja no navegador.',
                        complete: connected,
                        child: Wrap(
                          spacing: 8,
                          runSpacing: 8,
                          children: [
                            ElevatedButton.icon(
                              key: const Key('mercado-pago-connect'),
                              onPressed: _busy ? null : _connect,
                              icon: const Icon(Icons.link_rounded, size: 18),
                              label: Text(
                                connected
                                    ? 'Reconectar conta'
                                    : 'Conectar conta',
                              ),
                            ),
                            OutlinedButton.icon(
                              key: const Key('mercado-pago-refresh-status'),
                              onPressed: !_enabled || _busy
                                  ? null
                                  : () => _refresh(),
                              icon: _busy
                                  ? const SizedBox.square(
                                      dimension: 16,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                      ),
                                    )
                                  : const Icon(Icons.refresh_rounded, size: 18),
                              label: const Text('Checar conta'),
                            ),
                            if (_polling)
                              Padding(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 6,
                                  vertical: 10,
                                ),
                                child: Text(
                                  'Aguardando autorização…',
                                  style: TextStyle(
                                    color: t.muted,
                                    fontSize: 12,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 12),
                      _SetupStageCard(
                        number: '2',
                        title: 'Escolher maquininha Point',
                        subtitle:
                            'A Point selecionada será configurada em modo PDV.',
                        complete: ready,
                        child: LayoutBuilder(
                          builder: (context, constraints) {
                            final dropdownValue =
                                _selectedTerminalId != null &&
                                    _terminals.any(
                                      (terminal) =>
                                          terminal.id == _selectedTerminalId,
                                    )
                                ? _selectedTerminalId
                                : null;
                            final dropdown = DropdownButtonFormField<String>(
                              key: const Key('mercado-pago-terminal-select'),
                              initialValue: dropdownValue,
                              isExpanded: true,
                              decoration: const InputDecoration(
                                labelText: 'Maquininha da loja',
                                prefixIcon: Icon(Icons.point_of_sale_rounded),
                              ),
                              hint: const Text(
                                'Busque as Points da conta conectada',
                              ),
                              items: _terminals
                                  .map(
                                    (terminal) => DropdownMenuItem<String>(
                                      value: terminal.id,
                                      child: Text(
                                        terminal.display,
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                    ),
                                  )
                                  .toList(growable: false),
                              onChanged: !_enabled || !connected || _busy
                                  ? null
                                  : (value) => setState(
                                      () => _selectedTerminalId = value,
                                    ),
                            );
                            final refresh = OutlinedButton.icon(
                              onPressed: !_enabled || !connected || _busy
                                  ? null
                                  : () => _refresh(),
                              icon: const Icon(Icons.sync_rounded, size: 18),
                              label: const Text('Buscar Points'),
                            );
                            if (constraints.maxWidth < 540) {
                              return Column(
                                crossAxisAlignment: CrossAxisAlignment.stretch,
                                children: [
                                  dropdown,
                                  const SizedBox(height: 8),
                                  refresh,
                                ],
                              );
                            }
                            return Row(
                              children: [
                                Expanded(child: dropdown),
                                const SizedBox(width: 8),
                                refresh,
                              ],
                            );
                          },
                        ),
                      ),
                      if (_error.isNotEmpty || _message.isNotEmpty) ...[
                        const SizedBox(height: 12),
                        _FeedbackBox(
                          error: _error.isNotEmpty,
                          message: _error.isNotEmpty ? _error : _message,
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              Divider(height: 1, color: t.line),
              Padding(
                padding: EdgeInsets.all(compact ? 12 : 16),
                child: compact
                    ? Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          ElevatedButton.icon(
                            key: const Key('mercado-pago-save'),
                            onPressed: _busy ? null : _save,
                            icon: const Icon(Icons.check_rounded, size: 18),
                            label: const Text('Salvar configuração'),
                          ),
                          const SizedBox(height: 8),
                          OutlinedButton(
                            onPressed: _busy
                                ? null
                                : () => Navigator.of(context).pop(false),
                            child: const Text('Cancelar'),
                          ),
                        ],
                      )
                    : Row(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [
                          OutlinedButton(
                            onPressed: _busy
                                ? null
                                : () => Navigator.of(context).pop(false),
                            child: const Text('Cancelar'),
                          ),
                          const SizedBox(width: 8),
                          ElevatedButton.icon(
                            key: const Key('mercado-pago-save'),
                            onPressed: _busy ? null : _save,
                            icon: const Icon(Icons.check_rounded, size: 18),
                            label: const Text('Salvar configuração'),
                          ),
                        ],
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  static String _messageFor(Object error) {
    if (error is MercadoPagoException) return error.message;
    return 'Não foi possível concluir a operação Mercado Pago. Tente novamente.';
  }
}

class _DialogHeader extends StatelessWidget {
  const _DialogHeader({required this.compact, required this.onClose});

  final bool compact;
  final VoidCallback? onClose;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Padding(
      padding: EdgeInsets.fromLTRB(
        compact ? 14 : 22,
        compact ? 13 : 17,
        compact ? 8 : 12,
        compact ? 13 : 17,
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Mercado Pago',
                  style: TextStyle(
                    color: t.ink,
                    fontSize: compact ? 21 : 24,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  'Conecte a conta e prepare a maquininha da loja.',
                  style: TextStyle(color: t.muted, fontSize: 12),
                ),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Fechar',
            onPressed: onClose,
            icon: const Icon(Icons.close_rounded),
          ),
        ],
      ),
    );
  }
}

class _SetupStageCard extends StatelessWidget {
  const _SetupStageCard({
    required this.number,
    required this.title,
    required this.subtitle,
    required this.complete,
    required this.child,
  });

  final String number;
  final String title;
  final String subtitle;
  final bool complete;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return AgendaPanel(
      radius: 15,
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Container(
                width: 32,
                height: 32,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: complete ? const Color(0xFFDCFCE7) : t.accentSoft,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: complete
                    ? const Icon(
                        Icons.check_rounded,
                        size: 19,
                        color: Color(0xFF15803D),
                      )
                    : Text(
                        number,
                        style: TextStyle(
                          color: t.accentDark,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
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
                        fontSize: 15,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    Text(
                      subtitle,
                      style: TextStyle(color: t.muted, fontSize: 11.5),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          child,
        ],
      ),
    );
  }
}

class _FeedbackBox extends StatelessWidget {
  const _FeedbackBox({required this.error, required this.message});

  final bool error;
  final String message;

  @override
  Widget build(BuildContext context) {
    final color = error ? const Color(0xFFB91C1C) : const Color(0xFF166534);
    final background = error
        ? const Color(0xFFFEF2F2)
        : const Color(0xFFF0FDF4);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 13, vertical: 11),
      decoration: BoxDecoration(
        color: background,
        border: Border.all(color: color.withValues(alpha: .28)),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            error ? Icons.error_outline_rounded : Icons.check_circle_outline,
            color: color,
            size: 19,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              message,
              style: TextStyle(
                color: color,
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
