import { Component, inject } from '@angular/core';
import { ThemeService } from '../../../core/services/theme.service';
import { LucideAngularModule, Sun, Moon } from 'lucide-angular';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './theme-toggle.component.html',
  styleUrl: './theme-toggle.component.scss'
})
export class ThemeToggleComponent {
  themeService = inject(ThemeService);

  readonly SunIcon = Sun;
  readonly MoonIcon = Moon;

  toggleTheme() {
    this.themeService.toggleTheme();
  }
  getIcon() {
    return this.themeService.isDarkTheme() ? this.SunIcon : this.MoonIcon;
  }
}
