using Polar.Factograph.Api;
using Polar.Factograph.Api.Authentication;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddFactographApi(builder.Configuration, builder.Environment);

WebApplication app = builder.Build();
app.UseMiddleware<AuthenticationStorageExceptionMiddleware>();
app.MapFactographApi();
app.Run();
