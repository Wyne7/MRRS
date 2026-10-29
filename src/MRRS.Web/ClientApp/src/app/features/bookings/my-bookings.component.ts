import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { BookingService } from '../../core/booking.service';
import { addDays, apiMessage, formatTime, longDate, reservationNote, todayInput } from '../../core/date-util';
import { Booking } from '../../core/models';

@Component({
  selector: 'mrrs-my-bookings',
  templateUrl: './my-bookings.component.html',
  styleUrl: './my-bookings.component.css',
  standalone: false
})
export class MyBookingsComponent implements OnInit, OnDestroy {
  bookings: Booking[] = [];
  loading = true;
  error: string | null = null;
  notice: string | null = null;
  pendingId: number | null = null;
  saving = false;
  private readonly sub = new Subscription();

  constructor(private readonly bookingsApi: BookingService) {}

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  get upcoming(): Booking[] {
    const now = Date.now();
    return this.bookings
      .filter(booking => booking.status === 'Confirmed' && this.endMs(booking) > now)
      .sort((a, b) => a.start.localeCompare(b.start));
  }

  get history(): Booking[] {
    const upcomingIds = new Set(this.upcoming.map(booking => booking.id));
    return this.bookings
      .filter(booking => !upcomingIds.has(booking.id))
      .sort((a, b) => b.start.localeCompare(a.start));
  }

  when(booking: Booking): string {
    return `${longDate(booking.start.slice(0, 10))} · ${formatTime(booking.start)}–${formatTime(booking.end)}`;
  }

  ask(booking: Booking): void {
    this.pendingId = booking.id;
    this.error = null;
  }

  dismiss(): void {
    this.pendingId = null;
  }

  cancel(booking: Booking): void {
    this.saving = true;
    this.sub.add(this.bookingsApi.cancel(booking.id).subscribe({
      next: result => {
        this.saving = false;
        this.pendingId = null;
        this.notice = reservationNote(result, 'Cancelled');
        this.load();
      },
      error: (error: unknown) => {
        this.saving = false;
        this.error = apiMessage(error, 'That reservation could not be cancelled.');
      }
    }));
  }

  private load(): void {
    this.loading = true;
    this.error = null;
    const today = todayInput();
    this.sub.add(this.bookingsApi.list({
      from: `${addDays(today, -30)}T00:00:00`,
      to: `${addDays(today, 180)}T00:00:00`,
      mine: true,
      includeCancelled: true
    }).subscribe({
      next: bookings => {
        this.bookings = bookings;
        this.loading = false;
      },
      error: (error: unknown) => {
        this.loading = false;
        this.error = apiMessage(error, 'Your reservations could not be loaded.');
      }
    }));
  }

  private endMs(booking: Booking): number {
    const [date, time] = booking.end.split('T');
    const [year, month, day] = date.split('-').map(Number);
    const [hour, minute] = (time || '00:00').split(':').map(Number);
    return new Date(year || 1970, (month || 1) - 1, day || 1, hour || 0, minute || 0).getTime();
  }
}
