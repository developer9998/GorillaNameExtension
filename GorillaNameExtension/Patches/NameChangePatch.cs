using GorillaNameExtension.Behaviours;
using GorillaNameExtension.Models;
using GorillaNetworking;
using HarmonyLib;

namespace GorillaNameExtension.Patches;

[HarmonyPatch(typeof(VRRig), nameof(VRRig.UpdateName), typeof(bool)), HarmonyPriority(100), HarmonyWrapSafe]
internal class NameChangePatch
{
    public static void Postfix(VRRig __instance)
    {
        bool isLocalRig = __instance.isOfflineVRRig || __instance.isLocal;
        string userId = isLocalRig ? PlayFabAuthenticator.instance.GetPlayFabPlayerId() : __instance.Creator.UserId;

        if (!isLocalRig && SpecialtyManager.IsFriend(userId))
        {
            TagConfigService.SetConfigWithGlow(__instance, SpecialtyManager.FriendColour, 0.3f, 100);
            return;
        }

        if (!isLocalRig && !SpecialtyManager.NeedToCheckRecently(userId) && SpecialtyManager.HasPlayedWithUsRecently(userId) is var hasPlayedBefore && hasPlayedBefore.recentlyPlayed == RecentState.Before)
        {
            TagConfigService.SetConfig(__instance, SpecialtyManager.RecentTagColour, 0.3f, 1f - hasPlayedBefore.value, 100);
            return;
        }

        TagConfigService.RemoveConfig(__instance, 100);
    }
}
