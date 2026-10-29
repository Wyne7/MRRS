import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { UserProfile } from './models';

@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly userSubject = new BehaviorSubject<UserProfile | null>(null);
  readonly user$ = this.userSubject.asObservable();

  get snapshot(): UserProfile | null {
    return this.userSubject.value;
  }

  set(user: UserProfile | null): void {
    this.userSubject.next(user);
  }
}
