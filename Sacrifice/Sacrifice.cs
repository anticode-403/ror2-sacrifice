using BepInEx;
using System;
using System.Collections.Generic;
using System.Reflection;
using RoR2;
using UnityEngine;
using ConfigurationEnhanced;

namespace Sacrifice
{
  [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
  [BepInDependency("com.anticode.ConfigurationEnhanced", BepInDependency.DependencyFlags.HardDependency)]
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

    public class InteractableConfig
    {
      public string Name;
      public float SpawnWeightModifier;
      public bool Banned;

      public InteractableConfig(string name, float spawnWeightModifier, bool banned)
      {
        Name = name;
        SpawnWeightModifier = spawnWeightModifier;
        Banned = banned;
      }
    }

    public static ConfigWrapper<InteractableConfig> Interactables;
    private static List<InteractableConfig> interactables;
    public Sacrifice()
    {
      ConfigFile config = new ConfigFile("anticode-Sacrifice", false);
      // Fuck me, this is a long list of configs.
      Logger.Log(BepInEx.Logging.LogLevel.Debug, "hi 0");
      BaseDropChance = config.Wrap(
        new string[] { "Chances" },
        "BaseDropChance",
        "The base percent chance of an item dropping.",
        1.0f);
      Logger.Log(BepInEx.Logging.LogLevel.Debug, "hi 1");
      CloverRerollDrops = config.Wrap(
        new string[] { "Other" },
        "CloversRerollDrops",
        "Can clovers reroll the chance of an item dropping.",
        true);
      Logger.Log(BepInEx.Logging.LogLevel.Debug, "hi 2");
      InteractableSpawnMultiplier = config.Wrap(
        new string[] { "Interactables" },
        "InteractableSpawnMultiplier",
        "A multiplier on the amount of interactables that will spawn in a level.",
        1.0f);
      Logger.Log(BepInEx.Logging.LogLevel.Debug, "hi 4");
      InteractableCostMultiplier = config.Wrap(
        new string[] { "Interactables" },
        "InteractableCostMultiplier",
        "A multiplier applied to the cost of all interactables.",
        1.0f);
      Logger.Log(BepInEx.Logging.LogLevel.Debug, "hi 5");
      Interactables = config.ListWrap(
        new string[] { "Interactables" },
        "Individual",
        "An array of interactable configurations.",
        new List<InteractableConfig>
        {
          new InteractableConfig("Chest",1.0f,true),
          new InteractableConfig("Chest2",1.0f,true),
          new InteractableConfig("EquipmentBarrel",1.0f,true),
          new InteractableConfig("TripleShop",1.0f,true),
          new InteractableConfig("TripleShopLarge",1.0f,true),
          new InteractableConfig("GoldChest",1.0f,true),
          new InteractableConfig("LunarChest",1.0f,false),
          new InteractableConfig("Barrel1",0.5f,false),
          new InteractableConfig("ShrineHealing",1.0f,false),
          new InteractableConfig("ShrineCombat", 1.0f,false),
          new InteractableConfig("ShrineBlood",1.0f,true),
          new InteractableConfig("ShrineBoss",1.0f,false),
          new InteractableConfig("ShrineRestack",1.0f,false),
          new InteractableConfig("ShrineChance",1.0f,true),
          new InteractableConfig("BrokenDrone1",1.0f,false),
          new InteractableConfig("BrokenDrone2",1.0f,false),
          new InteractableConfig("BrokenMegaDrone",1.0f,false),
          new InteractableConfig("BrokenMissileDrone",1.0f,false),
          new InteractableConfig("BrokenTurret1",1.0f,false),
          new InteractableConfig("Chest1Stealthed",1.0f,false),
          new InteractableConfig("RadarTower",1.0f,false),
          new InteractableConfig("ShrineGoldshoresAccess",0.5f,false),
          new InteractableConfig("Duplicator",1.0f,false),
          new InteractableConfig("DuplicatorLarge",1.0f,false),
          new InteractableConfig("DuplicatorMilitary",1.0f,false),
        });
      baseDropChance = BaseDropChance.Read();
      cloverRerollDrops = CloverRerollDrops.Read();
      interactableSpawnMultiplier = InteractableSpawnMultiplier.Read();
      interactableCostMultiplier = InteractableSpawnMultiplier.Read();
      interactables = Interactables.ListRead();
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
      if (victimBody.isElite)
      {
        weightedSelection.AddChoice(Run.instance.availableLunarDropList, 0.05f);
        weightedSelection.AddChoice(Run.instance.availableTier2DropList, 0.3f);
        weightedSelection.AddChoice(Run.instance.availableTier3DropList, 0.1f);
      }
      if (victimBody.isBoss)
      {
        weightedSelection.AddChoice(Run.instance.availableTier2DropList, 0.6f);
        weightedSelection.AddChoice(Run.instance.availableTier3DropList, 0.3f);
      }
      // If the enemy in question is dead, then chances shoulder be the default for chests + some equipment item drop chances.
      else if (!victimBody.isElite)
      {
        weightedSelection.AddChoice(Run.instance.availableEquipmentDropList, 0.05f);
        weightedSelection.AddChoice(Run.instance.availableTier1DropList, 0.8f);
        weightedSelection.AddChoice(Run.instance.availableTier2DropList, 0.2f);
        weightedSelection.AddChoice(Run.instance.availableTier3DropList, 0.01f);
      }
      // Item to drop is generated before the item pick up is generated for a future update.
      List<PickupIndex> list = weightedSelection.Evaluate(Run.instance.spawnRng.nextNormalizedFloat);
      PickupIndex pickupIndex = list[Run.instance.spawnRng.RangeInt(0, list.Count)];
      CharacterMaster master = attackerBody.master;
      float luck = 0f;
      if (master && cloverRerollDrops == true) luck = master.luck;
      if (Util.CheckRoll(percentChance, luck, null))
      {
        // Drop an item.
        PickupDropletController.CreatePickupDroplet(
          pickupIndex,
          victimBody.transform.position,
          new Vector3(UnityEngine.Random.Range(-5.0f, 5.0f), 20f, UnityEngine.Random.Range(-5.0f, 5.0f)));
      }
    }

    private static bool ApplyConfigModifiers(DirectorCard card)
    {
      foreach (InteractableConfig interactableConfig in interactables)
      {
        if (card.spawnCard.prefab.name != interactableConfig.Name) continue;
        if (interactableConfig.Banned) return false;
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
