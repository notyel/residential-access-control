import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { User } from '../../core/models/user.model';

@Injectable({
  providedIn: 'root'
})
export class ResidentsService {

  constructor(private http: HttpClient) { }

  getResidents(): Observable<User[]> {
    return this.http.get<User[]>(`${environment.apiUrl}/users?role=Owner`);
  }

  updateResident(id: string, resident: Partial<User>): Observable<User> {
    return this.http.put<User>(`${environment.apiUrl}/users/${id}`, resident);
  }
}
