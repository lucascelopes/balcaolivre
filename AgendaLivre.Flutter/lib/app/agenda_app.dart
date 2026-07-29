import 'dart:async';
import 'dart:ui' show PointerDeviceKind;

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';

import '../features/onboarding/onboarding_page.dart';
import 'agenda_controller.dart';
import 'responsive_shell.dart';
import 'theme/agenda_theme.dart';

class AgendaLivreApp extends StatefulWidget {
  const AgendaLivreApp({super.key, required this.controller, this.homeBuilder});

  final AgendaController controller;
  final Widget Function(BuildContext context, Widget child)? homeBuilder;

  @override
  State<AgendaLivreApp> createState() => _AgendaLivreAppState();
}

class _AgendaLivreAppState extends State<AgendaLivreApp>
    with WidgetsBindingObserver {
  static const _cloudRefreshInterval = Duration(seconds: 10);

  Timer? _cloudRefreshTimer;
  bool _appIsActive = true;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    if (widget.controller.loading) {
      unawaited(widget.controller.initialize());
    }
    _cloudRefreshTimer = Timer.periodic(
      _cloudRefreshInterval,
      (_) => _refreshCloudIfActive(),
    );
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    _appIsActive = state == AppLifecycleState.resumed;
    if (_appIsActive) _refreshCloudIfActive();
  }

  void _refreshCloudIfActive() {
    if (!_appIsActive) return;
    unawaited(widget.controller.refreshRemoteIfSafe());
  }

  @override
  void dispose() {
    _cloudRefreshTimer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        final controller = widget.controller;
        final theme = AgendaThemes.byId(controller.data.settings.themeId);
        return MaterialApp(
          title: 'Agenda Livre',
          debugShowCheckedModeBanner: false,
          theme: theme.toThemeData(),
          locale: const Locale('pt', 'BR'),
          supportedLocales: const [Locale('pt', 'BR')],
          localizationsDelegates: const [
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          scrollBehavior: const MaterialScrollBehavior().copyWith(
            scrollbars: true,
            dragDevices: const {
              PointerDeviceKind.touch,
              PointerDeviceKind.mouse,
              PointerDeviceKind.trackpad,
            },
          ),
          home: Builder(
            builder: (context) {
              final home = _home(controller);
              return widget.homeBuilder?.call(context, home) ?? home;
            },
          ),
        );
      },
    );
  }

  Widget _home(AgendaController controller) {
    if (controller.loading) return const _AgendaSplash();
    if (controller.loadError != null) {
      return _AgendaLoadError(
        message: controller.loadError!,
        onRetry: controller.initialize,
      );
    }
    if (controller.needsOnboarding) {
      return OnboardingPage(controller: controller);
    }
    return ResponsiveAgendaShell(controller: controller);
  }
}

class _AgendaSplash extends StatelessWidget {
  const _AgendaSplash();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Image.asset(
          'assets/branding/agenda-livre-mark.png',
          key: const Key('agenda-splash-logo'),
          width: 190,
          height: 92,
          fit: BoxFit.contain,
          filterQuality: FilterQuality.high,
          semanticLabel: 'Agenda Livre',
        ),
      ),
    );
  }
}

class _AgendaLoadError extends StatelessWidget {
  const _AgendaLoadError({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final t = AgendaThemeTokens.of(context);
    return Scaffold(
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.error_outline_rounded, color: t.accent, size: 52),
              const SizedBox(height: 16),
              Text(
                'Não foi possível abrir o Agenda Livre',
                textAlign: TextAlign.center,
                style: TextStyle(
                  color: t.ink,
                  fontSize: 20,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                message,
                textAlign: TextAlign.center,
                style: TextStyle(color: t.muted),
              ),
              const SizedBox(height: 20),
              ElevatedButton.icon(
                onPressed: onRetry,
                icon: const Icon(Icons.refresh),
                label: const Text('Tentar novamente'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
