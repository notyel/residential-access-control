import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  output,
} from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { LucideAngularModule, LogOut, Box, Circle } from 'lucide-angular';
import { MenuService } from '../../../core/services/menu.service';
import { AuthService } from '../../../core/services/auth.service';
import { IconService } from '../../../core/services/icon.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, LucideAngularModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarComponent implements OnInit {
  readonly menuService = inject(MenuService);
  private readonly authService = inject(AuthService);
  private readonly iconService = inject(IconService);
  private readonly router = inject(Router);

  /** Se emite al elegir un destino, para cerrar el menú en pantallas compactas. */
  readonly navigate = output<void>();

  readonly LogOut = LogOut;
  readonly Box = Box;

  ngOnInit(): void {
    this.menuService.getMenuForCurrentUser().subscribe();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  getMenuIcon(iconName: string) {
    return this.iconService.getIcon(iconName) ?? Circle;
  }
}
