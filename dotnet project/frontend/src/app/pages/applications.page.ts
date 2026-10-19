import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { getErrorMessage } from '../core/error-message';
import { RoleApplication, UserRole } from '../core/models';
import { ToastService } from '../core/toast.service';
import { IconComponent } from '../shared/icon.component';

@Component({
  selector: 'fs-applications-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IconComponent],
  template: `
    <section class="page applications-page">
      <div class="page-heading"><div><p class="eyebrow">GROW WITH THE NEIGHBOURHOOD</p><h1>Bring your good thing to the table.</h1><p class="page-heading__sub">Apply to run a restaurant or deliver meals. Our administrator reviews each request before any role changes.</p></div><div class="application-heading-mark"><fs-icon name="sparkles" /></div></div>
      @if (errorMessage) { <div class="inline-alert" role="alert"><fs-icon name="close" /><span>{{ errorMessage }}</span><button class="text-button" (click)="load()">Try again</button></div> }
      <div class="application-layout">
        <section class="application-form-card"><div class="application-form-card__head"><span class="application-form-card__icon"><fs-icon name="users" /></span><p class="eyebrow">YOUR NEXT CHAPTER</p><h2>Tell us what you have in mind.</h2><p>We’ll review your details and let you know once your application is considered.</p></div>
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <label class="field"><span>I'd like to join as</span><select formControlName="requestedRole" (change)="businessNameMissing = false"><option value="RestaurantOwner">Restaurant owner</option><option value="DeliveryPartner">Delivery partner</option></select></label>
            @if (form.controls.requestedRole.value === 'RestaurantOwner') { <label class="field"><span>Restaurant or business name</span><input formControlName="businessName" (input)="businessNameMissing = false" maxlength="150" placeholder="The name guests will remember"><small class="field__error">{{ businessNameError }}</small></label> }
            <label class="field"><span>Tell us about your plans</span><textarea formControlName="applicationDetails" rows="6" maxlength="1500" placeholder="What are you building? Where do you serve? What would you like to bring to FoodSupply?"></textarea><span class="field__counter">{{ form.controls.applicationDetails.value?.length || 0 }}/1500 · at least 20 characters</span><small class="field__error">{{ detailsError }}</small></label>
            <button class="button button--primary button--full" type="submit" [disabled]="saving || hasPendingApplication">@if (saving) { Sending your application… } @else if (hasPendingApplication) { Application under review } @else { Send application <fs-icon name="arrow" /> }</button>
            <p class="application-form__note"><fs-icon name="clock" /> You’ll continue using your Customer account while your application is under review.</p>
          </form>
        </section>
        <section class="application-history panel"><div class="panel__heading"><div><p class="eyebrow">YOUR REQUESTS</p><h2>Application history</h2></div><span class="history-count">{{ applications.length }}</span></div>
          @if (loading && !applications.length) { <div class="loading-inline"><span class="spinner spinner--dark"></span> Gathering your applications…</div> }
          @else if (applications.length) { <div class="application-history__list">@for (application of applications; track application.id) { <article class="application-history-card"><div class="application-history-card__top"><span class="role-icon"><fs-icon [name]="application.requestedRole === 'RestaurantOwner' ? 'market' : 'pin'" /></span><span class="application-history-card__name"><strong>{{ application.requestedRole === 'RestaurantOwner' ? 'Restaurant partner' : 'Delivery partner' }}</strong><small>{{ application.businessName || application.applicantName }}</small></span><span class="status-chip" [ngClass]="'status-chip--' + application.status.toLowerCase()">{{ application.status }}</span></div><p>{{ application.applicationDetails }}</p>@if (application.reviewNote) { <div class="review-note"><strong>Review note</strong><span>{{ application.reviewNote }}</span></div> }<div class="application-history-card__foot"><span>Submitted {{ application.submittedAtUtc | date:'d MMM yyyy' }}</span>@if (application.reviewedAtUtc) { <span>Reviewed {{ application.reviewedAtUtc | date:'d MMM yyyy' }}</span> }</div></article> }</div> }
          @else { <div class="empty-state empty-state--compact"><span class="empty-state__icon"><fs-icon name="apply" /></span><strong>A new opportunity starts with a note</strong><p>Send us a little context and your application will appear here for you to follow.</p></div> }
        </section>
      </div>
      <div class="application-steps"><div><span>01</span><strong>Share your plan</strong><small>Tell us about your restaurant or delivery availability.</small></div><i></i><div><span>02</span><strong>Administrator review</strong><small>An administrator checks the request and may add a review note.</small></div><i></i><div><span>03</span><strong>Get the right access</strong><small>When approved, sign in again to use your new role.</small></div></div>
    </section>
  `
})
export class ApplicationsPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly toast = inject(ToastService);
  applications: RoleApplication[] = [];
  loading = true;
  saving = false;
  businessNameMissing = false;
  errorMessage = '';
  readonly form = this.fb.group({
    requestedRole: this.fb.control<'RestaurantOwner' | 'DeliveryPartner'>('RestaurantOwner', { validators: Validators.required, nonNullable: true }),
    businessName: ['', Validators.maxLength(150)],
    applicationDetails: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(1500)]]
  });

  get hasPendingApplication(): boolean { return this.applications.some(item => item.status === 'Pending'); }
  get businessNameError(): string { const field = this.form.controls.businessName; return this.businessNameMissing ? 'Business name is required for a restaurant partner application.' : field.hasError('maxlength') && field.touched ? 'Use 150 characters or fewer.' : ''; }
  get detailsError(): string { const field = this.form.controls.applicationDetails; if (!field.invalid || !field.touched) return ''; return field.hasError('required') ? 'Please tell us about your plans.' : field.hasError('minlength') ? 'Add at least 20 characters so we understand your plans.' : 'Keep this under 1,500 characters.'; }

  ngOnInit(): void { this.load(); }
  load(): void { this.loading = true; this.errorMessage = ''; this.api.roleApplications().pipe(finalize(() => this.loading = false)).subscribe({ next: applications => this.applications = applications.sort((a, b) => Date.parse(b.submittedAtUtc) - Date.parse(a.submittedAtUtc)), error: error => this.errorMessage = getErrorMessage(error, 'Your applications could not be loaded.') }); }
  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.hasPendingApplication) return;
    const value = this.form.getRawValue();
    if (value.requestedRole === 'RestaurantOwner' && !value.businessName?.trim()) { this.businessNameMissing = true; return; }
    this.saving = true;
    this.api.applyForRole({ requestedRole: value.requestedRole, businessName: value.requestedRole === 'RestaurantOwner' ? value.businessName?.trim() : null, applicationDetails: value.applicationDetails!.trim() }).pipe(finalize(() => this.saving = false)).subscribe({
      next: application => { this.applications = [application, ...this.applications]; this.form.controls.applicationDetails.reset(''); this.toast.show('Your application is in. We’ll keep it here for you to follow.', 'success'); },
      error: error => this.toast.show(getErrorMessage(error, 'Your application could not be sent.'), 'error')
    });
  }
}
