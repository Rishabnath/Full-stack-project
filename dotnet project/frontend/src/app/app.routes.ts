import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { AuthPageComponent } from './pages/auth.page';
import { ShellComponent } from './shell/shell.component';
import { DashboardPageComponent } from './pages/dashboard.page';
import { MarketPageComponent } from './pages/market.page';
import { OrdersPageComponent } from './pages/orders.page';
import { ManagePageComponent } from './pages/manage.page';
import { AddressesPageComponent } from './pages/addresses.page';
import { ApplicationsPageComponent } from './pages/applications.page';
import { SessionsPageComponent } from './pages/sessions.page';
import { UserRole } from './core/models';

const customer: UserRole[] = ['Customer'];
const restaurantRoles: UserRole[] = ['RestaurantOwner', 'Admin'];
const admin: UserRole[] = ['Admin'];

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: AuthPageComponent, data: { mode: 'login' } },
  { path: 'register', component: AuthPageComponent, data: { mode: 'register' } },
  {
    path: 'app', component: ShellComponent, canActivate: [authGuard], children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: DashboardPageComponent },
      { path: 'market', component: MarketPageComponent, canActivate: [authGuard], data: { roles: customer } },
      { path: 'orders', component: OrdersPageComponent },
      { path: 'manage', component: ManagePageComponent, canActivate: [authGuard], data: { roles: restaurantRoles } },
      { path: 'admin', component: ManagePageComponent, canActivate: [authGuard], data: { roles: admin } },
      { path: 'addresses', component: AddressesPageComponent, canActivate: [authGuard], data: { roles: customer } },
      { path: 'applications', component: ApplicationsPageComponent, canActivate: [authGuard], data: { roles: customer } },
      { path: 'sessions', component: SessionsPageComponent }
    ]
  },
  { path: '**', redirectTo: 'app/dashboard' }
];
