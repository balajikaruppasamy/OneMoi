import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastService } from './core/ui';
import { IconComponent } from './shared/icon.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, IconComponent],
  template: `
    <router-outlet />
    <div class="toast-host" aria-live="polite">
      @for (t of toast.items(); track t.id) {
        <div class="toast" [class.ok]="t.type === 'ok'" [class.err]="t.type === 'err'">
          <mk-icon [name]="t.type === 'ok' ? 'check' : t.type === 'err' ? 'alert' : 'info'" /><span>{{ t.text }}</span>
        </div>
      }
    </div>`
})
export class App {
  protected toast = inject(ToastService);
  constructor() {
    try { const t = localStorage.getItem('mk-theme'); if (t) document.documentElement.setAttribute('data-theme', t); } catch { /* ignore */ }
  }
}
