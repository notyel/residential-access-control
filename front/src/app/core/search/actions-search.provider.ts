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
      title: 'Registrar visita',
      icon: Plus,
      permission: P.visitsRegister,
      run: () => this.router.navigate(['/access-control/visits/register']),
    },
    {
      id: 'action-new-resident',
      group: 'Acciones',
      title: 'Nuevo residente',
      icon: UserPlus,
      permission: P.usersManage,
      run: () => this.router.navigate(['/access-control/residents/new']),
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
        this.authService.logout();
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
      title: 'Visitas',
      icon: Calendar,
      permission: [P.visitsRead, P.visitsReadOwn],
      run: () => this.router.navigate(['/access-control/visits']),
    },
    {
      id: 'nav-residents',
      group: 'Ir a',
      title: 'Residentes',
      icon: Users,
      permission: P.usersRead,
      run: () => this.router.navigate(['/access-control/residents']),
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
