import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { PageHeaderService } from './page-header.service';

const APP_NAME = 'Zentryal';

/** Lleva el `title` de la ruta activa a la pestaña y a la barra superior. */
@Injectable()
export class AppTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly pageHeader = inject(PageHeaderService);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const pageTitle = this.buildTitle(snapshot) ?? '';

    let route = snapshot.root;
    while (route.firstChild) {
      route = route.firstChild;
    }

    this.pageHeader.setFromRoute(pageTitle, route.data);
    this.title.setTitle(pageTitle ? `${pageTitle} · ${APP_NAME}` : APP_NAME);
  }
}
