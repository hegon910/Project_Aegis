using System;
using System.Collections.Generic;


[Serializable]
public class GameData
{
    // --- ���� ���� ---
     public GameState currentGameState;
    public int playthroughCount;        // ���� ȸ�� (PlayerStats)
    public CommanderTrait activeTrait;
    public int currentChapter;          // ���� é�� (GameManager)
    public int eventPlaylistIndex;      // ���� é���� �̺�Ʈ ���൵ (EventManager)
    public List<int> currentPlaylist;   // ���� é���� �̺�Ʈ ��� (EventManager)

    // --- �Ķ���� ---
    public int politics;                // ��ġ��
    public int militaryPower;           // ����
    public int supplies;                // ����
    public int leadership;              // ������
    public int warSituation;            // ����
    public int karma;                   // ī����

    // --- �÷��� & ��� ---
    public List<int> completedEventIds;        // �Ϸ��� �̺�Ʈ ID ��� (PlayerStats)
    public List<string> unlockedAchievements;  // �޼��� ���� ID ���
    public List<string> completedEndings;      // �� ���� ID ���
    public List<int> playedSubEventGroups;     // �÷����� ���� �̺�Ʈ �׷� (EventManager)

    // --- ��Ÿ ������ ---
    public bool isTutorialFinished;     // Ʃ�丮�� �Ϸ� ����
    public float totalPlayTime;         // �� �÷��� �ð�
    public GameSettings settings;       // ȯ�� ����

    // --- ���̺����� �ֽ�ȭ �񱳿� ��ǥ -- 9.9. ���б� �߰�
    public long lastUpdated;
    // 서버 권위 타임스탬프(UTC ms). Firebase RTDB ServerValue.Timestamp로 채워짐
    public long lastUpdatedServer;

    /// <summary>
    ///     ⺻ 
    /// </summary>
    public GameData()
    {
        // PlayerStats.InitializeStats()   ʱⰪ 
        playthroughCount = 1;
        activeTrait = CommanderTrait.Devost;// [߰] ⺻ ְ Ư ʱȭ
        currentChapter = 1;
        eventPlaylistIndex = 0;
        currentPlaylist = new List<int>();

        politics = 50;
        militaryPower = 50;
        supplies = 50;
        leadership = 50;
        warSituation = 50;
        karma = 50;

        completedEventIds = new List<int>();
        unlockedAchievements = new List<string>();
        completedEndings = new List<string>();
        playedSubEventGroups = new List<int>();

        isTutorialFinished = false;
        totalPlayTime = 0f;
   //     settings = new GameSettings();

        //   ǥ 9.9. б ߰
        lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lastUpdatedServer = 0;
    }
}