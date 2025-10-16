using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepsEvents : MonoBehaviour
{
    public AudioClip[] footstepClips;
    public AudioClip jumpClip, landClip, deathClip;
    public float volStep = 0.6f, volJump = 0.9f, volLand = 0.7f, volDeath = 1f;

    AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        if (src) src.playOnAwake = false;
    }

    // Estes nomes aparecerão no dropdown "Function"
    public void Anim_Footstep()
    {
        if (footstepClips == null || footstepClips.Length == 0 || !src) return;
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        src.PlayOneShot(clip, volStep);
    }

    public void Anim_Jump()  { if (jumpClip)  src.PlayOneShot(jumpClip,  volJump);  }
    public void Anim_Land()  { if (landClip)  src.PlayOneShot(landClip,  volLand);  }
    public void Anim_Death() { if (deathClip) src.PlayOneShot(deathClip, volDeath); }
}
