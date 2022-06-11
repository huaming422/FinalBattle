using System;
using System.IO;
using System.Xml;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace BlGame.Resource
{
    //ResourceManager는 자산들을 경로를 통하여 읽는 작용을 한다. 
    public class ResourcesManager : UnitySingleton<ResourcesManager>
    {
        //assetbundle을 사용하겠는가를 결정한다.
        public bool UsedAssetBundle = false;

        private bool mInit = false;
        private int mFrameCount = 0;
        private Request mCurrentRequest = null;
        //mAllRequests의 개수가 0보다 크면 Request를 처리한다.
        private Queue<Request> mAllRequests = new Queue<Request>();

        //Assetbundle을리용하는 경우 Resource정보를 관리한다.
        private AssetInfoManager mAssetInfoManager = null;
        private Dictionary<string, string> mResources = new Dictionary<string, string>();
        //Assetbundle을리용하는 경우 불러들인Resource를 기록한다.
        private Dictionary<string, ResourceUnit> mLoadedResourceUnit = new Dictionary<string, ResourceUnit>();

        public delegate void HandleFinishLoad(ResourceUnit resource);
        public delegate void HandleFinishLoadLevel();
        public delegate void HandleFinishUnLoadLevel();


        public void Init()
        {
            //assetbundle을 리용하는 경우
            if (UsedAssetBundle)
            {
                mAssetInfoManager = new AssetInfoManager();
                mAssetInfoManager.LoadAssetInfo();

                ArchiveManager.Instance.Init();
            }
            //mIint는 assetbundle을 리용하는 경우 자산을 불러들였다는것을 규정한다.
            mInit = true;
        }

        
        public void Update()
        {
          
            if (!mInit)
                return;

            if (null == mCurrentRequest && mAllRequests.Count > 0)
                handleRequest();

            ++mFrameCount;
            if (mFrameCount == 300)
            {
                //Resources.UnloadUnusedAssets();
                mFrameCount = 0;
            }
        }
        /// <summary>
        /// //handleRequest는 오직 Scene읽기만을 처리한다.
        /// </summary>

        private void handleRequest()
        {
            //assetbundle을 리용하는 경우
            if (UsedAssetBundle)
            {
                mCurrentRequest = mAllRequests.Dequeue();

                //相对Asset的完整资源路径
                string fileName = mCurrentRequest.mFileName;

                //ResourceCommon.Log("handleRequest, the type is : " + mCurrentRequest.mResourceType + "\nthe relativePath path is : " + relativePath);

                switch (mCurrentRequest.mRequestType)
                {
                    case RequestType.LOAD:
                        {
                            switch (mCurrentRequest.mResourceType)
                            {
                                case ResourceType.ASSET:
                                case ResourceType.PREFAB:
                                    {
                                        if (mLoadedResourceUnit.ContainsKey(fileName))
                                        {
                                            //(mLoadedResourceUnit[fileName] as ResourceUnit).addReferenceCount();

                                            mCurrentRequest.mResourceAsyncOperation.mComplete = true;
                                            mCurrentRequest.mResourceAsyncOperation.mResource = mLoadedResourceUnit[fileName] as ResourceUnit;

                                            if (null != mCurrentRequest.mHandle)
                                                mCurrentRequest.mHandle(mLoadedResourceUnit[fileName] as ResourceUnit);
                                            handleResponse();
                                        }
                                        else
                                        {
                                            //传入相对路径名称
                                            //StartCoroutine(_load(fileName, mCurrentRequest.mHandle, mCurrentRequest.mResourceType, mCurrentRequest.mResourceAsyncOperation));
                                        }
                                    }
                                    break;
                                case ResourceType.LEVELASSET:
                                    {
                                        DebugEx.LogError("do you real need a single level asset??? this is have not decide!!!", ResourceCommon.DEBUGTYPENAME);
                                    }
                                    break;
                                case ResourceType.LEVEL:
                                    {
                                        DebugEx.LogError("this is impossible!!!", ResourceCommon.DEBUGTYPENAME);
                                    }
                                    break;
                            }
                        }
                        break;
                    case RequestType.UNLOAD:
                        {
                            if (!mLoadedResourceUnit.ContainsKey(fileName))
                                DebugEx.LogError("can not find " + fileName, ResourceCommon.DEBUGTYPENAME);
                            else
                            {
                                //(mLoadedResourceUnit[fileName] as ResourceUnit).reduceReferenceCount();
                            }
                            handleResponse();
                        }
                        break;
                    case RequestType.LOADLEVEL:
                        {
                            StartCoroutine(_loadLevel(fileName, mCurrentRequest.mHandleLevel, ResourceType.LEVEL, mCurrentRequest.mResourceAsyncOperation));
                        }
                        break;
                    case RequestType.UNLOADLEVEL:
                        {
                            if (!mLoadedResourceUnit.ContainsKey(fileName))
                                DebugEx.LogError("can not find level " + fileName, ResourceCommon.DEBUGTYPENAME);
                            else
                            {
                                //(mLoadedResourceUnit[fileName] as ResourceUnit).reduceReferenceCount();

                                if (null != mCurrentRequest.mHandleUnloadLevel)
                                    mCurrentRequest.mHandleUnloadLevel();
                            }
                            handleResponse();
                        }
                        break;
                }
            }
            //assetbundle을 리용하지 않는경우
            else
            {
                mCurrentRequest = mAllRequests.Dequeue();

                switch (mCurrentRequest.mRequestType)
                {
                    case RequestType.LOAD:
                        {
                            switch (mCurrentRequest.mResourceType)
                            {
                                case ResourceType.ASSET:
                                case ResourceType.PREFAB:
                                    {
                                        //暂시不处理，直接使用资源相对路径
                                        //StartCoroutine(_load(mCurrentRequest.mFileName, mCurrentRequest.mHandle, mCurrentRequest.mResourceType, mCurrentRequest.mResourceAsyncOperation));
                                    }
                                    break;
                                case ResourceType.LEVELASSET:
                                    {
                                        DebugEx.LogError("do you real need a single level asset??? this is have not decide!!!", ResourceCommon.DEBUGTYPENAME);
                                    }
                                    break;
                                case ResourceType.LEVEL:
                                    {
                                        DebugEx.LogError("this is impossible!!!", ResourceCommon.DEBUGTYPENAME);
                                    }
                                    break;
                            }
                        }
                        break;
                    case RequestType.UNLOAD:
                        {
                            handleResponse();
                        }
                        break;
                    case RequestType.LOADLEVEL:
                        {
                            StartCoroutine(_loadLevel(mCurrentRequest.mFileName, mCurrentRequest.mHandleLevel, ResourceType.LEVEL, mCurrentRequest.mResourceAsyncOperation));
                        }
                        break;
                    case RequestType.UNLOADLEVEL:
                        {
                            if (null != mCurrentRequest.mHandleUnloadLevel)
                                mCurrentRequest.mHandleUnloadLevel();
                            handleResponse();
                        }
                        break;
                }
            }
        }

        private void handleResponse()
        {
            mCurrentRequest = null;
        }
        ///<summary>
        //해당경로를 통하여 각종형태의 자산들을 불러들인다.
        ///</summary>
        public ResourceUnit loadImmediate(string filePathName, ResourceType resourceType, string archiveName = "Resources")
        {
            //使用assetbundle打包
            if (UsedAssetBundle)
            {
                //添加Resource
                string completePath = "Resources/" + filePathName;

                string completeName = ArchiveManager.Instance.getPath("Resources", completePath);

                //根据场景名称获取asset信息
                AssetInfo sceneAssetInfo = mAssetInfoManager.GetAssetInfo(completeName);

                //获取依赖的asset的索引
                foreach (int index in sceneAssetInfo.mDependencys)
                {
                    //根据索引获取依赖的Asset
                    AssetInfo depencyAsset = mAssetInfoManager.GetAssetInfo(index);
                    string depencyAssetName = depencyAsset.mName;

                    //加载场景依赖assetbundle


                    _LoadImmediate(depencyAssetName, ResourceType.ASSET);
                }

                //加载本身预制件
                ResourceUnit unit = _LoadImmediate(completeName, resourceType);

                return unit;
            }
            //不使用
            else
            {
                Object asset = Resources.Load(filePathName);
                ResourceUnit resource = new ResourceUnit(null, 0, asset, null, resourceType);
                return resource;
            }

        }

        

        /// <summary>
        //fileName = "Scene/1"
        //자산을 읽기위한 AysncOperation을 창조하며 mAllRequest에 Request 를 추가한다.
        //여기서 Request를 추가하면 Update()에서 처리한다.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="handle"></param>
        /// <param name="archiveName"></param>
        /// <returns></returns>
        
        public ResourceAsyncOperation loadLevel(string fileName, HandleFinishLoadLevel handle, string archiveName = "Level")
        {
            //使用assetbundle打包
            if (UsedAssetBundle)
            {
                //ResourceCommon.Log("loadLevel : " + fileName + " and the archiveName is : " + archiveName);

                //获取完整路径
                string completeName = ArchiveManager.Instance.getPath(archiveName, fileName);
                if (mLoadedResourceUnit.ContainsKey(completeName))
                {
                    DebugEx.LogError("why you load same level twice, maybe you have not unload last time!!!", ResourceCommon.DEBUGTYPENAME);
                    return null;
                }
                else
                {
                    ResourceAsyncOperation operation = new ResourceAsyncOperation(RequestType.LOADLEVEL);
                    mAllRequests.Enqueue(new Request(completeName, ResourceType.LEVEL, handle, RequestType.LOADLEVEL, operation));
                    return operation;
                }
            }
            //不使用
            else
            {
                ResourceAsyncOperation operation = new ResourceAsyncOperation(RequestType.LOADLEVEL);
                mAllRequests.Enqueue(new Request(fileName, ResourceType.LEVEL, handle, RequestType.LOADLEVEL, operation));
                return operation;
            }
        }

        /// <summary>
        /// //Scene읽기를 진행한다.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="handle"></param>
        /// <param name="resourceType"></param>
        /// <param name="operation"></param>
        /// <returns></returns>

        private IEnumerator _loadLevel(string path, HandleFinishLoadLevel handle, ResourceType resourceType, ResourceAsyncOperation operation)
        {
            //使用assetbundle打包
            if (UsedAssetBundle)
            {
                //根据场景名称获取asset信息
                AssetInfo sceneAssetInfo = mAssetInfoManager.GetAssetInfo(path);
                //获取该包总大小
                operation.mAllDependencesAssetSize = mAssetInfoManager.GetAllAssetSize(sceneAssetInfo);

                //获取依赖的asset的索引
                foreach (int index in sceneAssetInfo.mDependencys)
                {
                    //根据索引获取依赖的Asset
                    AssetInfo depencyAsset = mAssetInfoManager.GetAssetInfo(index);
                    string depencyAssetName = depencyAsset.mName;

                    //加载场景依赖assetbundle
                    ResourceUnit unit = _LoadImmediate(depencyAssetName, ResourceType.LEVEL);
                    operation.mLoadDependencesAssetSize += unit.AssetBundleSize;
                }

                //加载场景assetbundle     
                int scenAssetBundleSize = 0;
                byte[] binary = ResourceCommon.getAssetBundleFileBytes(path, ref scenAssetBundleSize);
                AssetBundle assetBundle = AssetBundle.LoadFromMemory(binary);
                if (!assetBundle)
                    DebugEx.LogError("create scene assetbundle " + path + "in _LoadImmediate failed");

                //添加场景大小
                operation.mLoadDependencesAssetSize += scenAssetBundleSize;

               // AsyncOperation asyncOperation = Application.LoadLevelAsync(ResourceCommon.getFileName(path, false));
                AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(ResourceCommon.getFileName(path, false));
                operation.asyncOperation = asyncOperation;
                yield return asyncOperation;

                
                handleResponse();

               
                operation.asyncOperation = null;
                operation.mComplete = true;
                operation.mResource = null;

                //ResourceCommon.Log("end inner loadLevel :" + path);

                if (null != handle)
                    handle();


            }
            //不使用
            else
            {
                ResourceUnit level = new ResourceUnit(null, 0, null, path, resourceType);

                //获取加载场景名称 
                string sceneName = ResourceCommon.getFileName(path, true);
                
                AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
				operation.asyncOperation = asyncOperation;
                yield return asyncOperation;

                handleResponse();

                operation.asyncOperation = null;
                operation.mComplete = true;

                if (null != handle)
                    handle();
            }
        }

        //assetbundle의 resource를 불러들인다.
        ResourceUnit _LoadImmediate(string fileName, ResourceType resourceType)
        {
            //没有该资源，加载
            if (!mLoadedResourceUnit.ContainsKey(fileName))
            {
                //资源大小
                int assetBundleSize = 0;
                byte[] binary = ResourceCommon.getAssetBundleFileBytes(fileName, ref assetBundleSize);
                AssetBundle assetBundle = AssetBundle.LoadFromMemory(binary);
                if (!assetBundle)
                    DebugEx.LogError("create assetbundle " + fileName + "in _LoadImmediate failed");

                Object asset = assetBundle.LoadAsset(fileName);
                if (!asset)
                    DebugEx.LogError("load assetbundle " + fileName + "in _LoadImmediate failed");


                //调试用
                //DebugEx.LogError("load asset bundle:" + fileName);

                ResourceUnit ru = new ResourceUnit(assetBundle, assetBundleSize, asset, fileName, resourceType);

                //添加到资源中
                mLoadedResourceUnit.Add(fileName, ru);

                return ru;
            }
            else
            {
                return mLoadedResourceUnit[fileName];
            }
        }







       
        //assetbundle을 리용하여 불러들이기 위한 resource의 경로를 txt화일을 리용하여 정해 놓는데 txt를 읽는다.
        public static Stream Open(string path)
        {
            string localPath;
            //Andrio跟IOS环境使用沙箱目录
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                localPath = string.Format("{0}/{1}", Application.persistentDataPath, path + ResourceCommon.assetbundleFileSuffix);
            }
            //Window下使用assetbunlde资源目录
            else
            {
                localPath = ResourceCommon.assetbundleFilePath + path + ResourceCommon.assetbundleFileSuffix;
            }

            //首先检查沙箱目录中是否有更新资源
            if (File.Exists(localPath))
            {
                return File.Open(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            //没有的话原始包中查找
            else
            {
                TextAsset text = Resources.Load(path) as TextAsset;
                if (null == text)
                    DebugEx.LogError("can not find : " + path + " in OpenText", ResourceCommon.DEBUGTYPENAME);
                return new MemoryStream(text.bytes);
            }


        }

        public static StreamReader OpenText(string path)
        {
            return new StreamReader(Open(path), System.Text.Encoding.Default);
        }

       
    }
}