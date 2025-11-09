import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { Observable } from 'rxjs';
import { PagedVisits, VisitsService } from '../visits.service';

@Component({
  selector: 'app-visits',
  templateUrl: './visits.component.html',
  styleUrls: ['./visits.component.scss']
})
export class VisitsComponent implements OnInit {
  visits$: Observable<PagedVisits>;
  filterForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private visitsService: VisitsService
  ) {
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

  private loadVisits(pageNumber: number = 1) {
    this.visits$ = this.visitsService.getVisits({
      pageNumber,
      pageSize: 10,
      ...this.filterForm.value
    });
  }
}
