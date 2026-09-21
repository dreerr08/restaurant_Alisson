namespace AliGame.Data
{
    public enum StationType
    {
        Stove,
        CutStation,
        MixStation
    }

    public static class StationTypeExtensions
    {
        public static string DisplayName(this StationType type)
        {
            switch (type)
            {
                case StationType.Stove: return "Fogão";
                case StationType.CutStation: return "Estação de Corte";
                case StationType.MixStation: return "Estação de Mistura";
                default: return type.ToString();
            }
        }
    }
}
