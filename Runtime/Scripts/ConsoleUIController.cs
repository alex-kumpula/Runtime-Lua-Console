using UnityEngine;
using UnityEngine.UIElements;
using MoonSharp.Interpreter;
using DCG.RuntimeConsole.Lua;
using UnityEngine.InputSystem;
using System.Linq;
using System;

namespace DCG.RuntimeConsole
{
    public class ConsoleUIController : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference showHideToggleActionReference;

        [SerializeField]
        private UIDocument runtimeLuaConsoleUIDocument;

        private ConsoleEnvironmentLua consoleEnvironment;
        private LuaSyntaxHighlighter syntaxHighlighter = new();

        private TextField consoleInputField;
        private Label textOverlay;
        private Label outputLabel;
        private Label suggestionsLabel;

        private InputAction showHideToggleAction;

        #region Public Properties
        /// <summary>
        /// Whether or not the console is currently hidden.
        /// </summary>
        public bool IsHidden
        {
            get { return this.runtimeLuaConsoleUIDocument.rootVisualElement.style.display == DisplayStyle.None; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Hides the console.
        /// </summary>
        public void HideConsole()
        {
            this.runtimeLuaConsoleUIDocument.rootVisualElement.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Shows the console.
        /// </summary>
        public void ShowConsole()
        {
            this.runtimeLuaConsoleUIDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }
        #endregion

        #region Unity Loop Methods
        private void Start()
        {
            this.FindUIElements();
            this.ConfigureUIElements();
            this.RegisterUICallbacks();
            this.CreateConsoleEnvironment();
            this.BindDefaultLuaFunctions();

            this.HideConsole();
        }

        private void OnEnable()
        {
            this.RegisterVisibilityToggleCallback();
        }

        private void OnDisable()
        {
            this.DeregisterVisibilityToggleCallback();
        }
        #endregion

        #region Callbacks
        private void OnShowHideTogglePerformed(InputAction.CallbackContext context)
        {
            // Dont do anything if the user has a textfield focused (ie. they are typing).
            if (this.runtimeLuaConsoleUIDocument.rootVisualElement.focusController.focusedElement is TextField)
                return;

            if (this.IsHidden)
            {
                this.ShowConsole();
            }
            else
            {
                this.HideConsole();
            }
        }
        #endregion

        #region Private Methods
        private void FindUIElements()
        {
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
        }

        private void ConfigureUIElements()
        {
            // Update input field settings
            consoleInputField.selectAllOnFocus = false;
            consoleInputField.selectAllOnMouseUp = false;
            consoleInputField.multiline = true;
        }

        private void RegisterUICallbacks()
        {
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
        }

        private void CreateConsoleEnvironment()
        {
            // Create the console environment
            ConsoleEnvironmentLua consoleLuaEnvironment = new();
            this.consoleEnvironment = consoleLuaEnvironment;
            this.consoleEnvironment.OutputReceived += this.AppendOutput;
        }

        private void BindDefaultLuaFunctions()
        {
            // Bind example functions
            UserData.RegisterType<SceneUtilities>();
            this.consoleEnvironment.LuaScript.Globals["SceneUtils"] = UserData.Create(new SceneUtilities());
        }

        private void RegisterVisibilityToggleCallback()
        {
            if (
                this.showHideToggleActionReference != null
                && this.showHideToggleActionReference.action != null
            )
            {
                this.showHideToggleAction = this.showHideToggleActionReference.action;
            }
            else
            {
                this.showHideToggleAction = new InputAction(
                    name: "Toggle Console Visibility",
                    type: InputActionType.Button
                );
                this.showHideToggleAction.AddBinding("<Keyboard>/backquote");
                this.showHideToggleAction.Enable();
            }

            this.showHideToggleAction.performed += this.OnShowHideTogglePerformed;
        }

        private void DeregisterVisibilityToggleCallback()
        {
            if (this.showHideToggleAction != null)
            {
                this.showHideToggleAction.performed -= this.OnShowHideTogglePerformed;

                if (this.showHideToggleActionReference == null)
                {
                    this.showHideToggleAction.Disable();
                }
            }
        }

        private void AppendOutput(string message)
        {
            this.outputLabel.text += "=> " + message + "\n";
        }
        #endregion
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

