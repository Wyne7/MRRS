import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { apiMessage } from '../../core/date-util';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'mrrs-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
  standalone: false
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  error: string | null = null;
  saving = false;

  readonly form = this.fb.nonNullable.group({
    userName: ['', Validators.required],
    password: ['', Validators.required]
  });

  ngOnInit(): void {
    this.auth.loadCurrentUser().subscribe(user => {
      if (user) {
        void this.router.navigate(['/schedule']);
      }
    });
  }

  submit(): void {
    if (this.form.invalid || this.saving) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.error = null;
    const { userName, password } = this.form.getRawValue();
    this.auth.login(userName.trim(), password).subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigate(['/schedule']);
      },
      error: (error: unknown) => {
        this.saving = false;
        this.error = apiMessage(error, 'Those credentials were not recognized.');
      }
    });
  }
}
