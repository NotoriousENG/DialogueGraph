using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

public class DialogueReceiver : MonoBehaviour
{
    public string Dialogue;


    public void ReceiveDialogue()
    {
        var cont = MasterManager.DialogueContainer;
        var root = cont.DialogueNodeData[0];

        Dialogue = root.DialogueText;

        var currData = root;

        while (currData != null)
        {
            var links = cont.NodeLinks.Where(node => node.BaseNodeGuid == currData.Guid);

            if (links.Count() == 0)
            {
                Dialogue = currData.DialogueText;
                break;
            }

            else if (links.Count() == 1)
            {
                currData = cont.DialogueNodeData.Find(node => node.Guid == links.First().BaseNodeGuid);
                Dialogue = currData.DialogueText;

                currData = cont.DialogueNodeData.Find(node => node.Guid == links.First().TargetNodeGuid);
            }
            else
            {
                Dialogue = "Choices: \n";
                foreach (var branch in links)
                {
                    Dialogue += branch.PortName + "\n";
                }

                currData = cont.DialogueNodeData.Find(node => node.Guid == links.First().TargetNodeGuid);
            }
        }
    }
}
