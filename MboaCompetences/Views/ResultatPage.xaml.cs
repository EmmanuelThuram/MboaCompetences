using MboaCompetences.ViewModels;

namespace MboaCompetences.Views;

public partial class ResultatPage : ContentPage
{
    private readonly ResultatViewModel _vm;

    public ResultatPage(ResultatViewModel vm)
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
