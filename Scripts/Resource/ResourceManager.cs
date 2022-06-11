using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
//-------------------------------------------------------------------------------
//壅꾣틦?녵맏?삭풌壅꾣틦?뚨돥?녻탡繹?
//?삭풌壅꾣틦??삭풌?띴퐳?誤곭쉪?ε룭竊뚧캈倻귛쑛??펽鰲믦돯嶺?
//?⑴릤壅꾣틦??돥?녻탡繹먪쉪瀯꾣닇竊뚧캈倻굆esh,texture嶺?
//歷멩닆壅꾣틦?꾤??녽삭풌訝삭쫨?녵릎?③삭풌掠귡씊竊뚨돥?녻탡繹먬싪퓝?뷴댍嶸←릤
//
//-------------------------------------------------------------------------------
//瓦쇿룾?썸삸訝닸뿶?방죭
//?삭풌壅꾣틦凉뺟뵪?⑴릤壅꾣틦竊뚦폊?ⓨ벆雅쏂탡繹먨쑉?삭풌壅꾣틦?꼙refab訝?춼?①뇨凉?
//-------------------------------------------------------------------------------

namespace BlGame.Resource
{
    public class ResourceManager
    {
        private Dictionary<string, LogicResouce> logicResDic = new Dictionary<string, LogicResouce>();
        private Dictionary<string, PhyResouce> phyResDic = new Dictionary<string, PhyResouce>();
        //凉귝??좄슬
        private Dictionary<string, LoadImplement> loadImplDic = new Dictionary<string, LoadImplement>();
        private Dictionary<string, LogicResourceBuilder> logicResBuilder = new Dictionary<string, LogicResourceBuilder>();
        public static readonly ResourceManager Instance = new ResourceManager();
        //
        public LoadProcess loadProcess = new LoadProcess();
        public ResourceManager()
        {

        }
        //?룟룚LoadImplement?꾣빊??
        public int getLoadCount()
        {
            return loadImplDic.Count;
        }
        //
        public void Update()
        {
            if (loadProcess != null)
            {
                if (loadProcess.isLoading == true)
                {
                    loadProcess.Update();

                }
            }

            List<string> removeLoadList = new List<string>();
            //?띶럣loadimplement
            foreach (LoadImplement l in loadImplDic.Values)
            {
                if (l.www.isDone == true)
                {
                    if (l.www.error == null)
                    {
                        if (l.createReq == null)
                        {
                            l.createReq = AssetBundle.LoadFromMemoryAsync(l.www.bytes);
                        }
                        if (l.createReq.isDone == true)
                        {
                            // GameDefine.GameMethod.DebugError("Load Res Success:" + l.resPath);
                            PhyResouce phyRes = new PhyResouce();
                            phyRes.resPath = l.resPath;
                            phyRes.phyResType = l.phyResType;
                            phyRes.assetBundle = l.createReq.assetBundle;
                            //if(phyResDic.ContainsKey(phyRes.resPath) == true)
                            //{
                            //    Debug.LogError("already have:"+phyRes.resPath);
                            //}
                            //else
                            // Debug.LogError("phy res :" + phyRes.resPath);
                            phyResDic.Add(phyRes.resPath, phyRes);
                            // }
                            removeLoadList.Add(l.resPath);


                        }
                    }
                    else
                    {
                        GameDefine.GameMethod.DebugError("Load Res Failed:" + l.www.url + "======" + l.resPath + " error is:" + l.www.error.ToString());
                        removeLoadList.Add(l.resPath);
                    }
                }
            }
            foreach (string s in removeLoadList)
            {

                LoadImplement l = getLoadImplement(s);
                if (l.onResLoaded != null)
                {
                    l.onResLoaded(l.resPath);
                }
                loadImplDic.Remove(s);
            }

            List<string> removeBuilderList = new List<string>();
            foreach (LogicResourceBuilder builder in logicResBuilder.Values)
            {
                bool allLoaded = true;
                foreach (string s in builder.resLists)
                {
                    if (GetPhyRes(s) == null)
                    {
                        allLoaded = false;
                        break;
                    }
                }
                if (allLoaded == true)
                {
                    removeBuilderList.Add(builder.resPath);
                    if (builder.onLogicResourceBuilded != null)
                    {
                        builder.onLogicResourceBuilded(builder.resPath);
                    }
                    LogicResouce logicRes = new LogicResouce();
                    logicRes.resPath = builder.resPath;
                    logicRes.logicResType = builder.logicResType;
                    logicRes.phyResList = builder.resLists;
                    if (logicResDic.ContainsKey(logicRes.resPath) == true)
                    {
                        logicResDic[logicRes.resPath] = logicRes;
                    }
                    else
                    {
                        logicResDic.Add(logicRes.resPath, logicRes);
                    }


                    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


                    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                }
            }
            foreach (string s in removeBuilderList)
            {
                logicResBuilder.Remove(s);
            }
            //
        }
        //
        //?좄슬訝訝ら삭풌壅꾣틦,鵝욜뵪?삣줊?좄슬
        public void AsynLoadLogicRes(string path, LogicResouce.ELogicResType type, OnResLoaded onLoaded = null)
        {
            if (GetLogicRes(path) != null)
            {
                if (onLoaded != null)
                {
                    onLoaded(path);
                }
                return;
            }
            //
            //LogicResouce res = new LogicResouce();
            //res.resPath = path;
            //res.logicResType = type;
            switch (type)
            {
                case LogicResouce.ELogicResType.ERes_Effect:
                    {
                        LoadImpl(path, PhyResouce.EPhyResType.EPhyResPrefab,
                            (resPath) =>
                            {
                                AssetBundle ab = ResourceManager.Instance.GetPhyRes(path).assetBundle;
                                StringScriptableObject holder = (StringScriptableObject)ab.LoadAsset("DependentBundleNames");
                                LogicResourceBuilder logicBuilder = new LogicResourceBuilder();
                                logicBuilder.resPath = resPath;
                                logicBuilder.logicResType = type;
                                logicBuilder.onLogicResourceBuilded = (builderPath) =>
                                {
                                    if (onLoaded != null)
                                    {
                                        onLoaded(builderPath);
                                    }
                                };
                                //
                                if (holder != null)
                                {
                                    if (holder.content != null)
                                    {
                                        foreach (string s in holder.content)
                                        {
                                            logicBuilder.resLists.Add(s);
                                            LoadImpl(s, PhyResouce.EPhyResType.EPhyResPrefab);
                                            //Debug.LogError(s);
                                        }
                                    }
                                }
                                ResourceManager.Instance.AddLogicResourceBuilder(logicBuilder);
                            }
                        , null);
                    }
                    break;

            }
        }
        public void AddPhyRes(string resPath, PhyResouce phyRes)
        {
            if (GetPhyRes(resPath) != null)
            {
                return;
            }
            phyResDic[resPath] = phyRes;
        }
        //?룟룚壅꾣틦
        public LogicResouce GetLogicRes(string path)
        {
            if (logicResDic.ContainsKey(path) == false)
            {
                return null;
            }
            return logicResDic[path];
        }
        //?딀붂壅꾣틦
        public void ReleaseLogicResource(string path)
        {
        }
        //
        public void LoadImpl(string path, PhyResouce.EPhyResType type, OnResLoaded onLoaded = null, OnResLoadError onError = null)
        {
            //GameDefine.GameMethod.DebugError("load " + path);
            if (phyResDic.ContainsKey(path) == true)
            {
                return;
            }
            if (loadImplDic.ContainsKey(path) == true)
            {
                return;
            }
            LoadImplement l = new LoadImplement();
            l.phyResType = type;
            l.onResLoaded = onLoaded;
            l.onResLoadError = onError;
            l.resPath = path;
            l.start();
            loadImplDic.Add(path, l);
        }
        //?룟룚?⑴릤壅꾣틦
        public PhyResouce GetPhyRes(string path)
        {
            if (phyResDic.ContainsKey(path) == false)
            {
                return null;
            }
            return phyResDic[path];
        }
        //
        public void AddLogicResourceBuilder(LogicResourceBuilder builder)
        {
            //
            if (logicResBuilder.ContainsKey(builder.resPath))
            {
                return;
            }
            if (logicResDic.ContainsKey(builder.resPath))
            {
                return;
            }
            logicResBuilder.Add(builder.resPath, builder);
        }
        //
        public LoadImplement getLoadImplement(string path)
        {
            if (loadImplDic.ContainsKey(path))
            {
                return loadImplDic[path];
            }
            return null;
        }
    }
}

