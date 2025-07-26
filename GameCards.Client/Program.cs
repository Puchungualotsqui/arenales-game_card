using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GameCards.Client;
using GameCards.Client.Extras;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5026/") // ✅ backend port
});
builder.Services.AddScoped<SignalRGameService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<UserProfileService>();


await builder.Build().RunAsync();
