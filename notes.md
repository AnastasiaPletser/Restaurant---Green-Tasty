Практика:

Реалізувати АРІ для отримання списку офіціантів певної локації

GET /locations/(locationId)/waiters

Response:

[
    {
"userId": "user1",
"locationId": "location1"
},
{
"userId": "user2",
"Locationid": "location1™
}
]

---------------

1. create location
POST /locations

{
"address": "15 Freedom Square, Tbilisit",
"description": "Cozy family-owned bistro specializing in traditional 
Georgian cuisine with live folk music on weekends.",
"averageOccupancy": "72%"
}

2. register user
POST /register
{
"firstName": "Waiter",
"lastName": "Test",
"email": "waiter.test.001@example.com",
"password": "StrongPass123!",
"confirmPassword": "StrongPass123!"
}

3. Додати користувача в waiters для потрібної локації
POST /waiters
{
"email": "waiter.test.001@exanple.com",
"locationId": "b0defЗЗd-2f00-41ba-b9ad-174c34äbcd29"
}

4. GET /Locations/(locationId)/waiters

---------

dev notes:

"/locations/(id)/waiters": {
"GET": {
"authorization_type": "NONE",
"integration_type": "lambda",
"lambda_name": "api-handller",
"enable proxy"= true,
"default_error_pattern": true
    }
}

new RouteHandler(new Route(HttpMethods,GET, "/locations/(locationid)/waiters"), Activator 
Create Instance<GetLocationWaitersHandler>(provider))


using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Services.Waiters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Waiters
{
    public class GetLocationWaitersHandler
    {
        private readonly IWaiterService _waiterService;

        public GetLocationWaitersHandler(IWaiterService waiterService)
        {
            _waiterService = waiterService
                ?? throw new ArgumentNullException(nameof(waiterService));
        }

        public async Task<APIGatewayProxyResponse> HandleRequest(
            APIGatewayProxyRequest request,
            ILambdaContext context)
        {
            string? locationId = null;

            var hasLocationId =
                request.PathParameters != null &&
                (
                    (request.PathParameters.TryGetValue(
                        "locationId", out locationId)
                     && !string.IsNullOrWhiteSpace(locationId))
                    ||
                    (request.PathParameters.TryGetValue(
                        "id", out locationId)
                     && !string.IsNullOrWhiteSpace(locationId))
                );

            if (!hasLocationId)
            {
                return ResponseBuilder.CreateErrorResponse(
                    400,
                    "Missing 'locationId' in path parameters.",
                    context.Logger);
            }

            var waiters = await _waiterService
                .GetWaitersByLocationIdAsync(locationId!);

            var response = waiters
                .Select(waiter => new
                {
                    userId = waiter.UserId,
                    locationId = waiter.LocationId
                })
                .ToList();

            return ResponseBuilder.CreateSuccessResponse(response);
        }
    }
}
