using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;

namespace UpgradeUrEquipment
{
    public class UpgradeUrEquipmentSubModule : MBSubModuleBase
    {
        private readonly UpgradeUrEquipmentBehaviour upgradeUrEquipmentBehaviour;
        private readonly BlacksmithNightInTavernBehaviour blacksmithNightInTavernBehaviour;

        public UpgradeUrEquipmentSubModule()
        {
            upgradeUrEquipmentBehaviour = new UpgradeUrEquipmentBehaviour();
            blacksmithNightInTavernBehaviour = new BlacksmithNightInTavernBehaviour();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter gameStarterObject)
        {
            if (!(game.GameType is Campaign))
                return;
            if (!(gameStarterObject is CampaignGameStarter campaignGameStarter))
                return;
            campaignGameStarter.AddBehavior(upgradeUrEquipmentBehaviour);
            campaignGameStarter.AddBehavior(blacksmithNightInTavernBehaviour);
        }
    }
}
