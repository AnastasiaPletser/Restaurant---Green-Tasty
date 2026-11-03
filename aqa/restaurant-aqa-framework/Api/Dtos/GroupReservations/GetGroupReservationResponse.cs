using System;

namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

public sealed record GetGroupReservationResponse(
    string GroupReservationId,
    string LocationId,
    string RestaurantName,
    string LocationAddress,
    DateTime StartTime,
    DateTime EndTime,
    int MaxGuestCount,
    int TableNumber,
    string InviterName
);
