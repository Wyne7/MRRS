import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of, tap, catchError } from 'rxjs';
import { UserProfile } from './models';
import { SessionStore } from './session.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  constructor(private readonly http: HttpClient, private readonly session: SessionStore) {}

  loadCurrentUser(): Observable<UserProfile | null> {
    if (this.session.snapshot) {
      return of(this.session.snapshot);
    }

    return this.http.get<UserProfile>('/api/account/me').pipe(
      tap(user => this.session.set(user)),
      catchError(() => {
        this.session.set(null);
        return of(null);
      })
    );
  }

  login(userName: string, password: string): Observable<UserProfile> {
    return this.http.post<UserProfile>('/api/account/login', { userName, password }).pipe(
      tap(user => this.session.set(user))
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>('/api/account/logout', {}).pipe(
      tap(() => this.session.set(null)),
      catchError(() => {
        this.session.set(null);
        return of(undefined);
      })
    );
  }
}
