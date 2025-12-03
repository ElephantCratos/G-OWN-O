using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using BNG;

public class ChargingStation : MonoBehaviour
{
    [Header("References")]
    public DayEventManager dayEventManager;
    public Transform cablePlugPoint; 
    public GameObject chargingCable; 
    public Grabbable cableGrabbable; 

    [Header("Settings")]
    public float plugDistance = 0.15f;
    public float chargingDelay = 1f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip plugInSound;
    public AudioClip plugInFailSound;
    public AudioClip chargingSound;
    public AudioClip unplugSound;

    [Header("Events")]
    public UnityEvent OnCablePluggedIn;
    public UnityEvent OnChargingStarted;
    public UnityEvent OnDayChanged;
    public UnityEvent OnCableUnplugged;
    public UnityEvent OnTasksNotCompleted;

    private bool isPluggedIn = false;
    private bool isCharging = false;
    private bool canUnplug = false;

    private Transform cableTransform;
    private Vector3 cableOriginalPosition;
    private Quaternion cableOriginalRotation;

    private bool wasGrabbedLastFrame = false;

    // NEW: VR Screen Fader (BNG)
    private ScreenFader fader;

    void Start()
    {
        // Save initial cable transform
        if (chargingCable != null)
        {
            cableTransform = chargingCable.transform;
            cableOriginalPosition = cableTransform.position;
            cableOriginalRotation = cableTransform.rotation;
        }

        // === VR FADER SETUP ===
        fader = Camera.main.GetComponentInChildren<ScreenFader>();

        if (fader == null)
        {
            // Create a fader automatically if it doesn't exist
            fader = Camera.main.gameObject.AddComponent<ScreenFader>();
            fader.FadeOnSceneLoaded = false; 
            fader.FadeColor = Color.black;
        }
    }

    void Update()
    {
        if (cableGrabbable == null) return;

        bool isGrabbedNow = cableGrabbable.BeingHeld;

        // Grabbed
        if (isGrabbedNow && !wasGrabbedLastFrame)
        {
            OnCableGrabbed();
        }
        // Released
        else if (!isGrabbedNow && wasGrabbedLastFrame)
        {
            OnCableReleased();
        }

        wasGrabbedLastFrame = isGrabbedNow;
    }

    private void OnCableGrabbed()
    {
        if (isPluggedIn && canUnplug)
        {
            UnplugCable();
        }
    }

    private void OnCableReleased()
    {
        if (!isPluggedIn)
        {
            TryPlugIn();
        }
    }

    private void TryPlugIn()
    {
        if (isPluggedIn || isCharging) return;

        float distance = Vector3.Distance(cableTransform.position, cablePlugPoint.position);

        if (distance <= plugDistance)
        {
            if (dayEventManager != null && dayEventManager.AreAllEventsCompleted())
            {
                PlugInCable();
            }
            else
            {
                PlaySound(plugInFailSound);
                OnTasksNotCompleted?.Invoke();

                StartCoroutine(ReturnCableToPlace());
            }
        }
    }

    private void PlugInCable()
    {
        isPluggedIn = true;
        canUnplug = false;

        if (cableGrabbable != null)
            cableGrabbable.enabled = false;

        cableTransform.position = cablePlugPoint.position;
        cableTransform.rotation = cablePlugPoint.rotation;
        cableTransform.SetParent(cablePlugPoint);

        Rigidbody rb = chargingCable.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        PlaySound(plugInSound);
        OnCablePluggedIn?.Invoke();

        StartCoroutine(StartChargingSequence());
    }

    private void UnplugCable()
    {
        isPluggedIn = false;
        canUnplug = false;

        cableTransform.SetParent(null);

        Rigidbody rb = chargingCable.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;

        if (cableGrabbable != null)
            cableGrabbable.enabled = true;

        PlaySound(unplugSound);
        OnCableUnplugged?.Invoke();
    }

    private IEnumerator StartChargingSequence()
    {
        isCharging = true;

        yield return new WaitForSeconds(chargingDelay);

        PlaySound(chargingSound);
        OnChargingStarted?.Invoke();

        // === VR FADE TO BLACK ===
        if (fader != null)
        {
            fader.DoFadeIn();
            yield return new WaitForSeconds(1f / fader.FadeInSpeed + 0.1f);
        }

        // DAY CHANGE
        if (dayEventManager != null)
        {
            dayEventManager.StartNewDay();
        }

        OnDayChanged?.Invoke();

        yield return new WaitForSeconds(0.5f);

        // === VR FADE FROM BLACK ===
        if (fader != null)
        {
            fader.DoFadeOut();
            yield return new WaitForSeconds(1f / fader.FadeOutSpeed + 0.1f);
        }

        isCharging = false;
        canUnplug = true;

        if (cableGrabbable != null)
            cableGrabbable.enabled = true;
    }

    private IEnumerator ReturnCableToPlace()
    {
        if (cableGrabbable != null)
            cableGrabbable.enabled = false;

        float duration = 0.5f;
        float elapsed = 0;
        Vector3 startPos = cableTransform.position;
        Quaternion startRot = cableTransform.rotation;

        Rigidbody rb = chargingCable.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            cableTransform.position = Vector3.Lerp(startPos, cableOriginalPosition, t);
            cableTransform.rotation = Quaternion.Lerp(startRot, cableOriginalRotation, t);

            yield return null;
        }

        cableTransform.position = cableOriginalPosition;
        cableTransform.rotation = cableOriginalRotation;

        if (rb != null)
            rb.isKinematic = false;

        if (cableGrabbable != null)
            cableGrabbable.enabled = true;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    // Public API
    public bool IsPluggedIn() => isPluggedIn;
    public bool IsCharging() => isCharging;
    public bool CanUnplug() => canUnplug;
}
