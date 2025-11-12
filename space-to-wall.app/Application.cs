using Autodesk.Revit.UI;
using Revit.Async;
using space_to_wall.app.Handlers;
using System;

namespace space_to_wall.app
{
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Initialiser Revit.Async au démarrage
                RevitTask.Initialize(application);
                
                // Enregistrer les handlers personnalisés
                RevitTask.RegisterGlobal(new CreatePaintWallsHandler());
                RevitTask.RegisterGlobal(new DeletePaintWallsHandler());
                
                // Créer un ruban avec les boutons
                CreateRibbonPanel(application);
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Erreur", $"Erreur au démarrage: {ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void CreateRibbonPanel(UIControlledApplication app)
        {
            string tabName = "Space to Wall";
            
            try
            {
                app.CreateRibbonTab(tabName);
            }
            catch
            {
                // L'onglet existe déjà
            }

            RibbonPanel panel = app.CreateRibbonPanel(tabName, "Murs de Peinture");
            
            // Bouton pour créer les murs
            PushButtonData createButtonData = new PushButtonData(
                "CreatePaintWalls",
                "Créer Murs\nde Peinture",
                typeof(Application).Assembly.Location,
                "space_to_wall.app.Commands.CreatePaintWallsCommand");
            
            PushButton createButton = panel.AddItem(createButtonData) as PushButton;
            createButton.ToolTip = "Crée automatiquement des murs de peinture (5mm) pour toutes les pièces";
            
            // Bouton pour supprimer les murs
            PushButtonData deleteButtonData = new PushButtonData(
                "DeletePaintWalls",
                "Supprimer Murs\nde Peinture",
                typeof(Application).Assembly.Location,
                "space_to_wall.app.Commands.DeletePaintWallsCommand");
            
            PushButton deleteButton = panel.AddItem(deleteButtonData) as PushButton;
            deleteButton.ToolTip = "Supprime tous les murs de peinture générés";
        }
    }
}
