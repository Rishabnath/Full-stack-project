import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { getErrorMessage } from '../core/error-message';
import { Category, MenuItem, Restaurant, RoleApplication, UserRole } from '../core/models';
import { ToastService } from '../core/toast.service';
import { httpUrlPattern } from '../core/validation';
import { IconComponent } from '../shared/icon.component';

type AdminTab = 'applications' | 'categories' | 'restaurants' | 'menu';
type OwnerTab = 'restaurants' | 'menu';

@Component({
  selector: 'fs-manage-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IconComponent],
  template: `
    <section class="page manage-page">
      <div class="page-heading"><div><p class="eyebrow">{{ isAdmin ? 'THE FOODSUPPLY CONTROL ROOM' : 'YOUR KITCHEN, YOUR WAY' }}</p><h1>{{ isAdmin ? 'A thoughtful way to manage.' : 'Your restaurant studio.' }}</h1><p class="page-heading__sub">{{ isAdmin ? 'Keep the catalogue considered, restaurants up to date, and partner applications moving.' : 'Shape your storefront, refine your menu, and give your neighbourhood a reason to come back.' }}</p></div><span class="manage-heading-mark"><fs-icon [name]="isAdmin ? 'chart' : 'market'" /></span></div>
      @if (errorMessage) { <div class="inline-alert" role="alert"><fs-icon name="close" /><span>{{ errorMessage }}</span><button class="text-button" (click)="load()">Try again</button></div> }
      <div class="manage-tabs" role="tablist" [attr.aria-label]="isAdmin ? 'Administration sections' : 'Restaurant studio sections'">
        @if (isAdmin) {
          <button role="tab" [attr.aria-selected]="tab === 'applications'" [class.manage-tabs__item--active]="tab === 'applications'" (click)="setTab('applications')"><fs-icon name="users" /> Applications <span>{{ pendingApplications.length }}</span></button>
          <button role="tab" [attr.aria-selected]="tab === 'categories'" [class.manage-tabs__item--active]="tab === 'categories'" (click)="setTab('categories')"><fs-icon name="bag" /> Categories</button>
          <button role="tab" [attr.aria-selected]="tab === 'restaurants'" [class.manage-tabs__item--active]="tab === 'restaurants'" (click)="setTab('restaurants')"><fs-icon name="market" /> Restaurants <span>{{ restaurants.length }}</span></button>
          <button role="tab" [attr.aria-selected]="tab === 'menu'" [class.manage-tabs__item--active]="tab === 'menu'" (click)="setTab('menu')"><fs-icon name="bag" /> Menu studio</button>
        } @else {
          <button role="tab" [attr.aria-selected]="tab === 'restaurants'" [class.manage-tabs__item--active]="tab === 'restaurants'" (click)="setTab('restaurants')"><fs-icon name="market" /> My restaurants <span>{{ restaurants.length }}</span></button>
          <button role="tab" [attr.aria-selected]="tab === 'menu'" [class.manage-tabs__item--active]="tab === 'menu'" (click)="setTab('menu')"><fs-icon name="bag" /> Menu studio</button>
        }
        <button class="manage-tabs__refresh" (click)="load()" [disabled]="loading"><fs-icon name="sessions" /> Refresh</button>
      </div>

      @if (isAdmin && tab === 'applications') {
        <div class="manage-section-head"><div><p class="eyebrow">A HUMAN REVIEW, EVERY TIME</p><h2>Partner applications</h2><p>Review each request before granting access to restaurant or delivery tools.</p></div><span class="status-chip" [ngClass]="pendingApplications.length ? 'status-chip--pending' : 'status-chip--delivered'">{{ pendingApplications.length }} waiting</span></div>
        @if (loading && !pendingApplications.length) { <div class="application-review-grid">@for (i of [1,2]; track i) { <div class="review-skeleton"></div> }</div> }
        @else if (pendingApplications.length) { <div class="application-review-grid">@for (application of pendingApplications; track application.id) {
          <article class="review-card"><div class="review-card__top"><span class="review-card__avatar">{{ application.applicantName.slice(0,1) }}</span><div class="review-card__who"><strong>{{ application.applicantName }}</strong><small>{{ application.applicantEmail }}</small></div><span class="status-chip status-chip--pending">Pending</span></div><div class="review-card__role"><span class="role-icon"><fs-icon [name]="application.requestedRole === 'RestaurantOwner' ? 'market' : 'pin'" /></span><span><small>REQUESTING ACCESS AS</small><strong>{{ roleName(application.requestedRole) }}</strong></span></div>@if (application.businessName) { <div class="review-card__business"><small>BUSINESS NAME</small><strong>{{ application.businessName }}</strong></div> }<div class="review-card__message"><small>THEIR NOTE</small><p>{{ application.applicationDetails }}</p></div><div class="review-card__foot"><span>Received {{ application.submittedAtUtc | date:'d MMM yyyy · h:mm a' }}</span><div><button class="button button--quiet-danger button--small" (click)="openReview(application, false)">Decline</button><button class="button button--primary button--small" (click)="openReview(application, true)">Approve <fs-icon name="check" /></button></div></div></article>
        }</div> }
        @else { <div class="empty-state"><span class="empty-state__icon"><fs-icon name="check" /></span><strong>The partner queue is clear</strong><p>New restaurant and delivery applications will appear here when they are submitted.</p></div> }
        <div class="admin-note"><fs-icon name="sparkles" /><span>Approving changes the applicant's role after they sign in again. They cannot choose an administrator role through this application.</span></div>
      }

      @if (isAdmin && tab === 'categories') {
        <div class="manage-section-head"><div><p class="eyebrow">A WELL-KEPT MENU STARTS HERE</p><h2>Food categories</h2><p>Keep the catalogue easy to explore. Inactive categories stay attached to past orders.</p></div><button class="button button--primary" (click)="startCategoryCreate()"><fs-icon name="plus" /> Add category</button></div>
        @if (loading && !categories.length) { <div class="category-grid">@for (i of [1,2,3,4]; track i) { <div class="category-skeleton"></div> }</div> }
        @else if (categories.length) { <div class="category-grid">@for (category of categories; track category.id) { <article class="category-card" [class.category-card--inactive]="!category.isActive"><span class="category-card__icon"><fs-icon name="bag" /></span><span class="category-card__state" [class.category-card__state--off]="!category.isActive"><i></i>{{ category.isActive ? 'Active' : 'Inactive' }}</span><h3>{{ category.name }}</h3><p>{{ category.description || 'A tasty corner of the neighbourhood menu.' }}</p><div class="category-card__foot"><span>Category #{{ category.id }}</span><span><button class="icon-button" (click)="startCategoryEdit(category)" [attr.aria-label]="'Edit ' + category.name"><fs-icon name="edit" /></button><button class="icon-button icon-button--danger" (click)="confirmCategoryDeactivate(category)" [disabled]="!category.isActive" [attr.aria-label]="'Deactivate ' + category.name"><fs-icon name="trash" /></button></span></div></article> }</div> }
        @else { <div class="empty-state"><span class="empty-state__icon"><fs-icon name="bag" /></span><strong>No categories yet</strong><p>Create a few categories so restaurant partners can organize their menus.</p></div> }
      }

      @if (isAdmin && tab === 'restaurants' || !isAdmin && tab === 'restaurants') {
        <div class="manage-section-head"><div><p class="eyebrow">{{ isAdmin ? 'THE NEIGHBOURHOOD DIRECTORY' : 'YOUR STOREFRONT' }}</p><h2>{{ isAdmin ? 'Active restaurants' : 'Your restaurants' }}</h2><p>{{ isAdmin ? 'Review active partner storefronts. Owners can update their own restaurant details.' : 'Create and refine your storefront. Approved restaurant partners can manage their own kitchens.' }}</p></div>@if (!isAdmin) { <button class="button button--primary" (click)="startRestaurantCreate()" [disabled]="saving"><fs-icon name="plus" /> Add restaurant</button> }</div>
        @if (loading && !restaurants.length) { <div class="manage-restaurant-grid">@for (i of [1,2,3]; track i) { <div class="restaurant-skeleton"></div> }</div> }
        @else if (restaurants.length) { <div class="manage-restaurant-grid">@for (restaurant of restaurants; track restaurant.id) { <article class="manage-restaurant-card"><div class="manage-restaurant-card__image"><img [src]="restaurant.imageUrl || fallbackImage" [alt]="restaurant.name" loading="lazy"><span class="manage-restaurant-card__status" [class.manage-restaurant-card__status--off]="!restaurant.isActive"><i></i>{{ restaurant.isActive ? 'Open' : 'Inactive' }}</span></div><div class="manage-restaurant-card__body"><div><p class="eyebrow">{{ restaurant.city }}</p><h3>{{ restaurant.name }}</h3></div><p>{{ restaurant.description || restaurant.address }}</p><div class="manage-restaurant-card__meta"><span><fs-icon name="pin" /> {{ restaurant.address }}</span>@if (restaurant.phoneNumber) { <span><fs-icon name="phone" /> {{ restaurant.phoneNumber }}</span> }</div><div class="manage-restaurant-card__actions"><button class="button button--outline button--small" (click)="selectRestaurant(restaurant); setTab('menu')"><fs-icon name="bag" /> Menu studio</button><button class="button button--outline button--small" (click)="startRestaurantEdit(restaurant)"><fs-icon name="edit" /> Edit</button>@if (restaurant.isActive) { <button class="icon-button icon-button--danger" (click)="askRestaurantDeactivate(restaurant)" [attr.aria-label]="'Deactivate ' + restaurant.name"><fs-icon name="trash" /></button> }</div></div></article> }</div> }
        @else { <div class="empty-state"><span class="empty-state__icon"><fs-icon name="market" /></span><strong>{{ isAdmin ? 'No active restaurants yet' : 'Let’s open your first kitchen' }}</strong><p>{{ isAdmin ? 'Restaurant partner applications are reviewed in the partner queue.' : 'Add your restaurant details, then fill the menu with dishes your neighbourhood will love.' }}</p>@if (!isAdmin) { <button class="button button--primary" (click)="startRestaurantCreate()"><fs-icon name="plus" /> Create restaurant</button> }</div> }
      }

      @if (tab === 'menu') {
        <div class="manage-section-head"><div><p class="eyebrow">{{ isAdmin ? 'THE PARTNER CATALOGUE' : 'FROM YOUR KITCHEN' }}</p><h2>Menu studio</h2><p>{{ isAdmin ? 'Review and maintain the active menus across partner restaurants.' : 'Keep dishes, prices, categories and availability current for customers.' }}</p></div>@if (restaurants.length) { <button class="button button--primary" (click)="startMenuCreate()" [disabled]="!hasActiveCategories"><fs-icon name="plus" /> Add a dish</button> }</div>
        @if (restaurants.length > 1) { <label class="select-filter"><span>Kitchen</span><select [value]="selectedRestaurantId" (change)="changeMenuRestaurant($event)">@for (restaurant of restaurants; track restaurant.id) { <option [value]="restaurant.id">{{ restaurant.name }} · {{ restaurant.city }}</option> }</select><fs-icon name="chevrondown" /></label> }
        @if (!restaurants.length && !loading) { <div class="empty-state"><span class="empty-state__icon"><fs-icon name="market" /></span><strong>Start with a restaurant</strong><p>Your menu belongs to a restaurant. Create your storefront first.</p><button class="button button--primary" (click)="setTab('restaurants')">Go to restaurants <fs-icon name="arrow" /></button></div> }
        @else if (loading && !menuItems.length) { <div class="menu-manage-grid">@for (i of [1,2,3,4]; track i) { <div class="menu-skeleton"></div> }</div> }
        @else if (menuItems.length) { <div class="menu-manage-grid">@for (item of menuItems; track item.id) { <article class="menu-manage-card"><div class="menu-manage-card__image"><img [src]="item.imageUrl || fallbackImage" [alt]="item.name" loading="lazy"><span class="menu-manage-card__availability" [class.menu-manage-card__availability--off]="!item.isAvailable"><i></i>{{ item.isAvailable ? 'Available' : 'Hidden' }}</span></div><div class="menu-manage-card__body"><p class="eyebrow">{{ item.categoryName }}</p><h3>{{ item.name }}</h3><p>{{ item.description || 'A fresh addition to the menu.' }}</p><div class="menu-manage-card__foot"><strong>{{ item.price | currency:'INR':'symbol':'1.0-0' }}</strong><span><button class="icon-button" (click)="startMenuEdit(item)" [attr.aria-label]="'Edit ' + item.name"><fs-icon name="edit" /></button><button class="icon-button icon-button--danger" (click)="askMenuDeactivate(item)" [attr.aria-label]="'Hide ' + item.name"><fs-icon name="trash" /></button></span></div></div></article> }</div> }
        @else if (!loading && restaurants.length) { <div class="empty-state"><span class="empty-state__icon"><fs-icon name="bag" /></span><strong>The menu is a blank canvas</strong><p>Add your first dish and customers will be able to find it.</p>@if (hasActiveCategories) { <button class="button button--primary" (click)="startMenuCreate()"><fs-icon name="plus" /> Add a dish</button> } @else { <p class="field__error">An administrator needs to create a food category before a dish can be added.</p> }</div> }
      }
    </section>

    @if (restaurantFormOpen) { <div class="modal-backdrop" (click)="closeForms()"><section class="form-modal" role="dialog" aria-modal="true" aria-labelledby="restaurant-form-title" (click)="$event.stopPropagation()"><div class="modal-heading"><div><p class="eyebrow">{{ editingRestaurantId ? 'REFINE YOUR STOREFRONT' : 'OPEN YOUR KITCHEN' }}</p><h2 id="restaurant-form-title">{{ editingRestaurantId ? 'Update restaurant.' : 'Add a restaurant.' }}</h2><p>Share the details customers need to find your kitchen.</p></div><button class="modal-close" (click)="closeForms()" aria-label="Close restaurant form"><fs-icon name="close" /></button></div>
      <form [formGroup]="restaurantForm" (ngSubmit)="saveRestaurant()" novalidate><label class="field"><span>Restaurant name</span><input formControlName="name" maxlength="150" placeholder="A name guests will remember"><small class="field__error">{{ requiredError(restaurantForm, 'name', 2, 150) }}</small></label><label class="field"><span>Description <small>(optional)</small></span><textarea formControlName="description" maxlength="500" rows="3" placeholder="What makes your kitchen special?"></textarea><small class="field__error">{{ maxError(restaurantForm, 'description', 500) }}</small></label><label class="field"><span>Street address</span><input formControlName="address" maxlength="250" autocomplete="street-address" placeholder="Street, building, and area"><small class="field__error">{{ requiredError(restaurantForm, 'address', 5, 250) }}</small></label><div class="form-row"><label class="field"><span>City</span><input formControlName="city" maxlength="100" autocomplete="address-level2"><small class="field__error">{{ requiredError(restaurantForm, 'city', 2, 100) }}</small></label><label class="field"><span>Phone <small>(optional)</small></span><input formControlName="phoneNumber" maxlength="30" type="tel" placeholder="+91 98765 43210"><small class="field__error">{{ maxError(restaurantForm, 'phoneNumber', 30) }}</small></label></div><label class="field"><span>Cover image URL <small>(optional, HTTPS recommended)</small></span><input formControlName="imageUrl" maxlength="2048" type="url" placeholder="https://images.example.com/your-restaurant.jpg"><small class="field__error">{{ urlError(restaurantForm, 'imageUrl') }}</small><small class="field__hint">Add a direct HTTPS image link. We’ll show a tasteful food photo if left blank.</small></label><div class="form-modal__actions"><button type="button" class="button button--outline" (click)="closeForms()" [disabled]="saving">Cancel</button><button type="submit" class="button button--primary" [disabled]="saving">{{ saving ? 'Saving…' : editingRestaurantId ? 'Save changes' : 'Create restaurant' }} <fs-icon name="arrow" /></button></div></form>
    </section></div> }

    @if (menuFormOpen) { <div class="modal-backdrop" (click)="closeForms()"><section class="form-modal" role="dialog" aria-modal="true" aria-labelledby="menu-form-title" (click)="$event.stopPropagation()"><div class="modal-heading"><div><p class="eyebrow">{{ editingMenuId ? 'REFINE THE RECIPE CARD' : 'ADD SOMETHING DELICIOUS' }}</p><h2 id="menu-form-title">{{ editingMenuId ? 'Edit dish.' : 'Add to your menu.' }}</h2><p>{{ selectedRestaurant?.name }} · Menu updates appear in the customer catalogue.</p></div><button class="modal-close" (click)="closeForms()" aria-label="Close menu form"><fs-icon name="close" /></button></div>
      <form [formGroup]="menuForm" (ngSubmit)="saveMenuItem()" novalidate><label class="field"><span>Dish name</span><input formControlName="name" maxlength="150" placeholder="What are we calling it?"><small class="field__error">{{ requiredError(menuForm, 'name', 2, 150) }}</small></label><label class="field"><span>Description <small>(optional)</small></span><textarea formControlName="description" maxlength="500" rows="3" placeholder="A few words about flavours and ingredients"></textarea><small class="field__error">{{ maxError(menuForm, 'description', 500) }}</small></label><div class="form-row"><label class="field"><span>Price (INR)</span><input formControlName="price" type="number" inputmode="decimal" min="0.01" max="9999999999.99" step="0.01" placeholder="249.00"><small class="field__error">{{ priceError }}</small></label><label class="field"><span>Category</span><select formControlName="categoryId"><option value="">Choose a category</option>@for (category of activeCategories; track category.id) { <option [value]="category.id">{{ category.name }}</option> }</select><small class="field__error">{{ menuForm.controls.categoryId.invalid && menuForm.controls.categoryId.touched ? 'Choose an active category.' : '' }}</small></label></div><label class="field"><span>Dish image URL <small>(optional)</small></span><input formControlName="imageUrl" maxlength="2048" type="url" placeholder="https://images.example.com/your-dish.jpg"><small class="field__error">{{ urlError(menuForm, 'imageUrl') }}</small><small class="field__hint">Use a direct HTTP or HTTPS image URL. You can add real uploads later.</small></label>@if (editingMenuId) { <label class="check-label check-label--default"><input type="checkbox" formControlName="isAvailable"><span>Show this dish as available to customers</span></label> }<div class="form-modal__actions"><button type="button" class="button button--outline" (click)="closeForms()" [disabled]="saving">Cancel</button><button type="submit" class="button button--primary" [disabled]="saving">{{ saving ? 'Saving…' : editingMenuId ? 'Save dish' : 'Add dish' }} <fs-icon name="arrow" /></button></div></form>
    </section></div> }

    @if (categoryFormOpen) { <div class="modal-backdrop" (click)="closeForms()"><section class="form-modal form-modal--narrow" role="dialog" aria-modal="true" aria-labelledby="category-form-title" (click)="$event.stopPropagation()"><div class="modal-heading"><div><p class="eyebrow">SHAPE THE CATALOGUE</p><h2 id="category-form-title">{{ editingCategoryId ? 'Edit category.' : 'Add a category.' }}</h2></div><button class="modal-close" (click)="closeForms()" aria-label="Close category form"><fs-icon name="close" /></button></div><form [formGroup]="categoryForm" (ngSubmit)="saveCategory()" novalidate><label class="field"><span>Category name</span><input formControlName="name" maxlength="100" placeholder="For example, North Indian"><small class="field__error">{{ requiredError(categoryForm, 'name', 2, 100) }}</small></label><label class="field"><span>Description <small>(optional)</small></span><textarea formControlName="description" maxlength="300" rows="3" placeholder="A little context for the menu"></textarea><small class="field__error">{{ maxError(categoryForm, 'description', 300) }}</small></label><div class="form-modal__actions"><button type="button" class="button button--outline" (click)="closeForms()">Cancel</button><button class="button button--primary" type="submit" [disabled]="saving">{{ saving ? 'Saving…' : editingCategoryId ? 'Save changes' : 'Create category' }} <fs-icon name="arrow" /></button></div></form></section></div> }

    @if (reviewTarget) { <div class="modal-backdrop" (click)="reviewTarget = null"><section class="form-modal form-modal--narrow" role="dialog" aria-modal="true" aria-labelledby="review-title" (click)="$event.stopPropagation()"><div class="modal-heading"><div><p class="eyebrow">ADMINISTRATOR REVIEW</p><h2 id="review-title">{{ reviewApprove ? 'Approve partner access?' : 'Decline this request?' }}</h2><p>{{ reviewTarget.applicantName }} · {{ roleName(reviewTarget.requestedRole) }}</p></div><button class="modal-close" (click)="reviewTarget = null" aria-label="Close review"><fs-icon name="close" /></button></div><div class="review-modal-note"><strong>Application details</strong><p>{{ reviewTarget.applicationDetails }}</p></div><form [formGroup]="reviewForm" (ngSubmit)="submitReview()" novalidate><label class="field"><span>{{ reviewApprove ? 'Review note' : 'Reason for declining' }} {{ reviewApprove ? '(optional)' : '' }}</span><textarea formControlName="reviewNote" maxlength="500" rows="4" [placeholder]="reviewApprove ? 'Add a note for the applicant, if useful.' : 'Please explain what the applicant could improve.'"></textarea><span class="field__counter">{{ reviewForm.controls.reviewNote.value?.length || 0 }}/500</span><small class="field__error">{{ reviewForm.controls.reviewNote.invalid && reviewForm.controls.reviewNote.touched ? reviewApprove ? 'Use 500 characters or fewer.' : 'A short reason is required (2–500 characters).' : '' }}</small></label><div class="form-modal__actions"><button type="button" class="button button--outline" (click)="reviewTarget = null" [disabled]="saving">Go back</button><button class="button" [class.button--primary]="reviewApprove" [class.button--danger]="!reviewApprove" type="submit" [disabled]="saving">{{ saving ? 'Saving review…' : reviewApprove ? 'Approve application' : 'Decline application' }} <fs-icon [name]="reviewApprove ? 'check' : 'close'" /></button></div></form></section></div> }

    @if (restaurantDeactivateTarget || menuDeactivateTarget || categoryDeactivateTarget) { <div class="modal-backdrop" (click)="clearDeactivateTargets()"><section class="confirm-modal" role="dialog" aria-modal="true" aria-labelledby="deactivate-title" (click)="$event.stopPropagation()"><span class="confirm-modal__icon"><fs-icon name="trash" /></span><h2 id="deactivate-title">{{ menuDeactivateTarget ? 'Hide this menu item?' : categoryDeactivateTarget ? 'Deactivate this category?' : 'Close this restaurant?' }}</h2><p>{{ menuDeactivateTarget ? menuDeactivateTarget.name + ' will no longer appear in the available menu. Past orders keep their saved item details.' : categoryDeactivateTarget ? categoryDeactivateTarget.name + ' will no longer be available for new menu items.' : (restaurantDeactivateTarget?.name ?? '') + ' will no longer appear in the public restaurant catalogue.' }}</p><div class="confirm-modal__actions"><button class="button button--outline" (click)="clearDeactivateTargets()">Keep it active</button><button class="button button--danger" (click)="confirmDeactivate()" [disabled]="saving">{{ saving ? 'Updating…' : 'Confirm change' }}</button></div></section></div> }
  `
})
export class ManagePageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly fallbackImage = 'https://images.unsplash.com/photo-1751200503125-d8cb239f95ba?auto=format&fit=crop&w=1200&q=82';
  readonly isAdmin = this.auth.role === 'Admin';
  restaurants: Restaurant[] = [];
  categories: Category[] = [];
  menuItems: MenuItem[] = [];
  pendingApplications: RoleApplication[] = [];
  selectedRestaurantId: number | null = null;
  selectedRestaurant: Restaurant | null = null;
  tab: AdminTab | OwnerTab = this.isAdmin ? 'applications' : 'restaurants';
  loading = true;
  saving = false;
  errorMessage = '';
  restaurantFormOpen = false;
  menuFormOpen = false;
  categoryFormOpen = false;
  editingRestaurantId: number | null = null;
  editingMenuId: number | null = null;
  editingCategoryId: number | null = null;
  restaurantDeactivateTarget: Restaurant | null = null;
  menuDeactivateTarget: MenuItem | null = null;
  categoryDeactivateTarget: Category | null = null;
  reviewTarget: RoleApplication | null = null;
  reviewApprove = true;

  readonly restaurantForm = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
    description: ['', Validators.maxLength(500)],
    address: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(250)]],
    city: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    phoneNumber: ['', Validators.maxLength(30)],
    imageUrl: ['', [Validators.maxLength(2048), Validators.pattern(/^$|^https?:\/\/.+/i)]]
  });
  readonly menuForm = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
    description: ['', Validators.maxLength(500)],
    price: [null as number | null, [Validators.required, Validators.min(0.01), Validators.max(9999999999.99)]],
    categoryId: ['', Validators.required],
    imageUrl: ['', [Validators.maxLength(2048), Validators.pattern(/^$|^https?:\/\/.+/i)]],
    isAvailable: [true]
  });
  readonly categoryForm = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    description: ['', Validators.maxLength(300)]
  });
  readonly reviewForm = this.fb.group({ reviewNote: ['', Validators.maxLength(500)] });

  get activeCategories(): Category[] { return this.categories.filter(category => category.isActive); }
  get hasActiveCategories(): boolean { return this.categories.some(category => category.isActive); }
  get priceError(): string { const field = this.menuForm.controls.price; if (!field.invalid || !field.touched) return ''; return field.hasError('required') ? 'Enter a price.' : field.hasError('min') ? 'Price must be greater than zero.' : field.hasError('max') ? 'That price is above the supported limit.' : 'Use a valid price with up to two decimal places.'; }

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      const requested = params.get('tab');
      if (this.isAdmin && ['applications', 'categories', 'restaurants', 'menu'].includes(requested ?? '')) this.tab = requested as AdminTab;
      else if (!this.isAdmin && requested === 'menu') this.tab = 'menu';
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';
    if (this.isAdmin) {
      forkJoin({ restaurants: this.api.restaurants(1, 100), categories: this.api.categories(), applications: this.api.pendingApplications() }).pipe(finalize(() => this.loading = false)).subscribe({
        next: data => { this.restaurants = data.restaurants.items; this.categories = data.categories; this.pendingApplications = data.applications.filter(app => app.status === 'Pending').sort((a, b) => Date.parse(a.submittedAtUtc) - Date.parse(b.submittedAtUtc)); if (!this.selectedRestaurantId || !this.restaurants.some(item => item.id === this.selectedRestaurantId)) this.selectedRestaurantId = this.restaurants[0]?.id ?? null; this.selectedRestaurant = this.restaurants.find(item => item.id === this.selectedRestaurantId) ?? null; if (this.tab === 'menu' && this.selectedRestaurant) this.loadMenuItems(); },
        error: error => this.errorMessage = getErrorMessage(error, 'Administration data could not be loaded.')
      });
    } else {
      forkJoin({ restaurants: this.api.myRestaurants(), categories: this.api.categories() }).pipe(finalize(() => this.loading = false)).subscribe({
        next: data => { this.restaurants = data.restaurants; this.categories = data.categories; if (!this.selectedRestaurantId || !data.restaurants.some(item => item.id === this.selectedRestaurantId)) this.selectedRestaurantId = data.restaurants[0]?.id ?? null; this.selectedRestaurant = data.restaurants.find(item => item.id === this.selectedRestaurantId) ?? null; if (this.selectedRestaurant) this.loadMenuItems(); else this.menuItems = []; },
        error: error => this.errorMessage = getErrorMessage(error, 'Your restaurant studio could not be loaded.')
      });
    }
  }

  setTab(tab: AdminTab | OwnerTab): void { this.tab = tab; this.menuFormOpen = false; if (tab === 'menu' && this.selectedRestaurantId) this.loadMenuItems(); }
  changeMenuRestaurant(event: Event): void { const restaurant = this.restaurants.find(item => item.id === Number((event.target as HTMLSelectElement).value)); if (restaurant) this.selectRestaurant(restaurant); }
  selectRestaurant(restaurant: Restaurant): void { this.selectedRestaurant = restaurant; this.selectedRestaurantId = restaurant.id; this.loadMenuItems(); }
  private loadMenuItems(): void { if (!this.selectedRestaurantId) { this.menuItems = []; return; } this.api.managedMenu(this.selectedRestaurantId).subscribe({ next: items => this.menuItems = items, error: error => { this.errorMessage = getErrorMessage(error, 'The menu could not be loaded.'); } }); }

  roleName(role: UserRole): string { return role === 'RestaurantOwner' ? 'Restaurant owner' : role === 'DeliveryPartner' ? 'Delivery partner' : role; }
  requiredError(form: import('@angular/forms').FormGroup, name: string, min: number, max: number): string {
    const control = form.get(name);
    if (!control?.invalid || !control.touched) return '';
    if (control.hasError('required')) return 'This field is required.';
    if (control.hasError('minlength')) return `Use at least ${min} characters.`;
    if (control.hasError('maxlength')) return `Use ${max} characters or fewer.`;
    return 'Please check this field.';
  }
  maxError(form: import('@angular/forms').FormGroup, name: string, max: number): string { const field = form.get(name); return field?.hasError('maxlength') && field.touched ? `Use ${max} characters or fewer.` : ''; }
  urlError(form: import('@angular/forms').FormGroup, name: string): string { const field = form.get(name); return field?.invalid && field.touched ? field.hasError('maxlength') ? 'Image URL must be 2,048 characters or fewer.' : 'Use an absolute HTTP or HTTPS URL.' : ''; }

  startRestaurantCreate(): void { this.editingRestaurantId = null; this.restaurantForm.reset({ name: '', description: '', address: '', city: '', phoneNumber: '', imageUrl: '' }); this.restaurantFormOpen = true; }
  startRestaurantEdit(restaurant: Restaurant): void { this.editingRestaurantId = restaurant.id; this.restaurantForm.reset({ name: restaurant.name, description: restaurant.description ?? '', address: restaurant.address, city: restaurant.city, phoneNumber: restaurant.phoneNumber ?? '', imageUrl: restaurant.imageUrl ?? '' }); this.restaurantFormOpen = true; }
  saveRestaurant(): void {
    this.restaurantForm.markAllAsTouched();
    if (this.restaurantForm.invalid) return;
    const value = this.restaurantForm.getRawValue();
    const body = { name: value.name!.trim(), description: value.description?.trim() || null, address: value.address!.trim(), city: value.city!.trim(), phoneNumber: value.phoneNumber?.trim() || null, imageUrl: value.imageUrl?.trim() || null };
    this.saving = true;
    const request = this.editingRestaurantId ? this.api.updateRestaurant(this.editingRestaurantId, body) : this.api.createRestaurant(body);
    request.pipe(finalize(() => this.saving = false)).subscribe({ next: restaurant => { this.restaurantFormOpen = false; this.toast.show(this.editingRestaurantId ? 'Restaurant details updated.' : 'Your restaurant is ready to manage.', 'success'); if (this.isAdmin) this.load(); else { this.restaurants = this.editingRestaurantId ? this.restaurants.map(item => item.id === restaurant.id ? restaurant : item) : [...this.restaurants, restaurant]; this.selectRestaurant(restaurant); } }, error: error => this.toast.show(getErrorMessage(error, 'Restaurant details could not be saved.'), 'error') });
  }

  startMenuCreate(): void { if (!this.selectedRestaurantId || !this.activeCategories.length) return; this.editingMenuId = null; this.menuForm.reset({ name: '', description: '', price: null, categoryId: '', imageUrl: '', isAvailable: true }); this.menuFormOpen = true; }
  startMenuEdit(item: MenuItem): void { this.editingMenuId = item.id; this.menuForm.reset({ name: item.name, description: item.description ?? '', price: item.price, categoryId: String(item.categoryId), imageUrl: item.imageUrl ?? '', isAvailable: item.isAvailable }); this.menuFormOpen = true; }
  saveMenuItem(): void {
    this.menuForm.markAllAsTouched();
    if (this.menuForm.invalid || !this.selectedRestaurantId) return;
    const value = this.menuForm.getRawValue();
    const price = Number(value.price);
    if (!Number.isFinite(price) || Math.round(price * 100) !== price * 100) { this.menuForm.controls.price.setErrors({ decimal: true }); this.menuForm.controls.price.markAsTouched(); return; }
    const body = { name: value.name!.trim(), description: value.description?.trim() || null, price, categoryId: Number(value.categoryId), imageUrl: value.imageUrl?.trim() || null, ...(this.editingMenuId ? { isAvailable: value.isAvailable } : {}) };
    this.saving = true;
    const request = this.editingMenuId ? this.api.updateMenuItem(this.selectedRestaurantId, this.editingMenuId, body) : this.api.createMenuItem(this.selectedRestaurantId, body);
    request.pipe(finalize(() => this.saving = false)).subscribe({ next: () => { this.menuFormOpen = false; this.toast.show(this.editingMenuId ? 'Dish details updated.' : 'Dish added to your menu.', 'success'); this.loadMenuItems(); }, error: error => this.toast.show(getErrorMessage(error, 'The menu item could not be saved.'), 'error') });
  }

  startCategoryCreate(): void { this.editingCategoryId = null; this.categoryForm.reset({ name: '', description: '' }); this.categoryFormOpen = true; }
  startCategoryEdit(category: Category): void { this.editingCategoryId = category.id; this.categoryForm.reset({ name: category.name, description: category.description ?? '' }); this.categoryFormOpen = true; }
  saveCategory(): void { this.categoryForm.markAllAsTouched(); if (this.categoryForm.invalid) return; const value = this.categoryForm.getRawValue(); const body = { name: value.name!.trim(), description: value.description?.trim() || null }; this.saving = true; const request = this.editingCategoryId ? this.api.updateCategory(this.editingCategoryId, body) : this.api.createCategory(body); request.pipe(finalize(() => this.saving = false)).subscribe({ next: () => { this.categoryFormOpen = false; this.toast.show(this.editingCategoryId ? 'Category updated.' : 'Category created.', 'success'); this.load(); }, error: error => this.toast.show(getErrorMessage(error, 'The category could not be saved.'), 'error') }); }

  openReview(application: RoleApplication, approve: boolean): void {
    this.reviewTarget = application; this.reviewApprove = approve;
    const note = this.reviewForm.controls.reviewNote;
    note.setValidators(approve ? [Validators.maxLength(500)] : [Validators.required, Validators.minLength(2), Validators.maxLength(500)]);
    note.reset(''); note.updateValueAndValidity();
  }
  submitReview(): void { this.reviewForm.markAllAsTouched(); if (this.reviewForm.invalid || !this.reviewTarget) return; const target = this.reviewTarget; const approve = this.reviewApprove; const note = this.reviewForm.controls.reviewNote.value?.trim() ?? ''; this.saving = true; this.api.reviewApplication(target.id, approve, note).pipe(finalize(() => this.saving = false)).subscribe({ next: () => { this.reviewTarget = null; this.toast.show(approve ? `${target.applicantName} was approved. They need to sign in again for the new role.` : `Application from ${target.applicantName} was declined.`, 'success'); this.load(); }, error: error => this.toast.show(getErrorMessage(error, 'The application could not be reviewed.'), 'error') }); }

  askRestaurantDeactivate(restaurant: Restaurant): void { this.restaurantDeactivateTarget = restaurant; }
  askMenuDeactivate(item: MenuItem): void { this.menuDeactivateTarget = item; }
  confirmCategoryDeactivate(category: Category): void { this.categoryDeactivateTarget = category; }
  clearDeactivateTargets(): void { if (!this.saving) { this.restaurantDeactivateTarget = null; this.menuDeactivateTarget = null; this.categoryDeactivateTarget = null; } }
  confirmDeactivate(): void {
    this.saving = true;
    if (this.restaurantDeactivateTarget) {
      const target = this.restaurantDeactivateTarget;
      this.api.deactivateRestaurant(target.id).pipe(finalize(() => this.saving = false)).subscribe({ next: () => { this.clearDeactivateTargets(); this.toast.show(`${target.name} is no longer active.`, 'success'); this.load(); }, error: error => this.toast.show(getErrorMessage(error, 'The restaurant could not be deactivated.'), 'error') });
    } else if (this.menuDeactivateTarget && this.selectedRestaurantId) {
      const target = this.menuDeactivateTarget;
      this.api.deactivateMenuItem(this.selectedRestaurantId, target.id).pipe(finalize(() => this.saving = false)).subscribe({ next: () => { this.clearDeactivateTargets(); this.toast.show(`${target.name} is now hidden from the menu.`, 'success'); this.loadMenuItems(); }, error: error => this.toast.show(getErrorMessage(error, 'The menu item could not be hidden.'), 'error') });
    } else if (this.categoryDeactivateTarget) {
      const target = this.categoryDeactivateTarget;
      this.api.deactivateCategory(target.id).pipe(finalize(() => this.saving = false)).subscribe({ next: () => { this.clearDeactivateTargets(); this.toast.show(`${target.name} is no longer available for new dishes.`, 'success'); this.load(); }, error: error => this.toast.show(getErrorMessage(error, 'The category could not be deactivated.'), 'error') });
    } else this.saving = false;
  }

  closeForms(): void { if (!this.saving) { this.restaurantFormOpen = false; this.menuFormOpen = false; this.categoryFormOpen = false; } }
}
