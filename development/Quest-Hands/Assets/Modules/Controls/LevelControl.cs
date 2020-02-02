using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class LevelControl : MonoBehaviour
{
    public Transform levelParent;
    public int currentLevel;

    public Transform numberParent;

    private void Start() {
        StartLevel();    
    }

    public PlayableDirector timeline;
    public PlayableDirector moveOutTimeline, moveInTimeline;

    IEnumerator StopLevel() {
        timeline.Stop();
        numberParent.GetChild(currentLevel).gameObject.SetActive(false);
        moveOutTimeline.Stop();
        moveOutTimeline.Play();
        yield return new WaitForSeconds(1f);
        levelParent.GetChild(currentLevel).gameObject.SetActive(false);
    }

    IEnumerator StartLevel() {
        numberParent.GetChild(currentLevel).gameObject.SetActive(true);
        timeline.Play();
        yield return new WaitForSeconds(0.2f);
        levelParent.GetChild(currentLevel).gameObject.SetActive(true);
        moveInTimeline.Play();
        yield return new WaitForSeconds(1f);
    }

    [ContextMenu("Next")]
    public void NextLevel() {
        StartCoroutine(_NextLevel());
    }

    IEnumerator _NextLevel() {
        yield return StartCoroutine(StopLevel());
        currentLevel++;
        if(currentLevel >= levelParent.childCount)
            currentLevel = 0;
        yield return StartCoroutine(StartLevel());
    }

    [ContextMenu("Prev")]
    public void PreviousLevel() {
        StartCoroutine(_PrevLevel());
    }

    IEnumerator _PrevLevel() {
        yield return StartCoroutine(StopLevel());
        
        currentLevel--;
        if(currentLevel < 0)
            currentLevel = levelParent.childCount - 1;

        yield return StartCoroutine(StartLevel());
    }
}
