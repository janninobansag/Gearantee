using ASI.Basecode.Data;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.WebApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;

namespace ASI.Basecode.WebApp
{
    internal partial class StartupConfigurer
    {
        private void ConfigureOtherServices()
        {
            _services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            _services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            _services.AddScoped<IUnitOfWork, UnitOfWork>();
            _services.Configure<BrevoOptions>(
                Configuration.GetSection(BrevoOptions.SectionName));
            _services.AddHttpClient<IBrevoEmailSender, BrevoEmailSender>(
                (serviceProvider, client) =>
                {
                    var options = serviceProvider
                        .GetRequiredService<IOptions<BrevoOptions>>()
                        .Value;
                    client.BaseAddress = new Uri(
                        options.BaseUrl.TrimEnd('/') + "/");
                });
        }
    }
}
