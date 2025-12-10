using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSoundController : MonoBehaviour
{
    public AudioClip[] FootstepAudioClips;
    public AudioClip JumpLand;
    public AudioClip UseLadder;

    [SerializeField] private AudioSource playerSource;

    void Awake()
    {
        if (playerSource == null) playerSource = gameObject.AddComponent<AudioSource>();
    }

    // 폴더 전용 자동 바인딩
    [ContextMenu("Auto Bind Sounds")]
    public void LoadAllSounds()
    {
        // Assets/Resources/Sound/ 경로에서 파일을 가져옵니다.
        string path = "Sound/PlayerSound/";

        for (int i = 0; i < 10; i++)
        {
            FootstepAudioClips[i] = Resources.Load<AudioClip>(path + "Ground/Player_Footstep_" + (i + 1));
        }
        UseLadder = Resources.Load<AudioClip>(path + "metal_ladder");
        JumpLand = Resources.Load<AudioClip>(path + "Ground/Player_Land");
        // uiTabSwapClip = Resources.Load<AudioClip>(path + "UI_Tab");
        // uiHoverClip = Resources.Load<AudioClip>(path + "UI_Hover");
        // uiErrorClip = Resources.Load<AudioClip>(path + "UI_Error");

        // itemPickupClip = Resources.Load<AudioClip>(path + "Item_Pickup");
        // itemEquipClip = Resources.Load<AudioClip>(path + "Item_Equip");
        // itemUnequipClip = Resources.Load<AudioClip>(path + "Item_Unequip");

        // shopBuyClip = Resources.Load<AudioClip>(path + "Shop_Buy");
        // shopSellClip = Resources.Load<AudioClip>(path + "Shop_Sell");
        // Shop_Reroll = Resources.Load<AudioClip>(path + "Shop_Reroll");
        // upgradeSuccessClip = Resources.Load<AudioClip>(path + "Upgrade_Success");
        // upgradeFailClip = Resources.Load<AudioClip>(path + "Upgrade_Fail");

        // // BGM
        // AudioClip lobbyBgm = Resources.Load<AudioClip>(path + "BGM_Lobby");
        // if (lobbyBgm != null) lobbyMusicClips = new AudioClip[] { lobbyBgm };

        Debug.Log("[PlayerSoundController] Resources/Sound/PlayerSound 폴더에서 사운드 연결 완료!");
    }

    public void PlayFootStep()
    {
        var index = Random.Range(0, FootstepAudioClips.Length);
        PlaySFX(FootstepAudioClips[index]);
    }

    public void PlayUseLadder()
    {
        PlaySFX(UseLadder);
    }

    public void PlayJumpLand()
    {
        PlaySFX(JumpLand);
    }

    string prevClip;
    float prevTime;
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && playerSource != null)
        {
            if (playerSource.isPlaying && prevClip == clip.name.Split('_')[1] && prevTime + 0.1 > Time.time)
                return;

            playerSource.PlayOneShot(clip, AudioManager.Instance.EffectVolume * AudioManager.Instance.MasterVolume);
            prevClip = clip.name.Split('_')[1];
            prevTime = Time.time;
        }
    }
}
