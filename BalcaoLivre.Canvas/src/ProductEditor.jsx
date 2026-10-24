import React,{useState} from 'react';
import {BookOpen,Camera,X} from '@phosphor-icons/react';
import {parseMoney} from './domain.js';

async function preparePhoto(file){
 if(!['image/jpeg','image/png','image/webp'].includes(file.type))throw Error('Escolha uma imagem JPG, PNG ou WebP.');
 if(file.size>8*1024*1024)throw Error('A imagem deve ter até 8 MB.');
 const url=URL.createObjectURL(file);
 try{
  const image=await new Promise((resolve,reject)=>{
   const element=new Image();
   element.onload=()=>resolve(element);
   element.onerror=()=>reject(Error('Não foi possível abrir essa imagem.'));
   element.src=url;
  });
  if(!image.naturalWidth||!image.naturalHeight||image.naturalWidth*image.naturalHeight>25000000)throw Error('A imagem é grande demais para esta prévia.');
  const scale=Math.min(1,720/image.naturalWidth,540/image.naturalHeight);
  const canvas=document.createElement('canvas');
  canvas.width=Math.max(1,Math.round(image.naturalWidth*scale));
  canvas.height=Math.max(1,Math.round(image.naturalHeight*scale));
  const context=canvas.getContext('2d');
  if(!context)throw Error('Não foi possível preparar a foto.');
  context.fillStyle='#fff';
  context.fillRect(0,0,canvas.width,canvas.height);
  context.drawImage(image,0,0,canvas.width,canvas.height);
  const data=canvas.toDataURL('image/jpeg',0.76);
  if(data.length>300000)throw Error('A foto ficou grande demais. Escolha uma imagem menor.');
  return data;
 }finally{URL.revokeObjectURL(url)}
}

export default function ProductEditor({product,onSave}){
 const [photoData,setPhotoData]=useState(product?.photoData||'');
 const [busy,setBusy]=useState(false),[error,setError]=useState('');
 async function changePhoto(event){
  const file=event.target.files?.[0];
  if(!file)return;
  setBusy(true);setError('');
  try{setPhotoData(await preparePhoto(file))}catch(cause){setError(cause.message)}finally{setBusy(false);event.target.value=''}
 }
 function submit(event){
  event.preventDefault();
  const form=new FormData(event.currentTarget);
  try{
   const name=String(form.get('name')||'').trim();
   if(!name)throw Error('Informe o nome do item.');
   const item={...product,id:product?.id||crypto.randomUUID(),name,price:parseMoney(form.get('price')),category:String(form.get('category')),available:product?.available??true,photo:product?.photo||'',photoData};
   onSave(item);
  }catch(cause){setError(cause.message)}
 }
 return <form className="product-editor" onSubmit={submit}>
  <h2>{product?'Editar item':'Novo item'}</h2>
  <div className={'food product-photo-preview '+(product?.photo||'')}>{photoData?<img src={photoData} alt="Prévia da foto do item"/>:<BookOpen size={40}/>}</div>
  <label>Foto do item<input type="file" accept="image/jpeg,image/png,image/webp" onChange={changePhoto}/></label>
  {photoData&&<button type="button" className="product-remove-photo" onClick={()=>setPhotoData('')}><X/>Remover foto própria</button>}
  <p className="hint">A foto própria fica salva somente neste navegador. JPG, PNG ou WebP de até 8 MB.</p>
  <label>Nome<input name="name" required maxLength={120} defaultValue={product?.name}/></label>
  <label>Preço<input name="price" type="number" min="0" step="0.01" required defaultValue={product?product.price/100:''}/></label>
  <label>Categoria<select name="category" defaultValue={product?.category||'Pratos'}>{['Pratos','Lanches','Bebidas','Sobremesas'].map(category=><option key={category}>{category}</option>)}</select></label>
  {error&&<p role="alert" className="note">{error}</p>}
  <button className="primary" disabled={busy}><Camera/>{busy?'Preparando foto…':'Salvar item'}</button>
 </form>;
}
