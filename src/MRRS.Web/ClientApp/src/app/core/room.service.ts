import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Room, RoomWrite } from './models';

@Injectable({ providedIn: 'root' })
export class RoomService {
  constructor(private readonly http: HttpClient) {}

  list(includeInactive = false): Observable<Room[]> {
    const params = includeInactive ? new HttpParams().set('includeInactive', 'true') : undefined;
    return this.http.get<Room[]>('/api/rooms', { params });
  }

  get(id: number): Observable<Room> {
    return this.http.get<Room>(`/api/rooms/${id}`);
  }

  create(room: RoomWrite): Observable<Room> {
    return this.http.post<Room>('/api/rooms', room);
  }

  update(id: number, room: RoomWrite): Observable<Room> {
    return this.http.put<Room>(`/api/rooms/${id}`, room);
  }
}
