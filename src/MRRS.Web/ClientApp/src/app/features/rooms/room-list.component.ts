import { Component, OnInit } from '@angular/core';
import { apiMessage } from '../../core/date-util';
import { Room, equipmentLabels, locationLine } from '../../core/models';
import { RoomService } from '../../core/room.service';

@Component({
  selector: 'mrrs-room-list',
  templateUrl: './room-list.component.html',
  styleUrl: './room-list.component.css',
  standalone: false
})
export class RoomListComponent implements OnInit {
  rooms: Room[] = [];
  loading = true;
  error: string | null = null;

  constructor(private readonly roomsApi: RoomService) {}

  ngOnInit(): void {
    this.roomsApi.list(true).subscribe({
      next: rooms => {
        this.rooms = rooms;
        this.loading = false;
      },
      error: (error: unknown) => {
        this.loading = false;
        this.error = apiMessage(error, 'Rooms could not be loaded.');
      }
    });
  }

  equipment(room: Room): string[] {
    return equipmentLabels(room);
  }

  where(room: Room): string {
    return locationLine(room);
  }
}
