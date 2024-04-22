using UnityEngine;
using UnityEditor;

public class UpdatableData : ScriptableObject
{
    public event System.Action OnValueUpdated;
    public bool autoUpdate;

#if UNITY_EDITOR

    protected virtual void OnValidate()
    {
        if (autoUpdate)
        {
            EditorApplication.update += NotifyOfUpdatedValues;
        }
    }

    public void NotifyOfUpdatedValues()
    {
        EditorApplication.update -= NotifyOfUpdatedValues;
        if (OnValueUpdated != null)
        {
            OnValueUpdated ();
        }
    }
#endif
}
