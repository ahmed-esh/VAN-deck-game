#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VanGame;
using VanGame.Core;
using VanGame.Data;
using VanGame.UI;

namespace VanGame.Editor
{
  public static class VanGameSetupWizard
  {
    const string DataRoot = "Assets/Data";
    const string CitiesPath = DataRoot + "/Cities";
    const string CardsPath = DataRoot + "/Cards";
    const string DecksPath = DataRoot + "/Decks";
    const string EventsPath = DataRoot + "/Events";
    const string AbilitiesPath = DataRoot + "/Abilities";
    const string ConfigPath = DataRoot + "/GameConfig.asset";
    const string AbilityCatalogPath = DataRoot + "/AbilityCatalog.asset";
    const string PrefabsPath = "Assets/Prefabs/VanGame";
    const string CardPrefabPath = PrefabsPath + "/CardView.prefab";
    const string AbilityPrefabPath = PrefabsPath + "/AbilityCardView.prefab";

    [MenuItem("Van Game/Create Sample Data Assets")]
    public static void CreateSampleData()
    {
      EnsureFolder(DataRoot);
      EnsureFolder(CitiesPath);
      EnsureFolder(CardsPath);
      EnsureFolder(DecksPath);
      EnsureFolder(EventsPath);
      EnsureFolder(AbilitiesPath);

      GameConfig config = LoadOrCreate<GameConfig>(ConfigPath);

      CityDefinition cityA = LoadOrCreate<CityDefinition>(CitiesPath + "/CityA.asset");
      cityA.cityId = "city_a";
      cityA.displayName = "City A";
      cityA.isStartCity = true;
      cityA.isDestinationCity = false;
      cityA.parking = ParkingType.Available;
      cityA.costOfLiving = CostOfLiving.Low;
      cityA.funTheme = "Grandpa's hometown";
      cityA.baseMoraleDelta = 0;
      cityA.stayDaysInCity = 0;

      CityDefinition dentone = LoadOrCreate<CityDefinition>(CitiesPath + "/Dentone.asset");
      dentone.cityId = "dentone";
      dentone.displayName = "Dentone";
      dentone.parking = ParkingType.Available;
      dentone.costOfLiving = CostOfLiving.Low;
      dentone.funTheme = "Oil Rigs";
      dentone.baseMoraleDelta = -20;
      dentone.stayDaysInCity = 1;

      CityDefinition southridge = LoadOrCreate<CityDefinition>(CitiesPath + "/Southridge.asset");
      southridge.cityId = "southridge";
      southridge.displayName = "Southridge";
      southridge.parking = ParkingType.FreeTrailer;
      southridge.costOfLiving = CostOfLiving.Low;
      southridge.funTheme = "Camel riding";
      southridge.baseMoraleDelta = 5;
      southridge.stayDaysInCity = 1;

      CityDefinition argylle = LoadOrCreate<CityDefinition>(CitiesPath + "/Argylle.asset");
      argylle.cityId = "argylle";
      argylle.displayName = "Argylle";
      argylle.parking = ParkingType.Limited;
      argylle.costOfLiving = CostOfLiving.High;
      argylle.funTheme = "Water park";
      argylle.baseMoraleDelta = 50;
      argylle.stayDaysInCity = 3;

      CityDefinition cityB = LoadOrCreate<CityDefinition>(CitiesPath + "/CityB.asset");
      cityB.cityId = "city_b";
      cityB.displayName = "City B";
      cityB.isDestinationCity = true;
      cityB.parking = ParkingType.Available;
      cityB.costOfLiving = CostOfLiving.Medium;
      cityB.funTheme = "Grandpa's dream destination";
      cityB.baseMoraleDelta = 20;
      cityB.stayDaysInCity = 1;

      cityA.neighborCities = new[] { dentone, southridge };
      cityA.drivingDaysToNeighbor = new[] { 2, 3 };

      dentone.neighborCities = new[] { cityA, argylle, cityB };
      dentone.drivingDaysToNeighbor = new[] { 2, 4, 6 };

      southridge.neighborCities = new[] { cityA, argylle };
      southridge.drivingDaysToNeighbor = new[] { 3, 5 };

      argylle.neighborCities = new[] { dentone, southridge, cityB };
      argylle.drivingDaysToNeighbor = new[] { 4, 5, 3 };

      cityB.neighborCities = new[] { dentone, argylle };
      cityB.drivingDaysToNeighbor = new[] { 6, 3 };

      ActionCardDefinition snack = LoadOrCreate<ActionCardDefinition>(CardsPath + "/Food_Snack.asset");
      snack.cardId = "food_snack";
      snack.title = "Have a snack";
      snack.description = "Humble beginning — cheap bite.";
      snack.category = CardCategory.Food;
      snack.moneyCostMin = 10;
      snack.moneyCostMax = 10;
      snack.effects = new[]
      {
        new CardEffect { target = CardEffectTarget.Morale, operation = CardStatOperation.Add, value = 5f }
      };
      snack.dayTimeCost = CardDayTimeCost.OneSection;
      snack.countsAsFedToday = true;
      snack.includeInStartingHand = true;

      ActionCardDefinition fuel = LoadOrCreate<ActionCardDefinition>(CardsPath + "/Fuel_LowGrade.asset");
      fuel.cardId = "fuel_low";
      fuel.title = "Fill low grade";
      fuel.category = CardCategory.Fuel;
      fuel.moneyCostMin = 40;
      fuel.moneyCostMax = 40;
      fuel.effects = new[]
      {
        new CardEffect { target = CardEffectTarget.Fuel, operation = CardStatOperation.Add, value = 25f },
        new CardEffect { target = CardEffectTarget.VanCondition, operation = CardStatOperation.Subtract, value = 10f }
      };
      fuel.dayTimeCost = CardDayTimeCost.TwoSections;
      fuel.includeInStartingHand = true;

      ActionCardDefinition foodTruck = LoadOrCreate<ActionCardDefinition>(CardsPath + "/Food_FoodTruck.asset");
      foodTruck.cardId = "food_truck";
      foodTruck.title = "Food truck";
      foodTruck.category = CardCategory.Food;
      foodTruck.moneyCostMin = 20;
      foodTruck.moneyCostMax = 20;
      foodTruck.effects = new[]
      {
        new CardEffect { target = CardEffectTarget.Morale, operation = CardStatOperation.Add, value = 10f }
      };
      foodTruck.dayTimeCost = CardDayTimeCost.TwoSections;
      foodTruck.countsAsFedToday = true;

      ActionCardDefinition cookVan = LoadOrCreate<ActionCardDefinition>(CardsPath + "/Food_CookVan.asset");
      cookVan.cardId = "food_cook_van";
      cookVan.title = "Cook in van";
      cookVan.category = CardCategory.Food;
      cookVan.moneyCostMin = 5;
      cookVan.moneyCostMax = 10;
      cookVan.rollCostOnPlay = true;
      cookVan.effects = new[]
      {
        new CardEffect { target = CardEffectTarget.Morale, operation = CardStatOperation.Add, value = 5f }
      };
      cookVan.dayTimeCost = CardDayTimeCost.FourSections;
      cookVan.countsAsFedToday = true;

      DeckDefinition deck = LoadOrCreate<DeckDefinition>(DecksPath + "/MainDeck.asset");
      deck.deckName = "Main Deck";
      deck.startingHandCards = new[] { snack, fuel };
      deck.drawPoolCards = new[] { foodTruck, cookVan, snack, fuel };

      RandomEventDefinition traffic = LoadOrCreate<RandomEventDefinition>(EventsPath + "/Event_TrafficDelay.asset");
      traffic.eventId = "traffic_delay";
      traffic.title = "Traffic jam";
      traffic.logText = "Heavy traffic added an extra unexpected day on the road.";
      traffic.extraDaysAdded = 1;

      RandomEventDefinition paidParking = LoadOrCreate<RandomEventDefinition>(EventsPath + "/Event_PaidParking.asset");
      paidParking.eventId = "paid_parking";
      paidParking.title = "Paid parking";
      paidParking.logText = "Limited parking meant paying for a downtown lot.";
      paidParking.requireParkingMatch = true;
      paidParking.requiredParking = ParkingType.Limited;
      paidParking.moneyDelta = -30;

      RandomEventDefinition cheapCamp = LoadOrCreate<RandomEventDefinition>(EventsPath + "/Event_CheapCamp.asset");
      cheapCamp.eventId = "cheap_camp";
      cheapCamp.title = "Free trailer camp";
      cheapCamp.logText = "The free trailer park lifted everyone's spirits.";
      cheapCamp.requireParkingMatch = true;
      cheapCamp.requiredParking = ParkingType.FreeTrailer;
      cheapCamp.moraleDeltaPercent = 8f;

      dentone.possibleEvents = new[] { traffic };
      dentone.eventWeights = new[] { 1f };
      southridge.possibleEvents = new[] { cheapCamp, traffic };
      southridge.eventWeights = new[] { 2f, 1f };
      argylle.possibleEvents = new[] { paidParking, traffic };
      argylle.eventWeights = new[] { 3f, 1f };
      cityB.possibleEvents = new[] { traffic };
      cityB.eventWeights = new[] { 1f };

      AbilityDefinition selfless = CreateAbility(AbilitiesPath + "/Ability_Selfless.asset", "selfless",
        "Selfless Member", "+2% morale from future actions.",
        true, ModifierTarget.MoraleGain, ModifierOperation.Multiply, 1.02f);
      AbilityDefinition bargainer = CreateAbility(AbilitiesPath + "/Ability_Bargainer.asset", "bargainer",
        "Bargainer", "Spend 2% less money on actions.",
        true, ModifierTarget.MoneyCost, ModifierOperation.Multiply, 0.98f);
      AbilityDefinition practitioner = CreateAbility(AbilitiesPath + "/Ability_Practitioner.asset", "practitioner",
        "Practitioner", "Actions take 5% less real time.",
        true, ModifierTarget.ActionDuration, ModifierOperation.Multiply, 0.95f);
      AbilityDefinition mechanic = CreateAbility(AbilitiesPath + "/Ability_Mechanic.asset", "mechanic",
        "Mechanic", "Van actions take 20% less time.",
        false, ModifierTarget.ActionDuration, ModifierOperation.Multiply, 0.8f);
      AbilityDefinition charmer = CreateAbility(AbilitiesPath + "/Ability_Charmer.asset", "charmer",
        "Charmer", "Gain 10% more morale from actions.",
        false, ModifierTarget.MoraleGain, ModifierOperation.Multiply, 1.1f);
      AbilityDefinition thinker = CreateAbility(AbilitiesPath + "/Ability_Thinker.asset", "thinker",
        "Thinker", "+15 seconds each driving day.",
        false, ModifierTarget.DrivingDayBudget, ModifierOperation.Add, 15f);

      AbilityCatalog catalog = LoadOrCreate<AbilityCatalog>(AbilityCatalogPath);
      catalog.firstCityRewards = new[] { selfless, bargainer, practitioner };
      catalog.generalPool = new[] { mechanic, charmer, thinker, selfless, bargainer, practitioner };

      EditorUtility.SetDirty(config);
      EditorUtility.SetDirty(cityA);
      EditorUtility.SetDirty(dentone);
      EditorUtility.SetDirty(southridge);
      EditorUtility.SetDirty(argylle);
      EditorUtility.SetDirty(cityB);
      EditorUtility.SetDirty(snack);
      EditorUtility.SetDirty(fuel);
      EditorUtility.SetDirty(foodTruck);
      EditorUtility.SetDirty(cookVan);
      EditorUtility.SetDirty(deck);
      EditorUtility.SetDirty(traffic);
      EditorUtility.SetDirty(paidParking);
      EditorUtility.SetDirty(cheapCamp);
      EditorUtility.SetDirty(catalog);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Debug.Log("Van Game sample data created under Assets/Data/. Assign CityA, CityB, MainDeck, and GameConfig on GameManager.");
    }

    [MenuItem("Van Game/Build UI Hierarchy In Scene")]
    public static void BuildSceneHierarchy()
    {
      CreateSampleData();
      EnsureEventSystem();

      GameObject manager = GetOrCreate("GameManager");
      GameFlowController flow = GetOrAdd<GameFlowController>(manager);
      StatResolver resolver = GetOrAdd<StatResolver>(manager);
      DeckController deck = GetOrAdd<DeckController>(manager);
      EndOfDayResolver endOfDay = GetOrAdd<EndOfDayResolver>(manager);
      DrivingTurnController drivingTurn = GetOrAdd<DrivingTurnController>(manager);
      CityArrivalController cityArrival = GetOrAdd<CityArrivalController>(manager);
      CityRandomEventResolver randomEvents = GetOrAdd<CityRandomEventResolver>(manager);
      CanvasTransitionController transitions = GetOrAdd<CanvasTransitionController>(manager);
      MapController map = GetOrAdd<MapController>(manager);

      Canvas cardCanvas = FindOrCreateCanvas("Canvas_Cards", 0);
      GetOrAdd<CanvasGroup>(cardCanvas.gameObject);
      Canvas mapCanvas = FindOrCreateCanvas("Canvas_Map", 10);

      StatsHudView hud = BuildStatsHud(cardCanvas.transform);
      Button openMapBtn = CreateButton(cardCanvas.transform, "Button_OpenMap", "Open Map", new Vector2(-120f, -260f));

      GameObject drivingPanel = CreateUiObject("DrivingPanel", cardCanvas.transform);
      StretchFull(drivingPanel.GetComponent<RectTransform>());
      DrivingDayTimerView timerView = BuildDrivingTimer(drivingPanel.transform);
      CardHandController cardHand = BuildCardHand(drivingPanel.transform, CreateOrLoadCardPrefab());

      GameObject cityArrivalPanel = BuildCityArrivalPanel(cardCanvas.transform, out EventLogView eventLog,
        out AbilityPickController abilityPick);
      WinLoseView winView = BuildWinLosePanel(cardCanvas.transform, "WinPanel", true);
      WinLoseView loseView = BuildWinLosePanel(cardCanvas.transform, "LosePanel", false);

      CanvasGroup mapGroup = GetOrAdd<CanvasGroup>(mapCanvas.gameObject);
      RectTransform mapRoot = CreateMapStructure(mapCanvas.transform, out CanvasGroup shade, out MapStatsTooltipView tooltip, out MapVanMarkerView vanMarker, out MapRegionView[] regions);
      Button closeMapBtn = CreateButton(mapCanvas.transform, "Button_CloseMap", "Close Map", new Vector2(320f, -260f));

      AssignFlowReferences(flow, resolver, deck, endOfDay, drivingTurn, cityArrival, randomEvents, transitions, map,
        hud, cardHand, timerView, eventLog, abilityPick, winView, loseView,
        openMapBtn, closeMapBtn, drivingPanel, cityArrivalPanel, cardCanvas, mapCanvas, mapGroup, mapRoot, shade, tooltip, vanMarker, regions);
      AssignCityDefinitionsToRegions(regions);

      mapCanvas.gameObject.SetActive(false);
      Selection.activeGameObject = manager;
      Debug.Log("Van Game UI hierarchy built. Place map region Images under MapRegions and assign CityDefinition on each MapRegionView.");
    }

    static void AssignCityDefinitionsToRegions(MapRegionView[] regions)
    {
      if (regions == null || regions.Length == 0)
        return;

      CityDefinition[] cities = {
        AssetDatabase.LoadAssetAtPath<CityDefinition>(CitiesPath + "/CityA.asset"),
        AssetDatabase.LoadAssetAtPath<CityDefinition>(CitiesPath + "/Dentone.asset"),
        AssetDatabase.LoadAssetAtPath<CityDefinition>(CitiesPath + "/Southridge.asset"),
        AssetDatabase.LoadAssetAtPath<CityDefinition>(CitiesPath + "/Argylle.asset"),
        AssetDatabase.LoadAssetAtPath<CityDefinition>(CitiesPath + "/CityB.asset")
      };

      for (int i = 0; i < regions.Length && i < cities.Length; i++)
      {
        if (regions[i] == null || cities[i] == null)
          continue;

        SerializedObject so = new SerializedObject(regions[i]);
        so.FindProperty("city").objectReferenceValue = cities[i];
        so.ApplyModifiedProperties();
      }
    }

    static AbilityDefinition CreateAbility(string path, string id, string title, string description, bool firstCity,
      ModifierTarget target, ModifierOperation op, float value)
    {
      AbilityDefinition ability = LoadOrCreate<AbilityDefinition>(path);
      ability.abilityId = id;
      ability.title = title;
      ability.description = description;
      ability.isFirstCityReward = firstCity;
      ability.modifiers = new[]
      {
        new AbilityModifier { target = target, operation = op, value = value }
      };
      EditorUtility.SetDirty(ability);
      return ability;
    }

    static void AssignFlowReferences(
      GameFlowController flow,
      StatResolver resolver,
      DeckController deck,
      EndOfDayResolver endOfDay,
      DrivingTurnController drivingTurn,
      CityArrivalController cityArrival,
      CityRandomEventResolver randomEvents,
      CanvasTransitionController transitions,
      MapController mapCtrl,
      StatsHudView hud,
      CardHandController cardHand,
      DrivingDayTimerView timerView,
      EventLogView eventLog,
      AbilityPickController abilityPick,
      WinLoseView winView,
      WinLoseView loseView,
      Button openMap,
      Button closeMap,
      GameObject drivingPanel,
      GameObject cityArrivalPanel,
      Canvas cardCanvas,
      Canvas mapCanvas,
      CanvasGroup mapGroup,
      RectTransform mapRoot,
      CanvasGroup shade,
      MapStatsTooltipView tooltip,
      MapVanMarkerView vanMarker,
      MapRegionView[] regions)
    {
      GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
      AbilityCatalog catalog = AssetDatabase.LoadAssetAtPath<AbilityCatalog>(AbilityCatalogPath);

      SerializedObject flowSo = new SerializedObject(flow);
      flowSo.FindProperty("gameConfig").objectReferenceValue = config;
      flowSo.FindProperty("startCity").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CityDefinition>(CitiesPath + "/CityA.asset");
      flowSo.FindProperty("destinationCity").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CityDefinition>(CitiesPath + "/CityB.asset");
      flowSo.FindProperty("deckDefinition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DeckDefinition>(DecksPath + "/MainDeck.asset");
      flowSo.FindProperty("abilityCatalog").objectReferenceValue = catalog;
      flowSo.FindProperty("statResolver").objectReferenceValue = resolver;
      flowSo.FindProperty("deckController").objectReferenceValue = deck;
      flowSo.FindProperty("drivingTurn").objectReferenceValue = drivingTurn;
      flowSo.FindProperty("cityArrival").objectReferenceValue = cityArrival;
      flowSo.FindProperty("randomEventResolver").objectReferenceValue = randomEvents;
      flowSo.FindProperty("canvasTransition").objectReferenceValue = transitions;
      flowSo.FindProperty("mapController").objectReferenceValue = mapCtrl;
      flowSo.FindProperty("statsHud").objectReferenceValue = hud;
      flowSo.FindProperty("openMapButton").objectReferenceValue = openMap;
      flowSo.FindProperty("closeMapButton").objectReferenceValue = closeMap;
      flowSo.FindProperty("drivingPanel").objectReferenceValue = drivingPanel;
      flowSo.FindProperty("cityArrivalPanel").objectReferenceValue = cityArrivalPanel;
      flowSo.FindProperty("winView").objectReferenceValue = winView;
      flowSo.FindProperty("loseView").objectReferenceValue = loseView;
      flowSo.ApplyModifiedProperties();

      SerializedObject arrivalSo = new SerializedObject(cityArrival);
      arrivalSo.FindProperty("gameConfig").objectReferenceValue = config;
      arrivalSo.FindProperty("statResolver").objectReferenceValue = resolver;
      arrivalSo.FindProperty("eventResolver").objectReferenceValue = randomEvents;
      arrivalSo.FindProperty("eventLogView").objectReferenceValue = eventLog;
      arrivalSo.FindProperty("abilityPick").objectReferenceValue = abilityPick;
      arrivalSo.FindProperty("drivingPanel").objectReferenceValue = drivingPanel;
      arrivalSo.ApplyModifiedProperties();

      SerializedObject pickSo = new SerializedObject(abilityPick);
      pickSo.FindProperty("abilityCatalog").objectReferenceValue = catalog;
      pickSo.FindProperty("gameConfig").objectReferenceValue = config;
      pickSo.ApplyModifiedProperties();

      winView?.Initialize(flow, resolver, config);
      loseView?.Initialize(flow, resolver, config);

      SerializedObject drivingSo = new SerializedObject(drivingTurn);
      drivingSo.FindProperty("gameFlow").objectReferenceValue = flow;
      drivingSo.FindProperty("gameConfig").objectReferenceValue = config;
      drivingSo.FindProperty("statResolver").objectReferenceValue = resolver;
      drivingSo.FindProperty("deckController").objectReferenceValue = deck;
      drivingSo.FindProperty("endOfDayResolver").objectReferenceValue = endOfDay;
      drivingSo.FindProperty("cardHand").objectReferenceValue = cardHand;
      drivingSo.FindProperty("timerView").objectReferenceValue = timerView;
      drivingSo.ApplyModifiedProperties();

      SerializedObject endOfDaySo = new SerializedObject(endOfDay);
      endOfDaySo.FindProperty("gameConfig").objectReferenceValue = config;
      endOfDaySo.ApplyModifiedProperties();

      SerializedObject transSo = new SerializedObject(transitions);
      transSo.FindProperty("cardCanvas").objectReferenceValue = cardCanvas;
      transSo.FindProperty("mapCanvas").objectReferenceValue = mapCanvas;
      transSo.FindProperty("mapCanvasGroup").objectReferenceValue = mapGroup;
      transSo.FindProperty("mapRoot").objectReferenceValue = mapRoot;
      transSo.FindProperty("mapShadeOverlay").objectReferenceValue = shade;
      transSo.FindProperty("gameConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
      transSo.ApplyModifiedProperties();

      SerializedObject mapSo = new SerializedObject(mapCtrl);
      mapSo.FindProperty("mapRegions").arraySize = regions.Length;
      for (int i = 0; i < regions.Length; i++)
        mapSo.FindProperty("mapRegions").GetArrayElementAtIndex(i).objectReferenceValue = regions[i];
      mapSo.FindProperty("tooltip").objectReferenceValue = tooltip;
      mapSo.FindProperty("closeMapButton").objectReferenceValue = closeMap.gameObject;
      mapSo.FindProperty("vanMarker").objectReferenceValue = vanMarker;
      mapSo.ApplyModifiedProperties();
    }

    static RectTransform CreateMapStructure(Transform mapCanvas, out CanvasGroup shade, out MapStatsTooltipView tooltip, out MapVanMarkerView vanMarker, out MapRegionView[] regions)
    {
      GameObject shadeGo = CreateUiObject("MapShadeOverlay", mapCanvas);
      RectTransform shadeRect = shadeGo.GetComponent<RectTransform>();
      StretchFull(shadeRect);
      Image shadeImg = shadeGo.AddComponent<Image>();
      shadeImg.color = Color.black;
      shadeImg.raycastTarget = true;
      shade = shadeGo.AddComponent<CanvasGroup>();
      shade.alpha = 0f;
      shade.blocksRaycasts = false;
      shadeGo.SetActive(false);

      GameObject rootGo = CreateUiObject("MapRoot", mapCanvas);
      RectTransform mapRoot = rootGo.GetComponent<RectTransform>();
      StretchFull(mapRoot);

      GameObject bgGo = CreateUiObject("MapBackground", rootGo.transform);
      StretchFull(bgGo.GetComponent<RectTransform>());
      Image bgImg = bgGo.AddComponent<Image>();
      bgImg.color = new Color(0.15f, 0.35f, 0.55f, 1f);
      bgImg.raycastTarget = false;

      GameObject regionsGo = CreateUiObject("MapRegions", rootGo.transform);
      StretchFull(regionsGo.GetComponent<RectTransform>());

      string[] names = { "Region_CityA", "Region_Dentone", "Region_Southridge", "Region_Argylle", "Region_CityB" };
      Vector2[] positions = {
        new Vector2(-280f, -40f),
        new Vector2(-120f, 60f),
        new Vector2(-200f, -120f),
        new Vector2(80f, 20f),
        new Vector2(280f, -20f)
      };

      regions = new MapRegionView[names.Length];
      for (int i = 0; i < names.Length; i++)
      {
        GameObject regionGo = CreateUiObject(names[i], regionsGo.transform);
        RectTransform rt = regionGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 90f);
        rt.anchoredPosition = positions[i];
        Image img = regionGo.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.35f);
        img.raycastTarget = true;
        regions[i] = regionGo.AddComponent<MapRegionView>();
      }

      vanMarker = CreateMapVanMarker(regionsGo.transform);

      GameObject tooltipGo = CreateUiObject("MapStatsTooltip", mapCanvas);
      RectTransform tooltipRect = tooltipGo.GetComponent<RectTransform>();
      tooltipRect.anchorMin = new Vector2(0f, 1f);
      tooltipRect.anchorMax = new Vector2(0f, 1f);
      tooltipRect.pivot = new Vector2(0f, 1f);
      tooltipRect.anchoredPosition = new Vector2(24f, -24f);
      tooltipRect.sizeDelta = new Vector2(320f, 220f);
      Image tooltipBg = tooltipGo.AddComponent<Image>();
      tooltipBg.color = new Color(0f, 0f, 0f, 0.82f);
      CanvasGroup tooltipGroup = tooltipGo.AddComponent<CanvasGroup>();
      tooltip = tooltipGo.AddComponent<MapStatsTooltipView>();

      SerializedObject tooltipSo = new SerializedObject(tooltip);
      tooltipSo.FindProperty("canvasGroup").objectReferenceValue = tooltipGroup;
      tooltipSo.FindProperty("cityNameText").objectReferenceValue = CreateTmpChild(tooltipGo.transform, "CityName", new Vector2(12f, -12f));
      tooltipSo.FindProperty("parkingText").objectReferenceValue = CreateTmpChild(tooltipGo.transform, "Parking", new Vector2(12f, -44f));
      tooltipSo.FindProperty("costOfLivingText").objectReferenceValue = CreateTmpChild(tooltipGo.transform, "Cost", new Vector2(12f, -76f));
      tooltipSo.FindProperty("funThemeText").objectReferenceValue = CreateTmpChild(tooltipGo.transform, "Fun", new Vector2(12f, -108f));
      tooltipSo.FindProperty("moraleText").objectReferenceValue = CreateTmpChild(tooltipGo.transform, "Morale", new Vector2(12f, -140f));
      tooltipSo.FindProperty("stayDaysText").objectReferenceValue = CreateTmpChild(tooltipGo.transform, "StayDays", new Vector2(12f, -172f));
      tooltipSo.FindProperty("drivingDaysText").objectReferenceValue = CreateTmpChild(tooltipGo.transform, "DrivingDays", new Vector2(160f, -172f));
      tooltipSo.ApplyModifiedProperties();

      tooltipGo.SetActive(false);
      return mapRoot;
    }

    static MapVanMarkerView CreateMapVanMarker(Transform parent)
    {
      GameObject go = CreateUiObject("MapVanMarker", parent);
      RectTransform rt = go.GetComponent<RectTransform>();
      rt.anchorMin = new Vector2(0.5f, 0.5f);
      rt.anchorMax = new Vector2(0.5f, 0.5f);
      rt.pivot = new Vector2(0.5f, 0.5f);
      rt.sizeDelta = new Vector2(64f, 64f);
      rt.anchoredPosition = Vector2.zero;
      rt.SetAsLastSibling();

      Image image = go.AddComponent<Image>();
      image.raycastTarget = false;
      image.preserveAspect = true;
      Sprite vanSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Visuals/vadfwdwadawn.png");
      if (vanSprite != null)
        image.sprite = vanSprite;

      MapVanMarkerView marker = go.AddComponent<MapVanMarkerView>();
      SerializedObject markerSo = new SerializedObject(marker);
      markerSo.FindProperty("rectTransform").objectReferenceValue = rt;
      markerSo.FindProperty("vanImage").objectReferenceValue = image;
      markerSo.ApplyModifiedProperties();
      return marker;
    }

    static DrivingDayTimerView BuildDrivingTimer(Transform parent)
    {
      GameObject go = CreateUiObject("DrivingTimer", parent);
      RectTransform rt = go.GetComponent<RectTransform>();
      rt.anchorMin = new Vector2(0.5f, 1f);
      rt.anchorMax = new Vector2(0.5f, 1f);
      rt.pivot = new Vector2(0.5f, 1f);
      rt.anchoredPosition = new Vector2(0f, -70f);
      rt.sizeDelta = new Vector2(520f, 72f);

      DrivingDayTimerView view = go.AddComponent<DrivingDayTimerView>();
      SerializedObject so = new SerializedObject(view);
      so.FindProperty("destinationText").objectReferenceValue = CreateTmpChild(go.transform, "Destination", new Vector2(0f, 20f));
      so.FindProperty("legDaysText").objectReferenceValue = CreateTmpChild(go.transform, "LegDays", new Vector2(0f, -8f));
      so.FindProperty("dayTimerText").objectReferenceValue = CreateTmpChild(go.transform, "Timer", new Vector2(0f, -36f));

      GameObject fillGo = CreateUiObject("TimerFill", go.transform);
      RectTransform fillRt = fillGo.GetComponent<RectTransform>();
      fillRt.sizeDelta = new Vector2(400f, 12f);
      fillRt.anchoredPosition = new Vector2(0f, -52f);
      Image fillImg = fillGo.AddComponent<Image>();
      fillImg.color = new Color(0.2f, 0.75f, 0.35f, 1f);
      fillImg.type = Image.Type.Filled;
      fillImg.fillMethod = Image.FillMethod.Horizontal;
      so.FindProperty("dayTimerFill").objectReferenceValue = fillImg;
      so.ApplyModifiedProperties();
      return view;
    }

    static CardHandController BuildCardHand(Transform parent, CardView cardPrefab)
    {
      GameObject handGo = CreateUiObject("CardHandArea", parent);
      RectTransform rt = handGo.GetComponent<RectTransform>();
      rt.anchorMin = new Vector2(0.5f, 0f);
      rt.anchorMax = new Vector2(0.5f, 0f);
      rt.pivot = new Vector2(0.5f, 0f);
      rt.anchoredPosition = new Vector2(0f, 40f);
      rt.sizeDelta = new Vector2(720f, 200f);

      Image handBg = handGo.AddComponent<Image>();
      handBg.color = new Color(0f, 0f, 0f, 0.25f);
      handBg.raycastTarget = true;

      handGo.AddComponent<BoxCollider2D>();
      CardHandHoverFan fan = handGo.AddComponent<CardHandHoverFan>();
      CardHandController hand = handGo.AddComponent<CardHandController>();

      SerializedObject handSo = new SerializedObject(hand);
      handSo.FindProperty("handContainer").objectReferenceValue = rt;
      handSo.FindProperty("hoverFan").objectReferenceValue = fan;
      handSo.FindProperty("fallbackCardPrefab").objectReferenceValue = cardPrefab;
      handSo.ApplyModifiedProperties();

      return hand;
    }

    static CardView CreateOrLoadCardPrefab()
    {
      EnsureFolder("Assets/Prefabs");
      EnsureFolder(PrefabsPath);

      CardView existing = AssetDatabase.LoadAssetAtPath<CardView>(CardPrefabPath);
      if (existing != null)
        return existing;

      GameObject go = CreateUiObject("CardView", null);
      RectTransform rt = go.GetComponent<RectTransform>();
      rt.sizeDelta = new Vector2(110f, 150f);

      Image bg = go.AddComponent<Image>();
      bg.color = new Color(0.95f, 0.92f, 0.85f, 1f);
      go.AddComponent<CanvasGroup>();

      GameObject descriptionGo = CreateUiObject("Description", go.transform);
      RectTransform descriptionRt = descriptionGo.GetComponent<RectTransform>();
      descriptionRt.sizeDelta = new Vector2(100f, 120f);
      descriptionRt.anchoredPosition = new Vector2(0f, 8f);
      TMP_Text descriptionTmp = CreateTmpChild(descriptionGo.transform, "DescriptionText", Vector2.zero);
      descriptionTmp.fontSize = 14f;
      descriptionTmp.alignment = TextAlignmentOptions.Center;
      descriptionGo.SetActive(false);

      CardView view = go.AddComponent<CardView>();
      SerializedObject so = new SerializedObject(view);
      so.FindProperty("backgroundImage").objectReferenceValue = bg;
      so.FindProperty("descriptionRoot").objectReferenceValue = descriptionGo;
      so.FindProperty("descriptionText").objectReferenceValue = descriptionTmp;
      so.ApplyModifiedProperties();

      PrefabUtility.SaveAsPrefabAsset(go, CardPrefabPath);
      Object.DestroyImmediate(go);
      return AssetDatabase.LoadAssetAtPath<CardView>(CardPrefabPath);
    }

    static GameObject BuildCityArrivalPanel(Transform parent, out EventLogView eventLog, out AbilityPickController abilityPick)
    {
      GameObject panel = CreateUiObject("CityArrivalPanel", parent);
      StretchFull(panel.GetComponent<RectTransform>());
      Image bg = panel.AddComponent<Image>();
      bg.color = new Color(0f, 0f, 0f, 0.72f);
      bg.raycastTarget = true;
      panel.SetActive(false);

      GameObject logRoot = CreateUiObject("EventLogPanel", panel.transform);
      RectTransform logRt = logRoot.GetComponent<RectTransform>();
      logRt.anchorMin = new Vector2(0.5f, 0.5f);
      logRt.anchorMax = new Vector2(0.5f, 0.5f);
      logRt.sizeDelta = new Vector2(560f, 360f);
      Image logBg = logRoot.AddComponent<Image>();
      logBg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

      eventLog = logRoot.AddComponent<EventLogView>();
      TMP_Text header = CreateTmpChild(logRoot.transform, "Header", new Vector2(0f, 150f));
      header.fontSize = 26f;
      header.alignment = TextAlignmentOptions.Center;

      GameObject linesGo = CreateUiObject("Lines", logRoot.transform);
      RectTransform linesRt = linesGo.GetComponent<RectTransform>();
      linesRt.sizeDelta = new Vector2(500f, 220f);
      linesRt.anchoredPosition = new Vector2(0f, 10f);
      VerticalLayoutGroup layout = linesGo.AddComponent<VerticalLayoutGroup>();
      layout.childAlignment = TextAnchor.UpperLeft;
      layout.spacing = 8f;
      layout.padding = new RectOffset(12, 12, 8, 8);

      TMP_Text lineTemplate = CreateTmpChild(linesGo.transform, "LineTemplate", Vector2.zero);
      lineTemplate.fontSize = 18f;
      lineTemplate.alignment = TextAlignmentOptions.TopLeft;
      lineTemplate.rectTransform.sizeDelta = new Vector2(480f, 48f);
      lineTemplate.gameObject.SetActive(false);

      Button continueBtn = CreateButton(logRoot.transform, "ContinueButton", "Continue", new Vector2(0f, -150f));

      SerializedObject logSo = new SerializedObject(eventLog);
      logSo.FindProperty("root").objectReferenceValue = logRoot;
      logSo.FindProperty("headerText").objectReferenceValue = header;
      logSo.FindProperty("linesContainer").objectReferenceValue = linesRt;
      logSo.FindProperty("linePrefab").objectReferenceValue = lineTemplate;
      logSo.FindProperty("continueButton").objectReferenceValue = continueBtn;
      logSo.FindProperty("gameConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
      logSo.ApplyModifiedProperties();

      GameObject pickRoot = CreateUiObject("AbilityPickPanel", panel.transform);
      StretchFull(pickRoot.GetComponent<RectTransform>());
      pickRoot.SetActive(false);

      GameObject handGo = CreateUiObject("AbilityHandArea", pickRoot.transform);
      RectTransform handRt = handGo.GetComponent<RectTransform>();
      handRt.anchorMin = new Vector2(0.5f, 0f);
      handRt.anchorMax = new Vector2(0.5f, 0f);
      handRt.pivot = new Vector2(0.5f, 0f);
      handRt.anchoredPosition = new Vector2(0f, 80f);
      handRt.sizeDelta = new Vector2(720f, 220f);
      handGo.AddComponent<BoxCollider2D>();
      CardHandHoverFan fan = handGo.AddComponent<CardHandHoverFan>();
      abilityPick = handGo.AddComponent<AbilityPickController>();

      TMP_Text pickHeader = CreateTmpChild(pickRoot.transform, "PickHeader", new Vector2(0f, 280f));
      pickHeader.text = "Choose an ability";
      pickHeader.fontSize = 28f;
      pickHeader.alignment = TextAlignmentOptions.Center;

      SerializedObject pickSo = new SerializedObject(abilityPick);
      pickSo.FindProperty("root").objectReferenceValue = pickRoot;
      pickSo.FindProperty("choicesContainer").objectReferenceValue = handRt;
      pickSo.FindProperty("abilityCardPrefab").objectReferenceValue = CreateOrLoadAbilityCardPrefab();
      pickSo.FindProperty("hoverFan").objectReferenceValue = fan;
      pickSo.ApplyModifiedProperties();

      return panel;
    }

    static WinLoseView BuildWinLosePanel(Transform parent, string name, bool isWin)
    {
      GameObject panel = CreateUiObject(name, parent);
      StretchFull(panel.GetComponent<RectTransform>());
      Image bg = panel.AddComponent<Image>();
      bg.color = new Color(0f, 0f, 0f, 0.82f);
      CanvasGroup group = panel.AddComponent<CanvasGroup>();
      WinLoseView view = panel.AddComponent<WinLoseView>();

      TMP_Text title = CreateTmpChild(panel.transform, "Title", new Vector2(0f, 80f));
      title.fontSize = 32f;
      title.alignment = TextAlignmentOptions.Center;
      title.text = isWin ? "You made it!" : "Trip over";

      TMP_Text summary = CreateTmpChild(panel.transform, "Summary", new Vector2(0f, -20f));
      summary.fontSize = 20f;
      summary.alignment = TextAlignmentOptions.Center;
      summary.rectTransform.sizeDelta = new Vector2(520f, 160f);

      Button restart = CreateButton(panel.transform, "RestartButton", "Play Again", new Vector2(0f, -140f));

      SerializedObject so = new SerializedObject(view);
      so.FindProperty("canvasGroup").objectReferenceValue = group;
      so.FindProperty("titleText").objectReferenceValue = title;
      so.FindProperty("summaryText").objectReferenceValue = summary;
      so.FindProperty("restartButton").objectReferenceValue = restart;
      so.FindProperty("gameConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
      so.ApplyModifiedProperties();

      panel.SetActive(false);
      return view;
    }

    static AbilityCardView CreateOrLoadAbilityCardPrefab()
    {
      EnsureFolder("Assets/Prefabs");
      EnsureFolder(PrefabsPath);

      AbilityCardView existing = AssetDatabase.LoadAssetAtPath<AbilityCardView>(AbilityPrefabPath);
      if (existing != null)
        return existing;

      GameObject go = CreateUiObject("AbilityCardView", null);
      go.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 200f);
      Image bg = go.AddComponent<Image>();
      bg.color = new Color(0.85f, 0.92f, 1f, 1f);
      AbilityCardView view = go.AddComponent<AbilityCardView>();

      SerializedObject so = new SerializedObject(view);
      so.FindProperty("backgroundImage").objectReferenceValue = bg;
      so.FindProperty("titleText").objectReferenceValue = CreateTmpChild(go.transform, "Title", new Vector2(0f, 70f));
      so.FindProperty("descriptionText").objectReferenceValue = CreateTmpChild(go.transform, "Description", new Vector2(0f, -10f));
      so.ApplyModifiedProperties();

      PrefabUtility.SaveAsPrefabAsset(go, AbilityPrefabPath);
      Object.DestroyImmediate(go);
      return AssetDatabase.LoadAssetAtPath<AbilityCardView>(AbilityPrefabPath);
    }

    static StatsHudView BuildStatsHud(Transform parent)
    {
      GameObject hudGo = CreateUiObject("StatsHUD", parent);
      RectTransform rt = hudGo.GetComponent<RectTransform>();
      rt.anchorMin = new Vector2(0.5f, 1f);
      rt.anchorMax = new Vector2(0.5f, 1f);
      rt.pivot = new Vector2(0.5f, 1f);
      rt.anchoredPosition = new Vector2(0f, -12f);
      rt.sizeDelta = new Vector2(700f, 48f);

      StatsHudView hud = hudGo.AddComponent<StatsHudView>();
      SerializedObject so = new SerializedObject(hud);
      so.FindProperty("moneyText").objectReferenceValue = CreateTmpChild(hudGo.transform, "Money", new Vector2(-300f, 0f));
      so.FindProperty("fuelText").objectReferenceValue = CreateTmpChild(hudGo.transform, "Fuel", new Vector2(-150f, 0f));
      so.FindProperty("moraleText").objectReferenceValue = CreateTmpChild(hudGo.transform, "Morale", new Vector2(0f, 0f));
      so.FindProperty("vanText").objectReferenceValue = CreateTmpChild(hudGo.transform, "Van", new Vector2(150f, 0f));
      so.FindProperty("dayText").objectReferenceValue = CreateTmpChild(hudGo.transform, "Day", new Vector2(300f, 0f));
      so.ApplyModifiedProperties();
      return hud;
    }

    static TMP_Text CreateTmpChild(Transform parent, string label, Vector2 anchoredPos)
    {
      GameObject go = CreateUiObject(label, parent);
      RectTransform rt = go.GetComponent<RectTransform>();
      rt.sizeDelta = new Vector2(140f, 32f);
      rt.anchoredPosition = anchoredPos;
      TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
      tmp.fontSize = 20f;
      tmp.text = label;
      tmp.color = Color.white;
      return tmp;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
    {
      GameObject go = CreateUiObject(name, parent);
      RectTransform rt = go.GetComponent<RectTransform>();
      rt.sizeDelta = new Vector2(180f, 48f);
      rt.anchoredPosition = pos;
      Image img = go.AddComponent<Image>();
      img.color = new Color(0.2f, 0.45f, 0.85f, 1f);
      Button btn = go.AddComponent<Button>();

      GameObject textGo = CreateUiObject("Text", go.transform);
      StretchFull(textGo.GetComponent<RectTransform>());
      TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
      tmp.text = label;
      tmp.alignment = TextAlignmentOptions.Center;
      tmp.color = Color.white;
      tmp.fontSize = 20f;
      return btn;
    }

    static GameObject CreatePanel(Transform parent, string name, string label)
    {
      GameObject go = CreateUiObject(name, parent);
      StretchFull(go.GetComponent<RectTransform>());
      TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
      tmp.text = label;
      tmp.alignment = TextAlignmentOptions.Center;
      tmp.color = new Color(1f, 1f, 1f, 0.35f);
      tmp.fontSize = 28f;
      return go;
    }

    static Canvas FindOrCreateCanvas(string name, int sortOrder)
    {
      GameObject existing = GameObject.Find(name);
      if (existing != null)
        return existing.GetComponent<Canvas>();

      GameObject go = new GameObject(name);
      Canvas canvas = go.AddComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvas.sortingOrder = sortOrder;
      go.AddComponent<CanvasScaler>();
      go.AddComponent<GraphicRaycaster>();
      return canvas;
    }

    static void EnsureEventSystem()
    {
      if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
        return;

      GameObject es = new GameObject("EventSystem");
      es.AddComponent<UnityEngine.EventSystems.EventSystem>();
      es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    static GameObject GetOrCreate(string name)
    {
      GameObject go = GameObject.Find(name);
      return go != null ? go : new GameObject(name);
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
      T c = go.GetComponent<T>();
      return c != null ? c : go.AddComponent<T>();
    }

    static GameObject CreateUiObject(string name, Transform parent)
    {
      GameObject go = new GameObject(name, typeof(RectTransform));
      go.transform.SetParent(parent, false);
      return go;
    }

    static void StretchFull(RectTransform rt)
    {
      rt.anchorMin = Vector2.zero;
      rt.anchorMax = Vector2.one;
      rt.offsetMin = Vector2.zero;
      rt.offsetMax = Vector2.zero;
    }

    static void EnsureFolder(string path)
    {
      if (AssetDatabase.IsValidFolder(path))
        return;

      string[] parts = path.Split('/');
      string current = parts[0];
      for (int i = 1; i < parts.Length; i++)
      {
        string next = current + "/" + parts[i];
        if (!AssetDatabase.IsValidFolder(next))
          AssetDatabase.CreateFolder(current, parts[i]);
        current = next;
      }
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
      T asset = AssetDatabase.LoadAssetAtPath<T>(path);
      if (asset != null)
        return asset;

      asset = ScriptableObject.CreateInstance<T>();
      AssetDatabase.CreateAsset(asset, path);
      return asset;
    }
  }
}
#endif
