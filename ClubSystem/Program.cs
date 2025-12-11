using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Net.payOS;
using Repository.Models;
using Repository.Repo.Implements;
using Repository.Repo.Interfaces;
using Service.Service.Implements;
using Service.Service.Interfaces;
using Service.Services;
using Service.Services.Interfaces;
using System.Text;
using System.Threading.RateLimiting;

namespace ClubSystem;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        // DB
        builder.Services.AddDbContext<StudentClubManagementContext>(options =>
            options.UseSqlServer(connectionString));

        // REPOSITORIES
        builder.Services.AddScoped<IClubRepository, ClubRepository>();
        builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
        builder.Services.AddScoped<IAuthRepository, AuthRepository>();
        builder.Services.AddScoped<IClubLeaderRequestRepository, ClubLeaderRequestRepository>();
        builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
        builder.Services.AddScoped<IMembershipRequestRepository, MembershipRequestRepository>();
        builder.Services.AddScoped<IActivityParticipantRepository, ActivityParticipantRepository>();
        builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

        // SERVICES
        builder.Services.AddScoped<IClubService, ClubService>();
        builder.Services.AddScoped<IActivityService, ActivityService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IAuthBusinessService, AuthBusinessService>();
        builder.Services.AddScoped<IClubLeaderRequestService, ClubLeaderRequestService>();
        builder.Services.AddScoped<IAdminAccountService, AdminAccountService>();
        builder.Services.AddScoped<IStudentMembershipService, StudentMembershipService>();
        builder.Services.AddScoped<IClubLeaderMembershipService, ClubLeaderMembershipService>();
        builder.Services.AddScoped<IStudentActivityService, StudentActivityService>();
        builder.Services.AddScoped<IPayOSService, PayOSService>();

        builder.Services.AddSingleton(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();

            var clientId = cfg["PayOSSettings:ClientId"]
                ?? throw new InvalidOperationException("PayOSSettings:ClientId not configured in appsettings.");
            var apiKey = cfg["PayOSSettings:ApiKey"]
                ?? throw new InvalidOperationException("PayOSSettings:ApiKey not configured in appsettings.");
            var checksumKey = cfg["PayOSSettings:ChecksumKey"]
                ?? throw new InvalidOperationException("PayOSSettings:ChecksumKey not configured in appsettings.");

            return new PayOS(clientId, apiKey, checksumKey);
        });


        builder.Services.AddControllers();

        // CORS - read from configuration
        var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new string[0];
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFE", policy =>
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // Rate limiting (simple token bucket)
        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                return RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });
        });

        // SWAGGER
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ClubSystem",
                Version = "v1"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Nhập token theo dạng: Bearer {token}"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // JWT
        var jwt = builder.Configuration.GetSection("Jwt");

        var keyString = jwt["Key"]
            ?? throw new InvalidOperationException("JWT Key not configured in appsettings.");

        var key = Encoding.UTF8.GetBytes(keyString);

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateIssuer = true,
                    ValidateAudience = true,

                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });

        builder.Services.AddAuthorization();


        // APP BUILD
        var app = builder.Build();

        app.UseRateLimiter();

        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<StudentClubManagementContext>();

                string[] baseRoles =
                {
                    "admin",
                    "student",
                    "clubleader"
                };

                foreach (var r in baseRoles)
                {
                    if (!db.Roles.Any(x => x.Name == r))
                        db.Roles.Add(new Role { Name = r });
                }

                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            // If DB is unavailable or migration not applied, don't crash -- log and continue so Swagger can be used for development.
            Console.WriteLine("Warning: failed to initialize DB roles: " + ex.Message);
        }

        // MIDDLEWARE PIPELINE
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCors("AllowFE");

        app.MapControllers();

        app.Run();
    }
}
