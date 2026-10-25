using MboaCompetences.Views;

namespace MboaCompetences;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("ajoutEleve", typeof(AjoutElevePage));
        Routing.RegisterRoute("exercice", typeof(ExercicePage));
        Routing.RegisterRoute("resultat", typeof(ResultatPage));
    }
}
