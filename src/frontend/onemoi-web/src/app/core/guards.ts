import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Allows the route only for the given areas: 'admin' | 'vendor' | 'operator' | 'individual'. */
export const areaGuard = (...areas: string[]): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const area = auth.area();
  if (!area) return router.parseUrl('/login');
  return areas.includes(area) ? true : router.parseUrl(auth.profile()!.home);
};

/** Any logged-in user. */
export const loggedInGuard: CanActivateFn = () => (inject(AuthService).isLoggedIn() ? true : inject(Router).parseUrl('/login'));

/**
 * Route needs a permission:  { path: 'operators', data: { permission: 'operators.view' }, canActivate: [permissionGuard] }
 * Without it the user lands on the "no access" page instead of a broken screen.
 */
export const permissionGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const needed = route.data['permission'] as string | undefined;
  if (!needed || auth.can(needed)) return true;
  return inject(Router).parseUrl('/forbidden');
};

/** Login pages: if already logged in, go to the user's home. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isLoggedIn() ? inject(Router).parseUrl(auth.profile()!.home) : true;
};

/**
 * Shows the element only when the user has the permission.
 *   <button *mkCan="P.functionsManage">Edit</button>
 * (This is only for a clean UI — the server enforces every permission anyway.)
 */
@Directive({ selector: '[mkCan]' })
export class CanDirective {
  private auth = inject(AuthService);
  private tpl = inject(TemplateRef<unknown>);
  private vcr = inject(ViewContainerRef);
  mkCan = input.required<string | string[]>();
  private shown = false;

  constructor() {
    effect(() => {
      const need = this.mkCan();
      const ok = Array.isArray(need) ? this.auth.canAny(...need) : this.auth.can(need);
      if (ok && !this.shown) { this.vcr.createEmbeddedView(this.tpl); this.shown = true; }
      if (!ok && this.shown) { this.vcr.clear(); this.shown = false; }
    });
  }
}
