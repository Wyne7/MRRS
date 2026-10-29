import { Component, HostListener, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Subscription, forkJoin } from 'rxjs';
import { BookingService } from '../../core/booking.service';
import { addDays, apiMessage, clock, formatTime, longDate, minutesOf, quarterHours, reservationNote, todayInput } from '../../core/date-util';
import { Booking, Room, equipmentLabels, locationLine } from '../../core/models';
import { RoomService } from '../../core/room.service';
import { SessionStore } from '../../core/session.store';

@Component({
  selector: 'mrrs-schedule',
  templateUrl: './schedule.component.html',
  styleUrl: './schedule.component.css',
  standalone: false
})
export class ScheduleComponent implements OnInit, OnDestroy {
  readonly times = quarterHours();
  readonly hours = Array.from({ length: 11 }, (_, index) => {
    const hour = 8 + index;
    return {
      label: clock(hour, 0).replace(':00', ''),
      top: index === 10 ? 'calc(100% - 0.2rem)' : `${(index / 10) * 100}%`
    };
  });

  date = todayInput();
  rooms: Room[] = [];
  bookings: Booking[] = [];
  loading = true;
  saving = false;
  error: string | null = null;
  notice: string | null = null;
  formError: string | null = null;
  submitted = false;
  mode: 'closed' | 'create' | 'detail' = 'closed';
  selected: Booking | null = null;
  confirming = false;

  private readonly fb = inject(FormBuilder);
  private readonly roomsApi = inject(RoomService);
  private readonly bookingsApi = inject(BookingService);
  private readonly session = inject(SessionStore);

  readonly form = this.fb.group({
    roomId: this.fb.control<number | null>(null, Validators.required),
    date: this.fb.nonNullable.control(this.date, Validators.required),
    startTime: this.fb.nonNullable.control('09:00', Validators.required),
    endTime: this.fb.nonNullable.control('10:00', Validators.required),
    title: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(160)]),
    notes: this.fb.nonNullable.control(''),
    attendeeEmails: this.fb.nonNullable.control('')
  });

  private readonly sub = new Subscription();
  private loadSub?: Subscription;
  private readonly origin = 8 * 60;
  private readonly span = 10 * 60;

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.loadSub?.unsubscribe();
    this.sub.unsubscribe();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeDrawer();
  }

  get heading(): string {
    return longDate(this.date);
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.loadSub?.unsubscribe();
    this.loadSub = forkJoin({
      rooms: this.roomsApi.list(false),
      bookings: this.bookingsApi.list({
        from: `${this.date}T00:00:00`,
        to: `${addDays(this.date, 1)}T00:00:00`
      })
    }).subscribe({
      next: result => {
        this.rooms = result.rooms;
        this.bookings = result.bookings;
        this.loading = false;
      },
      error: (error: unknown) => {
        this.loading = false;
        this.error = apiMessage(error, 'The day sheet could not be loaded.');
      }
    });
  }

  shift(days: number): void {
    this.date = addDays(this.date, days);
    this.closeDrawer();
    this.load();
  }

  goToday(): void {
    this.date = todayInput();
    this.closeDrawer();
    this.load();
  }

  onDate(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    if (!value) {
      return;
    }
    this.date = value;
    this.closeDrawer();
    this.load();
  }

  bookingsFor(room: Room): Booking[] {
    return this.bookings.filter(booking => booking.roomId === room.id);
  }

  onGrid(booking: Booking): boolean {
    const start = minutesOf(booking.start);
    const end = minutesOf(booking.end);
    return end > this.origin && start < this.origin + this.span;
  }

  offsheet(room: Room): Booking[] {
    return this.bookingsFor(room).filter(booking => !this.onGrid(booking));
  }

  blockStyle(booking: Booking): { top: string; height: string } {
    const start = minutesOf(booking.start);
    const end = minutesOf(booking.end);
    const top = Math.min(1, Math.max(0, (start - this.origin) / this.span));
    const bottom = Math.min(1, Math.max(0, (end - this.origin) / this.span));
    const height = Math.max(bottom - top, 0.045);
    return { top: `${top * 100}%`, height: `${height * 100}%` };
  }

  isMine(booking: Booking): boolean {
    return booking.organizerId === this.session.snapshot?.id;
  }

  timeRange(booking: Booking): string {
    return `${formatTime(booking.start)}–${formatTime(booking.end)}`;
  }

  clockLabel(value: string): string {
    return formatTime(value);
  }

  equipment(room: Room): string[] {
    return equipmentLabels(room);
  }

  where(room: Room): string {
    return locationLine(room);
  }

  openCreate(roomId?: number): void {
    this.mode = 'create';
    this.selected = null;
    this.confirming = false;
    this.submitted = false;
    this.formError = null;
    this.form.reset({
      roomId: roomId ?? this.rooms[0]?.id ?? null,
      date: this.date,
      startTime: '09:00',
      endTime: '10:00',
      title: '',
      notes: '',
      attendeeEmails: ''
    });
  }

  openDetail(booking: Booking): void {
    this.mode = 'detail';
    this.selected = booking;
    this.confirming = false;
    this.formError = null;
  }

  closeDrawer(): void {
    this.mode = 'closed';
    this.selected = null;
    this.confirming = false;
    this.formError = null;
  }

  invalid(name: 'roomId' | 'date' | 'startTime' | 'endTime' | 'title'): boolean {
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
    if (value.roomId == null) {
      return;
    }

    this.saving = true;
    this.formError = null;
    this.sub.add(this.bookingsApi.create({
      roomId: value.roomId,
      title: value.title.trim(),
      notes: value.notes.trim() || null,
      attendeeEmails: value.attendeeEmails.trim() || null,
      start: `${value.date}T${value.startTime}:00`,
      end: `${value.date}T${value.endTime}:00`
    }).subscribe({
      next: result => {
        this.saving = false;
        this.notice = reservationNote(result, 'Reserved');
        this.date = value.date;
        this.closeDrawer();
        this.load();
      },
      error: (error: unknown) => {
        this.saving = false;
        this.formError = apiMessage(error, 'That reservation could not be saved.');
      }
    }));
  }

  cancelSelected(): void {
    if (!this.selected || this.saving) {
      return;
    }

    this.saving = true;
    this.formError = null;
    const id = this.selected.id;
    this.sub.add(this.bookingsApi.cancel(id).subscribe({
      next: result => {
        this.saving = false;
        this.notice = reservationNote(result, 'Cancelled');
        this.closeDrawer();
        this.load();
      },
      error: (error: unknown) => {
        this.saving = false;
        this.formError = apiMessage(error, 'That reservation could not be cancelled.');
      }
    }));
  }
}
