// ignore_for_file: avoid_web_libraries_in_flutter, deprecated_member_use

import 'dart:html' as html;

class AgendaOAuthBrowserWindow {
  AgendaOAuthBrowserWindow(this._window);

  final html.WindowBase _window;

  bool navigate(Uri uri) {
    try {
      _window.location.href = uri.toString();
      return true;
    } on Object {
      return false;
    }
  }

  void close() {
    try {
      _window.close();
    } on Object {
      // The user may already have closed the authorization window.
    }
  }
}

AgendaOAuthBrowserWindow? openAgendaOAuthBrowserWindow() {
  try {
    final popup = html.window.open(
      'about:blank',
      'agenda_livre_mercado_pago_oauth',
      'popup=yes,width=720,height=760,resizable=yes,scrollbars=yes',
    );
    return AgendaOAuthBrowserWindow(popup);
  } on Object {
    return null;
  }
}
