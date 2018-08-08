// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using AspNetCore.DataProtection.SqlServer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using PwC.MTS.Contract.Common;


namespace AspNetCoreSecurity
{
    
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            
            services.AddDataProtection()
                .PersistKeysToSqlServer(Configuration["DataProtection:ConnectionString"], "dbo", "DataProtectionKeys")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(7))
                .SetApplicationName("MVCCORETEST");

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var settings = Configuration.GetSection(typeof(SecuritySettings).Name).Get<SecuritySettings>() ??
                           new SecuritySettings();


            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Cookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("Cookies", options =>
                        {
                            options.AccessDeniedPath = "/account/denied";
                        })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.SignInScheme = "Cookies";
                    if (settings.EnableCorrelationCookieBuilder)
                    {
                        options.CorrelationCookie = new CookieBuilder()
                        {
                            Name = "my_correlation_cookie",
                            HttpOnly = true,
                            SameSite = SameSiteMode.None,
                            SecurePolicy = CookieSecurePolicy.None,
                            Expiration = new TimeSpan(0, 15, 0)
                        };

                        // https://github.com/aspnet/Security/blob/release/2.0/src/Microsoft.AspNetCore.Authentication.OpenIdConnect/OpenIdConnectOptions.cs#L71
                        options.NonceCookie = new CookieBuilder()
                        {
                            Name = "my_nonce_cookie",
                            HttpOnly = true,
                            SameSite = SameSiteMode.None,
                            SecurePolicy = CookieSecurePolicy.None,
                            Expiration = new TimeSpan(0, 15, 0)
                        };
                    }

                    options.Authority = settings.SecurityAuthority;
                    options.RequireHttpsMetadata = false;
                    options.MetadataAddress = settings.SecurityMetadataAddress;
                    options.ClientId = settings.ClientId;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = redirectContext =>
                        {
                            if (!string.IsNullOrEmpty(settings.RedirectUri))
                            {
                                //Force scheme of redirect URI (THE IMPORTANT PART)
                                redirectContext.ProtocolMessage.RedirectUri = settings.RedirectUri;
                                redirectContext.Request.Scheme = "https";
                            }

                            return Task.FromResult(0);
                        }
                    };
                });
            //services.AddAuthentication(options =>
            //{
            //    options.DefaultScheme = "Cookies";
            //    options.DefaultChallengeScheme = "oidc";
            //})
            //    .AddCookie("Cookies", options =>
            //    {
            //        options.AccessDeniedPath = "/account/denied";
            //    })
            //    .AddOpenIdConnect("oidc", options =>
            //    {
            //        options.SignInScheme = "Cookies";

            //        options.Authority = "https://demo.identityserver.io";
            //        options.ClientId = "server.hybrid";
            //        options.ClientSecret = "secret";
            //        options.ResponseType = "code id_token";

            //        options.SaveTokens = true;
            //        options.GetClaimsFromUserInfoEndpoint = true;

            //        options.Scope.Clear();
            //        options.Scope.Add("openid");
            //        options.Scope.Add("profile");
            //        options.Scope.Add("email");
            //        options.Scope.Add("offline_access");
            //        options.Scope.Add("api");

            //        options.ClaimActions.MapAllExcept("iss", "exp", "nbf", "iat", "nonce", "aud", "c_hash", "auth_time");
                    
            //        options.TokenValidationParameters = new TokenValidationParameters
            //        {
            //            NameClaimType = "name", 
            //            RoleClaimType = "role"
            //        };
            //    });

            services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            //Handler that changes the scheme based on the incoming headers. Needed when IdentityServer is hosted behind load balancer or reverse proxy
            var options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                RequireHeaderSymmetry = false
            };

            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            app.UseForwardedHeaders(options);
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseMvcWithDefaultRoute();
        }
    }
}