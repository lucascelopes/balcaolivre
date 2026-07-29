import 'dart:async';
import 'dart:ui' show PointerDeviceKind;

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';

import '../features/auth/agenda_auth_page.dart';
import '../features/subscription/agenda_subscription_pages.dart';
import 'agenda_app.dart';
import 'theme/agenda_theme.dart';
import 'web_agenda_session.dart';

class AgendaLivreWebRoot extends StatefulWidget {
  const AgendaLivreWebRoot({
    super.key,
    required this.session,
    this.autoInitialize = true,
  });

  final AgendaWebSession session;
  final bool autoInitialize;

  @override
  State<AgendaLivreWebRoot> createState() => _AgendaLivreWebRootState();
}

class _AgendaLivreWebRootState extends State<AgendaLivreWebRoot>
    with WidgetsBindingObserver {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    if (widget.autoInitialize) widget.session.initialize();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state != AppLifecycleState.resumed) return;
    final session = widget.session;
    if (session is AgendaWebSessionController) {
      unawaited(_reconcileAndRefresh(session));
      return;
    }
    final controller = widget.session.agendaController;
    if (controller != null) unawaited(controller.refreshRemoteIfSafe());
  }

  Future<void> _reconcileAndRefresh(AgendaWebSessionController session) async {
    await session.reconcilePersistedSession();
    await session.agendaController?.refreshRemoteIfSafe();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.session,
      builder: (context, _) {
        final managedSession = widget.session is AgendaWebSessionController
            ? widget.session as AgendaWebSessionController
            : null;
        if (managedSession?.checkoutActivation != null) {
          return MaterialApp(
            title: 'Ativar Agenda Livre',
            debugShowCheckedModeBanner: false,
            theme: AgendaThemes.byId('').toThemeData(),
            home: AgendaCheckoutActivationPage(session: managedSession!),
          );
        }
        if (managedSession?.localRenewalPreview == true) {
          return MaterialApp(
            title: 'Renovar Agenda Livre',
            debugShowCheckedModeBanner: false,
            theme: AgendaThemes.byId('').toThemeData(),
            home: AgendaSubscriptionRenewalPage(session: managedSession!),
          );
        }
        final controller = widget.session.agendaController;
        if (controller != null) {
          return AnimatedBuilder(
            animation: controller,
            builder: (context, _) {
              if (managedSession != null &&
                  controller.needsSubscriptionRenewal) {
                return MaterialApp(
                  title: 'Renovar Agenda Livre',
                  debugShowCheckedModeBanner: false,
                  theme: AgendaThemes.byId('').toThemeData(),
                  home: AgendaSubscriptionRenewalPage(session: managedSession),
                );
              }
              return AgendaLivreApp(controller: controller);
            },
          );
        }
        return MaterialApp(
          title: 'Agenda Livre',
          debugShowCheckedModeBanner: false,
          theme: AgendaThemes.byId('').toThemeData(),
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
          home: widget.session.initializing
              ? const _WebSessionSplash()
              : AgendaAuthPage(session: widget.session),
        );
      },
    );
  }
}

class _WebSessionSplash extends StatelessWidget {
  const _WebSessionSplash();

  @override
  Widget build(BuildContext context) {
    final tokens = AgendaThemeTokens.of(context);
    return Scaffold(
      backgroundColor: tokens.appBackground,
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Image.asset(
              'assets/branding/agenda-livre-mark.png',
              width: 168,
              height: 82,
              fit: BoxFit.contain,
              semanticLabel: 'Agenda Livre',
            ),
            const SizedBox(height: 18),
            SizedBox(
              width: 160,
              child: LinearProgressIndicator(
                color: tokens.accent,
                backgroundColor: tokens.accentSoft,
                borderRadius: BorderRadius.circular(20),
              ),
            ),
            const SizedBox(height: 10),
            Text(
              'Abrindo sua conta…',
              style: TextStyle(color: tokens.muted, fontSize: 13),
            ),
          ],
        ),
      ),
    );
  }
}
