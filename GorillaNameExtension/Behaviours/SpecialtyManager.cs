/*

MIT License

Copyright (c) 2021-2024 RusJJ

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using GorillaLibrary.Utilities;
using GorillaNameExtension.Models;
using GorillaNetworking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace GorillaNameExtension.Behaviours;

internal class SpecialtyManager : MonoBehaviour
{
    public static Color FriendColour = new(0.8f, 0.5f, 0.9f);

    public static Color RecentTagColour = new(1f, 0.67f, 0.67f);

    public static int RecentTimeDelay;

    public static int RecentTimeLength;

    private static HashSet<string> friendUserIDs = [];

    private static readonly HashSet<string> checkedUserIDs = [];

    private static readonly Dictionary<string, RecentlyPlayed> checkedUserCache = [];

    public void Start()
    {
        FriendColour = Plugin.Config.Bind("Specialty", "Friend Colour", FriendColour, "The colour of names for players you have added as a friend").Value;
        RecentTagColour = Plugin.Config.Bind("Specialty", "Recent Player Colour", RecentTagColour, "The colour of names for players you recently played with").Value;
        RecentTimeDelay = Plugin.Config.Bind("Specialty", "Recent Delay", 5, "The delay in seconds before a player is counted as being recently played with").Value;
        RecentTimeLength = Plugin.Config.Bind("Specialty", "Recent Duration", 259200, "The duration in seconds that counts as being recently played within").Value;

        NetworkSystem.Instance.OnMultiplayerStarted += OnJoinedRoom;
        NetworkSystem.Instance.OnReturnedToSinglePlayer += OnLeftRoom;
        NetworkSystem.Instance.OnPlayerJoined += OnPlayerJoined;
        NetworkSystem.Instance.OnPlayerLeft += OnPlayerLeft;
        FriendBackendController.Instance.OnGetFriendsComplete += OnGetFriends;
        FriendBackendController.Instance.OnAddFriendComplete += OnAddFriend;
        FriendBackendController.Instance.OnRemoveFriendComplete += OnRemoveFriend;

        StartCoroutine(nameof(FriendCheck));
    }

    #region Room/Player Join/Leave Methods

    public void OnJoinedRoom()
    {
        Plugin.Logger.LogMessage("Join");

        FriendBackendController.Instance.GetFriends();

        foreach (NetPlayer netPlayer in NetworkSystem.Instance.PlayerListOthers)
        {
            ScanPlayer(netPlayer);
        }
    }

    public void OnPlayerJoined(NetPlayer netPlayer)
    {
        if (IsFriend(netPlayer)) FriendBackendController.Instance.GetFriends();
        ScanPlayer(netPlayer);
    }

    private void ScanPlayer(NetPlayer netPlayer)
    {
        string userId = netPlayer.UserId;

        bool updateName = false;

        if (NeedToCheckRecently(userId) && checkedUserIDs.Add(userId))
        {
            updateName = true;

            var hasPlayedBefore = HasPlayedWithUsRecently(userId);
            checkedUserCache.Add(userId, hasPlayedBefore);

            Plugin.Logger.LogMessage($"{netPlayer.GetPlayerRef().NickName}: {hasPlayedBefore.State} with value {hasPlayedBefore.Value:F2}");
        }

        if (IsFriend(userId)) updateName = true;

        if (updateName && RigUtility.TryGetRig(netPlayer, out var rig)) rig.Rig.UpdateName();
    }

    public void OnPlayerLeft(NetPlayer netPlayer)
    {
        string userId = netPlayer.UserId;

        if (!NeedToCheckRecently(userId))
        {
            checkedUserIDs.Remove(userId);
            checkedUserCache.Remove(userId);
        }
    }

    public void OnLeftRoom()
    {
        checkedUserIDs.Clear();
        checkedUserCache.Clear();
    }

    #endregion

    #region Friend Methods

    public static bool IsFriend(NetPlayer player) => IsFriend(player.UserId);

    public static bool IsFriend(string userId) => friendUserIDs.Contains(userId);

    private void OnGetFriends(bool success)
    {
        if (!success) return;

        var friends = FriendBackendController.Instance.FriendsList;
        friendUserIDs = [.. friends.Where(friend => friend != null && friend.Presence != null).Select(friend => friend.Presence.FriendLinkId)];
        RigUtility.Rigs.Values.ForEach(rig => rig.Rig.UpdateName());
        GorillaScoreboardTotalUpdater.instance.UpdateActiveScoreboards();

        Plugin.Logger.LogMessage($"Friend list returned with {friends.Count} total");
    }

    private void OnAddFriend(NetPlayer player, bool success)
    {
        if (!success) return;

        friendUserIDs.Add(player.UserId);
        RigUtility.GetRig(player)?.Rig?.UpdateName();
        GorillaScoreboardTotalUpdater.instance.UpdateActiveScoreboards();

        Plugin.Logger.LogMessage($"Friend added {player.GetPlayerRef()}");
    }

    private void OnRemoveFriend(FriendBackendController.Friend friend, bool success)
    {
        if (!success || friend == null || friend.Presence == null) return;

        friendUserIDs.Remove(friend.Presence.FriendLinkId);
        RigUtility.Rigs.Values.ForEach(rig => rig.Rig.UpdateName());
        GorillaScoreboardTotalUpdater.instance.UpdateActiveScoreboards();

        Plugin.Logger.LogMessage("Friend removed");
    }

    private IEnumerator FriendCheck()
    {
        yield return new WaitWhile(() => string.IsNullOrEmpty(PlayFabAuthenticator.instance.GetPlayFabPlayerId()));
        FriendBackendController.Instance.GetFriends();
        yield break;
    }

    #endregion

    #region Recent Player Methods

    public static bool NeedToCheckRecently(string userId) => !checkedUserIDs.Contains(userId);

    public static RecentlyPlayed HasPlayedWithUsRecently(string userId)
    {
        if (checkedUserCache.TryGetValue(userId, out var cache))
            return cache;

        string key = GetRecentKey(userId);
        string value = PlayerPrefs.GetString(key, "-1");

        RecentlyPlayed result = RecentlyPlayed.Never;
        long curTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

        if (value == "-1" || !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long time)) goto SetResult;

        if (time > curTime - RecentTimeDelay && time <= curTime)
        {
            result = RecentlyPlayed.Now;
            goto SetResult;
        }

        if ((time + RecentTimeLength) > curTime)
        {
            result = new()
            {
                Value = 1f - Mathf.InverseLerp(time, time + RecentTimeLength, curTime),
                State = RecentState.Before
            };
        }

    SetResult:

        PlayerPrefs.SetString(key, (result.State == RecentState.Before ? curTime + RecentTimeDelay : curTime).ToString());

        return result;
    }

    private static string GetRecentKey(string userId) => string.Concat("rp_", userId);

    #endregion
}
