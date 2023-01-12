using Microsoft.AspNetCore.Builder;
using WebDavServer.Infrastructure;
using WebDavServer.WebApi;
using WebDavService.Application;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var env = builder.Environment;

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(configuration)
    .AddWebApiServices();

var app = builder.Build();

app
    .UseSwagger()
    .UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CrossTech.WebApi v1");
        c.RoutePrefix = "swagger";
    })
    .UseRouting()
    .UseAuthorization()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });

app.Run();