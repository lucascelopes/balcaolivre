import React,{useState} from 'react';
import {ArrowRight,Check,Package,Plus,ShoppingCart,Trash,WarningCircle,X} from '@phosphor-icons/react';
import {money} from './domain.js';

const round=n=>Math.round(n*1000000)/1000000;
const fmt=n=>n.toLocaleString('pt-BR',{maximumFractionDigits:6});

export default function Purchasing({state,setState,onNavigate,onToast}){
 const ingredients=state.ingredients||[],orders=state.purchaseOrders||[];
 const low=ingredients.filter(i=>i.quantity<=i.minimum);
 const [draft,setDraft]=useState([]),[supplier,setSupplier]=useState(''),[selected,setSelected]=useState(''),[receiving,setReceiving]=useState(null);
 const selectedId=ingredients.some(i=>i.id===selected)?selected:ingredients[0]?.id||'';
 const total=draft.reduce((sum,line)=>{const ingredient=ingredients.find(i=>i.id===line.id);return sum+(Number.isFinite(line.quantity)&&line.quantity>0?Math.round((ingredient?.cost||0)*line.quantity):0)},0);
 const add=(id,quantity=1)=>setDraft(current=>current.some(line=>line.id===id)?current.map(line=>line.id===id?{...line,quantity:round(line.quantity+quantity)}:line):[...current,{id,quantity}]);
 function suggest(){setDraft(current=>{const next=[...current];for(const item of low){const quantity=round(item.minimum>0?Math.max(0,item.minimum*2-item.quantity):1);if(quantity>0&&!next.some(line=>line.id===item.id))next.push({id:item.id,quantity})}return next})}
 function save(){
  if(!supplier.trim()){onToast('Informe o fornecedor ou a origem da compra.');return}
  if(!draft.length||draft.some(line=>!Number.isFinite(line.quantity)||line.quantity<=0||line.quantity>1000000)){onToast('Confira os itens e as quantidades.');return}
  const lines=draft.map(line=>{const item=ingredients.find(i=>i.id===line.id);return item&&{ingredientId:item.id,name:item.name,unit:item.unit,quantity:round(line.quantity),unitCost:item.cost}});
  if(lines.some(line=>!line)){onToast('Um ingrediente do rascunho não está mais no estoque.');return}
  const order={id:crypto.randomUUID(),date:new Date().toISOString(),supplier:supplier.trim(),status:'Pendente',lines,total};
  setState(current=>({...current,purchaseOrders:[order,...(current.purchaseOrders||[])]}));
  setDraft([]);setSupplier('');onToast('Pedido registrado localmente. Envie ao fornecedor pelos seus canais.');
 }
 function receive(){
  const order=orders.find(item=>item.id===receiving);
  if(!order||order.status!=='Pendente'){setReceiving(null);return}
  if(order.lines.some(line=>!ingredients.some(i=>i.id===line.ingredientId))){onToast('Um ingrediente não existe mais no estoque.');return}
  const date=new Date().toISOString();
  setState(current=>current.purchaseOrders?.some(item=>item.id===order.id&&item.status==='Pendente')?({...current,
   ingredients:(current.ingredients||[]).map(item=>{const line=order.lines.find(l=>l.ingredientId===item.id);return line?{...item,quantity:round(item.quantity+line.quantity)}:item}),
   stockMovements:[...order.lines.map(line=>({id:crypto.randomUUID(),ingredientId:line.ingredientId,ingredient:line.name,unit:line.unit,delta:line.quantity,reason:`Compra #${order.id.slice(0,8)}`,date})),...(current.stockMovements||[])],
   purchaseOrders:(current.purchaseOrders||[]).map(item=>item.id===order.id?{...item,status:'Recebido',receivedAt:date}:item)
  }):current);
  setReceiving(null);onToast('Recebimento registrado e estoque atualizado.');
 }
 return <div className="purchase-page">
  <div className="operations-intro"><p>Planeje a reposição e registre o recebimento no estoque.</p><span className="demo-label">Compras locais · Sem envio automático</span></div>
  <div className="purchase-metrics"><div className="panel"><Package/><span>Ingredientes cadastrados</span><strong>{ingredients.length}</strong></div><div className="panel"><WarningCircle/><span>No mínimo ou abaixo</span><strong>{low.length}</strong></div><div className="panel"><ShoppingCart/><span>Compras pendentes</span><strong>{orders.filter(o=>o.status==='Pendente').length}</strong></div></div>
  <div className="purchase-layout"><section className="panel purchase-editor"><div className="panel-title"><h2>Novo pedido de compra</h2></div><p className="hint">As quantidades sugeridas levam cada ingrediente a duas vezes o mínimo cadastrado; sem mínimo, sugerem uma unidade. Revise antes de registrar.</p>
   <label>Fornecedor ou origem<input value={supplier} maxLength={80} onChange={e=>setSupplier(e.target.value)} placeholder="Ex.: Mercado Central"/></label>
   <div className="purchase-picker"><select aria-label="Ingrediente para adicionar" value={selectedId} onChange={e=>setSelected(e.target.value)} disabled={!ingredients.length}>{ingredients.map(i=><option key={i.id} value={i.id}>{i.name} · {i.unit}</option>)}</select><button disabled={!ingredients.length} onClick={()=>add(selectedId)}><Plus/>Adicionar</button></div>
   <button className="purchase-suggest" disabled={!low.some(i=>!draft.some(line=>line.id===i.id))} onClick={suggest}><WarningCircle/>Adicionar sugestões de reposição</button>
   {!ingredients.length&&<div className="kitchen-empty"><Package size={30}/><p>Cadastre ingredientes no Estoque para preparar uma compra.</p><button onClick={()=>onNavigate('Estoque')}>Abrir estoque<ArrowRight/></button></div>}
   {draft.map(line=>{const item=ingredients.find(i=>i.id===line.id);if(!item)return null;return <div className="purchase-line" key={line.id}><span><b>{item.name}</b><small>Atual: {fmt(item.quantity)} {item.unit} · mínimo: {fmt(item.minimum)} {item.unit}</small></span><label>Comprar ({item.unit})<input type="number" min="0.001" max="1000000" step="any" value={line.quantity} onChange={e=>setDraft(current=>current.map(x=>x.id===line.id?{...x,quantity:Number(e.target.value)}:x))}/></label><button className="icon" aria-label={`Remover ${item.name}`} onClick={()=>setDraft(current=>current.filter(x=>x.id!==line.id))}><Trash/></button></div>})}
   {ingredients.length>0&&!draft.length&&<p className="empty">Adicione ingredientes para montar o pedido.</p>}
   <div className="purchase-total"><span>Estimativa pelos custos cadastrados</span><strong>{money(total)}</strong></div><button className="primary purchase-save" disabled={!draft.length} onClick={save}><Check/>Registrar pedido</button><p className="hint">Registrar aqui não faz a compra, não cria despesa no caixa e não envia mensagem ao fornecedor.</p>
  </section><section className="panel purchase-history"><div className="panel-title"><h2>Pedidos registrados</h2><small>{orders.length}</small></div>
   {orders.map(order=><article className="purchase-order" key={order.id}><div className="purchase-order-head"><div><b>{order.supplier}</b><small>{new Date(order.date).toLocaleString('pt-BR')} · #{order.id.slice(0,8)}</small></div><span className={order.status==='Pendente'?'purchase-pending':'purchase-received'}>{order.status}</span></div><ul>{order.lines.map(line=><li key={line.ingredientId}><span>{line.name}</span><b>{fmt(line.quantity)} {line.unit}</b></li>)}</ul><div className="purchase-order-foot"><small>Estimativa {money(order.total)}</small>{order.status==='Pendente'&&<button onClick={()=>setReceiving(order.id)}>Conferir recebimento<ArrowRight/></button>}</div></article>)}
   {!orders.length&&<div className="kitchen-empty"><ShoppingCart size={30}/><p>Seus pedidos de compra aparecerão aqui.</p></div>}
  </section></div>
  {receiving&&<div className="backdrop" onClick={()=>setReceiving(null)}><section className="panel modal purchase-confirm" role="dialog" aria-modal="true" aria-label="Conferir recebimento" onClick={e=>e.stopPropagation()}><button className="icon close" aria-label="Fechar conferência" onClick={()=>setReceiving(null)}><X/></button><h2>Conferir recebimento</h2><p>Confirme que todos os itens e quantidades deste pedido chegaram. O estoque será atualizado uma vez.</p>{orders.find(o=>o.id===receiving)?.lines.map(line=><div className="line" key={line.ingredientId}><span>{line.name}</span><b>+{fmt(line.quantity)} {line.unit}</b></div>)}<button className="primary" onClick={receive}><Check/>Confirmar entrada no estoque</button></section></div>}
 </div>;
}
