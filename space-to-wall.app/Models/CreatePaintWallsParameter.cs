namespace space_to_wall.app.Models
{
    /// <summary>
    /// Paramètre pour la création de murs de peinture
    /// </summary>
    public class CreatePaintWallsParameter
    {
        // Peut contenir des options futures (filtres, types spécifiques, etc.)
        public bool IncludeAllRooms { get; set; } = true;
    }
}
