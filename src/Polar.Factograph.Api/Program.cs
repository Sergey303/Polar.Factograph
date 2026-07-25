using Polar.Factograph.Api;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddFactographApi();

WebApplication app = builder.Build();
app.MapFactographApi();
app.Run();
