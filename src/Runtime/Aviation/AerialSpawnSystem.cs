using System;
using System.IO;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DINOForge.Runtime.Aviation
{
    /// <summary>
    /// ECS system that detects newly spawned aerial units and initializes their altitude.
    /// Watches for entities that have <see cref="AerialUnitComponent"/> but are at Y=0
    /// (ground level), and immediately sets them to their CruiseAltitude to avoid
    /// the appearance of units popping up from the ground.
    ///
    /// An alternative "takeoff" effect can be achieved by leaving Y=0 and letting
    /// AerialMovementSystem ascend naturally — set SpawnAtAltitude=false to enable this.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class AerialSpawnSystem : SystemBase
    {
        /// <summary>
        /// When true, newly detected aerial units are teleported to CruiseAltitude instantly.
        /// When false, units start at Y=0 and ascend naturally (takeoff animation feel).
        /// </summary>
        public static bool SpawnAtAltitude = true;

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            if (!SpawnAtAltitude)
                return;

            // Set translation Y for any aerial unit still at ground level (Y < 1f)
            Entities
                .WithAll<AerialUnitComponent>()
                .WithAll<Translation>()
                .ForEach((Entity entity, ref AerialUnitComponent aerial, ref Translation translation) =>
                {
                    if (translation.Value.y < 1f && aerial.CruiseAltitude > 0f)
                    {
                        translation.Value = new float3(
                            translation.Value.x,
                            aerial.CruiseAltitude,
                            translation.Value.z);
                    }
                })
                .WithoutBurst()
                .Run();
        }
    }
}
