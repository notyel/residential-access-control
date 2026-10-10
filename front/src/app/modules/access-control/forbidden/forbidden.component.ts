import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { LucideAngularModule, ShieldOff } from 'lucide-angular';
import { MenuService } from '../../../core/services/menu.service';

/** Destino de las rutas que existen pero que el usuario no puede abrir. */
@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [RouterLink, MatButtonModule, LucideAngularModule],
  template: `
    <div class="app-page app-page--scroll">
      <section class="app-card forbidden" role="alert">
        <lucide-icon [img]="Icon" [size]="32"></lucide-icon>
        <h2>No tiene acceso a esta página</h2>
        <p class="text-muted">
          Su usuario no tiene el permiso necesario. Si cree que debería tenerlo,
          pídalo a la administración del conjunto.
        </p>
        @if (firstOption(); as option) {
        <a mat-flat-button [routerLink]="option.path">Ir a {{ option.name }}</a>
        }
      </section>
    </div>
  `,
  styles: `
    .forbidden {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--app-space-3);
      max-width: 480px;
      margin: var(--app-space-6, 48px) auto 0;
      padding: var(--app-space-6, 48px) var(--app-space-4);
      text-align: center;

      h2,
      p {
        margin: 0;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForbiddenComponent {
  private readonly menuService = inject(MenuService);
  readonly Icon = ShieldOff;

  readonly firstOption = () => this.menuService.menuItems()[0] ?? null;
}
