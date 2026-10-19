import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { UserRole } from '../core/models';
import { ToastService } from '../core/toast.service';
import { IconComponent } from '../shared/icon.component';

interface NavLink { label: string; path: string; icon: string; }

@Component({
  selector: 'fs-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, IconComponent],
  template: `
    <div class="app-frame" [class.app-frame--collapsed]="collapsed" [class.app-frame--mobile-open]="mobileOpen">
      <button class="mobile-scrim" (click)="mobileOpen = false" aria-label="Close navigation"></button>
      <aside class="sidebar" [attr.aria-label]="'Main navigation'">
        <div class="sidebar__top">
          <a class="brand brand--sidebar" routerLink="/app/dashboard" (click)="mobileOpen = false">
            <span class="brand__mark"><fs-icon name="sparkles" /></span>
            <span class="brand__name">Food<span>Supply</span><small>THE NEIGHBOURHOOD TABLE</small></span>
          </a>
          <button class="sidebar__collapse" (click)="toggleCollapse()" [attr.aria-label]="collapsed ? 'Expand sidebar' : 'Collapse sidebar'" [title]="collapsed ? 'Expand sidebar' : 'Collapse sidebar'"><fs-icon name="collapse" /></button>
          <button class="sidebar__mobile-close" (click)="mobileOpen = false" aria-label="Close menu"><fs-icon name="close" /></button>
        </div>

        <div class="sidebar__identity">
          <div class="avatar avatar--sidebar">{{ initials }}</div>
          <div class="sidebar__identity-copy"><strong>{{ fullName }}</strong><span>{{ roleLabel }}</span></div>
          <span class="online-dot" title="Signed in"></span>
        </div>

        <div class="nav-caption">YOUR SPACE</div>
        <nav class="side-nav">
          @for (link of links; track link.path) {
            <a class="side-nav__link" [routerLink]="link.path" routerLinkActive="side-nav__link--active" [routerLinkActiveOptions]="{ exact: true }" [title]="collapsed ? link.label : ''" (click)="mobileOpen = false">
              <fs-icon [name]="link.icon" /><span>{{ link.label }}</span><fs-icon class="side-nav__chevron" name="chevron" />
            </a>
          }
        </nav>

        <div class="sidebar__bottom">
          <div class="sidebar-note"><div class="sidebar-note__icon"><fs-icon name="star" /></div><strong>A little extra joy</strong><p>Good things are always on the menu.</p><a routerLink="/app/market" (click)="mobileOpen = false">Explore favourites <fs-icon name="arrow" /></a></div>
          <button class="side-nav__link side-nav__logout" (click)="logout()" [disabled]="loggingOut"><fs-icon name="logout" /><span>{{ loggingOut ? 'Signing out…' : 'Sign out' }}</span></button>
        </div>
      </aside>

      <div class="workspace">
        <header class="topbar">
          <button class="topbar__mobile-menu" (click)="mobileOpen = true" aria-label="Open menu"><fs-icon name="menu" /></button>
          <div class="topbar__crumb"><span>FoodSupply</span><fs-icon name="chevron" /><strong>{{ pageTitle }}</strong></div>
          <div class="topbar__right">
            <span class="topbar__date">{{ today }}</span>
            <span class="topbar__divider"></span>
            <button class="topbar__profile" routerLink="/app/sessions" aria-label="View your active sessions">
              <span class="avatar">{{ initials }}</span><span class="topbar__profile-copy"><strong>{{ firstName }}</strong><small>{{ roleLabel }}</small></span><fs-icon name="chevrondown" />
            </button>
          </div>
        </header>
        <main class="workspace__main"><router-outlet /></main>
        <footer class="workspace__footer"><span>FoodSupply <b>·</b> Made for good company</span><span>Your neighbourhood, beautifully served</span></footer>
      </div>
    </div>
  `
})
export class ShellComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  collapsed = localStorage.getItem('foodsupply.sidebar.collapsed') === 'true';
  mobileOpen = false;
  loggingOut = false;
  readonly today = new Intl.DateTimeFormat('en-IN', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date());

  get role(): UserRole { return this.auth.role ?? 'Customer'; }
  get firstName(): string { return this.auth.session?.firstName ?? 'Guest'; }
  get fullName(): string { return `${this.auth.session?.firstName ?? ''} ${this.auth.session?.lastName ?? ''}`.trim(); }
  get initials(): string { return `${this.auth.session?.firstName?.[0] ?? 'F'}${this.auth.session?.lastName?.[0] ?? 'S'}`.toUpperCase(); }
  get roleLabel(): string { return ({ Customer: 'Customer', RestaurantOwner: 'Restaurant partner', Admin: 'Administrator', DeliveryPartner: 'Delivery partner' } as const)[this.role]; }
  get pageTitle(): string {
    const page = this.router.url.split('?')[0].split('/').at(-1) ?? 'dashboard';
    return ({ dashboard: 'Overview', market: 'Discover food', orders: this.role === 'Customer' ? 'My orders' : this.role === 'DeliveryPartner' ? 'Delivery desk' : 'Order desk', manage: this.role === 'Admin' ? 'Administration' : 'Restaurant studio', admin: 'Administration', addresses: 'Delivery addresses', applications: 'Role applications', sessions: 'Account & sessions' } as Record<string, string>)[page] ?? 'Overview';
  }

  get links(): NavLink[] {
    const common: NavLink[] = [{ label: 'Overview', path: '/app/dashboard', icon: 'dashboard' }];
    if (this.role === 'Customer') common.push({ label: 'Discover & order', path: '/app/market', icon: 'market' });
    if (this.role === 'RestaurantOwner') common.push({ label: 'Restaurant studio', path: '/app/manage', icon: 'manage' });
    if (this.role === 'Admin') common.push({ label: 'Administration', path: '/app/admin', icon: 'manage' });
    common.push({ label: this.role === 'Customer' ? 'My orders' : this.role === 'DeliveryPartner' ? 'Delivery desk' : 'Order desk', path: '/app/orders', icon: 'orders' });
    if (this.role === 'Customer') {
      common.push({ label: 'My addresses', path: '/app/addresses', icon: 'addresses' });
      common.push({ label: 'Become a partner', path: '/app/applications', icon: 'apply' });
    }
    if (this.role === 'Admin') common.push({ label: 'Role applications', path: '/app/admin?tab=applications', icon: 'users' });
    common.push({ label: 'Account & sessions', path: '/app/sessions', icon: 'sessions' });
    return common;
  }

  toggleCollapse(): void {
    this.collapsed = !this.collapsed;
    localStorage.setItem('foodsupply.sidebar.collapsed', String(this.collapsed));
  }

  logout(): void {
    if (this.loggingOut) return;
    this.loggingOut = true;
    this.auth.logout().pipe(finalize(() => this.loggingOut = false)).subscribe(() => {
      this.toast.show('You have signed out safely.', 'success');
      void this.router.navigate(['/login']);
    });
  }
}
