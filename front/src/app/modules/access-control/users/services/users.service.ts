import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { User, UserStatus } from '../../../../core/models/user.model';
import { PaginatedResultDto } from '../../../../core/types/paginated-result.dto';
import { ResponseModel } from '../../../../core/types/response.model';

export interface UserFilter {
  pageNumber: number;
  pageSize: number;
  search?: string;
  role?: string;
  status?: UserStatus;
}

export interface SaveUser {
  firstName: string;
  lastName: string;
  roleIds: string[];
}

export interface CreateUser extends SaveUser {
  email: string;
}

/** Acciones sobre un usuario que no son editar sus datos. */
export type UserAction = 'activate' | 'deactivate' | 'revoke';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/users`;

  getPage(filter: UserFilter): Observable<PaginatedResultDto<User>> {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber)
      .set('pageSize', filter.pageSize);
    if (filter.search?.trim()) {
      params = params.set('search', filter.search.trim());
    }
    if (filter.role) {
      params = params.set('role', filter.role);
    }
    if (filter.status) {
      params = params.set('status', filter.status);
    }

    return this.http
      .get<ResponseModel<PaginatedResultDto<User>>>(this.apiUrl, { params })
      .pipe(map((response) => response.data ?? { items: [], totalCount: 0 }));
  }

  getById(id: string): Observable<User> {
    return this.http
      .get<ResponseModel<User>>(`${this.apiUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  /** El usuario queda invitado: recibe por correo el enlace para definir su contraseña. */
  create(user: CreateUser): Observable<User> {
    return this.http
      .post<ResponseModel<User>>(this.apiUrl, user)
      .pipe(map((response) => response.data!));
  }

  update(id: string, user: SaveUser): Observable<User> {
    return this.http
      .put<ResponseModel<User>>(`${this.apiUrl}/${id}`, user)
      .pipe(map((response) => response.data!));
  }

  run(id: string, action: UserAction): Observable<User> {
    return this.http
      .post<ResponseModel<User>>(`${this.apiUrl}/${id}/${action}`, null)
      .pipe(map((response) => response.data!));
  }

  resendInvitation(id: string): Observable<void> {
    return this.http
      .post<ResponseModel<null>>(`${this.apiUrl}/${id}/resend-invitation`, null)
      .pipe(map(() => undefined));
  }
}
