import React,{useEffect,useRef} from 'react';
import {House,Layout,Receipt,ChefHat,BookOpen,Package,Users,Wallet,ChartBar,Robot,Scan,Plugs,Gear,CaretRight,DeviceMobile,X} from '@phosphor-icons/react';

export default function SpaceMenu({onClose,onNavigate,onScanner,onAssistant,onModal,onKitchen}){
 const root=useRef(null),close=useRef(null);
 useEffect(()=>{
  const previous=document.activeElement;
  const overflow=document.body.style.overflow;
  document.body.style.overflow='hidden';close.current?.focus();
  const key=e=>{
   if(e.key==='Escape'){e.preventDefault();e.stopPropagation();onClose();}
   if(e.key==='Tab'){
    const nodes=[...root.current.querySelectorAll('button:not(:disabled),a[href],[tabindex="0"]')];
    const first=nodes[0],last=nodes[nodes.length-1];
    if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}
    else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}
   }
  };
  const node=root.current;node.addEventListener('keydown',key);
  return()=>{document.body.style.overflow=overflow;node.removeEventListener('keydown',key);previous?.focus();};
 },[]);
 const sections=[
  {title:'Dia a dia',items:[
   ['Início',House,()=>onNavigate('Início')],
   ['Salão e mesas',Layout,()=>onNavigate('Salão')],
   ['Pedidos e delivery',Receipt,()=>onNavigate('Pedidos')],
   ['Cozinha',ChefHat,onKitchen],
   ['Cardápio',BookOpen,()=>onNavigate('Cardápio')],
   ['Estoque e fichas técnicas',Package,()=>onNavigate('Estoque')]
  ]},
  {title:'Seu negócio',items:[
   ['Equipe e permissões',Users,()=>onModal('roadmap'),'Em breve'],
   ['Caixa e financeiro',Wallet,()=>onNavigate('Caixa')],
   ['Histórico de fechamentos',ChartBar,()=>onNavigate('Histórico')],
   ['Relatórios',ChartBar,()=>onNavigate('Relatórios')],
   ['Integrações',Plugs,()=>onModal('integrations'),'Não conectadas'],
   ['Configurações',Gear,()=>onNavigate('Configurações')]
  ]},
  {title:'Inteligência e automação',items:[
   ['Assistente BL.',Robot,onAssistant],
   ['Scanner',Scan,onScanner],
   ['Automações com IA',Robot,()=>onModal('automation'),'Planejado']
  ]}
 ];
 return <div className="space-backdrop" onClick={onClose}><section ref={root} className="space-menu" role="dialog" aria-modal="true" aria-labelledby="space-title" onClick={e=>e.stopPropagation()}>
  <div className="space-menu-top"><span className="space-mark">BL<span>.</span></span><button ref={close} onClick={onClose} aria-label="Fechar menu do restaurante"><X size={28} weight="light"/></button></div>
  <div className="space-heading"><p>BALCÃO LIVRE</p><h2 id="space-title">Seu restaurante<span className="orange">.</span></h2></div>
  <nav aria-label="Todas as áreas do restaurante">{sections.map(section=><section className="space-section" key={section.title}><h3>{section.title}</h3>{section.items.map(([label,Icon,action,status])=><button className="space-row" onClick={action} key={label}><Icon size={28} weight="regular"/><span>{label}{status&&<small>{status}</small>}</span><CaretRight size={20} weight="light"/></button>)}</section>)}</nav>
  <footer className="space-footer"><p><DeviceMobile size={23}/>Dados salvos neste navegador</p><small>BL. Canvas · Ambiente de demonstração<br/>Sincronização em nuvem ainda não conectada.</small></footer>
 </section></div>;
}
