using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Arcweave;
using Arcweave.Interpreter.INodes;
using Arcweave.Project;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Component = Arcweave.Project.Component;
using Project = Arcweave.Project.Project;
using Object = UnityEngine.Object;

public class ArcweaveProgressTests
{
    private string key;
    private ArcweaveProgressStore store;
    private readonly List<Object> objects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        key = "arcweave_progress_test_" + Guid.NewGuid().ToString("N");
        store = new ArcweaveProgressStore(key);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var instance in objects) Object.DestroyImmediate(instance);
        objects.Clear();
        store.Clear();
        PlayerPrefs.DeleteKey(key + "_unrelated");
        PlayerPrefs.Save();
    }

    [Test]
    public void RoundTripRestoresAllScopesAndPrimitiveTypes()
    {
        var project = CreateProject("board", "start",
            new Variable("gold", "gold", 10), new Variable("ratio", "ratio", 0.5d),
            new Variable("flag", "flag", false), new Variable("name", "name", "default"));
        var local = new Variable("local", "gold", 20);
        project.Boards[0].AddVariable(local);
        var component = new Component();
        component.Set("character", "character", "Character", new List<Arcweave.Project.Attribute>(), null);
        var componentVariable = new Variable("component", "gold", 30);
        component.AddVariable(componentVariable);
        project.Components.Add(component);
        project.Initialize();
        project.GetVariable("gold").Value = 100;
        project.GetVariable("ratio").Value = 1.25d;
        project.GetVariable("flag").Value = true;
        project.GetVariable("name").Value = "saved";
        local.Value = 200;
        componentVariable.Value = 300;
        store.Save(project, "start");
        project.Initialize();

        Assert.That(store.TryRestore(project, out string element), Is.True);
        Assert.That(element, Is.EqualTo("start"));
        Assert.That(project.GetVariable("gold").Value, Is.EqualTo(100));
        Assert.That(project.GetVariable("ratio").Value, Is.EqualTo(1.25d));
        Assert.That(project.GetVariable("flag").Value, Is.True);
        Assert.That(project.GetVariable("name").Value, Is.EqualTo("saved"));
        Assert.That(local.Value, Is.EqualTo(200));
        Assert.That(componentVariable.Value, Is.EqualTo(300));
    }

    [Test]
    public void SchemaChangesPreserveCompatibleValuesAndNormalizeTheSave()
    {
        var original = CreateProject("board", "start", new Variable("keep", "old name", 1),
            new Variable("remove", "remove", true), new Variable("change", "change", 5));
        original.GlobalVariables[0].Value = 42;
        store.Save(original, "start");
        var updated = CreateProject("board", "start", new Variable("keep", "renamed", 9),
            new Variable("add", "add", 8), new Variable("change", "change", "new type"));
        updated.GlobalVariables[1].Value = 999;

        Assert.That(store.TryRestore(updated, out _), Is.True);
        Assert.That(updated.GlobalVariables[0].Value, Is.EqualTo(42));
        Assert.That(updated.GlobalVariables[1].Value, Is.EqualTo(8));
        Assert.That(updated.GlobalVariables[2].Value, Is.EqualTo("new type"));
        Assert.That(PlayerPrefs.GetString(key + "_progress"), Does.Not.Contain("remove"));
    }

    [Test]
    public void MissingSavedElementStillRestoresVariables()
    {
        var old = CreateProject("board", "deleted", new Variable("value", "value", 1));
        old.GlobalVariables[0].Value = 7;
        store.Save(old, "deleted");
        var updated = CreateProject("board", "new-start", new Variable("value", "value", 1));

        Assert.That(store.TryRestore(updated, out string element), Is.True);
        Assert.That(element, Is.Null);
        Assert.That(updated.GlobalVariables[0].Value, Is.EqualTo(7));
    }

    [Test]
    public void SharedBoardSurvivesRenamesAndBoardAdditionsOrRemovals()
    {
        var original = CreateProject("kept", "start", new Variable("value", "value", 1));
        original.Boards.Add(new Board("removed", "Removed", "removed", new List<INode>()));
        original.GlobalVariables[0].Value = 7;
        store.Save(original, "start");
        var updated = CreateProject("kept", "start", new Variable("value", "value", 1));
        typeof(Project).GetField("<name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(updated, "Renamed project");
        updated.Boards.Insert(0, new Board("added", "Added", "added", new List<INode>()));

        Assert.That(store.TryRestore(updated, out _), Is.True);
        Assert.That(updated.GlobalVariables[0].Value, Is.EqualTo(7));
    }

    [Test]
    public void DifferentProjectIsNotRestoredEvenWithMatchingNamesAndVariableIds()
    {
        var first = CreateProject("first", "start", new Variable("value", "value", 1));
        first.GlobalVariables[0].Value = 9;
        store.Save(first, "start");
        string before = PlayerPrefs.GetString(key + "_progress");
        var second = CreateProject("second", "start", new Variable("value", "value", 2));

        Assert.That(store.TryRestore(second, out _), Is.False);
        Assert.That(second.GlobalVariables[0].Value, Is.EqualTo(2));
        Assert.That(PlayerPrefs.GetString(key + "_progress"), Is.EqualTo(before));
    }

    [Test]
    public void LegacySaveMigratesOnlyCompatibleIdsAndDeletesLegacyKeys()
    {
        var old = CreateProject("board", "deleted", new Variable("keep", "keep", 1),
            new Variable("removed", "removed", 2));
        old.GlobalVariables[0].Value = 12;
        PlayerPrefs.SetString(key + "_variables", old.SaveVariables());
        PlayerPrefs.SetString(key + "_currentElement", "deleted");
        var updated = CreateProject("board", "start", new Variable("keep", "keep", 1));

        Assert.That(store.TryRestore(updated, out string element), Is.True);
        Assert.That(element, Is.Null);
        Assert.That(updated.GlobalVariables[0].Value, Is.EqualTo(12));
        Assert.That(PlayerPrefs.HasKey(key + "_progress"), Is.True);
        Assert.That(PlayerPrefs.HasKey(key + "_variables"), Is.False);
        Assert.That(PlayerPrefs.HasKey(key + "_currentElement"), Is.False);
    }

    [Test]
    public void LegacySaveWithOnlyAKnownElementCanMigrate()
    {
        PlayerPrefs.SetString(key + "_variables", "{\"variables\":[]}");
        PlayerPrefs.SetString(key + "_currentElement", "start");
        Assert.That(store.TryRestore(CreateProject("board", "start"), out string element), Is.True);
        Assert.That(element, Is.EqualTo("start"));
    }

    [Test]
    public void UnrelatedLegacySaveIsLeftUntouched()
    {
        string legacy = "{\"variables\":[{\"id\":\"other\",\"type\":\"System.Int32\",\"value\":\"4\"}]}";
        PlayerPrefs.SetString(key + "_variables", legacy);
        PlayerPrefs.SetString(key + "_currentElement", "other-node");
        Assert.That(store.TryRestore(CreateProject("board", "start"), out _), Is.False);
        Assert.That(PlayerPrefs.GetString(key + "_variables"), Is.EqualTo(legacy));
        Assert.That(PlayerPrefs.HasKey(key + "_progress"), Is.False);
    }

    [TestCase("{broken")]
    [TestCase("")]
    [TestCase("null")]
    [TestCase("{}")]
    public void UnreadableSaveDoesNotThrowOrChangeLiveValues(string json)
    {
        var project = CreateProject("board", "start", new Variable("value", "value", 1));
        project.GlobalVariables[0].Value = 99;
        PlayerPrefs.SetString(key + "_progress", json);
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Ignoring .*Arcweave saved progress.*"));
        Assert.That(store.TryRestore(project, out _), Is.False);
        Assert.That(project.GlobalVariables[0].Value, Is.EqualTo(99));
        Assert.That(PlayerPrefs.GetString(key + "_progress"), Is.EqualTo(json));
    }

    [Test]
    public void UnsupportedVersionIsLeftUntouched()
    {
        var project = CreateProject("board", "start");
        string json = "{\"version\":99,\"boardIds\":[\"board\"],\"variables\":[]}";
        PlayerPrefs.SetString(key + "_progress", json);
        Assert.That(store.TryRestore(project, out _), Is.False);
        Assert.That(PlayerPrefs.GetString(key + "_progress"), Is.EqualTo(json));
    }

    [TestCase("System.Int32", "not-a-number")]
    [TestCase("System.Int32", "999999999999999999999")]
    [TestCase("System.Boolean", "maybe")]
    [TestCase("System.Double", "NaN")]
    [TestCase("System.Double", "Infinity")]
    [TestCase("System.Double", "1,5")]
    public void InvalidPrimitiveUsesDefaultWithoutLosingOtherVariables(string type, string value)
    {
        object defaultValue = type == "System.Boolean" ? (object)false :
            type == "System.Double" ? (object)0.5d : 3;
        var project = CreateProject("board", "start", new Variable("bad", "bad", defaultValue),
            new Variable("good", "good", 1));
        string json = "{\"version\":1,\"boardIds\":[\"board\"],\"variables\":[" +
            "{\"id\":\"bad\",\"type\":\"" + type + "\",\"value\":\"" + value + "\"}," +
            "{\"id\":\"good\",\"type\":\"System.Int32\",\"value\":\"8\"}]}";
        PlayerPrefs.SetString(key + "_progress", json);

        Assert.That(store.TryRestore(project, out _), Is.True);
        Assert.That(project.GlobalVariables[0].Value, Is.EqualTo(defaultValue));
        Assert.That(project.GlobalVariables[1].Value, Is.EqualTo(8));
    }

    [Test]
    public void NumbersAreRestoredWithInvariantCulture()
    {
        var project = CreateProject("board", "start", new Variable("number", "number", 0.5d));
        project.GlobalVariables[0].Value = 1.25d;
        store.Save(project, "start");
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("it-IT");
            Assert.That(store.TryRestore(project, out _), Is.True);
            Assert.That(project.GlobalVariables[0].Value, Is.EqualTo(1.25d));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Test]
    public void InvalidDuplicateDoesNotMaskTheFirstCompatibleValue()
    {
        var project = CreateProject("board", "start", new Variable("value", "value", 1));
        PlayerPrefs.SetString(key + "_progress",
            "{\"version\":1,\"boardIds\":[\"board\"],\"variables\":[" +
            "{\"id\":\"value\",\"type\":\"System.Int32\",\"value\":\"bad\"}," +
            "{\"id\":\"value\",\"type\":\"System.Int32\",\"value\":\"8\"}," +
            "{\"id\":\"value\",\"type\":\"System.Int32\",\"value\":\"9\"}]}");
        Assert.That(store.TryRestore(project, out _), Is.True);
        Assert.That(project.GlobalVariables[0].Value, Is.EqualTo(8));
    }

    [Test]
    public void ClearRemovesBothFormatsAndPreservesOtherPreferences()
    {
        store.Save(CreateProject("board", "start"), "start");
        PlayerPrefs.SetString(key + "_variables", "legacy");
        PlayerPrefs.SetString(key + "_currentElement", "legacy-node");
        PlayerPrefs.SetString(key + "_unrelated", "keep");
        store.Clear();
        Assert.That(store.HasSavedProgress, Is.False);
        Assert.That(PlayerPrefs.HasKey(key + "_currentElement"), Is.False);
        Assert.That(PlayerPrefs.GetString(key + "_unrelated"), Is.EqualTo("keep"));
    }

    [Test]
    public void EditorInitializationUsesDefaultsOnceAndDetectsReplacementInsideTheSameAsset()
    {
        var first = CreateProject("board", "start", new Variable("value", "value", 1));
        first.GlobalVariables[0].Value = 7;
        store.Save(first, "start");
        string savedProgress = PlayerPrefs.GetString(key + "_progress");
        var player = CreatePlayer(first, out var asset);
        Invoke(player, "EnsureInitialized");
        Assert.That(first.GlobalVariables[0].Value, Is.EqualTo(1));
        first.GlobalVariables[0].Value = 99;
        Invoke(player, "EnsureInitialized");
        Assert.That(first.GlobalVariables[0].Value, Is.EqualTo(99));

        var replacement = CreateProject("board", "start", new Variable("value", "value", 2));
        AssignProject(asset, replacement);
        Invoke(player, "EnsureInitialized");
        Assert.That(replacement.GlobalVariables[0].Value, Is.EqualTo(2));
        Assert.That(replacement.StartingElement.Project, Is.SameAs(replacement));
        Assert.That(PlayerPrefs.GetString(key + "_progress"), Is.EqualTo(savedProgress));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NewEditorSessionStartsFreshAndExplicitLoadStillRestoresProgress(bool legacy)
    {
        var project = CreateProject("board", "start", new Variable("gold", "gold", 50),
            new Variable("sword", "sword_locked", true));
        project.GlobalVariables[0].Value = 0;
        project.GlobalVariables[1].Value = false;
        if (legacy)
        {
            PlayerPrefs.SetString(key + "_variables", project.SaveVariables());
            PlayerPrefs.SetString(key + "_currentElement", "start");
        }
        else store.Save(project, "start");
        string dataKey = key + (legacy ? "_variables" : "_progress");
        string savedProgress = PlayerPrefs.GetString(dataKey);

        var firstSession = CreatePlayer(project, out _);
        Invoke(firstSession, "EnsureInitialized");
        Assert.That(project.GlobalVariables[0].Value, Is.EqualTo(50));
        Assert.That(project.GlobalVariables[1].Value, Is.True);
        project.GlobalVariables[0].Value = 20;
        project.GlobalVariables[1].Value = false;

        var nextSession = CreatePlayer(project, out _);
        Invoke(nextSession, "EnsureInitialized");
        Assert.That(project.GlobalVariables[0].Value, Is.EqualTo(50));
        Assert.That(project.GlobalVariables[1].Value, Is.True);
        Assert.That(PlayerPrefs.GetString(dataKey), Is.EqualTo(savedProgress));
        Assert.That(project.StartingElement.Visits, Is.Zero);

        Assert.That(Invoke(nextSession, "TryLoad"), Is.True);
        Assert.That(project.GlobalVariables[0].Value, Is.Zero);
        Assert.That(project.GlobalVariables[1].Value, Is.False);
    }

    [Test]
    public void ResumeInitializationRestoresProgressOnlyOnce()
    {
        var project = CreateProject("board", "start", new Variable("gold", "gold", 50));
        project.GlobalVariables[0].Value = 10;
        store.Save(project, "start");
        var player = CreatePlayer(project, out _);
        var initialize = player.GetType().GetMethod("InitializeProject", BindingFlags.Instance | BindingFlags.NonPublic);

        initialize.Invoke(player, new object[] { true });
        Assert.That(project.GlobalVariables[0].Value, Is.EqualTo(10));
        project.GlobalVariables[0].Value = 25;
        initialize.Invoke(player, new object[] { true });
        Assert.That(project.GlobalVariables[0].Value, Is.EqualTo(25));
    }

    [Test]
    public void SetProjectInitializesTheImportedAssetWithoutResettingItsSave()
    {
        var old = CreateProject("old-board", "old-start", new Variable("value", "value", 1));
        var imported = CreateProject("new-board", "new-start", new Variable("value", "value", 2));
        imported.GlobalVariables[0].Value = 8;
        store.Save(imported, "new-start");
        var player = CreatePlayer(old, out _);
        Invoke(player, "EnsureInitialized");
        var asset = CreateAsset(imported);

        Invoke(player, "SetProject", asset);
        Assert.That(imported.GlobalVariables[0].Value, Is.EqualTo(8));
        Assert.That(imported.StartingElement.Project, Is.SameAs(imported));
        Assert.That(store.HasSavedProgress, Is.True);
    }

    [Test]
    public void PlayerLoadFallsBackToStartWhenSavedElementWasDeleted()
    {
        var old = CreateProject("board", "deleted", new Variable("value", "value", 1));
        old.GlobalVariables[0].Value = 6;
        store.Save(old, "deleted");
        var updated = CreateProject("board", "new-start", new Variable("value", "value", 1));
        var player = CreatePlayer(updated, out _);
        Assert.That(Invoke(player, "TryLoad"), Is.True);
        Assert.That(updated.StartingElement.Visits, Is.EqualTo(1));
        Assert.That(updated.GlobalVariables[0].Value, Is.EqualTo(6));
    }

    [Test]
    public void PlayerResetClearsProgressAndRestoresDefaults()
    {
        var project = CreateProject("board", "start", new Variable("value", "value", 1));
        project.GlobalVariables[0].Value = 8;
        store.Save(project, "start");
        var player = CreatePlayer(project, out _);
        Invoke(player, "EnsureInitialized");
        Invoke(player, "ResetVariables");
        Assert.That(project.GlobalVariables[0].Value, Is.EqualTo(1));
        Assert.That(store.HasSavedProgress, Is.False);
    }

    [Test]
    public void PlayerExplainsIncompatibleProgressWithoutChangingLiveValues()
    {
        store.Save(CreateProject("other-board", "start"), "start");
        var project = CreateProject("board", "start", new Variable("value", "value", 1));
        var player = CreatePlayer(project, out _);
        Invoke(player, "EnsureInitialized");
        project.GlobalVariables[0].Value = 5;
        LogAssert.Expect(LogType.Warning, "No compatible saved Arcweave progress is available for this project.");
        Assert.That(Invoke(player, "TryLoad"), Is.False);
        Assert.That(project.GlobalVariables[0].Value, Is.EqualTo(5));
        Assert.That(project.StartingElement.Visits, Is.Zero);
    }

    private Object CreatePlayer(Project project, out ArcweaveProjectAsset asset)
    {
        // Reflection keeps tests separate without moving all demo scripts out of Assembly-CSharp.
        var type = Type.GetType("Arcweave.ArcweavePlayer, Assembly-CSharp", true);
        var gameObject = new GameObject("Progress test player");
        gameObject.SetActive(false);
        objects.Add(gameObject);
        var player = gameObject.AddComponent(type);
        asset = CreateAsset(project);
        type.GetField("aw").SetValue(player, asset);
        type.GetField("autoStart").SetValue(player, false);
        type.GetField("progressStore", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, store);
        return player;
    }

    private ArcweaveProjectAsset CreateAsset(Project project)
    {
        var asset = ScriptableObject.CreateInstance<ArcweaveProjectAsset>();
        objects.Add(asset);
        AssignProject(asset, project);
        return asset;
    }

    private static void AssignProject(ArcweaveProjectAsset asset, Project project)
    {
        typeof(ArcweaveProjectAsset).GetProperty("Project").SetValue(asset, project);
    }

    private static object Invoke(Object player, string method, params object[] args)
    {
        return player.GetType().GetMethod(method).Invoke(player, args);
    }

    private static Project CreateProject(string boardId, string elementId, params Variable[] variables)
    {
        var element = new Element();
        typeof(Element).GetMethod("Set", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(element,
            new object[] { elementId, Vector2Int.zero, new List<Connection>(), "Start", "Hello",
                new List<Component>(), new List<Arcweave.Project.Attribute>(), null, "", Array.Empty<AudioAsset>() });
        var board = new Board(boardId, "Board", "board", new List<INode> { element });
        var project = new Project("Test project", element, new List<Board> { board },
            new List<Component>(), new List<Variable>(variables));
        project.Initialize();
        return project;
    }
}
