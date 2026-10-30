using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models;
using APIHandler.Models.GroupReservation;
using APIHandler.Services.GroupReservations;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GroupReservations;

public class ConfirmGroupPreOrderHandler(
    IGroupReservationService groupReservationService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        if (!request.TryGetPathValue(
                "groupReservationId",
                out string? groupReservationId))
        {
            return ResponseBuilder.CreateErrorResponse(
                400,
                "Missing 'groupReservationId' in path parameters.",
                context.Logger);
        }

        var claims = request.RequestContext?.Authorizer?.Claims;

        if (claims == null ||
            !claims.TryGetValue("sub", out string? userId) ||
            string.IsNullOrWhiteSpace(userId))
        {
            return ResponseBuilder.CreateErrorResponse(
                401,
                "Unauthorized.",
                context.Logger);
        }

        SubmitGroupPreOrderRequest? body;

        try
        {
            body = JsonSerializer.Deserialize<SubmitGroupPreOrderRequest>(
                request.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException ex)
        {
            return ResponseBuilder.CreateErrorResponse(
                400,
                ex.Message,
                context.Logger);
        }

        if (body is null || string.IsNullOrWhiteSpace(body.ReservationId))
        {
            return ResponseBuilder.CreateErrorResponse(
                400,
                "Missing 'reservationId' in request body.",
                context.Logger);
        }

        try
        {
            var result = await groupReservationService.ConfirmGroupPreOrderAsync(
                userId,
                groupReservationId,
                body.ReservationId);

            var response = result.Select(item => new
        {
            dishId = item.DishId,
            quantity = item.Quantity
        }).ToList();

        return ResponseBuilder.CreateSuccessResponse(response);
        }
        catch (GroupReservationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(
                404, ex.Message, context.Logger);
        }
        catch (ReservationNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(
                404, ex.Message, context.Logger);
        }
        catch (ActionForbiddenException ex)
        {
            return ResponseBuilder.CreateErrorResponse(
                403, ex.Message, context.Logger);
        }
        catch (GroupReservationSubmittedException ex)
        {
            return ResponseBuilder.CreateErrorResponse(
                409, ex.Message, context.Logger);
        }
        catch (GroupReservationExpiredException ex)
        {
            return ResponseBuilder.CreateErrorResponse(
                409, ex.Message, context.Logger);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorCodeResponse(ex);
        }
    }
}