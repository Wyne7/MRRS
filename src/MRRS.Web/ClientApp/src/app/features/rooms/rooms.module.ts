import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { RoomFormComponent } from './room-form.component';
import { RoomListComponent } from './room-list.component';
import { RoomsRoutingModule } from './rooms-routing.module';

@NgModule({
  declarations: [RoomListComponent, RoomFormComponent],
  imports: [SharedModule, RoomsRoutingModule]
})
export class RoomsModule {}
