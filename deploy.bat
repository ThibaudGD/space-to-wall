@echo off
REM Script de déploiement pour Space to Wall - Paint Wall Creator
REM Copie les fichiers compilés vers le dossier AddIns de Revit

REM Configuration par défaut: Revit 2024
set REVIT_VERSION=2024
if not "%1"=="" set REVIT_VERSION=%1

set REVIT_ADDINS=%APPDATA%\Autodesk\Revit\Addins\%REVIT_VERSION%
set BUILD_CONFIG=Release
set BUILD_OUTPUT=space-to-wall.app\bin\%BUILD_CONFIG%\%REVIT_VERSION%

echo ============================================
echo Space to Wall - Paint Wall Creator
echo Déploiement pour Revit %REVIT_VERSION%
echo ============================================
echo.

REM Vérifier si le dossier Revit AddIns existe
if not exist "%REVIT_ADDINS%" (
    echo ERREUR: Le dossier Revit AddIns n'existe pas:
    echo %REVIT_ADDINS%
    echo.
    echo Assurez-vous que Revit %REVIT_VERSION% est installé.
    pause
    exit /b 1
)

echo Compilation du projet en mode %BUILD_CONFIG% pour Revit %REVIT_VERSION%...
cd space-to-wall.app
dotnet build --configuration %BUILD_CONFIG%
if errorlevel 1 (
    echo.
    echo ERREUR: La compilation a échoué!
    pause
    exit /b 1
)
cd ..

echo.
echo Copie des fichiers vers Revit AddIns...
echo Destination: %REVIT_ADDINS%
echo Source: %BUILD_OUTPUT%
echo.

copy /Y "%BUILD_OUTPUT%\space_to_wall.app.dll" "%REVIT_ADDINS%\"
if errorlevel 1 (
    echo ERREUR: Impossible de copier space_to_wall.app.dll
    pause
    exit /b 1
)

copy /Y "%BUILD_OUTPUT%\Revit.Async.dll" "%REVIT_ADDINS%\"
if errorlevel 1 (
    echo ERREUR: Impossible de copier Revit.Async.dll
    pause
    exit /b 1
)

copy /Y "%BUILD_OUTPUT%\space_to_wall.app.addin" "%REVIT_ADDINS%\"
if errorlevel 1 (
    echo ERREUR: Impossible de copier space_to_wall.app.addin
    pause
    exit /b 1
)

echo.
echo ============================================
echo Installation terminée avec succès!
echo ============================================
echo.
echo Fichiers installés:
echo - space_to_wall.app.dll
echo - Revit.Async.dll
echo - space_to_wall.app.addin
echo.
echo Pour utiliser l'extension:
echo 1. Lancez Revit %REVIT_VERSION%
echo 2. Ouvrez un projet avec des pièces (Rooms)
echo 3. Cherchez l'onglet "Space to Wall" dans le ruban
echo 4. Cliquez sur "Créer Murs de Peinture"
echo.
echo Note: Pour déployer vers une autre version de Revit, utilisez:
echo   deploy.bat 2023
echo   deploy.bat 2024
echo   deploy.bat 2025
echo.
pause
