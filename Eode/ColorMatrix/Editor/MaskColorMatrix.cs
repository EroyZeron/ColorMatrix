using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(EODE.MaskColorMatrix))]
public class MaskColorMatrixEditor : Editor {
    bool _opened = false;
    ColorMatrixEditor _cme;

    public void OnEnable() {
        EODE.MaskColorMatrix filterColorMatrix = (EODE.MaskColorMatrix)target;
        _cme = (ColorMatrixEditor)ColorMatrixEditor.CreateEditor(filterColorMatrix.ColorMatrix != null ? filterColorMatrix.ColorMatrix : CreateInstance<EODE.ColorMatrix>());
        _opened = filterColorMatrix.ColorMatrix != null && filterColorMatrix.ColorMatrix.Local;
    }

    public override void OnInspectorGUI() {
        EODE.MaskColorMatrix filterColorMatrix = (EODE.MaskColorMatrix)target;

        EditorGUILayout.LabelField("Mask", EditorStyles.centeredGreyMiniLabel);
        filterColorMatrix.Mask = (Texture2D)EditorGUILayout.ObjectField(filterColorMatrix.Mask, typeof(Texture2D), false);

        EditorGUILayout.Space();

        #region matrix
        EditorGUILayout.LabelField("Matrix", EditorStyles.centeredGreyMiniLabel);

        var opened = _opened;

        _opened = !EditorGUILayout.BeginToggleGroup("Preset", !_opened);
        filterColorMatrix.ColorMatrix = (EODE.ColorMatrix)EditorGUILayout.ObjectField(filterColorMatrix.ColorMatrix, typeof(EODE.ColorMatrix), false);
        if (_opened && filterColorMatrix.ColorMatrix != null)
            filterColorMatrix.ColorMatrix.Local = false;
        EditorGUILayout.EndToggleGroup();

        _opened = EditorGUILayout.BeginToggleGroup("Make Unique", _opened);
        if (_opened)
            _cme.OnInspectorGUI();
        EditorGUILayout.EndToggleGroup();
        #endregion

        Save(_opened != opened);
    }

    private void Save(bool edit) {
        if (_opened)
        {
            EODE.MaskColorMatrix filterColorMatrix = (EODE.MaskColorMatrix)target;
            filterColorMatrix.ColorMatrix = ((EODE.ColorMatrix)_cme.target).Clone();
            filterColorMatrix.ColorMatrix.Local = true;
        }
        else if (edit)
        {
            EODE.MaskColorMatrix filterColorMatrix = (EODE.MaskColorMatrix)target;
            filterColorMatrix.ColorMatrix = null;
        }
    }
}
