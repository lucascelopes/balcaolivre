import React,{useEffect,useState} from 'react';
import {Clock,Package,Receipt,Wallet,ArrowRight,Robot,Check} from '@phosphor-icons/react';
import {activeRules,attentionItems} from './automations.js';

const icons={Pedido:Receipt,Mesa:Clock,Estoque:Package,Caixa:Wallet};

export default function AutomationCenter({state,setState,onNavigate,onTable}){
 const [now,setNow]=useState(Date.now());
 useEffect(()=>{const timer=setInterval(()=>setNow(Date.now()),30000);return()=>clearInterval(timer)},[]);
 const rules=activeRules(state),items=attentionItems(state,now);
 function change(key,value){setState(current=>({...current,automationRules:{...activeRules(current),[key]:value}}))}
 function open(item){if(item.tableId){const table=state.tables.find(t=>t.id===item.tableId);if(table){onTable(table);return}}onNavigate(item.page)}
 return <div className="automation-page">
  <div className="operations-intro"><p>O BL acompanha situações que precisam de atenção usando os dados deste navegador.</p><span className="demo-label">Regras locais · IA ainda não conectada</span></div>
  <section className="panel automation-attention"><div className="panel-title"><h2>Precisa de atenção</h2><span className="automation-count">{items.length}</span></div>
   {items.map(item=>{const Icon=icons[item.kind];return <button className="automation-alert" key={item.id} onClick={()=>open(item)}><Icon/><span><small>{item.kind}</small><b>{item.title}</b><small>{item.detail}</small></span><ArrowRight/></button>})}
   {!items.length&&<div className="kitchen-empty"><Check size={30}/><h3>Tudo em dia por enquanto</h3><p>Os avisos aparecerão aqui quando uma regra for atingida.</p></div>}
  </section>
  <div className="automation-heading"><Robot size={22}/><h2>Regras ativas nesta prévia</h2></div>
  <div className="automation-rules">
   <section className="panel"><label><span><b>Pedidos demorados</b><small>Avisar sobre pedidos ainda em andamento.</small></span><input type="checkbox" checked={rules.lateOrders} onChange={e=>change('lateOrders',e.target.checked)}/></label><div className="automation-setting">Avisar após <select value={rules.lateMinutes} onChange={e=>change('lateMinutes',Number(e.target.value))}>{[5,10,15,20,30,45,60,90,120].map(n=><option key={n} value={n}>{n} minutos</option>)}</select></div></section>
   <section className="panel"><label><span><b>Mesas há muito tempo ocupadas</b><small>Acompanhar mesas que continuam abertas.</small></span><input type="checkbox" checked={rules.longTables} onChange={e=>change('longTables',e.target.checked)}/></label><div className="automation-setting">Avisar após <select value={rules.tableMinutes} onChange={e=>change('tableMinutes',Number(e.target.value))}>{[30,60,90,120,180,240].map(n=><option key={n} value={n}>{n} minutos</option>)}</select></div></section>
   <section className="panel"><label><span><b>Estoque para repor</b><small>Avisar quando um ingrediente chega ao mínimo.</small></span><input type="checkbox" checked={rules.lowStock} onChange={e=>change('lowStock',e.target.checked)}/></label></section>
   <section className="panel"><label><span><b>Diferença no caixa</b><small>Avisar sobre o último fechamento, durante 24 horas.</small></span><input type="checkbox" checked={rules.cashDifference} onChange={e=>change('cashDifference',e.target.checked)}/></label></section>
  </div>
  <p className="hint">Os avisos são atualizados enquanto esta página está aberta. Não enviam mensagens nem alteram pedidos, caixa ou estoque.</p>
 </div>;
}
