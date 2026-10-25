# MboaCompétences (APC-MALIN)

Application mobile **.NET MAUI / C#** (UI en XAML) qui accompagne l'Approche Par Compétences au primaire au Cameroun (SIL à CM2).
Elle fonctionne **100 % hors-ligne** : toutes les données sont dans une base SQLite locale.

## Ce que fait l'application

| Écran | Rôle |
|---|---|
| **Élèves** | Choix du profil, ajout / suppression d'un enfant, chargement d'un jeu de démonstration (5 semaines d'historique) |
| **Exercices** | Mathématiques, Français, Éveil : séries de 5 situations-problèmes contextualisées au Cameroun (marché Mokolo, FCFA, manioc, koki, paludisme…) |
| **Remédiation** | Série ciblée sur les compétences au rouge, indice visible dès le départ |
| **Résultat** | Étoiles, réussites au 1er essai, temps moyen, feux des compétences travaillées |
| **Espace parent** | 4 onglets : Compétences (feux), Indicateurs (TAG, IVR, TRE), Progrès (courbe + radar), Bilan (Avant / Après + export PDF) |

### Principe d'un exercice
1 énoncé → 1 réponse (saisie ou QCM) → si faux, indice et **2e essai** → correction expliquée.
Seule la réussite au **1er essai** compte pour le TAG. Le temps est mesuré avec un `Stopwatch`.

### Les feux tricolores
Calculés sur les 5 derniers exercices d'une compétence :

* **Vert** : au moins 3 exercices et 75 % de réussite au 1er essai ou plus
* **Rouge** : au moins 2 exercices et moins de 40 %
* **Orange** : tout le reste (en cours)
* **Gris** : à découvrir

### Les trois indicateurs
* **TAG %** : réussis au 1er essai / exercices réalisés.
* **IVR** : temps moyen par exercice (une baisse = meilleure assimilation).
* **TRE %** : compétences passées de Rouge à Vert / compétences déjà passées au Rouge. L'historique est rejoué exercice par exercice.

Le bilan **Avant / Après** coupe la période d'utilisation en deux moitiés égales et compare les deux.

## Structure

```
MboaCompetences.slnx
├── MboaCompetences.Core/      logique métier (net10.0, sans dépendance MAUI)
│   ├── Models/                Eleve, Tentative, VariableLocale, Catalogue des compétences, Exercice
│   └── Services/              DatabaseService (SQLite-net), GenerateurExercices, Analyse (feux + KPI),
│                              PlanificateurSerie, DonneesDemonstration, PdfBilanService (QuestPDF), GraphiquesSvg
├── MboaCompetences/           application MAUI (Android + Windows)
│   ├── Views/                 pages XAML
│   ├── ViewModels/            MVVM (CommunityToolkit.Mvvm)
│   ├── Drawables/             courbe et radar dessinés avec Microsoft.Maui.Graphics
│   └── Services/              état de l'application, export / partage du PDF
├── MboaCompetences.Tests/     36 tests xUnit (générateur, feux, KPI, SQLite, PDF)
└── docs/                      maquette (HTML + PDF) et exemple de bilan PDF
```

## Lancer

Ouvrir `MboaCompetences.slnx` dans Visual Studio 2026 (charge de travail « .NET MAUI »), choisir la cible **Windows Machine** ou un émulateur Android, puis F5.
Sur l'écran *Élèves*, le bouton jaune **Charger des données de démonstration** crée Aïcha (CM1) avec 5 semaines d'historique pour explorer l'Espace parent.

En ligne de commande :

```bash
dotnet test MboaCompetences.Tests
dotnet build MboaCompetences/MboaCompetences.csproj -f net10.0-windows10.0.19041.0
```

Le PDF est enregistré dans `Documents\MboaCompetences` sous Windows, et partagé via la feuille de partage Android sur téléphone.

## Technique

* Base : `sqlite-net-pcl`, fichier `mboacompetences.db3` dans le dossier de données de l'application.
* Générateur : modèles de phrases + dictionnaire local (10 marchés, 14 articles avec prix en FCFA, 16 prénoms, 10 villes), toujours résolus hors-ligne.
* PDF : QuestPDF (licence Community), graphiques en SVG intégrés.
* Graphiques à l'écran : `GraphicsView` + `IDrawable` (`CourbeDrawable`, `RadarDrawable`).
