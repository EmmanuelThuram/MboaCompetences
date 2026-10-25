using System.Globalization;
using System.Text;
using MboaCompetences.Core.Models;

namespace MboaCompetences.Core.Services;

/// <summary>Graphiques vectoriels intégrés au bilan PDF (courbe de progression et radar).</summary>
public static class GraphiquesSvg
{
    public const string Vert = "#007A5E";
    public const string Rouge = "#CE1126";
    public const string Gris = "#6B7280";
    public const string GrisClair = "#E5E7EB";

    private static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    public static string Courbe(IReadOnlyList<PointProgression> points, double largeur = 480, double hauteur = 170)
    {
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(largeur)}\" height=\"{N(hauteur)}\" viewBox=\"0 0 {N(largeur)} {N(hauteur)}\">");
        sb.Append($"<rect width=\"{N(largeur)}\" height=\"{N(hauteur)}\" fill=\"#FFFFFF\"/>");

        const double gauche = 34, droite = 14, haut = 12, bas = 26;
        var l = largeur - gauche - droite;
        var h = hauteur - haut - bas;

        foreach (var graduation in new[] { 0, 25, 50, 75, 100 })
        {
            var y = haut + h - graduation / 100.0 * h;
            sb.Append($"<line x1=\"{N(gauche)}\" y1=\"{N(y)}\" x2=\"{N(largeur - droite)}\" y2=\"{N(y)}\" stroke=\"{GrisClair}\" stroke-width=\"1\"/>");
            sb.Append($"<text x=\"{N(gauche - 6)}\" y=\"{N(y + 3.5)}\" font-size=\"9\" fill=\"{Gris}\" text-anchor=\"end\">{graduation}</text>");
        }

        if (points.Count == 0)
        {
            sb.Append($"<text x=\"{N(largeur / 2)}\" y=\"{N(hauteur / 2)}\" font-size=\"11\" fill=\"{Gris}\" text-anchor=\"middle\">Pas encore de données</text></svg>");
            return sb.ToString();
        }

        double X(int i) => points.Count == 1 ? gauche + l / 2 : gauche + l * i / (points.Count - 1);
        double Y(double v) => haut + h - Math.Clamp(v, 0, 100) / 100.0 * h;

        var trace = string.Join(" ", points.Select((p, i) => $"{N(X(i))},{N(Y(p.Valeur))}"));
        if (points.Count > 1)
            sb.Append($"<polyline points=\"{trace}\" fill=\"none\" stroke=\"{Vert}\" stroke-width=\"2.5\" stroke-linejoin=\"round\"/>");

        for (var i = 0; i < points.Count; i++)
        {
            sb.Append($"<circle cx=\"{N(X(i))}\" cy=\"{N(Y(points[i].Valeur))}\" r=\"3.5\" fill=\"{Vert}\"/>");
            sb.Append($"<text x=\"{N(X(i))}\" y=\"{N(hauteur - 9)}\" font-size=\"9\" fill=\"{Gris}\" text-anchor=\"middle\">{points[i].Libelle}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    public static string Radar(IReadOnlyDictionary<Domaine, double>? avant, IReadOnlyDictionary<Domaine, double> apres, double taille = 260)
    {
        var domaines = Enum.GetValues<Domaine>();
        var cx = taille / 2;
        var cy = taille / 2 + 4;
        var rayon = taille / 2 - 52;

        (double x, double y) Point(int i, double valeur, double rayonPlus = 0)
        {
            var angle = (-90 + i * 360.0 / domaines.Length) * Math.PI / 180;
            var r = rayon * valeur / 100 + rayonPlus;
            return (cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
        }

        string Polygone(Func<int, (double x, double y)> point) =>
            string.Join(" ", Enumerable.Range(0, domaines.Length).Select(i => { var (x, y) = point(i); return $"{N(x)},{N(y)}"; }));

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(taille)}\" height=\"{N(taille)}\" viewBox=\"0 0 {N(taille)} {N(taille)}\">");
        sb.Append($"<rect width=\"{N(taille)}\" height=\"{N(taille)}\" fill=\"#FFFFFF\"/>");

        foreach (var niveau in new[] { 25, 50, 75, 100 })
            sb.Append($"<polygon points=\"{Polygone(i => Point(i, niveau))}\" fill=\"none\" stroke=\"{GrisClair}\" stroke-width=\"1\"/>");

        for (var i = 0; i < domaines.Length; i++)
        {
            var (x, y) = Point(i, 100);
            sb.Append($"<line x1=\"{N(cx)}\" y1=\"{N(cy)}\" x2=\"{N(x)}\" y2=\"{N(y)}\" stroke=\"{GrisClair}\" stroke-width=\"1\"/>");
        }

        if (avant is not null)
            sb.Append($"<polygon points=\"{Polygone(i => Point(i, avant[domaines[i]]))}\" fill=\"#CE1126\" fill-opacity=\"0.12\" stroke=\"{Rouge}\" stroke-width=\"1.8\" stroke-dasharray=\"5,3\"/>");

        sb.Append($"<polygon points=\"{Polygone(i => Point(i, apres[domaines[i]]))}\" fill=\"{Vert}\" fill-opacity=\"0.28\" stroke=\"{Vert}\" stroke-width=\"2.2\"/>");

        for (var i = 0; i < domaines.Length; i++)
        {
            var (x, y) = Point(i, 100, 14);
            var ancre = Math.Abs(x - cx) < 6 ? "middle" : x < cx ? "end" : "start";
            sb.Append($"<text x=\"{N(x)}\" y=\"{N(y + 3)}\" font-size=\"10\" fill=\"#1F2937\" text-anchor=\"{ancre}\">{Catalogue.Nom(domaines[i])}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    public static string Pastille(StatutCompetence statut)
    {
        var couleur = statut switch
        {
            StatutCompetence.Vert => "#16A34A",
            StatutCompetence.Orange => "#F59E0B",
            StatutCompetence.Rouge => "#CE1126",
            _ => "#CBD5E1"
        };
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"12\" height=\"12\" viewBox=\"0 0 12 12\"><circle cx=\"6\" cy=\"6\" r=\"5.5\" fill=\"{couleur}\"/></svg>";
    }
}
