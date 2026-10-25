using MboaCompetences.ViewModels;

namespace MboaCompetences.Views;

public partial class ElevesPage : ContentPage
{
    private readonly ElevesViewModel _vm;

    public ElevesPage(ElevesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.ChargerAsync();
    }
}
