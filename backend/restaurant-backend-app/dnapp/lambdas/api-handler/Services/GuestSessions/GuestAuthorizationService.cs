using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using System.Threading.Tasks;

namespace APIHandler.Services.GuestSessions;

public class GuestAuthorizationService(
    IGuestSessionService guestSessionService) : IGuestAuthorizationService
{
    public async ValueTask<APIGatewayProxyResponse?> ValidateAsync(
        APIGatewayProxyRequest request,
        ILambdaContext context,
        string guestSessionId)
    {
        var claims = request.RequestContext?.Authorizer?.Claims;

        // Якщо користувач авторизований у системі,
        // перевіряємо, чи належить йому Owner-сесія.
        if (claims != null &&
            claims.TryGetValue("sub", out var userId) &&
            !string.IsNullOrWhiteSpace(userId))
        {
            bool isOwner = await guestSessionService.IsOwnerSessionAsync(
                userId,
                guestSessionId);

            if (isOwner)
            {
                return null;
            }
        }

        // Якщо це не Owner-сесія, перевіряємо гостьовий токен.
        if (request.Headers == null ||
            !request.Headers.TryGetValue(
                "Authorization",
                out string? authorizationValue) ||
            string.IsNullOrWhiteSpace(authorizationValue))
        {
            return CreateErrorResponse(context);
        }

        const string BearerString = "Bearer ";

        if (!authorizationValue.StartsWith(
                BearerString,
                System.StringComparison.OrdinalIgnoreCase) ||
            authorizationValue.Length <= BearerString.Length)
        {
            return CreateErrorResponse(context);
        }

        string token = authorizationValue[BearerString.Length..].Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            return CreateErrorResponse(context);
        }

        bool authorized = await guestSessionService.IsAuthorizedAsync(
            token,
            guestSessionId);

        return authorized ? null : CreateErrorResponse(context);
    }

    private static APIGatewayProxyResponse CreateErrorResponse(
        ILambdaContext context)
    {
        return ResponseBuilder.CreateErrorResponse(
            401,
            "Invalid guest session token",
            context.Logger);
    }
}