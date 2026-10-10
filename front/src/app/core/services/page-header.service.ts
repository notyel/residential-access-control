import { Injectable, TemplateRef, signal } from '@angular/core';
import { Data } from '@angular/router';

export interface PageParent {
  label: string;
  url: string;
}

/** Datos opcionales que una ruta declara en `data` para la barra superior. */
export interface PageHeaderData {
  subtitle?: string;
  backUrl?: string;
  parent?: PageParent;
}

/**
 * Estado de la barra superior: el título de la vista activa (tomado de la
 * ruta) y las acciones que la página declara con `appPageActions`.
 */
@Injectable({
  providedIn: 'root',
})
export class PageHeaderService {
  readonly title = signal('');
  readonly subtitle = signal<string | null>(null);
  readonly backUrl = signal<string | null>(null);
  readonly parent = signal<PageParent | null>(null);
  readonly actions = signal<TemplateRef<unknown> | null>(null);

  setFromRoute(title: string, data: Data): void {
    const header = data as PageHeaderData;
    this.title.set(title);
    this.subtitle.set(header.subtitle ?? null);
    this.backUrl.set(header.backUrl ?? null);
    this.parent.set(header.parent ?? null);
  }

  setActions(template: TemplateRef<unknown>): void {
    this.actions.set(template);
  }

  clearActions(template: TemplateRef<unknown>): void {
    if (this.actions() === template) {
      this.actions.set(null);
    }
  }
}
