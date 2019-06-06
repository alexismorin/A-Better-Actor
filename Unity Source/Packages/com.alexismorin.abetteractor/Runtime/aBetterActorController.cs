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

    [Header ("Facial Animation Drivers")]
    [SerializeField]
    float volume;
    [SerializeField]
    float pitch;

    // Privates

    [HideInInspector]
    public float armIK;
    [HideInInspector]
    public float waveIK;

    int indexWalk = -1;
    int indexTalk = -1;
    int indexLook = -1;
    int indexPoint = -1;
    int indexWave = -1;

    AudioSource larynx;
    Animator anim;
    NavMeshAgent agent;
    Vector2 smoothDeltaPosition = Vector2.zero;
    Vector2 velocity = Vector2.zero;
    float defaultWalkVelocity;
    GameObject currentLookAtObject = null;
    GameObject currentPointAtObject = null;
    aBetterActorLookAt lookAtScript;
    float moodOffset = 0f;

    // AudioDetection

    int qSamples = 64;
    float refValue = 0.1f;
    float threshold = 0.02f;
    float rmsValue;
    float dbValue;
    float pitchValue;

    float[] samples;
    float[] spectrum;
    float fSample;
    float pitchBuffer;

    // Core Methods

    void Start () {
        samples = new float[qSamples];
        spectrum = new float[qSamples];
        fSample = 11000f;
        lookAtScript = GetComponent<aBetterActorLookAt> ();
        larynx = GetComponent<AudioSource> ();
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
            lookAtScript.lookAtTargetPosition = new Vector3 (currentLookAtObject.transform.position.x, currentLookAtObject.transform.position.y + moodOffset, currentLookAtObject.transform.position.z);
        }
        if (currentLookAtObject == null) {
            lookAtScript.lookAtTargetPosition = new Vector3 (agent.steeringTarget.x + transform.forward.x, agent.steeringTarget.y + moodOffset, agent.steeringTarget.z + transform.forward.z);
        }

        AnalyzeAudio ();

        if (dbValue >= pitchBuffer) {
            pitchBuffer = dbValue;

        }

        pitch = pitchValue;
        volume = Mathf.Abs ((1f / 180f) * (dbValue + 160f));

    }

    void OnAnimatorMove () {
        // Update position to agent position
        transform.position = agent.nextPosition;
    }

    void OnAnimatorIK () {
        anim.SetIKPositionWeight (AvatarIKGoal.RightHand, armIK);
        if (currentPointAtObject != null) {
            anim.SetIKPosition (AvatarIKGoal.RightHand, currentPointAtObject.transform.position);
        }

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

    GameObject GetClosestGameObject (string tag) {
        GameObject[] foundGameObjects = GameObject.FindGameObjectsWithTag (tag);
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        foreach (GameObject potentialTarget in foundGameObjects) {
            Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr) {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget.transform;
            }
        }

        return bestTarget.gameObject;
    }

    void AnalyzeAudio () {

        // I've been carrying this old-ass code from the unityscript days - thanks whoever wrote it in the first place

        larynx.GetOutputData (samples, 0);
        float sum = 0;
        for (int i = 0; i < qSamples; i++) {
            sum += samples[i] * samples[i];
        }
        rmsValue = Mathf.Sqrt (sum / qSamples);
        dbValue = 20 * Mathf.Log10 (rmsValue / refValue); // calculate dB
        if (dbValue < -160) {
            dbValue = -160;
        }

        // get sound spectrum
        larynx.GetSpectrumData (spectrum, 0, FFTWindow.Hanning);
        float maxV = 0f;
        int maxN = 0;
        for (int y = 0; y < qSamples; y++) { // find max 

            float intake = (spectrum[y] * 312499.935f) - 0.9999999f;

            if (intake > maxV && intake > threshold) {

                maxV = intake;
                maxN = y; // maxN is the index of max
            }

        }

        pitchValue = Mathf.Abs ((1f / 4000f) * maxV);

    }

    // _____________________________________ Signal Receiver Methods _____________________________________

    // Locomotion
    public void Sneak () {
        InternalMove (0.5f);
    }
    public void Walk () {
        InternalMove (0.8f);
    }
    public void Run () {
        InternalMove (2f);
    }

    // Dialogue
    public void Talk () {
        if (indexTalk < lines.Length - 1) {
            indexTalk++;
            larynx.PlayOneShot (lines[indexTalk]);
        }
    }

    // Body Language
    public void Wave () {
        anim.SetTrigger ("wave");
    }

    public void Point () {
        anim.SetTrigger ("point");
        if (lookAt.Length == 0) {
            currentPointAtObject = GetClosestGameObject ("pointOfInterest");
            return;
        }
        if (indexPoint < pointAt.Length - 1) {
            indexPoint++;
            currentPointAtObject = pointAt[indexPoint].gameObject;
        }

    }

    public void Look () {
        if (lookAt.Length == 0) {
            CancelInvoke ("ClearLookAt");
            currentLookAtObject = GetClosestGameObject ("pointOfInterest");
            Invoke ("ClearLookAt", lookAtDuration);
            return;
        }
        if (indexLook < lookAt.Length - 1) {
            CancelInvoke ("ClearLookAt");
            indexLook++;
            currentLookAtObject = lookAt[indexLook].gameObject;
            Invoke ("ClearLookAt", lookAtDuration);
        }

    }

    public void Bow () {
        anim.SetTrigger ("bow");
    }

    // Gait Modifiers
    public void Sadden () {
        moodOffset = -2f;
    }
    public void Brighten () {
        moodOffset = 4f;
    }
    public void Scare () {
        moodOffset = 0.5f;
    }
    public void Normalize () {
        moodOffset = 0f;
    }

    // Point of Interest Modifiers
    public void MakeBoring () {
        gameObject.tag = "Untagged";
    }
    public void MakeInteresting () {
        gameObject.tag = "pointOfInterest";
    }
}