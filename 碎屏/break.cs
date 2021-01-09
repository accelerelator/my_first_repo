using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 脚本挂载在摄像机
/// </summary>
[ExecuteInEditMode]
public class backCrmera : MonoBehaviour
{

    public Material mat;
    [Range(0, 2f)]
    public float NormalScale = 0;
    [Range(0, 2f)]
    public float LuminanceScale = 0.25f;
    /// <summary>
    /// 由于脚本加在Main Camera上，
    /// 所以OnRenderImage的source就是Main Camera渲染的画面
    /// destination就是Main Camera的Camera组件中的Target Texture属性
    /// Blit作用是 让source 经过 此代码的mat对应的shader 后
    /// 再输出至destination
    /// 也就是说作为后处理效果，这个shader只需要处理offset就行
    /// 光照计算还是放在物体的shader上
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //设置参数图像根据法线贴图的方向偏移的程度
        mat.SetFloat("_BrokenScale", NormalScale);
        mat.SetFloat("_LuminanceScale", LuminanceScale);
        //将source的图像通过mat的shader的
        //第1个pass渲染后传回给destination
        Graphics.Blit(source, destination, mat);
    }
}