import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { finalize } from 'rxjs';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { getErrorMessage } from '../core/error-message';
import { UserSession } from '../core/models';
import { ToastService } from '../core/toast.service';
import { IconComponent } from '../shared/icon.component';

@Component({
  selector: 'fs-sessions-page',
  standalone: true,
  imports: [CommonModule, IconComponent],
  template: `
    <section class="page sessions-page">
      <div class="page-heading"><div><p class="eyebrow">YOUR ACCOUNT, YOUR CALL</p><h1>Good to have you here.</h1><p class="page-heading__sub">Review the devices signed in to your account and end any session you no longer recognize.</p></div><span class="account-avatar-large">{{ initials }}</span></div>
      <div class="account-summary"><div class="account-summary__avatar">{{ initials }}</div><div><p class="eyebrow">SIGNED IN AS</p><h2>{{ fullName }}</h2><p>{{ email }}</p></div><span class="account-role"><fs-icon name="sparkles" /> {{ roleLabel }}</span></div>
      @if (errorMessage) { <div class="inline-alert" role="alert"><fs-icon name="close" /><span>{{ errorMessage }}</span><button class="text-button" (click)="load()">Try again</button></div> }
      <section class="panel sessions-panel"><div class="panel__heading"><div><p class="eyebrow">SESSION CONTROL</p><h2>Your signed-in devices</h2></div><span class="history-count">{{ sessions.length }}</span></div>
        @if (loading && !sessions.length) { <div class="loading-inline"><span class="spinner spinner--dark"></span> Checking active sessions…</div> }
        @else if (sessions.length) { <div class="session-list">@for (session of sessions; track session.id) { <article class="session-row"><span class="session-row__icon"><fs-icon [name]="session.deviceName.toLowerCase().includes('mobile') || session.deviceName.toLowerCase().includes('android') || session.deviceName.toLowerCase().includes('iphone') ? 'phone' : 'sessions'" /></span><div class="session-row__details"><strong>{{ session.deviceName || 'Unknown browser' }}</strong><span>Started {{ session.createdAtUtc | date:'d MMM yyyy · h:mm a' }}</span><small>Session expires {{ session.expiresAtUtc | date:'d MMM yyyy · h:mm a' }}</small></div>@if (session.isCurrent) { <span class="current-session"><i></i> THIS DEVICE</span> } @else { <button class="button button--quiet-danger button--small" (click)="revoke(session)" [disabled]="busyId === session.id">{{ busyId === session.id ? 'Ending…' : 'End session' }}</button> }</article> }</div> }
        @else { <div class="empty-state empty-state--compact"><span class="empty-state__icon"><fs-icon name="sessions" /></span><strong>No active sessions found</strong><p>Your current login will appear here after the next refresh.</p></div> }
        <div class="security-note"><span class="security-note__icon"><fs-icon name="check" /></span><span><strong>Sessions are checked on every API request.</strong> Ending a session revokes its access token on the server, so a copied token cannot keep working.</span></div>
      </section>
      <div class="account-data-note"><fs-icon name="users" /><span>FoodSupply stores your account details securely. Use the sign-out option in the sidebar when you finish on a shared device.</span></div>
    </section>
  `
})
export class SessionsPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  sessions: UserSession[] = [];
  loading = true;
  busyId: string | null = null;
  errorMessage = '';

  get initials(): string { return `${this.auth.session?.firstName?.[0] ?? 'F'}${this.auth.session?.lastName?.[0] ?? 'S'}`.toUpperCase(); }
  get fullName(): string { return `${this.auth.session?.firstName ?? ''} ${this.auth.session?.lastName ?? ''}`.trim(); }
  get email(): string { return this.auth.session?.email ?? ''; }
  get roleLabel(): string { return ({ Customer: 'Customer', RestaurantOwner: 'Restaurant partner', Admin: 'Administrator', DeliveryPartner: 'Delivery partner' } as const)[this.auth.role ?? 'Customer']; }

  ngOnInit(): void { this.load(); }
  load(): void { this.loading = true; this.errorMessage = ''; this.api.sessions().pipe(finalize(() => this.loading = false)).subscribe({ next: sessions => this.sessions = sessions.sort((a, b) => Number(b.isCurrent) - Number(a.isCurrent) || Date.parse(b.createdAtUtc) - Date.parse(a.createdAtUtc)), error: error => this.errorMessage = getErrorMessage(error, 'Sessions could not be loaded.') }); }

  revoke(session: UserSession): void {
    this.busyId = session.id;
    this.api.revokeSession(session.id).pipe(finalize(() => this.busyId = null)).subscribe({ next: () => { this.toast.show('That device has been signed out.', 'success'); this.load(); }, error: error => this.toast.show(getErrorMessage(error, 'The session could not be ended.'), 'error') });
  }
}
