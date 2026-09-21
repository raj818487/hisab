import { PersonalPage } from './features/personal/personal.page';
import { Routes } from '@angular/router';
import { DashboardPage } from './features/dashboard/dashboard.page';
import { CustomersListPage } from './features/customers/customers-list.page';
import { CustomerFormPage } from './features/customers/customer-form.page';
import { DailyPage } from './features/daily/daily.page';
import { HisabPage } from './features/hisab/hisab.page';
import { PaymentsPage } from './features/payments/payments.page';
import { ReceiptPage } from './features/receipts/receipt.page';

export const routes: Routes = [
  { path: 'personal', component: PersonalPage },
  { path: '', pathMatch: 'full', redirectTo: 'personal' },
  { path: 'dashboard', component: DashboardPage },
  { path: 'customers', component: CustomersListPage },
  { path: 'customers/new', component: CustomerFormPage },
  { path: 'customers/:id', component: CustomerFormPage },
  { path: 'daily', component: DailyPage },
  { path: 'hisab', component: HisabPage },
  { path: 'payments', component: PaymentsPage },
  { path: 'receipts/:paymentId', component: ReceiptPage },
  { path: '**', redirectTo: 'personal' }
];