import React from 'react';
import {ArrowRight,Plus,Receipt,Plugs,WarningCircle} from '@phosphor-icons/react';

const channels=[
 {name:'iFood',description:'Pedidos recebidos pelo iFood'},
 {name:'Keeta',description:'Pedidos recebidos pela Keeta'},
 {name:'99Food',description:'Pedidos recebidos pelo 99Food'},
 {name:'WhatsApp',description:'Pedidos combinados por conversa'},
 {name:'Retirada',description:'Pedidos para buscar no balcão'}
];
const isManual=order=>order.source==='Manual'||(!order.source&&!order.tableId&&!['1047','1046','1045','1044'].includes(order.id));

export default function Channels({orders,onCreate,onView}){
 const manual=orders.filter(isManual);
 return <div className="channels-page">
  <div className="channels-hero panel"><div><span className="demo-label">CANAIS DE VENDA · PRÉVIA LOCAL</span><h2>Todos os pedidos, com a origem certa<span className="orange">.</span></h2><p>Registre pedidos que chegaram por outros canais e acompanhe o preparo no mesmo fluxo da cozinha.</p></div><span className="channels-hero-icon"><Plugs size={36}/></span></div>
  <div className="channels-summary"><div className="panel"><Receipt/><span>Pedidos manuais cadastrados</span><strong>{manual.length}</strong></div><div className="panel"><WarningCircle/><span>Integrações automáticas ativas</span><strong>0</strong></div></div>
  <div className="channels-heading"><h2>Origens disponíveis</h2><p>As plataformas abaixo não estão conectadas. Os pedidos de exemplo não representam sincronização.</p></div>
  <div className="channels-grid">{channels.map(({name,description})=>{const list=orders.filter(order=>order.channel===name&&isManual(order));const pending=list.filter(order=>order.status!=='Concluído');return <article className="panel channel-card" key={name}><div className="channel-card-top"><span className={'channel-mark channel-'+name}>{name==='Retirada'?'BL.':name}</span><span className="channel-state">{name==='Retirada'?'Local':'Não conectado'}</span></div><h3>{name}</h3><p>{description}</p><div className="channel-numbers"><div><strong>{list.length}</strong><small>Registrados manualmente</small></div><div><strong>{pending.length}</strong><small>Em andamento</small></div></div><div className="channel-actions"><button className="primary" onClick={()=>onCreate(name)}><Plus/>Registrar pedido</button><button onClick={()=>onView(name)}>Ver pedidos<ArrowRight/></button></div></article>})}</div>
  <aside className="panel channels-note"><WarningCircle size={22}/><p>O BL. ainda não recebe pedidos automaticamente de iFood, Keeta, 99Food ou WhatsApp. Registre aqui apenas pedidos já recebidos por seus canais. Confira preços, pagamento e referência com a plataforma antes de concluir.</p></aside>
 </div>;
}
