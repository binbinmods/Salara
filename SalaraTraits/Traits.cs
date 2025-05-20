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
                // Transform damage to Blunt
                // Start of Turn suffer 5 Chill
                string traitName = traitData.TraitName;
                string traitId = _trait;
                LogDebug($"Handling Trait {traitId}: {traitName}");
                _character.SetAuraTrait(_character, "chill", 5);
            }


            else if (_trait == trait2a)
            {
                // trait2a
                //Your Cold Spells are also Ranged Attacks. 
                // When you play a Cold Spell that costs Energy, refund 1 Energy and apply 1 Crack to a random enemy. (2 times/turn)                
                string traitName = traitData.TraitName;
                string traitId = _trait;

                if (CanIncrementTraitActivations(traitId) && _castedCard.HasCardType(Enums.CardType.Cold_Spell))// && MatchManager.Instance.energyJustWastedByHero > 0)
                {
                    LogDebug($"Handling Trait {traitId}: {traitName}");
                    _character.ModifyEnergy(1);
                    Character randNPC = GetRandomCharacter(teamNpc);
                    randNPC.SetAuraTrait(_character, "crack", 1);
                    IncrementTraitActivations(traitId);
                }
            }



            else if (_trait == trait2b)
            {
                // trait 2b:  
                // Your Cold Spells are also Defenses. 
                // When you play a Cold Spell that costs Energy, refund 1 Energy and apply 1 Fortify to the hero with the highest Block. (2 times/turn)
                string traitName = traitData.TraitName;
                string traitId = _trait;

                if (CanIncrementTraitActivations(traitId) && _castedCard.HasCardType(Enums.CardType.Cold_Spell))// && MatchManager.Instance.energyJustWastedByHero > 0)
                {
                    LogDebug($"Handling Trait {traitId}: {traitName}");
                    List<Character> highestBlockHeroes = [];
                    Character highestBlockHero = null;
                    int curBlock = 0;
                    foreach (Character hero in teamHero)
                    {
                        if (IsLivingHero(hero) && hero.GetAuraCharges("block") > curBlock)
                        {
                            curBlock = hero.GetAuraCharges("block");
                            highestBlockHeroes = [hero];
                        }
                        else if (IsLivingHero(hero) && hero.GetAuraCharges("block") == curBlock)
                        {
                            highestBlockHeroes.Add(hero);
                        }
                    }
                    if (highestBlockHeroes == null)
                    {
                        return;
                    }
                    else
                    {
                        highestBlockHero = GetRandomCharacter(highestBlockHeroes.ToArray());
                    }
                    _character?.ModifyEnergy(1);
                    highestBlockHero?.SetAuraTrait(_character, "fortify", 1);
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
        [HarmonyPatch(typeof(Character), "GetItemModifiedDamageType")]
        public static void GetItemModifiedDamageTypePostfix(
            ref Character __instance,
            ref Enums.DamageType __result)
        {
            if (__instance.HaveTrait(trait0) && __result == Enums.DamageType.None)
            {
                __result = Enums.DamageType.Blunt;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MatchManager), "CastCard")]
        public static void CastCardPrefix(
            MatchManager __instance,
            ref CardData _card,
            ref CardItem theCardItem,
            bool _automatic = false,
            int _energy = -1,
            int _posInTable = -1,
            bool _propagate = true)
        {
            Hero currentHero = __instance.GetHeroHeroActive();
            LogDebug("CastCardPrefix");
            if (theCardItem == null)
            {
                LogDebug("CastCardPrefix - Null Card");
                return;
            }
            if (currentHero == null)
            {
                LogDebug("CastCardPrefix - Null Hero ");
                return;
            }
            if (theCardItem.CardData.HasCardType(Enums.CardType.Cold_Spell) && currentHero.HaveTrait(trait2a))
            {
                LogDebug($"{trait2a} - Adding Ranged Attack card type to {theCardItem.CardData.Id}");
                List<Enums.CardType> types = [.. theCardItem.CardData.CardTypeAux];
                types.Add(Enums.CardType.Ranged_Attack);
                theCardItem.CardData.CardTypeAux = [.. types];
            }
            if (theCardItem.CardData.HasCardType(Enums.CardType.Cold_Spell) && currentHero.HaveTrait(trait2b))
            {
                LogDebug($"{trait2b} - Adding Defense card type to {theCardItem.CardData.Id}");
                List<Enums.CardType> types = [.. theCardItem.CardData.CardTypeAux];
                types.Add(Enums.CardType.Defense);
                theCardItem.CardData.CardTypeAux = [.. types];
            }
            else
            {
                LogDebug("CastCardPrefix - Not Cold Spell");
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardData), "HasCardType")]
        public static void HasCardTypePostfix(
            CardData __instance,
            ref bool __result,
            Enums.CardType type)
        {
            if (!EventManager.Instance || !AtOManager.Instance || __instance == null)
            {
                return;
            }

            EventReplyData replySelected = Traverse.Create(EventManager.Instance).Field("replySelected").GetValue<EventReplyData>();
            if (replySelected == null || replySelected.SsRollCard == Enums.CardType.None)
            {
                return;
            }

            LogDebug($"HasCardTypePostfix - Checking for Card type {type} in event for card {__instance.CardName}.");
            int chosenInd = -1;

            for (int index = 0; index < 4; ++index)
            {
                bool[] charRoll = Traverse.Create(EventManager.Instance).Field("charRoll").GetValue<bool[]>();
                if (charRoll[index])
                {
                    chosenInd = index;
                }
            }

            Hero hero = AtOManager.Instance.GetTeam()[chosenInd];
            if (hero.HaveTrait(trait2a) && (type == Enums.CardType.Ranged_Attack || type == Enums.CardType.Attack) && __instance.HasCardType(Enums.CardType.Cold_Spell))
            {
                LogDebug($"HasCardTypePostfix - Adding Ranged Attack.");
                __result = true;
            }

            if (hero.HaveTrait(trait2b) && type == Enums.CardType.Defense && __instance.HasCardType(Enums.CardType.Cold_Spell))
            {
                LogDebug($"HasCardTypePostfix - Adding Defense.");
                __result = true;
            }


        }





        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(CharacterItem), nameof(CharacterItem.CalculateDamagePrePostForThisCharacter))]
        // public static void CalculateDamagePrePostForThisCharacterPrefix()
        // {
        //     isDamagePreviewActive = true;
        // }
        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(CharacterItem), nameof(CharacterItem.CalculateDamagePrePostForThisCharacter))]
        // public static void CalculateDamagePrePostForThisCharacterPostfix()
        // {
        //     isDamagePreviewActive = false;
        // }


        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(MatchManager), nameof(MatchManager.SetDamagePreview))]
        // public static void SetDamagePreviewPrefix()
        // {
        //     isDamagePreviewActive = true;
        // }
        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(MatchManager), nameof(MatchManager.SetDamagePreview))]
        // public static void SetDamagePreviewPostfix()
        // {
        //     isDamagePreviewActive = false;
        // }

        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(Character), nameof(Character.BeginTurn))]
        // public static void BeginTurnPrefix(ref Character __instance)
        // {

        //     infiniteProctection = 0;
        // }

    }
}

