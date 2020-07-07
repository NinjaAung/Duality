using UnityEngine;
using AmbientSounds;

public class PositionalEvent : MonoBehaviour {
    /// <summary> Event to set/unset based on position of PlayerObject </summary>
    [Tooltip("Event to set/unset based on position of PlayerObject")]
    public string EventName = "Event";
    /// <summary> PlayerObject to track (auto-set if left null from AmbienceManager's value or Camera.main) </summary>
    [Tooltip("PlayerObject to track (auto-set if left null from AmbienceManager's value or Camera.main)")]
    public Transform PlayerObject = null;
    /// <summary> How many dimensions should factor into Positional calculations </summary>
    [Tooltip("How many dimensions should factor into Positional calculations")]
    public Dimentions m_dimensions = Dimentions.THREE;
    /// <summary> Shape of area </summary>
    [Tooltip("Shape of area")]
    public Shape m_shape = Shape.RECTANGLE;
    /// <summary> Direction to ignore when 2D or to check when 1D </summary>
    [Tooltip("Direction to ignore when 2D or to check when 1D")]
    public Direction m_directon = Direction.Y;
    /// <summary> Size of area </summary>
    [Tooltip("Size of area")]
    public Vector3 m_areaSize = Vector3.one;
    /// <summary> Gizmo color to show in editor </summary>
    [Tooltip("Gizmo color to show in editor")]
    public Color m_displayColour = new Color(0f, 1f, 0f, 0.5f);

    /// <summary> If PlayerObject was within this Event last LateUpdate (prevents spamming system) </summary>
    bool lastWasSet = false;
#if UNITY_EDITOR
    /// <summary> Draws Gizmos for Unity Editor </summary>
    private void OnDrawGizmos() {
        Gizmos.color = m_displayColour;
        Color falloffColour = m_displayColour;
        falloffColour.a *= 0.33f;
        Vector3 Position = transform.position;
        Vector3 areaSize = m_areaSize;
        Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one);
        if (m_shape == Shape.SPHERE) {
            Vector3 scale = transform.lossyScale;
            float mult = (scale.x + scale.y + scale.z) / 3.0f;
            areaSize *= mult;
        } else {
            areaSize.Scale(transform.lossyScale);
        }
        if (m_dimensions == Dimentions.ONE) {
            switch (m_directon) {
                case Direction.X:
                    areaSize.y = areaSize.z = 1f;
                    break;
                case Direction.Y:
                    areaSize.x = areaSize.z = 1f;
                    break;
                case Direction.Z:
                    areaSize.x = areaSize.y = 1f;
                    break;
                default:
                    break;
            }
            Gizmos.DrawCube(transform.position, areaSize);
        } else if (m_shape == Shape.SPHERE) {
            if (m_dimensions == Dimentions.THREE)
                Gizmos.DrawSphere(transform.position, areaSize.x / 2);
            else {
                UnityEditor.Handles.color = m_displayColour;
                Vector3 drawDir;
                if (m_directon == Direction.X)
                    drawDir = Vector3.right;
                else if (m_directon == Direction.Y)
                    drawDir = Vector3.up;
                else
                    drawDir = Vector3.forward;
                UnityEditor.Handles.DrawSolidDisc(transform.position, drawDir, areaSize.x / 2);
                //added to allow click select of gizmo like the other types
                Gizmos.color = Color.clear;
                if (m_directon == Direction.X)
                    Gizmos.DrawCube(transform.position, new Vector3(0f, areaSize.x, areaSize.x));
                else if (m_directon == Direction.Y)
                    Gizmos.DrawCube(transform.position, new Vector3(areaSize.x, 0f, areaSize.x));
                else
                    Gizmos.DrawCube(transform.position, new Vector3(areaSize.x, areaSize.x, 0f));
            }
        } else {
            if (m_dimensions == Dimentions.TWO) {
                if (m_directon == Direction.X)
                    areaSize.x = 0f;
                else if (m_directon == Direction.Y)
                    areaSize.y = 0f;
                else
                    areaSize.z = 0f;
            }
            Gizmos.DrawCube(transform.position, areaSize);
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
    /// <summary> Draws Selected Gizmos for UnityEditor </summary>
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.white;
        Color falloffColour = m_displayColour;
        falloffColour.a *= 0.5f;
        Vector3 areaSize = m_areaSize;
        Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one);
        if (m_shape == Shape.SPHERE) {
            Vector3 scale = transform.lossyScale;
            float mult = (scale.x + scale.y + scale.z) / 3.0f;
            areaSize *= mult;
        } else {
            areaSize.Scale(transform.lossyScale);
        }

        if (m_dimensions == Dimentions.ONE) {
            switch (m_directon) {
                case Direction.X:
                    areaSize.y = areaSize.z = 1f;
                    break;
                case Direction.Y:
                    areaSize.x = areaSize.z = 1f;
                    break;
                case Direction.Z:
                    areaSize.x = areaSize.y = 1f;
                    break;
                default:
                    break;
            }
            Gizmos.DrawWireCube(transform.position, areaSize);
        } else if (m_shape == Shape.SPHERE) {
            if (m_dimensions == Dimentions.THREE)
                Gizmos.DrawWireSphere(transform.position, areaSize.x / 2);
            else {
                Vector3 drawDir;
                if (m_directon == Direction.X)
                    drawDir = Vector3.left;
                else if (m_directon == Direction.Y)
                    drawDir = Vector3.up;
                else
                    drawDir = Vector3.forward;
                UnityEditor.Handles.DrawWireDisc(transform.position, drawDir, areaSize.x * 0.5f);
            }
        } else {
            if (m_dimensions == Dimentions.TWO) {
                if (m_directon == Direction.X)
                    areaSize.x = 0f;
                else if (m_directon == Direction.Y)
                    areaSize.y = 0f;
                else
                    areaSize.z = 0f;
            }
            Gizmos.DrawWireCube(transform.position, areaSize);
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
    /// <summary> Gets whether position is within area of this PositionalEvent </summary>
    /// <param name="position">Position to check</param>
    /// <returns>If passed position is within this area</returns>
    bool isWithin(Vector3 position) {
        position = transform.InverseTransformPoint(position);
        Vector3 halfSize = m_areaSize * 0.5f;
        if (m_dimensions == Dimentions.ONE) {
            switch (m_directon) {
                case Direction.Z:
                    return Mathf.Abs(position.z) <= halfSize.z;
                case Direction.Y:
                    return Mathf.Abs(position.y) <= halfSize.y;
                case Direction.X:
                default:
                    return Mathf.Abs(position.x) <= halfSize.x;
            }
        }
        if (m_shape == Shape.SPHERE) { //Spheres are easy ... just get the distance and interpolate
            if (m_dimensions == Dimentions.TWO) {
                if (m_directon == Direction.X)
                    position.x = 0f;
                else if (m_directon == Direction.Y)
                    position.y = 0f;
                else
                    position.z = 0f;
            }
            float Dist = position.sqrMagnitude;
            return Dist <= halfSize.x * halfSize.x;
        } else {
            //rectangle or rectangular prizm
            bool withinX = (m_dimensions == Dimentions.TWO && m_directon == Direction.X) || Mathf.Abs(position.x) <= halfSize.x;
            bool withinY = (m_dimensions == Dimentions.TWO && m_directon == Direction.Y) || Mathf.Abs(position.y) <= halfSize.y;
            bool withinZ = (m_dimensions == Dimentions.TWO && m_directon == Direction.Z) || Mathf.Abs(position.z) <= halfSize.z;
            return withinX && withinY && withinZ;
        }
    }
    /// <summary> Checks position in LateUpdate after everything should have finished moving </summary>
    private void LateUpdate() {
        if (PlayerObject == null) {
            AmbienceManager manager = FindObjectOfType<AmbienceManager>();
            if (manager)
                PlayerObject = manager.m_playerObject;
            else {
                Camera mainCamera = Camera.main;
                if (mainCamera)
                    PlayerObject = mainCamera.transform;
                else {
                    Debug.LogWarning("No Player Object specified, no AmbienceManager found, and no Camera. Unable to auto-select object.", this);
                    return;
                }
            }
        }
        if (PlayerObject) {
            if (isWithin(PlayerObject.position)) {
                if (!lastWasSet) {
                    lastWasSet = true;
                    AmbienceManager.ActivateEvent(EventName);
                }
            } else {
                if (lastWasSet) {
                    lastWasSet = false;
                    AmbienceManager.DeactivateEvent(EventName);
                }
            }
        }
    }
}
