using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace webUiPathTest
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseWorkflow();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapGet("/", async context =>
            //    {
            //        await context.Response.WriteAsync("Hello World!");
            //    });
            //});

            string backToHome = "</br></br><a href='/'>Back to home</a>";

            string sampleMethosPrefix = "sample_";
            //var samples = typeof(Workflows.Samples).GetMethods().Where(m => m.IsStatic && m.Name.StartsWith(sampleMethosPrefix));
            var samples = from m in typeof(Workflows.Samples).GetMethods()
                          where m.IsStatic && m.Name.StartsWith(sampleMethosPrefix)
                          let args = m.GetParameters()
                          where args.Length == 1 && args[0].ParameterType == typeof(HttpContext)
                          select m;

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    StringBuilder sBuilder = new StringBuilder();
                    sBuilder.Append("<html>");
                    sBuilder.Append("<body>");

                    foreach (var sample in samples)
                    {
                        string sampleSuffix = sample.Name.Substring(sampleMethosPrefix.Length);
                        sBuilder.Append($"<a href='/{sampleSuffix}'>{sampleSuffix}</a></br></br>");
                    }

                    sBuilder.Append(backToHome);
                    sBuilder.Append("</body>");
                    sBuilder.Append("</html>");

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(sBuilder.ToString());
                });

                foreach (var sample in samples)
                {
                    string sampleSuffix = sample.Name.Substring(sampleMethosPrefix.Length);

                    endpoints.MapGet($"/{sampleSuffix}", async context =>
                    {
                        StringBuilder sBuilder = new StringBuilder();
                        var ret = sample.Invoke(null, new object[] { context });
                        sBuilder.Append("<html>");
                        sBuilder.Append("<body>");
                        sBuilder.Append($"{ret}");
                        sBuilder.Append("</br>");
                        //sBuilder.Append($"sample {sampleSuffix} completed!!!</br>");
                        sBuilder.Append($"</br>Sample {sample.Name} executed!!!</br>");
                        sBuilder.Append(backToHome);
                        sBuilder.Append("</body>");
                        sBuilder.Append("</html>");
                        await context.Response.WriteAsync(sBuilder.ToString());
                    });
                }

                endpoints.MapPost($"/step", async context =>
                {
                    var stepName = context.Request.Form["step"];
                    if (string.IsNullOrEmpty(stepName))
                    {
                        await context.Response.WriteAsync($"stepName missing!!!");
                        return;
                    }

                    var stepMethod = (from m in typeof(Workflows.Samples).GetMethods()
                                      where m.IsStatic && m.Name == stepName
                                      let args = m.GetParameters()
                                      where args.Length == 1 && args[0].ParameterType == typeof(HttpContext)
                                      select m).FirstOrDefault();
                    if (stepMethod != null)
                    {
                        var ret = stepMethod.Invoke(null, new object[] { context });

                        StringBuilder sBuilder = new StringBuilder();
                        sBuilder.Append("<html>");
                        sBuilder.Append("<body>");
                        sBuilder.Append($"{ret}");
                        sBuilder.Append("</br>");
                        sBuilder.Append($"</br>Sample {stepMethod.Name} executed!!!</br>");
                        sBuilder.Append(backToHome);
                        sBuilder.Append("</body>");
                        sBuilder.Append("</html>");
                        await context.Response.WriteAsync(sBuilder.ToString());
                        return;
                    }

                    await context.Response.WriteAsync($"step {stepName} not found!!!");
                });
            });
        }
    }
}
