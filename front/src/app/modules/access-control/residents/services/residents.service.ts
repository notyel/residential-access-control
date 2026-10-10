import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { User } from '../../../../core/models/user.model';
import { ResponseModel } from '../../../../core/types/response.model';
import { PaginatedResultDto } from '../../../../core/types/paginated-result.dto';

// Tamaño de página para cargar la lista completa en selectores y búsquedas
const ALL_RESIDENTS_PAGE_SIZE = 500;

@Injectable({
  providedIn: 'root',
})
export class ResidentsService {
  private apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getResidentsPage(
    pageNumber: number,
    pageSize: number
  ): Observable<PaginatedResultDto<User>> {
    const params = new HttpParams()
      .set('role', 'Owner')
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http
      .get<ResponseModel<PaginatedResultDto<User>>>(this.apiUrl, { params })
      .pipe(map((response) => response.data ?? { items: [], totalCount: 0 }));
  }

  getResidents(): Observable<User[]> {
    return this.getResidentsPage(1, ALL_RESIDENTS_PAGE_SIZE).pipe(
      map((page) => page.items)
    );
  }

  getResident(id: string): Observable<User> {
    return this.http
      .get<ResponseModel<User>>(`${this.apiUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  createResident(resident: Partial<User>): Observable<User> {
    return this.http.post<User>(this.apiUrl, { ...resident, role: 'Owner' });
  }

  updateResident(id: string, resident: Partial<User>): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/${id}`, resident);
  }
}
