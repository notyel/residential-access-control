import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { SidePanelComponent } from '../../../../../shared/components/side-panel/side-panel.component';
import { VisitStatus } from '../../services/visits.service';

export interface VisitFilters {
  startDate: Date | null;
  endDate: Date | null;
  status: VisitStatus | null;
}

export const EMPTY_VISIT_FILTERS: VisitFilters = {
  startDate: null,
  endDate: null,
  status: null,
};

export const VISIT_STATUS_LABELS: Record<VisitStatus, string> = {
  Inside: 'En sitio',
  CheckedOut: 'Finalizada',
};

/** Filtros del listado de visitas; se abre en el panel lateral. */
@Component({
  selector: 'app-visit-filters',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    SidePanelComponent,
  ],
  templateUrl: './visit-filters.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VisitFiltersComponent {
  private readonly dialogRef =
    inject<DialogRef<VisitFilters>>(DialogRef);
  private readonly current = inject<VisitFilters>(DIALOG_DATA);

  readonly statusLabels = VISIT_STATUS_LABELS;

  readonly form = inject(FormBuilder).group({
    startDate: [this.current.startDate],
    endDate: [this.current.endDate],
    status: [this.current.status],
  });

  apply(): void {
    if (this.form.invalid) {
      return;
    }
    const { startDate, endDate, status } = this.form.getRawValue();
    this.dialogRef.close({ startDate, endDate, status });
  }

  clear(): void {
    this.dialogRef.close({ ...EMPTY_VISIT_FILTERS });
  }
}
