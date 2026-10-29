using APIHandler.Models.GuestSessions;
using System.Threading.Tasks;

namespace APIHandler.Services.GuestSessions;

public interface IGuestSessionService
{
    Task<CreatGuestSessionResponse> CreateGuestSessionAsync(
        CreateGuestSessionRequest request);

    Task AddDishAsync(
        string guestSessionId,
        string dishId,
        int quantity);

    Task RemoveDishAsync(
        string guestSessionId,
        string dishId,
        int quantity);

    Task<GroupReservationStatusResponse> GetGroupReservationStatusAsync(
        string groupReservationId);

    Task<bool> IsAuthorizedAsync(
        string guestSessionToken,
        string guestSessionId);

    Task<bool> IsOwnerSessionAsync(
    string userId,
    string guestSessionId);

    Task SubmitCartAsync(string guestSessionId);

    Task<GuestMenuResponse> GetGuestMenuAsync(
        string guestSessionId);

    Task<GetAllSessionsResponse> DeleteGuestSessionAsync(
        string userId,
        string groupReservationId,
        string guestSessionId);

    Task<GetAllSessionsResponse> GetAllSessionsAsync(
        string userId,
        string groupReservationId);

    Task<GetGuestSessionResponse> GetGuestSessionInfoAsync(
        string guestSessionId);

    Task CreateGroupOwnerSessionAsync(
        string userId,
        string groupReservationId);

    Task EnsureGroupOwnerSessionAsync(
        string userId,
        string reservationId);

    Task<string> GetOwnerSessionIdAsync(
        string userId,
        string reservationId);

    Task<GetGuestSessionResponse> GetOwnerSessionInfoAsync(
    string userId,
    string groupReservationId);
}