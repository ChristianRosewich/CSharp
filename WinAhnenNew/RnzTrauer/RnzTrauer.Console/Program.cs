using Db.Core.Abstractions.Sql.Interfaaces;
using Db.Provider.MySql;
using Microsoft.Extensions.DependencyInjection;
using RnzTrauer.Console.ViewModels;
using RnzTrauer.Console.Views;
using RnzTrauer.Core;
using RnzTrauer.Core.Services.Interfaces;
using RnzTrauer.WebDriver.Edge;
using RnzTrauer.WebDriver.Firefox;

try
{
    var xFile = new FileProxy();
    var xConfigLoader = new ConfigLoader(xFile);
    var xConfig = new RnzConfig(xConfigLoader).Load(Path.Combine(AppContext.BaseDirectory, "RNZ_Config.json"));

    var xServices = new ServiceCollection()
        .AddSingleton<IFile>(xFile)
        .AddSingleton<IConfigLoader>(xConfigLoader)
        .AddSingleton(xConfig)
        .AddSingleton<IDbConnectionFactory, MySqlDbConnectionFactory>()
        .AddSingleton<IHttpClientProxy, HttpClientProxy>()
        .AddFirefoxWebDriver()
        .AddEdgeWebDriver()
        .AddSingleton<IWebDriverFactory, BrowserWebDriverFactory>()
        .AddTransient<ConsoleOutputView>()
        .AddTransient<RnzTrauerConsoleViewModel>()
        .BuildServiceProvider();

    var xViewModel = xServices.GetRequiredService<RnzTrauerConsoleViewModel>();
    xViewModel.Run(xConfig, args.FirstOrDefault() ?? "");
}
catch (FileNotFoundException ex)
{
    var xView = new ConsoleOutputView();
    xView.WriteErrorLine(ex.Message);
    xView.WriteErrorLine("Lege eine Datei `RNZ_Config.json` neben die EXE. Eine Vorlage liegt als `RNZ_Config.sample.json` im Projekt.");
}
