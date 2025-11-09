import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { PagedVisits, VisitsService } from '../../visits.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-visits',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './visits.component.html',
  styleUrls: ['./visits.component.scss']
})
export class VisitsComponent implements OnInit {
  visits = signal<PagedVisits>({ visits: [], totalCount: 0, pageNumber: 1 });
  filterForm: FormGroup;

  private fb = inject(FormBuilder);
  private visitsService = inject(VisitsService);

  constructor() {
    this.filterForm = this.fb.group({
      startDate: [''],
      endDate: ['']
    });
  }

  ngOnInit(): void {
    this.loadVisits();
  }

  onFilter() {
    this.loadVisits();
  }

  onPageChange(pageNumber: number) {
    this.loadVisits(pageNumber);
  }

  getTotalPages(totalCount: number, pageSize: number): number {
    if (!totalCount || totalCount === 0) {
      return 1;
    }
    return Math.ceil(totalCount / pageSize);
  }

  private loadVisits(pageNumber: number = 1) {
    this.visitsService.getVisits({
      pageNumber,
      pageSize: 10,
      ...this.filterForm.value
    }).subscribe(pagedVisits => this.visits.set(pagedVisits));
  }
}
