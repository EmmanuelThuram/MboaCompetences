using MboaCompetences.ViewModels;

namespace MboaCompetences.Views;

public partial class ExercicePage : ContentPage
{
    private readonly ExerciceViewModel _vm;

    public ExercicePage(ExerciceViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.DemarrerAsync();
        _vm.Reprendre();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Arreter();
    }
}
