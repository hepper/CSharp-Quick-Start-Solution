using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SampleSolution.Core.Repositories.IRepositories.Base;
using SampleSolution.ServerCore.DbContexts;
using SampleSolution.ServerCore.DBContexts;
using SampleSolution.ServerCore.IServices.Base;
using SampleSolution.ServerCore.Repositories.Base;
using SampleSolution.ServerCore.Services;
using SampleSolution.ServerCore.Services.Base;
using SampleSolution.ServerCore.Services.IServices;

#if DEBUG

using Swashbuckle.AspNetCore.Swagger;

#endif

namespace SampleSolution.Backend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddDbContext<MainDbContext>(options =>
            {
                // options.UseInMemoryDatabase("sample_solution_main");
                // options.UseInMemoryDatabase(this.Configuration.GetConnectionString("MainDBConnection"));
                options.UseNpgsql(this.Configuration.GetConnectionString("MainDbConnection"));
                // options.UseSqlServer(this.Configuration.GetConnectionString("MainDBConnection"));
            });

            services.AddDbContext<AuthDbContext>(options =>
            {
                // options.UseInMemoryDatabase("sample_solution_auth");
                // options.UseInMemoryDatabase(this.Configuration.GetConnectionString("AuthDBConnection"));
                options.UseNpgsql(this.Configuration.GetConnectionString("AuthDbConnection"));
                // options.UseSqlServer(this.Configuration.GetConnectionString("AuthDBConnection"));
            });

            services.AddIdentity<AuthUser, IdentityRole>()
                .AddEntityFrameworkStores<AuthDbContext>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSenderService, EmailSenderService>();

            services.AddScoped(typeof(ICrudDbRepository<,,>), typeof(CrudDbRepository<,,>));
            services.AddScoped(typeof(ICrudDbService<,,>), typeof(CrudDbService<,,>));

#if DEBUG
            services.AddCors();

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(
                    "v1",
                    new Info
                    {
                        Title = "SampleSolution Backend",
                        Version = "v1"
                    }
                );
            });
#endif

            // Add framework services.
            services
                .AddMvc(mvcOptions =>
                {
                })
                .AddJsonOptions((options) =>
                {
                    // options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    // options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                    // options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                    // options.SerializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // previos for global
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Local,
                // ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            // intialize DBs
            MainDbContext mainDBContext = serviceProvider.GetService<MainDbContext>();
            mainDBContext.Initialize();

            AuthDbContext authDbContext = serviceProvider.GetService<AuthDbContext>();
            UserManager<AuthUser> userManager = serviceProvider.GetService<UserManager<AuthUser>>();
            authDbContext.Initialize(userManager);

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                // app.UseExceptionHandler("/Home/Error"); // todo: make nice error page
                app.UseHsts();
            }

#if DEBUG
            // Shows UseCors with CorsPolicyBuilder. // todo: wrap in developer environment only
            app.UseCors(builder =>
               builder.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
            );

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(); // https://docs.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger?tabs=visual-studio

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SampleSolution Backend API V1");
            });
#endif

            app.UseHttpsRedirection();
            // app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc();
        }
    }
}