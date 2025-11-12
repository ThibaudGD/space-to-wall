using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Revit.Async;
using Revit.Async.Entities;
using Revit.Async.ExternalEvents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace space_to_wall.app
{
    /// <summary>
    /// Paramètre pour la création de murs de peinture
    /// </summary>
    public class CreatePaintWallsParameter
    {
        // Peut contenir des options futures (filtres, types spécifiques, etc.)
        public bool IncludeAllRooms { get; set; } = true;
    }

    /// <summary>
    /// Résultat de la création de murs de peinture
    /// </summary>
    public class CreatePaintWallsResult
    {
        public int WallsCreated { get; set; }
        public int RoomCount { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Résultat de la suppression de murs de peinture
    /// </summary>
    public class DeletePaintWallsResult
    {
        public int WallsDeleted { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Handler pour créer les murs de peinture
    /// </summary>
    public class CreatePaintWallsHandler : SyncGenericExternalEventHandler<CreatePaintWallsParameter, CreatePaintWallsResult>
    {
        private const string PAINT_WALL_TYPE_NAME = "Peinture - 5mm";
        private const string PARAM_ROOM_NAME = "Peinture_NomPiece";
        private const string PARAM_ROOM_NUMBER = "Peinture_NumeroPiece";
        private const string PARAM_ROOM_FINISH = "Peinture_Finition";

        public override string GetName()
        {
            return "CreatePaintWallsHandler";
        }

        public override object Clone()
        {
            return new CreatePaintWallsHandler();
        }

        protected override CreatePaintWallsResult Handle(UIApplication app, CreatePaintWallsParameter parameter)
        {
            Document doc = app.ActiveUIDocument.Document;
            var result = new CreatePaintWallsResult { Success = false };

            using (Transaction trans = new Transaction(doc, "Créer murs de peinture"))
            {
                trans.Start();

                try
                {
                    // 1. Créer ou récupérer le type de mur
                    WallType paintWallType = GetOrCreatePaintWallType(doc);

                    if (paintWallType == null)
                    {
                        result.Message = "Impossible de créer le type de mur de peinture";
                        trans.RollBack();
                        return result;
                    }

                    // 2. Récupérer toutes les pièces
                    FilteredElementCollector roomCollector = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Rooms)
                        .WhereElementIsNotElementType();

                    int wallsCreated = 0;
                    int roomCount = 0;

                    foreach (Room room in roomCollector)
                    {
                        if (room.Area > 0) // Pièce placée
                        {
                            roomCount++;
                            wallsCreated += CreatePaintWallsForRoom(doc, room, paintWallType);
                        }
                    }

                    trans.Commit();

                    result.Success = true;
                    result.WallsCreated = wallsCreated;
                    result.RoomCount = roomCount;
                    result.Message = $"{wallsCreated} murs de peinture créés pour {roomCount} pièces.";

                    return result;
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    result.Message = $"Erreur lors de la création : {ex.Message}";
                    return result;
                }
            }
        }

        private int CreatePaintWallsForRoom(Document doc, Room room, WallType paintWallType)
        {
            int wallCount = 0;
            Level level = doc.GetElement(room.LevelId) as Level;

            if (level == null)
                return 0;

            // Options pour récupérer les boundaries
            SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish,
                StoreFreeBoundaryFaces = true
            };

            IList<IList<BoundarySegment>> boundaries = room.GetBoundarySegments(options);

            if (boundaries == null || boundaries.Count == 0)
                return 0;

            // Parcourir tous les contours (extérieur + îlots)
            foreach (IList<BoundarySegment> boundary in boundaries)
            {
                foreach (BoundarySegment segment in boundary)
                {
                    // Vérifier si un mur existe déjà (pour éviter les doublons)
                    if (segment.ElementId != null && segment.ElementId != ElementId.InvalidElementId)
                    {
                        Element boundaryElement = doc.GetElement(segment.ElementId);

                        // Si c'est déjà un mur de peinture, on skip
                        if (boundaryElement is Wall existingWall &&
                            existingWall.WallType.Id == paintWallType.Id)
                        {
                            continue;
                        }
                    }

                    try
                    {
                        Curve curve = segment.GetCurve();
                        
                        // Décaler la courbe de 2.5mm (moitié de l'épaisseur) vers l'intérieur de la pièce
                        double offsetDistance = 2.5 / 304.8; // en pieds
                        
                        // Calculer la direction perpendiculaire vers l'intérieur (à gauche de la courbe)
                        XYZ startPoint = curve.GetEndPoint(0);
                        XYZ endPoint = curve.GetEndPoint(1);
                        XYZ direction = (endPoint - startPoint).Normalize();
                        XYZ normal = new XYZ(-direction.Y, direction.X, 0); // Perpendiculaire à gauche
                        XYZ translation = normal * offsetDistance;
                        
                        // Créer une nouvelle courbe décalée
                        Transform transform = Transform.CreateTranslation(translation);
                        Curve offsetCurve = curve.CreateTransformed(transform);
                        
                        double height = room.UnboundedHeight;

                        // Créer le mur de peinture avec la courbe décalée
                        Wall paintWall = Wall.Create(doc, offsetCurve, paintWallType.Id,
                            level.Id, height, 0, false, false);

                        if (paintWall != null)
                        {
                            // Définir la ligne de justification à "Nu fini: Extérieur"
                            Parameter locationParam = paintWall.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM);
                            if (locationParam != null && !locationParam.IsReadOnly)
                            {
                                // WallLocationLine.FinishFaceExterior = 2
                                locationParam.Set(2);
                            }

                            // Affecter les paramètres
                            SetPaintWallParameters(paintWall, room);
                            wallCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log l'erreur mais continue avec les autres segments
                        System.Diagnostics.Debug.WriteLine(
                            $"Erreur création mur pour pièce {room.Name}: {ex.Message}");
                    }
                }
            }

            return wallCount;
        }

        private void SetPaintWallParameters(Wall paintWall, Room room)
        {
            // Nom de la pièce
            Parameter roomNameParam = paintWall.LookupParameter(PARAM_ROOM_NAME);
            if (roomNameParam != null && !roomNameParam.IsReadOnly)
                roomNameParam.Set(room.Name);

            // Numéro de la pièce
            Parameter roomNumberParam = paintWall.LookupParameter(PARAM_ROOM_NUMBER);
            if (roomNumberParam != null && !roomNumberParam.IsReadOnly)
                roomNumberParam.Set(room.Number);

            // Finition (depuis les paramètres de la pièce si disponible)
            Parameter roomFinishParam = room.LookupParameter("Finition murs");
            Parameter paintFinishParam = paintWall.LookupParameter(PARAM_ROOM_FINISH);

            if (roomFinishParam != null && paintFinishParam != null && !paintFinishParam.IsReadOnly)
            {
                if (roomFinishParam.HasValue)
                    paintFinishParam.Set(roomFinishParam.AsString());
            }

            // Marquer comme mur de peinture dans les commentaires
            Parameter commentsParam = paintWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (commentsParam != null && !commentsParam.IsReadOnly)
                commentsParam.Set("Mur de peinture généré automatiquement");
        }

        private WallType GetOrCreatePaintWallType(Document doc)
        {
            // Chercher le type existant
            WallType paintType = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .FirstOrDefault(wt => wt.Name == PAINT_WALL_TYPE_NAME);

            // Si trouvé, vérifier et corriger la structure si nécessaire
            if (paintType != null)
            {
                UpdatePaintWallStructure(paintType);
                return paintType;
            }

            // Si pas trouvé, prendre un type basique et le dupliquer
            WallType baseType = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .FirstOrDefault(wt => wt.Kind == WallKind.Basic);

            if (baseType != null)
            {
                paintType = baseType.Duplicate(PAINT_WALL_TYPE_NAME) as WallType;
                UpdatePaintWallStructure(paintType);
            }

            return paintType;
        }

        private void UpdatePaintWallStructure(WallType paintType)
        {
            CompoundStructure structure = paintType.GetCompoundStructure();
            if (structure == null || structure.LayerCount == 0)
                return;

            try
            {
                // Supprimer TOUTES les couches
                while (structure.LayerCount > 1)
                {
                    structure.DeleteLayer(0);
                }

                // Obtenir un matériau par défaut (ou créer un matériau de finition)
                ElementId materialId = ElementId.InvalidElementId;
                
                // Créer une nouvelle couche unique de 5mm
                double layerWidth = 5.0 / 304.8; // 5mm en pieds
                CompoundStructureLayer newLayer = new CompoundStructureLayer(
                    layerWidth,
                    MaterialFunctionAssignment.Structure, // Fonction de structure
                    materialId);

                // Ajouter la couche
                structure.SetLayers(new List<CompoundStructureLayer> { newLayer });

                // Appliquer la structure modifiée
                paintType.SetCompoundStructure(structure);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Erreur lors de la modification de la structure du mur : {ex.Message}");
            }
        }
    }

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
