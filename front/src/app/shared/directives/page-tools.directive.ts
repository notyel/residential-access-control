import {
  Directive,
  OnDestroy,
  OnInit,
  TemplateRef,
  inject,
} from '@angular/core';
import { PageHeaderService } from '../../core/services/page-header.service';

/**
 * Declara las herramientas de una página que viven en la barra superior
 * (p. ej. los filtros de una tabla). Las acciones de la página (Guardar,
 * Registrar…) no van aquí: se declaran con `appPageActions` y flotan
 * dentro del contenido.
 */
@Directive({
  selector: 'ng-template[appPageTools]',
  standalone: true,
})
export class PageToolsDirective implements OnInit, OnDestroy {
  private readonly template = inject<TemplateRef<unknown>>(TemplateRef);
  private readonly pageHeader = inject(PageHeaderService);

  ngOnInit(): void {
    this.pageHeader.setTools(this.template);
  }

  ngOnDestroy(): void {
    this.pageHeader.clearTools(this.template);
  }
}
