using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Services.GuestSessions;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace APIHandler.Tests.Services;

public class GuestAuthorizationServiceTests
{
    private readonly Mock<IGuestSessionService> _guestSessionServiceMock = new();
    private readonly Mock<ILambdaContext> _lambdaContextMock = new();
    private readonly Mock<ILambdaLogger> _lambdaLoggerMock = new();

    private readonly GuestAuthorizationService _service;

    public GuestAuthorizationServiceTests()
    {
        _lambdaContextMock
            .SetupGet(context => context.Logger)
            .Returns(_lambdaLoggerMock.Object);

        _service = new GuestAuthorizationService(
            _guestSessionServiceMock.Object);
    }

    [Fact]
    public async Task ValidateAsync_OwnerSession_ReturnsNull()
    {
        const string userId = "owner-123";
        const string sessionId = "owner-session-123";

        _guestSessionServiceMock
            .Setup(service => service.IsOwnerSessionAsync(userId, sessionId))
            .ReturnsAsync(true);

        var request = CreateRequest(userId, null);

        var result = await _service.ValidateAsync(
            request,
            _lambdaContextMock.Object,
            sessionId);

        Assert.Null(result);

        _guestSessionServiceMock.Verify(
            service => service.IsOwnerSessionAsync(userId, sessionId),
            Times.Once);

        _guestSessionServiceMock.Verify(
            service => service.IsAuthorizedAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_ValidGuestToken_AllowsGuestWithUserClaims()
    {
        const string userId = "logged-in-user";
        const string sessionId = "guest-session-456";
        const string token = "guest-token";

        _guestSessionServiceMock
            .Setup(service => service.IsOwnerSessionAsync(userId, sessionId))
            .ReturnsAsync(false);

        _guestSessionServiceMock
            .Setup(service => service.IsAuthorizedAsync(token, sessionId))
            .ReturnsAsync(true);

        var request = CreateRequest(userId, $"Bearer {token}");

        var result = await _service.ValidateAsync(
            request,
            _lambdaContextMock.Object,
            sessionId);

        Assert.Null(result);

        _guestSessionServiceMock.Verify(
            service => service.IsAuthorizedAsync(token, sessionId),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_InvalidGuestToken_ReturnsUnauthorized()
    {
        const string userId = "logged-in-user";
        const string sessionId = "guest-session-789";
        const string token = "invalid-token";

        _guestSessionServiceMock
            .Setup(service => service.IsOwnerSessionAsync(userId, sessionId))
            .ReturnsAsync(false);

        _guestSessionServiceMock
            .Setup(service => service.IsAuthorizedAsync(token, sessionId))
            .ReturnsAsync(false);

        var request = CreateRequest(userId, $"Bearer {token}");

        var result = await _service.ValidateAsync(
            request,
            _lambdaContextMock.Object,
            sessionId);

        Assert.NotNull(result);
        Assert.Equal(401, result.StatusCode);
    }

    private static APIGatewayProxyRequest CreateRequest(
        string? userId,
        string? authorizationHeader)
    {
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>()
        };

        if (authorizationHeader is not null)
        {
            request.Headers["Authorization"] = authorizationHeader;
        }

        if (userId is not null)
        {
            request.RequestContext =
                new APIGatewayProxyRequest.ProxyRequestContext
                {
                    Authorizer = new APIGatewayCustomAuthorizerContext
                    {
                        Claims = new Dictionary<string, string>
                        {
                            ["sub"] = userId
                        }
                    }
                };
        }

        return request;
    }
}