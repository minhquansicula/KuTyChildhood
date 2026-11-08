using System;
using UnityEngine;

/// <summary>Serializable rules, separate from pointer input, camera, graphics and rewards.</summary>
[Serializable]
public sealed class HouseholdSimulatorState
{
    public int version = 1;
    public HouseholdChore chore;
    public int delivered, placedMask, letterStep;
    public float fill, transferred, rinse;
    public bool wetted, soaped, flipped;
    public float[] stains = { 1f, 1f, 1f, 1f, 1f, 1f };
    public bool Scrubbed => stains != null && stains.Length == 6 && Array.TrueForAll(stains, v => v <= .02f);
    public bool DishReady => Scrubbed && rinse >= 1f;
    public bool Complete => chore == HouseholdChore.WashDishes ? delivered >= 3 :
        chore == HouseholdChore.FetchWater ? transferred >= 1f :
        chore == HouseholdChore.MeasureRice ? delivered >= 3 :
        chore == HouseholdChore.ReadLetter ? letterStep >= 3 : (placedMask & 7) == 7;

    public static HouseholdSimulatorState Restore(HouseholdChore chore, string json)
    {
        try
        {
            var state = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<HouseholdSimulatorState>(json);
            if (state != null && state.version == 1 && state.chore == chore && state.stains != null && state.stains.Length == 6)
            {
                state.delivered = Mathf.Clamp(state.delivered, 0, 3);
                state.placedMask &= 7;
                state.letterStep = Mathf.Clamp(state.letterStep, 0, 3);
                state.fill = Safe01(state.fill); state.transferred = Safe01(state.transferred); state.rinse = Safe01(state.rinse);
                for (int i = 0; i < 6; i++) state.stains[i] = Safe01(state.stains[i]);
                if (!state.Scrubbed) state.rinse = 0f;
                return state;
            }
        }
        catch (ArgumentException) { /* Old or corrupt work never blocks the story. */ }
        return new HouseholdSimulatorState { chore = chore };
    }

    private static float Safe01(float v) => float.IsNaN(v) || float.IsInfinity(v) ? 0 : Mathf.Clamp01(v);

    public void Wet(float dt)
    {
        if (dt > 0f) wetted = true;
    }
    public bool Scrub(int patch, float travel)
    {
        if (!wetted || !soaped || patch < 0 || patch >= 6 || travel <= 0 || patch / 3 != (flipped ? 1 : 0)) return false;
        float old = stains[patch];
        stains[patch] = Mathf.Max(0, old - Mathf.Min(travel, .035f) * 10f);
        return stains[patch] < old;
    }
    public void Rinse(float dt)
    {
        if (dt <= 0) return;
        Wet(dt);
        if (Scrubbed) rinse = Mathf.Clamp01(rinse + dt / 1.4f);
    }
    public bool RackDish()
    {
        if (!DishReady || delivered >= 3) return false;
        delivered++;
        fill = rinse = 0;
        wetted = soaped = flipped = false;
        for (int i = 0; i < stains.Length; i++) stains[i] = 1f;
        return true;
    }
    public void Scoop(float dt)
    {
        if (dt > 0 && !Complete) fill = Mathf.Clamp01(fill + dt / .8f);
    }
    public void Pour(float tilt, float dt)
    {
        if (tilt < .35f || dt <= 0 || fill <= 0 || Complete) return;
        float amount = Mathf.Min(fill, dt * Mathf.Clamp01(tilt) * .9f);
        fill -= amount;
        if (chore == HouseholdChore.FetchWater) transferred = Mathf.Clamp01(transferred + amount * .5f);
        else if (chore == HouseholdChore.MeasureRice)
        {
            transferred = Mathf.Clamp01(transferred + amount);
            if (transferred >= .95f) { delivered++; transferred = fill = 0; }
        }
    }
    public bool Place(int item, bool inCorrectZone)
    {
        if (!inCorrectZone || item < 0 || item >= 3 || (placedMask & (1 << item)) != 0) return false;
        placedMask |= 1 << item;
        return true;
    }
}
