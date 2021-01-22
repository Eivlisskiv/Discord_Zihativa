using System;

namespace AMI.Neitsillia
{
    static class PatchNote
    {
        public static string GeneralPatchNotes()
        {
            //return null;
            return
                $"Fixed daily rewards being too high tier for level < 5 {Environment.NewLine}"
                + $"Added `help schematic` to inform people of the (2) different schems types {Environment.NewLine}"
                + $"Fixed parties failing to save due to the wrong _id being used {Environment.NewLine}"
                + $"Fixed party data not properly being shared {Environment.NewLine}"
                ;
        }
        public static string StatsPatchNotes()
        {
            return null;
            return
                $" {Environment.NewLine}"
                + $" {Environment.NewLine}"
                ;
        }
        public static string ItemsPatchNotes()
        {
            //return null;
            return
                $"Fixed Unique items being scrappable {Environment.NewLine}"
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
            return null;
            return
                $" {Environment.NewLine}"
                + $" {Environment.NewLine}"
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
                 $" {Environment.NewLine}"
                ;
        }
    }
}
