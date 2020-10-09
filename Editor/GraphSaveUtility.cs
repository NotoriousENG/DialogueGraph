using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using System.Linq;
using UnityEditor;
using System;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private DialogueGraphView _targetGraphView;
    private DialogueContainer _containerCache;

    private List<Edge> Edges => _targetGraphView.edges.ToList();
    private List<DialogueNode> Nodes => _targetGraphView.nodes.ToList().Cast<DialogueNode>().ToList();

    public static GraphSaveUtility GetInstance(DialogueGraphView targetGraphView)
    {
        return new GraphSaveUtility
        {
            _targetGraphView = targetGraphView
        };
    }

    public void SaveGraph(string fileName)
    {
        var dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();
        if(!SaveNodes(dialogueContainer)) return;

        SaveExposedProperties(dialogueContainer);

        // Auto create resources folder if it does not exist
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Resources/{fileName}.asset");
        AssetDatabase.SaveAssets();
    }

    private void SaveExposedProperties(DialogueContainer dialogueContainer)
    {
        dialogueContainer.ExposedProperties.AddRange(_targetGraphView.ExposedProperties);
    }

    private bool SaveNodes(DialogueContainer dialogueContainer)
    {
        if (!Edges.Any()) return false;

        var connectedPorts = Edges.Where(x => x.input.node != null).OrderByDescending(edge => ((DialogueNode)edge.output.node).EntryPoint).ToArray();
        foreach (var port in connectedPorts)
        {
            var outputNode = port.output.node as DialogueNode;
            var inputNode = port.input.node as DialogueNode;

            dialogueContainer.NodeLinks.Add(new NodeLinkData
            {
                BaseNodeGuid = outputNode.Guid,
                PortName = port.output.portName,
                TargetNodeGuid = inputNode.Guid
            });
        }

        foreach (var dialogueNode in Nodes.Where(node => !node.EntryPoint))
        {
            dialogueContainer.DialogueNodeData.Add(new DialogueNodeData
            {
                Guid = dialogueNode.Guid,
                DialogueText = dialogueNode.DialogueText,
                Position = dialogueNode.GetPosition().position
            });
        }

        return true;
    }

    public void LoadGraph(string fileName)
    {
        _containerCache = Resources.Load<DialogueContainer>(fileName);
        if (_containerCache == null)
        {
            EditorUtility.DisplayDialog("File Not Found", "Target dialogue graph file does not exist!", "OK");
            return;
        }

        ClearGraph();
        CreateNodes();
        ConnectNodes();
        CreateExposedProperties();
    }

    private void CreateExposedProperties()
    {
        // Clear existing properties on hot-reload
        _targetGraphView.ClearBlackBoardAndExposedProperties();
        // Add properties from data
        foreach (var exposedProperty in _containerCache.ExposedProperties)
        {
            _targetGraphView.AddPropertyToBlackBoard(exposedProperty);
        }
    }

    private void ConnectNodes()
    {
        foreach (var node in Nodes)
        {
            var connections = _containerCache.NodeLinks.Where(x => x.BaseNodeGuid == node.Guid).ToList();
            for (var j = 0; j < connections.Count; j++)
            {
                var targetNodeGuid = connections[j].TargetNodeGuid;
                var targetNode = Nodes.First(x => x.Guid == targetNodeGuid);
                LinkNodes(node.outputContainer[j].Q<Port>(), (Port) targetNode.inputContainer[0]);

                targetNode.SetPosition(new Rect(_containerCache.DialogueNodeData.First(x => x.Guid == targetNodeGuid).Position, 
                    _targetGraphView.DefaultNodeSize));
            }
        }
    }

    private void LinkNodes(Port output, Port input)
    {
        var tempEdge = new Edge
        {
            output = output,
            input = input
        };

        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);

        _targetGraphView.Add(tempEdge);
    }

    private void CreateNodes()
    {
        foreach (var nodeData in _containerCache.DialogueNodeData)
        {
            // We''ll handle position later, pass V2.zero for now
            var tempNode = _targetGraphView.CreateDialogueNode(nodeData.DialogueText, Vector2.zero);
            tempNode.Guid = nodeData.Guid;
            _targetGraphView.AddElement(tempNode);

            var nodePorts = _containerCache.NodeLinks.Where(x => x.BaseNodeGuid == nodeData.Guid).ToList();
            nodePorts.ForEach(x => _targetGraphView.AddChoicePort(tempNode, x.PortName));
        }
    }

    private void ClearGraph()
    {
        // Set entry points guid back from the save. Discard existing guid.
        Nodes.Find(x => x.EntryPoint).Guid = _containerCache.NodeLinks[0].BaseNodeGuid;

        foreach ( var node in Nodes)
        {
            if (node.EntryPoint) continue;

            // Remove edges that are connected to this node
            Edges.Where(x => x.input.node == node).ToList()
                .ForEach(edge => _targetGraphView.RemoveElement(edge));

            // Then remove the node
            _targetGraphView.RemoveElement(node);
        }
    }
}