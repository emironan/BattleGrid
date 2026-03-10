namespace BattleGrid.Contracts.ResponseDtos
{
    public class ShipTypeListResponseDto
    {
        public int ShipID { get; set; }
        public string ShipName { get; set; } = string.Empty;
        public int Length { get; set; }
        public int Width { get; set; }
        public int MaxPerPlayer { get; set; }
    }
}