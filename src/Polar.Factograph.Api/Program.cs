using Polar.Factograph.Api;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});
builder.Services.AddFactographApi(builder.Configuration, builder.Environment);

WebApplication app = builder.Build();
app.UseResponseCompression();
app.MapFactographApi();
app.Run();
