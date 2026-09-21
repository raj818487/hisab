import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService, DayBoardItem } from '../../core/api.service';
import { readApiError } from '../../core/http-error';

export interface DayDraft {
  qty: number;
  rate: number;
}

@Injectable({ providedIn: 'root' })
export class DailyService {
  private readonly api = inject(ApiService);

  readonly date = signal(new Date().toISOString().slice(0, 10));
  readonly board = signal<DayBoardItem[]>([]);
  readonly draft = signal<Record<number, DayDraft>>({});
  readonly msg = signal('');
  readonly msgOk = signal(true);
  readonly loading = signal(false);

  setDate(d: string): void {
    this.date.set(d);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.msg.set('');
    this.api.getDayBoard(this.date()).subscribe({
      next: (rows) => {
        this.board.set(rows);
        const next: Record<number, DayDraft> = {};
        for (const r of rows) {
          next[r.customerId] = {
            qty: r.quantityLitres ?? 0,
            rate: r.rate ?? r.defaultRate
          };
        }
        this.draft.set(next);
        this.loading.set(false);
      },
      error: (e) => {
        this.board.set([]);
        this.draft.set({});
        this.msgOk.set(false);
        this.msg.set(readApiError(e, 'Failed to load day-board'));
        this.loading.set(false);
      }
    });
  }

  updateDraft(customerId: number, patch: Partial<DayDraft>): void {
    const cur = this.draft();
    const row = cur[customerId] ?? { qty: 0, rate: 0 };
    this.draft.set({ ...cur, [customerId]: { ...row, ...patch } });
  }

  /** Used by page + toast wiring */
  apiUpsert(row: DayBoardItem, qty: number, rate: number): Observable<unknown> {
    return this.api
      .upsertDailyEntry({
        customerId: row.customerId,
        date: this.date(),
        quantityLitres: qty,
        rate
      })
      .pipe(
        tap({
          next: () => {
            this.msgOk.set(true);
            this.msg.set(`Saved ${row.customerName}`);
          },
          error: (e) => {
            this.msgOk.set(false);
            this.msg.set(readApiError(e, 'Save failed'));
          }
        })
      );
  }

  save(row: DayBoardItem): void {
    const d = this.draft()[row.customerId];
    if (!d) return;
    this.apiUpsert(row, +d.qty, +d.rate).subscribe({
      next: () => this.load(),
      error: () => undefined
    });
  }
}
