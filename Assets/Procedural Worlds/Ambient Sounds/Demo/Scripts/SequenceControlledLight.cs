using UnityEngine;
using System.Collections;

public class SequenceControlledLight : MonoBehaviour {
    public AmbientSounds.Sequence m_sequenceToWatch = null;
    public Light m_lightToFlash = null;
    public float m_minFlashDuration = 0f;
    public float m_maxFlashDuration = 0.1f;
    public int m_minFlashes = 1;
    public int m_maxFlashes = 3;
    public float m_minFlashPauseDuration = 0f;
    public float m_maxFlashPauseDuration = 0.1f;

    Coroutine flashCoro = null;

    private void OnEnable() {
        if (m_sequenceToWatch)
            m_sequenceToWatch.m_OnPlayClip.AddListener(OnWillPlay);
    }
    private void OnDisable() {
        if (m_sequenceToWatch)
            m_sequenceToWatch.m_OnPlayClip.RemoveListener(OnWillPlay);
    }
    public void OnWillPlay(AudioClip clip) {
        if (flashCoro != null)
            StopCoroutine(flashCoro);
        flashCoro = StartCoroutine(CoroDoFlash());
    }
    IEnumerator CoroDoFlash() {
        if (m_lightToFlash == null)
            yield break;
        int numFlashes = Random.Range(m_minFlashes, m_maxFlashes);
        for (int f = 0; f < numFlashes; ++f) {
            m_lightToFlash.enabled = true;
            yield return new WaitForSeconds(Random.Range(m_minFlashDuration, m_maxFlashDuration));
            m_lightToFlash.enabled = false;
            yield return new WaitForSeconds(Random.Range(m_minFlashPauseDuration, m_maxFlashPauseDuration));
        }
    }
}
