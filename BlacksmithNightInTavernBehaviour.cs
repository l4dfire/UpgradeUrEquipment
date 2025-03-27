using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using System.Collections.Generic;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace UpgradeUrEquipment
{
    //2024-07-17 铁匠加班
    //2025-01-07 铁匠不加班了，晚上去酒馆喝酒
    public class BlacksmithNightInTavernBehaviour : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.BlacksmithNightInTavern));
        }

        public override void SyncData(IDataStore dataStore)
        {
            //do nothing
        }

        private void BlacksmithNightInTavern(Dictionary<string, int> unusedUsablePointCount)
        {
            //晚上生效
            if (Campaign.Current.IsDay)
                return;
            Location location = CampaignMission.Current.Location;
            if (location == null || location.StringId != "tavern")
                return;
            CampaignMission.Current.Location.
                AddLocationCharacters(new CreateLocationCharacterDelegate(CreateTavernBlacksmith),
                Settlement.CurrentSettlement.Culture,
                LocationCharacter.CharacterRelations.Neutral, 1);
        }

        private static LocationCharacter CreateTavernBlacksmith(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject blacksmith = culture.Blacksmith;
            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(blacksmith.Race, "_settlement_slow");
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(blacksmith, out int minimumAge, out int maximumAge);
            return new LocationCharacter(new AgentData(new SimpleAgentOrigin(blacksmith)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge)),
                new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors),
                "npc_common", false, relation, null, true);
        }


    }
}
