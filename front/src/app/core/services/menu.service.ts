import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { tap, map, catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ResponseModel } from '../types/response.model';
import { LucideIconData } from 'lucide-angular';
import { IconService } from './icon.service';

export interface MenuItem {
  id: string;
  name: string;
  path: string;
  icon: string;
  iconData?: LucideIconData;
}

@Injectable({
  providedIn: 'root',
})
export class MenuService {
  private apiUrl = `${environment.apiUrl}/menu`;
  private iconService = inject(IconService);
  public menuItems = signal<MenuItem[]>([]);

  constructor(private http: HttpClient) {}

  getMenuForCurrentUser(): Observable<MenuItem[]> {
    return this.http.get<ResponseModel<MenuItem[]>>(this.apiUrl).pipe(
      map((response) => response.data!),
      tap((items) => {
        const processedItems = this.processMenuItems(items);
        this.menuItems.set(processedItems);
      }),
      catchError((error) => {
        console.warn(
          'Failed to load menu from backend, using fallback menu:',
          error
        );
        const fallbackMenu = this.getFallbackMenu();
        const processedItems = this.processMenuItems(fallbackMenu);
        this.menuItems.set(processedItems);
        return of(fallbackMenu);
      })
    );
  }

  private getFallbackMenu(): MenuItem[] {
    return [
      {
        id: '1',
        name: 'Tablero',
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
  }

  private processMenuItems(items: MenuItem[]): MenuItem[] {
    return items.map((item) => ({
      ...item,
      iconData: this.iconService.getIcon(item.icon),
    }));
  }

  getIconData(iconName: string): LucideIconData | undefined {
    return this.iconService.getIcon(iconName);
  }
}
