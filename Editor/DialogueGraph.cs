using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraph : EditorWindow
{
    private DialogueGraphView _graphView;
    private string _fileName = "New Narrative";

    [MenuItem("Graph/Dialogue Graph")]
    public static void OpenDialogueGraphWindow()
    {
        var window = GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue Graph");
    }

    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
        GenerateMiniMap();
        GenerateBlackboard();
    }

    /// <summary>
    /// OnOpenAssetAttribute has an option to provide an order index in the callback, starting at 0.
    /// This is useful if you have more than one OnOpenAssetAttribute callback,
    /// and you would like them to be called in a certain order.Callbacks are called in order, starting at zero.
    /// Must return true if you handled the opening of the asset or false if an external tool should open it.
    /// The method with this attribute must use at least these two parameters :
    /// </summary>
    /// <param name="instanceID"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)

    {
        //Get the instanceID of the DialogueGraphContainer to find it in the project.
        string assetPath = AssetDatabase.GetAssetPath(instanceID);
        DialogueContainer dgc = AssetDatabase.LoadAssetAtPath<DialogueContainer>(assetPath);
        if (dgc != null)

        {
            //Debug.Log($"Opening graph \"{dgc .name}\"");

            DialogueGraph window = GetWindow<DialogueGraph>();

            window.titleContent = new GUIContent($"{dgc.name} (Dialogue Graph)");

            //Once the window is opened, we load the content of the scriptable object.
            //Even if the new name doesn't show up in the TextField, we need to assign the _fileName

            //to load the appropriate file.
            window._fileName = dgc.name;
            window.RequestDataOperation(false);
            return true;
        }

        //If object not found, won't open anything since we need the object to draw the window.
        return false;
    }

    private void GenerateBlackboard()
    {
        var blackboard = new Blackboard(_graphView);
        blackboard.Add(new BlackboardSection {title = "Exposed Properties"});
        blackboard.addItemRequested = x => { _graphView.AddPropertyToBlackBoard(new ExposedProperty()); };
        blackboard.editTextRequested = (blackboard1, element, newValue) =>
        {
            var oldPropertyName = ((BlackboardField) element).text;
            if (_graphView.ExposedProperties.Any(x => x.PropertyName == newValue))
            {
                EditorUtility.DisplayDialog("Error", "This property name already exists, please choose another one!",
                    "OK");
                return;
            }

            var propertyIndex = _graphView.ExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
            _graphView.ExposedProperties[propertyIndex].PropertyName = newValue;
            ((BlackboardField) element).text = newValue;
        };

        blackboard.SetPosition(new Rect(10,30,200,300));
        _graphView.Add(blackboard);
        _graphView.Blackboard = blackboard;
    }

    private void GenerateMiniMap()
    {
        var miniMap = new MiniMap { anchored = true };
        var coords = _graphView.contentViewContainer.WorldToLocal(new Vector2(this.maxSize.x - 10, 30));
        miniMap.SetPosition(new Rect(coords.x, coords.y, 200, 140));
        _graphView.Add(miniMap);
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(_graphView);
    }

    private void ConstructGraphView()
    {
        _graphView = new DialogueGraphView(this)
        {
            name = "Dialogue Graph"
        };

        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        var fileNameTextField = new TextField("File Name: ");
        fileNameTextField.SetValueWithoutNotify("New Narrative");
        fileNameTextField.MarkDirtyRepaint();
        fileNameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue);
        toolbar.Add(fileNameTextField);

        toolbar.Add(new Button(() => RequestDataOperation(true)) { text = "Save Data" });
        toolbar.Add(new Button(() => RequestDataOperation(false)) { text = "Load Data" });

        //var nodeCreateButton = new Button(() =>
        //{
        //    _graphView.CreateNode("Dialogue Node", Vector2.zero);
        //});
        //nodeCreateButton.text = "Create Node";
        //toolbar.Add(nodeCreateButton);

        rootVisualElement.Add(toolbar);
    }

    private void RequestDataOperation(bool save)
    {
        if (string.IsNullOrEmpty(_fileName))
        {
            EditorUtility.DisplayDialog("Invalid file name!", "Please enter a valid file name.", "OK");
            return;
        }

        var saveUtility = GraphSaveUtility.GetInstance(_graphView);
        if (save)
        {
            saveUtility.SaveGraph(_fileName);
        }
        else
        {
            saveUtility.LoadGraph(_fileName);
        }
    }
}
