using AMI.AMIData;
using AMI.Neitsillia.Encounters;
using System;
using AMI.Neitsillia.NPCSystems;
using AMI.Neitsillia.User.UserInterface;

namespace AMI.Neitsillia.User.PlayerPartials
{
    partial class Player
    {
        public DataBaseRelation<string, Encounter> EncounterKey;

        internal Encounter Encounter
        {
            get => EncounterKey?.Data ?? (EncounterKey = new DataBaseRelation<string, Encounter>(Party?.EncounterKey, null)).Data;
            set => EncounterKey = new DataBaseRelation<string, Encounter>(value._id, value);
        }

        public void EndEncounter()
        {
            if (Encounter == null) return;
            if (IsEncounter("npc"))
            {
                if(Party != null)
                {
                    Party.ForEachPlayerSync(this, p =>
                    {
                        if(p.ui?.type == MsgType.ConfirmTransaction)
                            Shopping.PendingTransaction.Cancel(p, p.ui?.data);
                    });
                }
                if (Party == null || !Party.UpdateFollower(Encounter.npc))
                {
                    var popu = Area.GetPopulation(Areas.AreaExtentions.Population.Type.Population);
                    if (popu.Count < 20) popu.Add(Encounter.npc);
                    else PopulationHandler.Add(Area, Encounter.npc);
                }
            }
            if (Encounter.loot.Count > 0 && Party != null && Party.NPCMembers.Count > 0)
                Party.NPCMembers[AMYPrototype.Program.rng.Next(Party.NPCMembers.Count)].inventory.Add(Encounter.loot, -1);

            try {
                EncounterKey.Delete().Wait();
            } catch (Exception) { }
        }

        public Encounter NewEncounter(Encounter.Names name, bool endOld = false, string data = null)
            => NewEncounter(new Encounter(name, this, data), endOld);

        public Encounter NewEncounter(Encounter e, bool endOld = false)
        {
            if (endOld) EndEncounter();

            if (e.IsCombatEncounter() && e.mobs != null)
                e.StartCombat();

            if (Party != null) e._id = Party.EncounterKey;
            else if(e._id == null)  e._id = this._id;

            Encounter = e;

            return e;
        }

        public bool IsEncounter(string type)
        {
            if (Encounter == null)
                return false;
            return (type.ToLower()) switch
            {
                "combat" => Encounter.IsCombatEncounter(),
                "passive" => Encounter.IsPassiveEncounter(),
                "npc" => Encounter.IsNPC(),
                "partyshared" => 
                    Encounter.IsCombatEncounter() || 
                    Encounter.Name == Encounter.Names.Puzzle || 
                    Encounter.Name == Encounter.Names.Loot || 
                    Encounter.IsNPC(),
                _ => false,
            };
        }
        public bool IsEncounter(params Encounter.Names[] names)
        {
            if (Encounter == null)
                return false;
            foreach (var n in names)
                if (Encounter.Name == n)
                    return true;
            return false;
        }
    }
}
