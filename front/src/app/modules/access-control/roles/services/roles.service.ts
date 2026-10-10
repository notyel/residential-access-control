import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import {
  PermissionDefinition,
  Role,
  SaveRole,
} from '../../../../core/models/role.model';
import { ResponseModel } from '../../../../core/types/response.model';

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/roles`;

  getAll(): Observable<Role[]> {
    return this.http
      .get<ResponseModel<Role[]>>(this.apiUrl)
      .pipe(map((response) => response.data ?? []));
  }

  /** Catálogo de permisos asignables, con su módulo y descripción. */
  getCatalog(): Observable<PermissionDefinition[]> {
    return this.http
      .get<ResponseModel<PermissionDefinition[]>>(`${this.apiUrl}/permissions`)
      .pipe(map((response) => response.data ?? []));
  }

  getById(id: string): Observable<Role> {
    return this.http
      .get<ResponseModel<Role>>(`${this.apiUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  create(role: SaveRole): Observable<Role> {
    return this.http
      .post<ResponseModel<Role>>(this.apiUrl, role)
      .pipe(map((response) => response.data!));
  }

  update(id: string, role: SaveRole): Observable<Role> {
    return this.http
      .put<ResponseModel<Role>>(`${this.apiUrl}/${id}`, role)
      .pipe(map((response) => response.data!));
  }

  setActive(id: string, active: boolean): Observable<Role> {
    const action = active ? 'activate' : 'deactivate';
    return this.http
      .post<ResponseModel<Role>>(`${this.apiUrl}/${id}/${action}`, null)
      .pipe(map((response) => response.data!));
  }

  delete(id: string): Observable<void> {
    return this.http
      .delete<ResponseModel<null>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => undefined));
  }
}
