import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface ToastMessage { id: number; text: string; kind: 'success' | 'error' | 'info'; }

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  private readonly messagesSubject = new BehaviorSubject<ToastMessage[]>([]);
  readonly messages$ = this.messagesSubject.asObservable();

  show(text: string, kind: ToastMessage['kind'] = 'info'): void {
    const message = { id: ++this.nextId, text, kind };
    this.messagesSubject.next([...this.messagesSubject.value, message]);
    window.setTimeout(() => this.dismiss(message.id), kind === 'error' ? 6000 : 3600);
  }

  dismiss(id: number): void {
    this.messagesSubject.next(this.messagesSubject.value.filter(message => message.id !== id));
  }
}
