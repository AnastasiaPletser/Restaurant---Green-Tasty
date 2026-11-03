using System.Text.Json;
using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.StepDefinitions;

[Binding]
public sealed class GroupReservationStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Given("a customer has created a group reservation for {int} guests")]
    public async Task GivenCustomerHasCreatedGroupReservation(int guestCount)
    {
        var customer = SampleDataFactory.RegisterRequest();

        await Actor.AttemptsToAsync(new RegisterThroughTheService(customer));

        var registrationOutcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(registrationOutcome.Succeeded).IsTrue()
            .Because($"Customer registration failed: {registrationOutcome.Body}");

        await Actor.AttemptsToAsync(
            new SignInThroughTheService(customer.Email, customer.Password));

        var token = await Actor.AsksForAsync(new TheIdToken());
        await Assert.That(token).IsNotNullOrEmpty();

        await Actor.AttemptsToAsync(
            new CreateGroupReservationForCustomer(guestCount, token!));
    }

    [When("the owner views the group participants")]
    public async Task WhenOwnerViewsGroupParticipants()
    {
        var group = Recall<GetGroupReservationResponse>("group-test-reservation-details");
        var token = Recall<string>("group-test-customer-token");

        await Actor.TheLastResponse().CaptureAsync(
            await new GroupReservationsApi(Actor.UsingTheApi())
                .GetAllSessionsAsync(group.GroupReservationId, token));
    }

    [Then("the group contains {int} guest sessions")]
public async Task ThenTheGroupContainsGuestSessions(int expectedCount)
{
    var result = Actor.TheLastResponse().Json<GetAllSessionsResponse>();

    await Assert.That(result.Participants).Count().IsEqualTo(expectedCount);
}

[Then("each guest order total is displayed")]
public async Task ThenEachGuestOrderTotalIsDisplayed()
{
    var result = Actor.TheLastResponse().Json<GetAllSessionsResponse>();

    foreach (var participant in result.Participants)
    {
        var calculatedTotal = participant.Items.Sum(
            item => item.Dish.Price * item.Quantity);

        await Assert.That(participant.Total).IsEqualTo(calculatedTotal);
    }
}

    [Then("the group total is correct")]
    public async Task ThenTheGroupTotalIsCorrect()
    {
        var result = Actor.TheLastResponse().Json<GetAllSessionsResponse>();

        var calculatedGroupTotal = result.Participants.Sum(
            participant => participant.Total);

        await Assert.That(result.Total).IsEqualTo(calculatedGroupTotal);
    }

    [Then("the group contains the owner session")]
    public async Task ThenGroupContainsOwnerSession()
    {
        var participants = Actor.TheLastResponse().Json<GetAllSessionsResponse>().Participants;

        await Assert.That(participants).Count().IsEqualTo(1);
        await Assert.That(participants[0].Type).IsEqualTo("Owner");
    }

    [Then("the group total is {decimal}")]
    public async Task ThenGroupTotalIs(decimal expectedTotal)
    {
        var result = Actor.TheLastResponse().Json<GetAllSessionsResponse>();
        await Assert.That(result.Total).IsEqualTo(expectedTotal);
    }

    [When("the owner retrieves the guest session ID")]
    public async Task WhenOwnerRetrievesGuestSessionId()
    {
        var reservation = Recall<ReservationResponse>("group-test-reservation");
        var token = Recall<string>("group-test-customer-token");

        await Actor.TheLastResponse().CaptureAsync(
            await new GroupReservationsApi(Actor.UsingTheApi())
                .GetOwnerSessionIdAsync(reservation.Id, token));
    }

    [Then("the owner guest session ID is returned")]
    public async Task ThenOwnerGuestSessionIdIsReturned()
    {
        var response = Actor.TheLastResponse();

        await Assert.That(response.Succeeded).IsTrue()
            .Because($"Owner session ID request failed: HTTP {response.Status}: {response.Body}");

        using var json = JsonDocument.Parse(response.Body);
        var guestSessionId = json.RootElement
            .GetProperty("guestSessionId")
            .GetString();

        await Assert.That(guestSessionId).IsNotNullOrEmpty();
    }

    [Given("another customer is signed in")]
    public async Task GivenAnotherCustomerIsSignedIn()
    {
        var customer = SampleDataFactory.RegisterRequest();

        await Actor.AttemptsToAsync(new RegisterThroughTheService(customer));

        var registrationOutcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(registrationOutcome.Succeeded).IsTrue()
            .Because($"Second customer registration failed: {registrationOutcome.Body}");

        await Actor.AttemptsToAsync(
            new SignInThroughTheService(customer.Email, customer.Password));

        var token = await Actor.AsksForAsync(new TheIdToken());
        await Assert.That(token).IsNotNullOrEmpty();

        Actor.Remember("group-test-other-customer-token", token!);
    }

    [When("another customer tries to view the group participants")]
    public async Task WhenAnotherCustomerTriesToViewGroupParticipants()
    {
        var group = Recall<GetGroupReservationResponse>("group-test-reservation-details");
        var token = Recall<string>("group-test-other-customer-token");

        await Actor.TheLastResponse().CaptureAsync(
            await new GroupReservationsApi(Actor.UsingTheApi())
                .GetAllSessionsAsync(group.GroupReservationId, token));
    }

    [Then("the request is forbidden")]
    public async Task ThenTheRequestIsForbidden()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(403);
    }

    [Given("a guest has joined the group")]
    public async Task GivenGuestHasJoinedGroup()
    {
        var group = Recall<GetGroupReservationResponse>("group-test-reservation-details");

        await Actor.TheLastResponse().CaptureAsync(
            await new GroupReservationsApi(Actor.UsingTheApi())
                .CreateGuestSessionAsync(new CreateGuestSessionRequest(
                    group.GroupReservationId,
                    "AQA Test Guest")));

        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsTrue()
            .Because($"Guest session creation failed: HTTP {outcome.Status}: {outcome.Body}");

        var guest = Actor.TheLastResponse().Json<CreateGuestSessionResponse>();
        Actor.Remember("group-test-guest-session-id", guest.SessionId);
        Actor.Remember("group-test-guest-session-token", guest.SessionToken);
    }

    [When("the owner deletes the guest")]
    public async Task WhenOwnerDeletesGuest()
    {
        var group = Recall<GetGroupReservationResponse>("group-test-reservation-details");
        var guestSessionId = Recall<string>("group-test-guest-session-id");
        var token = Recall<string>("group-test-customer-token");

        await Actor.TheLastResponse().CaptureAsync(
            await new GroupReservationsApi(Actor.UsingTheApi())
                .DeleteGuestSessionAsync(group.GroupReservationId, guestSessionId, token));
    }

    [Then("only the owner remains in the group")]
    public async Task ThenOnlyOwnerRemainsInGroup()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsTrue()
            .Because($"Deleting guest failed: HTTP {outcome.Status}: {outcome.Body}");

        var result = Actor.TheLastResponse().Json<GetAllSessionsResponse>();
        await Assert.That(result.Participants).Count().IsEqualTo(1).Because($"Delete response: {Actor.TheLastResponse().Body}");
        await Assert.That(result.Participants[0].Type).IsEqualTo("Owner");
        await Assert.That(result.Total).IsEqualTo(0m);
    }
        [Then("only {int} guest sessions remain in the group")]
    public async Task ThenOnlyGuestSessionsRemainInTheGroup(int expectedCount)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Succeeded).IsTrue()
            .Because($"Deleting guest failed: HTTP {outcome.Status}: {outcome.Body}");

        var result = Actor.TheLastResponse().Json<GetAllSessionsResponse>();

        await Assert.That(result.Participants).Count().IsEqualTo(expectedCount);
    }

        [Then("the group total is recalculated")]
    public async Task ThenTheGroupTotalIsRecalculated()
    {
        var result = Actor.TheLastResponse().Json<GetAllSessionsResponse>();

        var calculatedTotal = result.Participants.Sum(
            participant => participant.Total);

        await Assert.That(result.Total).IsEqualTo(calculatedTotal);
    }

    [Given("two guests have joined the group")]
public async Task GivenTwoGuestsHaveJoinedGroup()
{
    var group = Recall<GetGroupReservationResponse>("group-test-reservation-details");
    var api = new GroupReservationsApi(Actor.UsingTheApi());

    var firstResponse = await api.CreateGuestSessionAsync(
        new CreateGuestSessionRequest(group.GroupReservationId, "AQA Guest 1"));

    await Actor.TheLastResponse().CaptureAsync(firstResponse);
    var firstOutcome = await Actor.AsksForAsync(new TheOutcome());

    await Assert.That(firstOutcome.Succeeded).IsTrue()
        .Because($"First guest creation failed: {firstOutcome.Body}");

    var firstGuest = Actor.TheLastResponse().Json<CreateGuestSessionResponse>();
    Actor.Remember("group-test-guest-1-id", firstGuest.SessionId);
    Actor.Remember("group-test-guest-1-token", firstGuest.SessionToken);

    var secondResponse = await api.CreateGuestSessionAsync(
        new CreateGuestSessionRequest(group.GroupReservationId, "AQA Guest 2"));

    await Actor.TheLastResponse().CaptureAsync(secondResponse);
    var secondOutcome = await Actor.AsksForAsync(new TheOutcome());

    await Assert.That(secondOutcome.Succeeded).IsTrue()
        .Because($"Second guest creation failed: {secondOutcome.Body}");

    var secondGuest = Actor.TheLastResponse().Json<CreateGuestSessionResponse>();
    Actor.Remember("group-test-guest-2-id", secondGuest.SessionId);
    Actor.Remember("group-test-guest-2-token", secondGuest.SessionToken);
}

[When("the owner deletes one guest")]
public async Task WhenOwnerDeletesOneGuest()
{
    var group = Recall<GetGroupReservationResponse>("group-test-reservation-details");
    var guestSessionId = Recall<string>("group-test-guest-1-id");
    var token = Recall<string>("group-test-customer-token");

    await Actor.TheLastResponse().CaptureAsync(
        await new GroupReservationsApi(Actor.UsingTheApi())
            .DeleteGuestSessionAsync(group.GroupReservationId, guestSessionId, token));
}

[When("both guests submit the same dish with quantities 2 and 3")]
public async Task WhenBothGuestsSubmitSameDish()
{
    var dishesApi = new DishesApi(Actor.UsingTheApi());

    await Actor.TheLastResponse().CaptureAsync(
        await dishesApi.GetDishesAsync());

    var dishes = Actor.TheLastResponse().Json<List<DishListResponse>>();

    await Assert.That(dishes).Count().IsGreaterThan(0)
        .Because("No dishes are available for the group pre-order test.");

    var dishId = dishes[0].Id;
    var api = new GroupReservationsApi(Actor.UsingTheApi());

    var firstId = Recall<string>("group-test-guest-1-id");
    var firstToken = Recall<string>("group-test-guest-1-token");

    await Actor.TheLastResponse().CaptureAsync(
        await api.AddDishToGuestSessionAsync(firstId, dishId, 2, firstToken));

    var firstAdd = await Actor.AsksForAsync(new TheOutcome());
    await Assert.That(firstAdd.Succeeded).IsTrue()
        .Because($"First guest could not add dish: {firstAdd.Body}");

    await Actor.TheLastResponse().CaptureAsync(
        await api.SubmitGuestCartAsync(firstId, firstToken));

    var firstSubmit = await Actor.AsksForAsync(new TheOutcome());
    await Assert.That(firstSubmit.Succeeded).IsTrue()
        .Because($"First guest could not submit cart: {firstSubmit.Body}");

    var secondId = Recall<string>("group-test-guest-2-id");
    var secondToken = Recall<string>("group-test-guest-2-token");

    await Actor.TheLastResponse().CaptureAsync(
        await api.AddDishToGuestSessionAsync(secondId, dishId, 3, secondToken));

    var secondAdd = await Actor.AsksForAsync(new TheOutcome());
    await Assert.That(secondAdd.Succeeded).IsTrue()
        .Because($"Second guest could not add dish: {secondAdd.Body}");

    await Actor.TheLastResponse().CaptureAsync(
        await api.SubmitGuestCartAsync(secondId, secondToken));

    var secondSubmit = await Actor.AsksForAsync(new TheOutcome());
    await Assert.That(secondSubmit.Succeeded).IsTrue()
        .Because($"Second guest could not submit cart: {secondSubmit.Body}");

    Actor.Remember("group-test-merged-dish-id", dishId);
}
[When("the owner confirms the group pre-order")]
public async Task WhenOwnerConfirmsGroupPreOrder()
{
    var group = Recall<GetGroupReservationResponse>("group-test-reservation-details");
    var reservation = Recall<ReservationResponse>("group-test-reservation");
    var token = Recall<string>("group-test-customer-token");

    await Actor.TheLastResponse().CaptureAsync(
        await new GroupReservationsApi(Actor.UsingTheApi())
            .ConfirmGroupPreOrderAsync(
                group.GroupReservationId,
                reservation.Id,
                token));
}

[Then("the group pre-order contains one merged dish with quantity 5")]
public async Task ThenGroupPreOrderContainsOneMergedDish()
{
    var outcome = await Actor.AsksForAsync(new TheOutcome());

    await Assert.That(outcome.Succeeded).IsTrue()
        .Because($"Group pre-order confirmation failed: HTTP {outcome.Status}: {outcome.Body}");

    using var json = JsonDocument.Parse(Actor.TheLastResponse().Body);
    var items = json.RootElement;

    await Assert.That(items.GetArrayLength()).IsEqualTo(1);

    var item = items[0];
    var expectedDishId = Recall<string>("group-test-merged-dish-id");

    await Assert.That(item.GetProperty("dishId").GetString())
        .IsEqualTo(expectedDishId);

    await Assert.That(item.GetProperty("quantity").GetInt32())
        .IsEqualTo(5);
}
[Then("the group pre-order has the submitted status")]
public async Task ThenGroupPreOrderHasSubmittedStatus()
{
    var group = Recall<GetGroupReservationResponse>("group-test-reservation-details");
    var token = Recall<string>("group-test-customer-token");

    await Actor.TheLastResponse().CaptureAsync(
        await new GroupReservationsApi(Actor.UsingTheApi())
            .GetGroupReservationStatusAsync(group.GroupReservationId, token));

    var outcome = await Actor.AsksForAsync(new TheOutcome());

    await Assert.That(outcome.Succeeded).IsTrue()
        .Because($"Could not get group reservation status: HTTP {outcome.Status}: {outcome.Body}");

    using var json = JsonDocument.Parse(Actor.TheLastResponse().Body);

    await Assert.That(json.RootElement.GetProperty("status").GetString())
        .IsEqualTo("Submitted");
}
}