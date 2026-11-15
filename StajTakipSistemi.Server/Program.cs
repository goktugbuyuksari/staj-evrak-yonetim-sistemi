using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using StajTakipSistemi.Server.Data;
using StajTakipSistemi.Server.Services;
using StajTakipSistemi.Server.Services.OOP_RuleEngine;

var builder = WebApplication.CreateBuilder(args);

// Data Protection'ı Sandbox uyumlu bellek içi depo ile yapılandır
builder.Services.Configure<KeyManagementOptions>(options =>
{
    options.XmlRepository = new InMemoryXmlRepository();
});

// 1. Veri Tabanı (SQLite)
var connString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=staj_takip.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connString));

// 2. Servis Kayıtları (Dependency Injection)
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddScoped<IDocumentValidationEngine, DocumentValidationEngine>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDatasetImportService, DatasetImportService>();

// 3. Kimlik Doğrulama  & Yetkilendirme (ADMİN ve ÖĞRENCİ rolleri)  
var jwtKey = builder.Configuration["Jwt:Key"] ?? "StajTakipSistemi_SuperSecret_SecurityKey_2026_ProLab3!LongSecretKey";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "StajTakipApi",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "StajTakipClient",
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
            if (authService.IsTokenInvalidated(token))
            {
                context.Fail("Token geçersiz kılınmıştır.");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StudentPolicy", policy => policy.RequireRole("ROLE_STUDENT"));
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("ROLE_ADMIN"));
});

// 4. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();

var app = builder.Build();

// Veritabanını otomatik başlat ve seed et
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(db);
}

app.UseCors("AllowAll");

// Statik Dosyalar (Frontend arayüzü)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SPA yönlendirmesi
app.MapFallbackToFile("index.html");

app.Run();

/// <summary>
/// Sandbox ortamında disk ve registry izin hatalarını önleyen bellek içi anahtar deposu.
/// </summary>
public class InMemoryXmlRepository : IXmlRepository
{
    private readonly List<XElement> _elements = new();
    public IReadOnlyCollection<XElement> GetAllElements() => _elements.AsReadOnly();
    public void StoreElement(XElement element, string friendlyName)
    {
        _elements.Add(new XElement(element));
    }
}
