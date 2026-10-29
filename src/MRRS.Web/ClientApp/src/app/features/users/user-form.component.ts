import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { apiMessage } from '../../core/date-util';
import { UserWrite } from '../../core/models';
import { UserService } from '../../core/user.service';

@Component({
  selector: 'mrrs-user-form',
  templateUrl: './user-form.component.html',
  styleUrl: './user-form.component.css',
  standalone: false
})
export class UserFormComponent implements OnInit {
  userId: string | null = null;
  userName = '';
  loading = false;
  saving = false;
  error: string | null = null;
  submitted = false;

  private readonly fb = inject(FormBuilder);
  private readonly usersApi = inject(UserService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    userName: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9._-]{3,40}$/)]],
    email: ['', [Validators.required, Validators.email]],
    displayName: ['', [Validators.required, Validators.maxLength(120)]],
    department: [''],
    password: [''],
    role: this.fb.nonNullable.control<'Admin' | 'Employee'>('Employee', Validators.required),
    isActive: [true]
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id || id === 'new') {
      this.form.controls.password.addValidators([Validators.required, Validators.minLength(8)]);
      this.form.controls.password.updateValueAndValidity();
      return;
    }

    this.userId = id;
    this.form.controls.userName.disable();
    this.loading = true;
    this.usersApi.get(id).subscribe({
      next: user => {
        this.userName = user.userName;
        this.form.patchValue({
          userName: user.userName,
          email: user.email,
          displayName: user.displayName,
          department: user.department ?? '',
          role: user.roles.includes('Admin') ? 'Admin' : 'Employee',
          isActive: user.isActive
        });
        this.loading = false;
      },
      error: (error: unknown) => {
        this.loading = false;
        this.error = apiMessage(error, 'That account could not be loaded.');
      }
    });
  }

  get heading(): string {
    return this.userId ? 'Edit person' : 'New person';
  }

  invalid(name: 'userName' | 'email' | 'displayName' | 'password'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || this.submitted);
  }

  submit(): void {
    this.submitted = true;
    if (this.form.invalid || this.saving) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const body: UserWrite = {
      userName: value.userName.trim(),
      email: value.email.trim(),
      displayName: value.displayName.trim(),
      department: value.department.trim() || null,
      password: value.password.trim() || null,
      role: value.role,
      isActive: value.isActive
    };

    this.saving = true;
    this.error = null;
    const request = this.userId ? this.usersApi.update(this.userId, body) : this.usersApi.create(body);
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigate(['/users']);
      },
      error: (error: unknown) => {
        this.saving = false;
        this.error = apiMessage(error, 'That account could not be saved.');
      }
    });
  }
}
