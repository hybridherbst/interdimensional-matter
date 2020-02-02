using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;

public class LevelControl : MonoBehaviour
{
    public Transform levelParent;
    public int currentLevel;

    public Transform numberParent;

    private IEnumerator Start() {
        for(int i = 0; i < numberParent.childCount; i++)
            numberParent.GetChild(i).gameObject.SetActive(true);
        for(int i = 0; i < levelParent.childCount; i++)
            levelParent.GetChild(i).gameObject.SetActive(true);

        for(int i = 0; i < numberParent.childCount; i++)
            numberParent.GetChild(i).gameObject.SetActive(false);
        for(int i = 0; i < levelParent.childCount; i++)
            levelParent.GetChild(i).gameObject.SetActive(false);

        yield return null;

        currentMain = StartCoroutine(StartLevel());    
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
        
        // save isKinematic state
        var rs = levelParent.GetComponentsInChildren<Rigidbody>().ToList();
        var states = rs.Select(x => x.isKinematic).ToList();
        foreach(var r in rs) {r.isKinematic = true;}

        moveInTimeline.Play();
        yield return new WaitForSeconds(1f);

        // restore isKinematic state
        for(int i = 0; i < rs.Count; i++)
            rs[i].isKinematic = states[i];
    }

    Coroutine currentMain;

    [ContextMenu("Next")]
    public void NextLevel() {
        StartCoroutine(_NextLevel());
    }

    IEnumerator _NextLevel() {
        yield return currentMain;

        currentMain = StartCoroutine(StopLevel());
        yield return currentMain;
        currentLevel++;
        if(currentLevel >= levelParent.childCount)
            currentLevel = 0;
        currentMain = StartCoroutine(StartLevel());
        yield return currentMain;
    }

    [ContextMenu("Prev")]
    public void PreviousLevel() {
        currentMain = StartCoroutine(_PrevLevel());
    }

    IEnumerator _PrevLevel() {
        yield return currentMain;

        currentMain = StartCoroutine(StopLevel());
        yield return currentMain;
        
        currentLevel--;
        if(currentLevel < 0)
            currentLevel = levelParent.childCount - 1;

        currentMain = StartCoroutine(StartLevel());
        yield return currentMain;
    }
}
