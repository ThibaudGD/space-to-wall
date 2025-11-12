using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async;
using System;

namespace space_to_wall.app
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DeletePaintWallsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Exécuter de manière asynchrone en utilisant le handler enregistré
                _ = ExecuteAsync();
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private async System.Threading.Tasks.Task ExecuteAsync()
        {
            try
            {
                // Appel du handler enregistré avec RevitTask.RaiseGlobal
                // Passer null car le handler ne nécessite pas de paramètre spécifique
                var result = await RevitTask.RaiseGlobal<DeletePaintWallsHandler, object, DeletePaintWallsResult>(null);

                // Afficher le résultat à l'utilisateur
                if (result.Success)
                {
                    TaskDialog.Show("Succès", result.Message);
                }
                else
                {
                    TaskDialog.Show("Information", result.Message);
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Erreur", $"Erreur lors de l'exécution : {ex.Message}");
            }
        }
    }
}
