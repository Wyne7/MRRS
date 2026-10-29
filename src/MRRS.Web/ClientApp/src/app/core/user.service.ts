import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserProfile, UserWrite } from './models';

@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private readonly http: HttpClient) {}

  list(): Observable<UserProfile[]> {
    return this.http.get<UserProfile[]>('/api/users');
  }

  get(id: string): Observable<UserProfile> {
    return this.http.get<UserProfile>(`/api/users/${id}`);
  }

  create(body: UserWrite): Observable<UserProfile> {
    return this.http.post<UserProfile>('/api/users', body);
  }

  update(id: string, body: UserWrite): Observable<UserProfile> {
    return this.http.put<UserProfile>(`/api/users/${id}`, body);
  }
}
