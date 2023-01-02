using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    /// <summary>
    /// The axis about which the object will rotate.
    /// </summary>
    [Tooltip("Specifies the axis about which the object will rotate.")]
    [SerializeField]
    private PivotAxis pivotAxis = PivotAxis.XY;

    /// <summary>
    /// Rotational Pivot axis for orientating an object
    /// </summary>
    public enum PivotAxis {
        // Most common options, preserving current functionality with the same enum order.
        XY,
        Y,
        // Rotate about an individual axis.
        X,
        Z,
        // Rotate about a pair of axes.
        XZ,
        YZ,
        // Rotate about all axes.
        Free
    }

    /// <summary>
    /// The target we will orient to. If no target is specified, the main camera will be used.
    /// </summary>
    public Transform TargetTransform {
        get { return targetTransform; }
        set { targetTransform = value; }
    }

    [Tooltip("Specifies the target we will orient to. If no target is specified, the main camera will be used.")]
    [SerializeField]
    private Transform targetTransform;

    private void OnEnable() {
        if (targetTransform == null) {
            targetTransform = Camera.main.transform;
        }
    }

    /// <summary>
    /// Keeps the object facing the camera.
    /// </summary>
    private void Update() {
        if (targetTransform == null) {
            return;
        }

        // Get a Vector that points from the target to the main camera.
        Vector3 directionToTarget = targetTransform.position - transform.position;

        bool useCameraAsUpVector = true;

        // Adjust for the pivot axis.
        switch (pivotAxis) {
            case PivotAxis.X:
                directionToTarget.x = 0.0f;
                useCameraAsUpVector = false;
                break;

            case PivotAxis.Y:
                directionToTarget.y = 0.0f;
                useCameraAsUpVector = false;
                break;

            case PivotAxis.Z:
                directionToTarget.x = 0.0f;
                directionToTarget.y = 0.0f;
                break;

            case PivotAxis.XY:
                useCameraAsUpVector = false;
                break;

            case PivotAxis.XZ:
                directionToTarget.x = 0.0f;
                break;

            case PivotAxis.YZ:
                directionToTarget.y = 0.0f;
                break;

            case PivotAxis.Free:
            default:
                // No changes needed.
                break;
        }

        // If we are right next to the camera the rotation is undefined. 
        if (directionToTarget.sqrMagnitude < 0.001f) {
            return;
        }

        // Calculate and apply the rotation required to reorient the object
        if (useCameraAsUpVector) {
            transform.localRotation = Quaternion.LookRotation(-directionToTarget, Camera.main.transform.up);
        } else {
            transform.localRotation = Quaternion.LookRotation(-directionToTarget);
        }
    }
}
