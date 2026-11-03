namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

public sealed record ParticipantResponse(
    string? Id,
    string Type,
    string Name,
    List<ParticipantItemResponse> Items,
    decimal Total,
    bool IsSubmitted);