import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private _counter = 0;
  private readonly _toasts$ = new BehaviorSubject<Toast[]>([]);
  readonly toasts$ = this._toasts$.asObservable();

  success(message: string): void { this.show('success', message, 4000); }
  error(message: string): void { this.show('error', message, 6000); }
  info(message: string): void { this.show('info', message, 4000); }

  dismiss(id: number): void {
    this._toasts$.next(this._toasts$.getValue().filter(t => t.id !== id));
  }

  private show(type: ToastType, message: string, duration: number): void {
    const id = ++this._counter;
    const current = this._toasts$.getValue();
    this._toasts$.next([...current, { id, type, message }]);
    setTimeout(() => this.dismiss(id), duration);
  }
}
