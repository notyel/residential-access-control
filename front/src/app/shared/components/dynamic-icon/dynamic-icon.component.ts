import { Component, Input, inject, OnInit } from '@angular/core';
import {
  LucideAngularModule,
  LucideIconData,
  HelpCircle,
} from 'lucide-angular';
import { IconService } from '../../../core/services/icon.service';

@Component({
  selector: 'app-dynamic-icon',
  standalone: true,
  imports: [LucideAngularModule],
  template: `
    <lucide-icon
      [img]="currentIcon"
      [size]="size"
      [color]="color"
      [strokeWidth]="strokeWidth"
      [class]="cssClass"
    >
    </lucide-icon>
  `,
})
export class DynamicIconComponent implements OnInit {
  @Input() iconName: string = '';
  @Input() iconData?: LucideIconData;
  @Input() size: number = 24;
  @Input() color?: string;
  @Input() strokeWidth?: number;
  @Input() cssClass?: string;

  private iconService = inject(IconService);

  // Icono de fallback si no se encuentra el icono solicitado
  private fallbackIcon = HelpCircle;

  protected currentIcon: LucideIconData = this.fallbackIcon;

  ngOnInit(): void {
    // Prioridad: iconData pasado directamente > buscar por nombre > fallback
    if (this.iconData) {
      this.currentIcon = this.iconData;
    } else if (this.iconName) {
      const foundIcon = this.iconService.getIcon(this.iconName);
      this.currentIcon = foundIcon || this.fallbackIcon;
    } else {
      this.currentIcon = this.fallbackIcon;
    }
  }
}
