using NUnit.Framework;

using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchRulesTests
{
    private List<CardDefinition> card;
    private CardDefinition testCard;

    [SetUp]
    public void SetUp()
    {
        testCard = new CardDefinition("test", "Test", 2, 0, 3, CardType.Monster);
        card = new List<CardDefinition> { testCard };
    }




    [Test]
    public void PlaySupportCard_RejectsInvalidCardWithoutChangingState()
    {
        MatchState state = MatchRules.CreateMatch(card, 2, 1, 42);
        CardInstance monster = state.PlayerHand[0];

        bool applied = MatchRules.TryApply(state,
            new PlaySupportCardAction(state.Id, PlayerSide.Player, monster.Id));

        Assert.IsFalse(applied);
        Assert.AreEqual(1, state.PlayerHand.Count);
        Assert.AreEqual(1, state.PlayerDeck.Count);
        Assert.AreEqual(1, state.PlayerSummonsRemaining);
    }




    private sealed class AlwaysCondition : ISupportCondition
    {
        public string Description => "Always active";

        public bool IsMet(MatchState state, PlayerSide actor) => true;
    }

    private sealed class NeverCondition : ISupportCondition
    {
        public string Description => "Never active";

        public bool IsMet(MatchState state, PlayerSide actor) => false;
    }

    [Test]
    public void DrawTwo_AllowsOneHiSupporterPerSideTurn()
    {
        SupportCardData drawTwo = AssetDatabase.LoadAssetAtPath<SupportCardData>(
            "Assets/_Data/_SupportCard/DrawTwo.asset");
        Assert.IsNotNull(drawTwo);
        CardDefinition definition = drawTwo.ToDefinition();
        Assert.AreEqual(CardType.HiSupporter, definition.Type);
        MatchState state = MatchRules.CreateMatch(Enumerable.Repeat(definition, 4).ToList(), 4, 1, 42);

        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaySupportCardAction(state.Id, PlayerSide.Player, state.PlayerHand[0].Id)));
        Assert.AreEqual(2, state.PlayerHand.Count);
        Assert.AreEqual(1, state.PlayerSummonsRemaining);
        long secondCardId = state.PlayerHand[0].Id;
        var secondPlay = new PlaySupportCardAction(state.Id, PlayerSide.Player, secondCardId);
        Assert.IsFalse(MatchRules.CanPlaySupportCard(state, PlayerSide.Player, secondCardId));
        Assert.IsFalse(MatchRules.TryApply(state, secondPlay));
        Assert.AreEqual(2, state.PlayerHand.Count);

        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
        var enemy = new RandomPlayerAgent(42);
        MatchAction enemyAction = null;
        enemy.RequestAction(state, action => enemyAction = action);
        Assert.IsInstanceOf<PlaySupportCardAction>(enemyAction);
        Assert.IsTrue(MatchRules.TryApply(state, enemyAction));
        Assert.IsFalse(MatchRules.TryApply(state,
            new PlaySupportCardAction(state.Id, PlayerSide.Enemy, state.EnemyHand[0].Id)));
        enemy.RequestAction(state, action => enemyAction = action);
        Assert.IsInstanceOf<EndTurnAction>(enemyAction);
        Assert.IsTrue(MatchRules.TryApply(state, enemyAction));

        Assert.IsTrue(MatchRules.CanPlaySupportCard(state, PlayerSide.Player, secondCardId));
        Assert.IsTrue(MatchRules.TryApply(state, secondPlay));
    }

    [Test]
    public void RegularSupporters_DoNotConsumeOrRequireTheHiSupporterPlay()
    {
        var effect = ScriptableObject.CreateInstance<DrawCardsSupportEffectData>();
        try
        {
            var regular = new CardDefinition("regular", "Regular", 0, 0, 0,
                CardType.Supporter, effect, new AlwaysCondition());
            var high = new CardDefinition("high", "High", 0, 0, 0,
                CardType.HiSupporter, effect, new AlwaysCondition());
            MatchState state = MatchRules.CreateMatch(new List<CardDefinition> { regular, regular, high }, card, 3, 3, 42);

            long firstRegularId = state.PlayerHand.First(candidate => candidate.Definition == regular).Id;
            long highId = state.PlayerHand.Single(candidate => candidate.Definition == high).Id;
            Assert.IsTrue(MatchRules.TryApply(state, new PlaySupportCardAction(state.Id, PlayerSide.Player, firstRegularId)));
            Assert.IsTrue(MatchRules.TryApply(state, new PlaySupportCardAction(state.Id, PlayerSide.Player, highId)));
            Assert.IsTrue(MatchRules.TryApply(state,
                new PlaySupportCardAction(state.Id, PlayerSide.Player, state.PlayerHand[0].Id)));
        }
        finally
        {
            Object.DestroyImmediate(effect);
        }
    }

    [Test]
    public void PlaceCard_ConsumesCardAndOneSummon()
    {
        MatchState state = MatchRules.CreateMatch(card, 2, 1, 42);
        MatchAction action = new PlaceCardAction(state.Id, PlayerSide.Player, state.PlayerHand[0].Id, 1, 1);

        bool applied = MatchRules.TryApply(state, action);

        Assert.IsTrue(applied);
        Assert.AreEqual(PlayerSide.Player, state.GetCardAt(1, 1).Owner);
        Assert.AreEqual(0, state.PlayerHand.Count);
        Assert.AreEqual(2, state.PlayerSummonsRemaining);
    }

    [Test]
    public void TurnHistory_RecordsAcceptedActionsInTheTurnThatTookThem()
    {
        MatchState state = MatchRules.CreateMatch(card, 1, 1, 42);
        var placement = new PlaceCardAction(state.Id, PlayerSide.Player, state.PlayerHand[0].Id, 0, 0);
        var playerEnd = new EndTurnAction(state.Id, PlayerSide.Player);
        var enemyEnd = new EndTurnAction(state.Id, PlayerSide.Enemy);

        Assert.AreEqual(1, state.TurnNumber);
        Assert.AreEqual(0, state.CompletedTurnCount);
        Assert.IsTrue(MatchRules.TryApply(state, placement));
        Assert.IsFalse(MatchRules.TryApply(state, enemyEnd));
        CollectionAssert.AreEqual(new MatchAction[] { placement }, state.TurnHistory[0].Actions);

        Assert.IsTrue(MatchRules.TryApply(state, playerEnd));
        Assert.IsTrue(MatchRules.TryApply(state, enemyEnd));

        Assert.AreEqual(3, state.TurnNumber);
        Assert.AreEqual(2, state.CompletedTurnCount);
        CollectionAssert.AreEqual(new[] { PlayerSide.Player, PlayerSide.Enemy, PlayerSide.Player },
            state.TurnHistory.Select(turn => turn.Actor));
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, state.TurnHistory.Select(turn => turn.Number));
        CollectionAssert.AreEqual(new MatchAction[] { placement, playerEnd }, state.TurnHistory[0].Actions);
        CollectionAssert.AreEqual(new MatchAction[] { enemyEnd }, state.TurnHistory[1].Actions);
        Assert.IsEmpty(state.TurnHistory[2].Actions);
        Assert.IsTrue(state.TurnHistory[1].IsComplete);
        Assert.IsFalse(state.TurnHistory[2].IsComplete);
    }

    [Test]
    public void TurnHistory_KeepsTheActionThatEndsTheMatch()
    {
        MatchState state = MatchRules.CreateMatch(card, 1, 1, 42);
        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Player, state.PlayerHand[0].Id, 0, 0)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
        var finalPlacement = new PlaceCardAction(state.Id, PlayerSide.Enemy, state.EnemyHand[0].Id, 1, 0);

        Assert.IsTrue(MatchRules.TryApply(state, finalPlacement));

        Assert.AreEqual(TurnState.GameOver, state.Turn);
        Assert.AreEqual(2, state.TurnNumber);
        Assert.AreEqual(2, state.CompletedTurnCount);
        Assert.IsTrue(state.TurnHistory[1].IsComplete);
        CollectionAssert.AreEqual(new MatchAction[] { finalPlacement }, state.TurnHistory[1].Actions);
    }

    [Test]
    public void ChooseMonsterEffect_ReturnsMonsterToDeckAndDrawsTwoCards()
    {
        CardDefinition effectCard = new CardDefinition(
            "gunner", "Gunner", 3, 1, 4, CardType.Monster,
            new[]
            {
                new MonsterEffectEntry(MonsterEffectType.PlacedDown, new ReturnToDeckDrawTestEffect(1)),
                new MonsterEffectEntry(MonsterEffectType.ChooseToTrigger, new ReturnToDeckDrawTestEffect(2))
            },
            System.Array.Empty<CardDirection>());
        MatchState state = MatchRules.CreateMatch(
            new List<CardDefinition> { effectCard, effectCard, effectCard, effectCard, effectCard },
            new List<CardDefinition> { testCard },
            5, 1, 42);
        CardInstance gunner = state.PlayerHand[0];
        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Player, gunner.Id, 1, 1)));
        int deckCountBefore = state.PlayerDeck.Count;
        int handCountBefore = state.PlayerHand.Count;

        bool applied = MatchRules.TryApply(state,
            new ActivateMonsterEffectAction(state.Id, PlayerSide.Player, gunner.Id));

        Assert.IsTrue(applied);
        Assert.IsNull(state.GetCardAt(1, 1));
        Assert.AreEqual(deckCountBefore + 1 - 2, state.PlayerDeck.Count);
        Assert.AreEqual(handCountBefore + 2, state.PlayerHand.Count);
        Assert.Contains(gunner, state.PlayerDeck.ToArray());
        Assert.IsInstanceOf<ActivateMonsterEffectAction>(state.TurnHistory[0].Actions[1]);
    }

    [Test]
    public void MonsterCardData_PreservesEachEffectAndActivationType()
    {
        MonsterCardData cardData = ScriptableObject.CreateInstance<MonsterCardData>();
        MonsterEffectData first = ScriptableObject.CreateInstance<TestMonsterEffectData>();
        MonsterEffectData second = ScriptableObject.CreateInstance<TestMonsterEffectData>();
        try
        {
            SerializedObject serializedCard = new SerializedObject(cardData);
            SerializedProperty effects = serializedCard.FindProperty("effects");
            effects.arraySize = 2;
            SerializedProperty firstEntry = effects.GetArrayElementAtIndex(0);
            firstEntry.FindPropertyRelative("activateType").intValue = (int)MonsterEffectType.PlacedDown;
            firstEntry.FindPropertyRelative("effect").objectReferenceValue = first;
            SerializedProperty secondEntry = effects.GetArrayElementAtIndex(1);
            secondEntry.FindPropertyRelative("activateType").intValue = (int)MonsterEffectType.ChooseToTrigger;
            secondEntry.FindPropertyRelative("effect").objectReferenceValue = second;
            serializedCard.ApplyModifiedPropertiesWithoutUndo();

            IReadOnlyList<MonsterEffectEntry> definitionEffects = cardData.ToDefinition().MonsterEffects;

            Assert.AreEqual(2, definitionEffects.Count);
            Assert.AreEqual(MonsterEffectType.PlacedDown, definitionEffects[0].ActivateType);
            Assert.AreSame(first, definitionEffects[0].Effect);
            Assert.AreEqual(MonsterEffectType.ChooseToTrigger, definitionEffects[1].ActivateType);
            Assert.AreSame(second, definitionEffects[1].Effect);
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(cardData);
        }
    }

    private sealed class TestMonsterEffectData : MonsterEffectData
    {
        public override string Description => "Test effect";

        public override void Resolve(MonsterEffectContext context)
        {
            Assert.Fail("The effect should not resolve when building a card definition.");
        }
    }

    [Test]
    public void BoardSlot_ShowsWiredEffectButtonOnlyDuringAnEligiblePlayerTurn()
    {
        MonsterCardData cardData = AssetDatabase.LoadAssetAtPath<MonsterCardData>(
            "Assets/_Data/_Monster/CardGunner.asset");
        Assert.IsNotNull(cardData);
        CardDefinition effectCard = cardData.ToDefinition();
        Assert.AreEqual(MonsterEffectType.ChooseToTrigger, effectCard.MonsterEffects.Single().ActivateType);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/BoardSlot.prefab");
        GameObject slotObject = Object.Instantiate(prefab);
        try
        {
            // BoardSlot lives in Assembly-CSharp, which the test assembly cannot reference directly.
            Component slot = slotObject.GetComponent("BoardSlot");
            SerializedProperty buttonProperty = new SerializedObject(slot).FindProperty("monsterEffectButton");
            Assert.IsNotNull(buttonProperty);
            Button button = buttonProperty.objectReferenceValue as Button;
            Assert.IsNotNull(button);
            Assert.AreSame(slot, button.onClick.GetPersistentTarget(0));
            Assert.AreEqual("OnMonsterEffectButtonClicked", button.onClick.GetPersistentMethodName(0));

            var view = new SerializedObject(slot);
            var coordinates = (Behaviour)view.FindProperty("coordinatesLabel").objectReferenceValue;
            var stats = (Behaviour)view.FindProperty("statsText").objectReferenceValue;
            int objectCount = slotObject.GetComponentsInChildren<Transform>(true).Length;
            int componentCount = slotObject.GetComponentsInChildren<Component>(true).Length;
            slot.GetType().GetMethod("Init").Invoke(slot, new object[] { 0, 0 });
            Assert.IsFalse(button.gameObject.activeSelf);
            Assert.IsTrue(coordinates.enabled);
            Assert.IsFalse(stats.enabled);
            Assert.AreEqual(objectCount, slotObject.GetComponentsInChildren<Transform>(true).Length);
            Assert.AreEqual(componentCount, slotObject.GetComponentsInChildren<Component>(true).Length);

            MatchState state = MatchRules.CreateMatch(
                new List<CardDefinition> { effectCard, testCard },
                new List<CardDefinition> { effectCard }, 2, 2, 42);
            CardInstance monster = state.PlayerHand.Single(candidate => candidate.Definition == effectCard);
            Assert.IsTrue(MatchRules.TryApply(state, new PlaceCardAction(state.Id, PlayerSide.Player, monster.Id, 0, 0)));

            var placeCard = slot.GetType().GetMethod("PlaceCard");
            placeCard.Invoke(slot, new object[] { monster, state });
            Assert.IsTrue(button.gameObject.activeSelf);
            Assert.IsTrue(button.interactable);
            Assert.IsFalse(coordinates.enabled);
            Assert.IsTrue(stats.enabled);

            Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
            placeCard.Invoke(slot, new object[] { monster, state });
            Assert.IsFalse(button.gameObject.activeSelf);

            Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Enemy)));
            placeCard.Invoke(slot, new object[] { monster, state });
            Assert.IsTrue(button.gameObject.activeSelf);
            Assert.IsTrue(MatchRules.TryApply(state, new ActivateMonsterEffectAction(state.Id, PlayerSide.Player, monster.Id)));
            Assert.IsNull(state.GetCardAt(0, 0));
            slot.GetType().GetMethod("ClearSlot").Invoke(slot, null);
            Assert.IsFalse(button.gameObject.activeSelf);
            Assert.IsTrue(coordinates.enabled);
            Assert.IsFalse(stats.enabled);

            Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
            CardInstance enemyMonster = state.EnemyHand[0];
            Assert.IsTrue(MatchRules.TryApply(state, new PlaceCardAction(state.Id, PlayerSide.Enemy, enemyMonster.Id, 0, 0)));
            Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Enemy)));
            placeCard.Invoke(slot, new object[] { enemyMonster, state });
            Assert.IsFalse(button.gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(slotObject);
        }
    }

    [Test]
    public void MatchScene_UsesWiredHudAndHandCardPrefabs()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = SceneManager.GetSceneByPath(scenePath);
        bool openedForTest = !scene.isLoaded;
        if (openedForTest) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            var manager = scene.GetRootGameObjects()
                .Select(root => root.GetComponent("MatchManager"))
                .Single(component => component != null);
            var hud = (Component)new SerializedObject(manager).FindProperty("handUI").objectReferenceValue;
            Assert.IsNotNull(hud);
            Assert.AreEqual("Assets/_Prefabs/MatchHUD.prefab",
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(hud));

            var view = new SerializedObject(hud);
            foreach (string field in new[]
            {
                "handContent", "titleRect", "playerPanel", "enemyPanel", "tray", "viewportRect", "divider",
                "turnLabel", "promptLabel", "playerDeckLabel", "enemyDeckLabel", "handLabel", "endTurnLabel",
                "endTurnButton", "cardPreview"
            })
                Assert.IsNotNull(view.FindProperty(field).objectReferenceValue, field);

            var content = (RectTransform)view.FindProperty("handContent").objectReferenceValue;
            Assert.AreEqual(0, content.childCount);
            Assert.AreSame(content, content.GetComponentInParent<ScrollRect>().content);
            var preview = (Component)view.FindProperty("cardPreview").objectReferenceValue;
            Assert.IsFalse(preview.gameObject.activeSelf);
            Assert.AreSame(hud.transform.parent, preview.transform.parent);
            Assert.Greater(preview.transform.GetSiblingIndex(), hud.transform.GetSiblingIndex());

            foreach (string field in new[] { "monsterCardPrefab", "supportCardPrefab" })
            {
                var card = (Component)view.FindProperty(field).objectReferenceValue;
                Assert.IsNotNull(card, field);
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabAsset(card));
                var cardView = new SerializedObject(card);
                foreach (string reference in new[] { "frame", "surface", "artwork", "title", "details", "button", "holdPreview" })
                    Assert.IsNotNull(cardView.FindProperty(reference).objectReferenceValue, reference);
            }
        }
        finally
        {
            if (openedForTest) EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void CardPreviewPrefab_HasAllDisplayReferences()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/CardPreview.prefab");
        Assert.IsNotNull(prefab);
        Component view = prefab.GetComponent("CardPreviewView");
        Assert.IsNotNull(view);
        SerializedObject serializedView = new SerializedObject(view);
        foreach (string field in new[] { "frame", "artwork", "title", "stats", "descriptionLabel" })
            Assert.IsNotNull(serializedView.FindProperty(field).objectReferenceValue, field);

        SerializedProperty arrows = serializedView.FindProperty("arrows");
        Assert.AreEqual(8, arrows.arraySize);
        for (int i = 0; i < arrows.arraySize; i++)
            Assert.IsNotNull(arrows.GetArrayElementAtIndex(i).objectReferenceValue);
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void CardHoldPreview_OpensOnlyForACompletedHold(bool holdElapsed, bool dragging)
    {
        GameObject cardObject = new GameObject("Preview Source");
        try
        {
            var previewType = System.Type.GetType("CardHoldPreview, Assembly-CSharp", true);
            Component preview = cardObject.AddComponent(previewType);
            bool shown = false;
            previewType.GetProperty("Show").SetValue(preview, new System.Func<bool>(() => shown = true));
            previewType.GetProperty("Hide").SetValue(preview, new System.Action(() => shown = false));
            PointerEventData pointer = new PointerEventData(null)
            {
                eligibleForClick = true,
                button = PointerEventData.InputButton.Left,
                dragging = dragging
            };
            ((IPointerDownHandler)preview).OnPointerDown(pointer);
            var privateInstance = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            previewType.GetField("previewTime", privateInstance).SetValue(preview,
                holdElapsed ? float.NegativeInfinity : float.PositiveInfinity);
            previewType.GetMethod("Update", privateInstance).Invoke(preview, null);

            bool shouldShow = holdElapsed && !dragging;
            Assert.AreEqual(shouldShow, shown);
            ((IPointerUpHandler)preview).OnPointerUp(pointer);
            Assert.IsFalse(shown);
            Assert.AreEqual(!shouldShow, pointer.eligibleForClick);
        }
        finally
        {
            Object.DestroyImmediate(cardObject);
        }
    }

    private sealed class ReturnToDeckDrawTestEffect : IMonsterEffect
    {
        private readonly int cardsToDraw;

        public string Description => "Test effect";

        public ReturnToDeckDrawTestEffect(int cardsToDraw)
        {
            this.cardsToDraw = cardsToDraw;
        }

        public void Resolve(MonsterEffectContext context)
        {
            Assert.AreEqual(1, context.TurnHistory[0].Actions.Count);
            Assert.IsInstanceOf<PlaceCardAction>(context.TurnHistory[0].Actions[0]);
            context.AddOperation(new ReturnMonsterToDeckAndDrawOperation(cardsToDraw));
        }
    }

    [Test]
    public void CreateMatch_UsesSeparateDeckDefinitions()
    {
        List<CardDefinition> playerCards = new List<CardDefinition>
        {
            new CardDefinition("player", "Player", 2, 0, 3, CardType.Monster)
        };
        List<CardDefinition> enemyCards = new List<CardDefinition>
        {
            new CardDefinition("enemy", "Enemy", 4, 0, 5, CardType.Monster)
        };

        MatchState state = MatchRules.CreateMatch(playerCards, enemyCards, 2, 1, 42);

        Assert.AreEqual("Player", state.PlayerHand[0].Definition.Name);
        Assert.AreEqual("Enemy", state.EnemyHand[0].Definition.Name);
        Assert.AreNotEqual(state.PlayerHand[0].Definition, state.EnemyHand[0].Definition);
    }

    [Test]
    public void InvalidPlacement_DoesNotChangeState()
    {
        MatchState state = MatchRules.CreateMatch(card, 2, 1, 42);

        bool applied = MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Player, state.PlayerHand[0].Id, -1, 0));

        Assert.IsFalse(applied);
        Assert.AreEqual(1, state.PlayerHand.Count);
        Assert.AreEqual(3, state.PlayerSummonsRemaining);
    }

    [Test]
    public void ActionFromAnotherMatch_IsRejected()
    {
        MatchState source = MatchRules.CreateMatch(card, 2, 1, 42);
        MatchState target = MatchRules.CreateMatch(card, 2, 1, 42);
        MatchAction staleAction = new PlaceCardAction(source.Id, PlayerSide.Player, source.PlayerHand[0].Id, 1, 1);

        bool applied = MatchRules.TryApply(target, staleAction);

        Assert.IsFalse(applied);
        Assert.IsNull(target.GetCardAt(1, 1));
        Assert.AreEqual(1, target.PlayerHand.Count);
    }

    [Test]
    public void DrawFromEmptyDeck_DoesNotFailOrChangeHand()
    {
        MatchState state = MatchRules.CreateMatch(card, 1, 1, 42);
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Enemy)));

        Assert.AreEqual(0, state.PlayerDeck.Count);
        Assert.AreEqual(1, state.PlayerHand.Count);
    }

    [Test]
    public void PlacementOnLastCell_EndsMatch()
    {
        MatchState state = MatchRules.CreateMatch(card, 16, 1, 42);
        for (int i = 0; i < 15; i++)
        {
            Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
            Assert.IsTrue(MatchRules.TryApply(state,
                new PlaceCardAction(state.Id, PlayerSide.Enemy, state.EnemyHand[0].Id, i % MatchState.Width, i / MatchState.Width)));
            Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Enemy)));
        }

        bool applied = MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Player, state.PlayerHand[0].Id, 3, 3));

        Assert.IsTrue(applied);
        Assert.AreEqual(TurnState.GameOver, state.Turn);
        Assert.IsFalse(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
    }

    [Test]
    public void RandomPlayerAgent_ProducesActionForAvailableCardAndCell()
    {
        MatchState state = MatchRules.CreateMatch(card, 2, 1, 42);
        IPlayerAgent agent = new RandomPlayerAgent(42);
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));

        MatchAction action = null;
        agent.RequestAction(state, submittedAction => action = submittedAction);

        Assert.IsInstanceOf<PlaceCardAction>(action);
        Assert.IsTrue(MatchRules.TryApply(state, action));
    }

    [Test]
    public void RandomPlayerAgent_AttacksWithEveryEnemyMonsterThatHasATarget()
    {
        List<CardDefinition> cards = new List<CardDefinition>
        {
            new CardDefinition("test", "Test", 1, 0, 3, CardType.Monster,
                new CardDirection(1, 0))
        };
        MatchState state = MatchRules.CreateMatch(cards, 4, 2, 42);

        CardInstance playerCard = state.PlayerHand[0];
        CardInstance secondPlayerCard = state.PlayerHand[1];
        CardInstance firstEnemyCard = state.EnemyHand[0];
        CardInstance secondEnemyCard = state.EnemyHand[1];
        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Player, playerCard.Id, 1, 1)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Enemy, firstEnemyCard.Id, 0, 1)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Enemy)));

        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Player, secondPlayerCard.Id, 1, 0)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Enemy, secondEnemyCard.Id, 0, 0)));

        IPlayerAgent agent = new RandomPlayerAgent(42);
        MatchAction firstAction = null;
        agent.RequestAction(state, submittedAction => firstAction = submittedAction);
        Assert.IsInstanceOf<AttackCardAction>(firstAction);
        Assert.IsTrue(MatchRules.TryApply(state, firstAction));

        MatchAction secondAction = null;
        agent.RequestAction(state, submittedAction => secondAction = submittedAction);
        Assert.IsInstanceOf<AttackCardAction>(secondAction);
        Assert.IsTrue(MatchRules.TryApply(state, secondAction));
        Assert.AreEqual(2, state.GetCardAt(1, 0).CurrentHealth);
        Assert.AreEqual(2, state.GetCardAt(1, 1).CurrentHealth);
    }

    [Test]
    public void Attack_RequiresConfiguredDirectionAndDamagesAdjacentEnemy()
    {
        MatchState state = CreateAttackState(out long attackerId);
        CardInstance target = state.GetCardAt(2, 1);

        bool wrongDirection = MatchRules.TryApply(state,
            new AttackCardAction(state.Id, PlayerSide.Player, attackerId, -1, 0));
        bool attacked = MatchRules.TryApply(state,
            new AttackCardAction(state.Id, PlayerSide.Player, attackerId, 1, 0));

        Assert.IsFalse(wrongDirection);
        Assert.IsTrue(attacked);
        Assert.AreEqual(1, target.CurrentHealth);
        Assert.AreEqual(2, state.GetCardAt(1, 1).CurrentHealth);
        Assert.AreEqual(0, state.GetCardAt(1, 1).RemainAttacks);
    }

    [Test]
    public void DefeatedMonsterKeepsCardOwnerWhileCellOwnershipChanges()
    {
        List<CardDefinition> playerCards = new List<CardDefinition>
        {
            new CardDefinition("player", "Player", 3, 0, 3, CardType.Monster,
                new CardDirection(1, 0))
        };
        List<CardDefinition> enemyCards = new List<CardDefinition>
        {
            new CardDefinition("enemy", "Enemy", 1, 0, 3, CardType.Monster)
        };
        MatchState state = MatchRules.CreateMatch(playerCards, enemyCards, 1, 1, 73);
        CardInstance attacker = state.PlayerHand[0];
        CardInstance defender = state.EnemyHand[0];

        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Player, attacker.Id, 1, 1)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Enemy, defender.Id, 2, 1)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Enemy)));

        Assert.IsTrue(MatchRules.TryApply(state,
            new AttackCardAction(state.Id, PlayerSide.Player, attacker.Id, 1, 0)));

        Assert.AreEqual(PlayerSide.Enemy, defender.Owner);
        Assert.AreEqual(PlayerSide.Player, state.GetCellOwner(2, 1));
        Assert.AreEqual(defender, state.GetCardAt(2, 1));
    }

    [Test]
    public void Card_CanAttackOnlyOncePerTurnAndResetsNextTurn()
    {
        MatchState state = CreateAttackState(out long attackerId);
        AttackCardAction attack = new AttackCardAction(state.Id, PlayerSide.Player, attackerId, 1, 0);

        Assert.IsTrue(MatchRules.TryApply(state, attack));
        Assert.IsFalse(MatchRules.TryApply(state, attack));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Enemy)));
        Assert.AreEqual(1, state.GetCardAt(1, 1).RemainAttacks);
        Assert.IsTrue(MatchRules.TryApply(state, attack));
        Assert.IsNull(state.GetCardAt(2, 1));
    }

    [Test]
    public void Attack_RequiresAnEnemyInTheConfiguredAdjacentCell()
    {
        List<CardDefinition> cards = new();
        cards.Add(new CardDefinition("test", "test", 2, 0, 3, CardType.Monster));
        MatchState state = MatchRules.CreateMatch(cards, 4, 2, 18);
        CardInstance attacker = state.PlayerHand[0];
        CardInstance friendlyCard = state.PlayerHand[1];

        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Player, attacker.Id, 1, 1)));
        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Player, friendlyCard.Id, 2, 1)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
        CardInstance enemyCard = state.EnemyHand[0];
        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Enemy, enemyCard.Id, 0, 1)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Enemy)));

        Assert.IsFalse(MatchRules.CanAttack(state, PlayerSide.Player, attacker.Id, new CardDirection(1, 0)));
        Assert.IsFalse(MatchRules.CanAttack(state, PlayerSide.Player, attacker.Id, new CardDirection(0, 1)));
        Assert.IsFalse(MatchRules.CanAttack(state, PlayerSide.Enemy, attacker.Id, new CardDirection(1, 0)));
    }

    [Test]
    public void Attack_ReflectsDefenseWhenDefenderArrowPointsBack()
    {
        List<CardDefinition> cards = new();
        cards.Add(new CardDefinition("test", "test", 2, 0, 3, CardType.Monster));
        MatchState state = MatchRules.CreateMatch(cards, 4, 1, 51);
        CardInstance attacker = state.PlayerHand[0];
        CardInstance defender = state.EnemyHand[0];

        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Player, attacker.Id, 1, 1)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));
        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Enemy, defender.Id, 2, 1)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Enemy)));

        bool applied = MatchRules.TryApply(state,
            new AttackCardAction(state.Id, PlayerSide.Player, attacker.Id, 1, 0));

        Assert.IsTrue(applied);
        Assert.IsNull(state.GetCardAt(1, 1));
        Assert.AreEqual(1, defender.CurrentHealth);
    }

    private MatchState CreateAttackState(out long attackerId)
    {
        List<CardDefinition> cards = new();
        cards.Add(new CardDefinition("test", "test", 2, 0, 3, CardType.Monster));
        MatchState state = MatchRules.CreateMatch(cards, 4, 1, 24);
        CardInstance attacker = state.PlayerHand[0];
        attackerId = attacker.Id;

        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Player, attacker.Id, 1, 1)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Player)));

        CardInstance defender = state.EnemyHand[0];
        Assert.IsTrue(MatchRules.TryApply(state,
            new PlaceCardAction(state.Id, PlayerSide.Enemy, defender.Id, 2, 1)));
        Assert.IsTrue(MatchRules.TryApply(state, new EndTurnAction(state.Id, PlayerSide.Enemy)));
        return state;
    }
}
