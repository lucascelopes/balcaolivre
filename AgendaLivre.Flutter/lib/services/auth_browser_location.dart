import 'auth_browser_location_stub.dart'
    if (dart.library.html) 'auth_browser_location_web.dart'
    as platform;

Uri get currentAgendaAuthBrowserUri => platform.currentAgendaAuthBrowserUri;

void replaceAgendaAuthBrowserUri(Uri uri) =>
    platform.replaceAgendaAuthBrowserUri(uri);
