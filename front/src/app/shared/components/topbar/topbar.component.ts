import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import {
  ArrowLeft,
  LucideAngularModule,
  PanelLeft,
  Search,
} from 'lucide-angular';
import { AuthService } from '../../../core/services/auth.service';
import { CommandPaletteService } from '../../../core/services/command-palette.service';
import { ComplexContextService } from '../../../core/services/complex-context.service';
import { PageHeaderService } from '../../../core/services/page-header.service';
import { initials } from '../../utils/initials';
import { ThemeToggleComponent } from '../theme-toggle/theme-toggle.component';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [
    NgTemplateOutlet,
    RouterLink,
    LucideAngularModule,
    ThemeToggleComponent,
  ],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopbarComponent {
  readonly pageHeader = inject(PageHeaderService);
  readonly commandPalette = inject(CommandPaletteService);
  private readonly authService = inject(AuthService);

  readonly sidebarOpen = input(false);
  readonly menuToggle = output<void>();

  readonly MenuIcon = PanelLeft;
  readonly BackIcon = ArrowLeft;
  readonly SearchIcon = Search;

  private readonly user = this.authService.currentUser;

  readonly displayName = computed(() => {
    const user = this.user();
    if (!user) {
      return 'Usuario';
    }
    const fullName = `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim();
    return fullName || user.email.split('@')[0];
  });

  readonly userInitials = computed(() => {
    const user = this.user();
    return user?.firstName || user?.lastName
      ? initials(user.firstName, user.lastName)
      : this.displayName().charAt(0).toUpperCase();
  });

  /** Rol y conjunto en el que se trabaja; el superadministrador ve el que tenga elegido. */
  readonly userRole = computed(() => {
    const user = this.user();
    if (!user) {
      return 'Usuario';
    }
    if (user.isSuperAdmin) {
      const selected = this.complexContext.selected();
      return selected ? `Superadmin · ${selected.name}` : 'Superadmin';
    }
    const role = this.authService.roleNames();
    return user.complexName ? `${role} · ${user.complexName}` : role;
  });
  private readonly complexContext = inject(ComplexContextService);
}
