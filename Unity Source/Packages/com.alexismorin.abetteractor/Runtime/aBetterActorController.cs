using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class aBetterActorController : MonoBehaviour {

    [Header ("Stage Directions")]

    [SerializeField]
    AudioClip[] lines;
    [SerializeField]
    string[] subtitles;
    [SerializeField]
    Transform[] locations;
    [SerializeField]
    Transform[] lookAt;
    [SerializeField]
    Transform[] pointAt;
    [SerializeField]
    Transform[] waveAt;

    [Header ("Actor Settings")]
    [SerializeField]
    float lookAtDuration = 3f;
    [SerializeField]
    GameObject subtitleManager;

    // Privates
    int indexWalk = -1;
    int indexTalk = -1;
    int indexLook = -1;
    int indexPoint = -1;
    int indexWave = -1;

    Animator anim;
    NavMeshAgent agent;
    Vector2 smoothDeltaPosition = Vector2.zero;
    Vector2 velocity = Vector2.zero;
    float defaultWalkVelocity;
    GameObject currentLookAtObject = null;
    aBetterActorLookAt lookAtScript;

    // Core Methods

    void Start () {
        lookAtScript = GetComponent<aBetterActorLookAt> ();
        agent = GetComponent<NavMeshAgent> ();
        anim = GetComponent<Animator> ();
        defaultWalkVelocity = agent.speed;
        agent.updatePosition = false;
    }

    void Update () {
        Vector3 worldDeltaPosition = agent.nextPosition - transform.position;

        // Map 'worldDeltaPosition' to local space
        float dx = Vector3.Dot (transform.right, worldDeltaPosition);
        float dy = Vector3.Dot (transform.forward, worldDeltaPosition);
        Vector2 deltaPosition = new Vector2 (dx, dy);

        // Low-pass filter the deltaMove
        float smooth = Mathf.Min (1.0f, Time.deltaTime / 0.15f);
        smoothDeltaPosition = Vector2.Lerp (smoothDeltaPosition, deltaPosition, smooth);

        // Update velocity if time advances
        if (Time.deltaTime > 1e-5f)
            velocity = smoothDeltaPosition / Time.deltaTime;

        bool shouldMove = velocity.magnitude > 0.5f && agent.remainingDistance > agent.radius;

        // Update animation parameters
        anim.SetBool ("moving", shouldMove);
        anim.SetFloat ("horizontalVelocity", velocity.x);
        anim.SetFloat ("verticalVelocity", velocity.y);

        if (currentLookAtObject != null) {
            lookAtScript.lookAtTargetPosition = currentLookAtObject.transform.position;
        }
        if (currentLookAtObject == null) {
            lookAtScript.lookAtTargetPosition = agent.steeringTarget + transform.forward;
        }

    }

    void OnAnimatorMove () {
        // Update position to agent position
        transform.position = agent.nextPosition;
    }

    void InternalMove (float newMoveSpeed) {
        agent.speed = defaultWalkVelocity * newMoveSpeed;
        if (indexWalk < locations.Length - 1) {
            indexWalk++;
            agent.destination = locations[indexWalk].transform.position;
        }
    }

    void ClearLookAt () {
        currentLookAtObject = null;
    }

    // _____________________________________ Signal Receiver Methods _____________________________________

    // Locomotion
    public void Sneak () {
        InternalMove (0.5f);
    }
    public void Walk () {
        InternalMove (1f);
    }
    public void Run () {
        InternalMove (2f);
    }

    // Dialogue
    public void Whisper () {

    }
    public void Talk () {

    }
    public void Shout () {

    }

    // Body Language
    public void Point () {

    }
    public void Wave () {

    }
    public void Look () {
        if (indexLook < lookAt.Length - 1) {
            CancelInvoke ("ClearLookAt");
            indexLook++;
            currentLookAtObject = lookAt[indexLook].gameObject;
            Invoke ("ClearLookAt", lookAtDuration);
        }

    }
    public void Bow () {

    }
    public void Dance () {

    }
    public void Crouch () {

    }

    // Gait Modifiers
    public void Sadden () {

    }
    public void Brighten () {

    }
    public void Anger () {

    }
    public void Scare () {

    }
    public void Normalize () {

    }

    // Point of Interest Modifiers
    public void MakeBoring () {

    }
    public void MakeInteresting () {

    }
}