import React from 'react';
import {Layout,ChefHat,Wallet,Package,ArrowRight,Receipt,Plus} from '@phosphor-icons/react';
import {money,total,cashTotals} from './domain.js';

export default function Overview({state,onNavigate,onNewOrder,onTable}){
 const occupied=state.tables.filter(t=>t.people>0),pending=state.orders.filter(o=>o.status!=='Concluído');
 const low=(state.ingredients||[]).filter(i=>i.quantity<=i.minimum),cash=cashTotals(state);
 const cards=[
  {label:'Mesas ocupadas',value:occupied.length+'/'+state.tables.length,detail:'Acompanhar o salão',icon:Layout,page:'Salão'},
  {label:'Pedidos em andamento',value:pending.length,detail:'Ver fila da cozinha',icon:ChefHat,page:'Cozinha'},
  {label:state.cash.open?'Entradas do caixa atual':'Entradas do último caixa',value:money(cash.income),detail:state.cash.open?'Caixa aberto':'Caixa fechado',icon:Wallet,page:'Caixa'},
  {label:'Ingredientes para repor',value:low.length,detail:'Consultar estoque',icon:Package,page:'Estoque'}
 ];
 return <div className="overview"><div className="overview-welcome"><div><span className="demo-label">VISÃO DO RESTAURANTE · DADOS LOCAIS</span><h2>Seu atendimento, em um só lugar<span className="orange">.</span></h2><p>Veja o que está acontecendo e continue de onde precisa.</p></div><button className="primary" onClick={onNewOrder}><Plus/>Novo pedido</button></div>
 <div className="overview-metrics">{cards.map(({label,value,detail,icon:Icon,page})=><button key={label} className="panel overview-metric" onClick={()=>onNavigate(page)}><Icon size={24}/><span>{label}</span><strong>{value}</strong><small>{detail}<ArrowRight size={15}/></small></button>)}</div>
 <div className="overview-columns"><section className="panel"><div className="panel-title"><h2>Na cozinha</h2><button onClick={()=>onNavigate('Cozinha')}>Ver todos<ArrowRight/></button></div>{pending.slice(0,5).map(o=><div className="overview-order" key={o.id}><Receipt size={22}/><span><b>{o.customer}</b><small>{o.channel} · {o.items.reduce((sum,i)=>sum+i.qty,0)} itens</small></span><span className="badge">{o.status}</span></div>)}{!pending.length&&<p className="empty">Nenhum pedido em andamento.</p>}</section>
 <section className="panel"><div className="panel-title"><h2>Comandas abertas</h2><button onClick={()=>onNavigate('Salão')}>Ver salão<ArrowRight/></button></div>{occupied.slice(0,5).map(t=><button className="picker-row" key={t.id} onClick={()=>onTable(t)}><span><b>Mesa {String(t.id).padStart(2,'0')}</b><small className="overview-sub">{t.zone} · {t.people} pessoas</small></span><b>{money(total(t.items))}</b><ArrowRight/></button>)}{!occupied.length&&<p className="empty">Todas as mesas estão livres.</p>}</section></div>
 <section className="panel overview-replenish"><div><h2>Reposição de ingredientes</h2><p>{low.length?`${low.length} ingrediente(s) no mínimo ou abaixo dele.`:(state.ingredients||[]).length?'Todos os ingredientes estão acima do mínimo cadastrado.':'Cadastre os ingredientes para acompanhar a reposição.'}</p></div><button onClick={()=>onNavigate('Estoque')}><Package/>Abrir estoque</button>{low.length>0&&<div className="overview-low">{low.slice(0,6).map(i=><span key={i.id}><b>{i.name}</b><small>{i.quantity.toLocaleString('pt-BR')} {i.unit} · mínimo {i.minimum.toLocaleString('pt-BR')} {i.unit}</small></span>)}{low.length>6&&<small>Mais {low.length-6} no estoque</small>}</div>}</section>
 </div>;
}
