import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface ToastItem {
  id: number;
  kind: ToastKind;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
  readonly items = signal<ToastItem[]>([]);

  success(message: string, ms = 3200): void {
    this.push('success', message, ms);
  }

  error(message: string, ms = 4500): void {
    this.push('error', message, ms);
  }

  info(message: string, ms = 3200): void {
    this.push('info', message, ms);
  }

  dismiss(id: number): void {
    this.items.update((list) => list.filter((t) => t.id !== id));
  }

  private push(kind: ToastKind, message: string, ms: number): void {
    const id = ++this.seq;
    this.items.update((list) => [...list, { id, kind, message }]);
    window.setTimeout(() => this.dismiss(id), ms);
  }
}
