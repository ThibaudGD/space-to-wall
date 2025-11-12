@echo off
REM Script d'installation automatique pour Space to Wall
REM Ce script lance l'installateur avec le fichier ZIP trouvé dans le dossier

echo ============================================
echo   Space to Wall - Installation automatique
echo ============================================
echo.

REM Chercher le fichier ZIP dans le dossier courant
for %%f in (space_to_wall.app*.zip) do (
    echo Fichier trouve: %%f
    echo.
    space-to-wall.installer.exe "%%f"
    goto :end
)

echo ERREUR: Aucun fichier ZIP trouve dans ce dossier.
echo Placez le fichier space_to_wall.app*.zip dans ce dossier et relancez.
echo.
pause
goto :end

:end
