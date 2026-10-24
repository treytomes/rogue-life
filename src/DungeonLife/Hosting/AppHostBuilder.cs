using System;
using DungeonLife.ViewModels;
using DungeonLife.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DungeonLife.Hosting
{
    /// <summary>
    /// Composes the desktop application's services with the Microsoft Generic Host.
    /// </summary>
    internal static class AppHostBuilder
    {
        public static IHost Build()
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = AppContext.BaseDirectory,
            });

            builder.Services.AddSingleton<WorldGenerator, CavernGenerator>();
            builder.Services.AddSingleton<WorldSimulation>();
            builder.Services.AddSingleton<MainWindowViewModel>();
            builder.Services.AddSingleton(serviceProvider =>
                new MainWindow(serviceProvider.GetRequiredService<MainWindowViewModel>()));

            return builder.Build();
        }
    }
}
