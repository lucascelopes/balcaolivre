Uri get currentAgendaAuthBrowserUri => Uri.base;

void replaceAgendaAuthBrowserUri(Uri uri) {
  // Native builds and VM tests do not have a browser address bar to clean.
}
