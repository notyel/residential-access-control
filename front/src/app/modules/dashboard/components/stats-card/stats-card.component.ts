import { Component, Input } from '@angular/core';
import {
  LucideAngularModule,
  Users,
  DollarSign,
  BarChart,
  Activity,
} from 'lucide-angular';

@Component({
  selector: 'app-stats-card',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './stats-card.component.html',
  styleUrl: './stats-card.component.scss',
})
export class StatsCardComponent {
  @Input() title: string = '';
  @Input() value: string = '';
  @Input() icon: string = '';

  readonly UsersIcon = Users;
  readonly DollarSignIcon = DollarSign;
  readonly BarChartIcon = BarChart;
  readonly ActivityIcon = Activity;

  getIcon() {
    switch (this.icon) {
      case 'users':
        return this.UsersIcon;
      case 'dollar-sign':
        return this.DollarSignIcon;
      case 'bar-chart':
        return this.BarChartIcon;
      case 'activity':
        return this.ActivityIcon;
      default:
        return this.UsersIcon;
    }
  }
}
