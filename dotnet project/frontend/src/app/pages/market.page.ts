import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, forkJoin, of } from 'rxjs';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { getErrorMessage } from '../core/error-message';
import { Address, CartLine, Category, MenuItem, Restaurant } from '../core/models';
import { ToastService } from '../core/toast.service';
import { IconComponent } from '../shared/icon.component';

const CART_KEY = 'foodsupply.cart.v1';
const FOOD_FALLBACK = 'https://images.unsplash.com/photo-1631515243349-e0cb75fb8d3a?auto=format&fit=crop&w=1000&q=82';

@Component({
  selector: 'fs-market-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, IconComponent],
  template: `
    <section class="page market-page">
      <div class="market-hero"><div class="market-hero__copy"><p class="eyebrow eyebrow--light">A GOOD MEAL IS CLOSER THAN YOU THINK</p><h1>Find your<br><em>next favourite.</em></h1><p>Neighbourhood kitchens, beautiful food, delivered to your door.</p><div class="market-search"><fs-icon name="search" /><input [value]="search" (input)="setSearch($event)" placeholder="Search restaurants, neighbourhoods…" aria-label="Search restaurants"><button *ngIf="search" (click)="search = ''; loadRestaurants()" aria-label="Clear search"><fs-icon name="close" /></button></div></div><div class="market-hero__art"><div class="market-hero__plate"><img [src]="heroImage" alt="Freshly prepared biryani" fetchpriority="high"></div><span class="market-hero__stamp">GOOD FOOD<br>GATHERED WELL</span><span class="market-hero__leaf market-hero__leaf--one">✦</span><span class="market-hero__leaf market-hero__leaf--two">✧</span></div></div>

      <div class="section-heading market-section-heading"><div><p class="eyebrow">THE NEIGHBOURHOOD TABLE</p><h2>{{ selectedRestaurant ? selectedRestaurant.name + ' menu' : search ? 'A few good matches' : 'Kitchens worth knowing' }}</h2><p class="section-heading__sub">{{ selectedRestaurant ? selectedRestaurant.city + ' · choose a dish to add it to your basket' : 'Thoughtful menus from local restaurants, all in one place.' }}</p></div>@if (selectedRestaurant) { <button class="button button--outline" (click)="backToRestaurants()"><fs-icon name="arrow" /> All restaurants</button> }</div>

      @if (errorMessage) { <div class="inline-alert" role="alert"><fs-icon name="close" /><span>{{ errorMessage }}</span><button class="text-button" (click)="selectedRestaurant ? loadMenu(selectedRestaurant) : loadRestaurants()">Retry</button></div> }
      @if (loading) { <div class="restaurant-grid">@for (i of [1,2,3,4,5,6]; track i) { <div class="restaurant-skeleton"></div> }</div> }
      @else if (!selectedRestaurant) {
        @if (restaurants.length) { <div class="restaurant-grid">@for (restaurant of filteredRestaurants; track restaurant.id) {
          <button class="restaurant-card" (click)="openRestaurant(restaurant)"><span class="restaurant-card__image"><img [src]="restaurant.imageUrl || heroImage" [alt]="restaurant.name" loading="lazy"><span class="restaurant-card__badge"><fs-icon name="sparkles" /> Neighbourhood kitchen</span><span class="restaurant-card__arrow"><fs-icon name="arrow" /></span></span><span class="restaurant-card__content"><span><strong>{{ restaurant.name }}</strong><small><fs-icon name="pin" /> {{ restaurant.city }}</small></span><span class="restaurant-card__open"><i></i> Open</span></span><span class="restaurant-card__description">{{ restaurant.description || restaurant.address }}</span></button>
        }</div> @if (!filteredRestaurants.length) { <div class="empty-state"><span class="empty-state__icon"><fs-icon name="search" /></span><strong>No kitchens found</strong><p>Try a different restaurant name or neighbourhood.</p><button class="button button--outline" (click)="search = ''">Clear search</button></div> } }
        @else { <div class="empty-state"><span class="empty-state__icon"><fs-icon name="market" /></span><strong>The table is being set</strong><p>There are no active restaurants in the catalogue yet. Come back when the first kitchen opens.</p></div> }
      } @else {
        <div class="menu-layout">
          <div class="menu-main">
            @if (categories.length) { <div class="category-pills"><button [class.category-pill--active]="activeCategory === 'All'" (click)="activeCategory = 'All'">Everything</button>@for (category of categories; track category.id) { <button [class.category-pill--active]="activeCategory === category.name" (click)="activeCategory = category.name">{{ category.name }}</button> }</div> }
            @if (menuItems.length) { <div class="menu-grid">@for (item of filteredMenu; track item.id) { <article class="menu-card"><div class="menu-card__image"><img [src]="item.imageUrl || heroImage" [alt]="item.name" loading="lazy"><span class="menu-card__category">{{ item.categoryName }}</span></div><div class="menu-card__copy"><h3>{{ item.name }}</h3><p>{{ item.description || 'Made fresh in our neighbourhood kitchen.' }}</p><div class="menu-card__bottom"><strong>{{ item.price | currency:'INR':'symbol':'1.0-0' }}</strong><button class="add-button" (click)="addToCart(item)" [attr.aria-label]="'Add ' + item.name + ' to basket'"><fs-icon name="plus" /> Add</button></div></div></article> }</div> @if (!filteredMenu.length) { <div class="empty-inline">No dishes in this category just yet.</div> } }
            @else { <div class="empty-state"><span class="empty-state__icon"><fs-icon name="bag" /></span><strong>This menu is being refreshed</strong><p>This restaurant has no available dishes at the moment.</p></div> }
          </div>
          <aside class="basket-card"><div class="basket-card__head"><span class="basket-card__icon"><fs-icon name="bag" /></span><div><p class="eyebrow">YOUR BASKET</p><h3>{{ cartCount }} {{ cartCount === 1 ? 'dish' : 'dishes' }}</h3></div><button class="basket-card__clear" *ngIf="cart.length" (click)="clearCart()">Clear</button></div>
            @if (cart.length) { <div class="basket-lines">@for (line of cart; track line.item.id) { <div class="basket-line"><div class="basket-line__main"><strong>{{ line.item.name }}</strong><span>{{ line.item.price | currency:'INR':'symbol':'1.0-0' }}</span></div><div class="quantity-control"><button (click)="changeQuantity(line.item, -1)" [attr.aria-label]="'Remove one ' + line.item.name">−</button><span>{{ line.quantity }}</span><button (click)="changeQuantity(line.item, 1)" [attr.aria-label]="'Add one ' + line.item.name">+</button></div></div> }</div><div class="basket-total"><span>Subtotal</span><strong>{{ cartSubtotal | currency:'INR':'symbol':'1.0-0' }}</strong></div><p class="basket-hint">Delivery and taxes are calculated at checkout.</p><button class="button button--primary button--full" (click)="openCheckout()">Continue to checkout <fs-icon name="arrow" /></button> }
            @else { <div class="basket-empty"><span><fs-icon name="bag" /></span><strong>Your basket is taking a breather</strong><p>Add a dish from the menu and it will appear here.</p></div> }
          </aside>
        </div>
      }
      @if (checkoutOpen) { <div class="modal-backdrop" (click)="closeCheckout()"><section class="checkout-modal" role="dialog" aria-modal="true" aria-labelledby="checkout-title" (click)="$event.stopPropagation()"><div class="modal-heading"><div><p class="eyebrow">ONE LAST DETAIL</p><h2 id="checkout-title">Make it yours.</h2><p>Choose your address and we’ll send your order to the kitchen.</p></div><button class="modal-close" (click)="closeCheckout()" aria-label="Close checkout"><fs-icon name="close" /></button></div>
          @if (!addresses.length && !loadingAddresses) { <div class="empty-state empty-state--compact"><span class="empty-state__icon"><fs-icon name="addresses" /></span><strong>Add a delivery address first</strong><p>Your saved addresses make checkout quicker next time.</p><a class="button button--outline" routerLink="/app/addresses" (click)="closeCheckout()">Manage addresses <fs-icon name="arrow" /></a></div> }
          @else { <form [formGroup]="checkoutForm" (ngSubmit)="placeOrder()" novalidate><label class="field"><span>Delivery address</span><select formControlName="addressId"><option value="">Choose an address</option>@for (address of addresses; track address.id) { <option [value]="address.id">{{ address.label }} · {{ address.line1 }}, {{ address.city }}{{ address.isDefault ? ' · Default' : '' }}</option> }</select><small class="field__error">{{ showCheckoutError('addressId') ? 'Choose a delivery address.' : '' }}</small></label><div class="form-row"><label class="field"><span>Recipient name</span><input formControlName="recipientName" maxlength="200" autocomplete="name" placeholder="Name at the door"><small class="field__error">{{ showCheckoutError('recipientName') ? 'Enter a recipient name.' : '' }}</small></label><label class="field"><span>Phone number</span><input formControlName="recipientPhoneNumber" maxlength="30" type="tel" autocomplete="tel" placeholder="+91 98765 43210"><small class="field__error">{{ showCheckoutError('recipientPhoneNumber') ? 'Enter a valid contact number.' : '' }}</small></label></div><label class="field"><span>Note for the kitchen <small>(optional)</small></span><textarea formControlName="customerNote" maxlength="500" rows="3" placeholder="Anything the kitchen should know?"></textarea><small class="field__counter">{{ checkoutForm.controls.customerNote.value?.length || 0 }}/500</small></label><div class="checkout-summary"><span>Food subtotal · {{ cartCount }} dishes</span><strong>{{ cartSubtotal | currency:'INR':'symbol':'1.0-0' }}</strong></div><div class="checkout-callout"><fs-icon name="sparkles" /><span>Cash on delivery. Delivery and tax are calculated with your order; no card details are needed.</span></div><button class="button button--primary button--full" type="submit" [disabled]="placingOrder">{{ placingOrder ? 'Sending to the kitchen…' : 'Place cash-on-delivery order' }}<fs-icon name="arrow" /></button></form> }
        </section></div> }
    </section>
  `
})
export class MarketPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  readonly heroImage = FOOD_FALLBACK;
  restaurants: Restaurant[] = [];
  selectedRestaurant: Restaurant | null = null;
  menuItems: MenuItem[] = [];
  categories: Category[] = [];
  addresses: Address[] = [];
  cart: CartLine[] = [];
  search = '';
  activeCategory = 'All';
  loading = true;
  loadingAddresses = false;
  placingOrder = false;
  checkoutOpen = false;
  errorMessage = '';
  readonly checkoutForm = this.fb.group({
    addressId: ['', Validators.required],
    recipientName: ['', [Validators.required, Validators.maxLength(200)]],
    recipientPhoneNumber: ['', [Validators.required, Validators.maxLength(30), Validators.pattern(/^[+()\d .-]{7,30}$/)]],
    customerNote: ['', Validators.maxLength(500)]
  });

  get filteredRestaurants(): Restaurant[] {
    const query = this.search.trim().toLocaleLowerCase();
    return this.restaurants.filter(item => !query || `${item.name} ${item.city} ${item.address}`.toLocaleLowerCase().includes(query));
  }
  get filteredMenu(): MenuItem[] { return this.menuItems.filter(item => item.isAvailable && (this.activeCategory === 'All' || item.categoryName === this.activeCategory)); }
  get cartCount(): number { return this.cart.reduce((total, line) => total + line.quantity, 0); }
  get cartSubtotal(): number { return this.cart.reduce((total, line) => total + line.item.price * line.quantity, 0); }

  ngOnInit(): void { this.loadRestaurants(); }

  loadRestaurants(): void {
    this.loading = true;
    this.errorMessage = '';
    this.api.restaurants(1, 100).pipe(finalize(() => this.loading = false)).subscribe({
      next: data => this.restaurants = data.items,
      error: error => this.errorMessage = getErrorMessage(error, 'Restaurants could not be loaded.')
    });
  }

  setSearch(event: Event): void { this.search = (event.target as HTMLInputElement).value; }

  openRestaurant(restaurant: Restaurant): void {
    this.selectedRestaurant = restaurant;
    this.activeCategory = 'All';
    this.loadCart();
    this.loadMenu(restaurant);
  }

  loadMenu(restaurant: Restaurant): void {
    this.loading = true;
    this.errorMessage = '';
    forkJoin({ menu: this.api.menu(restaurant.id), categories: this.api.categories() })
      .pipe(finalize(() => this.loading = false)).subscribe({
        next: data => {
          this.menuItems = data.menu;
          this.categories = data.categories.filter(category => data.menu.some(item => item.categoryId === category.id));
          const currentItems = new Map(data.menu.map(item => [item.id, item]));
          this.cart = this.cart.map(line => ({ ...line, item: currentItems.get(line.item.id)! }))
            .filter(line => line.item && line.item.isAvailable);
          this.saveCart();
        },
        error: error => this.errorMessage = getErrorMessage(error, 'This restaurant menu could not be loaded.')
      });
  }

  backToRestaurants(): void { this.selectedRestaurant = null; this.menuItems = []; this.categories = []; }

  addToCart(item: MenuItem): void {
    if (!this.selectedRestaurant) return;
    const existing = this.cart.find(line => line.item.id === item.id);
    if (existing) existing.quantity = Math.min(existing.quantity + 1, 20);
    else this.cart.push({ item, quantity: 1 });
    this.saveCart();
    this.toast.show(`${item.name} added to your basket.`, 'success');
  }

  changeQuantity(item: MenuItem, amount: number): void {
    const line = this.cart.find(row => row.item.id === item.id);
    if (!line) return;
    line.quantity += amount;
    if (line.quantity <= 0) this.cart = this.cart.filter(row => row.item.id !== item.id);
    if (line.quantity > 20) line.quantity = 20;
    this.saveCart();
  }

  clearCart(): void { this.cart = []; this.saveCart(); }

  openCheckout(): void {
    if (!this.cart.length || !this.selectedRestaurant) return;
    this.checkoutOpen = true;
    this.loadingAddresses = true;
    this.api.addresses().pipe(finalize(() => this.loadingAddresses = false)).subscribe({
      next: addresses => {
        this.addresses = addresses;
        const selected = addresses.find(address => address.isDefault) ?? addresses[0];
        this.checkoutForm.patchValue({ addressId: selected ? String(selected.id) : '', recipientName: `${this.auth.session?.firstName ?? ''} ${this.auth.session?.lastName ?? ''}`.trim() });
      },
      error: error => { this.checkoutOpen = false; this.toast.show(getErrorMessage(error, 'Saved addresses could not be loaded.'), 'error'); }
    });
  }

  closeCheckout(): void { if (!this.placingOrder) this.checkoutOpen = false; }
  showCheckoutError(name: string): boolean { const control = this.checkoutForm.get(name); return !!control?.invalid && (control.touched || this.checkoutForm.touched); }

  placeOrder(): void {
    this.checkoutForm.markAllAsTouched();
    if (this.checkoutForm.invalid || !this.selectedRestaurant || !this.cart.length) return;
    const data = this.checkoutForm.getRawValue();
    this.placingOrder = true;
    this.api.createOrder({
      restaurantId: this.selectedRestaurant.id,
      addressId: Number(data.addressId),
      recipientName: data.recipientName!.trim(),
      recipientPhoneNumber: data.recipientPhoneNumber!.trim(),
      customerNote: data.customerNote?.trim() || null,
      items: this.cart.map(line => ({ menuItemId: line.item.id, quantity: line.quantity, specialInstructions: null }))
    }).pipe(finalize(() => this.placingOrder = false)).subscribe({
      next: order => {
        this.clearCart();
        this.checkoutOpen = false;
        this.toast.show(`Order ${order.orderNumber} is on its way to the kitchen.`, 'success');
        void this.router.navigate(['/app/orders']);
      },
      error: error => this.toast.show(getErrorMessage(error, 'The order could not be placed.'), 'error')
    });
  }

  private loadCart(): void {
    try {
      const saved = localStorage.getItem(CART_KEY);
      const parsed = saved ? JSON.parse(saved) as { restaurantId: number; items: CartLine[] } : null;
      this.cart = parsed && parsed.restaurantId === this.selectedRestaurant?.id && Array.isArray(parsed.items)
        ? parsed.items.filter(line => line.item?.id && line.quantity > 0 && line.quantity <= 20)
        : [];
    } catch { this.cart = []; }
  }

  private saveCart(): void {
    const restaurantId = this.selectedRestaurant?.id;
    if (!restaurantId || !this.cart.length) { localStorage.removeItem(CART_KEY); return; }
    localStorage.setItem(CART_KEY, JSON.stringify({ restaurantId, items: this.cart }));
  }
}
