using MboaCompetences.Core.Models;
using MboaCompetences.Core.Services;

namespace MboaCompetences.Services;

/// <summary>Enregistre le bilan PDF sur le téléphone puis le partage (ou l'ouvre sur ordinateur).</summary>
public class ExportPdfService
{
    public async Task<string> GenererAsync(Eleve eleve, IReadOnlyList<Tentative> tentatives)
    {
        var dossier = DeviceInfo.Platform == DevicePlatform.WinUI
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MboaCompetences")
            : FileSystem.CacheDirectory;
        Directory.CreateDirectory(dossier);

        var chemin = Path.Combine(dossier, PdfBilanService.NomFichier(eleve, DateTime.Now));
        await Task.Run(() => PdfBilanService.GenererFichier(eleve, tentatives, chemin));
        return chemin;
    }

    public async Task PartagerAsync(string chemin)
    {
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest("Bilan de compétences", new ReadOnlyFile(chemin)));
            return;
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Bilan de compétences",
            File = new ShareFile(chemin)
        });
    }
}
