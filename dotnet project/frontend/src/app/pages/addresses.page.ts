import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ApiService } from '../core/api.service';
import { getErrorMessage } from '../core/error-message';
import { Address } from '../core/models';
import { ToastService } from '../core/toast.service';
import { IconComponent } from '../shared/icon.component';

@Component({
  selector: 'fs-addresses-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IconComponent],
  template: `
    <section class="page addresses-page">
      <div class="page-heading"><div><p class="eyebrow">THE LITTLE DETAILS</p><h1>Your places.</h1><p class="page-heading__sub">Keep the places you love close. Your default address is ready when you are.</p></div><button class="button button--primary" (click)="startCreate()"><fs-icon name="plus" /> Add an address</button></div>
      @if (errorMessage) { <div class="inline-alert" role="alert"><fs-icon name="close" /><span>{{ errorMessage }}</span><button class="text-button" (click)="load()">Try again</button></div> }
      @if (loading && !addresses.length) { <div class="address-grid">@for (i of [1,2,3]; track i) { <div class="address-skeleton"></div> }</div> }
      @else if (addresses.length) { <div class="address-grid">@for (address of addresses; track address.id) {
        <article class="address-card" [class.address-card--default]="address.isDefault"><div class="address-card__top"><span class="address-label"><span class="address-label__icon"><fs-icon [name]="address.label.toLowerCase().includes('work') ? 'manage' : address.label.toLowerCase().includes('other') ? 'pin' : 'addresses'" /></span>{{ address.label }}</span>@if (address.isDefault) { <span class="default-badge"><fs-icon name="check" /> DEFAULT</span> } @else { <button class="text-button" (click)="makeDefault(address)">Make default</button> }</div><h2>{{ address.line1 }}</h2>@if (address.line2) { <p>{{ address.line2 }}</p> }<p>{{ address.city }}, {{ address.state }} {{ address.postalCode }}</p><p>{{ address.country }}</p><div class="address-card__foot"><span>Added {{ address.createdAtUtc | date:'d MMM yyyy' }}</span><span class="address-card__actions"><button (click)="startEdit(address)" [attr.aria-label]="'Edit ' + address.label + ' address'"><fs-icon name="edit" /> Edit</button><button class="address-card__delete" (click)="askDelete(address)" [attr.aria-label]="'Delete ' + address.label + ' address'"><fs-icon name="trash" /></button></span></div></article>
      }</div> }
      @else { <div class="empty-state"><span class="empty-state__icon"><fs-icon name="addresses" /></span><strong>Your first address is a small step home</strong><p>Add a delivery location to make checkout quicker and your favourite meals easier to reach.</p><button class="button button--primary" (click)="startCreate()"><fs-icon name="plus" /> Add an address</button></div> }
      <div class="address-tip"><span class="address-tip__icon"><fs-icon name="sparkles" /></span><div><strong>Good to know</strong><p>You can save multiple delivery locations. Only you can see and manage the addresses on your account.</p></div></div>
    </section>

    @if (formOpen) { <div class="modal-backdrop" (click)="closeForm()"><section class="form-modal" role="dialog" aria-modal="true" aria-labelledby="address-title" (click)="$event.stopPropagation()"><div class="modal-heading"><div><p class="eyebrow">{{ editingId ? 'MAKE AN UPDATE' : 'A PLACE TO CALL YOURS' }}</p><h2 id="address-title">{{ editingId ? 'Edit address.' : 'Add a new place.' }}</h2><p>Give it a label you’ll recognize at checkout.</p></div><button class="modal-close" (click)="closeForm()" aria-label="Close address form"><fs-icon name="close" /></button></div>
      <form [formGroup]="form" (ngSubmit)="save()" novalidate>
        <div class="form-row"><label class="field"><span>Label</span><input formControlName="label" maxlength="60" placeholder="Home, work, or somewhere lovely"><small class="field__error">{{ fieldError('label', 2, 60) }}</small></label><label class="field"><span>Country</span><input formControlName="country" maxlength="100" autocomplete="country-name" placeholder="India"><small class="field__error">{{ fieldError('country', 2, 100) }}</small></label></div>
        <label class="field"><span>Address line 1</span><input formControlName="line1" maxlength="250" autocomplete="address-line1" placeholder="Flat / house number and street"><small class="field__error">{{ fieldError('line1', 5, 250) }}</small></label>
        <label class="field"><span>Address line 2 <small>(optional)</small></span><input formControlName="line2" maxlength="250" autocomplete="address-line2" placeholder="Apartment, landmark, or area"><small class="field__error">{{ fieldError('line2', 0, 250) }}</small></label>
        <div class="form-row form-row--three"><label class="field"><span>City</span><input formControlName="city" maxlength="100" autocomplete="address-level2"><small class="field__error">{{ fieldError('city', 2, 100) }}</small></label><label class="field"><span>State</span><input formControlName="state" maxlength="100" autocomplete="address-level1"><small class="field__error">{{ fieldError('state', 2, 100) }}</small></label><label class="field"><span>Postal code</span><input formControlName="postalCode" maxlength="20" autocomplete="postal-code"><small class="field__error">{{ fieldError('postalCode', 3, 20) }}</small></label></div>
        @if (!editingId) { <label class="check-label check-label--default"><input type="checkbox" formControlName="isDefault"><span>Use as my default delivery address</span></label> }
        <div class="form-modal__actions"><button type="button" class="button button--outline" (click)="closeForm()" [disabled]="saving">Cancel</button><button type="submit" class="button button--primary" [disabled]="saving">{{ saving ? 'Saving…' : editingId ? 'Save changes' : 'Add address' }} <fs-icon name="arrow" /></button></div>
      </form>
    </section></div> }

    @if (deleteTarget) { <div class="modal-backdrop" (click)="deleteTarget = null"><section class="confirm-modal" role="dialog" aria-modal="true" aria-labelledby="delete-address-title" (click)="$event.stopPropagation()"><span class="confirm-modal__icon"><fs-icon name="trash" /></span><h2 id="delete-address-title">Remove this address?</h2><p>{{ deleteTarget.label }} · {{ deleteTarget.line1 }}, {{ deleteTarget.city }}</p><div class="confirm-modal__actions"><button class="button button--outline" (click)="deleteTarget = null">Keep address</button><button class="button button--danger" (click)="confirmDelete()" [disabled]="saving">{{ saving ? 'Removing…' : 'Remove address' }}</button></div></section></div> }
  `
})
export class AddressesPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);
  private readonly toast = inject(ToastService);
  addresses: Address[] = [];
  loading = true;
  saving = false;
  formOpen = false;
  editingId: number | null = null;
  deleteTarget: Address | null = null;
  errorMessage = '';
  readonly form = this.fb.group({
    label: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(60)]],
    line1: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(250)]],
    line2: ['', Validators.maxLength(250)],
    city: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    state: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    postalCode: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(20)]],
    country: ['India', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    isDefault: [false]
  });

  ngOnInit(): void { this.load(); }
  load(): void { this.loading = true; this.errorMessage = ''; this.api.addresses().pipe(finalize(() => this.loading = false)).subscribe({ next: addresses => this.addresses = addresses, error: error => this.errorMessage = getErrorMessage(error, 'Addresses could not be loaded.') }); }
  startCreate(): void { this.editingId = null; this.form.reset({ label: '', line1: '', line2: '', city: '', state: '', postalCode: '', country: 'India', isDefault: this.addresses.length === 0 }); this.formOpen = true; }
  startEdit(address: Address): void { this.editingId = address.id; this.form.reset({ ...address, line2: address.line2 ?? '', isDefault: false }); this.formOpen = true; }
  closeForm(): void { if (!this.saving) this.formOpen = false; }

  fieldError(name: string, min: number, max: number): string {
    const control = this.form.get(name);
    if (!control?.invalid || !(control.touched || this.form.touched)) return '';
    if (control.hasError('required')) return 'This field is required.';
    if (control.hasError('minlength')) return `Use at least ${min} characters.`;
    if (control.hasError('maxlength')) return `Use ${max} characters or fewer.`;
    return 'Please check this value.';
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    const body = { label: value.label!.trim(), line1: value.line1!.trim(), line2: value.line2?.trim() || null, city: value.city!.trim(), state: value.state!.trim(), postalCode: value.postalCode!.trim(), country: value.country!.trim() };
    this.saving = true;
    const request = this.editingId ? this.api.updateAddress(this.editingId, body) : this.api.createAddress({ ...body, isDefault: value.isDefault });
    request.pipe(finalize(() => this.saving = false)).subscribe({ next: () => { this.formOpen = false; this.toast.show(this.editingId ? 'Address updated.' : 'Address added.', 'success'); this.load(); }, error: error => this.toast.show(getErrorMessage(error, 'The address could not be saved.'), 'error') });
  }

  makeDefault(address: Address): void {
    this.api.setDefaultAddress(address.id).subscribe({ next: () => { this.toast.show(`${address.label} is now your default address.`, 'success'); this.load(); }, error: error => this.toast.show(getErrorMessage(error, 'The default address could not be changed.'), 'error') });
  }

  askDelete(address: Address): void { this.deleteTarget = address; }
  confirmDelete(): void {
    const address = this.deleteTarget;
    if (!address) return;
    this.saving = true;
    this.api.deleteAddress(address.id).pipe(finalize(() => this.saving = false)).subscribe({ next: () => { this.deleteTarget = null; this.toast.show(`${address.label} address removed.`, 'success'); this.load(); }, error: error => this.toast.show(getErrorMessage(error, 'The address could not be removed.'), 'error') });
  }
}
