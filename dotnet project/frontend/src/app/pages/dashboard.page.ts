import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EMPTY, Observable, Subscription, catchError, forkJoin, interval, of, startWith, switchMap } from 'rxjs';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { getErrorMessage } from '../core/error-message';
import { OrderSummary, OrderStatus, Restaurant, RoleApplication, UserRole } from '../core/models';
import { ToastService } from '../core/toast.service';
import { IconComponent } from '../shared/icon.component';

interface Metric { label: string; value: number | string; note: string; icon: string; tone: string; link?: string; }
interface Bar { label: string; value: number; width: number; tone: string; }

@Component({
  selector: 'fs-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink, IconComponent],
  template: `
    <section class="page dashboard-page">
      <div class="page-heading page-heading--welcome">
        <div><p class="eyebrow">{{ greeting }} · {{ dateLabel }}</p><h1>{{ welcomeTitle }}</h1><p class="page-heading__sub">{{ welcomeSubtitle }}</p></div>
        @if (role === 'Admin') { <button class="button button--outline" (click)="refreshNow()" [disabled]="loading"><fs-icon name="sessions" /> {{ loading ? 'Refreshing…' : 'Refresh overview' }}</button> }
        @if (role === 'Customer') { <a class="button button--primary" routerLink="/app/market"><fs-icon name="market" /> Find something lovely</a> }
        @if (role === 'RestaurantOwner') { <a class="button button--primary" routerLink="/app/manage"><fs-icon name="manage" /> Open restaurant studio</a> }
      </div>

      @if (errorMessage) { <div class="inline-alert" role="alert"><fs-icon name="close" /><span>{{ errorMessage }}</span><button class="text-button" (click)="refreshNow()">Try again</button></div> }

      <div class="metric-grid" [class.metric-grid--three]="metrics.length === 3">
        @for (metric of metrics; track metric.label) {
          <a class="metric-card" [ngClass]="'metric-card--' + metric.tone" [routerLink]="metric.link || null">
            <div class="metric-card__top"><span>{{ metric.label }}</span><span class="metric-card__icon"><fs-icon [name]="metric.icon" /></span></div>
            <strong class="metric-card__value">{{ metric.value }}</strong><span class="metric-card__note">{{ metric.note }}</span>
          </a>
        }
        @if (loading && metrics.length === 0) { @for (i of [1,2,3,4]; track i) { <div class="metric-skeleton"></div> } }
      </div>

      @if (role === 'Admin') {
        <div class="dashboard-grid dashboard-grid--admin">
          <section class="panel chart-panel">
            <div class="panel__heading"><div><p class="eyebrow">A LOOK AROUND TOWN</p><h2>Restaurant footprint</h2></div><span class="live-pill"><i></i> Updates every 30 sec</span></div>
            @if (cityBars.length) {
              <div class="bar-chart" role="img" aria-label="Active restaurants grouped by city">
                @for (bar of cityBars; track bar.label) { <div class="bar-chart__row"><span>{{ bar.label }}</span><div class="bar-chart__track"><i [style.width.%]="bar.width"></i></div><strong>{{ bar.value }}</strong></div> }
              </div>
              <p class="chart-footnote">{{ restaurantSnapshotNote }} · Restaurant directory from the live API</p>
            } @else { <div class="empty-state empty-state--compact"><span class="empty-state__icon"><fs-icon name="pin" /></span><strong>No restaurants yet</strong><p>Active restaurants will appear here as partners join.</p></div> }
          </section>
          <section class="panel chart-panel">
            <div class="panel__heading"><div><p class="eyebrow">PARTNER QUEUE</p><h2>Applications to review</h2></div><a class="panel-link" routerLink="/app/admin" [queryParams]="{ tab: 'applications' }">Review queue <fs-icon name="arrow" /></a></div>
            @if (roleBars.length) {
              <div class="role-chart">
                @for (bar of roleBars; track bar.label) { <div class="role-chart__row"><div class="role-chart__label"><span class="role-chart__dot" [ngClass]="'tone-' + bar.tone"></span><span>{{ bar.label }}</span><strong>{{ bar.value }}</strong></div><div class="bar-chart__track"><i [ngClass]="'tone-bg-' + bar.tone" [style.width.%]="bar.width"></i></div></div> }
              </div>
              <div class="chart-callout"><span class="chart-callout__spark"><fs-icon name="sparkles" /></span><span>Each application is reviewed by an administrator before a role changes.</span></div>
            } @else { <div class="empty-state empty-state--compact"><span class="empty-state__icon"><fs-icon name="check" /></span><strong>You're all caught up</strong><p>New partner requests will appear in this queue.</p></div> }
          </section>
        </div>
        <section class="panel table-panel">
          <div class="panel__heading"><div><p class="eyebrow">NEEDS YOUR EYE</p><h2>Latest partner requests</h2></div><a class="panel-link" routerLink="/app/admin" [queryParams]="{ tab: 'applications' }">See all <fs-icon name="arrow" /></a></div>
          @if (pendingApplications.length) {
            <div class="application-list">
              @for (application of pendingApplications.slice(0, 4); track application.id) {
                <div class="application-row"><span class="application-avatar">{{ application.applicantName.slice(0,1) }}</span><div class="application-row__who"><strong>{{ application.applicantName }}</strong><span>{{ application.businessName || roleName(application.requestedRole) }} · {{ application.applicantEmail }}</span></div><span class="status-chip status-chip--pending">Pending</span><span class="application-row__date">{{ application.submittedAtUtc | date:'d MMM, h:mm a' }}</span><a class="row-arrow" routerLink="/app/admin" [queryParams]="{ tab: 'applications' }" aria-label="Review application"><fs-icon name="arrow" /></a></div>
              }
            </div>
          } @else { <div class="empty-inline"><span>No applications are waiting for review.</span></div> }
        </section>
      }

      @if (role === 'Customer') {
        <div class="dashboard-grid dashboard-grid--customer">
          <section class="panel welcome-card"><div class="welcome-card__copy"><p class="eyebrow eyebrow--light">A LITTLE INSPIRATION</p><h2>Make tonight<br>the delicious part.</h2><p>Find a new local favourite, or return to the dishes you already love.</p><a class="button button--gold" routerLink="/app/market">Explore the menu <fs-icon name="arrow" /></a></div><div class="welcome-card__image"></div><div class="welcome-card__ornament">✦</div></section>
          <section class="panel recent-panel"><div class="panel__heading"><div><p class="eyebrow">YOUR RECENT ORDERS</p><h2>Back for seconds?</h2></div><a class="panel-link" routerLink="/app/orders">All orders <fs-icon name="arrow" /></a></div>
            @if (recentOrders.length) { @for (order of recentOrders.slice(0, 4); track order.id) { <a class="recent-order" routerLink="/app/orders"><span class="recent-order__icon"><fs-icon name="bag" /></span><span class="recent-order__main"><strong>{{ order.restaurantName }}</strong><small>{{ order.orderNumber }} · {{ order.placedAtUtc | date:'d MMM, h:mm a' }}</small></span><span class="recent-order__amount">{{ order.totalAmount | currency:'INR':'symbol':'1.0-0' }}</span><span class="status-chip" [ngClass]="'status-chip--' + order.status.toLowerCase()">{{ statusLabel(order.status) }}</span></a> } }
            @else { <div class="empty-state empty-state--compact"><span class="empty-state__icon"><fs-icon name="bag" /></span><strong>Your first order is waiting</strong><p>Explore local kitchens and find something worth sharing.</p><a class="panel-link" routerLink="/app/market">Browse restaurants <fs-icon name="arrow" /></a></div> }
          </section>
        </div>
        @if (featuredRestaurants.length) {
          <section class="section-block"><div class="section-heading"><div><p class="eyebrow">A FEW NEARBY FAVOURITES</p><h2>Good things, close by</h2></div><a class="panel-link" routerLink="/app/market">Discover all <fs-icon name="arrow" /></a></div><div class="restaurant-strip">@for (restaurant of featuredRestaurants.slice(0, 3); track restaurant.id) { <a class="mini-restaurant" routerLink="/app/market"><img [src]="restaurant.imageUrl || fallbackImage" [alt]="restaurant.name" loading="lazy"><span class="mini-restaurant__copy"><strong>{{ restaurant.name }}</strong><small><fs-icon name="pin" /> {{ restaurant.city }}</small></span><span class="mini-restaurant__arrow"><fs-icon name="arrow" /></span></a> }</div></section>
        }
      }

      @if (role === 'RestaurantOwner') {
        <div class="dashboard-grid dashboard-grid--owner">
          <section class="panel chart-panel"><div class="panel__heading"><div><p class="eyebrow">YOUR KITCHENS</p><h2>Restaurant snapshot</h2></div><a class="panel-link" routerLink="/app/manage">Manage <fs-icon name="arrow" /></a></div>
            @if (ownerRestaurants.length) { <div class="owner-list">@for (restaurant of ownerRestaurants; track restaurant.id) { <div class="owner-row"><img [src]="restaurant.imageUrl || fallbackImage" [alt]="restaurant.name"><span><strong>{{ restaurant.name }}</strong><small><fs-icon name="pin" /> {{ restaurant.city }}</small></span><span class="status-chip" [ngClass]="restaurant.isActive ? 'status-chip--delivered' : 'status-chip--rejected'">{{ restaurant.isActive ? 'Open' : 'Inactive' }}</span></div> }</div> }
            @else { <div class="empty-state empty-state--compact"><span class="empty-state__icon"><fs-icon name="manage" /></span><strong>Let's open your first kitchen</strong><p>Add your restaurant details, then build a menu for your neighbourhood.</p><a routerLink="/app/manage" class="panel-link">Create restaurant <fs-icon name="arrow" /></a></div> }
          </section>
          <section class="panel chart-panel"><div class="panel__heading"><div><p class="eyebrow">SERVICE AT A GLANCE</p><h2>Order flow</h2></div><span class="live-pill"><i></i> Updates every 30 sec</span></div>
            @if (ownerRestaurants.length) { @for (bar of orderFlowBars; track bar.label) { <div class="flow-row"><span class="flow-row__dot" [ngClass]="'flow-dot--' + bar.tone"></span><span>{{ bar.label }}</span><div class="bar-chart__track"><i [ngClass]="'tone-bg-' + bar.tone" [style.width.%]="bar.width"></i></div><strong>{{ bar.value }}</strong></div> } <p class="chart-footnote">Current order snapshot · {{ ownerOrders.length }} recent orders loaded</p> }
            @else { <div class="empty-state empty-state--compact"><span class="empty-state__icon"><fs-icon name="orders" /></span><strong>Orders will appear here</strong><p>Once your restaurant is open and customers place orders, you can manage each stage from the order desk.</p></div> }
          </section>
        </div>
        <section class="panel table-panel"><div class="panel__heading"><div><p class="eyebrow">NEEDS ATTENTION</p><h2>New orders to prepare</h2></div><a class="panel-link" routerLink="/app/orders">Open order desk <fs-icon name="arrow" /></a></div>
          @if (ownerOrders.length) { @for (order of ownerOrders.slice(0, 5); track order.id) { <a class="recent-order" routerLink="/app/orders"><span class="recent-order__icon"><fs-icon name="bag" /></span><span class="recent-order__main"><strong>{{ order.orderNumber }}</strong><small>{{ order.restaurantName }} · {{ order.placedAtUtc | date:'d MMM, h:mm a' }}</small></span><span class="recent-order__amount">{{ order.totalAmount | currency:'INR':'symbol':'1.0-0' }}</span><span class="status-chip" [ngClass]="'status-chip--' + order.status.toLowerCase()">{{ statusLabel(order.status) }}</span></a> } } @else { <div class="empty-inline"><span>No orders are waiting right now. Your restaurant is ready for its next guest.</span></div> }
        </section>
      }

      @if (role === 'DeliveryPartner') {
        <div class="delivery-banner"><span class="delivery-banner__icon"><fs-icon name="bag" /></span><div><p class="eyebrow eyebrow--light">YOUR NEXT DELIVERY</p><h2>Every good meal has a last mile.</h2><p>Choose a packed order, claim it, and update the hand-off when it reaches your customer.</p></div><a class="button button--gold" routerLink="/app/orders">Open delivery desk <fs-icon name="arrow" /></a></div>
        <section class="panel table-panel"><div class="panel__heading"><div><p class="eyebrow">YOUR ACTIVE DELIVERIES</p><h2>On the way with you</h2></div><span class="live-pill"><i></i> Updates every 30 sec</span></div>
          @if (deliveryOrders.length) { @for (order of deliveryOrders.slice(0, 6); track order.id) { <a class="recent-order" routerLink="/app/orders"><span class="recent-order__icon"><fs-icon name="pin" /></span><span class="recent-order__main"><strong>{{ order.restaurantName }}</strong><small>{{ order.orderNumber }} · {{ order.status === 'PickedUp' ? 'On the way' : 'Ready for pickup' }}</small></span><span class="recent-order__amount">{{ order.placedAtUtc | date:'d MMM' }}</span><span class="status-chip" [ngClass]="'status-chip--' + order.status.toLowerCase()">{{ statusLabel(order.status) }}</span></a> } } @else { <div class="empty-state empty-state--compact"><span class="empty-state__icon"><fs-icon name="check" /></span><strong>A clear route ahead</strong><p>Claim a packed order from the delivery desk and it will appear here.</p></div> }
        </section>
      }
      <p class="refresh-note"><span class="refresh-dot"></span> Overview refreshes automatically every 30 seconds while this page is open.</p>
    </section>
  `
})
export class DashboardPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private poll?: Subscription;
  readonly fallbackImage = 'https://images.unsplash.com/photo-1631515243349-e0cb75fb8d3a?auto=format&fit=crop&w=900&q=82';
  readonly dateLabel = new Intl.DateTimeFormat('en-IN', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date());
  loading = true;
  errorMessage = '';
  metrics: Metric[] = [];
  cityBars: Bar[] = [];
  roleBars: Bar[] = [];
  restaurantSnapshotNote = '';
  pendingApplications: RoleApplication[] = [];
  recentOrders: OrderSummary[] = [];
  featuredRestaurants: Restaurant[] = [];
  ownerRestaurants: Restaurant[] = [];
  ownerOrders: OrderSummary[] = [];
  deliveryOrders: OrderSummary[] = [];
  addressCount = 0;

  get role(): UserRole { return this.auth.role ?? 'Customer'; }
  get orderFlowBars(): Bar[] {
    const stages: { label: string; statuses: OrderStatus[]; tone: string }[] = [
      { label: 'New orders', statuses: ['Placed'], tone: 'plum' },
      { label: 'Accepted', statuses: ['Accepted'], tone: 'blue' },
      { label: 'Preparing', statuses: ['Preparing'], tone: 'gold' },
      { label: 'Ready', statuses: ['Packed'], tone: 'sage' },
      { label: 'Completed', statuses: ['Delivered'], tone: 'rose' }
    ];
    const max = Math.max(1, ...stages.map(stage => this.ownerOrders.filter(order => stage.statuses.includes(order.status)).length));
    return stages.map(stage => {
      const value = this.ownerOrders.filter(order => stage.statuses.includes(order.status)).length;
      return { label: stage.label, value, width: value ? Math.max(5, Math.round(value / max * 100)) : 0, tone: stage.tone };
    });
  }
  get greeting(): string { const hour = new Date().getHours(); return hour < 12 ? 'GOOD MORNING' : hour < 17 ? 'GOOD AFTERNOON' : 'GOOD EVENING'; }
  get welcomeTitle(): string {
    if (this.role === 'Admin') return 'A clearer view of the table.';
    if (this.role === 'RestaurantOwner') return `Welcome to your studio, ${this.auth.session?.firstName ?? 'partner'}.`;
    if (this.role === 'DeliveryPartner') return `Good to see you, ${this.auth.session?.firstName ?? 'partner'}.`;
    return `What sounds lovely today, ${this.auth.session?.firstName ?? 'there'}?`;
  }
  get welcomeSubtitle(): string {
    if (this.role === 'Admin') return 'A live pulse on restaurants, categories and partner applications across FoodSupply.';
    if (this.role === 'RestaurantOwner') return 'Your kitchen, your menu, and the orders waiting for your touch.';
    if (this.role === 'DeliveryPartner') return 'Your delivery queue and active hand-offs, all in one place.';
    return 'A good meal has a way of making an ordinary day feel special.';
  }

  ngOnInit(): void {
    this.poll = interval(30_000).pipe(startWith(0), switchMap(() => {
      this.loading = true;
      this.errorMessage = '';
      return this.loadOverview().pipe(catchError(error => {
        this.errorMessage = getErrorMessage(error, 'The overview could not refresh.');
        this.loading = false;
        return EMPTY;
      }));
    })).subscribe({ error: error => { this.errorMessage = getErrorMessage(error); this.loading = false; } });
  }

  ngOnDestroy(): void { this.poll?.unsubscribe(); }
  refreshNow(): void { this.loading = true; this.errorMessage = ''; this.loadOverview().subscribe({ error: error => { this.errorMessage = getErrorMessage(error); this.loading = false; } }); }

  statusLabel(status: OrderStatus): string { return ({ Placed: 'New', Accepted: 'Accepted', Preparing: 'Preparing', Packed: 'Ready', PickedUp: 'On the way', Delivered: 'Delivered', Rejected: 'Declined', Cancelled: 'Cancelled' } as const)[status]; }
  roleName(role: UserRole): string { return role === 'RestaurantOwner' ? 'Restaurant partner' : role === 'DeliveryPartner' ? 'Delivery partner' : role; }

  private loadOverview(): Observable<unknown> {
    if (this.role === 'Admin') return this.loadAdmin();
    if (this.role === 'RestaurantOwner') return this.loadOwner();
    if (this.role === 'DeliveryPartner') return this.loadDelivery();
    return this.loadCustomer();
  }

  private loadAdmin(): Observable<unknown> {
    return forkJoin({ restaurants: this.api.restaurants(1, 100), categories: this.api.categories(), applications: this.api.pendingApplications() }).pipe(switchMap(data => {
      const pending = data.applications.filter(item => item.status === 'Pending');
      const cities = new Map<string, number>();
      data.restaurants.items.forEach(item => cities.set(item.city, (cities.get(item.city) ?? 0) + 1));
      this.cityBars = this.toBars([...cities.entries()].map(([label, value]) => ({ label, value })), 'plum', 5);
      const roleCounts = new Map<UserRole, number>();
      pending.forEach(item => roleCounts.set(item.requestedRole, (roleCounts.get(item.requestedRole) ?? 0) + 1));
      this.roleBars = this.toBars([...roleCounts.entries()].map(([role, value]) => ({ label: this.roleName(role), value })), 'gold', 3);
      this.pendingApplications = pending.sort((a, b) => Date.parse(b.submittedAtUtc) - Date.parse(a.submittedAtUtc));
      this.restaurantSnapshotNote = data.restaurants.totalCount > data.restaurants.items.length ? `Showing ${data.restaurants.items.length} of ${data.restaurants.totalCount} active restaurants` : `${data.restaurants.totalCount} active restaurants`;
      this.metrics = [
        { label: 'Active restaurants', value: data.restaurants.totalCount, note: 'Open for neighbourhood orders', icon: 'market', tone: 'plum', link: '/app/admin' },
        { label: 'Menu categories', value: data.categories.filter(item => item.isActive).length, note: 'Available across the catalogue', icon: 'bag', tone: 'gold', link: '/app/admin?tab=categories' },
        { label: 'Partner applications', value: pending.length, note: pending.length ? 'Awaiting your review' : 'Queue is all caught up', icon: 'users', tone: pending.length ? 'rose' : 'sage', link: '/app/admin?tab=applications' },
        { label: 'Your role', value: 'Admin', note: 'Full platform access', icon: 'chart', tone: 'blue' }
      ];
      return of(data);
    }), catchError(error => { this.loading = false; throw error; }), switchMap(data => { this.loading = false; return of(data); }));
  }

  private loadCustomer(): Observable<unknown> {
    return forkJoin({ restaurants: this.api.restaurants(1, 100), orders: this.api.myOrders(1, 20), addresses: this.api.addresses() }).pipe(switchMap(data => {
      this.featuredRestaurants = data.restaurants.items;
      this.recentOrders = data.orders.items;
      this.addressCount = data.addresses.length;
      const inProgress = data.orders.items.filter(order => ['Placed', 'Accepted', 'Preparing', 'Packed', 'PickedUp'].includes(order.status)).length;
      this.metrics = [
        { label: 'Restaurants nearby', value: data.restaurants.totalCount, note: 'Explore kitchens in your city', icon: 'market', tone: 'plum', link: '/app/market' },
        { label: 'Orders in motion', value: inProgress, note: 'Follow your latest order', icon: 'orders', tone: 'gold', link: '/app/orders' },
        { label: 'Saved addresses', value: data.addresses.length, note: data.addresses.some(address => address.isDefault) ? 'Default address is ready' : 'Add an address for checkout', icon: 'addresses', tone: 'sage', link: '/app/addresses' },
        { label: 'Your good times', value: data.orders.totalCount, note: 'Orders placed with FoodSupply', icon: 'star', tone: 'rose', link: '/app/orders' }
      ];
      this.loading = false;
      return of(data);
    }));
  }

  private loadOwner(): Observable<unknown> {
    return this.api.myRestaurants().pipe(switchMap(restaurants => {
      this.ownerRestaurants = restaurants;
      if (!restaurants.length) {
        this.ownerOrders = [];
        this.metrics = [
          { label: 'Your restaurants', value: 0, note: 'Create your first kitchen', icon: 'market', tone: 'plum', link: '/app/manage' },
          { label: 'Orders to prepare', value: 0, note: 'They will show up here', icon: 'orders', tone: 'gold', link: '/app/orders' },
          { label: 'Menu items', value: '—', note: 'Add a restaurant first', icon: 'bag', tone: 'sage', link: '/app/manage' }
        ];
        this.loading = false;
        return of(restaurants);
      }
      return forkJoin(restaurants.map(restaurant => this.api.restaurantOrders(restaurant.id, 1, 40))).pipe(switchMap(pages => {
        this.ownerOrders = pages.flatMap(page => page.items).sort((a, b) => Date.parse(b.placedAtUtc) - Date.parse(a.placedAtUtc));
        const openOrders = this.ownerOrders.filter(order => ['Placed', 'Accepted', 'Preparing', 'Packed'].includes(order.status)).length;
        const sales = this.ownerOrders.filter(order => !['Rejected', 'Cancelled'].includes(order.status)).reduce((sum, order) => sum + order.totalAmount, 0);
        this.metrics = [
          { label: 'Your restaurants', value: restaurants.length, note: restaurants.filter(item => item.isActive).length + ' currently active', icon: 'market', tone: 'plum', link: '/app/manage' },
          { label: 'Orders to prepare', value: openOrders, note: 'Open the desk to update status', icon: 'orders', tone: openOrders ? 'gold' : 'sage', link: '/app/orders' },
          { label: 'Recent order value', value: this.currency(sales), note: 'From the latest 40 per restaurant', icon: 'chart', tone: 'blue', link: '/app/orders' },
          { label: 'Partner status', value: restaurants.some(item => item.isActive) ? 'Open' : 'Inactive', note: 'Your storefront availability', icon: 'sparkles', tone: 'rose', link: '/app/manage' }
        ];
        this.loading = false;
        return of(restaurants);
      }));
    }));
  }

  private loadDelivery(): Observable<unknown> {
    return forkJoin({ available: this.api.availableOrders(1, 40), mine: this.api.deliveryOrders(1, 40) }).pipe(switchMap(data => {
      this.deliveryOrders = data.mine.items;
      const active = data.mine.items.filter(order => ['PickedUp', 'Packed'].includes(order.status));
      this.metrics = [
        { label: 'Ready to claim', value: data.available.totalCount, note: 'Packed and awaiting a partner', icon: 'bag', tone: data.available.totalCount ? 'gold' : 'sage', link: '/app/orders' },
        { label: 'Active deliveries', value: active.length, note: 'Claimed by your account', icon: 'pin', tone: active.length ? 'plum' : 'sage', link: '/app/orders' },
        { label: 'Delivered orders', value: data.mine.items.filter(order => order.status === 'Delivered').length, note: 'In your recent delivery list', icon: 'check', tone: 'blue', link: '/app/orders' },
        { label: 'Delivery status', value: 'Ready', note: 'No live GPS tracking is required', icon: 'sparkles', tone: 'rose' }
      ];
      this.loading = false;
      return of(data);
    }));
  }

  private toBars(values: { label: string; value: number }[], tone: string, maxItems: number): Bar[] {
    const top = values.sort((a, b) => b.value - a.value).slice(0, maxItems);
    const max = Math.max(1, ...top.map(item => item.value));
    return top.map((item, index) => ({ ...item, width: Math.max(5, Math.round(item.value / max * 100)), tone: ['plum', 'gold', 'sage', 'blue', 'rose'][index] ?? tone }));
  }

  private currency(value: number): string { return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(value); }
}
