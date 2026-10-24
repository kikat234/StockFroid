# StockFroid — application .NET MAUI (MVVM) avec back-end local (SQLite)

Suivi des lots agricoles stockés : péremption calculée, alertes, pertes, bilan par campagne.
Les 8 écrans de la maquette sont réalisés, avec une base SQLite locale (fonctionne hors connexion),
des comptes agents enregistrés localement et un export du bilan en PDF ou CSV.

> **Important :** le projet MAUI complet n'a pas pu être compilé là où il a été écrit (pas d'accès à NuGet).
> Seules les parties sans interface (hachage des mots de passe, génération du CSV et du PDF, calculs de péremption)
> ont été compilées et exécutées dans un projet de test, et les PDF produits ont été contrôlés (structure et rendu).
> Si Visual Studio signale une erreur au premier build, copiez-la telle quelle : elle se corrige en général en une ligne.

---

## 1. Mise en place (Visual Studio)

1. **Créer un projet neuf** : *Nouveau projet → .NET MAUI App*, nommé exactement **`StockFroid`**
   (le nom fixe l'espace de noms `StockFroid` utilisé partout dans le code).
   Si l'assistant propose « Include sample content », **décochez-le**.
2. **Nettoyer le modèle** (si du contenu d'exemple a été généré) : supprimez les dossiers
   `Data`, `Models`, `PageModels`, `Pages`, `Services`, `Utilities`, ainsi que `GlobalUsings.cs`,
   `MainPage.xaml(.cs)`, `AppShell.xaml(.cs)`, `App.xaml(.cs)` et `MauiProgram.cs`.
   Gardez `Platforms/` et le reste de `Resources/` (icône, splash, polices).
3. **Packages NuGet** (*Outils → Gestionnaire de package NuGet*) :
   - installer **`CommunityToolkit.Mvvm`**, **`sqlite-net-pcl`** et **`SQLitePCLRaw.bundle_green`** (aucun autre package n'est nécessaire :
     le hachage des mots de passe et la génération du PDF utilisent uniquement .NET) ;
   - si présents et inutiles, désinstaller `Microsoft.Data.Sqlite`, `CommunityToolkit.Maui` et `Syncfusion.Maui.Toolkit`
     (deux fournisseurs SQLite ensemble peuvent entrer en conflit).
4. **Copier les fichiers** de cette archive dans le projet (même arborescence) :
   `App.*`, `AppShell.*`, `MauiProgram.cs`, `Models/`, `Data/`, `Services/`, `Utilities/`, `PageModels/`, `Pages/`,
   `Resources/Styles/Theme.xaml` et `Resources/Images/tab_*.svg`.
5. **Lancer** : cible *Windows Machine* (ou un émulateur Android), puis **F5**.
   Au premier lancement, des données de démonstration et un compte de démonstration sont créés (voir `Data/DemoData.cs`) :
   identifiant **`AGT-2026-88`**, mot de passe **`stockfroid2026`**. Vous pouvez aussi créer votre propre compte via « S'inscrire ».

## 2. Architecture MVVM

| Couche | Dossier | Rôle |
|---|---|---|
| **Vue** | `Pages/` | XAML + code-behind minimal (constructeur et `OnAppearing`) |
| **ViewModel** | `PageModels/` | Un PageModel par page (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`) |
| **Éléments d'affichage** | `PageModels/Items/` | Cartes prêtes à afficher (textes, couleurs) |
| **Modèle** | `Models/` | Entités `Campagne`, `Lot`, `Perte` + règles de péremption |
| **Services / données** | `Services/`, `Data/` | Logique métier et accès SQLite |

Les PageModels et les pages sont injectés par `MauiProgram.cs` (injection de dépendances).
La navigation utilise le Shell : onglets (Campagnes, Nouveau lot, Pertes, Profil) et pages empilées
(`campagnedetail`, `lotdetail`, `declarationperte`, `register`) avec passage de paramètres.

## 3. Les 8 écrans

| N° | Écran | Page | PageModel |
|---|---|---|---|
| 1 | Connexion | `LoginPage` | `LoginPageModel` |
| 2 | Inscription (profil agent) | `RegisterPage` | `RegisterPageModel` |
| 3 | Liste des campagnes | `CampagnesPage` | `CampagnesPageModel` |
| 4 | Détail d'une campagne (lots) | `CampagneDetailPage` | `CampagneDetailPageModel` |
| 5 | Nouveau lot | `NouveauLotPage` | `NouveauLotPageModel` |
| 6 | Détail du lot | `LotDetailPage` | `LotDetailPageModel` |
| 7 | Déclaration de perte | `DeclarationPertePage` | `DeclarationPertePageModel` |
| 8 | Pertes et bilan | `BilanPage` | `BilanPageModel` |

(+ un onglet **Profil** minimal : nom de l'agent et déconnexion.)

## 4. Ce qui est implémenté côté back-end

**Fonctions métier :**
- `PeremptionCalculator` (`Models/`) : date de péremption = récolte + durée ; état *à jour / bientôt / périmé* (seuil de 3 jours).
- `LotService.CreerLotAsync` : validation (date, durée, quantité, lieu, campagne en cours), génération du code `LOT-TOM-003`, enregistrement.
- `PerteService.DeclarerPerteAsync` : refuse une perte supérieure au stock restant ou datée hors période, puis enregistre.
- `BilanService.CalculerAsync` : stock, pertes, taux de perte, répartition par cause, lots les plus touchés, détail de chaque lot.
- `CampagneService` : liste et création de campagnes.

**Comptes agents (voir § 6) :**
- `AuthService` : inscription et connexion, comptes enregistrés dans la table `Agent` de SQLite.
- `MotDePasseHasher` : hachage PBKDF2-SHA256 avec sel aléatoire ; aucun mot de passe n'est stocké en clair.

**Export du bilan (voir § 7) :**
- `ExportService` + `RapportBuilder` : rapport de campagne en PDF ou en CSV, partagé via la feuille de partage du système.

**Volontairement non implémenté :**
- la synchronisation entre appareils (l'application est hors connexion par conception : les comptes sont propres à l'appareil) ;
- la récupération du mot de passe oublié (le lien affiche un message) ;
- la modification ou la suppression d'un compte.

## 5. Choix assumés (utiles pour la soutenance)

- Les stocks sont **toujours en kg** (la saisie en tonnes est convertie) afin que les totaux du bilan restent cohérents.
- La date de péremption n'est **jamais stockée ni saisie** : elle est calculée, donc toujours cohérente.
- L'état de péremption est exprimé par **une couleur, une icône et un texte** (jamais la couleur seule).
- Thème clair imposé, une seule palette (`Resources/Styles/Theme.xaml`), polices système, icônes SVG légères.
- Les listes utilisent `BindableLayout` dans un `ScrollView` : adapté à de petits volumes locaux.
- Le PDF est généré par un petit écrivain maison (`Utilities/PdfWriter.cs`, texte et rectangles, polices standard) plutôt que par une bibliothèque : aucune dépendance, aucune licence, fonctionne identiquement sur Android, iOS et Windows.

## 6. Comptes et sécurité

- **Inscription** (`RegisterPageModel` → `AuthService.InscrireAsync`) : l'écran contrôle tous les champs et affiche toutes les erreurs d'un coup ;
  le service revalide l'essentiel (nom, identifiant, 8 caractères minimum) et refuse un identifiant déjà utilisé. Le compte est créé puis l'agent est connecté.
- **Connexion** (`LoginPageModel` → `AuthService.ConnecterAsync`) : l'identifiant (matricule ou e-mail) est comparé sans tenir compte de la casse.
  Le message d'erreur est le même que l'identifiant soit inconnu ou le mot de passe faux, pour ne pas révéler quels comptes existent.
- **Stockage** : table `Agent` (identifiant unique, nom, affectation, sel, empreinte, nombre d'itérations, date de création).
  L'empreinte est un PBKDF2-SHA256 (210 000 itérations, sel de 16 octets propre à chaque compte), comparée à temps constant.
  Le calcul est lancé hors du thread d'interface pour ne pas figer l'écran.
- **Limites assumées** : les comptes restent sur l'appareil (pas de serveur) ; il n'y a pas de limitation des tentatives de connexion ni de récupération de mot de passe.
- Le **compte de démonstration** (`AGT-2026-88` / `stockfroid2026`) est créé par `DemoData.InsererAgentAsync` uniquement si la table est vide ;
  pour une mise en production, supprimez cet appel dans `Data/StockFroidDatabase.cs`.

## 7. Export du bilan (PDF / CSV)

Sur l'onglet **Pertes**, le bouton « Générer rapport local (PDF/CSV) » propose le format, génère le rapport de la campagne affichée
dans le dossier de cache de l'application (`exports/`), puis ouvre la feuille de partage (enregistrer, envoyer, ouvrir avec…).
Si le partage n'est pas disponible sur l'appareil, un message indique l'emplacement du fichier.

- **PDF** (A4, plusieurs pages si besoin, en-tête du tableau répété) : indicateurs clés (stock initial, perdu, restant, taux de perte avec seuil critique),
  répartition des pertes par cause avec barres, détail de tous les lots (péremption, état, quantités), historique des pertes.
- **CSV** (pour Excel en français : séparateur `;`, virgule décimale, UTF-8 avec BOM) : quatre blocs — synthèse, répartition par cause, détail des lots, historique des pertes.
  Les champs texte commençant par `=`, `+`, `-` ou `@` sont précédés d'une apostrophe pour qu'un tableur ne les exécute pas comme une formule.
- Nom de fichier : `StockFroid_Bilan_<campagne>_<date>_<heure>.pdf|csv`.
- Code : `Services/ExportService.cs` (données, fichiers, partage), `Services/RapportBuilder.cs` (contenu, sans dépendance à MAUI),
  `Utilities/PdfWriter.cs` (génération du PDF).

## 8. Points d'attention

- Sous .NET 10, `DisplayAlert` / `DisplayActionSheet` / `DisplayPromptAsync` génèrent un simple avertissement d'obsolescence (sans effet sur le fonctionnement).
- Sous Windows, les champs `Entry` gardent leur bordure native ; l'affichage est plus fidèle à la maquette sur Android.
- Pour repartir de données vierges : désinstaller l'application (ou supprimer `stockfroid.db3` dans le dossier de données de l'application).
