using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;
using UnityEngine;
using static Salara.CustomFunctions;
using static Salara.Plugin;
using static Salara.DescriptionFunctions;
using static Salara.SalaraFunctions;
using System.Text;
using TMPro;
using Obeliskial_Essentials;
using System.Data.Common;

namespace Salara
{
    [HarmonyPatch]
    internal class Traits
    {
        // list of your trait IDs

        public static string[] simpleTraitList = ["trait0", "trait1a", "trait1b", "trait2a", "trait2b", "trait3a", "trait3b", "trait4a", "trait4b"];

        public static string[] myTraitList = simpleTraitList.Select(trait => subclassname.ToLower() + trait).ToArray(); // Needs testing

        public static string trait0 = myTraitList[0];
        // static string trait1b = myTraitList[1];
        public static string trait2a = myTraitList[3];
        public static string trait2b = myTraitList[4];
        public static string trait4a = myTraitList[7];
        public static string trait4b = myTraitList[8];

        // public static int infiniteProctection = 0;
        // public static int bleedInfiniteProtection = 0;
        public static bool isDamagePreviewActive = false;

        public static bool isCalculateDamageActive = false;
        public static int infiniteProctection = 0;

        public static string debugBase = "Binbin - Testing " + heroName + " ";


        public static void DoCustomTrait(string _trait, ref Trait __instance)
        {
            // get info you may need
            Enums.EventActivation _theEvent = Traverse.Create(__instance).Field("theEvent").GetValue<Enums.EventActivation>();
            Character _character = Traverse.Create(__instance).Field("character").GetValue<Character>();
            Character _target = Traverse.Create(__instance).Field("target").GetValue<Character>();
            int _auxInt = Traverse.Create(__instance).Field("auxInt").GetValue<int>();
            string _auxString = Traverse.Create(__instance).Field("auxString").GetValue<string>();
            CardData _castedCard = Traverse.Create(__instance).Field("castedCard").GetValue<CardData>();
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            TraitData traitData = Globals.Instance.GetTraitData(_trait);
            List<CardData> cardDataList = [];
            List<string> heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);
            Hero[] teamHero = MatchManager.Instance.GetTeamHero();
            NPC[] teamNpc = MatchManager.Instance.GetTeamNPC();

            if (!IsLivingHero(_character))
            {
                return;
            }

            if (_trait == trait0)
            {
                // Shield on you increases All Damage and Healing Done by 0.2 per charge
                // Done in GACM
            }


            else if (_trait == trait2a)
            {
                // trait2a
                //When you play a Mind Spell, add a randomly upgraded Prayer of Protection with cost 0 and Vanish to your hand. (1 time/turn)",
                string traitName = traitData.TraitName;
                string traitId = _trait;

                if (CanIncrementTraitActivations(traitId) && _castedCard.HasCardType(Enums.CardType.Holy_Spell))// && MatchManager.Instance.energyJustWastedByHero > 0)
                {

                    LogDebug($"Handling Trait {traitId}: {traitName}");
                    if (!((UnityEngine.Object)_character.HeroData != (UnityEngine.Object)null) || MatchManager.Instance.CountHeroHand() == 10)
                        return;
                    string str = "prayerofprotection";
                    int randomIntRange = MatchManager.Instance.GetRandomIntRange(0, 100, "trait");
                    string cardInDictionary = MatchManager.Instance.CreateCardInDictionary(randomIntRange >= 45 ? (randomIntRange >= 90 ? str + "rare" : str + "b") : str + "a");
                    CardData cardData = MatchManager.Instance.GetCardData(cardInDictionary);
                    cardData.Vanish = true;
                    cardData.EnergyReductionToZeroPermanent = true;
                    MatchManager.Instance.GenerateNewCard(1, cardInDictionary, false, Enums.CardPlace.Hand);
                    // _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_Chastise") + Functions.TextChargesLeft(MatchManager.Instance.activatedTraits[nameof(chastise)], traitData.TimesPerTurn), Enums.CombatScrollEffectType.Trait);
                    MatchManager.Instance.ItemTraitActivated();
                    MatchManager.Instance.CreateLogCardModification(cardData.InternalId, MatchManager.Instance.GetHero(_character.HeroIndex));
                    IncrementTraitActivations(traitId);
                }
            }



            else if (_trait == trait2b)
            {
                // trait 2b:  
                // When you play a Healing Spell that costs Energy, refund 1 and gain 3 Shield. (3 times/turn)",
                string traitName = traitData.TraitName;
                string traitId = _trait;

                if (CanIncrementTraitActivations(traitId) && _castedCard.HasCardType(Enums.CardType.Healing_Spell) && MatchManager.Instance.energyJustWastedByHero > 0)
                {
                    LogDebug($"Handling Trait {traitId}: {traitName}");
                    _character?.ModifyEnergy(1);
                    _character?.SetAuraTrait(_character, "shield", 3);
                    IncrementTraitActivations(traitId);
                }
            }

            else if (_trait == trait4a)
            {
                // trait4a:
                // When you play a Cold Spell, suffer 2 Chill. When you play a Fire Spell, gain 2 Fury.
                string traitName = traitData.TraitName;
                string traitId = _trait;

                LogDebug($"Handling Trait {traitId}: {traitName}");
                if (_castedCard.HasCardType(Enums.CardType.Cold_Spell))
                {
                    _character.SetAuraTrait(_character, "chill", 2);
                }
                if (_castedCard.HasCardType(Enums.CardType.Fire_Spell))
                {
                    _character.SetAuraTrait(_character, "fury", 2);
                }
            }

            else if (_trait == trait4b)
            {
                string traitName = traitData.TraitName;
                string traitId = _trait;
                LogDebug($"Handling Trait {traitId}: {traitName}");
                // trait4b:
                // When you play a Defense, gain 2 Block and apply 2 Chill to all enemies.
                if (_castedCard.HasCardType(Enums.CardType.Defense))
                {
                    _character.SetAuraTrait(_character, "block", 2);
                    ApplyAuraCurseToAll("chill", 2, AppliesTo.Monsters, _character, useCharacterMods: true);
                }
            }

        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Trait), "DoTrait")]
        public static bool DoTrait(Enums.EventActivation _theEvent, string _trait, Character _character, Character _target, int _auxInt, string _auxString, CardData _castedCard, ref Trait __instance)
        {
            if ((UnityEngine.Object)MatchManager.Instance == (UnityEngine.Object)null)
                return false;
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            if (Content.medsCustomTraitsSource.Contains(_trait) && myTraitList.Contains(_trait))
            {
                DoCustomTrait(_trait, ref __instance);
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.HealReceivedFinal))]
        public static void HealReceivedFinalPostfix(Character __instance, int __result, int heal, bool isIndirect = false)
        {
            LogDebug("HealReceivedFinalPostfix");
            // if (infiniteProctection > 100)
            //     return;
            if (isDamagePreviewActive || isCalculateDamageActive)
                return;
            if (MatchManager.Instance == null)
                return;
            // if (!IsLivingHero(__instance))
            //     return;

            string traitOfInterest = trait2a;
            // int heal = __result;

            HandleOverhealTraits(ref __instance, __result, "HealRecievedFinalPostfix");

        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Item), "DoItemData")]
        private static void DoItemDataPrefix(
            Item __instance,
            Character target,
            string itemName,
            int auxInt,
            CardData cardItem,
            string itemType,
            ItemData itemData,
            Character character,
            int order,
            string castedCardId = "",
            Enums.EventActivation theEvent = Enums.EventActivation.None)
        {
            if (itemData?.HealQuantity > 0)
            {
                List<Hero> characterList = itemData.ItemTarget switch
                {
                    Enums.ItemTarget.AllHero => MatchManager.Instance.GetTeamHero().ToList(),
                    _ => []
                };
                for (int i = 0; i < characterList.Count; i++)
                {
                    Character hero = characterList[i];
                    int heal = itemData?.HealQuantity ?? 0;
                    HandleOverhealTraits(ref hero, heal, "DoItemDataPrefix");

                }
                if (itemData.ItemTarget == Enums.ItemTarget.SelfEnemy || itemData.ItemTarget == Enums.ItemTarget.Self || itemData.ItemTarget == Enums.ItemTarget.CurrentTarget)
                {
                    int heal = itemData.HealQuantity;
                    HandleOverhealTraits(ref target, heal, "DoItemDataPrefix");

                }
            }
            if (itemData.HealPercentQuantity > 0)
            {
                if (itemData.ItemTarget == Enums.ItemTarget.AllHero)
                {
                    for (int i = 0; i < MatchManager.Instance.GetTeamHero().Count(); i++)
                    {
                        Character hero = MatchManager.Instance.GetTeamHero()[i];
                        // foreach (Character hero in MatchManager.Instance.GetTeamHero())
                        // {
                        int heal = Mathf.RoundToInt(itemData.HealPercentQuantity * target.GetMaxHP() * 0.01f);
                        HandleOverhealTraits(ref hero, heal, "DoItemDataPrefix");
                    }

                }

                if (itemData.ItemTarget == Enums.ItemTarget.SelfEnemy || itemData.ItemTarget == Enums.ItemTarget.Self || itemData.ItemTarget == Enums.ItemTarget.CurrentTarget)
                {
                    int heal = Mathf.RoundToInt(itemData.HealPercentQuantity * target.GetMaxHP() * 0.01f);
                    HandleOverhealTraits(ref target, heal, "DoItemDataPrefix");

                }
            }

            if (itemData.HealPercentQuantitySelf > 0)
            {
                int heal = Mathf.RoundToInt(itemData.HealPercentQuantitySelf * target.GetMaxHP() * 0.01f);
                HandleOverhealTraits(ref target, heal, "DoItemDataPrefix");

            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.HealAttacker))]
        public static void HealAttackerPostfix(Character __instance, Hero theCasterHero, NPC theCasterNPC)
        {
            if (__instance.IsHero && theCasterHero != null || theCasterHero != null && !theCasterHero.Alive || theCasterNPC != null && !theCasterNPC.Alive)
                return;
            for (int index1 = 0; index1 < __instance.AuraList.Count; ++index1)
            {
                if (__instance.AuraList[index1] == null)
                {
                    continue;
                }
                AuraCurseData acData = __instance.AuraList[index1].ACData;
                if ((UnityEngine.Object)acData != (UnityEngine.Object)null && acData.HealAttackerPerStack > 0)
                {
                    int heal = acData.HealAttackerPerStack * __instance.AuraList[index1].AuraCharges;
                    Character hero = theCasterHero;
                    HandleOverhealTraits(ref hero, heal, "HealAttackerPostfix");
                }

            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), "GlobalAuraCurseModificationByTraitsAndItems")]
        // [HarmonyPriority(Priority.Last)]
        public static void GlobalAuraCurseModificationByTraitsAndItemsPostfix(ref AtOManager __instance, ref AuraCurseData __result, string _type, string _acId, Character _characterCaster, Character _characterTarget)
        {
            // LogInfo($"GACM {subclassName}");

            Character characterOfInterest = _type == "set" ? _characterTarget : _characterCaster;
            string traitOfInterest;
            switch (_acId)
            {
                // trait4a:
                // Shield on Hero increases All Damage and Healing Done by 0.2 per charge

                case "shield":
                    traitOfInterest = trait0;
                    if (IfCharacterHas(characterOfInterest, CharacterHas.Trait, traitOfInterest, AppliesTo.ThisHero))
                    {
                        __result.AuraDamageType = Enums.DamageType.All;
                        float multiplierAmount = characterOfInterest.HaveTrait(trait4a) ? 0.3f : 0.2f;
                        __result.AuraDamageIncreasedPerStack = multiplierAmount;
                        __result.HealDoneTotal = Mathf.RoundToInt(multiplierAmount * characterOfInterest.GetAuraCharges("shield"));
                    }
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.ActivateItem))]
        public static void ActivateItem(
            Character __instance,
            Enums.EventActivation theEvent,
            Character target,
            int auxInt,
            string auxString)
        {
            string enchantId = "savantmindmaze";
            if (theEvent == Enums.EventActivation.CastCard && IfCharacterHas(__instance, CharacterHas.Enchantment, enchantId, AppliesTo.ThisHero))
            {
                string id = enchantId;
                if (__instance.HaveItem("savantmindmazea"))
                {
                    id = "savantmindmazea";
                }
                if (__instance.HaveItem("savantmindmazeb"))
                {
                    id = "savantmindmazeb";
                }
                CardData cardData = Globals.Instance.GetCardData(id, false);
                int timesActivated = 0;
                MatchManager.Instance.DoItem(__instance, theEvent, cardData, id, target, auxInt, auxString, timesActivated);
            }
        }





        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterItem), nameof(CharacterItem.CalculateDamagePrePostForThisCharacter))]
        public static void CalculateDamagePrePostForThisCharacterPrefix()
        {
            isDamagePreviewActive = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterItem), nameof(CharacterItem.CalculateDamagePrePostForThisCharacter))]
        public static void CalculateDamagePrePostForThisCharacterPostfix()
        {
            isDamagePreviewActive = false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MatchManager), nameof(MatchManager.SetDamagePreview))]
        public static void SetDamagePreviewPrefix()
        {
            isDamagePreviewActive = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MatchManager), nameof(MatchManager.SetDamagePreview))]
        public static void SetDamagePreviewPostfix()
        {
            isDamagePreviewActive = false;
        }

        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(Character), nameof(Character.BeginTurn))]
        // public static void BeginTurnPrefix(ref Character __instance)
        // {

        //     infiniteProctection = 0;
        // }

    }
}

