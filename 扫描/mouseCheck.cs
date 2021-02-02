using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mouseCheck : MonoBehaviour
{
    private bool f;
    public float maxrange;
    private float scanfrange,clearTime;
    private Camera m_camera;
    void Start()
    {
        m_camera = GetComponent<Camera>();
    }
    
    void Update()
    {
        //GetComponent<GraphControl>().MousePos = Vector3.zero;
        
        if (Input.GetMouseButtonDown(0))
        {
            scanfrange = 0;
            f = true;
            Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                GetComponent<GraphControl>().MousePos = new Vector4(hit.point.x, hit.point.y, hit.point.z, 0);
            }
        }
        if (maxrange - scanfrange > 0.015f && f)
        {
            scanfrange = Mathf.Lerp(scanfrange, maxrange, 0.02f);
        }
        else
        {
            scanfrange = 0f;
            f = false;
        }
        
        GetComponent<GraphControl>().ScanfRange = scanfrange;
       
    }
}
