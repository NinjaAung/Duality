// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;

namespace AmbientSounds {
    /// <summary> Position based audio selection </summary>
    [AddComponentMenu("Procedural Worlds/Ambient Sounds/Positional Sequence")]
    public class AudioArea : MonoBehaviour {
        #region Public Variables
        /// <summary> Array of Sequences to play </summary>
        public Sequence[] m_sequences = new Sequence[0];

        /// <summary> Output type to use for this PositionalSequence </summary>
        public OutputType m_outputType = OutputType.STRAIGHT;
        /// <summary> Prefab to spawn for AudioSource output. (null will create default empty object) </summary>
        public GameObject m_outputPrefab = null;
        /// <summary> Min/Max distance to place AudioSource when output type is not "STRAIGHT" </summary>
        public Vector2 m_outputDistance = Vector2.zero;
        /// <summary> Min/Max angle (in degrees) to place AudioSource when output type is not "STRAIGHT" </summary>
        public Vector2 m_outputVerticalAngle = new Vector2(-180, 180);
        /// <summary> Min/Max angle (in degrees) to place AudioSource when output type is not "STRAIGHT" </summary>
        public Vector2 m_outputHorizontalAngle = new Vector2(-180, 180);
        /// <summary> Does the output move with the Camera/Player/Area? </summary>
        public bool m_outputFollowPosition = true;
        /// <summary> Does the output rotate with the Camera/Player/Area? </summary>
        public bool m_outputFollowRotation = false;

        /// <summary> How many dimensions should factor into Positional calculations. </summary>
        public Dimentions m_dimensions = Dimentions.THREE;
        /// <summary> Shape of area </summary>
        public Shape m_shape = Shape.RECTANGLE;
        /// <summary> Direction to ignore when 2D or to check when 1D </summary>
        public Direction m_directon = Direction.Y;
        /// <summary> Size of area </summary>
        public Vector3 m_areaSize = Vector3.one;
        /// <summary> Falloff size around area </summary>
        public Vector3 m_falloff = Vector3.zero;

        /// <summary> Gizmo color to show in editor </summary>
        public Color m_displayColour = new Color(0f, 1f, 0f, 0.5f);
        /// <summary> Should gizmo always be drawn or only while selected? </summary>
        public bool m_alwaysDisplayGizmo = false;
        public float m_1D_GizmoSize = 20f;
        /// <summary> Used by editor to show fade values during play. </summary>
        [System.NonSerialized]
        public float m_lastFade = 0f;
        #endregion
        #region Public Functions
        /// <summary>
        /// Gets amount of this audio source to apply based on position and falloff values
        /// </summary>
        /// <param name="position">Position to base fade on</param>
        /// <returns>float between 0.0 and 1.0 for percent of this source to apply</returns>
        public float GetFade(Vector3 position) {
            position = transform.InverseTransformPoint(position);
            Vector3 halfSize = m_areaSize * 0.5f;
            if (m_dimensions == Dimentions.ONE) {
                switch (m_directon) {
                    case Direction.Z:
                        return m_lastFade = 1f - Mathf.Clamp01(m_falloff.z > 0f ? (Mathf.Abs(position.z) - halfSize.z) / m_falloff.z : 1f);
                    case Direction.Y:
                        return m_lastFade = 1f - Mathf.Clamp01(m_falloff.y > 0f ? (Mathf.Abs(position.y) - halfSize.y) / m_falloff.y : 1f);
                    case Direction.X:
                    default:
                        return m_lastFade = 1f - Mathf.Clamp01(m_falloff.x > 0f ? (Mathf.Abs(position.x) - halfSize.x) / m_falloff.x : 1f);
                }
            }
            if (m_dimensions == Dimentions.TWO) {
                if (m_directon == Direction.X)
                    position.x = 0f;
                else if (m_directon == Direction.Y)
                    position.y = 0f;
                else
                    position.z = 0f;
            }
            if (m_shape == Shape.SPHERE) { //Spheres are easy ... just get the distance and interpolate
                float Dist = position.sqrMagnitude;
                if (Dist <= halfSize.x * halfSize.x)
                    return m_lastFade = 1f;
                else if (m_falloff.x > 0f && Dist <= (halfSize.x + m_falloff.x) * (halfSize.x + m_falloff.x))
                    return m_lastFade = 1f - Mathf.Clamp01((Mathf.Sqrt(Dist) - halfSize.x) / m_falloff.x);
                else
                    return m_lastFade = 0f;
            } else {
                //rectangle or rectangular prizm ... first get if we are within the rectangle
                bool withinX = (m_dimensions == Dimentions.TWO && m_directon == Direction.X) || Mathf.Abs(position.x) <= halfSize.x;
                bool withinY = (m_dimensions == Dimentions.TWO && m_directon == Direction.Y) || Mathf.Abs(position.y) <= halfSize.y;
                bool withinZ = (m_dimensions == Dimentions.TWO && m_directon == Direction.Z) || Mathf.Abs(position.z) <= halfSize.z;
                if (withinX && withinY && withinZ)
                    return m_lastFade = 1f;
                //we are outside the rectangle but now check if we are within the falloff area (<0 = inside >1f = outside)
                float fadeX = withinX ? 1f : 1f - Mathf.Clamp01(m_falloff.x > 0f ? (Mathf.Abs(position.x) - halfSize.x) / m_falloff.x : 1f);
                float fadeY = withinY ? 1f : 1f - Mathf.Clamp01(m_falloff.y > 0f ? (Mathf.Abs(position.y) - halfSize.y) / m_falloff.y : 1f);
                float fadeZ = withinZ ? 1f : 1f - Mathf.Clamp01(m_falloff.z > 0f ? (Mathf.Abs(position.z) - halfSize.z) / m_falloff.z : 1f);
                if (fadeX <= 0f || fadeY <= 0f || fadeZ <= 0f)
                    return m_lastFade = 0f;
                return m_lastFade = Mathf.Min(fadeX, fadeY, fadeZ);
            }
        }
        #endregion
        #region Unity Lifecycle
        /// <summary> Adds this Positional to the AmbientSounds Manager when enabling </summary>
        private void OnEnable() {
            AmbienceManager.AddArea(this);
        }
        /// <summary> Removes this Positional from the AmbientSounds Manager when disabling/destroying </summary>
        private void OnDisable() {
            AmbienceManager.RemoveArea(this);
        }
        #endregion
        #region Gizmos
#if UNITY_EDITOR
        /// <summary> Draws Gizmos for Unity Editor </summary>
        private void OnDrawGizmos() {
            DrawGizmo();
        }
        /// <summary> Draws Selected Gizmos for UnityEditor </summary>
        private void OnDrawGizmosSelected() {
            DrawGizmo(true);
        }

        /// <summary> Internal function to draw Gizmos </summary>
        /// <param name="isSelection">Is this being called from OnDrawGizmosSelected event?</param>
        private void DrawGizmo(bool isSelection = false) {
            if (!m_alwaysDisplayGizmo && !isSelection)
                return;
            Color startGizmoColor = Gizmos.color;

            Vector3 areaSize = m_areaSize;
            Vector3 falloffSize = m_falloff;
            Vector3 myScale = transform.lossyScale;


            if (m_dimensions == Dimentions.ONE) {
                Vector3 planeSize = new Vector3();
                Vector3 planeOffset = new Vector3();
                switch (m_directon) {
                    case Direction.X:
                        planeOffset = new Vector3(m_areaSize.x * 0.5f, 0, 0);
                        planeSize = new Vector3(0f, m_1D_GizmoSize, m_1D_GizmoSize);
                        areaSize.y = areaSize.z = 1f;
                        myScale.y = myScale.z = 1f;
                        break;
                    case Direction.Y:
                        planeOffset = new Vector3(0, m_areaSize.y * 0.5f, 0);
                        planeSize = new Vector3(m_1D_GizmoSize, 0f, m_1D_GizmoSize);
                        areaSize.x = areaSize.z = 1f;
                        myScale.x = myScale.z = 1f;
                        break;
                    case Direction.Z:
                        planeOffset = new Vector3(0, 0, m_areaSize.z * 0.5f);
                        planeSize = new Vector3(m_1D_GizmoSize, m_1D_GizmoSize, 0f);
                        areaSize.x = areaSize.y = 1f;
                        myScale.x = myScale.y = 1f;
                        break;
                    default:
                        break;
                }
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, myScale);
                if (!isSelection || !m_alwaysDisplayGizmo) {
                    Gizmos.color = m_displayColour;
                    Gizmos.DrawCube(Vector3.zero, areaSize);
                    Gizmos.DrawCube(planeOffset, planeSize);
                    Gizmos.DrawCube(-planeOffset, planeSize);
                }
                if (isSelection) {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(Vector3.zero, areaSize);
                    Gizmos.DrawWireCube(planeOffset, planeSize);
                    Gizmos.DrawWireCube(-planeOffset, planeSize);
                }
                bool drawFalloff;
                switch (m_directon) {
                    case Direction.X:
                        drawFalloff = falloffSize.x > 0f;
                        planeOffset.x += falloffSize.x;
                        areaSize.x += falloffSize.x * 2f;
                        break;
                    case Direction.Y:
                        drawFalloff = falloffSize.y > 0f;
                        planeOffset.y += falloffSize.y;
                        areaSize.y += falloffSize.y * 2f;
                        break;
                    case Direction.Z:
                        drawFalloff = falloffSize.z > 0f;
                        planeOffset.z += falloffSize.z;
                        areaSize.z += falloffSize.z * 2f;
                        break;
                    default:
                        drawFalloff = false;
                        break;
                }
                if (drawFalloff) {
                    if (isSelection)
                        Gizmos.color = Color.white;
                    else {
                        Color falloffColour = m_displayColour;
                        falloffColour.a *= 0.33f;
                        Gizmos.color = falloffColour;
                    }
                    Gizmos.DrawWireCube(Vector3.zero, areaSize);
                    Gizmos.DrawWireCube(planeOffset, planeSize);
                    Gizmos.DrawWireCube(-planeOffset, planeSize);
                }
            } else {
                if (m_dimensions == Dimentions.TWO) {
                    if (m_directon == Direction.X) {
                        areaSize.x = 0f;
                        myScale.x = 0f;
                    } else if (m_directon == Direction.Y) {
                        areaSize.y = 0f;
                        myScale.y = 0f;
                    } else {
                        areaSize.z = 0f;
                        myScale.z = 0f;
                    }
                }
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, myScale);

                if (!isSelection || !m_alwaysDisplayGizmo) {
                    Gizmos.color = m_displayColour;
                    if (m_shape == Shape.SPHERE)
                        Gizmos.DrawSphere(Vector3.zero, areaSize.x * 0.5f);
                    else
                        Gizmos.DrawCube(Vector3.zero, areaSize);
                }
                if (isSelection) {
                    Gizmos.color = Color.white;
                    if (m_shape == Shape.SPHERE)
                        Gizmos.DrawWireSphere(Vector3.zero, areaSize.x * 0.5f);
                    else
                        Gizmos.DrawWireCube(Vector3.zero, areaSize);
                }
                if (falloffSize.x > 0f || falloffSize.z > 0f || falloffSize.y > 0f) {
                    areaSize += falloffSize * 2f;
                    if (m_shape != Shape.SPHERE && m_dimensions == Dimentions.TWO) {
                        if (m_directon == Direction.X)
                            areaSize.x = 0f;
                        else if (m_directon == Direction.Y)
                            areaSize.y = 0f;
                        else
                            areaSize.z = 0f;
                    }

                    if (isSelection) {
                        Gizmos.color = Color.white;
                    } else {
                        Color falloffColour = m_displayColour;
                        falloffColour.a *= 0.33f;
                        Gizmos.color = falloffColour;
                    }
                    if(m_shape == Shape.SPHERE)
                        Gizmos.DrawWireSphere(Vector3.zero, areaSize.x * 0.5f);
                    else
                        Gizmos.DrawWireCube(Vector3.zero, areaSize);
                }
            }
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = startGizmoColor;
        }
#endif
        #endregion
    }

}
