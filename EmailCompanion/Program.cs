using EmailCompanion.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<AppConfigService>();
builder.Services.AddSingleton<TemplateFileService>();
builder.Services.AddSingleton<TemplateSchemaService>();
builder.Services.AddSingleton<TemplateRenderService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<EmailCompanion.Components.App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
