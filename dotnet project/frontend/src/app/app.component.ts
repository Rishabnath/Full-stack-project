import { AsyncPipe, NgClass } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastService } from './core/toast.service';
import { IconComponent } from './shared/icon.component';

@Component({
  selector: 'fs-root',
  standalone: true,
  imports: [RouterOutlet, AsyncPipe, NgClass, IconComponent],
  template: `
    <router-outlet />
    <div class="toast-stack" aria-live="polite" aria-atomic="false">
      @for (toast of (toastService.messages$ | async) ?? []; track toast.id) {
        <div class="toast" [ngClass]="'toast--' + toast.kind" role="status">
          <fs-icon [name]="toast.kind === 'success' ? 'check' : toast.kind === 'error' ? 'close' : 'sparkles'" />
          <span>{{ toast.text }}</span>
          <button class="toast__close" (click)="toastService.dismiss(toast.id)" aria-label="Dismiss notification"><fs-icon name="close" /></button>
        </div>
      }
    </div>
  `
})
export class AppComponent {
  readonly toastService = inject(ToastService);
}
