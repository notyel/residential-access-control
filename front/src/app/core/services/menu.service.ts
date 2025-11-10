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
    return this.http.get<ResponseModel<MenuItem[]>>(this.apiUrl).pipe(
      map((response) => response.data!),
      tap((items) => this.menuItems.set(items))
    );
  }
}
