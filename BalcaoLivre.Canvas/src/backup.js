const APP='balcao-livre-canvas';
const isObject=value=>value!==null&&typeof value==='object'&&!Array.isArray(value);
const isMoney=value=>Number.isSafeInteger(value)&&value>=0;
const isItem=item=>isObject(item)&&typeof item.id==='string'&&typeof item.name==='string'&&isMoney(item.price)&&Number.isInteger(item.qty)&&item.qty>0&&item.qty<=99;

export function makeBackup(state){
 return {app:APP,version:1,exportedAt:new Date().toISOString(),state};
}

export function readBackup(text){
 let backup;
 try{backup=JSON.parse(text)}catch{throw Error('O arquivo não contém um JSON válido.')}
 if(!isObject(backup)||backup.app!==APP||backup.version!==1||!isObject(backup.state))throw Error('Escolha uma cópia exportada pelo BL. Canvas.');
 if(typeof backup.exportedAt!=='string'||!Number.isFinite(Date.parse(backup.exportedAt)))throw Error('A cópia não possui uma data de exportação válida.');
 const state=backup.state;
 if(!Array.isArray(state.products)||!Array.isArray(state.tables)||!Array.isArray(state.orders)||!Array.isArray(state.transactions)||!Array.isArray(state.history)||!isObject(state.cash))throw Error('A cópia está incompleta.');
 if(state.products.length>5000||state.orders.length>50000||state.tables.length>5000||state.transactions.length>50000)throw Error('A cópia excede o limite desta prévia.');
 if(!state.products.every(p=>isObject(p)&&typeof p.id==='string'&&typeof p.name==='string'&&typeof p.category==='string'&&isMoney(p.price)&&typeof p.available==='boolean'&&(!p.photoData||(typeof p.photoData==='string'&&p.photoData.length<=300000&&p.photoData.startsWith('data:image/jpeg;base64,')))))throw Error('A cópia contém produtos inválidos.');
 if(!state.tables.every(t=>isObject(t)&&Number.isInteger(t.id)&&typeof t.zone==='string'&&Number.isInteger(t.people)&&t.people>=0&&Array.isArray(t.items)&&t.items.every(isItem)))throw Error('A cópia contém mesas inválidas.');
 if(!state.orders.every(o=>isObject(o)&&typeof o.id==='string'&&typeof o.customer==='string'&&typeof o.channel==='string'&&typeof o.createdAt==='string'&&Number.isFinite(Date.parse(o.createdAt))&&['Recebido','Em preparo','Pronto','Concluído'].includes(o.status)&&Array.isArray(o.items)&&o.items.every(isItem)))throw Error('A cópia contém pedidos inválidos.');
 if(new Set(state.products.map(p=>p.id)).size!==state.products.length||new Set(state.tables.map(t=>t.id)).size!==state.tables.length||new Set(state.orders.map(o=>o.id)).size!==state.orders.length)throw Error('A cópia contém identificadores repetidos.');
 if(typeof state.cash.open!=='boolean'||!isMoney(state.cash.opening))throw Error('A cópia contém um caixa inválido.');
 if(!state.transactions.every(t=>isObject(t)&&Number.isSafeInteger(t.amount)&&typeof t.date==='string'))throw Error('A cópia contém lançamentos inválidos.');
 if(!state.history.every(h=>isObject(h)&&typeof h.date==='string'&&isMoney(h.counted)&&(!h.transactions||(Array.isArray(h.transactions)&&h.transactions.every(t=>isObject(t)&&Number.isSafeInteger(t.amount)&&typeof t.date==='string')))))throw Error('A cópia contém fechamentos inválidos.');
 if(state.ingredients!==undefined&&(!Array.isArray(state.ingredients)||!state.ingredients.every(i=>isObject(i)&&typeof i.id==='string'&&typeof i.name==='string'&&Number.isFinite(i.quantity)&&i.quantity>=0&&Number.isFinite(i.minimum)&&i.minimum>=0&&isMoney(i.cost))))throw Error('A cópia contém ingredientes inválidos.');
 if(state.recipes!==undefined&&(!isObject(state.recipes)||!Object.values(state.recipes).every(r=>isObject(r)&&Number.isInteger(r.yield)&&r.yield>0&&Array.isArray(r.lines)&&r.lines.every(l=>isObject(l)&&typeof l.id==='string'&&Number.isFinite(l.quantity)&&l.quantity>0))))throw Error('A cópia contém fichas técnicas inválidas.');
 if(state.stockMovements!==undefined&&(!Array.isArray(state.stockMovements)||!state.stockMovements.every(m=>isObject(m)&&typeof m.id==='string'&&typeof m.ingredient==='string'&&typeof m.unit==='string'&&Number.isFinite(m.delta)&&typeof m.date==='string')))throw Error('A cópia contém movimentações de estoque inválidas.');
 if(state.purchaseOrders!==undefined&&(!Array.isArray(state.purchaseOrders)||state.purchaseOrders.length>10000||!state.purchaseOrders.every(o=>isObject(o)&&typeof o.id==='string'&&typeof o.supplier==='string'&&typeof o.date==='string'&&Number.isFinite(Date.parse(o.date))&&['Pendente','Recebido'].includes(o.status)&&isMoney(o.total)&&Array.isArray(o.lines)&&o.lines.length>0&&o.lines.every(line=>isObject(line)&&typeof line.ingredientId==='string'&&typeof line.name==='string'&&typeof line.unit==='string'&&Number.isFinite(line.quantity)&&line.quantity>0&&isMoney(line.unitCost)))))throw Error('A cópia contém pedidos de compra inválidos.');
 return {exportedAt:backup.exportedAt,state};
}
