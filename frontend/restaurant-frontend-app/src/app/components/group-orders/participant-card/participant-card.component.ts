import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { CurrencyPipe } from '@angular/common';

import { Participant } from '@app/models/group-orders/participant.model';

@Component({
  selector: 'app-participant-card',
  standalone: true,
  imports: [CurrencyPipe],
  templateUrl: './participant-card.component.html',
  styleUrl: './participant-card.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class ParticipantCardComponent {
  readonly participant = input.required<Participant>();
  readonly isCurrentUser = input(false);

  readonly title = computed(() => {
    const person = this.participant();

    if (this.isCurrentUser()) {
      return 'My Pre-Order';
    }

    return person.type === 'Owner'
      ? `Owner: ${person.name}`
      : `Guest: ${person.name}`;
  });
}