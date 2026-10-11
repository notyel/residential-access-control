import { Injectable, computed, signal, inject } from '@angular/core';
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
  order: number;
  /** Encabezado bajo el que se agrupa; null = sin grupo. */
  group?: string | null;
  iconData?: LucideIconData;
}

export interface MenuSection {
  group: string | null;
  items: MenuItem[];
}

@Injectable({
  providedIn: 'root',
})
export class MenuService {
  private apiUrl = `${environment.apiUrl}/menu`;
  private iconService = inject(IconService);
  public menuItems = signal<MenuItem[]>([]);
  /** El menú no se pudo cargar. No hay menú de respaldo: mostrar opciones sin saber el rol las expondría a todos. */
  public loadFailed = signal(false);

  /** Opciones en el orden del servidor, partidas en bloques consecutivos del mismo grupo. */
  public readonly sections = computed<MenuSection[]>(() => {
    const sections: MenuSection[] = [];
    for (const item of this.menuItems()) {
      const group = item.group ?? null;
      const last = sections[sections.length - 1];
      if (last && last.group === group) {
        last.items.push(item);
      } else {
        sections.push({ group, items: [item] });
      }
    }
    return sections;
  });

  constructor(private http: HttpClient) {}

  getMenuForCurrentUser(): Observable<MenuItem[]> {
    this.loadFailed.set(false);

    return this.http.get<ResponseModel<MenuItem[]>>(this.apiUrl).pipe(
      map((response) =>
        (response.data ?? []).map((item) => ({
          ...item,
          iconData: this.iconService.getIcon(item.icon),
        }))
      ),
      tap((items) => this.menuItems.set(items)),
      catchError(() => {
        this.menuItems.set([]);
        this.loadFailed.set(true);
        return of([]);
      })
    );
  }

  getIconData(iconName: string): LucideIconData | undefined {
    return this.iconService.getIcon(iconName);
  }
}
