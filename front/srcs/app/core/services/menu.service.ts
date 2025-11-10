import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface MenuItem {
  label: string;
  icon: string;
  path: string;
  roles: string[];
}

@Injectable({
  providedIn: 'root',
})
export class MenuService {
  private apiUrl = `${environment.apiUrl}/menu`;
  public menuItems = signal<MenuItem[]>([]);

  constructor(private http: HttpClient) {}

  getMenuForCurrentUser(): Observable<MenuItem[]> {
    return this.http.get<MenuItem[]>(this.apiUrl).pipe(
      tap((items) => this.menuItems.set(items))
    );
  }
}
