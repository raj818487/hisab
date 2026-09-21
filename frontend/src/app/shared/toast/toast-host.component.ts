import { Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast-host',
  standalone: true,
  template: `
    <div class="toast-host" aria-live="polite">
      @for (t of toast.items(); track t.id) {
        <div
          class="toast"
          [class.success]="t.kind === 'success'"
          [class.error]="t.kind === 'error'"
          [class.info]="t.kind === 'info'"
        >
          <div class="toast-msg">{{ t.message }}</div>
          <button type="button" (click)="toast.dismiss(t.id)" aria-label="Dismiss">×</button>
        </div>
      }
    </div>
  `
})
export class ToastHostComponent {
  readonly toast = inject(ToastService);
}
