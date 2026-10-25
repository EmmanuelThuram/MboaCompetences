using System.Globalization;
using MboaCompetences.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MboaCompetences.Core.Services;

/// <summary>Génère, directement sur le téléphone, le bilan de compétences comparatif Avant / Après au format PDF.</summary>
public static class PdfBilanService
{
    private const string Vert = "#007A5E";
    private const string VertFonce = "#00573F";
    private const string VertClair = "#E3F4EE";
    private const string Rouge = "#CE1126";
    private const string Jaune = "#FCD116";
    private const string Gris = "#6B7280";
    private const string Bord = "#E5E7EB";

    static PdfBilanService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static string NomFichier(Eleve eleve, DateTime date) =>
        $"Bilan_{Nettoyer(eleve.Prenom)}_{eleve.NomNiveau}_{date:yyyyMMdd}.pdf";

    public static void GenererFichier(Eleve eleve, IReadOnlyList<Tentative> tentatives, string chemin, DateTime? maintenant = null)
    {
        Dossier(eleve, tentatives, maintenant ?? DateTime.Now).GeneratePdf(chemin);
    }

    public static List<byte[]> GenererImages(Eleve eleve, IReadOnlyList<Tentative> tentatives, DateTime? maintenant = null)
    {
        return Dossier(eleve, tentatives, maintenant ?? DateTime.Now).GenerateImages().ToList();
    }

    private static string Nettoyer(string texte) =>
        new string(texte.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => char.IsLetterOrDigit(c) && c < 128).ToArray());

    private static string Date(DateTime? d) => d?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—";

    private static string Pct(double? v) => v is null ? "—" : $"{Math.Round(v.Value)} %";

    private static string Sec(double? v) => v is null ? "—" : $"{Math.Round(v.Value)} s";

    private static string CouleurStatut(StatutCompetence s) => s switch
    {
        StatutCompetence.Vert => "#15803D",
        StatutCompetence.Orange => "#B45309",
        StatutCompetence.Rouge => Rouge,
        _ => Gris
    };

    private static string TexteStatut(StatutCompetence s) => s switch
    {
        StatutCompetence.Vert => "Acquise",
        StatutCompetence.Orange => "En cours",
        StatutCompetence.Rouge => "Non acquise",
        _ => "À découvrir"
    };

    private static Document Dossier(Eleve eleve, IReadOnlyList<Tentative> tentatives, DateTime maintenant)
    {
        var comparatif = Analyse.Comparer(tentatives, eleve.Niveau);
        var progression = Analyse.ProgressionHebdomadaire(tentatives);
        var conseils = Analyse.Conseils(comparatif, eleve.Prenom);

        return Document.Create(doc =>
        {
            // ---------------------------------------------------------------- page 1 : indicateurs et graphiques
            doc.Page(page =>
            {
                ConfigurerPage(page, maintenant, 1);

                page.Header().Element(c => EnTete(c, eleve, comparatif, maintenant));

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Element(c => Titre(c, "Indicateurs clés"));
                    col.Item().Element(c => TableauIndicateurs(c, comparatif));

                    col.Item().Element(c => Titre(c, "Courbe de progression (taux d'acquisition par semaine)"));
                    col.Item().Border(0.7f).BorderColor(Bord).Padding(6).Svg(GraphiquesSvg.Courbe(progression, 500, 150));

                    col.Item().Element(c => Titre(c, "Compétences par domaine de la vie"));
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Width(220).Height(220)
                            .Svg(GraphiquesSvg.Radar(comparatif.ADeuxPeriodes ? comparatif.RadarAvant : null, comparatif.RadarApres, 220));
                        row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(legende =>
                        {
                            legende.Spacing(6);
                            if (comparatif.ADeuxPeriodes)
                                legende.Item().Text(t =>
                                {
                                    t.Span("(tirets rouges)  ").FontColor(Rouge).Bold();
                                    t.Span("Avant").FontSize(10);
                                });
                            legende.Item().Text(t =>
                            {
                                t.Span("(trait vert)  ").FontColor(Vert).Bold();
                                t.Span(comparatif.ADeuxPeriodes ? "Après" : "Résultats").FontSize(10);
                            });
                            legende.Item().Text("Plus la zone verte est grande, plus l'enfant réussit dès le premier essai dans ce domaine.")
                                .FontSize(9).FontColor(Gris);
                        });
                    });
                });
            });

            // ---------------------------------------------------------------- page 2 : détail et conseils
            doc.Page(page =>
            {
                ConfigurerPage(page, maintenant, 2);

                page.Header().Element(c => EnTete(c, eleve, comparatif, maintenant));

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Element(c => Titre(c, "État des compétences (feux tricolores)"));
                    col.Item().Element(c => TableauCompetences(c, comparatif));

                    col.Item().Element(c => Titre(c, "Conseils pour le parent"));
                    col.Item().Column(liste =>
                    {
                        liste.Spacing(4);
                        foreach (var conseil in conseils)
                            liste.Item().Row(r =>
                            {
                                r.ConstantItem(12).AlignMiddle().Height(10).Svg(GraphiquesSvg.Pastille(StatutCompetence.Vert));
                                r.RelativeItem().Text(conseil).FontSize(10);
                            });
                    });

                    col.Item().Element(c => Titre(c, "Observation de l'enseignant"));
                    col.Item().Border(0.7f).BorderColor(Bord).Padding(8).Column(zone =>
                    {
                        zone.Spacing(14);
                        for (var i = 0; i < 3; i++)
                            zone.Item().LineHorizontal(0.5f).LineColor(Bord);
                        zone.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Date :").FontSize(9).FontColor(Gris);
                            r.RelativeItem().Text("Signature de l'enseignant :").FontSize(9).FontColor(Gris);
                            r.RelativeItem().Text("Signature du parent :").FontSize(9).FontColor(Gris);
                        });
                        zone.Item().Height(20);
                    });
                });
            });
        });
    }

    private static void ConfigurerPage(PageDescriptor page, DateTime maintenant, int numero)
    {
        page.Size(PageSizes.A4);
        page.Margin(30);
        page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1F2937"));

        page.Footer().Row(r =>
        {
            r.RelativeItem().Text($"MboaCompétences · bilan généré le {Date(maintenant)}, sans connexion internet").FontSize(8).FontColor(Gris);
            r.ConstantItem(60).AlignRight().Text($"Page {numero}/2").FontSize(8).FontColor(Gris);
        });
    }

    private static void EnTete(IContainer c, Eleve eleve, Comparatif comparatif, DateTime maintenant)
    {
        c.Column(col =>
        {
            col.Item().Background(Vert).Padding(12).Row(r =>
            {
                r.RelativeItem().Column(titre =>
                {
                    titre.Item().Text("Bilan de compétences").FontSize(20).Bold().FontColor(Colors.White);
                    titre.Item().Text("Comparatif Avant / Après · Approche Par Compétences").FontSize(10).FontColor("#D1FAE5");
                });
                r.ConstantItem(150).AlignRight().AlignMiddle().Column(d =>
                {
                    d.Item().AlignRight().Text("MboaCompétences").Bold().FontColor(Jaune).FontSize(12);
                    d.Item().AlignRight().Text("APC-MALIN").FontColor(Colors.White).FontSize(9);
                });
            });
            col.Item().Row(r =>
            {
                r.RelativeItem().Background(Vert).Height(3);
                r.RelativeItem().Background(Rouge).Height(3);
                r.RelativeItem().Background(Jaune).Height(3);
            });
            col.Item().PaddingTop(8).Row(r =>
            {
                r.RelativeItem().Column(i =>
                {
                    i.Item().Text(t =>
                    {
                        t.Span("Élève : ").FontColor(Gris);
                        t.Span($"{eleve.Prenom}").Bold();
                        t.Span($"   ·   Niveau : ").FontColor(Gris);
                        t.Span(eleve.NomNiveau).Bold();
                    });
                    i.Item().Text(t =>
                    {
                        t.Span("Période étudiée : ").FontColor(Gris);
                        t.Span($"du {Date(comparatif.Debut)} au {Date(comparatif.Fin)}").Bold();
                        t.Span($"   ·   {comparatif.Apres.NbExercices + comparatif.Avant.NbExercices} exercices").FontColor(Gris);
                    });
                });
            });
        });
    }

    private static void Titre(IContainer c, string texte)
    {
        c.BorderBottom(1.2f).BorderColor(Vert).PaddingBottom(2).Text(texte).FontSize(12).Bold().FontColor(VertFonce);
    }

    private static void TableauIndicateurs(IContainer c, Comparatif cmp)
    {
        var deux = cmp.ADeuxPeriodes;

        // La police du PDF n'a pas de flèches : on écrit l'évolution avec un signe, et la couleur dit si c'est favorable.
        string Signe(double v, string unite) => $"{(v >= 0 ? "+" : "-")}{Math.Abs(Math.Round(v))} {unite}";

        var evolTag = cmp.EvolutionTag is { } t ? (Signe(t, "pts"), (bool?)(t >= 0)) : ("—", null);
        var evolIvr = cmp.EvolutionIvrPourcent is { } v ? (Signe(v, "%"), (bool?)(v <= 0)) : ("—", null);
        var evolTre = cmp.Apres.ExosMoyensPourRedresser is { } m ? ($"en {Math.Round(m)} exos", (bool?)true) : ("—", null);
        var ecartVertes = cmp.Apres.NbVertes - cmp.Avant.NbVertes;

        (string, string, string, string, string, bool?)[] lignes =
        [
            ("TAG", "Taux d'Acquisition Global (réussite dès le 1er essai)", deux ? Pct(cmp.Avant.Tag) : "—", Pct(cmp.Apres.Tag), evolTag.Item1, evolTag.Item2),
            ("IVR", "Indice de Vitesse de Résolution (temps moyen par exercice)", deux ? Sec(cmp.Avant.IvrSecondes) : "—", Sec(cmp.Apres.IvrSecondes), evolIvr.Item1, evolIvr.Item2),
            ("TRE", "Taux de Réduction des Erreurs (de Rouge à Vert)", deux ? Pct(cmp.Avant.Tre) : "—", Pct(cmp.Apres.Tre), evolTre.Item1, evolTre.Item2),
            ("Acquises", "Compétences au vert", deux ? cmp.Avant.NbVertes.ToString() : "—", cmp.Apres.NbVertes.ToString(),
                deux ? Signe(ecartVertes, "") : "—", deux ? ecartVertes >= 0 : null),
            ("Exercices", "Nombre d'exercices réalisés", deux ? cmp.Avant.NbExercices.ToString() : "—", cmp.Apres.NbExercices.ToString(), "", null)
        ];

        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(62);
                cols.RelativeColumn(5);
                cols.ConstantColumn(52);
                cols.ConstantColumn(52);
                cols.ConstantColumn(78);
            });

            table.Header(h =>
            {
                foreach (var entete in new[] { "Indicateur", "Définition", "Avant", "Après", "Évolution" })
                    h.Cell().Background(Vert).Padding(5).Text(entete).Bold().FontColor(Colors.White).FontSize(9);
            });

            foreach (var (nom, def, avant, apres, evol, favorable) in lignes)
            {
                var couleur = favorable is null ? "#1F2937" : favorable.Value ? "#15803D" : Rouge;

                table.Cell().BorderBottom(0.5f).BorderColor(Bord).Padding(5).Text(nom).Bold().FontColor(VertFonce);
                table.Cell().BorderBottom(0.5f).BorderColor(Bord).Padding(5).Text(def).FontSize(9);
                table.Cell().BorderBottom(0.5f).BorderColor(Bord).Padding(5).Text(avant);
                table.Cell().BorderBottom(0.5f).BorderColor(Bord).Padding(5).Text(apres).Bold();
                table.Cell().BorderBottom(0.5f).BorderColor(Bord).Padding(5).Text(evol).Bold().FontColor(couleur);
            }
        });
    }

    private static void TableauCompetences(IContainer c, Comparatif cmp)
    {
        var deux = cmp.ADeuxPeriodes;
        var avantParCode = cmp.StatutsAvant.ToDictionary(s => s.Competence.Code);

        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(78);
                cols.RelativeColumn(4);
                cols.RelativeColumn(2.2f);
                cols.RelativeColumn(2.2f);
            });

            table.Header(h =>
            {
                foreach (var entete in new[] { "Matière", "Compétence", deux ? "Avant" : "—", "Après" })
                    h.Cell().Background(Vert).Padding(5).Text(entete).Bold().FontColor(Colors.White).FontSize(9);
            });

            foreach (var apres in cmp.StatutsApres.OrderBy(s => s.Competence.Matiere))
            {
                avantParCode.TryGetValue(apres.Competence.Code, out var avant);

                table.Cell().BorderBottom(0.5f).BorderColor(Bord).Padding(4).Text(Catalogue.Nom(apres.Competence.Matiere)).FontSize(9).FontColor(Gris);
                table.Cell().BorderBottom(0.5f).BorderColor(Bord).Padding(4).Text(apres.Competence.Libelle);

                table.Cell().BorderBottom(0.5f).BorderColor(Bord).Padding(4).Element(cell => CelluleStatut(cell, deux ? avant : null));
                table.Cell().BorderBottom(0.5f).BorderColor(Bord).Padding(4).Element(cell => CelluleStatut(cell, apres));
            }
        });
    }

    private static void CelluleStatut(IContainer cell, StatutInfo? info)
    {
        if (info is null)
        {
            cell.Text("—").FontColor(Gris);
            return;
        }

        cell.Row(r =>
        {
            r.ConstantItem(12).AlignMiddle().Height(12).Svg(GraphiquesSvg.Pastille(info.Statut));
            r.RelativeItem().PaddingLeft(5).Text(t =>
            {
                t.Span(TexteStatut(info.Statut)).FontSize(9).FontColor(CouleurStatut(info.Statut)).Bold();
                if (info.Pourcent is { } p) t.Span($" {p} %").FontSize(8).FontColor(Gris);
            });
        });
    }
}

