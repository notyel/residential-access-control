import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

interface LoginCredentials {
  email: string;
  password: string;
}

interface AuthResponse {
  token: string;
  role: string;
  userId: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly TOKEN_KEY = 'jwt';
  private readonly ROLE_KEY = 'role';
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(
    this.hasToken()
  );
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  private userRoleSubject = new BehaviorSubject<string | null>(
    this.getRole()
  );
  public userRole$ = this.userRoleSubject.asObservable();

  constructor(private http: HttpClient) {}

  login(credentials: LoginCredentials): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/login`, credentials)
      .pipe(
        tap((response) => {
          localStorage.setItem(this.TOKEN_KEY, response.token);
          localStorage.setItem(this.ROLE_KEY, response.role);
          this.isAuthenticatedSubject.next(true);
          this.userRoleSubject.next(response.role);
        })
      );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.ROLE_KEY);
    this.isAuthenticatedSubject.next(false);
    this.userRoleSubject.next(null);
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    return this.hasToken();
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getRole(): string | null {
    return localStorage.getItem(this.ROLE_KEY);
  }

  hasRole(role: string): boolean {
    return this.getRole() === role;
  }
}
