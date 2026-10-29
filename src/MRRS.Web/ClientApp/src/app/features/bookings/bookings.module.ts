import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { BookingsRoutingModule } from './bookings-routing.module';
import { MyBookingsComponent } from './my-bookings.component';

@NgModule({
  declarations: [MyBookingsComponent],
  imports: [SharedModule, BookingsRoutingModule]
})
export class BookingsModule {}
