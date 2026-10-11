import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import {
  PARKING_KIND_LABELS,
  PARKING_STATE_LABELS,
  PARKING_STATE_VARIANTS,
  ParkingSpace,
} from '../../../core/models/parking.model';
import { ParkingApiService } from '../../../core/services/parking-api.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

/** "Mis parqueaderos": los privados de las unidades del usuario, con quién los ocupa ahora. */
@Component({
  selector: 'app-my-parking',
  standalone: true,
  imports: [DatePipe, StatusBadgeComponent],
  template: `
    <div class="app-page app-page--scroll">
      @if (errorMessage()) {
      <p class="app-alert" role="alert">{{ errorMessage() }}</p>
      }
      <section class="app-card unit-card">
        <header class="unit-card__header">
          <h2>Parqueaderos de mis unidades</h2>
        </header>
        <ul class="unit-list">
          @for (space of spaces(); track space.id) {
          <li>
            <strong class="text-mono">{{ space.code }}</strong>
            <span class="text-muted">
              Unidad <span class="text-mono">{{ space.holderUnitLabel }}</span>
              @if (space.location) { · {{ space.location }} } ·
              {{ kindLabels[space.vehicleKind] }}
            </span>
            <app-status-badge [variant]="stateVariants[space.state]">{{
              stateLabels[space.state]
            }}</app-status-badge>
            @if (space.occupant; as o) {
            <span class="text-muted">
              <span class="text-mono">{{ o.plate }}</span> desde
              {{ o.since | date : 'd MMM, h:mm a' }}
            </span>
            } @if (space.outOfServiceReason) {
            <span class="text-muted">{{ space.outOfServiceReason }}</span>
            }
          </li>
          } @empty {
          <li class="text-muted">
            {{ loading() ? 'Cargando…' : 'Sus unidades no tienen parqueaderos privados asociados.' }}
          </li>
          }
        </ul>
      </section>
    </div>
  `,
  styleUrl: '../units/pages/unit-detail/unit-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyParkingComponent implements OnInit {
  private readonly api = inject(ParkingApiService);

  readonly spaces = signal<ParkingSpace[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');

  readonly kindLabels = PARKING_KIND_LABELS;
  readonly stateLabels = PARKING_STATE_LABELS;
  readonly stateVariants = PARKING_STATE_VARIANTS;

  ngOnInit(): void {
    this.api.mine().subscribe({
      next: (spaces) => {
        this.spaces.set(spaces);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(apiErrorMessage(err, 'No se pudo cargar la información.'));
      },
    });
  }
}
