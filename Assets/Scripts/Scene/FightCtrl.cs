﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FightCtrl : MonoBehaviour
{
    public int FightType;

    public GameObject UIPanel;//UI界面
    public GameObject LoadPanel;//对局加载界面
    public GameObject StarterCam;//视角物体
    public GameObject Looker;//自由观察视角
    public GameObject GreenPointer, RedPointer; //攻击方指示器

    /// <summary>
    /// 对战台
    /// </summary>
    public GameObject Platform;
    /// <summary>
    /// 模型位置
    /// </summary>
    public List<GameObject> PlayerA_Pos;
    public List<GameObject> PlayerB_Pos;
    /// <summary>
    /// 坦克对象
    /// </summary>
    public List<GameObject> PlayerA_Tanks;
    public List<GameObject> PlayerB_Tanks;

    /// <summary>
    /// 玩家信息
    /// </summary>
    public Player PlayerA, PlayerB;

    List<FightItem> OrderList;

    //public int index = 0;
    private int CountDownTime = 5; //倒计时
    private int Round = 0; //回合数
    private int FightIndex = -1; //当前战斗方的下标
    private bool Finish = false;//战斗是否结束 

    private bool LookAtSwitch = true;//坦克第三人称视角开关
    private bool FreeLookSwitch = false;//自由视角开关
    private bool Surrender = false; //头像


    List<FightItem> DuelList = new List<FightItem>(); //决斗中的坦克

    private void Awake()
    {
        //StartCoroutine(CommonHelper.DelayToInvokeDo(() => {
        //    UIPanel.transform.Find("OverPanel").gameObject.SetActive(true);
        //    UIPanel.transform.Find("OverPanel").SetAsLastSibling();
        //    UIPanel.transform.Find("OverPanel").DOScale(new Vector3(1, 1, 1), 0.5f).From(new Vector3(1, 0, 0));
        //    UIPanel.transform.Find("victory").gameObject.SetActive(true);
        //    UIPanel.transform.Find("victory").SetAsLastSibling();
        //    UIPanel.transform.Find("victory").DOLocalRotate(new Vector3(0,0, 0), 1f).From(new Vector3(100,100,0));// .DOShakeScale(1f);
        //}, 2f));

        //1v1自动关闭lookat视角，非pc端也自动关闭
        if (FightType == 1 || ResourceCtrl.Instance.IsPC==false) { LookAtSwitch = false; }


        AudioMgr.Instance.PlayBgm("Sound/bg-fight");

        InitPlatform();
        InitTank();
        //StartTransfer();
        InitPlayer();
        GetFightOrder();
        ShowLoadPanel();
        UIPanel.transform.Find("btnBack").SetAsLastSibling();
        UIPanel.transform.Find("btnBack").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
        {
            AudioMgr.Instance.PlayBtnAudio();
            AudioMgr.Instance.PlayBgm("Sound/bg-sound");
            SceneManager.LoadSceneAsync("BattleMode");
            System.GC.Collect();
        });
        UIPanel.transform.Find("btnSurrender").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
        {
            UIPanel.transform.Find("SurrenderPanel").gameObject.SetActive(true);
            UIPanel.transform.Find("SurrenderPanel").DOScale(new Vector3(1, 1, 1), 0.5f).From(new Vector3(0, 0, 0));
            UIPanel.transform.Find("SurrenderPanel").SetAsLastSibling();


        });
        UIPanel.transform.Find("SurrenderPanel/btnSure").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
        {
            UIPanel.transform.Find("SurrenderPanel").gameObject.SetActive(false);
            Surrender = true;
        });
        UIPanel.transform.Find("SurrenderPanel/btnCancel").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
        {
            UIPanel.transform.Find("SurrenderPanel").gameObject.SetActive(false);

        });

        //5秒后开始
        InvokeRepeating("TimePlay", 1f, 1f);

        StartCoroutine(CommonHelper.DelayToInvokeDo(() => {
            LoadPanel.transform.Find("Panel/Panel/txt_time_center").gameObject.SetActive(false);
            LoadPanel.SetActive(false);
            StarterCam.transform.GetComponent<Animator>().enabled = true;
            StartTransfer();
            StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
            {
                LoadPanel.transform.Find("Panel/Panel/txt_time_center").gameObject.SetActive(true);
                //UIPanel.transform.Find("txt_time_center").GetComponent<Text>().DOText("Fight", 0.5f).OnComplete(() =>
                //{
                LoadPanel.transform.Find("Panel/Panel/txt_time_center").gameObject.SetActive(false);
                    NextRound();
               // });
            }, 3f));

        },6f));


        //15秒后显示投降按钮
        StartCoroutine(CommonHelper.DelayToInvokeDo(() => {
            UIPanel.transform.Find("btnSurrender").gameObject.SetActive(true);
        },15f));

        
    }
    

    void TimePlay() 
    {
        Transform timePanel = LoadPanel.transform.Find("Panel/Panel/txt_time_center");// UIPanel.transform.Find("txt_time_center");
        timePanel.SetAsLastSibling();
        timePanel.GetComponent<Text>().text = CountDownTime.ToString();
        timePanel.gameObject.SetActive(true);
        timePanel.transform.DOScale(new Vector3(1, 1, 1), 0.5f).From(new Vector3(0, 0, 0));
        CountDownTime--;
        if (CountDownTime == 0) 
        {
            CancelInvoke("TimePlay");
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //AutoShoot();

        //血条位置实时更新
        foreach (FightItem fightItem in OrderList)
        {
            GameObject bloodbar = UIPanel.transform.Find("bloodbar").gameObject;
            Transform bar = UIPanel.transform.Find("bloodbar_" + fightItem.Code);
            bar.position = Camera.main.WorldToScreenPoint(fightItem.Tank.transform.position + new Vector3(0, 13, 0));
        }
        //技能图标及buff图标实时更新


        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Looker != null && Round>=1 && Finish==false) 
            {
                Looker.SetActive(!Looker.activeSelf);
                FreeLookSwitch = Looker.activeSelf;
                Debug.Log(Looker.activeSelf == true ? "开启自由视角" : "关闭自由视角");
            }
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (Looker != null && Round >= 1 && Finish == false)
            {
                LookAtSwitch = !LookAtSwitch;
                if (LookAtSwitch == false) { CMLookAt(-1); }
                Debug.Log(LookAtSwitch == true ? "开启锁定视角" : "关闭锁定视角");
            }
        }
    }

  
    //战斗平台初始化
    public void InitPlatform()
    {
        int count = Platform.transform.childCount;
        for (int i = 0; i < count; i++)
        {
            GameObject child = Platform.transform.GetChild(i).gameObject;
            if (child.name.Contains("A"))
            {
                PlayerA_Pos.Add(child);
            }
            else
            {
                PlayerB_Pos.Add(child);
            }
        }
    }

    //进入场景后随机匹配对战双方的坦克，后期通过数据拉取
    public void InitTank()
    {
        //玩家A
        for (int i = 0; i < ResourceCtrl.Instance.SelectList.Count; i++)
        {

            TankProperty item = ResourceCtrl.Instance.SelectList[i];
            GameObject tank = GetTankGameObj(item);
            tank.SetActive(false);
            tank.transform.SetParent(PlayerA_Pos[i].transform.Find("Tank"), false);
            item.TankObject = tank;
            PlayerA_Tanks.Add(tank);

        }
        //玩家B
        for (int i = 0; i < ResourceCtrl.Instance.SelectListB.Count; i++)
        {

            TankProperty item = ResourceCtrl.Instance.SelectListB[i];
            GameObject tank = GetTankGameObj(item);
            tank.SetActive(false);
            tank.transform.SetParent(PlayerB_Pos[i].transform.Find("Tank"), false);
            item.TankObject = tank;
            PlayerB_Tanks.Add(tank);
        }

    }
    
    //获得坦克游戏物体
    public GameObject GetTankGameObj(TankProperty item)
    {
        GameObject tank = null;
        if (item.IsSetup == true)
        {
            AOT_SetupRecord record = ResourceCtrl.Instance.MountTanks.Find(u => u.Code == item.Code);
            AOT_Parts Engine = ResourceCtrl.Instance.PartsList.Find(u => u.Code == record.LegCode);
            AOT_Parts Body = ResourceCtrl.Instance.PartsList.Find(u => u.Code == record.BodyCode);
            AOT_Parts Head = ResourceCtrl.Instance.PartsList.Find(u => u.Code == record.HeadCode);
            AOT_Parts Weapon = ResourceCtrl.Instance.PartsList.Find(u => u.Code == record.WeaponCode);
            tank = new GameObject();
            tank.name = item.Code;
            ResourceCtrl.Instance.Mount(Engine, Body, Head, Weapon).transform.SetParent(tank.transform, false);
        }
        else
        {
            AOT_Models tankModel = ResourceCtrl.Instance.ModelList.Find(u => u.Code == item.ModelCode);
            GameObject tankPrefab = Resources.Load<GameObject>(tankModel.FilePath);
            AOT_SkinInfo skin = ResourceCtrl.Instance.SkinList.Find(u => u.Code == item.SkinCode);
            tank = new GameObject();
            tank.name = item.Code;
            Instantiate(tankPrefab, tank.transform, false);
            CommonHelper.ReplaceMaterialByPath(tank, skin.MaterialPath);
        }
        tank.AddComponent<Weapon>();
        return tank;
    }

    //开启传送阵
    public void StartTransfer()
    {
        GameObject transfer = CommonHelper.GetPrefabs("skill", "Transfer");

     
        foreach (GameObject pos in PlayerA_Pos)
        {
            int num = PlayerA_Pos.IndexOf(pos);
            StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
            {
                GameObject newObj = Instantiate(transfer);
                newObj.transform.SetParent(pos.transform, false);
                newObj.SetActive(true);
                StartCoroutine(CommonHelper.DelayToInvokeDo(() => { DestroyImmediate(newObj); }, 5f));
                GameObject tank = PlayerA_Tanks[num];
                tank.SetActive(true);
                tank.transform.DOScale(new Vector3(1, 1, 1), 0.1f).From(new Vector3(0, 0, 0)).SetEase(Ease.InOutExpo);
            }, (num + 1) * 0.2f));
        }
        foreach (GameObject pos in PlayerB_Pos)
        {
            int num = PlayerB_Pos.IndexOf(pos);
            StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
            {
                GameObject newObj = Instantiate(transfer);
                newObj.transform.SetParent(pos.transform, false);
                newObj.SetActive(true);
                StartCoroutine(CommonHelper.DelayToInvokeDo(() => { DestroyImmediate(newObj); }, 5f));
                GameObject tank = PlayerB_Tanks[num];
                tank.SetActive(true);
                tank.transform.DOScale(new Vector3(1, 1, 1), 0.1f).From(new Vector3(0, 0, 0)).SetEase(Ease.InOutExpo);


            }, (num + 1) * 0.2f));
        }

    }

    //初始化玩家数据
    public void InitPlayer()
    {
        List<TankProperty> tankPropertiesA = new List<TankProperty>();
        //foreach (GameObject item in PlayerA_Tanks)
        //{
        //    int num = PlayerA_Tanks.IndexOf(item);
        //    string id = (num + 1).ToString().PadLeft(2, '0');
        //    tankPropertiesA.Add(new TankProperty("坦克"+id, "蝎式坦克", item,num));
        //}

        tankPropertiesA = ResourceCtrl.Instance.SelectList;
        PlayerA = new Player
        {
            Name = "玩家A",
            Photo = "",
            Hero = new HeroProperty()
            {
                Code = "A1",
                Name = "英雄A",
                Speed = 0,
                Attack = 0,
                Defense = 0,
                CritRate = 0,
                Blood = 0
            },
            TankList = tankPropertiesA
        };

        List<TankProperty> tankPropertiesB = ResourceCtrl.Instance.SelectListB;
        //foreach (GameObject item in PlayerB_Tanks)
        //{
        //    int num = PlayerB_Tanks.IndexOf(item);
        //    string id = (num + 1).ToString().PadLeft(5, '0');
        //    TankProperty tank = new TankProperty(id, "蝎式坦克", item);
        //    tank.AttackSkill = ResourceCtrl.Instance.SkillList.FindAll(u=>u.Type=="Attack")[CommonHelper.GetRandom(0,13)];
        //    tank.DefenseSkill = ResourceCtrl.Instance.SkillList.FindAll(u => u.Type == "Defense")[CommonHelper.GetRandom(0, 8)];
        //    tankPropertiesB.Add(tank);
        //}
        PlayerB = new Player
        {
            Name = "玩家B",
            Photo = "",
            Hero = new HeroProperty()
            {
                Code = "B1",
                Name = "英雄B",
                Speed = 0,
                Attack = 0,
                Defense = 0,
                CritRate = 0,
                Blood = 0
            },
            TankList = tankPropertiesB
        };
    }


    //显示对局卡牌
    public void ShowLoadPanel()
    {
        GameObject loadPanel = Instantiate(ResourceCtrl.Instance.ResourceRoot.transform.Find("UI/LoadPanel").gameObject, UIPanel.transform, false);
        LoadPanel = loadPanel;
        GameObject loadCard = loadPanel.transform.Find("Panel/LoadCard").gameObject;//  Resources.Load<GameObject>("UI/LoadCard");
        //A
        Transform ContentA = loadPanel.transform.Find("Panel/PlayerA/Scroll View/Viewport/Content");
        foreach (TankProperty item in PlayerA.TankList)
        {
            GameObject newCard = Instantiate(loadCard, ContentA);
            newCard.SetActive(true);
            newCard.name = "TankCard" + item.Code;

            if (item.IsSetup == true)
            {
                newCard.transform.Find("Panel/image").GetComponent<RawImage>().texture = ResourceCtrl.Instance.MountTanksSprite[item.Code].texture;
            }
            else
            {
                newCard.transform.Find("Panel/image").GetComponent<RawImage>().texture = ResourceCtrl.Instance.TanksSprite[item.Code].texture;
                //newCard.transform.Find("Panel/image").GetComponent<RawImage>().texture = CommonHelper.LoadTankImage(item.Code);
            }

            newCard.transform.Find("Panel/txtNo").GetComponent<Text>().text = "#" + item.Code;
            newCard.transform.Find("Panel/bgA").gameObject.SetActive(true);
            newCard.transform.Find("Panel/iconAttack/txtVal").GetComponent<Text>().text = item.Attack.ToString();
            newCard.transform.Find("Panel/iconDefense/txtVal").GetComponent<Text>().text = item.Blood.ToString();

            newCard.transform.Find("Panel/iconCrit/txtVal").GetComponent<Text>().text = item.CritRate.ToString() + "%";
            newCard.transform.Find("Panel/iconSpeed/txtVal").GetComponent<Text>().text = item.Speed.ToString();

            if (item.AttackSkill != null)
            {
                newCard.transform.Find("Panel/attackSkill/icon").GetComponent<RawImage>().texture = CommonHelper.LoadSkillImage(item.AttackSkill.SkillName);
            }
            else
            {
                newCard.transform.Find("Panel/attackSkill/icon").gameObject.SetActive(false);
            }

            if (item.DefenseSkill != null)
            {
                newCard.transform.Find("Panel/defenseSkill/icon").GetComponent<RawImage>().texture = CommonHelper.LoadSkillImage(item.DefenseSkill.SkillName);
            }
            else
            {
                newCard.transform.Find("Panel/defenseSkill/icon").gameObject.SetActive(false);
            }
        }


        //B
        Transform ContentB = loadPanel.transform.Find("Panel/PlayerB/Scroll View/Viewport/Content");
        foreach (TankProperty item in PlayerB.TankList)
        {
            GameObject newCard = Instantiate(loadCard, ContentB);
            newCard.SetActive(true);
            newCard.name = "TankCard" + item.Code;

            if (item.IsSetup == true)
            {
                newCard.transform.Find("Panel/image").GetComponent<RawImage>().texture = ResourceCtrl.Instance.MountTanksSprite[item.Code].texture;
            }
            else
            {
                newCard.transform.Find("Panel/image").GetComponent<RawImage>().texture = ResourceCtrl.Instance.TanksSprite[item.Code].texture;
                //newCard.transform.Find("Panel/image").GetComponent<RawImage>().texture = CommonHelper.LoadTankImage(item.Code);
            }

            newCard.transform.Find("Panel/txtNo").GetComponent<Text>().text = "#" + item.Code;
            newCard.transform.Find("Panel/bgB").gameObject.SetActive(true);
            newCard.transform.Find("Panel/iconAttack/txtVal").GetComponent<Text>().text = item.Attack.ToString();
            newCard.transform.Find("Panel/iconDefense/txtVal").GetComponent<Text>().text = item.Blood.ToString();

            newCard.transform.Find("Panel/iconCrit/txtVal").GetComponent<Text>().text = item.CritRate.ToString() + "%";
            newCard.transform.Find("Panel/iconSpeed/txtVal").GetComponent<Text>().text = item.Speed.ToString();

            if (item.AttackSkill != null)
            {
                newCard.transform.Find("Panel/attackSkill/icon").GetComponent<RawImage>().texture = CommonHelper.LoadSkillImage(item.AttackSkill.SkillName);
            }
            else
            {
                newCard.transform.Find("Panel/attackSkill/icon").gameObject.SetActive(false);
            }
            if (item.DefenseSkill != null)
            {
                newCard.transform.Find("Panel/defenseSkill/icon").GetComponent<RawImage>().texture = CommonHelper.LoadSkillImage(item.DefenseSkill.SkillName);
            }
            else
            {
                newCard.transform.Find("Panel/defenseSkill/icon").gameObject.SetActive(false);
            }
        }

    }


    /// <summary>
    /// 按照从左到右进行排序
    /// </summary>
    public void GetFightOrder()
    {

        OrderList = new List<FightItem>();
        
        //双方速度最大值进行随机，随机数大的先手

        int SpeedA = (int)PlayerA.TankList.Max(u => u.Speed);
        int SpeedB = (int)PlayerB.TankList.Max(u => u.Speed);

        int RandomA = CommonHelper.GetRandom(0, SpeedA); //随机数A
        int RandomB = CommonHelper.GetRandom(0, SpeedB); //随机数B
        Debug.Log($"随机数A：{RandomA}，随机数B：{RandomB}");

        int count = FightType == 1 ? 1 : 7; //坦克数

        for (int i = 0; i < count; i++)
        {
            if (RandomA >= RandomB)
            {
                if (PlayerA.TankList.Count >= i + 1)
                {
                    TankProperty itemA = PlayerA.TankList[i];
                    OrderList.Add(new FightItem
                    {
                        Player = PlayerA,
                        Tank = itemA.TankObject,
                        Code = itemA.Code,
                        Speed = PlayerA.Hero.Speed + itemA.Speed,
                        Attack = PlayerA.Hero.Attack + itemA.Attack,
                        CritRate = PlayerA.Hero.CritRate + itemA.CritRate,
                        //Defense = PlayerA.Hero.Defense + item.Defense,
                        Blood = PlayerA.Hero.Blood + itemA.Blood,
                        // HitRate = item.HitRate,
                        Range = itemA.Range,
                        AttackSkill = itemA.AttackSkill,
                        DefenseSkill = itemA.DefenseSkill,
                        Buffs = new List<Buff>()
                    });
                }

                if (PlayerB.TankList.Count >= i + 1)
                {
                    TankProperty itemB = PlayerB.TankList[i];
                    OrderList.Add(new FightItem
                    {
                        Player = PlayerB,
                        Tank = itemB.TankObject,
                        Code = itemB.Code,
                        Speed = PlayerB.Hero.Speed + itemB.Speed,
                        Attack = PlayerB.Hero.Attack + itemB.Attack,
                        CritRate = PlayerB.Hero.CritRate + itemB.CritRate,
                        //Defense = PlayerB.Hero.Defense + item.Defense,
                        Blood = PlayerB.Hero.Blood + itemB.Blood,
                        //HitRate = item.HitRate,
                        Range = itemB.Range,
                        AttackSkill = itemB.AttackSkill,
                        DefenseSkill = itemB.DefenseSkill,
                        Buffs = new List<Buff>()
                    });
                }
            }
            else
            {
                if (PlayerB.TankList.Count >= i + 1)
                {
                    TankProperty itemB = PlayerB.TankList[i];
                    OrderList.Add(new FightItem
                    {
                        Player = PlayerB,
                        Tank = itemB.TankObject,
                        Code = itemB.Code,
                        Speed = PlayerB.Hero.Speed + itemB.Speed,
                        Attack = PlayerB.Hero.Attack + itemB.Attack,
                        CritRate = PlayerB.Hero.CritRate + itemB.CritRate,
                        //Defense = PlayerB.Hero.Defense + item.Defense,
                        Blood = PlayerB.Hero.Blood + itemB.Blood,
                        //HitRate = item.HitRate,
                        Range = itemB.Range,
                        AttackSkill = itemB.AttackSkill,
                        DefenseSkill = itemB.DefenseSkill,
                        Buffs = new List<Buff>()
                    });
                }

                if (PlayerA.TankList.Count >= i + 1)
                {
                    TankProperty itemA = PlayerA.TankList[i];
                    OrderList.Add(new FightItem
                    {
                        Player = PlayerA,
                        Tank = itemA.TankObject,
                        Code = itemA.Code,
                        Speed = PlayerA.Hero.Speed + itemA.Speed,
                        Attack = PlayerA.Hero.Attack + itemA.Attack,
                        CritRate = PlayerA.Hero.CritRate + itemA.CritRate,
                        //Defense = PlayerA.Hero.Defense + item.Defense,
                        Blood = PlayerA.Hero.Blood + itemA.Blood,
                        // HitRate = item.HitRate,
                        Range = itemA.Range,
                        AttackSkill = itemA.AttackSkill,
                        DefenseSkill = itemA.DefenseSkill,
                        Buffs = new List<Buff>()
                    });
                }

                
            }
        }

        #region 按速度
        //foreach (TankProperty item in PlayerA.TankList)
        //{
        //    OrderList.Add(new FightItem {
        //        Player = PlayerA,
        //        Tank = item.TankObject,
        //        Code = item.Code,
        //        Speed = PlayerA.Hero.Speed + item.Speed,
        //        Attack = PlayerA.Hero.Attack + item.Attack,
        //        CritRate = PlayerA.Hero.CritRate + item.CritRate,
        //        //Defense = PlayerA.Hero.Defense + item.Defense,
        //        Blood = PlayerA.Hero.Blood + item.Blood,
        //       // HitRate = item.HitRate,
        //        Range = item.Range,
        //        AttackSkill = item.AttackSkill,
        //        DefenseSkill = item.DefenseSkill,
        //        Buffs = new List<Buff>()
        //    });
        //}
        //foreach (TankProperty item in PlayerB.TankList)
        //{
        //    OrderList.Add(new FightItem
        //    {
        //        Player = PlayerB,
        //        Tank = item.TankObject,
        //        Code = item.Code,
        //        Speed = PlayerB.Hero.Speed + item.Speed,
        //        Attack = PlayerB.Hero.Attack + item.Attack,
        //        CritRate = PlayerB.Hero.CritRate + item.CritRate,
        //        //Defense = PlayerB.Hero.Defense + item.Defense,
        //        Blood = PlayerB.Hero.Blood + item.Blood,
        //        //HitRate = item.HitRate,
        //        Range = item.Range,
        //        AttackSkill = item.AttackSkill,
        //        DefenseSkill = item.DefenseSkill,
        //        Buffs = new List<Buff>()

        //    });
        //}

        //OrderList = OrderList.OrderByDescending(u => u.Speed).ToList();
        #endregion

        foreach (FightItem item in OrderList)
        {
            RefreshBloodBar(item);
            
            SetupDefenseSkills(item);
            SetupAttackSkills(item);
          
        }

        
    }

    /// <summary>
    /// 为当前攻击方随机设置攻击目标
    /// </summary>
    /// <param name="fightOrder"></param>
    /// <param name="count">目标数</param>
    /// <param name="ignoreTaunt">是否无视嘲讽</param>
    /// <returns></returns>
    public List<FightItem> SetTarget(FightItem fightOrder)
    {
        List<FightItem> list = new List<FightItem>();
  
        int count = 1;
        //对方存活坦克
        List<FightItem> livelist = OrderList.FindAll(u => u.Player.Name != fightOrder.Player.Name && u.Death == false);


        string atkName = "";
        if (fightOrder.AttackSkill != null)
        {
            atkName = fightOrder.AttackSkill.SkillName;
        }
        if (atkName.ToLower() == "scatter")
        {
            count = fightOrder.Tank.GetComponent<Scatter>().Value;
        }

        //是否有决斗buff,如有决斗则判断对方是否存活，若存活则攻击否则取消双方buff攻击其他敌人
        if (fightOrder.Buffs.FindIndex(u => u.Disable == false && u.Name == "duel") > -1)
        {
            foreach (FightItem item in livelist)
            {
                if (item.Buffs.FindIndex(u => u.Disable == false && u.Name == "duel" && (u.FromTankCode==fightOrder.Code || u.TankCode==fightOrder.Code)) > -1)
                {
                    list.Add(item);
                }
            }
            if (list.Count == 0)
            {
                Buff buff = fightOrder.Buffs.Find(u => u.Disable == false && u.Name == "duel");
                buff.Disable = true;
                DestroyImmediate(buff.BuffObject);

                FightItem tar = OrderList.Find(u => (u.Code == buff.FromTankCode || u.Code == buff.TankCode) && u.Code != fightOrder.Code);
                if (tar != null) 
                {
                    Buff tar_buff = tar.Buffs.Find(u => u.Disable == false && u.Name == "duel");
                    if (tar_buff != null)
                    {
                        tar_buff.Disable = true;
                        DestroyImmediate(tar_buff.BuffObject);
                    }
                }

            }
            else
            {
                return list;
            }
        }
        


        //逐个击破 攻击攻击力最弱的
        if (atkName.ToLower() == "vulnerable")
        {
            //按攻击力排序，第一个就是攻击力最弱的
            FightItem fo = livelist.OrderBy(u => u.Attack).FirstOrDefault();
            if (fo != null)
            {
                list.Add(fo);
            }
        }
        //攻无不克 攻击攻击力最强的
        else if (atkName.ToLower() == "unstoppable")
        {
            //按攻击力排序，第一个就是攻击力最弱的
            FightItem fo = livelist.OrderByDescending(u => u.Attack).FirstOrDefault();
            if (fo != null)
            {
                list.Add(fo);
            }
        }
        else
        {
            //具有帝国指令debuff的敌方
            List<FightItem> directive = new List<FightItem>();
            foreach (FightItem item in livelist)
            {
                if (item.Buffs.FindIndex(u => u.Disable == false && u.Name == "directive") > -1)
                {
                    directive.Add(item);
                }
            }


            //具有嘲讽技能的敌方
            List<FightItem> tauntlist = new List<FightItem>();
            foreach (FightItem item in livelist)
            {
                if (item.DefenseSkill != null)
                {
                    if (item.DefenseSkill.SkillName.ToLower() == "taunt")
                    {
                        tauntlist.Add(item);
                    }
                }
            }


            //优先级：帝国指令>嘲讽
            if (directive.Count > 0)
            {
                if (count == directive.Count)
                {
                    list.AddRange(directive);
                }
                else if (count > directive.Count)
                {
                    //先全选帝国指令
                    list.AddRange(directive);
                }
                //小于帝国指令总数
                else
                {
                    list.AddRange(directive.GetRange(0, count));
                }
            }



            //从嘲讽中选
            if (count > list.Count)
            {
                //嘲讽列表中排除帝国指令
                List<FightItem> tauntlist2 = tauntlist.FindAll(u => directive.Contains(u) == false);
                if (tauntlist2.Count > 0)
                {
                    //足够
                    if (count - list.Count == tauntlist2.Count)
                    {
                        list.AddRange(tauntlist2);
                    }
                    //不够
                    else if (count - list.Count > tauntlist2.Count)
                    {
                        list.AddRange(tauntlist2);
                    }
                    //多余
                    else
                    {
                        list.AddRange(tauntlist2.GetRange(0, count - list.Count));
                    }

                }
            }


            //从其余存活中选择
            if (count > list.Count)
            {
                List<FightItem> livelist2 = livelist.FindAll(u => directive.Contains(u) == false && tauntlist.Contains(u) == false);

                if (livelist2.Count > 0)
                {
                    //足够
                    if (count - list.Count == livelist2.Count)
                    {
                        list.AddRange(livelist2);
                    }
                    //不够
                    else if (count - list.Count > livelist2.Count)
                    {
                        list.AddRange(livelist2);
                    }
                    //多余
                    else
                    {
                        List<int> indexs = new List<int>();
                        int index = CommonHelper.GetRandom(0, livelist2.Count);
                        while (indexs.Count < count - list.Count)
                        {
                            if (indexs.Contains(index))
                            {
                                index = CommonHelper.GetRandom(0, livelist2.Count);
                            }
                            else {
                                indexs.Add(index);
                            }
                        }
                        for (int i = 0; i < indexs.Count; i++)
                        {
                            list.Add(livelist2[indexs[i]]);
                        }

                       
                    }

                }
            }
        }

        return list;
    }

   

    /// <summary>
    /// 下个回合
    /// </summary>
    void NextRound() 
    {
        if (Round == 0) { Round++; }
        GameObject roundPanel = UIPanel.transform.Find("txt_round_center").gameObject;
         UIPanel.transform.Find("txt_round_center").SetAsLastSibling();
        Text txt_round = roundPanel.transform.GetComponent<Text>();
        txt_round.text = "Round " + Round;
        UIPanel.transform.Find("txt_round").GetComponent<Text>().text= "Round " + Round;
        roundPanel.SetActive(true);
        roundPanel.transform.DOScale(new Vector3(1, 1, 1), 0.5f).From(new Vector3(0, 0, 0));
        StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
        {
            UIPanel.transform.Find("txt_round_center").gameObject.SetActive(false);
            int nextIndex = GetNextFightIndex();
            Fight(nextIndex);
        },2f));
        
    }


    /// <summary>
    /// 显示减血数字效果
    /// </summary>
    /// <param name="target">目标</param>
    /// <param name="value">数值</param>
    /// <param name="type">0：减血 1：加血 2:魔法伤害</param>
    /// <param name="crit">是否暴击</param>
    void ShowBlood(GameObject target,float value,int type=0,bool crit=false)
    {
        GameObject txtblood = UIPanel.transform.Find("txt_blood").gameObject;
        GameObject newblood = Instantiate(txtblood, UIPanel.transform, false);
        newblood.transform.position = Camera.main.WorldToScreenPoint(target.transform.position + new Vector3(0, CommonHelper.GetRandom(3,7), 0));
        Text val = newblood.transform.Find("txt_val").GetComponent<Text>();
        val.text =(type==1?"+":"-")+ value.ToString();
        switch (type)
        {
            case 0:
                val.color = Color.white;
                break;
            case 1:
                val.color = Color.green;
                break;
            case 2:
                val.color = Color.magenta;
                break;
        }
        newblood.SetActive(true);
        if (crit == true) 
        {
            newblood.transform.Find("critimg").gameObject.SetActive(true);
        }
        newblood.transform.DOMove(Camera.main.WorldToScreenPoint(target.transform.position + new Vector3(-0.5f, CommonHelper.GetRandom(17, 25), 0)), 0.5f);//上飘
        newblood.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.5f).OnComplete(()=> {
            val.DOColor(new Color(val.color.r, val.color.g, val.color.b, 0), 0.8f).OnComplete(() => {
                DestroyImmediate(newblood);
            }); //消失
        });//字体放大
        
    }

    //显示miss 未命中
    void showMiss(FightItem target)
    {
        //闪避触发
        //未命中
        GameObject txtblood = UIPanel.transform.Find("txt_blood").gameObject;
        GameObject miss = Instantiate(txtblood, UIPanel.transform, false);
        miss.transform.position = Camera.main.WorldToScreenPoint(target.Tank.transform.position + new Vector3(0, 2, 0));
        Text val = miss.transform.Find("txt_val").GetComponent<Text>();
        val.text = "MISS";
        val.color = Color.red;
        miss.SetActive(true);

        miss.transform.DOMove(Camera.main.WorldToScreenPoint(target.Tank.transform.position + new Vector3(-0.5f, CommonHelper.GetRandom(16, 25), 0)), 0.5f);//上飘
        miss.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.5f).OnComplete(() => {
            val.DOColor(new Color(val.color.r, val.color.g, val.color.b, 0), 0.8f).OnComplete(() => {
                DestroyImmediate(miss);
            }); //消失
        });//字体放大


    }

    /// <summary>
    /// 刷新血条
    /// </summary>
    /// <param name="fightItem"></param>
    void RefreshBloodBar(FightItem fightItem)
    {
        GameObject bloodbar = UIPanel.transform.Find("bloodbar").gameObject;
        Transform bar = UIPanel.transform.Find("bloodbar_" + fightItem.Code);
        Slider slider;
        if (bar == null)
        {
            bar = Instantiate(bloodbar, UIPanel.transform, false).transform;
            bar.gameObject.name = "bloodbar_" + fightItem.Code;
            StartCoroutine(CommonHelper.DelayToInvokeDo(() => { bar.gameObject.SetActive(true); }, 6f));//延迟显示血条
            slider = bar.GetComponent<Slider>();
            TankProperty tp = fightItem.Player.TankList.Find(u => u.TankObject == fightItem.Tank);
            slider.maxValue = fightItem.Player.Hero.Blood + tp.Blood;
            slider.minValue = 0;
            bar.transform.Find("atk/txt_val").GetComponent<Text>().text = (fightItem.Player.Hero.Attack + tp.Attack).ToString();
            bar.transform.Find("speed/txt_val").GetComponent<Text>().text = (fightItem.Player.Hero.Speed + tp.Speed).ToString();
            bar.transform.Find("crit/txt_val").GetComponent<Text>().text = (fightItem.Player.Hero.CritRate + tp.CritRate).ToString()+"%";
            bar.transform.Find("code/txt_val").GetComponent<Text>().text = "#" + tp.Code;
            if (fightItem.AttackSkill != null)
            {
                bar.transform.Find("skills/atk").GetComponent<RawImage>().texture = CommonHelper.LoadSkillImage(fightItem.AttackSkill.SkillName);
                
            }
            bar.transform.Find("skills/atk").gameObject.SetActive(fightItem.AttackSkill != null);
            if (fightItem.DefenseSkill != null)
            {
                bar.transform.Find("skills/def").GetComponent<RawImage>().texture = CommonHelper.LoadSkillImage(fightItem.DefenseSkill.SkillName);
                
            }
            bar.transform.Find("skills/def").gameObject.SetActive(fightItem.DefenseSkill != null);
        }

       // bar.position = Camera.main.WorldToScreenPoint(fightItem.Tank.transform.position + new Vector3(0, 13, 0));
        slider = bar.GetComponent<Slider>();
        slider.value =  fightItem.Blood<=0?0: fightItem.Blood;
        bar.Find("Fill Area/txt_Val").GetComponent<Text>().text = slider.value.ToString();
        if (slider.value <= slider.maxValue * 0.33)
        {
            bar.Find("Fill Area/Fill").GetComponent<Image>().color = Color.red;
        }
        else if (slider.value <= slider.maxValue * 0.66)
        {
            bar.Find("Fill Area/Fill").GetComponent<Image>().color = Color.yellow;
        }
        else
        {
            bar.Find("Fill Area/Fill").GetComponent<Image>().color = Color.green;
        }
    }

    /// <summary>
    /// 暴击判定
    /// </summary>
    /// <param name="rate">概率</param>
    /// <returns></returns>
    bool IsCrit(float rate) 
    {
        int random = CommonHelper.GetRandom(1, 100);
        return random <= rate;
    }

    /// <summary>
    /// 是否命中判定
    /// </summary>
    /// <param name="rate">概率</param>
    /// <returns></returns>
    bool IsHit(float rate)
    {
        int random = CommonHelper.GetRandom(1, 100);
        return random <= rate;
    }

    /// <summary>
    /// 获取下一个攻击方
    /// </summary>
    /// <returns></returns>
    int GetNextFightIndex() 
    {
        if (FightIndex >= OrderList.Count-1) {
            FightIndex = -1;
        }
        int index = FightIndex+1;    
        while(OrderList[index].Death == true)
        { 
            //一个回合之后进入下一回合
            if (index == OrderList.Count - 1)
            {
                index = -1;
                break;
            }
            else
            {
                index++;
            }
        }
        FightIndex = index;
        return index;
    }

    //轮流对战，递归轮流执行操作
    void Fight(int index)
    {
        CMLookAt(index);
        SwitchPointer(index);
        FightItem fightitem = OrderList[index];

        #region 判定是否有恢复技能
        if (fightitem.DefenseSkill != null)
        {
            if (fightitem.DefenseSkill.SkillName.ToLower() == "recover" && fightitem.Death==false)
            {
                Recover recover = fightitem.Tank.GetComponent<Recover>();
                if (recover != null)
                {

                    if (recover.Trigger() == true)
                    {
                        float maxBlood = fightitem.Player.Hero.Blood + fightitem.Player.TankList.Find(u => u.TankObject == fightitem.Tank).Blood;
                        int addBlood = (int)Math.Round( maxBlood * recover.Value);
                        if (addBlood + fightitem.Blood > maxBlood)
                        {
                            fightitem.Blood = maxBlood;
                        }
                        else
                        {
                            fightitem.Blood += addBlood;
                        }
                        ShowBlood(fightitem.Tank, addBlood, 1);
                        RefreshBloodBar(fightitem);
                    }
                }
            }
        }

        #endregion

        #region 判定是否有Debuff
        //燃烧
        Buff rs = fightitem.Buffs.Find(u => u.Name == "burn" && u.Disable == false);

        if (rs != null)
        {
            rs.EffectCount++;
            float maxBlood = fightitem.Player.Hero.Blood + fightitem.Player.TankList.Find(u => u.TankObject == fightitem.Tank).Blood;
            int rsAtk = (int)Math.Round(maxBlood * rs.Value);
            fightitem.Blood -= rsAtk;
            ShowBlood(fightitem.Tank, rsAtk, 2);
            RefreshBloodBar(fightitem);

            CheckDeath(fightitem);
            
        }


        #endregion

        if (fightitem.Death == false)
        {
            //冻结
            Buff dj = fightitem.Buffs.Find(u => u.Name == "freeze" && u.Disable == false);
            if (dj != null)
            {
                dj.EffectCount = 1;
                dj.Disable = true;
                StartCoroutine(CommonHelper.DelayToInvokeDo(() => { DestroyImmediate(dj.BuffObject); }, 1f));
                //游戏是否结束
                StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
                {
                    CheckGameStatus(index);
                }, 0.3f));
            }
            else
            {
                List<FightItem> targets = SetTarget(fightitem);
                int targetIndex = 0;
                foreach (FightItem item in targets)
                {
                    //普通攻击
                    StartCoroutine(CommonHelper.DelayToInvokeDo(() => { SimpleAttack(fightitem, item, targets.Count>1?true:false); }, (targetIndex+1) * 1f));
                    targetIndex++;
                }
                //游戏是否结束
                StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
                {
                    CheckGameStatus(index);
                }, targets.Count * 0.5f));
            }
        }
        else 
        {
            //游戏是否结束
            StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
            {
                CheckGameStatus(index);
            }, 2f));
        }
       


    }


    //普通攻击
    void SimpleAttack(FightItem fightitem, FightItem target,bool isskill=false)
    {
        if (fightitem.Death == false)
        {
            if (target == null) 
            {
                return;
            }
            Weapon w = fightitem.Tank.GetComponent<Weapon>();
            w.target = new List<GameObject>() { target.Tank };
            w.Shoot(fightitem.Tank);
            //被攻击坦克普通攻击血量减少操作


            bool iscrit = IsCrit(fightitem.CritRate);
            int attack = (int)Math.Round(iscrit ? fightitem.Attack * 2 : fightitem.Attack);



            #region Buff效果生效
            //穿甲
            Buff buff = target.Buffs.Find(u => u.Name == "penetrate" && u.Disable == false);
            if (buff != null)
            {
                attack = (int)Math.Round(attack * (1 + buff.Value));
                buff.Disable = true;
                buff.EffectCount++;
                StartCoroutine(CommonHelper.DelayToInvokeDo(() => { DestroyImmediate(buff.BuffObject); }, 1f));
            }

            #endregion

            #region 防御技能触发判定
            if (target.DefenseSkill != null)
            {
                //禁魔 有禁魔状态的不能使用技能
                Buff jinmo = target.Buffs.Find(u => u.Name == "forbidden" && u.Disable == false);
                if (jinmo != null)
                {
                    jinmo.EffectCount++;
                }
                else
                {
                   // StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
                   // {
                        switch (target.DefenseSkill.SkillName.ToLower())
                        {

                            //圣盾
                            case "holyshield":
                                HolyShield holyShield = target.Tank.GetComponent<HolyShield>();
                                if (holyShield != null)
                                {
                                    if (holyShield.Trigger() == true)
                                    {
                                        CommonHelper.ShowSkillIcon(target.Tank, target.DefenseSkill.SkillName, UIPanel);
                                        return;
                                    }
                                }
                                break;
                            //重甲
                            case "heavyarmor":
                                HeavyArmor heavyArmor = target.Tank.GetComponent<HeavyArmor>();
                                if (heavyArmor != null)
                                {
                                    if (heavyArmor.Trigger() == true)
                                    {
                                        CommonHelper.ShowSkillIcon(target.Tank, target.DefenseSkill.SkillName, UIPanel);
                                        //伤害减少百分比
                                        attack = (int)Math.Round((1 - heavyArmor.Value) * attack);
                                    }
                                }
                                break;
                            //闪避
                            case "dodge":
                                Dodge dodge = target.Tank.GetComponent<Dodge>();
                                if (dodge != null)
                                {
                                    if (dodge.Trigger() == true)
                                    {
                                        CommonHelper.ShowSkillIcon(target.Tank, target.DefenseSkill.SkillName, UIPanel);
                                        //闪避触发
                                        //未命中
                                        showMiss(target);
                                        return;
                                    }
                                }
                                break;
                            //嘲讽
                            case "taunt":
                                Taunt taunt = target.Tank.GetComponent<Taunt>();
                                if (taunt != null)
                                {
                                    if (taunt.Trigger() == true)
                                    {
                                        CommonHelper.ShowSkillIcon(target.Tank, target.DefenseSkill.SkillName, UIPanel);
                                    }
                                }
                                break;
                            //还击
                            case "revenge":
                                Revenge revenge = target.Tank.GetComponent<Revenge>();
                                if (revenge != null)
                                {
                                    if (revenge.Trigger() == true)
                                    {
                                        CommonHelper.ShowSkillIcon(target.Tank, target.DefenseSkill.SkillName, UIPanel);
                                    //攻击伤害来源的坦克
                                    StartCoroutine(CommonHelper.DelayToInvokeDo(() => { SimpleAttack(target, fightitem, true); }, 0.5f)); 
                                    }
                                }
                                break;
                            //反伤
                            case "reflect":
                            Reflect reflect = target.Tank.GetComponent<Reflect>();
                                if (reflect != null)
                                {
                                reflect.AttackTank = fightitem.Tank;
                                    if (reflect.Trigger() == true)
                                    {
                                        CommonHelper.ShowSkillIcon(target.Tank, target.DefenseSkill.SkillName, UIPanel);
                                        //反伤
                                        int replay_atk = (int)Math.Round(attack * reflect.Value);
                                        fightitem.Blood -= replay_atk;
                                        ShowBlood(fightitem.Tank, replay_atk, 0, false);
                                        RefreshBloodBar(fightitem);

                                    }
                                }
                                break;

                        }
                    //}, 0.3f));
                }
            }

            #endregion

            #region 攻击技能特效触发
            if (fightitem.AttackSkill != null && isskill == false)
            {
                //禁魔 有禁魔状态的不能使用技能
                Buff jinmo = fightitem.Buffs.Find(u => u.Name == "forbidden" && u.Disable == false);
                if (jinmo != null)
                {
                    jinmo.EffectCount++;
                }
                else
                {
                    switch (fightitem.AttackSkill.SkillName.ToLower())
                    {
                        case "batter":
                            Batter batter = fightitem.Tank.GetComponent<Batter>();
                            if (batter != null)
                            {
                                batter.TargetTank = target.Tank;
                                target.Blood -= attack;
                                ShowBlood(target.Tank, attack, 0, iscrit);
                                RefreshBloodBar(target);

                                if (batter.EffectAttack(() => { SimpleAttack(fightitem, target, true); }) == true)
                                {
                                    CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                    return;
                                }
                            }
                            break;
                        case "critical":
                            Critical critical = fightitem.Tank.GetComponent<Critical>();
                            if (critical != null)
                            {
                                CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                critical.TargetTank = target.Tank;
                                critical.EffectAttack();

                                attack = (int)Math.Round(attack * critical.Value);
                            }
                            break;
                        case "penetrate":
                            Penetrate penetrate = fightitem.Tank.GetComponent<Penetrate>();
                            if (penetrate != null)
                            {
                                CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                penetrate.TargetTank = target.Tank;
                                penetrate.EffectAttack();
                                //buff标记
                                GameObject pojia = Instantiate(Resources.Load<GameObject>("Skills/Buff/pojia"), target.Tank.transform.parent, false);
                                //添加buff
                                target.Buffs.Add(new Buff
                                {
                                    TankCode = target.Code,
                                    TankObject = target.Tank,
                                    FromTankCode = fightitem.Code,
                                    FromTankObject = fightitem.Tank,
                                    BuffObject = pojia,
                                    Name = "penetrate",
                                    BuffType = "debuff",
                                    Value = penetrate.Value,
                                    EffectCount = 0,
                                    Disable = false
                                });
                            }
                            break;
                        //燃烧
                        case "burn":
                            Burn burn = fightitem.Tank.GetComponent<Burn>();
                            if (burn != null)
                            {
                                CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                burn.TargetTank = target.Tank;
                                burn.EffectAttack();
                                //只挂载一个燃烧效果
                                if (target.Buffs.Find(u => u.Name == "burn" && u.Disable == false) == null)
                                {
                                    //buff标记
                                    
                                    GameObject ranshao = GameObject.Instantiate(CommonHelper.GetPrefabs("skill", "Attack/燃烧"), target.Tank.transform.parent, false);
                                    //添加buff
                                    target.Buffs.Add(new Buff
                                    {
                                        TankCode = target.Code,
                                        TankObject = target.Tank,
                                        FromTankCode = fightitem.Code,
                                        FromTankObject = fightitem.Tank,
                                        BuffObject = ranshao,
                                        Name = "burn",
                                        BuffType = "debuff",
                                        Value = burn.Value,
                                        EffectCount = 0,
                                        Disable = false
                                    });
                                }
                            }
                            break;
                        //散射
                        case "scatter":
                            Scatter scatter = fightitem.Tank.GetComponent<Scatter>();
                            if (scatter != null)
                            {
                                CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                scatter.TargetTank = target.Tank;
                                scatter.EffectAttack();

                            }
                            break;
                        //逐个击破
                        case "vulnerable":
                            Vulnerable vulnerable = fightitem.Tank.GetComponent<Vulnerable>();
                            if (vulnerable != null)
                            {
                                CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                vulnerable.TargetTank = target.Tank;
                                vulnerable.EffectAttack();

                            }
                            break;
                        //攻无不克
                        case "unstoppable":
                            Unstoppable unstoppable = fightitem.Tank.GetComponent<Unstoppable>();
                            if (unstoppable != null)
                            {
                                CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                unstoppable.TargetTank = target.Tank;
                                unstoppable.EffectAttack();

                            }
                            break;
                        //必杀
                        case "hatchshot":
                            Hatchshot hatchshot = fightitem.Tank.GetComponent<Hatchshot>();
                            if (hatchshot != null)
                            {

                                //概率判定成功
                                if (CommonHelper.IsHit(hatchshot.Value))
                                {
                                    CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                    hatchshot.TargetTank = target.Tank;
                                    hatchshot.EffectAttack();
                                    attack = (int)target.Blood;

                                }

                            }
                            break;
                        //吸血
                        case "leech":
                            Leech leech = fightitem.Tank.GetComponent<Leech>();
                            if (leech != null)
                            {

                                CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                leech.TargetTank = target.Tank;
                                leech.EffectAttack();
                                int suckvalue = (int)Math.Round(attack * leech.Value);
                                float maxBlood = fightitem.Player.Hero.Blood + fightitem.Player.TankList.Find(u => u.TankObject == fightitem.Tank).Blood;
                                if (suckvalue + fightitem.Blood > maxBlood)
                                {
                                    fightitem.Blood = maxBlood;
                                }
                                else
                                {
                                    fightitem.Blood += suckvalue;
                                }
                                ShowBlood(fightitem.Tank, suckvalue, 1);
                                RefreshBloodBar(fightitem);

                            }
                            break;
                        //冰冻
                        case "freeze":
                            Freeze freeze = fightitem.Tank.GetComponent<Freeze>();
                            if (freeze != null)
                            {
                                CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                freeze.TargetTank = target.Tank;
                                freeze.EffectAttack();
                                //只挂载一个冰冻效果
                                if (target.Buffs.Find(u => u.Name == "freeze" && u.Disable == false) == null)
                                {
                                    //buff标记
                                    GameObject dongjie = GameObject.Instantiate(CommonHelper.GetPrefabs("skill", "Attack/冰冻"), target.Tank.transform.parent, false);
                                    //添加buff
                                    target.Buffs.Add(new Buff
                                    {
                                        TankCode = target.Code,
                                        TankObject = target.Tank,
                                        FromTankCode = fightitem.Code,
                                        FromTankObject = fightitem.Tank,
                                        BuffObject = dongjie,
                                        Name = "freeze",
                                        BuffType = "debuff",
                                        Value = freeze.Value,
                                        EffectCount = 0,
                                        Disable = false
                                    });
                                }
                            }
                            break;
                        //禁魔
                        case "forbidden":
                            Forbidden forbidden = fightitem.Tank.GetComponent<Forbidden>();
                            if (forbidden != null)
                            {
                                CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                forbidden.TargetTank = target.Tank;
                                forbidden.EffectAttack();
                                //只挂载一个效果
                                if (target.Buffs.Find(u => u.Name == "forbidden" && u.Disable == false) == null)
                                {
                                    //buff标记
                                    GameObject chengmo = Instantiate(Resources.Load<GameObject>("Skills/Buff/chengmo"), target.Tank.transform.parent, false);
                                    //添加buff
                                    target.Buffs.Add(new Buff
                                    {
                                        TankCode = target.Code,
                                        TankObject = target.Tank,
                                        FromTankCode = fightitem.Code,
                                        FromTankObject = fightitem.Tank,
                                        BuffObject = chengmo,
                                        Name = "forbidden",
                                        BuffType = "debuff",
                                        Value = forbidden.Value,
                                        EffectCount = 0,
                                        Disable = false
                                    });
                                }
                            }
                            break;
                        //协同开火
                        case "co-fire":
                            Cofire cofire = fightitem.Tank.GetComponent<Cofire>();
                            if (cofire != null)
                            {
                                //从存活的坦克中选择指定数量的进行一次攻击
                                List<FightItem> livelist = OrderList.FindAll(u => u.Player.Name == fightitem.Player.Name && u.Death == false && u.Code!=fightitem.Code);
                                List<FightItem> atklist = new List<FightItem>();
                                if (cofire.Value >= livelist.Count)
                                {
                                    atklist = livelist;
                                }
                                else
                                {
                                    atklist = livelist.GetRange(0, cofire.Value);
                                }
                                if (atklist.Count > 0)
                                {
                                    CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                    cofire.TargetTank = target.Tank;
                                    cofire.EffectAttack(atklist);

                         
                                    for (int i = 0; i < atklist.Count; i++)
                                    {
                                        FightItem item = atklist[i];
                                        StartCoroutine(CommonHelper.DelayToInvokeDo(() => { SimpleAttack(item, SetTarget(item).FirstOrDefault(), true); }, (i + 1) * 0.3f));
                                    }
                                    
                                }
                            }
                            break;
                        //帝国指令
                        case "directive":
                            Directive directive = fightitem.Tank.GetComponent<Directive>();
                            if (directive != null)
                            {

                                CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                directive.TargetTank = target.Tank;
                                //empiredirective.EffectAttack();
                                //只挂载一个指令效果
                                if (target.Buffs.Find(u => u.Name == "directive" && u.Disable == false) == null)
                                {
                                    //buff标记
                                    GameObject ed = GameObject.Instantiate(CommonHelper.GetPrefabs("skill", "Attack/帝国指令"), target.Tank.transform.parent, false);
                                    
                                    //添加buff
                                    target.Buffs.Add(new Buff
                                    {
                                        TankCode = target.Code,
                                        TankObject = target.Tank,
                                        FromTankCode = fightitem.Code,
                                        FromTankObject = fightitem.Tank,
                                        BuffObject = ed,
                                        Name = "directive",
                                        BuffType = "debuff",
                                        EffectCount = 0,
                                        Disable = false
                                    });
                                }

                            }
                            break;
                        //决斗
                        case "duel":
                            Duel duel = fightitem.Tank.GetComponent<Duel>();
                            if (duel != null)
                            {
                                CommonHelper.ShowSkillIcon(fightitem.Tank, fightitem.AttackSkill.SkillName, UIPanel);
                                duel.TargetTank = target.Tank;
                                duel.EffectAttack();


                                //只挂载一个指令效果
                                if (target.Buffs.Find(u => u.Name == "duel" && u.Disable == false) == null)
                                {
                                    //buff标记
                                    GameObject fromTankIcon = Instantiate(Resources.Load<GameObject>("Skills/Buff/duel"), fightitem.Tank.transform.parent, false);
                                    //GameObject fromTankIcon = Instantiate(UIPanel.transform.Find("duelicon").gameObject, UIPanel.transform, false);
                                    fromTankIcon.SetActive(true);
                                    fromTankIcon.transform.position = fightitem.Tank.transform.position + new Vector3(0, 10.5f, 0);
                                    //fromTankIcon.transform.position = Camera.main.WorldToScreenPoint(fightitem.Tank.transform.position + new Vector3(0, 18, 0));
                                    //添加buff
                                    fightitem.Buffs.Add(new Buff
                                    {
                                        TankCode = target.Code,
                                        TankObject = target.Tank,
                                        FromTankCode = fightitem.Code,
                                        FromTankObject = fightitem.Tank,
                                        BuffObject = fromTankIcon,
                                        Name = "duel",
                                        BuffType = "debuff",
                                        EffectCount = 0,
                                        Disable = false
                                    });

                                    GameObject targetTankIcon = Instantiate(Resources.Load<GameObject>("Skills/Buff/duel"), target.Tank.transform.parent, false);
                                    //GameObject targetTankIcon = Instantiate(UIPanel.transform.Find("duelicon").gameObject, UIPanel.transform, false);
                                    targetTankIcon.SetActive(true);
                                    //targetTankIcon.transform.position = Camera.main.WorldToScreenPoint(target.Tank.transform.position + new Vector3(0, 18, 0));
                                    targetTankIcon.transform.position = target.Tank.transform.position + new Vector3(0, 10.5f, 0);
                                    //添加buff
                                    target.Buffs.Add(new Buff
                                    {
                                        TankCode = target.Code,
                                        TankObject = target.Tank,
                                        FromTankCode = fightitem.Code,
                                        FromTankObject = fightitem.Tank,
                                        BuffObject = targetTankIcon,
                                        Name = "duel",
                                        BuffType = "debuff",
                                        EffectCount = 0,
                                        Disable = false
                                    });
                                }


                                //StartCoroutine(CommonHelper.DelayToInvokeDo(() => { DuelFight(target, fightitem); }, 0.5f));
                                //if (DuelList == null) 
                                //{
                                //    DuelList = new List<FightItem>();
                                //}
                                //DuelList.Add(fightitem);
                                //DuelList.Add(target);
                            }
                            break;
                    }
                }
            }
            #endregion

            target.Blood -= attack;
            ShowBlood(target.Tank, attack, 0, iscrit);
            RefreshBloodBar(target);

            CheckDeath(target);
        }
        //if (times > 1) 
        //{
        //    StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
        //    {
        //        SimpleAttack(fightitem, targets, times - 1);
        //    }, 0.5f));
        //}
    }

 
    /// <summary>
    /// 检查游戏状态 继续或是结束
    /// </summary>
    void CheckGameStatus(int index)
    {
        if (Surrender == true)
        {
            UIPanel.transform.Find("OverPanel").gameObject.SetActive(true);
            UIPanel.transform.Find("OverPanel").SetAsLastSibling();
            UIPanel.transform.Find("OverPanel").DOScale(new Vector3(1, 1, 1), 0.5f).From(new Vector3(1, 0, 0));
            UIPanel.transform.Find("btnBack").gameObject.SetActive(true);
            Finish = true;
            if (Looker != null) Looker.SetActive(false);
            AudioMgr.Instance.PlayAudio("Sound/defeat");
            //UIPanel.transform.Find("txt_round_center").GetComponent<Text>().text = "Defeated";
            UIPanel.transform.Find("defeat").gameObject.SetActive(true);
            UIPanel.transform.Find("defeat").SetAsLastSibling();
            UIPanel.transform.Find("defeat").DOLocalRotate(new Vector3(0, 0, 0), 1f).From(new Vector3(100, 100, 0));
        }
        else
        {
            //判断是否一方全部阵亡，若全部阵亡则不再执行攻击，否则继续。当一个回合结束后，开始下一轮
            if (OrderList.FindAll(u => u.Player.Name == "玩家A" && u.Death == false).Count == 0 || OrderList.FindAll(u => u.Player.Name == "玩家B" && u.Death == false).Count == 0)
            {
                UIPanel.transform.Find("OverPanel").gameObject.SetActive(true);
                UIPanel.transform.Find("OverPanel").SetAsLastSibling();
                UIPanel.transform.Find("OverPanel").DOScale(new Vector3(1, 1, 1), 0.5f).From(new Vector3(1, 0, 0));
                // UIPanel.transform.Find("txt_round_center").gameObject.SetActive(true);
                // UIPanel.transform.Find("txt_round_center").SetAsLastSibling();

                if (OrderList.FindAll(u => u.Player.Name == "玩家A" && u.Death == false).Count == 0)
                {
                    Finish = true;
                    if (Looker != null) Looker.SetActive(false);
                    AudioMgr.Instance.PlayAudio("Sound/defeat");
                    //UIPanel.transform.Find("txt_round_center").GetComponent<Text>().text = "Defeated";
                    UIPanel.transform.Find("defeat").gameObject.SetActive(true);
                    UIPanel.transform.Find("defeat").SetAsLastSibling();
                    UIPanel.transform.Find("defeat").DOLocalRotate(new Vector3(0, 0, 0), 1f).From(new Vector3(100, 100, 0));
                }
                else
                {
                    Finish = true;
                    if (Looker != null) Looker.SetActive(false);
                    AudioMgr.Instance.PlayAudio("Sound/victory");
                    //UIPanel.transform.Find("txt_round_center").GetComponent<Text>().text = "VICTORY";
                    UIPanel.transform.Find("victory").gameObject.SetActive(true);
                    UIPanel.transform.Find("victory").SetAsLastSibling();
                    UIPanel.transform.Find("victory").DOLocalRotate(new Vector3(0, 0, 0), 1f).From(new Vector3(100, 100, 0));
                }

                UIPanel.transform.Find("btnBack").gameObject.SetActive(true);

            }
            else
            {
                if (index >= OrderList.Count - 1)
                {
                    Round++;
                    NextRound();
                }
                else
                {


                    int nextindex = GetNextFightIndex();
                    if (nextindex == -1)
                    {
                        Round++;
                        NextRound();
                    }
                    else
                    {
                        StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
                        {
                            Fight(nextindex);
                        }, 1.2f));
                    }

                }

            }
        }
    }


    /// <summary>
    /// 判定是否死亡
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    bool CheckDeath(FightItem target)
    {
        if (target.Blood <= 0)
        {
            target.Blood = 0;
            target.Death = true;
            //死亡爆炸效果
            GameObject death = Instantiate(CommonHelper.GetPrefabs("skill", "Death"));
            death.SetActive(true);
            death.transform.SetParent(target.Tank.transform.parent.parent, false);
            //StartCoroutine(CommonHelper.DelayToInvokeDo(() => { DestroyImmediate(death); }, 4f));
            Animator t_animator = target.Tank.transform.GetChild(0).GetComponent<Animator>();
            if (t_animator != null && t_animator.gameObject.activeInHierarchy)
            {
                t_animator.Play("Death");
            }

            #region 死亡后判定是否有复生技能和亡语技能
            if (target.DefenseSkill != null)
            {
                if (target.DefenseSkill.SkillName.ToLower() == "revive")
                {
                    Revive revive = target.Tank.GetComponent<Revive>();
                    if (revive != null)
                    {
                        if (revive.Trigger() == true)
                        {
                            target.Death = false;
                            target.Blood = (int)((target.Player.Hero.Blood + target.Player.TankList.Find(u => u.TankObject == target.Tank).Blood) * 0.2f);
                            CommonHelper.ShowSkillIcon(target.Tank, target.DefenseSkill.SkillName, UIPanel);
                            StartCoroutine(CommonHelper.DelayToInvokeDo(() =>
                            {
                                //恢复20%血量，移除死亡效果，恢复正常状态
                                Animator animator = target.Tank.transform.GetChild(0).GetComponent<Animator>();
                                if (animator != null && animator.gameObject.activeInHierarchy)
                                {
                                    animator.Play("Idle");
                                }

                                DestroyImmediate(death);
                                RefreshBloodBar(target);
                            }, 1f));
                        }
                    }
                }
                else if (target.DefenseSkill.SkillName.ToLower() == "deathrally")
                {
                    DeathRally deathrally = target.Tank.GetComponent<DeathRally>();
                    if (deathrally != null)
                    {
                        if (deathrally.Trigger() == true)
                        {
                            CommonHelper.ShowSkillIcon(target.Tank, target.DefenseSkill.SkillName, UIPanel);
                            //亡语效果

                        }
                    }
                }

            }
            #endregion

            //死亡后移除身上的特效
            if (target.Death == true)
            {
                if (target.DefenseSkill != null)
                {
                    Transform sk = target.Tank.transform.parent.Find(target.DefenseSkill.Title + "(Clone)");
                    if (sk != null)
                    {
                        DestroyImmediate(sk.gameObject);
                    }
                }
                //移除buff效果
                if (target.Buffs.Count > 0)
                {
                    foreach (Buff item in target.Buffs.FindAll(u=>u.Disable==false))
                    {
                        item.Disable = true;
                        DestroyImmediate(item.BuffObject);
                    }
                }
            }

            
        }
        return target.Death;
    }


   

    
    //防御技能挂载
    void SetupDefenseSkills(FightItem fromItem)
    {
        if (fromItem.DefenseSkill != null)
        {
            string skillname = fromItem.DefenseSkill.Title;
            if (!string.IsNullOrEmpty(skillname))
            {
                switch (fromItem.DefenseSkill.SkillName.ToLower())
                {
                    case "holyshield":
                        HolyShield holyshield = fromItem.Tank.AddComponent<HolyShield>();
                        holyshield.Total = 4;
                        holyshield.Effected = 0;
                        holyshield.Tank = fromItem.Tank;
                        break;
                    case "heavyarmor":
                        HeavyArmor heavyArmor = fromItem.Tank.AddComponent<HeavyArmor>();
                        heavyArmor.Value = 0.7f;
                        heavyArmor.Effected = 0;
                        heavyArmor.Tank = fromItem.Tank;
                        break;
                    case "dodge":
                        Dodge dodge = fromItem.Tank.AddComponent<Dodge>();
                        dodge.Value = 0.4f;
                        dodge.Effected = 0;
                        dodge.Tank = fromItem.Tank;
                        break;
                    case "revive":
                        Revive revive = fromItem.Tank.AddComponent<Revive>();
                        revive.Total = 1;
                        revive.Effected = 0;
                        revive.Tank = fromItem.Tank;
                        break;
                    case "recover":
                        Recover recover = fromItem.Tank.AddComponent<Recover>();
                        recover.Value = 0.1f;
                        recover.Effected = 0;
                        recover.Tank = fromItem.Tank;
                        break;
                        //嘲讽
                    case "taunt":
                        Taunt taunt = fromItem.Tank.AddComponent<Taunt>();
                        //taunt.Value = 0.1f;
                        taunt.Effected = 0;
                        taunt.Tank = fromItem.Tank;
                        break;
                    case "deathrally":
                        DeathRally deathrally = fromItem.Tank.AddComponent<DeathRally>();
                        deathrally.Total = 1;
                        deathrally.Effected = 0;
                        deathrally.Tank = fromItem.Tank;
                        break;
                    case "revenge":
                        Revenge revenge = fromItem.Tank.AddComponent<Revenge>();
                        //revenge.Total = 1;
                        revenge.Effected = 0;
                        revenge.Tank = fromItem.Tank;
                        break;
                    case "reflect":
                        Reflect reflect = fromItem.Tank.AddComponent<Reflect>();
                        reflect.Value = 0.5f;
                        reflect.Effected = 0;
                        reflect.Tank = fromItem.Tank;
                        break;
                    default:
                        //获取技能
                        GameObject skillprefab = CommonHelper.GetPrefabs("skill", "Defense/" + skillname);
                        GameObject skill = Instantiate(skillprefab, fromItem.Tank.transform.parent.parent, false);
                        skill.SetActive(true);
                        break;
                }
             
            }
        }
    }

    //攻击技能挂载
    void SetupAttackSkills(FightItem fromItem)
    {
        if (fromItem.AttackSkill != null)
        {
            string skillname = fromItem.AttackSkill.SkillName;
            if (!string.IsNullOrEmpty(skillname))
            {
                switch (fromItem.AttackSkill.SkillName.ToLower())
                {
                    //连击
                    case "batter":
                        Batter batter = fromItem.Tank.AddComponent<Batter>();
                        batter.Value = 3;//连续攻击3次
                        batter.Effected = 0;
                        batter.FromTank = fromItem.Tank;
                        break;
                    //重击
                    case "critical":
                        Critical critical = fromItem.Tank.AddComponent<Critical>();
                        critical.Value = 2; //2倍攻击
                        critical.Effected = 0;
                        critical.FromTank = fromItem.Tank;
                        break;
                    //穿甲
                    case "penetrate":
                        Penetrate penetrate = fromItem.Tank.AddComponent<Penetrate>();
                        penetrate.Value = 0.5f; //50%附加伤害
                        penetrate.Effected = 0;
                        penetrate.FromTank = fromItem.Tank;
                        break;
                    //燃烧
                    case "burn":
                        Burn burn = fromItem.Tank.AddComponent<Burn>();
                        burn.Value = 0.1f; //10%附加伤害
                        burn.Effected = 0;
                        burn.FromTank = fromItem.Tank;
                        break;
                    //散射
                    case "scatter":
                        Scatter scatter = fromItem.Tank.AddComponent<Scatter>();
                        scatter.Value = 3;//同时攻击3个目标
                        scatter.Effected = 0;
                        scatter.FromTank = fromItem.Tank;
                        break;
                    //逐个击破
                    case "vulnerable":
                        Vulnerable vulnerable = fromItem.Tank.AddComponent<Vulnerable>();
                        vulnerable.Effected = 0;
                        vulnerable.FromTank = fromItem.Tank;
                        break;
                    //攻无不克
                    case "unstoppable":
                        Unstoppable unstoppable = fromItem.Tank.AddComponent<Unstoppable>();
                        unstoppable.Effected = 0;
                        unstoppable.FromTank = fromItem.Tank;
                        break;
                    //必杀
                    case "hatchshot":
                        Hatchshot hatchshot = fromItem.Tank.AddComponent<Hatchshot>();
                        hatchshot.Value = 25f;
                        hatchshot.Effected = 0;
                        hatchshot.FromTank = fromItem.Tank;
                        break;
                    //吸血
                    case "leech":
                        Leech leech = fromItem.Tank.AddComponent<Leech>();
                        leech.Value = 0.4f;
                        leech.Effected = 0;
                        leech.FromTank = fromItem.Tank;
                        break;
                    //冰冻
                    case "freeze":
                        Freeze freeze = fromItem.Tank.AddComponent<Freeze>();
                        freeze.Value =1;
                        freeze.Effected = 0;
                        freeze.FromTank = fromItem.Tank;
                        break;
                    //禁魔
                    case "forbidden":
                        Forbidden forbidden = fromItem.Tank.AddComponent<Forbidden>();
                        forbidden.Value = 0;
                        forbidden.Effected = 0;
                        forbidden.FromTank = fromItem.Tank;
                        break;
                    //协同开火
                    case "co-fire":
                        Cofire cofire = fromItem.Tank.AddComponent<Cofire>();
                        cofire.Value = 3;
                        cofire.Effected = 0;
                        cofire.FromTank = fromItem.Tank;
                        break;
                    //帝国指令
                    case "directive":
                        Directive directive = fromItem.Tank.AddComponent<Directive>();
                        directive.Effected = 0;
                        directive.FromTank = fromItem.Tank;
                        break;
                    //决斗
                    case "duel":
                        Duel duel = fromItem.Tank.AddComponent<Duel>();
                        duel.Effected = 0;
                        duel.FromTank = fromItem.Tank;
                        break;
                }
            }
        }
    }


    /// <summary>
    /// 相机朝向
    /// </summary>
    /// <param name="index">坦克坐标</param>
    void CMLookAt(int index)
    {
        foreach (FightItem item in OrderList)
        {
            Transform cm2 = item.Tank.transform.parent.parent.Find("CM");
            if (cm2 != null)
            {
                cm2.gameObject.SetActive(false);
            }
        }
        if (LookAtSwitch == true)
        {
            Transform cm = OrderList[index].Tank.transform.parent.parent.Find("CM");
            if (cm != null)
            {
                cm.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 切换攻击方指示器
    /// </summary>
    void SwitchPointer(int index)
    {

        Transform tankPos = OrderList[index].Tank.transform.parent.parent;
        if (tankPos != null)
        {
            if (OrderList[index].Player.Name == "玩家A")
            {
                GreenPointer.SetActive(true);
                RedPointer.SetActive(false);
                GreenPointer.transform.position = new Vector3(tankPos.position.x, GreenPointer.transform.position.y, tankPos.position.z);
                GreenPointer.transform.DOScale(new Vector3(1,1,1), 0.5f).From(new Vector3(2,2,2));
            }
            else
            {
                GreenPointer.SetActive(false);
                RedPointer.SetActive(true);
                RedPointer.transform.position = new Vector3(tankPos.position.x, RedPointer.transform.position.y, tankPos.position.z);
                RedPointer.transform.DOScale(new Vector3(1, 1, 1), 0.5f).From(new Vector3(2, 2, 2));
            }
            
        }
        
    }
}
