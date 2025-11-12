@echo off
REM Script pour créer un package d'installation complet
REM Compile l'installateur et crée un ZIP avec tout le nécessaire

echo ============================================
echo   Space to Wall - Build Installer Package
echo ============================================
echo.

REM Nettoyer les anciens builds
if exist "installer-package" rd /s /q "installer-package"
mkdir "installer-package"

REM Compiler l'installateur en Release
echo [1/3] Compilation de l'installateur...
cd space-to-wall.installer
dotnet publish -c Release -r win-x64 --self-contained false -o ..\installer-package
if %ERRORLEVEL% neq 0 (
    echo ERREUR: La compilation a echoue
    pause
    exit /b 1
)
cd ..

REM Copier les fichiers nécessaires
echo [2/3] Copie des fichiers...
copy space-to-wall.installer\install.bat installer-package\
copy space-to-wall.installer\README.md installer-package\

REM Créer un README pour le package
echo [3/3] Creation du README...
(
echo # Space to Wall - Package d'installation
echo.
echo ## Installation
echo.
echo 1. Placez votre fichier ZIP de l'extension Space to Wall dans ce dossier
echo 2. Double-cliquez sur `install.bat` OU `space-to-wall.installer.exe`
echo 3. Suivez les instructions
echo 4. Redemarrez Revit
echo.
echo ## Contenu du package
echo.
echo - `space-to-wall.installer.exe` : L'installateur
echo - `install.bat` : Script d'installation automatique
echo - `README.md` : Documentation detaillee
echo.
echo ## Support
echo.
echo - GitHub: https://github.com/ThibaudGD/space-to-wall
) > installer-package\README_PACKAGE.md

echo.
echo ============================================
echo Package cree avec succes !
echo ============================================
echo.
echo Dossier: installer-package\
echo.
echo Pour distribuer:
echo 1. Placez le fichier space_to_wall.app*.zip dans installer-package\
echo 2. Compressez le dossier installer-package en ZIP
echo 3. Distribuez le ZIP aux utilisateurs
echo.
pause
