using ElmahCore;
using ElmahCore.Mvc;
using FoodChainAPI.Infrastructure;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoodChainBrandsAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();

            // configure identity server with in-memory stores, keys, clients and scopes
            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddInMemoryPersistedGrants()
                .AddInMemoryIdentityResources(GetIdentityResources())
                .AddInMemoryApiResources(GetApiResources())
                .AddInMemoryClients(GetClients())
                .AddTestUsers(GetUsers());
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
           .AddCookie()
           .AddJwtBearer(options =>
           {
               // base-address of your identityserver
               options.Authority = "http://localhost:5000/";
               // name of the API resource
               // options.Audience = "http://localhost:53752/";
               options.TokenValidationParameters = new TokenValidationParameters
               {
                   ValidateAudience = false
               };
               options.RequireHttpsMetadata = false;

           });
            services.AddElmah();
            services.AddElmah<XmlFileErrorLog>(options =>
            {
                options.Path = @"elmah";
                options.LogPath = "Logs/elmahlog"; // OR options.LogPath = "с:\errors";
            });
            services.AddTransient<IDatabaseConnectionFactory>(provider => new DatabaseConnectionFactory(Configuration.GetConnectionString("DefaultConnection")));
            services.AddTransient<IFoodChainRepository, FoodChainRepository>();
            services.AddTransient<IFoodChainFranchisesRepository, FoodChainFranchisesRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "FoodChainBrandsAPI", Version = "v1" });
                var security = new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", new string[] { }},
                };

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description =
        "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            },
            Scheme = "oauth2",
            Name = "Bearer",
            In = ParameterLocation.Header,

        },
        new List<string>()
    }
});
            });
            services.Configure<FormOptions>(o =>
            {
                o.ValueLengthLimit = int.MaxValue;
                o.MultipartBodyLengthLimit = int.MaxValue;
                o.MemoryBufferThreshold = int.MaxValue;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FoodChainBrandsAPI v1"));
            }
            else
            {
                app.UseExceptionHandler("/Error/Log");
            }


            app.UseStaticFiles();

            //app.UseStaticFiles(new StaticFileOptions()
            //{
            //    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"Resources")),
            //    RequestPath = new PathString("/Resources")
            //});
            //Add our new middleware to the pipeline

            app.UseRouting();
            app.UseAuthentication();
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = LogHelper.EnrichFromRequest);
            app.UseElmah();
            app.UseCustomExceptionMiddleware();
        }

        /// <summary>
        /// This method is used to get identity resource for identity server.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Email(),
                new IdentityResources.Profile()
            };
        }

        /// <summary>
        /// This method is used to get API resources.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource("api1", "My API")
            };
        }

        /// <summary>
        /// This method is used to get clients.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IEnumerable<Client> GetClients()
        {
            // client credentials client
            return new List<Client>
            {                
                // resource owner password grant client
                new Client
                {
                    ClientId = "ro.angular",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    AllowedScopes = {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.Address,
                        "api1",
                         IdentityServerConstants.StandardScopes.OfflineAccess,
                    },
                    AllowOfflineAccess=true,
                    RefreshTokenUsage=TokenUsage.OneTimeOnly,
                    UpdateAccessTokenClaimsOnRefresh=true,
                    AccessTokenLifetime=3600,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    SlidingRefreshTokenLifetime=7200
                }
            };
        }

        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
         {
             new TestUser
             {
                 SubjectId = "1",
                 Username = "useradmin",
                 Password = "Welcome@123",
                 Claims = new List<Claim>
                 {
                     new Claim("name", "Gurmeet Kaur"),
                     new Claim("id", "1")
                 }
             }
         };
        }
    }
}
