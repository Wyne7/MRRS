import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { UserProfile } from '../../core/models';
import { SessionStore } from '../../core/session.store';

@Component({
  selector: 'mrrs-shell',
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.css',
  standalone: false
})
export class ShellComponent {
  private readonly session = inject(SessionStore);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly user$ = this.session.user$;

  roleLabel(user: UserProfile): string {
    return user.roles.includes('Admin') ? 'Administrator' : 'Employee';
  }

  isAdmin(user: UserProfile): boolean {
    return user.roles.includes('Admin');
  }

  signOut(): void {
    this.auth.logout().subscribe(() => {
      void this.router.navigate(['/login']);
    });
  }
}
