import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { PaymentsService } from './payments.service';
import { readApiError } from '../../core/http-error';
import { ToastService } from '../../shared/toast/toast.service';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './payments.page.html',
  styleUrl: './payments.page.scss'
})
export class PaymentsPage implements OnInit {
  readonly svc = inject(PaymentsService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  customerId = 0;
  amount = 0;
  mode = 'cash';
  paidOn = new Date().toISOString().slice(0, 10);
  note = '';
  error = signal('');
  saving = signal(false);

  ngOnInit(): void {
    this.svc.loadActiveCustomers();
    this.svc.loadMonth();
  }

  record(): void {
    this.error.set('');
    if (!this.customerId) {
      this.error.set('Select a customer');
      this.toast.error('Select a customer');
      return;
    }
    if (this.amount <= 0) {
      this.error.set('Amount must be greater than zero');
      this.toast.error('Amount must be greater than zero');
      return;
    }
    this.saving.set(true);
    this.svc
      .record({
        customerId: this.customerId,
        amount: this.amount,
        mode: this.mode,
        paidOn: this.paidOn,
        note: this.note || null
      })
      .subscribe({
        next: (p) => {
          this.toast.success('Payment recorded');
          this.router.navigate(['/receipts', p.id]);
        },
        error: (e) => {
          const msg = readApiError(e, 'Failed to record payment');
          this.error.set(msg);
          this.toast.error(msg);
          this.saving.set(false);
        }
      });
  }
}
