import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Booking, BookingWriteResult } from './models';

export interface BookingQuery {
  from: string;
  to: string;
  roomId?: number;
  mine?: boolean;
  includeCancelled?: boolean;
}

export interface BookingCreate {
  roomId: number;
  title: string;
  notes: string | null;
  attendeeEmails: string | null;
  start: string;
  end: string;
}

@Injectable({ providedIn: 'root' })
export class BookingService {
  constructor(private readonly http: HttpClient) {}

  list(query: BookingQuery): Observable<Booking[]> {
    let params = new HttpParams().set('from', query.from).set('to', query.to);
    if (query.roomId) {
      params = params.set('roomId', String(query.roomId));
    }
    if (query.mine) {
      params = params.set('mine', 'true');
    }
    if (query.includeCancelled) {
      params = params.set('includeCancelled', 'true');
    }
    return this.http.get<Booking[]>('/api/bookings', { params });
  }

  create(body: BookingCreate): Observable<BookingWriteResult> {
    return this.http.post<BookingWriteResult>('/api/bookings', body);
  }

  cancel(id: number): Observable<BookingWriteResult> {
    return this.http.post<BookingWriteResult>(`/api/bookings/${id}/cancel`, {});
  }
}
