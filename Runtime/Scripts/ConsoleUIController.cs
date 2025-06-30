using UnityEngine;
using UnityEngine.UIElements;
using MoonSharp.Interpreter;
using DCG.RuntimeConsole.Lua;
using System.Linq;

namespace DCG.RuntimeConsole
{
    public class ConsoleUIController : MonoBehaviour
    {
        [SerializeField]
        UIDocument runtimeLuaConsoleUIDocument;

        [SerializeField]
        VisualTreeAsset suggestionsListUIAsset;

        private ConsoleEnvironmentLua consoleEnvironment;
        private LuaSyntaxHighlighter syntaxHighlighter = new();

        private TextField consoleInputField;
        private Label textOverlay;
        private Label outputLabel;
        private Label suggestionsLabel;

        void Start()
        {
            // Create the console environment
            ConsoleEnvironmentLua consoleLuaEnvironment = new();
            this.consoleEnvironment = consoleLuaEnvironment;
            this.consoleEnvironment.OutputReceived += this.AppendOutput;

            // Find the Console Input Field
            this.consoleInputField = this.runtimeLuaConsoleUIDocument
                .rootVisualElement
                .Q<TextField>("ConsoleInputField");

            // Find the Text Overlay
            this.textOverlay = this.runtimeLuaConsoleUIDocument
                .rootVisualElement
                .Q<Label>("TextOverlay");

            // Find the Console Output Label
            this.outputLabel = runtimeLuaConsoleUIDocument
                .rootVisualElement
                .Q<Label>("ConsoleOutputLabel");

            // Find the Suggestions Label
            this.suggestionsLabel = runtimeLuaConsoleUIDocument
                .rootVisualElement
                .Q<Label>("SuggestionsLabel");

            // Update input field settings
            consoleInputField.selectAllOnFocus = false;
            consoleInputField.selectAllOnMouseUp = false;
            consoleInputField.multiline = true;

            // Register example functions
            UserData.RegisterType<SceneUtilities>();
            this.consoleEnvironment.LuaScript.Globals["SceneUtils"] = UserData.Create(new SceneUtilities());

            // Register callbacks
            consoleInputField.RegisterValueChangedCallback(changeEvent =>
            {
                // Syntax highlighting
                string highlighted = this.syntaxHighlighter.Highlight(this.consoleInputField.value);
                this.textOverlay.text = highlighted;

                // Suggestions / autocomplete
                int cursor = this.consoleInputField.cursorIndex;
                string input = this.consoleInputField.value;

                LuaSuggestor luaSuggestor = new();
                luaSuggestor.luaScript = this.consoleEnvironment.LuaScript;
                string closestSuggestion = luaSuggestor.GetClosestSuggestion(input, cursor);
                suggestionsLabel.text = closestSuggestion;

                string[] allSuggestions = luaSuggestor.GetSuggestions(input, cursor);
                string allSuggestionsSeparated = string.Join("\n", allSuggestions);
                suggestionsLabel.text = allSuggestionsSeparated;
                Debug.Log(allSuggestionsSeparated); // For debug
            });

            consoleInputField.RegisterCallback<KeyUpEvent>(evt =>
            {
                // Shift+Enter => insert newline
                if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && evt.shiftKey)
                {
                    // Manually insert newline at caret position
                    int caret = consoleInputField.cursorIndex;
                    string text = consoleInputField.value;
                    consoleInputField.value = text.Insert(caret, "\n");

                    // Move cursor after newline
                    consoleInputField.cursorIndex = caret + 1;
                    consoleInputField.selectIndex = caret + 1;

                    evt.StopPropagation(); // prevent bubbling
                }
                // Just Enter => submit
                else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    string command = consoleInputField.value.Trim();
                    Debug.Log("Running Lua: " + command);

                    // Run the Lua code here
                    string code = this.consoleInputField.value;
                    // this.RunLua(code);
                    this.consoleEnvironment.Execute(code);

                    // Clear the input field
                    consoleInputField.value = "";
                    consoleInputField.Blur();

                    evt.StopPropagation(); // block default behavior
                }
            });

            // Create suggestions popup
            
        }




        void AppendOutput(string message)
        {
            this.outputLabel.text += "=> " + message + "\n";
        }





    }





    public class SceneUtilities
    {
        string testPrefix = "testPrefix: ";

        public void RenameAllGameObjects(string newName)
        {
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                go.name = this.testPrefix + newName;
            }

            Debug.Log($"Renamed all GameObjects to '{newName}'");
        }

        public GameObject GetFirstGO()
        {
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                return go;
            }

            return null;
        }
    }
}

