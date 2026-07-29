import 'dart:io';

import 'package:image/image.dart' as image;

void main() {
  final source = File('assets/branding/agenda-livre-mark.png');
  final target = File('windows/runner/resources/app_icon.ico');
  final decoded = image.decodeImage(source.readAsBytesSync());
  if (decoded == null) {
    throw StateError('Não foi possível ler a marca do Agenda Livre.');
  }
  final square = image.copyResize(
    decoded,
    width: 256,
    height: 256,
    interpolation: image.Interpolation.cubic,
  );
  target.writeAsBytesSync(image.encodeIco(square));
}
