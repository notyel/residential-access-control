import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { VisitorService } from '../../core/services/visitor.service';
import { Visitor } from '../../core/models/visitor.model';

@Component({
  selector: 'app-visitor-form',
  templateUrl: './visitor-form.component.html',
})
export class VisitorFormComponent implements OnInit {
  visitorForm: FormGroup;
  isEditMode = false;
  visitorId: string | null = null;

  constructor(
    private fb: FormBuilder,
    private visitorService: VisitorService,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.visitorId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.visitorId;

    this.visitorForm = this.fb.group({
      fullName: ['', Validators.required],
      documentType: ['', Validators.required],
      documentNumber: ['', Validators.required],
      gender: ['', Validators.required],
      visitorType: ['', Validators.required],
      isMinor: [false, Validators.required],
      status: ['Activo', Validators.required]
    });

    if (this.isEditMode) {
      this.visitorService.getVisitor(this.visitorId!).subscribe(response => {
        if (response.success) {
          this.visitorForm.patchValue(response.data);
        }
      });
    }
  }

  onSubmit(): void {
    if (this.visitorForm.invalid) {
      return;
    }

    const visitor: Visitor = this.visitorForm.value;

    if (this.isEditMode) {
      this.visitorService.updateVisitor(this.visitorId!, visitor).subscribe(() => {
        this.router.navigate(['/access-control/visitors']);
      });
    } else {
      this.visitorService.createVisitor(visitor).subscribe(() => {
        this.router.navigate(['/access-control/visitors']);
      });
    }
  }
}
