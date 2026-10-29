import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminGuard } from '../../core/admin.guard';
import { ShellComponent } from './shell.component';

const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'schedule' },
      {
        path: 'schedule',
        loadChildren: () => import('../schedule/schedule.module').then(m => m.ScheduleModule)
      },
      {
        path: 'mine',
        loadChildren: () => import('../bookings/bookings.module').then(m => m.BookingsModule)
      },
      {
        path: 'rooms',
        canActivate: [AdminGuard],
        loadChildren: () => import('../rooms/rooms.module').then(m => m.RoomsModule)
      },
      {
        path: 'users',
        canActivate: [AdminGuard],
        loadChildren: () => import('../users/users.module').then(m => m.UsersModule)
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ShellRoutingModule {}
