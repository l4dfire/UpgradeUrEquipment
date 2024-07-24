using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.AgentOrigins;

namespace UpgradeUrEquipment
{
    //2024-07-17 铁匠加班
    public class BlacksmithWorkingOvertimeBehaviour : CampaignBehaviorBase
    {
        public override void RegisterEvents() => CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener((object)this, new Action<Dictionary<string, int>>(this.BlacksmithWorkingOvertime));

        public override void SyncData(IDataStore dataStore)
        {
            //do nothing
        }

        private void BlacksmithWorkingOvertime(Dictionary<string, int> unusedUsablePointCount)
        {
            //游戏自身逻辑让铁匠白天出来，所以这里只让晚上出来
            if (CampaignMission.Current.Location != PlayerEncounter.LocationEncounter.Settlement.LocationComplex.GetLocationWithId("center") || Campaign.Current.IsDay)
                return;
            if (!unusedUsablePointCount.TryGetValue("sp_blacksmith", out int count))
                return;
            Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center").
                AddLocationCharacters(new CreateLocationCharacterDelegate(CreateBlacksmith), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, count);
        }

        private static LocationCharacter CreateBlacksmith(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject blacksmith = culture.Blacksmith;
            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(blacksmith.Race, "_settlement");
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(blacksmith, out int minimumAge, out int maximumAge);
            return new LocationCharacter(new AgentData(new SimpleAgentOrigin(blacksmith)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "sp_blacksmith", true, relation, null, true);
        }
    }
}
