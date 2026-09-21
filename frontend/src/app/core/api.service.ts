import {
  Expense,
  ExpenseDraft,
  ExpensePayment,
  PersonalData,
  Product,
} from '../features/personal/personal.models';
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/** Thin HTTP transport only â€” feature services own orchestration. */

export interface Customer {
  id: number;
  name: string;
  phone?: string | null;
  address?: string | null;
  defaultRate: number;
  isActive: boolean;
  createdAt: string;
}

export interface DayBoardItem {
  customerId: number;
  customerName: string;
  defaultRate: number;
  entryId?: number | null;
  quantityLitres?: number | null;
  rate?: number | null;
  amount?: number | null;
  note?: string | null;
}

export interface HisabMonthSummary {
  year: number;
  month: number;
  totalDelivered: number;
  totalPaid: number;
  totalDue: number;
  customers: CustomerHisab[];
}

export interface CustomerHisab {
  customerId: number;
  customerName: string;
  deliveredAmount: number;
  paidAmount: number;
  dueAmount: number;
  totalLitres: number;
}

export interface Payment {
  id: number;
  customerId: number;
  customerName?: string | null;
  amount: number;
  mode: string;
  paidOn: string;
  note?: string | null;
  receiptNumber: string;
  createdAt: string;
}

export interface Receipt {
  receiptNumber: string;
  paymentId: number;
  customerId: number;
  customerName: string;
  customerPhone?: string | null;
  customerAddress?: string | null;
  amount: number;
  mode: string;
  paidOn: string;
  note?: string | null;
  generatedAt: string;
  monthDelivered: number;
  monthPaid: number;
  monthDue: number;
  year: number;
  month: number;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  personalSession() {
    return this.http.get<{ name: string }>(`${this.base}/account`);
  }
  personalAuth(name: string, password: string, register: boolean) {
    return this.http.post<{
      accessToken: string;
      refreshToken: string;
      expiresAt: string;
      name: string;
    }>(`${this.base}/account/${register ? 'register' : 'login'}`, { name, password });
  }
  personalLogout() {
    return this.http.post(`${this.base}/account/logout`, {});
  }
  personalData() {
    return this.http.get<PersonalData>(`${this.base}/personal`);
  }
  personalProduct(name: string, unit: string, rate: number) {
    return this.http.post<Product>(`${this.base}/personal/products`, { name, unit, rate });
  }
  personalExpense(body: ExpenseDraft) {
    return this.http.post<Expense>(`${this.base}/personal/expenses`, body);
  }
  personalPay(id: number, date: string, amount: number, mode: string) {
    return this.http.post<ExpensePayment>(`${this.base}/personal/expenses/${id}/payments`, {
      date,
      amount,
      mode,
    });
  }
  personalExpenseUpdate(id: number, body: ExpenseDraft) {
    return this.http.put<Expense>(`${this.base}/personal/expenses/${id}`, body);
  }
  personalExpenseDelete(id: number) {
    return this.http.delete(`${this.base}/personal/expenses/${id}`);
  }
  personalProductDelete(id: number) {
    return this.http.delete(`${this.base}/personal/products/${id}`);
  }
  // --- Customers ---
  getCustomers(activeOnly = false): Observable<Customer[]> {
    const params = new HttpParams().set('activeOnly', String(activeOnly));
    return this.http.get<Customer[]>(`${this.base}/customers`, { params });
  }

  getCustomer(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${this.base}/customers/${id}`);
  }

  createCustomer(body: {
    name: string;
    phone?: string | null;
    address?: string | null;
    defaultRate: number;
  }): Observable<Customer> {
    return this.http.post<Customer>(`${this.base}/customers`, body);
  }

  updateCustomer(
    id: number,
    body: {
      name: string;
      phone?: string | null;
      address?: string | null;
      defaultRate: number;
      isActive: boolean;
    },
  ): Observable<Customer> {
    return this.http.put<Customer>(`${this.base}/customers/${id}`, body);
  }

  deleteCustomer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/customers/${id}`);
  }

  // --- Daily entries ---
  getDayBoard(date: string): Observable<DayBoardItem[]> {
    const params = new HttpParams().set('date', date);
    return this.http.get<DayBoardItem[]>(`${this.base}/daily-entries/day-board`, { params });
  }

  upsertDailyEntry(body: {
    customerId: number;
    date: string;
    quantityLitres: number;
    rate: number;
    note?: string | null;
  }): Observable<unknown> {
    return this.http.post(`${this.base}/daily-entries`, body);
  }

  // --- Hisab ---
  getHisabMonth(year: number, month: number): Observable<HisabMonthSummary> {
    const params = new HttpParams().set('year', year).set('month', month);
    return this.http.get<HisabMonthSummary>(`${this.base}/hisab/month`, { params });
  }

  // --- Payments (manual ledger only â€” no gateway) ---
  recordPayment(body: {
    customerId: number;
    amount: number;
    mode: string;
    paidOn: string;
    note?: string | null;
  }): Observable<Payment> {
    return this.http.post<Payment>(`${this.base}/payments`, body);
  }

  getPaymentsByMonth(year: number, month: number): Observable<Payment[]> {
    const params = new HttpParams().set('year', year).set('month', month);
    return this.http.get<Payment[]>(`${this.base}/payments/by-month`, { params });
  }

  getPaymentsByCustomer(customerId: number, year?: number, month?: number): Observable<Payment[]> {
    let params = new HttpParams();
    if (year != null) params = params.set('year', year);
    if (month != null) params = params.set('month', month);
    return this.http.get<Payment[]>(`${this.base}/payments/by-customer/${customerId}`, {
      params,
    });
  }

  getPayment(id: number): Observable<Payment> {
    return this.http.get<Payment>(`${this.base}/payments/${id}`);
  }

  // --- Receipts ---
  getReceipt(paymentId: number): Observable<Receipt> {
    return this.http.get<Receipt>(`${this.base}/receipts/${paymentId}`);
  }

  downloadReceiptHtml(paymentId: number): Observable<Blob> {
    return this.http.get(`${this.base}/receipts/${paymentId}/html`, {
      responseType: 'blob',
    });
  }
}
