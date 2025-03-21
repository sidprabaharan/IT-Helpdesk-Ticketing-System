using HelpdeskTicketing.Core.Models;
using HelpdeskTicketing.Infrastructure.Data;
using HelpdeskTicketing.Infrastructure.Repositories;
using HelpdeskTicketing.Infrastructure.Repositories.Interfaces;
using HelpdeskTicketing.Infrastructure.Services;
using HelpdeskTicketing.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace HelpdeskTicketing.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Configure cookie settings
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
            });

            // Configure settings
            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));
            services.Configure<SecuritySettings>(Configuration.GetSection("SecuritySettings"));

            // Register services
            services.AddScoped<ITicketRepository, TicketRepository>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ISecurityService, SecurityService>();

            // Add HTTP clients
            services.AddHttpClient("GeoIpApi", client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "IT-Helpdesk-Ticketing-System");
            });

            // Add MVC and API controllers
            services.AddControllersWithViews();
            services.AddRazorPages();

            // Configure API settings
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "IT Helpdesk Ticketing API",
                    Version = "v1",
                    Description = "REST API for IT Helpdesk Ticketing System",
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Helpdesk Ticketing API V1");
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Dashboard}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
