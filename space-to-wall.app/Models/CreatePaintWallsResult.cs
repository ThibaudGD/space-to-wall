namespace space_to_wall.app.Models
{
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
}
