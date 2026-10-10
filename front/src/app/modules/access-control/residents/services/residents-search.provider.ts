import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { User as UserIcon } from 'lucide-angular';
import { Observable, map, of } from 'rxjs';
import {
  SearchProvider,
  SearchResult,
  normalizeSearchText,
} from '../../../../core/search/search-provider';
import { AuthService } from '../../../../core/services/auth.service';
import { P } from '../../../../core/security/permissions';
import { ResidentsService } from './residents.service';

const MIN_TERM_LENGTH = 2;
const MAX_RESULTS = 6;

/** Residentes por nombre, apartamento o correo (solo administradores). */
@Injectable()
export class ResidentsSearchProvider implements SearchProvider {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly residentsService = inject(ResidentsService);

  search(term: string): Observable<SearchResult[]> {
    const query = normalizeSearchText(term);

    if (query.length < MIN_TERM_LENGTH || !this.authService.hasPermission(P.usersRead)) {
      return of([]);
    }

    return this.residentsService.getResidents().pipe(
      map((residents) =>
        residents
          .filter((resident) =>
            normalizeSearchText(
              `${resident.firstName} ${resident.lastName} ${resident.apartmentNumber} ${resident.email}`
            ).includes(query)
          )
          .slice(0, MAX_RESULTS)
          .map((resident) => ({
            id: `resident-${resident.id}`,
            group: 'Residentes',
            title: `${resident.firstName} ${resident.lastName}`,
            subtitle: `Apartamento ${resident.apartmentNumber}`,
            icon: UserIcon,
            run: () =>
              this.router.navigate([
                '/access-control/residents/edit',
                resident.id,
              ]),
          }))
      )
    );
  }
}
