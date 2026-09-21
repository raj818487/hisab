import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, Customer, Payment } from '../../core/api.service';
import { readApiError } from '../../core/http-error';

/** Manual ledger only — cash / UPI / other labels. No payment gateway. */
@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private readonly api = inject(ApiService);

  readonly customers = signal<Customer[]>([]);
  readonly payments = signal<Payment[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');

  loadActiveCustomers(): void {
    this.api.getCustomers(true).subscribe({
      next: (c) => this.customers.set(c),
      error: (e) => {
        this.customers.set([]);
        this.error.set(readApiError(e, 'Failed to load customers'));
      }
    });
  }

  loadMonth(year?: number, month?: number): void {
    const now = new Date();
    const y = year ?? now.getFullYear();
    const m = month ?? now.getMonth() + 1;
    this.loading.set(true);
    this.error.set('');
    this.api.getPaymentsByMonth(y, m).subscribe({
      next: (p) => {
        this.payments.set(p);
        this.loading.set(false);
      },
      error: (e) => {
        this.payments.set([]);
        this.error.set(readApiError(e, 'Failed to load payments'));
        this.loading.set(false);
      }
    });
  }

  loadByCustomer(customerId: number, year?: number, month?: number): void {
    this.api.getPaymentsByCustomer(customerId, year, month).subscribe({
      next: (p) => this.payments.set(p),
      error: (e) => {
        this.payments.set([]);
        this.error.set(readApiError(e, 'Failed to load payments'));
      }
    });
  }

  record(body: {
    customerId: number;
    amount: number;
    mode: string;
    paidOn: string;
    note?: string | null;
  }): Observable<Payment> {
    return this.api.recordPayment(body);
  }
}
