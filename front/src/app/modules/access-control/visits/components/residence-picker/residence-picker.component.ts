import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Check, LucideAngularModule, Search } from 'lucide-angular';
import { User as Resident } from '../../../../../core/models/user.model';
import { normalizeSearchText } from '../../../../../core/search/search-provider';
import { SidePanelComponent } from '../../../../../shared/components/side-panel/side-panel.component';

export interface ResidencePickerData {
  residents: Resident[];
  selectedId: string | null;
}

/** Selector de residencia; se abre en el panel lateral y devuelve la elegida. */
@Component({
  selector: 'app-residence-picker',
  standalone: true,
  imports: [LucideAngularModule, SidePanelComponent],
  templateUrl: './residence-picker.component.html',
  styleUrl: './residence-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResidencePickerComponent {
  private readonly dialogRef = inject<DialogRef<Resident>>(DialogRef);
  readonly data = inject<ResidencePickerData>(DIALOG_DATA);

  readonly SearchIcon = Search;
  readonly CheckIcon = Check;

  readonly term = signal('');

  readonly results = computed(() => {
    const query = normalizeSearchText(this.term());
    return this.data.residents.filter((resident) =>
      normalizeSearchText(
        `${resident.apartmentNumber} ${resident.firstName} ${resident.lastName}`
      ).includes(query)
    );
  });

  onInput(event: Event): void {
    this.term.set((event.target as HTMLInputElement).value);
  }

  select(resident: Resident): void {
    this.dialogRef.close(resident);
  }
}
