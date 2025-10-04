import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';

export interface LoginResponse {
  token: string;
  user: {
    id: string;
    email: string;
    fullName: string;
  };
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = 'http://localhost:5069/api/auth';
  private tokenSubject = new BehaviorSubject<string | null>(this.getToken());
  private userSubject = new BehaviorSubject<any>(this.getUser());

  public token$ = this.tokenSubject.asObservable();
  public user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.API_URL}/login`, { email, password })
      .pipe(
        tap(response => {
          this.setToken(response.token);
          this.setUser(response.user);
          this.tokenSubject.next(response.token);
          this.userSubject.next(response.user);
        })
      );
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.tokenSubject.next(null);
    this.userSubject.next(null);
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  private setToken(token: string): void {
    localStorage.setItem('token', token);
  }

  private getUser(): any {
    const user = localStorage.getItem('user');
    return user ? JSON.parse(user) : null;
  }

  private setUser(user: any): void {
    localStorage.setItem('user', JSON.stringify(user));
  }
}
