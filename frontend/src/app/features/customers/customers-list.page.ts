import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CustomersService } from './customers.service';
import { pageCount, paginate } from '../../shared/pagination/paginate';

@Component({
  selector: 'app-customers-list',
  standalone: true,
  imports: [RouterLink, DecimalPipe, FormsModule],
  templateUrl: './customers-list.page.html',
  styleUrl: './customers-list.page.scss',
})
export class CustomersListPage implements OnInit {
  readonly svc = inject(CustomersService);

  readonly q = signal('');
  readonly page = signal(1);
  readonly pageSize = 8;

  readonly filtered = computed(() => {
    const term = this.q().trim().toLowerCase();
    const all = this.svc.customers();
    if (!term) return all;
    return all.filter(
      (c) =>
        c.name.toLowerCase().includes(term) ||
        (c.phone ?? '').toLowerCase().includes(term) ||
        (c.address ?? '').toLowerCase().includes(term),
    );
  });

  readonly totalPages = computed(() => pageCount(this.filtered().length, this.pageSize));

  readonly pageItems = computed(() => {
    const pages = this.totalPages();
    const p = Math.min(Math.max(1, this.page()), pages);
    return paginate(this.filtered(), p, this.pageSize);
  });

  ngOnInit(): void {
    this.svc.loadAll();
  }

  onSearch(value: string): void {
    this.q.set(value);
    this.page.set(1);
  }

  go(page: number): void {
    const max = this.totalPages();
    this.page.set(Math.min(Math.max(1, page), max));
  }
}
