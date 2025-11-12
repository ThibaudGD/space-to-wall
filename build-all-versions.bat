@echo off
REM Script pour compiler toutes les versions et créer les packages ZIP
setlocal enabledelayedexpansion

echo ============================================
echo   Space to Wall - Build All Versions
echo ============================================
echo.

REM Nettoyer les anciens builds
if exist "build-output" rd /s /q "build-output"
mkdir "build-output"

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
    
    REM Dossier source des fichiers compilés
    set SOURCE_DIR=%APPDATA%\Autodesk\Revit\Addins\%%V
    
    REM Vérifier si le dossier source existe
    if not exist "!SOURCE_DIR!" (
        echo   ATTENTION: Dossier !SOURCE_DIR! introuvable, utilisation du dossier bin
        set SOURCE_DIR=space-to-wall.app\bin\Release\%%V
    )
    
    REM Copier uniquement les DLL et fichiers binaires (pas le .addin)
    REM Le .addin sera géré séparément par l'installateur
    if exist "!SOURCE_DIR!\space_to_wall.app\" (
        copy "!SOURCE_DIR!\space_to_wall.app\*.dll" "!TEMP_DIR!\" >nul 2>&1
        copy "!SOURCE_DIR!\space_to_wall.app\*.pdb" "!TEMP_DIR!\" >nul 2>&1
        copy "!SOURCE_DIR!\space_to_wall.app\*.config" "!TEMP_DIR!\" >nul 2>&1
    )
    
    REM Si pas trouvé dans AppData, copier depuis bin
    if not exist "!TEMP_DIR!\space_to_wall.app.dll" (
        echo   Copie depuis bin\Release\net48...
        copy "space-to-wall.app\bin\Release\net48\*.dll" "!TEMP_DIR!\" >nul 2>&1
        copy "space-to-wall.app\bin\Release\net48\*.pdb" "!TEMP_DIR!\" >nul 2>&1
        copy "space-to-wall.app\bin\Release\net48\*.config" "!TEMP_DIR!\" >nul 2>&1
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
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ..\build-output\installer
cd ..

if %ERRORLEVEL% neq 0 (
    echo ERREUR: La compilation de l'installateur a echoue
    pause
    exit /b 1
)

echo.
echo ============================================
echo Build termine avec succes !
echo ============================================
echo.
echo Fichiers crees:
echo   build-output\space_to_wall.app_2022.zip
echo   build-output\space_to_wall.app_2023.zip
echo   build-output\space_to_wall.app_2024.zip
echo   build-output\installer\space-to-wall.installer.exe
echo.
echo L'installateur contient tous les ZIP embarques.
echo Distribuez uniquement: space-to-wall.installer.exe
echo.
pause
