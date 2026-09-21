import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService, Customer } from '../../core/api.service';
import { readApiError } from '../../core/http-error';

@Injectable({ providedIn: 'root' })
export class CustomersService {
  private readonly api = inject(ApiService);

  readonly customers = signal<Customer[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');

  loadAll(activeOnly = false): void {
    this.loading.set(true);
    this.error.set('');
    this.api.getCustomers(activeOnly).subscribe({
      next: (list) => {
        this.customers.set(list);
        this.loading.set(false);
      },
      error: (e) => {
        this.customers.set([]);
        this.error.set(readApiError(e, 'Failed to load customers'));
        this.loading.set(false);
      },
    });
  }

  getById(id: number): Observable<Customer> {
    return this.api.getCustomer(id);
  }

  create(body: {
    name: string;
    phone?: string | null;
    address?: string | null;
    defaultRate: number;
  }): Observable<Customer> {
    return this.api.createCustomer(body).pipe(tap(() => this.loadAll()));
  }

  update(
    id: number,
    body: {
      name: string;
      phone?: string | null;
      address?: string | null;
      defaultRate: number;
      isActive: boolean;
    },
  ): Observable<Customer> {
    return this.api.updateCustomer(id, body).pipe(tap(() => this.loadAll()));
  }

  remove(id: number): Observable<void> {
    return this.api.deleteCustomer(id).pipe(tap(() => this.loadAll()));
  }
}
