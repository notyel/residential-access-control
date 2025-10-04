import { Component } from '@angular/core';
import { StatsCardComponent } from '../../components/stats-card/stats-card.component';
import { UserListComponent } from '../../components/user-list/user-list.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [StatsCardComponent, UserListComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {}
