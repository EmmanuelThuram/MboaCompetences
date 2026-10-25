using MboaCompetences.Core.Models;

namespace MboaCompetences.Services;

/// <summary>Couleurs utilisées par le code (les mêmes que dans Colors.xaml).</summary>
public static class Palette
{
    public static readonly Color Vert = Color.FromArgb("#007A5E");
    public static readonly Color VertFonce = Color.FromArgb("#00573F");
    public static readonly Color VertClair = Color.FromArgb("#E3F4EE");
    public static readonly Color Rouge = Color.FromArgb("#CE1126");
    public static readonly Color RougeClair = Color.FromArgb("#FDECEE");
    public static readonly Color Jaune = Color.FromArgb("#FCD116");
    public static readonly Color FeuVert = Color.FromArgb("#16A34A");
    public static readonly Color FeuVertClair = Color.FromArgb("#E8F8EE");
    public static readonly Color FeuOrange = Color.FromArgb("#F59E0B");
    public static readonly Color FeuOrangeClair = Color.FromArgb("#FEF3C7");
    public static readonly Color FeuGris = Color.FromArgb("#CBD5E1");
    public static readonly Color Texte = Color.FromArgb("#1F2937");
    public static readonly Color TexteGris = Color.FromArgb("#6B7280");
    public static readonly Color Bord = Color.FromArgb("#E5E7EB");
    public static readonly Color Blanc = Colors.White;

    public static readonly Color[] Avatars =
    [
        Color.FromArgb("#FCD116"), Color.FromArgb("#BFE8DB"), Color.FromArgb("#F9C7CD"), Color.FromArgb("#BFDBFE")
    ];

    public static Color Feu(StatutCompetence s) => s switch
    {
        StatutCompetence.Vert => FeuVert,
        StatutCompetence.Orange => FeuOrange,
        StatutCompetence.Rouge => Rouge,
        _ => FeuGris
    };

    public static Color FeuFonce(StatutCompetence s) => s switch
    {
        StatutCompetence.Vert => Color.FromArgb("#15803D"),
        StatutCompetence.Orange => Color.FromArgb("#B45309"),
        StatutCompetence.Rouge => Rouge,
        _ => TexteGris
    };

    public static Color FeuClair(StatutCompetence s) => s switch
    {
        StatutCompetence.Vert => FeuVertClair,
        StatutCompetence.Orange => FeuOrangeClair,
        StatutCompetence.Rouge => RougeClair,
        _ => Color.FromArgb("#F1F5F9")
    };

    public static string Texte_(StatutCompetence s) => s switch
    {
        StatutCompetence.Vert => "acquise",
        StatutCompetence.Orange => "en cours",
        StatutCompetence.Rouge => "à revoir",
        _ => "à découvrir"
    };
}
