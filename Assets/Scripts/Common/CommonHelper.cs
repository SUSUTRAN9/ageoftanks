﻿using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class CommonHelper
{
    /// <summary>
    /// 获取随机数
    /// </summary>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <returns></returns>
    public static int GetRandom(int min, int max)
    {
        return new System.Random(Guid.NewGuid().GetHashCode()).Next(min, max);
    }

    /// <summary>
    /// 延迟执行
    /// </summary>
    /// <param name="action"></param>
    /// <param name="delaySeconds"></param>
    /// <returns></returns>
    public static IEnumerator DelayToInvokeDo(Action action, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        action();
    }

    /// <summary>
    /// 倒计时显示器
    /// </summary>
    /// <param name="time">时间</param>
    /// <param name="onCountdown">每秒倒计时回调，返回当前秒数</param>
    /// <param name="onComplete">倒计时结束后回调</param>
    public static void CountDown(float time, Action<float> onCountdown,  Action onComplete)
    {
        //每一个数字显示1秒时长，动画结束后递归调用并将时间减1秒，从而达到倒计时效果
        vp_Timer.In(1f, () =>
        {
            time -= 1;
            onCountdown(time);
            if (time > 0)
            {
                CountDown(time, onCountdown, onComplete);
            }
            else
            {
                onComplete();
            }
        });
    }

    /// <summary>
    /// 加载坦克图
    /// </summary>
    /// <param name="code">坦克编号</param>
    /// <returns></returns>
    public static Texture LoadTankImage(string code)
    {
        Texture texture = Resources.Load<Texture>("Texture/tank/" + code);
        return texture;
    }
    /// <summary>
    /// 加载技能图
    /// </summary>
    /// <param name="skill">技能名称</param>
    /// <returns></returns>
    public static Texture LoadSkillImage(string skill)
    {
        Texture texture = Resources.Load<Texture>("Texture/skill/" + skill);
        return texture;
    }


    /// <summary>
    /// 运行模式下Texture转换成Texture2D
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    public static Texture2D TextureToTexture2D(Texture texture)
    {
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 32);
        Graphics.Blit(texture, renderTexture);

        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(renderTexture);

        return texture2D;
    }


    /// <summary>
    /// 材质球替换（换肤功能）
    /// </summary>
    /// <param name="gameObject"></param>
    public static void ReplaceMaterial(GameObject gameObject,string skinname)
    {
        Material mat;// = mats[GetRandom(0, 6)];
        switch (skinname)
        {
            case "red":
                mat = Resources.Load<Material>("Materials/A_Spiders_Mat(red)");
                break;
            case "blue":
                mat = Resources.Load<Material>("Materials/A_Spiders_Mat(blue)");
                break;
            case "black":
                mat = Resources.Load<Material>("Materials/A_Spiders_Mat(black)");
                break;
            case "yellow":
                mat = Resources.Load<Material>("Materials/A_Spiders_Mat(yellow)");
                break;
            case "green":
                mat = Resources.Load<Material>("Materials/A_Spiders_Mat(green)");
                break;
            default:
                mat = Resources.Load<Material>("Materials/A_Spiders_Mat(rusty)");
                break;
        }

       
        //不替换的
        List<string> noReplace = new List<string>() {
            "Tracks_R_Geom","Tracks_L_Geom",
            "Tracks_FL_Geom","Tracks_RL_Geom","Tracks_FR_Geom","Tracks_RR_Geom",
            "FX_Laser_Ray_Geom",
            "RL_Track_Geom","RR_Track_Geom",
            "FL_Track_Geom","FR_Track_Geom",
            "FL_Wheel_Geom","FR_Wheel_Geom","RL_Wheel_Geom","RR_Wheel_Geom"
        };
        foreach (MeshRenderer item in gameObject.transform.GetComponentsInChildren<MeshRenderer>())
        {
            
            if (noReplace.Contains(item.gameObject.name)==false)
            {
                // rend.Add(item);
                item.material = mat;
            }
        }
        

        List <SkinnedMeshRenderer> skinrend = new List<SkinnedMeshRenderer>(); 
        foreach (SkinnedMeshRenderer item in gameObject.transform.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (noReplace.Contains(item.gameObject.name) == false)
            {
                // skinrend.Add(item);
                item.material = mat;
            }
        }

    }

    /// <summary>
    /// 材质球替换（换肤功能）
    /// </summary>
    /// <param name="gameObject"></param>
    public static void ReplaceMaterialByPath(GameObject gameObject, string path)
    {
        Material mat= Resources.Load<Material>(path);

        //不替换的
        List<string> noReplace = new List<string>() {
            "Tracks_R_Geom","Tracks_L_Geom",
            "Tracks_FL_Geom","Tracks_RL_Geom","Tracks_FR_Geom","Tracks_RR_Geom",
            "FX_Laser_Ray_Geom",
            "RL_Track_Geom","RR_Track_Geom",
            "FL_Track_Geom","FR_Track_Geom",
            "FL_Wheel_Geom","FR_Wheel_Geom","RL_Wheel_Geom","RR_Wheel_Geom"
        };
        foreach (MeshRenderer item in gameObject.transform.GetComponentsInChildren<MeshRenderer>())
        {

            if (noReplace.Contains(item.gameObject.name) == false)
            {
                // rend.Add(item);
                item.material = mat;
            }
        }


        List<SkinnedMeshRenderer> skinrend = new List<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer item in gameObject.transform.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (noReplace.Contains(item.gameObject.name) == false)
            {
                // skinrend.Add(item);
                item.material = mat;
            }
        }

    }

    /// <summary>
    /// 给物体添加Mesh特效
    /// </summary>
    /// <param name="effectName">特效名称</param>
    /// <param name="MeshObject">目标物体</param>
    public static GameObject AddEffect(string effectName,GameObject MeshObject)
    {
        GameObject Effect = Resources.Load<GameObject>("Effects/" + effectName);
        GameObject currentInstance = GameObject.Instantiate(Effect, MeshObject.transform,false);
        PSMeshRendererUpdater psUpdater = currentInstance.GetComponent<PSMeshRendererUpdater>();
        psUpdater.UpdateMeshEffect(MeshObject);
        
        return currentInstance;
    }

    /// <summary>
    /// 获取子弹预制体
    /// </summary>
    /// <param name="type">武器类型：DoubleGun、GLauncher、Shocker、Shocker_Rifle、Sniper</param>
    /// <param name="bulletPrefab"></param>
    /// <param name="firePrefab"></param>
    public static void GetBulletPrefab(string type, out GameObject bulletPrefab, out GameObject firePrefab)
    {
        switch (type.ToLower())
        {
            case "doublegun":
                bulletPrefab = Resources.Load<GameObject>("Bullets/DoubleGun/Bullet_BlazingRed_Big_Projectile").gameObject;
                firePrefab = Resources.Load<GameObject>("Bullets/DoubleGun/Bullet_BlazingRed_Big_MuzzleFlare").gameObject;
                break;
            case "glauncher":
                bulletPrefab = Resources.Load<GameObject>("Bullets/GLauncher/Plasma_LightBlue_Medium_Projectile").gameObject;
                firePrefab = Resources.Load<GameObject>("Bullets/GLauncher/Plasma_LightBlue_Medium_MuzzleFlare").gameObject;
                break;
            case "shocker":
                bulletPrefab = Resources.Load<GameObject>("Bullets/Shocker/Plasma_RagingRed_Big_Projectile").gameObject;
                firePrefab = Resources.Load<GameObject>("Bullets/Shocker/Plasma_RagingRed_Big_MuzzleFlare").gameObject;
                break;
            case "shocker_rifle":
                bulletPrefab = Resources.Load<GameObject>("Bullets/Shocker_Rifle/Laser_Red_Medium_Projectile").gameObject;
                firePrefab = Resources.Load<GameObject>("Bullets/Shocker_Rifle/Laser_Red_Medium_MuzzleFlare").gameObject;
                break;
            case "sniper":
                bulletPrefab = Resources.Load<GameObject>("Bullets/Sniper/Bullet_GoldFire_Big_Projectile").gameObject;
                firePrefab = Resources.Load<GameObject>("Bullets/Sniper/Bullet_GoldFire_Big_MuzzleFlare").gameObject;
                break;
            default:
                bulletPrefab = Resources.Load<GameObject>("Bullets/Sniper/Bullet_GoldFire_Big_Projectile").gameObject;
                firePrefab = Resources.Load<GameObject>("Bullets/Sniper/Bullet_GoldFire_Big_MuzzleFlare").gameObject;
                break;
        }
    }


    /// <summary>
    /// 获取预制体
    /// </summary>
    /// <param name="type">分类：取值包含 backpack、head、body、weapon、leg、tank、bullet、skill</param>
    /// <param name="path">路径</param>
    /// <returns></returns>
    public static GameObject GetPrefabs(string type, string path)
    {
        string root = "";
        switch (type.ToLower())
        {
            case "backpack":
                root = "Backpacks";
                break;
            case "head":
                root = "Heads";
                break;
            case "body":
                root = "Bodys";
                break;
            case "weapon":
                root = "Weapons";
                break;
            case "leg":
                root = "Legs";
                break;
            case "tank":
                root = "Tanks";
                break;
            case "bullet":
                root = "Bullets";
                break;
            case "skill":
                root = "Skills";
                break;

        }
        GameObject tf = Resources.Load<GameObject>($"{root}/{path}");
        if (tf != null)
        {
            return tf;
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// 概率判定
    /// </summary>
    /// <param name="rate">概率值1~100</param>
    /// <returns></returns>
    public static bool IsHit(float rate)
    {
        int random = CommonHelper.GetRandom(1, 100);
        return random <= rate;
    }

    /// <summary>
    /// 获取物体的尺寸
    /// </summary>
    /// <param name="gameObject">物体对象</param>
    /// <param name="withScale">是否为缩放后的尺寸</param>
    /// <returns></returns>
    public static Vector3 GetObjectSize(GameObject gameObject,bool withScale=true)
    {
        float xSize, ySize, zSize;
        if (withScale == true)
        {
            xSize = gameObject.GetComponent<MeshFilter>().mesh.bounds.size.x * gameObject.transform.localScale.x;
            ySize = gameObject.GetComponent<MeshFilter>().mesh.bounds.size.y * gameObject.transform.localScale.y;
            zSize = gameObject.GetComponent<MeshFilter>().mesh.bounds.size.z * gameObject.transform.localScale.z;
        }
        else
        {
            xSize = gameObject.GetComponent<MeshFilter>().mesh.bounds.size.x;
            ySize = gameObject.GetComponent<MeshFilter>().mesh.bounds.size.y;
            zSize = gameObject.GetComponent<MeshFilter>().mesh.bounds.size.z;
        }

        return new Vector3(xSize, ySize, zSize);
    }


    //在对战中显示技能触发的图标
    public static void ShowSkillIcon(GameObject gameObject,string skillName,GameObject UIObject)
    {
        GameObject skillimg = new GameObject();
        
        RawImage rawImage = skillimg.AddComponent<RawImage>();
        skillimg.AddComponent<Outline>();
        RectTransform rect = skillimg.GetComponent<RectTransform>();
        rawImage.texture = LoadSkillImage(skillName);
        skillimg.transform.SetParent(UIObject.transform, false);
        rect.sizeDelta = new Vector2(25,25);

        skillimg.transform.position = Camera.main.WorldToScreenPoint(gameObject.transform.position + new Vector3(0, 15, 0));

        skillimg.transform.DOMove(Camera.main.WorldToScreenPoint(gameObject.transform.position + new Vector3(0, 17, -2)), 0.5f);//上飘
        skillimg.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.3f).From(new Vector3(0,0,0)).OnComplete(() => {
            vp_Timer.In(1f, () =>
            {
                UnityEngine.Object.DestroyImmediate(skillimg);
            });
            //消失
        });//放大


    }
}
