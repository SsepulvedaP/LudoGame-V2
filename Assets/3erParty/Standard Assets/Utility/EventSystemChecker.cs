using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

public class EventSystemChecker : MonoBehaviour
{
    //public GameObject eventSystem;

	// Use this for initialization
	void Awake ()
	{
	    EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
	    if(!eventSystem)
        {
           //Instantiate(eventSystem);
            GameObject obj = new GameObject("EventSystem");
            eventSystem = obj.AddComponent<EventSystem>();
        }

        EnsureCompatibleInputModule(eventSystem.gameObject);
	}

    private void EnsureCompatibleInputModule(GameObject eventSystemObject)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        StandaloneInputModule legacyInputModule = eventSystemObject.GetComponent<StandaloneInputModule>();
        if (legacyInputModule != null)
        {
            Destroy(legacyInputModule);
        }

        if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (eventSystemObject.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }
}
