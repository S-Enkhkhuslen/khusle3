using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

// Callbacks remain reliable even when a key press and release arrive in one frame.
public sealed class UIKeyActions : IDisposable
{
    private readonly List<InputAction> actions = new List<InputAction>();
    public void Add(string key, Action callback)
    {
        var action = new InputAction(key, InputActionType.Button, "<Keyboard>/" + key);
        action.performed += context => callback();
        action.Enable(); actions.Add(action);
    }
    public void Dispose()
    {
        foreach (InputAction action in actions) { action.Disable(); action.Dispose(); }
        actions.Clear();
    }
}
