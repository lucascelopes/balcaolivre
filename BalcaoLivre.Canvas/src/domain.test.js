import test from 'node:test';
import assert from 'node:assert/strict';
import {seed,total,addItem,settleTable,cashTotals,scanText,parseMoney} from './domain.js';
test('comanda soma centavos e incrementa quantidades sem alterar origem',()=>{const s=seed(),t=s.tables[3];assert.equal(total(t.items),8600);const next=addItem(t.items,s.products[0]);assert.equal(total(next),11400);assert.equal(total(t.items),8600)});
test('recebimento libera mesa e registra pagamento uma única vez',()=>{const s=settleTable(seed(),4,'Dinheiro');assert.equal(s.tables[3].people,0);assert.equal(s.tables[3].items.length,0);assert.equal(s.transactions[0].amount,8600);assert.equal(cashTotals(s).expected,28600);assert.throws(()=>settleTable(s,4,'Dinheiro'),/vazia/)});
test('caixa fechado impede recebimento',()=>{const s=seed();s.cash.open=false;assert.throws(()=>settleTable(s,4,'Pix'),/Abra o caixa/)});
test('Pix entra no faturamento mas não no dinheiro esperado',()=>{const s=settleTable(seed(),4,'Pix');assert.equal(cashTotals(s).income,8600);assert.equal(cashTotals(s).expected,20000)});
test('scanner extrai apenas linhas com preço explícito',()=>{const rows=scanText('CARDÁPIO\nExecutivo R$ 32,90\nSuco 9.00\nSem preço');assert.equal(rows.length,2);assert.equal(rows[0].price,3290);assert.equal(rows[1].name,'Suco')});
test('valores inválidos não viram movimentação financeira',()=>{assert.throws(()=>parseMoney('-1'));assert.throws(()=>parseMoney('abc'));assert.equal(parseMoney('12,90'),1290)});
