import { Component, OnInit, inject, signal } from '@angular/core';
import { User } from '../../../../../core/models/user.model';
import { ResidentsService } from '../../services/residents.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import {
  LucideAngularModule,
  User as UserIconLucide,
  SquarePen as EditIconLucide,
  CirclePlus,
} from 'lucide-angular';

@Component({
  selector: 'app-residents',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    LucideAngularModule,
  ],
  templateUrl: './residents.component.html',
  styleUrls: ['./residents.component.scss'],
})
export class ResidentsComponent implements OnInit {
  residents = signal<User[]>([]);
  displayedColumns: string[] = ['name', 'apartment', 'actions'];

  // Icons
  UserIcon = UserIconLucide;
  EditIcon = EditIconLucide;
  PlusCircleIcon = CirclePlus;

  private residentsService = inject(ResidentsService);

  ngOnInit(): void {
    this.residentsService
      .getResidents()
      .subscribe((items) => this.residents.set(items));
  }
}
