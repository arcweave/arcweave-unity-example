#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arcweave;
using Arcweave.Project;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ArcweaveDemoSceneTests
{
    private HashSet<GameObject> originalRoots;
    private Scene demoScene;
    private ArcweaveProgressStore store;
    private MonoBehaviour player;
    private MonoBehaviour ui;
    private MonoBehaviour manager;
    private MonoBehaviour[] triggers;
    private Arcweave.Project.Project project;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        originalRoots = new HashSet<GameObject>(Roots());
        store = new ArcweaveProgressStore("arcweave_scene_test_" + Guid.NewGuid().ToString("N"));
        demoScene = EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Demo Scene.unity",
            new LoadSceneParameters(LoadSceneMode.Additive));
        yield return null;
        SceneManager.SetActiveScene(demoScene);
        player = FindBehaviour("Arcweave.ArcweavePlayer");
        SetField(player, "progressStore", store);
        ui = FindBehaviour("Arcweave.ArcweavePlayerUI");
        manager = FindBehaviour("GameManager");
        triggers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(behaviour => behaviour.GetType().Name == "DialogueTrigger" && behaviour.isActiveAndEnabled).ToArray();
        project = ((ArcweaveProjectAsset)Field(player, "aw")).Project;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // Include scene-owned DontDestroyOnLoad managers without touching the test runner.
        foreach (var root in Roots())
            if (!originalRoots.Contains(root)) Object.Destroy(root);
        yield return null;
        if (demoScene.IsValid() && demoScene.isLoaded)
            yield return SceneManager.UnloadSceneAsync(demoScene);
        store.Clear();
    }

    [UnityTest]
    public IEnumerator DemoFinalElementsRemainVisibleUntilClose()
    {
        int checkedEndings = 0;
        foreach (var trigger in triggers)
        {
            var board = project.Boards.Single(item => item.Name == (string)Field(trigger, "specificBoardName"));
            var endings = board.Nodes.OfType<Element>()
                .Where(element => (bool)Invoke(manager, "HasDialogueEndTag", element) || element.Outputs.Count == 0)
                .ToArray();
            foreach (var ending in endings)
            {
                project.ResetVariablesToDefaultValues();
                Invoke(trigger, "StartDialogue");
                Invoke(player, "Next", ending);
                yield return WaitForTextFade();

                AssertFinalIsVisible(ending);
                CloseButton().onClick.Invoke();
                Assert.That(Field(manager, "currentState").ToString(), Is.EqualTo("Gameplay"), ending.Title);
                Assert.That(((GameObject)Field(manager, "arcweaveUI")).activeInHierarchy, Is.False, ending.Title);
                checkedEndings++;
                yield return null;
            }
        }
        Assert.That(checkedEndings, Is.GreaterThan(0), "The demo must exercise actual narrative endings.");
    }

    [UnityTest]
    public IEnumerator DelayedImportCompletionCannotCloseAnActiveDialogue()
    {
        Invoke(manager, "TogglePause");
        yield return null;
        var importerUI = FindBehaviour("ArcweaveImporterUI");
        SetField(importerUI, "autoCloseDelay", 0.05f);
        Invoke(importerUI, "OnImportSuccess");

        var trigger = triggers.First();
        var board = project.Boards.Single(item => item.Name == (string)Field(trigger, "specificBoardName"));
        var ending = board.Nodes.OfType<Element>().First(element => (bool)Invoke(manager, "HasDialogueEndTag", element));
        Invoke(trigger, "StartDialogue");
        Invoke(player, "Next", ending);
        yield return WaitForTextFade();

        AssertFinalIsVisible(ending);
        CloseButton().onClick.Invoke();
        Assert.That(Field(manager, "currentState").ToString(), Is.EqualTo("Gameplay"));
    }

    [UnityTest]
    public IEnumerator ResumeStillReturnsAPausedGameToGameplay()
    {
        Invoke(manager, "TogglePause");
        Assert.That(Field(manager, "currentState").ToString(), Is.EqualTo("Paused"));
        Invoke(manager, "ResumeGame");
        Assert.That(Field(manager, "currentState").ToString(), Is.EqualTo("Gameplay"));
        Assert.That(((GameObject)Field(manager, "importerUI")).activeSelf, Is.False);
        yield return null;
    }

    private void AssertFinalIsVisible(Element ending)
    {
        Assert.That(Field(manager, "currentState").ToString(), Is.EqualTo("Dialogue"), ending.Title);
        Assert.That(((GameObject)Field(manager, "arcweaveUI")).activeInHierarchy, Is.True, ending.Title);
        var content = (Text)Field(ui, "content");
        Assert.That(content.gameObject.activeInHierarchy, Is.True, ending.Title);
        Assert.That(content.text, Is.Not.Empty, ending.Title);
        Assert.That(content.text, Is.EqualTo(ending.RuntimeContent), ending.Title);
        Assert.That(content.canvasRenderer.GetAlpha(), Is.GreaterThan(0.95f), ending.Title);
        Assert.That(CloseButton().gameObject.activeInHierarchy, Is.True, ending.Title);
    }

    private Button CloseButton()
    {
        var buttons = (List<Button>)Field(ui, "tempButtons");
        Assert.That(buttons.Count, Is.EqualTo(1));
        Assert.That(buttons[0].GetComponentInChildren<Text>().text, Is.EqualTo("Close"));
        return buttons[0];
    }

    private IEnumerator WaitForTextFade()
    {
        yield return new WaitForSeconds((float)Field(ui, "crossfadeTime") + 0.15f);
    }

    private static IEnumerable<GameObject> Roots() =>
        Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(transform => transform.parent == null).Select(transform => transform.gameObject).ToArray();
    private static MonoBehaviour FindBehaviour(string type) =>
        Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(behaviour => behaviour.GetType().FullName == type);
    private static object Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(target);
    private static void SetField(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(target, value);
    private static object Invoke(object target, string name, params object[] args) =>
        (target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public) ??
         target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)).Invoke(target, args);
}
#endif
