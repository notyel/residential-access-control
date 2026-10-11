import {
  Directive,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  input,
} from '@angular/core';
import { PermissionRule } from '../../core/security/permissions';
import { AuthService } from '../../core/services/auth.service';

/**
 * Muestra el contenido solo si el usuario tiene el permiso (con varios, basta uno):
 * <button *appIfPermission="P.accessRegister">. Se actualiza si los permisos cambian.
 * Solo oculta la acción; quien la autoriza es el servidor.
 */
@Directive({
  selector: '[appIfPermission]',
  standalone: true,
})
export class IfPermissionDirective {
  readonly appIfPermission = input.required<PermissionRule>();

  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly authService = inject(AuthService);
  private visible = false;

  constructor() {
    effect(() => {
      const allowed = this.authService.hasPermission(this.appIfPermission());
      if (allowed && !this.visible) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.visible = true;
      } else if (!allowed && this.visible) {
        this.viewContainer.clear();
        this.visible = false;
      }
    });
  }
}
