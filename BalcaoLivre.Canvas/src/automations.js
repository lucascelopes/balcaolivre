export const defaultRules={lateOrders:true,lateMinutes:15,longTables:true,tableMinutes:90,lowStock:true,cashDifference:true};

export function activeRules(state){
 const saved=state.automationRules||{};
 return {...defaultRules,...saved,
  lateMinutes:Number.isInteger(saved.lateMinutes)&&saved.lateMinutes>=5&&saved.lateMinutes<=240?saved.lateMinutes:defaultRules.lateMinutes,
  tableMinutes:Number.isInteger(saved.tableMinutes)&&saved.tableMinutes>=15&&saved.tableMinutes<=480?saved.tableMinutes:defaultRules.tableMinutes
 };
}

export function attentionItems(state,now=Date.now()){
 const rules=activeRules(state),items=[];
 const elapsed=date=>{const time=Date.parse(date);return Number.isFinite(time)?Math.floor((now-time)/60000):-1};
 if(rules.lateOrders)for(const order of state.orders||[]){
  const minutes=elapsed(order.createdAt);
  if(order.status!=='Concluído'&&minutes>=rules.lateMinutes)items.push({id:`order-${order.id}`,kind:'Pedido',title:`${order.customer} · ${order.channel}`,detail:`Há ${minutes} min em andamento · ${order.status}`,page:'Cozinha'});
 }
 if(rules.longTables)for(const table of state.tables||[]){
  const minutes=elapsed(table.openedAt);
  if(table.people>0&&minutes>=rules.tableMinutes)items.push({id:`table-${table.id}`,kind:'Mesa',title:`Mesa ${String(table.id).padStart(2,'0')}`,detail:`Há ${minutes} min ocupada · ${table.zone}`,page:'Salão',tableId:table.id});
 }
 if(rules.lowStock)for(const ingredient of state.ingredients||[]){
  if(ingredient.quantity<=ingredient.minimum)items.push({id:`stock-${ingredient.id}`,kind:'Estoque',title:ingredient.name,detail:`Saldo ${ingredient.quantity.toLocaleString('pt-BR')} ${ingredient.unit} · mínimo ${ingredient.minimum.toLocaleString('pt-BR')} ${ingredient.unit}`,page:'Estoque'});
 }
 const lastClose=(state.history||[])[0];
 if(rules.cashDifference&&lastClose&&Number.isFinite(lastClose.counted)&&Number.isFinite(lastClose.expected)&&lastClose.counted!==lastClose.expected&&elapsed(lastClose.date)>=0&&elapsed(lastClose.date)<=1440){
  items.push({id:`cash-${lastClose.id||lastClose.date}`,kind:'Caixa',title:'Diferença no último fechamento',detail:`Conferir ${new Intl.NumberFormat('pt-BR',{style:'currency',currency:'BRL'}).format((lastClose.counted-lastClose.expected)/100)}`,page:'Histórico'});
 }
 return items;
}
