import React from 'react';
import {Package,Check,WarningCircle} from '@phosphor-icons/react';
import {applyOrderStock,previewOrderStock} from './domain.js';

export default function StockDeductions({state,setState,onToast,onRecipes}){
 const pending=(state.orders||[]).filter(o=>o.status==='Concluído'&&!o.stockAppliedAt);
 return <div className="stock-deductions"><div className="operations-intro"><p>Revise o consumo calculado pelas fichas técnicas atuais antes de confirmar.</p><span className="demo-label">Baixa manual · Dados locais</span></div>
 {pending.map(order=>{
  const preview=previewOrderStock(state,order.id);
  return <article className="panel stock-deduction" key={order.id}>
   <div className="panel-title"><h2>{order.customer}</h2><span className="channel">{order.channel}</span></div>
   <p className="meta">Pedido #{order.id.slice(0,8)} · {order.items.map(i=>`${i.qty}× ${i.name}`).join(', ')}</p>
   {preview.lines.map(line=><div className="line" key={line.id}><span><Package/> {line.name}</span><b>{line.quantity.toLocaleString('pt-BR')} {line.unit}</b><small>Disponível: {Number.isFinite(line.available)?line.available.toLocaleString('pt-BR'):'—'} {line.unit}</small>{line.shortage&&<strong className="orange">Insuficiente</strong>}</div>)}
   {!!(preview.missingProducts.length||preview.missingIngredients.length)&&<p className="stock-warning"><WarningCircle/>Faltam fichas para {preview.missingProducts.join(', ')||'itens do pedido'}{preview.missingIngredients.length?` e ingredientes: ${preview.missingIngredients.join(', ')}`:''}.</p>}
   <div className="stock-deduction-actions"><button className="primary" disabled={!preview.canApply} onClick={()=>{try{setState(applyOrderStock(state,order.id));onToast('Estoque baixado para este pedido.')}catch(error){onToast(error.message)}}}><Check/>Confirmar baixa</button>{(preview.missingProducts.length||preview.missingIngredients.length)>0&&<button onClick={onRecipes}>Completar fichas técnicas</button>}</div>
  </article>;
 })}
 {!pending.length&&<div className="panel kitchen-empty"><Check size={32}/><h3>Nenhuma baixa pendente</h3><p>Pedidos concluídos com fichas técnicas aparecerão aqui para conferência.</p></div>}
 </div>;
}
