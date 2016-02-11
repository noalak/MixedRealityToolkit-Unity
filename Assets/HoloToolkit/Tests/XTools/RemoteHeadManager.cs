﻿using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.XTools;
using System.Collections.Generic;

/// <summary>
/// Broadcasts the head transform of the local user to other users in the session,
/// and adds and updates the head transforms of remote users.  
/// Head transforms are sent and received in the local coordinate space of the gameobject
/// this component is on.  
/// </summary>
public class RemoteHeadManager : Singleton<RemoteHeadManager>
{
    public class RemoteHeadInfo
    {
        public long UserID;
        public GameObject HeadObject;        
    }

    /// <summary>
    /// Keep a list of the remote heads, indexed by XTools userID
    /// </summary>
    Dictionary<long, RemoteHeadInfo> remoteHeads = new Dictionary<long, RemoteHeadInfo>();

    void Start()
    {
        XtoolsServerManager.Instance.MessageHandlers[XtoolsServerManager.TestMessageID.HeadTransform] = this.UpdateHeadTransform;
        
        XToolsSessionTracker.Instance.SessionJoined += Instance_SessionJoined;
        XToolsSessionTracker.Instance.SessionLeft += Instance_SessionLeft;
    }

    void Update()
    {
        // Grab the current head transform and broadcast it to all the other users in the session
        Transform headTransform = Camera.main.transform;

        // Transform the head position and rotation into local space
        Vector3 headPosition = this.transform.InverseTransformPoint(headTransform.position);
        Quaternion headRotation = Quaternion.Inverse(this.transform.rotation) * headTransform.rotation;
        XtoolsServerManager.Instance.SendHeadTransform(headPosition, headRotation);
    }


    /// <summary>
    /// Called when a new user is leaving.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Instance_SessionLeft(object sender, XToolsSessionTracker.SessionLeftEventArgs e)
    {
        RemoveRemoteHead(this.remoteHeads[e.exitingUserId].HeadObject);
        this.remoteHeads.Remove(e.exitingUserId);
    }

    /// <summary>
    /// Called when a user is joining.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Instance_SessionJoined(object sender, XToolsSessionTracker.SessionJoinedEventArgs e)
    {
#if !UNITY_EDITOR
        GetRemoteHeadInfo(e.joiningUser.GetID());
#endif
    }

    /// <summary>
    /// Gets the data structure for the remote users' head position.
    /// </summary>
    /// <param name="userID"></param>
    /// <returns></returns>
    public RemoteHeadInfo GetRemoteHeadInfo(long userID)
    {
        RemoteHeadInfo headInfo;

        // Get the head info if its already in the list, otherwise add it
        if (!this.remoteHeads.TryGetValue(userID, out headInfo))
        {
            headInfo = new RemoteHeadInfo();
            headInfo.UserID = userID;
            headInfo.HeadObject = CreateRemoteHead();

            this.remoteHeads.Add(userID, headInfo);
        }

        return headInfo;
    }

    /// <summary>
    /// Called when a remote user sends a head transform.
    /// </summary>
    /// <param name="msg"></param>
    void UpdateHeadTransform(NetworkInMessage msg)
    {
        // Parse the message
        long userID = msg.ReadInt64();

        Vector3 headPos = Vector3.zero;
        headPos.x = msg.ReadFloat();
        headPos.y = msg.ReadFloat();
        headPos.z = msg.ReadFloat();

        Quaternion headRot = Quaternion.identity;
        headRot.x = msg.ReadFloat();
        headRot.y = msg.ReadFloat();
        headRot.z = msg.ReadFloat();
        headRot.w = msg.ReadFloat();

        RemoteHeadInfo headInfo = GetRemoteHeadInfo(userID);
        headInfo.HeadObject.transform.localPosition = headPos;
        headInfo.HeadObject.transform.localRotation = headRot;        
    }

    /// <summary>
    /// Creates a new game object to represent the user's head.
    /// </summary>
    /// <returns></returns>
    GameObject CreateRemoteHead()
    {
        GameObject newHeadObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newHeadObj.transform.parent = this.gameObject.transform;
        newHeadObj.transform.localScale = Vector3.one * 0.2f;
        return newHeadObj;
    }

    /// <summary>
    /// When a user has left the session this will cleanup their
    /// head data.
    /// </summary>
    /// <param name="remoteHeadObject"></param>
	void RemoveRemoteHead(GameObject remoteHeadObject)
    {
        DestroyImmediate(remoteHeadObject);
    }
}