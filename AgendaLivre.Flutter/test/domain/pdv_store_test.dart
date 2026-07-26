import 'package:agenda_livre/domain/pdv/pdv.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('PdvTicket', () {
    test('calcula o total exatamente como o PDV Web', () {
      final ticket = PdvTicket(
        coverValue: 5,
        servicePercent: 10,
        discountPercent: 20,
        items: <PdvTicketItem>[
          PdvTicketItem(
            id: 'item-1',
            productId: 'product-1',
            code: '000001',
            name: 'Produto 1',
            quantity: 2,
            unitPrice: 20,
          ),
          PdvTicketItem(
            id: 'item-2',
            productId: 'product-2',
            code: '000002',
            name: 'Produto 2',
            unitPrice: 10,
          ),
        ],
      );

      expect(ticket.subtotal, 50);
      expect(ticket.serviceValue, 5);
      expect(ticket.discountValue, 10);
      expect(ticket.total, 50);

      ticket.addPayment(
        PdvPayment(id: 'payment-1', method: PdvPaymentMethod.cash, amount: 60),
      );

      expect(ticket.paid, 60);
      expect(ticket.balance, 0);
      expect(ticket.change, 10);
      expect(ticket.canFinalize, isTrue);
    });

    test('agrupa o mesmo produto e remove quantidade zerada', () {
      final ticket = PdvTicket();
      ticket.addItem(
        PdvTicketItem(
          id: 'line-1',
          productId: 'product-1',
          code: '000001',
          name: 'Produto',
          quantity: 2,
          unitPrice: 12,
        ),
      );
      ticket.addItem(
        PdvTicketItem(
          id: 'line-2',
          productId: 'product-1',
          code: '000001',
          name: 'Produto',
          quantity: 3,
          unitPrice: 12,
        ),
      );

      expect(ticket.items, hasLength(1));
      expect(ticket.items.single.quantity, 5);

      ticket.changeItemQuantity(ticket.items.single.id, 0);
      expect(ticket.items, isEmpty);
    });
  });

  group('PdvStore', () {
    test('transfere e combina comandas ocupadas de forma auditável', () {
      final store = PdvStore(storeId: 'store', terminalId: 'terminal');
      final source = store.openTicket(boardNumber: '000001');
      source
        ..customerName = 'Ana'
        ..addItem(
          PdvTicketItem(
            id: 'source-line',
            productId: 'product-1',
            code: '000001',
            name: 'Café',
            quantity: 2,
            unitPrice: 5,
          ),
        );
      final destination = store.openTicket(boardNumber: '000002');
      destination.addItem(
        PdvTicketItem(
          id: 'destination-line',
          productId: 'product-1',
          code: '000001',
          name: 'Café',
          unitPrice: 5,
        ),
      );

      final transferred = store.transferTicket('000001', '000002');

      expect(transferred.id, destination.id);
      expect(transferred.customerName, 'Ana');
      expect(transferred.items.single.quantity, 3);
      expect(source.status, PdvTicketStatus.canceled);
      expect(store.syncQueue.last.type, 'ticket_transferred');
      expect(store.syncQueue.last.payload['DestinationBoard'], '000002');
    });

    test('fecha venda, registra dinheiro líquido e preserva JSON', () {
      final now = DateTime(2026, 7, 26, 12);
      final store = PdvStore(storeId: 'store', terminalId: 'terminal');
      final cash = store.openCash(
        operatorId: 'operator',
        openingAmount: 100,
        now: now,
      );
      final ticket = store.openTicket(
        boardNumber: '000001',
        operatorId: 'operator',
        now: now,
      );
      ticket.addItem(
        PdvTicketItem(
          id: 'line',
          productId: 'product',
          code: '000001',
          name: 'Almoço',
          quantity: 2,
          unitPrice: 25,
        ),
        now: now,
      );
      ticket.addPayment(
        PdvPayment(
          id: 'payment',
          method: PdvPaymentMethod.cash,
          amount: 60,
          createdAt: now,
        ),
        now: now,
      );

      store.completeTicket(ticket, now: now);

      expect(ticket.status, PdvTicketStatus.completed);
      expect(cash.expectedBalance, 155);
      expect(cash.movements.single.amount, 55);
      expect(store.syncQueue.last.type, 'sale_created');

      final decoded = PdvStore.fromJson(store.toJson());
      expect(decoded.toJson(), store.toJson());
      expect(decoded.tickets.single.change, 5);
      expect(decoded.openCashSession?.expectedBalance, 155);
    });
  });
}
