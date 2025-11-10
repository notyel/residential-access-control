import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, map } from 'rxjs/operators';
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
    // TODO: Remove mock data when backend is ready
    const mockMenuItems: MenuItem[] = [
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

    // Agregar los datos de iconos a los elementos del menú
    const itemsWithIcons = this.processMenuItems(mockMenuItems);
    this.menuItems.set(itemsWithIcons);

    return this.http.get<ResponseModel<MenuItem[]>>(this.apiUrl).pipe(
      map((response) => response.data!),
      tap((items) => {
        const processedItems = this.processMenuItems(items);
        this.menuItems.set(processedItems);
      })
    );
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
