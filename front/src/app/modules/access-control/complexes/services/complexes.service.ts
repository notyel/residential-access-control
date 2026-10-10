import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import {
  Complex,
  CreateComplex,
  SaveComplex,
} from '../../../../core/models/complex.model';
import { ResponseModel } from '../../../../core/types/response.model';

@Injectable({ providedIn: 'root' })
export class ComplexesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/complexes`;

  getAll(): Observable<Complex[]> {
    return this.http
      .get<ResponseModel<Complex[]>>(this.apiUrl)
      .pipe(map((response) => response.data ?? []));
  }

  getById(id: string): Observable<Complex> {
    return this.http
      .get<ResponseModel<Complex>>(`${this.apiUrl}/${id}`)
      .pipe(map((response) => response.data!));
  }

  create(complex: CreateComplex): Observable<Complex> {
    return this.http
      .post<ResponseModel<Complex>>(this.apiUrl, complex)
      .pipe(map((response) => response.data!));
  }

  update(id: string, complex: SaveComplex): Observable<Complex> {
    return this.http
      .put<ResponseModel<Complex>>(`${this.apiUrl}/${id}`, complex)
      .pipe(map((response) => response.data!));
  }

  setActive(id: string, active: boolean): Observable<Complex> {
    const action = active ? 'activate' : 'deactivate';
    return this.http
      .post<ResponseModel<Complex>>(`${this.apiUrl}/${id}/${action}`, null)
      .pipe(map((response) => response.data!));
  }
}
