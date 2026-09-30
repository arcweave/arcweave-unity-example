using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arcweave;
using Arcweave.Interpreter.INodes;
using Arcweave.Project;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Component = Arcweave.Project.Component;
using Project = Arcweave.Project.Project;
using Object = UnityEngine.Object;

public class ArcweaveDialogueTests
{
    private readonly List<Object> objects = new List<Object>();
    private ArcweaveProgressStore store;
    private MonoBehaviour player;
    private MonoBehaviour ui;
    private Text content;
    private Project project;

    [SetUp]
    public void SetUp()
    {
        store = new ArcweaveProgressStore("arcweave_dialogue_test_" + Guid.NewGuid().ToString("N"));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var instance in objects) if (instance != null) Object.Destroy(instance);
        objects.Clear();
        yield return null;
        store.Clear();
    }

    [UnityTest]
    public IEnumerator FinalElementStaysVisibleUntilCloseAndFinishesOnce()
    {
        var end = Element("end", "<p>Last line.</p>");
        CreatePlayer(end);
        CreateUI();
        var manager = CreateManager();
        var trigger = CreateTrigger();
        int finishes = 0;
        Subscribe(player, "onProjectFinish", new Action<Project>(_ => finishes++));
        Invoke(trigger, "StartDialogue");
        yield return null;

        Assert.That(content.text, Does.Contain("Last line."));
        Assert.That(content.gameObject.activeInHierarchy, Is.True);
        Assert.That(Field(manager, "currentState").ToString(), Is.EqualTo("Dialogue"));
        Assert.That(finishes, Is.Zero);
        Assert.That(store.HasSavedProgress, Is.False);
        var close = OnlyButton("Close");
        close.onClick.Invoke();
        close.onClick.Invoke();
        Assert.That(finishes, Is.EqualTo(1));
        Assert.That(store.HasSavedProgress, Is.True);
        Assert.That(Field(manager, "currentState").ToString(), Is.EqualTo("Gameplay"));
        Assert.That(ui.gameObject.activeSelf, Is.False);
        yield return null;
    }

    [UnityTest]
    public IEnumerator FinalScriptRunsBeforeRenderingAndIsNotRepeatedByRefreshOrClose()
    {
        var end = Element("end", "<pre><code>counter += 1</code></pre><p>Final line</p>");
        CreatePlayer(end);
        CreateUI();
        int observedCounter = -1;
        Subscribe(player, "onElementEnter", new Action<Element>(_ =>
            observedCounter = (int)project.GetVariable("counter").Value));
        Invoke(player, "Next", end);
        Invoke(ui, "RefreshCurrentElement");
        yield return null;

        Assert.That(observedCounter, Is.EqualTo(1));
        Assert.That(project.GetVariable("counter").Value, Is.EqualTo(1));
        OnlyButton("Close").onClick.Invoke();
        Assert.That(project.GetVariable("counter").Value, Is.EqualTo(1));
        Assert.That(store.TryRestore(project, out _), Is.True);
        Assert.That(project.GetVariable("counter").Value, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ScriptsRunWithoutAUI()
    {
        var end = Element("end", "<pre><code>counter += 1</code></pre>");
        CreatePlayer(end);
        Action close = null;
        Subscribe(player, "onWaitInputFinish", new Action<Action>(action => close = action));
        Invoke(player, "Next", end);
        Assert.That(project.GetVariable("counter").Value, Is.EqualTo(1));
        Assert.That(close, Is.Not.Null);
        close();
        yield return null;
    }

    [UnityTest]
    public IEnumerator SinglePathShowsContinueBeforeTheFinalClose()
    {
        var first = Element("first", "First line");
        var last = Element("last", "Last line");
        Connect(first, last);
        CreatePlayer(first, last);
        CreateUI();
        Invoke(player, "Next", first);
        OnlyButton("Continue").onClick.Invoke();
        yield return null;
        Assert.That(content.text, Does.Contain("Last line"));
        Assert.That(last.Visits, Is.EqualTo(1));
        OnlyButton("Close").onClick.Invoke();
    }

    [UnityTest]
    public IEnumerator ChoicesStillSelectTheRequestedPath()
    {
        var first = Element("first", "Choose");
        var left = Element("left", "Left");
        var right = Element("right", "Right");
        Connect(first, left, "Left choice");
        Connect(first, right, "Right choice");
        CreatePlayer(first, left, right);
        CreateUI();
        Invoke(player, "Next", first);
        Assert.That(Buttons().Count, Is.EqualTo(2));
        Buttons()[1].onClick.Invoke();
        yield return null;
        Assert.That(right.Visits, Is.EqualTo(1));
        Assert.That(left.Visits, Is.Zero);
        OnlyButton("Close");
    }

    [UnityTest]
    public IEnumerator EndMarkerDoesNotEvaluateOrTraverseOutgoingConnections()
    {
        var end = Element("end", "Last visible line");
        AddEndComponent(end);
        var hidden = Element("hidden", "<pre><code>counter += 100</code></pre>");
        var connection = Connect(end, hidden, "<pre><code>counter += 10</code></pre><p>Hidden option</p>");
        CreatePlayer(end, hidden);
        CreateUI();
        CreateManager();
        var trigger = CreateTrigger();
        Invoke(trigger, "StartDialogue");
        yield return null;

        OnlyButton("Close").onClick.Invoke();
        Assert.That(connection.RuntimeLabel, Is.Null, "Terminal paths must not even be evaluated.");
        Assert.That(hidden.Visits, Is.Zero);
        Assert.That(project.GetVariable("counter").Value, Is.EqualTo(0));
    }

    [UnityTest]
    public IEnumerator LegacyAttributeEndMarkerAlsoWaitsForClose()
    {
        var end = Element("end", "Legacy ending");
        var attribute = new Arcweave.Project.Attribute();
        InvokeInternal(attribute, "Set", "tag", "tag", "tag", IAttribute.DataType.StringPlainText,
            "dialogue_end", default(IAttribute.ContainerType), "end");
        end.AddAttribute(attribute);
        var hidden = Element("hidden", "Hidden");
        Connect(end, hidden);
        CreatePlayer(end, hidden);
        CreateUI();
        CreateManager();
        var trigger = CreateTrigger();
        Invoke(trigger, "StartDialogue");
        yield return null;
        OnlyButton("Close").onClick.Invoke();
        Assert.That(hidden.Visits, Is.Zero);
    }

    [UnityTest]
    public IEnumerator ExplicitComponentWorksWhenTheLegacyTagIsBlank()
    {
        var end = Element("end", "End");
        AddEndComponent(end);
        var hidden = Element("hidden", "Hidden");
        Connect(end, hidden);
        CreatePlayer(end, hidden);
        CreateUI();
        var manager = CreateManager();
        SetField(manager, "dialogueEndTag", "");
        SetField(manager, "dialogueEndComponentName", "dialogue_end");
        var trigger = CreateTrigger();
        Invoke(trigger, "StartDialogue");
        yield return null;
        OnlyButton("Close");
    }

    [UnityTest]
    public IEnumerator EndTagMustMatchTheWholeValue()
    {
        var first = Element("first", "Continue");
        var attribute = new Arcweave.Project.Attribute();
        InvokeInternal(attribute, "Set", "tag", "tag", "tag", IAttribute.DataType.StringPlainText,
            "not_dialogue_end", default(IAttribute.ContainerType), "first");
        first.AddAttribute(attribute);
        var last = Element("last", "Last");
        Connect(first, last);
        CreatePlayer(first, last);
        CreateUI();
        Invoke(player, "Next", first);
        yield return null;
        OnlyButton("Continue");
    }

    [UnityTest]
    public IEnumerator StandalonePlaybackAlsoStopsAtAnEndMarker()
    {
        var first = Element("first", "Standalone ending");
        AddEndComponent(first);
        var last = Element("last", "Last");
        Connect(first, last);
        CreatePlayer(first, last);
        CreateUI();
        Invoke(player, "Next", first);
        yield return null;
        OnlyButton("Close").onClick.Invoke();
        Assert.That(last.Visits, Is.Zero);
    }

    [UnityTest]
    public IEnumerator ContentScriptChangesConditionsBeforePathsAreEvaluated()
    {
        var first = Element("first", "<pre><code>counter = 1</code></pre><p>Choose</p>");
        var last = Element("last", "Allowed");
        var branch = new Branch();
        var condition = new Condition();
        var output = new Connection("condition-output");
        output.Set(null, condition, last);
        InvokeInternal(condition, "Set", "condition", output, "counter == 1");
        InvokeInternal(branch, "Set", "branch", Vector2Int.zero, new List<Condition> { condition }, "");
        Connect(first, branch);
        CreatePlayer(first, last, branch, condition);
        CreateUI();
        Invoke(player, "Next", first);
        yield return null;
        OnlyButton("Continue").onClick.Invoke();
        Assert.That(last.Visits, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator NoAvailableConditionalPathIsATerminal()
    {
        var first = Element("first", "No available path");
        var branch = new Branch();
        InvokeInternal(branch, "Set", "branch", Vector2Int.zero, new List<Condition>(), "");
        Connect(first, branch);
        CreatePlayer(first, branch);
        CreateUI();
        Invoke(player, "Next", first);
        yield return null;
        OnlyButton("Close");
        Assert.That(content.text, Does.Contain("No available path"));
    }

    [UnityTest]
    public IEnumerator StaleCallbacksCannotAdvanceOrCloseANewerElement()
    {
        var first = Element("first", "First");
        var last = Element("last", "Last");
        Connect(first, last);
        CreatePlayer(first, last);
        Action next = null;
        Action close = null;
        Subscribe(player, "onWaitInputNext", new Action<Action>(action => next = action));
        Subscribe(player, "onWaitInputFinish", new Action<Action>(action => close = action));
        int finishes = 0;
        Subscribe(player, "onProjectFinish", new Action<Project>(_ => finishes++));
        Invoke(player, "Next", first);
        Action oldNext = next;
        next();
        Action oldClose = close;
        oldNext();
        Assert.That(last.Visits, Is.EqualTo(1));
        Invoke(player, "Next", first);
        oldClose();
        Assert.That(finishes, Is.Zero);
        next();
        close();
        oldNext();
        close();
        Assert.That(finishes, Is.EqualTo(1));
        yield return null;
    }

    [UnityTest]
    public IEnumerator StaleOptionSelectionCannotReplayAConnection()
    {
        var first = Element("first", "Choose");
        var last = Element("last", "Last");
        Connect(first, last, "<pre><code>counter += 1</code></pre><p>Select</p>");
        CreatePlayer(first, last);
        Action<int> select = null;
        Subscribe(player, "onElementOptions", new Action<Options, Action<int>>((_, action) => select = action));
        Invoke(player, "Next", first);
        select(-1);
        select(99);
        select(0);
        select(0);
        Assert.That(project.GetVariable("counter").Value, Is.EqualTo(1));
        Assert.That(last.Visits, Is.EqualTo(1));
        yield return null;
    }

    [UnityTest]
    public IEnumerator UIAndTriggerCanBeReenabledForAnotherConversation()
    {
        var end = Element("end", "Repeatable ending");
        CreatePlayer(end);
        CreateUI();
        var manager = CreateManager();
        var trigger = CreateTrigger();
        Invoke(trigger, "StartDialogue");
        OnlyButton("Close").onClick.Invoke();
        trigger.enabled = false;
        trigger.enabled = true;
        Invoke(trigger, "StartDialogue");
        yield return null;
        Assert.That(content.gameObject.activeInHierarchy, Is.True);
        Assert.That(end.Visits, Is.EqualTo(2));
        Assert.That(Buttons().Count, Is.EqualTo(1));
        OnlyButton("Close").onClick.Invoke();
        Assert.That(Field(manager, "currentState").ToString(), Is.EqualTo("Gameplay"));
        Assert.That(Invoke(trigger, "IsInDialogue"), Is.False);
    }

    [UnityTest]
    public IEnumerator AudioResubscribesOnceAfterDisableEnable()
    {
        var end = Element("end", "Ending");
        CreatePlayer(end);
        CreateUI();
        CreateManager();
        var trigger = CreateTrigger();
        var audioObject = NewObject("Audio", false);
        var audio = AddBehaviour(audioObject, "ArcweavePlayerAudio");
        SetField(audio, "player", player);
        SetField(audio, "dialogueTrigger", trigger);
        var managerObject = NewObject("Audio manager", false);
        AddBehaviour(managerObject, "ArcweaveAudioManager");
        managerObject.SetActive(true);
        audioObject.SetActive(true);
        audio.enabled = false;
        audio.enabled = true;
        var handlers = (Delegate)Field(player, "onElementEnter");
        Assert.That(handlers.GetInvocationList().Count(handler => ReferenceEquals(handler.Target, audio)), Is.EqualTo(1));
        Invoke(trigger, "StartDialogue");
        Assert.That(Field(audio, "currentElement"), Is.SameAs(end));
        Assert.That(Invoke(trigger, "IsInDialogue"), Is.True);
        OnlyButton("Close").onClick.Invoke();
        Assert.That(Invoke(trigger, "IsInDialogue"), Is.False);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ImmediateFinishListenerDoesNotLeaveAStaleCloseButton()
    {
        var end = Element("end", "End");
        CreatePlayer(end);
        Subscribe(player, "onWaitInputFinish", new Action<Action>(finish => finish()));
        CreateUI();
        Invoke(player, "Next", end);
        yield return null;
        Assert.That(Buttons(), Is.Empty);
    }

    [UnityTest]
    public IEnumerator ImmediateContinueListenerDoesNotLeaveThePreviousButton()
    {
        var first = Element("first", "First");
        var last = Element("last", "Last");
        Connect(first, last);
        CreatePlayer(first, last);
        Subscribe(player, "onWaitInputNext", new Action<Action>(next => next()));
        CreateUI();
        Invoke(player, "Next", first);
        yield return null;
        Assert.That(content.text, Does.Contain("Last"));
        OnlyButton("Close");
    }

    [UnityTest]
    public IEnumerator ImmediateOptionListenerDoesNotLeaveOldOptions()
    {
        var first = Element("first", "Choose");
        var last = Element("last", "Last");
        Connect(first, last, "Choose this");
        CreatePlayer(first, last);
        Subscribe(player, "onElementOptions", new Action<Options, Action<int>>((_, select) => select(0)));
        CreateUI();
        Invoke(player, "Next", first);
        yield return null;
        Assert.That(content.text, Does.Contain("Last"));
        OnlyButton("Close");
    }

    [UnityTest]
    public IEnumerator ImmediateElementListenerDoesNotRenderOldContent()
    {
        var first = Element("first", "Old");
        var last = Element("last", "Current");
        CreatePlayer(first, last);
        Subscribe(player, "onElementEnter", new Action<Element>(element =>
        {
            if (element == first) Invoke(player, "Next", last);
        }));
        CreateUI();
        Invoke(player, "Next", first);
        yield return null;
        Assert.That(content.text, Does.Contain("Current"));
        OnlyButton("Close");
    }

    [UnityTest]
    public IEnumerator FinishListenerCanStartANewConversationWithoutClearingItsUI()
    {
        var first = Element("first", "Old ending");
        var next = Element("next", "New conversation");
        CreatePlayer(first, next);
        Subscribe(player, "onProjectFinish", new Action<Project>(_ => Invoke(player, "Next", next)));
        CreateUI();
        Invoke(player, "Next", first);
        OnlyButton("Close").onClick.Invoke();
        yield return null;
        Assert.That(content.text, Does.Contain("New conversation"));
        OnlyButton("Close");
    }

    private void CreatePlayer(params INode[] nodes)
    {
        var start = nodes.OfType<Element>().First();
        var board = new Board("board", "Test Board", "test", new List<INode>(nodes));
        var components = nodes.OfType<Element>().SelectMany(element => element.Components).Distinct().ToList();
        project = new Project("Dialogue tests", start, new List<Board> { board }, components,
            new List<Variable> { new Variable("counter", "counter", 0) });
        var asset = ScriptableObject.CreateInstance<ArcweaveProjectAsset>();
        objects.Add(asset);
        typeof(ArcweaveProjectAsset).GetProperty("Project").SetValue(asset, project);
        var playerObject = NewObject("Player", false);
        playerObject.tag = "Player";
        player = AddBehaviour(playerObject, "Arcweave.ArcweavePlayer");
        SetField(player, "aw", asset);
        SetField(player, "autoStart", false);
        SetField(player, "progressStore", store);
        Invoke(player, "EnsureInitialized");
        playerObject.SetActive(true);
    }

    private void CreateUI()
    {
        var root = NewObject("Dialogue UI", false);
        root.AddComponent<Canvas>();
        ui = AddBehaviour(root, "Arcweave.ArcweavePlayerUI");
        SetField(ui, "player", player);
        SetField(ui, "animateTextEntries", false);
        SetField(ui, "showVariables", false);
        content = CreateText("Content", root.transform);
        SetField(ui, "content", content);
        var buttonObject = new GameObject("Button template", typeof(RectTransform));
        buttonObject.transform.SetParent(root.transform, false);
        var button = buttonObject.AddComponent<Button>();
        CreateText("Label", button.transform);
        SetField(ui, "buttonTemplate", button);
        root.SetActive(true);
    }

    private MonoBehaviour CreateManager()
    {
        var root = NewObject("Game manager", false);
        var manager = AddBehaviour(root, "GameManager");
        SetField(manager, "arcweaveUI", ui.gameObject);
        root.SetActive(true);
        return manager;
    }

    private MonoBehaviour CreateTrigger()
    {
        var root = NewObject("NPC", false);
        var trigger = AddBehaviour(root, "DialogueTrigger");
        SetField(trigger, "arcweavePlayer", player);
        SetField(trigger, "specificBoardName", "Test Board");
        SetField(trigger, "controlPlayerAnimator", false);
        root.SetActive(true);
        return trigger;
    }

    private GameObject NewObject(string name, bool active)
    {
        var result = new GameObject(name);
        result.SetActive(active);
        objects.Add(result);
        return result;
    }

    private static Text CreateText(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        var text = gameObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return text;
    }

    private List<Button> Buttons() => (List<Button>)Field(ui, "tempButtons");

    private Button OnlyButton(string label)
    {
        Assert.That(Buttons().Count, Is.EqualTo(1));
        var button = Buttons()[0];
        Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo(label));
        Assert.That(button.gameObject.activeInHierarchy, Is.True);
        return button;
    }

    private static Element Element(string id, string text)
    {
        var element = new Element();
        if (!text.StartsWith("<")) text = "<p>" + text + "</p>";
        InvokeInternal(element, "Set", id, Vector2Int.zero, new List<Connection>(), id, text,
            new List<Component>(), new List<Arcweave.Project.Attribute>(), null, "", Array.Empty<AudioAsset>());
        return element;
    }

    private static Connection Connect(Element source, INode target, string label = null)
    {
        var connection = new Connection(Guid.NewGuid().ToString());
        if (label != null && !label.StartsWith("<")) label = "<p>" + label + "</p>";
        connection.Set(label, source, target);
        source.Outputs.Add(connection);
        return connection;
    }

    private static void AddEndComponent(Element element)
    {
        var component = new Component();
        component.Set("end-component", "end", "dialogue_end", new List<Arcweave.Project.Attribute>(), null);
        element.Components.Add(component);
    }

    // Demo scripts stay in Unity's predefined assembly; only the test boundary uses reflection.
    private static MonoBehaviour AddBehaviour(GameObject root, string type) =>
        (MonoBehaviour)root.AddComponent(Type.GetType(type + ", Assembly-CSharp", true));
    private static object Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(target);
    private static void SetField(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(target, value);
    private static object Invoke(object target, string name, params object[] args) => target.GetType()
        .GetMethod(name, BindingFlags.Instance | BindingFlags.Public).Invoke(target, args);
    private static object InvokeInternal(object target, string name, params object[] args) => target.GetType()
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static void Subscribe(object target, string name, Delegate callback)
    {
        var eventInfo = target.GetType().GetEvent(name);
        eventInfo.AddEventHandler(target, Delegate.CreateDelegate(eventInfo.EventHandlerType, callback.Target, callback.Method));
    }
}
