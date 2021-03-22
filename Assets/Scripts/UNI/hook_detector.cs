﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;

public class hook_detector : MonoBehaviour
{
    private Inputs inputs;

    /*[HideInInspector] */public GameObject nearest_hook;
    private GameObject player;
    public List<GameObject> all_hooks;

    [HideInInspector] public bool nearHook = false;

    //HINTS & DEAD ROBOTS
    [HideInInspector] public bool nearHint = false;
    [HideInInspector] public bool nearDead = false;
    [HideInInspector] public Transform hintPosition;
    [HideInInspector] public Transform deadPosition;

    [SerializeField] private int index = 0;

    public bool gamepad = false;

    Vector3 LastPosition;
    private void Awake()
    {
        inputs = new Inputs();
    }

    private void OnEnable()
    {
        inputs.Enable();
    }
    private void OnDisable()
    {
        inputs.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        all_hooks = new List<GameObject>();
        player = GameObject.FindGameObjectWithTag("uni");

        inputs.Uni.NextHook.performed += ctx => NextIndex();
        inputs.Uni.PrevHook.performed += ctx => PrevIndex();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (all_hooks.Count != 0)
        {
            if(!nearest_hook)
                nearest_hook = all_hooks.OrderBy(o => Vector3.Distance(Camera.main.WorldToScreenPoint(o.transform.position), Mouse.current.position.ReadValue())).ToList()[0];

            //SELECT HOOK WITH GAMEPAD
            if (inputs.Uni.NextHook.ReadValue<float>() > 0.1f || inputs.Uni.PrevHook.ReadValue<float>() > 0.1f)
                nearest_hook = all_hooks[index % all_hooks.Count];

            //CHOOSE THE NEAREST HOOK TO THE CURSOR
            if (Vector3.Distance(Mouse.current.position.ReadValue(), LastPosition) > .1f)
                nearest_hook = all_hooks.OrderBy(o => Vector3.Distance(Camera.main.WorldToScreenPoint(o.transform.position), Mouse.current.position.ReadValue())).ToList()[0];
        }
        else
            nearest_hook = null;


        //CHANGE THE APPEARANCE OF THE SELECTED HOOK
        foreach(var h in all_hooks)
        {
            if(h == nearest_hook) h.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.red);
            else h.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.white);
        }

        LastPosition = Mouse.current.position.ReadValue();
    }

    public GameObject nh()
    {
        return nearest_hook;
    }

    void NextIndex()
    {
        if (index == all_hooks.Count - 1)
            index = 0;
        else
            index++;
        //nearest_hook = all_hooks[all_hooks.IndexOf(nearest_hook) + 1];
    }

    void PrevIndex()
    {
        if (index == 0)
            index += all_hooks.Count;
        else
            index--;
        //nearest_hook = all_hooks[all_hooks.IndexOf(nearest_hook) - 1];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!all_hooks.Contains(other.gameObject) && (other.tag == "hook" || other.tag == "movable_hook" || other.tag == "lever") )
        {
            all_hooks.Add(other.gameObject);
            nearHook = true;
        }
        else

        // HINTS
        if (other.tag == "Hint")
        {
            hintPosition = other.gameObject.transform;
            nearHint = true;
        }

        // DEAD ROBOT
        if (other.tag == "Dead")
        {
            deadPosition = other.gameObject.transform;
            nearDead = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "hook" || other.tag == "movable_hook")
        {
            nearHook = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (all_hooks.Contains(other.gameObject))
        {
            all_hooks.Remove(other.gameObject);
            other.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.white);
            nearHook = false;
        }

        // HINTS
        if (other.tag == "Hint")
        {
            nearHint = false;
        }

        //DEAD
        if (other.tag == "Dead")
        {
            nearDead = false;
        }
    }
}
