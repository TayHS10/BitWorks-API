using GPP_API.Models;
using GPP_API.Services;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<GPP_API.Models.ApplicationDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("StringConexion")));


builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();

// ***********************************************************************************
// ¡AQUÍ ES DONDE DEBES AGREGAR EL REGISTRO PARA IEmailService!
// Asumiendo que tu implementación se llama EmailService y está en GPP_API.Services
builder.Services.AddScoped<IEmailService, EmailService>(); // O AddTransient, o AddSingleton
// ***********************************************************************************

var key = builder.Configuration.GetValue<string>("JwtSettings:Key");
var keyBytes = Encoding.ASCII.GetBytes(key);

builder.Services.AddAuthentication(
 config =>
 {
     config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
     config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
 }).AddJwtBearer(
    config => {
        config.RequireHttpsMetadata = false;
        config.SaveToken = true;
        config.TokenValidationParameters =
        new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();