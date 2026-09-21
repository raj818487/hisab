import { Injectable, inject, signal } from '@angular/core';
import { ApiService, HisabMonthSummary } from '../../core/api.service';
import { readApiError } from '../../core/http-error';

@Injectable({ providedIn: 'root' })
export class HisabService {
  private readonly api = inject(ApiService);

  readonly year = signal(new Date().getFullYear());
  readonly month = signal(new Date().getMonth() + 1);
  readonly summary = signal<HisabMonthSummary | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');

  setYear(y: number): void {
    this.year.set(+y);
    this.load();
  }

  setMonth(m: number): void {
    this.month.set(+m);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.getHisabMonth(this.year(), this.month()).subscribe({
      next: (s) => {
        this.summary.set(s);
        this.loading.set(false);
      },
      error: (e) => {
        this.summary.set(null);
        this.error.set(readApiError(e, 'Failed to load hisab'));
        this.loading.set(false);
      },
    });
  }
}
