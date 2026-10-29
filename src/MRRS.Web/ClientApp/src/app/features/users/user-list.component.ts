import { Component, OnInit } from '@angular/core';
import { apiMessage } from '../../core/date-util';
import { UserProfile } from '../../core/models';
import { UserService } from '../../core/user.service';

@Component({
  selector: 'mrrs-user-list',
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.css',
  standalone: false
})
export class UserListComponent implements OnInit {
  users: UserProfile[] = [];
  loading = true;
  error: string | null = null;

  constructor(private readonly usersApi: UserService) {}

  ngOnInit(): void {
    this.usersApi.list().subscribe({
      next: users => {
        this.users = users;
        this.loading = false;
      },
      error: (error: unknown) => {
        this.loading = false;
        this.error = apiMessage(error, 'People could not be loaded.');
      }
    });
  }

  role(user: UserProfile): string {
    return user.roles.includes('Admin') ? 'Admin' : 'Employee';
  }
}
