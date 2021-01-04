using System;

namespace AMI.Neitsillia
{
    static class PatchNote
    {
        public static string GeneralPatchNotes()
        {
            return null;
            return
                $" {Environment.NewLine}"
                + $" {Environment.NewLine}"
                ;
        }
        public static string StatsPatchNotes()
        {
            //return null;
            return
                $"Dexterity no longer grant an Agility multiplier {Environment.NewLine}"
                + $"Lowered Agility's stamina buff from 150% to 80% agility {Environment.NewLine}"
                + $"Fixed event shop {Environment.NewLine}"
                ;
        }
        public static string ItemsPatchNotes()
        {
            //return null;
            return
                $"Gear of tier lower than rank 20 now gain up to 20 extra durability. (No applicable to existing gear) {Environment.NewLine}"
                ;
        }
        public static string CombatPatchNotes()
        {
            return null;
            return
                $" {Environment.NewLine}"
                 ;
        }
        public static string AbilitiesPatchNotes()
        {
            return null;
            return
                $" {Environment.NewLine}" 
                 ;
        }
        public static string WorldPatchNotes()
        {
            //return null;
            return
                $"Fixed Dungeon Bosses not spawning while in a party {Environment.NewLine}"
                + $"Fixed the increase in floors climbed while being higher level than the area's level {Environment.NewLine}"
                ;
        }
        public static string MobsPatchNotes()
        {
            return null;
            return
               $" {Environment.NewLine}"
                ;
        }
        public static string CraftingPatchNotes()
        {
            return null;
            return
                $" {Environment.NewLine}"
                ;
        }
        public static string NPCPatchNotes()
        {
            return null;
            return
                $" {Environment.NewLine}"
                ;
        }
        public static string SocialPatchNotes()
        {
            return null;
            return
                $" {Environment.NewLine}"
                ;
        }
        public static string QuestPatchNotes()
        {
            //return null;
            return
                 $"Fixed Whispers Of The Wind quest line not being given after completing the required quest. If you've missed this quest due to this, contact an admin {Environment.NewLine}"
                ;
        }
    }
}
