namespace MahjongGame.Orbiosis
{
    public static class OrbiosisBestiaryLibrary
    {
        public const string EnemyMine = "enemy_mine";
        public const string SeekerMine = "seeker_mine";
        public const string HeavyMine = "heavy_mine";
        public const string FragmentMine = "fragment_mine";
        public const string MinesCarrier = "mines_carrier";

        private static readonly OrbiosisBestiaryEntry[] Entries =
        {
            new OrbiosisBestiaryEntry(
                EnemyMine,
                "Enemy Mine",
                "Contact explosive",
                "Orbiosis/EnemyMine_Level1",
                "A compact drifting mine used to deny safe lanes around the Orb station.",
                "HP 10",
                "Falls through the playfield, spins, and detonates on contact or proximity.",
                "Light",
                "Destroy early with the core cannon. If it reaches the base ship, its blast is more costly than its HP suggests."),

            new OrbiosisBestiaryEntry(
                SeekerMine,
                "Seeker Mine",
                "Homing explosive",
                "Orbiosis/EnemyMine_HomingQuadSpikeShort",
                "A guided mine that sleeps while drifting, then locks onto the Orb or a vulnerable drone.",
                "HP 20",
                "Moves slowly at first. When a target enters range, it accelerates and spins into an attack line.",
                "Medium",
                "Break lock-on by shooting it before it turns. Shield drones can intercept, but the blast still hurts nearby systems."),

            new OrbiosisBestiaryEntry(
                HeavyMine,
                "Heavy Mine",
                "Splitting explosive",
                "Orbiosis/EnemyMine_Heavy_TopDown",
                "A reinforced mine shell designed to absorb fire and split the lane after destruction.",
                "HP 100",
                "Falls slowly, survives several hits, and breaks into smaller fragments when destroyed.",
                "High",
                "Focus fire before it reaches the center. Rail Spike and Laser modules are ideal against its high HP."),

            new OrbiosisBestiaryEntry(
                FragmentMine,
                "Fragment Mine",
                "Heavy-mine shard",
                "Orbiosis/EnemyMine_Heavy_TopDown",
                "A smaller fragment released from a heavy mine casing. It is weaker, but the spread creates crossfire pressure.",
                "HP 50 / 20",
                "Splits outward from a destroyed heavy mine, then continues toward the lower field at higher speed.",
                "Medium",
                "Do not chase only the parent mine. After the split, clear the side fragments before they touch drones or the base."),

            new OrbiosisBestiaryEntry(
                MinesCarrier,
                "Mines Carrier",
                "Story boss",
                "Orbiosis/Boss_MinesCarrier",
                "A carrier-class mining construct that floods the route with mixed mine patterns.",
                "HP 260 / 420 / 640",
                "Appears in the Mines story finale and launches normal, seeker, and heavy mines in timed volleys.",
                "Boss",
                "Keep the Orb near stable firing lanes, preserve shields for volleys, and use modules to damage the boss between mine waves.")
        };

        public static int Count => Entries.Length;

        public static OrbiosisBestiaryEntry[] All()
        {
            return Entries;
        }

        public static OrbiosisBestiaryEntry Find(string id)
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].Id == id)
                    return Entries[i];
            }

            return null;
        }
    }
}
