namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

public sealed record GetAllSessionsResponse(
    List<ParticipantResponse> Participants,
    decimal Total);