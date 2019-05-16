using BepInEx;
using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
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
    public static ConfigWrapper<float> InteractableSpawnMultiplier;
    private static float interactableSpawnMultiplier;
    public static ConfigWrapper<float> InteractableCostMultiplier;
    private static float interactableCostMultiplier;

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
    public Sacrifice()
    {
      // Fuck me, this is a long list of configs.
      BaseDropChance = Config.Wrap(
        "Chances",
        "BaseDropChance",
        "The base percent chance of an item dropping.",
        1.0f);
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
              "Chances.Boss",
              "Lunar",
              "The weight of a lunar item dropping (in comparison to other item catagories) on kills.",
              0.0f
            ),
          Config.Wrap(
              "Chances.Boss",
              "Tier1",
              "The weight of a white item dropping (in comparison to other item catagories) on kills.",
              0.8f
            ),
          Config.Wrap(
              "Chances.Boss",
              "Tier2",
              "The weight of a green item dropping (in comparison to other item catagories) on kills.",
              0.2f
            ),
          Config.Wrap(
              "Chances.Boss",
              "Tier3",
              "The weight of a red item dropping (in comparison to other item catagories) on kills.",
              0.01f
            ),
          Config.Wrap(
              "Chances.Boss",
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
      ConfigWrapper<float> Chest = Config.Wrap(
        "Interactables.Chances",
        "Chest",
        "The multiplier for this item to spawn.",
        0.0f);
      ConfigWrapper<float> Chest2 = Config.Wrap(
        "Interactables.Chances",
        "Chest2",
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
      ConfigWrapper<float> BrokenDrone1 = Config.Wrap(
        "Interactables.Chances",
        "BrokenDrone1",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> BrokenDrone2 = Config.Wrap(
        "Interactables.Chances",
        "BrokenDrone2",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> BrokenMegaDrone = Config.Wrap(
        "Interactables.Chances",
        "BrokenMegaDrone",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> BrokenMissileDrone = Config.Wrap(
        "Interactables.Chances",
        "BrokenMissileDrone",
        "The multiplier for this item to spawn.",
        1.0f);
      ConfigWrapper<float> BrokenTurret1 = Config.Wrap(
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
      cloverRerollDrops = CloverRerollDrops.Value;
      interactableSpawnMultiplier = InteractableSpawnMultiplier.Value;
      interactableCostMultiplier = InteractableSpawnMultiplier.Value;
      interactables = new List<InteractableConfig>
      {
        new InteractableConfig("Chest", Chest.Value),
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
        new InteractableConfig("BrokenDrone1", BrokenDrone1.Value),
        new InteractableConfig("BrokenDrone2", BrokenDrone2.Value),
        new InteractableConfig("BrokenMegaDrone", BrokenMegaDrone.Value),
        new InteractableConfig("BrokenMissileDrone", BrokenMissileDrone.Value),
        new InteractableConfig("BrokenTurret1", BrokenTurret1.Value),
        new InteractableConfig("ShrineBlood", ShrineBlood.Value),
        new InteractableConfig("ShrineBoss", ShrineBoss.Value),
        new InteractableConfig("ShrineCombat", ShrineCombat.Value),
        new InteractableConfig("ShrineChance", ShrineChance.Value),
        new InteractableConfig("ShrineRestack", ShrineRestack.Value),
        new InteractableConfig("ShrineHealing", ShrineHealing.Value),
        new InteractableConfig("TripleShop", TripleShop.Value),
        new InteractableConfig("TripleShopLarge", TripleShopLarge.Value),
      };
    }

    public void Awake()
    {
      // Give player allies the chance to drop items.
      On.RoR2.DeathRewards.OnKilled += (orig, self, damageInfo) =>
      {
        if (damageInfo == null) return;
        CharacterBody victimBody = (CharacterBody)GetInstanceField(typeof(DeathRewards), self, "characterBody");
        CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
        GameObject attackerMaster = attackerBody.masterObject;
        if (attackerMaster == null || victimBody.teamComponent.teamIndex != TeamIndex.Monster) return;
        RollSpawnChance(victimBody, attackerBody);
        orig(self, damageInfo);
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
            if (!ApplyConfigModifiers(directorCard)) continue;
            directorCard.cost = Mathf.RoundToInt(directorCard.cost * interactableCostMultiplier);
            weightedSelection.AddChoice(directorCard, directorCard.selectionWeight / num * category.selectionWeight);
          }
        }
        return weightedSelection;
      };
      SceneDirector.onPrePopulateSceneServer += (director) =>
      {
        int interactableCredit = (int)GetInstanceField(typeof(SceneDirector), director, "interactableCredit");
        interactableCredit = Mathf.RoundToInt(interactableCredit * interactableSpawnMultiplier);
        SetInstanceField(typeof(SceneDirector), director, "interactableCredit", interactableCredit);
      };
    }

    private void RollSpawnChance(CharacterBody victimBody, CharacterBody attackerBody)
    {
      // Roll percent chance has a base value of 7% (configurable), multiplied by 1 + .3 per player above 1.
      float percentChance = baseDropChance * (1f + ((NetworkUser.readOnlyInstancesList.Count - 1f) * 0.3f));
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
        // Drop an item.
        PickupDropletController.CreatePickupDroplet(
          pickupIndex,
          victimBody.transform.position,
          new Vector3(UnityEngine.Random.Range(-5.0f, 5.0f), 20f, UnityEngine.Random.Range(-5.0f, 5.0f)));
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

    internal static object GetInstanceField(Type type, object instance, string fieldName)
    {
      BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
      FieldInfo field = type.GetField(fieldName, bindFlags);
      return field.GetValue(instance);
    }

    internal static void SetInstanceField(Type type, object instance, string fieldName, dynamic value)
    {
      BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
      FieldInfo field = type.GetField(fieldName, bindFlags);
      field.SetValue(instance, value);
    }
  }
}
