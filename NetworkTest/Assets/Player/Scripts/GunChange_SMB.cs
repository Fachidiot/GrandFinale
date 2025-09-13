using System.Collections.Generic;
using UnityEngine;

public class GunChange_SMB : StateMachineBehaviour, IEventCenterComponent, ICurveEventsList, IEventsLists
{
    [Range(0, 1)] public float endTime; // transition time to another animation
    public WeaponController weaponController;
    public EventsCenter EventsCenter { get; set; }

    [SerializeField] private List<CurveEventModel> curveEvents = new List<CurveEventModel>();
    public List<CurveEventModel> CurveEvents { get => curveEvents; set => curveEvents = value; }

    [SerializeField] private List<EventModel> enterEvents = new List<EventModel>();
    public List<EventModel> EnterEvents { get => enterEvents; set => enterEvents = value; }

    [SerializeField] private List<EventModel> updateEvents = new List<EventModel>();
    public List<EventModel> UpdateEvents { get => updateEvents; set => updateEvents = value; }

    [SerializeField] private List<EventModel> exitEvents = new List<EventModel>();
    public List<EventModel> ExitEvents { get => exitEvents; set => exitEvents = value; }

    public void setWeaponController(WeaponController weaponController)
    {
        this.weaponController = weaponController;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        if (UpdateEvents.Count > 0)
        {
            foreach (var eventModel in UpdateEvents) //shutdown for everyone events
            {
                eventModel.done = false;
            }
        }

        if (EventsCenter == null | EnterEvents.Count == 0) return;
        foreach (var eventModel in EnterEvents)
        {
            // invoke enterEvents
            EventsCenter.EventInvoke(eventModel.funcName, EventParameterConverter.ConvertParametersToObjectType(eventModel.parameters));
        }
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.normalizedTime > endTime)
        {
            if (weaponController.nextID == 0) return;

            //set active id
            weaponController.activeID = weaponController.nextID;

            //if nextID != 0 then transition to grab animation
            animator.CrossFadeInFixedTime("GrabSlot" + weaponController.nextID, 0.25f, layerIndex);
            weaponController.nextID = 0;
            return;
        }

        if (EventsCenter == null) return;

        foreach (var eventModel in UpdateEvents)
        {
            if (eventModel.done) continue;
            
            // invoke updateEvents
            if (stateInfo.normalizedTime > eventModel.time && !eventModel.done)
            {
                EventsCenter.EventInvoke(eventModel.funcName,
                        EventParameterConverter.ConvertParametersToObjectType(eventModel.parameters));
                eventModel.done = true;
            }
        }

        foreach (var animationCurveEvent in CurveEvents)
        {
              // invoke curveEvents
            object[] floatValueParameter = { animationCurveEvent.animationCurve.Evaluate(stateInfo.normalizedTime) };
            EventsCenter.EventInvoke(animationCurveEvent.eventName, floatValueParameter);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (EventsCenter == null) return;

          // invoke exitEvents
        foreach (var eventModel in ExitEvents)
        {
            EventsCenter.EventInvoke(eventModel.funcName,
                    EventParameterConverter.ConvertParametersToObjectType(eventModel.parameters));
        }
    }

}
