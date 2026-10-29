import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, map, of } from 'rxjs';
import { AuthService } from './auth.service';
import { SessionStore } from './session.store';
import { UserProfile } from './models';

@Injectable({ providedIn: 'root' })
export class AdminGuard implements CanActivate {
  constructor(
    private readonly auth: AuthService,
    private readonly session: SessionStore,
    private readonly router: Router
  ) {}

  canActivate(): Observable<boolean | UrlTree> {
    const decide = (user: UserProfile | null): boolean | UrlTree => {
      if (!user) {
        return this.router.createUrlTree(['/login']);
      }
      return user.roles.includes('Admin') ? true : this.router.createUrlTree(['/schedule']);
    };

    if (this.session.snapshot) {
      return of(decide(this.session.snapshot));
    }

    return this.auth.loadCurrentUser().pipe(map(decide));
  }
}
