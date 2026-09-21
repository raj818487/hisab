import { Injectable, inject, signal, computed } from '@angular/core';
import { ApiService, DayBoardItem, HisabMonthSummary } from '../../core/api.service';
import { readApiError } from '../../core/http-error';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly api = inject(ApiService);

  readonly summary = signal<HisabMonthSummary | null>(null);
  readonly board = signal<DayBoardItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');

  readonly todayTaken = computed(
    () =>
      this.board().filter(
        (r) => (r.entryId != null || r.quantityLitres != null) && (r.quantityLitres ?? 0) > 0,
      ).length,
  );
  readonly todaySkip = computed(
    () =>
      this.board().filter(
        (r) =>
          (r.entryId != null || r.quantityLitres != null) &&
          ((r.quantityLitres ?? 0) === 0 || (r.note ?? '').toLowerCase().includes('skip')),
      ).length,
  );
  readonly todayPending = computed(
    () => this.board().filter((r) => r.entryId == null && r.quantityLitres == null).length,
  );
  readonly todayTotal = computed(() => this.board().length);

  loadCurrentMonth(): void {
    const now = new Date();
    const year = now.getFullYear();
    const month = now.getMonth() + 1;
    const date = now.toISOString().slice(0, 10);
    this.loading.set(true);
    this.error.set('');
    let pending = 2;
    const done = () => {
      pending -= 1;
      if (pending <= 0) this.loading.set(false);
    };

    this.api.getHisabMonth(year, month).subscribe({
      next: (s) => {
        this.summary.set(s);
        done();
      },
      error: (e) => {
        this.summary.set({
          year,
          month,
          totalDelivered: 0,
          totalPaid: 0,
          totalDue: 0,
          customers: [],
        });
        this.error.set(readApiError(e, 'Could not load month summary (API offline?).'));
        done();
      },
    });

    this.api.getDayBoard(date).subscribe({
      next: (rows) => {
        this.board.set(rows);
        done();
      },
      error: (e) => {
        this.board.set([]);
        if (!this.error()) {
          this.error.set(readApiError(e, "Could not load today's day-board."));
        }
        done();
      },
    });
  }
}
