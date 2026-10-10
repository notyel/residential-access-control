import {
  Directive,
  OnDestroy,
  OnInit,
  TemplateRef,
  inject,
} from '@angular/core';
import { PageHeaderService } from '../../core/services/page-header.service';

/**
 * Declara las acciones de una página (Guardar, Cancelar, Registrar…). Se
 * muestran en la barra flotante del contenido mientras la página esté activa.
 * La acción principal va la última: queda a la derecha y las demás se
 * despliegan desde ella.
 *
 * ```html
 * <ng-template appPageActions>
 *   <button mat-flat-button (click)="save()">Guardar</button>
 * </ng-template>
 * ```
 */
@Directive({
  selector: 'ng-template[appPageActions]',
  standalone: true,
})
export class PageActionsDirective implements OnInit, OnDestroy {
  private readonly template = inject<TemplateRef<unknown>>(TemplateRef);
  private readonly pageHeader = inject(PageHeaderService);

  ngOnInit(): void {
    this.pageHeader.setActions(this.template);
  }

  ngOnDestroy(): void {
    this.pageHeader.clearActions(this.template);
  }
}
