using Identity.Application.Identity.Interfaces;
using Identity.Application.Identity.Models;
using Identity.Application.Identity.Services;
using Identity.Application.IdentityService;
using Identity.Data;
using Identity.Data.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApp.Application.Cart.Interfaces;
using MyApp.Application.Cart.Services;
using MyApp.Application.Product.Interfaces;
using MyApp.Application.Product.Services;
using MyApp.Application.Shared.Interfaces;
using MyApp.Application.Shared.Services;
using System.Text;
using System.Threading.RateLimiting;

namespace Identity.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();



            var defaultConnection = builder.Configuration.GetConnectionString("Default");

            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(defaultConnection));

            //Updated
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders(); //important

            //---------------------
            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

                options.SignIn.RequireConfirmedPhoneNumber = true;
                options.SignIn.RequireConfirmedEmail = true;

                options.Tokens.EmailConfirmationTokenProvider = "Default";
            });






            // Configure SmtpSettings
            builder.Services.Configure<SmtpSettings>(
                builder.Configuration.GetSection("SmtpSettings")
            );

            // Register Email Service
            builder.Services.AddScoped<IEmailService, EmailService>();





            // Register Services
            builder.Services.AddScoped<IIdentityService, IdentityService>();
            builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
            builder.Services.AddScoped<IProudctCategoryService, ProudctCategoryService>();
            builder.Services.AddScoped<ISubCategoryService, SubCategoryService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddHttpContextAccessor();
            //enabling access to the current HTTP context anywhere in your application
            //because we need to access it from the application project to generate image URLs

            builder.Services.AddScoped<IFileUrlService, FileUrlService>();


            // Configure JWT Authentication ------------------------------------
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
                };
            });

            builder.Services.AddAuthorization();

            //---------------------------------
            // Rate Limit
            //---------------------------------


            builder.Services.AddRateLimiter(options =>
            {
                // Global rejection behavior
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // -----------------------------
                // Public endpoints --> per IP
                // -----------------------------
                options.AddPolicy("PublicPerIp", httpContext =>
                {
                    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ip,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 4,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0//Allows 0 requests to queue when limit is reached
                            /*
                             * ex QueueLimit = 2

                              Time 00:00 - 4 tokens available
                                Request #1 → Uses token 1 (3 left) Immediate
                                Request #2 → Uses token 2 (2 left) Immediate
                                Request #3 → Uses token 3 (1 left) Immediate
                                Request #4 → Uses token 4 (0 left) Immediate

                             Time 00:01 - NO tokens available (waiting for replenishment)
                                 Request #5 → Enters queue position 1  Waits
                                 Request #6 → Enters queue position 2  Waits
                                 Request #7 → REJECTED immediately (429 Too Many Requests)
                            */
                        });
                });

                // ----------------------------------
                // Authorized endpoints --> per USER
                // ----------------------------------
                options.AddPolicy("PerUser", httpContext =>
                {
                    var userId =
                        httpContext.User?.Identity?.IsAuthenticated == true
                            ? httpContext.User.Identity.Name!
                            : "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: userId,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 4,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0//Allows 0 requests to queue when limit is reached

                        });
                });
            });

            //------------------------



            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseStaticFiles(); // Serves files from wwwroot

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.UseHttpsRedirection();

            //---------------------------------------------------
            app.UseAuthentication(); // This must come BEFORE UseAuthorization
            app.UseAuthorization();
            //---------------------------------------------------

            app.UseRateLimiter();

            app.MapControllers();

            app.Run();
        }
    }
}