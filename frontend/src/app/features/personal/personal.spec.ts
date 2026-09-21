import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { PersonalService } from './personal.service';
import { PersonalPage } from './personal.page';

describe('Personal hisab', () => {
  it('carries debt forward, allocates settlements by product, and renders the signed-in workspace', async () => {
    const data = {
      products: [{ id: 1, name: 'Milk', unit: 'litre', rate: 60 }],
      expenses: [
        {
          id: 1,
          date: '2026-08-20',
          title: 'Milk',
          category: 'Food & groceries',
          productId: 1,
          amount: 120,
        },
        { id: 2, date: '2026-09-01', title: 'Bus', category: 'Transport', amount: 80 },
      ],
      payments: [
        { id: 1, expenseId: 1, date: '2026-08-20', amount: 20, mode: 'cash' },
        { id: 2, expenseId: 1, date: '2026-09-01', amount: 50, mode: 'upi' },
        { id: 3, expenseId: 2, date: '2026-09-01', amount: 80, mode: 'cash' },
      ],
    };
    const api = {
      personalSession: () => of({ name: 'Test account' }),
      personalData: () => of(data),
    };
    await TestBed.configureTestingModule({
      imports: [PersonalPage],
      providers: [{ provide: ApiService, useValue: api }],
    }).compileComponents();
    const svc = TestBed.inject(PersonalService);
    await svc.init();
    svc.month.set('2026-09');
    expect(svc.totals()).toEqual({ opening: 100, purchases: 80, paid: 130, closing: 50 });
    svc.product.set(1);
    expect(svc.totals()).toEqual({ opening: 100, purchases: 0, paid: 50, closing: 50 });
    expect(svc.outstanding().length).toBe(1);
    svc.product.set(0);
    const fixture = TestBed.createComponent(PersonalPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Everyday, in balance.');
    expect(fixture.nativeElement.textContent).toContain('Bus');
    expect(fixture.nativeElement.textContent).toContain('₹50.00');
    fixture.componentInstance.beginPayment(data.expenses[0]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('#payment-form').textContent).toContain('Milk');
  });
  it('does not invite duplicate submission after a successful save followed by a failed refresh', async () => {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: ApiService,
          useValue: {
            personalProduct: () => of({ id: 1, name: 'Milk', unit: 'litre', rate: 60 }),
            personalData: () => throwError(() => new Error('offline')),
          },
        },
      ],
    });
    const svc = TestBed.inject(PersonalService);
    expect(await svc.addProduct('Milk', 'litre', 60)).toBeTrue();
    expect(svc.error()).toContain('Saved successfully');
  });
});
