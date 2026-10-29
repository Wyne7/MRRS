import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { apiMessage } from '../../core/date-util';
import { RoomWrite } from '../../core/models';
import { RoomService } from '../../core/room.service';

@Component({
  selector: 'mrrs-room-form',
  templateUrl: './room-form.component.html',
  styleUrl: './room-form.component.css',
  standalone: false
})
export class RoomFormComponent implements OnInit {
  roomId: number | null = null;
  loading = false;
  saving = false;
  error: string | null = null;
  submitted = false;

  private readonly fb = inject(FormBuilder);
  private readonly roomsApi = inject(RoomService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    building: ['', [Validators.required, Validators.maxLength(80)]],
    floor: ['', [Validators.required, Validators.maxLength(40)]],
    locationDetail: [''],
    capacity: [8, [Validators.required, Validators.min(1), Validators.max(500)]],
    hasProjector: [false],
    hasVideoConference: [false],
    hasWhiteboard: [false],
    hasConferencePhone: [false],
    avNotes: [''],
    isActive: [true]
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id || id === 'new') {
      return;
    }

    this.roomId = Number(id);
    this.loading = true;
    this.roomsApi.get(this.roomId).subscribe({
      next: room => {
        this.form.setValue({
          name: room.name,
          building: room.building,
          floor: room.floor,
          locationDetail: room.locationDetail ?? '',
          capacity: room.capacity,
          hasProjector: room.hasProjector,
          hasVideoConference: room.hasVideoConference,
          hasWhiteboard: room.hasWhiteboard,
          hasConferencePhone: room.hasConferencePhone,
          avNotes: room.avNotes ?? '',
          isActive: room.isActive
        });
        this.loading = false;
      },
      error: (error: unknown) => {
        this.loading = false;
        this.error = apiMessage(error, 'That room could not be loaded.');
      }
    });
  }

  get heading(): string {
    return this.roomId ? 'Edit room' : 'New room';
  }

  invalid(name: 'name' | 'building' | 'floor' | 'capacity'): boolean {
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
    const body: RoomWrite = {
      name: value.name.trim(),
      building: value.building.trim(),
      floor: value.floor.trim(),
      locationDetail: value.locationDetail.trim() || null,
      capacity: Number(value.capacity),
      hasProjector: value.hasProjector,
      hasVideoConference: value.hasVideoConference,
      hasWhiteboard: value.hasWhiteboard,
      hasConferencePhone: value.hasConferencePhone,
      avNotes: value.avNotes.trim() || null,
      isActive: value.isActive
    };

    this.saving = true;
    this.error = null;
    const request = this.roomId ? this.roomsApi.update(this.roomId, body) : this.roomsApi.create(body);
    request.subscribe({
      next: () => {
        this.saving = false;
        void this.router.navigate(['/rooms']);
      },
      error: (error: unknown) => {
        this.saving = false;
        this.error = apiMessage(error, 'That room could not be saved.');
      }
    });
  }
}
