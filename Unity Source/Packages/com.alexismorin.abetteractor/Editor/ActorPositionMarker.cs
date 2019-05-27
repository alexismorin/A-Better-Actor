using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class actorPositionMarker : MonoBehaviour {
#if UNITY_EDITOR
    void OnDrawGizmos () {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine (transform.position, transform.position + Vector3.up);
    }
#endif
}