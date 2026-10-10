import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { Residence } from '../../../../core/models/residence.model';
import { PaginatedResultDto } from '../../../../core/types/paginated-result.dto';
import { ResponseModel } from '../../../../core/types/response.model';

const SEARCH_PAGE_SIZE = 30;

@Injectable({
  providedIn: 'root',
})
export class ResidencesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/residences`;

  /** Busca por identificador de la residencia o nombre del propietario. */
  search(term: string): Observable<PaginatedResultDto<Residence>> {
    let params = new HttpParams().set('pageSize', SEARCH_PAGE_SIZE);
    if (term.trim()) {
      params = params.set('search', term.trim());
    }

    return this.http
      .get<ResponseModel<PaginatedResultDto<Residence>>>(this.apiUrl, { params })
      .pipe(map((response) => response.data ?? { items: [], totalCount: 0 }));
  }
}
