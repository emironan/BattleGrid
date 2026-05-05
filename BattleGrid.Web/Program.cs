using BattleGrid.Web.Services;
using BattleGrid.Web.Components;
using BattleGrid.Web.Components.Layout;
using BattleGrid.Web.Components.Pages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Components/Pages";
});
builder.Services.AddServerSideBlazor();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:4744";

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<ApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapBlazorHub();

app.MapFallbackToPage("/_Host");

app.Run();
