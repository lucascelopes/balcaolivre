// ignore_for_file: avoid_web_libraries_in_flutter, deprecated_member_use

import 'dart:html' as html;

Uri get currentAgendaAuthBrowserUri => Uri.parse(html.window.location.href);

void replaceAgendaAuthBrowserUri(Uri uri) {
  html.window.history.replaceState(null, '', uri.toString());
}
