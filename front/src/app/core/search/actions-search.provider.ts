import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  BarChart,
  Calendar,
  LogOut,
  LucideIconData,
  Plus,
  SunMoon,
  UserPlus,
  Users,
} from 'lucide-angular';
import { Observable, of } from 'rxjs';
import { P, PermissionRule } from '../security/permissions';
import { AuthService } from '../services/auth.service';
import { ThemeService } from '../services/theme.service';
import {
  SearchProvider,
  SearchResult,
  normalizeSearchText,
} from './search-provider';

interface ActionDefinition {
  id: string;
  group: string;
  title: string;
  icon: LucideIconData;
  /** Permiso que habilita la acción; sin él, está disponible para todos. */
  permission?: PermissionRule;
  run: () => void;
}

/** Navegación y acciones rápidas, filtradas por los permisos del usuario. */
@Injectable()
export class ActionsSearchProvider implements SearchProvider {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly themeService = inject(ThemeService);

  private readonly actions: ActionDefinition[] = [
    {
      id: 'action-register-visit',
      group: 'Acciones',
      title: 'Validar visitante',
      icon: Plus,
      permission: P.accessRegister,
      run: () => this.router.navigate(['/access-control/gate']),
    },
    {
      id: 'action-new-person',
      group: 'Acciones',
      title: 'Nueva persona',
      icon: UserPlus,
      permission: P.peopleWrite,
      run: () => this.router.navigate(['/access-control/people/new']),
    },
    {
      id: 'action-new-user',
      group: 'Acciones',
      title: 'Nuevo usuario',
      icon: UserPlus,
      permission: P.usersManage,
      run: () => this.router.navigate(['/access-control/users/new']),
    },
    {
      id: 'action-toggle-theme',
      group: 'Acciones',
      title: 'Cambiar entre modo claro y oscuro',
      icon: SunMoon,
      run: () => this.themeService.toggleTheme(),
    },
    {
      id: 'action-logout',
      group: 'Acciones',
      title: 'Cerrar sesión',
      icon: LogOut,
      run: () => {
        this.authService.signOut();
        this.router.navigate(['/login']);
      },
    },
    {
      id: 'nav-dashboard',
      group: 'Ir a',
      title: 'Panel',
      icon: BarChart,
      permission: P.dashboardRead,
      run: () => this.router.navigate(['/access-control/dashboard']),
    },
    {
      id: 'nav-visits',
      group: 'Ir a',
      title: 'Visitantes',
      icon: Calendar,
      permission: [P.visitsRead, P.visitsReadOwn],
      run: () => this.router.navigate(['/access-control/visits']),
    },
    {
      id: 'nav-vehicle-check',
      group: 'Ir a',
      title: 'Verificar vehículos',
      icon: Calendar,
      permission: P.accessRegister,
      run: () => this.router.navigate(['/access-control/gate/vehicles']),
    },
    {
      id: 'nav-vehicles',
      group: 'Ir a',
      title: 'Vehículos',
      icon: Calendar,
      permission: P.vehiclesRead,
      run: () => this.router.navigate(['/access-control/vehicles']),
    },
    {
      id: 'nav-parking',
      group: 'Ir a',
      title: 'Parqueaderos',
      icon: Calendar,
      permission: P.parkingRead,
      run: () => this.router.navigate(['/access-control/parking']),
    },
    {
      id: 'nav-access-history',
      group: 'Ir a',
      title: 'Historial de accesos',
      icon: Calendar,
      permission: [P.accessRead, P.accessReadOwn],
      run: () => this.router.navigate(['/access-control/access-history']),
    },
    {
      id: 'nav-reports',
      group: 'Ir a',
      title: 'Reportes',
      icon: BarChart,
      permission: P.reportsRead,
      run: () => this.router.navigate(['/access-control/reports']),
    },
    {
      id: 'nav-people',
      group: 'Ir a',
      title: 'Propietarios y residentes',
      icon: Users,
      permission: P.peopleRead,
      run: () => this.router.navigate(['/access-control/people']),
    },
    {
      id: 'nav-units',
      group: 'Ir a',
      title: 'Unidades',
      icon: Users,
      permission: P.unitsRead,
      run: () => this.router.navigate(['/access-control/units']),
    },
  ];

  search(term: string): Observable<SearchResult[]> {
    const query = normalizeSearchText(term);

    return of(
      this.actions.filter(
        (action) =>
          (!action.permission ||
            this.authService.hasPermission(action.permission)) &&
          normalizeSearchText(action.title).includes(query)
      )
    );
  }
}
