﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻击技能-散射
/// 同时对多个目标造成普通攻击伤害（2-6）
/// </summary>
public class Scattering : MonoBehaviour
{
    public int Value = 0;//目标数
    public int Effected = 0;//已触发次数

    //技能预制体
    public GameObject SkillPrefab = null;
    //实例化体
    public GameObject SkillEffect = null;

    //攻击方
    public GameObject FromTank = null;
    //被攻击方
    public GameObject TargetTank = null;


    private void Awake()
    {
        //挂载后的默认状态
        SkillPrefab = CommonHelper.GetPrefabs("skill", "Attack/散射");

    }
    /// <summary>
    /// 攻击特效
    /// </summary>
    /// <returns></returns>
    public bool EffectAttack()
    {

        SkillEffect = GameObject.Instantiate(SkillPrefab, TargetTank.transform.parent, false);
        SkillEffect.SetActive(true);
        GameObject child = SkillEffect.transform.Find("ExplosionFX").gameObject;
        child.SetActive(true);
        child.transform.GetChild(0).GetComponent<ParticleSystem>().Play();
        StartCoroutine(CommonHelper.DelayToInvokeDo(() => { DestroyImmediate(SkillEffect); }, 2f));
        Effected++;

        return true;
    }

}
