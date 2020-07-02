using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DualityES { 
    public class Event
    {

    }
    public class EventSystem {
        private static EventSystem _instance = null;

        public static EventSystem instance //Ensures that this is the only instance in the class
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EventSystem();
                }
                return _instance;
            }
        }

        /*
        void ShowUI(DialogueUIOption option)
            public delegate void EventDelegate(DialogueUIOption e);
        */
        /*
         * <T> allows use to be any type of datatype. The where T : Event makes it so that it has to be dereived from Event class
         */
        public delegate void EventDelegate<T>(T e) where T : Event; // Declaring the delegate of a genric type. T can be replaced with any parameter
        private delegate void EventDelegate(Event e); //The delegeate for all the events

        private Dictionary<System.Type, EventDelegate> eventDelegates = new Dictionary<System.Type, EventDelegate>();//Creates Dictionary for all the Delegates of different Event types
        private Dictionary<System.Delegate, EventDelegate> delegateLookup = new Dictionary<System.Delegate, EventDelegate>();//Creates a dictionary for all the events 

        public void AddListener<T>(EventDelegate<T> del) where T : Event
        {
            if (delegateLookup.ContainsKey(del))//If delegate is already stored in the dictionary then exit
            {
                return;
            }

            EventDelegate internalDelegate = (e) => del((T)e); //Storing the delegate into a local variable
            delegateLookup[del] = internalDelegate; //Adds the local varaible into the Dictionary with all the events

            EventDelegate tempDel;
            if(eventDelegates.TryGetValue(typeof(T), out tempDel)){ 
                /*^ Sees if the dictionary with delegates of this type exist
                 * Sets the local variable tempDel to the value of delegate
                 */
                eventDelegates[typeof(T)] = tempDel += internalDelegate;
                //searching for the type T Delegates and adding the new delegate to The type Dictionary of type t delegates
            }
            else
            {
                //If the dictionary doesn't not have the T type values then it will search for internalDelegate
                eventDelegates[typeof(T)] = internalDelegate;
            }

            if(delegateLookup.TryGetValue(del, out internalDelegate))
            {

            }

        }

        public void RemoveListener<T>(EventDelegate<T> del) where T : Event //Removes Delegates for the Delegate Dictionary
        {
            Debug.Log("RemoveListener Works/ is called");
            EventDelegate internalDelegate; // this an Event Delegate
            if (delegateLookup.TryGetValue(del, out internalDelegate))// looking for internal Delegate. Sets the local variable internalDelegate to the one passed in
            {
                EventDelegate tempDel;
                if (eventDelegates.TryGetValue(typeof(T), out tempDel)) //looking through the dictionary of the same type to see if it exist. Sets the local variable tempDel to it
                {
                    tempDel -= internalDelegate;//Removes the parameter delegate from the delegate of all the same type

                    if (tempDel == null) //if it is an empety delegate of a certain type  
                    {
                        eventDelegates.Remove(typeof(T)); //The empty delegate 
                    }
                    else
                    {
                        eventDelegates[typeof(T)] = tempDel; //If delegate is not empty then set the delegate to the new delegate without the missing delegate
                    }
                }
                delegateLookup.Remove(del);//Removes the event delegate from the dictrioary lookup
            }
        }

        public void RaiseEvent(Event eventArguements)//Calls the delegate action
        {
            EventDelegate del;
            if(eventDelegates.TryGetValue(eventArguements.GetType(), out del)) //Sets del to all the EventDelegates
            {
                del.Invoke(eventArguements); //Calls all the events
            }
        }
    }
}