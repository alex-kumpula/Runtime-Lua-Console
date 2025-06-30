using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCG.RuntimeConsole.UI
{
    public class InputHistoryController
    {
        private readonly TextField inputField;

        // Live editing history (undo/redo within current edit)
        private readonly Stack<string> undoStack = new();
        private readonly Stack<string> redoStack = new();
        private string lastSnapshot = "";

        // Command submission history (navigated with up/down)
        private readonly List<string> commandHistory = new();
        private int commandHistoryIndex = -1;
        private string inProgressText = "";

        public InputHistoryController(TextField field)
        {
            inputField = field ?? throw new ArgumentNullException(nameof(field));
            lastSnapshot = inputField.value;

            inputField.RegisterValueChangedCallback(OnValueChanged);
            inputField.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            if (evt.newValue != lastSnapshot)
            {
                undoStack.Push(lastSnapshot);
                lastSnapshot = evt.newValue;
                redoStack.Clear(); // Invalidate redo
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.actionKey)
            {
                if (evt.keyCode == KeyCode.Z && undoStack.Count > 0)
                {
                    // Undo
                    redoStack.Push(inputField.value);
                    ApplyText(undoStack.Pop());
                    evt.StopPropagation();
                    return;
                }

                if (evt.keyCode == KeyCode.Y && redoStack.Count > 0)
                {
                    // Redo
                    undoStack.Push(inputField.value);
                    ApplyText(redoStack.Pop());
                    evt.StopPropagation();
                    return;
                }
            }

            if (evt.keyCode == KeyCode.UpArrow)
            {
                if (commandHistory.Count == 0)
                    return;

                if (commandHistoryIndex == -1)
                    inProgressText = inputField.value;

                commandHistoryIndex = Mathf.Clamp(commandHistoryIndex + 1, 0, commandHistory.Count - 1);
                ApplyText(commandHistory[commandHistory.Count - 1 - commandHistoryIndex]);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.DownArrow)
            {
                if (commandHistoryIndex == -1)
                    return;

                commandHistoryIndex--;

                if (commandHistoryIndex == -1)
                {
                    ApplyText(inProgressText);
                }
                else
                {
                    ApplyText(commandHistory[commandHistory.Count - 1 - commandHistoryIndex]);
                }

                evt.StopPropagation();
            }
        }

        private void ApplyText(string newValue)
        {
            inputField.SetValueWithoutNotify(newValue);
            inputField.cursorIndex = newValue.Length;
            lastSnapshot = newValue;
        }

        /// <summary>
        /// Call this when a command is submitted (e.g., on Enter)
        /// </summary>
        public void RecordCommand(string command)
        {
            if (!string.IsNullOrWhiteSpace(command))
            {
                commandHistory.Add(command);
                if (commandHistory.Count > 100)
                    commandHistory.RemoveAt(0);
            }

            commandHistoryIndex = -1;
            inProgressText = "";
        }

        public void ClearUndoHistory()
        {
            undoStack.Clear();
            redoStack.Clear();
            lastSnapshot = inputField.value;
        }

        public void ClearCommandHistory()
        {
            commandHistory.Clear();
            commandHistoryIndex = -1;
            inProgressText = "";
        }
    }
}