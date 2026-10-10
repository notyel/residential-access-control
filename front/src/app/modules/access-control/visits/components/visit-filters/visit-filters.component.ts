import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { SidePanelComponent } from '../../../../../shared/components/side-panel/side-panel.component';

export interface VisitFilters {
  startDate: Date | null;
  endDate: Date | null;
}

export const EMPTY_VISIT_FILTERS: VisitFilters = {
  startDate: null,
  endDate: null,
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
    SidePanelComponent,
  ],
  templateUrl: './visit-filters.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VisitFiltersComponent {
  private readonly dialogRef =
    inject<DialogRef<VisitFilters>>(DialogRef);
  private readonly current = inject<VisitFilters>(DIALOG_DATA);

  readonly form = inject(FormBuilder).group({
    startDate: [this.current.startDate],
    endDate: [this.current.endDate],
  });

  apply(): void {
    if (this.form.invalid) {
      return;
    }
    const { startDate, endDate } = this.form.getRawValue();
    this.dialogRef.close({ startDate, endDate });
  }

  clear(): void {
    this.dialogRef.close({ ...EMPTY_VISIT_FILTERS });
  }
}
