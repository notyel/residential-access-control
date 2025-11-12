import { Component, OnInit, inject, signal } from '@angular/core';
import { User } from '../../../../../core/models/user.model';
import { ResidentsService } from '../../services/residents.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import {
  LucideAngularModule,
  User as UserIconLucide,
  CirclePlus,
} from 'lucide-angular';
import { ResidentCardComponent } from '../../components/resident-card/resident-card.component';

@Component({
  selector: 'app-residents',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    LucideAngularModule,
    ResidentCardComponent,
  ],
  templateUrl: './residents.component.html',
  styleUrls: ['./residents.component.scss'],
})
export class ResidentsComponent implements OnInit {
  residents = signal<User[]>([]);
  isLoading = signal(true);

  // Icons
  UserIcon = UserIconLucide;
  PlusCircleIcon = CirclePlus;

  private residentsService = inject(ResidentsService);

  ngOnInit(): void {
    this.loadResidents();
  }

  private loadResidents(): void {
    this.isLoading.set(true);
    this.residentsService.getResidents().subscribe({
      next: (items) => {
        this.residents.set(items);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading residents:', error);
        this.isLoading.set(false);
      },
    });
  }

  trackByResident(index: number, resident: User): string {
    return resident.id;
  }
}
