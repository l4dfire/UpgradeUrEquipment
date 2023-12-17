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

        private const string SelectedUpgradeAll = "UUE_player_selected_all";
        private const string SelectedUpgradeAllResp = "UUE_player_selected_all_response";

        private const string SelectedCompanion = "UUE_player_selected_companion";
        private const string SelectedCompanionResp = "UUE_player_selected_companion_resp";
        private const string NeedToUpgradeEquipment = "UUE_player_need_to_upgrade_equipment";
        private const string NeedToUpgradeEquipmentResp = "UUE_player_need_to_upgrade_equipment_response";
        private const string SelectedEquipment = "UUE_player_selected_equipment";
        private const string SelectedEquipmentResp = "UUE_player_selected_equipment_response";
        private const string SelectedEquipmentModifier = "UUE_player_selected_equipment_modifier";
        private const string SelectedEquipmentModifierResp = "UUE_player_selected_equipment_modifier_resp";

        private const string SelectedCancel = "UUE_player_selected_cancel";

        private Hero selectedHero;
        private Tuple<EquipmentElement, bool, EquipmentIndex> selectedUpgradeEquipment;
        private ItemModifier selectedUpgradeItemModifier;
        private int selectedUpgradeItemPrice;

        private readonly ItemModifier defaultItemModifier;

        //拓展映射
        private Dictionary<ItemTypeEnum, string> additionalTypeMappingGroupName = new Dictionary<ItemTypeEnum, string>() {
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
                new ConversationSentence.OnConsequenceDelegate(() =>
                {
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
                new ConversationSentence.OnConsequenceDelegate(AcceptUpgradeEquipment), //同意之后替换装备并扣钱
                clickableConditionDelegate: new ConversationSentence.OnClickableConditionDelegate(CheckIfPlayerHasEnoughMoney)); //钱不够不让点

            //不接收报价
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_3_2", SelectedEquipmentModifierResp, "close_window", "{=8hNYr2VX}I was just passing by.",
                null,
                null);

            //放弃升级统一出口
            _ = campaignGameStarter.AddDialogLine("hatDogUprgadeEquipmentCancel", SelectedCancel, "close_window", "{=FpNWdIaT}Yes, of course. Just ask me if there is anything you need.", null, null);

            //2023-07-15 更新:选择人物后直接升级一整套
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_X_1", NeedToUpgradeEquipmentResp, SelectedUpgradeAll, "{=hatDogUpgradeAllEquipment}Upgrade all equipment to the best", null, new ConversationSentence.OnConsequenceDelegate(PlayerChooseUpgradeAll),
                clickableConditionDelegate: new ConversationSentence.OnClickableConditionDelegate(CanUpgradeAll), //可以升级装备需要 >= 3
                priority:110);

            //2023-07-15 更新:选择升级全部报价
            _ = campaignGameStarter.AddDialogLine("hatDogUprgadeEquipment_X_2", SelectedUpgradeAll, SelectedUpgradeAllResp, "{=hatDogUpgradePriceResponse}ok, that should cost around {UPGRADE_PRICE}{GOLD_ICON}", null, null, priority: 110);

            //2023-07-15 更新:接收全部升级报价
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_X_3", SelectedUpgradeAllResp, "close_window", "{=oHaWR73d}OK",
                null,
                new ConversationSentence.OnConsequenceDelegate(AcceptUpgradeAllEquipment), //同意之后替换装备并扣钱
                clickableConditionDelegate: new ConversationSentence.OnClickableConditionDelegate(CheckIfPlayerHasEnoughMoney), //钱不够不让点
                priority: 110); 

            //2023-07-15 更新:不接收全部升级报价
            _ = campaignGameStarter.AddPlayerLine("hatDogUprgadeEquipment_X_4", SelectedUpgradeAllResp, "close_window", "{=8hNYr2VX}I was just passing by.",
                null,null, priority: 110);
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
            .Where(h => 
                Enumerable.Range(0, Equipment.EquipmentSlotLength)
                    .SelectMany(i => new[] { h.BattleEquipment[i], h.CivilianEquipment[i] })
                    .Any(e => !LoadItemModifiers(e).IsEmpty())
            )
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
            List<Tuple<EquipmentElement, bool, EquipmentIndex>> equipments = new List<Tuple<EquipmentElement, bool, EquipmentIndex>>();
            for (int i = 0; i < Equipment.EquipmentSlotLength; i++)
            {
                AddEquipmentToList(equipments, selectedHero.BattleEquipment[i], false, (EquipmentIndex)i);
                AddEquipmentToList(equipments, selectedHero.CivilianEquipment[i], true, (EquipmentIndex)i);
            }
            ConversationSentence.SetObjectsToRepeatOver(equipments);
        }

        private void AddEquipmentToList(List<Tuple<EquipmentElement, bool, EquipmentIndex>> equipments, EquipmentElement equipmentElement, bool isCivilian, EquipmentIndex equipmentIndex)
        {
            if (equipmentElement.Item != null && CanAddEquipmentModifier(equipmentElement.Item))
            {
                equipments.Add(new Tuple<EquipmentElement, bool, EquipmentIndex>(equipmentElement, isCivilian, equipmentIndex));
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
            ConversationSentence.SetObjectsToRepeatOver(LoadItemModifiers(selectedUpgradeEquipment.Item1));
        }

        private bool EquipmentClickable(out TextObject explanation)
        {
            Tuple<EquipmentElement, bool, EquipmentIndex> currentProcessedRepeatObject = ConversationSentence.CurrentProcessedRepeatObject as Tuple<EquipmentElement, bool, EquipmentIndex>;
            if (currentProcessedRepeatObject == null)
            {
                explanation = new TextObject("{=oZrVNUOk}Error");
                return false;
            }
            if (LoadItemModifiers(currentProcessedRepeatObject.Item1).IsEmpty())
            {
                explanation = new TextObject("{=hatDogUpgradeRefuse}refuse, it's already the highest level");
                return false;
            }
            explanation = TextObject.Empty;
            return true;
        }

        private List<ItemModifier> LoadItemModifiers(EquipmentElement item)
        {
            if (!CanAddEquipmentModifier(item.Item))
            {
                return Enumerable.Empty<ItemModifier>().ToList();
            }
            if (item.Item.ItemComponent.ItemModifierGroup == null)
            {
                return LoadPrefabModifiers(item, GetAdditionalModifier(item.Item.ItemType));
            }
            return LoadPrefabModifiers(item, item.Item.ItemComponent.ItemModifierGroup.ItemModifiers);
        }

        private List<ItemModifier> GetAdditionalModifier(ItemTypeEnum itemType)
        {
            if (!additionalTypeMappingGroupName.ContainsKey(itemType))
            {
                return Enumerable.Empty<ItemModifier>().ToList();
            }
            return Campaign.Current.ItemModifierGroups
                .Where(itemModifierGroup => itemModifierGroup.StringId == additionalTypeMappingGroupName[itemType])
                .SelectMany(itemModifierGroup => itemModifierGroup.ItemModifiers)
                .ToList();
        }

        private List<ItemModifier> LoadPrefabModifiers(EquipmentElement item, List<ItemModifier> prefabItemModifiers)
        {
            List<ItemModifier> itemModifiers = new List<ItemModifier>();
            ItemModifier currentItemModifier = item.ItemModifier;
            float priceMultiplier = currentItemModifier?.PriceMultiplier ?? 1;
            float prePriceMultiplier = priceMultiplier;

            foreach (ItemModifier itemModifier in prefabItemModifiers)
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

        //选择升级全部
        private void PlayerChooseUpgradeAll()
        {
            if (selectedHero == null)
            {
                return;
            }
            selectedUpgradeItemPrice = 0;
            for (int i = 0; i < Equipment.EquipmentSlotLength; i++)
            {
                selectedUpgradeItemPrice += CalculateMaxUpgradePrice(selectedHero.BattleEquipment[i]);
                selectedUpgradeItemPrice += CalculateMaxUpgradePrice(selectedHero.CivilianEquipment[i]);
            }
            MBTextManager.SetTextVariable("UPGRADE_PRICE", selectedUpgradeItemPrice);
        }

        private int CalculateMaxUpgradePrice(EquipmentElement equipmentElement)
        {
            if (equipmentElement.Item == null || equipmentElement.Item.ItemComponent == null)
            {
                return 0;
            }
            List<ItemModifier> equipmentModifier = LoadItemModifiers(equipmentElement);
            if (equipmentModifier.IsEmpty())
            {
                return 0;
            }
            return CalculateUpgradePrice(equipmentElement, equipmentModifier[0]);
        }

        // 计算装备升级价格
        private int CalculateUpgradePrice(EquipmentElement equipment, ItemModifier targetModifier)
        {
            // 如果装备没有可升级的属性，则返回0
            if (LoadItemModifiers(equipment).Count == 0)
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
            //2023-07-15: 高级装备需要更贵的价格，低级装备升级更便宜
            return upgradePrice * (int)(equipment.Item.Tier + 1);
        }

        //检查可以升级的装备总数是否 >= 3
        private bool CanUpgradeAll(out TextObject target)
        {
            if (selectedHero == null)
            {
                target = TextObject.Empty;
                return false;
            }
            int cnt = 0;
            for (int i = 0; i < Equipment.EquipmentSlotLength && cnt < 3; i++)
            {
                if(!LoadItemModifiers(selectedHero.BattleEquipment[i]).IsEmpty()) { cnt++; }
                if (!LoadItemModifiers(selectedHero.CivilianEquipment[i]).IsEmpty()) { cnt++; }
            }
            if (cnt < 3)
            {
                target = new TextObject("{=hatDogUpgradeAllRefuse} The number of upgradable equipment is less than 3.");
                return false;
            }
            target = TextObject.Empty;
            return true;
        }

        //最后一个判断: 是否可以升级
        private bool CheckIfPlayerHasEnoughMoney(out TextObject target)
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
        private void AcceptUpgradeEquipment()
        {
            if (selectedUpgradeEquipment == null || selectedUpgradeItemModifier == null || selectedUpgradeItemPrice == 0)
            {
                DisplayErrorMessage();
                return;
            }
            if (selectedUpgradeEquipment.Item2)
            {
                EquipmentElement newEquipmentElement = new EquipmentElement(selectedUpgradeEquipment.Item1.Item, selectedUpgradeItemModifier == defaultItemModifier ? null : selectedUpgradeItemModifier);
                selectedHero.CivilianEquipment[selectedUpgradeEquipment.Item3] = newEquipmentElement;
                if (selectedHero.CivilianEquipment[selectedUpgradeEquipment.Item3].ItemModifier == null && selectedUpgradeItemModifier != defaultItemModifier)
                {
                    DisplayErrorMessage();
                    return;
                }
            }
            else
            {
                EquipmentElement newEquipmentElement = new EquipmentElement(selectedUpgradeEquipment.Item1.Item, selectedUpgradeItemModifier == defaultItemModifier ? null : selectedUpgradeItemModifier);
                selectedHero.BattleEquipment[selectedUpgradeEquipment.Item3] = newEquipmentElement;
                if (selectedHero.BattleEquipment[selectedUpgradeEquipment.Item3].ItemModifier == null && selectedUpgradeItemModifier != defaultItemModifier)
                {
                    DisplayErrorMessage();
                    return;
                }
            }
            GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, selectedUpgradeItemPrice);
        }

        private void DisplayErrorMessage()
        {
            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=hatDogUpgradeFailed}Upgrade Ur Equipment:failed!!!!! plz try again").ToString(), new Color(1, 0, 0)));
        }

        //点击升级，扣钱并升级全部装备
        private void AcceptUpgradeAllEquipment()
        {
            if (selectedHero == null)
            {
                DisplayErrorMessage();
                return;
            }
            if (selectedUpgradeItemPrice == 0)
            {
                return;
            }
            for (int i = 0; i < Equipment.EquipmentSlotLength; i++)
            {
                selectedHero.BattleEquipment[i] = GetMaxUpgradedEquipment(selectedHero.BattleEquipment[i]);
                selectedHero.CivilianEquipment[i] = GetMaxUpgradedEquipment(selectedHero.CivilianEquipment[i]);
            }
            GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, selectedUpgradeItemPrice);
        }

        private EquipmentElement GetMaxUpgradedEquipment(EquipmentElement equipmentElement)
        {
            if (!CanAddEquipmentModifier(equipmentElement.Item))
            {
                return equipmentElement;
            }
            List<ItemModifier> equipmentModifier = LoadItemModifiers(equipmentElement);
            if (equipmentModifier.IsEmpty())
            {
                return equipmentElement;
            }
            return new EquipmentElement(equipmentElement.Item, equipmentModifier[0]);
        }

        private bool CanAddEquipmentModifier(ItemObject item)
        {
            return item != null && item.ItemComponent != null && (item.ItemComponent.ItemModifierGroup != null || IsAdditionalSupportWeapon(item) || IsAdditionalSupportArmor(item));
        }

        private bool IsAdditionalSupportArmor(ItemObject item)
        {
            return item.ItemType == ItemTypeEnum.HorseHarness;
        }

        private bool IsAdditionalSupportWeapon(ItemObject item)
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
