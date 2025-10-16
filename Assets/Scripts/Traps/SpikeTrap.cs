using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpikeTrap : MonoBehaviour
{
    private static readonly Dictionary<string, List<SpikeTrap>> GroupRegistry = new Dictionary<string, List<SpikeTrap>>();

    [Header("Trap Type")]
    [SerializeField] private bool useTimedTrap = true; // false = trap always active

    [Header("References")]
    [SerializeField] private Transform spikeVisual;
    [SerializeField] private Collider2D triggerCollider;  // area that detects the player
    [SerializeField] private Collider2D damageCollider;   // damage collider (optional)

    [Header("Movement")]
    [SerializeField] private Vector2 activeDisplacement = new Vector2(0f, 1f);
    [SerializeField] private float moveDuration = 0.15f;
    [SerializeField] private AnimationCurve moveCurve = null;

    [Header("Timed Trap")]
    [Tooltip("Delay between detecting the player and raising spikes.")]
    [SerializeField] private float activationDelay = 0.1f;
    [Tooltip("How long the spikes stay active before retracting.")]
    [SerializeField] private float activeDuration = 1.5f;
    [Tooltip("Extra cooldown before the trap can trigger again.")]
    [SerializeField] private float cooldown = 1.0f;
    [Tooltip("Traps that share this group id fire together.")]
    [SerializeField] private string trapGroupId = string.Empty;
    [Tooltip("Starts hidden with spikes retracted.")]
    [SerializeField] private bool startHidden = true;

    [Header("Feedback (optional)")]
    [SerializeField] private AudioSource activationSound;

    private Vector3 restLocalPosition;
    private Coroutine moveRoutine;
    private Coroutine activationRoutine;
    private float nextAvailableTime;
    private bool isActive;

    private void Awake()
    {
        if (!spikeVisual) spikeVisual = transform;
        restLocalPosition = spikeVisual.localPosition;

        if (!triggerCollider)
            triggerCollider = GetComponent<Collider2D>();

        if (!damageCollider)
            damageCollider = triggerCollider;

        if (useTimedTrap && triggerCollider)
            triggerCollider.isTrigger = true;

        if (moveCurve == null)
            moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    private void OnEnable()
    {
        RegisterInGroup();
    }

    private void OnDisable()
    {
        UnregisterFromGroup();
    }

    private void Start()
    {
        if (useTimedTrap)
        {
            bool startActive = !startHidden;
            SetTrapState(startActive, true);
        }
        else
        {
            SetTrapState(true, true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (useTimedTrap)
            TryActivate();

        if (isActive)
            KillPlayer(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive)
            return;

        if (collision.collider.CompareTag("Player"))
            KillPlayer(collision.collider);
    }

    private void TryActivate(bool force = false)
    {
        if (!force && Time.time < nextAvailableTime)
            return;

        if (!string.IsNullOrEmpty(trapGroupId))
        {
            TriggerGroup(force);
            return;
        }

        ScheduleActivation(force);
    }

    private void TriggerGroup(bool force)
    {
        if (!GroupRegistry.TryGetValue(trapGroupId, out var members) || members == null)
        {
            ScheduleActivation(force);
            return;
        }

        for (int i = 0; i < members.Count; i++)
        {
            SpikeTrap trap = members[i];
            if (!trap || !trap.isActiveAndEnabled)
                continue;

            trap.ScheduleActivation(true);
        }
    }

    private void ScheduleActivation(bool force)
    {
        if (!force && Time.time < nextAvailableTime)
            return;

        if (activationRoutine != null)
            StopCoroutine(activationRoutine);

        activationRoutine = StartCoroutine(ActivationSequence());
    }

    private IEnumerator ActivationSequence()
    {
        float lockTime = Mathf.Max(0f, activationDelay + activeDuration + cooldown);
        nextAvailableTime = Time.time + lockTime;

        if (activationDelay > 0f)
            yield return new WaitForSeconds(activationDelay);

        if (activationSound)
            activationSound.Play();

        SetTrapState(true);

        if (activeDuration > 0f)
            yield return new WaitForSeconds(activeDuration);

        SetTrapState(false);
        activationRoutine = null;
    }

    private void SetTrapState(bool active, bool instant = false)
    {
        isActive = active;

        if (damageCollider && damageCollider != triggerCollider)
            damageCollider.enabled = active;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        Vector3 target = restLocalPosition + (active ? (Vector3)activeDisplacement : Vector3.zero);

        if (instant || moveDuration <= 0.0001f)
        {
            spikeVisual.localPosition = target;
        }
        else
        {
            moveRoutine = StartCoroutine(MoveRoutine(target));
        }
    }

    private IEnumerator MoveRoutine(Vector3 target)
    {
        Vector3 from = spikeVisual.localPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float eased = moveCurve.Evaluate(t);
            spikeVisual.localPosition = Vector3.LerpUnclamped(from, target, eased);
            yield return null;
        }

        spikeVisual.localPosition = target;
        moveRoutine = null;
    }

    private void KillPlayer(Collider2D playerCollider)
    {
        if (GameController.instance != null)
        {
            GameController.instance.LoseLifeFromHit(transform.position);
        }
    }

    private void RegisterInGroup()
    {
        if (string.IsNullOrEmpty(trapGroupId))
            return;

        if (!GroupRegistry.TryGetValue(trapGroupId, out var list))
        {
            list = new List<SpikeTrap>();
            GroupRegistry.Add(trapGroupId, list);
        }

        if (!list.Contains(this))
            list.Add(this);
    }

    private void UnregisterFromGroup()
    {
        if (string.IsNullOrEmpty(trapGroupId))
            return;

        if (!GroupRegistry.TryGetValue(trapGroupId, out var list))
            return;

        list.Remove(this);
        if (list.Count == 0)
            GroupRegistry.Remove(trapGroupId);
    }
}
