export const money = n => new Intl.NumberFormat('pt-BR',{style:'currency',currency:'BRL'}).format(n/100);
export const total = items => items.reduce((sum,i)=>sum+i.price*i.qty,0);
export const parseMoney = value => { const raw=String(value??'').replace(/R\$|\s/g,''); if(!/^\d+(?:[.,]\d{1,2})?$/.test(raw)) throw new Error('Informe um valor válido com até duas casas decimais.'); const n=Number(raw.replace(',','.')); if(!Number.isSafeInteger(Math.round(n*100))) throw new Error('Valor acima do limite permitido.'); return Math.round(n*100); };
export const products = [ ['Burger Clássico',2800,'Lanches','burger'],['Batata Frita',1600,'Lanches','fries'],['Executivo',3200,'Pratos','plate'],['Frango Grelhado',2900,'Pratos','chicken'],['Suco de Laranja',900,'Bebidas','juice'],['Pudim',1200,'Sobremesas','dessert'],['Chopp Pilsen',2100,'Bebidas','beer'] ].map(([name,price,category,photo],id)=>({id:String(id+1),name,price,category,photo,available:true}));
export function seed(){return {products,tables:Array.from({length:19},(_,i)=>({id:i+1,zone:i<7?'Janela':i<15?'Centro':'Varanda',people:[1,2,4,6,8,10,12,14,16,18].includes(i+1)?(i===11?6:2):0,items:i===3?[{...products[0],qty:1},{...products[1],qty:1},{...products[6],qty:2}]:[],openedAt:new Date().toISOString()})),orders:[['1047','iFood','Ana'],['1046','Keeta','Bruno'],['1045','99Food','Carla'],['1044','Retirada','Pedro']].map(([id,channel,customer],i)=>({id,channel,customer,status:i===3?'Pronto':i===1?'Em preparo':'Recebido',items:[{...products[0],qty:1},{...products[1],qty:1}],createdAt:new Date().toISOString(),note:i===0?'Sem cebola.':''})),cash:{open:true,opening:20000},transactions:[],history:[]};}
export function addItem(items,product){const found=items.find(x=>x.id===product.id);return found?items.map(x=>x.id===product.id?{...x,qty:x.qty+1}:x):[...items,{...product,qty:1}];}
export function settleTable(state,id,method){if(!state.cash.open)throw new Error('Abra o caixa antes de receber.');if(!['Pix','Cartão','Dinheiro'].includes(method))throw Error('Forma de pagamento inválida.');const table=state.tables.find(t=>t.id===id);if(!table||!table.items.length)throw new Error('A comanda está vazia.');const transaction={id:crypto.randomUUID(),label:`Mesa ${String(id).padStart(2,'0')}`,amount:total(table.items),method,date:new Date().toISOString()};return {...state,tables:state.tables.map(t=>t.id===id?{...t,people:0,items:[],kitchenSession:null}:t),orders:state.orders.map(o=>o.tableId===id&&o.tableSession&&o.tableSession===table.kitchenSession?{...o,paidAt:transaction.date,paymentMethod:method,transactionId:transaction.id}:o),transactions:[transaction,...state.transactions]};}
export const cashTotals=s=>({income:s.transactions.filter(t=>t.amount>0).reduce((a,t)=>a+t.amount,0),expense:-s.transactions.filter(t=>t.amount<0).reduce((a,t)=>a+t.amount,0),expected:s.cash.opening+s.transactions.filter(t=>t.method==='Dinheiro').reduce((a,t)=>a+t.amount,0)});
export function scanText(text){return text.split(/\n/).map(line=>{const m=line.match(/^\s*(.+?)\s+(?:R\$\s*)?(\d+[,.]\d{2})\s*$/);return m?{id:crypto.randomUUID(),name:m[1].replace(/[.·]+$/,'').trim(),price:parseMoney(m[2]),category:'Pratos',available:true}:null}).filter(Boolean);}
export function changeQuantity(items,id,qty){if(!Number.isInteger(qty)||qty<0||qty>99)throw Error('Quantidade deve estar entre 0 e 99.');if(qty<(items.find(i=>i.id===id)?.sentQty||0))throw Error('Itens já enviados à cozinha não podem ser reduzidos por este controle.');return qty===0?items.filter(i=>i.id!==id):items.map(i=>i.id===id?{...i,qty}:i);}
export function closeCash(state,counted){if(!state.cash.open)throw Error('O caixa já está fechado.');if(!Number.isSafeInteger(counted)||counted<0)throw Error('Valor contado inválido.');return {...state,cash:{...state.cash,open:false},history:[{id:crypto.randomUUID(),date:new Date().toISOString(),opening:state.cash.opening,...cashTotals(state),counted,transactions:state.transactions.map(t=>({...t}))},...state.history]};}
export function openCash(state,opening){if(state.cash.open)throw Error('O caixa já está aberto.');if(!Number.isSafeInteger(opening)||opening<0)throw Error('Saldo inicial inválido.');return {...state,cash:{open:true,opening},transactions:[]};}
export function settleOrder(state,id,method){if(!state.cash.open)throw Error('Abra o caixa antes de receber.');if(!['Pix','Cartão','Dinheiro'].includes(method))throw Error('Forma de pagamento inválida.');const order=state.orders.find(o=>o.id===id);if(!order||!order.items.length)throw Error('Pedido vazio ou não encontrado.');if(order.paidAt)throw Error('Este pedido já foi recebido.');if(order.tableId)throw Error('Receba este pedido pela comanda da mesa.');const date=new Date().toISOString();return {...state,orders:state.orders.map(o=>o.id===id?{...o,paidAt:date,paymentMethod:method}:o),transactions:[{id:crypto.randomUUID(),orderId:id,label:`Pedido #${id}`,amount:total(order.items),method,date},...state.transactions]};}
export function sendTableToKitchen(state,id){const table=state.tables.find(t=>t.id===id);if(!table)throw Error('Mesa não encontrada.');const items=table.items.map(i=>({...i,qty:i.qty-(i.sentQty||0)})).filter(i=>i.qty>0);if(!items.length)throw Error('Nenhum item novo para enviar.');const tableSession=table.kitchenSession||crypto.randomUUID();const order={id:crypto.randomUUID(),channel:'Salão',customer:`Mesa ${String(id).padStart(2,'0')}`,tableId:id,tableSession,status:'Recebido',items:items.map(({sentQty,...i})=>i),createdAt:new Date().toISOString(),note:''};return {...state,tables:state.tables.map(t=>t.id===id?{...t,kitchenSession:tableSession,items:t.items.map(i=>({...i,sentQty:i.qty}))}:t),orders:[order,...state.orders]};}

const stockAmount=value=>Math.round(value*1000000)/1000000;

export function previewOrderStock(state,orderId){
 const order=state.orders.find(o=>o.id===orderId);
 if(!order)throw Error('Pedido não encontrado.');
 const ingredients=state.ingredients||[],recipes=state.recipes||{};
 const required=new Map(),missingProducts=[],missingIngredients=[];
 for(const item of order.items||[]){
  const recipe=recipes[item.id];
  if(!Number.isInteger(item.qty)||item.qty<=0||!recipe||!Number.isInteger(recipe.yield)||recipe.yield<=0||!Array.isArray(recipe.lines)||!recipe.lines.length){missingProducts.push(item.name);continue}
  for(const line of recipe.lines){
   const ingredient=ingredients.find(i=>i.id===line.id);
   if(!ingredient||!Number.isFinite(line.quantity)||line.quantity<=0){missingIngredients.push(ingredient?.name||'Ingrediente não encontrado');continue}
   required.set(ingredient.id,(required.get(ingredient.id)||0)+line.quantity*item.qty/recipe.yield);
  }
 }
 const lines=[...required].map(([id,amount])=>{
  const ingredient=ingredients.find(i=>i.id===id);
  const quantity=stockAmount(amount);
  return {id,name:ingredient.name,unit:ingredient.unit,quantity,available:ingredient.quantity,shortage:!Number.isFinite(quantity)||quantity<=0||!Number.isFinite(ingredient.quantity)||ingredient.quantity+0.000001<quantity};
 });
 return {order,lines,missingProducts,missingIngredients,canApply:order.status==='Concluído'&&!order.stockAppliedAt&&!missingProducts.length&&!missingIngredients.length&&lines.length>0&&!lines.some(line=>line.shortage)};
}

export function applyOrderStock(state,orderId){
 const preview=previewOrderStock(state,orderId);
 if(preview.order.status!=='Concluído')throw Error('Conclua o pedido antes da baixa.');
 if(preview.order.stockAppliedAt)throw Error('O estoque deste pedido já foi baixado.');
 if(preview.missingProducts.length||preview.missingIngredients.length)throw Error('Complete as fichas técnicas antes da baixa.');
 if(preview.lines.some(line=>line.shortage))throw Error('Estoque insuficiente para esta baixa.');
 if(!preview.lines.length)throw Error('Nenhum ingrediente para baixar.');
 const date=new Date().toISOString(),usage=preview.lines.map(({id,name,unit,quantity})=>({id,name,unit,quantity}));
 return {...state,
  ingredients:(state.ingredients||[]).map(i=>{const line=usage.find(x=>x.id===i.id);return line?{...i,quantity:stockAmount(i.quantity-line.quantity)}:i}),
  orders:state.orders.map(o=>o.id===orderId?{...o,stockAppliedAt:date,stockUsage:usage}:o),
  stockMovements:[...usage.map(line=>({id:crypto.randomUUID(),ingredient:line.name,ingredientId:line.id,delta:-line.quantity,unit:line.unit,reason:`Pedido #${orderId.slice(0,8)}`,orderId,date})),...(state.stockMovements||[])]
 };
}
