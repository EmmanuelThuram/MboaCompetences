using MboaCompetences.Core.Services;
using MboaCompetences.Services;
using Microsoft.Maui.Graphics;

namespace MboaCompetences.Drawables;

/// <summary>Courbe de progression de l'enfant (taux d'acquisition par semaine), dessinée avec le framework graphique de MAUI.</summary>
public class CourbeDrawable : IDrawable
{
    public IReadOnlyList<PointProgression> Points { get; set; } = [];

    public void Draw(ICanvas canvas, RectF zone)
    {
        const float gauche = 34, droite = 12, haut = 10, bas = 24;
        var l = zone.Width - gauche - droite;
        var h = zone.Height - haut - bas;

        canvas.FontColor = Palette.TexteGris;
        canvas.FontSize = 10;

        foreach (var graduation in new[] { 0, 25, 50, 75, 100 })
        {
            var y = haut + h - graduation / 100f * h;
            canvas.StrokeColor = Palette.Bord;
            canvas.StrokeSize = 1;
            canvas.DrawLine(gauche, y, gauche + l, y);
            canvas.DrawString(graduation.ToString(), 0, y - 7, gauche - 6, 14, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        if (Points.Count == 0)
        {
            canvas.DrawString("Pas encore de données", zone.Left, zone.Top, zone.Width, zone.Height, HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        float X(int i) => Points.Count == 1 ? gauche + l / 2 : gauche + l * i / (Points.Count - 1);
        float Y(double v) => haut + h - (float)Math.Clamp(v, 0, 100) / 100f * h;

        if (Points.Count > 1)
        {
            var chemin = new PathF();
            chemin.MoveTo(X(0), Y(Points[0].Valeur));
            for (var i = 1; i < Points.Count; i++) chemin.LineTo(X(i), Y(Points[i].Valeur));

            canvas.StrokeColor = Palette.Vert;
            canvas.StrokeSize = 3;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.DrawPath(chemin);
        }

        // Les libellés d'axe : on en saute quelques-uns quand il y a beaucoup de semaines.
        var pas = Math.Max(1, (int)Math.Ceiling(Points.Count / 8.0));
        for (var i = 0; i < Points.Count; i++)
        {
            canvas.FillColor = Palette.Vert;
            canvas.FillCircle(X(i), Y(Points[i].Valeur), 4);
            if (i % pas == 0 || i == Points.Count - 1)
                canvas.DrawString(Points[i].Libelle, X(i) - 20, zone.Height - bas + 6, 40, 14, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
