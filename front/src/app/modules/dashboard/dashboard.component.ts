import { Component } from '@angular/core';
import { HeaderComponent } from './components/header/header.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { StatsCardComponent } from './components/stats-card/stats-card.component';
import { UserListComponent } from './components/user-list/user-list.component';
import { LucideAngularModule } from 'lucide-angular';
import { Users, DollarSign, BarChart, Activity } from 'lucide-angular';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    HeaderComponent,
    SidebarComponent,
    StatsCardComponent,
    UserListComponent,
    LucideAngularModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  usersIcon = Users;
  dollarSignIcon = DollarSign;
  barChartIcon = BarChart;
  activityIcon = Activity;
}
