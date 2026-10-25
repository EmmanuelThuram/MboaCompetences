using MboaCompetences.Core.Services;
using MboaCompetences.Services;
using MboaCompetences.ViewModels;
using MboaCompetences.Views;
using Microsoft.Extensions.Logging;

namespace MboaCompetences;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Base SQLite locale : l'application fonctionne sans aucune connexion internet.
        builder.Services.AddSingleton(_ => new DatabaseService(Path.Combine(FileSystem.AppDataDirectory, "mboacompetences.db3")));
        builder.Services.AddSingleton<EtatApplication>();
        builder.Services.AddSingleton<ExportPdfService>();

        builder.Services.AddTransient<ElevesViewModel>();
        builder.Services.AddTransient<AjoutEleveViewModel>();
        builder.Services.AddTransient<AccueilViewModel>();
        builder.Services.AddTransient<ExerciceViewModel>();
        builder.Services.AddTransient<ResultatViewModel>();
        builder.Services.AddTransient<ParentViewModel>();

        builder.Services.AddTransient<ElevesPage>();
        builder.Services.AddTransient<AjoutElevePage>();
        builder.Services.AddTransient<AccueilPage>();
        builder.Services.AddTransient<ExercicePage>();
        builder.Services.AddTransient<ResultatPage>();
        builder.Services.AddTransient<ParentPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
