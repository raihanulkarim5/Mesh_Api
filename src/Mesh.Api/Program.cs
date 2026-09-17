using System.Text;
using Mesh.Api.Auth;
using Mesh.Domain.Entities;
using Mesh.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration ----
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// ---- Database ----
builder.Services.AddDbContext<MeshDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Identity ----
// AddIdentity (not AddIdentityCore) so we get SignInManager/RoleManager too -
// standard, complete setup. This internally registers a cookie auth scheme
// as the default, which we deliberately override below with AddAuthentication
// so JWT Bearer is what actually gets used for API requests.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Identity's default password policy is reasonable as-is; only
    // overriding what's worth calling out explicitly.
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // no email verification yet, by design
})
    .AddEntityFrameworkStores<MeshDbContext>()
    .AddDefaultTokenProviders();

// ---- JWT Bearer authentication (overrides Identity's cookie default) ----
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30), // access tokens are short-lived; don't need the 5-minute default slack
    };
});

builder.Services.AddAuthorization();

// ---- CORS ----
// Permissive for now - the frontend runs on a dynamic Codespaces-forwarded
// port during dev. Tighten this to a specific allowed-origins list once
// there's a real hosting URL to lock it down to.
const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// ---- Controllers + Swagger ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Mesh API", Version = "v1" });

    // Lets you paste a JWT into Swagger UI's "Authorize" button to test
    // protected endpoints directly from the docs page.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste just the token - Swagger adds the 'Bearer ' prefix.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(DevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
