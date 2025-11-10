import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { User } from '../../../../core/models/user.model';

@Injectable({
  providedIn: 'root',
})
export class ResidentsService {
  private apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getResidents(): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}?role=Owner`);
  }

  getResident(id: string): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/${id}`);
  }

  createResident(resident: Partial<User>): Observable<User> {
    return this.http.post<User>(this.apiUrl, { ...resident, role: 'Owner' });
  }

  updateResident(id: string, resident: Partial<User>): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/${id}`, resident);
  }
}
