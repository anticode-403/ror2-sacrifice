using BepInEx;
using BepInEx.Configuration;
using RoR2;
using R2API.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Sacrifice
{
  [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
  [BepInPlugin("com.anticode.Sacrifice", "Sacrifice", "1.4.0")]
  public class Sacrifice : BaseUnityPlugin
  {
    public static ConfigWrapper<float> BaseDropChance;
    private static float baseDropChance;
    public static ConfigWrapper<bool> CloverRerollDrops;
    private static bool cloverRerollDrops;
    public static ConfigWrapper<float> PlayerScaling;
    private static float playerScaling;
    public static ConfigWrapper<bool> ReverseScaling;
    private static bool reverseScaling;
    public static ConfigWrapper<float> ReverseScalingRate;
    private static float reverseScalingRate;
    public static ConfigWrapper<float> InteractableSpawnMultiplier;
    private static float interactableSpawnMultiplier;
    public static ConfigWrapper<float> InteractableCostMultiplier;
    private static float interactableCostMultiplier;
    public static ConfigWrapper<bool> DropChestsInstead;
    private static bool dropChestsInstead;

    private static DirectorCard deathChestSpawnCard;

    public class DropWeights
    {
      public float LunarDropWeight;
      public float Tier1DropWeight;
      public float Tier2DropWeight;
      public float Tier3DropWeight;
      public float EquipmentDropWeight;

      public DropWeights(ConfigWrapper<float> lunar, ConfigWrapper<float> tier1, ConfigWrapper<float> tier2, ConfigWrapper<float> tier3, ConfigWrapper<float> equipment)
      {
        LunarDropWeight = lunar.Value;
        Tier1DropWeight = tier1.Value;
        Tier2DropWeight = tier2.Value;
        Tier3DropWeight = tier3.Value;
        EquipmentDropWeight = equipment.Value;
      }
    }

    private static DropWeights eliteDropWeights;
    private static DropWeights bossDropWeights;
    private static DropWeights normalDropWeights;

    public class InteractableConfig
    {
      public string Name;
      public float SpawnWeightModifier;

      public InteractableConfig(string name, float spawnWeightModifier)
      {
        Name = name;
        SpawnWeightModifier = spawnWeightModifier;
      }
    }
    private static List<InteractableConfig> interactables;
    private string[] interactablesCategories;
    public Sacrifice()
    {
      // Fuck me, this is a long list of configs.
      BaseDropChance = Config.Wrap(
        "Chances",
        "BaseDropChance",
        "The base percent chance of an item dropping.",
        1.0f);
      PlayerScaling = Config.Wrap(
        "Chances",
        "PlayerScaling",
        "The multiplier applied to the base drop chance per player. 0 to disable.",
        0.3f);
      ReverseScaling = Config.Wrap(
        "Chances",
        "ReverseScaling",
        "Scale back drop chances. Note that reverse scaling can quickly backfire. You should find yourself adjusting ReverseScalingRate a lot.",
        false);
      ReverseScalingRate = Config.Wrap(
        "Chances",
        "ReverseScalingRate",
        "Rate at which to scale back drop chances in relation to time played.",
        0.1f);
      eliteDropWeights = new DropWeights(
        Config.Wrap(
          "Chances.Elite",
          "Lunar",
          "The weight of a lunar item dropping (in comparison to other item catagories) on elite kills.",
          0.05f
        ),
        Config.Wrap(
          "Chances.Elite",
          "Tier1",
          "The weight of a white item dropping (in comparison to other item catagories) on elite kills.",
          0.0f
        ),
        Config.Wrap(
          "Chances.Elite",
          "Tier2",
          "The weight of a green item dropping (in comparison to other item catagories) on elite kills.",
          0.3f
        ),
        Config.Wrap(
          "Chances.Elite",
          "Tier3",
          "The weight of a red item dropping (in comparison to other item catagories) on elite kills.",
          0.1f
        ),
        Config.Wrap(
          "Chances.Elite",
          "Equipment",
          "The weight of an equipment item dropping (in comparison to other item catagories) on elite kills.",
          0.0f
        )
      );
      bossDropWeights = new DropWeights(
        Config.Wrap(
          "Chances.Boss",
          "Lunar",
          "The weight of a lunar item dropping (in comparison to other item catagories) on boss kills.",
          0.0f
        ),
        Config.Wrap(
          "Chances.Boss",
          "Tier1",
          "The weight of a white item dropping (in comparison to other item catagories) on boss kills.",
          0.05f
        ),
        Config.Wrap(
          "Chances.Boss",
          "Tier2",
          "The weight of a green item dropping (in comparison to other item catagories) on boss kills.",
          0.6f
        ),
        Config.Wrap(
          "Chances.Boss",
          "Tier3",
          "The weight of a red item dropping (in comparison to other item catagories) on boss kills.",
          0.3f
        ),
        Config.Wrap(
          "Chances.Boss",
          "Equipment",
          "The weight of an equipment item dropping (in comparison to other item catagories) on boss kills.",
          0.0f
        )
      );
      normalDropWeights = new DropWeights(
        Config.Wrap(
          "Chances.Normal",
          "Lunar",
          "The weight of a lunar item dropping (in comparison to other item catagories) on kills.",
          0.0f
        ),
        Config.Wrap(
          "Chances.Normal",
          "Tier1",
          "The weight of a white item dropping (in comparison to other item catagories) on kills.",
          0.8f
        ),
        Config.Wrap(
          "Chances.Normal",
          "Tier2",
          "The weight of a green item dropping (in comparison to other item catagories) on kills.",
          0.2f
        ),
        Config.Wrap(
          "Chances.Normal",
          "Tier3",
          "The weight of a red item dropping (in comparison to other item catagories) on kills.",
          0.01f
        ),
        Config.Wrap(
          "Chances.Normal",
          "Equipment",
          "The weight of an equipment item dropping (in comparison to other item catagories) on kills.",
          0.05f
        )
      );
      CloverRerollDrops = Config.Wrap(
        "Other",
        "CloversRerollDrops",
        "Can clovers reroll the chance of an item dropping.",
        true);
      DropChestsInstead = Config.Wrap(
        "Other",
        "DropChestsInstead",
        "Instead of dropping items, enemies drop chests.",
        false);
      InteractableSpawnMultiplier = Config.Wrap(
        "Interactables",
        "InteractableSpawnMultiplier",
        "A multiplier on the amount of interactables that will spawn in a level.",
        1.0f);
      InteractableCostMultiplier = Config.Wrap(
        "Interactables",
        "InteractableCostMultiplier",
        "A multiplier applied to the cost of all interactables.",
        1.0f);
      ConfigWrapper<float> Chest1 = Config.Wrap(
        "Interactables.Chances",
        "Chest",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> Chest2 = Config.Wrap(
        "Interactables.Chances",
        "Chest2",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> CategoryChestDamage = Config.Wrap(
        "Interactables.Chances",
        "CategoryChestDamage",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> CategoryChestHealing = Config.Wrap(
        "Interactables.Chances",
        "CategoryChestHealing",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> CategoryChestUtility = Config.Wrap(
        "Interactables.Chances",
        "CategoryChestUtility",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> EquipmentBarrel = Config.Wrap(
        "Interactables.Chances",
        "EquipmentBarrel",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> TripleShop = Config.Wrap(
        "Interactables.Chances",
        "TripleShop",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> TripleShopLarge = Config.Wrap(
        "Interactables.Chances",
        "TripleShopLarge",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> GoldChest = Config.Wrap(
        "Interactables.Chances",
        "GoldChest",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> LunarChest = Config.Wrap(
        "Interactables.Chances",
        "LunarChest",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> Barrel1 = Config.Wrap(
        "Interactables.Chances",
        "Barrel1",
        "The multiplier for this item to spawn.",
        0.5f);
      ConfigWrapper<float> ShrineHealing = Config.Wrap(
        "Interactables.Chances",
        "ShrineHealing",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> ShrineBlood = Config.Wrap(
        "Interactables.Chances",
        "ShrineBlood",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> ShrineBoss = Config.Wrap(
        "Interactables.Chances",
        "ShrineBoss",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> ShrineChance = Config.Wrap(
        "Interactables.Chances",
        "ShrineChance",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> ShrineCombat = Config.Wrap(
        "Interactables.Chances",
        "ShrineCombat",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> ShrineRestack = Config.Wrap(
        "Interactables.Chances",
        "ShrineRestack",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> Drone1Broken = Config.Wrap(
        "Interactables.Chances",
        "BrokenDrone1",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> Drone2Broken = Config.Wrap(
        "Interactables.Chances",
        "BrokenDrone2",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> MegaDroneBroken = Config.Wrap(
        "Interactables.Chances",
        "BrokenMegaDrone",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> MissileDroneBroken = Config.Wrap(
        "Interactables.Chances",
        "BrokenMissileDrone",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> EquipmentDroneBroken = Config.Wrap(
        "Interactables.Chances",
        "EquipmentDroneBroken",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> FlameDroneBroken = Config.Wrap(
        "Interactables.Chances",
        "FlameDroneBroken",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> Turret1Broken = Config.Wrap(
        "Interactables.Chances",
        "BrokenTurret1",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> Chest1Stealthed = Config.Wrap(
        "Interactables.Chances",
        "Chest1Stealthed",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> RadarTower = Config.Wrap(
        "Interactables.Chances",
        "RadarTower",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> ShrineGoldshoresAccess = Config.Wrap(
        "Interactables.Chances",
        "ShrineGoldshoresAccess",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> Duplicator = Config.Wrap(
        "Interactables.Chances",
        "Duplicator",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> DuplicatorLarge = Config.Wrap(
        "Interactables.Chances",
        "DuplicatorLarge",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> DuplicatorMilitary = Config.Wrap(
        "Interactables.Chances",
        "DuplicatorMilitary",
        "The multiplier for this item to spawn.",
        1.0f);

      baseDropChance = BaseDropChance.Value;
      playerScaling = PlayerScaling.Value;
      reverseScaling = ReverseScaling.Value;
      reverseScalingRate = ReverseScalingRate.Value;
      cloverRerollDrops = CloverRerollDrops.Value;
      interactableSpawnMultiplier = InteractableSpawnMultiplier.Value;
      interactableCostMultiplier = InteractableCostMultiplier.Value;
      dropChestsInstead = DropChestsInstead.Value;
      interactables = new List<InteractableConfig>
      {
        new InteractableConfig("Chest1", Chest1.Value),
        new InteractableConfig("Chest2", Chest2.Value),
        new InteractableConfig("GoldChest", GoldChest.Value),
        new InteractableConfig("LunarChest", LunarChest.Value),
        new InteractableConfig("Barrel1", Barrel1.Value),
        new InteractableConfig("EquipmentBarrel", EquipmentBarrel.Value),
        new InteractableConfig("Duplicator", Duplicator.Value),
        new InteractableConfig("DuplicatorLarge", DuplicatorLarge.Value),
        new InteractableConfig("DuplicatorMilitary", DuplicatorMilitary.Value),
        new InteractableConfig("ShrineGoldshoresAccess", ShrineGoldshoresAccess.Value),
        new InteractableConfig("RadarTower", RadarTower.Value),
        new InteractableConfig("Chest1Stealthed", Chest1Stealthed.Value),
        new InteractableConfig("Drone1Broken", Drone1Broken.Value),
        new InteractableConfig("Drone2Broken", Drone2Broken.Value),
        new InteractableConfig("MegaDroneBroken", MegaDroneBroken.Value),
        new InteractableConfig("MissileDroneBroken", MissileDroneBroken.Value),
        new InteractableConfig("EquipmentDroneBroken", EquipmentDroneBroken.Value),
        new InteractableConfig("FlameDroneBroken", FlameDroneBroken.Value),
        new InteractableConfig("Turret1Broken", Turret1Broken.Value),
        new InteractableConfig("ShrineBlood", ShrineBlood.Value),
        new InteractableConfig("ShrineBoss", ShrineBoss.Value),
        new InteractableConfig("ShrineCombat", ShrineCombat.Value),
        new InteractableConfig("ShrineChance", ShrineChance.Value),
        new InteractableConfig("ShrineRestack", ShrineRestack.Value),
        new InteractableConfig("ShrineHealing", ShrineHealing.Value),
        new InteractableConfig("TripleShop", TripleShop.Value),
        new InteractableConfig("TripleShopLarge", TripleShopLarge.Value),
        new InteractableConfig("CategoryChestDamage", CategoryChestDamage.Value),
        new InteractableConfig("CategoryChestHealing", CategoryChestHealing.Value),
        new InteractableConfig("CategoryChestUtility", CategoryChestUtility.Value),
      };
      interactablesCategories = new string[]
      {
        "Chests",
        "Barrels",
        "Shrines",
        "Drones",
        "Misc",
        "Rare",
        "Duplicator"
      };
    }

    public void Awake()
    {
      // Give player allies the chance to drop items.
      On.RoR2.DeathRewards.OnKilledServer += (orig, self, damageReport) =>
      {
        if (damageReport == null || damageReport.damageInfo.attacker == null) return;
        CharacterBody victimBody = self.GetFieldValue<CharacterBody>("characterBody");
        //CharacterBody victimBody = (CharacterBody)GetInstanceField(typeof(DeathRewards), self, "characterBody");
        CharacterBody attackerBody = damageReport.damageInfo.attacker.GetComponent<CharacterBody>();
        GameObject attackerMaster = attackerBody.masterObject;
        if (attackerMaster == null || victimBody.teamComponent.teamIndex != TeamIndex.Monster) return;
        RollSpawnChance(victimBody, attackerBody);
        orig(self, damageReport);
      };
      // Remove banned items from cards. This replaces the default card selection behavior.
      On.RoR2.ClassicStageInfo.GenerateDirectorCardWeightedSelection += (orig, instance, categorySelection) =>
      {
        // categorySelection is either interactableCategories or monsterCategories and we only want to modify the former
        if (!IsInteractableCategorySelection(categorySelection)) return orig(instance, categorySelection);
        WeightedSelection<DirectorCard> weightedSelection = new WeightedSelection<DirectorCard>(8);
        foreach (DirectorCardCategorySelection.Category category in categorySelection.categories)
        {
          float num = categorySelection.SumAllWeightsInCategory(category);
          foreach (DirectorCard directorCard in category.cards)
          {
            if (directorCard.spawnCard.prefab.name == "Chest1") deathChestSpawnCard = directorCard;
            if (!ApplyConfigModifiers(directorCard)) continue;
            directorCard.spawnCard.directorCreditCost = Mathf.RoundToInt(directorCard.cost * interactableCostMultiplier);
            weightedSelection.AddChoice(directorCard, directorCard.selectionWeight / num * category.selectionWeight);
          }
        }
        return weightedSelection;
      };
      On.RoR2.SceneDirector.PopulateScene += (orig, self) =>
      {
        int interactableCredit = self.GetFieldValue<int>("interactableCredit");
        interactableCredit = Mathf.RoundToInt(interactableCredit * interactableSpawnMultiplier);
        self.SetFieldValue("interactableCredit", interactableCredit);
        orig(self);
      };
    }

    private void RollSpawnChance(CharacterBody victimBody, CharacterBody attackerBody)
    {
      // Roll percent chance has a base value of 7% (configurable), multiplied by 1 + .3 per player above 1.
      float percentChance = baseDropChance * (1f + ((NetworkUser.readOnlyInstancesList.Count - 1f) * playerScaling));
      if (reverseScaling) percentChance /= Run.instance.difficultyCoefficient * reverseScalingRate;
      WeightedSelection<List<PickupIndex>> weightedSelection = new WeightedSelection<List<PickupIndex>>(5);
      // This is done this way because elite bosses are possible, and should have the option to drop reds than their standard boss counterparts.
      if (victimBody.isElite) AddDropWeights(weightedSelection, eliteDropWeights);
      if (victimBody.isBoss) AddDropWeights(weightedSelection, bossDropWeights);
      // If the enemy in question is dead, then chances shoulder be the default for chests + some equipment item drop chances.
      else if (!victimBody.isElite) AddDropWeights(weightedSelection, normalDropWeights);
      // Item to drop is generated before the item pick up is generated for a future update.
      List<PickupIndex> list = weightedSelection.Evaluate(Run.instance.spawnRng.nextNormalizedFloat);
      PickupIndex pickupIndex = list[Run.instance.spawnRng.RangeInt(0, list.Count)];
      CharacterMaster master = attackerBody.master;
      float luck = (master && cloverRerollDrops) ? master.luck : 0f;
      if (Util.CheckRoll(percentChance, luck, null))
      {
        if (!dropChestsInstead)
        {
          // Drop an item.
          PickupDropletController.CreatePickupDroplet(
            pickupIndex,
            victimBody.transform.position,
            new Vector3(Random.Range(-5.0f, 5.0f), 20f, Random.Range(-5.0f, 5.0f)));
        } else
        {
          // I wasn't able to find any way to copy DirectorCards, so this is the best way to get 0 cost cards.
          GameObject spawnedChest = deathChestSpawnCard.spawnCard.DoSpawn(
            victimBody.transform.position,
            victimBody.transform.rotation,
            null);
          ChestBehavior chestBehavior = spawnedChest.GetComponent<ChestBehavior>();
          PurchaseInteraction purchaseInteraction = spawnedChest.GetComponent<PurchaseInteraction>();
          purchaseInteraction.automaticallyScaleCostWithDifficulty = false;
          purchaseInteraction.cost = 0;
          purchaseInteraction.costType = CostTypeIndex.None;
          // For some ungodly reason, ChestBehavior doesn't let you specify the drop chance for equipment items.
          // So we have to do this to keep full functionality.
          if (Run.instance.availableEquipmentDropList.Contains(pickupIndex))
          {
            chestBehavior.RollEquipment();
          } else
          {
            // This, despite how unbelievably ugly it looks, forced the item that the chest rolled to be whatever WE rolled.
            // This way, the drop weights still work as intended (even though rolling is done twice here).
            chestBehavior.lunarChance = Run.instance.availableLunarDropList.Contains(pickupIndex) ? 100f : 0f;
            chestBehavior.tier1Chance = Run.instance.availableTier1DropList.Contains(pickupIndex) ? 100f : 0f;
            chestBehavior.tier2Chance = Run.instance.availableTier2DropList.Contains(pickupIndex) ? 100f : 0f;
            chestBehavior.tier3Chance = Run.instance.availableTier3DropList.Contains(pickupIndex) ? 100f : 0f;
            chestBehavior.RollItem();
          }
        }
      }
    }

    private static void AddDropWeights(WeightedSelection<List<PickupIndex>> weightedSelection, DropWeights dropWeights)
    {
      weightedSelection.AddChoice(Run.instance.availableLunarDropList, dropWeights.LunarDropWeight);
      weightedSelection.AddChoice(Run.instance.availableTier1DropList, dropWeights.Tier1DropWeight);
      weightedSelection.AddChoice(Run.instance.availableTier2DropList, dropWeights.Tier2DropWeight);
      weightedSelection.AddChoice(Run.instance.availableTier3DropList, dropWeights.Tier3DropWeight);
      weightedSelection.AddChoice(Run.instance.availableEquipmentDropList, dropWeights.EquipmentDropWeight);
    }

    private static bool ApplyConfigModifiers(DirectorCard card)
    {
      foreach (InteractableConfig interactableConfig in interactables)
      {
        if (card.spawnCard.prefab.name != interactableConfig.Name) continue;
        if (interactableConfig.SpawnWeightModifier <= 0.0f) return false;
        card.selectionWeight = Mathf.RoundToInt(card.selectionWeight * interactableConfig.SpawnWeightModifier);
        break;
      }
      return true;
    }

    private bool IsInteractableCategorySelection(DirectorCardCategorySelection categorySelection)
    {
      // categorySelection either contains only interactable or monster category. So it's enough to check the first category
      foreach (DirectorCardCategorySelection.Category category in categorySelection.categories)
      {
        foreach(string interactableCategory in interactablesCategories)
        {
          if(category.name.Equals(interactableCategory))
          {
            return true;
          }
        }
      }
      return false;
    }
  }
}
