import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ResponseModel } from '../types/response.model';

export interface MenuItem {
  id: string;
  name: string;
  path: string;
  icon: string;
}

@Injectable({
  providedIn: 'root',
})
export class MenuService {
  private apiUrl = `${environment.apiUrl}/menu`;
  public menuItems = signal<MenuItem[]>([]);

  constructor(private http: HttpClient) {}

  getMenuForCurrentUser(): Observable<MenuItem[]> {
    // TODO: Remove mock data when backend is ready
    const mockMenuItems: MenuItem[] = [
      {
        id: '1',
        name: 'Dashboard',
        path: '/access-control/dashboard',
        icon: 'BarChart',
      },
      {
        id: '2',
        name: 'Visitas',
        path: '/access-control/visits',
        icon: 'Calendar',
      },
      {
        id: '3',
        name: 'Residentes',
        path: '/access-control/residents',
        icon: 'Users',
      },
    ];

    this.menuItems.set(mockMenuItems);

    return this.http.get<ResponseModel<MenuItem[]>>(this.apiUrl).pipe(
      map((response) => response.data!),
      tap((items) => this.menuItems.set(items))
    );
  }
}
