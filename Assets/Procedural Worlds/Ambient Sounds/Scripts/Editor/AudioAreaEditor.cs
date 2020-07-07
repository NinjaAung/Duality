// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using PWCommon2;
using AmbientSounds.Internal;

/*
 * Custom Editor for PositionalSequence Components
 */

namespace AmbientSounds {
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioArea))]
    public class AudioAreaEditor : PWEditor, IPWEditor {
        #region Internal Variables
        /// <summary> Reference to EditorUtils for GUI functions </summary>
        private EditorUtils m_editorUtils;
        //Properties of PositionalSequence object
        SerializedProperty m_sequencesProp = null;
        SerializedProperty m_outputTypeProp = null;
        SerializedProperty m_outputPrefabProp = null;
        SerializedProperty m_outputDistanceProp = null;
        SerializedProperty m_outputVerticalAngleProp = null;
        SerializedProperty m_outputHorizontalAngleProp = null;
        SerializedProperty m_shapeProp = null;
        SerializedProperty m_dimensionsProp = null;
        SerializedProperty m_directonProp = null;
        SerializedProperty m_areaSizeProp = null;
        SerializedProperty m_falloffProp = null;
        SerializedProperty m_alwaysDisplayGizmoProp = null;
        SerializedProperty m_displayColourProp = null;
        SerializedProperty m_outputFollowPosition = null;
        SerializedProperty m_outputFollowRotation = null;
        SerializedProperty m_1D_GizmoSize = null;

        /// <summary> Animated Boolean for if Output Distance should be shown </summary>
        AnimBool outputBool = null;
        /// <summary> Animated Boolean for if Shape should be shown </summary>
        AnimBool shapeBool = null;
        /// <summary> Animated Boolean for if Direction should be shown </summary>
        AnimBool directionBool = null;
        
        UnityEditorInternal.ReorderableList m_sequencesReorderable;
        #endregion
        /// <summary> Destructor to release references </summary>
        private void OnDestroy() {
            if (m_editorUtils != null) {
                m_editorUtils.Dispose();
            }
        }
        /// <summary> Constructor to set up references </summary>
        void OnEnable() {
            m_sequencesProp = serializedObject.FindProperty("m_sequences");
            m_outputTypeProp = serializedObject.FindProperty("m_outputType");
            m_outputPrefabProp = serializedObject.FindProperty("m_outputPrefab");
            m_outputDistanceProp = serializedObject.FindProperty("m_outputDistance");
            m_outputVerticalAngleProp = serializedObject.FindProperty("m_outputVerticalAngle");
            m_outputHorizontalAngleProp = serializedObject.FindProperty("m_outputHorizontalAngle");
            m_shapeProp = serializedObject.FindProperty("m_shape");
            m_dimensionsProp = serializedObject.FindProperty("m_dimensions");
            m_directonProp = serializedObject.FindProperty("m_directon");
            m_areaSizeProp = serializedObject.FindProperty("m_areaSize");
            m_falloffProp = serializedObject.FindProperty("m_falloff");
            m_alwaysDisplayGizmoProp = serializedObject.FindProperty("m_alwaysDisplayGizmo");
            m_displayColourProp = serializedObject.FindProperty("m_displayColour");
            m_outputFollowPosition = serializedObject.FindProperty("m_outputFollowPosition");
            m_outputFollowRotation = serializedObject.FindProperty("m_outputFollowRotation");
            m_1D_GizmoSize = serializedObject.FindProperty("m_1D_GizmoSize");

            OutputType outputType = ( OutputType)System.Enum.GetValues(typeof( OutputType)).GetValue(m_outputTypeProp.enumValueIndex);
             Dimentions dimensions = ( Dimentions)System.Enum.GetValues(typeof( Dimentions)).GetValue(m_dimensionsProp.enumValueIndex);

            outputBool = new AnimBool(outputType !=  OutputType.STRAIGHT);
            outputBool.valueChanged.AddListener(Repaint);

            shapeBool = new AnimBool(dimensions !=  Dimentions.ONE);
            shapeBool.valueChanged.AddListener(Repaint);

            directionBool = new AnimBool(dimensions !=  Dimentions.THREE);
            directionBool.valueChanged.AddListener(Repaint);

            m_sequencesReorderable = new UnityEditorInternal.ReorderableList(serializedObject, m_sequencesProp, true, false, true, true);
            m_sequencesReorderable.elementHeight = EditorGUIUtility.singleLineHeight;
            m_sequencesReorderable.drawElementCallback = DrawSequenceElement;
            m_sequencesReorderable.drawHeaderCallback = DrawSequenceHeader;

            if (m_editorUtils == null) {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }
        /// <summary> Main GUI function </summary>
        public override void OnInspectorGUI() {
            serializedObject.Update();

            m_editorUtils.Initialize(); // Do not remove this!

            m_editorUtils.Panel("SequencesPanel", SequencesPanel, true);
            m_editorUtils.Panel("OutputPanel", OutputPanel, true);
            m_editorUtils.Panel("ShapePanel", ShapePanel, true);
            m_editorUtils.Panel("GizmoPanel", GizmoPanel, true);
            
            //m_editorUtils.GUIFooter();

            serializedObject.ApplyModifiedProperties();
        }
        void DrawSequenceElement(Rect rect, int index, bool isActive, bool isFocused) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.ObjectField(rect, m_sequencesProp.GetArrayElementAtIndex(index), GUIContent.none);
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawSequenceHeader(Rect rect) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(rect, PropertyCount("mSequences", m_sequencesProp));
            EditorGUI.indentLevel = oldIndent;
        }
        /// <summary> Panel to display Sequences to play </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void SequencesPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            m_sequencesReorderable.DoLayoutList();
            m_editorUtils.InlineHelp("mSequences", inlineHelp);
            --EditorGUI.indentLevel;
        }
        /// <summary> Panel to display Output options </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void OutputPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            m_editorUtils.EnumPopupLocalized("mOutputType", "OutputType_", m_outputTypeProp, inlineHelp);
             OutputType outputType = ( OutputType)System.Enum.GetValues(typeof( OutputType)).GetValue(m_outputTypeProp.enumValueIndex);
            outputBool.target = outputType !=  OutputType.STRAIGHT;
            if (EditorGUILayout.BeginFadeGroup(outputBool.faded)) {
                EditorGUI.BeginChangeCheck();
                Object prefab = m_editorUtils.ObjectField("mOutputPrefab", m_outputPrefabProp.objectReferenceValue, typeof(GameObject), false, inlineHelp);
                if (EditorGUI.EndChangeCheck())
                m_outputPrefabProp.objectReferenceValue = prefab;
                if (m_outputPrefabProp != null && m_outputPrefabProp.objectReferenceValue != null)
                {
                    AudioSource prefabSource = ((GameObject)m_outputPrefabProp.objectReferenceValue).GetComponent<AudioSource>();
                    if (prefabSource == null)
                    {
                        EditorGUILayout.HelpBox(m_editorUtils.GetContent("PrefabNoSource").text, MessageType.Info);
                    }
                    else
                    {
                        bool spatialize = false;
                        bool spatialblend = false;
                        if (prefabSource.spatialize == false)
                            spatialize = true;
                        if (prefabSource.spatialBlend < 1f)
                            spatialblend = true;
                        if (spatialize || spatialblend)
                        {
                            EditorGUILayout.HelpBox(m_editorUtils.GetContent("PrefabSettingsWarningPrefix").text
                                + (spatialize ? m_editorUtils.GetContent("PrefabSettingsWarningSpatialize").text : "")
                                + (spatialblend ? m_editorUtils.GetContent("PrefabSettingsWarningSpatialBlend").text : "")
                                , MessageType.Warning);
                            if (m_editorUtils.Button("PrefabSettingsButton"))
                            {
                                prefabSource.spatialize = true;
                                prefabSource.spatialBlend = 1.0f;
                                EditorUtility.SetDirty(m_outputPrefabProp.objectReferenceValue);
#if UNITY_2018_3_OR_NEWER
                        PrefabUtility.SavePrefabAsset(prefabSource.gameObject);
#else
                                PrefabUtility.ReplacePrefab(prefabSource.gameObject, m_outputPrefabProp.objectReferenceValue);
#endif
                            }
                        }
                    }
                }
                Rect r = EditorGUILayout.GetControlRect();
                float[] vals = new float[2] { m_outputDistanceProp.vector2Value.x, m_outputDistanceProp.vector2Value.y };
                EditorGUI.BeginChangeCheck();
                EditorGUI.MultiFloatField(r, m_editorUtils.GetContent("mOutputDistance"), new GUIContent[] { new GUIContent(""), new GUIContent("-") }, vals);
                if (EditorGUI.EndChangeCheck())
                    m_outputDistanceProp.vector2Value = new Vector2(vals[0], vals[1]);
                m_editorUtils.SliderRange("mOutputVerticalAngle", m_outputVerticalAngleProp, inlineHelp, -180, 180);
                m_editorUtils.SliderRange("mOutputHorizontalAngle", m_outputHorizontalAngleProp, inlineHelp, -180, 180);
                m_outputFollowPosition.boolValue = m_editorUtils.Toggle("mOutputFollowPosition", m_outputFollowPosition.boolValue, inlineHelp);
                EditorGUI.BeginDisabledGroup(!m_outputFollowPosition.boolValue);
                m_outputFollowRotation.boolValue = m_editorUtils.Toggle("mOutputFollowRotation", m_outputFollowRotation.boolValue, inlineHelp);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFadeGroup();
            --EditorGUI.indentLevel;
        }
        /// <summary> Panel to show shape options </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void ShapePanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            m_editorUtils.EnumPopupLocalized("mDimensions", "Dimensions_", m_dimensionsProp, inlineHelp);
             Dimentions dimensions = ( Dimentions)System.Enum.GetValues(typeof( Dimentions)).GetValue(m_dimensionsProp.enumValueIndex);
            shapeBool.target = dimensions !=  Dimentions.ONE;
            if (EditorGUILayout.BeginFadeGroup(shapeBool.faded)) {
                m_editorUtils.EnumPopupLocalized("mShape", "Shape_", m_shapeProp, inlineHelp);
            }
            EditorGUILayout.EndFadeGroup();
             Shape shape = ( Shape)System.Enum.GetValues(typeof( Shape)).GetValue(m_shapeProp.enumValueIndex);
            directionBool.target = dimensions !=  Dimentions.THREE;
            if (EditorGUILayout.BeginFadeGroup(directionBool.faded)) {
                m_editorUtils.PropertyField("mDirection", m_directonProp, inlineHelp);
            }
            EditorGUILayout.EndFadeGroup();
             Direction direction = ( Direction)System.Enum.GetValues(typeof( Direction)).GetValue(m_directonProp.enumValueIndex);
            if (dimensions ==  Dimentions.ONE) {
                float tmpVal;
                switch (direction) {
                    case  Direction.X:
                        tmpVal = m_areaSizeProp.vector3Value.x;
                        break;
                    case  Direction.Y:
                        tmpVal = m_areaSizeProp.vector3Value.y;
                        break;
                    case  Direction.Z:
                    default:
                        tmpVal = m_areaSizeProp.vector3Value.z;
                        break;
                }
                EditorGUI.BeginChangeCheck();
                tmpVal = Mathf.Max(m_editorUtils.FloatField("mSize1D", tmpVal, inlineHelp), 0f);
                if (EditorGUI.EndChangeCheck()) {
                    Vector3 areaSize = m_areaSizeProp.vector3Value;
                    switch (direction) {
                        case  Direction.X:
                            areaSize.x = tmpVal;
                            break;
                        case  Direction.Y:
                            areaSize.y = tmpVal;
                            break;
                        case  Direction.Z:
                        default:
                            areaSize.z = tmpVal;
                            break;
                    }
                    m_areaSizeProp.vector3Value = areaSize;
                }
                switch (direction) {
                    case  Direction.X:
                        tmpVal = m_falloffProp.vector3Value.x;
                        break;
                    case  Direction.Y:
                        tmpVal = m_falloffProp.vector3Value.y;
                        break;
                    case  Direction.Z:
                    default:
                        tmpVal = m_falloffProp.vector3Value.z;
                        break;
                }
                EditorGUI.BeginChangeCheck();
                tmpVal = Mathf.Max(m_editorUtils.FloatField("mFalloff1D", tmpVal, inlineHelp), 0f);
                if (EditorGUI.EndChangeCheck()) {
                    Vector3 falloff = m_falloffProp.vector3Value;
                    switch (direction) {
                        case  Direction.X:
                            falloff.x = tmpVal;
                            break;
                        case  Direction.Y:
                            falloff.y = tmpVal;
                            break;
                        case  Direction.Z:
                        default:
                            falloff.z = tmpVal;
                            break;
                    }
                    m_falloffProp.vector3Value = falloff;
                }
            } else if (shape ==  Shape.SPHERE) {
                Vector3 oldVal = m_areaSizeProp.vector3Value;
                EditorGUI.BeginChangeCheck();
                oldVal.x = Mathf.Max(m_editorUtils.FloatField("mSizeSphere", oldVal.x, inlineHelp), 0f);
                if (EditorGUI.EndChangeCheck()) {
                    m_areaSizeProp.vector3Value = oldVal;
                }

                oldVal = m_falloffProp.vector3Value;
                EditorGUI.BeginChangeCheck();
                oldVal.x = Mathf.Max(m_editorUtils.FloatField("mFalloffSphere", oldVal.x, inlineHelp), 0f);
                if (EditorGUI.EndChangeCheck()) {
                    oldVal.x = Mathf.Max(oldVal.x, 0f);
                    m_falloffProp.vector3Value = oldVal;
                }
            } else {
                if (dimensions ==  Dimentions.THREE) {
                    EditorGUI.BeginChangeCheck();
                    Vector3 areaSize = m_editorUtils.Vector3Field("mSize3D", m_areaSizeProp.vector3Value, inlineHelp);
                    if (EditorGUI.EndChangeCheck()) {
                        areaSize.x = Mathf.Max(0f, areaSize.x);
                        areaSize.y = Mathf.Max(0f, areaSize.y);
                        areaSize.z = Mathf.Max(0f, areaSize.z);
                        m_areaSizeProp.vector3Value = areaSize;
                    }
                    EditorGUI.BeginChangeCheck();
                    Vector3 falloffSize = m_editorUtils.Vector3Field("mFalloff3D", m_falloffProp.vector3Value, inlineHelp);
                    if (EditorGUI.EndChangeCheck()) {
                        falloffSize.x = Mathf.Max(0f, falloffSize.x);
                        falloffSize.y = Mathf.Max(0f, falloffSize.y);
                        falloffSize.z = Mathf.Max(0f, falloffSize.z);
                        m_falloffProp.vector3Value = falloffSize;
                    }
                } else {
                    Vector3 oldVal = m_areaSizeProp.vector3Value;
                    Vector2 newVal;
                    if (direction ==  Direction.X)
                        newVal = new Vector2(oldVal.y, oldVal.z);
                    else if (direction ==  Direction.Y)
                        newVal = new Vector2(oldVal.x, oldVal.z);
                    else
                        newVal = new Vector2(oldVal.x, oldVal.y);
                    EditorGUI.BeginChangeCheck();
                    newVal = m_editorUtils.Vector2Field("mSize2D", newVal, inlineHelp);
                    if (EditorGUI.EndChangeCheck()) {
                        if (direction ==  Direction.X)
                            oldVal.y = Mathf.Max(newVal.x, 0f);
                        else
                            oldVal.x = Mathf.Max(newVal.x, 0f);
                        if (direction !=  Direction.Z)
                            oldVal.z = Mathf.Max(newVal.y, 0f);
                        else
                            oldVal.y = Mathf.Max(newVal.y, 0f);
                        m_areaSizeProp.vector3Value = oldVal;
                    }

                    oldVal = m_falloffProp.vector3Value;
                    if (direction ==  Direction.X)
                        newVal = new Vector2(oldVal.y, oldVal.z);
                    else if (direction ==  Direction.Y)
                        newVal = new Vector2(oldVal.x, oldVal.z);
                    else
                        newVal = new Vector2(oldVal.x, oldVal.y);
                    EditorGUI.BeginChangeCheck();
                    newVal = m_editorUtils.Vector2Field("mFalloff2D", newVal, inlineHelp);
                    if (EditorGUI.EndChangeCheck()) {
                        if (direction ==  Direction.X)
                            oldVal.y = Mathf.Max(newVal.x, 0f);
                        else
                            oldVal.x = Mathf.Max(newVal.x, 0f);
                        if (direction !=  Direction.Z)
                            oldVal.z = Mathf.Max(newVal.y, 0f);
                        else
                            oldVal.y = Mathf.Max(newVal.y, 0f);
                        m_falloffProp.vector3Value = oldVal;
                    }
                }
            }
            --EditorGUI.indentLevel;
        }
        /// <summary> Panel to show Gizmo options </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void GizmoPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            m_alwaysDisplayGizmoProp.boolValue = m_editorUtils.Toggle("mAlwaysDisplayGizmoProp", m_alwaysDisplayGizmoProp.boolValue, inlineHelp);
            m_displayColourProp.colorValue = m_editorUtils.ColorField("mDisplayColor", m_displayColourProp.colorValue, inlineHelp);
            if ((Dimentions)System.Enum.GetValues(typeof(Dimentions)).GetValue(m_dimensionsProp.enumValueIndex) == Dimentions.ONE)
                m_1D_GizmoSize.floatValue = Mathf.Max(0f, m_editorUtils.FloatField("m1DGizmoSize", m_1D_GizmoSize.floatValue));
            --EditorGUI.indentLevel;
        }
        private GUIContent PropertyCount(string key, SerializedProperty property) {
            GUIContent content = m_editorUtils.GetContent(key);
            content.text += " [" + property.arraySize + "]";
            return content;
        }
    }
}