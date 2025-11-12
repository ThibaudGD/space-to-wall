# Space to Wall - Installateur

Installateur en ligne de commande pour l'extension Revit "Space to Wall".

## Utilisation

### Méthode 1 : Glisser-déposer
1. Placez le fichier ZIP de l'extension dans le même dossier que `space-to-wall.installer.exe`
2. Double-cliquez sur `space-to-wall.installer.exe`
3. Suivez les instructions à l'écran

### Méthode 2 : Ligne de commande
```bash
space-to-wall.installer.exe chemin/vers/space_to_wall.app.zip
```

## Fonctionnalités

- ✅ Détection automatique de la version Revit depuis le nom du fichier
- ✅ Installation dans le dossier Addins approprié
- ✅ Support de Revit 2023, 2024 et 2025
- ✅ Gestion automatique des dépendances
- ✅ Écrasement des anciennes versions

## Emplacement d'installation

Les fichiers sont installés dans :
```
%AppData%\Autodesk\Revit\Addins\{version}\
├── space_to_wall.app.addin
└── space_to_wall.app\
    ├── space_to_wall.app.dll
    ├── Revit.Async.dll
    └── ... (autres dépendances)
```

## Désinstallation

Pour désinstaller, supprimez simplement :
- Le fichier `space_to_wall.app.addin`
- Le dossier `space_to_wall.app`

dans le répertoire Addins de Revit.

## Compilation

```bash
dotnet build -c Release
```

L'exécutable sera généré dans `bin/Release/net9.0/`
