﻿using FishermanMod.Survivors.Fisherman.SkillStates;

namespace FishermanMod.Survivors.Fisherman
{
    public static class FishermanStates
    {
        public static void Init()
        {
            Modules.Content.AddEntityState(typeof(SlashCombo));

            Modules.Content.AddEntityState(typeof(Shoot));

            Modules.Content.AddEntityState(typeof(Roll));

            Modules.Content.AddEntityState(typeof(ThrowBomb));
        }
    }
}
