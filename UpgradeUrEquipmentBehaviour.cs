using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using System.Collections.Generic;
using static TaleWorlds.Core.ItemObject;
using TaleWorlds.Localization;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;

namespace UpgradeUrEquipment
{
    public class UpgradeUrEquipmentBehaviour : CampaignBehaviorBase
    {
        private const string SelectedCompanion = "UUE_player_selected_companion";
        private const string SelectedCompanionResp = "UUE_player_selected_companion_resp";
        private const string NeedToUpgradeEquipment = "UUE_player_need_to_upgrade_equipment";
        private const string NeedToUpgradeEquipmentResp = "UUE_player_need_to_upgrade_equipment_response";
        private const string SelectedEquipment = "UUE_player_selected_equipment";
        private const string SelectedEquipmentResp = "UUE_player_selected_equipment_response";
        private const string SelectedEquipmentModifier = "UUE_player_selected_equipment_modifier";
        private const string SelectedEquipmentModifierResp = "UUE_player_selected_equipment_modifier_resp";

        private const string SelectedCancel = "UUE_player_selectd_cancel";

        private Hero selectedHero;
        private Tuple<EquipmentElement, bool, EquipmentIndex> selectedUpgradeEquipment;
        private ItemModifier selectedUpgradeItemModifier;
        private int selectedUpgradeItemPrice;

        private readonly ItemModifier defaultItemModifier;

        //拓展映射
        private Dictionary<ItemTypeEnum, string> addtionalTypeMappingGroupName = new Dictionary<ItemTypeEnum, string>() {
            { ItemTypeEnum.HorseHarness, "cloth"},
            { ItemTypeEnum.OneHandedWeapon, "sword"},
            { ItemTypeEnum.TwoHandedWeapon, "sword"},
            { ItemTypeEnum.Polearm, "polearm"},
            { ItemTypeEnum.Thrown, "spear_dart_throwing"},
            { ItemTypeEnum.Arrows, "arrow"},
            { ItemTypeEnum.Bolts, "bolt"},
            { ItemTypeEnum.Shield, "shield"},
            { ItemTypeEnum.Bow, "bow"},
            { ItemTypeEnum.Crossbow, "crossbow"},
            { ItemTypeEnum.HeadArmor, "plate"},
            { ItemTypeEnum.BodyArmor, "plate"},
            { ItemTypeEnum.LegArmor, "plate"},
            { ItemTypeEnum.HandArmor, "plate"},
            { ItemTypeEnum.Pistol, "crossbow"},
            { ItemTypeEnum.Musket, "crossbow"},
            { ItemTypeEnum.Bullets, "arrow"},
            { ItemTypeEnum.ChestArmor, "plate"},
            { ItemTypeEnum.Cape, "plate" },
        };

        public UpgradeUrEquipmentBehaviour()
        {
            defaultItemModifier = new ItemModifier();
        }

        public override void SyncData(IDataStore dataStore)
        {
            //do nothing
        }

        public override void RegisterEvents() => CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(this.OnSessionLaunched));

        public void OnSessionLaunched(CampaignGameStarter campaignGameStarter) => AddDialogs(campaignGameStarter);

        protected void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            //添加强化装备选项
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment", "weaponsmith_talk_player", SelectedCompanion, "{=hatDogUpgradeArmorOrWeapons}I need you to upgrade a armor or weapons for me",
                new ConversationSentence.OnConditionDelegate(IsBlacksmith), //铁匠才弹这玩意
                new ConversationSentence.OnConsequenceDelegate(InitializeCompanionOptions)); //选完准备同伴数据

            //老板要求选择同伴
            _ = campaignGameStarter.AddDialogLine("hatDogUprgadeEquipment_0", SelectedCompanion, SelectedCompanionResp,
                "{=NyWXSHH2}Of course. Was this for you, or someone else?",
                null, null);

            //玩家同伴选项
            _ = campaignGameStarter.AddRepeatablePlayerLine("hatDogUprgadeEquipment_0_1",
                SelectedCompanionResp,
                NeedToUpgradeEquipment,
                "{=!}{COMPANION_NAME}",
                "{=qOcw1xap}Page Down",
                SelectedCompanion,
                new ConversationSentence.OnConditionDelegate(DisplayCompanionName), 
                new ConversationSentence.OnConsequenceDelegate(SelectCompanion));
            //玩家选自己
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_0_2",
                SelectedCompanionResp,
                NeedToUpgradeEquipment,
                "{=3VxA6HaZ}This is for me.",
                null,
                new ConversationSentence.OnConsequenceDelegate(() => { 
                    selectedHero = Hero.MainHero;
                    LoadEquipmentOptions();
                }));

            //玩家取消选择
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_0_3", SelectedCompanionResp, SelectedCancel, "{=8hNYr2VX}I was just passing by.", null, null);

            //老板要求选装备
            _ = campaignGameStarter.AddDialogLine("hatDogUprgadeEquipment_1", NeedToUpgradeEquipment, NeedToUpgradeEquipmentResp,
                "{=hatDogUpgradeChooseArmorOrWeapons}Yes, of course. which armor or weapon? ",
                null, null);

            //被选择人装备选项
            _ = campaignGameStarter.AddRepeatablePlayerLine("hatDogUprgadeEquipment_1_1",
                NeedToUpgradeEquipmentResp,
                SelectedEquipment,
                "{=!}{EquipmentName}",
                "{=qOcw1xap}Page Down",
                NeedToUpgradeEquipment,
                new ConversationSentence.OnConditionDelegate(RenderEquipmentName), //获取装备名填到选项里
                new ConversationSentence.OnConsequenceDelegate(PlayerChooseEquipment), //选完之后存一下，然后获取装备等级准备后面选项
                clickableConditionDelegate: new ConversationSentence.OnClickableConditionDelegate(EquipmentClickable)); //升到顶级的装备给下不让选原因
            //重选同伴
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_1_2", NeedToUpgradeEquipmentResp, SelectedCompanion, "{=ElG1LnCA}I am thinking of someone else.", null,
                new ConversationSentence.OnConsequenceDelegate(InitializeCompanionOptions)); //重新准备同伴数据
            //不选装备了
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_1_3", NeedToUpgradeEquipmentResp, SelectedCancel, "{=8hNYr2VX}I was just passing by.", null, null);

            //老板要求选择等级
             _ = campaignGameStarter.AddDialogLine("hatDogUprgadeEquipment_2", SelectedEquipment, SelectedEquipmentResp, "{=hatDogUpgradeChooseLevel}Yes, of course. What equipment modifier do you need?", null, null);

            //可选择等级选项
            _ = campaignGameStarter.AddRepeatablePlayerLine("hatDogUprgadeEquipment_2_1",
                SelectedEquipmentResp,
                SelectedEquipmentModifier,
                "{=!}{ItemModifierName}",
                "{=qOcw1xap}Page Down",
                SelectedEquipment,
                new ConversationSentence.OnConditionDelegate(RenderItemModifierName), //获取前缀名填到选项
                new ConversationSentence.OnConsequenceDelegate(PlayerChooseItemModifier)); //选完之后算价并存一下选项
            //重选装备
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_2_2", SelectedEquipmentResp, NeedToUpgradeEquipment, "{=hatDogUpgradeReChooseEquipment}I am thinking of other equipment.", null,
                new ConversationSentence.OnConsequenceDelegate(LoadEquipmentOptions)); // 重新准备装备数据
            //选一半不选
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_2_3", SelectedEquipmentResp, SelectedCancel, "{=8hNYr2VX}I was just passing by.", null, null);

            //选择装备后报价
            _ = campaignGameStarter.AddDialogLine("hatDogUprgadeEquipment_3", SelectedEquipmentModifier, SelectedEquipmentModifierResp, "{=hatDogUpgradePriceResponse}ok, that should cost around {UPGRADE_PRICE}{GOLD_ICON}", null, null);

            //接收报价
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_3_1", SelectedEquipmentModifierResp, "close_window", "{=oHaWR73d}OK",
                null,
                new ConversationSentence.OnConsequenceDelegate(AccpetUpgradeEquipment), //同意之后替换装备并扣钱
                clickableConditionDelegate: new ConversationSentence.OnClickableConditionDelegate(MoneyCheck)); //钱不够不让点

            //不接收报价
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_3_2", SelectedEquipmentModifierResp, "close_window", "{=8hNYr2VX}I was just passing by.",
                null,
                null);

            //选一半不选统一出口
            _ = campaignGameStarter.AddDialogLine("hatDogUprgadeEquipmentCancel", SelectedCancel, "close_window", "{=FpNWdIaT}Yes, of course. Just ask me if there is anything you need.", null, null);
        }

        //出现加强装备选项判断
        private bool IsBlacksmith()
        {
            return CharacterObject.OneToOneConversationCharacter != null
                && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Blacksmith;
        }

        //初始化同伴选项
        private void InitializeCompanionOptions()
        {
            ConversationSentence.SetObjectsToRepeatOver(LoadCompanions());
        }

        private List<Hero> LoadCompanions() => PartyBase.MainParty.MemberRoster.GetTroopRoster()
            .Where(m => m.Character.IsHero && m.Character.HeroObject.Clan == Clan.PlayerClan && !m.Character.HeroObject.IsHumanPlayerCharacter)
            .Select(t => t.Character.HeroObject)
            .ToList();

        //同伴选项渲染
        private bool DisplayCompanionName()
        {
            Hero processedRepeatObject = ConversationSentence.CurrentProcessedRepeatObject as Hero;
            if (processedRepeatObject == null)
            {
                return false;
            }
            ConversationSentence.SelectedRepeatLine.SetTextVariable("COMPANION_NAME", processedRepeatObject.Name);
            return true;
        }

        //选择同伴
        private void SelectCompanion()
        {
            selectedHero = ConversationSentence.SelectedRepeatObject as Hero;
            if (selectedHero == null)
            {
                return;
            }
            LoadEquipmentOptions();
        }

        //初始化装备选项
        private void LoadEquipmentOptions()
        {
            List<Tuple<EquipmentElement, bool, EquipmentIndex>> equpiments = new List<Tuple<EquipmentElement, bool, EquipmentIndex>>();
            for (int i = 0; i < Equipment.EquipmentSlotLength; i++)
            {
                AddEquipmentToList(equpiments, selectedHero.BattleEquipment[i], false, (EquipmentIndex)i);
                AddEquipmentToList(equpiments, selectedHero.CivilianEquipment[i], true, (EquipmentIndex)i);
            }
            ConversationSentence.SetObjectsToRepeatOver(equpiments);
        }

        private void AddEquipmentToList(List<Tuple<EquipmentElement, bool, EquipmentIndex>> equpiments, EquipmentElement equipmentElement, bool isCivlian, EquipmentIndex equipmentIndex)
        {
            if (equipmentElement.Item != null && CanAddEuqipmentModifier(equipmentElement.Item))
            {
                equpiments.Add(new Tuple<EquipmentElement, bool, EquipmentIndex>(equipmentElement, isCivlian, equipmentIndex));
            }
        }

        //装备选项渲染
        private bool RenderEquipmentName()
        {
            Tuple<EquipmentElement, bool, EquipmentIndex> processedRepeatObject = ConversationSentence.CurrentProcessedRepeatObject as Tuple<EquipmentElement, bool, EquipmentIndex>;
            if (processedRepeatObject == null)
            {
                return false;
            }
            ConversationSentence.SelectedRepeatLine.SetTextVariable("EquipmentName", processedRepeatObject.Item1.Item.Name);
            return true;
        }

        //选择装备，并准备升级选项
        private void PlayerChooseEquipment()
        {
            selectedUpgradeEquipment = ConversationSentence.SelectedRepeatObject as Tuple<EquipmentElement, bool, EquipmentIndex>;
            if (selectedUpgradeEquipment == null)
            {
                return;
            }
            ConversationSentence.SetObjectsToRepeatOver(LoadItemModifers(selectedUpgradeEquipment.Item1));
        }

        private bool EquipmentClickable(out TextObject explanation)
        {
            Tuple<EquipmentElement, bool, EquipmentIndex> currentProcessedRepeatObject = ConversationSentence.CurrentProcessedRepeatObject as Tuple<EquipmentElement, bool, EquipmentIndex>;
            if (currentProcessedRepeatObject == null)
            {
                explanation = new TextObject("{=oZrVNUOk}Error");
                return false;
            }
            if (LoadItemModifers(currentProcessedRepeatObject.Item1).IsEmpty())
            {
                explanation = new TextObject("{=hatDogUpgradeRefuse}refuse, it's already the highest level");
                return false;
            }
            explanation = TextObject.Empty;
            return true;
        }

        private List<ItemModifier> LoadItemModifers(EquipmentElement item)
        {
            if (!CanAddEuqipmentModifier(item.Item))
            {
                return Enumerable.Empty<ItemModifier>().ToList();
            }
            if (item.Item.ItemComponent.ItemModifierGroup == null)
            {
                return LoadPrefabModifers(item, GetAddtionalModifier(item.Item.ItemType));
            }
            return LoadPrefabModifers(item, item.Item.ItemComponent.ItemModifierGroup.ItemModifiers);
        }

        private List<ItemModifier> GetAddtionalModifier(ItemTypeEnum itemType)
        {
            if (!addtionalTypeMappingGroupName.ContainsKey(itemType))
            {
                return Enumerable.Empty<ItemModifier>().ToList();
            }
            return Campaign.Current.ItemModifierGroups
                .Where(itemModifierGroup => itemModifierGroup.StringId == addtionalTypeMappingGroupName[itemType])
                .SelectMany(itemModifierGroup => itemModifierGroup.ItemModifiers)
                .ToList();
        }

        private List<ItemModifier> LoadPrefabModifers(EquipmentElement item, List<ItemModifier> perfabItemModifiers)
        {
            List<ItemModifier> itemModifiers = new List<ItemModifier>();
            ItemModifier currentItemModifier = item.ItemModifier;
            float priceMultiplier = currentItemModifier?.PriceMultiplier ?? 1;
            float prePriceMultiplier = priceMultiplier;

            foreach (ItemModifier itemModifier in perfabItemModifiers)
            {
                if (itemModifier.PriceMultiplier > priceMultiplier)
                {
                    if (itemModifier.PriceMultiplier < 1 && prePriceMultiplier > 1 && priceMultiplier < 1)
                    {
                        itemModifiers.Add(defaultItemModifier);
                    }
                    itemModifiers.Add(itemModifier);
                }
                prePriceMultiplier = itemModifier.PriceMultiplier;
            }
            return itemModifiers;
        }

        //升级选项渲染
        private bool RenderItemModifierName()
        {
            ItemModifier processedRepeatObject = ConversationSentence.CurrentProcessedRepeatObject as ItemModifier;
            if (selectedUpgradeEquipment == null || processedRepeatObject == null)
            {
                return false;
            }
            _ = processedRepeatObject == defaultItemModifier ?
                ConversationSentence.SelectedRepeatLine.SetTextVariable("ItemModifierName", new TextObject("{=8UBfIenN}Normal").ToString() + " " + selectedUpgradeEquipment.Item1.Item.Name.ToString()) :
                ConversationSentence.SelectedRepeatLine.SetTextVariable("ItemModifierName", processedRepeatObject.Name.SetTextVariable("ITEMNAME", selectedUpgradeEquipment.Item1.Item.Name));
            return true;
        }

        //选择升级项
        //TODO 除了算价还需要加入材料等需求
        private void PlayerChooseItemModifier()
        {
            selectedUpgradeItemModifier = ConversationSentence.SelectedRepeatObject as ItemModifier;
            if (selectedUpgradeItemModifier == null)
            {
                return;
            }
            selectedUpgradeItemPrice = Math.Max(0, CalculateUpgradePrice(selectedUpgradeEquipment.Item1, selectedUpgradeItemModifier));
            MBTextManager.SetTextVariable("UPGRADE_PRICE", selectedUpgradeItemPrice);
        }

        // 计算装备升级价格
        private int CalculateUpgradePrice(EquipmentElement equipment, ItemModifier targetModifier)
        {
            // 如果装备没有可升级的属性，则返回0
            if (LoadItemModifers(equipment).Count == 0)
            {
                return 0;
            }
            // 获取当前装备的属性和价格乘数
            ItemModifier currentModifier = equipment.ItemModifier;
            float currentPriceMultiplier = currentModifier?.PriceMultiplier ?? 1f;
            // 获取目标属性和价格乘数
            float targetPriceMultiplier = targetModifier?.PriceMultiplier ?? 1f;
            // 如果目标属性为默认属性，则价格乘数为1
            if (targetModifier == defaultItemModifier)
            {
                targetPriceMultiplier = 1f;
            }
            // 计算价格差异
            float priceDifference = targetPriceMultiplier - currentPriceMultiplier;
            // 获取装备基础价格
            int basePrice = equipment.Item.Value;
            // 计算升级价格
            int upgradePrice = (int)(basePrice * priceDifference);
            return upgradePrice * 5;
        }

        //最后一个判断: 是否可以升级
        private bool MoneyCheck(out TextObject target)
        {
            if (Hero.MainHero.Gold < selectedUpgradeItemPrice)
            {
                target = new TextObject("{=m6uSOtE4} You don't have enough money.");
                return false;
            }
            target = TextObject.Empty;
            return true;
        }

        //点击升级，扣钱并升级装备
        private void AccpetUpgradeEquipment()
        {
            if (selectedUpgradeEquipment == null || selectedUpgradeItemModifier == null || selectedUpgradeItemPrice == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=hatDogUpgradeFailed}Upgrade Ur Equipment:failed!!!!! plz try again").ToString(), new Color(1, 0, 0)));
                return;
            }
            if (selectedUpgradeEquipment.Item2)
            {
                EquipmentElement newEquipmentElement = new EquipmentElement(selectedUpgradeEquipment.Item1.Item, selectedUpgradeItemModifier == defaultItemModifier ? null : selectedUpgradeItemModifier);
                selectedHero.CivilianEquipment[selectedUpgradeEquipment.Item3] = newEquipmentElement;
                if (selectedHero.CivilianEquipment[selectedUpgradeEquipment.Item3].ItemModifier == null && selectedUpgradeItemModifier != defaultItemModifier)
                {
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=hatDogUpgradeFailed}Upgrade Ur Equipment:failed!!!!! plz try again").ToString(), new Color(1, 0, 0)));
                    return;
                }
            }
            else
            {
                EquipmentElement newEquipmentElement = new EquipmentElement(selectedUpgradeEquipment.Item1.Item, selectedUpgradeItemModifier == defaultItemModifier ? null : selectedUpgradeItemModifier);
                selectedHero.BattleEquipment[selectedUpgradeEquipment.Item3] = newEquipmentElement;
                if (selectedHero.BattleEquipment[selectedUpgradeEquipment.Item3].ItemModifier == null && selectedUpgradeItemModifier != defaultItemModifier)
                {
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=hatDogUpgradeFailed}Upgrade Ur Equipment:failed!!!!! plz try again").ToString(), new Color(1, 0, 0)));
                    return;
                }
            }
            GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, selectedUpgradeItemPrice);
        }

        private bool CanAddEuqipmentModifier(ItemObject item)
        {
            return item.ItemComponent.ItemModifierGroup != null || IsAddtionalSupportWeapon(item) || IsAddtionalSupportArmor(item);
        }

        private bool IsAddtionalSupportArmor(ItemObject item)
        {
            return  item.ItemType == ItemTypeEnum.HorseHarness;
        }

        private bool IsAddtionalSupportWeapon(ItemObject item)
        {
            return item.ItemType == ItemTypeEnum.OneHandedWeapon
                    || item.ItemType == ItemTypeEnum.TwoHandedWeapon
                    || item.ItemType == ItemTypeEnum.Polearm
                    || item.ItemType == ItemTypeEnum.Arrows
                    || item.ItemType == ItemTypeEnum.Bolts
                    || item.ItemType == ItemTypeEnum.Shield
                    || item.ItemType == ItemTypeEnum.Bow
                    || item.ItemType == ItemTypeEnum.Crossbow
                    || item.ItemType == ItemTypeEnum.Thrown;
        }

    }
}
