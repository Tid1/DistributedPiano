using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCreationScript : MonoBehaviour {
    public GameObject holder;
    public GameObject prefab;
    
    // Start is called before the first frame update
    void Start()
    {
        CreateAtPos(new Vector3(0, 30, 0));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateAtPos(Vector3 pos) {
        GameObject createdObj = Instantiate(prefab, holder.transform);
        createdObj.GetComponent<RectTransform>().anchoredPosition3D = pos;
        createdObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 500);
        //createdObj.transform.localPosition = pos;
    }
}
