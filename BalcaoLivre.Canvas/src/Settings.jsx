import React,{useState} from 'react';
import {DownloadSimple,UploadSimple,HardDrives,Package,Receipt,Layout,WarningCircle} from '@phosphor-icons/react';
import {makeBackup,readBackup} from './backup.js';

export default function Settings({state,onRestore}){
 const [draft,setDraft]=useState(null),[error,setError]=useState('');
 const counts=[['Produtos',state.products.length,Package],['Mesas',state.tables.length,Layout],['Pedidos',state.orders.length,Receipt]];
 const bytes=new Blob([JSON.stringify(state)]).size;
 function exportBackup(){
  const url=URL.createObjectURL(new Blob([JSON.stringify(makeBackup(state),null,2)],{type:'application/json'}));
  const link=document.createElement('a');
  link.href=url;link.download=`bl-canvas-${new Date().toISOString().slice(0,10)}.json`;
  document.body.appendChild(link);link.click();link.remove();
  setTimeout(()=>URL.revokeObjectURL(url),1000);
 }
 async function chooseFile(event){
  setDraft(null);setError('');
  const file=event.target.files?.[0];
  if(!file)return;
  try{
   if(file.size>8*1024*1024)throw Error('A cópia deve ter até 8 MB.');
   setDraft(readBackup(await file.text()));
  }catch(cause){setError(cause.message)}finally{event.target.value=''}
 }
 function restore(){
  if(!draft)return;
  try{onRestore(draft.state);setDraft(null);setError('')}catch(cause){setError(cause.message)}
 }
 return <div className="settings-page">
  <div className="operations-intro"><p>Controle os dados desta prévia do restaurante.</p><span className="demo-label">BL. Canvas · Ambiente local</span></div>
  <div className="settings-stats">{counts.map(([label,value,Icon])=><div className="panel" key={label}><Icon size={24}/><span>{label}</span><strong>{value}</strong></div>)}</div>
  <section className="panel settings-data"><div className="settings-icon"><HardDrives size={30}/></div><div><h2>Dados deste navegador</h2><p>Mesas, pedidos, cardápio, estoque e caixa ficam neste dispositivo. Espaço usado nesta prévia: cerca de {(bytes/1024).toLocaleString('pt-BR',{maximumFractionDigits:0})} KB.</p></div></section>
  <div className="settings-columns"><section className="panel"><h2>Salvar uma cópia</h2><p>Baixe um arquivo com os dados atuais para guardá-lo no seu computador.</p><button onClick={exportBackup}><DownloadSimple/>Baixar cópia JSON</button><small>A cópia inclui nomes de clientes, movimentações e fotos próprias do cardápio.</small></section>
  <section className="panel"><h2>Restaurar uma cópia</h2><p>Selecione um arquivo exportado por esta prévia para conferir antes de substituir os dados atuais.</p><label className="settings-file"><UploadSimple/>Escolher arquivo JSON<input type="file" accept=".json,application/json" onChange={chooseFile}/></label>
   {draft&&<div className="settings-import"><b>Cópia pronta para restauração</b><small>Exportada em {new Date(draft.exportedAt).toLocaleString('pt-BR')} · {draft.state.products.length} produtos · {draft.state.orders.length} pedidos</small><p><WarningCircle/>Ao restaurar, os dados atuais deste navegador serão substituídos.</p><button className="primary" onClick={restore}>Restaurar esta cópia</button></div>}
   {error&&<p role="alert" className="settings-error">{error}</p>}
  </section></div>
 </div>;
}
