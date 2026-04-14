using System;

public static class MissionEvents 
{   
    public static Action<QuestDataSO> OnMissionComplete;
    public static Action<QuestDataSO> OnMissionFailed;
    public static Action<QuestDataSO> OnUpdateProgress;
    public static Action<QuestDataSO> OnMissionStart;
    public static Action<QuestDataSO> OnQuestInviteReceived;
}
