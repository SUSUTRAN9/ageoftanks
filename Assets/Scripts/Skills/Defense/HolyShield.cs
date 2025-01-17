﻿using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 防御技能-圣盾 脚本
/// 额外抵挡多次攻击（1-5次）
/// </summary>
public class HolyShield:MonoBehaviour
{
    //特效说明:正常状态显示保护罩，被攻击时发功防御抵挡一次攻击抵挡时防护罩放大随后恢复到初始大小

    public int Total = 3;//可抵挡攻击次数
    public int Effected = 0;//已触发次数

    //技能预制体
    public GameObject SkillPrefab = null; 

    //防御坦克
    public GameObject Tank = null;
    public GameObject SkillEffect = null;

    private void Awake()
    {
        //挂载后的默认状态
        SkillPrefab= CommonHelper.GetPrefabs("skill", "Defense/圣盾");
        SkillEffect = GameObject.Instantiate(SkillPrefab, Tank.transform.parent, false);
        SkillEffect.SetActive(true);
    }



    /// <summary>
    /// 触发防御,在有效防御内触发，达到次数后自动销毁
    /// </summary>
    /// <returns></returns>
    public bool Trigger()
    {
        if (Effected < Total) 
        {
            StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
            {
                if (SkillEffect != null)
                {
                    GameObject child = SkillEffect.transform.Find("GroundFX_Fire01").gameObject;
                    if (child != null)
                    {
                        child.SetActive(true);
                        ParticleSystem particleSystem = child.transform.GetChild(0).GetComponent<ParticleSystem>();
                        particleSystem.Play();
                        StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
                            {
                                if (child != null)
                                {
                                    child.SetActive(false);
                                }
                            }, 1f));
                    }
                }
            }, 0.3f));
           


            Effected++;
            if (Effected == Total) 
            {
                StartCoroutine(CommonHelper.DelayToInvokeDo(() => { DestroyImmediate(SkillEffect); }, 2f));
            }
            return true;
        }
        return false;
    }

}
