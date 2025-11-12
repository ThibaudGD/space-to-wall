# Space to Wall - Paint Wall Creator

Extension Revit pour créer automatiquement des murs de peinture (5mm d'épaisseur) à partir des pièces dans un projet.

## Fonctionnalités

- **Création automatique de murs de peinture** : Génère des murs de 5mm d'épaisseur le long des contours de toutes les pièces
- **Paramètres personnalisés** : Les murs créés contiennent des informations sur la pièce (nom, numéro, finition)
- **Traitement asynchrone** : Utilise Revit.Async avec handlers personnalisés pour une exécution fluide
- **Support multi-version** : Compatible Revit 2022, 2023 et 2024
- **Gestion facile** : Boutons dans le ruban Revit pour créer et supprimer les murs de peinture

## Architecture

Le projet utilise :
- **Autodesk Revit SDK** : Support des versions 2022, 2023, 2024
- **Revit.Async v2.1.1** : Exécution asynchrone avec pattern "Define Your Own Handler"
- **.NET Framework 4.8** (Revit 2022-2024)

### Structure du projet

```
space-to-wall/
├── space-to-wall.app/                # Extension Revit principale
│   ├── Commands/                     # Commandes Revit (UI entry points)
│   │   ├── CreatePaintWallsCommand.cs
│   │   └── DeletePaintWallsCommand.cs
│   ├── Handlers/                     # Handlers async (business logic)
│   │   ├── CreatePaintWallsHandler.cs
│   │   └── DeletePaintWallsHandler.cs
│   ├── Models/                       # DTOs et modèles de données
│   │   ├── CreatePaintWallsParameter.cs
│   │   ├── CreatePaintWallsResult.cs
│   │   └── DeletePaintWallsResult.cs
│   ├── Properties/
│   ├── Application.cs                # Point d'entrée + enregistrement des handlers
│   ├── SpaceToWall.addin             # Manifest template
│   └── space-to-wall.app.csproj      # Fichier de projet SDK-style
├── space-to-wall.installer/          # Application CLI pour installation
│   ├── Program.cs                    # Logique d'installation
│   ├── install.bat                   # Script batch d'installation
│   ├── README.md                     # Documentation de l'installateur
│   └── space-to-wall.installer.csproj
├── build-installer-package.bat       # Script pour créer le package complet
├── deploy.bat                        # Script de déploiement développeur
└── README.md
```

## Installation

### Pour les utilisateurs finaux

#### Installateur automatique (Recommandée)

1. Téléchargez `space-to-wall.installer.exe`
2. Double-cliquez sur l'exécutable
3. L'installateur va :
   - Détecter automatiquement les versions Revit installées sur votre système
   - Proposer d'installer les versions correspondantes (2022, 2023, 2024)
   - Copier les fichiers `.addin` dans `%AppData%\Autodesk\Revit\Addins\{version}`
   - Copier les DLL dans `%AppData%\Autodesk\ApplicationPlugins\SpaceToWall\{version}`
4. Suivez les instructions à l'écran
5. Redémarrez Revit

**Avantages :**
- ✅ Un seul fichier .exe à télécharger (toutes les versions embarquées)
- ✅ Détection automatique des versions Revit installées
- ✅ Installation multi-version en une seule opération
- ✅ Pas de fichiers ZIP à gérer manuellement

#### Installation manuelle depuis ZIP

Voir la section [Installation manuelle](#installation-manuelle) ci-dessous.

### Pour les développeurs

#### Méthode rapide (Recommandée)

**Sur Windows**, utilisez le script de déploiement :

```batch
# Pour Revit 2024 (par défaut)
deploy.bat

# Pour une version spécifique
deploy.bat 2023
deploy.bat 2024
deploy.bat 2025
```

Le script va :
1. Compiler le projet pour la version Revit ciblée
2. Copier automatiquement les fichiers vers `%AppData%\Autodesk\Revit\Addins\{version}`
3. Préparer le manifest avec le bon chemin d'assembly

### Installation manuelle

#### 1. Compilation du projet

```bash
cd space-to-wall.app

# Pour Revit 2024 (défaut)
dotnet build --configuration Release

# Pour Revit 2023
dotnet build --configuration "Release 2023"

# Pour Revit 2025
dotnet build --configuration "Release 2025"
```

#### 2. Copie des fichiers

Les fichiers compilés se trouvent dans `bin/Release/{version}/` :

Copiez vers `%AppData%\Autodesk\Revit\Addins\{version}\`:
- `space_to_wall.app.dll`
- `Revit.Async.dll`
- `space_to_wall.app.addin`

### 3. Vérification

1. Lancez Revit
2. Vous devriez voir un nouvel onglet "Space to Wall" dans le ruban
3. L'onglet contient deux boutons :
   - **Créer Murs de Peinture**
   - **Supprimer Murs de Peinture**

## Utilisation

### Créer des murs de peinture

1. Ouvrez un projet Revit contenant des pièces (Rooms)
2. Cliquez sur **"Créer Murs de Peinture"** dans le ruban
3. L'extension va :
   - Créer un type de mur "Peinture - 5mm" (si non existant)
   - Générer des murs le long des contours de chaque pièce
   - Affecter les paramètres (nom de pièce, numéro, finition)
4. Un message affiche le nombre de murs créés

### Supprimer les murs de peinture

1. Cliquez sur **"Supprimer Murs de Peinture"**
2. Tous les murs de type "Peinture - 5mm" seront supprimés
3. Un message de confirmation s'affiche

## Paramètres personnalisés

Les murs de peinture créés contiennent les paramètres suivants :

- **Peinture_NomPiece** : Nom de la pièce
- **Peinture_NumeroPiece** : Numéro de la pièce
- **Peinture_Finition** : Finition des murs (si définie dans la pièce)
- **Commentaires** : "Mur de peinture généré automatiquement"

> **Note** : Pour utiliser les paramètres personnalisés, vous devez les créer dans votre fichier de paramètres partagés ou décommenter la section `EnsureSharedParametersExist()` dans `PaintWallCreator.cs`

## Distribution

### Créer l'installateur autonome pour les utilisateurs

Un seul script fait tout le travail :

```batch
build-all-versions.bat
```

Ce script va :
1. ✅ Compiler l'extension pour Revit 2022, 2023 et 2024
2. ✅ Créer les ZIP pour chaque version (DLL uniquement)
3. ✅ Embarquer les ZIP et le template .addin dans l'installateur
4. ✅ Compiler l'installateur en un exécutable unique autonome

L'installateur gère automatiquement :
- ✅ Installation des DLL dans `ApplicationPlugins\SpaceToWall\{version}`
- ✅ Copie du fichier `.addin` personnalisé par version dans `Revit\Addins\{version}`
- ✅ Remplacement de `{VERSIONS}` par la version appropriée dans chaque .addin

Le fichier final à distribuer :
```
build-output\installer\space-to-wall.installer.exe
```

**C'est tout !** Un seul fichier .exe contenant toutes les versions Revit.

### Avantages de cette approche

- ✅ **Distribution simplifiée** : Un seul .exe à distribuer
- ✅ **Pas de fichiers manquants** : Les ZIP sont embarqués
- ✅ **Installation intelligente** : Détection automatique des versions Revit
- ✅ **Expérience utilisateur optimale** : Aucune manipulation de fichiers ZIP
- ✅ **Installation offline** : Fonctionne sans connexion Internet

## Développement

### Prérequis

- Visual Studio 2022, Rider ou VS Code
- .NET SDK 8.0+ (pour la compilation)
- Revit 2023, 2024 ou 2025

### Mode Debug

En mode Debug, l'addin est automatiquement copié vers `%AppData%\Autodesk\Revit\Addins\` avec un chemin absolu pour faciliter le debugging :

```bash
dotnet build --configuration Debug
```

Le manifest généré contiendra le chemin absolu vers votre DLL compilée.

### Personnalisation

#### Changer l'épaisseur des murs

Dans `PaintWallHandlers.cs`, méthode `GetOrCreatePaintWallType()` :

```csharp
structure.SetLayerWidth(0, 5.0 / 304.8); // 5mm en pieds
```

Changez `5.0` à la valeur souhaitée en millimètres.

#### Modifier les paramètres

Dans `PaintWallHandlers.cs`, constantes en haut de fichier :

```csharp
private const string PARAM_ROOM_NAME = "Peinture_NomPiece";
private const string PARAM_ROOM_NUMBER = "Peinture_NumeroPiece";
private const string PARAM_ROOM_FINISH = "Peinture_Finition";
```

### Code conditionnel par version

Le projet définit automatiquement `REVIT2023`, `REVIT2024`, `REVIT2025` selon la configuration :

```csharp
#if REVIT2025
    // Code spécifique à Revit 2025
#elif REVIT2024
    // Code spécifique à Revit 2024
#endif
```

## Architecture Revit.Async

Ce projet utilise le pattern **"Define Your Own Handler"** de [Revit.Async](https://github.com/KennanChan/Revit.Async) :

### Handlers personnalisés (PaintWallHandlers.cs)

- `CreatePaintWallsHandler` : Hérite de `SyncGenericExternalEventHandler`
- `DeletePaintWallsHandler` : Hérite de `SyncGenericExternalEventHandler`
- Classes DTO pour paramètres et résultats

### Enregistrement (Application.cs)

```csharp
public Result OnStartup(UIControlledApplication application)
{
    RevitTask.Initialize(application);
    RevitTask.RegisterGlobal(new CreatePaintWallsHandler());
    RevitTask.RegisterGlobal(new DeletePaintWallsHandler());
    // ...
}
```

### Invocation (Commands)

```csharp
var result = await RevitTask.RaiseGlobal<CreatePaintWallsHandler, 
                                         CreatePaintWallsParameter, 
                                         CreatePaintWallsResult>(param);
```

### Avantages

- ✅ Séparation propre : logique métier dans les handlers
- ✅ Testabilité : handlers peuvent être testés unitairement
- ✅ Réutilisabilité : handlers peuvent être appelés depuis n'importe où
- ✅ Typage fort : paramètres et résultats typés
- ✅ Pas de blocage UI : exécution asynchrone

## Améliorations du projet

### Configuration multi-version

- Support de Revit 2023, 2024, 2025
- Output séparé par version : `bin/Release/{version}/`
- SDK Revit résolu automatiquement : `<PackageReference Include="Autodesk.Revit.SDK" Version="$(RevitVersion).*" />`

### Build propre

- Warnings MSB3246 supprimés (DLLs Revit SDK sur Linux)
- DLLs Revit SDK non copiées dans l'output (Target `PreventRevitSDKCopyLocal`)
- Manifest auto-généré avec le bon chemin

### Déploiement automatisé

- Script `deploy.bat` avec support multi-version
- Mode Debug : auto-copie vers AppData avec chemin absolu
- Mode Release : chemin relatif pour distribution

## Limitations

- Les pièces doivent être placées (Area > 0)
- Le type de mur "Peinture - 5mm" sera créé s'il n'existe pas
- Les paramètres partagés doivent être créés manuellement (ou via `EnsureSharedParametersExist()`)

## Support

Pour signaler un bug ou demander une fonctionnalité, ouvrez une issue sur le dépôt GitHub.

## Licence

Ce projet est fourni tel quel, sans garantie. Utilisez-le à vos propres risques.
