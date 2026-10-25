using MboaCompetences.ViewModels;

namespace MboaCompetences.Views;

public partial class AjoutElevePage : ContentPage
{
    public AjoutElevePage(AjoutEleveViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
