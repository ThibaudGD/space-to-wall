namespace space_to_wall.app.Models
{
    /// <summary>
    /// Résultat de la suppression de murs de peinture
    /// </summary>
    public class DeletePaintWallsResult
    {
        public int WallsDeleted { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
