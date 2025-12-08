using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Repository.Models;
using Repository.Repo.Implements;
using Repository.Repo.Interfaces;
using Service.Service.Implements;
using Service.Service.Interfaces;
using Service.Services;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ClubSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<StudentClubManagementContext>(options =>
                options.UseSqlServer(connectionString));
            // Add services to the container.
            //var config = builder.Configuration;

            // Repositories
            builder.Services.AddScoped<IClubRepository, ClubRepository>();
            builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();

            // Services
            builder.Services.AddScoped<IClubService, ClubService>();
            builder.Services.AddScoped<IActivityService, ActivityService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAuthBusinessService, AuthBusinessService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // JWT
            var jwt = builder.Configuration.GetSection("Jwt");
            var keyString = jwt["Key"] ?? throw new InvalidOperationException("JWT Key not configured. Please set Jwt:Key in appsettings.");
            var key = Encoding.UTF8.GetBytes(keyString);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt["Issuer"],
                        ValidAudience = jwt["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(key)
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

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
