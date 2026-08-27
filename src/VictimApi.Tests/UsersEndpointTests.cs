using Microsoft.AspNetCore.Http;
using VictimApi;
using Xunit;

namespace VictimApi.Tests;

public class UsersEndpointTests
{
    [Fact]
    public void ReturnsOkForKnownUser()
    {
        var service = new UserService();

        var result = UsersEndpoint.HandleGetUser(service, "1");

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(200, statusResult.StatusCode);
    }

    [Fact]
    public void ReturnsFiveHundredForUnknownUser_ThisIsTheBugTheIssueReports()
    {
        var service = new UserService();

        var result = UsersEndpoint.HandleGetUser(service, "999");

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
