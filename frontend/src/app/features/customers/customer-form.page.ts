import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CustomersService } from './customers.service';
import { readApiError } from '../../core/http-error';

@Component({
  selector: 'app-customer-form',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './customer-form.page.html',
  styleUrl: './customer-form.page.scss'
})
export class CustomerFormPage implements OnInit {
  private readonly svc = inject(CustomersService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  isNew = signal(true);
  id: number | null = null;
  name = '';
  phone = '';
  address = '';
  defaultRate = 60;
  isActive = true;
  error = signal('');
  saving = signal(false);

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam && idParam !== 'new') {
      this.isNew.set(false);
      this.id = +idParam;
      this.svc.getById(this.id).subscribe({
        next: (c) => {
          this.name = c.name;
          this.phone = c.phone ?? '';
          this.address = c.address ?? '';
          this.defaultRate = c.defaultRate;
          this.isActive = c.isActive;
        },
        error: (e) => this.error.set(readApiError(e, 'Customer not found'))
      });
    }
  }

  save(): void {
    this.error.set('');
    if (!this.name.trim()) {
      this.error.set('Name is required');
      return;
    }
    if (this.defaultRate < 0) {
      this.error.set('Default rate cannot be negative');
      return;
    }
    this.saving.set(true);
    if (this.isNew()) {
      this.svc
        .create({
          name: this.name.trim(),
          phone: this.phone || null,
          address: this.address || null,
          defaultRate: this.defaultRate
        })
        .subscribe({
          next: () => this.router.navigateByUrl('/customers'),
          error: (e) => {
            this.error.set(readApiError(e, 'Save failed'));
            this.saving.set(false);
          }
        });
    } else if (this.id != null) {
      this.svc
        .update(this.id, {
          name: this.name.trim(),
          phone: this.phone || null,
          address: this.address || null,
          defaultRate: this.defaultRate,
          isActive: this.isActive
        })
        .subscribe({
          next: () => this.router.navigateByUrl('/customers'),
          error: (e) => {
            this.error.set(readApiError(e, 'Save failed'));
            this.saving.set(false);
          }
        });
    }
  }
}
