import React,{useState} from 'react';
import {Plus,Minus,MagnifyingGlass} from '@phosphor-icons/react';
import {addItem,total,money} from './domain.js';

export default function OrderComposer({products,onSave}){
 const [items,setItems]=useState([]),[query,setQuery]=useState(''),[customer,setCustomer]=useState(''),[note,setNote]=useState('');
 const available=products.filter(p=>p.available&&p.name.toLocaleLowerCase('pt-BR').includes(query.toLocaleLowerCase('pt-BR')));
 function quantity(id,delta){setItems(current=>current.map(i=>i.id===id?{...i,qty:Math.min(99,i.qty+delta)}:i).filter(i=>i.qty>0))}
 return <form onSubmit={e=>{e.preventDefault();if(!items.length||!customer.trim())return;onSave({id:crypto.randomUUID(),customer:customer.trim(),channel:'Retirada',items,status:'Recebido',createdAt:new Date().toISOString(),note:note.trim()})}}>
 <h2>Novo pedido de retirada</h2><p className="meta">Monte o pedido e envie para a cozinha.</p>
 <label>Nome do cliente<input required maxLength={100} value={customer} onChange={e=>setCustomer(e.target.value)}/></label>
 <label className="search"><MagnifyingGlass/><input aria-label="Buscar item para o pedido" placeholder="Buscar no cardápio" value={query} onChange={e=>setQuery(e.target.value)}/></label>
 <div className="composer-catalog">{available.map(p=><button type="button" className="picker-row" key={p.id} disabled={(items.find(i=>i.id===p.id)?.qty||0)>=99} onClick={()=>setItems(current=>addItem(current,p))}><span>{p.name}</span><b>{money(p.price)}</b><Plus/></button>)}{!available.length&&<p className="empty">Nenhum item disponível nesta busca.</p>}</div>
 <h3 className="composer-title">Resumo do pedido</h3>{items.map(i=><div className="composer-item" key={i.id}><span>{i.name}<small>{money(i.price)} cada</small></span><div className="quantity"><button type="button" aria-label={'Diminuir '+i.name} onClick={()=>quantity(i.id,-1)}><Minus/></button><b>{i.qty}</b><button type="button" disabled={i.qty>=99} aria-label={'Aumentar '+i.name} onClick={()=>quantity(i.id,1)}><Plus/></button></div><b>{money(i.price*i.qty)}</b></div>)}{!items.length&&<p className="hint">Escolha os itens no cardápio acima.</p>}
 <label>Observações para a cozinha<textarea maxLength={500} rows={2} placeholder="Ex.: sem cebola, molho à parte" value={note} onChange={e=>setNote(e.target.value)}/></label>
 <div className="line total"><b>Total</b><b>{money(total(items))}</b></div><button className="primary" disabled={!items.length||!customer.trim()}>Criar e enviar à cozinha</button><p className="hint">O pagamento será registrado separadamente no pedido.</p>
 </form>;
}
