# Space to Wall - Installateur

Installateur autonome en ligne de commande pour l'extension Revit "Space to Wall".

## Fonctionnalités

- ✅ **Installateur autonome** : Embarque tous les ZIP des versions Revit (2023, 2024, 2025)
- ✅ **Détection automatique** : Détecte les versions Revit installées sur le système
- ✅ **Installation intelligente** : Propose d'installer uniquement les versions pertinentes
- ✅ **Installation multi-version** : Peut installer plusieurs versions en une seule exécution
- ✅ **Pas de fichiers externes** : Tous les fichiers nécessaires sont embarqués dans l'exécutable

## Utilisation

### Installation automatique (Recommandée)

1. Double-cliquez sur `space-to-wall.installer.exe`
2. L'installateur va :
   - Détecter les versions Revit installées sur votre système
   - Proposer d'installer les versions correspondantes
   - Copier automatiquement les fichiers au bon endroit
3. Suivez les instructions à l'écran
4. Redémarrez Revit

### Options d'installation

L'installateur offre plusieurs options selon votre configuration :

#### Si Revit est détecté sur le système :
- Installation automatique des versions Revit détectées
- Possibilité d'installer aussi les autres versions disponibles

#### Si aucun Revit n'est détecté :
- Installation de toutes les versions disponibles
- Installation manuelle version par version

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

## Versions Revit supportées

- ✅ Revit 2023
- ✅ Revit 2024
- ✅ Revit 2025

## Désinstallation

Pour désinstaller, supprimez simplement :
- Le fichier `space_to_wall.app.addin`
- Le dossier `space_to_wall.app`

dans le répertoire Addins de Revit pour chaque version installée.

## Pour les développeurs

### Compiler l'installateur avec les ZIP embarqués

1. Utilisez le script de build complet :
   ```batch
   build-all-versions.bat
   ```

2. Ce script va :
   - Compiler l'extension pour Revit 2023, 2024, 2025
   - Créer les ZIP pour chaque version
   - Embarquer les ZIP comme ressources dans l'installateur
   - Compiler l'installateur en un exécutable unique

3. L'exécutable final se trouve dans :
   ```
   build-output\installer\space-to-wall.installer.exe
   ```

### Structure des ressources embarquées

Les ZIP sont embarqués comme `EmbeddedResource` dans le projet :
```xml
<ItemGroup>
  <EmbeddedResource Include="Resources\*.zip" />
</ItemGroup>
```

Les fichiers doivent suivre le format de nommage :
```
Resources\space_to_wall.app_2023.zip
Resources\space_to_wall.app_2024.zip
Resources\space_to_wall.app_2025.zip
```

### Compilation manuelle

```bash
# 1. Placer les ZIP dans Resources/
copy build-output\*.zip space-to-wall.installer\Resources\

# 2. Compiler l'installateur
cd space-to-wall.installer
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Avantages de cette approche

- ✅ **Distribution simplifiée** : Un seul fichier .exe à distribuer
- ✅ **Pas de fichiers manquants** : Impossible d'oublier les ZIP
- ✅ **Installation offline** : Fonctionne sans connexion Internet
- ✅ **Expérience utilisateur améliorée** : Pas de fichiers à gérer manuellement
- ✅ **Installation intelligente** : Détection automatique des versions Revit

## Dépannage

### "Aucune version embarquée trouvée"
L'installateur n'a pas été compilé avec les ZIP embarqués. Utilisez `build-all-versions.bat` pour recompiler.

### "Aucune installation Revit détectée"
L'installateur peut quand même procéder à l'installation. Les fichiers seront copiés mais Revit devra être installé pour les utiliser.

### Problèmes de permissions
Exécutez l'installateur en tant qu'administrateur si vous rencontrez des problèmes de permissions.

## Support

Pour signaler un bug ou demander une fonctionnalité, ouvrez une issue sur le dépôt GitHub.
