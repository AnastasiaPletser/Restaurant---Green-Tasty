using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models;
using APIHandler.Services.GroupReservations;
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

        try
        {
            await groupReservationService.ConfirmGroupPreOrderAsync(
                userId,
                groupReservationId);

            return ResponseBuilder.CreateSuccessResponse(
                new SuccessResponse(
                    "Group pre-order confirmed successfully."));
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
        catch (GuestSessionNotFoundException ex)
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