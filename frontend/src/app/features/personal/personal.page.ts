import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PersonalService, localDate } from './personal.service';
import { Expense, ExpenseDraft, Product } from './personal.models';

interface CalCell {
  date: string;
  day: number;
  inMonth: boolean;
  taken: boolean;
  expenses: Expense[];
}

@Component({
  selector: 'app-personal',
  standalone: true,
  imports: [FormsModule, DecimalPipe],
  templateUrl: './personal.page.html',
  styleUrl: './personal.page.scss',
})
export class PersonalPage implements OnInit {
  readonly svc = inject(PersonalService);
  readonly today = localDate();

  username = '';
  password = '';
  register = true;
  showExpense = false;
  showProduct = false;
  productName = '';
  unit = 'litre';
  productRate = 0;
  paymentExpense: Expense | null = null;
  paymentAmount = 0;
  paymentDate = this.today;
  paymentMode = 'cash';
  paidNow = true;
  editingId: number | null = null;
  draft: ExpenseDraft = this.blank();

  calProductId = signal(0);
  calMonth = signal(localDate().slice(0, 7));
  selectedDay = signal<string | null>(null);

  readonly calProduct = computed(
    () => this.svc.data().products.find((p) => p.id === this.calProductId()) ?? null,
  );

  readonly calCells = computed(() => {
    const ym = this.calMonth();
    const [y, m] = ym.split('-').map(Number);
    const first = new Date(y, m - 1, 1);
    const startPad = first.getDay();
    const daysInMonth = new Date(y, m, 0).getDate();
    const pid = this.calProductId();
    const expenses = this.svc
      .data()
      .expenses.filter((e) => e.date.startsWith(ym) && (!pid || e.productId === pid));
    const byDate = new Map<string, Expense[]>();
    for (const e of expenses) {
      const list = byDate.get(e.date) ?? [];
      list.push(e);
      byDate.set(e.date, list);
    }
    const cells: CalCell[] = [];
    for (let i = 0; i < startPad; i++) {
      cells.push({ date: '', day: 0, inMonth: false, taken: false, expenses: [] });
    }
    for (let d = 1; d <= daysInMonth; d++) {
      const date = `${ym}-${String(d).padStart(2, '0')}`;
      const list = byDate.get(date) ?? [];
      cells.push({ date, day: d, inMonth: true, taken: list.length > 0, expenses: list });
    }
    while (cells.length % 7 !== 0) {
      cells.push({ date: '', day: 0, inMonth: false, taken: false, expenses: [] });
    }
    return cells;
  });

  readonly dayExpenses = computed(() => {
    const day = this.selectedDay();
    if (!day) return [] as Expense[];
    const pid = this.calProductId();
    return this.svc.data().expenses.filter((e) => e.date === day && (!pid || e.productId === pid));
  });

  ngOnInit(): void {
    void this.svc.init();
  }

  async logout(): Promise<void> {
    await this.svc.logout();
    if (!this.svc.user()) {
      this.draft = this.blank();
      this.paymentExpense = null;
      this.showExpense = false;
      this.showProduct = false;
      this.editingId = null;
      this.username = '';
      this.password = '';
      this.productName = '';
      this.selectedDay.set(null);
    }
  }

  blank(): ExpenseDraft {
    return {
      date: localDate(),
      title: '',
      category: 'Food & groceries',
      supplier: '',
      productId: null,
      quantity: 1,
      rate: 0,
      amount: 0,
      paid: 0,
      mode: 'cash',
      note: '',
    };
  }

  selectProduct(): void {
    const p = this.svc.data().products.find((x) => x.id === Number(this.draft.productId));
    if (p) {
      this.draft.title = p.name;
      this.draft.rate = p.rate;
      this.calculate();
    }
  }

  calculate(): void {
    if (this.draft.productId) {
      this.draft.amount =
        Math.round((this.draft.quantity * this.draft.rate + Number.EPSILON) * 100) / 100;
    }
  }

  openNewExpense(): void {
    this.editingId = null;
    this.draft = this.blank();
    this.paidNow = true;
    this.showExpense = true;
  }

  beginEdit(e: Expense): void {
    this.editingId = e.id;
    this.draft = {
      date: e.date,
      title: e.title,
      category: e.category,
      supplier: e.supplier ?? '',
      productId: e.productId ?? null,
      quantity: e.quantity ?? 1,
      rate: e.rate ?? 0,
      amount: e.amount,
      paid: 0,
      mode: 'cash',
      note: e.note ?? '',
    };
    this.paidNow = false;
    this.showExpense = true;
    setTimeout(() =>
      document
        .getElementById('expense-form')
        ?.scrollIntoView({ behavior: 'smooth', block: 'center' }),
    );
  }

  async saveExpense(): Promise<void> {
    const draft = {
      ...this.draft,
      paid: this.editingId ? 0 : this.paidNow ? this.draft.amount : this.draft.paid,
    };
    const ok = this.editingId
      ? await this.svc.updateExpense(this.editingId, draft)
      : await this.svc.addExpense(draft);
    if (ok) {
      this.showExpense = false;
      this.editingId = null;
      this.draft = this.blank();
      this.paidNow = true;
    }
  }

  async removeExpense(e: Expense): Promise<void> {
    const due = this.svc.due(e);
    const hint = due > 0 ? ` (â‚¹${due.toFixed(2)} still due)` : '';
    if (!confirm(`Remove "${e.title}" on ${e.date}${hint}? This also removes its payments.`))
      return;
    if (await this.svc.deleteExpense(e.id)) {
      if (this.editingId === e.id) {
        this.showExpense = false;
        this.editingId = null;
        this.draft = this.blank();
      }
      if (this.paymentExpense?.id === e.id) this.paymentExpense = null;
    }
  }

  async saveProduct(): Promise<void> {
    if (await this.svc.addProduct(this.productName, this.unit, this.productRate)) {
      this.showProduct = false;
      this.productName = '';
      this.productRate = 0;
      const last = this.svc.data().products.at(-1);
      if (last && !this.calProductId()) this.calProductId.set(last.id);
    }
  }

  async removeProduct(p: Product): Promise<void> {
    if (!confirm(`Remove product "${p.name}"?`)) return;
    if ((await this.svc.deleteProduct(p.id)) && this.calProductId() === p.id) {
      this.calProductId.set(0);
    }
  }

  beginPayment(e: Expense): void {
    this.paymentExpense = e;
    this.paymentAmount = this.svc.due(e);
    this.paymentDate = this.today;
    setTimeout(() =>
      document
        .getElementById('payment-form')
        ?.scrollIntoView({ behavior: 'smooth', block: 'center' }),
    );
  }

  async savePayment(): Promise<void> {
    if (
      this.paymentExpense &&
      (await this.svc.pay(
        this.paymentExpense.id,
        this.paymentDate,
        this.paymentAmount,
        this.paymentMode,
      ))
    ) {
      this.paymentExpense = null;
    }
  }

  setMonth(value: string): void {
    if (/^\d{4}-\d{2}$/.test(value)) this.svc.month.set(value);
  }

  setCalMonth(value: string): void {
    if (/^\d{4}-\d{2}$/.test(value)) {
      this.calMonth.set(value);
      this.selectedDay.set(null);
    }
  }

  shiftCalMonth(delta: number): void {
    const [y, m] = this.calMonth().split('-').map(Number);
    const d = new Date(y, m - 1 + delta, 1);
    this.calMonth.set(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`);
    this.selectedDay.set(null);
  }

  onCalProduct(id: number | string): void {
    this.calProductId.set(Number(id) || 0);
    this.selectedDay.set(null);
  }

  pickDay(cell: CalCell): void {
    if (!cell.inMonth || !cell.date || cell.date > this.today) return;
    this.selectedDay.set(cell.date);
  }

  takeOnDay(date?: string): void {
    const p = this.calProduct();
    if (!p) {
      this.svc.error.set('Pick a product first, then tap a date.');
      return;
    }
    const day = date ?? this.selectedDay() ?? this.today;
    if (day > this.today) return;
    this.editingId = null;
    this.draft = {
      ...this.blank(),
      date: day,
      title: p.name,
      productId: p.id,
      quantity: 1,
      rate: p.rate,
      amount: p.rate,
      category: 'Food & groceries',
    };
    this.paidNow = true;
    this.showExpense = true;
    this.calMonth.set(day.slice(0, 7));
    this.selectedDay.set(day);
    setTimeout(() =>
      document
        .getElementById('expense-form')
        ?.scrollIntoView({ behavior: 'smooth', block: 'center' }),
    );
  }
}
