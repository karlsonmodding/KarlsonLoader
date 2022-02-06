using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.AI;
using System;
using System.Threading;

namespace KarlsonLoader
{
    public class Prefabs
    {
        /* Weapons */

        private static GameObject pistol;
        public static GameObject NewPistol()
        {
            GameObject _pistol = UnityEngine.Object.Instantiate(pistol);
            _pistol.name = "Pistol #" + UnityEngine.Random.Range(0, 32767);
            _pistol.SetActive(true);
            return _pistol;
        }

        private static GameObject ak47;
        public static GameObject NewAk47()
        {
            GameObject _ak47 = UnityEngine.Object.Instantiate(ak47);
            _ak47.name = "Ak47 #" + UnityEngine.Random.Range(0, 32767);
            _ak47.SetActive(true);
            return _ak47;
        }

        private static GameObject shotgun;
        public static GameObject NewShotgun()
        {
            GameObject _shotgun = UnityEngine.Object.Instantiate(shotgun);
            _shotgun.name = "Shotgun #" + UnityEngine.Random.Range(0, 32767);
            _shotgun.SetActive(true);
            return _shotgun;
        }

        private static GameObject boomer;
        public static GameObject NewBoomer()
        {
            GameObject _boomer = UnityEngine.Object.Instantiate(boomer);
            _boomer.name = "Boomer #" + UnityEngine.Random.Range(0, 32767);
            _boomer.SetActive(true);
            return _boomer;
        }

        private static GameObject grappler;
        private static GameObject grappler_aim;
        public static GameObject NewGrappler()
        {
            GameObject _grappler = UnityEngine.Object.Instantiate(grappler);
            _grappler.name = "Grappler #" + UnityEngine.Random.Range(0, 32767);
            _grappler.SetActive(true);
            _grappler.GetComponent<Grappler>().aim = UnityEngine.Object.Instantiate(grappler_aim);
            return _grappler;
        }


        /* Entities */

        private static GameObject table;
        public static GameObject NewTable()
        {
            GameObject _table = UnityEngine.Object.Instantiate(table);
            _table.name = "Table #" + UnityEngine.Random.Range(0, 32767);
            _table.SetActive(true);
            return _table;
        }

        private static GameObject barrel;
        public static GameObject NewBarrel()
        {
            GameObject _barrel = UnityEngine.Object.Instantiate(barrel);
            _barrel.name = "Barrel #" + UnityEngine.Random.Range(0, 32767);
            _barrel.SetActive(true);
            return _barrel;
        }

        private static GameObject locker;
        public static GameObject NewLocker()
        {
            GameObject _locker = UnityEngine.Object.Instantiate(locker);
            _locker.name = "Locker #" + UnityEngine.Random.Range(0, 32767);
            _locker.SetActive(true);
            return _locker;
        }

        private static GameObject screen;
        public static GameObject NewScreen()
        {
            GameObject _screen = UnityEngine.Object.Instantiate(screen);
            _screen.name = "Screen #" + UnityEngine.Random.Range(0, 32767);
            _screen.SetActive(true);
            return _screen;
        }


        /* Misc */

        private static GameObject enemy;
        public static GameObject NewEnemy()
        {
            GameObject _enemy = UnityEngine.Object.Instantiate(enemy);
            _enemy.name = "Enemy #" + UnityEngine.Random.Range(0, 32767);
            _enemy.SetActive(true);
            _enemy.GetComponent<NavMeshAgent>().enabled = true;
            Enemy e = _enemy.GetComponent<Enemy>();
            e.startGun = NewPistol();
            typeof(Enemy).GetMethod("GiveGun", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(e, Array.Empty<object>());
            return _enemy;
        }

        private static GameObject milk;
        public static GameObject NewMilk()
        {
            GameObject _milk = UnityEngine.Object.Instantiate(milk);
            _milk.name = "Milk #" + UnityEngine.Random.Range(0, 32767);
            _milk.SetActive(true);
            return _milk;
        }

        static bool initialize = false;

        public static void InitializePrefabs()
        {
            initialize = true;
            SceneManager.sceneLoaded += OnInitialize;
            var asd = SceneManager.LoadSceneAsync("0Tutorial");
        }

        private static void OnInitialize(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name != "0Tutorial")
                return;
            if (!initialize)
                return;
            initialize = false;
            foreach (var o in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                switch (o.name)
                {
                    case "Enemy":
                    {
                        Animator animator = o.GetComponentInChildren<Animator>();
                        animator.SetBool("Running", false);
                        animator.SetBool("Aiming", false);
                        o.GetComponent<NavMeshAgent>().enabled = false;
                        o.transform.localScale = new Vector3(0.399f, 0.399f, 0.399f);
                        /*foreach (Collider coll in o.GetComponentsInChildren<BoxCollider>())
                            coll.enabled = false;
                        foreach (Collider coll in o.GetComponentsInChildren<CapsuleCollider>())
                            coll.enabled = false;
                        foreach (Collider coll in o.GetComponentsInChildren<Collider>())
                            coll.enabled = false;*/
                        GameObject _enemy = UnityEngine.Object.Instantiate(o);
                        _enemy.name = "KarlsonLoader-Instance Enemy";
                        UnityEngine.Object.DontDestroyOnLoad(_enemy);
                        _enemy.SetActive(false);
                        _enemy.transform.position = new Vector3(-10000f, -10000f, -10000f);
                        enemy = _enemy;
                        LoaderAsm.Loader.Log("[KarlsonLoader] (Prefabs) Instantiated Enemy");
                        break;
                    }
                    case "Pistol":
                    {
                        pistol = UnityEngine.Object.Instantiate(o);
                        pistol.name = "KarlsonLoader-Instance Pistol";
                        UnityEngine.Object.DontDestroyOnLoad(pistol);
                        pistol.SetActive(false);
                        LoaderAsm.Loader.Log("[KarlsonLoader] (Prefabs) Instantiated Pistol");
                        break;
                    }
                    case "Ak47":
                    {
                        ak47 = UnityEngine.Object.Instantiate(o);
                        ak47.name = "KarlsonLoader-Instance Ak47";
                        UnityEngine.Object.DontDestroyOnLoad(ak47);
                        ak47.SetActive(false);
                        LoaderAsm.Loader.Log("[KarlsonLoader] (Prefabs) Instantiated Ak47");
                        break;
                    }
                    case "Shotgun":
                    {
                        shotgun = UnityEngine.Object.Instantiate(o);
                        shotgun.name = "KarlsonLoader-Instance Shotgun";
                        UnityEngine.Object.DontDestroyOnLoad(shotgun);
                        shotgun.SetActive(false);
                        LoaderAsm.Loader.Log("[KarlsonLoader] (Prefabs) Instantiated Shotgun");
                        break;
                    }
                    case "Boomer":
                    {
                        boomer = UnityEngine.Object.Instantiate(o);
                        boomer.name = "KarlsonLoader-Instance Boomer";
                        UnityEngine.Object.DontDestroyOnLoad(boomer);
                        boomer.SetActive(false);
                        LoaderAsm.Loader.Log("[KarlsonLoader] (Prefabs) Instantiated Boomer");
                        break;
                    }
                    case "Grappler":
                    {
                        grappler = UnityEngine.Object.Instantiate(o);
                        grappler.name = "KarlsonLoader-Instance Grappler";
                        UnityEngine.Object.DontDestroyOnLoad(grappler);
                        grappler.SetActive(false);
                        grappler_aim = UnityEngine.Object.Instantiate(o.GetComponent<Grappler>().aim);
                        UnityEngine.Object.DontDestroyOnLoad(grappler_aim);
                        LoaderAsm.Loader.Log("[KarlsonLoader] (Prefabs) Instantiated Grappler");
                        break;
                    }
                    case "Table":
                    {
                        table = UnityEngine.Object.Instantiate(o);
                        table.name = "KarlsonLoader-Instance Table";
                        UnityEngine.Object.DontDestroyOnLoad(table);
                        table.SetActive(false);
                        LoaderAsm.Loader.Log("[KarlsonLoader] (Prefabs) Instantiated Table");
                        break;
                    }
                    case "Barrel":
                    {
                        barrel = UnityEngine.Object.Instantiate(o);
                        barrel.name = "KarlsonLoader-Instance Barrel";
                        UnityEngine.Object.DontDestroyOnLoad(barrel);
                        barrel.SetActive(false);
                        LoaderAsm.Loader.Log("[KarlsonLoader] (Prefabs) Instantiated Barrel");
                        break;
                    }
                    case "Locker":
                    {
                        locker = UnityEngine.Object.Instantiate(o);
                        locker.name = "KarlsonLoader-Instance Locker";
                        UnityEngine.Object.DontDestroyOnLoad(locker);
                        locker.SetActive(false);
                        LoaderAsm.Loader.Log("[KarlsonLoader] (Prefabs) Instantiated Locker");
                        break;
                    }
                    case "Screen":
                    {
                        screen = UnityEngine.Object.Instantiate(o);
                        screen.name = "KarlsonLoader-Instance Screen";
                        UnityEngine.Object.DontDestroyOnLoad(screen);
                        screen.SetActive(false);
                        LoaderAsm.Loader.Log("[KarlsonLoader] (Prefabs) Instantiated Screen");
                        break;
                    }
                    case "Milk":
                    {
                        milk = UnityEngine.Object.Instantiate(o);
                        milk.name = "KarlsonLoader-Instance Milk";
                        UnityEngine.Object.DontDestroyOnLoad(milk);
                        milk.SetActive(false);
                        LoaderAsm.Loader.Log("[KarlsonLoader] (Prefabs) Instantiated Milk");
                        break;
                    }
                }
            }
            MonoHooks.instance.StartCoroutine(LoadBack());
        }

        private static IEnumerator LoadBack()
        {
            yield return new WaitForSeconds(0.1f);
            SceneManager.LoadScene("Initialize");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
