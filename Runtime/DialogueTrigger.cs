using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public GameEvent DialogueTriggerEvent;

    public DialogueContainer defaultDialogue;

    public void TriggerDialogue()
    {
        DialogueTriggerEvent.Raise();
    }

    private void Start()
    {
        MasterManager.DialogueContainer = defaultDialogue;
        TriggerDialogue();
    }
}
