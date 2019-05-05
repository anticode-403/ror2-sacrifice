using BepInEx;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using BepInEx.Configuration;

namespace Sacrifice
{
  [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
  [BepInPlugin("com.anticode.Sacrifice", "Sacrifice", "1.0.0")]
  public class Sacrifice : BaseUnityPlugin
  {
    public static ConfigWrapper<int> BaseDropChance;
    public static ConfigWrapper<bool> CloverRerollDrops;
    public static ConfigWrapper<bool> BanChests;
    public static ConfigWrapper<bool> BanBarrels;
    public static ConfigWrapper<bool> BanEquipmentBarrels;
    public static ConfigWrapper<bool> BanChanceShrine;
    public static ConfigWrapper<bool> BanBloodShrine;
    public static ConfigWrapper<bool> BanTripleShops;
    public static ConfigWrapper<bool> BanOtherShrines;
    public static ConfigWrapper<bool> BanLunarChests;
    public static ConfigWrapper<bool> ReduceBarrelSpawns;
    public Sacrifice()
    {
      // Fuck me, this is a long list of configs.
      BaseDropChance = Config.Wrap(
        "Chances",
        "Base Drop Chance",
        "The base percent chance of an item dropping.",
        7);
      CloverRerollDrops = Config.Wrap(
        "Other",
        "Clovers Reroll Drops",
        "Can clovers reroll the chance of an item dropping.",
        true);
      ReduceBarrelSpawns = Config.Wrap(
        "Other",
        "Reduce Barrel Spawns",
        "Reduce the spawn rate of barrels.",
        true);
      BanChests = Config.Wrap(
        "Bans",
        "Chests",
        "Ban chests that aren't Lunar Chests.",
        true);
      BanLunarChests = Config.Wrap(
        "Bans",
        "Lunar Chests",
        "Ban lunar chests",
        true);
      BanTripleShops = Config.Wrap(
        "Bans",
        "Triple Shop",
        "Ban triple shops",
        true);
      BanEquipmentBarrels = Config.Wrap(
        "Bans",
        "Equipment Barrels",
        "Ban equipment barrels",
        true);
      BanBarrels = Config.Wrap(
        "Bans",
        "Barrels",
        "Ban any barrels that aren't Equipment Barrels",
        false);
      BanBloodShrine = Config.Wrap(
        "Bans",
        "Blood Shrine",
        "Ban blood shrines",
        true);
      BanChanceShrine = Config.Wrap(
        "Bans",
        "Chance Shrine",
        "Ban chance shrines",
        true);
      BanOtherShrines = Config.Wrap(
        "Bans",
        "Other Shrines",
        "Ban any shrines that aren't blood shrines or chance shrines.",
        false);
    }

    public void Awake()
    {
      // Give players and their allies the chance to drop items.
      On.RoR2.CharacterMaster.Init += (orig) =>
      {
        GlobalEventManager.onCharacterDeathGlobal += (damageReport) =>
        {
          CharacterBody attackerBody = damageReport.damageInfo.attacker.GetComponent<CharacterBody>();
          GameObject masterObject = attackerBody.masterObject;
          if (masterObject == null || attackerBody.teamComponent.teamIndex == TeamIndex.Player) return;
          RollSpawnChance(damageReport, masterObject);
        };
        orig();
      };
      // Remove banned items from cards. This replaces the default card selection behavior.
      On.RoR2.ClassicStageInfo.GenerateDirectorCardWeightedSelection += (orig, instance, categorySelection) =>
      {
        WeightedSelection<DirectorCard> weightedSelection = new WeightedSelection<DirectorCard>(8);
        foreach (DirectorCardCategorySelection.Category category in categorySelection.categories)
        {
          float num = categorySelection.SumAllWeightsInCategory(category);
          foreach (DirectorCard directorCard in category.cards)
          {
            if (IsBanned(directorCard)) continue;
            if (CardIsBarrel(directorCard) && ReduceBarrelSpawns.Value)
            {
              directorCard.selectionWeight /= 2;
            }
            weightedSelection.AddChoice(directorCard, directorCard.selectionWeight / num * category.selectionWeight);
          }
        }
        return weightedSelection;
      };
    }

    private static bool IsBanned (DirectorCard card)
    {
      bool chest = BanChests.Value ? CardIsChest(card) : false;
      bool equipBarrel = BanEquipmentBarrels.Value ? CardIsEquipmentBarrel(card) : false;
      bool chanceShrine = BanChanceShrine.Value ? CardIsChanceShrine(card) : false;
      bool bloodShrine = BanBloodShrine.Value ? CardIsBloodShrine(card) : false;
      bool tripleShop = BanTripleShops.Value ? CardIsTripleShop(card) : false;
      bool barrel = BanBarrels.Value ? CardIsBarrel(card) : false;
      bool otherShrine = BanOtherShrines.Value ? CardIsOtherShrine(card) : false;
      bool lunarChest = BanLunarChests.Value ? CardIsLunarChest(card) : false;
      return chest || equipBarrel || chanceShrine || bloodShrine || tripleShop || barrel || otherShrine || lunarChest;
    }

    private static bool CardIsLunarChest(DirectorCard card)
    {
      string name = card.spawnCard.prefab.name;
      return name == "LunarChest";
    }

    private static bool CardIsChest(DirectorCard card)
    {
      string name = card.spawnCard.prefab.name;
      return name.Contains("Chest") && name != "LunarChest";
    }

    private static bool CardIsEquipmentBarrel(DirectorCard card)
    {
      string name = card.spawnCard.prefab.name;
      return name == "EquipmentBarrel";
    }

    private static bool CardIsChanceShrine(DirectorCard card)
    {
      string name = card.spawnCard.prefab.name;
      return name.Contains("Chance");
    }

    private static bool CardIsBloodShrine(DirectorCard card)
    {
      string name = card.spawnCard.prefab.name;
      return name.Contains("Blood");
    }

    private static bool CardIsTripleShop(DirectorCard card)
    {
      string name = card.spawnCard.prefab.name;
      return name.Contains("TripleShop");
    }

    private static bool CardIsBarrel(DirectorCard card)
    {
      string name = card.spawnCard.prefab.name;
      return name.Contains("Barrel") && name != "Equipment";
    }

    private static bool CardIsOtherShrine(DirectorCard card)
    {
      string name = card.spawnCard.prefab.name;
      return name.Contains("Shrine") && !name.Contains("Blood") && !name.Contains("Chance");
    }

    private void RollSpawnChance(DamageReport damageReport, GameObject masterObject)
    {
      float percentChance = (BaseDropChance.Value / 100) * (1f + (Run.instance.participatingPlayerCount - 1f) * 0.3f);
      percentChance = percentChance < 0.2f ? percentChance : 0.2f;
      WeightedSelection<List<PickupIndex>> weightedSelection = new WeightedSelection<List<PickupIndex>>(8);
      if (damageReport.victimBody.isElite)
      {
        weightedSelection.AddChoice(Run.instance.availableLunarDropList, 0.1f);
        weightedSelection.AddChoice(Run.instance.availableTier2DropList, 0.4f);
        weightedSelection.AddChoice(Run.instance.availableTier3DropList, 0.2f);
      }
      if (damageReport.victimBody.isBoss)
      {
        weightedSelection.AddChoice(Run.instance.availableTier2DropList, 0.7f);
        weightedSelection.AddChoice(Run.instance.availableTier3DropList, 0.3f);
      }
      else if (!damageReport.victimBody.isElite)
      {
        weightedSelection.AddChoice(Run.instance.availableEquipmentDropList, 0.05f);
        weightedSelection.AddChoice(Run.instance.availableTier1DropList, 0.8f);
        weightedSelection.AddChoice(Run.instance.availableTier2DropList, 0.2f);
        weightedSelection.AddChoice(Run.instance.availableTier3DropList, 0.01f);
      }
      List<PickupIndex> list = weightedSelection.Evaluate(Run.instance.spawnRng.nextNormalizedFloat);
      PickupIndex pickupIndex = list[Run.instance.spawnRng.RangeInt(0, list.Count)];
      PlayerCharacterMasterController component = masterObject.GetComponent<PlayerCharacterMasterController>();
      if (component && Util.CheckRoll(percentChance, (component.master && CloverRerollDrops.Value) ? component.master.luck : 0f, null))
      {
        PickupDropletController.CreatePickupDroplet(
          pickupIndex,
          damageReport.victim.transform.position,
          new Vector3(Random.Range(-3.0f, 3.0f), 15f, Random.Range(-3.0f, 3.0f)));
      }
    }
  }
}
