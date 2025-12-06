using Microsoft.AspNetCore.Mvc;
using Wuno.Api.Controllers;

public sealed class HealthControllerTests
{
    [Fact]
    public void Live_ReturnsOk()
    {
        var controller = new HealthController();

        var result = controller.Live();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value?.GetType().GetProperty("ok")?.GetValue(ok.Value));
    }

    [Fact]
    public void Ready_ReturnsOk()
    {
        var controller = new HealthController();

        var result = controller.Ready();

        Assert.IsType<OkObjectResult>(result);
    }
}
