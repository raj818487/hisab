import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { DayBoardItem } from '../../core/api.service';
import { DailyService } from './daily.service';
import { ToastService } from '../../shared/toast/toast.service';

@Component({
  selector: 'app-daily',
  standalone: true,
  imports: [FormsModule, DecimalPipe],
  templateUrl: './daily.page.html',
  styleUrl: './daily.page.scss',
})
export class DailyPage implements OnInit {
  readonly svc = inject(DailyService);
  private readonly toast = inject(ToastService);

  ngOnInit(): void {
    this.svc.load();
  }

  onDate(d: string): void {
    this.svc.setDate(d);
  }

  onQty(row: DayBoardItem, qty: number): void {
    this.svc.updateDraft(row.customerId, { qty: +qty });
  }

  onRate(row: DayBoardItem, rate: number): void {
    this.svc.updateDraft(row.customerId, { rate: +rate });
  }

  draftFor(customerId: number) {
    return this.svc.draft()[customerId] ?? { qty: 0, rate: 0 };
  }

  save(row: DayBoardItem): void {
    const d = this.svc.draft()[row.customerId];
    if (!d) return;
    if (+d.qty < 0 || +d.rate < 0) {
      this.toast.error('Quantity and rate cannot be negative');
      return;
    }
    this.svc.apiUpsert(row, +d.qty, +d.rate).subscribe({
      next: () => {
        this.toast.success(`Saved ${row.customerName}`);
        this.svc.load();
      },
      error: () => this.toast.error(this.svc.msg() || 'Save failed'),
    });
  }
}
