import { Injectable, inject, signal } from '@angular/core';
import { ApiService, Receipt } from '../../core/api.service';
import { readApiError } from '../../core/http-error';

/**
 * Receipt download + print live here (not in payments).
 * No payment gateway — view / HTML download / window.print only.
 */
@Injectable({ providedIn: 'root' })
export class ReceiptsService {
  private readonly api = inject(ApiService);

  readonly receipt = signal<Receipt | null>(null);
  readonly error = signal('');
  readonly loading = signal(false);

  private paymentId = 0;

  load(paymentId: number): void {
    this.paymentId = paymentId;
    this.loading.set(true);
    this.error.set('');
    this.receipt.set(null);
    this.api.getReceipt(paymentId).subscribe({
      next: (r) => {
        this.receipt.set(r);
        this.loading.set(false);
      },
      error: (e) => {
        this.error.set(readApiError(e, 'Receipt not found'));
        this.loading.set(false);
      }
    });
  }

  modeLabel(mode: string): string {
    if (mode === 'upi') return 'UPI (manual record)';
    if (mode === 'other') return 'Other';
    return 'Cash';
  }

  /** Download HTML blob from GET /receipts/:id/html */
  downloadHtml(): void {
    if (!this.paymentId) return;
    this.api.downloadReceiptHtml(this.paymentId).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `receipt-${this.paymentId}.html`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: (e) => this.error.set(readApiError(e, 'Download failed'))
    });
  }

  /** Browser print / Save as PDF */
  print(): void {
    window.print();
  }
}
