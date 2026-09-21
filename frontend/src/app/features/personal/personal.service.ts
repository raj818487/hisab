import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { readApiError } from '../../core/http-error';
import { Expense, ExpenseDraft, ExpensePayment, PersonalData } from './personal.models';

export function localDate(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}
@Injectable({ providedIn: 'root' })
export class PersonalService {
  private readonly api = inject(ApiService);
  readonly user = signal('');
  readonly ready = signal(false);
  readonly busy = signal(false);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  // ponytail: load one person's history; add server paging when ledgers reach thousands of entries.
  readonly data = signal<PersonalData>({ products: [], expenses: [], payments: [] });
  readonly month = signal(localDate().slice(0, 7));
  readonly product = signal(0);
  readonly search = signal('');
  readonly category = signal('');
  readonly unpaidOnly = signal(false);
  readonly receipt = signal<ExpensePayment | null>(null);
  readonly categories = ['Food & groceries', 'Transport', 'Home & bills', 'Health', 'Shopping', 'Education', 'Other'];
  readonly selected = computed(() => this.data().expenses.filter(e =>
    (!this.product() || e.productId === this.product()) &&
    (!this.category() || e.category === this.category()) &&
    `${e.title} ${e.supplier ?? ''} ${e.note ?? ''}`.toLowerCase().includes(this.search().toLowerCase())));
  readonly rows = computed(() => this.selected().filter(e => e.date.startsWith(this.month()) && (!this.unpaidOnly() || this.due(e) > 0)));
  readonly totals = computed(() => {
    const start = `${this.month()}-01`;
    const end = this.month() === '9999-12' ? '9999-12-32' : (() => { const [y,m] = this.month().split('-').map(Number); return `${m === 12 ? y + 1 : y}-${String(m === 12 ? 1 : m + 1).padStart(2,'0')}-01`; })();
    const entries = this.selected();
    const ids = new Set(entries.map(e => e.id));
    const payments = this.data().payments.filter(p => ids.has(p.expenseId));
    const sum = (items: { amount: number }[]) => items.reduce((n, e) => n + Math.round(e.amount * 100), 0) / 100;
    const opening = sum(entries.filter(e => e.date < start)) - sum(payments.filter(p => p.date < start));
    const purchases = sum(entries.filter(e => e.date >= start && e.date < end));
    const paid = sum(payments.filter(p => p.date >= start && p.date < end));
    return { opening, purchases, paid, closing: opening + purchases - paid };
  });
  readonly breakdown = computed(() => {
    const entries = this.selected().filter(e => e.date.startsWith(this.month()));
    return this.categories.map(name => ({ name, amount: entries.filter(e => e.category === name).reduce((n,e) => n + e.amount,0) })).filter(c => c.amount > 0).sort((a,b) => b.amount-a.amount);
  });
  readonly outstanding = computed(() => this.selected().filter(e => this.due(e) > 0));
  due(e: Expense): number { return Math.max(0, Math.round((e.amount - this.data().payments.filter(p => p.expenseId === e.id).reduce((n,p) => n+p.amount,0))*100)/100); }
  productName(id?: number): string { return this.data().products.find(p => p.id === id)?.name ?? 'General expense'; }
  expense(id: number): Expense | undefined { return this.data().expenses.find(e => e.id === id); }
  async init(): Promise<void> {
    this.error.set('');
    try { const u = await firstValueFrom(this.api.personalSession()); this.user.set(u.name); await this.load(); }
    catch (e: any) { if (e.status === 401) this.clearSession(); else this.error.set(readApiError(e, 'Cannot connect to your account.')); }
    finally { this.ready.set(true); }
  }
  async authenticate(name: string, password: string, register: boolean): Promise<void> {
    await this.run(async () => { const u = await firstValueFrom(this.api.personalAuth(name,password,register)); this.user.set(u.name); await this.load(); });
  }
  async logout(): Promise<void> {
    await this.run(async () => { await firstValueFrom(this.api.personalLogout()); this.clearSession(); });
  }
  private clearSession(): void {
    this.user.set(''); this.data.set({products:[],expenses:[],payments:[]}); this.receipt.set(null);
    this.search.set(''); this.category.set(''); this.product.set(0); this.unpaidOnly.set(false);
  }
  private async refreshSaved(): Promise<void> {
    try { await this.load(); }
    catch { this.error.set('Saved successfully, but the list could not refresh. Reload the page; do not submit again.'); }
  }
  async load(): Promise<void> {
    this.loading.set(true);
    try { this.data.set(await firstValueFrom(this.api.personalData())); }
    finally { this.loading.set(false); }
  }
  async run(action: () => Promise<void>): Promise<boolean> {
    if (this.busy()) return false;
    this.busy.set(true); this.error.set(''); this.message.set('');
    try { await action(); return true; }
    catch (e) { this.error.set(readApiError(e, 'Could not save. Please try again.')); return false; }
    finally { this.busy.set(false); }
  }
  async addExpense(draft: ExpenseDraft): Promise<boolean> {
    return this.run(async () => { const e = await firstValueFrom(this.api.personalExpense(draft)); await this.refreshSaved(); this.month.set(e.date.slice(0,7)); this.message.set('Expense saved. Your hisab is up to date.'); if (draft.paid > 0) this.receipt.set(this.data().payments.find(p => p.expenseId === e.id) ?? null); });
  }
  async addProduct(name: string, unit: string, rate: number): Promise<boolean> {
    return this.run(async () => { await firstValueFrom(this.api.personalProduct(name,unit,rate)); await this.refreshSaved(); this.message.set('Product added. Select it when recording an expense.'); });
  }
  async pay(id: number, date: string, amount: number, mode: string): Promise<boolean> {
    return this.run(async () => { const p = await firstValueFrom(this.api.personalPay(id,date,amount,mode)); await this.refreshSaved(); this.receipt.set(p); this.message.set('Payment recorded. Purchase totals are unchanged.'); });
  }
  async updateExpense(id: number, draft: ExpenseDraft): Promise<boolean> {
    return this.run(async () => {
      await firstValueFrom(this.api.personalExpenseUpdate(id, draft));
      await this.refreshSaved();
      this.message.set('Expense updated.');
    });
  }

  async deleteExpense(id: number): Promise<boolean> {
    return this.run(async () => {
      await firstValueFrom(this.api.personalExpenseDelete(id));
      await this.refreshSaved();
      this.receipt.set(null);
      this.message.set('Expense removed.');
    });
  }

  async deleteProduct(id: number): Promise<boolean> {
    return this.run(async () => {
      await firstValueFrom(this.api.personalProductDelete(id));
      await this.refreshSaved();
      this.message.set('Product removed.');
    });
  }
  downloadReceipt(): void {
    const p = this.receipt(); if (!p) return;
    const e = this.expense(p.expenseId); if (!e) return;
    const escape = (s: string) => s.replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]!));
    const html = `<!doctype html><html lang="en"><meta charset="utf-8"><title>Hisab payment ${p.id}</title><style>body{font:16px system-ui;max-width:640px;margin:60px auto;padding:24px;color:#061b31}h1{color:#533afd}dt{margin-top:20px;color:#64748d}dd{margin:6px 0;font-size:20px}</style><h1>Hisab Ã‚Â· Payment record</h1><p>Personal record of a manual payment</p><dl><dt>Reference</dt><dd>HP-${p.id}</dd><dt>Expense</dt><dd>${escape(e.title)}</dd><dt>Supplier</dt><dd>${escape(e.supplier || 'Not specified')}</dd><dt>Paid on</dt><dd>${p.date}</dd><dt>Amount</dt><dd>₹ ${p.amount.toFixed(2)}</dd><dt>Mode</dt><dd>${escape(p.mode)}</dd></dl></html>`;
    const url = URL.createObjectURL(new Blob([html],{type:'text/html'}));
    const a = document.createElement('a'); a.href=url; a.download=`hisab-payment-${p.id}.html`; a.click(); setTimeout(() => URL.revokeObjectURL(url),1000);
  }
  print(): void { window.print(); }
}

