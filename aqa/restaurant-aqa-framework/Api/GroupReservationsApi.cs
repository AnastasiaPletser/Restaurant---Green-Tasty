using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

namespace Restaurant_Aqa_Framework.Api;

public sealed class GroupReservationsApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> CreateGuestSessionAsync(
        CreateGuestSessionRequest request) =>
        Api.PostAsync(Url("/guest/sessions"),
            new APIRequestContextOptions
            {
                DataString = Serialize(request),
                Headers = Headers(),
            });

    public Task<IAPIResponse> GetInviteTokenAsync(
        string reservationId,
        string? bearerToken = null) =>
        GetAsync(Url($"/reservations/{reservationId}/invite-token"),
            new APIRequestContextOptions { Headers = Headers(bearerToken) });

    public Task<IAPIResponse> GetGroupReservationByTokenAsync(
        string token,
        string? bearerToken = null) =>
        GetAsync(Url($"/guest/invites/{token}"),
            new APIRequestContextOptions { Headers = Headers(bearerToken) });

    public Task<IAPIResponse> GetAllSessionsAsync(
        string groupReservationId,
        string? bearerToken = null) =>
        GetAsync(Url($"/guest/groups/{groupReservationId}/sessions"),
            new APIRequestContextOptions { Headers = Headers(bearerToken) });

    public Task<IAPIResponse> GetGroupReservationStatusAsync(
    string groupReservationId,
    string? bearerToken = null) =>
    GetAsync(Url($"/guest/groups/{groupReservationId}/status"),
        new APIRequestContextOptions { Headers = Headers(bearerToken) });

    public Task<IAPIResponse> GetOwnerSessionIdAsync(
        string reservationId,
        string? bearerToken = null) =>
        GetAsync(Url($"/reservations/{reservationId}/guest-session-id"),
            new APIRequestContextOptions { Headers = Headers(bearerToken) });

    public Task<IAPIResponse> DeleteGuestSessionAsync(
        string groupReservationId,
        string guestSessionId,
        string? bearerToken = null) =>
        DeleteAsync(Url($"/guest/groups/{groupReservationId}/sessions/{guestSessionId}"),
            new APIRequestContextOptions { Headers = Headers(bearerToken) });

    public Task<IAPIResponse> ConfirmGroupPreOrderAsync(
        string groupReservationId,
        string reservationId,
        string? bearerToken = null) =>
        Api.PostAsync(Url($"/group-reservations/{groupReservationId}/submit"),
            new APIRequestContextOptions
            {
                DataString = Serialize(new { reservationId }),
                Headers = Headers(bearerToken),
            });
               public Task<IAPIResponse> AddDishToGuestSessionAsync(
        string guestSessionId,
        string dishId,
        int quantity,
        string? bearerToken = null) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/dishes"),
            new APIRequestContextOptions
            {
                DataString = Serialize(new { dishId, quantity }),
                Headers = Headers(bearerToken),
            });

    public Task<IAPIResponse> SubmitGuestCartAsync(
        string guestSessionId,
        string? bearerToken = null) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/submit"),
            new APIRequestContextOptions
            {
                Headers = Headers(bearerToken),
            });
}