import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
const dir = mkdtempSync(join(tmpdir(), 'hisab-check-'));
const base = 'http://localhost:8187/api/v1';
const server = spawn('dotnet', [resolve('.verification/backend/MilkHisab.Api.dll'), '--urls', 'http://localhost:8187'], {cwd:dir, env:{...process.env, ConnectionStrings__Default:`Data Source=${join(dir,'test.db')}`, ASPNETCORE_ENVIRONMENT:'Development',DataProtection__KeyPath:join(dir,'keys')},windowsHide:true,stdio:['ignore','pipe','pipe']});
let serverLog = ''; server.stdout.on('data', d => serverLog += d); server.stderr.on('data', d => serverLog += d);
async function request(path,body,cookie='',expected=200) {
 const res = await fetch(base+path,{method:body === undefined ? 'GET':'POST',headers:{'Content-Type':'application/json',Cookie:cookie},body:body === undefined ? undefined:JSON.stringify(body)});
 const text = await res.text(); assert.equal(res.status,expected,`${path}: ${text}`);
 return {data:text ? JSON.parse(text):null,cookie:res.headers.get('set-cookie')?.split(';')[0] ?? cookie};
}
try {
 for(let i=0;i<80;i++){try {await fetch(base+'/health');break;}catch{await new Promise(r=>setTimeout(r,250));}}
 await request('/personal',undefined,'',401);
 const a=await request('/account/register',{name:'alice',password:'test-password-123'});
 const b=await request('/account/register',{name:'bob',password:'test-password-456'});
 const p=(await request('/personal/products',{name:'Milk',unit:'litre',rate:60},a.cookie)).data;
 const draft={date:'2026-08-20',title:'Milk',category:'Food & groceries',supplier:'Dairy',productId:p.id,quantity:2,rate:60,amount:999,paid:20,mode:'cash',note:''};
 const expense=(await request('/personal/expenses',draft,a.cookie)).data;
 assert.equal(expense.amount,120,'Server must calculate quantity × rate');
 await request('/personal/expenses',draft,b.cookie,400);
 await request(`/personal/expenses/${expense.id}/payments`,{date:'2026-09-01',amount:50,mode:'upi'},b.cookie,400);
 await request(`/personal/expenses/${expense.id}/payments`,{date:'2026-09-01',amount:101,mode:'upi'},a.cookie,400);
 await request(`/personal/expenses/${expense.id}/payments`,{date:'2026-08-19',amount:5,mode:'upi'},a.cookie,400);
 await request(`/personal/expenses/${expense.id}/payments`,{date:'2026-09-01',amount:50,mode:'upi'},a.cookie);
 await request('/personal/expenses',{...draft,productId:null,quantity:null,rate:null,title:'Travel',amount:80,paid:80,date:'2026-09-01'},a.cookie);
 await request('/personal/expenses',{...draft,paid:121},a.cookie,400);
 await request('/personal/expenses',{...draft,quantity:-1},a.cookie,400);
 const ledger=(await request('/personal',undefined,a.cookie)).data;
 assert.equal(ledger.expenses.length,2);
 assert.equal(ledger.payments.reduce((s,p)=>s+p.amount,0),150);
 assert.equal(ledger.expenses.reduce((s,e)=>s+e.amount,0),200,'Payment must not create another expense');
 const opening=ledger.expenses.filter(e=>e.date<'2026-09-01').reduce((s,e)=>s+e.amount,0)-ledger.payments.filter(p=>p.date<'2026-09-01').reduce((s,p)=>s+p.amount,0);
 assert.equal(opening,100);
 assert.equal(opening+80-130,50);
 const other=(await request('/personal',undefined,b.cookie)).data;
 assert.equal(other.expenses.length,0);assert.equal(other.products.length,0);assert.equal(other.payments.length,0);
 const customer=(await request('/customers',{name:'Archive regression',defaultRate:60},'',201)).data;
 await request('/daily-entries',{customerId:customer.id,date:'2026-09-01',quantityLitres:2,rate:60});
 const archived=await fetch(base+`/customers/${customer.id}`,{method:'DELETE'});
 assert.equal(archived.status,204);
 assert.equal((await request(`/customers/${customer.id}`)).data.isActive,false);
 assert.equal((await request('/hisab/month?year=2026&month=9')).data.customers.find(c=>c.customerId===customer.id).deliveredAmount,120,'Archiving must retain historical hisab');
 await request('/account/logout',{},a.cookie,204);
 await request('/account/login',{name:'alice',password:'wrong-password'},'',401);
 const login=await request('/account/login',{name:'alice',password:'test-password-123'});
 assert.equal((await request('/personal',undefined,login.cookie)).data.expenses.length,2);
 console.log('PASS: authentication, account isolation, server money calculation, partial payments, overpayment prevention, date validation, carry-forward, persistence, and archived business history.');
} catch (e) { console.error(serverLog.slice(-12000)); throw e; } finally { server.kill(); await new Promise(r=>server.once('exit',r)); rmSync(dir,{recursive:true,force:true}); }



