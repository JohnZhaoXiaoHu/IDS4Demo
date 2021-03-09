using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AuthorizationServerDemo.Data;
using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuthorizationServerDemo
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
            // ���ݿ������ַ�����������ʾ��д�����ˣ���Ŀ��һ����������ļ��������õ�����localdb 
            string strConn = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=IdentityServer4DB;
                    Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;
                    ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            // ָ��Ǩ�Ƶĳ��򼯣�����ָ��Startup�����ڵĳ���ΪǨ�Ƴ���
            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(strConn));
            services.AddIdentity<IdentityUser<int>, IdentityRole<int>>()
               .AddEntityFrameworkStores<ApplicationDbContext>()
               .AddDefaultTokenProviders();

            var builder = services.AddIdentityServer()
                // ����ConfigurationDbContext������
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = dbBuilder =>
                    {
                        // ֻ��ָ����Ӧ�����ݿ⡢�����ַ�����Ǩ�Ƴ��򼯼���
                        dbBuilder.UseSqlServer(strConn, t_builder => t_builder.MigrationsAssembly(migrationAssembly));
                    };
                })
                // ����PersistedGrantDbContext������
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = dbBuilder =>
                    {
                        // ֻ��ָ����Ӧ�����ݿ⡢�����ַ�����Ǩ�Ƴ��򼯼���
                        dbBuilder.UseSqlServer(strConn, t_tbuilder => t_tbuilder.MigrationsAssembly(migrationAssembly));
                    };
                })
                  // �û���ʱ�����ڴ���ȡ�����浥��˵
                  //.AddTestUsers(Config.GetTestUsers());
                  .AddAspNetIdentity<IdentityUser<int>>();

            // ע���Ӧ��IdentityServer�������� Ȼ�����ģ������
            //var builder = services.AddIdentityServer()
            //     .AddInMemoryIdentityResources(Config.GetIdentityResources())
            //    .AddInMemoryApiScopes(Config.GetApiScopes())
            //    .AddInMemoryClients(Config.GetClients())
            //    // ģ�ⱸ���û�������Դӵ����
            //    .AddTestUsers(Config.GetTestUsers());
               



            // ָ��Tokenǩ������֤����Կ��ʽ�� ��������ʹ����ʱ��Կ�����ڱ��أ�
            // ������AddSigingCredential, ���������˵��
            builder.AddDeveloperSigningCredential();

            services.Configure<CookiePolicyOptions>(options =>
            {
                //https://docs.microsoft.com/zh-cn/aspnet/core/security/samesite?view=aspnetcore-3.1&viewFallbackFrom=aspnetcore-3
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Lax;

            });
            services.ConfigureNonBreakingSameSiteCookies();
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            InitDatabase(app);
            InitDatabaseUser(app);
            // ���Ӿ�̬�ļ�
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCookiePolicy();
            // �����м��
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }

        private void InitDatabase(IApplicationBuilder app)
        {
            // �Ӹ��������д���һ���������������֮ǰ������ע��������������˵��
            using(var scope = app.ApplicationServices.CreateScope())
            {
                // ÿ�������������ж�Ӧ������������ȡ����Ӧ�Ķ���
                scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
                // ����ȡ����������������
                var configurationDbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                // ���ж�Clients����������û��û�оͽ��ڴ��е����ݴ��ȥ
                if(!configurationDbContext.Clients.Any())
                {
                    // �����ڴ������õĿͻ������ݣ�ֱ�Ӵ��ȥ����
                    foreach(var client in Config.GetClients())
                    {
                        configurationDbContext.Clients.Add(client.ToEntity());
                    }
                    configurationDbContext.SaveChanges();
                }
                // ��ApiScopes
                if (!configurationDbContext.ApiScopes.Any())
                {
                    foreach (var apiScope in Config.GetApiScopes())
                    {
                        configurationDbContext.ApiScopes.Add(apiScope.ToEntity());
                    }
                    configurationDbContext.SaveChanges();
                }
                //��IdentityResources
                if (!configurationDbContext.IdentityResources.Any())
                {
                    foreach (var identity in Config.GetIdentityResources())
                    {
                        configurationDbContext.IdentityResources.Add(identity.ToEntity());
                    }
                    configurationDbContext.SaveChanges();
                }
            }
        }

        private void InitDatabaseUser(IApplicationBuilder app)
        {
            // �Ӹ��������д���һ���������������֮ǰ������ע��������������˵��
            using (var scope = app.ApplicationServices.CreateScope())
            {
                // ÿ�������������ж�Ӧ������������ȡ����Ӧ�Ķ���
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate();

                // ����ֱ��ʹ��΢���װ�õ�manager
                var userManager  = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<int>>>();
                IdentityUser<int> user = new IdentityUser<int>
                {
                    UserName = "Zoe",
                    Email = "Zoe@qq.com"
                };
                var u = userManager.FindByNameAsync(user.UserName).Result;
                if(u!=null)
                {
                    return;
                }
                // Ĭ�����������Ҫ��Ƚ��ϣ���������Ϲ���Ͳ�����ӳɹ�
                var res = userManager.CreateAsync(user, "Zoe123456&").Result;
                if(!res.Succeeded)
                {
                    throw new Exception("ͬ���û�ʧ��");
                }
                var claims = new List<Claim>{
                                    new Claim(JwtClaimTypes.Name, user.UserName),
                                    new Claim(JwtClaimTypes.Email, user.Email),
                                    new Claim(JwtClaimTypes.GivenName, "Zoe11"),
                                    new Claim(JwtClaimTypes.FamilyName, "ZZZ"),
                                    new Claim(JwtClaimTypes.Email, "Zoe@email.com"),
                                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                                    new Claim(JwtClaimTypes.WebSite, "https://www.cnblogs.com/zoe-zyq/")
                                };
                res=userManager.AddClaimsAsync(user, claims).Result;
                if (!res.Succeeded)
                {
                    throw new Exception("ͬ��Claimʧ��");
                }
            }
        }
    }
}
