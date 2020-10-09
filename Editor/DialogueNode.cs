using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class DialogueNode : Node
{
    public string Guid;
    public string DialogueText;
    public bool EntryPoint = false;
}
