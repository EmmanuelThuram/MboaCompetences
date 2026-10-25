using MboaCompetences.ViewModels;

namespace MboaCompetences.Views;

public partial class ParentPage : ContentPage
{
    private readonly ParentViewModel _vm;

    public ParentPage(ParentViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        VueCourbe.Drawable = vm.Courbe;
        VueRadar.Drawable = vm.Radar;
        vm.DessinsChanges += () =>
        {
            VueCourbe.Invalidate();
            VueRadar.Invalidate();
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.ChargerAsync();
    }
}
