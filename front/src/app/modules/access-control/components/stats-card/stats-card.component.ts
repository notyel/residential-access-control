import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-stats-card',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="stats-card">
      <div class="stats-card-icon">
        <lucide-icon [img]="icon" [size]="24"></lucide-icon>
      </div>
      <div class="stats-card-content">
        <h3 class="stats-card-title">{{ title }}</h3>
        <p class="stats-card-value">{{ value }}</p>
        <span class="stats-card-description">{{ description }}</span>
      </div>
    </div>
  `,
  styleUrl: './stats-card.component.scss',
})
export class StatsCardComponent {
  @Input() title!: string;
  @Input() value!: string | number;
  @Input() description!: string;
  @Input() icon!: any;
  @Input() color: 'primary' | 'secondary' | 'success' | 'warning' = 'primary';
}
