import { Component, OnInit, inject, signal } from '@angular/core';
import { User } from '../../../../core/models/user.model';
import { ResidentsService } from '../../residents.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-residents',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './residents.component.html',
  styleUrls: ['./residents.component.scss']
})
export class ResidentsComponent implements OnInit {
  residents = signal<User[]>([]);

  private residentsService = inject(ResidentsService);

  ngOnInit(): void {
    this.residentsService.getResidents().subscribe(items => this.residents.set(items));
  }
}
