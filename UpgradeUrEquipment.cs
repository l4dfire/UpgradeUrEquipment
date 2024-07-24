using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;

namespace UpgradeUrEquipment
{
    public class UpgradeUrEquipmentSubModule : MBSubModuleBase
    {
        private readonly UpgradeUrEquipmentBehaviour upgradeUrEquipmentBehaviour;
        private readonly BlacksmithWorkingOvertimeBehaviour blacksmithWorkingOvertimeBehaviour;

        public UpgradeUrEquipmentSubModule()
        {
            upgradeUrEquipmentBehaviour = new UpgradeUrEquipmentBehaviour();
            blacksmithWorkingOvertimeBehaviour = new BlacksmithWorkingOvertimeBehaviour();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter gameStarterObject)
        {
            if (!(game.GameType is Campaign))
                return;
            if (!(gameStarterObject is CampaignGameStarter campaignGameStarter))
                return;
            campaignGameStarter.AddBehavior(upgradeUrEquipmentBehaviour);
            campaignGameStarter.AddBehavior(blacksmithWorkingOvertimeBehaviour);
        }
    }
}
