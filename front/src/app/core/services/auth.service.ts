import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SessionUser } from '../models/user.model';
import { PermissionRule, grants } from '../security/permissions';
import { ResponseModel } from '../types/response.model';
import { ComplexContextService } from './complex-context.service';

interface LoginCredentials {
  email: string;
  password: string;
}

interface AuthResponse {
  user: SessionUser;
  token: string;
}

/**
 * Sesión del usuario. En el navegador solo se guarda el token; el usuario, sus roles y sus
 * permisos viven en memoria y se piden al servidor (auth/me) al abrir la aplicación y cada vez
 * que hay que refrescarlos, así un cambio hecho por el administrador se refleja sin cerrar sesión.
 */
@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly complexContext = inject(ComplexContextService);

  private readonly TOKEN_KEY = 'jwt';

  private readonly userState = signal<SessionUser | null>(null);
  readonly currentUser = this.userState.asReadonly();
  readonly user$ = toObservable(this.userState);

  private readonly permissions = computed<ReadonlySet<string>>(
    () => new Set(this.userState()?.permissions ?? [])
  );

  login(credentials: LoginCredentials): Observable<AuthResponse> {
    return this.http
      .post<ResponseModel<AuthResponse>>(
        `${environment.apiUrl}/auth/login`,
        credentials
      )
      .pipe(
        map((response) => response.data!),
        tap((response) => {
          localStorage.setItem(this.TOKEN_KEY, response.token);
          this.userState.set(response.user);
        })
      );
  }

  /**
   * Cierre de sesión pedido por el usuario: invalida sus tokens en el servidor (en todos los
   * dispositivos) y limpia la sesión local, aunque el servidor no responda.
   */
  signOut(): void {
    if (this.hasValidToken()) {
      this.http
        .post(`${environment.apiUrl}/auth/logout`, null)
        .subscribe({ error: () => undefined });
    }
    this.logout();
  }

  /** Limpia la sesión local sin avisar al servidor (token vencido o ya rechazado). */
  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.complexContext.clear();
    this.userState.set(null);
  }

  /**
   * Vuelve a pedir el usuario con sus permisos al día. Devuelve false si no hay sesión válida.
   * Un fallo de red no cierra la sesión: se conserva lo que ya había en memoria.
   */
  refresh(): Observable<boolean> {
    if (!this.hasValidToken()) {
      this.userState.set(null);
      return of(false);
    }

    return this.http
      .get<ResponseModel<SessionUser>>(`${environment.apiUrl}/auth/me`)
      .pipe(
        map((response) => {
          this.userState.set(response.data ?? null);
          return response.data != null;
        }),
        catchError(() => of(this.userState() !== null))
      );
  }

  /** Hay token vigente y el usuario ya se cargó. */
  isAuthenticated(): boolean {
    return this.hasValidToken() && this.userState() !== null;
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getUser(): SessionUser | null {
    return this.userState();
  }

  isSuperAdmin(): boolean {
    return this.userState()?.isSuperAdmin === true;
  }

  /** Reactivo: dentro de un computed o una plantilla se actualiza al cambiar los permisos. */
  hasPermission(rule: PermissionRule): boolean {
    const user = this.userState();
    if (!user) {
      return false;
    }
    if (user.isSuperAdmin) {
      return true;
    }
    const owned = this.permissions();
    const anyOf = typeof rule === 'string' ? [rule] : rule;
    return anyOf.some((permission) => grants(owned, permission));
  }

  /** Nombres de los roles del usuario, para mostrarlos. */
  roleNames(): string {
    const user = this.userState();
    if (!user) {
      return '';
    }
    return user.isSuperAdmin
      ? 'Superadmin'
      : user.roles.map((role) => role.name).join(', ') || 'Sin rol';
  }

  /** No valida la firma: eso lo hace el servidor en cada petición. */
  private hasValidToken(): boolean {
    const token = this.getToken();
    return !!token && !this.isExpired(token);
  }

  private isExpired(token: string): boolean {
    try {
      const payload = token.split('.')[1];
      const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      const exp = (JSON.parse(json) as { exp?: number }).exp;
      return typeof exp !== 'number' || exp * 1000 <= Date.now();
    } catch {
      // un token que no se puede leer se trata como vencido
      return true;
    }
  }
}
