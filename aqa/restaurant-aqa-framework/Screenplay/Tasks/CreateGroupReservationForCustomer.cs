using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Tables;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Configuration;
using Restaurant_Aqa_Framework.Contexts;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class CreateGroupReservationForCustomer(int guestCount, string customerToken) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var tableId = Settings.Current.TestTableId;

        if (string.IsNullOrWhiteSpace(tableId))
            throw new InvalidOperationException(
                "TESTTABLE_ID is not configured in .env. Set it to a dedicated test table ID.");

        var tablesApi = new TablesApi(actor.UsingTheApi());
        var response = actor.TheLastResponse();

        TimeSlotResponse? availableSlot = null;

        for (var day = 1; day <= 7 && availableSlot is null; day++)
        {
            await response.CaptureAsync(
                await tablesApi.GetAvailableTimeSlotsAsync(tableId, DateTime.UtcNow.Date.AddDays(day)));

            if (!response.Succeeded)
                throw new InvalidOperationException(
                    $"Could not get available time slots. HTTP {response.Status}: {response.Body}");

            availableSlot = response.Json<List<TimeSlotResponse>>()
                .FirstOrDefault(slot => slot.Start > DateTime.UtcNow);
        }

        if (availableSlot is null)
            throw new InvalidOperationException(
                $"No available time slots found for test table {tableId} in the next 7 days.");

        var reservationsApi = new ReservationsApi(actor.UsingTheApi());
        var request = SampleDataFactory.CreateReservationRequest(
            tableId, availableSlot.Start, guestCount);

        await response.CaptureAsync(
            await reservationsApi.CreateReservationAsync(request, customerToken));

        if (!response.Succeeded)
            throw new InvalidOperationException(
                $"Could not create test reservation. HTTP {response.Status}: {response.Body}");

        var reservation = response.Json<ReservationResponse>();
        actor.Remember("group-test-reservation", reservation);

        actor.Recall<CleanupScope>().TrackDelete(
            $"reservation:{reservation.Id}",
            () => reservationsApi.CancelReservationAsync(reservation.Id, customerToken));

        var groupApi = new GroupReservationsApi(actor.UsingTheApi());

        await response.CaptureAsync(
            await groupApi.GetInviteTokenAsync(reservation.Id, customerToken));

        if (!response.Succeeded)
            throw new InvalidOperationException(
                $"Could not get group invite token. HTTP {response.Status}: {response.Body}");

        var invite = response.Json<GetGroupInviteTokenResponse>();

        await response.CaptureAsync(
            await groupApi.GetGroupReservationByTokenAsync(invite.Token));

        if (!response.Succeeded)
            throw new InvalidOperationException(
                $"Could not get group reservation. HTTP {response.Status}: {response.Body}");

        var group = response.Json<GetGroupReservationResponse>();
        actor.Remember("group-test-reservation-details", group);
        actor.Remember("group-test-customer-token", customerToken);
    }
}
