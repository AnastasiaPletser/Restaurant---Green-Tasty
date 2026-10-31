import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnDestroy,
  signal,
} from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

import {
  EMPTY,
  exhaustMap,
  forkJoin,
  Subscription,
  timer,
  catchError,
  finalize,
  tap,
} from 'rxjs';

import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ToastService } from '@app/services/toast/toast.service';

import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { GroupReservationStatus } from '@app/models/group-orders/group-status.model';

import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';
import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';

@Component({
  selector: 'app-group-order-page',
  standalone: true,
  imports: [
    ParticipantCardComponent,
    ModalComponent,
    CurrencyPipe,
    DatePipe,
  ],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class GroupOrderPageComponent implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly groupOrdersService = inject(GroupOrdersService);
  private readonly toast = inject(ToastService);
  private pollingSubscription?: Subscription;

  private readonly reservationId =
    this.route.snapshot.paramMap.get('reservationId') ?? '';

  readonly invite = signal<GuestInvite | null>(null);
  readonly participants = signal<Participant[]>([]);

  readonly guestToRemove = signal<Participant | null>(null);
  readonly isRemovingGuest = signal(false);
  readonly removeGuestError = signal('');

  readonly guestSessionId = signal('');

  readonly status = signal<GroupReservationStatus>('Open');

  readonly loading = signal(false);
  readonly error = signal('');
  readonly isSubmitting = signal(false);
  readonly showSubmitModal = signal(false);
  readonly isSubmitted = signal(false);
  readonly updatedAt = signal<Date | null>(null);
  readonly refreshError = signal('');
  readonly isRefreshing = signal(false);

  readonly guestCount = computed(() => this.participants().length);

  readonly unsubmittedCount = computed(
    () => this.participants().filter((person) => !person.isSubmitted).length,
  );

  readonly consolidatedOrder = computed(() =>
    consolidatePreOrder(this.participants()),
  );

  constructor() {
    this.loadGroup();
  }

  private refreshGroup$() {
    const invite = this.invite();

    if (!invite) {
      return EMPTY;
    }

    return forkJoin({
      sessions: this.groupOrdersService.getSessions$(
        invite.GroupReservationId,
        { silent: true },
      ),
      status: this.groupOrdersService.getStatus$(
        invite.GroupReservationId,
        { silent: true },
      ),
    }).pipe(
      tap(({ sessions, status }) => {
        this.participants.set(sessions.participants);
        this.status.set(status.status);
        this.isSubmitted.set(status.status === 'Submitted');
        this.updatedAt.set(new Date());
        this.refreshError.set('');

        if (status.status !== 'Open') {
          this.stopPolling();
        }
      }),
      catchError(() => {
        const lastUpdated = this.updatedAt();

        this.refreshError.set(
          lastUpdated
            ? `Couldn't refresh. Showing data from ${lastUpdated.toLocaleTimeString()}`
            : "Couldn't refresh. Showing the last available data.",
        );

        return EMPTY;
      }),
      finalize(() => this.isRefreshing.set(false)),
    );
  }

  private startPolling(): void {
    if (this.pollingSubscription || this.isSubmitted()) {
      return;
    }

    this.pollingSubscription = timer(15_000, 15_000)
      .pipe(
        exhaustMap(() => {
          if (this.isRefreshing()) {
            return EMPTY;
          }

          this.isRefreshing.set(true);
          return this.refreshGroup$();
        }),
      )
      .subscribe();
  }

  private stopPolling(): void {
    this.pollingSubscription?.unsubscribe();
    this.pollingSubscription = undefined;
  }

  loadGroup(): void {
    this.stopPolling();

    if (!this.reservationId) {
      this.error.set('Group reservation not found');
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.refreshError.set('');

    this.groupOrdersService.getGroupInvite$(this.reservationId).subscribe({
      next: (invite) => {
        this.invite.set(invite);

        this.groupOrdersService
          .getSessions$(invite.GroupReservationId)
          .subscribe({
            next: (response) => {
              this.participants.set(response.participants);
              this.loadOwnerAndStatus(invite.GroupReservationId);
            },
            error: (error) => this.handleLoadError(error),
          });
      },
      error: (error) => this.handleLoadError(error),
    });
  }

  private loadOwnerAndStatus(groupReservationId: string): void {
    this.groupOrdersService
      .getOwnerSessionId$(this.reservationId)
      .subscribe({
        next: (sessionId) => this.guestSessionId.set(sessionId),
        error: (error) => this.handleLoadError(error),
      });

    this.groupOrdersService.getStatus$(groupReservationId).subscribe({
      next: (response) => {
        this.status.set(response.status);
        this.isSubmitted.set(response.status === 'Submitted');
        this.updatedAt.set(new Date());
        this.loading.set(false);

        if (response.status === 'Open') {
          this.startPolling();
        }
      },
      error: (error) => this.handleLoadError(error),
    });
  }

  private handleLoadError(error: { status?: number }): void {
    this.loading.set(false);

    this.error.set(
      error.status === 404
        ? 'Group reservation not found'
        : 'Something went wrong. Please try again.',
    );
  }

  openSubmitModal(): void {
    this.showSubmitModal.set(true);
  }

  openRemoveGuestModal(guest: Participant): void {
    if (
      guest.type !== 'Guest' ||
      this.status() !== 'Open' ||
      this.isSubmitted() ||
      this.isRemovingGuest()
    ) {
      return;
    }

    this.guestToRemove.set(guest);
    this.removeGuestError.set('');
  }

  closeRemoveGuestModal(): void {
    if (this.isRemovingGuest()) {
      return;
    }

    this.guestToRemove.set(null);
    this.removeGuestError.set('');
  }

  confirmRemoveGuest(): void {
  const guest = this.guestToRemove();
  const invite = this.invite();

  if (
    !guest ||
    !invite ||
    guest.type !== 'Guest' ||
    this.status() !== 'Open' ||
    this.isSubmitted() ||
    this.isRemovingGuest()
  ) {
    return;
  }

  this.isRemovingGuest.set(true);
  this.removeGuestError.set('');

  this.groupOrdersService
    .deleteGuestSession$(invite.GroupReservationId, guest.id)
    .pipe(
      finalize(() => this.isRemovingGuest.set(false)),
    )
    .subscribe({
      next: (response) => {
        this.participants.set(response.participants);
        this.guestToRemove.set(null);
        this.removeGuestError.set('');
        this.toast.success('Guest removed successfully');
      },
      error: () => {
        this.removeGuestError.set(
          'Could not remove the guest. Please try again.',
        );
      },
    });
}

  closeSubmitModal(): void {
    if (!this.isSubmitting()) {
      this.showSubmitModal.set(false);
    }
  }

  submitPreOrder(): void {
    if (
      this.isSubmitting() ||
      this.isSubmitted() ||
      this.consolidatedOrder().items.length === 0
    ) {
      return;
    }

    const invite = this.invite();

    if (!invite) {
      return;
    }

    this.isSubmitting.set(true);

    this.groupOrdersService
      .submit$(invite.GroupReservationId, this.reservationId)
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.isSubmitted.set(true);
          this.status.set('Submitted');
          this.stopPolling();
          this.showSubmitModal.set(false);
          this.toast.success('Pre-order sent to the restaurant');
        },
        error: () => {
          this.isSubmitting.set(false);
        },
      });
  }

  get statusLabel(): string {
    if (this.isSubmitted() || this.status() === 'Submitted') {
      return 'Sent to Restaurant';
    }

    if (this.status() === 'Closed') {
      return 'Closed';
    }

    return 'Collecting pre-orders';
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  refreshNow(): void {
    if (this.isRefreshing() || this.loading() || this.isSubmitted()) {
      return;
    }

    this.isRefreshing.set(true);
    this.refreshGroup$().subscribe();
  }
}