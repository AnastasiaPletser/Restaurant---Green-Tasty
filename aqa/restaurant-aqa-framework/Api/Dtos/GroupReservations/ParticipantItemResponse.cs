namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

public sealed record ParticipantItemResponse(
    ParticipantDishResponse Dish,
    int Quantity);

public sealed record ParticipantDishResponse(
    string Id,
    string Name,
    decimal Price);