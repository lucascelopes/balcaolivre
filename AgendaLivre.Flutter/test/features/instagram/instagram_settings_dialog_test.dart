import 'dart:io';

import 'package:agenda_livre/app/agenda_controller.dart';
import 'package:agenda_livre/app/theme/agenda_theme.dart';
import 'package:agenda_livre/domain/models/models.dart';
import 'package:agenda_livre/domain/repositories/agenda_repository.dart';
import 'package:agenda_livre/features/instagram/instagram_settings_dialog.dart';
import 'package:agenda_livre/services/http_transport.dart';
import 'package:agenda_livre/services/instagram_service.dart';
import 'package:agenda_livre/services/oauth_browser_window.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../services/fake_http_transport.dart';

void main() {
  setUpAll(() async {
    await _loadFont('Segoe UI', r'C:\Windows\Fonts\segoeui.ttf');
    await _loadFont('Ahem', r'C:\Windows\Fonts\segoeui.ttf');
    await _loadFont(
      'MaterialIcons',
      r'C:\src\flutter\bin\cache\artifacts\material_fonts\materialicons-regular.otf',
    );
    Directory('artifacts/audit-current-2026-07-23').createSync(recursive: true);
  });

  testWidgets('connects a first Instagram account on mobile through OAuth', (
    tester,
  ) async {
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/oauth/start')) {
        return const ServiceHttpResponse(
          statusCode: 200,
          body:
              '{"ok":true,"authorizationUrl":"https://www.instagram.com/oauth/authorize"}',
        );
      }
      return const ServiceHttpResponse(
        statusCode: 200,
        body:
            '{"ok":true,"connected":false,"status":"NAO_CONECTADO","message":"Instagram nao conectado."}',
      );
    });
    final controller = _controller(transport);
    final oauthWindow = _FakeOAuthWindow();
    await _pumpLauncher(
      tester,
      controller,
      const Size(390, 844),
      oauthWindow: oauthWindow,
    );

    await tester.tap(find.byKey(const Key('open-instagram')));
    await tester.pumpAndSettle();

    expect(find.text('Instagram profissional'), findsOneWidget);
    final connect = tester.widget<FilledButton>(
      find.byKey(const Key('instagram-connect')),
    );
    expect(connect.onPressed, isNotNull);

    await tester.tap(find.byKey(const Key('instagram-connect')));
    await tester.pumpAndSettle();

    expect(oauthWindow.navigated?.host, 'www.instagram.com');
    expect(controller.data.settings.instagramEnabled, isTrue);
    expect(controller.data.settings.instagramState, 'aguardando_oauth');
    expect(
      transport.requests.any(
        (request) => request.uri.path.endsWith('/oauth/start'),
      ),
      isTrue,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('shows Direct and sends a reply on desktop', (tester) async {
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/status')) {
        return const ServiceHttpResponse(
          statusCode: 200,
          body:
              '{"ok":true,"connected":true,"username":"agenda.livre","displayName":"Agenda Livre","instagramUserId":"ig-42","status":"ATIVO"}',
        );
      }
      if (request.uri.path.endsWith('/messages/send')) {
        return const ServiceHttpResponse(
          statusCode: 200,
          body:
              '{"ok":true,"message":"Mensagem enviada.","remoteMessageId":"remote-2"}',
        );
      }
      return const ServiceHttpResponse(
        statusCode: 200,
        body:
            '{"ok":true,"messages":[{"id":"m1","instagramScopedId":"customer-1","senderName":"Nina","senderUsername":"nina.beauty","text":"Tem horário hoje?","direction":"entrada","createdAt":"2026-07-23T15:00:00Z","status":"recebida"}]}',
      );
    });
    final controller = _controller(transport);
    controller.data.settings.instagramLinked = true;
    await _pumpLauncher(
      tester,
      controller,
      const Size(1200, 800),
      oauthWindow: _FakeOAuthWindow(),
    );

    await tester.tap(find.byKey(const Key('open-instagram')));
    await tester.pumpAndSettle();

    expect(find.text('Conectado: @agenda.livre'), findsOneWidget);
    expect(find.byKey(const Key('instagram-direct-list')), findsOneWidget);
    expect(find.text('Nina'), findsOneWidget);

    await tester.enterText(
      find.byKey(const Key('instagram-reply-field')),
      'Sim, temos às 16h.',
    );
    await tester.tap(find.byKey(const Key('instagram-send-reply')));
    await tester.pumpAndSettle();

    expect(find.text('Resposta enviada pelo Instagram.'), findsOneWidget);
    expect(
      transport.requests.any(
        (request) => request.uri.path.endsWith('/messages/send'),
      ),
      isTrue,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('captures the disconnected mobile layout', (tester) async {
    final transport = FakeHttpTransport(
      (_) => const ServiceHttpResponse(
        statusCode: 200,
        body:
            '{"ok":true,"connected":false,"status":"NAO_CONECTADO","message":"Instagram nao conectado."}',
      ),
    );
    await _pumpLauncher(
      tester,
      _controller(transport),
      const Size(390, 844),
      oauthWindow: _FakeOAuthWindow(),
    );

    await tester.tap(find.byKey(const Key('open-instagram')));
    await tester.pumpAndSettle();

    await expectLater(
      find.byKey(const Key('instagram-settings-dialog')),
      matchesGoldenFile(
        '../../../artifacts/audit-current-2026-07-23/'
        '03-flutter-instagram-mobile-connect.png',
      ),
    );
  });

  testWidgets('captures the connected desktop Direct layout', (tester) async {
    final transport = FakeHttpTransport((request) {
      if (request.uri.path.endsWith('/status')) {
        return const ServiceHttpResponse(
          statusCode: 200,
          body:
              '{"ok":true,"connected":true,"username":"agenda.livre","displayName":"Agenda Livre","instagramUserId":"ig-42","status":"ATIVO"}',
        );
      }
      return const ServiceHttpResponse(
        statusCode: 200,
        body:
            '{"ok":true,"messages":[{"id":"m1","instagramScopedId":"customer-1","senderName":"Nina","senderUsername":"nina.beauty","text":"Tem horário hoje?","direction":"entrada","createdAt":"2026-07-23T15:00:00Z","status":"recebida"},{"id":"m2","instagramScopedId":"customer-1","senderName":"Agenda Livre","senderUsername":"agenda.livre","text":"Sim, temos às 16h.","direction":"saida","createdAt":"2026-07-23T15:02:00Z","status":"enviada"}]}',
      );
    });
    final controller = _controller(transport);
    controller.data.settings.instagramLinked = true;
    await _pumpLauncher(
      tester,
      controller,
      const Size(1200, 800),
      oauthWindow: _FakeOAuthWindow(),
    );

    await tester.tap(find.byKey(const Key('open-instagram')));
    await tester.pumpAndSettle();

    await expectLater(
      find.byKey(const Key('instagram-settings-dialog')),
      matchesGoldenFile(
        '../../../artifacts/audit-current-2026-07-23/'
        '04-flutter-instagram-desktop-connected.png',
      ),
    );
  });
}

Future<void> _loadFont(String family, String path) async {
  final loader = FontLoader(family);
  final bytes = await File(path).readAsBytes();
  loader.addFont(Future<ByteData>.value(ByteData.sublistView(bytes)));
  await loader.load();
}

AgendaController _controller(FakeHttpTransport transport) {
  final service = InstagramService(
    transport: transport,
    config: InstagramServiceConfig(
      baseUri: Uri.parse('https://api.example/functions/v1/instagram'),
      contextProvider: () => const InstagramClientContext(
        licenseKey: 'BLV-TEST',
        machineHash: 'A1B2C3D4',
        machineCode: 'A1B2C3D4',
      ),
    ),
  );
  return AgendaController(_MemoryAgendaRepository(), instagramService: service)
    ..data = AgendaData()
    ..loading = false;
}

Future<void> _pumpLauncher(
  WidgetTester tester,
  AgendaController controller,
  Size size, {
  required _FakeOAuthWindow oauthWindow,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(
    MaterialApp(
      theme: AgendaThemes.byId('aesthetic-coral').toThemeData(),
      home: Scaffold(
        body: Builder(
          builder: (context) => Center(
            child: ElevatedButton(
              key: const Key('open-instagram'),
              onPressed: () => showInstagramSettingsDialog(
                context,
                controller,
                launchAuthorization: (_) async => true,
                openOAuthWindow: () => oauthWindow,
                pollInterval: Duration.zero,
                pollAttempts: 1,
              ),
              child: const Text('Abrir'),
            ),
          ),
        ),
      ),
    ),
  );
}

class _FakeOAuthWindow extends AgendaOAuthBrowserWindow {
  Uri? navigated;

  @override
  bool navigate(Uri uri) {
    navigated = uri;
    return true;
  }
}

class _MemoryAgendaRepository implements AgendaRepository {
  AgendaData? value;

  @override
  Future<void> clear() async => value = null;

  @override
  Future<bool> hasData() async => value != null;

  @override
  Future<AgendaData?> load() async => value;

  @override
  Future<AgendaData> loadOrCreate() async => value ?? AgendaData();

  @override
  Future<void> save(AgendaData data) async => value = data;
}
