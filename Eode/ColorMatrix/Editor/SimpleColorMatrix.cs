using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(EODE.SimpleColorMatrix))]
public class SimpleColorMatrixEditor : Editor {
    bool _opened = false;
    ColorMatrixEditor _cme;

    public void OnEnable() {
        EODE.SimpleColorMatrix simpleColorMatrix = (EODE.SimpleColorMatrix)target;
        _cme = (ColorMatrixEditor)ColorMatrixEditor.CreateEditor(simpleColorMatrix.ColorMatrix != null ? simpleColorMatrix.ColorMatrix : CreateInstance<EODE.ColorMatrix>());
        _opened = simpleColorMatrix.ColorMatrix != null && simpleColorMatrix.ColorMatrix.Local;
    }

    public override void OnInspectorGUI() {
        EODE.SimpleColorMatrix simpleColorMatrix = (EODE.SimpleColorMatrix)target;

        var opened = _opened;

        _opened = !EditorGUILayout.BeginToggleGroup("Preset", !_opened);
        simpleColorMatrix.ColorMatrix = (EODE.ColorMatrix)EditorGUILayout.ObjectField(simpleColorMatrix.ColorMatrix, typeof(EODE.ColorMatrix), false);
        if (_opened && simpleColorMatrix.ColorMatrix != null)
            simpleColorMatrix.ColorMatrix.Local = false;
        EditorGUILayout.EndToggleGroup();

        _opened = EditorGUILayout.BeginToggleGroup("Make Unique", _opened);
        if (_opened)
            _cme.OnInspectorGUI();
        EditorGUILayout.EndToggleGroup();

        Save(_opened != opened);
    }

    private void Save(bool edit) {
        if (_opened)
        {
            EODE.SimpleColorMatrix simpleColorMatrix = (EODE.SimpleColorMatrix)target;
            simpleColorMatrix.ColorMatrix = ((EODE.ColorMatrix)_cme.target).Clone();
            simpleColorMatrix.ColorMatrix.Local = true;
        }
        else if (edit)
        {
            
            EODE.SimpleColorMatrix simpleColorMatrix = (EODE.SimpleColorMatrix)target;
            simpleColorMatrix.ColorMatrix = null;
        }
    }
}
