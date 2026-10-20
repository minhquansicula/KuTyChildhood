#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class OfficeBlockoutGameplaySmokeBootstrap
{
    private const string PendingKey = "KuTy.OfficeBlockoutGameplaySmoke.Pending";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPendingRun()
    {
        if (!SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        new GameObject("Office Blockout Gameplay Smoke")
            .AddComponent<OfficeBlockoutGameplaySmokeRunner>();
    }
}

public sealed class OfficeBlockoutGameplaySmokeRunner : MonoBehaviour
{
    private const string ResultPath = "Assets/Screenshots/OfficeBlockoutGameplay_Integration.txt";
    private int assertions;
    private bool finished;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += OnLog;
        StartCoroutine(Run());
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
    }

    private IEnumerator Run()
    {
        yield return null;
        OfficeIntroController intro = FindObjectOfType<OfficeIntroController>();
        OfficeBlockoutGameplayBridge bridge = FindObjectOfType<OfficeBlockoutGameplayBridge>();
        OfficeSceneController office = FindObjectOfType<OfficeSceneController>();
        OfficeWorkstationInteractable workstation = FindObjectOfType<OfficeWorkstationInteractable>();
        OfficeBlockoutChairInteractable chair = FindObjectOfType<OfficeBlockoutChairInteractable>();
        ExcelDataAuditorGame excel = FindObjectOfType<ExcelDataAuditorGame>(true);

        Check(intro != null && bridge != null && office != null, "Core office controllers are missing");
        Check(workstation != null && chair != null && excel != null,
            "Excel task interactions are missing");
        OfficeReportMiniGame legacyReport = FindObjectOfType<OfficeReportMiniGame>();
        Check(legacyReport == null || !legacyReport.enabled,
            "Legacy email/document/report mini-game is still enabled");
        Check(GameManager.Instance != null && SceneLoader.Instance != null,
            "Direct-play runtime services are missing");
        Check(intro.State == OfficeIntroController.IntroState.OpeningDialogue && office.Phase == OfficePhase.Opening,
            "Office did not begin in the boss-room opening");

        intro.SkipToReadyAtDeskForTests();
        yield return null;
        Check(bridge.HandoffComplete && intro.GameplayHandoffComplete,
            "Intro-to-gameplay handoff did not complete");
        Check(office.Phase == OfficePhase.ReturnToDesk && office.IsSeated,
            "Handoff did not adopt the seated work state");
        Check(!GameManager.Instance.InputBlocked, "Handoff left gameplay input blocked");

        // Edge case: stale callbacks must not skip the Excel task or acquire input.
        office.CancelWork();
        office.CompleteExcelTask();
        Check(office.Phase == OfficePhase.ReturnToDesk && !office.IsWorking &&
              !GameManager.Instance.InputBlocked,
            "A stale cancel/complete callback skipped or locked the Excel task");

        office.StandUp(true);
        Check(!office.IsSeated, "Safe stand position was incorrectly rejected");
        chair.Interact();
        yield return null;
        Check(office.IsSeated, "Chair did not return the player to the seated state");
        yield return new WaitForSeconds(.6f);
        CaptureUI("OfficeShift_HUD", 1366, 768);

        workstation.Interact();
        yield return null;
        Check(office.Phase == OfficePhase.ExcelAuditing && office.IsWorking && excel.gameObject.activeInHierarchy,
            "Excel task did not open directly from the workstation");
        Check(GameManager.Instance.InputBlocked, "Work UI did not acquire input");
        yield return new WaitForSeconds(.3f);
        CaptureUI("OfficeExcel_UI", 1920, 1080);
        CaptureUI("OfficeExcel_UI", 1366, 768);

        // Edge case: repeated interaction/open events are idempotent and do not reset the timer.
        float beforeRepeatedOpen = Get<float>(excel, "timeRemaining");
        workstation.Interact();
        Check(excel.Open(office), "Repeated Excel open should be accepted as an idempotent no-op");
        yield return null;
        Check(office.IsWorking && Get<bool>(excel, "isGameActive") &&
              Get<float>(excel, "timeRemaining") <= beforeRepeatedOpen,
            "Repeated Excel interaction restarted or closed the active task");

        GameManager.Instance.SetPaused(true);
        Check(Time.timeScale == 0f, "Pause did not freeze scaled gameplay time");
        float pausedTimer = Get<float>(excel, "timeRemaining");
        yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(Get<float>(excel, "timeRemaining") - pausedTimer) < .01f,
            "Excel timer advanced during pause");
        GameManager.Instance.SetPaused(false);

        // Edge case: the boss popup pauses the task timer and safely resumes it when closed.
        GameObject chatPopup = Get<GameObject>(excel, "chatPopupPanel");
        Call(excel, "ShowChatPopup");
        Check(chatPopup != null && chatPopup.activeSelf, "Boss popup could not be shown");
        Check(Get<Image>(excel, "chatBackdrop").gameObject.activeInHierarchy,
            "Boss popup has no modal input shield");
        int originalSheet = Get<int>(excel, "currentSheetIndex");
        Call(excel, "SwitchSheet", 1);
        Call(excel, "CheckValidation");
        Check(Get<int>(excel, "currentSheetIndex") == originalSheet && office.IsWorking,
            "The spreadsheet accepted input behind the boss message");
        CaptureUI("OfficeExcel_Chat", 1366, 768);
        float frozenTime = Get<float>(excel, "timeRemaining");
        yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(Get<float>(excel, "timeRemaining") - frozenTime) < .05f,
            "Excel timer continued while the boss popup was open");
        Call(excel, "CloseChatPopup");
        float resumedTime = Get<float>(excel, "timeRemaining");
        yield return new WaitForSecondsRealtime(.2f);
        Check(Get<float>(excel, "timeRemaining") < resumedTime,
            "Excel timer did not resume after the boss popup closed");

        Set<GameObject>(excel, "chatPopupPanel", null);
        IEnumerator missingPopupRoutine = (IEnumerator)Call(excel, "BossDistractionRoutine");
        Check(missingPopupRoutine.MoveNext() && missingPopupRoutine.MoveNext(),
            "Missing boss popup reference caused the distraction routine to stop unexpectedly");
        Set(excel, "chatPopupPanel", chatPopup);

        var inputs = Get<Dictionary<int, Dictionary<Vector2Int, string>>>(excel, "userInputs");
        Vector2Int answerCell = new Vector2Int(4, 3);
        inputs[0][answerCell] = "-1";
        inputs[1][answerCell] = "11500";
        Call(excel, "CheckValidation");
        var errors = Get<Dictionary<int, HashSet<Vector2Int>>>(excel, "validationErrors");
        Check(errors.Count == 1 && errors.TryGetValue(0, out var firstSheetErrors) &&
              firstSheetErrors.Contains(answerCell) && office.Phase == OfficePhase.ExcelAuditing && office.IsWorking,
            "Invalid Excel input was accepted or did not focus the incorrect sheet");

        // Edge case: timeout retries in place, closes the popup, and preserves typed data.
        Call(excel, "ShowChatPopup");
        Call(excel, "FailGame", "Kiểm tra hết giờ");
        Check(!Get<bool>(excel, "isGameActive") && !chatPopup.activeSelf &&
              !Get<Button>(excel, "submitButton").interactable,
            "Timeout did not enter a safe retry state");
        yield return new WaitForSecondsRealtime(2.15f);
        Check(Get<bool>(excel, "isGameActive") &&
              inputs[0].TryGetValue(answerCell, out string retainedValue) && retainedValue == "-1" &&
              Get<float>(excel, "timeRemaining") > 0f,
            "Timeout retry lost user data or failed to restart the task");

        float retainedTime = Get<float>(excel, "timeRemaining");
        office.CancelWork();
        Check(office.Phase == OfficePhase.ExcelAuditing && !office.IsWorking && !excel.gameObject.activeInHierarchy,
            "Excel cancellation did not return control cleanly");
        office.CancelWork();
        Check(office.Phase == OfficePhase.ExcelAuditing && !GameManager.Instance.InputBlocked,
            "Repeated Excel cancellation was not idempotent");

        workstation.Interact();
        yield return null;
        Check(Get<float>(excel, "timeRemaining") <= retainedTime && Get<float>(excel, "timeRemaining") > retainedTime - 1f,
            "Closing and reopening reset the task deadline");
        office.CancelWork();

        // Start a fresh attempt with an invalid Inspector limit: use a playable default.
        float configuredTimeLimit = Get<float>(excel, "timeLimit");
        Set(excel, "timeLimit", 0f);
        Set(excel, "attemptStarted", false);
        workstation.Interact();
        yield return null;
        Check(office.IsWorking && Get<float>(excel, "timeRemaining") >= 1f,
            "A non-positive Excel time limit caused an immediate failure loop");
        office.CancelWork();
        Set(excel, "timeLimit", configuredTimeLimit);

        workstation.Interact();
        yield return null;
        Check(office.IsWorking && excel.gameObject.activeInHierarchy && inputs[0][answerCell] == "-1",
            "Excel task did not reopen or preserve data after cancellation");

        // Whitespace around otherwise correct numeric input should be accepted.
        Call(excel, "SwitchSheet", 0);
        Get<Dictionary<Vector2Int, TMP_InputField>>(excel, "visibleInputs")[answerCell].text = "1300";
        Call(excel, "SwitchSheet", 1);
        yield return null;
        Get<Dictionary<Vector2Int, TMP_InputField>>(excel, "visibleInputs")[answerCell].text = "11500";
        Check(inputs[0][answerCell] == "1300" && inputs[1][answerCell] == "11500" && office.WorkProgress == 1f,
            "Typing into the actual TMP fields failed to update task data and progress");
        inputs[0][answerCell] = " 1300 ";
        Call(excel, "CheckValidation");
        Check(!Get<bool>(excel, "isGameActive") && !Get<Button>(excel, "submitButton").interactable,
            "Correct Excel data did not enter the completion state");
        yield return new WaitForSecondsRealtime(1.65f);
        Check(office.Phase == OfficePhase.AfterWork && !office.IsWorking,
            "Excel completion did not start after-work free roam");
        yield return null;
        CaptureUI("OfficeShift_Complete", 1366, 768);

        office.MakeRestAvailableForTests();
        workstation.Interact();
        Check(office.Phase == OfficePhase.Leaving && GameManager.Instance.InputBlocked,
            "Rest interaction did not start the exhaustion sequence");

        yield return WaitUntilOrFail(
            () => SceneManager.GetActiveScene().name == SceneNames.Act2_Home,
            45f,
            "Exhaustion did not load Act 2");
        Check(SceneManager.GetActiveScene().name == SceneNames.Act2_Home,
            "Exhaustion did not load Act 2");
        Complete(true, "Act1 Excel gameplay and edge-case smoke passed");
    }

    private IEnumerator WaitUntilOrFail(Func<bool> predicate, float timeout, string message)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!predicate() && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(predicate(), message + $" (timeScale={Time.timeScale}, paused={GameManager.Instance?.IsPaused}, loading={SceneLoader.Instance?.IsLoading})");
    }

    private static void CaptureUI(string name, int width, int height)
    {
        Camera camera = Camera.main;
        if (camera == null) throw new InvalidOperationException("Missing player camera for UI verification");
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        var modes = new RenderMode[canvases.Length];
        var cameras = new Camera[canvases.Length];
        var distances = new float[canvases.Length];
        var target = RenderTexture.GetTemporary(width, height, 24);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        float oldAspect = camera.aspect;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.aspect = (float)width / height;
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                cameras[i] = canvases[i].worldCamera;
                distances[i] = canvases[i].planeDistance;
                if (!canvases[i].isRootCanvas || modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = .5f;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            Directory.CreateDirectory("Assets/Screenshots");
            File.WriteAllBytes($"Assets/Screenshots/{name}_{width}.png", texture.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = modes[i];
                canvases[i].worldCamera = cameras[i];
                canvases[i].planeDistance = distances[i];
            }
            camera.targetTexture = oldTarget;
            camera.aspect = oldAspect;
            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(target);
            Destroy(texture);
            Canvas.ForceUpdateCanvases();
        }
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        assertions++;
    }

    private static T Get<T>(object instance, string field)
    {
        FieldInfo info = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        if (info == null) throw new MissingFieldException(instance.GetType().Name, field);
        return (T)info.GetValue(instance);
    }

    private static void Set<T>(object instance, string field, T value)
    {
        FieldInfo info = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        if (info == null) throw new MissingFieldException(instance.GetType().Name, field);
        info.SetValue(instance, value);
    }

    private static object Call(object instance, string method, params object[] arguments)
    {
        MethodInfo info = instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        if (info == null) throw new MissingMethodException(instance.GetType().Name, method);
        return info.Invoke(instance, arguments);
    }

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        if (finished || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        Complete(false, condition + "\n" + stackTrace);
    }

    private void Complete(bool success, string message)
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= OnLog;
        string result = (success ? "PASS" : "FAIL") + " (" + assertions + " assertions)\n" + message;
        Directory.CreateDirectory("Assets/Screenshots");
        File.WriteAllText(ResultPath, result);
        if (success) Debug.Log("[KuTy] " + result);
        else Debug.LogError("[KuTy] " + result);
        EditorApplication.isPlaying = false;
    }
}
#endif
