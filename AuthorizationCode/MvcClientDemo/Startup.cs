using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MvcClientDemo
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
            services.AddControllersWithViews();
     
            //JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
            // ȥ��ӳ�䣬����Jwtԭ�е�Claim����
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(options =>
            {
                // ʹ��Cookies 
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; //Cookies
                // ʹ��OpenID Connect 
                options.DefaultChallengeScheme = "oidc";//OpenIdConnectDefaults.AuthenticationScheme; //oidc
            }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)//Cookies
            .AddOpenIdConnect("oidc", options =>//oidc
            {
                options.SignInScheme = "Cookies";
                options.ClientId = "MvcClient";
                // �ͻ�������
                options.ClientSecret = "codetest_secret";
                // ��Ȩ��������ַ
                options.Authority = "http://localhost:6100";
                // ������Ȩ��
                options.ResponseType = "code";
                // ��ʹ��https
                options.RequireHttpsMetadata = false;
                // ����Token�����Cookies��
                options.SaveTokens = true;

                //��Ҫ��Ȩ�޷�Χ
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                // �����ȡˢ��Token
                options.Scope.Add("offline_access");

            });

            //services.Configure<CookiePolicyOptions>(options =>
            //{
            //    //https://docs.microsoft.com/zh-cn/aspnet/core/security/samesite?view=aspnetcore-3.1&viewFallbackFrom=aspnetcore-3
            //    options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Lax;

            //});
            //services.ConfigureNonBreakingSameSiteCookies();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();
            //app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
