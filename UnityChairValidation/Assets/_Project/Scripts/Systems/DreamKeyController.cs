using UnityEngine;

public class DreamKeyController : MonoBehaviour
{
    [SerializeField] private GameObject keyVisual;
    [SerializeField] private float spinSpeed = 36f;
    private QuestManager quests;
    private void Start()
    {
        quests = QuestManager.Instance;
        if (quests != null) quests.OnQuestChanged += Refresh;
        Refresh(quests != null ? quests.CurrentStep : QuestStep.ReadJournal);
    }
    private void OnDestroy() { if (quests != null) quests.OnQuestChanged -= Refresh; }
    private void Refresh(QuestStep step)
    {
        if (keyVisual != null) keyVisual.SetActive(step == QuestStep.TakeDreamKey);
    }
    private void Update()
    {
        if (keyVisual != null && keyVisual.activeSelf)
            keyVisual.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
    }
}
