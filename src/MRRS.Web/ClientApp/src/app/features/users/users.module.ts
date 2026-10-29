import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { UserFormComponent } from './user-form.component';
import { UserListComponent } from './user-list.component';
import { UsersRoutingModule } from './users-routing.module';

@NgModule({
  declarations: [UserListComponent, UserFormComponent],
  imports: [SharedModule, UsersRoutingModule]
})
export class UsersModule {}
