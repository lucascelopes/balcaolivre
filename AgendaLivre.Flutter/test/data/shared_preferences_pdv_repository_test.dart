import 'package:agenda_livre/data/repositories/shared_preferences_pdv_repository.dart';
import 'package:agenda_livre/domain/pdv/pdv.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues(<String, Object>{});
  });

  test('cria, salva e recarrega o PDV offline', () async {
    final preferences = await SharedPreferences.getInstance();
    final repository = SharedPreferencesPdvRepository(preferences);
    final store = await repository.loadOrCreate(
      storeId: 'store',
      terminalId: 'terminal',
    );
    store
        .openTicket(boardNumber: '000001')
        .addItem(
          PdvTicketItem(
            id: 'line',
            productId: 'product',
            code: '000001',
            name: 'Produto',
            unitPrice: 10,
          ),
        );
    await repository.save(store);

    final loaded = await repository.load();

    expect(loaded?.storeId, 'store');
    expect(loaded?.terminalId, 'terminal');
    expect(loaded?.tickets.single.total, 11);
    expect(loaded?.syncQueue.single.type, 'ticket_opened');
  });

  test('recupera o backup quando o JSON principal está corrompido', () async {
    final preferences = await SharedPreferences.getInstance();
    final repository = SharedPreferencesPdvRepository(preferences);
    final first = PdvStore(storeId: 'first', terminalId: 'terminal');
    await repository.save(first);
    final second = PdvStore(storeId: 'second', terminalId: 'terminal');
    await repository.save(second);
    await preferences.setString(repository.storageKey, '{invalid');

    final recovered = await repository.load();

    expect(recovered?.storeId, 'first');
    expect(preferences.getString(repository.storageKey), isNot('{invalid'));
  });
}
