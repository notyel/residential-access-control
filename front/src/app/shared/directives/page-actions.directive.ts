import {
  Directive,
  OnDestroy,
  OnInit,
  TemplateRef,
  inject,
} from '@angular/core';
import { PageHeaderService } from '../../core/services/page-header.service';

/**
 * Declara las acciones de una página para que se muestren en la barra
 * superior mientras la página esté activa.
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
