namespace BattleGrid.Domain.Entities;

    public sealed class ShipType
    {
        public int ShipID { get; set; }
        public string ShipName { get; set; } = "";
        public int Length { get; set; }
        public int Width { get; set; }
        public int MaxPerPlayer { get; set; }
    }