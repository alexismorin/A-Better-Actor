using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class actorPointOfInterest : MonoBehaviour {
#if UNITY_EDITOR
    void OnDrawGizmos () {
        Gizmos.color = Color.green;
        Gizmos.DrawLine (transform.position, transform.position + Vector3.up);
    }
#endif
}