import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  readonly apiBase = environment.apiBaseUrl;
  readonly paymentsMode = 'Manual ledger only (cash / UPI / other labels)';
  readonly gateways = 'None — no Razorpay, Stripe, or UPI collect SDK';
  readonly receiptsNote = 'View + HTML download / print from Payments → Receipt';
}
