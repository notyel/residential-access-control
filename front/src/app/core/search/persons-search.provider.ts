import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { IdCard } from 'lucide-angular';
import { Observable, map, of } from 'rxjs';
import { P } from '../security/permissions';
import { AuthService } from '../services/auth.service';
import { PersonService } from '../services/person.service';
import { SearchProvider, SearchResult } from './search-provider';

const DOCUMENT_PATTERN = /^\d{5,}$/;

/**
 * Busca una persona por número de documento exacto (lo que admite el API) y
 * ofrece registrarle una visita.
 */
@Injectable()
export class PersonsSearchProvider implements SearchProvider {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly personService = inject(PersonService);

  search(term: string): Observable<SearchResult[]> {
    const documentNumber = term.trim();
    const canRegister =
      this.authService.hasPermission(P.accessRegister) &&
      this.authService.hasPermission(P.peopleRead);

    if (!canRegister || !DOCUMENT_PATTERN.test(documentNumber)) {
      return of([]);
    }

    return this.personService.searchPersons(documentNumber).pipe(
      map((persons) =>
        persons.map((person) => ({
          id: `person-${person.id}`,
          group: 'Personas',
          title: `${person.firstName} ${person.lastName}`,
          subtitle: `${person.documentType} ${person.documentNumber} · Validar ingreso`,
          icon: IdCard,
          // El documento viaja en el estado de navegación, no en la URL
          run: () =>
            this.router.navigate(['/access-control/gate'], {
              state: { documentNumber: person.documentNumber },
            }),
        }))
      )
    );
  }
}
