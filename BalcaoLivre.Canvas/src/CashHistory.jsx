import React,{useState} from 'react';
import {money} from './domain.js';

export default function CashHistory({state}){
 const [selected,setSelected]=useState(0);
 const history=state.history||[],entry=history[selected];
 return <><p className="meta">Conferências e movimentações dos caixas encerrados neste navegador.</p><div className="split"><section className="panel">
 {history.map((h,i)=><button className="picker-row" key={h.id||i} onClick={()=>setSelected(i)} aria-pressed={selected===i}><span>{new Date(h.date).toLocaleString('pt-BR')}</span><b>{money(h.counted)}</b></button>)}
 {!history.length&&<p className="empty">Nenhum caixa encerrado. Os fechamentos feitos na página Caixa aparecerão aqui.</p>}
 </section>{entry&&<section className="panel"><h2>Conferência do fechamento</h2><p className="meta">{new Date(entry.date).toLocaleString('pt-BR')}</p>
 { [['Saldo inicial',entry.opening],['Entradas',entry.income],['Saídas',entry.expense],['Dinheiro esperado',entry.expected],['Dinheiro contado',entry.counted],['Diferença',entry.counted-entry.expected]].map(([label,value])=><div className="line" key={label}><span>{label}</span><b>{Number.isFinite(value)?money(value):'Não registrado'}</b></div>)}
 <h3>Lançamentos deste caixa</h3>{entry.transactions?.map(t=><div className="transaction" key={t.id}><span>{t.label}<small>{t.method}</small></span><b>{money(t.amount)}</b></div>)}
 {!entry.transactions?.length&&<p className="note">{entry.transactions?'Nenhum lançamento neste fechamento.':'Este fechamento antigo não possui detalhamento salvo.'}</p>}
 </section>}</div></>;
}
