using Polar.Factograph.Api;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddFactographApi(builder.Configuration);

WebApplication app = builder.Build();
app.MapFactographApi();
app.Run();
