namespace AMI.Neitsillia.User.UserInterface
{
    public enum MsgType
    {
        Other,
        //Basics
        Main, Inventory, Sheet, Stats, Schems, Junctions,
        //Levels
        XP, Skills, Abilities, AbilityLevel,
        //NPC
        NPC, NPCInv, ConfirmTransaction, NPCRepair,
        //Encounter
        Combat, Loot, Puzzle,
        //Character Creation
        SetSkill, ConfirmSkills, ConfirmCharDel, 
        ChooseRace, StarterAbilities, AutoNewCharacter,
        //Adventure
        Adventure, AdventureQuest,
        //Rest
        Rest, EndRest, 
        //Crafting
        ConfirmUpgrade, Craft,
        //Social
        PartyInvite, DuelOffer, 
        //Trading
        OfferList, ConfirmOffer, InspectOffer, Inbox,
        //Speciality
        SpecSelection, SpecMain,
        SpecPerks, SpecAbility,
        //Arena
        ArenaGameMode, ArenaModifiers, ArenaService, ArenaFights,
        ResourceCrateList, ResourceCrateOpening,
        //Pets
        EggPocket, ConfirmEggDiscard, EggInfo, PetList, 
        PetUpgrade, PetEvolve, PetShop, InspectPet,
        //Quest
        QuestList, QuestInfo, AcceptQuests, DailyQuestBoard,
        //Tavern Service
        Tavern, BountyBoard,
        //Gambling
        Lottery, GamblingGames, DiceGame, CardGame,
        //Event
        Event, EventShop,
        //House
        Sandbox, SandboxStorage, HouseFollowers, 

        //Sandbox
        ComfirmTile, TileControls, TileProductions, TileProduce,

        //Dynasty
        DynastyUpgrade, DynastyInvite,
        DynastyMembership,
        DynastyMember,

        NewStronghold, SandboxInventory, AcceptBuilding, AcceptBuildingUpgrade,
        
    }
}
