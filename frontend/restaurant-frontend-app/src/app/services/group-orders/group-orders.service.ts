import {
  HttpClient,
  HttpContext,
} from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';

import {
  GuestInvite,
  GroupInviteToken,
} from '@app/models/reservations/guest-invite.model';

import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GroupStatusResponse } from '@app/models/group-orders/group-status.model';
import {
  SubmittedDish,
  SubmitGroupPreOrderRequest,
} from '@app/models/group-orders/submit-group-pre-order.model';

import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

import { SKIP_ERROR_TOAST } from '@app/auth/skip-error-toast.context';

@Injectable({
  providedIn: 'root',
})
export class GroupOrdersService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);
  private createContext(silent = false): HttpContext {
    return new HttpContext().set(SKIP_ERROR_TOAST, silent);
  }

  getGroupInvite$(reservationId: string): Observable<GuestInvite> {
    return this.http
      .get<GroupInviteToken>(
        `${this.baseUrl}/reservations/${encodeURIComponent(reservationId)}/invite-token`,
      )
      .pipe(
        switchMap(({ token }) =>
          this.http.get<GuestInvite>(
            `${this.baseUrl}/guest/invites/${encodeURIComponent(token)}`,
          ),
        ),
      );
  }

  getSessions$(
    groupReservationId: string,
    options?: { silent?: boolean },
  ): Observable<GroupSessionsResponse> {
    return this.http.get<GroupSessionsResponse>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions`,
      {
        context: this.createContext(options?.silent),
      },
    );
  }

  deleteGuestSession$(
    groupReservationId: string,
    guestSessionId: string,
  ): Observable<GroupSessionsResponse> {
    return this.http.delete<GroupSessionsResponse>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions/${encodeURIComponent(guestSessionId)}`,
    );
  }

  getOwnerSessionId$(reservationId: string): Observable<string> {
    return this.http
      .get<{ guestSessionId: string }>(
        `${this.baseUrl}/reservations/${encodeURIComponent(reservationId)}/guest-session-id`,
      )
      .pipe(
        switchMap(({ guestSessionId }) => [guestSessionId]),
      );
  }

  getStatus$(
    groupReservationId: string,
    options?: { silent?: boolean },
  ): Observable<GroupStatusResponse> {
    return this.http.get<GroupStatusResponse>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/status`,
      {
        context: this.createContext(options?.silent),
      },
    );
  }

  submit$(
    groupReservationId: string,
    reservationId: string,
  ): Observable<SubmittedDish[]> {
    const body: SubmitGroupPreOrderRequest = { reservationId };

    return this.http.post<SubmittedDish[]>(
      `${this.baseUrl}/group-reservations/${encodeURIComponent(groupReservationId)}/submit`,
      body,
    );
  }
}