using LettuceEncrypt.Accounts;
using LettuceEncrypt;
using WeatherApp.LettuceEncrypt;
using WeatherApp.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(8080);
});

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseKestrel(options =>
    {
        var appServices = options.ApplicationServices;
        options.ListenAnyIP(8081, listenOptions => listenOptions.UseHttps(o => o.UseLettuceEncrypt(appServices)));
    });
}
else
{
    builder.WebHost.UseKestrel(options =>
    {
        options.ListenAnyIP(8081, listenOptions => listenOptions.UseHttps());
    });
}

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddLettuceEncrypt(options =>
{
    options.AcceptTermsOfService = true;
    options.DomainNames = new string[] { EnvironmentVariableGetter.Get("DOMAIN_NAME") };
    options.EmailAddress = "email@example.com";
});
builder.Services.AddTransient<Bucket>();
builder.Services.AddTransient<ICertificateRepository, CertificateRepository>();
builder.Services.AddTransient<ICertificateSource, CertificateSource>();
builder.Services.AddTransient<IAccountStore, AccountStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
