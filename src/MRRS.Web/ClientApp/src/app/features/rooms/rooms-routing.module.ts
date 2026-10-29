import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RoomFormComponent } from './room-form.component';
import { RoomListComponent } from './room-list.component';

const routes: Routes = [
  { path: '', component: RoomListComponent },
  { path: 'new', component: RoomFormComponent },
  { path: ':id', component: RoomFormComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class RoomsRoutingModule {}
