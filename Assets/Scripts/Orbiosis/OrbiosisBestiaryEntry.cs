namespace MahjongGame.Orbiosis
{
    public sealed class OrbiosisBestiaryEntry
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string ClassName;
        public readonly string SpriteResourcePath;
        public readonly string Description;
        public readonly string HitPoints;
        public readonly string Behavior;
        public readonly string Threat;
        public readonly string Tactics;

        public OrbiosisBestiaryEntry(
            string id,
            string displayName,
            string className,
            string spriteResourcePath,
            string description,
            string hitPoints,
            string behavior,
            string threat,
            string tactics)
        {
            Id = id;
            DisplayName = displayName;
            ClassName = className;
            SpriteResourcePath = spriteResourcePath;
            Description = description;
            HitPoints = hitPoints;
            Behavior = behavior;
            Threat = threat;
            Tactics = tactics;
        }
    }
}
