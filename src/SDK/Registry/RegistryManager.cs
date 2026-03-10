using DINOForge.SDK.Models;

namespace DINOForge.SDK.Registry
{
    public class RegistryManager
    {
        public IRegistry<UnitDefinition> Units { get; }
        public IRegistry<BuildingDefinition> Buildings { get; }
        public IRegistry<FactionDefinition> Factions { get; }
        public IRegistry<WeaponDefinition> Weapons { get; }
        public IRegistry<ProjectileDefinition> Projectiles { get; }
        public IRegistry<DoctrineDefinition> Doctrines { get; }

        public RegistryManager()
        {
            Units       = new Registry<UnitDefinition>();
            Buildings   = new Registry<BuildingDefinition>();
            Factions    = new Registry<FactionDefinition>();
            Weapons     = new Registry<WeaponDefinition>();
            Projectiles = new Registry<ProjectileDefinition>();
            Doctrines   = new Registry<DoctrineDefinition>();
        }
    }
}
