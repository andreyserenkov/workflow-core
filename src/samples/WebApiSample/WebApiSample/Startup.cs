using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApiSample.Workflows;
using WorkflowCore.Interface;
using WorkflowCore.LockProviders.PostgreSQL;

namespace WebApiSample
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
            services.AddMvc();
            var conString = @"Server=db_test_host;Port=5432;Database=testdb;User Id=postgres;Password=qqqqqqqq1;";
            services.AddWorkflow(cfg =>
            {
                cfg.UsePostgreSQL(conString, true, true);
                cfg.UsePostgresDistributedLockManager(conString);
                //cfg.UseElasticsearch(new ConnectionSettings(new Uri("http://elastic:9200")), "workflows");
            });

            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" }));

            services.AddControllers().AddNewtonsoftJson();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var host = app.ApplicationServices.GetService<IWorkflowHost>();
            host.RegisterWorkflow<TestWorkflow, MyDataClass>();
            host.Start();
        }
    }
}
