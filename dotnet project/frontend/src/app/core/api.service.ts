import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from './environment';
import { Address, Category, MenuItem, Order, OrderStatus, OrderSummary, Page, Restaurant, RoleApplication, UserSession } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;
  private url(path: string): string { return `${this.base}${path}`; }

  restaurants(page = 1, pageSize = 100, city?: string): Observable<Page<Restaurant>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (city) params = params.set('city', city);
    return this.http.get<Page<Restaurant>>(this.url('/restaurants'), { params });
  }
  restaurant(id: number): Observable<Restaurant> { return this.http.get<Restaurant>(this.url(`/restaurants/${id}`)); }
  myRestaurants(): Observable<Restaurant[]> { return this.http.get<Restaurant[]>(this.url('/restaurants/mine')); }
  createRestaurant(body: object): Observable<Restaurant> { return this.http.post<Restaurant>(this.url('/restaurants'), body); }
  updateRestaurant(id: number, body: object): Observable<Restaurant> { return this.http.put<Restaurant>(this.url(`/restaurants/${id}`), body); }
  deactivateRestaurant(id: number): Observable<void> { return this.http.delete<void>(this.url(`/restaurants/${id}`)); }

  categories(): Observable<Category[]> { return this.http.get<Category[]>(this.url('/categories')); }
  createCategory(body: object): Observable<Category> { return this.http.post<Category>(this.url('/categories'), body); }
  updateCategory(id: number, body: object): Observable<Category> { return this.http.put<Category>(this.url(`/categories/${id}`), body); }
  deactivateCategory(id: number): Observable<void> { return this.http.delete<void>(this.url(`/categories/${id}`)); }

  menu(restaurantId: number): Observable<MenuItem[]> { return this.http.get<MenuItem[]>(this.url(`/restaurants/${restaurantId}/menu-items`)); }
  managedMenu(restaurantId: number): Observable<MenuItem[]> { return this.http.get<MenuItem[]>(this.url(`/restaurants/${restaurantId}/menu-items/manage`)); }
  createMenuItem(restaurantId: number, body: object): Observable<MenuItem> { return this.http.post<MenuItem>(this.url(`/restaurants/${restaurantId}/menu-items`), body); }
  updateMenuItem(restaurantId: number, id: number, body: object): Observable<MenuItem> { return this.http.put<MenuItem>(this.url(`/restaurants/${restaurantId}/menu-items/${id}`), body); }
  deactivateMenuItem(restaurantId: number, id: number): Observable<void> { return this.http.delete<void>(this.url(`/restaurants/${restaurantId}/menu-items/${id}`)); }

  addresses(): Observable<Address[]> { return this.http.get<Address[]>(this.url('/addresses')); }
  createAddress(body: object): Observable<Address> { return this.http.post<Address>(this.url('/addresses'), body); }
  updateAddress(id: number, body: object): Observable<Address> { return this.http.put<Address>(this.url(`/addresses/${id}`), body); }
  setDefaultAddress(id: number): Observable<void> { return this.http.put<void>(this.url(`/addresses/${id}/default`), {}); }
  deleteAddress(id: number): Observable<void> { return this.http.delete<void>(this.url(`/addresses/${id}`)); }

  myOrders(page = 1, pageSize = 50): Observable<Page<OrderSummary>> {
    return this.http.get<Page<OrderSummary>>(this.url('/orders/mine'), { params: { page, pageSize } });
  }
  restaurantOrders(restaurantId: number, page = 1, pageSize = 50, status?: OrderStatus): Observable<Page<OrderSummary>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<Page<OrderSummary>>(this.url(`/orders/restaurant/${restaurantId}`), { params });
  }
  availableOrders(page = 1, pageSize = 50): Observable<Page<OrderSummary>> { return this.http.get<Page<OrderSummary>>(this.url('/orders/delivery/available'), { params: { page, pageSize } }); }
  deliveryOrders(page = 1, pageSize = 50): Observable<Page<OrderSummary>> { return this.http.get<Page<OrderSummary>>(this.url('/orders/delivery/mine'), { params: { page, pageSize } }); }
  order(id: number): Observable<Order> { return this.http.get<Order>(this.url(`/orders/${id}`)); }
  operationalOrder(id: number): Observable<Order> { return this.http.get<Order>(this.url(`/orders/${id}/operations`)); }
  createOrder(body: object): Observable<Order> { return this.http.post<Order>(this.url('/orders'), body); }
  cancelOrder(id: number): Observable<void> { return this.http.post<void>(this.url(`/orders/${id}/cancel`), {}); }
  claimOrder(id: number): Observable<void> { return this.http.post<void>(this.url(`/orders/${id}/claim`), {}); }
  changeOrderStatus(id: number, status: OrderStatus, note?: string): Observable<void> { return this.http.patch<void>(this.url(`/orders/${id}/status`), { status, note }); }

  roleApplications(): Observable<RoleApplication[]> { return this.http.get<RoleApplication[]>(this.url('/role-applications/mine')); }
  pendingApplications(): Observable<RoleApplication[]> { return this.http.get<RoleApplication[]>(this.url('/role-applications/pending')); }
  applyForRole(body: object): Observable<RoleApplication> { return this.http.post<RoleApplication>(this.url('/role-applications'), body); }
  reviewApplication(id: string, approve: boolean, reviewNote: string): Observable<RoleApplication> {
    return this.http.post<RoleApplication>(this.url(`/role-applications/${id}/${approve ? 'approve' : 'reject'}`), { reviewNote });
  }

  sessions(): Observable<UserSession[]> { return this.http.get<UserSession[]>(this.url('/auth/sessions')); }
  revokeSession(id: string): Observable<void> { return this.http.delete<void>(this.url(`/auth/sessions/${id}`)); }
}
