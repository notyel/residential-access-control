import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ResponseModel } from '../types/response.model';

interface LoginCredentials {
  email: string;
  password: string;
}

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  apartmentNumber: string;
  role: number;
}

interface AuthResponse {
  user: User;
  token: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly TOKEN_KEY = 'jwt';
  private readonly USER_KEY = 'user';
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(
    this.hasToken()
  );
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  private userSubject = new BehaviorSubject<User | null>(this.getUser());
  public user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient) {}

  // Mapeo de roles numéricos a strings
  private getRoleText(role: number): string {
    const roleMap: { [key: number]: string } = {
      0: 'Admin',
      1: 'Guard',
      2: 'Owner',
    };
    return roleMap[role] || 'Usuario';
  }

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
          localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
          this.isAuthenticatedSubject.next(true);
          this.userSubject.next(response.user);
        })
      );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.isAuthenticatedSubject.next(false);
    this.userSubject.next(null);
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

  getUser(): User | null {
    const userData = localStorage.getItem(this.USER_KEY);
    return userData ? JSON.parse(userData) : null;
  }

  getUserRole(): string | null {
    const user = this.getUser();
    return user ? this.getRoleText(user.role) : null;
  }

  getRole(): string | null {
    return this.getUserRole();
  }

  hasRole(role: string): boolean {
    return this.getUserRole() === role;
  }
}
