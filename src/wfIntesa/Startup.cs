using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace wfIntesa
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public static IConfiguration config { get; private set; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration config)
        {
            if (Startup.config == null)
                Startup.config = config;

            string backToHome = "</br></br><a href='/'>Back to home</a>";
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            var myKeyValue = config["MyKey"];

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    StringBuilder sBuilder = new StringBuilder();
                    sBuilder.Append("<html>");
                    sBuilder.Append("<body>");
                    sBuilder.Append($"<a href='/hello'>hello</a></br>");
                    sBuilder.Append($"<a href='/bookmark1'>bookmark1</a></br>");
                    sBuilder.Append(backToHome);
                    sBuilder.Append("</body>");
                    sBuilder.Append("</html>");
                    await context.Response.WriteAsync(sBuilder.ToString());
                });

                endpoints.MapGet("/hello", async context =>
                {
                    StringBuilder sBuilder = new StringBuilder();
                    Workflows.Samples.sample_hello();
                    sBuilder.Append("<html>");
                    sBuilder.Append("<body>");
                    sBuilder.Append($"Hello world!!!");
                    sBuilder.Append(backToHome);
                    sBuilder.Append("</body>");
                    sBuilder.Append("</html>");
                    await context.Response.WriteAsync(sBuilder.ToString());
                });

                endpoints.MapGet("/bookmark1", async context =>
                {
                    Type t = Type.GetType("CoreWf.Variable`1+VariableLocation[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]");

                    Guid bookmarkId = Guid.Empty; 
                    if (context.Request.Query.ContainsKey("id"))
                    {
                        Guid.TryParse(context.Request.Query["id"], out bookmarkId);
                    }

                    Guid wfId = Workflows.Samples.sample_bookmark1(bookmarkId);

                    if (bookmarkId == Guid.Empty)
                    {
                        StringBuilder sBuilder = new StringBuilder();
                        sBuilder.Append("<html>");
                        sBuilder.Append("<body>");
                        sBuilder.Append($"Workflows id: <a href='/bookmark1/?id={wfId}'>{wfId}</a>");
                        sBuilder.Append(backToHome);
                        sBuilder.Append("</body>");
                        sBuilder.Append("</html>");
                        await context.Response.WriteAsync(sBuilder.ToString());
                    }
                    else if (wfId == bookmarkId)
                    {
                        StringBuilder sBuilder = new StringBuilder();
                        sBuilder.Append("<html>");
                        sBuilder.Append("<body>");
                        sBuilder.Append($"Workflows id: {wfId} terminato");
                        sBuilder.Append(backToHome);
                        sBuilder.Append("</body>");
                        sBuilder.Append("</html>");
                        await context.Response.WriteAsync(sBuilder.ToString());
                    }
                });
            });
        }
    }
}
