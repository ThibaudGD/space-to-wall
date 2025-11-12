using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Async.ExternalEvents;
using space_to_wall.app.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace space_to_wall.app.Handlers
{
    /// <summary>
    /// Handler pour supprimer les murs de peinture
    /// </summary>
    public class DeletePaintWallsHandler : SyncGenericExternalEventHandler<object, DeletePaintWallsResult>
    {
        private const string PAINT_WALL_TYPE_NAME = "Peinture - 5mm";

        public override string GetName()
        {
            return "DeletePaintWallsHandler";
        }

        public override object Clone()
        {
            return new DeletePaintWallsHandler();
        }

        protected override DeletePaintWallsResult Handle(UIApplication app, object parameter)
        {
            Document doc = app.ActiveUIDocument.Document;
            var result = new DeletePaintWallsResult { Success = false };

            using (Transaction trans = new Transaction(doc, "Supprimer murs de peinture"))
            {
                trans.Start();

                try
                {
                    WallType paintWallType = new FilteredElementCollector(doc)
                        .OfClass(typeof(WallType))
                        .Cast<WallType>()
                        .FirstOrDefault(wt => wt.Name == PAINT_WALL_TYPE_NAME);

                    if (paintWallType != null)
                    {
                        FilteredElementCollector wallCollector = new FilteredElementCollector(doc)
                            .OfClass(typeof(Wall))
                            .WhereElementIsNotElementType();

                        List<ElementId> toDelete = new List<ElementId>();

                        foreach (Wall wall in wallCollector)
                        {
                            if (wall.WallType.Id == paintWallType.Id)
                            {
                                toDelete.Add(wall.Id);
                            }
                        }

                        if (toDelete.Count > 0)
                        {
                            doc.Delete(toDelete);
                        }

                        result.WallsDeleted = toDelete.Count;
                    }
                    else
                    {
                        result.WallsDeleted = 0;
                    }

                    trans.Commit();

                    result.Success = true;
                    result.Message = result.WallsDeleted > 0
                        ? $"{result.WallsDeleted} murs de peinture supprimés."
                        : "Aucun mur de peinture à supprimer.";

                    return result;
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    result.Message = $"Erreur lors de la suppression : {ex.Message}";
                    return result;
                }
            }
        }
    }
}
