using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Builds task UI on disposable objects. Never opens/saves the user's scene or awards memories.
public static class HouseholdArtReview
{
    private const string Output = "Logs/HouseholdArtReview";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("KuTy/Tests/Review all household artwork")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        int count = 0;
        try
        {
            foreach (HouseholdChore chore in Enum.GetValues(typeof(HouseholdChore)))
            {
                Review(chore);
                count++;
            }
            File.WriteAllText(Output + "/result.txt", "PASS: " + count + " task surfaces built; sprites, wood tie gate, dish stage gates and cancellation verified. Visual review in Game View remains.");
            Debug.Log("[HouseholdArtReview] PASS " + count + " surfaces");
        }
        catch (Exception error)
        {
            File.WriteAllText(Output + "/result.txt", "FAIL: " + error);
            Debug.LogException(error);
        }
    }

    [MenuItem("KuTy/Tests/Review rice interaction in Play Mode")]
    public static void ReviewRiceInteraction()
    {
        Directory.CreateDirectory(Output);
        try
        {
            Require(Application.isPlaying, "Enter Play Mode first");
            var player = UnityEngine.Object.FindObjectOfType<PlayerInteraction>();
            var flow = HouseholdChoreFlow.Instance;
            Require(player != null && flow != null, "Player interaction or chore flow missing");
            flow.ShowChecklist();
            Require(flow.IsCurrent(HouseholdChore.WashRice), "Rice task is not currently available");
            HouseholdChoreInteractable pot = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsOfType<HouseholdChoreInteractable>(true))
                if (candidate.Chore == HouseholdChore.WashRice && candidate.name == "SM_RiceWashingPot")
                    pot = candidate;
            Require(pot != null && pot.isActiveAndEnabled, "Active rice pot interaction missing");
            var nearby = (System.Collections.IEnumerable)typeof(PlayerInteraction)
                .GetField("proximityTargets", Private).GetValue(player);
            bool registered = false;
            foreach (object candidate in nearby)
                if (ReferenceEquals(candidate, pot)) { registered = true; break; }
            Require(registered, "Rice pot was not registered by PlayerInteraction");
            Require(pot.GetPromptText().StartsWith("[E] Vo gạo"), "Rice task does not offer E prompt");
            File.WriteAllText(Output + "/rice-focus-result.txt",
                "PASS: rice pot active, registered for proximity focus, prompt=" + pot.GetPromptText());
            Debug.Log("[HouseholdArtReview] Rice interaction binding PASS");
        }
        catch (Exception error)
        {
            File.WriteAllText(Output + "/rice-focus-result.txt", "FAIL: " + error);
            Debug.LogException(error);
        }
    }

    private static T Get<T>(object value, string name) => (T)value.GetType().GetField(name, Private).GetValue(value);
    private static void Call(object value, string name) => value.GetType().GetMethod(name, Private).Invoke(value, null);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private static void Review(HouseholdChore chore)
    {
        GameObject host = null;
        try
        {
            host = new GameObject("HouseholdArtReview");
            host.hideFlags = HideFlags.HideAndDontSave;
            var ui = new HouseholdChoreUI(host.transform);
            ui.SetChecklistVisible(false);
            ui.ShowGame(HouseholdChoreFlow.Titles[(int)chore], "Thao tác trực tiếp trên đồ vật", false);
            int completion = 0;
            ui.Actions.Begin(chore, () => completion++, ui.SetProgress, ui.SetHint, () => { });
            var root = Get<RectTransform>(ui.Actions, "root");
            Require(root.Find("WorkCloth").GetComponent<Image>().sprite != null, chore + ": no backdrop");
            bool hasDetail = false;
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                if (image.sprite != null && image.sprite.name != "Task_Worktop" &&
                    image.sprite.name != "HouseholdUIRoundSprite") { hasDetail = true; break; }
            Require(hasDetail, chore + ": no task-specific illustration");
            if (chore == HouseholdChore.SetTable)
                foreach (var item in Get<RectTransform[]>(ui.Actions, "items"))
                    Require(item.Find("PropIcon").GetComponent<Image>().sprite != null, "Meal item missing artwork");
            if (chore == HouseholdChore.FoldBlanket)
                foreach (var item in Get<RectTransform[]>(ui.Actions, "items"))
                    Require(item.Find("PropIcon").GetComponent<Image>().sprite.name == "Fold_Blanket", "Blanket artwork missing");
            if (chore == HouseholdChore.CarryWood)
            {
                Require(root.Find("WoodMat").GetComponent<Image>().sprite.name == "Wood_Mat", "Wood mat missing");
                for (int i = 0; i < 3; i++)
                    typeof(HouseholdActionBoard).GetMethod("Place", Private).Invoke(ui.Actions, new object[] { i });
                Require(Get<int>(ui.Actions, "stage") == 1 && completion == 0, "Wood tie step skipped");
                Require(Get<RectTransform>(ui.Actions, "actionArea").GetComponent<Image>().sprite.name == "Wood_Rope", "Rope missing");
                Require(!root.Find("TiedWoodBundle").gameObject.activeSelf, "Bundle appears before tying");
            }
            if (chore == HouseholdChore.WashDishes)
            {
                Require(Get<RectTransform>(ui.Actions, "scrubber").GetComponent<Image>().sprite.name == "Dish_Sponge", "Wrong sponge");
                Require(Get<RectTransform>(ui.Actions, "rack").GetComponent<Image>().sprite.name == "Dish_Rack", "Dish rack artwork missing");
                float[] stains = Get<float[]>(ui.Actions, "stainRemaining");
                stains[0] = 0f;
                Call(ui.Actions, "TickScrub");
                Require(Get<int>(ui.Actions, "stage") == 0, "One stain bypassed washing");
                Array.Clear(stains, 0, stains.Length);
                Call(ui.Actions, "TickScrub");
                Require(Get<int>(ui.Actions, "stage") == 1 && completion == 0, "Rinse skipped");
                Require(!Get<RectTransform>(ui.Actions, "rack").gameObject.activeSelf, "Rack available too soon");
                Call(ui.Actions, "TickScrub");
                Require(Get<int>(ui.Actions, "stage") == 1 && completion == 0, "Rinse starts without dropping dish");
                typeof(HouseholdActionBoard).GetField("rinseSeconds", Private).SetValue(ui.Actions, 1.2f);
                Call(ui.Actions, "TickScrub");
                Require(Get<int>(ui.Actions, "stage") == 2 && completion == 0, "Rack step bypassed");
                Require(Get<RectTransform>(ui.Actions, "rack").gameObject.activeSelf, "Clean rack missing");
            }
            ui.Actions.Hide();
            Call(ui.Actions, "Complete");
            Require(completion == 0, "Cancelled task awarded completion");
            Require(!ui.Actions.Restart(), "Hidden task restarted");
        }
        finally
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
        }
    }
}
