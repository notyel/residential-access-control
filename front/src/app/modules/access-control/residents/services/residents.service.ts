import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { User } from '../../../../core/models/user.model';
import { ResponseModel } from '../../../../core/types/response.model';
import { PaginatedResultDto } from '../../../../core/types/paginated-result.dto';

// Tamaño de página para cargar la lista completa en selectores y búsquedas
const ALL_RESIDENTS_PAGE_SIZE = 500;
// Clave del rol de sistema "Propietario"
const OWNER_ROLE_KEY = 'owner';

export interface CreateResident {
  email: string;
  firstName: string;
  lastName: string;
  apartmentNumber: string;
  password: string;
}

export type UpdateResident = Pick<
  User,
  'firstName' | 'lastName' | 'apartmentNumber'
>;

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
      .set('role', OWNER_ROLE_KEY)
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

  createResident(resident: CreateResident): Observable<User> {
    return this.http
      .post<ResponseModel<User>>(this.apiUrl, {
        ...resident,
        roleKeys: [OWNER_ROLE_KEY],
      })
      .pipe(map((response) => response.data!));
  }

  updateResident(id: string, resident: UpdateResident): Observable<User> {
    return this.http
      .put<ResponseModel<User>>(`${this.apiUrl}/${id}`, resident)
      .pipe(map((response) => response.data!));
  }
}
