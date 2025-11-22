import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { VisitsService } from '../../services/visits.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import {
  LucideAngularModule,
  Calendar,
  Filter,
  PlusCircle,
} from 'lucide-angular';
import { PaginatedResultDto } from '../../../../../core/types/paginated-result.dto';
import { Visit } from '../../../../../core/models/visit.model';
import { PageHeaderComponent } from '../../../components/page-header/page-header.component';
import { IfRoleDirective } from '../../../../../shared/directives/if-role.directive';

@Component({
  selector: 'app-visits',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatPaginatorModule,
    LucideAngularModule,
    PageHeaderComponent,
    IfRoleDirective,
  ],
  templateUrl: './visits.component.html',
  styleUrls: ['./visits.component.scss'],
})
export class VisitsComponent implements OnInit {
  visits = signal<PaginatedResultDto<Visit>>({
    items: [],
    totalCount: 0,
  });
  filterForm: FormGroup;
  displayedColumns: string[] = ['visitor', 'document', 'date', 'actions'];

  // Paginator properties
  totalCount = 0;
  pageSize = 10;
  pageIndex = 0;

  // Icons
  CalendarIcon = Calendar;
  FilterIcon = Filter;
  PlusCircleIcon = PlusCircle;

  private fb = inject(FormBuilder);
  private visitsService = inject(VisitsService);

  constructor() {
    this.filterForm = this.fb.group({
      startDate: [''],
      endDate: [''],
    });
  }

  ngOnInit(): void {
    this.loadVisits();
  }

  onFilter() {
    this.loadVisits();
  }

  onPageChange(event: PageEvent) {
    this.loadVisits(event.pageIndex + 1, event.pageSize);
  }

  private loadVisits(pageNumber: number = 1, pageSize: number = 10) {
    this.visitsService
      .getVisits({
        pageNumber,
        pageSize,
        ...this.filterForm.value,
      })
      .subscribe((pagedVisits) => {
        this.visits.set(pagedVisits);
        this.totalCount = pagedVisits.totalCount;
        this.pageSize = pageSize;
        this.pageIndex = pageNumber - 1;
      });
  }
}
