import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Person } from '../models/person.model';
import {
  BulkUnits,
  BulkUnitsResult,
  CatalogItem,
  MyUnit,
  Occupancy,
  OccupancyRelation,
  Ownership,
  PersonListItem,
  SaveUnit,
  Structure,
  Unit,
  UnitOccupancyState,
  UnitSummary,
} from '../models/unit.model';
import { User } from '../models/user.model';
import { PaginatedResultDto } from '../types/paginated-result.dto';
import { ResponseModel } from '../types/response.model';

export interface UnitFilter {
  pageNumber: number;
  pageSize: number;
  search?: string;
  state?: UnitOccupancyState | null;
  isActive?: boolean | null;
}

export interface PersonDetail {
  person: Person;
  ownerships: Ownership[];
  occupancies: Occupancy[];
  user?: User | null;
}

export interface SavePerson {
  firstName: string;
  lastName: string;
  documentType: string;
  documentNumber: string;
  phone?: string | null;
  email?: string | null;
}

export interface AddOccupancy {
  unitId: string;
  personId: string;
  relation: OccupancyRelation;
  startDate: string;
  endDate?: string | null;
  fixedTerm?: boolean;
}

export interface AddMyOccupant extends Partial<SavePerson> {
  unitId: string;
  relation: OccupancyRelation;
  startDate: string;
  endDate?: string | null;
  fixedTerm?: boolean;
}

export interface EndDate {
  endDate: string;
  reason?: string | null;
  includeDependents?: boolean;
}

const EMPTY_PAGE = { items: [], totalCount: 0 };

/**
 * Llamadas al API de unidades, personas, propietarios, ocupantes y portal. Están juntas porque
 * comparten modelos y las usan las mismas pantallas (ficha de unidad, ficha de persona).
 */
@Injectable({ providedIn: 'root' })
export class UnitsApiService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  // ---- catálogos y estructuras ----

  catalog(name: string): Observable<CatalogItem[]> {
    return this.get<CatalogItem[]>(`catalogs/${name}`).pipe(map((d) => d ?? []));
  }

  structures(): Observable<Structure[]> {
    return this.get<Structure[]>('structures').pipe(map((d) => d ?? []));
  }

  saveStructure(
    structure: { type: string; name: string; parentId?: string | null },
    id?: string
  ): Observable<Structure> {
    return id
      ? this.put<Structure>(`structures/${id}`, structure)
      : this.post<Structure>('structures', structure);
  }

  deleteStructure(id: string): Observable<void> {
    return this.http
      .delete<ResponseModel<null>>(`${this.api}/structures/${id}`)
      .pipe(map(() => undefined));
  }

  // ---- unidades ----

  units(filter: UnitFilter): Observable<PaginatedResultDto<Unit>> {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber)
      .set('pageSize', filter.pageSize);
    if (filter.search?.trim()) {
      params = params.set('search', filter.search.trim());
    }
    if (filter.state !== null && filter.state !== undefined) {
      params = params.set('state', filter.state);
    }
    if (filter.isActive !== null && filter.isActive !== undefined) {
      params = params.set('isActive', filter.isActive);
    }
    return this.get<PaginatedResultDto<Unit>>('units', params).pipe(
      map((d) => d ?? EMPTY_PAGE)
    );
  }

  unit(id: string): Observable<Unit> {
    return this.get<Unit>(`units/${id}`).pipe(map((d) => d!));
  }

  unitSummary(id: string): Observable<UnitSummary> {
    return this.get<UnitSummary>(`units/${id}/summary`).pipe(map((d) => d!));
  }

  saveUnit(unit: SaveUnit, id?: string): Observable<Unit> {
    return id ? this.put<Unit>(`units/${id}`, unit) : this.post<Unit>('units', unit);
  }

  bulkUnits(request: BulkUnits): Observable<BulkUnitsResult> {
    return this.post<BulkUnitsResult>('units/bulk', request);
  }

  setUnitActive(id: string, active: boolean): Observable<Unit> {
    return this.post<Unit>(`units/${id}/${active ? 'activate' : 'deactivate'}`, null);
  }

  // ---- propietarios ----

  addOwner(unitId: string, personId: string, startDate: string): Observable<Ownership> {
    return this.post<Ownership>(`units/${unitId}/owners`, { personId, startDate });
  }

  transferOwnership(unitId: string, personIds: string[], date: string): Observable<Ownership[]> {
    return this.post<Ownership[]>(`units/${unitId}/owners/transfer`, { personIds, date });
  }

  // ---- ocupantes ----

  addOccupancy(occupancy: AddOccupancy): Observable<Occupancy> {
    return this.post<Occupancy>('occupancies', occupancy);
  }

  endOccupancy(id: string, body: EndDate): Observable<Occupancy> {
    return this.post<Occupancy>(`occupancies/${id}/end`, body);
  }

  pendingOccupancies(): Observable<Occupancy[]> {
    return this.get<Occupancy[]>('occupancies/pending').pipe(map((d) => d ?? []));
  }

  decideOccupancy(id: string, approve: boolean, reason?: string): Observable<Occupancy> {
    return this.post<Occupancy>(`occupancies/${id}/decide`, { approve, reason });
  }

  // ---- personas ----

  people(
    search: string,
    onlyLinked: boolean,
    pageNumber: number,
    pageSize: number
  ): Observable<PaginatedResultDto<PersonListItem>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize)
      .set('onlyLinked', onlyLinked);
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    return this.get<PaginatedResultDto<PersonListItem>>('persons/search', params).pipe(
      map((d) => d ?? EMPTY_PAGE)
    );
  }

  person(id: string): Observable<PersonDetail> {
    return this.get<PersonDetail>(`persons/${id}`).pipe(map((d) => d!));
  }

  savePerson(person: SavePerson, id?: string): Observable<Person> {
    return id ? this.put<Person>(`persons/${id}`, person) : this.post<Person>('persons', person);
  }

  grantAccess(personId: string, email: string, roleKey: 'owner' | 'resident'): Observable<User> {
    return this.post<User>(`persons/${personId}/grant-access`, { email, roleKey });
  }

  // ---- portal ----

  myUnits(): Observable<MyUnit[]> {
    return this.get<MyUnit[]>('my/units').pipe(map((d) => d ?? []));
  }

  /** Devuelve además la política del conjunto: no, approval o direct. */
  myOccupants(): Observable<{ policy: string; occupants: Occupancy[] }> {
    return this.http
      .get<ResponseModel<Occupancy[]>>(`${this.api}/my/occupants`)
      .pipe(map((r) => ({ policy: r.message, occupants: r.data ?? [] })));
  }

  addMyOccupant(occupant: AddMyOccupant): Observable<{ message: string; occupancy: Occupancy }> {
    return this.http
      .post<ResponseModel<Occupancy>>(`${this.api}/my/occupants`, occupant)
      .pipe(map((r) => ({ message: r.message, occupancy: r.data! })));
  }

  endMyOccupancy(id: string, body: EndDate): Observable<Occupancy> {
    return this.post<Occupancy>(`my/occupants/${id}/end`, body);
  }

  private get<T>(path: string, params?: HttpParams): Observable<T | undefined> {
    return this.http
      .get<ResponseModel<T>>(`${this.api}/${path}`, { params })
      .pipe(map((r) => r.data));
  }

  private post<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .post<ResponseModel<T>>(`${this.api}/${path}`, body)
      .pipe(map((r) => r.data!));
  }

  private put<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .put<ResponseModel<T>>(`${this.api}/${path}`, body)
      .pipe(map((r) => r.data!));
  }
}
