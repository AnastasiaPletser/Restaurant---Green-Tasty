using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.GroupReservation;
using APIHandler.Models.GuestReservation;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.Auth;
using APIHandler.Services.Locations;
using APIHandler.Constants.Orders;
using APIHandler.Services.Orders;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace APIHandler.Services.GroupReservations;

public class GroupReservationService(
    IGroupReservationRepository groupReservationRepository,
    ITableRepository tableRepository,
    IReservationRepository reservationRepository,
    IGuestSessionRepository guestSessionRepository,
    IUserService userService,
    ILocationService locationService,
    IOrderService orderService) : IGroupReservationService
{
    private const string DefaultRestaurantName = "Green & Tasty";

    public async Task<GetGroupReservationResponse> GetGroupReservationByTokenAsync(string token)
    {
        GroupReservation groupReservation = await groupReservationRepository.GetGroupReservationByTokenAsync(token)
            ?? throw new GroupReservationNotFoundException();

        if (groupReservation.Status != GroupReservationStatus.Open)
            throw new GroupReservationExpiredException();

        Task<Reservation?> reservationTask = reservationRepository.GetReservationByIdAsync(groupReservation.ReservationId);
        Task<int> countTask = guestSessionRepository.CountByGroupReservationId(groupReservation.GroupReservationId);

        Reservation reservation = await reservationTask ?? throw new ValidationException("Associated reservation not found.");
        int joinedGuestsCount = await countTask;

        if (joinedGuestsCount + 1 >= reservation.GuestCount)
            throw new GuestLimitExceededException();

        Table table = await tableRepository.GetTableAsync(reservation.TableId)
            ?? throw new InvalidOperationException("Associated table not found.");

        Location? location = await locationService.GetLocationByIdAsync(table.LocationId);
        string restaurantName = DefaultRestaurantName;
        string address = location?.Address ?? "Unknown Address";

        string inviterName = "Unknown";
        if (!string.IsNullOrWhiteSpace(reservation.CustomerId))
        {
            inviterName = await userService.GetUserName(reservation.CustomerId) ?? "Unknown";
        }
        return new GetGroupReservationResponse(
            groupReservation.GroupReservationId,
            table.LocationId,
            restaurantName,
            address,
            reservation.StartTime,
            reservation.EndTime,
            reservation.GuestCount,
            table.TableNumber,
            inviterName);
    }

    public async Task<GroupReservation> GetGroupReservationAsync(string groupReservationId)
    {
        GroupReservation? groupReservation = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId);
        return groupReservation ?? throw new InvalidOperationException($"Group reservation with ID {groupReservationId} not found.");
    }

    public async Task<GroupReservation> CreateGroupReservationAsync(string reservationId)
    {
        var groupReservation = new GroupReservation
        {
            GroupReservationId = Guid.NewGuid().ToString(),
            ReservationId = reservationId,
            ReservationToken = Guid.NewGuid().ToString("N"),
            Status = GroupReservationStatus.Open,
        };

        await groupReservationRepository.SaveGroupReservationAsync(groupReservation);
        return groupReservation;
    }

    public async Task CreateGroupReservationIfDoesNotExistAsync(string userId, string reservationId)
    {
        GroupReservation? group = await groupReservationRepository.GetByReservationIdAsync(reservationId);
        if (group is not null)
            return;

        await CreateGroupReservationAsync(reservationId);
    }

    public async Task UpdateGroupReservationStatusAsync(string groupReservationId, GroupReservationStatus status)
    {
        var groupReservation = await GetGroupReservationAsync(groupReservationId);

        groupReservation.Status = status;

        await groupReservationRepository.SaveGroupReservationAsync(groupReservation);
    }

    public async Task DeleteGroupReservationAsync(string groupReservationId)
    {
        var groupReservation = await GetGroupReservationAsync(groupReservationId);

        await groupReservationRepository.DeleteGroupReservationAsync(groupReservation);
    }

    public async Task CloseGroupReservationAsync(string reservationId)
    {
        GroupReservation? groupReservation = await groupReservationRepository.GetByReservationIdAsync(reservationId);
        if (groupReservation is null)
            return;

        groupReservation.Status = GroupReservationStatus.Closed;

        await groupReservationRepository.SaveGroupReservationAsync(groupReservation);
    }

    public async Task<GetGroupInviteTokenResponse> GetGroupReservationInviteTokenAsync(string reservationId)
    {
        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId)
            ?? throw new ReservationNotFoundException($"Reservation {reservationId} was not found.");

        if (reservation.GuestCount < 2)
            throw new ValidationException("Retrieving invitation token for the reservation with only one guest is not permitted.");

        if (reservation.Status != ReservationStatus.Reserved)
            throw new GroupReservationExpiredException("The group reservation is expired.");

        GroupReservation groupReservation = (await groupReservationRepository.GetByReservationIdAsync(reservationId))!;

        if (groupReservation.Status == GroupReservationStatus.Submitted)
            throw new GroupReservationSubmittedException();

        if (groupReservation.Status == GroupReservationStatus.Closed)
            throw new GroupReservationExpiredException();

        return new GetGroupInviteTokenResponse(groupReservation.ReservationToken);
    }
    public async Task<List<OrderItem>> ConfirmGroupPreOrderAsync(
    string userId,
    string groupReservationId,
    string reservationId)
{
    var group = await groupReservationRepository
        .GetGroupReservationByIdAsync(groupReservationId)
        ?? throw new GroupReservationNotFoundException();

    if (group.ReservationId != reservationId)
{
    throw new ValidationException(
        "Reservation does not belong to this group reservation.");
}

var reservation = await reservationRepository
    .GetReservationByIdAsync(reservationId)
    ?? throw new ReservationNotFoundException(
        $"Reservation '{reservationId}' was not found.");

    if (string.IsNullOrWhiteSpace(reservation.CustomerId) ||
        !string.Equals(
            reservation.CustomerId,
            userId,
            StringComparison.OrdinalIgnoreCase))
    {
        throw new ActionForbiddenException(
            "Only the reservation owner can confirm the group pre-order.");
    }

    if (group.Status == GroupReservationStatus.Submitted)
    {
        throw new GroupReservationSubmittedException();
    }

    if (group.Status == GroupReservationStatus.Closed)
    {
        throw new GroupReservationExpiredException();
    }

    if (reservation.Status != ReservationStatus.Reserved)
    {
        throw new ValidationException(
            "Group pre-order can only be confirmed for a reserved reservation.");
    }

    var sessions = await guestSessionRepository
        .GetAllByGroupReservationIdAsync(groupReservationId);

    // Беремо лише сесії, які учасники вже подали.
    var submittedSessions = sessions
        .Where(session => session.IsSubmitted)
        .ToList();

    if (submittedSessions.Count == 0)
    {
        throw new ValidationException(
            "There are no submitted guest sessions to confirm.");
    }

    // Об'єднуємо однакові страви за DishId.
    var consolidatedDishes = submittedSessions
        .SelectMany(session => session.Dishes)
        .GroupBy(dish => dish.DishId)
        .Select(dishGroup => new OrderItem
        {
            DishId = dishGroup.Key,
            Quantity = dishGroup.Sum(dish => dish.Quantity),
            State = OrderItemState.Pending
        })
        .ToList();

    if (consolidatedDishes.Count == 0)
    {
        throw new ValidationException(
            "There are no dishes in the submitted sessions.");
    }

    var table = await tableRepository.GetTableAsync(reservation.TableId)
        ?? throw new ValidationException(
            "Associated table was not found.");

    var location = await locationService
        .GetLocationByIdAsync(table.LocationId);

    var reservationTime =
        $"{reservation.StartTime:MMM dd, yyyy, h:mm tt} - " +
        $"{reservation.EndTime:h:mm tt}";

    var order = new Order
    {
        OrderId = Guid.NewGuid().ToString(),
        CustomerId = reservation.CustomerId,
        ReservationId = reservation.Id,
        ReservationAddress = location?.Address ?? "Unknown Address",
        ReservationTime = reservationTime,
        Dishes = consolidatedDishes,
        Status = OrderStatus.SubmittedByCustomer
    };

    await orderService.AddOrderAsync(order,submittedByCustomer: true);

    group.Status = GroupReservationStatus.Submitted;

    await groupReservationRepository.SaveGroupReservationAsync(group);

    return consolidatedDishes;
}
}

