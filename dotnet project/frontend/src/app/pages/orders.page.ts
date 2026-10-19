import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EMPTY, Observable, Subscription, catchError, forkJoin, interval, of, startWith, switchMap } from 'rxjs';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { getErrorMessage } from '../core/error-message';
import { Order, OrderStatus, OrderSummary, Restaurant, UserRole } from '../core/models';
import { ToastService } from '../core/toast.service';
import { IconComponent } from '../shared/icon.component';

@Component({
  selector: 'fs-orders-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, IconComponent],
  template: `
    <section class="page orders-page">
      <div class="page-heading"><div><p class="eyebrow">{{ role === 'Customer' ? 'YOUR FOOD JOURNEY' : role === 'DeliveryPartner' ? 'DELIVERY PARTNER DESK' : 'RESTAURANT OPERATIONS' }}</p><h1>{{ title }}</h1><p class="page-heading__sub">{{ subtitle }}</p></div><button class="button button--outline" (click)="refresh()" [disabled]="loading"><fs-icon name="sessions" /> {{ loading ? 'Refreshing…' : 'Refresh' }}</button></div>

      @if (role !== 'Customer' && role !== 'DeliveryPartner' && restaurants.length > 1) { <label class="select-filter"><span>Kitchen</span><select [value]="selectedRestaurantId" (change)="selectRestaurant($event)">@for (restaurant of restaurants; track restaurant.id) { <option [value]="restaurant.id">{{ restaurant.name }} · {{ restaurant.city }}</option> }</select><fs-icon name="chevrondown" /></label> }
      @if (errorMessage) { <div class="inline-alert" role="alert"><fs-icon name="close" /><span>{{ errorMessage }}</span><button class="text-button" (click)="refresh()">Try again</button></div> }
      @if (role === 'DeliveryPartner') { <div class="delivery-tabs"><button [class.delivery-tabs__item--active]="deliveryTab === 'available'" (click)="deliveryTab = 'available'">Ready to claim <span>{{ availableOrders.length }}</span></button><button [class.delivery-tabs__item--active]="deliveryTab === 'mine'" (click)="deliveryTab = 'mine'">My deliveries <span>{{ deliveryOrders.length }}</span></button></div> }
      @if (role === 'Customer') { <div class="filter-row"><span>Show</span><button [class.filter-chip--active]="filter === 'all'" (click)="filter = 'all'">Everything</button><button [class.filter-chip--active]="filter === 'active'" (click)="filter = 'active'">In progress</button><button [class.filter-chip--active]="filter === 'complete'" (click)="filter = 'complete'">Past orders</button></div> }

      <div class="orders-summary-strip"><span><strong>{{ visibleOrders.length }}</strong> {{ visibleOrders.length === 1 ? 'order shown' : 'orders shown' }}</span><span><i class="refresh-dot"></i> {{ role === 'Customer' ? 'Your orders are private to your account' : 'Order status refreshes every 30 seconds' }}</span></div>

      @if (loading && !visibleOrders.length) { <div class="order-card-grid">@for (i of [1,2,3]; track i) { <div class="order-skeleton"></div> }</div> }
      @else if (visibleOrders.length) {
        <div class="order-card-grid">
          @for (order of visibleOrders; track order.id) {
            <article class="order-card">
              <div class="order-card__top"><span class="order-card__restaurant-icon"><fs-icon [name]="role === 'DeliveryPartner' ? 'pin' : 'bag'" /></span><span class="order-card__restaurant"><strong>{{ role === 'Customer' ? order.restaurantName : order.restaurantName }}</strong><small>{{ order.placedAtUtc | date:'EEE, d MMM · h:mm a' }}</small></span><span class="status-chip" [ngClass]="'status-chip--' + order.status.toLowerCase()">{{ statusLabel(order.status) }}</span></div>
              <div class="order-card__number"><span>ORDER</span><strong>{{ order.orderNumber }}</strong><span class="order-card__amount">{{ order.totalAmount | currency:'INR':'symbol':'1.0-0' }}</span></div>
              <div class="order-card__progress" [class.order-card__progress--terminal]="isTerminal(order.status)"><span class="progress-step" [class.progress-step--done]="progress(order.status) >= 1"><i></i><small>Placed</small></span><span class="progress-line" [class.progress-line--done]="progress(order.status) >= 2"></span><span class="progress-step" [class.progress-step--done]="progress(order.status) >= 2"><i></i><small>Preparing</small></span><span class="progress-line" [class.progress-line--done]="progress(order.status) >= 3"></span><span class="progress-step" [class.progress-step--done]="progress(order.status) >= 3"><i></i><small>Ready</small></span><span class="progress-line" [class.progress-line--done]="progress(order.status) >= 4"></span><span class="progress-step" [class.progress-step--done]="progress(order.status) >= 4"><i></i><small>Complete</small></span></div>
              <div class="order-card__actions">@if (!(role === 'DeliveryPartner' && deliveryTab === 'available')) { <button class="button button--outline button--small" (click)="openDetails(order)">Order details <fs-icon name="chevron" /></button> }
                @if (role === 'Customer' && order.status === 'Placed') { <button class="button button--quiet-danger button--small" (click)="askCancel(order)">Cancel order</button> }
                @if (role === 'DeliveryPartner' && deliveryTab === 'available') { <button class="button button--primary button--small" (click)="claim(order)" [disabled]="busyId === order.id">{{ busyId === order.id ? 'Claiming…' : 'Claim delivery' }} <fs-icon name="arrow" /></button> }
                @if (role === 'DeliveryPartner' && deliveryTab === 'mine' && order.status === 'Packed') { <button class="button button--primary button--small" (click)="setStatus(order, 'PickedUp')" [disabled]="busyId === order.id">Mark picked up <fs-icon name="arrow" /></button> }
                @if (role === 'DeliveryPartner' && deliveryTab === 'mine' && order.status === 'PickedUp') { <button class="button button--primary button--small" (click)="setStatus(order, 'Delivered')" [disabled]="busyId === order.id">Mark delivered <fs-icon name="check" /></button> }
                @if ((role === 'RestaurantOwner' || role === 'Admin') && order.status === 'Placed') { <button class="button button--quiet-danger button--small" (click)="setStatus(order, 'Rejected')" [disabled]="busyId === order.id">Decline</button><button class="button button--primary button--small" (click)="setStatus(order, 'Accepted')" [disabled]="busyId === order.id">Accept order <fs-icon name="arrow" /></button> }
                @if ((role === 'RestaurantOwner' || role === 'Admin') && order.status === 'Accepted') { <button class="button button--primary button--small" (click)="setStatus(order, 'Preparing')" [disabled]="busyId === order.id">Start preparing <fs-icon name="arrow" /></button> }
                @if ((role === 'RestaurantOwner' || role === 'Admin') && order.status === 'Preparing') { <button class="button button--primary button--small" (click)="setStatus(order, 'Packed')" [disabled]="busyId === order.id">Mark packed <fs-icon name="check" /></button> }
              </div>
            </article>
          }
        </div>
      } @else {
        <div class="empty-state"><span class="empty-state__icon"><fs-icon [name]="role === 'Customer' ? 'bag' : role === 'DeliveryPartner' ? 'pin' : 'orders'" /></span><strong>{{ emptyTitle }}</strong><p>{{ emptySubtitle }}</p>@if (role === 'Customer') { <a class="button button--primary" routerLink="/app/market">Find your next meal <fs-icon name="arrow" /></a> } @if (role === 'RestaurantOwner') { <a class="button button--outline" routerLink="/app/manage">Set up your restaurant</a> }</div>
      }
      <div class="rabbitmq-note"><span class="rabbitmq-note__icon"><fs-icon name="sparkles" /></span><span><strong>Every order change travels with its history.</strong> Status updates are saved and sent to the backend order event queue.</span></div>
    </section>

    @if (selectedOrder) { <div class="modal-backdrop" (click)="selectedOrder = null"><section class="detail-modal" role="dialog" aria-modal="true" aria-labelledby="order-detail-title" (click)="$event.stopPropagation()"><div class="modal-heading"><div><p class="eyebrow">{{ selectedOrder.orderNumber }}</p><h2 id="order-detail-title">{{ selectedOrder.restaurantName }}</h2><p>{{ selectedOrder.placedAtUtc | date:'EEEE, d MMMM yyyy · h:mm a' }}</p></div><button class="modal-close" (click)="selectedOrder = null" aria-label="Close order details"><fs-icon name="close" /></button></div>
      @if (detailsLoading) { <div class="detail-loading"><span class="spinner spinner--dark"></span> Loading your order…</div> }
      @else { @if (detailError) { <div class="inline-alert"><span>{{ detailError }}</span></div> } @else {
        <div class="detail-status"><span class="status-chip" [ngClass]="'status-chip--' + selectedOrder.status.toLowerCase()">{{ statusLabel(selectedOrder.status) }}</span><span>{{ selectedOrder.totalAmount | currency:selectedOrder.currencyCode:'symbol':'1.2-2' }}</span></div>
        <div class="detail-items">@for (item of selectedOrder.items; track item.menuItemId) { <div class="detail-item"><span><strong>{{ item.quantity }} × {{ item.itemName }}</strong>@if (item.specialInstructions) { <small>{{ item.specialInstructions }}</small> }</span><strong>{{ item.lineTotal | currency:selectedOrder.currencyCode:'symbol':'1.2-2' }}</strong></div> }</div>
        <div class="detail-totals"><div><span>Subtotal</span><span>{{ selectedOrder.subtotal | currency:selectedOrder.currencyCode:'symbol':'1.2-2' }}</span></div><div><span>Delivery</span><span>{{ selectedOrder.deliveryFee | currency:selectedOrder.currencyCode:'symbol':'1.2-2' }}</span></div><div><span>Tax</span><span>{{ selectedOrder.taxAmount | currency:selectedOrder.currencyCode:'symbol':'1.2-2' }}</span></div><div class="detail-totals__grand"><strong>Total</strong><strong>{{ selectedOrder.totalAmount | currency:selectedOrder.currencyCode:'symbol':'1.2-2' }}</strong></div></div>
        <div class="detail-address"><p class="eyebrow">DELIVERY TO</p><strong>{{ selectedOrder.recipientName }} · {{ selectedOrder.recipientPhoneNumber }}</strong><span>{{ selectedOrder.deliveryAddressLine1 }}{{ selectedOrder.deliveryAddressLine2 ? ', ' + selectedOrder.deliveryAddressLine2 : '' }}, {{ selectedOrder.deliveryCity }}, {{ selectedOrder.deliveryState }} {{ selectedOrder.deliveryPostalCode }}, {{ selectedOrder.deliveryCountry }}</span></div>
        @if (selectedOrder.statusHistory.length) { <div class="status-history"><p class="eyebrow">ORDER HISTORY</p>@for (entry of selectedOrder.statusHistory; track entry.changedAtUtc) { <div class="history-row"><i></i><span><strong>{{ statusLabel(entry.status) }}</strong><small>{{ entry.changedAtUtc | date:'d MMM, h:mm a' }}{{ entry.note ? ' · ' + entry.note : '' }}</small></span></div> }</div> }
      } }
    </section></div> }

    @if (cancelTarget) { <div class="modal-backdrop" (click)="cancelTarget = null"><section class="confirm-modal" role="dialog" aria-modal="true" aria-labelledby="cancel-title" (click)="$event.stopPropagation()"><span class="confirm-modal__icon"><fs-icon name="close" /></span><h2 id="cancel-title">Cancel this order?</h2><p>{{ cancelTarget.orderNumber }} has only been placed. Cancelling it will stop it from going to the kitchen.</p><div class="confirm-modal__actions"><button class="button button--outline" (click)="cancelTarget = null">Keep order</button><button class="button button--danger" (click)="confirmCancel()" [disabled]="busyId === cancelTarget.id">{{ busyId === cancelTarget.id ? 'Cancelling…' : 'Yes, cancel order' }}</button></div></section></div> }
  `
})
export class OrdersPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private poll?: Subscription;
  role: UserRole = this.auth.role ?? 'Customer';
  restaurants: Restaurant[] = [];
  selectedRestaurantId = 0;
  allOrders: OrderSummary[] = [];
  availableOrders: OrderSummary[] = [];
  deliveryOrders: OrderSummary[] = [];
  selectedOrder: Order | null = null;
  cancelTarget: OrderSummary | null = null;
  loading = true;
  detailsLoading = false;
  busyId: number | null = null;
  errorMessage = '';
  detailError = '';
  deliveryTab: 'available' | 'mine' = 'available';
  filter: 'all' | 'active' | 'complete' = 'all';

  get title(): string { return this.role === 'Customer' ? 'Your orders, all in one place.' : this.role === 'DeliveryPartner' ? 'The delivery desk.' : 'The order desk.'; }
  get subtitle(): string { return this.role === 'Customer' ? 'Follow a meal from the kitchen to your door, and find the details of every order.' : this.role === 'DeliveryPartner' ? 'Claim a packed meal, keep its status current, and complete the hand-off.' : 'Keep each kitchen moving with clear, deliberate order updates.'; }
  get visibleOrders(): OrderSummary[] {
    if (this.role === 'DeliveryPartner') return this.deliveryTab === 'available' ? this.availableOrders : this.deliveryOrders;
    if (this.role !== 'Customer') return this.allOrders;
    return this.allOrders.filter(order => this.filter === 'all' || (this.filter === 'active'
      ? ['Placed', 'Accepted', 'Preparing', 'Packed', 'PickedUp'].includes(order.status)
      : ['Delivered', 'Rejected', 'Cancelled'].includes(order.status)));
  }
  get emptyTitle(): string {
    if (this.role === 'DeliveryPartner') return this.deliveryTab === 'available' ? 'No packed orders are waiting' : 'Nothing on your route yet';
    if (this.role === 'RestaurantOwner' && !this.restaurants.length) return 'Set up your first kitchen';
    return this.filter === 'complete' ? 'No past orders yet' : this.filter === 'active' ? 'No orders in progress' : 'No orders to show yet';
  }
  get emptySubtitle(): string {
    if (this.role === 'DeliveryPartner') return this.deliveryTab === 'available' ? 'When a restaurant packs an order, it will appear in this list.' : 'Claim a ready order and your delivery work will appear here.';
    if (this.role === 'RestaurantOwner' && !this.restaurants.length) return 'Create your restaurant and add a menu before customers can start ordering.';
    return this.role === 'Customer' ? 'Your next favourite meal is only a few steps away.' : 'New orders will show up here as soon as customers place them.';
  }

  ngOnInit(): void {
    this.poll = interval(30_000).pipe(startWith(0), switchMap(() => this.loadOrders().pipe(catchError(error => {
      this.errorMessage = getErrorMessage(error, 'Orders could not be refreshed.');
      this.loading = false;
      return EMPTY;
    })))).subscribe();
  }
  ngOnDestroy(): void { this.poll?.unsubscribe(); }

  refresh(): void { this.loading = true; this.errorMessage = ''; this.loadOrders().subscribe({ error: error => { this.errorMessage = getErrorMessage(error); this.loading = false; } }); }
  selectRestaurant(event: Event): void { this.selectedRestaurantId = Number((event.target as HTMLSelectElement).value); this.refresh(); }

  statusLabel(status: OrderStatus): string { return ({ Placed: 'New order', Accepted: 'Accepted', Preparing: 'Preparing', Packed: 'Ready for pickup', PickedUp: 'On the way', Delivered: 'Delivered', Rejected: 'Declined', Cancelled: 'Cancelled' } as const)[status]; }
  isTerminal(status: OrderStatus): boolean { return ['Delivered', 'Rejected', 'Cancelled'].includes(status); }
  progress(status: OrderStatus): number {
    if (status === 'Rejected' || status === 'Cancelled') return 1;
    return ({ Placed: 1, Accepted: 2, Preparing: 2, Packed: 3, PickedUp: 3, Delivered: 4 } as const)[status];
  }

  openDetails(summary: OrderSummary): void {
    this.detailsLoading = true;
    this.detailError = '';
    this.selectedOrder = { ...summary, subtotal: 0, deliveryFee: 0, taxAmount: 0, currencyCode: 'INR', recipientName: '', recipientPhoneNumber: '', deliveryAddressLine1: '', deliveryCity: '', deliveryState: '', deliveryPostalCode: '', deliveryCountry: '', placedAtUtc: summary.placedAtUtc, updatedAtUtc: summary.placedAtUtc, items: [], payments: [], statusHistory: [] };
    const details = this.role === 'Customer' ? this.api.order(summary.id) : this.api.operationalOrder(summary.id);
    details.subscribe({ next: order => { this.selectedOrder = order; this.detailsLoading = false; }, error: error => { this.detailError = getErrorMessage(error, 'Order details could not be loaded.'); this.detailsLoading = false; } });
  }

  claim(order: OrderSummary): void {
    this.busyId = order.id;
    this.api.claimOrder(order.id).subscribe({ next: () => { this.busyId = null; this.toast.show(`${order.orderNumber} is now assigned to you.`, 'success'); this.refresh(); }, error: error => { this.busyId = null; this.toast.show(getErrorMessage(error, 'This order could not be claimed.'), 'error'); } });
  }

  setStatus(order: OrderSummary, status: OrderStatus): void {
    this.busyId = order.id;
    this.api.changeOrderStatus(order.id, status, `Updated by ${this.role === 'DeliveryPartner' ? 'delivery partner' : 'restaurant'}.`).subscribe({
      next: () => { this.busyId = null; this.toast.show(`${order.orderNumber} marked ${this.statusLabel(status).toLowerCase()}.`, 'success'); this.refresh(); },
      error: error => { this.busyId = null; this.toast.show(getErrorMessage(error, 'The order status could not be changed.'), 'error'); }
    });
  }

  askCancel(order: OrderSummary): void { this.cancelTarget = order; }
  confirmCancel(): void {
    const target = this.cancelTarget;
    if (!target) return;
    this.busyId = target.id;
    this.api.cancelOrder(target.id).subscribe({ next: () => { this.cancelTarget = null; this.busyId = null; this.toast.show(`${target.orderNumber} was cancelled.`, 'success'); this.refresh(); }, error: error => { this.busyId = null; this.toast.show(getErrorMessage(error, 'The order could not be cancelled.'), 'error'); } });
  }

  private loadOrders(): Observable<unknown> {
    this.errorMessage = '';
    this.loading = true;
    if (this.role === 'Customer') return this.api.myOrders(1, 50).pipe(switchMap(page => { this.allOrders = page.items; this.loading = false; return of(page); }));
    if (this.role === 'DeliveryPartner') return forkJoin({ available: this.api.availableOrders(1, 50), mine: this.api.deliveryOrders(1, 50) }).pipe(switchMap(data => { this.availableOrders = data.available.items; this.deliveryOrders = data.mine.items; this.loading = false; return of(data); }));
    const restaurants$ = this.role === 'Admin' ? this.api.restaurants(1, 100).pipe(switchMap(page => of(page.items))) : this.api.myRestaurants();
    return restaurants$.pipe(switchMap(restaurants => {
      this.restaurants = restaurants;
      if (!restaurants.length) { this.allOrders = []; this.loading = false; return of([]); }
      if (!this.selectedRestaurantId || !restaurants.some(item => item.id === this.selectedRestaurantId)) this.selectedRestaurantId = restaurants[0].id;
      const requestedRestaurants = restaurants.length === 1 ? restaurants : restaurants.filter(item => item.id === this.selectedRestaurantId);
      return forkJoin(requestedRestaurants.map(restaurant => this.api.restaurantOrders(restaurant.id, 1, 50))).pipe(switchMap(pages => { this.allOrders = pages.flatMap(page => page.items); this.loading = false; return of(pages); }));
    }));
  }
}
