import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Building2, User as UserIcon } from 'lucide-angular';
import { Observable, catchError, forkJoin, map, of } from 'rxjs';
import { P } from '../security/permissions';
import { AuthService } from '../services/auth.service';
import { UnitsApiService } from '../services/units-api.service';
import { SearchProvider, SearchResult } from './search-provider';

const MIN_TERM_LENGTH = 2;
const MAX_RESULTS = 5;

/** Unidades por código o propietario, y personas por nombre o documento; lleva a su ficha. */
@Injectable()
export class UnitsSearchProvider implements SearchProvider {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly api = inject(UnitsApiService);

  search(term: string): Observable<SearchResult[]> {
    const query = term.trim();
    if (query.length < MIN_TERM_LENGTH) {
      return of([]);
    }

    const units = this.authService.hasPermission(P.unitsRead)
      ? this.api.units({ pageNumber: 1, pageSize: MAX_RESULTS, search: query }).pipe(
          map((page) =>
            page.items.map<SearchResult>((unit) => ({
              id: `unit-${unit.id}`,
              group: 'Unidades',
              title: unit.label,
              subtitle: unit.owners.join(', ') || 'Sin propietario registrado',
              icon: Building2,
              run: () => this.router.navigate(['/access-control/units', unit.id]),
            }))
          ),
          catchError(() => of([] as SearchResult[]))
        )
      : of([] as SearchResult[]);

    const people = this.authService.hasPermission(P.peopleRead)
      ? this.api.people(query, false, 1, MAX_RESULTS).pipe(
          map((page) =>
            page.items.map<SearchResult>((person) => ({
              id: `person-${person.id}`,
              group: 'Personas',
              title: `${person.firstName} ${person.lastName}`,
              subtitle: `${person.documentType} ${person.documentNumber}`,
              icon: UserIcon,
              run: () => this.router.navigate(['/access-control/people', person.id]),
            }))
          ),
          catchError(() => of([] as SearchResult[]))
        )
      : of([] as SearchResult[]);

    return forkJoin([units, people]).pipe(map(([u, p]) => [...u, ...p]));
  }
}
