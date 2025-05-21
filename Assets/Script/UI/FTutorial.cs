using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FTutorial : MonoBehaviour
{


    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F))
            Destroy(gameObject);
    }
}
