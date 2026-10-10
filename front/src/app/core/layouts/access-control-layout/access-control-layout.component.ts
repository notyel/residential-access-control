import { A11yModule } from '@angular/cdk/a11y';
import { BreakpointObserver } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { CommandPaletteService } from '../../services/command-palette.service';
import { SidebarComponent } from '../../../shared/components/sidebar/sidebar.component';
import { TopbarComponent } from '../../../shared/components/topbar/topbar.component';

const COMPACT_QUERY = '(max-width: 1023.98px)';
const SIDEBAR_STORAGE_KEY = 'sidebarCollapsed';

@Component({
  selector: 'app-access-control-layout',
  standalone: true,
  imports: [A11yModule, RouterOutlet, TopbarComponent, SidebarComponent],
  templateUrl: './access-control-layout.component.html',
  styleUrl: './access-control-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessControlLayoutComponent {
  private readonly commandPalette = inject(CommandPaletteService);
  private readonly main = viewChild.required<ElementRef<HTMLElement>>('main');

  /** En pantallas compactas el menú se muestra como overlay. */
  readonly isCompact = toSignal(
    inject(BreakpointObserver)
      .observe(COMPACT_QUERY)
      .pipe(map((state) => state.matches)),
    { initialValue: false }
  );

  private readonly collapsed = signal(this.readCollapsed());
  private readonly overlayOpen = signal(false);

  readonly sidebarOpen = computed(() =>
    this.isCompact() ? this.overlayOpen() : !this.collapsed()
  );

  constructor() {
    // Al cambiar de tamaño el overlay del menú siempre arranca cerrado
    effect(
      () => {
        this.isCompact();
        this.overlayOpen.set(false);
      },
      { allowSignalWrites: true }
    );
  }

  toggleSidebar(): void {
    if (this.isCompact()) {
      this.overlayOpen.update((open) => !open);
      return;
    }

    this.collapsed.update((collapsed) => !collapsed);
    try {
      localStorage.setItem(SIDEBAR_STORAGE_KEY, String(this.collapsed()));
    } catch {
      // Sin almacenamiento la preferencia solo dura la sesión
    }
  }

  closeOverlay(): void {
    this.overlayOpen.set(false);
  }

  skipToContent(event: Event): void {
    event.preventDefault();
    this.main().nativeElement.focus();
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.commandPalette.toggle();
      return;
    }

    if (event.key === '/' && !this.isTyping(event.target)) {
      event.preventDefault();
      void this.commandPalette.open();
      return;
    }

    if (event.key === 'Escape' && this.isCompact() && this.overlayOpen()) {
      this.closeOverlay();
    }
  }

  private isTyping(target: EventTarget | null): boolean {
    return (
      target instanceof HTMLElement &&
      (target.isContentEditable ||
        ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName))
    );
  }

  private readCollapsed(): boolean {
    try {
      return localStorage.getItem(SIDEBAR_STORAGE_KEY) === 'true';
    } catch {
      return false;
    }
  }
}
