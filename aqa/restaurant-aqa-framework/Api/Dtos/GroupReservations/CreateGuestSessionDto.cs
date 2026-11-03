namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

public sealed record CreateGuestSessionRequest(
    string GroupReservationId,
    string GuestName);

public sealed record CreateGuestSessionResponse(
    string SessionId,
    string SessionToken);
