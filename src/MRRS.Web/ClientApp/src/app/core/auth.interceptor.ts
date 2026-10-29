import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, throwError } from 'rxjs';
import { SessionStore } from './session.store';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private readonly session: SessionStore, private readonly router: Router) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const authed = req.clone({ withCredentials: true });
    return next.handle(authed).pipe(
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401 && !req.url.includes('/api/account/login')) {
          this.session.set(null);
          void this.router.navigate(['/login']);
        }
        return throwError(() => error);
      })
    );
  }
}
