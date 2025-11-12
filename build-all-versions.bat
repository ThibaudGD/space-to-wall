@echo off
REM Script pour compiler toutes les versions et créer les packages ZIP
setlocal enabledelayedexpansion

echo ============================================
echo   Space to Wall - Build All Versions
echo ============================================
echo.

REM Vérifier qu'on est dans le bon dossier
if not exist "space-to-wall.app\space-to-wall.app.csproj" (
    echo ERREUR: Script doit etre execute depuis la racine du projet
    echo Dossier courant: %CD%
    pause
    exit /b 1
)

REM Nettoyer les anciens builds
echo Nettoyage des anciens builds...
if exist "build-output" rd /s /q "build-output"
mkdir "build-output"
echo.

REM Versions à compiler
set VERSIONS=2022 2023 2024

echo [1/3] Compilation des extensions Revit...
echo.

for %%V in (%VERSIONS%) do (
    echo Compilation pour Revit %%V...
    cd space-to-wall.app
    
    REM Modifier temporairement le .csproj pour définir la version
    powershell -Command "(Get-Content space-to-wall.app.csproj) -replace '<RevitVersion>.*</RevitVersion>', '<RevitVersion>%%V</RevitVersion>' | Set-Content space-to-wall.app.csproj"
    
    REM Compiler en Release
    dotnet build -c Release >nul 2>&1
    
    if !ERRORLEVEL! neq 0 (
        echo ERREUR: La compilation pour Revit %%V a echoue
        cd ..
        pause
        exit /b 1
    )
    
    cd ..
    echo   [OK] Revit %%V compile
)

echo.
echo [2/3] Creation des packages ZIP...
echo.

for %%V in (%VERSIONS%) do (
    echo Creation du ZIP pour Revit %%V...
    
    REM Créer un dossier temporaire pour le contenu du ZIP
    set TEMP_DIR=build-output\temp_%%V
    mkdir "!TEMP_DIR!"
    
    REM Utiliser directement le dossier de build (bin\Release\net48)
    set SOURCE_DIR=space-to-wall.app\bin\Release\net48
    
    REM Vérifier si les fichiers existent
    if not exist "!SOURCE_DIR!\space_to_wall.app.dll" (
        echo   ERREUR: Fichiers compiles introuvables dans !SOURCE_DIR!
        echo   Verifiez que la compilation a reussi
        rd /s /q "!TEMP_DIR!"
        cd ..
        pause
        exit /b 1
    )
    
    REM Copier uniquement les DLL et fichiers binaires (pas le .addin)
    REM Le .addin sera géré séparément par l'installateur
    echo   Copie des fichiers depuis !SOURCE_DIR!...
    copy "!SOURCE_DIR!\*.dll" "!TEMP_DIR!\" >nul 2>&1
    copy "!SOURCE_DIR!\*.pdb" "!TEMP_DIR!\" >nul 2>&1
    copy "!SOURCE_DIR!\*.config" "!TEMP_DIR!\" >nul 2>&1
    
    REM Vérifier que la DLL principale a été copiée
    if not exist "!TEMP_DIR!\space_to_wall.app.dll" (
        echo   ERREUR: Impossible de copier space_to_wall.app.dll
        rd /s /q "!TEMP_DIR!"
        pause
        exit /b 1
    )
    
    REM Créer le ZIP
    powershell -Command "Compress-Archive -Path '!TEMP_DIR!\*' -DestinationPath 'build-output\space_to_wall.app_%%V.zip' -Force"
    
    REM Nettoyer le dossier temporaire
    rd /s /q "!TEMP_DIR!"
    
    echo   [OK] space_to_wall.app_%%V.zip cree
)

echo.
echo [3/3] Compilation de l'installateur avec les ZIP embarques...
echo.

REM Créer le dossier Resources dans l'installateur s'il n'existe pas
if not exist "space-to-wall.installer\Resources" mkdir "space-to-wall.installer\Resources"

REM Copier les ZIP dans les ressources
copy "build-output\space_to_wall.app_*.zip" "space-to-wall.installer\Resources\" >nul

REM Compiler l'installateur
cd space-to-wall.installer
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ..\build-output\installer >nul 2>&1
cd ..

if %ERRORLEVEL% neq 0 (
    echo ERREUR: La compilation de l'installateur a echoue
    pause
    exit /b 1
)

echo   [OK] Installateur compile

REM Créer un README dans build-output
echo Creation du fichier README dans build-output...
(
echo # Space to Wall - Build Output
echo.
echo Ce dossier contient les fichiers generes par le script build-all-versions.bat
echo.
echo ## Fichiers
echo.
echo ### ZIP des versions Revit
echo - space_to_wall.app_2022.zip
echo - space_to_wall.app_2023.zip  
echo - space_to_wall.app_2024.zip
echo.
echo Ces fichiers contiennent uniquement les DLL et dependances.
echo Le fichier .addin est gere separement par l'installateur.
echo.
echo ### Installateur autonome
echo - installer\space-to-wall.installer.exe
echo.
echo Cet executable contient:
echo - Tous les ZIP embarques
echo - Le template SpaceToWall.addin
echo - Detection automatique des versions Revit
echo - Installation dans ApplicationPlugins
echo.
echo ## Distribution
echo.
echo Pour distribuer aux utilisateurs:
echo 1. Donnez uniquement: installer\space-to-wall.installer.exe
echo 2. L'utilisateur double-clique dessus
echo 3. L'installateur gere tout automatiquement
echo.
echo ## Structure d'installation
echo.
echo L'installateur cree cette structure:
echo.
echo %%AppData%%\Autodesk\Revit\Addins\{version}\
echo   SpaceToWall.addin
echo.
echo %%AppData%%\Autodesk\ApplicationPlugins\SpaceToWall\{version}\
echo   space_to_wall.app.dll
echo   Revit.Async.dll
echo   ^(autres dependances^)
) > build-output\README.txt

echo.
echo ============================================
echo Build termine avec succes !
echo ============================================
echo.
echo Fichiers crees:
dir build-output\*.zip /B 2>nul
if exist "build-output\installer\space-to-wall.installer.exe" (
    echo.
    echo Installateur autonome:
    echo   build-output\installer\space-to-wall.installer.exe
    echo.
    echo L'installateur contient:
    echo   - Tous les ZIP des versions Revit ^(2022, 2023, 2024^)
    echo   - Le template SpaceToWall.addin
    echo   - Detection automatique des versions Revit installees
    echo.
    echo Distribution:
    echo   Distribuez uniquement: build-output\installer\space-to-wall.installer.exe
    echo   Taille du fichier: 
    for %%F in ("build-output\installer\space-to-wall.installer.exe") do echo   %%~zF octets ^(~%%~zFKb^)
) else (
    echo.
    echo ATTENTION: L'installateur n'a pas ete cree correctement
)
echo.
pause
