import React,{useState} from 'react';
import {ChartBar,Receipt,Wallet,ArrowUp,ArrowDown} from '@phosphor-icons/react';
import {money} from './domain.js';

const periods=[['Hoje',1],['7 dias',7],['30 dias',30],['Todo o período',null]];
const channels=['Salão','Retirada','iFood','Keeta','99Food'];

function inPeriod(date,days){
 if(days===null)return true;
 const value=new Date(date).getTime();
 if(!Number.isFinite(value))return false;
 if(days===1){const midnight=new Date();midnight.setHours(0,0,0,0);return value>=midnight.getTime()}
 return value>=Date.now()-days*86400000;
}

export default function Reports({state,onNavigate}){
 const [days,setDays]=useState(1);
 // Um caixa encerrado ainda pode manter seus lançamentos até a próxima abertura.
 const allTransactions=[...(state.transactions||[]),...(state.history||[]).flatMap(h=>h.transactions||[])];
 const seen=new Set();
 const transactions=allTransactions.filter(t=>{
  if(!inPeriod(t.date,days))return false;
  if(!t.id)return true;
  if(seen.has(t.id))return false;
  seen.add(t.id);
  return true;
 });
 const income=transactions.filter(t=>t.amount>0).reduce((sum,t)=>sum+t.amount,0);
 const expense=-transactions.filter(t=>t.amount<0).reduce((sum,t)=>sum+t.amount,0);
 const orders=(state.orders||[]).filter(o=>inPeriod(o.createdAt,days));
 const paid=transactions.filter(t=>t.amount>0);
 const methods=['Pix','Cartão','Dinheiro'].map(name=>({name,value:paid.filter(t=>t.method===name).reduce((sum,t)=>sum+t.amount,0)}));
 const byChannel=channels.map(name=>({name,value:orders.filter(o=>o.channel===name).length}));
 const maxChannel=Math.max(1,...byChannel.map(x=>x.value));
 return <div className="reports-page">
  <div className="operations-intro"><p>Movimento registrado neste navegador.</p><span className="demo-label">Dados locais · Canais externos de demonstração</span></div>
  <div className="filters">{periods.map(([label,value])=><button key={label} className={days===value?'active':''} onClick={()=>setDays(value)}>{label}</button>)}</div>
  <div className="report-metrics">
   <div className="panel"><ArrowUp/><small>Entradas registradas</small><strong>{money(income)}</strong></div>
   <div className="panel"><ArrowDown/><small>Saídas registradas</small><strong>{money(expense)}</strong></div>
   <div className="panel"><Wallet/><small>Resultado das movimentações</small><strong>{money(income-expense)}</strong></div>
   <div className="panel"><Receipt/><small>Pedidos criados</small><strong>{orders.length}</strong></div>
  </div>
  <div className="report-columns">
   <section className="panel"><h2>Pedidos por origem</h2><p className="meta">Contagem de pedidos criados no período, incluindo os ainda abertos.</p>{byChannel.map(({name,value})=><div className="report-bar-row" key={name}><span>{name}</span><div className="report-bar-track"><i style={{width:`${value/maxChannel*100}%`}}/></div><b>{value}</b></div>)}<button onClick={()=>onNavigate('Pedidos')}>Ver pedidos</button></section>
   <section className="panel"><h2>Recebimentos por forma</h2><p className="meta">Somente valores registrados no caixa.</p>{methods.map(({name,value})=><div className="line" key={name}><span>{name}</span><b>{money(value)}</b></div>)}<button onClick={()=>onNavigate('Caixa')}>Abrir caixa</button></section>
  </div>
  <section className="panel report-note"><ChartBar/><div><h3>Sobre este relatório</h3><p>Os valores usam o caixa atual e os fechamentos com lançamentos salvos. Fechamentos antigos sem detalhamento não entram nos totais. As origens iFood, Keeta e 99Food ainda são exemplos locais, sem conexão com as plataformas.</p></div></section>
 </div>;
}
