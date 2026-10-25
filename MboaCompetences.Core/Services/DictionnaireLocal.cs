using MboaCompetences.Core.Models;

namespace MboaCompetences.Core.Services;

/// <summary>Le dictionnaire des variables locales camerounaises, chargé depuis SQLite.</summary>
public class DictionnaireLocal
{
    public IReadOnlyList<VariableLocale> Marches { get; }
    public IReadOnlyList<VariableLocale> Articles { get; }
    public IReadOnlyList<VariableLocale> Prenoms { get; }
    public IReadOnlyList<VariableLocale> Villes { get; }

    public DictionnaireLocal(IEnumerable<VariableLocale> variables)
    {
        var liste = variables.ToList();
        Marches = liste.Where(v => v.Categorie == "Marche").ToList();
        Articles = liste.Where(v => v.Categorie == "Article").ToList();
        Prenoms = liste.Where(v => v.Categorie == "Prenom").ToList();
        Villes = liste.Where(v => v.Categorie == "Ville").ToList();
    }

    public static DictionnaireLocal PuisDeLaGraine() => new(DonneesInitiales.Variables());
}

public static class DonneesInitiales
{
    public static List<VariableLocale> Variables()
    {
        var v = new List<VariableLocale>();

        void Marche(string nom, string ville) => v.Add(new() { Categorie = "Marche", Valeur = nom, Detail = ville });
        Marche("marché Mokolo", "Yaoundé");
        Marche("marché Mvog-Mbi", "Yaoundé");
        Marche("marché Essos", "Yaoundé");
        Marche("marché Congo", "Douala");
        Marche("marché New Bell", "Douala");
        Marche("marché Sandaga", "Douala");
        Marche("marché Mboppi", "Douala");
        Marche("marché de Bafoussam", "Bafoussam");
        Marche("marché de Garoua", "Garoua");
        Marche("marché de Bamenda", "Bamenda");

        // Valeur = complément (« de manioc »), Detail = unité, Unite = formule du prix unitaire.
        void Article(string valeur, string unite, string formule, int prix) =>
            v.Add(new() { Categorie = "Article", Valeur = valeur, Detail = unite, Unite = formule, PrixUnitaire = prix });
        Article("de manioc", "kg", "le kilo", 350);
        Article("de riz", "kg", "le kilo", 500);
        Article("de tomates", "tas", "le tas", 200);
        Article("d'oranges", "tas", "le tas", 100);
        Article("de mangues", "tas", "le tas", 250);
        Article("de plantains", "régime", "le régime", 1500);
        Article("de ndolè", "botte", "la botte", 150);
        Article("de koki", "sachet", "le sachet", 200);
        Article("d'arachides", "sachet", "le sachet", 150);
        Article("de poisson fumé", "tas", "le tas", 500);
        Article("de sardines", "boîte", "la boîte", 400);
        Article("d'huile de palme", "litre", "le litre", 750);
        Article("de beignets", "sachet", "le sachet", 100);
        Article("de farine de maïs", "kg", "le kilo", 450);

        void Prenom(string nom, string genre) => v.Add(new() { Categorie = "Prenom", Valeur = nom, Detail = genre });
        foreach (var p in new[] { "Aïcha", "Mireille", "Fatou", "Estelle", "Nadège", "Chantal", "Ngono", "Josiane" }) Prenom(p, "F");
        foreach (var p in new[] { "Kevin", "Paul", "Brice", "Junior", "Idriss", "Samuel", "Mbarga", "Ndzana" }) Prenom(p, "M");

        foreach (var ville in new[] { "Yaoundé", "Douala", "Bafoussam", "Garoua", "Bamenda", "Bertoua", "Ebolowa", "Maroua", "Kribi", "Limbé" })
            v.Add(new() { Categorie = "Ville", Valeur = ville });

        return v;
    }
}
