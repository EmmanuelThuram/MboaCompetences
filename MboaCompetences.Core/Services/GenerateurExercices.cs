using MboaCompetences.Core.Models;

namespace MboaCompetences.Core.Services;

/// <summary>
/// Moteur de texte dynamique : injecte les éléments locaux (marchés, articles, prix en FCFA, prénoms)
/// dans des modèles de situations-problèmes mathématiques, grammaticales ou d'éveil.
/// </summary>
public class GenerateurExercices
{
    private static readonly int[] Billets = [500, 1000, 2000, 5000, 10000];

    private readonly DictionnaireLocal _dico;
    private readonly Random _rng;

    public GenerateurExercices(DictionnaireLocal dico, Random? rng = null)
    {
        _dico = dico;
        _rng = rng ?? new Random();
    }

    public Exercice Generer(Competence competence, int niveau) => competence.Code switch
    {
        "MA-ADD" => Additionner(niveau),
        "MA-SOU" => Soustraire(niveau),
        "MA-MON" => RendreMonnaie(),
        "MA-MUL" => Multiplier(niveau),
        "MA-DIV" => Partager(niveau),
        "MA-REM" => Remise(),
        "FR-CON" => ConjuguerPresent(),
        "FR-FUT" => ConjuguerFutur(),
        "FR-PLU" => Pluriel(),
        "FR-ACC" => AccorderAdjectif(),
        _ => QuestionEveil(competence.Code)
    };

    // ---------------------------------------------------------------- mathématiques

    private Exercice Additionner(int niveau)
    {
        var nb = niveau <= 1 ? 2 : 3;
        var articles = ArticlesAbordables(niveau).OrderBy(_ => _rng.Next()).Take(nb).ToList();
        var (prenom, marche) = (Pick(_dico.Prenoms), Pick(_dico.Marches));
        var achats = string.Join(", ", articles.Select(a => $"{Quantite(a, 1)} à {Formats.Fcfa(a.PrixUnitaire)}"));
        var total = articles.Sum(a => a.PrixUnitaire);
        var calcul = string.Join(" + ", articles.Select(a => Formats.Nombre(a.PrixUnitaire)));

        return Calcul("MA-ADD",
            $"Au {marche.Valeur} de {marche.Detail}, {prenom.Valeur} achète {achats}. Combien dépense-t-{Pron(prenom)} en tout ?",
            total,
            "Additionne le prix de chaque article.",
            $"{calcul} = {Formats.Fcfa(total)}.");
    }

    private Exercice Soustraire(int niveau)
    {
        var article = Pick(ArticlesAbordables(niveau));
        var (prenom, marche) = (Pick(_dico.Prenoms), Pick(_dico.Marches));
        var qte = niveau <= 1 ? 1 : _rng.Next(1, 4);
        var depense = qte * article.PrixUnitaire;
        var billet = Billets.First(b => b > depense);
        var argent = billet;

        return Calcul("MA-SOU",
            $"Au {marche.Valeur} de {marche.Detail}, {prenom.Valeur} a {Formats.Fcfa(argent)}. {Maj(Pron(prenom))} achète {Quantite(article, qte)} (vendu {Formats.Fcfa(article.PrixUnitaire)} {article.Unite}). Combien lui reste-t-il ?",
            argent - depense,
            qte > 1 ? "Calcule d'abord la dépense : quantité × prix, puis retire-la de l'argent de départ." : "Retire le prix de l'achat de l'argent de départ.",
            qte > 1
                ? $"{qte} × {Formats.Nombre(article.PrixUnitaire)} = {Formats.Fcfa(depense)} ; {Formats.Nombre(argent)} − {Formats.Nombre(depense)} = {Formats.Fcfa(argent - depense)}."
                : $"{Formats.Nombre(argent)} − {Formats.Nombre(depense)} = {Formats.Fcfa(argent - depense)}.");
    }

    private Exercice RendreMonnaie()
    {
        var article = Pick(ArticlesAbordables(2));
        var (prenom, marche) = (Pick(_dico.Prenoms), Pick(_dico.Marches));
        var qte = _rng.Next(2, 6);
        while (qte * article.PrixUnitaire > 9500) qte--;
        var total = qte * article.PrixUnitaire;
        var billet = Billets.First(b => b > total);

        return Calcul("MA-MON",
            $"Au {marche.Valeur} de {marche.Detail}, {prenom.Valeur} achète {Quantite(article, qte)} (vendu {Formats.Fcfa(article.PrixUnitaire)} {article.Unite}). {Maj(Pron(prenom))} donne un billet de {Formats.Fcfa(billet)}. Combien le vendeur doit-il lui rendre ?",
            billet - total,
            $"Calcule d'abord le prix total des {qte} {article.Detail}{(article.Detail is "kg" or "tas" ? "" : "s")}.",
            $"{qte} × {Formats.Nombre(article.PrixUnitaire)} = {Formats.Fcfa(total)} ; {Formats.Nombre(billet)} − {Formats.Nombre(total)} = {Formats.Fcfa(billet - total)}.");
    }

    private Exercice Multiplier(int niveau)
    {
        var article = Pick(ArticlesAbordables(niveau));
        var (prenom, marche) = (Pick(_dico.Prenoms), Pick(_dico.Marches));
        var qte = niveau <= 3 ? _rng.Next(3, 10) : _rng.Next(6, 13);

        return Calcul("MA-MUL",
            $"Au {marche.Valeur} de {marche.Detail}, {prenom.Valeur} achète {Quantite(article, qte)}. {Maj(article.Unite)} coûte {Formats.Fcfa(article.PrixUnitaire)}. Combien paie-t-{Pron(prenom)} ?",
            qte * article.PrixUnitaire,
            "Le prix d'une unité est répété autant de fois que la quantité : c'est une multiplication.",
            $"{qte} × {Formats.Nombre(article.PrixUnitaire)} = {Formats.Fcfa(qte * article.PrixUnitaire)}.");
    }

    private Exercice Partager(int niveau)
    {
        var prenom = Pick(_dico.Prenoms);
        var ville = Pick(_dico.Villes).Valeur;
        var k = niveau <= 4 ? _rng.Next(2, 10) : _rng.Next(3, 13);
        var q = _rng.Next(2, 13) * 50;
        var n = k * q;
        var enonce = _rng.Next(3) switch
        {
            0 => $"{prenom.Valeur} a gagné {Formats.Fcfa(n)} en vendant des beignets à {ville}. {Maj(Pron(prenom))} partage cette somme équitablement entre {k} frères et sœurs. Combien chacun reçoit-il ?",
            1 => $"À {ville}, {k} amis achètent ensemble un ballon à {Formats.Fcfa(n)}. Ils paient tous la même somme. Combien chacun paie-t-il ?",
            _ => $"Une classe {De(ville)} a collecté {Formats.Fcfa(n)} pour acheter des cahiers. L'argent est réparti équitablement entre {k} groupes. Combien reçoit chaque groupe ?"
        };

        return Calcul("MA-DIV", enonce, q,
            "Partager équitablement, c'est diviser la somme par le nombre de parts.",
            $"{Formats.Nombre(n)} ÷ {k} = {Formats.Fcfa(q)} (vérifie : {k} × {Formats.Nombre(q)} = {Formats.Nombre(n)}).");
    }

    private Exercice Remise()
    {
        (string nom, int prix)[] objets =
        [
            ("un pagne", 4500), ("un sac d'école", 6000), ("une paire de sandales", 3500),
            ("un ballon", 5000), ("une robe", 8000), ("un cartable", 7000)
        ];
        var (nom, prix) = objets[_rng.Next(objets.Length)];
        var marche = Pick(_dico.Marches);
        var pct = new[] { 10, 20, 25, 50 }[_rng.Next(4)];
        var reduction = prix * pct / 100;

        return Calcul("MA-REM",
            $"Au {marche.Valeur} de {marche.Detail}, {nom} coûte {Formats.Fcfa(prix)}. Le vendeur fait une remise de {pct} %. Quel prix faut-il payer ?",
            prix - reduction,
            $"Calcule d'abord {pct} % de {Formats.Nombre(prix)} (la remise), puis retire-la du prix.",
            $"{pct} % de {Formats.Nombre(prix)} = {Formats.Fcfa(reduction)} ; {Formats.Nombre(prix)} − {Formats.Nombre(reduction)} = {Formats.Fcfa(prix - reduction)}.");
    }

    private static Exercice Calcul(string code, string enonce, int reponse, string indice, string explication) => new()
    {
        CompetenceCode = code,
        Enonce = enonce,
        ReponseNombre = reponse,
        Unite = "FCFA",
        Indice = indice,
        Explication = explication
    };

    private IReadOnlyList<VariableLocale> ArticlesAbordables(int niveau) =>
        niveau <= 1 ? _dico.Articles.Where(a => a.PrixUnitaire <= 500).ToList() : _dico.Articles;

    private static string Quantite(VariableLocale article, int qte)
    {
        var unite = article.Detail;
        var pluriel = qte > 1 && unite is not ("kg" or "tas") ? "s" : "";
        return $"{qte} {unite}{pluriel} {article.Valeur}";
    }

    // ---------------------------------------------------------------- français

    private static readonly (string inf, string radical)[] Verbes =
    [
        ("chanter", "chant"), ("marcher", "march"), ("danser", "dans"), ("regarder", "regard"),
        ("laver", "lav"), ("cuisiner", "cuisin"), ("porter", "port"), ("jouer", "jou")
    ];

    private Exercice ConjuguerPresent()
    {
        var prenom = Pick(_dico.Prenoms);
        (string sujet, string fin)[] sujets =
        [
            ("je", "e"), ("tu", "es"), (prenom.Valeur, "e"), ("les enfants", "ent"),
            ("nous", "ons"), ("vous", "ez"), ("les vendeuses", "ent")
        ];
        var (sujet, fin) = sujets[_rng.Next(sujets.Length)];
        var (inf, radical) = Verbes[_rng.Next(Verbes.Length)];
        var ville = Pick(_dico.Villes).Valeur;
        var fausses = new[] { "e", "es", "ons", "ez", "ent" }.Where(f => f != fin).OrderBy(_ => _rng.Next()).Take(2);

        return Qcm("FR-CON", $"Complète la phrase : à {ville}, {sujet} ___ ({inf}).",
            radical + fin, fausses.Select(f => radical + f),
            "Cherche le sujet de la phrase : c'est lui qui décide de la terminaison du verbe.",
            $"Avec « {sujet} », le verbe {inf} se termine par « {fin} » au présent : {sujet} {radical}{fin}.");
    }

    private Exercice ConjuguerFutur()
    {
        var prenom = Pick(_dico.Prenoms);
        (string sujet, string fin)[] sujets =
        [
            ("je", "ai"), ("tu", "as"), (prenom.Valeur, "a"), ("nous", "ons"), ("vous", "ez"),
            ("les enfants", "ont"), ("les vendeuses", "ont")
        ];
        var (sujet, fin) = sujets[_rng.Next(sujets.Length)];
        var (inf, _) = Verbes[_rng.Next(Verbes.Length)];
        var ville = Pick(_dico.Villes).Valeur;
        var fausses = new[] { "ai", "as", "a", "ons", "ez", "ont" }.Where(f => f != fin).OrderBy(_ => _rng.Next()).Take(2);

        return Qcm("FR-FUT", $"Complète la phrase : demain, à {ville}, {sujet} ___ ({inf}).",
            inf + fin, fausses.Select(f => inf + f),
            "Au futur, on garde l'infinitif entier et on ajoute la terminaison du sujet.",
            $"Au futur, l'infinitif « {inf} » reçoit « {fin} » avec « {sujet} » : {sujet} {inf}{fin}.");
    }

    private static readonly (string sing, string plur)[] Noms =
    [
        ("manioc", "maniocs"), ("banane", "bananes"), ("tomate", "tomates"), ("marché", "marchés"),
        ("couteau", "couteaux"), ("chou", "choux"), ("journal", "journaux"), ("cheval", "chevaux"),
        ("bateau", "bateaux"), ("poisson", "poissons"), ("animal", "animaux"), ("oiseau", "oiseaux"),
        ("bijou", "bijoux"), ("fruit", "fruits")
    ];

    private Exercice Pluriel()
    {
        var (sing, plur) = Noms[_rng.Next(Noms.Length)];
        var pool = new[] { sing + "s", sing + "x", sing, sing + "es" }.Where(x => x != plur).Distinct();
        var fausses = pool.OrderBy(_ => _rng.Next()).Take(2);
        var regle = plur.EndsWith("eaux") || plur.EndsWith("oux") ? "Certains noms en « -eau » ou « -ou » prennent un « x » au pluriel."
            : plur.EndsWith("aux") ? "Les noms en « -al » font leur pluriel en « -aux »."
            : "En général, on ajoute un « s » au nom pour faire le pluriel.";

        return Qcm("FR-PLU", $"Quel est le pluriel de « {sing} » ?", plur, fausses,
            "Regarde la fin du mot : elle te dit comment former le pluriel.",
            $"{regle} Un {sing} → des {plur}.");
    }

    private static readonly (string sujet, bool feminin, bool pluriel)[] GroupesNominaux =
    [
        ("Le marché", false, false), ("La mangue", true, false), ("Les marchés", false, true),
        ("Les mangues", true, true), ("Le poisson", false, false), ("La banane", true, false),
        ("Les bananes", true, true), ("Le manioc", false, false)
    ];

    private static readonly string[] Adjectifs = ["grand", "petit", "mûr", "joli", "content", "fort"];

    private Exercice AccorderAdjectif()
    {
        var (sujet, fem, plur) = GroupesNominaux[_rng.Next(GroupesNominaux.Length)];
        var adj = Adjectifs[_rng.Next(Adjectifs.Length)];
        string Forme(bool f, bool p) => adj + (f ? "e" : "") + (p ? "s" : "");
        var bonne = Forme(fem, plur);
        var fausses = new[] { Forme(false, false), Forme(true, false), Forme(false, true), Forme(true, true) }
            .Where(f => f != bonne).OrderBy(_ => _rng.Next()).Take(2);
        var ville = Pick(_dico.Villes).Valeur;

        return Qcm("FR-ACC", $"Complète : {sujet} {De(ville)} {(plur ? "sont" : "est")} ___ ({adj}).", bonne, fausses,
            "L'adjectif s'accorde avec le nom : cherche s'il est féminin ou masculin, singulier ou pluriel.",
            $"« {sujet} » est {(fem ? "féminin" : "masculin")} {(plur ? "pluriel" : "singulier")} : l'adjectif devient « {bonne} ».");
    }

    private Exercice Qcm(string code, string enonce, string bonne, IEnumerable<string> fausses, string indice, string explication)
    {
        var choix = fausses.Prepend(bonne).Distinct().OrderBy(_ => _rng.Next()).ToList();
        return new Exercice
        {
            CompetenceCode = code,
            Enonce = enonce,
            EstQcm = true,
            Choix = choix,
            IndexBonneReponse = choix.IndexOf(bonne),
            Indice = indice,
            Explication = explication
        };
    }

    // ---------------------------------------------------------------- éveil

    private record QuestionEveilModele(string Question, string Bonne, string Fausse1, string Fausse2, string Explication);

    private static readonly Dictionary<string, QuestionEveilModele[]> BanqueEveil = new()
    {
        ["EV-PAL"] =
        [
            new("À {V}, la saison des pluies commence. {P} veut éviter le paludisme. Que doit faire {P} ?",
                "Dormir sous une moustiquaire imprégnée", "Boire de l'eau de la mare", "Dormir sans moustiquaire",
                "Les moustiques piquent la nuit : la moustiquaire protège de leurs piqûres."),
            new("Comment attrape-t-on le paludisme ?",
                "Par la piqûre d'un moustique", "En touchant un malade", "En mangeant des mangues",
                "Le paludisme est transmis par la piqûre du moustique anophèle."),
            new("Où les moustiques pondent-ils leurs œufs ?",
                "Dans les eaux stagnantes", "Dans le sable sec", "Dans les arbres secs",
                "Les larves se développent dans l'eau qui ne bouge pas : il faut vider les flaques et les vieux pneus."),
            new("{P} a de la fièvre et des frissons. Que faut-il faire en premier ?",
                "Aller au centre de santé faire un test", "Attendre une semaine", "Prendre des médicaments vendus dans la rue",
                "Seul un test au centre de santé confirme le paludisme et permet un bon traitement.")
        ],
        ["EV-HYG"] =
        [
            new("Quand faut-il se laver les mains avec du savon ?",
                "Avant de manger et après les toilettes", "Seulement le dimanche", "Seulement quand elles sont très sales",
                "Les mains sales transportent des microbes : le savon les élimine."),
            new("Comment rendre l'eau du puits plus sûre à boire ?",
                "La faire bouillir ou la filtrer", "La laisser au soleil sans couvercle", "Y ajouter du sable",
                "Faire bouillir l'eau tue les microbes qui donnent la diarrhée."),
            new("Combien de fois faut-il se brosser les dents ?",
                "Au moins deux fois par jour", "Une fois par mois", "Seulement avant les fêtes",
                "Se brosser les dents matin et soir évite les caries."),
            new("{P} a acheté des fruits au {M}. Que doit faire {P} avant de les manger ?",
                "Les laver avec de l'eau propre", "Les manger directement", "Les poser par terre",
                "Les fruits ont été manipulés : il faut les laver pour enlever la saleté et les microbes.")
        ],
        ["EV-DEC"] =
        [
            new("Où {P} doit-{pron} jeter l'emballage de son sachet de beignets ?",
                "Dans la poubelle", "Dans le caniveau", "Dans la rivière",
                "Les déchets jetés dans le caniveau bouchent l'eau et provoquent des inondations."),
            new("Pourquoi ne faut-il pas jeter les sachets plastiques dans la nature ?",
                "Ils ne pourrissent pas et polluent le sol et l'eau", "Ils sont bons pour les animaux", "Ils font pousser les plantes",
                "Le plastique met des centaines d'années à disparaître."),
            new("Que peut-on faire avec les épluchures de manioc et de banane ?",
                "Du compost pour le jardin", "Les jeter dans la rue", "Les brûler près des maisons",
                "Le compost transforme les déchets de cuisine en engrais naturel."),
            new("Que veut dire « recycler » ?",
                "Réutiliser un objet pour en faire un nouveau", "Jeter plus vite", "Cacher les déchets",
                "Recycler évite de produire plus de déchets.")
        ],
        ["EV-ARB"] =
        [
            new("Pourquoi les arbres sont-ils importants ?",
                "Ils donnent de l'ombre, de l'air pur et des fruits", "Ils ne servent à rien", "Ils font du bruit",
                "Les arbres nettoient l'air et protègent le sol."),
            new("À {V}, que doit-on faire quand on coupe un arbre ?",
                "En planter un autre", "Rien du tout", "Mettre le feu à la brousse",
                "Replanter permet de garder la forêt pour demain."),
            new("Pourquoi les feux de brousse sont-ils dangereux ?",
                "Ils détruisent les plantes et les animaux", "Ils font tomber la pluie", "Ils font pousser les arbres plus vite",
                "Le feu brûle les habitations des animaux et appauvrit la terre."),
            new("Quel animal vit dans nos forêts et doit être protégé ?",
                "Le gorille", "La poule", "Le chat de maison",
                "Le gorille est une espèce menacée : il ne faut pas le chasser.")
        ],
        ["EV-ROU"] =
        [
            new("Quelle couleur de feu te dit de t'arrêter ?",
                "Rouge", "Vert", "Bleu", "Au feu rouge, tous les piétons et les véhicules doivent s'arrêter."),
            new("Où doit marcher un piéton quand il n'y a pas de trottoir ?",
                "Au bord de la route, face aux véhicules", "Au milieu de la route", "Derrière les motos",
                "Face aux véhicules, on les voit arriver et on peut se pousser à temps."),
            new("Que fait-on avant de traverser la route ?",
                "On regarde à gauche et à droite", "On ferme les yeux", "On court sans regarder",
                "On s'assure qu'aucun véhicule n'arrive avant de traverser."),
            new("Que doit porter un motard pour être protégé ?",
                "Un casque", "Un chapeau", "Des lunettes de soleil",
                "Le casque protège la tête en cas de chute.")
        ],
        ["EV-CIV"] =
        [
            new("Quelles sont les couleurs du drapeau du Cameroun ?",
                "Vert, rouge et jaune, avec une étoile", "Bleu, blanc et rouge", "Noir, jaune et rouge",
                "Le drapeau camerounais est vert, rouge, jaune, avec une étoile jaune au centre."),
            new("Quelle est la capitale politique du Cameroun ?",
                "Yaoundé", "Douala", "Garoua",
                "Yaoundé est la capitale politique ; Douala est la capitale économique."),
            new("En classe, que fait-on pour prendre la parole ?",
                "On lève la main et on attend son tour", "On crie plus fort que les autres", "On se promène dans la classe",
                "Respecter son tour de parole permet à tout le monde de s'entendre."),
            new("{P} voit un camarade tomber dans la cour de l'école. Que fait {P} ?",
                "{Pron} l'aide à se relever", "{Pron} rit et s'en va", "{Pron} fait semblant de ne pas voir",
                "Aider les autres est une règle du vivre ensemble.")
        ]
    };

    private Exercice QuestionEveil(string code)
    {
        var modele = BanqueEveil[code][_rng.Next(BanqueEveil[code].Length)];
        var prenom = Pick(_dico.Prenoms);
        var marche = Pick(_dico.Marches);
        var ville = marche.Detail;

        string Remplacer(string texte) => texte
            .Replace("{P}", prenom.Valeur).Replace("{V}", ville).Replace("{M}", marche.Valeur)
            .Replace("{Pron}", Maj(Pron(prenom))).Replace("{pron}", Pron(prenom));

        return Qcm(code, Remplacer(modele.Question), Remplacer(modele.Bonne),
            [Remplacer(modele.Fausse1), Remplacer(modele.Fausse2)],
            "Élimine d'abord les réponses qui te semblent dangereuses ou impossibles.",
            Remplacer(modele.Explication));
    }

    // ---------------------------------------------------------------- utilitaires

    private T Pick<T>(IReadOnlyList<T> liste) => liste[_rng.Next(liste.Count)];

    private static string Pron(VariableLocale prenom) => prenom.Detail == "F" ? "elle" : "il";

    /// <summary>« de Douala » ou « d'Ebolowa ».</summary>
    private static string De(string ville) => "AEIOUÉ".Contains(ville[0]) ? $"d'{ville}" : $"de {ville}";

    private static string Maj(string texte) => texte.Length == 0 ? texte : char.ToUpper(texte[0]) + texte[1..];
}
