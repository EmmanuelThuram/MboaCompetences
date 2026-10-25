using MboaCompetences.Core.Models;
using MboaCompetences.Services;
using Microsoft.Maui.Graphics;

namespace MboaCompetences.Drawables;

/// <summary>Radar comparant les compétences dans les différents domaines de la vie.</summary>
public class RadarDrawable : IDrawable
{
    public IReadOnlyDictionary<Domaine, double>? Avant { get; set; }

    public IReadOnlyDictionary<Domaine, double> Apres { get; set; } = new Dictionary<Domaine, double>();

    public void Draw(ICanvas canvas, RectF zone)
    {
        var domaines = Enum.GetValues<Domaine>();
        var cx = zone.Center.X;
        var cy = zone.Center.Y + 4;
        var rayon = Math.Min(zone.Width * 0.5f - 62, zone.Height * 0.5f - 30);

        PointF Point(int i, double valeur, float plus = 0)
        {
            var angle = (-90 + i * 360.0 / domaines.Length) * Math.PI / 180;
            var r = rayon * (float)valeur / 100f + plus;
            return new PointF(cx + r * (float)Math.Cos(angle), cy + r * (float)Math.Sin(angle));
        }

        PathF Polygone(Func<int, PointF> point)
        {
            var chemin = new PathF();
            for (var i = 0; i < domaines.Length; i++)
            {
                var p = point(i);
                if (i == 0) chemin.MoveTo(p); else chemin.LineTo(p);
            }
            chemin.Close();
            return chemin;
        }

        canvas.StrokeColor = Palette.Bord;
        canvas.StrokeSize = 1;
        foreach (var niveau in new[] { 25, 50, 75, 100 })
            canvas.DrawPath(Polygone(i => Point(i, niveau)));
        for (var i = 0; i < domaines.Length; i++)
            canvas.DrawLine(cx, cy, Point(i, 100).X, Point(i, 100).Y);

        if (Avant is not null)
        {
            var avant = Polygone(i => Point(i, Avant[domaines[i]]));
            canvas.FillColor = Palette.Rouge.WithAlpha(0.12f);
            canvas.FillPath(avant);
            canvas.StrokeColor = Palette.Rouge;
            canvas.StrokeSize = 2;
            canvas.StrokeDashPattern = [5, 3];
            canvas.DrawPath(avant);
            canvas.StrokeDashPattern = null;
        }

        var apres = Polygone(i => Point(i, Apres.GetValueOrDefault(domaines[i])));
        canvas.FillColor = Palette.Vert.WithAlpha(0.28f);
        canvas.FillPath(apres);
        canvas.StrokeColor = Palette.Vert;
        canvas.StrokeSize = 2.5f;
        canvas.DrawPath(apres);

        canvas.FontColor = Palette.Texte;
        canvas.FontSize = 11;
        for (var i = 0; i < domaines.Length; i++)
        {
            var p = Point(i, 100, 12);
            var haut = Math.Abs(p.X - cx) < 6;
            var droite = p.X > cx;
            const float largeur = 96;
            var x = haut ? p.X - largeur / 2 : droite ? p.X : p.X - largeur;
            var alignement = haut ? HorizontalAlignment.Center : droite ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            canvas.DrawString(Catalogue.Nom(domaines[i]), x, p.Y - 8, largeur, 16, alignement, VerticalAlignment.Center);
        }
    }
}
