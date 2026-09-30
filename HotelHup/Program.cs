using System.Reflection;
using System.Text;
using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.implementation;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.Data;
using HotelHup.INFRASTRUCTURE.Context;
using HotelHup.INFRASTRUCTURE.repos;
using HotelHup.INFRASTRUCTURE.services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HotelHup;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString =
            builder.Configuration.GetConnectionString("HotelHup")
            ?? throw new InvalidOperationException(
                "Connection string 'HotelHup' not found.");

        var jwtKey =
            builder.Configuration["JWT:Key"]
            ?? throw new InvalidOperationException(
                "JWT:Key not found.");

        builder.Services.AddDbContext<hotelhupContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddHttpContextAccessor();

        builder.Services
            .AddIdentity<User, Role>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<hotelhupContext>()
            .AddDefaultTokenProviders();

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["JWT:Issuer"],

                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["JWT:Audience"],

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
            });

        /*
         * Register every policy used by:
         *
         * [Authorize(Policy = "Property.Create")]
         * [Authorize(Policy = "Service.Read")]
         * [Authorize(Policy = "Folio.AddCharge")]
         *
         * This prevents the:
         *
         * The AuthorizationPolicy named ... was not found
         *
         * exception.
         *
         * The Application Services still perform the actual
         * role, permission, and property-scope validation.
         */
        builder.Services.AddAuthorization(options =>
        {
            AddPermissionPolicies(options);
        });

        // Repositories
        builder.Services.AddScoped<IAuthRepository, AuthRepository>();
        builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();

        builder.Services.AddScoped<
            IPropertyCancellationRepo,
            PropertycancellationRepo>();

        builder.Services.AddScoped<
            IPropertyDepositRepo,
            PropertyDepositRepo>();

        builder.Services.AddScoped<IPropertyTaxRepo, PropertyTaxRepo>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
         builder.Services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
        builder.Services.AddScoped<IRatePlanRepository, RatePlanRepository>();
        builder.Services.AddScoped<IFolioRepository, FolioRepository>();
        builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
        // Application Services
        builder.Services.AddScoped<IAuthService, AuthService>();

        builder.Services.AddScoped<
            ICancellationPolicyService,
            Cancellationpolicyservice>();

        builder.Services.AddScoped<
            IPropertyDepositService,
            propertydepositservicecs>();

        builder.Services.AddScoped<IPropertyService, PropertyService>();
        builder.Services.AddScoped<IPropertyTax, PropertyTax>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IUserService, UserService>();
         builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
        builder.Services.AddScoped<IRatePlanService, RatePlanService>();
        builder.Services.AddScoped<IFolioService, FolioService>();
        builder.Services.AddScoped<IServiceService, ServiceService>();
        builder.Services.AddCors(options =>
            options.AddPolicy(
                "AngularPolicy",
                policy =>
                    policy
                        .WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()));

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "HotelHup API",
                    Version = "v1"
                });

            options.AddSecurityDefinition(
                "Bearer",
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter your JWT token."
                });

            options.AddSecurityRequirement(
                new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
                });
        });
        var app = builder.Build();

        /*
         * Seeder يعمل افتراضيًا في Development فقط.
         * ويمكن تفعيله من appsettings باستخدام:
         *
         * "Seed": {
         *   "Enabled": true
         * }
         */
        var seedEnabled = builder.Configuration.GetValue(
            "Seed:Enabled",
            builder.Environment.IsDevelopment());

        if (seedEnabled)
        {
            DatabaseSeeder.SeedAsync(app.Services)
                .GetAwaiter()
                .GetResult();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("AngularPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }

    private static void AddPermissionPolicies(
        AuthorizationOptions options)
    {
        var permissionNames =
            typeof(Permissions)
                .GetNestedTypes(BindingFlags.Public)
                .SelectMany(resourceType =>
                    resourceType
                        .GetFields(
                            BindingFlags.Public |
                            BindingFlags.Static)
                        .Where(field =>
                            field.FieldType == typeof(string))
                        .Select(field =>
                            field.GetValue(null) as string))
                .Where(permission =>
                    !string.IsNullOrWhiteSpace(permission))
                .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var permissionName in permissionNames)
        {
            options.AddPolicy(
                permissionName!,
                policy =>
                    policy.RequireAuthenticatedUser());
        }
    }
}
