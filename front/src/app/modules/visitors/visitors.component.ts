import { Component, OnInit } from '@angular/core';
import { Visitor } from '../../core/models/visitor.model';
import { VisitorService } from '../../core/services/visitor.service';

@Component({
  selector: 'app-visitors',
  templateUrl: './visitors.component.html',
})
export class VisitorsComponent implements OnInit {
  visitors: Visitor[] = [];

  constructor(private visitorService: VisitorService) { }

  ngOnInit(): void {
    this.visitorService.getAllVisitors().subscribe(response => {
      if (response.success) {
        this.visitors = response.data;
      }
    });
  }
}
