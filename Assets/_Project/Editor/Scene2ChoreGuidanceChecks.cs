using System;
using UnityEditor;
using UnityEngine;

public static class Scene2ChoreGuidanceChecks
{
    [MenuItem("KuTy/Tests/Check Scene 2 chore guidance (no checkpoint writes)")]
    public static void Run()
    {
        int checks = 0;
        Action<bool, string> check = (ok, message) =>
        {
            if (!ok) throw new Exception("Scene2 guidance check failed: " + message);
            checks++;
        };
        float turns = 0f;
        for (int i = 0; i < 20; i++)
        {
            turns = Scene2CircularGesture.Accumulate(turns, 0, 30);
            turns = Scene2CircularGesture.Accumulate(turns, 30, 0);
        }
        check(Mathf.Abs(turns) < .001f, "shaking does not earn turns");
        for (int i = 0; i < 12; i++) turns = Scene2CircularGesture.Accumulate(turns, i * 30, (i + 1) * 30);
        check(Mathf.Abs(turns - 1f) < .001f, "clockwise full circle");
        turns = 0;
        for (int i = 0; i < 12; i++) turns = Scene2CircularGesture.Accumulate(turns, -i * 30, -(i + 1) * 30);
        check(Mathf.Abs(turns + 1f) < .001f, "counterclockwise full circle");
        check(Scene2CircularGesture.Accumulate(0, 0, 100) == 0, "reject pointer teleport");
        check(Mathf.Abs(Scene2CircularGesture.Accumulate(0, 350, 10) - 20f / 360f) < .001f, "wrap across zero");
        check(Scene2ChoreGuidance.CompletionLine(HouseholdChore.ReadLetter) == null, "do not interrupt letter");
        foreach (var chore in new[] { HouseholdChore.WashRice, HouseholdChore.LightFire, HouseholdChore.CookRice })
            check(Scene2ChoreGuidance.CompletionLine(chore) == null, "do not duplicate kitchen voice");
        foreach (var chore in new[] { HouseholdChore.ArrangeKeepsakes, HouseholdChore.CarryWood, HouseholdChore.SetTable, HouseholdChore.PutAwayDishes })
            check(!string.IsNullOrEmpty(Scene2ChoreGuidance.CompletionLine(chore)), "key mother reaction present");
        Debug.Log("Scene2 chore guidance: PASS " + checks + " checks. Pure logic only; no scene or checkpoint changes.");
    }
}
