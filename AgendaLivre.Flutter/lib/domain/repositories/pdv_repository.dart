import '../pdv/pdv.dart';

abstract interface class PdvRepository {
  Future<bool> hasData();

  Future<PdvStore?> load();

  Future<PdvStore> loadOrCreate({
    required String storeId,
    required String terminalId,
  });

  Future<void> save(PdvStore store);

  Future<void> clear();
}
